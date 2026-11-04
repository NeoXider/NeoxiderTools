# RevertAmount

**What it is:** a NoCode micro-utility `MonoBehaviour` that inverts an incoming value (`1 - amount`) and forwards it
via a `UnityEvent<float>`, e.g. to make a bar deplete while a timer counts up. Script:
`Scripts/Tools/Other/RevertAmount.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Other`.

**How to use:**
1. Add **Neoxider / Tools / RevertAmount** between a 0..1 producer and its consumer.
2. Wire the producer's float event to `Amount(float)`.
3. Wire **On Change** to the consumer (for example `Image.fillAmount`).

---

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
