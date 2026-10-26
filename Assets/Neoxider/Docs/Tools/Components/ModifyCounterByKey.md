# ModifyCounterByKey

**Purpose:** A NoCode component that finds a `Counter` or `Money` instance by save key and applies a numeric operation.

**Location:** `Assets/Neoxider/Scripts/Tools/Components/ModifyCounterByKey.cs`, menu `Neoxider/Tools/ModifyCounterByKey`.

---

## Use Case

Use this component when a button, trigger, reward, or network event should modify a named counter without a direct scene reference. It first checks the `Money` singleton by save key, then falls back to registered `Counter` instances.

## Inspector Fields

| Field | Description |
|---|---|
| `targetSaveKey` | Save key of the target `Counter` or `Money`. |
| `operation` | Add, Subtract, Multiply, Divide, or Set. |
| `value` | Numeric value used by the operation. |

## API

| Method | Description |
|---|---|
| `Execute()` | Finds the target and applies the configured operation. |

For explicit C# dependencies, call `Counter.Add/Set/Multiply` or the corresponding `Money` API directly.
`Execute()` is useful when an Inspector-authored button or trigger needs key-based routing. Money takes
priority over counters with the same key. Counter registrations last while the component is enabled;
after editing a live counter save key, disable and re-enable that counter before routing by the new key.

## Network Note

When Mirror is active and multiple counters share a key, the component prefers the counter owned by the local client before falling back to the first registered counter.

## See Also

- [Counter](Counter.md)
- [Money](../../Shop/Money.md)
