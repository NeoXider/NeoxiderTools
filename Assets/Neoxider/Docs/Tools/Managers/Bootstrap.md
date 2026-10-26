# Bootstrap

## Overview
`Bootstrap` is the initialization coordinator for services and managers that implement `IInit`.

- **Namespace**: `Neo.Tools`
- **Path**: `Assets/Neoxider/Scripts/Tools/Managers/Bootstrap.cs`

It can initialize components from a manual list, discover them automatically in loaded scenes, and process later runtime registrations through the same priority-based pipeline. `Register(IInit)` also accepts plain C# services; they do not need to inherit `MonoBehaviour`.

## How to use
1. Add `Bootstrap` to the scene.
2. Put critical services into `Manual Initializables` for explicit control.
3. Enable `Auto Find Components` when you want discovery across loaded scenes, including inactive objects.
4. Implement `IInit` on services that need ordered initialization.
5. Use `Register(IInit)` and `Unregister(IInit)` for runtime-managed services.

## Inspector fields
- `Manual Initializables`: manually assigned initialization targets.
- `Auto Find Components`: includes loaded scene `MonoBehaviour` instances that implement `IInit`, including inactive objects. Discovery runs during the initial bootstrap pass, rather than on every scene load.

## Initialization flow
1. Manual targets are collected.
2. Optional scene discovery runs.
3. Collected targets are sorted by `InitPriority` descending.
4. `Init()` is called once for each pending registration.
5. Targets registered by an `Init()` callback run in a subsequent sorted batch before the outer initialization call returns. The current batch finishes first, even if a newly registered target has a higher priority.
6. After the first bootstrap pass, later `Register()` calls still go through the same ordered execution path. Callbacks do not recursively initialize the currently running service.

Equal priorities retain collection order. Automatic discovery uses Unity's unsorted enumeration, so use distinct priorities or explicit registration when tie order matters. Bootstrap persists across scene loads; place it on a root GameObject.

## Runtime registration
- `Register(IInit)` adds a service or component to bootstrap tracking. Duplicate registrations, null, and destroyed Unity objects are ignored.
- If bootstrap has already finished its first pass, the new component is initialized immediately through the pending-registration pipeline.
- `Unregister(IInit)` removes the target and its initialized status. A target removed before its turn is skipped, even if it was already in the current batch. Registering it again allows another initialization.
- A Unity target destroyed by an earlier callback is skipped. Dead Unity registrations are pruned on the next drain; plain C# services require explicit `Unregister()` when their lifetime ends.
- Exceptions from `Init()` propagate to the caller and stop the pass. Already successful targets remain initialized. After a completed startup, a failed runtime registration can be explicitly unregistered and registered again to retry. A failed initial startup has no automatic recovery API.

## Typical use cases
- startup ordering for service facades, UI roots, or adapters that implement `IInit`;
- scenes with multiple managers that should not rely on implicit `Awake` timing;
- optional systems that appear later and still need consistent initialization behavior.

`GM`, `EM`, and other singleton managers are not automatically `IInit` targets. Bootstrap does not change their Unity `Awake` order; use an explicit adapter when ordered work is required.

## Example
```csharp
using Neo.Tools;

public sealed class SaveStartupService : IInit
{
    public int InitPriority => 100;
    public bool IsReady { get; private set; }

    public void Init()
    {
        IsReady = true;
    }
}
```

Register through an explicitly supplied `Bootstrap` reference:

```csharp
SaveStartupService service = new SaveStartupService();
bootstrap.Register(service);
// At the end of this service's lifetime:
bootstrap.Unregister(service);
```

For scene authoring, implement the same `IInit` interface on a `MonoBehaviour` and assign it to `Manual Initializables`. Scene UnityEvents can call an adapter's typed methods; initialization logic stays in `IInit.Init()`.

## See also
- [`Singleton`](./Singleton.md)
- [`README`](./README.md)
