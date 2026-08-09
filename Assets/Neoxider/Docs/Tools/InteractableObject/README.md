# Tools / InteractableObject

Click/hover interactions and 2D/3D physics events. Scripts live in
`Scripts/Tools/InteractableObject/`. Use this page as the English module entry.

The reusable contracts, hit-order math, and camera resolver are isolated in the leaf assembly
`Neo.Tools.InteractableObject.Core` (`Scripts/Tools/InteractableObject/Core/`). It has no package
assembly references, so custom input, AI, XR, and proximity code can use the `Neo.Tools` APIs without
pulling in Mirror or `Neo.Network`. The scene-facing `Neo.Tools.InteractableObject` assembly adds the
MonoBehaviour, UnityEvent, input, and optional network integration layer.

## docs (per-component)

| Page | Description |
|------|-------------|
| [InteractiveObject](./InteractiveObject.md) | Interaction component with hover, click, keyboard, and distance checks |
| [PhysicsEvents2D](./PhysicsEvents2D.md), [PhysicsEvents3D](./PhysicsEvents3D.md) | 2D/3D collision and trigger events |
| [ToggleObject](./ToggleObject.md) | Boolean toggle state and events |

## See also

- [Tools/Components](../Components/README.md)
