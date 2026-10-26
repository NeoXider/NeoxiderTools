# Neo.Tools.Spawner: audit

Status: source review and scoped fixes complete; static validation passed. Unity compilation, Mirror weaving, and Edit/Play Mode execution remain with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Spawner/Neo.Tools.Spawner.asmdef`.
- Scope: 10 C# files; nested assemblies excluded.

## Scope

All ten manifest C# files were read completely, including conditional networking paths. No nested assembly exists inside this owned source directory; no other worker's source, tests, docs, or shared configuration was changed.

| File under `Assets/Neoxider/Scripts/Tools/Spawner/` | Review coverage / disposition |
|---|---|
| `Despawner.cs` | Disable/component-disable, inactive targets, effects and parent selection, reentrant UnityEvents, return/reissue during an event, pooled/unpooled/null targets. Fixed disable/reentry and generation guards. |
| `IPoolable.cs` | All three typed callbacks, root/child discovery, destroyed component handling, cache lifetime, prewarm semantics. Runtime contract retained; incorrect public docs corrected. |
| `NeoObjectPool.cs` | Constructor boundaries, retention, prewarming, all callback stages, externally destroyed objects, cache pruning, foreign/double returns, storage roots, Clear, server network lifecycle. Fixed boundaries, dead retrieval, ownership and Mirror unspawn. |
| `PoolExtensions.cs` | Both extensions delegate to SpawnUtility; null handling and manager/fallback selection traced. Corrected obsolete Instantiate description. |
| `PoolManager.cs` | Singleton/duplicate lifecycle, persistent storage, default/preconfigured pools, null entries and arguments, Get/Release and diagnostic paths. Fixed prewarm guards, zero maximum propagation, and null prefab. |
| `PoolableBehaviour.cs` | Reviewed every no-op callback and inheritance contract. No runtime defect found; clarified callbacks in docs. |
| `PooledObjectInfo.cs` | Owner, IsInPool, generation, public Return and missing-owner destruction. Retained public properties; runtime button gated and existing destruction primitive reused. |
| `SimpleSpawner.cs` | Null prefab, world offset/rotation, optional parent, pooled/Instantiate paths and Inspector action. Corrected explicit local type and pooling description. |
| `SpawnUtility.cs` | Every Spawn overload, nulls, parent/local pose, manager/fallback selection, scene helper subscription/unsubscription, Single/additive loads, persistence, static reset, Clear and unpooled destruction. Fixed fallback storage and Edit Mode cleanup. |
| `Spawner.cs` | Start/Stop/disable, complete Loop/Waves enumerator, attempt limits, event order, delayed generation checks, list ownership/count/Clear, every client/Command/RPC branch, all supported collider shapes, spawn points, rotations and bounded deny retries. Fixed restart overlap, stale rentals, reentrant Clear, capsule geometry and host duplicate events. |

Dependencies/tests read as context: `Singleton<T>`, `NeoNetworkComponent`, installed Mirror `NetworkServer.UnSpawnInternal`/client unspawn handling, current Spawner/deny-zone edit tests, and existing Play Mode network test helper. Existing `NeoObjectPool`, `PooledObjectInfo.SpawnGeneration`, `NeoDiagnostics`, and the pool's destruction helper were reused; no parallel pool framework, dependency, asset generator or configuration was added. Relevant `neoxider-tools` spawning/pooling and extension references were consulted before edits.

The scene/UnityEvent layer remains an adapter over the typed public APIs. No serialized field or public callable declaration was removed. No scene/prefab/sample layout changes, commits, installs, Unity/MCP, build/test runners, shared services, or child agents were used.

## Findings and fixes

Evidence below is source/control-flow evidence against baseline `ca198556`, followed by authored observable regressions. Tests have **not** been executed.

### SP-01 — P1: a foreign return corrupts pool ownership

Baseline `NeoObjectPool.ReturnObject` checked only `IsInPool`; `otherPool.ReturnObject(ownerRental)` released a live foreign object into the other pool. The object became inactive, remained tagged with its original owner, and could then be retrieved from the wrong pool.

Fix: reject missing metadata, a different owner, and duplicate returns before calling Unity ObjectPool. Public void API and tolerant null handling are preserved. Regression: `ReturnToWrongPool_DoesNotStealOrDeactivateRental` checks active state, both inactive counts, ownership, rightful reuse and distinct foreign-pool retrieval; `ReturnUnownedObject_DoesNotAdoptIt` covers unpooled inputs.

### SP-02 — P1: destroyed inactive objects break the next retrieval

Baseline `NeoObjectPool.OnGetFromPool` dereferenced a cached GameObject even if a caller or scene unload had destroyed it. The next Get failed before delivering a rental; pruning the separate component cache did not remove the Unity ObjectPool entry.

Fix: skip lifecycle callbacks on dead entries, drain those entries in `GetObject`, remove their component cache records, and keep retrieving until a live object is available. Regression: `DestroyedInactiveInstance_IsReplacedWithLiveRentalAtRequestedPose` destroys a returned instance and verifies its replacement's collider, active state, owner, pose and inactive count.

### SP-03 — P2: documented pool limits and empty initial pools were incorrect

Baseline constructor used maximum 100 when configured maximum was zero, although PoolConfig/manager tooltips promised no limit. PoolManager also translated zero to 100 before forwarding it. `new NeoObjectPool(prefab, 0, false)` passed zero as Unity's maximum and failed; negative initial sizes were invalid too. A null prefab failed later in Instantiate, rather than at the public constructor boundary.

Fix: centralize normalization in the existing constructor: initial size at least zero; non-expanding retention at least one; nonpositive expandable maximum uses `int.MaxValue`; null prefab throws a named ArgumentNullException. Manager forwards its configured maximum unchanged. Clarify that Unity ObjectPool limits inactive retention, not active population; no new gameplay capacity semantics are introduced.

Regressions: direct and manager `ZeroMaximum`/`UnlimitedRetention` tests retain 101 returned instances; initial zero/negative non-expanding cases verify a usable rental and subsequent reuse; null constructor input checks parameter `prefab`.

### SP-04 — P2: duplicate/null manager configuration still performs work or throws

Baseline `PoolManager.Awake` continued prewarming after `Singleton.Awake` scheduled a duplicate for destruction. Null `PoolConfig` entries were dereferenced. `PoolManager.Get(null, ...)` attempted a null dictionary key.

Fix: prewarm only the current singleton, ignore null entries, and return null before manager/pool resolution for a null prefab. Regressions check zero created instances for a destroyed duplicate, a valid two-item prewarm after a null entry, and null Get without creating a storage child. The singleton's existing protected current-instance predicate is reused; no singleton source is edited.

### SP-05 — P1: Despawn On Disable does not return disabled objects

Baseline `Despawner.OnDisable` called `Despawn`, which immediately returned when `activeInHierarchy` was false. GameObject disable therefore bypassed the advertised pool return and effect/event. Simply removing that check would cause a second despawn through pool-driven OnDisable or an OnDespawn listener, and could return a new rental issued by that listener.

Fix: use a request guard scoped to the current pool generation, reset on enable, skip already-pooled objects, and verify the generation again after the event. Inactive targets are supported. Normal pool return has already set IsInPool, so it emits no additional Despawner effect/event.

Regressions cover inactive direct calls, actual Play Mode disable, ordinary pool return after reuse, bounded event reentry, listener return/reissue, and two generations without Unity enable callbacks. Assertions cover event counts, active state, IsInPool and exact retention counts.

### SP-06 — P1: Stop/Start revives an old sleeping spawn loop

Baseline `Spawner.StopSpawn` only cleared the shared bool. If StartSpawn ran before the old WaitForSeconds completed, the old coroutine observed the new true flag and produced objects alongside the new coroutine.

Fix: stop and clear the specifically owned production coroutine; keep independent delayed despawns running. `StopThenRestart_DoesNotWakePreviousSleepingLoop` starts a short-delay loop, immediately replaces it with a ten-second loop, waits beyond the old delay and requires exactly two initial spawns. `StopSpawn_KeepsPendingRentalExpiry` verifies that stopping production still expires its already-issued rental.

### SP-07 — P1: stale tracked objects can despawn another producer's rental

Baseline SpawnedObjects retained pooled references after external return. When another spawner reused one, the first spawner's Clear returned that newer rental; active counts also attributed it to the old producer. Reuse by the same spawner produced duplicate entries. Clear enumerated the mutable list while invoking release callbacks, so a callback spawning another object could invalidate enumeration or lose that new object in the final list clear.

Fix: capture pool generations at tracking, deduplicate instance references, validate generations when counting/clearing, and clear a snapshot before release callbacks. Snapshot generations also protect later entries from callback-triggered reissue. Stale delayed expiry now leaves a newer tracked record intact. The public List property remains available.

Regressions: cross-spawner Clear leaves the second rental active; active count prunes the old lease; same-spawner reuse/old expiry retains exactly one current record; a release callback can spawn a new object which stays active and tracked after Clear.

### SP-08 — P2: capsule sampling ignores direction and includes outside points

Baseline `GetRandomPointInCollider` always replaced Y with a value spanning the full capsule height. X/Z capsules thus sampled a Y-oriented volume; even Y capsules could sample outside their rounded caps.

Fix: sample a sphere plus an offset along the actual capsule axis, using the scaled world radius and center-segment length. Tests call public `GetSpawnPosition` for all three directions and compare 256 samples per scale against `CapsuleCollider.ClosestPoint`, with translation, rotation, positive/nonuniform/negative scales and a restored fixed random seed. This is containment coverage; uniform volume density is not promised.

### SP-09 — P2: Mirror hosts receive duplicate local events

Baseline server paths invoked spawn/wave events locally and then invoked RPC handlers on the same host. Those handlers skipped only `isServerOnly`, which is false on a host, producing two notifications and two tracked entries.

Fix: skip the RPC notification body on any server instance. Pure clients retain their notification path. Conditional Mirror regression `HostWave_FiresEachEventAndTracksEachObjectOnce` uses the existing host helper and verifies three event counts, the list count and the server spawn registration.

### SP-10 — P1: pooled network returns leave old network registrations alive

Baseline server Spawner called NetworkServer.Spawn, but pool return only deactivated the GameObject. No path removed it from server/client spawned registries before reuse. Installed Mirror source confirms that `NetworkServer.UnSpawn` removes both registrations, sends observer removal, resets identity state and preserves the reusable server object (`Assets/Mirror/Core/NetworkServer.cs`, UnSpawnInternal).

Fix: in the existing common pool release callback, after marking IsInPool and running lifecycle callbacks with their existing network context, unspawn an identity only when it is actually registered in the active server registry. This happens before storage/deactivation. The existing IsInPool guard prevents reentrant host unspawn handlers or Despawner.OnDisable from double-releasing it. All Return/Release/Despawn/Clear/expiry routes benefit without changing public signatures. The code is conditional on the existing MIRROR define and uses an already-declared assembly reference.

Conditional regression `PooledNetworkRental_ReturnUnregistersItAndReuseRegistersANewSpawn` checks one release callback with preserved server context, both old registries, reset netId, inactive/pool state, same-instance reuse, and a fresh registered netId. Remote client integration remains an additional coordinator scenario; this worker did not execute a host or client.

### SP-11 — P3: incorrect public docs and unsafe runtime Inspector buttons

IPoolable documented nonexistent OnSpawn/OnDespawn; NeoObjectPool was called a legacy/internal alias and linked back to obsolete documentation; SpawnUtility was described as an addable component. Pooling tooltips incorrectly promised Instantiate fallback. Runtime-mutating buttons in Spawner, Despawner and PooledObjectInfo were available in Edit Mode; unpooled/root destruction used Destroy there.

Fix: update the owned canonical entry and API guides; mark runtime buttons PlayModeOnly; reuse the pool's existing play/edit destruction primitive across owned APIs. Correct both non-anonymous `var` locals. `DespawnUnpooledInactiveObject_DestroysItInEditMode` observes actual destruction and one event. Attribute/type/comment edits need no implementation-mirroring tests.

## Documentation

Updated ten existing pages under `Assets/Neoxider/Docs/Tools/Spawner/`: README, NeoObjectPool, IPoolable, PoolableBehaviour, PoolManager, PooledObjectInfo, SpawnUtility, Spawner, SimpleSpawner and Despawner. `ObjectPool.md` already provides the valid legacy migration pointer and remains unchanged.

The canonical README now links typed runtime entry points and UnityEvent scene workflows. The guides explain actual callback names/order and prewarming, nullable inputs, inactive retention versus active limits, fallback root ownership/Single/additive scene behavior, generation-aware Clear/count/expiry, world-space offsets, Start rather than Awake autostart, Play Mode buttons and Mirror server unspawn/client handler responsibilities. They explicitly retain the current before-final-pose get callback semantics and the caller's responsibility for active external-parent rentals during fallback clear.

All eleven pages' local links were checked. README's existing separator corruption was confirmed from actual UTF-8 Unicode escapes before replacing the affected redundant link line; console appearance alone was not used to rewrite text. Existing English style is retained. Ключевой результат: спавнер владеет конкретной выдачей объекта, поэтому очистка старого владельца не забирает новую выдачу из пула.

## Substantial improvement proposal

### Generation-safe leases and explicit lifecycle/retention policy (proposal only)

**Problem.** A GameObject reference alone cannot identify its current rental; this audit needed separate generation snapshots in producer and despawner code. The mutable public list exposes records that can be altered independently of those snapshots. Prewarming invokes gameplay callbacks, OnPoolGet sees the previous pose, Clear only destroys inactive entries while explicitly parented active rentals can outlive fallback storage, and active population limits are easy to confuse with retention. Optional Mirror logic is currently compiled into the same assembly.

**Architecture / public API.** Keep the prefab pool and scene wrappers, and add a small typed lease contract:

```csharp
public readonly struct PoolLease
{
    public GameObject Instance { get; }
    public int Generation { get; }
    public bool IsCurrent { get; }
    public bool TryReturn();
}

// Proposed additive overloads, not implemented in this audit:
// bool NeoObjectPool.TryRent(SpawnRequest request, out PoolLease lease)
// void NeoObjectPool.Clear(PoolClearMode mode)
// IReadOnlyList<PoolLease> Spawner.ActiveLeases
```

`SpawnRequest` carries world pose and parent. A plain C# lease registry owns generation validation and optional active-cap accounting; NeoObjectPool handles Unity creation/storage; MonoBehaviour spawners remain adapters. A pool policy names `PrewarmCount`, `InactiveRetentionLimit` and optional `ActiveLimit` separately. The new rent path applies pose/parent before get notifications, with explicit creation-only warmup. `PoolClearMode` distinguishes inactive-only clearing, rejecting clear with live leases, and invalidating/destroying all owned leases. Networking can move to an optional integration adapter/leaf assembly with before-release unspawn hooks and client handler support, preserving one authoritative release transition.

**Compatibility / migration.** Add these APIs first. Preserve existing constructor, serialized fields, GameObject Get/Return paths and public SpawnedObjects list. Legacy APIs retain callback order and warmup behavior; the new request/policy path is opt-in. Expose ActiveLeases as the supported observation API and document legacy list mutation before considering deprecation in a major version. Existing prefabs need no migration; an optional editor migration can map legacy retention fields only after an explicit product decision. Assembly changes belong to the coordinator and are not part of this patch.

**Acceptance scenarios.** (1) An expired copied lease cannot return or remove a newer rental. (2) Two producers can exchange the same physical object without ownership/count interference. (3) New-path callbacks see the requested pose and creation-only warmup emits no gameplay get/release events. (4) Retention 0/unlimited, initial 0 and independent active caps have named deterministic outcomes. (5) Each clear mode accounts for externally parented active rentals and late returns without orphan storage. (6) Single/additive loads and persistent roots preserve the selected ownership policy. (7) Duplicate release and host reentrant unspawn produce exactly one release transition; remote replicas unregister and respawn correctly. (8) Legacy GameObject APIs, callback order and serialized scenes continue to work unchanged.

**Effort / risk.** Approximately 3–5 focused developer days plus one integration day for edit/play and Mirror host/remote scenarios. Highest risks are event reentry, callback-order compatibility, Unity destruction timing and optional-provider packaging. Stage registry/lease support first, then opt-in lifecycle policy, then optional network extraction; retain the current API until migration evidence is available. This design is proposed, not delivered or tested here.

## Verification and remaining risks

Checks actually run:

- Full reads of AGENTS.md, WORKER-CONTRACT.md, the exact manifest entry, this report and all ten inventoried sources; relevant current tests/dependencies read as context.
- Python read-only static checks: exact owned inventory; UTF-8 decoding and selected known encoding marker absence; existing field names and public callable declarations compared with `git show ca198556:<owned path>`; no non-anonymous `var` or obsolete Unity ID calls in owned C#.
- Separate lexical delimiter/preprocessor checks for MIRROR enabled and disabled in all 14 scoped C# files (ten owned sources plus four new test/support files). These are lexical checks, **not compilation**.
- All local links in the eleven owned module pages resolve.
- Four new `.cs.meta` GUIDs searched across all Assets metadata, including ignored files and CRLF metadata: each occurs exactly once. New files have UTF-8 text and clean trailing whitespace.
- Scoped `git diff --check -- <owned sources/docs/report and new test/meta paths>`: exit 0. Git's existing SimpleSpawner line-ending conversion warning is informational; no whitespace errors were reported.

New regressions: `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsSpawnerTests.cs` (15 cases), `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPlayTests.cs` (12 cases), `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoToolsSpawnerNetworkTests.cs` (2 conditional Mirror cases), plus the uniquely named Play Mode pool probe. Every new package file has matching unique GUID metadata. **29 cases authored; zero executed by this worker.** No test asmdef edit is required for the current declared references.

Coordinator actions / unverified risks:

1. Compile/import and run the new Edit/Play fixtures plus existing SpawnerTests, AuditFixesSpawnerTests and SpawnerDenyZoneTests. Compile both conditional networking branches and supported Unity versions; static delimiter checks do not validate C# binding, Unity API versions, physics or Mirror weaving.
2. Execute the conditional host tests with MIRROR defined in the test assembly. Current Play asmdef already references Mirror, Neo.Network and Neo.Tools.Spawner but declares no versionDefines; check the central symbol/provider setup rather than assuming these tests were discovered. No shared define/asmdef was changed.
3. Add a remote-client integration pass for replicated pool return/respawn and any custom Mirror unspawn handlers. Host custom handlers may destroy the object; supported handlers must preserve the pooled server rental. Remote local return still does not request an authoritative server despawn.
4. Verify Single/additive scene transitions, persistent fallback storage and enter-play with domain reload disabled. The fallback setting now resets to its documented default; only static lifecycle inspection was possible here. Explicitly parented active fallback rentals remain caller-owned on Clear; return them first.
5. Delayed despawns remain spawner-owned Unity coroutines: GameObject disable/destruction cancels them. Get callbacks still precede final pose, warmup still invokes get/release, and callbacks must not throw or return/re-rent their own object mid-transition. These retained behaviors are documented; the proposal provides an opt-in replacement.
6. The legacy mutable SpawnedObjects list remains for compatibility; external mutation can bypass tracking metadata. Capsule containment with nonuniform/negative scale and custom transformed hierarchies needs the authored physics tests; the sampling algorithm is not a uniform-density guarantee. Unconditionally declared Mirror assembly references remain a packaging/configuration owner concern for genuinely Mirror-free package builds.

Independent QA and all Unity/runtime claims remain unverified because the worker contract explicitly reserves editor/build/test execution for the coordinator. No cross-boundary source edits are requested by these fixes; integration and provider/assembly configuration checks belong to the coordinator.
