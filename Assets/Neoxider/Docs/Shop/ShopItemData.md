## Переопределение валюты

Используйте `Currency Override Save Key`, чтобы ScriptableObject мог выбирать валюту по ключу сохранения `Money.SaveKey`.

- Пустой ключ: используется валюта магазина по умолчанию.
- Непустой ключ: `Shop` ищет кошелёк через `Money.FindBySaveKey(key)` и списывает цену из него.
- Поле GameObject `Currency Override` удалено: `ScriptableObject` не должен хранить ссылки на сценовые кошельки.

# ShopItemData

**Назначение:** `ScriptableObject` для хранения информации о товаре в магазине. С версии **8.5.0** содержит стабильный `Id`, опциональную категорию и опциональный per-item источник валюты. Связь с инвентарём вынесена в отдельный bridge — см. [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md).

## Подключение

1. `Right Click > Create > Neoxider > Shop > Shop Item Data`.
2. Задайте `Id` (или оставьте пустым — авто-fill из `nameItem` в `OnValidate`, как у [QuestConfig](../Quest/QuestConfig.md)).
3. Настройте остальные поля.
4. Добавьте ассет в массив `_shopItemDatas` контроллера [Shop](./Shop.md).

## Основные настройки (Inspector)

| Поле | Описание |
|------|----------|
| `_id` | **Стабильный идентификатор** (string). Используется как ключ владения, экипировки и пр. Автозаполняется из `_nameItem` при сохранении ассета, если пуст. **Не меняйте после первого релиза** — это сломает сейвы. |
| `_isSinglePurchase` | Может ли этот предмет быть куплен только один раз? Если `true`, после первой покупки попадает в `ShopProfileData.OwnedItemIds`. |
| `_nameItem` | Название товара. |
| `_description` | Описание. |
| `_price` | Базовая цена. Runtime-скидки накладываются через `Shop.SetRuntimePrice(id, price)`. |
| `_sprite` | Главная картинка (например, превью). |
| `_icon` | Иконка для маленькой ячейки. |
| `_category` | Опциональная строка-категория (`"weapons"`, `"skins"`, ...). Используется в `Shop.GetItemsInCategory(category)`. Пустая строка = без категории. |
| `_currencyOverrideSaveKey` | Опциональный ключ `Money.SaveKey`. Если задан, покупка списывает валюту из найденного `Money`; если пустой, используется default `moneySpendSource` магазина, затем `Money.I`. |

> Выдача предмета в инвентарь настраивается не здесь, а через [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md) — в его табличке маппингов вы укажете `ShopItemData.Id → InventoryItemData + Amount`.

## Code API

```csharp
ShopItemData data = ...;
data.Id;                  // "sword_basic"
data.price;               // base price
data.Category;            // "weapons"
data.CurrencyOverrideSaveKey;
```

## См. также

- [Shop](./Shop.md) · [ShopBundleData](./ShopBundleData.md) · [ShopItem](./ShopItem.md) · [Корень модуля](../README.md)
- [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md)
