# Shop

**Назначение:** главный контроллер внутриигрового магазина. Отвечает за:

- генерацию UI товаров на основе `ShopItemData`;
- обработку покупок одиночных товаров и бандлов (`ShopBundleData`);
- сохранение прогресса: купленные предметы, экипировка, runtime-скидки через единый JSON `ShopProfileData`;
- управление выбранным предметом;
- мультивалюту через per-item/per-bundle `IMoneySpend` override.

Интеграция с инвентарем вынесена в [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md) из `Neo.Tools.Inventory`. Bridge слушает `Shop.OnPurchasedId` и выдает `InventoryItemData` по таблице маппингов. `Neo.Shop.asmdef` намеренно не зависит от `Neo.Tools.Inventory`, чтобы не создавать asmdef-цикл.

С версии **8.5.0** идентичность предмета - стабильный `string Id` из `ShopItemData`, а не индекс массива. Старые ключи `Shop0/Shop1/.../ShopEquipped` больше не читаются.

С версии **8.5.1**, если в каталоге остались ассеты с пустым `Id`, `Shop` в `Awake` подставляет уникальные id до `LoadProfile()`. Это защищает `ShopListView` от ситуации, когда все ячейки показывают одно состояние.

## Динамические Вьюшки

`Shop` может работать только как контроллер каталога и покупок, а внешние вьюшки могут полностью управлять видимыми ячейками.

- Выключите `Auto Spawn Items`, если список рисует `ShopListView`.
- Используйте `ShopListView` для создания/переиспользования `ShopItem` и фильтрации по `ShopItemData.Category`.
- Используйте `ShopCategoryButton` для вкладок категорий, настроенных через Inspector.
- Runtime-хелперы каталога: `SetItems(...)`, `SetBundles(...)`, `SetMoneySpendSource(...)`, `SetAutoSpawnItems(...)`.
- Хелперы/события обновления: `RefreshVisuals()`, `OnShopChanged`, `GetCategories(...)`.

Один `Shop` остается источником правды для сейва, владения, цен, валют, бандлов и событий inventory bridge.

## Валюта По Ключу

`ShopItemData` и `ShopBundleData` могут выбирать валюту по `Money.SaveKey`.

- Оставьте `Currency Override Save Key` пустым, чтобы использовать валюту магазина по умолчанию (`moneySpendSource`, затем `Money.I`).
- Укажите ключ, например `Gems`, чтобы списывать из `Money`, у которого `SaveKey == "Gems"`.
- Старый GameObject override поддерживается как fallback для сценовых настроек, но для ScriptableObject рекомендуется ключ сохранения.

## Подключение

1. Добавьте `Add Component > Neoxider > Shop > Shop` на пустой объект.
2. Заполните `_shopItemDatas` ассетами `ShopItemData`.
3. Опционально заполните `_bundles` ассетами `ShopBundleData`.
4. Используйте `_prefab` + `_container` для автоспавна UI или вручную назначьте готовые `ShopItem` в `_shopItems`.
5. Опционально добавьте [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md) на тот же GameObject для авто-выдачи `InventoryItemData` при покупке.

## Покупочный Поток

| Режим | Поведение |
|-------|-----------|
| `BuyAndEquip` | Купить -> автоматически выбрать. Совместимо со старым `_useSetItem = true`. |
| `BuyOnly` | Только покупка, экипировка не меняется. |
| `EquipOnly` | Без списаний, только смена выбранного предмета. Подходит для косметики и переключателей скинов. |
| `Browse` | Read-only витрина: `Buy()` и `BuyBundle()` ничего не делают, preview работает. |

## Основные Поля

| Поле | Описание |
|------|----------|
| `_purchaseFlow` | Режим покупочного потока. |
| `_shopItemDatas` | Массив `ShopItemData`: цены, иконки, описания и стабильные id. |
| `_bundles` | Опциональный массив `ShopBundleData`. |
| `_shopItemPreview` | UI-предпросмотр выбранного товара. |
| `_shopItems` | Ячейки магазина, найденные в дочерних объектах или созданные из `_prefab`. |
| `_container`, `_prefab` | Контейнер и префаб для автоспавна. |
| `_keySave` | Единый ключ `SaveProvider` для JSON `ShopProfileData`. Удаление ключа полностью сбрасывает магазин. |
| `moneySpendSource` | Default-объект с `IMoneySpend`. Если `null`, используется `Money.I`; `CurrencyOverrideSaveKey` имеет приоритет. |
| `_autoSubscribe` | Авто-подписка `ShopItem.buttonBuy` на действие покупки. |
| `_changePreviewOnPurchaseFailed` | Менять preview при неудачной покупке. |
| `_propagateSelectionVisual` | Вызывать `ShopItem.Select(bool)` на всех элементах при смене экипировки. |
| `_activateSavedEquipped` | Автовыбор при загрузке (`BuyAndEquip` / `EquipOnly`): сохраненный item или первый элемент каталога. |
| `_prices`, `_keySaveEquipped` | Устарели. Сохранены как serialized-поля для совместимости старых сцен, но игнорируются в runtime. |

## Публичный API

### Typed Asset API (канонический перед v9)

Используйте эти перегрузки, когда gameplay/UI-код уже работает с ассетами каталога. Они не зависят от порядка массива и упрощают удаление int-indexed вызовов в v9.

| Член | Назначение |
|------|------------|
| `Buy(ShopItemData itemData)` | Купить / экипировать по ассету предмета. |
| `BuyBundle(ShopBundleData bundleData)` | Купить бандл по ассету бандла. |
| `Select(ShopItemData itemData)` | Экипировать по ассету; `null` очищает выбор. |
| `ShowPreview(ShopItemData itemData)` | Показать preview по ассету; `null` очищает preview. |
| `IsOwned(ShopItemData itemData)` / `IsBundleOwned(ShopBundleData bundleData)` | Проверка владения по typed-ассету. |
| `GetPrice(ShopItemData itemData)` | Текущая цена с учетом runtime override. |
| `SetRuntimePrice(ShopItemData itemData, float price)` / `ClearRuntimePrice(ShopItemData itemData)` | Runtime-скидки по typed-ассету предмета. |

### String Id API

Используйте этот слой, когда код хранит или получает id, а не ассеты.

| Член | Назначение |
|------|------------|
| `EquippedId : string` | Текущий выбранный предмет. |
| `PreviewIdString : string` | Предмет в preview-слоте. |
| `Buy(string itemId)` | Купить / экипировать по id с учетом `_purchaseFlow`. |
| `BuyBundle(string bundleId)` | Купить бандл по id. |
| `Select(string itemId)` | Экипировать без покупки; `""` очищает выбор. |
| `ShowPreview(string itemId)` | Установить preview. |
| `IsOwned(string itemId)` / `IsBundleOwned(string bundleId)` | Проверка владения. |
| `GetPrice(string itemId)` | Текущая цена с учетом runtime override. |
| `SetRuntimePrice(string itemId, float price)` / `ClearRuntimePrice(string itemId)` | Runtime-скидки / временные price overrides. |
| `GetItemsInCategory(string category)` | Фильтр по `ShopItemData.Category`. |
| `ShopItemDatas`, `Bundles` | Доступ к каталогам. |

### Legacy Int API (`[Obsolete]`, удаляется в v9)

| Член | Поведение |
|------|-----------|
| `Id : int` | Прокси: `IndexOfItemDataById(EquippedId)` / `Select(items[i].Id)`. |
| `PreviewId : int` | `IndexOfItemDataById(PreviewIdString)`. |
| `Buy()` | Покупает `PreviewIdString`, fallback на `EquippedId`. |
| `Buy(int id)` | Резолвит `_shopItemDatas[id].Id` -> `Buy(string)`. |
| `ShowPreview(int id)` | Резолвит `_shopItemDatas[id].Id` -> `ShowPreview(string)`. |
| `Prices : int[]` | Устаревший массив; в runtime игнорируется. |

## События

| Событие | Аргумент | Когда |
|---------|----------|-------|
| `OnSelect` | `int` index | Экипировка; legacy. |
| `OnSelectId` | `string` id | Экипировка. |
| `OnPurchased` | `int` index | Успешная покупка; legacy. |
| `OnPurchasedId` | `string` id | Успешная покупка предмета. |
| `OnPurchaseFailed` | `int` index | Не хватило денег; legacy. |
| `OnPurchaseFailedId` | `string` id | Не хватило денег для предмета или бандла. |
| `OnPurchasedBundle` | `ShopBundleData` | Бандл куплен после выдачи всех items. |
| `OnLoad` | нет | Срабатывает после `Start()`. |

Inventory grant события (`OnGranted` с `(InventoryItemData, int)`) живут на [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md), а не на `Shop`.

## Совместимость

- Старые serialized-поля (`_prices`, `_keySaveEquipped`, `_useSetItem` -> `_propagateSelectionVisual` через `FormerlySerializedAs`, `_activateSavedEquipped`) сохранены, чтобы сцены не теряли Inspector-данные.
- Save format - hard break: legacy `Shop0/Shop1` ключи не читаются.
- UnityEvent-подписки на `OnSelect<int>` / `OnPurchased<int>` продолжают работать: `Buy(string)` поднимает и int, и string события.

## Тесты

- `Assets/Neoxider/Tests/Play/ShopPurchasePlayModeTests.cs` - PlayMode-покрытие покупок, бандлов, потоков, multi-currency, inventory, `ShopListView` и typed asset API.
- `Assets/Neoxider/Tests/Edit/ShopProfileDataTests.cs` - EditMode-проверки профиля, JSON, sanitize и runtime price overrides.
- `Assets/Neoxider/Tests/Edit/Save/ShopManagerTests.cs` - legacy Shop/Save покрытие.

## См. Также

- [Корень модуля](../README.md)
- [ShopItemData](./ShopItemData.md)
- [ShopBundleData](./ShopBundleData.md)
- [Money](./Money.md)
- [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md)
