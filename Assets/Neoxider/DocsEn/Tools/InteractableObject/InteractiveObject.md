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

- `interactable`
- `useMouseInteraction`
- `useKeyboardInteraction`
- `keyboardInteractionMode`
- `requireViewForKeyboardInteraction`
- `minLookDot`
- `requireDirectLookRay`
- `includeTriggerCollidersInLookRay`
- `includeTriggerCollidersInMouseRaycast`

### Distance and checkpoints

- `interactionDistance`
- `distanceCheckPoint`
- `viewCheckPoint`
- `ignoreDistancePointHierarchyColliders`
- `checkObstacles`
- `obstacleLayers`

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
| `UseMouseInteraction` | Enables or disables mouse interaction. |
| `UseKeyboardInteraction` | Enables or disables keyboard interaction. |
| `IsInInteractionRange` | Whether the object is currently in valid range. |
| `DistanceToCheckPoint` | Current measured distance to the check point. |
| `IsHovered` | Whether the object is currently hovered. |

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
