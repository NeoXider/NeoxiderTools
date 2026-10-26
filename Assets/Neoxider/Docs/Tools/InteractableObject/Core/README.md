# Interaction core

Reusable interaction contracts, range checks, hit selection, and camera resolution. Namespace:
`Neo.Tools`; assembly: `Neo.Tools.InteractableObject.Core`. Reference this assembly from custom
asmdefs. It uses UnityEngine types and has no references to other package assemblies, input,
Mirror, or scene components.

The [InteractableObject module](../README.md) provides the optional scene and UnityEvent layer.

## Runtime contracts

`IInteractiveTarget` exposes `IsInteractable`, `InteractDown()`, and `InteractUp()`. A plain C#
object can implement it. Custom input sources decide when to dispatch; the interface itself
does not perform range, visibility, authority, or input checks.

`InteractionRayHit` is an immutable value containing `Distance`, `Point`, `IsTarget`, and
`BlocksInteraction`. Construct it from 2D/3D physics results or custom queries. The caller marks
which hits belong to the intended target and which foreign hits block interaction; the core
does not inspect colliders, layers, triggers, or object hierarchies.

## Query rules

| API | Contract |
|-----|----------|
| `IsWithinRange(source, target, maximumDistance)` | Inclusive Euclidean distance. A non-positive maximum is unlimited. Double intermediates prevent float-square overflow or underflow for finite inputs. |
| `GetObstacleCheckDistance(distance, targetPadding = 0.1f)` | Subtracts non-negative padding and clamps the result to zero. Negative padding is treated as zero. |
| `TryGetNearestHit(hits, count, out nearestHit)` | Finds the nearest valid hit, regardless of target/blocker flags. |
| `TrySelectTarget(hits, count, requireClearPath, out targetHit)` | Finds the nearest target; with a clear path required, accepts it only if no blocking non-target hit is closer. An equal-distance blocker allows the target. |

Both hit queries read only the first `min(count, hits.Length)` entries. Null arrays, non-positive
counts, and empty buffers return false with a default output. Distances must be finite and
non-negative: zero and `float.MaxValue` are valid, while negative values, NaN, and infinities
are ignored. Equal-distance candidates retain the first matching entry. Points are returned
unchanged and do not participate in ordering. A target hit never blocks itself, even if its
`BlocksInteraction` flag is set.

If no target exists, selection returns false with a default output. If a target exists but is
blocked, selection returns false **and retains that nearest target in the output** for callers
that need to display blocked-target feedback. Always inspect the Boolean result before dispatching.

Arrays can be reused and neither hit-query method sorts, mutates, or allocates a result buffer.
Rebuild the populated prefix and pass its current count so stale entries are not considered.
These methods do not perform the physics query or detect a full/truncated physics buffer.
Supply finite coordinates and configuration values; the non-positive unlimited-range shortcut
does not validate coordinates, and the padding helper does not sanitize NaN/infinities.

For complete raw scene-query results and their epoch lifetime, see
[Shared interaction queries](../SharedQueries.md) in the separate `Neo.Tools.InteractableObject`
assembly. Classify target and blocker flags in the consumer's own candidate buffer; shared raw
hits do not carry one global target decision. Finish reading borrowed hits before external
callbacks or another fresh query. The Core methods only reduce supplied values and do not
own physics-query buffers, cache epochs, or invalidation.

## C# input adapter

This example assumes every hit marked `IsTarget` belongs to the supplied receiver. It checks
the selected hit point rather than a collider's center. Reuse a caller-owned hit buffer.

```csharp
using Neo.Tools;
using UnityEngine;

public static class InteractionExample
{
    public static bool TryUse(IInteractiveTarget target, Vector3 origin, float maximumDistance,
        InteractionRayHit[] hits, int count)
    {
        if (target == null || !target.IsInteractable)
        {
            return false;
        }

        if (!InteractionQueryMath.TrySelectTarget(hits, count, true, out InteractionRayHit selected) ||
            !InteractionQueryMath.IsWithinRange(origin, selected.Point, maximumDistance))
        {
            return false;
        }

        target.InteractDown();
        target.InteractUp();
        return true;
    }
}
```

For held input, send down on press and up on release instead of invoking both immediately.
Keep receiver lifetime and Unity destroyed-object checks in the owning scene adapter;
an interface reference does not supply UnityEngine.Object null semantics.

## Camera policy and lifecycle

`InteractionCameraResolver.Resolve(cachedCamera = null, allowSceneFallback = true)` keeps any
non-destroyed cached camera, otherwise returns `Camera.main`, otherwise optionally calls
`Object.FindFirstObjectByType<Camera>()`. With fallback disabled and no main camera, it returns
null. A disabled cached camera remains eligible; a main-camera switch does not replace a live
cached reference. A custom adapter that owns its camera reference can clear or replace that
reference before calling `Resolve` again.

The concrete `InteractiveObject` keeps a private camera cache and currently exposes no public
camera setter or cache-reset method. `InvalidateCachedColliders()` refreshes collider/query state;
it does not reset the camera cache. Do not treat a custom adapter's cache control as an API on
that component. [InteractionRayProvider](../InteractionRayProvider.md) supplies the scene-facing
cursor/crosshair ray modes and follows its own main-camera provider selection policy.

The resolver stores no static cache, owns no cameras, and does not enable, disable, instantiate,
or destroy objects. Scene search is a fallback; prefer an explicit camera and retain the result
in the calling component. It must be called on Unity's main thread.

## Scene / NoCode workflow

1. Add [InteractiveObject](../InteractiveObject.md) to the scene object and configure its collider,
   distance checkpoint, range, and obstacle settings. This requires the separate
   `Neo.Tools.InteractableObject` assembly.
2. Wire the component's hover, click, range, and interact UnityEvents in the Inspector. The core
   types have no components to add and no ScriptableObject scene references to configure.
3. For AI, XR, or custom input, call the typed contract after applying the required query checks.
   The scene component's manual `InteractDown()` / `InteractUp()` methods bypass automatic hover,
   range, and look checks and use its normal local/network dispatch path.
4. For camera switching in custom C# input, own and update the camera reference passed to
   `InteractionCameraResolver.Resolve`. For the concrete scene workflow, configure
   [InteractionRayProvider](../InteractionRayProvider.md); it does not add a public camera-cache
   reset API to `InteractiveObject`. Shared-query invalidation and camera-cache replacement
   are separate operations.
