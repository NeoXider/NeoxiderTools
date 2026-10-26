# ReactiveProperty

**Purpose:** A reactive variable (R3-style). Stores a value and fires a `UnityEvent` when it changes. Works in Inspector (No-Code) through concrete wrappers and from code through generic `ReactiveProperty<T>`.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Value** | Serialized initial/current backing value. Inspector edits do not call the C# setter. |
| **On Changed** | `UnityEvent<T>` — fires when the value changes. Wire any method in Inspector. |

---

## API

| Method / Property | Description |
|-------------------|-------------|
| `T Value { get; set; }` | Current value. Setter compares with previous; if different — fires `OnChanged`. |
| `T CurrentValue { get; }` | Same value, read-only. |
| `TEvent OnChanged { get; }` | `UnityEvent<T>` for Inspector callbacks; prefer the property's `AddListener` / `Subscribe` in code. |
| `void AddListener(UnityAction<T> call)` | Subscribe from code. |
| `void RemoveListener(UnityAction<T> call)` | Remove matching code registrations and runtime UnityEvent listeners; null is ignored. Persistent Inspector callbacks remain. |
| `void RemoveAllListeners()` | Remove code registrations and runtime UnityEvent listeners. Persistent Inspector callbacks remain. |
| `void OnNext(T value)` | Set value (same as `Value = value`). |
| `void SetValueWithoutNotify(T value)` | Set value **without** firing `OnChanged` (e.g. on load). |
| `void ForceNotify()` | Force-fire `OnChanged` with the current value. |
| `int ListenerCount { get; }` | Number of live code subscriptions (dead ones are pruned first). |
| `IDisposable Subscribe(UnityAction<T> onNext, bool invokeImmediately = false)` | Subscribe and get a handle that unsubscribes on `Dispose()`. |
| `void AddListener(UnityAction<T> call, ReactiveListenerMode mode)` | Subscribe with an explicit lifetime mode. |
| `void AddWeakListener(UnityAction<T> call)` | Subscribe without keeping the target alive. |
| `IDisposable SubscribeWeak(UnityAction<T> onNext, bool invokeImmediately = false)` | Weak counterpart of `Subscribe`. |
| `IDisposable SubscribeWeak<TTarget>(TTarget target, Action<TTarget, T> handler, bool invokeImmediately = false)` | Reflection-free weak subscription; use a noncapturing handler for IL2CPP/AOT. Setup still allocates. |

---

## Subscription lifetime

Code subscriptions (`AddListener` / `Subscribe` and their weak variants) come with three guarantees:

- **Every listener registered when a notification starts is invoked exactly once**, with the value captured
  at that moment. Adding, removing, or assigning `Value` again from inside a callback does not change what
  the other listeners of that notification receive.
- **A listener that throws is logged and skipped.** The remaining listeners still run, and the exception
  never reaches the gameplay code that set the value.
- **A subscription whose target is gone is dropped, not thrown on.** A listener owned by a destroyed
  `UnityEngine.Object` is unsubscribed automatically. During notification it is reported through a
  throttled warning naming the method when `NeoDiagnostics` warnings are enabled; collected weak targets
  are dropped silently. Reading `ListenerCount` also prunes dead code targets without notifying or logging.

Cleanup does not guarantee a warning for each registration: a count read can remove a destroyed target
before the next notification, and the shared diagnostic throttle can suppress warnings with the same key.
Use `ListenerCount` to inspect code registrations rather than relying on a cleanup log. Inspector callbacks
remain on the separate `OnChanged` UnityEvent path described below.

`invokeImmediately: true` uses the same liveness check and exception isolation in both weak overloads and
in `Subscribe`. A failing initial callback still returns a usable disposal handle. Exception logging follows
the `NeoDiagnostics` error gate. `OnChanged` is invoked after the code snapshot and keeps UnityEvent's own
callback order and exception behavior: a throwing UnityEvent callback can stop later UnityEvent callbacks.

### Choosing a mode

| Situation | Use |
|-----------|-----|
| The subscriber owns the property, or you unsubscribe in `OnDestroy` | `AddListener` (default, strong) |
| You want a handle instead of remembering the delegate | `Subscribe(...)` and `Dispose()` the result |
| The property outlives the subscriber and you may forget to unsubscribe | `AddWeakListener` / `SubscribeWeak` |
| IL2CPP/AOT without reflective open-delegate creation | `SubscribeWeak(this, static (self, v) => self.Handle(v))` |

`ReactiveListenerMode.Weak` keeps only a weak reference to the delegate target, so a forgotten
`RemoveListener` cannot leak the subscriber - the subscription drops itself once the target is collected.
Only pass delegates whose target you keep alive yourself (a method group on a field or component). A
closure that captures locals has no other owner and may be collected immediately.

Weak dispatch goes through a cached open-instance delegate, so it costs a plain delegate call rather than
reflection. On AOT platforms that refuse the required generic instantiation, or for multicast delegates,
it degrades to a strong subscription and emits a gated, throttled warning. The complete multicast delegate
is retained and invoked; dispose its handle or call `RemoveListener` explicitly. Register each method
separately to give each subscriber its own weak lifetime and exception boundary.

The explicit-target overload avoids reflection, but retains `handler` strongly: do not capture the target
inside that handler. Both forms allocate subscription state during setup. Delegate-based registrations are
deduplicated; two `Subscribe` calls with the same delegate share a registration, so disposing either handle
removes it. Explicit-target subscriptions are independent registrations. `ListenerCount` counts code
registrations only, excluding `OnChanged` listeners; one multicast delegate counts as one registration.

```csharp
public class HealthBar : MonoBehaviour
{
    [SerializeField] private ReactivePropertyFloat _health = new(100f);
    private IDisposable _subscription;

    private void OnEnable()
    {
        // Delivers the current value right away, and unsubscribes on Dispose.
        _subscription = _health.Subscribe(OnHealthChanged, true);
    }

    private void OnDisable()
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    private void OnHealthChanged(float value) { /* ... */ }
}
```

Expose `IReadOnlyReactiveProperty<T>` to consumers that only need `CurrentValue`, `ListenerCount`, and
subscription management. `IReactiveProperty<T>` adds `Value`, `SetValueWithoutNotify`, and `ForceNotify`.
The explicit-target weak overload is available on the concrete property/base, not on those interfaces.

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
4. Change `Value` / call `OnNext` from code or an existing component API to notify the wired methods.
5. After `SetValueWithoutNotify` (for example, loading a save), call `ForceNotify` when the view should refresh.

Inspector changes to the serialized backing value do not invoke the `Value` setter. Removing listeners at
runtime does not remove persistent callbacks wired in the Inspector.

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
subscriptions inside its callback. Listeners added during a notification can receive the next notification,
including a nested notification started by a reentrant assignment.
Not thread-safe — Unity main thread only.

## Performance and naming notes

- The `Value` setter compares via `EqualityComparer<T>.Default`: custom structs without
  `IEquatable<T>` box on every set — implement `IEquatable<T>` for hot values.
- Notification reuses a snapshot buffer after its initial allocation; nested notifications allocate their
  own buffer. Reading `ListenerCount` scans code registrations, so avoid polling it in a hot loop.
- "R3-style" means familiar naming (`OnNext`, `CurrentValue`, `ForceNotify`), not R3/IObservable
  compatibility: the module has disposable subscriptions, but no observable operator pipeline.
