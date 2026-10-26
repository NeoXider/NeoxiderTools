# Reactive module

The `Reactive` module provides small reactive properties: Inspector-friendly wrappers for `float`, `int`, and `bool`, plus code-first generic `ReactiveProperty<T>` for any C# type.

## Included types

- `ReactivePropertyFloat`
- `ReactivePropertyInt`
- `ReactivePropertyBool`
- `ReactiveProperty<T>` for code-first use
- `IReadOnlyReactiveProperty<T>` and `IReactiveProperty<T>` for observation and mutation contracts
- `UnityEventFloat`, `UnityEventInt`, `UnityEventBool`

## When to use it

- You want a lightweight reactive value stored directly in a serialized Unity field.
- You want Inspector-friendly change callbacks for primitive values.
- You need to restore a value from save data without firing change listeners immediately.

## Shared API

All property types expose the same core API:

- `CurrentValue` for read-only access
- `Value` for read/write access with `OnChanged` invocation
- `OnChanged` as a typed `UnityEvent`
- `AddListener(...)`, `RemoveListener(...)`, `RemoveAllListeners()`
- `Subscribe(...)` / `SubscribeWeak(...)` returning an `IDisposable` subscription handle
- `ListenerCount` for live code registrations; reading it prunes dead targets without emitting a value
- `OnNext(value)` to set and notify
- `SetValueWithoutNotify(value)` to set silently
- `ForceNotify()` to emit the current value again

## Example

```csharp
using System;
using Neo.Reactive;
using UnityEngine;

[SerializeField] private ReactivePropertyBool isUnlocked = new(false);
private IDisposable unlockSubscription;

private void OnEnable()
{
    unlockSubscription = isUnlocked.Subscribe(OnUnlockChanged, true);
}

private void OnDisable()
{
    unlockSubscription?.Dispose();
    unlockSubscription = null;
}

private void OnUnlockChanged(bool unlocked)
{
    // Refresh this view with the current state.
}

public void Unlock()
{
    isUnlocked.Value = true;
}
```

Use `IReadOnlyReactiveProperty<T>` when a consumer only observes the value. Code registrations are
deduplicated by delegate: repeating `Subscribe` with the same delegate shares a registration, and disposing
either handle removes it. The explicit-target `SubscribeWeak(target, handler)` overload creates a separate
registration per call. Pass a noncapturing handler, such as `static (self, value) => self.Handle(value)`.

Weak delegate subscriptions fall back to strong retention for multicast delegates or unsupported AOT
invokers. Dispose the handle or call `RemoveListener` explicitly in those cases. Subscribe each instance
method separately when each target needs its own weak lifetime.

## Inspector workflow

Declare a concrete wrapper as a serialized field on your own component, set its initial value, and wire
**On Changed** to scene methods. Runtime changes through `Value` / `OnNext` notify those callbacks;
`SetValueWithoutNotify` restores data silently and `ForceNotify` refreshes the current state. These types
are helpers, so they are not added directly as components. Inspector edits to the serialized backing value
do not call the C# setter. Persistent Inspector callbacks remain when runtime listeners are removed.

See [ReactiveProperty](ReactiveProperty.md) for lifetime, notification, and allocation details.

## Generic

`ReactiveProperty<T>` supports any value/reference type from C# code:

```csharp
private readonly ReactiveProperty<string> state = new("Idle");

private void Awake()
{
    state.AddListener(OnStateChanged);
    state.Value = "Run";
}
```

For Inspector serialization, use the concrete wrappers (`ReactivePropertyFloat`, `ReactivePropertyInt`, `ReactivePropertyBool`) or create your own concrete class on top of `ReactivePropertyBase<T, TEvent>`. Unity does not reliably serialize open generic field types.

## Mirror

`ReactiveProperty<T>` does not make a type networked by itself. For Mirror, keep the authoritative value in a `[SyncVar(hook = ...)]` on a `NetworkBehaviour` and pass hook updates through `NetworkReactivePropertyBridge.SetFromNetwork(...)`.

Important: the generic bridge accepts any `T` on the Reactive API side. Mirror only synchronizes types supported by its SyncVar serializer, or types with custom Mirror serializers registered.

## Notes

- This is not a full observable framework with operators or streams.
- The current module only includes Inspector-friendly wrappers for `float`, `int`, and `bool`.
- These are serializable helper types, not standalone `MonoBehaviour` components.
- Access and notify on the Unity main thread. Code subscriptions use snapshot semantics and isolate
  callback exceptions through `NeoDiagnostics`; `OnChanged` uses UnityEvent's own invocation semantics.
- Collected weak targets and cleanup triggered by `ListenerCount` are silent. Destroyed code targets
  encountered during notification use gated, throttled `NeoDiagnostics` warnings; cleanup does not
  guarantee a warning for each registration.

## See also

- [Save](../Save/README.md)
- [Condition](../Condition/README.md)
