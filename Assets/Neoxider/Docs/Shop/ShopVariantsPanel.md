# ShopVariantsPanel

**What it is:** `Neo.Shop.ShopVariantsPanel` — a furniture/equipment variants panel on top of the existing Shop views. It pairs a `ShopListView` (item views, category filtering, button wiring) with an optional `EquipmentManager` and keeps every visible slot rendered as **unowned / owned / equipped**. Buying an unowned variant equips it after the successful purchase (`Equip After Purchase`), and the panel refreshes automatically after ownership, selection, equipment, and list changes.

**Visuals:** fully prefab-driven. Implement `IShopVariantView` on any component under a `ShopItem` to receive `ApplyVariantState(state, data)` — supply your own sprites, badges, preview dimming, lock/check icons, captions, and layout. `ShopVariantStateView` is the ready-made implementation: three GameObject groups (unowned/owned/equipped) plus `OnUnowned/OnOwned/OnEquipped` UnityEvents; it never resizes or repositions anything.

**Usage:** place the panel near the `ShopListView` (auto-resolves), optionally assign an `EquipmentManager` (item ids must match `EquipItemDefinition` ids; Shop selection is forwarded via `Forward Selection To Equipment`). Wire an optional empty/unequip button to `Unequip()` — it clears the `Unequip Category Id` slot and the Shop selection. API: `Equip(id)`, `Unequip()`, `GetState(id)`, `RefreshStates()`; events `OnVariantEquipped(string)`, `OnUnequipped`.

**See also:** [ShopListView](ShopListView.md), [EquipmentManager](Equipment/EquipmentManager.md), [ShopPurchaseButtonView](ShopPurchaseButtonView.md).
