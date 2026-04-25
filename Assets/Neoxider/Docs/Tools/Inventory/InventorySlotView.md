# InventorySlotView

**Назначение:** UI-компонент, представляющий одну физическую ячейку в `InventorySlotGridView`. Он содержит компонент для отрисовки самого предмета (`InventoryItemView`), а также логику подсветки при выделении и обработку кликов.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Item View** | Ссылка на `InventoryItemView` (картинка и количество), который лежит внутри этого слота. |
| **Selection Highlight** | GameObject (рамка или свечение), который включается, когда игрок кликает по слоту для переноса. |
| **Empty Root** | GameObject (например, полупрозрачный фон), который отображается, только если слот пуст. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void Bind(...)` | Вызывается автоматически из `InventorySlotGridView` для передачи данных о предмете и состоянии слота. |
| `void OnPointerClick(...)` | Обработчик интерфейса `IPointerClickHandler`. Передает индекс слота в родительский Grid для логики переноса. |

## Примеры

### Пример No-Code (в Inspector)
Создайте префаб UI-слота (квадратный фон). Внутрь поместите дочерний объект `ItemPresenter` (с `InventoryItemView`), дочерний объект `Highlight` (желтая рамка) и дочерний объект `EmptyBg` (серый фон). В `InventorySlotView` раскидайте эти ссылки по полям. Теперь при пустом слоте будет виден только `EmptyBg`, а при клике включится `Highlight`.

## См. также
- [InventorySlotGridView](InventorySlotGridView.md)
- [InventoryItemView](InventoryItemView.md)
- ← [Tools/Inventory](../README.md)
