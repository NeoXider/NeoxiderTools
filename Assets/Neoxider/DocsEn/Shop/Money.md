# Money

**Purpose:** Global in-game currency manager (Singleton). It automatically saves and loads the balance using `SaveProvider`, and supports reactive properties (`ReactiveProperty`) for easy UI binding.

## Setup

- Add the component via `Add Component > Neoxider > Shop > Money` to a manager object in the scene (preferably a persistent prefab that survives scene loads).
- Typically, one instance is used per game (`Money.I`).

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_moneySave` | The `SaveProvider` save key for the main balance. |
| `st_levelMoney` | References to `SetText` components for displaying current level earnings. |
| `st_money` | References to `SetText` components for displaying the global balance. |
| `t_levelMoney` | Direct references to `TMP_Text` components for level earnings. |
| `t_money` | Direct references to `TMP_Text` components for the main balance. |

## API & Usage

You can access the manager from anywhere via the global singleton:
```csharp
// Add 100 coins
Money.I.Add(100f);

// Try to spend 50 coins
bool success = Money.I.Spend(50f);
if (success) {
    // Purchase successful
}
```

To display the balance in the UI, it is recommended to use the `TextMoney` component.

## See Also

- [TextMoney](TextMoney.md) - UI component for text display.
- [Module Root](../README.md)
