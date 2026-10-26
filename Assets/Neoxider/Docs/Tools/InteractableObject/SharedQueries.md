# Shared interaction queries

`InteractionPhysicsQueries` (`Neo.Tools`, assembly `Neo.Tools.InteractableObject`) shares complete
raw physics hits across automatic targets. `InteractionRayProvider` supplies ray geometry and an
invalidation shortcut. Existing scenes do not need a new component, controller or registration.

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
`ClassificationPasses` and `ClassifiedHits` separately count target-local projection passes and raw
hit entries inspected, including entries later excluded. They are CPU-work diagnostics, not native calls.

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
off, all collinear targets remain eligible; the 1000-target cases also inspect 1000 hits per consumer.
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
and layer-mask negatives. Corrected source
requires integrated compilation, discovery and native verification after all writers finish. Native
query counts alone establish neither CPU nor allocation improvements; target-local N x H work remains.

## See also

- [Module entry](./README.md)
- [InteractiveObject](./InteractiveObject.md)
- [InteractionRayProvider](./InteractionRayProvider.md)
- [Core contracts](./Core/README.md)
