# ShopPurchaseButtonView

**What it is:** `Neo.Shop.ShopPurchaseButtonView` — reactive purchase-button state for one `ShopItem` slot. While enabled it subscribes to shop refreshes and to the balance of the same currency the purchase would spend from (per-item `Currency Override Save Key` included, via `Shop.ResolveCurrencyMoney`) and immediately drives the `ButtonPrice` state — `Buy` / `Select` / `Selected` / `Unaffordable` — plus the buy `Button.interactable` flag for unaffordable items.

**Usage:** drop it on (or under) a `ShopItem`; `Shop`, `ShopItem`, `ButtonPrice`, and the buy `Button` auto-resolve. Affordability comes from `Shop.CanAfford(itemId)` — owned and free items are always affordable; custom `IMoneySpend` wallets can implement `IMoneyCanSpend` to be queryable, otherwise the view stays optimistic and `Buy` reports the failure. Rebinding the slot to another item (list refresh, category switch) re-subscribes to that item's currency automatically.

**API:** `Refresh()` (manual re-evaluation), `CurrentState`. Related: `ButtonPrice.ButtonType.Unaffordable` with its optional `Visual.unaffordable` GameObject group, `Unaffordable` button text, and `OnUnaffordable` event (old prefabs without the group keep showing the Buy visuals).

**See also:** [Shop](Shop.md), [ButtonPrice](ButtonPrice.md), [Money](Money.md).
