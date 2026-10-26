# Bonus / Slot

## Purpose

Composable slot mechanics: `CheckSpin` evaluates symbol matrices in ordinary C#, `SlotEconomyDefinition`
stores symbol weights and rewards, and `SpinController` drives optional reel/line views and a wallet.
All grid queries use `[column, row]` with row zero at the bottom; payline queries use `[line, column]`.

## Runtime and scene setup

Assign a complete `SpritesData` table, ordered `Row` columns, optional `BetsData` and `LinesData`, and a
wallet implementing `IMoneySpend` / `IMoneyAdd`. Each reel needs its visible symbols plus buffer elements.
Without `LinesData`, fallback horizontal paylines cover the configured visible row range. Buttons can
call `StartSpin`, `AddLine` / `RemoveLine`, and bet-selection methods; code calls the same public APIs.
Line visual objects and LineRenderers are optional. Empty line-view arrays still allow line selection
to cycle within the actual payline definitions.

For rules without views, call `CheckSpin.GetWinningLines(ids, activeLines)` and `GetMultiplayers`.
Only contiguous runs of valid non-negative symbol IDs can win; missing-cell ID `-1` does not pay.
Destroyed reel elements are skipped during updates and visible-window recovery. A standalone
`SlotElement` can use automatic Scene labels without requiring a parent reel.

For a full scene spin, check `IsSpinInProgress` before changing layout or bet settings. `IsStop()` only
reports reel motion and may be true between staggered columns. `CompleteActiveSpinImmediately` settles
the active plan once; `CancelActiveSpin` cancels result callbacks and attempts to refund the charged bet.
See `SpinController` for wallet ownership, numeric limits, and lifecycle callbacks.

## Documents

- [SpinController](./SpinController.md)
- [SpeedControll](./SpeedControll.md)
- [SlotElement](./SlotElement.md)
- [Row](./Row.md)
- [VisualSlotLines](./VisualSlotLines.md)
- [CheckSpin](./CheckSpin.md)
- [Data](./Data/README.md)
- [SlotEconomyDefinition](./SlotEconomyDefinition.md)

