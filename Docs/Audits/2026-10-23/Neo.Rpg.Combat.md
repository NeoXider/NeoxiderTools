# Neo.Rpg.Combat: audit

Status: full source review and owned fixes complete; static checks passed. Unity compilation and
regression execution are centrally pending. Automatic healing remains blocked by a demonstrated
`Neo.Tools.Time.Timer` defect outside this ownership boundary.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Rpg/Combat/Neo.Rpg.Combat.asmdef`.
- Scope: 5 C# files; nested assemblies excluded.

## Scope

Read `AGENTS.md`, `WORKER-CONTRACT.md`, the exact `Neo.Rpg.Combat` manifest entry, and this report.
Every C# file in that inventory was read completely. The actual Combat directory contains exactly
these five files and no nested assembly. Source hashes below identify the reviewed final edits;
they are SHA-256 prefixes, not Unity compilation receipts.

| File under `Assets/Neoxider/Scripts/Rpg/Combat/` | Final lines | SHA-256 prefix | Review coverage |
|---|---:|---|---|
| `AdvancedAttackCollider.cs` | 414 | `c8144fc7d3c1ae1e` | All four physics callbacks, layers/ignore/repeat filters, swing activation/deactivation, damage/heal routing, force fallback, effects/destruction, null references, local collider discovery, gizmos. |
| `AttackExecution.cs` | 197 | `dc84d073974d5c71` | Manual/automatic requests, global gate, speed/multiplier calculation, wind-up/cooldown, synchronous UnityEvent reentrancy, coroutine ownership, disable/reset/reuse. |
| `Evade.cs` | 283 | `06725944900d4f49` | Action and both cooldown modes, scaled versus unscaled time, setters, reactive progress, callback ordering, disable/destroy, timer disposal, serialized former names. |
| `Health.cs` | 216 | `ecf91483c1a9c0ae` | Initialization/authored HP, sign and overflow boundaries, per-hit limits, death events/reentrancy, maximum changes, reactive/serialized state, auto-heal configuration and timer lifecycle. |
| `RpgStatsDamageableBridge.cs` | 58 | `c1ac4bc054004b5c` | Explicit character reference, lazy parent fallback/cache, missing/destroyed target handling, positive-amount guards, multiplier setters, delegation/network boundary. No behavioral change needed; help target updated. |

Context read, without edits: the Combat asmdef, both test asmdefs, existing attack-system and RPG
tests, old AttackSystem component docs, shared `Timer`, relevant `RpgCharacter.Damage`/`Heal` code,
and reactive event signatures. The NeoxiderTools combat reference and relevant timer/extension
catalog entries were checked before adding local lifecycle helpers. Existing package timers,
reactive values, interfaces, and force application are reused; no replacement system was added.

No owned source contains runtime logging or obsolete instance-ID API calls. There are no owned
ScriptableObjects, editor scripts, or NoCode action implementations. Scene/UnityEvent workflows
use the same public runtime methods. Public fields, serialized field names/types, former names,
and public method signatures are retained. Floating timing and multiplier settings are assumed
finite; arbitrary direct writes to public reactive fields remain outside the typed mutation path.

## Findings and fixes

The reproductions below follow directly from the inspected source and arithmetic/control flow.
They were **not executed in Unity**. Severity reflects the observable gameplay consequence.
Line references point to final owned source; the diff preserves the original defect evidence.

### C1 — High: invalid HP mutations and integer overflow

**Source:** `Health.cs:96`, `Health.cs:115`, `Health.cs:133`.

With HP 5/max 10, `TakeDamage(-1)` previously increased HP to 6, and `Heal(-1)` decreased it to 4.
`Heal(int.MaxValue)` added in `int`, overflowed, and clamped a living actor to zero. Neither result
matches damage/heal direction. Both methods now reject nonpositive requests, normalize invalid
negative limits to zero unless the documented `-1` sentinel is used, and calculate HP in `long`
before saturating into the valid integer range. Existing positive per-hit caps and requested-amount
event payloads are preserved, including overheal/overkill payloads.

**Regressions:** nonpositive amounts including `int.MinValue`, maximum-integer healing, and retained
positive-limit/event behavior in `Audit20261023NeoRpgCombatHealthTests`.

### C2 — Medium: maximum changes and serialized HP diverge from runtime state

**Source:** `Health.cs:71`, `Health.cs:133`, `Health.cs:189`.

At HP 10, `SetMaxHp(3)` previously left HP at 10 while reporting a fraction of 1. Negative maxima
were retained. `SetHp(7)` updated the reactive value while leaving serialized `hp` at its old value,
so an Inspector/serialization snapshot described different health.

`SetMaxHp` now clamps the maximum to zero or higher and immediately clamps HP; its event reports
the effective maximum. Every typed HP mutation updates serialized `hp` and both reactive outputs.
`Awake` captures authored HP before maximum normalization so `restoreOnAwake = false` still preserves
the initial value. Direct writes to `Hp.Value` remain possible for API compatibility and are documented
as bypassing these guarantees.

**Regressions:** smaller/zero/negative maxima and effective event values; serialized HP snapshot;
real `Awake` initialization with authored HP 7 and restore disabled.

### C3 — Medium: repeated and reentrant death notifications

**Source:** `Health.cs:96`.

Previously, every damage request at zero HP invoked death again. A nonlethal outer `OnDamage`
listener that issued lethal nested damage could also produce two death notifications for one
transition. Damage now ignores dead actors and records whether its own accepted request crosses
zero before notifying listeners. A later heal/restore permits a subsequent real death transition.

**Regressions:** repeated overkill emits one death, a later heal/death emits the second, and nested
lethal damage from a nonlethal damage listener emits only one death.

### C4 — Medium: auto-heal never starts, and pre-Awake configuration dereferences a null timer

**Source:** `Health.cs:79`, `Health.cs:84`, `Health.cs:89`, `Health.cs:146`, `Health.cs:172`.

The original code constructed a looping heal timer but never called `Start`. `SetHeal(3, 0.25f)`
before `Awake` unconditionally accessed `healTimer.Duration`. The owned fixes start a configured
positive auto-heal on enable/`SetHeal`, stop it on disable or nonpositive configuration, dispose it
on destruction, and allow pre-Awake configuration. Timer-end handling also checks active state.

**Important boundary:** these changes repair the component's missing scheduling and lifetime
ownership. They do **not** make automatic healing functional while the shared looping timer omits
its completion events. See R1; this feature is not claimed verified or fully integrated.

**Regressions:** pre-Awake configuration has observable retained settings. A lifecycle test asserts
actual healing, disabled HP stability, and resumed healing; its first healing assertion is expected
to fail against the inspected shared timer until R1 is fixed. It is not ignored or weakened.

### C5 — High: attack cancellation from events does not cancel the accepted sequence

**Source:** `AttackExecution.cs:71`, `AttackExecution.cs:97`, `AttackExecution.cs:127`,
`AttackExecution.cs:140`.

`StartCoroutine` runs the enumerator synchronously to its first yield. During `OnStartAttack`, the
original coroutine handle was therefore not yet assigned: `ResetAttack` or disable could not stop
it, and it subsequently delivered a hit. Reset from `OnAttack` was overwritten by the old sequence
entering cooldown. At completion, the old sequence cleared its handle after `OnEndAttack`, potentially
discarding a new sequence started by that listener.

Each cycle now has a version invalidated by reset/disable and checked after event callbacks and
waits. The completed handle is cleared before completion listeners, and a returned stale handle
cannot overwrite a newer one. Disabled components reject new requests and report `CanAttack = false`.
Cooldown is initialized in `Awake` and refreshed on requests so direct multiplier changes are used;
one nonnegative wind-up value determines both waits.

**Regressions:** reset and disable from start, reset from hit, and cancellation of a second attack
started by an end listener. Assertions inspect hit counts, readiness, and acceptance/rejection.

### C6 — Medium: stale hitbox deactivation interrupts a newer swing or pooled reuse

**Source:** `AdvancedAttackCollider.cs:292`, `AdvancedAttackCollider.cs:390`.

Two activations before `triggerDuration` elapsed previously retained two scheduled deactivations;
the first closed the second swing early. Disabling retained managed collider state and pending
callbacks. Activation now replaces the scheduled end. Disable cancels it, clears repeat-hit caches,
and disables managed colliders without synthesizing a completion event. Unmanaged collider state
remains scene-owned.

**Regression:** actual timed collider state after reactivation and exactly one normal completion;
disable/re-enable keeps the managed collider closed without a delayed completion event.

### C7 — Medium: ignored hits still publish trigger events and consume the hit allowance

**Source:** `AdvancedAttackCollider.cs:119`, `AdvancedAttackCollider.cs:147`,
`AdvancedAttackCollider.cs:216`, `AdvancedAttackCollider.cs:241`, `AdvancedAttackCollider.cs:267`.

The ignore check was inside `HandleAttack`, after repeat-hit insertion; trigger callbacks invoked
their output event even if that method returned early. An ignored object could therefore trigger
gameplay output and become un-hittable after removal from the ignore list during the same swing.
Filtering now precedes cache insertion and all attack/event handling for 2D and 3D callbacks.
Disabled components also reject callbacks; null trigger colliders are ignored.

**Regressions:** both trigger dimensions assert unchanged HP and zero events for ignored targets,
then exactly one damage/event delivery after removal from the list despite two callbacks. A separate
disabled-component callback test asserts unchanged target HP. Callbacks are delivered through
`SendMessage` to test filtering deterministically; these are not claims about physics-engine delivery.

### C8 — Medium: interrupted evade leaves cooldown active and readiness outputs disagree

**Source:** `Evade.cs:94`, `Evade.cs:97`, `Evade.cs:115`, `Evade.cs:243`.

Disable previously cleared `IsEvading` but left the independently scheduled timer running, retaining
cooldown and permitting later callbacks on a pooled/disabled actor. Ready reactive progress was 1
while `CooldownProgress` returned the stopped timer's 0. Disable now resets cooldown and outputs,
and suppresses the timer completion notification for that cancelled cycle. Both progress paths use
1 when ready. `CanEvade` and the request method reject disabled/inactive components.

**Regressions:** real disable/re-enable checks action/cooldown flags, inactive request rejection,
reactive remaining/progress, convenience progress, and absence of completion notifications.

### C9 — Medium: deferred cooldown is bypassable from completion; duration setter changes current cycle

**Source:** `Evade.cs:179`, `Evade.cs:190`, `Evade.cs:212`, `Evade.cs:255`.

Deferred cooldown originally started after `OnEvadeCompleted`. A completion listener could accept
a new evade before the cooldown existed. `SetCooldownDuration` immediately changed a running timer's
duration/remaining time despite its documented next-start contract. `OnEvadeStarted` could also
disable the object before the old code scheduled completion/started cooldown.

Deferred cooldown is now established before completion listeners. Duration setters preserve running
cooldowns, with duration/interval/time mode configured at the next start. Completion is scheduled
before start listeners so disable can cancel it; no cooldown is started after listener interruption.

**Regressions:** a completion listener cannot restart through a deferred cooldown; a duration change
preserves remaining time and affects the next cycle; disabling from the start listener leaves no
cooldown or delayed action completion.

## Documentation

The manifest-owned `Assets/Neoxider/Docs/Rpg/Combat` directory did not exist. Its old component pages
live outside this ownership boundary under `Docs/Tools/Components/AttackSystem`, so they were read
but not edited. A narrow canonical compatibility entry was created at
`Assets/Neoxider/Docs/Rpg/Combat/README.md`; every owned component's `NeoDoc` now opens it.

The English entry covers all five public APIs, retained event payloads, integer boundaries,
serialized/reactive state, callback cancellation, collider filters and ownership, both time modes,
readiness semantics, bridge placement and network authority, C# usage, and scene/UnityEvent setup.
It explicitly discloses the shared auto-heal blocker. Existing old documentation URLs remain intact.

Three new tests and the new README each have a unique `.meta` GUID. No `DocsEn`, folder metadata,
shared configuration, existing tests, scenes, prefabs, samples, or another assembly's source were edited.

## Substantial improvement proposal

### Inject a typed combat receiver into the compatibility bridge (proposal only)

**Problem.** `RpgStatsDamageableBridge` currently requires a concrete `RpgCharacter` scene component
or parent hierarchy. A custom plain-C# combat receiver cannot be attached through its public API;
programmatic composition must arrange a hierarchy or write another bridge. Its void legacy methods
also give C# callers no forwarding receipt. This limits the constructor-library goal even though
`Neo.Rpg.IRpgCombatReceiver` already exists and is used by the newer combat flow.

**Architecture/public API.** Extend this existing adapter instead of adding a manager or another
combat implementation. Retain serialized `_character` and the current fallback as the default path.
Add an explicit runtime receiver override with:

```csharp
public void Initialize(IRpgCombatReceiver receiver);
public void ClearReceiver();
public bool TryTakeDamage(int amount, out float applied);
public bool TryHeal(int amount, out float applied);
```

Injected damage uses a typed `RpgDamageInfo` and the receiver's `TakeDamage`; healing uses its
existing `Heal(float)`. A configured override takes precedence over hierarchy lookup. `ClearReceiver`
removes only that override and returns to the retained serialized-character/fallback route. Keep
`TakeDamage(int)`/`Heal(int)` as the exact void interface entry points delegating into the same route
once, avoiding double multiplier or network-command application. The bool receipt means a valid
positive request was forwarded, while `applied` is the receiver's synchronous result; a network-routed
request can therefore return true with applied 0. Document that distinction explicitly.

**Compatibility/migration.** This is opt-in and additive: existing scenes retain field names, GUIDs,
parent lookup, multiplier behavior, default `RpgCharacter.Damage`/`Heal` routing, and UnityEvent entry
points. No ScriptableObject stores a scene reference. Callers wanting explicit composition initialize
the receiver after spawning and clear it when changing ownership. Detect destroyed Unity receivers
through their Unity object lifetime rather than relying on interface null comparison alone. Plain-C#
receivers remain owned by the caller; the bridge never disposes them. No new dependency beyond the
already referenced `Neo.Rpg` should be necessary.

**Acceptance scenarios.**

1. A plain-C# fake `IRpgCombatReceiver` receives one multiplied damage/heal request without any actor,
   hierarchy search, or UnityEvent chain.
2. Explicit override beats a parent character; clearing it restores the original route.
3. Zero/negative amounts never reach the receiver; missing/destroyed receivers return false safely.
4. Existing bridge multiplier integration still produces HP 85 after damage 10 at x2 and heal 10 at x0.5.
5. Forwarding a network-owned request issues one command; the receipt does not claim remote HP changed.
6. Pool reuse with an explicitly replaced receiver cannot damage the previous actor.

**Effort/risk.** Approximately 2–3 engineer-days including regression coverage and docs; medium risk
around incoming-damage semantics, destroyed interface-backed Unity objects, and authority receipts.
Coordinate any network acceptance tests with the `Neo.Rpg.Network` owner. The API/design above is
an improvement proposal, not part of the implemented fixes or validation claims.

## Verification and remaining risks

### Checks actually run

- Full inventory comparison: 5 manifest files match all 5 actual Combat C# files.
- Scoped `git diff --check`: exit 0 for owned source/docs/new tests/report.
- Read-only Python static inspection: strict UTF-8 decode and trailing-whitespace checks across
  all 13 owned source/doc/test/meta assets; no replacement characters; delimiter balance,
  explicit-type/no-`var`, and obsolete-ID scans across all 8 owned/new C# files passed.
- Textual declaration comparison against `ca198556`: original field names/types and public method
  signatures are retained in all 5 owned C# files. This is not a binary compatibility/compiler check.
- All five `NeoDoc` targets exist; all three relative README links resolve.
- Existing `Neo.Editor.Tests` references include Combat, Time, Reactive, and both Unity test runner
  assemblies. No asmdef edit or new reference is required. Lifecycle regressions use the existing
  EditMode `EnterPlayMode`/`ExitPlayMode` pattern, with teardown of created objects and exit on failure.
- `rg` GUID scan across `Assets`: the four new GUIDs match exactly their four new metadata files.
- Authored **25 regression cases in 3 new files**, all with prefix `Audit20261023NeoRpgCombat`:
  `HealthTests` (12 cases), `HitFilterTests` (3), `LifecycleTests` (10). They assert real HP, events,
  public state, serialized HP, and collider enabled state. None were run in this worker.

### Coordinator actions and boundary findings

**R1 — High, owner `Neo.Tools.Time`: missing completion events for looping timers.** In the inspected
`Scripts/Tools/Time/Timer.cs`, `StartAsync` invokes `OnTimerEnd` only inside `if (!IsLooping)` after
each cycle. The looping `Health` timer therefore never reaches `OnHealTimerEnd`, even after its owned
missing-start fix. Fix per-cycle end delivery in the timer owner and run
`Health_AutoHealStarts_StopsWhileDisabled_AndRestartsOnEnable`. That case is expected to fail until
this dependency is corrected. No shared timer code or timer tests were edited here.

**R2 — Medium, owner `Neo.Tools.Time`: stop during synchronous timer-start event is lost.** The inspected
`Timer.StartAsync` sets running, invokes `OnTimerStart`, and only then allocates its cancellation token
source. A listener calling `Stop` clears running while there is no token to cancel; execution then
creates a fresh token and enters `RunTimerCycle` anyway. An `Evade.OnCooldownStarted` listener that
resets cooldown/disables its actor can trigger this path. The timer owner should cover immediate
stop/restart during start-event dispatch; the Combat changes do not claim to repair that shared race.

**R3 — Documentation integration.** The coordinator/module-doc owners should link
`Rpg/Combat/README.md` from the RPG and old Tools AttackSystem entries, and supply the new Combat
documentation folder metadata. These files are outside this worker's edit ownership. Inspector help
already points to the new entry through owned source attributes.

**Not executed:** Unity import/compilation, NUnit/Unity tests, physics simulation, Play Mode, build,
MCP, package installation, or any shared runner/service. These were explicitly prohibited for this
worker, as were child agents and commits. Both Unity version branches remain centrally uncompiled;
no new version-specific API was introduced. Force direction/physics delivery, effect-prefab lifetime,
network authority integration, and existing broader RPG regressions still require central runtime QA.

Кратко: просмотрены все 5 файлов сборки, внесены локальные исправления, добавлены 25 регрессионных
сценариев и каноническая документация. Статические проверки выполнены; тесты Unity не запускались.
Автолечение требует исправления общего таймера владельцем `Neo.Tools.Time`.
