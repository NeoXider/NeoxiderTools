# CameraRotationController

Drag-rotates an object's local Euler axes with independent speed and angle limits. Namespace `Neo.Tools`,
assembly `Neo.Tools.Move`.

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
| `AxisRotationSettings` | Axis Rotation Settings. |
| `isEnabled` | Is Enabled. |
| `maxAngle` | Max Angle. |
| `minAngle` | Min Angle. |
| `modifierKey` | Modifier Key. |
| `mouseButton` | Mouse Button. |
| `mouseSensitivity` | Mouse Sensitivity. |
| `onRotateEnd` | On Rotate End. |
| `onRotateStart` | On Rotate Start. |
| `rotationSpeed` | Rotation Speed. |
| `xAxisSettings` | X Axis Settings. |
| `yAxisSettings` | Y Axis Settings. |
| `zAxisSettings` | Z Axis Settings. |

## See Also

- [Module Root](../README.md)
