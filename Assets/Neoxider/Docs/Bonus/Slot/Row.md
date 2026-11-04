# Row

**What it is:** one slot reel column (`MonoBehaviour`, `Scripts/Bonus/Slot/Row.cs`). It scrolls its child `SlotElement`s on a single phase offset, swaps symbols only while they are hidden outside the window, decelerates onto the grid and lands on the target symbols supplied by [SpinController](SpinController.md).

**How to use:**
1. Add `Row` to a column object and place at least `countSlotElement + 1` `SlotElement` children under it.
2. Let `SpinController` drive it (it sets `countSlotElement`, `spaceY`, `offsetY` and speed when **Set Space** is on), or call `Spin(spritesData, targetIdsBottomUp)` and `Stop()` yourself.
3. Read the window with `GetVisibleTopDown()`, `GetVisibleBottomUp()`, `GetVisibleElement(rowFromBottom)` or `TryGetWindowRowFromBottom(element, out row)`.

---

Reel presentation uses unscaled time, so an accepted spin continues to settle when gameplay is paused with
`Time.timeScale = 0`. `CompleteSpinImmediately()` synchronously applies the planned visible outcome and is
idempotent. Deactivating the row invokes the same completion path so `is_spinning` is never left stranded.

## Window queries

| Member | Description |
|--------|-------------|
| `SlotElement[] GetVisibleTopDown()` | New array of the visible window, top first. Safe to keep. |
| `SlotElement[] GetVisibleBottomUp()` | Same, bottom first. |
| `SlotElement GetVisibleElement(int rowFromBottom)` | Element in one window row, or null out of range. Does not allocate. |
| `bool TryGetWindowRowFromBottom(SlotElement, out int)` | Window row of an element. Does not allocate. |

The non-allocating queries reuse buffers owned by the reel; payline geometry and Scene labels call them per cell.
`Spin` copies the target ids into the reel's own buffer, so callers may reuse their array. A reel spun before its
`Awake` (for example under an inactive parent) lays itself out first instead of throwing, and its `Awake` keeps
that spin: once the reel is active it decelerates, lands on the target symbols and raises `OnStop`. An inactive
reel does not move, so `SpinController` settles it through `CompleteSpinImmediately()` when its spin deadline
passes.

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
