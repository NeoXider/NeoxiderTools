# Singleton<T>

## Overview
`Singleton<T>` is the generic base class for managers and services that need a single active instance and a shared access point.

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Managers/Singleton.cs`

## How to use
1. Inherit from `Singleton<YourType>`.
2. Place the component on the scene.
3. Override `Init()` when you need custom startup logic and call `base.Init()`.
4. Use `TryGetInstance(out T)` or `HasInstance` for optional dependencies.

## Public API
- `CreateInstance`
- `I`
- `IsInitialized`
- `HasInstance`
- `TryGetInstance(out T instance)`
- `DestroyInstance()`

## Resolution rules
- On first access, `I` scans all objects of type `T`.
- It selects the first object whose `Set Instance On Awake` flag is enabled.
- If no suitable object is found and `CreateInstance == true`, a new GameObject is created automatically.
- Duplicate scene objects with `Set Instance On Awake` enabled are destroyed in `Awake()`.

## `I` vs `TryGetInstance(out T)`
Use `I` when the singleton must exist.

Use `TryGetInstance(out T)` when:
- the dependency is optional;
- you do not want an implicit scene lookup or auto-create path;
- the component should gracefully work without that manager.

## Example
```csharp
using Neo.Tools;
using UnityEngine;

public class MyManager : Singleton<MyManager>
{
    protected override void Init()
    {
        base.Init();
        Debug.Log("MyManager initialized.");
    }
}
```

Safe access:

```csharp
if (MyManager.TryGetInstance(out MyManager manager))
{
    Debug.Log("Manager is available.");
}
```

## Unity runtime initialization
Do not place `[RuntimeInitializeOnLoadMethod]` on static methods **declared inside** a type that inherits `Singleton<T>` — Unity reports an error (“method … is in a generic class”). Use a separate non-generic static bootstrap class (same pattern as **`SingletonRuntimeReset`**, **`SaveManagerSubsystemRegistration`**, **`MouseInputManagerSubsystemRegistration`**) or call into an `internal static` reset helper from such a class.

## See also
- [`Bootstrap`](./Bootstrap.md)
- [`README`](./README.md)
