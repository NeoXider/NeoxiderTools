# InventorySlotGridView

**Назначение:** UI-компонент для отображения физической сетки слотов (режим `SlotGrid` в `InventoryComponent`). Автоматически спавнит `InventorySlotView` для каждого слота (включая пустые) и управляет их обновлением.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Inventory** | Ссылка на инвентарь (должен быть в режиме `Slot Grid`). |
| **Auto Find Inventory** | Автоматически найти `InventoryComponent` на сцене при старте, если ссылка пуста. |
| **Slot Prefab** | Префаб пустого слота (с компонентом `InventorySlotView`), который будет клонироваться. |
| **Slots Root** | Контейнер (например, с `GridLayoutGroup`), куда будут помещаться заспавненные слоты. |
| **Manual Slots** | Список заранее расставленных слотов вручную (если вы не хотите спавнить префаб). |
| **Enable Click Transfer** | Включает логику перемещения: первый клик выделяет слот, второй клик (по другому слоту) перемещает предмет. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void SetInventory(InventoryComponent inventory)` | Привязать сетку к другому инвентарю "на лету" и обновить UI. |
| `void Refresh()` | Принудительно перерисовать все слоты (вызывается автоматически при изменениях). |
| `void HandleSlotClick(int slotIndex)` | Обрабатывает клик по конкретному слоту (вызывается из `InventorySlotView`). |

## Примеры

### Пример No-Code (в Inspector)
Создайте UI Panel, добавьте на неё `GridLayoutGroup` и `InventorySlotGridView`. Перетащите в поле `Slot Prefab` ваш префаб ячейки инвентаря. При запуске игры сетка автоматически заполнится пустыми слотами по размеру `Slot Capacity` из `InventoryComponent`.

### Пример (Код)
```csharp
[SerializeField] private InventorySlotGridView _playerGrid;
[SerializeField] private InventorySlotGridView _chestGrid;

public void OpenChest(InventoryComponent chestInventory)
{
    // Переключаем UI-сетку сундука на инвентарь нового сундука
    _chestGrid.SetInventory(chestInventory);
    
    // Благодаря Enable Click Transfer игрок сможет перекладывать вещи между сетками кликами
}
```

## См. также
- [InventorySlotView](InventorySlotView.md)
- [InventoryComponent](InventoryComponent.md)
- ← [Tools/Inventory](../README.md)