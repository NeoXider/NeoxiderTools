# EquipmentManager

**What it is:** `Neo.Shop.EquipmentManager` — multi-category equipment (dress-up/skins): one item per category, the sprite is applied to a slot (`SpriteRenderer` or `Image` + optional `SetNativeSize`), the worn set persists via `SaveProvider` (key `Equip_<category>`).

**Usage:** catalog = `EquipItemDefinition[]`; slots = `categoryId` → visual target (+ `defaultItemId`). API: `EquipById(id)` (NoCode entry — wire purchases/cell clicks here), `Equip(item)`, `Unequip(categoryId)`, `ToggleById(id)`, `GetEquippedId(categoryId)`, `IsEquipped(id)`, `OnEquipChanged(categoryId, itemId)` event. The worn set restores in `Start()`.

**Shop pairing:** ownership/purchases stay on `Shop`/`ShopItemData`; wire the shop purchase/equip event to `EquipById` for a buy-then-wear flow.
