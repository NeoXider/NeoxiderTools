# ModifyCounterByKey

**What it is:** a NoCode component that finds a `Counter` or `Money` instance by save key and applies a numeric operation.

**Where:** `Assets/Neoxider/Scripts/Tools/Components/ModifyCounterByKey.cs`, menu `Neoxider/Tools/ModifyCounterByKey`.

---

## Purpose

Use this component when a button, trigger, reward, or network event should modify a named counter without a direct scene reference. It first checks the `Money` singleton by save key, then falls back to registered `Counter` instances.

## Inspector

| Field | Description |
|------|-------------|
| `targetSaveKey` | Save key of the target `Counter` or `Money`. |
| `operation` | Add, Subtract, Multiply, Divide, or Set. |
| `value` | Numeric value used by the operation. |

## API

| Method | Use |
|------|-----|
| `Execute()` | Finds the target and applies the configured operation. |

## Network Note

When Mirror is active and multiple counters share a key, the component prefers the counter owned by the local client before falling back to the first registered counter.

## See Also

- [Counter](Counter.md)
- [Money](../../Shop/Money.md)
