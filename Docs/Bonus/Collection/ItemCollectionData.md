# ItemCollectionData

**What it is:** a `ScriptableObject` describing one collectible item: name, description, icon, numeric type,
rarity and numeric category. Script: `Scripts/Bonus/Collection/ItemCollectionData.cs`, namespace `Neo.Bonus`.
The same file declares the `ItemRarity` enum (`Common`, `Rare`, `Epic`, `Legendary`).

**How to use:**
1. Create an asset via **Create > Neoxider > Bonus > Collection > Item Collection Data** and fill its fields.
2. Add the assets to the **Item Collection Datas** array of a [Collection](Collection.md). The array index is the
   item id used by unlocks and saves, so keep the order stable once you ship.
3. Views read the data: [ItemCollection](ItemCollection.md) shows the sprite,
   [ItemCollectionInfo](ItemCollectionInfo.md) shows name and description, and [Box](Box.md) shows the granted
   item's sprite.

---

## Fields

| Field (Inspector) | Property | Type | Description |
|-------------------|----------|------|-------------|
| Item Name | `ItemName` | `string` | Display name. |
| Description | `Description` | `string` | Multi-line description for detail views. |
| Sprite | `Sprite` | `Sprite` | Icon used by item and box views. |
| Item Type | `ItemType` | `int` | Free numeric type id for your own grouping. |
| Rarity | `Rarity` | `ItemRarity` | Rarity tier; defaults to `Common`. |
| Category | `Category` | `int` | Free numeric category id. |

All properties are read-only at runtime; the asset holds configuration only and references no scene objects.

## Queries by type, rarity and category

`Collection` filters its catalog by these fields:

```csharp
int[] epicIds = collection.GetIdsByRarity(ItemRarity.Epic);
int[] weapons = collection.GetIdsByCategory(2);
int unlockedEpic = collection.GetUnlockedCountByRarity(ItemRarity.Epic);
```

## See also

- [Collection](Collection.md) — the catalog and unlock state.
- [Collection module](README.md)
