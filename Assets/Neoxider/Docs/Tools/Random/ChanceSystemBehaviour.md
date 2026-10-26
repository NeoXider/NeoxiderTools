# ChanceSystemBehaviour

Scene adapter for [ChanceManager](ChanceManager.md), with result state and UnityEvents. Use namespace `Neo.Tools` and assembly `Neo.Tools.Random` from C#.

## Setup

1. Add **Component > Neoxider > Tools > ChanceSystemBehaviour**.
2. Configure the inline **Manager**, or assign a [ChanceData](Data/ChanceData.md) asset. At `Awake`, the asset's entries and normalization settings are copied into the inline manager, then sanitized and assigned unique custom IDs. The scene table is independent of the asset afterward.
3. Connect a Button or another UnityEvent to `GenerateVoid()` to roll and notify listeners.
4. Configure the result events on the component. Put scene object references here rather than in the data asset.

## Events and result state

| Field | Description |
|-------|-------------|
| `OnIdGenerated` / `OnIndexSelected` | The same event, with the selected list index, not `CustomId`. |
| `OnIndexAndWeightSelected` | Selected index and its normalized probability. |
| `EventsByIndex` | Optional event at each list index; its size may differ from the table size. |
| `OnRollComplete` | Completion notification, including failed rolls. |
| `LastSelectedIndex` / `LastSelectedEntry` | Most recently notified index and entry; initially `-1` / `null`. |
| `Manager` | Runtime table; edits use the same mutation rules as `ChanceManager`. |

For each notified roll, result state is assigned first, then events run in this order: index, index/probability, matching per-index event, completion. A generated roll on an empty or zero-total table still emits index `-1`, probability `0`, and completion; no per-index event runs.

Listeners execute synchronously. A listener exception propagates to the caller and can stop later notifications; `OnRollComplete` runs when the preceding callbacks finish. Nested notifications follow the result-state rules below.

## C# API

```csharp
// chanceSystem is an explicitly supplied ChanceSystemBehaviour reference.
chanceSystem.OnIndexAndWeightSelected.AddListener((int index, float probability) =>
{
    // Update your reward UI using index and probability.
});
chanceSystem.GetOrAddEventForIndex(0).AddListener(() =>
{
    // Award the item at index 0.
});
ChanceManager.Entry selected = chanceSystem.EvaluateAndNotify();
```

- `GenerateId()` rolls, notifies, and returns the index; `GenerateVoid()` is its void UnityEvent adapter.
- `EvaluateAndNotify()` rolls, notifies, and returns that call's selected entry. If a listener performs another roll, the outer call still returns its own entry; `LastSelectedIndex` / `LastSelectedEntry` describe the most recent notified call, including nested calls.
- `GetId()` and `Evaluate()` roll without events or changes to the last notified result.
- `SetResultAndNotify(int index)` publishes a supplied index without rolling or clamping it. Supply a valid manager index, or `-1` for no selection.
- `GetOrAddEventForIndex(int index)` grows the per-index event list. Supply a nonnegative index.
- `AddChance`, `RemoveChance`, `GetChance`, `SetChance`, `ClearChances`, `Normalize`, and `GetNormalizedWeight` forward to the manager.

The optional **Log Debug Once** Inspector flag samples an entry during editor validation through gated diagnostics. It is not a gameplay roll notification.

## See Also

- [Module Root](README.md)
