# Bootstrap

**What it is:** a `Singleton<Bootstrap>` MonoBehaviour that initializes `IInit` services and components in
priority order, at startup and for later runtime registrations. Plain C# services are supported. Script:
`Scripts/Tools/Managers/Bootstrap.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Managers`.

**How to use:**
1. Add **Neoxider / Tools / Bootstrap** to a root GameObject (it persists across scene loads).
2. Implement `IInit` (`InitPriority` + `Init()`) on the services that need ordered initialization.
3. Put critical services into **Manual Initializables**, or enable **Auto Find Components**.
4. From code, call `Register(IInit)` / `Unregister(IInit)` for services with a runtime lifetime.

---

## Inspector fields

| Field | Description |
|-------|-------------|
| `Manual Initializables` | Explicit startup targets (`MonoBehaviour`s implementing `IInit`). |
| `Auto Find Components` | Also collects every loaded `MonoBehaviour` implementing `IInit`, including inactive objects. Discovery runs once, during the startup pass, not on every scene load. |

## Initialization flow

1. Manual targets are collected, then optional scene discovery runs.
2. Pending targets are sorted by `InitPriority` descending; equal priorities keep registration order.
3. `Init()` is called once per registration.
4. Targets registered from inside an `Init()` run in the next sorted batch before the outer call returns.
   The current batch finishes first, even if a newly registered target has a higher priority.
5. After the startup pass, `Register()` initializes new targets immediately through the same pipeline.
   Callbacks never re-enter the service that is currently initializing.

Automatic discovery uses Unity's unsorted enumeration, so use distinct priorities or explicit registration when
tie order matters.

## API

| Member | Description |
|--------|-------------|
| `void Register(IInit target)` | Tracks a target. Null, duplicates and destroyed Unity objects are ignored. After startup it is initialized immediately. |
| `void Unregister(IInit target)` | Removes the target and its initialized flag. A target removed before its turn is skipped; registering it again allows another `Init()`. |
| `void InitializePending()` | Initializes every registered target that has not been initialized yet and retries the failed ones. Use it to recover after a failure. |
| `bool HasFailed(IInit target)` | `true` while the target's last `Init()` threw and it waits for an explicit retry. |
| `bool IsBootstrapCompleted` | `true` once the startup pass has run, even if one of its targets threw. |

Destroyed Unity targets are pruned on the next drain; plain C# services need an explicit `Unregister()` when their
lifetime ends.

## Failures

An exception from `Init()` propagates to the caller of the pass that ran it and stops that pass (fail-fast):
targets that already succeeded stay initialized, the rest of the pass stays pending.

- The failing target is quarantined (`HasFailed(target)` is `true`). Automatic drains, i.e. the startup pass and
  every later `Register()`, skip it: they never re-run its `Init()` side effects and never rethrow its old exception
  from an unrelated `Register()`.
- The startup pass is still marked complete. The next `Register()` initializes the newcomer together with the
  targets the failed pass skipped, in priority order, without the quarantined target.
- Only `InitializePending()` retries a quarantined target; if it throws again, the exception propagates again and
  the target stays quarantined. `Unregister()` drops the failure, and registering the target again starts a new
  attempt.

```csharp
bootstrap.InitializePending(); // after fixing what brokenService needs: retries it and the skipped targets
// or give up on it:
bootstrap.Unregister(brokenService);
```

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

For scene authoring, implement the same `IInit` interface on a `MonoBehaviour` and assign it to
**Manual Initializables**. Scene UnityEvents can call an adapter's typed methods; initialization logic stays in
`IInit.Init()`.

`GM`, `EM` and other singleton managers are not automatically `IInit` targets. Bootstrap does not change their Unity
`Awake` order; use an explicit adapter when ordered work is required.

## See also

- [Singleton](./Singleton.md)
- [Managers](./README.md)
