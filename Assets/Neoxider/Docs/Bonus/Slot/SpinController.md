# SpinController

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Economy & per-machine weights (code)

Assign a `SlotEconomyDefinition` to the `Economy` field and, when one machine needs different drop odds, enable the local `Symbol Weight Overrides` table (one editable weight per symbol, synced by symbol id in `OnValidate` — reordering or extending the shared symbol list is safe). `PickEconomySymbolId()` returns a weighted symbol id honoring the override (falls back to the definition when disabled or when a symbol has no entry). Inspector `⋮` menu **Normalize Weights** rescales all positive local weights to a total of `1`. See [SlotEconomyDefinition.md](./SlotEconomyDefinition.md).

**One-call economy spin (9.13.0):** `StartEconomySpin()` builds the whole outcome from the economy (weighted pick per cell, then the special/wild rule along each active payline), queues it via `ForceNextOutcome`, and starts the spin. The pieces are public too: `BuildEconomyOutcomeMatrix()` (plus a deterministic overload taking a `Func<int>` picker for tests/replays/servers) and `EvaluateActivePaylinesWithEconomy()` — one `LineResult` (payouts, special flag) per active payline of the settled grid. Symbol ids must match `SpritesData` visual ids.

```csharp
spin.StartEconomySpin();
spin.OnEnd.AddListener(_ =>
{
    foreach (var line in spin.EvaluateActivePaylinesWithEconomy())
        if (line.IsWin) Money.I.Add(line.MoneyReward);
});
```

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
| `Rows` | Column reels; assign only when `IsStop()`. |
| `ActivePaylineCount` | Get/set active lines (clamped). |
| `VisibleWindowRows` | Window height; triggers layout + price refresh. |
| `BetSelectionIndex` | Index into `betsData.bets`. |
| `DelayBetweenColumnSpins` | Delay between column spin starts. |
| `CurrentSpinPrice` | Price basis for next `StartSpin`. |
| `TryPayForSpin()` | Pays the current spin price. Price `<= 0` is explicit free mode; positive prices require `moneySpend` and a successful `Spend`. |
| `ConfigureSlotRuntime(visibleWindowRows, activePaylineCount, fallbackMin, fallbackMax)` | Batch: window height + active lines + fallback row range in `checkSpin` (-1 / -1 = full visible window). Ignored while spinning (`IsStop()` required). |
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

Full RU details: .

## Inspector groups (summary)

Typical layout mirrors the RU page (). Highlights:

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

## See Also

- [Module Root](../README.md)
