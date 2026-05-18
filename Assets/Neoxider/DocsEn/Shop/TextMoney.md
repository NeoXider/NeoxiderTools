# TextMoney

**Purpose:** A UI component that automatically subscribes to currency changes in the `Money` manager and displays the current balance in a `TextMeshPro` text field.

## Runtime Source Switching

`TextMoney.SetMoneySource(Money money)` changes the displayed wallet at runtime and safely re-subscribes to the selected `Money` reactive property.

`TextMoney.SetMoneySaveKey(string saveKey)` selects a wallet by `Money.SaveKey`.

- Empty key: use explicit `Money Source`, then `Money.I`.
- Non-empty key: display the first registered/found `Money` with the matching save key.

## Setup

1. Add the `TextMoney` component to an object containing a `TextMeshPro` text component (`Add Component > Neoxider > Shop > TextMoney`).
2. Configure which balance type to display (current money, level money, or all money).

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_displayMode` | Balance display mode: `Money` (main), `LevelMoney` (level only), `AllMoney` (total sum). |
| `_moneySource` | Currency source (`Money` component). If left empty, the global singleton `Money.I` is used. This is useful if you have multiple resource types (e.g., Energy, Stars) with their own local `Money` instances. |
| `amount` | (Info) The current displayed value. |

## See Also

- [Money](Money.md) - Currency manager.
- [Module Root](../README.md)
