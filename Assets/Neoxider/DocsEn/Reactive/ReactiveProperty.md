# ReactiveProperty

**Purpose:** A serializable reactive variable (R3-style). Stores a value and fires a `UnityEvent` when it changes. Works in both Inspector (No-Code) and from code. Three built-in types: `ReactivePropertyFloat`, `ReactivePropertyInt`, `ReactivePropertyBool`.

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
