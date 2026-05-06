# PickableItem

**Назначение:** Компонент для предметов на уровне (монеты, аптечки, оружие), которые игрок может подобрать. Реагирует на триггеры (2D/3D), фильтрует по тегу, поддерживает валидацию коллектора и автоматическое уничтожение/деактивацию после сбора.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Item Data** | Ссылка на `InventoryItemData` (приоритетный источник ID). |
| **Item Id** | Запасной ID предмета, если `Item Data` не назначен. |
| **Amount** | Количество предметов, добавляемых при подборе. |
| **Target Inventory** | Целевой инвентарь. Если пуст и `Auto Find` включен — найдет сам. |
| **Collect On Trigger 3D / 2D** | Подбирать автоматически при входе в 3D/2D триггер. |
| **Required Collector Tag** | Фильтр по тегу (пусто = без фильтра). Например, `Player`. |
| **Require Collector Inventory** | Требовать наличие `InventoryComponent` на объекте-собирателе. |
| **Collect Only Once** | Подбирается только один раз (защита от повторного сбора). |
| **Destroy After Collect** | Удалить `GameObject` после успешного подбора. |
| **Deactivate After Collect** | Деактивировать объект, если `Destroy` выключен. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `bool Collect()` | Попытаться подобрать предмет вручную (без привязки к коллектору). |
| `bool CollectFromGameObject(GameObject collector)` | Подобрать с указанием коллектора (для проверки тега/инвентаря). |
| `void Activate()` | Вызывает событие `OnActivate` (для использования предмета в руке). |
| `void Configure(...)` | Настроить предмет из кода (ItemData, fallbackId, amount, targetInventory). |
| `int ResolvedItemId { get; }` | Возвращает итоговый ID предмета. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnCollectStarted` | *(нет)* | Начало сбора (до добавления в инвентарь). |
| `OnCollected` | `int itemId, int amount` | Предмет успешно добавлен в инвентарь. |
| `OnCollectFailed` | *(нет)* | Сбор не удался (нет инвентаря, фильтр не пройден и т.д.). |
| `OnAfterCollectDespawn` | *(нет)* | Вызывается перед уничтожением/деактивацией объекта. |
| `OnActivate` | *(нет)* | Предмет активирован (для использования в руке через `InventoryHand`). |

## Примеры

### Пример No-Code (в Inspector)
Создайте префаб монеты. Добавьте `SphereCollider` → `Is Trigger = true`. Повесьте `PickableItem`. Установите `Item Id = 10`, `Amount = 1`, `Collect On Trigger 3D = true`, `Destroy After Collect = true`. Теперь, когда игрок входит в триггер, монета подберется и исчезнет.

### Пример (Код)
```csharp
[SerializeField] private PickableItem _keyPickup;

public void ForcePickupKey()
{
    bool success = _keyPickup.Collect();
    if (success) Debug.Log("Ключ подобран!");
}
```

## См. также
- [InventoryPickupBridge](InventoryPickupBridge.md)
- [InventoryComponent](InventoryComponent.md)
- [InventoryDropper](InventoryDropper.md)
- ← [Tools/Inventory](README.md)
