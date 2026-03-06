# Reactive module

The `Reactive` module provides serializable reactive properties for `float`, `int`, and `bool` values with typed `UnityEvent` callbacks.

## Included types

- `ReactivePropertyFloat`
- `ReactivePropertyInt`
- `ReactivePropertyBool`
- `UnityEventFloat`, `UnityEventInt`, `UnityEventBool`

## When to use it

- You want a lightweight reactive value stored directly in a serialized Unity field.
- You want Inspector-friendly change callbacks for primitive values.
- You need to restore a value from save data without firing change listeners immediately.

## Shared API

All three property types expose the same core API:

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

## Notes

- This is not a full observable framework with operators or streams.
- The current module only includes `float`, `int`, and `bool` implementations.
- These are serializable helper types, not standalone `MonoBehaviour` components.

## See also

- Russian docs: [`../../Docs/Reactive/README.md`](../../Docs/Reactive/README.md)
- [Save](../Save/README.md)
- [Condition](../Condition/README.md)
