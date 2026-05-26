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
var model = new SelectorModel();
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

## Component Interop

`Selector.CreateModelSnapshot()` returns a pure snapshot of the component state. Mutating the snapshot does not change the scene component; use the existing `Selector` methods for scene behavior.
