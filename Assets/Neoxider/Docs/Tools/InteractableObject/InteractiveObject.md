# InteractiveObject

`InteractiveObject` is a no-code interaction component for UI, 2D, and 3D objects. It combines hover, mouse clicks, keyboard interaction, distance checks, optional look-direction checks, and UnityEvent callbacks in one scene-facing component. File: `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs`, namespace: `Neo.Tools`.

## Typical use

1. Add `InteractiveObject` to a UI element or to a scene object with a collider.
2. Decide whether interaction should use mouse, keyboard, or both.
3. Set interaction distance and checkpoints if range-limited interaction is needed.
4. Configure UnityEvents such as hover, click, enter-range, or interact-down.
5. Ensure the scene has the needed `EventSystem` and raycasters.

## Main settings

### Interaction mode

- `useHoverDetection`
- `interactable`
- `useMouseInteraction`
- `useKeyboardInteraction`
- `keyboardInteractionMode`
- `requireViewForKeyboardInteraction`
- `requireDirectLookRay`
- `includeTriggerCollidersInLookRay`
- `includeTriggerCollidersInMouseRaycast`
- `targetCollider3D` (optional; if unset, uses `Collider` on this object or the first child collider)
- `targetCollider2D` (optional; if unset, uses `Collider2D` on this object or the first child collider)

### Distance and checkpoints

- `interactionDistance`
- `distanceCheckPoint`
- `viewCheckPoint`
- `ignoreDistancePointHierarchyColliders`
- `checkObstacles`
- `obstacleLayers`
- `includeTriggerCollidersInObstacleCheck`

When `checkObstacles` is disabled, obstacle blocking is skipped for distance validation, keyboard direct-look ray checks, **and** the mouse hover/click ray (a hit on this object counts even if a non-trigger collider is closer to the camera). When enabled, **both** the mouse ray and the keyboard look ray require this object to be the nearest non-ignored hit before any foreign **non-trigger** collider along the ray.

The keyboard look ray and the mouse ray read the same settings on purpose: `E` and the cursor must reach the same objects, and any difference between them has to come from a flag you can see in the Inspector. Foreign **trigger** volumes never block either ray — a door's trigger zone is not a wall. Before `10.13.5` the keyboard branch ignored `checkObstacles` and treated foreign triggers as blockers, so a pickup standing inside a trigger volume was clickable while the key silently did nothing.

### Input bindings

- `downUpMouseButton`
- `keyboardKey`
- `doubleClickThreshold`

### Debug

- `drawInteractionRayForOneSecond`
- `interactionRayDrawDuration`

## Events

### Hover

- `onHoverEnter`
- `onHoverExit`
- `onHoverChanged(bool)`

### Click

- `onClick`
- `onDoubleClick`
- `onRightClick`
- `onMiddleClick`

### Interact down/up

- `onInteractDown`
- `onInteractUp`

### Range

- `onEnterRange`
- `onExitRange`

## Runtime API

| API | Description |
|-----|-------------|
| `InteractionDistance` | Gets or sets the interaction distance (`0` means unlimited). |
| `DistanceCheckPoint` | Gets or sets the transform used for distance checks. |
| `UseHoverDetection` | Enables or disables hover detection (cursor over collider). |
| `UseMouseInteraction` | Enables or disables mouse click/down/up interaction. |
| `UseKeyboardInteraction` | Enables or disables keyboard interaction. |
| `IsInteractable` | Typed `IInteractiveTarget` state exposed for custom interaction sources. |
| `InteractDown()` | Triggers interact-down through the normal local or Mirror network route. No-op when `interactable` is disabled. |
| `InteractUp()` | Triggers interact-up through the normal local or Mirror network route. No-op when `interactable` is disabled. |
| `Click(MouseButton, bool)` | Triggers left, double-left, right, or middle click through the normal local or Mirror network route. No-op when `interactable` is disabled. |
| `InvalidateCachedColliders()` | Re-resolves target colliders on the next interaction check after runtime collider changes. |
| `IsInInteractionRange` | Whether the object is currently in valid range. |
| `DistanceToCheckPoint` | Current measured distance to the check point. |
| `IsHovered` | Whether the object is currently hovered. |

The Inspector exposes Play Mode-only test buttons for interact down, interact up, and click. `Test Click` also shows button and double-click parameters. These manual methods intentionally bypass hover, range, look-direction, and input checks; they only require `interactable`, then use the normal local or Mirror authority/rate-limit dispatch path. Use `Invalidate Colliders` after adding, removing, replacing, or reassigning colliders at runtime.

## Reusable C# interaction core

Custom input, AI, XR, or proximity controllers can depend on `IInteractiveTarget` instead of the
scene component. `InteractiveObject` implements this contract without changing its existing
`InteractDown()` / `InteractUp()` network dispatch:

These APIs live in the named leaf assembly `Neo.Tools.InteractableObject.Core` and retain the
`Neo.Tools` namespace for source compatibility. A custom asmdef that uses them should reference
`Neo.Tools.InteractableObject.Core` directly. The leaf has no `Mirror` or `Neo.Network` reference;
reference `Neo.Tools.InteractableObject` only when the concrete scene component is needed.

```csharp
public void UseTarget(IInteractiveTarget target)
{
    if (target != null && target.IsInteractable)
    {
        target.InteractDown();
        target.InteractUp();
    }
}
```

`InteractionQueryMath` contains the scene-free rules used by the component:

- `IsWithinRange(...)` uses the same inclusive distance boundary (`0` means unlimited).
- `GetObstacleCheckDistance(...)` applies the target padding used by obstacle rays.
- `TryGetNearestHit(...)` and `TrySelectTarget(...)` resolve unsorted `InteractionRayHit` buffers,
  including the nearest-target-versus-nearest-blocker rule.

The query APIs accept reusable arrays and do not allocate. `InteractionCameraResolver` centralizes
the existing cached-camera policy (`Camera.main`, with an optional first-camera fallback) for custom
interaction components.

## Input rules

- Hover is driven by cursor hit on the target collider.
- If `interactionDistance > 0`, hover requires a ray hit and the **hit point** to be within range (same as click eligibility), not only the collider center—so “already aimed, then walk into range” works at the distance boundary. If `interactionDistance == 0`, hover has no distance limit.
- Mouse click / down / up require an actual current mouse hit on the target collider.
- With `checkObstacles` enabled: non-trigger colliders in front of the object block hover/click **and the keyboard look ray**; foreign trigger colliders block neither. With `checkObstacles` disabled, only a ray hit on this object (and distance rules) matters — again for both inputs.
- In `ViewOrMouse`, keyboard interaction no longer relies on hover; it uses view direction and optional direct look ray. `requireDirectLookRay` decides whether an aim ray is required at all; `checkObstacles` decides whether obstacles along that ray block the aim. The two flags stay independent.
- By default the component uses only a collider on the same GameObject. If the target collider is elsewhere, assign `targetCollider3D` or `targetCollider2D` explicitly in the Inspector.
- `includeTriggerCollidersInMouseRaycast` applies to 3D and 2D alike: without it the mouse ray cannot see an object whose own collider is a trigger. The two dimensions reach that result differently, because `Physics2D.GetRayIntersectionNonAlloc` accepts no `QueryTriggerInteraction` — 3D excludes triggers inside the query, 2D filters them out of the result afterwards. One consequence is 2D-only: the global `Physics2D.queriesHitTriggers` is consulted first, so the flag can only narrow what that global already returned and never widen it. If a 2D trigger stays invisible to the ray with the flag on, check the global before the component.

## Scene requirements

- UI interaction requires an `EventSystem`.
- Non-UI interaction requires colliders.
- The component can rely on `Physics Raycaster` or `Physics2D Raycaster` for scene objects.

## Typical scenarios

- Doors or chests that can be opened with `E` in range.
- Pickups that react to click or keyboard interaction.
- NPCs that require both range and look direction before dialogue starts.
- UI buttons that still want richer hover/click event wiring.

## See also

- [README](./README.md)
- [InteractableObject docs](./README.md)
- [Tools/Components](../Components/README.md)
