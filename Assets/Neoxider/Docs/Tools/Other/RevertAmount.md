# RevertAmount

**Purpose:** A micro-utility that inverts an incoming value (`1 - amount`) and forwards it via a `UnityEvent<float>`. Useful for inverting progress (e.g., making an HP bar deplete when a timer counts up).

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| *(no inspector fields beyond the event)* | |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Amount(float amount)` | Takes a value, computes `1 - amount`, and fires `OnChange`. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnChange` | `float` | Fired with the inverted value (1 - amount). |

## Examples

### No-Code Example (Inspector)
A timer sends progress (0→1) to `RevertAmount.Amount()`. Wire `OnChange` to `Image.fillAmount`. Now the bar depletes (1→0) even though the timer counts up.

### C# API

```csharp
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

RevertAmount invert = gameObject.AddComponent<RevertAmount>();
invert.OnChange = new UnityEvent<float>();
invert.OnChange.AddListener(value => Debug.Log(value));
invert.Amount(0.25f); // Emits 0.75.
```

`Amount` does not clamp or store its input: `Amount(1.5f)` emits `-0.5f`. Configure any required range limit at the producer/consumer. A missing `OnChange` event is allowed, and disabling the component does not prevent direct C# calls.

## See Also
- ← [Tools/Other](README.md)
