# InventoryPickupBridge

**Назначение:** Мост-утилита, который вешается на персонажа (или его дочерний триггер). Перенаправляет физические события (триггеры, события от `PhysicsEvents3D`) в метод `Collect()` у `PickableItem`. Удобен для No-Code подхода.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Pickable Item** | Ссылка на целевой `PickableItem`. Если не указан, берет компонент с этого же объекта. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void Collect()` | Вызывает `PickableItem.Collect()` (ручной подбор). |
| `void CollectFromCollider(Collider collider3D)` | Подобрать с передачей 3D-коллайдера коллектора. |
| `void CollectFromCollider2D(Collider2D collider2D)` | Подобрать с передачей 2D-коллайдера коллектора. |
| `void CollectFromGameObject(GameObject collector)` | Подобрать с передачей GameObject коллектора. |

## Примеры

### Пример No-Code (в Inspector)
На персонаже создайте дочерний объект с `SphereCollider (Is Trigger)`. Добавьте `PhysicsEvents3D` и `InventoryPickupBridge`. В событии `OnTriggerEnter` компонента `PhysicsEvents3D` вызовите `InventoryPickupBridge.CollectFromCollider()`. Теперь любой `PickableItem`, попавший в триггер, автоматически подберется.

## См. также
- [PickableItem](PickableItem.md)
- ← [Tools/Inventory](README.md)
