# ModifyCounterByKey

**What it is:** a NoCode `MonoBehaviour` that finds a `Counter` or `Money` instance by save key and applies a
configured numeric operation. Script: `Scripts/Tools/Components/ModifyCounterByKey.cs`, namespace `Neo.Tools`,
assembly `Neo.Tools.Components`.

**How to use:**
1. Add **Neoxider / Tools / ModifyCounterByKey** to a button, trigger or reward object.
2. Set **Target Save Key**, **Operation** and **Value**.
3. Call `Execute()` from a UnityEvent (for example `Button.onClick`).

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
