# Reactive module

The `Reactive` module provides small reactive properties: Inspector-friendly wrappers for `float`, `int`, and `bool`, plus code-first generic `ReactiveProperty<T>` for any C# type.

## Included types

- `ReactivePropertyFloat`
- `ReactivePropertyInt`
- `ReactivePropertyBool`
- `ReactiveProperty<T>` for code-first use
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
- `OnNext(value)` to set and notify
- `SetValueWithoutNotify(value)` to set silently
- `ForceNotify()` to emit the current value again

## Example

```csharp
[SerializeField] private ReactivePropertyBool isUnlocked = new(false);

private void Awake()
{
    isUnlocked.AddListener(OnUnlockChanged);
}

private void OnUnlockChanged(bool unlocked)
{
    Debug.Log($"Unlocked: {unlocked}");
}

public void Unlock()
{
    isUnlocked.Value = true;
}
```

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

## See also

- Russian docs: [`../../Docs/Reactive/README.md`](../../Docs/Reactive/README.md)
- [Save](../Save/README.md)
- [Condition](../Condition/README.md)
