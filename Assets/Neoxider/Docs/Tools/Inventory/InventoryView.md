# InventoryView

**Назначение:** Главный UI-компонент для отображения содержимого инвентаря в виде списка. Умеет автоматически спавнить `InventoryItemView` для каждого предмета (из префаба) или обновлять заранее расставленные вручную ячейки (Manual Mode).

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Inventory** | Ссылка на `InventoryComponent`. Если пустая, найдет автоматически. |
| **View Mode** | `SpawnFromPrefab` — динамически создает ячейки. `ManualList` — обновляет только ваши заранее расставленные `InventoryItemView`. |
| **Source Mode** | Откуда брать список предметов: `DatabaseItems` (все из БД), `SnapshotItems` (только имеющиеся), `Hybrid` (объединение). |
| **Show Only Non Zero** | Скрывать ячейки предметов, количество которых равно 0. |
| **Item View Prefab** | Префаб ячейки (с `InventoryItemView`) для режима `SpawnFromPrefab`. |
| **Items Root** | Контейнер, в который спавнятся ячейки. По умолчанию — текущий трансформ. |
| **Manual Views** | Список заранее расставленных `InventoryItemView` для режима `ManualList`. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void SetInventory(InventoryComponent inventory)` | Привязать к другому инвентарю и обновить UI. |
| `void Refresh()` | Принудительно перерисовать все ячейки. |

## Примеры

### Пример No-Code (в Inspector)
Создайте UI-панель с `Vertical Layout Group`. Повесьте `InventoryView`. В поле `Item View Prefab` перетащите префаб строки (с `InventoryItemView`). Выберите `Source Mode = Hybrid`, `Show Only Non Zero = true`. При запуске игры панель автоматически покажет список предметов из инвентаря.

### Пример (Код)
```csharp
[SerializeField] private InventoryView _shopView;

public void OpenShopUI(InventoryComponent shopInventory)
{
    _shopView.SetInventory(shopInventory);
    _shopView.gameObject.SetActive(true);
}
```

## См. также
- [InventoryItemView](InventoryItemView.md)
- [InventorySlotGridView](InventorySlotGridView.md)
- [InventoryComponent](InventoryComponent.md)
- ← [Tools/Inventory](README.md)