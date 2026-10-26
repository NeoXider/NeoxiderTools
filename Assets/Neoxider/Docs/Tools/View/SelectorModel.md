# SelectorModel

`SelectorModel` is the plain C# selection rule class used by `Selector`.

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
```

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

Candidate enumeration still scales with pool size and replacement inputs are materialized. These fixes
do not establish zero-allocation getters or a per-frame cost bound for large virtual pools.

## Component Interop

`Selector.CreateModelSnapshot()` returns a pure snapshot of the component state. Mutating the snapshot does not change the scene component; use the existing `Selector` methods for scene behavior.
