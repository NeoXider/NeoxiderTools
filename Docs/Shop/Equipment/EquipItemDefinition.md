# EquipItemDefinition

**What it is:** `Neo.Shop.EquipItemDefinition` — a ScriptableObject for one equippable piece: `Id`, `CategoryId`, `Sprite`. Create via `Create → Neoxider → Shop → Equip Item`. Convention: `Id` matches the `ShopItemData` id when the item is sold, linking purchase and wearing with one string. Script: `Scripts/Shop/Equipment/EquipItemDefinition.cs`.

**How to use:**
1. Create one asset per wearable piece via **Create > Neoxider > Shop > Equip Item** and set `Id`, `CategoryId`
   and `Sprite`.
2. Add the assets to the catalog of an [EquipmentManager](EquipmentManager.md) and call `EquipById(id)`.

**See also:** [EquipmentManager](EquipmentManager.md), [Shop module](../README.md).
