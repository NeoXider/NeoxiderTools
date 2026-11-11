# BetsData

**What it is:** a `ScriptableObject` holding the list of selectable bets (price per line) for a slot machine.
Script: `Scripts/Bonus/Slot/Data/BetsData.cs`, namespace `Neo.Bonus`.

**How to use:**
1. Create an asset via **Create > Neoxider > Bonus > Slot > Bets Data**. A new asset starts with
   `10, 20, 50, 100, 200, 500, 1000`.
2. Assign it to the **Bets Data** field of [SpinController](../SpinController.md).
3. Step through the list with `SpinController.AddBet()` / `RemoveBet()` / `SetMaxBet()` (for example from UI
   buttons) or set `SpinController.BetSelectionIndex` while the reels are stopped. `OnChangeBet` passes the
   resulting spin price as text.

---

## Fields

| Field (Inspector) | Property | Type | Description |
|-------------------|----------|------|-------------|
| Bets | `bets` | `int[]` | Bet values in the order the selector walks them. |

The selected value is the line price. The spin price is `SlotSpinMath.ComputeSpinPrice(linePrice, lineCount,
priceOnLine)`, and a win pays `SlotSpinMath.ComputePayout(multiplier, linePrice)`. Without an asset, or with an
empty array, the line price is `0` and the selection index stays `0`.

## See also

- [SpinController](../SpinController.md)
- [Slot data assets](README.md)
