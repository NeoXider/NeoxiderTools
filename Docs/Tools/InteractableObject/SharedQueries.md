# Shared interaction queries

**What it is:** `InteractionPhysicsQueries` (`Neo.Tools`, assembly `Neo.Tools.InteractableObject`) runs each
identical interaction raycast once per stable pass and shares the complete raw hits among all automatic targets.
`InteractionRayProvider` supplies ray geometry and an invalidation shortcut. Scenes need no extra component,
controller or registration.

**How to use:**
1. Nothing to set up: interaction components share hits automatically.
2. After replacing a target's colliders at runtime, call `InteractiveObject.InvalidateCachedColliders()`.
3. After moving physics outside the normal update order, declare the boundary with
   `InteractionRayProvider.InvalidateQueries()` ([Completeness and lifetime](#completeness-and-lifetime)).
4. To query interaction hits from your own code, see the [C# query API](#c-query-api).

---

## What is shared

The first consumer runs a query; later consumers of the exact same context in the same stable epoch borrow its
hits. The key contains:

- camera identity, viewport, display and render target, and the default physics world;
- query kind: 3D rays, 2D screen-ray intersections, legacy 2D planar rays and explicit-filter 2D rays are separate;
- exact float origin, direction and distance, compared without `Vector3.operator ==`;
- physical mask and the relevant trigger, contact and global policy. Masks are never widened to AllLayers.

Planar 2D queries keep the original `Vector2` origin and direction in the key and the native call, including
non-unit, tiny and zero directions; they never round-trip through a normalized 3D `Ray`. In `Both` mode a target
normally has a center ray and a pointer ray; identical hover and click rays share.

The cache does not pick one global winner. Each consumer still applies its own collider or hierarchy selection,
range, ignored source hierarchy, obstacle policy and the Core `InteractionQueryMath` rules; these post-filters are
not part of the key. With obstacles off, several targets along one ray can all stay eligible.

Range line-of-sight and keyboard look keep their own directions, lengths and masks, so distinct LOS rays remain
separate physics work. `PhysicsEvents2D` / `PhysicsEvents3D` remain real collision and trigger relays.

## Target projection

Each consumer projects the borrowed raw hits (H of them) onto its own target. Both strategies give the same
selected target, hit point and equal-distance order (raw order breaks ties).

**Indexed projection** serves camera and look selection:

- The first consumer of a shared entry builds a collider-to-raw-index map once per epoch. Each consumer then
  enumerates its own live collider hierarchy, or its explicit target collider, and reads only its own hits.
- With `checkObstacles` on, the entry is sorted by distance once per epoch. Each consumer walks only the hits in
  front of its nearest target hit, up to the first blocker; the clear-path verdict and target hit are unchanged.
- N consumers of one dense ray cost about H + N instead of N x H, with obstacles on or off. A miss returns no
  non-target candidates.

**Complete projection** inspects all H hits and marks target hits, blockers and ignored source-rig colliders. It is
used for:

- range line-of-sight, where foreign triggers block and the ray is unique per target anyway;
- raw sets smaller than 8 hits, where direct checks are cheaper than an index;
- a look-ray miss while a debug ray is recorded, because the debug ray ends at the nearest non-target hit;
- a target whose hierarchy had more colliders than the raw set at its last enumeration (a building or vehicle root
  with hundreds of child colliders). This size hint is re-measured after 64 such projections. It changes only the
  strategy, never the selection.

Nothing that decides the selection is cached between calls. Reparenting, explicit collider changes, 2D trigger
flags, ignored source rigs, disabled and destroyed colliders are evaluated live. Only the collider count is kept,
as a cost hint.

## Completeness and lifetime

A full NonAlloc buffer is never taken as complete. The backend fetches the full result through the matching
all-hits query (a reusable List for explicit-filter 2D rays) and grows its retained buffer with spare capacity.
An unchanged warm result then fits without another overflow allocation. Per-consumer buffers grow too and never
truncate the result.

`Hits3D` / `Hits2D` are read-only borrowed views, not copies:

- Check `IsComplete`, read `Count` and index the hits. The shared arrays cannot be sorted or modified.
- When the epoch expires, `IsValid` and `IsComplete` become false and indexed access throws.
- Finish processing and keep the values you need before invoking callbacks or another fresh query. Copy data
  explicitly if it must outlive the epoch.

**When the epoch ends:** on frame advance, explicit invalidation, disable/enable, collider-cache invalidation and
around interaction callbacks. External event dispatch (hover, click, range, down, up, physics relay, ToggleObject)
invalidates the epoch before it runs and again in a `finally` after it. Queries inside a callback bypass sharing;
event order stays synchronous.

Slots and arrays are reused across epochs, and old keys and hit references are cleared. Retained capacity follows
the peak number of slots used at once. `ReleaseBuffers()` ends the epoch and frees that capacity, for example at a
scene or memory boundary; it does not reset counters.

External movement or manual physics calls between other components' Update methods cannot be detected. Declare
that boundary yourself:

```csharp
blocker.enabled = false;
Physics.SyncTransforms();
InteractionRayProvider.InvalidateQueries();
```

Invalidation neither runs physics nor syncs transforms. Replacing the main-camera provider, changing its mode or
disabling it also invalidates query data. Camera choice still follows `InteractionCameraResolver` and component
settings. A disabled rendering Camera with a valid viewport still provides geometry for direct provider calls and
cached target center rays; provider selection keeps its own enabled check.

## C# query API

`Raycast3D`, `GetRayIntersection2D` and the layer-mask and `ContactFilter2D` overloads of `Raycast2D` query Unity's
default physics scenes on the main thread. The camera is a context identity, not a request to query that camera's
local physics scene.

`QueryMode` is `Fresh` (start a new epoch, the default for external calls) or `Shared` (reuse an identical query of
the current epoch). `InteractiveObject.IsInInteractionRange` also checks fresh. Use `Shared` only inside a pass you
know is stable:

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

Trigger policy:

- 3D explicit `Collide` / `Ignore` does not depend on the global trigger flag.
- Legacy 2D queries and the component's 2D camera and look paths follow `Physics2D.queriesHitTriggers`. A
  component's include-trigger option cannot recover hits that native query excluded.
- Explicit-filter 2D LOS passes the original `ContactFilter2D` unchanged to Unity's overloads. The backend never
  turns on a global physics flag to get a hit. When diagnosing, compare with those native overloads under the
  same flags.
- The global start-inside setting and every contact-filter field are part of the key where they apply.

## Measurement

`InteractionPhysicsQueries.Counters` returns a `Statistics` snapshot; `ResetCounters()` clears it without
invalidating data or running a query.

| Counter | Meaning |
|---|---|
| `Raycasts3D`, `RayIntersections2D`, `Raycasts2D`, `FilteredRaycasts2D`, `NativeCalls` | Actual backend physics calls by kind, and their sum. |
| `CacheHits`, `OverflowFallbacks`, `FreshQueries`, `Invalidations`, `FrameAdvances`, `CallbackBoundaries`, `NativeCallsInsideCallbacks` | Cache and epoch activity. |
| `CameraNativeCalls`, `LookNativeCalls`, `RangeNativeCalls`, `ExternalNativeCalls` | Which requester made the native call. When look and range share a query, the first requester owns the call and the other counts a hit. |
| `ClassificationPasses` | Target-local projection passes. |
| `ClassifiedHits` | Raw entries inspected: H per complete projection, H once per index build and once per distance order, plus each indexed consumer's own entries and the hits walked in front of it. CPU diagnostics, not native calls. |

The requester (`QueryPurpose`) is diagnostic only and never splits an identical physical key.

Profiler markers:

- `Neo.Interaction.Native.Raycast3D`, `.Intersection2D`, `.Raycast2D`, `.FilteredRaycast2D` — the native attempts
  and fallbacks.
- `Neo.Interaction.Classify3D` / `.Classify2D` — per-target projection, hierarchy filtering and buffer growth.
- `Neo.Interaction.Callbacks` — observer dispatch.
- `Neo.Interaction.AutomaticUpdate` — the whole target pass, including Core selection.

The contract: K native queries for N consumers of K complete, warm contexts in a stable epoch. Cold overflow,
callback or world invalidation, fresh calls and unique LOS are counted separately. A transition that invokes
callbacks can start another epoch in the same frame. External EventSystem or PhysicsRaycaster calls are outside
these counters.

## Performance suite

The `Performance` test category measures the real PlayerLoop and prints one line per case,
`NEO_PERF <case> key=value ...`, to the Unity log and test output. It never runs in a normal pass:

```text
Unity -batchmode -projectPath <project> -runTests -testPlatform PlayMode -testCategory Performance
```

Cases are named `interaction.<route>.<workload>.n<N>` with routes `camera3d`, `camera2d`, `keyboard3d` and
`keyboard2d`. Each default case has a `*_complete` control that forces the complete projection. Every line
carries `strategy`, `n`, `frames`, `colliders_per_target`, `inspected_per_frame` and `profiler_enabled`;
`auto_*_ms` is the CPU of `Neo.Interaction.AutomaticUpdate` per frame, `update_*_ms` the wall time of the script
Update phase and `gc_allocs_per_frame_*` the managed allocations in it. Compare a case with its control from one
run, on one machine, Unity version and `profiler_enabled` value. No timing threshold is asserted.

### Measured results

One run in the Unity 6000.3.14f1 Editor (Mono, batch mode, `profiler_enabled=0`): median
`AutomaticUpdate` CPU per frame for N targets on one shared ray. Every case recorded zero GC allocations.

| Case | Complete projection | Default |
|---|---:|---:|
| `camera3d` positive, N = 1000 | 1927 ms | 7.5 ms |
| `camera2d` positive, N = 1000 | 1542 ms | 10.9 ms |
| `keyboard3d` positive, N = 1000 | 1175 ms | 6.3 ms |
| `keyboard2d` positive, N = 1000 | 1008 ms | 6.7 ms |
| `camera3d` obstacles on, N = 1000 | 1831 ms | 8.6 ms |
| `camera3d` 100 of 1000 targets on the ray | 100.4 ms | 7.0 ms |
| `camera3d` positive, N = 100 | 12.3 ms | 0.64 ms |
| `camera3d` 20 targets with 201 colliders each | 0.43 ms | 0.41 ms |

A single target costs about 0.03–0.06 ms per frame on every route; a stable miss of 1000 targets costs 4.1–6.0 ms
(one native query, nothing inspected). For the large-hierarchy row, forcing the index took 1.91 ms, which is why
the size hint keeps the complete scan there. The Editor adds overhead: read the ratios, not the absolute times.

## See also

- [Module entry](./README.md)
- [InteractiveObject](./InteractiveObject.md)
- [InteractionRayProvider](./InteractionRayProvider.md)
- [Core contracts](./Core/README.md)
