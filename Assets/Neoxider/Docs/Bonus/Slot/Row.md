# Row

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

Reel presentation uses unscaled time, so an accepted spin continues to settle when gameplay is paused with
`Time.timeScale = 0`. `CompleteSpinImmediately()` synchronously applies the planned visible outcome and is
idempotent. Deactivating the row invokes the same completion path so `is_spinning` is never left stranded.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `OnStop` | On Stop. |
| `SlotElements` | Slot Elements. |
| `countSlotElement` | Count Slot Element. |
| `defaultStartSpeed` | Default Start Speed. |
| `extraStepsAtDecel` | Extra Steps At Decel. |
| `hiddenPaddingBottom` | Hidden Padding Bottom. |
| `hiddenPaddingTop` | Hidden Padding Top. |
| `is_spinning` | Is_spinning. |
| `maxDecel` | Max Decel. |
| `offsetY` | Offset Y. |
| `spaceY` | Space Y. |
| `speedControll` | Speed Controll. |
| `windowStartY` | Window Start Y. |

## See Also

- [Module Root](../README.md)
