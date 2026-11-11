# InventoryComponent

**What it is:** Scene `MonoBehaviour` (singleton-capable) facade over plain C# inventory storage that adds
database rules, UnityEvents, optional singleton lookup and SaveProvider persistence.
`Scripts/Tools/Inventory/Runtime/InventoryComponent*.cs` (partial class), namespace `Neo.Tools`.

**How to use:**
1. Add `InventoryComponent`, choose **Storage Mode**, and optionally assign a **Database** and **Save Key**.
2. Add/remove from C# (`AddItemByIdAmount`, `TryConsume`) or from Inspector buttons/UnityEvents.
3. Bind UI and gameplay to `OnInventoryChanged`, `OnItemCountChanged` and `OnSelectedItemChanged`.

---

## Inspector setup

| Field | Description |
|-------|-------------|
| **Database** / **Restrict To Database** | Item metadata and stack caps; optionally reject unknown ids, including direct slot insertions. |
| **Storage Mode** / **Slot Count** | `Aggregated` (one simple stack per id) or `SlotGrid` (fixed physical slots). |
| **Max Unique Items** / **Max Total Items** | Global limits; zero means unlimited within the `int` count range. |
| **Save Key** | Unique provider key for this container; leave blank for initial-content-only loading. |
| **Auto Load** / **Auto Save** | Load on initialization / write provider JSON after mutations. |
| **Load Mode** | Use save, merge initial content once, or ignore save. |
| **Apply Initial If Result Empty** | Reapply initial content when loading leaves an empty inventory. |
| **Set Instance On Awake** | Turn off for additional independent containers such as chests. |
| **Selected Item Id** | Id read by `SelectedItemCount` (NeoCondition-friendly); `InventoryHand` keeps it in sync. |
| **Debug Entries** | Read-only mirror of the contents in Editor Play Mode; not rebuilt in player builds. |

## C# API

| Method / property | Behavior |
|-------------------|----------|
| `TotalItemCount` / `UniqueItemCount` | Total units / distinct logical ids. |
| `GetCount(int itemId)` / `HasItemAmount(int itemId, int amount)` | Count queries across all records. |
| `AddItemByIdAmount(int itemId, int amount)` | Add simple stacks or separate instances according to item data; return accepted units. |
| `AddItemInstance(InventoryItemInstance instance)` | Add a cloned instance record; return its count, or zero. |
| `TryConsume(int itemId, int amount)` | Remove the full positive amount only if available. |
| `TryCaptureSelection(int index, out InventorySelection selection, bool physicalIndex = true)` | Capture the exact slot/UID; aggregated mode uses packed indices. |
| `TryGetSelectionRecord(selection, out record)` / `IsSelectionCurrent(selection)` | Validate ownership and identity; read current detached payload. |
| `TryTakeSelection(selection, amount, out record)` / `TryConsumeSelection(selection, amount = 1)` | Remove the full amount only from that exact record, or change nothing. |
| `ClearInventory()` | Remove all contents. |
| `GetSlot(int index)` / `GetPhysicalSlotRecord(int index)` | Cloned physical slot / record; empty slot or null when absent. |
| `TryInsertIntoSlot(int index, InventoryItemRecord record, int amount = 0)` | Constrained physical insertion; return units inserted. |
| `TrySetSlot(int index, InventorySlotState state)` | Validate and replace a physical slot, including payload-only updates. |
| `MoveSlot(int source, int target, int amount = 0)` / `SwapSlots(int source, int target)` | Move/merge/full-swap or explicit swap within this grid. |
| `TryTakeSlot(int index, int amount, out InventoryItemRecord record)` | Extract a physical grid slot; amount zero takes the whole slot. |
| `TryTakeRecordAtPackedIndex(int index, int amount, out InventoryItemRecord record)` | Extract a non-empty record by packed index. |
| `GetSnapshotRecords()` / `GetSnapshotInstances()` | Deep copies in storage order. |
| `GetSnapshotEntries()` / `GetNonEmptySlotCount()` | Id/count pairs / number of non-empty records in packed order; payloads are not cloned. |
| `GetFirstItemId()` / `GetLastItemId()` | Item id of the first / last packed record, or -1 when empty. |
| `SelectedItemId` / `SelectedItemCount` | Id used by NeoCondition-style checks and its current count. Setting a different id raises `OnSelectedItemChanged`. |
| `Save()` / `Load()` | Persist JSON / restore using the configured load mode. |
| `AutoSave` | Inspector **Auto Save** from code: write provider JSON after every changing mutation. |
| `DeferAutoSave()` / `IsAutoSaveDeferred` | Hold Auto Save back until the returned `AutoSaveScope` is disposed; the outermost scope saves once if a mutation changed the contents. Events still fire per mutation. |

```csharp
[SerializeField] private InventoryComponent _inventory;

public bool SpendCoins(int amount)
{
    return _inventory.TryConsume(10, amount);
}

public void GrantStarterPack()
{
    using (_inventory.DeferAutoSave())
    {
        _inventory.AddItemByIdAmount(1, 50);
        _inventory.AddItemByIdAmount(2, 3);
        _inventory.AddItemByIdAmount(3, 1);
    }
}
```

Scopes nest; only the outermost one writes, and a scope in which nothing changed writes nothing. An explicit
`Save()` inside a scope clears the pending write. Dispose every scope once (a `using` block does); a repeated
or stale dispose is ignored, even after newer scopes were opened at the same depth, and disposing an outer scope
first also closes the inner ones.

Prefer assigned references; `InventoryComponent.FindDefault()` is a fallback. Packed record indices skip
empty slots and differ from physical indices. See [core contracts](InternalTypes.md).

## UnityEvents and NoCode

| Event | Arguments | Behavior |
|-------|-----------|----------|
| `OnInventoryChanged` | none | Refresh UI after a successful mutation, including slot layout/payload changes. |
| `OnItemAdded` / `OnItemRemoved` | item id, delta | Actual count changes only. |
| `OnItemCountChanged` / `OnItemBecameZero` | id/count or id | Updated count / positive count reaching zero. |
| `OnCapacityRejected` | item id, rejected amount | Add request could not fit or was invalid. |
| `OnBeforeLoad` / `OnLoaded` | none | Before restoration / after resulting contents are ready. |
| `OnSaved` | none | JSON has been written to SaveProvider; this does not mean it was flushed to disk. |
| `OnSelectedItemChanged` | new selected id | `SelectedItemId` changed from code (for example by `InventoryHand`). Not a mutation: no save and no `OnInventoryChanged`. |

Successful moves, swaps, and slot replacements publish `OnInventoryChanged` and trigger Auto Save even
when per-id totals are equal. `TrySetSlot` also publishes payload-only writes. Invalid moves and rejected
insertions do not publish a mutation. Cross-container transfers commit both states before callbacks.

Notifications describe the committed mutation. They do not roll it back when an observer throws.
For extraction APIs the `out record` is assigned before notifications, so a caller catching an observer
exception must account for the extracted record. Do not retry a failed-looking add/take merely because
its listener threw. `PickableItem` and `InventoryDropper` settle their world quantity from an internal
commit receipt before publishing these notifications; that receipt is not a new public storage API.

Auto Save runs in finalization even if an item/change observer fails. For paired transfers both
inventories' finalization/save attempts run despite an earlier observer or `OnSaved` fault. The first
exception is rethrown with its original stack after those attempts; remaining listeners in an interrupted
UnityEvent are not replayed. Reentrant operations can change the live balance during notification;
the outer mutation's deltas still describe its own committed before/after snapshots.

In the Inspector, connect `OnInventoryChanged` to a view's `Refresh` or a button to `ClearInventory`.
Use the Play Mode Inspector buttons for ad-hoc adds/removes and C# for operations returning accepted
counts or consume results. Runtime Inspector buttons are restricted to Play Mode; C# methods remain callable.

## Persistence and compatibility

`Save()` writes a version-1 snapshot containing records for aggregated storage or a complete physical
slot array for grid storage. `Load()` restores the saved backend before importing items; a complete saved
slot array determines grid capacity. Grid saves can load into components initially configured as
aggregated, and the reverse.

Legacy saves containing only `Entries` and/or `Instances` retain the component's configured storage
mode and capacity and import through ordinary add rules. Loaded physical slot rows are validated against
current database, stack, unique, and total limits; invalid or over-limit rows are skipped. Malformed JSON
falls back to initial content. The save format and serialized field names are unchanged.

`Save()` and `Load()` use `InventorySaveData.ToJson()` / `InventorySaveData.FromJson(string)`.
These methods use private wire types: a missing slot item id stays -1, a record item id defaults to 0,
and optional instances have zero numeric defaults. This prevents Unity's inline serialization from
turning an absent optional instance into the domain's default item-0/count-1 instance. Only an entirely
empty wire instance (id/count/version zero, empty identity, no payload) becomes absent in slot/record
rows. Simple stacks and empty slots retain their own id/count fields. An explicit count-1 instance,
including item id 0 without an identity, remains an instance. Identity, version and opaque component
payloads are preserved before admission; invalid or over-limit instances never fall back to their
mirrored simple id/count. Existing JSON keys, version 1, legacy lists and merge metadata stay unchanged.

For custom snapshot persistence, use the same pair rather than passing domain snapshots directly
to `JsonUtility`; domain defaults cannot establish whether an inline value was originally absent:

```csharp
InventorySaveData snapshot = new();
string json = snapshot.ToJson();
InventorySaveData restored = InventorySaveData.FromJson(json);
```

An already encoded item-0/keyless/count-1 object is retained as real data. An external writer's
intention to use that same object for null is ambiguous and is not automatically repaired by deleting it.
Прямой `JsonUtility` не сохраняет смысл отсутствующего экземпляра: для снимков используйте эту пару
методов. Поля формата и версия остаются прежними; реальные экземпляры с id 0 сохраняются.

`InitialOnlyIgnoreSave` does not read the provider. A blank Save Key disables persistence reads/writes
while `Load()` can still apply initial content. `MergeSaveWithInitial` applies initial content only when
the saved `InitialStateMerged` marker is false; the optional empty-result fallback still applies.

Auto Save writes provider memory. Call `Neo.Save.SaveProvider.Save()` at the desired flush boundary.
An attempted write can itself fail. Finalization is neither a retry queue nor an atomic transaction
across provider keys or disk writes. `OnSaved` exceptions occur after the corresponding provider write;
do not interpret them as a failed storage mutation.

Inside `DeferAutoSave()` the per-mutation write is skipped and the outermost scope's dispose calls `Save()`
once; an exception from that write (or from `OnSaved`) surfaces from `Dispose`. A scope left open keeps
Auto Save off for this component until it is disposed.

Начиная с 12.0 серию изменений можно сохранить одной записью: `using (inventory.DeferAutoSave()) { ... }`
откладывает Auto Save до закрытия внешней области; события по-прежнему приходят на каждое изменение.

В 11.0 ошибка подписчика не отменяет уже выполненную мутацию: Auto Save всё равно пытается записать
состояние, а перенос между контейнерами завершает обе стороны перед повторным выбросом первой ошибки.

## See also

- [PickableItem](PickableItem.md), [InventoryDropper](InventoryDropper.md), [InventoryHand](InventoryHand.md)
- [Tools / Inventory](README.md)
