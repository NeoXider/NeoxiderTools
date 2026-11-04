# Shared interaction queries

**What it is:** `InteractionPhysicsQueries` (`Neo.Tools`, assembly `Neo.Tools.InteractableObject`) shares complete
raw physics hits across automatic targets. `InteractionRayProvider` supplies ray geometry and an
invalidation shortcut. Existing scenes do not need a new component, controller or registration.

**How to use:** nothing to set up: interaction components share hits automatically. Call
`InteractiveObject.InvalidateCachedColliders()` after replacing a target's colliders at runtime, and read the
[C# query API](#c-query-api) when you query interaction hits from your own code.

## What is shared

The first consumer executes a query; subsequent consumers of its exact context in the same stable
epoch borrow its hit set. The key includes camera identity, viewport/display/render target, the
default physics world, query kind, exact float origin/direction/distance, physical mask and relevant
trigger/contact/global policy. Float geometry is compared exactly, without `Vector3.operator ==`.
Planar 2D queries retain the original `Vector2` origin and direction in both the key and native call,
including non-unit, tiny and zero directions. They do not round-trip through a normalized 3D `Ray`.

3D rays, 2D screen-ray intersections, legacy 2D planar rays and explicit-filter 2D rays are distinct
kinds. `Both` normally has center and pointer rays; identical hover/click rays can share. Different
physical masks and trigger policies use separate results. Masks are not widened into AllLayers.

The cache does not choose one global winning target. Each consumer still applies its explicit
collider/hierarchy selection, range, ignored source hierarchy, obstacle policy and existing Core
`InteractionQueryMath` rules. These post-filters do not enter the raw-query key. With obstacles off,
multiple targets along the same ray may remain eligible, as before.

Range LOS and keyboard look retain their original directions, lengths and masks. Distinct LOS rays
are distinct physical work; sharing camera rays does not make all target-specific checks constant.
`PhysicsEvents2D` / `PhysicsEvents3D` remain real collision/trigger relay channels.

## Target projection

Each consumer projects the borrowed raw hits onto its own target. Two exact strategies produce the
same selected target, hit point and equal-distance tie order (raw order decides ties):

- **Complete projection** inspects all H raw hits and marks target hits, blockers and ignored
  source-rig colliders. It is used for range line of sight (foreign triggers block it, and the ray is
  unique per target anyway), for raw sets smaller than 8 hits, where direct checks are cheaper than an
  index, and as the fallback of a look-ray miss while a debug ray is recorded, because the debug ray
  ends at the nearest non-target hit. It is also used for a target whose hierarchy held more colliders
  than the raw set at its last enumeration (a building or vehicle root with hundreds of child colliders):
  enumerating C target colliders would cost more than H hierarchy checks. That size hint is measured again
  after 64 such projections, so a target that lost colliders returns to the index; it never changes the
  selection, only the strategy.
- **Indexed projection** serves camera and look selection. The first consumer of a shared entry builds
  a collider-to-raw-index map once per epoch; every consumer then enumerates its own live collider
  hierarchy (or its explicit target collider) and reads only its own hits. With `checkObstacles` on,
  the entry is also sorted by distance once per epoch, and each consumer walks only the hits in front of
  its nearest target hit until the first blocker; a blocked target keeps that blocker as a candidate, so
  the clear-path verdict and the returned target hit are unchanged. N consumers of one dense ray cost
  about H + N instead of N x H, with obstacles on or off. A miss returns no non-target candidates.

Nothing that decides the selection is cached between calls: reparenting, explicit collider changes,
2D trigger flags, ignored source rigs, disabled and destroyed colliders are evaluated live exactly as
before. Equal-distance targets keep raw order. Only the collider count above is remembered, as a cost hint.

## Completeness and lifetime

A full NonAlloc buffer is not accepted as complete. The backend obtains the full equivalent result
through the corresponding all-hits query, or a reusable List for explicit-filter 2D rays, then grows
its retained buffer with spare capacity. The initial attempt and full fallback are both counted.
An unchanged warmed result fits without another overflow allocation. Each consumer's classified
buffer can also grow; it never truncates the complete result to the former 16 entries.

`Hits3D` / `Hits2D` are read-only borrowed views, not permanent copies. Check `IsComplete`, inspect
`Count` and read hits by index; the shared backing arrays cannot be sorted or modified by callers.
`IsValid` becomes false on epoch expiry, `IsComplete` becomes false, and indexed access throws.
Complete processing and save necessary value results before invoking callbacks or another fresh
query. Copy data explicitly if it must outlive the epoch.

The epoch ends on frame advance, explicit invalidation and known interaction callback boundaries.
Before external interaction event dispatch it is invalidated; a `finally` boundary invalidates it
again after dispatch. Nested queries bypass sharing while a callback is active. Event order stays
synchronous. This includes hover/click/range/down/up, physics relay and ToggleObject dispatch.
Disable/enable and collider-cache invalidation also end the epoch. Slots and arrays are reused
across epochs; old query keys and hit references are cleared, and subsystem registration clears
static storage. Retained array capacity follows the high-water mark of concurrently used slots.
`ReleaseBuffers()` explicitly ends the epoch and releases that retained capacity at an application
scene or memory boundary. It does not reset diagnostic counters.

External movement or manual physics calls between other components' Update methods cannot be
detected automatically. The application declares that boundary:

```csharp
blocker.enabled = false;
Physics.SyncTransforms();
InteractionRayProvider.InvalidateQueries();
```

Invalidation does not execute physics or synchronize transforms. Main-camera provider replacement,
mode changes and provider disable invalidate existing query data. Camera retention and checkpoint
selection still follow the existing `InteractionCameraResolver` / component configuration policy.
A live disabled rendering Camera with valid viewport dimensions remains a geometry source for
direct provider calls and cached target center rays. Provider selection retains its own enabled check.

## C# query API

`Raycast3D`, `GetRayIntersection2D`, and the layer-mask / `ContactFilter2D` overloads of `Raycast2D`
operate on Unity's default physics scenes. A camera is a source-context identity, not a request to
query that camera's local physics scene. The APIs run on Unity's main thread.

External synchronous calls default to `QueryMode.Fresh`; they start a new epoch. The public
`InteractiveObject.IsInInteractionRange` property likewise keeps fresh-check behavior. Use Shared
only within an application-defined stable pass:

```csharp
InteractionPhysicsQueries.Hits3D hits = InteractionPhysicsQueries.Raycast3D(
    camera, ray, 100f, ~0, QueryTriggerInteraction.Collide,
    InteractionPhysicsQueries.QueryMode.Shared);
if (hits.IsComplete)
{
    int count = hits.Count;
    for (int i = 0; i < count; i++)
    {
        RaycastHit hit = hits[i];
        // Classify this consumer's target/blocker without modifying the shared data.
    }
}
```

3D explicit Collide/Ignore is independent of the global trigger flag. Legacy 2D queries and the
component's 2D camera/look paths retain `Physics2D.queriesHitTriggers`; a component include-trigger
option cannot recover hits excluded by that native query. Explicit-filter 2D LOS passes the original
`ContactFilter2D` unchanged to Unity's array and complete-list overloads. Its trigger flag is not a
promise to override Unity's active native policy, and the backend never enables a global physics
flag to obtain a hit. Compare those native overloads under the same flags when diagnosing a result.
The global start-inside setting and every contact-filter field are included where applicable.

## Measurement

`Counters` includes actual backend Physics calls by kind, cache hits, overflow fallbacks, fresh
requests, invalidations, frame advances, callback boundaries and calls inside callbacks.
`CameraNativeCalls`, `LookNativeCalls` and `RangeNativeCalls` identify the requester that performed
the native call. Purpose is diagnostic only and never splits an otherwise identical physical key.
If look and range share a query, its native call belongs to the first requester; the other is a hit.
`ResetCounters()` resets measurements without invalidating data or running a query.
`ClassificationPasses` counts target-local projection passes. `ClassifiedHits` counts raw entries actually
inspected: H per complete projection (including entries later excluded), H once per entry for an index
build and once for a distance order, plus the target's own entries and the hits walked in front of it per
indexed projection. They are CPU-work diagnostics, not native calls. The target-side collider enumeration
of an indexed projection is not a raw entry and is not counted; the large-hierarchy NEO_PERF cases measure
it as time. Before the indexed projection `ClassifiedHits` was the sum of H over all projections, so the
same scene now reports smaller values.

Profiler markers `Neo.Interaction.Native.Raycast3D`, `.Intersection2D`, `.Raycast2D` and
`.FilteredRaycast2D` enclose the actual native attempts/fallbacks, with counters immediately beside
those calls. `Neo.Interaction.Classify3D` / `.Classify2D` enclose per-target projection, hierarchy
filtering and buffer growth; `Neo.Interaction.Callbacks` identifies observer dispatch and
`Neo.Interaction.AutomaticUpdate` encloses the complete target pass, including Core selection.
Use these together to distinguish physics savings from classification, callback and total CPU cost.

The performance contract is K native queries for N consumers of K complete, warmed contexts in a
stable epoch. Cold overflow, callback/world invalidation, fresh calls and unique LOS are reported
separately and included in totals. Transitions that invoke callbacks can legitimately begin another
epoch within the frame. External EventSystem/PhysicsRaycaster calls are outside this counter and
must be included separately in an aggregate profiler capture.

Authored Play-mode regressions use real PlayerLoop Update for 1/100/1000 distinct collider targets
in 3D/2D camera and keyboard-look routes, positive hits and moved-off-ray controls. With obstacles
off, all collinear targets remain eligible; the 1000-target positive frame inspects H + N = 2000 raw
entries in total (one index build plus one entry per target), and a stable miss inspects none.
Stable measurements exclude callbacks/unique LOS and print their separate counters. Keyboard and
automatic mouse scenarios queue real virtual Input System states; optional-backend absence is an
explicit skip. Stable query fixtures assign individual settings without reconstructing their null
event objects; even an empty non-null UnityEvent is a real callback boundary. After moving targets,
first assert the actual miss transition and hover exits; measure
the next unchanged camera miss frame only after those callbacks finish. A transition can create
multiple epochs; its native-call count is separate from the stable-miss budget.

The query fixtures now run in Play mode and retain distinct LOS, 17/64/1000-hit completeness,
post-filter divergence, exact keys, global/explicit policies, direct planar-native parity, fresh
range and callback invalidation. Synchronous explicitly driven callback/query controls remain unit
controls; actual automatic delivery is measured by the separate PlayerLoop cases. Explicit 2D
trigger controls compare complete direct native array/list results, retained-buffer queries and
cached results under each flag combination, alongside hard 1/17-hit positives, trigger exclusions
and layer-mask negatives. Indexed-projection parity tests compare both strategies on the same borrowed
lease (nested and reparented children, explicit colliders, disabled/destroyed/trigger colliders, compound
equal-distance children, blockers, foreign triggers and ignored rigs in front of the target), and dense
partial-miss and obstacles-on PlayerLoop cases prove that missed or blocked consumers add no complete
fallback. Native query counts alone establish neither CPU nor allocation improvements; use the
performance suite below for those.

## Performance suite

The explicit `Performance` category measures the real PlayerLoop and prints one parsable line per case,
`NEO_PERF <case> key=value ...`, to the Unity log and the test output. It never runs in a normal pass:

```text
Unity -batchmode -projectPath <project> -runTests -testPlatform PlayMode -testCategory Performance
```

Interaction cases (`Audit20261023NeoToolsInteractableObjectLifecycleTests.Perf_Interaction_*`):

| Case id | Workload |
|---|---|
| `interaction.<route>.positive.n<N>` / `.stable_miss.n<N>` | route `camera3d`, `camera2d`, `keyboard3d`, `keyboard2d`; N = 1, 100, 1000 collinear targets on one shared ray, obstacles off; keyboard frames alternate press/release edges |
| `interaction.<route>.positive_complete.n<N>` | the same scene with the complete projection forced on every target (N = 100, 1000): the pre-index N x H control |
| `interaction.camera3d.partial_miss.n1000`, `interaction.camera2d.partial_miss.n1000`, `interaction.camera3d.partial_miss_complete.n1000` | 1000 targets, 100 on the ray, 900 missed consumers of the same query; default and complete control |
| `interaction.camera3d.obstacles_on.n100`, `.n1000` and `.obstacles_on_complete.n100`, `.n1000` | default `checkObstacles = true`: the clear-path walk (4N - 1 inspected entries per frame) and its N x H control |
| `interaction.camera3d.large_hierarchy.n20`, `_forced_index.n20`, `_complete.n20` | 20 targets with 201 colliders each, off a ray of 12 hits: default (size hint), index without the hint, complete control |
| `interaction.camera3d.positive_complete.n<H>`, `.positive_index_always.n<H>` | small-H sweep, H = 2, 4, 6, 8, 12, 16 targets on one ray (H raw hits): complete projection against the index from the first hit; it brackets the index threshold of 8 raw hits |

The backend's own cost per call, without any target projection, is printed by
`Audit20261102LibraryHotpathMeasurementTests` as `interaction.queries.raycast3d.shared_hit` and `.fresh`.

Every line carries `strategy=default|complete|forced_index|index_always`, `n`, `frames`, `colliders_per_target`,
`inspected_per_frame` and `profiler_enabled=0|1`. `auto_*_ms` is the summed CPU of all
`Neo.Interaction.AutomaticUpdate` samples of a frame, read with a `ProfilerRecorder`; `update_*_ms` is the
wall time of the whole script Update phase and `gc_allocs_per_frame_*` the managed allocations counted in
that phase. Each case asserts its workload after recording: one native query per frame, one projection
per target and the exact inspected-entry count of its strategy.

Read before/after from one run: compare a default case with its `*_complete` control on the same machine,
Unity version and `profiler_enabled` value. Do not compare NEO_PERF numbers with the 2026-11-02 audit
captures. Those ran with the profiler and a binary log on, whose per-marker overhead (about 3N nested
samples per frame) is part of their numbers, and they predate the clear-path walk and the size hint.
No timing threshold is asserted. Zero allocations of the warm projections and of the per-frame index
and distance-order rebuild are asserted in the normal PlayMode suite.

### Measured results (12.0)

One run of the suite, Unity 6000.3.14f1 Editor, Mono, batch mode, `profiler_enabled=0`. Values are the
median `AutomaticUpdate` CPU per frame (`auto_p50_ms`) for N targets on one shared ray; the complete
projection is the per-target scan that 11.x used for every target. Every case, default and control,
recorded zero GC allocations on every frame.

| Case | Complete projection | Default (12.0) |
|---|---:|---:|
| `camera3d` positive, N = 1000 | 1927 ms | 7.5 ms |
| `camera2d` positive, N = 1000 | 1542 ms | 10.9 ms |
| `keyboard3d` positive, N = 1000 | 1175 ms | 6.3 ms |
| `keyboard2d` positive, N = 1000 | 1008 ms | 6.7 ms |
| `camera3d` obstacles on, N = 1000 | 1831 ms | 8.6 ms |
| `camera3d` 100 of 1000 targets on the ray | 100.4 ms | 7.0 ms |
| `camera3d` positive, N = 100 | 12.3 ms | 0.64 ms |
| `camera3d` 20 targets with 201 colliders each | 0.43 ms | 0.41 ms |

A single target costs about 0.03-0.06 ms per frame on every route, and a stable miss of 1000 targets
4.1-6.0 ms (one native query, no inspected hits). The large-hierarchy row is what the size hint is
for: indexing those targets anyway (`forced_index`) took 1.91 ms, so the default keeps the complete
scan there. The Editor adds overhead, so read the ratios, not the absolute times, and expect different
numbers in a player build.

## See also

- [Module entry](./README.md)
- [InteractiveObject](./InteractiveObject.md)
- [InteractionRayProvider](./InteractionRayProvider.md)
- [Core contracts](./Core/README.md)
