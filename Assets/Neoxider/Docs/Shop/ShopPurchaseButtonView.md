# ShopPurchaseButtonView

**What it is:** `Neo.Shop.ShopPurchaseButtonView` — reactive purchase-button state for one `ShopItem` slot. While enabled it subscribes to shop refreshes and to the balance of the same currency the purchase would spend from (per-item `Currency Override Save Key` included, via `Shop.ResolveCurrencyMoney`) and immediately drives the `ButtonPrice` state — `Buy` / `Select` / `Selected` / `Unaffordable` — plus the buy `Button.interactable` flag for unaffordable items.

**Usage:** drop it on (or under) a `ShopItem`; `Shop`, `ShopItem`, `ButtonPrice`, and the buy `Button` auto-resolve. Affordability comes from `Shop.CanAfford(itemId)` — owned and free items are always affordable; custom `IMoneySpend` wallets can implement `IMoneyCanSpend` to be queryable, otherwise the view stays optimistic and `Buy` reports the failure. Rebinding the slot to another item (list refresh, category switch) re-subscribes to that item's currency automatically.

**API:** `Refresh()` (manual re-evaluation), `CurrentState`. Related: `ButtonPrice.ButtonType.Unaffordable` with its optional `Visual.unaffordable` GameObject group, `Unaffordable` button text, and `OnUnaffordable` event (old prefabs without the group keep showing the Buy visuals).

Row binding and selection changes are observed through `ShopItem.OnViewChanged`, so category changes need no extra Shop refresh. `Shop.SetMoneySpendSource(...)` refreshes the state and transfers the balance subscription even for the same item id. Disabled views detach their row, Shop, and wallet listeners; calling `Refresh()` while disabled does not reconnect them.

Affordability and Buy/Select state use the full runtime `float` price. The existing `ButtonPrice` caption is still rounded to an integer; use integral currency units when the caption must exactly match the debit.

**See also:** [Shop](Shop.md), [ButtonPrice](ButtonPrice.md), [Money](Money.md).
