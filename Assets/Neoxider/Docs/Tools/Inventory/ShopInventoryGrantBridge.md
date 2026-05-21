# ShopInventoryGrantBridge

**Назначение:** опциональный bridge-компонент, связывающий [Shop](../../Shop/Shop.md) и [InventoryComponent](./InventoryComponent.md). Слушает `Shop.OnPurchasedId(itemId)` и на каждом срабатывании ищет совпадение в таблице `Mappings`. При совпадении вызывает `InventoryComponent.AddItemData(...)` и поднимает `OnGranted(data, amount)`.

Доступно с **8.5.0**.

## Почему не на `ShopItemData`

Поля `_inventoryItem` / `_inventoryAmount` прямо на `ShopItemData` создали бы asmdef-цикл `Neo.Shop → Neo.Tools.Inventory → Neo.Tools.View → Neo.Tools.Components → Neo.Shop`. Bridge инвертирует направление — `Neo.Tools.Inventory` ссылается на `Neo.Shop`, и цикл уходит. Для пользователя это не приносит неудобства: bridge остаётся NoCode-дружественным.

## Подключение

1. На той же GameObject, где находится `Shop` (или на дочернем), добавьте компонент `Shop Inventory Grant Bridge` (`Add Component > Neoxider > Tools/Inventory > ShopInventoryGrantBridge`).
2. Заполните `_shop` (необязательно — bridge ищет Shop в родителях в `Awake`).
3. Заполните `_inventory` или включите `_useInventorySingleton` для использования `InventoryComponent.Instance`.
4. Добавьте записи в `Mappings`:
   - `Shop Item Id` — стабильный `ShopItemData.Id`.
   - `Inventory Item` — соответствующий `InventoryItemData`.
   - `Amount` — сколько выдавать за одну покупку (≥ 1).

При покупке любого `ShopItemData` (напрямую через `Shop.Buy(...)` или через `BuyBundle(...)`, который для каждого вложенного предмета поднимает `OnPurchasedId(item.Id)`) bridge просматривает свои mappings и грантит инвентарю.

## Основные поля

| Поле | Описание |
|------|----------|
| `_shop` | Источник событий. Если null — ищется через `GetComponentInParent<Shop>()` в `Awake`. |
| `_inventory` | Целевой инвентарь. Если null и `_useInventorySingleton == true`, используется `InventoryComponent.Instance`. |
| `_useInventorySingleton` | Fallback на singleton, когда `_inventory` null. |
| `_mappings` | Список `{ Shop Item Id, Inventory Item, Amount }`. |
| `OnGranted` | UnityEvent с аргументами `(InventoryItemData, int amountAdded)`. |

## Code API

```csharp
ShopInventoryGrantBridge bridge = ...;

bridge.SetShop(shop);           // программно сменить источник
bridge.SetInventory(inventory); // программно сменить инвентарь

bridge.GrantForShopItemId("sword_basic"); // ручной грант по id (NoCode-friendly UnityEvent target)
bridge.GrantDirect(inventoryItemData, 3); // напрямую — для DLC / кодовых паттернов

// Mappings можно править в рантайме
bridge.Mappings.Add(new ShopInventoryGrantBridge.GrantMapping {
    ShopItemId = "season_pass_reward",
    InventoryItem = chest,
    Amount = 1
});
```

## См. также

- [Shop](../../Shop/Shop.md) · [InventoryComponent](./InventoryComponent.md) · [Корень модуля Inventory](./README.md)
