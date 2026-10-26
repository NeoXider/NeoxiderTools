# Tools / Inventory

Reusable inventory storage, optional scene components, pickup/drop, equipped visuals, and UI binding.
Namespace: `Neo.Tools`; assembly: `Neo.Tools.Inventory`.

## Runtime API

```csharp
InventoryConstraints limits = new() { MaxTotalItems = 20, MaxUniqueItems = 4 };
limits.SetItemMaxStack(7, 5);
SlotGridInventory bag = new(6);
bag.SetConstraints(limits);
int added = bag.Add(7, 8); // 5 in slot 0, 3 in slot 1
int moved = bag.MoveSlot(1, 2, 2);
InventorySlotState slot = bag.GetSlot(2); // a copy; Count == 2
```

`AggregatedInventory` keeps one simple stack per item id and separate instance records.
`SlotGridInventory` uses physical indices, including empty slots, and caps each simple stack separately.
Both return actual accepted counts and clone instance payloads/snapshots. Unique limits count item ids,
including ids already represented by an instance. Unlimited limits still respect the `int.MaxValue`
total supported by the public count API.

Use `TryInsertIntoSlot` for a constrained insertion. Low-level `SlotGridInventory.SetSlot` is an
unchecked restoration/authoring API; `InventoryComponent.TrySetSlot` validates a complete replacement.
See [core contracts](InternalTypes.md) and [InventoryComponent](InventoryComponent.md).

## Scene / Inspector quick start

1. Add `InventoryComponent` and assign an optional `InventoryDatabase` for item metadata and validation.
2. Choose `Aggregated`, or `SlotGrid` with a slot count. Keep singleton registration off for additional chests.
3. Add `PickableItem` to world drops. Default collector validation requires an inventory on the collector or its parents.
4. Add `InventoryDropper` if items should return to the world. Assign the same inventory to the hand/dropper pair.
5. Bind counts and views with `InventoryView`, `InventorySlotGridView`, or the text components.
6. For use from the hand add [InventoryUseAction](InventoryUseAction.md), set a type/consume amount,
   and connect `OnUsed` to the effect. Prefer exact selections when multiple items share one type id.

## Playable example and collection games

Open `Demo/Scenes/Inventory/InventoryUseDemo.unity` from the **Demo Scenes** sample. Deposit the catch,
select a PET, place/recall it with its UID and state, then select CELL and use it to restore HP.
The screen explains each step, shows the selected item and remaining quantity, and provides Reset.
It demonstrates independent temporary carry, nine-slot hotbar and podium inventories. See
[exact selection and collection-game integration](InventorySelection.md) for API and save/catalog mapping.

**По-русски:** готовый пример показывает использование, а не только подбор и переключение. Улов,
постоянный инвентарь и подиум — отдельные контейнеры; UID, уровень и мутация сохраняются при переносе.
`TryConsumeSelection` расходует выбранный экземпляр, `TryConsume(itemId, amount)` — количество по типу.
Исходники сэмпла: `Assets/Neoxider/Samples~` (release), `Assets/Neoxider/Samples` (разработка); после
UPM-импорта — `Assets/Samples/NeoxiderTools/11.0.0/Demo Scenes`.

Successful slot moves, swaps, and payload replacements notify UI and auto-save even when item counts
stay equal. Item delta events only describe actual count changes. Auto-save writes JSON to the current
SaveProvider; call `SaveProvider.Save()` at your application's persistence boundary to flush it.
Auto-save is attempted even when a mutation observer throws; paired transfers attempt both inventories'
finalizers and rethrow the first fault after those attempts. This does not make two save keys atomic.

Partial pickups leave the rejected remainder in the world. `CanDrop` gates every drop entry point,
including calls from `InventoryHand`. Runtime Inspector buttons are available in Play Mode.

In 11.0, pickup remaining amounts and drop recovery use the exact committed quantity before inventory
callbacks. Failed drops return to their original physical slot first. If reentrant callbacks occupy the
available capacity, the dropper retains the unreturned quantity: inspect `PendingReturnCount`, free room,
and call `RetryPendingReturns()` before requesting more drops. See the pickup/drop pages for ownership
and exception behavior. Item icons remain authored asset references; prefab previews are resolved by views.

Кратко: количество в мире фиксируется до событий, сохранение выполняется и при ошибке обработчика.
Невместившийся возврат остаётся у `InventoryDropper` до явного `RetryPendingReturns()`.

## Documentation

| Page | Description |
|------|-------------|
| [InternalTypes](InternalTypes.md) | Public C# storage, constraints, snapshots, and slot contracts |
| [InventoryComponent](InventoryComponent.md) | Scene facade, persistence, events, and load compatibility |
| [PickableItem](PickableItem.md), [InventoryDropper](InventoryDropper.md) | Pickup and drop workflows |
| [InventorySlotGridView](InventorySlotGridView.md), [InventoryItemState](InventoryItemState.md) | Slot UI and instance payloads |
| [InventoryView](InventoryView.md), [InventoryHand](InventoryHand.md), [HandView](HandView.md) | List UI and equipped visuals |
| [InventorySelection](InventorySelection.md), [InventoryUseAction](InventoryUseAction.md) | Exact instance use, consumption and transfer; collection-game sample |
| [InventoryItemCountText](InventoryItemCountText.md), [InventoryTotalCountText](InventoryTotalCountText.md) | Count display |
| [InventoryDatabase](Data/InventoryDatabase.md) | Item definitions and initial-content assets |

## See also

- [Save](../../Save/README.md)
- [Shop](../../Shop/README.md)
- [Shop bridges](../../Shop/Bridges.md) — canonical purchase-to-inventory setup and best-effort grant contract
