# ReactiveProperty

**Purpose:** A reactive variable (R3-style). Stores a value and fires a `UnityEvent` when it changes. Works in Inspector (No-Code) through concrete wrappers and from code through generic `ReactiveProperty<T>`.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Value** | Current value. Changing it in Inspector fires `OnChanged`. |
| **On Changed** | `UnityEvent<T>` — fires when the value changes. Wire any method in Inspector. |

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `T Value { get; set; }` | Current value. Setter compares with previous; if different — fires `OnChanged`. |
| `T CurrentValue { get; }` | Same value, read-only. |
| `TEvent OnChanged { get; }` | `UnityEvent<T>` — subscribe via Inspector or `AddListener`. |
| `void AddListener(UnityAction<T> call)` | Subscribe from code. |
| `void RemoveListener(UnityAction<T> call)` | Unsubscribe. |
| `void RemoveAllListeners()` | Remove all subscribers. |
| `void OnNext(T value)` | Set value (same as `Value = value`). |
| `void SetValueWithoutNotify(T value)` | Set value **without** firing `OnChanged` (e.g. on load). |
| `void ForceNotify()` | Force-fire `OnChanged` with the current value. |

---

## Built-in Types

| Class | Value Type | Event Type |
|-------|-----------|------------|
| `ReactivePropertyFloat` | `float` | `UnityEventFloat` |
| `ReactivePropertyInt` | `int` | `UnityEventInt` |
| `ReactivePropertyBool` | `bool` | `UnityEventBool` |
| `ReactiveProperty<T>` | any C# type | `UnityEvent<T>` |

`ReactiveProperty<T>` is intended for code-first use. For Inspector fields, use the concrete wrappers or create a non-generic class on top of `ReactivePropertyBase<T, TEvent>`, because Unity does not reliably serialize open generic field types.

## Mirror warning

`ReactiveProperty<T>` is not network-synchronized by itself. For Mirror, keep the authoritative value in a `[SyncVar(hook = ...)]` and call `NetworkReactivePropertyBridge.SetFromNetwork(...)` from the hook method.

The generic bridge accepts any `T` on the Reactive API side, but Mirror SyncVar only works with types supported by the Mirror serializer, or with types that have registered custom serializers.

---

## Examples

### No-Code (Inspector)
1. Add a `ReactivePropertyFloat` field to your component.
2. Set the initial value in **Value**.
3. Wire a method in **On Changed** (e.g. `Slider.value` or `Text.SetText`).
4. When `Value` changes from any script — the wired method fires automatically.

### Code
```csharp
[SerializeField] private ReactivePropertyInt score = new(0);

void Start()
{
    score.AddListener(OnScoreChanged);
    score.Value = 10; // fires OnScoreChanged(10)
}

void OnScoreChanged(int newScore)
{
    Debug.Log($"Score: {newScore}");
}

void LoadFromSave(int savedScore)
{
    score.SetValueWithoutNotify(savedScore); // won't fire event
}
```

---

## See Also
- ← [Reactive](README.md)

## Notification semantics (9.6.2)

`NotifySubscribers` takes a **real snapshot** of code listeners into a reusable buffer: every listener
registered at notification time is invoked exactly once, even when another listener adds/removes
subscriptions inside its callback. Listeners added during a notification only receive the next value.
Not thread-safe — Unity main thread only.
