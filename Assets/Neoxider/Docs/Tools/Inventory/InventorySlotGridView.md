# InventorySlotGridView

**Что это:** компонент UI для отображения фиксированной сетки слотов одного `InventoryComponent` в режиме **Slot Grid**; опционально пошаговый перенос между двумя контейнерами через клик и `InventoryTransferService`. Файл: `Scripts/Tools/Inventory/UI/InventorySlotGridView.cs`.

**Как использовать:**
1. Убедитесь, что целевой `InventoryComponent` имеет **Storage Mode** = **Slot Grid**.
2. Добавьте `InventorySlotGridView` на объект UI (панель).
3. Назначьте **Inventory** (или включите **Auto Find Inventory** для `FindDefault()`).
4. Либо укажите **Slot Prefab** (`InventorySlotView`) и **Slots Root**, либо заполните список **Manual Slots** готовыми дочерними слотами (число должно покрывать `Slot Capacity` инвентаря).
5. Для переноса между двумя сетками оставьте **Enable Click Transfer** включённым: первый клик — выбор источника, второй клик по другой сетке — перенос в выбранный слот.
6. Подписка на `OnInventoryChanged` / `OnLoaded` выполняется компонентом автоматически (см. **Refresh On Loaded**).

---

## Поля

| Поле | Описание |
|------|----------|
| **Inventory** | Целевой контейнер (обязательно Slot Grid). |
| **Auto Find Inventory** | Если пусто — `InventoryComponent.FindDefault()`. |
| **Refresh On Loaded** | Обновлять сетку после `Load()`. |
| **Refresh Next Frame On Enable** | Отложенный `Refresh()` на следующий кадр (после layout). |
| **Slot Prefab** | Префаб ячейки с `InventorySlotView` и `InventoryItemView` при отсутствии manual slots. |
| **Slots Root** | Родитель для инстансов префаба. |
| **Manual Slots** | Явный список слотов без спавна из префаба. |
| **Enable Click Transfer** | Режим «выбрать слот → кликнуть цель» для `InventoryTransferService.Transfer`. |

## API

| Метод / свойство | Описание |
|------------------|----------|
| **Inventory** | Текущая привязка. |
| **SetInventory** | Сменить инвентарь и обновить отображение. |
| **Refresh** | Перечитать слоты из инвентаря. |
| **HandleSlotClick** | Вызывается из `InventorySlotView` при клике. |

## Пример настройки (два контейнера на экране)

1. **Backpack:** объект UI + `InventorySlotGridView`, **Inventory** → `InventoryComponent` игрока (рюкзак), **Save Key** у инвентаря свой.
2. **Chest:** вторая панель + второй `InventorySlotGridView`, **Inventory** → `InventoryComponent` сундука.
3. Оба инвентаря: **Slot Grid**, один **Database** (общая таблица предметов).
4. Игрок открывает сундук: оба `InventorySlotGridView` активны; клик по слоту рюкзака, затем по слоту сундука — перенос.

## См. также

- [InventoryComponent](./InventoryComponent.md) — раздел «Minecraft-style»
- [README](./README.md)

← [Tools/Inventory](README.md)
