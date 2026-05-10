# CheckSpin

**Purpose:** Serializable helper on **`SpinController`**: resolve paylines, compute multipliers, force-win / force-lose planning (`SetWin` / `SetLose`). Matrices of symbols and gizmo helpers live on **`SpinController`** — see [SpinController.md](./Slot/SpinController.md).

## Fallback rows (no valid `Lines Data`)

- **`Fallback Window Row Min` / `Max`** (`SpinController` → Check Spin): inclusive window row indices, **0 = bottom**.
- **−1** on either bound → auto: min→**0**, max→**(window height − 1)** (full window by default).
- Single middle row when window height is 3: **`Min = 1`**, **`Max = 1`**.
- Old **`Fallback Window Row Index`** assets migrate automatically.

## API (code)

| Member | Description |
|--------|-------------|
| `isActive` | Disable all logic when `false`. |
| `SequenceLength` | Read-only minimum match length (≥ 2). |
| `LinesDataAsset` / `SpritesMultiplierData` | Line defs + symbol payouts (writable before spin). |
| `FallbackWindowRowMinRaw` / `FallbackWindowRowMaxRaw` | Serialized −1 or row index. |
| `GetEffectiveLines(columns, windowRows)` | Effective line definitions (`Lines Data` filtered or horizontal fallback). Array index = payline id. |
| `GetPaylineDefinitionCount(columns, windowRows)` | Definition count for geometry (ignores controller `countLine`). |
| `GetResolvedFallbackWindowRowRange(windowRows, out min, out max)` | Resolved inclusive fallback rows after −1/auto rules and clamping. |
| `GetResolvedFallbackWindowRow(windowRows)` | Middle of resolved range (UI / gizmo helper). |
| `UsesFallbackPaylinesOnly(columns, windowRows)` | `true` if horizontal fallback is used for this size. |
| `SetSequenceLength`, `SetFallbackPaylineWindowRows`, `ClearLegacyFallbackSingleRowBinding` | Runtime tuning / migration cleanup. |
| `GetMultiplayers`, `GetWinningLines`, `SetWin`, `SetLose` | Evaluate or reshape symbol ID matrices. |

Full Russian reference: [CheckSpin.md](../../Docs/Bonus/Slot/CheckSpin.md).

## See Also

- [SpinController (EN)](./Slot/SpinController.md)
- [Bonus module README](./README.md)
