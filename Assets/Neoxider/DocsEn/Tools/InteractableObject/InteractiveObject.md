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

When `checkObstacles` is disabled, obstacle blocking is skipped for distance validation, keyboard direct-look ray checks, **and** the mouse hover/click ray (a hit on this object counts even if a non-trigger collider is closer to the camera). When enabled, the mouse ray requires this object to be the nearest non-ignored hit before any foreign **non-trigger** collider along the ray.

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
| `IsInInteractionRange` | Whether the object is currently in valid range. |
| `DistanceToCheckPoint` | Current measured distance to the check point. |
| `IsHovered` | Whether the object is currently hovered. |

## Input rules

- Hover is driven by cursor hit on the target collider.
- If `interactionDistance > 0`, hover requires a ray hit and the **hit point** to be within range (same as click eligibility), not only the collider center—so “already aimed, then walk into range” works at the distance boundary. If `interactionDistance == 0`, hover has no distance limit.
- Mouse click / down / up require an actual current mouse hit on the target collider.
- With `checkObstacles` enabled: non-trigger colliders in front of the object block hover/click; foreign trigger colliders do not. With `checkObstacles` disabled, only a ray hit on this object (and distance rules) matters.
- In `ViewOrMouse`, keyboard interaction no longer relies on hover; it uses view direction and optional direct look ray.
- By default the component uses only a collider on the same GameObject. If the target collider is elsewhere, assign `targetCollider3D` or `targetCollider2D` explicitly in the Inspector.

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
- [Russian InteractableObject docs](../../../Docs/Tools/InteractableObject/README.md)
- [Tools/Components](../Components/README.md)
