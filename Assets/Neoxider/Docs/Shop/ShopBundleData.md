## Переопределение валюты

Используйте `Currency Override Save Key`, чтобы бандл мог списывать валюту по ключу сохранения `Money.SaveKey`.

- Пустой ключ: используется валюта магазина по умолчанию.
- Непустой ключ: `Shop` ищет кошелёк через `Money.FindBySaveKey(key)` и списывает цену из него.
- Поле GameObject `Currency Override` удалено: бандл — это `ScriptableObject`, поэтому он выбирает валюту только по ключу сохранения.

# ShopBundleData

**Назначение:** `ScriptableObject` для **бандла** — набора `ShopItemData`, продающегося за одну цену. На успешной покупке Shop добавляет все предметы бандла в `ShopProfileData.OwnedItemIds` и (опционально) выдаёт каждому соответствующий `InventoryItemData` в подключённый инвентарь.

Доступно с версии **8.5.0**.

## Подключение

1. `Right Click > Create > Neoxider > Shop > Shop Bundle Data`.
2. Задайте `Id` (или оставьте пустым — авто-fill из `nameBundle`).
3. Заполните `_items` — массив `ShopItemData`, которые игрок получит за покупку.
4. Назначьте `_bundlePrice`.
5. Добавьте ассет в массив `_bundles` контроллера [Shop](./Shop.md).

## Основные настройки (Inspector)

| Поле | Описание |
|------|----------|
| `_id` | Стабильный идентификатор бандла. Авто-fill из `_nameBundle`. |
| `_nameBundle` / `_description` | Текст для UI. |
| `_sprite` / `_icon` | Превью и иконка. |
| `_bundlePrice` | Цена бандла. `0` = бандл бесплатный. |
| `_isSinglePurchase` | Если `true`, бандл попадает в `OwnedBundleIds` и больше не покупается. |
| `_items` | Массив `ShopItemData` — что игрок получит. Каждый вложенный предмет может быть привязан к инвентарю через [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md). |
| `_currencyOverrideSaveKey` | Опциональный ключ `Money.SaveKey`. Если задан, бандл списывает валюту из найденного `Money`; если пустой, используется default `moneySpendSource` магазина, затем `Money.I`. |

## Поведение покупки

`Shop.BuyBundle(id)`:

1. Если режим `_purchaseFlow` = `Browse` или `EquipOnly` — игнорируется.
2. Если бандл `isSinglePurchase` и уже в `OwnedBundleIds` — игнорируется.
3. Списание `_bundlePrice` через резолвнутый `IMoneySpend` (`_currencyOverrideSaveKey` приоритетнее default).
4. Для каждого `ShopItemData` из `_items`: если `isSinglePurchase` — добавление в `OwnedItemIds`; поднимается `OnPurchasedId(item.Id)`.
5. После выдачи всех предметов — `OnPurchasedBundle(bundle)`.
6. Если на сцене есть [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md) с маппингом для соответствующих `ShopItemData.Id`, он поймает каждое `OnPurchasedId` и выдаст инвентарю настроенные предметы.

## Code API

```csharp
ShopBundleData bundle = ...;
bundle.Id;
bundle.price;
bundle.isSinglePurchase;
foreach (var item in bundle.Items) { ... }
bundle.CurrencyOverrideSaveKey;
```

## См. также

- [Shop](./Shop.md) · [ShopItemData](./ShopItemData.md) · [Корень модуля](../README.md)
