# Money

**Purpose:** Global in-game currency manager (Singleton). By default it saves and loads the balance using `SaveProvider`, and supports reactive properties (`ReactiveProperty`) for easy UI binding. You can disable persistence for session-only modes and demos (NoCode-friendly).

## Lookup by Save Key

`Money` instances register themselves at runtime and can be resolved by save key:

```csharp
Money gems = Money.FindBySaveKey("Gems");
bool found = Money.TryFindBySaveKey("Gems", out Money wallet);
```

This is used by `ShopItemData.CurrencyOverrideSaveKey`, `ShopBundleData.CurrencyOverrideSaveKey`, and `TextMoney`.

If no key is selected, systems fall back to the usual `Money.I` singleton or the Shop default currency.

Loading persisted balances on `Start` notifies reactive subscribers, including views that subscribed earlier in `OnEnable`. Public `ReloadBalanceFromSave()` also notifies, so HUD and affordability views do not depend on script execution order.

## Setup

- Add the component via `Add Component > Neoxider > Shop > Money` to a manager object in the scene (preferably a persistent prefab that survives scene loads).
- Typically, one instance is used per game (`Money.I`).

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_moneySave` | The `SaveProvider` save key for the main balance. |
| `_persistMoney` | When enabled (default), balance is loaded on start and written on changes. When disabled, balance stays in memory only (no load/setfloat for currency keys). |
| `_maxMoney` | Soft cap for this wallet. `0` = unlimited. `Add()` and `SetMoney()` clamp to it; `AddOverflow(float)` ignores it (for bonus/overflow rewards allowed to exceed the cap). Lets you build capped resources (energy/stamina/lives) without custom code. |
| `_clientMutationAuthority` | Server-side client-operation policy; default `NetworkAuthorityMode.ServerOnly`. Does not gate direct offline/server calls. |
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

MoneySpendResult result = Money.I.TrySpend(50f);
if (result.IsConfirmed) {
    // The spend has already been applied.
}

// Set balance (persists when persistence is on)
Money.I.SetMoney(500f);
// Alias for UnityEvent / buttons:
Money.I.SetCurrentMoney(500f);

// Add ignoring the soft cap _maxMoney (bonus/overflow reward above the cap):
Money.I.AddOverflow(5f);

// For regular uGUI Button.onClick use the void wrapper:
Money.I.SpendFromButton(50f);

// Remove persisted keys and reset runtime balance to zero
Money.I.ClearSavedMoneyAndReset();

// Reload balance from SaveProvider after external key changes
Money.I.ReloadBalanceFromSave();
```

To display the balance in the UI, it is recommended to use the `TextMoney` component.

**NoCode / UnityEvent:** wire `Add(float)`, `AddOverflow(float)`, `SetCurrentMoney(float)`, `ClearSavedMoneyAndReset()`, and `ReloadBalanceFromSave()` to `UnityEvent`. `Spend(float)` returns `bool`, so regular `Button.onClick` does not list it; use `SpendFromButton(float)` for buttons. Use `Spend(float)` from code when you need the success/fail result.

`Spend(float)` rejects negative and non-finite amounts. `TrySpend(float)` classifies negative values, `NaN`, and infinities as `RejectedInvalidAmount`; it returns `MoneySpendResult` with `Confirmed`, `RejectedInvalidAmount`, `RejectedInsufficientFunds`, or `RequestedServerAuthority`.

Balances use `float`. After input validation and affordability, a positive authoritative spend must produce a stored balance smaller than the current one. For example, at `100000000f`, price `1f` rounds back to the same balance: `CanSpend(1f)` is false and an authoritative `TrySpend(1f)` returns `RejectedInsufficientFunds` without mutation, events, persistence or replication. Shop therefore cannot grant that paid item. Command ingress uses the same check before its rate budget. Zero-cost spends retain their existing success behavior for an affordable balance.

The representable decrease need not equal the nominal price. Ordinary fractional costs remain supported; `10f - 0.1f` and `100000000f - 5f` succeed, with the latter storing `99999992f` (a decrease of 8). `MoneySpendResult.Amount` and `LastChangeMoney` retain the nominal input; `BalanceBefore`/`BalanceAfter` expose stored float balances. Income caps still apply to nominal inputs through the existing Add/Set paths; this check introduces neither exact arithmetic nor a new cap policy. Choose balance/price ranges whose float precision suits your economy.

`SetMoneyForLevel(bool resetLevelMoney = true)` pays the level accumulator through the capped income path. A negative or non-finite accumulator is rejected with a diagnostic and returns zero; the main wallet and accumulator remain unchanged so the caller can correct it. `AddLevelMoney` / `SetLevelMoney` still support adjusting the level accumulator directly.

When Shop rolls back a confirmed purchase, its internal refund restores the debit above the soft cap and leaves `AllMoney` unchanged. Rewards should continue to use `Add` or `AddOverflow`; neither is a refund API.

In Mirror networked client-only mode, local code must not treat a spend as a confirmed purchase. `CanSpend(float)` is a local snapshot/precheck, not an authority decision. `Shop` grants items/bundles only when the spend is confirmed locally/server-side; pending server authority is not treated as a local failure and does not run a local grant.

A pure-client `TrySpend` still returns `RequestedServerAuthority` for any finite nonnegative input before checking local affordability or effective float decrease: its replica can be stale. The server evaluates its own current balance. Shop's existing local affordability precheck can still disable/reject a locally unavailable price; it does not turn a client wallet's pending spend into a confirmed purchase.

## Client Mutation Authority (11.0.0)

`ClientMutationAuthority` is a typed C# property over the serialized **Client Mutation Authority** Inspector field. The server's value controls Command admission; changing a client replica's Inspector/property does not change server permission.

| Policy | Remote client requests |
| --- | --- |
| `ServerOnly` (default) | Rejected for every money operation, including Spend, rewards, sets and level payouts. |
| `None` | Explicit cooperative/trusted-client compatibility: any admitted peer may request the existing amount-based operations. |
| `OwnerOnly` | Only this wallet GameObject's `NetworkIdentity.connectionToClient`; a scene wallet without an owner rejects remote callers. |

`None` and `OwnerOnly` trust an admitted caller's amounts; they do not prove that a reward was earned. `Money.I`, SaveKey and the caller's unrelated player identity do not establish wallet ownership. Existing `NeoNetworkState.IsAuthorized` allowances for direct-server null sender and the exact host local connection are retained; merely executing on a server grants no remote caller permission.

The Command checks server `isNetworked`, known client opcodes 0–6, finite/sign/affordability rules, a named authority policy and sender authorization before consuming its per-peer budget or mutating/synchronizing state. Unknown opcodes/policies and refund opcode 7 always reject. The limiter uses an unscaled double clock, so pausing game time does not freeze its windows. Direct offline/server/host APIs keep their previous cap, overflow, spend and level-adjustment paths.

**Migration:** old assets lacking the new field are intended to import as `ServerOnly`, not enum-zero `None`; actual import acceptance is covered by a dedicated authored fixture and remains part of release verification. Set `None` explicitly for an intentionally trusted cooperative economy, or configure wallet ownership and `OwnerOnly`. There is no purchase/reward request acknowledgement in this change: a pure-client `TrySpend` can still return `RequestedServerAuthority` even when the server rejects it, and void client methods return without a local mutation. Shop's existing pending path does not provide a correlated server request/result. Authoritative purchases must be invoked/confirmed by server gameplay code.

## See Also

- [TextMoney](TextMoney.md) - UI component for text display.
- [Module Root](README.md)
