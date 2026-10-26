# Neo.Rpg.Network: audit

Status: complete source review and owned fixes. Compilation and test execution are centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/RpgNetwork/Neo.Rpg.Network.asmdef`.
- Scope: 2 C# files; nested assemblies excluded.

## Scope

- `Assets/Neoxider/Scripts/RpgNetwork/RpgAttackNetworkAdapter.cs`
- `Assets/Neoxider/Scripts/RpgNetwork/RpgCharacterNetworkAdapter.cs`

**2/2 owned C# files reviewed completely.** No nested assembly exists in this directory. Changes are
limited to these sources, the assigned `Rpg/Network.md` topic, this report, and a new uniquely prefixed
regression file with package metadata. No existing tests or shared configuration were edited.

| File | Complete review coverage |
| --- | --- |
| `RpgAttackNetworkAdapter.cs` | All three public methods; source/self/parent identity resolution; null inputs; offline/server/host/client-only behavior; authority and projectile registration. No subscriptions, owned resources, persistence, scene search or logging. |
| `RpgCharacterNetworkAdapter.cs` | Awake and server/client startup; all nine Net wrappers; routing, authorization, per-sender rate limiting; initial/delta snapshots; simulation suppression; all 23 command cases; caching, serialization and offline fallback. Serializer is managed and needs no disposal. |

Context read: transport interfaces and command enum, profile DTO/serializer, relevant character
initialization/mutation/profile paths, attack-controller integration, `NeoNetworkComponent`,
`NeoNetworkState`, existing RPG network tests/helpers, test asmdefs and installed Mirror APIs.
The network skill reference and relevant package reference searches were checked before extracting
the existing identity lookup into a shared private method. No parallel gameplay subsystem was added.

Public methods, serialized fields, SyncVar name, command signature, required components and compatibility
wrappers are preserved. No version-specific instance-ID API, editor dependency, unconditional runtime
log or ScriptableObject scene reference was added. Both Mirror and offline branches were inspected.

## Findings and fixes

### F1 — P1: incoming state bypassed the character networking opt-out

- **Trigger:** an adapter remains installed but `RpgCharacter.isNetworked` is false. A snapshot arrives,
  or an otherwise authorized sender sends/has queued a legacy state command.
- **Source/consequence:** `ApplySnapshot` applied profiles unconditionally; `CmdExecute` checked authority
  and rate limiting but not the character flag. A locally simulated character could have resources,
  progression, effects or HP overwritten remotely.
- **Fix:** both ingress paths reject disabled character networking. The command guard precedes the
  rate limiter, preserving the next legitimate command's quota.
- **Regressions:** woven delta serialization asserts enabled/disabled target HP of 75/100. The registered
  Mirror command handler test checks HP and damage events while disabled, after enabling, and after a
  rate-limited repeat. These are authored tests, not executed results.

### F2 — P2: a flag changed after Awake could lose initial client state

- **Trigger:** runtime prefab setup changes the character switch between adapter Awake and client startup.
- **Source/consequence:** Awake copied the switch only once; `NeoNetworkComponent.OnStartClient` then
  used the inherited stale flag. Enabling could skip late-join application; disabling could still apply it.
- **Fix:** override `OnStartClient` to refresh the inherited flag before invoking the base lifecycle.
  The snapshot guard also protects SyncVar callbacks independently.
- **Regressions:** two flag-transition cases replay real woven initial serialization, invoke public
  client startup, and check both HP and the inherited flag. This is a bounded lifecycle replay on an
  unspawned target, not a claim of a real remote-client connection.

### F3 — P2: a separate local source bypassed client projectile blocking

- **Trigger:** a network actor's controller uses a separate source without an identity, or a public
  caller passes null; the adapter has an identity in its own parent hierarchy.
- **Source/consequence:** `CanProcessBuiltInInput` fell back to the adapter hierarchy, whereas
  `ShouldBlockProjectileSpawn` examined only the source. The controller could instantiate a client-local
  projectile; `SpawnProjectile` then does nothing on the client, leaving a projectile without server spawn.
- **Fix:** both policy methods share the existing source-first, adapter-parent-second identity lookup.
- **Regressions:** null/local source cases check input and projectile policy on a child adapter. A local
  object without any identity remains allowed, protecting offline/local composition.

### F4 — P2: Net wrappers dereferenced an uncached character before Awake

- **Trigger:** configure an inactive object, then call adapter `NetDamage`, `NetHeal` or another wrapper.
- **Source/consequence:** `_character` was assigned only in Awake; `RouteOrExecute` entered `Execute`
  and dereferenced null even though `RequireComponent` had supplied the character.
- **Fix:** lazily obtain the required same-object component at the wrapper entry point.
- **Regression:** before activation, damage and healing must produce HP 75 then 80, exactly one event
  each, and leave the object inactive. This case is available without Mirror as well.

### C1 — P1, cross-boundary: snapshot capture re-enters character initialization

**Not fixed here: the initialization contract belongs to `Neo.Rpg`.** With an active server and an
enabled adapter already resolved by the character, apply/rebuild a template containing a stat.
A source-level trigger is `character.Damage(0f)` to resolve the adapter, followed by
`character.ApplyTemplate(templateWithStat)`.

```text
RpgCharacter.RebuildRuntime: _initialized = false
  EnsureInitialized
    RefreshAllDerived(true)
      RefreshStat -> PushSnapshotIfServer
        RpgCharacterNetworkAdapter.NotifyStateChanged
          RpgCharacter.CaptureProfile
            EnsureInitialized  // still not initialized
```

The inspected core assigns `_initialized = true` after `RefreshAllDerived(true)`. Capture can therefore
rebuild dictionaries recursively, causing stack exhaustion or mutation during enumeration. This is
static control-flow evidence, not an executed reproduction. An adapter-only capture recursion guard
would still allow the first nested rebuild and does not establish safe initialization. New regressions
initialize templates before installing the adapter; they do not claim coverage of this unresolved path.

**Coordinator action:** forward C1 to the `Neo.Rpg` worker. Prevent initialization from re-entering
through its own state notifications and publish complete state after initialization/rebuild commits.
Add/run the rebuild regression after fixing the core. No other worker's code was changed here.

Кратко: исправлены обход выключенного networking через команды/снимки, устаревший флаг late join,
обход проверки спавна снарядов и локальные вызовы до Awake. Повторный вход в инициализацию требует
изменения у владельца `Neo.Rpg`; Unity и тестовые раннеры не запускались.

## Documentation

The assigned `Assets/Neoxider/Docs/Rpg/Network.md` did not exist. Added this narrow assembly topic and
pointed both owned components' `NeoDoc` attributes to it. It documents assembly boundaries, typed APIs,
character/attack setup, server authority and legacy client opt-in, current switch behavior, initial/delta
snapshots, matching data catalogs, offline fallback, UnityEvents, and the existing migration guide.
Every relative link resolves. Coordinator may link it from the parent RPG entry point; parent/per-type
documentation is outside this worker's ownership. Both new package files have unique GUID metadata.

## Substantial improvement proposal

### Committed, bounded snapshot publication — proposal only, not implemented

**Problem:** `NotifyStateChanged` captures and serializes the complete persistent profile synchronously
for each notification. Compound resource/stat changes can serialize intermediate states repeatedly;
C1 demonstrates an unsafe initialization boundary. Regeneration can allocate full JSON profiles
frequently, and the persistent DTO doubles as the network protocol without a network revision contract.

**Architecture/public API:**

1. Add transport-neutral readiness/commit APIs to `Neo.Rpg`, for example
   `bool TryCaptureProfile(out RpgCharacterProfileData profile)`, `uint StateRevision`, and
   `event Action<uint> StateCommitted`. Capture returns false during initialization/rebuild. Nested
   mutations publish one committed revision only after resources, stats, effects and death state agree.
2. Let the adapter mark committed revisions dirty and optionally coalesce publication to a configurable
   server interval. A plain-C# publisher takes the snapshot source, explicit clock and codec; the
   MonoBehaviour handles lifecycle and Mirror delivery. Always retain a complete latest snapshot for
   initial spawn/late join, even while periodic updates are deferred.
3. Add an explicit envelope version/revision and configured payload bound. Start with the existing
   profile serializer; a later network-only DTO can evolve independently from save data. Configuration
   assets retain no scene references.

**Compatibility/migration:** preserve `IRpgCharacterNetworkAdapter`, all `Net*` methods and serialized
field names. Keep immediate publication as the compatibility default while refusing incomplete capture;
batching is opt-in. Version the envelope with a legacy JSON reader and coordinate protocol versions
across peers. Existing prefabs need no mandatory migration. Keep `TryRoute(true)` as a transport result;
server acceptance acknowledgments require a separate request/result contract.

**Acceptance scenarios:** rebuilding a stat/resource template on an active server completes without
recursive initialization; a compound update emits one committed revision; a late join gets the latest
complete fields during a deferred interval; denied commands emit no revision; local opt-out rejects
incoming state; immediate mode retains settled-state synchronous publication; oversized snapshots keep
the last valid state with gated diagnostics. Measure JSON allocations and bytes for multi-resource
regeneration before/after batching.

**Effort/risk:** approximately 2–4 development days plus multiplayer QA, shared by `Neo.Rpg` and
`Neo.Rpg.Network`. Moderate risks: notification ordering, effect expiry during capture, initial-spawn
timing and protocol compatibility. Land the core initialization fix first, then optional batching and
versioned envelopes as separate changes.

## Verification and remaining risks

### Checks actually run

- Read complete root `AGENTS.md`, worker contract, exact manifest entry, starting report and both
  owned source files. Inventory comparison confirmed exactly **2/2** owned files and no nested assembly.
- Scoped `git diff --check` returned **exit 0**. Git reports its existing LF-to-CRLF checkout advisory,
  with no whitespace errors. An additional whitespace/final-newline scan covers the new untracked files,
  which ordinary `git diff` does not inspect.
- Static Python checks passed for strict UTF-8/no replacement characters, existing public signatures
  against `ca198556`, unchanged SyncVar field name, **23/23** enum dispatch cases, explicit types and
  obsolete instance-ID scan. MIRROR/non-MIRROR directive and delimiter checks passed on both sources
  and the new test file; these checks are **not C# compilation**.
- Checked all owned doc links and both new GUIDs for uniqueness across `Assets`. The existing
  `Neo.Tests.Play` references already cover all RPG/network/Mirror dependencies; no asmdef edits needed.
- Checked installed Mirror source signatures for pooled readers/writers, callbacks, command hash/
  registration and injected sender handling; checked the character event properties used by the tests.
  Regression helpers encode real command arguments and call Mirror's registered delegate without
  reflection into private adapter fields or generated UserCode methods.
- Added **9 NUnit cases** in
  `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoRpgNetworkRegressionTests.cs`: one offline/pre-Awake
  case and eight Mirror cases. **No cases were executed.** No Unity/MCP, compiler, build/test runner,
  install, child agent, commit, scene/prefab operation or shared configuration mutation was performed.

### Coordinator verification and remaining risks

- Compile sources/tests with Mirror weaving and run the nine regressions centrally. Also compile the
  supported Unity versions and a non-Mirror configuration; structural checks do not prove compatibility.
- C1 requires the `Neo.Rpg` owner's fix and regression before claiming active-server rebuild safety.
- New Mirror tests replay lifecycle, delta and command boundaries in one process with the existing dummy
  transport. Actual packet delivery, ownership transfer, reconnect, scene reactivation and projectile
  prefab registration remain integration checks.
- Legacy client state commands retain their broad explicit opt-in policy. No new per-command game-rule
  validation, numeric/string bound, acknowledgment or schema migration is claimed; those need a public
  contract rather than a silent audit rewrite.
- Independent runtime QA was unavailable under the no-agent/no-runner constraint. All successful
  validation reported here is source review and static checking.
