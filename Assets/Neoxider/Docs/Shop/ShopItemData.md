## Переопределение валюты

Используйте `Currency Override Save Key`, чтобы ScriptableObject мог выбирать валюту по ключу сохранения `Money.SaveKey`.

- Пустой ключ: используется валюта магазина по умолчанию.
- Непустой ключ: `Shop` ищет кошелёк через `Money.FindBySaveKey(key)` и списывает цену из него.
- Поле GameObject `Currency Override` удалено: `ScriptableObject` не должен хранить ссылки на сценовые кошельки.

# ShopItemData

**Назначение:** `ScriptableObject` для хранения информации о товаре в магазине. С версии **8.5.0** содержит стабильный `Id`, опциональную категорию и опциональный per-item источник валюты. Связь с инвентарём вынесена в отдельный bridge — см. [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md).

## Подключение

1. `Right Click > Create > Neoxider > Shop > Shop Item Data`.
2. Задайте `Id` (или оставьте пустым — см. **Автозаполнение Id** ниже).
3. Настройте остальные поля.
4. Добавьте ассет в массив `_shopItemDatas` контроллера [Shop](./Shop.md).

## Основные настройки (Inspector)

| Поле | Описание |
|------|----------|
| `_id` | **Стабильный идентификатор** (string). Ключ владения, экипировки и поиска в `Shop`. **Не меняйте после релиза** — иначе сейвы перестанут совпадать. |
| `_isSinglePurchase` | Может ли этот предмет быть куплен только один раз? Если `true`, после первой покупки попадает в `ShopProfileData.OwnedItemIds`. |
| `_nameItem` | Название товара. |
| `_description` | Описание. |
| `_price` | Базовая цена. Runtime-скидки накладываются через `Shop.SetRuntimePrice(id, price)`. |
| `_sprite` | Главная картинка (например, превью). |
| `_icon` | Иконка для маленькой ячейки. |
| `_category` | Опциональная строка-категория (`"weapons"`, `"skins"`, ...). Используется в `Shop.GetItemsInCategory(category)`. Пустая строка = без категории. |
| `_currencyOverrideSaveKey` | Опциональный ключ `Money.SaveKey`. Если задан, покупка списывает валюту из найденного `Money`; если пустой, используется default `moneySpendSource` магазина, затем `Money.I`. |

> Выдача предмета в инвентарь настраивается не здесь, а через [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md) — в его табличке маппингов вы укажете `ShopItemData.Id → InventoryItemData + Amount`.

## Автозаполнение Id

| Когда | Поведение |
|-------|-----------|
| **Редактор** (`OnValidate`) | Пустой `_id` → из `_nameItem` (пробелы → `_`), как у [QuestConfig](../Quest/QuestConfig.md). |
| **Рантайм** (с **8.5.1**) | [Shop](./Shop.md) в `Awake` **до** загрузки сейва вызывает `EnsureMissingItemIds()`: `nameItem` → имя файла ассета → `{база}_{индекс в массиве Shop}`. То же при `SetItems(...)`. Запись через `AssignIdIfEmpty` — только пока `_id` пустой. |

Для продакшена лучше задать уникальный `Id` вручную в Inspector: рантайм-подстановка живёт в памяти сессии и не заменяет явную настройку ассетов перед релизом.

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
