# NoCodeFloatBindingBehaviour

**What it is:** abstract base class shared by NoCode float-binding components (`NoCodeBindText`, `SetProgress`) — wires a `ComponentFloatBinding` to a target field/property, with either reactive-property subscription or interval polling. Path: `Scripts/NoCode/NoCodeFloatBindingBehaviour.cs`, namespace `Neo.NoCode`.

**How to use:** don't add this class directly — inherit from it to build a new NoCode float-display/binding component. Override `ApplyFloat(float value)` to consume the resolved value (e.g. drive a Slider or Image fill amount); the base class subscribes to `ReactiveProperty*` sources automatically, falling back to polling at **Poll Interval Seconds** when the source isn't reactive. From C#, configure `Binding` and call `RefreshBinding()`.

---

## Fields

| Field | Description |
|-------|-------------|
| **Binding** (`ComponentFloatBinding`) | The source component/field/property to read the float from. |
| **Update Mode** | `Once` (resolve and apply a single time on enable), `Reactive` (subscribe to a `ReactiveProperty*` source when available, falling back to polling — including while a `Find By Name` / `Wait For Object` source has not spawned yet), or `Poll` (always poll on an interval). |
| **Poll In Late Update** | Whether polling runs in `LateUpdate` (default `true`). |
| **Poll Interval Seconds** | Seconds between refreshes in Poll mode / reactive fallback (default `0.16`, minimum `0.016`). |

## API

| Member | Description |
|--------|-------------|
| `Binding` | Exposes the underlying `ComponentFloatBinding` (read-only reference; its setters apply on the next refresh). |
| `UpdateMode` | Get/set the update mode at runtime; switching releases or creates the reactive subscription immediately when active. |
| `PollIntervalSeconds` | Get/set the poll interval (clamped to at least `0.016`). |
| `RefreshBinding()` | Public: reads the source now and applies it. Use after rewiring `Binding`, or to refresh `Once` bindings. UnityEvent-callable. |
| `ApplyFloat(float value)` (abstract, override in a subclass) | Called on every refresh with the current value. Output callbacks may rewire the binding; the base re-syncs the subscription afterwards. |
| `RefreshFromSource()` (protected) | The refresh implementation behind `RefreshBinding()`. |

## Runtime configuration

The `Binding` object exposes `UseSceneSearch`, `SearchObjectName`, `SourceRoot`, `ComponentTypeName`, `ComponentOccurrence` and `MemberName` setters. After changing these, call `RefreshBinding()` or disable/re-enable the component. A refresh of a plain or polled source reads it once. A subscribed reactive source is read once more after `ApplyFloat` to catch an instance replaced by an output callback, so a reactive change costs two reads (reactive reads do not box). Each refresh reconciles the actual reactive instance: obsolete listeners are removed before subscribing to a different source or value type. An ordinary numeric source uses the polling fallback. Occurrence counts only matching types (`0` = first match); a missing/negative occurrence is unreadable rather than falling back to the first component.

```csharp
using Neo.NoCode;
using UnityEngine;

public sealed class BoundOpacity : NoCodeFloatBindingBehaviour
{
    [SerializeField] private CanvasGroup _target;

    protected override void ApplyFloat(float value)
    {
        if (_target != null)
        {
            _target.alpha = Mathf.Clamp01(value);
        }
    }
}
```

Configure this view's inherited **Binding** in the Inspector, or set its binding properties in C# and call `RefreshBinding()`. If overriding lifecycle methods (`OnEnable`, `OnDisable`, `OnValidate`, `LateUpdate`), call the base implementation to preserve cleanup and initialization. `ApplyFloat` runs on every refresh (including polls with an unchanged value); skip redundant work inside it when the output is expensive, as `NoCodeBindText` does.

Subscriptions exist only while `isActiveAndEnabled` is true. A manual refresh while disabled performs a one-time read without subscribing. Reactive callbacks already in flight after another listener disables the view are ignored. Polling uses unscaled time; **Poll In Late Update** controls both Poll mode and the Reactive fallback. An unreadable source leaves the existing target state intact.

`OnValidate` clamps the interval and marks configuration dirty; it does not read the bound member. The next active `LateUpdate`, or an explicit refresh, invalidates cached resolution and reconciles subscriptions on the main thread before applying the polling gate. Once remains a single enable-time read unless refreshed explicitly. Interval edits keep ordinary-source fallback working, including after an `ApplyFloat`/Slider callback switches the binding from a reactive member to an ordinary numeric field.

## See also

- [ComponentFloatBinding](./ComponentFloatBinding.md)
- [NoCode README](./README.md)
