# InventoryComponent

**Что это:** `MonoBehaviour`-фасад инвентаря в сцене: режимы `Aggregated` / `Slot Grid`, Add/Remove/TryConsume, сохранение в `SaveProvider` по одному ключу на контейнер, UnityEvents, опциональный `InventoryDropper`. Реализация: `Scripts/Tools/Inventory/Runtime/InventoryComponent.cs`, пространство имён `Neo.Tools`.

**Как использовать:**
1. Добавьте компонент на объект сцены (или используйте singleton через `Set Instance On Awake` у базового `Singleton<T>`).
2. Назначьте `Inventory Database`, при необходимости включите `Restrict To Database`.
3. Выберите `Storage Mode`: `Aggregated` для стека по типу или `Slot Grid` для фиксированной сетки слотов; при `Slot Grid` задайте `Slot Count`.
4. Задайте уникальный `Save Key` на каждый контейнер (хотбар, рюкзак, сундук).
5. Настройте `Load Mode` и при необходимости `Initial State Data` / список `Initial Entries`.
6. Подпишите UI на `OnInventoryChanged`, `OnItemAdded` и др.; для дропа назначьте `Dropper` (`InventoryDropper`).
7. Для UI сетки слотов используйте `InventorySlotGridView` (только при `Slot Grid`).

---

## Назначение

Один компонент описывает **один контейнер**. Несколько `InventoryComponent` в сцене — отдельные инвентари с отдельными ключами сохранения. Внутри используется чистый C# backend: `AggregatedInventory` или `SlotGridInventory`.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Database** | База `InventoryDatabase` для max stack и `GetItemData`. |
| **Restrict To Database** | Разрешать только `itemId` из базы. |
| **Initial Entries** | Список начальных предметов, если нет SO. |
| **Initial State Data** | `InventoryInitialStateData` с начальным наполнением. |
| **Storage Mode** | `Aggregated` или `Slot Grid`. |
| **Slot Count** | Число физических слотов в режиме сетки. |
| **Max Unique Items** / **Max Total Items** | Лимиты; 0 = без лимита. |
| **Auto Load** | Вызывать загрузку при инициализации. |
| **Auto Save** | После мутаций писать JSON в `SaveProvider` по `Save Key`. |
| **Save Key** | Ключ одного blob сохранения контейнера. |
| **Invoke Events On Load** | После `Load()` вызвать `OnInventoryChanged`. |
| **Load Mode** | `UseSaveIfExists`, `MergeSaveWithInitial`, `InitialOnlyIgnoreSave`. |
| **Apply Initial If Result Empty** | Если после загрузки пусто — снова применить initial. |
| **Selected Item Id** | Id для свойства `SelectedItemCount` (NeoCondition). |
| **Dropper** | `InventoryDropper` для методов `Drop*`. |

## События

| Событие | Аргументы | Когда |
|---------|-----------|--------|
| `OnInventoryChanged` | — | Любое изменение содержимого. |
| `OnItemAdded` | `(itemId, amount)` | Успешное добавление. |
| `OnItemRemoved` | `(itemId, amount)` | Успешное удаление. |
| `OnItemCountChanged` | `(itemId, newCount)` | Новое суммарное количество по id. |
| `OnItemBecameZero` | `(itemId)` | Количество стало 0. |
| `OnCapacityRejected` | `(itemId, rejectedAmount)` | Часть запроса отклонена лимитами/валидацией. |
| `OnBeforeLoad` | — | Начало `Load()`. |
| `OnLoaded` | — | Конец `Load()`. |
| `OnSaved` | — | После записи в `SaveProvider`. |

## API (кратко)

См. IntelliSense (`///` в коде). Основное: `AddItemByIdAmount`, `AddItemInstance`, `TryConsume`, `GetCount`, снимки `GetSnapshotRecords` / `GetSlot`, операции слотов `TrySetSlot`, `MoveSlot`, `SwapSlots`, `Save` / `Load`.

## Instance-based предметы

У `InventoryItemData` включите **Supports Instance State**. Такие предметы хранятся как `InventoryItemInstance` с JSON в `ComponentStates` внутри того же save blob контейнера. См. [InventoryItemState.md](./InventoryItemState.md).

---

## Примеры механик и настройки

### 1. Простой агрегированный инвентарь (валюта, расходники)

**Задача:** один список предметов по типам, без фиксированных ячеек, автосохранение.

1. `Storage Mode` = **Aggregated**.
2. Создайте `InventoryDatabase`, добавьте `InventoryItemData` (для стакающихся: `Max Stack` &gt; 1 или -1).
3. В `InventoryComponent` укажите **Database**, **Save Key** (например `player_inventory_main`).
4. `Load Mode` = **UseSaveIfExists**, при необходимости **Initial State Data**.
5. Подбор: на префаб мира — `PickableItem` с `Item Data` и сбором по триггеру или событию.

**Код:** покупка через `TryConsume(currencyId, price)` и `AddItemById(rewardId)` — см. раздел «Примеры» в конце.

### 2. Minecraft-style: хотбар + рюкзак + сундук + перенос кликом

**Задача:** несколько сеток слотов, перенос между контейнерами, рука по физическому слоту хотбара.

**Инвентари (три объекта или три компонента на разных объектах):**

| Объект | Storage Mode | Slot Count | Save Key | Примечание |
|--------|--------------|------------|----------|------------|
| Hotbar | Slot Grid | 9 | `player_hotbar` | Малый инвентарь под руку |
| Backpack | Slot Grid | 27–36 | `player_backpack` | Основной UI |
| Chest | Slot Grid | N | `chest_01` (или ключ от сущности сундука) | Мир / UI сундука |

На каждом: **Database**, **Auto Save** по необходимости, уникальный **Save Key**.

**UI:**

1. На панель хотбара: `InventorySlotGridView` → **Inventory** = компонент хотбара, **Slot Prefab** + **Slots Root** (или заполните **Manual Slots** готовыми `InventorySlotView`).
2. Аналогично для рюкзака и сундука — каждая сетка ссылается на **свой** `InventoryComponent`.
3. **Enable Click Transfer** = включено: первый клик по слоту выбирает источник, второй клик по слоту другой сетки вызывает `InventoryTransferService.Transfer` (merge / swap по правилам стака).

**Рука:**

1. На персонажа: `InventoryHand` → **Inventory** = тот же `InventoryComponent`, что и хотбар.
2. Включите **Use Physical Slot Indices**.
3. `SelectNext` / `SelectPrevious` / колесо через **Selector** — см. [InventoryHand.md](./InventoryHand.md).

### 3. Resident Evil–style: сетка + уникальные предметы с состоянием

**Задача:** фиксированные слоты; оружие/ключи с уникальным состоянием (патроны, апгрейды); состояние в одном save blob контейнера.

1. `Storage Mode` = **Slot Grid**, настройте **Slot Count**.
2. Для типов предметов с уникальным состоянием: в `InventoryItemData` включите **Supports Instance State**, **Max Stack** = 1.
3. На **world prefab** предмета (тот же, что **World Drop Prefab**): добавьте компоненты с `IInventoryItemState` (наследник `InventoryItemStateBehaviour` или своя реализация). При подборе `PickableItem` собирает состояние через `InventoryItemStateUtility.CaptureInstance`; при выбросе `InventoryDropper` восстанавливает через `RestoreInstance`.
4. **Save Key** контейнера один — в JSON попадут и слоты, и `Instances` с payload.

Подробнее по контракту состояния: [InventoryItemState.md](./InventoryItemState.md).

---

## Примеры (код)

```csharp
[SerializeField] private InventoryComponent inventory;

private void Buy(int itemId, int price, int currencyItemId)
{
    if (!inventory.TryConsume(currencyItemId, price))
        return;

    inventory.AddItemById(itemId);
}

if (inventory.GetCount(5) >= 3)
    DoSomething();
```

## См. также

- [README модуля](./README.md)
- [InventoryHand](./InventoryHand.md)
- [InventorySlotGridView](./InventorySlotGridView.md)
- [InventoryItemState](./InventoryItemState.md)
- [InventoryDropper](./InventoryDropper.md)
- [PickableItem](./PickableItem.md)

← [Tools/Inventory](README.md)
