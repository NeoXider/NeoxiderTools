# InventoryComponent

`InventoryComponent` — фасад инвентаря для сцены: No-Code события, API для кода, сохранение через `SaveProvider`.

## Основные возможности

- Добавление/удаление предметов по `itemId` или `InventoryItemData`.
- Ограничения: `Max Unique Items`, `Max Total Items`, `maxStack` из `InventoryDatabase`.
- Сохранение/загрузка (`Save`, `Load`) по `Save Key`.
- События UnityEvent для UI/геймплея.
- Свойства для условий в `NeoCondition`: `TotalItemCount`, `UniqueItemCount`, `SelectedItemCount`, `IsEmpty`.
- Опциональный делегат дропа через отдельный компонент `InventoryDropper`.
- Может работать как singleton или как обычный компонент для нескольких инвентарей (через `Set Instance On Awake` из `Singleton<T>`).

## Ключевые поля

| Поле | Описание |
|------|----------|
| `Database` | База предметов (`InventoryDatabase`) для maxStack и lookup. |
| `Restrict To Database` | Если включено — разрешены только `itemId`, присутствующие в `Database`; если выключено — можно добавлять любой id. |
| `Initial State Data` | SO с начальным наполнением (`InventoryInitialStateData`). |
| `Max Unique Items` | Лимит уникальных предметов (0 = без лимита). |
| `Max Total Items` | Лимит общего количества (0 = без лимита). |
| `Save Key` | Ключ сохранения для SaveProvider. |
| `Load Mode` | Режим комбинирования save и initial state (`UseSaveIfExists`, `MergeSaveWithInitial`, `InitialOnlyIgnoreSave`). |
| `Apply Initial If Result Empty` | Если после загрузки инвентарь пуст, повторно применить initial state. |
| `Selected Item Id` | Вспомогательный id для `SelectedItemCount`. |
| `Dropper` | Подключаемый модуль дропа (`InventoryDropper`). |

## События

| Событие | Аргументы | Когда вызывается |
|---------|-----------|------------------|
| `OnInventoryChanged` | — | Любое изменение инвентаря. |
| `OnItemAdded` | `(itemId, amount)` | Успешное добавление. |
| `OnItemRemoved` | `(itemId, amount)` | Успешное удаление. |
| `OnItemCountChanged` | `(itemId, newCount)` | После изменения количества конкретного itemId. |
| `OnItemBecameZero` | `(itemId)` | Когда количество itemId стало 0. |
| `OnCapacityRejected` | `(itemId, rejectedAmount)` | Часть или весь запрос отклонен лимитами/валидацией id. |
| `OnBeforeLoad` | — | Перед запуском `Load()`. |
| `OnLoaded` | — | После `Load()`. |
| `OnSaved` | — | После `Save()`. |

## Публичный API

- `int AddItemById(int itemId)`
- `int AddItemByIdAmount(int itemId, int amount)`
- `int AddItemData(InventoryItemData itemData, int amount = 1)`
- `int RemoveItemById(int itemId)`
- `int RemoveItemByIdAmount(int itemId, int amount)`
- `int DropSelected(int amount = 1)` — дроп выбранного (или первого при «Drop Next When Empty»).
- `int DropById(int itemId, int amount = 1)` — дроп по id.
- `int DropData(InventoryItemData itemData, int amount = 1)` — дроп по данным предмета.
- `int DropFirst(int amount = 1)` — дроп первого предмета в инвентаре (делегирует в Dropper).
- `int DropLast(int amount = 1)` — дроп последнего предмета в инвентаре (делегирует в Dropper).
- `bool HasItem(int itemId)`
- `bool HasItemAmount(int itemId, int amount)`
- `int GetCount(int itemId)`
- `List<InventoryEntry> GetSnapshotEntries()`
- `int GetFirstItemId()` — первый предмет с count &gt; 0 по порядку снимка; -1 если пусто.
- `int GetLastItemId()` — последний предмет с count &gt; 0 по порядку снимка; -1 если пусто.
- `void ClearInventory()`
- `void Save()`
- `void Load()`

## Пример

```csharp
[SerializeField] private InventoryComponent inventory;

private void Buy(int itemId, int price, int currencyItemId)
{
    if (!inventory.HasItemAmount(currencyItemId, price))
        return;

    inventory.RemoveItemByIdAmount(currencyItemId, price);
    inventory.AddItemById(itemId);
}
```
