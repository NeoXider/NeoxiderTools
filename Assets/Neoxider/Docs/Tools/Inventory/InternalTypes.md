# Inventory Core — Internal Types

Внутренние типы ядра инвентаря. Не являются MonoBehaviour — используются `InventoryComponent` и `InventoryManager` внутренне.

| Тип | Описание |
|-----|----------|
| `AggregatedInventory` | Объединение нескольких инвентарей в один. |
| `IInventoryItemState` | Интерфейс состояния предмета. |
| `IInventoryStorage` | Интерфейс хранилища предметов. |
| `InventoryConstraints` | Ограничения инвентаря (макс. слоты, макс. стек). |
| `InventoryEntry` | Запись предмета в инвентаре (данные + количество). |
| `InventoryItemComponentState` | Состояние компонента предмета. |
| `InventoryItemInstance` | Экземпляр предмета с уникальным ID. |
| `InventoryItemRecord` | Запись предмета для сериализации. |
| `InventoryManager` | Менеджер инвентаря (логика добавления/удаления). |
| `InventorySaveData` | Данные сохранения инвентаря. |
| `InventorySlotState` | Состояние слота (пустой, занятый, заблокированный). |
| `InventorySlotTransferRules` | Правила перемещения между слотами. |
| `InventoryStackRules` | Правила стекирования предметов. |
| `InventoryStorageMode` | Режим хранения (List, Slots). |
| `InventoryTransferService` | Сервис перемещения между инвентарями. |
| `ISlottedInventory` | Интерфейс слотового инвентаря. |
| `SlotGridInventory` | Сеточный инвентарь (2D слоты). |
| `InventoryDatabase` | ScriptableObject — база данных всех предметов. |
| `InventoryInitialStateData` | Начальное состояние инвентаря (пресет). |

### Partial-классы InventoryComponent
| Файл | Описание |
|------|----------|
| `InventoryComponent.Grid` | Grid-функциональность InventoryComponent. |
| `InventoryComponent.Operations` | Операции (добавление, удаление, перемещение). |
| `InventoryComponent.Persistence` | Сохранение/загрузка инвентаря. |
| `InventoryComponent.Queries` | Запросы (поиск, фильтрация, подсчёт). |

### Runtime утилиты
| Тип | Описание |
|-----|----------|
| `InventoryItemStateBehaviour` | MonoBehaviour-обёртка для IInventoryItemState. |
| `InventoryItemStateUtility` | Утилита для работы с состояниями предметов. |

## См. также
- [InventoryComponent](InventoryComponent.md) — ← [Tools/Inventory](README.md)
