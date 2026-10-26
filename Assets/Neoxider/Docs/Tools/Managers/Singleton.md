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
- `Instance` (alias of `I`)
- `IsInitialized`
- `HasInstance`
- `TryGetInstance(out T instance)`
- `DestroyInstance()`

## Resolution rules
- On first unresolved access, `I` scans loaded scene objects of type `T`, including inactive objects.
- It selects the first object whose `Set Instance On Awake` flag is enabled.
- If no suitable object is found and `CreateInstance == true`, a new GameObject is created automatically.
- Duplicate scene objects with `Set Instance On Awake` enabled are destroyed in `Awake()`.
- Scene enumeration is unsorted; do not depend on a particular winner when multiple unresolved candidates exist.
- Lazy resolution initializes the selected instance and applies `DontDestroyOnLoad` when enabled, even before its Unity `Awake`. Later `Awake` does not initialize it a second time when the override calls `base.Init()`.
- Put persistent managers on root GameObjects, as required by Unity's `DontDestroyOnLoad` API.
- A failed search is cached while `CreateInstance` is disabled. A registering `Awake` or the next runtime static reset clears the miss. `I` can return null if no manager is available.

`IsInitialized` and `HasInstance` both report whether a live instance is cached. They do not report whether a derived initialization callback has finished. `TryGetInstance` also observes only this cache.

## Lifecycle and destruction

Override `Init()` for startup and call `base.Init()`. If overriding `Awake()` or `OnDestroy()`, call the base implementation to retain registration, persistence, and cleanup. A duplicate must not run additional startup logic after `base.Awake()`; derived classes can check `IsCurrentSingletonInstance`.

`OnInstanceCreated()` runs only for the auto-create path. Unity can run `Awake()` / `Init()` while `AddComponent<T>()` executes, before `OnInstanceCreated()`; this hook is not a configuration step before initialization.

`DestroyInstance()` clears the cache and schedules the GameObject for destruction. The resolver excludes that pending object immediately. A subsequent `I` returns another eligible instance, null, or a newly created manager when `CreateInstance` is enabled. Destruction of the old object does not clear a replacement's cache.

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

`SingletonRuntimeReset` clears cached instances and failed searches at subsystem registration, including indirect subclasses sharing a closed generic base. `CreateInstance` is configuration and is not reset. Validate reload-disabled and IL2CPP startup behavior in the consuming project's Unity test/build matrix.

## See also
- [`Bootstrap`](./Bootstrap.md)
- [`README`](./README.md)
