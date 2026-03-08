# Shop

`Shop` is the main runtime controller of the shop module. It loads saved prices and equipped state through `SaveProvider`, shows previews, creates missing `ShopItem` views when configured, processes purchases through `IMoneySpend`, and refreshes visuals. File: `Assets/Neoxider/Scripts/Shop/Shop.cs`, namespace: `Neo.Shop`.

## Typical setup

1. Create one or more `ShopItemData` assets.
2. Add `Shop` to a scene object.
3. Assign `Shop Item Datas`.
4. Assign either ready-made `Shop Items` or a `Prefab` plus `Container` for auto-generation.
5. Set `Money Spend Source` to a component implementing `IMoneySpend`, or leave it empty to use `Money.I`.

## What it handles

- Loads startup prices from `ShopItemData` or saved values.
- Restores the last equipped item.
- Can auto-create missing `ShopItem` entries from a prefab.
- Can auto-subscribe item buy buttons.
- Purchases via `IMoneySpend`.
- Updates preview and item visuals after selection or purchase.

## Main properties

| Property | Description |
|----------|-------------|
| `Prices` | Current runtime price array. |
| `ShopItemDatas` | Assigned item data assets. |
| `PreviewId` | Current preview item id. |
| `Id` | Current selected item id. Setting it calls selection logic. |

## Main inspector settings

- `Shop Item Datas`
- `Shop Item Preview`
- `Shop Items`
- `Use Set Item`
- `Auto Subscribe`
- `Activate Saved Equipped`
- `Key Save`
- `Key Save Equipped`
- `Change Preview On Purchase Failed`
- `Container`
- `Prefab`
- `moneySpendSource`

## Main API

| API | Description |
|-----|-------------|
| `ShowPreview(int id)` | Shows one item in the preview without buying it. |
| `Buy()` | Buys the item currently shown in preview. |
| `Buy(int id)` | Buys an item by id. |
| `Visual()` | Refreshes all `ShopItem` visuals. |

## Events

- `OnSelect`
- `OnPurchased`
- `OnPurchaseFailed`
- `OnLoad`

## Save behavior

The current version uses `SaveProvider`, not direct `PlayerPrefs` calls:

- prices are read through `SaveProvider.GetInt(...)`
- changed prices are written through `SaveProvider.SetInt(...)`
- the equipped item id is stored through `SaveProvider.SetInt(...)`

`Shop.Save()` updates values in the active provider backend and does not force a separate `SaveProvider.Save()` call by itself.

## Purchase behavior

- If an item price is already `0`, the item is selected without spending money.
- If the purchase succeeds and `ShopItemData.isSinglePurchase` is enabled, the item price becomes `0`.
- If the player cannot afford an item, `OnPurchaseFailed` is raised.
- If `Change Preview On Purchase Failed` is enabled, the preview can still switch to the failed item.

## See also

- [README](./README.md)
- [Money](./Money.md)
- [Russian Shop docs](../../Docs/Shop/README.md)
