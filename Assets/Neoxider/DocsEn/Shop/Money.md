# Money

`Money` is a reusable currency component and an implementation source for `IMoneySpend` and `IMoneyAdd`. It can act as the main singleton `Money.I` or as an explicitly referenced local currency source. File: `Assets/Neoxider/Scripts/Shop/Money.cs`, namespace: `Neo.Shop`.

## Common use

1. Add `Money` to a scene object.
2. Leave `Set Instance On Awake` enabled only on the primary currency source.
3. Pass additional `Money` instances by reference to systems that should not depend on `Money.I`.
4. Use `Add`, `Spend`, `SetMoney`, or level-money helpers depending on the economy flow.

## Main fields

- `Money Save` is the base save key.
- `CurrentMoney` stores the current balance.
- `LevelMoney` stores per-level or per-session income.
- `AllMoney` stores the total accumulated amount.
- `LastChangeMoney` stores the last delta applied to the balance.

## Main API

| API | Description |
|-----|-------------|
| `money` | Current balance. |
| `levelMoney` | Current `LevelMoney` amount. |
| `allMoney` | Total accumulated amount. |
| `LastChangeMoneyValue` | Last applied delta. |
| `Add(float amount)` | Adds money to current and total values. |
| `Spend(float amount)` | Tries to spend money and returns `true` on success. |
| `CanSpend(float amount)` | Checks whether enough money is available. |
| `AddLevelMoney(float amount)` | Adds to `LevelMoney`. |
| `SetLevelMoney(float amount = 0)` | Sets `LevelMoney` directly. |
| `SetMoney(float amount = 0)` | Sets the current balance directly. |
| `SetMoneyForLevel(bool resetLevelMoney = true)` | Transfers `LevelMoney` into the current balance. |

## Reactive values

The current version uses `ReactivePropertyFloat`, not legacy `UnityEvent`-style money callbacks. Subscribe to `.OnChanged` on:

- `CurrentMoney`
- `LevelMoney`
- `AllMoney`
- `LastChangeMoney`

## Save behavior

- Persistence uses `SaveProvider.SetFloat()` and `SaveProvider.GetFloat()`.
- `CurrentMoney` and `AllMoney` are stored.
- `LevelMoney` is usually session-scoped and is not treated as permanent balance.

## See also

- [README](./README.md)
- [Russian Shop docs](../../Docs/Shop/README.md)
- [SaveProvider](../Save/SaveProvider.md)
