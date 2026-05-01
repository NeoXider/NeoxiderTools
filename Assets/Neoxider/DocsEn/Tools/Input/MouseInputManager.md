# MouseInputManager

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `10f` | 10f. |
| `drawGizmos` | Draw Gizmos. |
| `enableClick` | Enable Click. |
| `enableHold` | Enable Hold. |
| `enablePress` | Enable Press. |
| `enableRelease` | Enable Release. |
| `gizmoBaseFontSize` | Gizmo Base Font Size. |
| `gizmoColor` | Gizmo Color. |
| `gizmoDrawText` | Gizmo Draw Text. |
| `gizmoRadius` | Gizmo Radius. |
| `gizmoTextColor` | Gizmo Text Color. |
| `gizmoTextOffset` | Gizmo Text Offset. |
| `gizmoTextScale` | Gizmo Text Scale. |
| `interactableLayers` | Interactable Layers. |
| `targetCamera` | Target Camera. |

## Notes

- **Domain reload:** static polling state and `CreateInstance` bootstrap are driven by `MouseInputManagerSubsystemRegistration` (not `[RuntimeInitializeOnLoadMethod]` on `MouseInputManager` itself) so Unity does not reject hooks on `Singleton<T>` subclasses.

## See Also

- [Module Root](../README.md)