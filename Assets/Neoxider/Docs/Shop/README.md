# Магазин (Shop)

**Что это:** модуль внутриигрового магазина и экономики: контроллер Shop, предметы и бандлы, кошельки, кнопки покупки. Скрипты в `Scripts/Shop/`.

С версии **8.5.0** идентичность предмета — стабильный `string Id` из `ShopItemData`; сейв-формат жёстко поменялся (`ShopProfileData` JSON); добавлены бандлы, категории, multi-currency и **опциональная интеграция с инвентарём**. См. [Shop.md → Совместимость со старыми сценами](./Shop.md#совместимость-со-старыми-сценами).

**Навигация:** [← К Docs](../README.md) · оглавление — список внизу страницы

---

## Компоненты

### Логика магазина
- **Shop**: контроллер. Поддерживает покупку отдельных предметов, бандлов, категорий, multi-currency.
- **ShopItem**: визуальное представление одного товара / бандла (`Visual(ShopItemData, ...)` и `Visual(ShopBundleData, ...)`).
- **ShopItemData**: ScriptableObject товара. Стабильный `Id`, опциональные `Category` и `CurrencyOverrideSaveKey`.
- **ShopBundleData**: ScriptableObject бандла. Цена за весь набор; покупка выдаёт все включённые предметы.
- **ShopPurchaseFlow**: enum режимов магазина (`BuyAndEquip`, `BuyOnly`, `EquipOnly`, `Browse`).
- **ShopProfileData**: сериализуемый JSON-снимок состояния (owned items, owned bundles, runtime price overrides, equipped id). Хранится одним ключом в `SaveProvider`.
- **ShopRuntimePriceEntry**: запись runtime-скидки / временной цены.
- **ButtonPrice**: универсальная кнопка с поддержкой состояний (купить, выбрать, выбрано).

### Система денег
- **Money**: управление балансом. Один синглтон `Money.I` для основной валюты или несколько экземпляров для альт-валют (энергия, гемы).
- **TextMoney**: отображение суммы; опциональный `Money Source`.
- **IMoneySpend / IMoneyAdd**: интерфейсы операций с деньгами. Любой объект с `IMoneySpend` можно передать как default-валюту магазину (`moneySpendSource`), как per-item / per-bundle override, или использовать как полностью альтернативный кошелёк.

## Связь с инвентарём (опциональная)

Если хотите, чтобы покупка автоматически добавляла предметы в [InventoryComponent](../Tools/Inventory/README.md), используйте [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md):

1. Поставьте `InventoryComponent` в сцену.
2. Добавьте `ShopInventoryGrantBridge` на ту же GO, что и `Shop` (или дочернюю).
3. В табличке `Mappings` укажите `ShopItemData.Id → InventoryItemData + Amount`.
4. На покупке (одиночной или через бандл) bridge поймает `Shop.OnPurchasedId` и вызовет `inventory.AddItemData(...)`, подняв `OnGranted(data, amount)`.

Bridge живёт в `Neo.Tools.Inventory` (а не в `Neo.Shop`) — это сознательное решение, чтобы Shop не тянул `Neo.Tools.Inventory` в свою сборку и не создавал asmdef-цикл.

## Оглавление
- [Shop](./Shop.md)
- [ShopItem](./ShopItem.md)
- [ShopItemData](./ShopItemData.md)
- [ShopBundleData](./ShopBundleData.md)
- [ButtonPrice](./ButtonPrice.md)
- [Money](./Money.md)
- [TextMoney](./TextMoney.md)
- [Интерфейсы (IMoneySpend, IMoneyAdd)](./InterfaceMoney.md)
## Динамические вьюшки магазина

- [ShopListView](./ShopListView.md) - опциональная вьюшка категорий/фильтров, которая создаёт и переиспользует ячейки `ShopItem`.
- [ShopCategoryButton](./ShopCategoryButton.md) - NoCode-кнопка категории для `ShopListView`.

Рекомендуемая настройка для магазинов с категориями: один `Shop` отвечает за каталог, сейв, валюту и покупки; `Auto Spawn Items` у него выключен; один или несколько `ShopListView` управляют видимыми списками.

## Тесты

Shop покрыт EditMode и PlayMode тестами:

- `Assets/Tests/Play/ShopPurchasePlayModeTests.cs` — покупки, бандлы, runtime-цены, режимы `Browse` / `EquipOnly`, интеграция с инвентарём, multi-currency и `ShopListView`.
- `Assets/Tests/Edit/ShopProfileDataTests.cs` — JSON round-trip, sanitize/dedupe, runtime price overrides и clone.
- `Assets/Tests/Edit/Save/ShopManagerTests.cs` — legacy-проверки Shop/Save.

Запуск: Unity Test Runner → EditMode / PlayMode.
