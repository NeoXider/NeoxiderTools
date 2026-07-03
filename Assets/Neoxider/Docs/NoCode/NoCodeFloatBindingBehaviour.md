# NoCodeFloatBindingBehaviour

**What it is:** abstract base class shared by NoCode float-binding components — wires a `ComponentFloatBinding` to a target field/property, with either reactive-property subscription or interval polling. Path: `Scripts/NoCode/NoCodeFloatBindingBehaviour.cs`, namespace `Neo.NoCode`.

**How to use:** don't use this class directly — inherit from it to build a new NoCode float-display/binding component. Override `ApplyFloat(float value)` to consume the resolved value (e.g. drive a Slider or Image fill amount); the base class handles subscribing to `ReactiveProperty*` sources automatically, falling back to polling at **Poll Interval Seconds** when the target isn't reactive.

---

## Fields

| Field | Description |
|-------|-------------|
| **Binding** (`ComponentFloatBinding`) | The target component/field/property to read the float from. |
| **Update Mode** | `Reactive` (subscribe to a `ReactiveProperty*` source when available, falling back to polling) or `Poll` (always poll on an interval). |
| **Poll In Late Update** | Whether polling runs in `LateUpdate` (default `true`). |
| **Poll Interval Seconds** | Seconds between refreshes in Poll mode / reactive fallback (default `0.16`, minimum `0.016`). |

## API

| Member | Description |
|--------|-------------|
| `Binding` | Exposes the underlying `ComponentFloatBinding` (read-only). |
| `ApplyFloat(float value)` (abstract, override in a subclass) | Called whenever the bound value changes or is polled. |
| `RefreshFromSource()` (protected) | Forces an immediate re-read of the bound value. |

## See also

- [ComponentFloatBinding](./ComponentFloatBinding.md)
- [NoCode README](./README.md)
