# WheelMoneyWin

**What it is:** a small adapter (`MonoBehaviour`, `Scripts/Bonus/WheelFortune/WheelMoneyWin.cs`) that turns a wheel sector id into a money prize: it adds `wins[id]` to a `Money` wallet and optionally shows the amount in a `TMP_Text`.

**How to use:**
1. Add `WheelMoneyWin` next to a [WheelFortune](WheelFortune.md) and fill **Wins** with one amount per sector, in sector order.
2. Optionally assign the **Wallet** (`Money`); when empty the global `Money.I` is used. Assign **Prize** to show the won amount.
3. Wire `WheelFortune.OnWinIdVariant(int)` to `WheelMoneyWin.Win(int)`.

---

## Fields

| Field | Description |
|-------|-------------|
| `prize` | Optional label that shows the won amount. |
| `wins` | Prize amount per sector id. |
| `_wallet` | Optional wallet; empty uses `Money.I`. |
| `_debugLogWarnings` | Logs out-of-range ids and a missing wallet. |

## C# API

```csharp
moneyWin.Wallet = playerWallet;               // Explicit wallet instead of Money.I.
wheel.OnWinIdVariant.AddListener(moneyWin.Win);
```

## See Also

- [Module Root](../README.md)
