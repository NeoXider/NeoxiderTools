# Selector

**Purpose:** Select a single item, a fill prefix, or additive random items with optional unique cycles,
exclusions, persistence and networking. Namespace: `Neo.Tools`; assembly: `Neo.Tools.View`.
Use [SelectorModel](./SelectorModel.md) for selection rules without scene objects.

## Setup

- Add the component to a parent with item children. Child synchronization is on by default, including
  inactive children. For manually maintained `Items`, disable `_autoUpdateFromChildren`.
- Use `Count > 0` for virtual items without GameObjects/children. Methods, indices and events still work.
- `startOnAwake` applies the start selection on enable and initial synchronization. Disable it to retain
  authored states until an explicit `Set`, `Next`, `SetRandom` or `RefreshItems` call.
- For event-driven items, enable `NotifySelectorItemsOnly` and attach [SelectorItem](./SelectorItem.md).
  Matching entries receive typed state changes; entries without that component still use GameObject
  activation. `_controlGameObjectActive = false` suppresses both kinds of activation.

## C# API and index rules

```csharp
selector.Set(2);
selector.Next();
selector.ExcludeIndex(1);       // Exclusions affect random selection only.
selector.SetRandom(false);     // Activate the random result while keeping existing active entries.
GameObject selected = selector.Item;
int activeCount = selector.CountActive;
```

Enable random selection in the Inspector before calling `SetRandom`. `FillMode` always applies a prefix
exclusively. `Value` is the logical index; `IndexOffset` converts it to an item position. For example,
three items and offset 1 have logical bounds -1..1. `SetLast()` selects the third item at `Value == 1`.
Changing the offset or shrinking/refreshing the item collection clamps `Value` to those logical bounds,
so `Item`, `GetSelectedItem()` and the visible selection agree. Removing the last item emits a null
selected object and zero active count. `AddItem` / `RemoveItem` manage arrays; `RefreshItems` rebuilds
the array from children.

Logical bounds must fit in `Int32`. `Count` / `IndexOffset` reject incompatible combinations with
`ArgumentOutOfRangeException` before replacing their previous value. Wrapping, stepping, item lookups
and Inspector pool enumeration use wide intermediate arithmetic, including a singleton logical index
at `int.MaxValue`. `IndexWithOffset` is a checked raw int readback; see [SelectorModel](./SelectorModel.md)
for raw-sum overflow and individually configured model properties.

`SetFirst`, `SetLast`, `Reset`, `ToggleFillMode`, and `ToggleIndex` provide direct controls for UnityEvent
wiring or C#. `OnSelectionChanged`, `OnSelectionChangedGameObject` and `OnCountActiveChanged` expose
indices, selected objects and active counts. With real items, `CountActive` inspects actual GameObject
or SelectorItem state; virtual count uses the logical single/prefix count.

Unique random selection tracks every included index, even when only one remains. After exhaustion,
the next request emits `OnUniqueCycleComplete`; automatic reset also emits `OnUniqueReset`. With reset
disabled it does not select again until `ResetUnique()`. `CreateModelSnapshot()` returns an independent
plain C# snapshot, suitable for inspection without mutating scene state.

## Save and network lifecycle

Enable `_saveEnabled`, give each independent selector a unique `_saveKey`, and choose `SelectorSaveMode`.
Keys use `_Index` and `_Excluded` suffixes. Initial runtime selection/child synchronization cannot write
over stored values before `Start` loads them. Writes, including explicit calls made before the first
`Start`, are deferred until initialization; later `Set` and exclusion changes save immediately. Re-enable
continues the existing start-index behavior. Unique history and additive active sets are not persisted.

In network mode, `AuthorityMode` controls client permission through Neo's existing network contracts.
Random selection executes on the server. Single and bulk exclusion mutations (`SetExcludedIndices`)
are sent to the server with the same authorization/rate checks and broadcast back to clients. Inspector
callbacks remain optional; no additional NoCode component is needed for the public C# methods.

Queued ingress is rejected when the component is local-only or the sender's authority policy is
unknown/denied, before any rate budget is consumed. Selection requests also require a nonempty valid
logical range and an in-range logical index; random requests require random selection enabled.
Accepted remote callers have independent budgets through the shared unscaled-time Neo network limiter.
Repeated requests from one caller keep the normal limit; trusted server/null/exact host calls retain
the existing shared helper policy. `None` remains the cooperative default; `OwnerOnly` uses this
identity's owner. Exclusions keep their existing integer-set/CSV interpretation, including null/empty
bulk clearing and entries outside the current random bounds. Direct public server/host/offline calls
keep their existing paths. A rejected network request provides no acknowledgement or optimistic rollback.

Received selection and late-join snapshots apply the complete active set before publishing
`OnSelectionChanged`, `OnSelectionChangedGameObject`, persistence, and the final active-count event.
Those Selector-level observers therefore read the final active/excluded state. Local operations retain
their existing event order. Unity activation and individual SelectorItem callbacks are not an atomic
transaction across all items.

The 11.0 selection RPC includes both active and excluded index arrays in the same payload, rather than
depending on a later SyncVar update for exclusions. Network peers must use the matching package version;
existing serialized fields, public C# methods and save keys are retained.

Under `MIRROR`, derived C# server adapters can call the protected
`TryApplyNetworkSelectionRequest(index, fillMode, deactivateNonSelected, sender)` and
`TryApplyNetworkRandomRequest(deactivateOthers, sender)` paths used by the Commands. Their bool result
reports local server admission only; it is not a network response. No additional scene setup is needed.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `Count` | Count. |
| `CountActive` | Count Active. |
| `ExcludedCount` | Excluded Count. |
| `ExcludedIndices` | Excluded Indices. |
| `FillMode` | Fill Mode. |
| `HasItems` | Has Items. |
| `IncludedIndices` | Included Indices. |
| `IndexOffset` | Index Offset. |
| `IndexWithOffset` | Index With Offset. |
| `IsAtEnd` | Is At End. |
| `IsAtStart` | Is At Start. |
| `Item` | Item. |
| `Items` | Items. |
| `NotifySelectorItemsOnly` | Notify Selector Items Only. |
| `OnCountActiveChanged` | On Count Active Changed. |
| `OnFinished` | On Finished. |
| `OnSelectionChanged` | On Selection Changed. |
| `OnSelectionChangedGameObject` | On Selection Changed Game Object. |
| `OnUniqueCycleComplete` | On Unique Cycle Complete. |
| `OnUniqueReset` | On Unique Reset. |
| `SelectorSaveMode` | Selector Save Mode. |
| `UniqueRemainingCount` | Unique Remaining Count. |
| `UniqueSelectionMode` | Unique Selection Mode. |
| `UsedIndicesForUnique` | Used Indices For Unique. |
| `Value` | Value. |
| `_allowEmptyEffectiveIndex` | Allow Empty Effective Index. |
| `_count` | Count. |
| `_currentIndex` | Current Index. |
| `_excludedIndicesInspector` | Excluded Indices Inspector. |
| `_fillMode` | Fill Mode. |
| `_includedIndicesInspector` | Included Indices Inspector. |
| `_indexOffset` | Index Offset. |
| `_items` | Items. |
| `_keepOthersActiveOnRandom` | Keep Others Active On Random. |
| `_notifySelectorItemsOnly` | Notify Selector Items Only. |
| `_saveEnabled` | Save Enabled. |
| `_saveKey` | Save Key. |
| `_saveMode` | Save Mode. |
| `_setChild` | Set Child. |
| `_uniqueSelectionMode` | Unique Selection Mode. |
| `_useNextPreviousAsRandom` | Use Next Previous As Random. |
| `_useRandomSelection` | Use Random Selection. |
| `_usedIndicesForUniqueInspector` | Used Indices For Unique Inspector. |
| `startOnAwake` | Start On Awake. |

## See Also

- [Module Root](../README.md)
