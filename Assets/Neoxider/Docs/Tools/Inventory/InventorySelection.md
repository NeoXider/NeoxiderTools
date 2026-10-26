# Exact item selection / Выбор конкретного предмета

`InventorySelection` captures a storage owner, an index and item identity. It supports plain C#
`IInventoryStorage` implementations and the optional `InventoryComponent` scene facade.

Two specimens can share `ItemId = 101` while having different `InstanceId`, level and mutation.
`TryConsume(101, 1)` deliberately consumes by type. To use the selected specimen, capture a selection:

```csharp
if (inventory.TryCaptureSelection(8, out InventorySelection selection))
{
    // Exact ninth physical slot, including its UID and payload.
    bool consumed = inventory.TryConsumeSelection(selection, 1);
}
```

Use `hand.TryGetEquippedSelection(out InventorySelection selection)` for the currently equipped item.
Subscribe to `hand.OnUseSelectionRequested` for exact-instance use; the legacy integer event remains
available. The hand captures before callbacks and never activates a replacement visual after a callback
switches slots or consumes the item. Recursive use during the same request is ignored.

## Identity and mutation contract

- Selections belong to the exact backend object. A matching UID in another inventory is insufficient.
- A moved, removed or replaced unique instance invalidates the captured index. Capture again after moving.
- The same UID may receive new count/payload data. `TryGetSelectionRecord` reads its current state;
  `selection.Record` is a detached snapshot from capture time.
- Ordinary stacks have slot/type identity; replacing a stack with the same type cannot be distinguished.
  Use instance records when this distinction matters.
- Consumption requires a positive amount and sufficient quantity in that exact record. Failure changes
  nothing; it never falls back to another stack or instance with the same item id.
- Physical indices include empty slots. Packed indices skip them and can shift after removal.
- Operations are synchronous and intended for the same thread as the inventory. A selection is not a
  reservation, thread lock or durable reference across save/load.
- Existing mutation notification/save behavior applies. Observer faults propagate after commit and do
  not put consumed items back; do not retry an already committed effect as if it never happened.

For a plain backend:

```csharp
SlotGridInventory hotbar = new(9);
hotbar.AddInstance(new InventoryItemInstance(101));
if (InventorySelection.TryCapture(hotbar, 0, true, out InventorySelection selection))
{
    bool taken = selection.TryTake(hotbar, 1, out InventoryItemRecord item);
}
```

For scene endpoints use `InventoryComponent.TryTakeSelection` so notifications and saving also run.
`InventoryTransferService.TransferSelection(source, selection, target, targetSlot)` validates an exact
physical selection before the existing paired move/merge/swap operation. It rejects packed selections.
For deposit behavior select an empty or compatible destination explicitly; a full incompatible target
can swap under the existing transfer rules. Keep rejected/overflow records in their original container.

## Collection games: carry bag, nine-slot hotbar, podium

Configure separate `SlotGrid` components with capacities appropriate to each container, independent save
keys and singleton registration disabled on additional inventories. Store a collectible's persistent UID
in `InventoryItemInstance.InstanceId`; keep level, mutation, durability or other game data as keyed JSON
`ComponentStates`. Definitions remain reusable assets without scene references.

Map an external string `DataID` to a stable, authored integer catalog id. Do not derive persisted ids from
`string.GetHashCode()`. An existing UID can be assigned to a new instance before adding it. Application
code owns catalog mapping, cloud synchronization and multi-container persistence boundaries; the library
does not claim an atomic cloud transaction across separate keys.

The sample `Demo/Scenes/Inventory/InventoryUseDemo.unity` demonstrates a four-slot temporary catch,
nine physical hotbar slots, a one-slot podium, two same-type unique specimens, and consumable healing
cells. It keeps UID/level/mutation through deposit, placement and recall; full destinations reject without
losing items. Select a slot with a button or 1–9, then press E / **Use selected**. **Reset demo** starts over.
Sample source is under `Assets/Neoxider/Samples~` in release layout, `Assets/Neoxider/Samples` in developer
layout, or `Assets/Samples/NeoxiderTools/11.0.0/Demo Scenes` after Package Manager import.

## По-русски

Для игр с уловом, питомцами, мутациями и подиумами используйте отдельные контейнеры и уникальные
`InventoryItemInstance`, а не только счётчик по `ItemId`. Один тип может иметь много разных UID.
`InventorySelection` фиксирует конкретный слот и экземпляр; после его перемещения или замены старый
выбор отклоняется. Состояние того же экземпляра можно обновлять, затем читать через
`TryGetSelectionRecord`. Количество и payload возвращаются копиями.

Для использования предмета из руки подключите `OnUseSelectionRequested` или
[InventoryUseAction](InventoryUseAction.md). Для расходования вызывайте `TryConsumeSelection`, для
передачи — `TransferSelection`. Не удаляйте предмет из одного контейнера до проверки приёма другим.
При заполненном инвентаре остаток улова должен оставаться во временной сумке. Для обычных стеков
идентичность определяется слотом и типом; если важна история отдельной вещи, храните её как instance.

В `InventoryUseDemo` есть понятный цикл: перенести улов → выбрать PET → разместить на подиуме → вернуть
с тем же UID; выбрать CELL → использовать → увидеть рост HP и уменьшение ровно выбранного стека.

See [Inventory core](InternalTypes.md), [InventoryHand](InventoryHand.md) and [InventoryUseAction](InventoryUseAction.md).
