# AnimatorParameterDriver

**What it is:** a sealed `MonoBehaviour` that sets Animator parameters (trigger, bool, float, int) from code or
UnityEvents, by explicit name or through names configured in the Inspector. Script:
`Scripts/Tools/Components/AnimatorParameterDriver.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Components`.

**How to use:**
1. Add **Neoxider / Tools / AnimatorParameterDriver** next to the `Animator` (or assign **Animator**).
2. Fill the parameter-name fields you want to drive without arguments (e.g. **Float Parameter Name** = `Speed`).
3. Wire UnityEvents: `SetTrigger()`, `SetBool(bool)`, `SetFloat(float)`, `SetInt(int)` use the configured names;
   `SetTrigger(string)`, `SetBoolTrue(string)`, `SetBoolFalse(string)` take the name as the argument.

---

## Fields

| Field | Description |
|-------|-------------|
| `animator` | Target Animator; when empty, the Animator on the same GameObject is used. |
| `triggerParameterName` | Trigger fired by `SetTrigger()`. |
| `boolParameterName` | Bool set by `SetBool(bool)`. |
| `floatParameterName` | Float set by `SetFloat(float)`. |
| `intParameterName` | Integer set by `SetInt(int)`. |

## Methods

| Method | Description |
|--------|-------------|
| `SetTrigger()` / `SetTrigger(string)` | Fire the configured / named trigger. |
| `SetBool(bool)` / `SetBool(string, bool)` | Set the configured / named bool. |
| `SetBoolTrue(string)` / `SetBoolFalse(string)` | One-argument helpers for UnityEvents. |
| `SetFloat(float)` / `SetFloat(string, float)` | Set the configured / named float. |
| `SetInt(int)` / `SetInt(string, int)` | Set the configured / named integer. |
| `Animator` | Gets or sets the target Animator. |

Calls without an Animator or with an empty name do nothing. The configured-name methods hash each name once and reuse
it (the hash follows Inspector edits), so they are cheap enough for per-frame events such as movement speed.

## Example

```csharp
AnimatorParameterDriver driver = GetComponent<AnimatorParameterDriver>();
driver.SetFloat(rigidbody.linearVelocity.magnitude); // uses Float Parameter Name
driver.SetTrigger("Jump");
```

## See also

- [Components](README.md)
