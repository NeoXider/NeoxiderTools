# InterfaceMoney

**What it is:** the currency contracts used by `Shop` (`Scripts/Shop/InterfaceMoney.cs`). The interfaces are in the
global namespace; `MoneySpendStatus` and `MoneySpendResult` are in `Neo.Shop`. `Money` implements all of them, and a
custom wallet implements only what it supports.

**How to use:**
1. Implement `IMoneySpend` on a wallet component (minimum), plus `IMoneyCanSpend` for affordability and `IMoneyAdd`
   so failed purchases can be refunded.
2. Point `Shop.moneySpendSource` at that GameObject (or call `SetMoneySpendSource`).

---

## Interfaces

| Interface | Member | Used by Shop for |
|-----------|--------|------------------|
| `IMoneySpend` | `bool Spend(float count)` | The debit when nothing richer is implemented. |
| `IMoneySpendWithResult : IMoneySpend` | `MoneySpendResult TrySpend(float count)` | A typed debit result (confirmed / rejected / pending). |
| `IMoneySpendAuthority : IMoneySpendWithResult` | `bool CanConfirmSpendNow(float count)` | Networked wallets: detect "pending server confirmation" without a failure event. |
| `IMoneyAdd` | `void Add(float count)` | Refunding a debit when the profile commit fails. Without it the refund is logged as impossible. |
| `IMoneyCanSpend` | `bool CanSpend(float count)` | `Shop.CanAfford` and the affordability precheck before a debit. Without it `CanAfford` stays optimistic. |

## MoneySpendResult

`MoneySpendResult` is a readonly struct with `Status`, `Amount`, `BalanceBefore`, `BalanceAfter` and the helpers
`IsConfirmed`, `IsRejected`, `IsPendingServerAuthority`.

| `MoneySpendStatus` | Meaning |
|--------------------|---------|
| `Confirmed` | The balance was lowered. |
| `RejectedInvalidAmount` | Negative, NaN or infinite amount. |
| `RejectedInsufficientFunds` | Not enough balance (or no representable decrease). |
| `RequestedServerAuthority` | A networked client forwarded the spend to the server. |

```csharp
public sealed class EnergyWallet : MonoBehaviour, IMoneySpend, IMoneyCanSpend, IMoneyAdd
{
    [SerializeField] private float _energy = 10f;

    public bool CanSpend(float count) => count >= 0f && _energy >= count;

    public bool Spend(float count)
    {
        if (!CanSpend(count))
        {
            return false;
        }

        _energy -= count;
        return true;
    }

    public void Add(float count) => _energy += Mathf.Max(0f, count);
}
```

## See Also
- [Shop](Shop.md)
- [Money](Money.md)
- <- [Shop](README.md)
