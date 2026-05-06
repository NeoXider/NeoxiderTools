# InventoryHand

**Назначение:** Показывает один выбранный предмет из инвентаря на трансформе-анкоре (например, кость руки). Поддерживает переключение слотов (через `Selector` или код), бросание предмета (через `InventoryDropper`), использование предмета и синхронизацию с физическими слотами инвентаря.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Inventory** | Целевой инвентарь. Если пуст — автопоиск. |
| **Hand Anchor** | Трансформ, к которому крепится заспавненный предмет (например, кость руки). |
| **Selector** | Опциональный `Selector` — для переключения между слотами (колесо мыши, стрелки и т.д.). |
| **Dropper** | Опциональный `InventoryDropper` — для выбрасывания предмета из руки. |
| **Fallback Hand Prefab** | Префаб по умолчанию, если у предмета нет `WorldDropPrefab`. |
| **Scale In Hand Mode** | `Fixed` (умножение на фиксированное значение) или `Relative` (1 + offset поверх `HandView.ScaleInHand`). |
| **Disable Colliders In Hand** | Отключить все коллайдеры на заспавненном предмете в руке. |
| **Use Physical Slot Indices** | Использовать индексы физических слотов (включая пустые), а не упакованных. |
| **Drop Key** | Клавиша для выбрасывания предмета. |
| **Use Key** | Клавиша для использования предмета (вызывает `UseEquippedItem()`). |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void SelectNext()` / `void SelectPrevious()` | Переключить на следующий/предыдущий слот (с зацикливанием). |
| `void SetSlotIndex(int index)` | Установить конкретный слот. `-1` = пустая рука. |
| `void UseEquippedItem()` | Использовать предмет: вызывает `OnUseItemRequested` и `PickableItem.Activate()`. |
| `int DropEquipped(int amount = 1)` | Выбросить предмет через привязанный `InventoryDropper`. |
| `int EquippedItemId { get; }` | ID предмета в руке (или -1). |
| `int SlotIndex { get; }` | Текущий индекс слота. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnEquippedChanged` | `int itemId` | Экипированный предмет изменился. `-1` = пустая рука. |
| `OnUseItemRequested` | `int itemId` | Игрок нажал клавишу использования. |

## Примеры

### Пример No-Code (в Inspector)
Создайте пустой трансформ на кости руки персонажа. Повесьте `InventoryHand`. Перетащите трансформ руки в `Hand Anchor`. Добавьте `Selector` для переключения слотов колесом мыши. Запустите игру — при наличии предметов в инвентаре первый из них появится в руке.

### Пример (Код)
```csharp
[SerializeField] private InventoryHand _hand;

public void EquipSlot(int slotIndex)
{
    _hand.SetSlotIndex(slotIndex);
}

public void UseItem()
{
    _hand.UseEquippedItem();
}
```

## См. также
- [HandView](HandView.md)
- [InventoryDropper](InventoryDropper.md)
- [Selector](../View/Selector.md)
- ← [Tools/Inventory](README.md)
