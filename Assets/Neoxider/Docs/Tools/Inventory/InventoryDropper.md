# InventoryDropper

**Назначение:** Компонент для выбрасывания предметов из инвентаря в игровой мир. Спавнит WorldDropPrefab предмета с физикой, коллайдерами и автоматической настройкой `PickableItem` для повторного подбора. Поддерживает ввод с клавиатуры, бросок с импульсом и случайное смещение.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Inventory** | Источник предметов. Если пуст — автопоиск. |
| **Drop Point** | Трансформ точки спавна. По умолчанию — текущий объект. |
| **Drop Key** | Клавиша для выбрасывания (по умолчанию `G`). |
| **Drop Selected On Key** | Бросать предмет, выбранный в `InventoryComponent.SelectedItemId`. |
| **Fallback Drop Prefab** | Префаб по умолчанию, если у предмета нет `WorldDropPrefab`. |
| **Throw Direction** | Направление броска (локальные координаты). |
| **Throw Impulse** | Сила импульса при броске. |
| **Random Radius** | Случайный разброс позиции спавна вокруг точки. |
| **Add Rigidbody 3D / 2D** | Автоматически добавлять `Rigidbody` на заспавненный предмет. |
| **Configure Pickable Item** | Автоматически настроить `PickableItem` на выброшенном объекте. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `int DropSelected(int amount = 1)` | Выбросить текущий выбранный предмет. |
| `int DropById(int itemId, int amount = 1)` | Выбросить предмет по ID. |
| `int DropSlot(int slotIndex, int amount)` | Выбросить из конкретного физического слота. |
| `int DropFirst(int amount = 1)` | Выбросить первый доступный предмет. |
| `int DropLast(int amount = 1)` | Выбросить последний доступный предмет. |
| `void SetDropEnabled(bool enabled)` | Включить/выключить возможность выбрасывания. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnItemDropped` | `int itemId, int amount, GameObject dropped` | Предмет успешно выброшен. |
| `OnDropFailed` | `int itemId, int amount` | Попытка выбросить не удалась. |

## Примеры

### Пример No-Code (в Inspector)
На персонаже повесьте `InventoryDropper`. Создайте дочерний пустой объект перед персонажем — это `Drop Point`. Включите `Drop Selected On Key = true`, `Drop Key = G`. Теперь при нажатии `G` выбранный предмет вылетит вперёд из рук и его можно будет подобрать снова.

### Пример (Код)
```csharp
[SerializeField] private InventoryDropper _dropper;

public void DropCurrentWeapon()
{
    int dropped = _dropper.DropSelected();
    Debug.Log($"Выброшено: {dropped} шт.");
}
```

## См. также
- [InventoryHand](InventoryHand.md)
- [PickableItem](PickableItem.md)
- [InventoryComponent](InventoryComponent.md)
- ← [Tools/Inventory](README.md)
