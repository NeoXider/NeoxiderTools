# Money

**Purpose:** Global in-game currency manager (Singleton). By default it saves and loads the balance using `SaveProvider`, and supports reactive properties (`ReactiveProperty`) for easy UI binding. You can disable persistence for session-only modes and demos (NoCode-friendly).

## Setup

- Add the component via `Add Component > Neoxider > Shop > Money` to a manager object in the scene (preferably a persistent prefab that survives scene loads).
- Typically, one instance is used per game (`Money.I`).

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_moneySave` | The `SaveProvider` save key for the main balance. |
| `_persistMoney` | When enabled (default), balance is loaded on start and written on changes. When disabled, balance stays in memory only (no load/setfloat for currency keys). |
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

// Set balance (persists when persistence is on)
Money.I.SetMoney(500f);
// Alias for UnityEvent / buttons:
Money.I.SetCurrentMoney(500f);

// Remove persisted keys and reset runtime balance to zero
Money.I.ClearSavedMoneyAndReset();

// Reload balance from SaveProvider after external key changes
Money.I.ReloadBalanceFromSave();
```

To display the balance in the UI, it is recommended to use the `TextMoney` component.

**NoCode / UnityEvent:** wire `Add`, `Spend`, `SetCurrentMoney`, `ClearSavedMoneyAndReset`, `ReloadBalanceFromSave` to `UnityEvent` or UI buttons (for `float` parameters use the matching Unity event type).

## See Also

- [TextMoney](TextMoney.md) - UI component for text display.
- [Module Root](../README.md)
