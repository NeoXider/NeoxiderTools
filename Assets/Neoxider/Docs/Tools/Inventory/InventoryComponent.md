# InventoryComponent

`InventoryComponent` — фасад инвентаря для сцены: No-Code события, API для кода, сохранение через `SaveProvider`.

## Основные возможности

- Добавление/удаление предметов по `itemId` или `InventoryItemData`.
- Ограничения: `Max Unique Items`, `Max Total Items`, `maxStack` из `InventoryDatabase`.
- Сохранение/загрузка: при включённом **Auto Save** (по умолчанию) каждое изменение инвентаря автоматически записывается в SaveProvider по `Save Key`. Явно вызывать `Save()` не нужно. Запись на диск — по необходимости через `SaveProvider.Save()` (например при выходе или паузе).
- События UnityEvent для UI/геймплея. При `ClearInventory()` вызываются те же события по каждому предмету (`OnItemRemoved`, `OnItemCountChanged`, `OnItemBecameZero`), затем `OnInventoryChanged`.
- Порядок слотов: снимок и `GetFirstItemId()` / `GetLastItemId()` следуют порядку добавления предметов.
- Свойства для условий в `NeoCondition`: `TotalItemCount`, `UniqueItemCount`, `SelectedItemCount` (при заданном `SelectedItemId`), `IsEmpty`. Для проверки по конкретному id: `GetCount(itemId)` (например `inventory.GetCount(5) >= 3`).
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
| `Auto Save` | По умолчанию включено: при Add/Remove/Clear состояние пишется в SaveProvider по Save Key. |
| `Save Key` | Ключ сохранения для SaveProvider. Запись на диск — при вызове `SaveProvider.Save()` (например при выходе). |
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
- `int GetCount(int itemId)` — количество предмета; удобно для условий вида `inventory.GetCount(5) >= 3`.
- `bool TryConsume(int itemId, int amount)` — удаляет ровно `amount`, если хватает; возвращает `true` только при успехе. Удобно для трат: `if (inventory.TryConsume(currencyId, price)) { ... }`.
- `int GetNonEmptySlotCount()` — число слотов с count &gt; 0 (для системы «руки» / Selector).
- `int GetItemIdAtSlotIndex(int slotIndex)` — itemId по индексу слота (0 .. GetNonEmptySlotCount()-1); -1 если вне диапазона.
- `List<InventoryEntry> GetSnapshotEntries()` — снимок в порядке добавления.
- `int GetFirstItemId()` — первый предмет с count &gt; 0 по порядку снимка; -1 если пусто.
- `int GetLastItemId()` — последний предмет с count &gt; 0 по порядку снимка; -1 если пусто.
- `void ClearInventory()` — очистка; перед очисткой вызываются события по каждому предмету.
- `void Save()` — принудительно записывает инвентарь в SaveProvider по Save Key (при Auto Save вызывается автоматически).
- `void Load()`

## Примеры

```csharp
[SerializeField] private InventoryComponent inventory;

private void Buy(int itemId, int price, int currencyItemId)
{
    if (!inventory.TryConsume(currencyItemId, price))
        return;

    inventory.AddItemById(itemId);
}

// Условие по количеству предмета
if (inventory.GetCount(5) >= 3)
    DoSomething();
```
