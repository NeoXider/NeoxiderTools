# WheelFortune

Scene prize wheel with manual/automatic stop, optional alignment, and one sector result per completed spin.

## Setup

- Assign the wheel RectTransform, optional arrow/CanvasGroup, and ordered prize objects.
- Bind a button to `Spin()`, optionally bind another to `Stop()`, and grant through `OnWinIdVariant(int)`.
- For automatic stops, set **Auto Stop Time** and optional **Extra Spin Time**. Re-enable `canUse` to
  permit another spin when **Single Use** is enabled.

The equivalent C# flow is `wheel.OnWinIdVariant.AddListener(id => Grant(wheel.GetPrize(id))); wheel.Spin();`.
`State` exposes Idle/Spinning/Decelerating/Aligning. `ResolveSectorIndex` evaluates wheel/arrow angles without
a component, and `GetPrize(id)` returns null for an invalid or unassigned slot.

`Spin()` requires an active component, assigned wheel transform, and a non-empty prize array; rejected
calls do not consume single use. `Stop()` cancels the pending automatic stop so it cannot stop a later spin.
Non-positive deceleration and alignment duration complete their respective phases immediately.
Auto-arrangement skips empty prize references while retaining their sector indices. Disabling cancels
scheduled stops; re-enabling returns the wheel to Idle. Rotation otherwise uses scaled game time.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `Items` | Items. |
| `OnWinIdVariant` | On Win Id Variant. |
| `SpinState` | Spin State. |
| `State` | State. |
| `_alignmentDuration` | Alignment Duration. |
| `_arrow` | Arrow. |
| `_autoStopTime` | Auto Stop Time. |
| `_canvasGroup` | Canvas Group. |
| `_debugLogId` | Enables result id logging for wheel diagnostics. |
| `_enableAlignment` | Enable Alignment. |
| `_offsetZ` | Offset Z. |
| `_setPrizes` | Set Prizes. |
| `_wheelAngleInspector` | Wheel Angle Inspector. |
| `_wheelOffsetZ` | Wheel Offset Z. |
| `_wheelTransform` | Wheel Transform. |
| `canUse` | Can Use. |
| `items` | Items. |

## See Also

- [Module Root](../README.md)
