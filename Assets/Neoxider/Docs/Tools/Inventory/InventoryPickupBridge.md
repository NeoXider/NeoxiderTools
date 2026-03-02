# InventoryPickupBridge

**Что это:** компонент-мост: вызов Collect/CollectFromCollider/CollectFromGameObject перенаправляется в привязанный [PickableItem](./PickableItem.md). Позволяет вызывать подбор из кнопки, зоны или другого скрипта без привязки к конкретному коллайдеру. Пространство имён: `Neo.Tools`. Файл: `Scripts/Tools/Inventory/Runtime/InventoryPickupBridge.cs`.

**Как использовать:** добавить на объект с кнопкой или триггером, при необходимости назначить **Pickable Item** (если пусто — берётся на этом же объекте), из UnityEvent или кода вызывать Collect() или CollectFromCollider(Collider)/CollectFromGameObject(GameObject).

---

См. также [PickableItem](./PickableItem.md), [InventoryComponent](./InventoryComponent.md).
