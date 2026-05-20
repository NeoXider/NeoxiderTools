## Динамические вьюшки

`Shop` может работать только как контроллер каталога и покупок, а внешние вьюшки могут полностью управлять видимыми ячейками.

- Выключите `Auto Spawn Items`, если список рисует `ShopListView`.
- Используйте `ShopListView` для создания/переиспользования `ShopItem` и фильтрации по `ShopItemData.Category`.
- Используйте `ShopCategoryButton` для вкладок категорий, настроенных только через Inspector.
- Runtime-хелперы каталога: `SetItems(...)`, `SetBundles(...)`, `SetMoneySpendSource(...)`, `SetAutoSpawnItems(...)`.
- Хелперы/события обновления: `RefreshVisuals()`, `OnShopChanged`, `GetCategories(...)`.

Так один `Shop` остаётся источником правды для сейва, владения, цен, валют, бандлов и событий inventory bridge.

## Переопределение валюты по ключу сохранения

`ShopItemData` и `ShopBundleData` могут выбирать валюту по `Money.SaveKey`.

- Оставьте `Currency Override Save Key` пустым, чтобы использовать валюту магазина по умолчанию (`moneySpendSource`, затем `Money.I`).
- Укажите ключ, например `Gems`, чтобы списывать из `Money`, у которого `SaveKey == "Gems"`.
- Старый GameObject override всё ещё поддерживается как fallback для сценовых настроек, но для ScriptableObject рекомендуется ключ сохранения.

# Shop

**Назначение:** Главный контроллер внутриигрового магазина. Отвечает за:

- генерацию UI товаров на основе `ShopItemData`;
- обработку покупок одиночных товаров и **бандлов** (`ShopBundleData`);
- сохранение прогресса (купленные предметы, экипировка, runtime-скидки) в едином JSON-блобе через `SaveProvider`;
- управление выбранным (экипированным) предметом;
- мультивалюту: на уровне предмета и/или бандла можно переопределить источник `IMoneySpend`.

**Интеграция с инвентарём** вынесена в отдельный bridge — [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md) (живёт в `Neo.Tools.Inventory`). Bridge слушает `Shop.OnPurchasedId` и грантит `InventoryItemData` по табличке маппингов. Это сознательное решение: `Neo.Shop.asmdef` не зависит от `Neo.Tools.Inventory` (избегаем asmdef-цикла).

С версии **8.5.0** идентичность предмета — стабильный `string Id` из `ShopItemData` (а не индекс массива). Сейв-формат жёстко поменялся: старые ключи `Shop0/Shop1/.../ShopEquipped` больше не читаются (см. CHANGELOG `## [8.5.0] Breaking`).

С **8.5.1**, если в каталоге остались ассеты с пустым `Id`, `Shop` в `Awake` подставляет уникальные id **до** `LoadProfile()` (подробнее — [ShopItemData → Автозаполнение Id](./ShopItemData.md#автозаполнение-id)). Это устраняет ситуацию, когда все ячейки `ShopListView` показывают одно состояние (например **USED** без цены).

## Подключение

1. `Add Component > Neoxider > Shop > Shop` на пустой объект.
2. В `_shopItemDatas` — массив `ShopItemData` (см. [ShopItemData](./ShopItemData.md)).
3. (Опционально) `_bundles` — массив `ShopBundleData` (см. [ShopBundleData](./ShopBundleData.md)).
4. `_prefab` + `_container` если хотите автоспавн UI; иначе руками положите готовые `ShopItem` в `_shopItems`.
5. (Опционально) Добавьте [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md) на ту же GO для авто-выдачи `InventoryItemData` при покупке.

## Покупочный поток (`ShopPurchaseFlow`)

| Режим | Поведение |
|-------|-----------|
| `BuyAndEquip` (по умолчанию) | Купить → авто-выбрать. Поведение, совместимое со старым `_useSetItem = true`. |
| `BuyOnly` | Только покупка, экипировка не меняется. |
| `EquipOnly` | Никаких списаний — только смена выбранного предмета (косметика, переключатель скинов). |
| `Browse` | Read-only витрина: `Buy()` и `BuyBundle()` — no-op, превью работает. |

## Основные настройки (Inspector)

| Поле | Описание |
|------|----------|
| `_purchaseFlow` | Режим покупочного потока, см. таблицу выше. |
| `_shopItemDatas` | Массив `ShopItemData`. Источник цен, иконок, описаний и стабильных id. |
| `_bundles` | Опциональный массив `ShopBundleData`. |
| `_shopItemPreview` | UI-предпросмотр выбранного товара. |
| `_shopItems` | Заполняется автоматически дочерними `ShopItem` + автоспавном из `_prefab`. |
| `_container`, `_prefab` | Контейнер и префаб для автоспавна. |
| `_keySave` | Единый ключ `SaveProvider` для JSON `ShopProfileData`. Удаление этого ключа полностью сбрасывает магазин. |
| `moneySpendSource` | Default-объект с `IMoneySpend`. Если `null`, используется `Money.I`. Item/Bundle `CurrencyOverrideSaveKey` имеют приоритет. |
| `_autoSubscribe` | Авто-подписка `ShopItem.buttonBuy` на `Buy(index)`. |
| `_changePreviewOnPurchaseFailed` | Менять превью при неудачной покупке. |
| `_propagateSelectionVisual` (бывш. `_useSetItem`) | Вызывать `ShopItem.Select(bool)` на всех элементах при смене экипировки. |
| `_activateSavedEquipped` | Автовыбор при загрузке (только `BuyAndEquip` / `EquipOnly`): сохранённый предмет, а если сейва нет или id устарел — **первый** в `_shopItemDatas`. |
| `_prices`, `_keySaveEquipped` | **Устарели.** Сохранены как `[SerializeField]` для совместимости старых сцен, но игнорируются в рантайме. |

## Публичный API

### String-based (рекомендуется с 8.5.0)

| Член | Назначение |
|------|-----------|
| `EquippedId : string` | Текущий выбранный предмет. |
| `PreviewIdString : string` | Предмет в превью-слоте. |
| `Buy(string itemId)` | Купить / экипировать по id. Уважает `_purchaseFlow`. |
| `BuyBundle(string bundleId)` | Купить бандл. Все items бандла попадают в owned; `OnPurchasedId` срабатывает для каждого предмета (bridge подхватит выдачу в инвентарь). |
| `Select(string itemId)` | Экипировать без покупки. Передайте `""` для очистки. |
| `ShowPreview(string itemId)` | Подсветить предмет в превью. |
| `IsOwned(string itemId)` / `IsBundleOwned(string bundleId)` | Проверка владения. |
| `GetPrice(string itemId)` | Текущая цена (с учётом runtime-override). |
| `SetRuntimePrice(string itemId, float price)` / `ClearRuntimePrice(string itemId)` | Скидки / временные оверрайды цены. |
| `GetItemsInCategory(string category)` | Фильтр по `ShopItemData.Category`. |
| `ShopItemDatas`, `Bundles` | Доступ к каталогам. |

### Legacy int-API (`[Obsolete]`, удалится в v9)

| Член | Поведение |
|------|-----------|
| `Id : int` | Прокси: `IndexOfItemDataById(EquippedId)` / `Select(items[i].Id)`. |
| `PreviewId : int` | `IndexOfItemDataById(PreviewIdString)`. |
| `Buy()` | Покупает `PreviewIdString` (fallback на `EquippedId`). |
| `Buy(int id)` | Резолвит `_shopItemDatas[id].Id` → `Buy(string)`. |
| `ShowPreview(int id)` | Резолвит `_shopItemDatas[id].Id` → `ShowPreview(string)`. |
| `Prices : int[]` | Возвращает устаревший массив; в рантайме игнорируется. |

## События

| Событие | Параметр | Когда срабатывает |
|---------|----------|-------------------|
| `OnSelect` | `int` индекс | Экипировка — legacy. |
| `OnSelectId` | `string` id | Экипировка. |
| `OnPurchased` | `int` индекс | Успешная покупка — legacy. |
| `OnPurchasedId` | `string` id | Успешная покупка предмета. |
| `OnPurchaseFailed` | `int` индекс | Не хватило денег — legacy. |
| `OnPurchaseFailedId` | `string` id | Не хватило денег (для предмета или бандла). |
| `OnPurchasedBundle` | `ShopBundleData` | Бандл куплен (после выдачи всех items). |
| `OnLoad` | — | Готов после `Start()`. |

> Inventory grant события (`OnGranted` с `(InventoryItemData, int)`) живут на [`ShopInventoryGrantBridge`](../Tools/Inventory/ShopInventoryGrantBridge.md), а не на Shop.

## Совместимость со старыми сценами

- Старые поля `_prices`, `_keySaveEquipped`, `_useSetItem` (теперь `_propagateSelectionVisual` через `FormerlySerializedAs`), `_activateSavedEquipped` остаются сериализуемыми — сцены, открытые в Unity, не теряют данные инспектора.
- **Сейв-формат жёстко поменялся** (wipe): сохранённые покупки из старого формата `Shop0/Shop1` НЕ читаются. При первом запуске магазин стартует с пустым `ShopProfileData`.
- UnityEvent-подписки на `OnSelect<int>` / `OnPurchased<int>` продолжают работать — `Buy(string)` поднимает оба варианта событий (`int` + `string`).

## Тесты

- `Assets/Tests/Play/ShopPurchasePlayModeTests.cs` — основной PlayMode набор для покупок, бандлов, режимов магазина, multi-currency, инвентаря и `ShopListView`.
- `Assets/Tests/Edit/ShopProfileDataTests.cs` — EditMode проверки профиля, JSON, sanitize и runtime price overrides.
- `Assets/Tests/Edit/Save/ShopManagerTests.cs` — legacy-проверки Shop/Save.

## См. также

- [Корень модуля](../README.md) · [ShopItemData](./ShopItemData.md) · [ShopBundleData](./ShopBundleData.md) · [Money](./Money.md) · [ShopInventoryGrantBridge](../Tools/Inventory/ShopInventoryGrantBridge.md) · [Tools/Inventory](../Tools/Inventory/README.md)
