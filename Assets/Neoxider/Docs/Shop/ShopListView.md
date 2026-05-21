# ShopListView

**Назначение:** опциональная динамическая UI-вьюшка для `Shop`.

Используйте один `Shop` для логики покупок, сейва, валюты, владения и событий инвентаря. Добавляйте один или несколько `ShopListView`, если нужны отдельные категории, вкладки, фильтры или витрина, которую полностью создаёт сама вьюшка.

## Типовая настройка

1. Добавьте `Shop` на сцену и назначьте все `ShopItemData`.
2. Выключите `Auto Spawn Items` у `Shop`, если сам магазин не должен создавать полный список.
3. Добавьте `ShopListView` на UI-корень списка.
4. Назначьте тот же `Shop`, `Item Prefab` и `Items Root`.
5. Вызывайте `ShowCategory(string)` из кнопок категорий или `ShowAll()` для полного списка.

## Основные поля (Inspector)

| Поле | Назначение |
|------|------------|
| `Shop` | Источник данных и покупок. Если пусто, ищется в родителях/сцене. |
| `Category` | Текущий фильтр категории. |
| `Show All When Category Empty` | Пустая категория показывает все товары, а не только товары без категории. |
| `Include Owned` / `Include Unowned` | Базовые фильтры по владению. |
| `Hide Owned Single Purchase Items` | Скрывает уже купленные одноразовые товары. |
| `Item Prefab` | Префаб `ShopItem` для создания недостающих ячеек. |
| `Views` | Опционально подготовленные вручную ячейки, которые переиспользуются перед спавном новых. |
| `Button Action` | Что делает `ShopItem.buttonBuy`: покупка, превью или выбор. |

## NoCode API

- `ShowAll()`
- `ShowCategory(string category)`
- `SetCategory(string category)`
- `SetIncludeOwned(bool)`
- `SetIncludeUnowned(bool)`
- `SetButtonAction(ShopListButtonAction)`
- `SetItemPrefab(ShopItem)`
- `SetItemsRoot(Transform)`
- `SetShowAllWhenCategoryEmpty(bool)`
- `SetHideOwnedSinglePurchaseItems(bool)`
- `Refresh()`

Для большинства проектов удобнее использовать один `Shop` и несколько `ShopListView`, а не отдельный `Shop` на каждую категорию.

## Кнопки категорий

Если в UnityEvent неудобно передавать строковые параметры, добавьте `ShopCategoryButton` на UI `Button`.

1. Назначьте `Target View`.
2. Укажите `Category` или включите `Show All`.
3. Оставьте `Auto Bind Button` включённым.

В рантайме кнопка вызовет `ShopListView.ShowCategory(...)` или `ShopListView.ShowAll()`.
