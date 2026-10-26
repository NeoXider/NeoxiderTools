# Inventory Core — Public C# Contracts

These types are public runtime building blocks in `Neo.Tools`; storage does not require a scene,
singleton, ScriptableObject, or SaveProvider. `InventoryComponent` is an optional scene adapter.

| Type | Description |
|------|-------------|
| `IInventoryStorage` | Counts, constrained additions/removals, packed records, and cloned snapshots. |
| `AggregatedInventory` | One simple stack per item id; instance records stay separate. |
| `SlotGridInventory` / `ISlottedInventory` | Fixed array of physical slots; UI determines the spatial layout. |
| `InventoryConstraints` | Global distinct-id/total limits and per-id stack caps; copied by `SetConstraints`. |
| `InventoryEntry` | Legacy serializable item id/count pair. |
| `InventoryItemInstance` / `InventoryItemComponentState` | Instance identity, count, and keyed JSON payloads. |
| `InventoryItemRecord` | Simple stack or stateful instance, used for snapshots and transfers. |
| `InventorySelection` | Owner-bound slot/type/UID selection; exact take without falling back to another record. |
| `InventoryManager` | Legacy plain C# id/count storage; use the storage interfaces for instance/slot support. |
| `InventorySaveData` | Versioned snapshot with legacy entries, records, instances, and physical slots; use its `ToJson` / `FromJson` pair for JSON. |
| `InventorySlotState` | Empty slot, simple stack, or stateful instance; no built-in lock flag. |
| `InventorySlotTransferRules` | Low-level move/merge/swap/extract/insert helpers; caller owns global limits. |
| `InventoryStackRules` | Shared capacity arithmetic. |
| `InventoryStorageMode` | `Aggregated` or `SlotGrid`. |
| `InventoryTransferService` | Cross-component slot transfer; validates both endpoints before writing either. |
| `InventoryUseAction` | Optional scene adapter for exact-instance use, consumption and gameplay UnityEvents. |
| `IInventoryItemState` / `InventoryItemStateBehaviour` | Capture/restore contract and optional MonoBehaviour base. |
| `InventoryItemStateUtility` | Captures/restores matching state components throughout a prefab hierarchy. |
| `InventoryDatabase` / `InventoryInitialStateData` | Reusable ScriptableObject item definitions and initial content. |

## Mutation and boundary behavior

- `IInventoryStorage.Add` returns units accepted; invalid ids/non-positive counts return zero. `AddInstance` returns
  the entire normalized instance count or zero. It does not merge stateful records.
- `MaxUniqueItems` counts logical ids across simple stacks and instances. A simple stack for an
  existing instance's id does not consume another unique-id allowance.
- Non-positive configured limits mean unlimited, bounded by the public `int` count range. Additions
  stop at `int.MaxValue` total; instance additions exceeding it fail without inserting a part.
- `SetConstraints` clones its argument and does not trim existing contents. Lowering a stack cap can
  leave an existing stack above it; moving to an empty slot respects the new cap and keeps the remainder.
- `GetSlot` and all snapshots return copies. Update through a mutation API rather than editing a snapshot.
- `MoveSlot` / `TryInsertIntoSlot` use amount `0` for the default full request. Positive amounts are
  bounded by available source count. Simple merges conserve count; incompatible contents only swap
  for a full request. Empty-slot simple inserts also enforce the stack cap.
- `SlotGridInventory.TryInsertIntoSlot` enforces global limits. Its `SetSlot` method intentionally
  bypasses limits; validate arbitrary data or use the component's `TrySetSlot`.
- Packed indices skip empty slots; physical indices include them. Do not interchange the two.

`InventoryTransferService.Transfer` operates on `InventoryComponent` endpoints in slot mode. Both
candidate slot states are validated and written before inventory callbacks execute. Rejected transfers
do not emit temporary remove/add events. Callback exceptions propagate; committed contents remain
committed. Each side attempts eligible Auto Save even when its notifications throw, and both sides'
finalizers are attempted before the first fault is rethrown. Later listeners in an interrupted event
are not replayed. This is an in-memory paired commit, not an atomic multi-key or disk transaction;
provider write failures still need application-level handling.

## Snapshot JSON

Use `snapshot.ToJson(bool prettyPrint = false)` and `InventorySaveData.FromJson(string json)` for
custom persistence, matching the scene adapter. Public domain fields and `InventoryItemInstance.Count`
default 1 remain unchanged. Private slot wire rows retain `ItemId=-1`, record rows retain `ItemId=0`,
and optional instance wire fields have zero numeric defaults. This preserves empty/simple rows without
discarding real item-0/count-1 instances; decoding does not generate identities, clone/normalize counts,
or apply storage admission. Nonempty invalid instances retain precedence over a mirrored simple row
and are rejected by the existing load rules. JSON keys/version and legacy metadata remain compatible.
See [persistence and compatibility](InventoryComponent.md#persistence-and-compatibility) for null
semantics and the limitation of ambiguous externally encoded instances. Invalid JSON follows
`JsonUtility` error behavior; the component's `Load` handles it using its existing initial-content policy.

## Scene adapter

`InventoryComponent.Operations` owns additions/removals and delta events; `.Grid` owns physical slot
operations; `.Queries` exposes snapshots and packed indices; `.Persistence` owns SaveProvider import/export.
These partials are one public component, not separate storage implementations.
The internal `.Commit` partial separates backend writes from callbacks for pickup/drop ownership
handoffs. Its exact accepted/extracted quantity is captured before user code and is not inferred from
the live balance after reentrant observers. Plain C# storage stays callback-free and retains the public
contracts above; no reservation or general transaction API is introduced.

See [InventoryComponent](InventoryComponent.md) and the [module entry point](README.md).
