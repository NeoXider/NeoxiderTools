# InventoryPickupBridge

Мост между внешним вызовом (кнопка, коллайдер, другой скрипт) и подбором предмета: перенаправляет `Collect` и методы `CollectFromCollider*` / `CollectFromGameObject` в привязанный [PickableItem](./PickableItem.md).

- **Пространство имён:** `Neo.Tools`
- **Путь:** `Assets/Neoxider/Scripts/Tools/Inventory/Runtime/InventoryPickupBridge.cs`

Если `Pickable Item` не задан, используется компонент на том же объекте. Удобно для вызова подбора из UI или из зоны без привязки к конкретному коллайдеру.

См. также [PickableItem](./PickableItem.md), [InventoryComponent](./InventoryComponent.md).
