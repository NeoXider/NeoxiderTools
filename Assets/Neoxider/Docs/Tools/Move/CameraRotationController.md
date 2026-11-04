# CameraRotationController

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/CameraRotationController.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Move`) that drag-rotates an object's local Euler axes with independent speed and angle limits.

**How to use:**
1. Add **Neoxider > Tools > CameraRotationController** to the object to rotate (an orbit pivot or a camera).
2. Set the drag mouse button, optional modifier key, sensitivity and the per-axis settings.
3. Wire `onRotateStart` / `onRotateEnd`, or configure the axes from C# (see below).

---

## Setup

- Add the component to the rotating object, set the drag mouse button and optional modifier key,
  then configure enabled axes, speeds and limits.
- Pointer position/buttons use `MouseInputCompat`; the modifier uses `KeyInputCompat`. Both support
  legacy/new input without requiring a second rotation component.
- `xAxisSettings`, `yAxisSettings` and `zAxisSettings` are public C# configuration objects;
  `AxisRotationSettings.ClampAngle(float)` is the shared angle-limit API. Drag start/end events can
  be wired in the Inspector.

## Drag transitions and angle compatibility

A drag becomes active when the pointer is available, the mouse button is held and the modifier gate
passes. Pressing the modifier after the mouse button starts a fresh drag at the current pointer
position. Releasing either gate, losing pointer data, disabling the component, losing focus or pausing
the application ends the active drag once. Returning to drag captures a fresh pointer baseline and pose.
Wire `onRotateStart` / `onRotateEnd` to scene effects; no additional input component is required.

Initial Euler angles use an equivalent turn near the configured range. For example, X=350 degrees is
treated as -10 with limits -45..45, and remains 350 with limits 0..360. An initial/unchanged pointer
sample does not rotate the object. These corrections preserve serialized field names and authored ranges.

Sensitivity keeps its historical formula: **pointer pixels × mouseSensitivity × axis rotationSpeed ×
scaled deltaTime**. Existing scenes retain their tuning and frame-rate dependence. No frame-independent
mode is enabled by this update. While scaled time is zero, pointer baselines still update without motion.

## C# configuration

```csharp
CameraRotationController rotation = GetComponent<CameraRotationController>();
rotation.xAxisSettings = new AxisRotationSettings(true, 10f, -45f, 45f);
rotation.yAxisSettings.isEnabled = false;
rotation.enabled = false; // ends an active drag through the normal component lifecycle
```

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `mouseButton` | Drag button: 0 left, 1 right, 2 middle. |
| `modifierKey` | Optional key that must be held while dragging (`None` = no gate). |
| `mouseSensitivity` | Multiplier for pointer deltas. |
| `xAxisSettings` / `yAxisSettings` / `zAxisSettings` | `AxisRotationSettings`: `isEnabled`, `rotationSpeed`, `minAngle`, `maxAngle`. |
| `onRotateStart` / `onRotateEnd` | Raised once when a drag becomes active and when it ends. |

## See Also

- [FreeFlyCameraController](./FreeFlyCameraController.md)
- [Tools/Move](./README.md)
