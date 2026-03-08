# SaveableBehaviour

## Overview
`SaveableBehaviour` is the simplest way to connect a scene component to the `Save` module.

- **Namespace**: `Neo.Save`
- **Path**: `Assets/Neoxider/Scripts/Save/SaveableBehaviour.cs`

It already implements `ISaveableComponent` and automatically registers and unregisters itself in `SaveManager`.

## How to use
1. Inherit your component from `SaveableBehaviour`.
2. Mark fields with `[SaveField("key")]`.
3. Override `OnDataLoaded()` when you need post-load refresh logic.
4. Implement `ISaveIdentityProvider` if the component needs a custom stable key.

## Lifecycle behavior
- `OnEnable()` calls `SaveManager.Register(this)`.
- `OnDisable()` calls `SaveManager.Unregister(this)`.
- `OnDataLoaded()` is available as a virtual callback.

This makes it suitable for objects that may be enabled, disabled, or destroyed during gameplay.

## When to use it
Use `SaveableBehaviour` when:
- the component lives on a scene object;
- it needs field-based persistence through `[SaveField]`;
- you want minimal boilerplate.

Implement `ISaveableComponent` manually only when you need custom registration behavior or you cannot inherit from this base class.

## Example
```csharp
using Neo.Save;
using UnityEngine;

public class PlayerScore : SaveableBehaviour
{
    [SaveField("score")] [SerializeField] private int _score;
    [SaveField("best-score")] [SerializeField] private int _bestScore;

    public override void OnDataLoaded()
    {
        Debug.Log($"Loaded score: {_score}, best: {_bestScore}");
    }
}
```

## See also
- [`SaveManager`](./SaveManager.md)
- [`ISaveIdentityProvider`](./ISaveIdentityProvider.md)
- [`README`](./README.md)
