# SelectorModel

**What it is:** `SelectorModel` is the plain C# selection rule class used by `Selector`
(`Scripts/Tools/View/SelectorModel.cs`, namespace `Neo.Tools`): index bounds, loop, offset, fill mode, random,
unique and excluded indices, without any `GameObject` state.

**How to use:** create `new SelectorModel()`, call `Configure(...)` once, then drive it with `Set`, `Next`,
`Previous`, `SetFirst`, `SetLast` and `SetRandom`, and read `CurrentIndex` / `IsItemActive(i)` to update your own
view. See the [minimal example](#minimal-example).

---

## Use It When

- You need index selection without `MonoBehaviour`.
- The logic belongs in a service, view-model, test, UI presenter, or runtime system with no scene object references.
- You need random, unique, excluded indices, fill mode, loop, offset, and empty effective index without controlling `GameObject` state.

## What Selector Still Owns

`Selector` remains the compatible MonoBehaviour wrapper. Existing serialized fields, public API, UnityEvents, `GameObject`/`SelectorItem` application, SaveProvider persistence, and Mirror synchronization stay on the component.

## Minimal Example

```csharp
SelectorModel model = new();
model.Configure(
    count: 5,
    currentIndex: 0,
    indexOffset: 0,
    loop: true,
    fillMode: false,
    allowEmptyEffectiveIndex: false,
    uniqueSelectionMode: true,
    resetUniqueWhenCycleComplete: true,
    excludedIndices: null,
    usedIndicesForUnique: null);

model.Set(2);
model.ExcludeIndex(4);
model.SetRandom(deactivateOthers: true, randomRange: null);

int index = model.CurrentIndex;
int activeCount = model.GetLogicalActiveCount();
bool secondItemOn = model.IsItemActive(1);
```

## Item activation rule

`IsItemActive(itemIndex)` applies the rule `Selector` uses for its items, so a custom view built on the
model switches the same items on. `itemIndex` is an item position (logical index plus `IndexOffset`), not a
logical index. It returns false when `Count` is 0 or the effective index is -1. Otherwise only the effective
item is active, or, in fill mode, every item from 0 up to and including it. Like the other bounds-dependent
members it throws `ArgumentOutOfRangeException` (parameter `indexOffset`) when the logical bounds do not
fit in `Int32`.

## Boundary and unique-selection contracts

Indices are logical: the item position is `CurrentIndex + IndexOffset`. Bounds are
`-IndexOffset .. Count - 1 - IndexOffset`, or start one earlier when empty effective index is allowed.
`Set` loops or clamps according to `Loop`. Exclusions affect random selection; manual selection and
sequential stepping can still select an excluded index.

Bounds, wrapping and step arithmetic use wide intermediates. A singleton at logical `int.MaxValue`
is a finite one-item pool, including random/unique enumeration. `Configure` checks that the complete
logical bounds fit in `Int32` before changing configuration; incompatible count/offset combinations
throw `ArgumentOutOfRangeException`. If configuring individual properties, bounds-dependent operations
perform the same check. `GetEffectiveIndex()` clamps a wide raw sum; the raw `IndexWithOffset` int
readback instead throws `OverflowException` when that sum cannot fit. Ordinary indices and offsets keep
their existing behavior. `UniqueRemainingCount` saturates at `int.MaxValue` if the logical pool is larger.

Unique random selection consumes even a pool with just one included index. A request after exhaustion
reports `UniqueCycleComplete`; it either returns no selection change or clears history and reports
`UniqueReset`, according to `ResetUniqueWhenCycleComplete`. `UniqueRemainingCount` is zero with no items.
Inspect the returned `SelectorModelResult` to decide which events to expose in your own adapter.
Passing `ExcludedIndices` / `UsedIndicesForUnique` back to their corresponding setters or `Configure`
preserves those collections. The model does not modify GameObjects, save data or network state.

The two collection replacement setters also accept deferred self-derived inputs (for example, a LINQ
filter over `ExcludedIndices`). They fully enumerate before clearing the destination, so an enumeration
exception leaves that destination set unchanged. Direct self-assignment stays a no-op; null clears the
requested set. This is a per-set replacement contract, not a transaction over arbitrary iterator side
effects or the whole `Configure` call.

## Cost

The getters (`IsAtStart`, `IsAtEnd`, `IsValidIndex`, `GetEffectiveIndex`, `GetLogicalActiveCount`,
`IsItemActive`) and the commands `Set`, `Next`, `Previous`, `SetFirst`, `SetLast` and `SetRandom` do not
allocate. `SetRandom` counts the eligible indices and walks to the picked one, so its time grows with the
pool size, but it builds no candidate list; candidates are visited in ascending order, so a given
`randomRange` result always maps to the same index. Passing `randomRange: null` uses a shared
`System.Random` through a cached delegate, so that path does not allocate either. A `randomRange` callback
that changes the excluded or used sets during the pick does not make `SetRandom` throw: a ticket past the
remaining candidates falls back to the last one, and with no candidate left the current index is kept.
`UniqueRemainingCount` is also O(pool). The replacement setters (`SetExcludedIndices`,
`SetUsedIndicesForUnique`, `Configure`) materialize their input by design, and `GetIncludedIndices()`
returns a new list on every call.

## Component Interop

`Selector.CreateModelSnapshot()` returns a pure snapshot of the component state. Mutating the snapshot does not change the scene component; use the existing `Selector` methods for scene behavior.
