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

## See Also
- ← [Tools/Other](README.md)
