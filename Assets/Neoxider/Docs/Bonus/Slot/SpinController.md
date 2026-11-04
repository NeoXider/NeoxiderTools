# SpinController

**What it is:** the slot-machine orchestrator (`MonoBehaviour`, `Scripts/Bonus/Slot/SpinController.cs`, namespace `Neo.Bonus`). It charges the bet, plans the outcome, spins the `Row` reels, reads the visible grid, evaluates paylines with `CheckSpin`, pays the wallet and raises result events. Reel motion itself lives in [Row](Row.md); price and payout arithmetic lives in the plain C# `SlotSpinMath`.

**How to use:**
1. Add `SpinController` (or create the `SlotUI` prefab from the Neoxider menu) and assign ordered `Row` columns, a complete `SpritesData`, optional `BetsData` / `LinesData`, and a wallet object implementing `IMoneySpend` (and `IMoneyAdd` for payouts).
2. Bind buttons to `StartSpin()`, `AddLine()` / `RemoveLine()`, `AddBet()` / `RemoveBet()` / `SetMaxBet()`.
3. React to `OnEnd(bool)`, `OnWin(int)` and `OnWinLines(int[])`; read `GetLastResult()` from code.
4. Keep **Auto Payout** on unless your own `OnWin` handler pays the player.

---

## Pure bet and payout math (`SlotSpinMath`)

`SlotSpinMath` (`Scripts/Bonus/Slot/SlotSpinMath.cs`) is the plain C# arithmetic behind `CurrentSpinPrice` and
`LastPayout`, usable by servers, replays and tests without a scene:

| Member | Description |
|--------|-------------|
| `int ComputeSpinPrice(int lineBet, int activeLines, bool pricePerLine)` | Line bet x active lines (at least 1) when pricing per line, otherwise the line bet. Negative bets count as 0; saturates at `int.MaxValue`. |
| `int ComputePayout(float[] lineMultipliers, int lineBet)` | Sum of finite positive multipliers x bet, rounded half-to-even, saturated. |

```csharp
float[] multipliers = spin.checkSpin.GetMultiplayers(ids, activeLines);
int payout = SlotSpinMath.ComputePayout(multipliers, lineBet);
```

## Performance notes

`IsStop()` (polled every frame while a spin settles), `LastWinningPaylineIndices`, reel window queries and the
win-line travel animation do not allocate per call. Plan columns are copied through reused buffers, and visible
matrices resolve symbols without LINQ.

## Economy & per-machine weights (code)

Assign a `SlotEconomyDefinition` to the `Economy` field and, when one machine needs different drop odds, enable the local `Symbol Weight Overrides` table (one editable weight per symbol, synced by symbol id in `OnValidate` — reordering or extending the shared symbol list is safe). `PickEconomySymbolId()` returns a weighted symbol id honoring the override (falls back to the definition when disabled or when a symbol has no entry). Inspector `⋮` menu **Normalize Weights** rescales all positive local weights to a total of `1`. See [SlotEconomyDefinition.md](./SlotEconomyDefinition.md).

**One-call economy spin (9.13.0):** `StartEconomySpin()` builds the whole outcome from the economy (weighted pick per cell, then the special/wild rule along each active payline), queues it via `ForceNextOutcome`, and starts the spin. The pieces are public too: `BuildEconomyOutcomeMatrix()` (plus a deterministic overload taking a `Func<int>` picker for tests/replays/servers) and `EvaluateActivePaylinesWithEconomy()` — one `LineResult` (payouts, special flag) per active payline of the settled grid. Symbol ids must match `SpritesData` visual ids.

```csharp
// Disable Auto Payout in the Inspector when this handler owns economy payouts.
spin.OnEnd.AddListener(_ =>
{
    foreach (SlotEconomyDefinition.LineResult line in spin.EvaluateActivePaylinesWithEconomy())
    {
        if (line.IsWin && Money.I != null)
        {
            Money.I.Add(line.MoneyReward);
        }
    }
});
spin.StartEconomySpin();
```

Automatic payouts use `CheckSpin` multipliers times the selected line bet. Economy `MoneyReward` and
`BonusReward` values are queried separately; a caller that distributes those rewards owns that policy.

## Payline query API (code)

| Member | Description |
|--------|-------------|
| `EvaluatedPaylineDefinitionCount` | Number of payline definitions used for bets/check (`countLine` capped). |
| `GetPaylineDefinitionsSnapshot()` | All effective line defs (`LinesData` or fallback), index = line id used by `CheckSpin`. |
| `GetPaylineWindowRowsMatrix()` | `int[lineIndex, col]` = window row from bottom (`corY`). |
| `GetActivePaylineWindowRowsMatrix()` | Same, first `EvaluatedPaylineDefinitionCount` rows only. |
| `GetPaylineSymbolIdsMatrix(bool refresh)` | `int[lineIndex, col]` symbol IDs along lines. |
| `GetActivePaylineSymbolIdsMatrix(bool refresh)` | Active subset for betting scope. |
| `TryGetPaylineSlotElements(int lineIx, out SlotElement[], bool refresh)` | References per column for animations. |
| `LastWinningPaylineIndices` | Read-only indices after last finished spin (cleared on new spin / lose). |
| `GetLastWinningPaylinesSlotElements(bool refresh)` | `SlotElement[][]` parallel to `LastWinningPaylineIndices`. |
| `GetLastWinningPaylinesSymbolIds(bool refresh)` | `int[whichWin, col]` IDs on winning lines only. |
| `GetLastWinningPaylinesWindowRows()` | `int[whichWin, col]` window rows for last win. |

## Configure & snapshot (code)

| Member | Description |
|--------|-------------|
| `Rows` | Column reels; changes require idle reels and no active/starting spin. |
| `ActivePaylineCount` | Get/set active lines (clamped). |
| `VisibleWindowRows` | Window height; triggers layout + price refresh. |
| `BetSelectionIndex` | Index into `betsData.bets`. |
| `DelayBetweenColumnSpins` | Delay between column spin starts. |
| `SpinTimeoutSeconds` | Unscaled safety deadline; a non-settling row is completed to the planned outcome. |
| `IsSpinInProgress` | True during an accepted spin; cleared before result callbacks so the final callback can start another spin. |
| `CompleteActiveSpinImmediately()` | Settle the active plan and run normal result callbacks once; repeated calls are safe. |
| `CancelActiveSpin()` | Abandon presentation without result callbacks and attempt to refund the recorded bet through `moneyAdd`. |
| `CurrentSpinPrice` | Price basis for next `StartSpin`. |
| `TryPayForSpin()` | Pays the current spin price. Price `<= 0` is explicit free mode; positive prices require `moneySpend` and a successful `Spend`. |
| `ConfigureSlotRuntime(visibleWindowRows, activePaylineCount, fallbackMin, fallbackMax)` | Batch configuration; ignored throughout an active/starting spin, including stagger gaps. `-1 / -1` uses the full visible window. |
| `WinLinePlayback` | Mutable `WinLineRendererPlayback` settings. |
| `GetRuntimeSnapshot(refresh)` | `SpinRuntimeSnapshot` struct (idle, sizes, prices, fallback resolved, win copy). |

**CheckSpin:** `LinesDataAsset`, `SpritesMultiplierData`, `SequenceLength`, `SetSequenceLength`, `GetEffectiveLines`, `GetPaylineDefinitionCount`, `GetResolvedFallbackWindowRowRange`, `UsesFallbackPaylinesOnly`, `SetFallbackPaylineWindowRows`, `ClearLegacyFallbackSingleRowBinding` - see [CheckSpin.md](./CheckSpin.md).

##### `SpinRuntimeSnapshot` (fields)

| Field | Meaning |
|-------|---------|
| `IsIdle` | All `Row` stopped (`IsStop`). |
| `WindowHeight`, `ColumnCount` | Visible rows x columns. |
| `ActivePaylineCount` | Inspector `countLine`. |
| `EvaluatedPaylineCount` | Lines actually checked (`min(countLine, defs)`). |
| `TotalPaylineDefinitionCount` | All line definitions (asset or fallback). |
| `BetIndex`, `SpinPrice` | Current bet index and `CurrentSpinPrice`. |
| `CheckSpinActive` | `checkSpin.isActive`. |
| `UsesFallbackPaylinesOnly` | No valid Lines Data for this window size. |
| `FallbackMinRaw` / `FallbackMaxRaw` | Serialized -1 or row index from `CheckSpin`. |
| `FallbackResolvedMinRow` / `FallbackResolvedMaxRow` | Resolved inclusive window rows (0 = bottom). |
| `LastWinningPaylineIndicesCopy` | Copy of last spin's winning line indices (may be empty). |

## Inspector groups (summary)

`StartSpin`, `AddLine` / `RemoveLine`, `AddBet` / `RemoveBet` and `SetMaxBet` carry `[Button]`, so a spin and
bet/line changes can be triggered straight from the Inspector (Play Mode).

Highlights:

| Group / concept | Notes |
|-----------------|--------|
| **General** | **`checkSpin`** (lines + multipliers), **`betsData`**, **`allSpritesData`**, **`ChanceWin`** ([0-1], YAML alias `chanseWin`), **`moneySpend`**, column refs **`_rows`**, window **`_space`** / **`_setSpace`**, timings **`timeSpin`**, **`_delaySpinRoll`**, **`offsetY`**. |
| **Bet / lines UI** | **`_betsId`**, **`_textCountLine`**, **`_moneyGameObject`**, **`_firstWin`**, **`_logFinalVisuals`**, **`_debugLogWarnings`**. |
| **Runtime matrices** | **`Elements`**, **`finalVisuals`**, **`FinalElementIDs`** (filled after reels stop; column `x`, row `y=0` bottom). |
| **Visual -> Win Line Playback** | **`_winLinePlayback`** (`WinLineRendererPlayback`): optional **LineRenderer** paths, color modes, timing. |

## Unity Events

| Event | When |
|-------|------|
| `OnStartSpin` | Spin begins |
| `OnEndSpin` | All columns stopped, results processed |
| `OnEnd(bool)` | Final callback (win / lose) |
| `OnWin` / `OnWinLines` | Win amount / winning line indices |
| `OnLose` | No win |
| `OnChangeBet` / `OnChangeMoneyWin` | Bet price or win display string updates |

Spin timing is independent of `Time.timeScale`. If a row stops updating, the unscaled deadline completes every
column to the precomputed plan and emits the normal completion events once. Disabling the controller preserves
the plan without invoking callbacks into inactive objects; reactivation completes it through the normal path.
An owning durable lifecycle may instead call `CancelActiveSpin()` for cancellation and the built-in refund
attempt; it should account for that refund before applying its own recovery policy.

Reentrant `StartSpin` calls from price notifications or wallet callbacks cannot charge another bet. Layout,
payline count, and bet-selection changes are blocked until the accepted spin finishes. `IsStop()` reports
only whether reels are moving; use `IsSpinInProgress` to distinguish idle gaps from a finished spin.

Every `SpritesData` visual entry must be assigned before starting. Invalid tables are rejected before
payment and keep a queued forced outcome intact. Queries can still resolve assigned visuals in a partial
table for authoring/diagnostics. Missing reel columns are represented by missing cells, which cannot win.

Prices and rounded payouts saturate at `int.MaxValue`; they cannot wrap into a free spin or a zero payout.
Negative configured bets contribute zero; non-positive/non-finite multipliers contribute no payout.
Configure practical values within the precision of the chosen wallet (`IMoneySpend` / `IMoneyAdd` use floats).

## See Also

- [Module Root](../README.md)
