# TextMoney

**Purpose:** A UI component that automatically subscribes to currency changes in the `Money` manager and displays the current balance in a `TextMeshPro` text field.

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
