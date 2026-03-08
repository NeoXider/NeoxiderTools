# Bootstrap

## Overview
`Bootstrap` is the initialization coordinator for services and managers that implement `IInit`.

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Managers/Bootstrap.cs`

It can initialize components from a manual list, discover them automatically in the scene, and process later runtime registrations through the same priority-based pipeline.

## How to use
1. Add `Bootstrap` to the scene.
2. Put critical services into `Manual Initializables` for explicit control.
3. Enable `Auto Find Components` when you want scene-wide discovery of `IInit` components.
4. Implement `IInit` on services that need ordered initialization.
5. Use `Register(IInit)` and `Unregister(IInit)` for runtime-managed services.

## Inspector fields
- `Manual Initializables`: manually assigned initialization targets.
- `Auto Find Components`: includes all scene `MonoBehaviour` instances that implement `IInit`.

## Initialization flow
1. Manual targets are collected.
2. Optional scene discovery runs.
3. Collected targets are sorted by `InitPriority` descending.
4. `Init()` is called for pending targets.
5. After the first bootstrap pass, later `Register()` calls still go through the same ordered execution path.

## Runtime registration
- `Register(IInit)` adds a component to bootstrap tracking.
- If bootstrap has already finished its first pass, the new component is initialized immediately through the pending-registration pipeline.
- `Unregister(IInit)` removes the component from tracking.

## Typical use cases
- startup ordering for `GM`, `EM`, `SaveManager`, UI roots, or service facades;
- scenes with multiple managers that should not rely on implicit `Awake` timing;
- optional systems that appear later and still need consistent initialization behavior.

## Example
```csharp
using Neo.Tools;
using UnityEngine;

public class SaveStartupService : MonoBehaviour, IInit
{
    public int InitPriority => 100;

    public void Init()
    {
        Debug.Log("Save system initialized before gameplay services.");
    }
}
```

## See also
- [`Singleton`](./Singleton.md)
- [`README`](./README.md)
