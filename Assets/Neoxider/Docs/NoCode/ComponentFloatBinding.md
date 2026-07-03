# ComponentFloatBinding

**What it is:** a `[Serializable]` (not a component) that resolves a `float` — or a compatible `ReactiveProperty*` — from a field/property on another component, using `ReflectionCache`. Embedded as a field inside NoCode binding components (e.g. `NoCodeFloatBindingBehaviour` subclasses) rather than added to a GameObject directly. Path: `Scripts/NoCode/ComponentFloatBinding.cs`, namespace `Neo.NoCode`.

**How to use:**
1. Declare it as a `[SerializeField]` on your own binding component, or use it via an existing subclass of `NoCodeFloatBindingBehaviour`.
2. In the Inspector, either assign **Source Root** + pick a component/member, or enable **Use Scene Search** to resolve the object by name (`GameObject.Find`) at runtime.
3. Call `TryReadFloat(owner, out value)` (or let the owning `NoCodeFloatBindingBehaviour` do it) to pull the current value.

---

## Fields

| Field | Description |
|-------|-------------|
| **Use Scene Search** | Resolve the source object via `GameObject.Find` (same as NeoCondition's "Find By Name") instead of **Source Root**. |
| **Search Object Name** | Name passed to `GameObject.Find` when **Use Scene Search** is on. |
| **Wait For Object** | Suppress the "object not found" warning — useful for prefabs or objects that spawn later. |
| **Find Retry Interval Seconds** | Throttle between `GameObject.Find` retries while the object is missing. `0` = retry every check. |
| **Prefab Preview** | Editor-only prefab used to preview available components/members when the target isn't in the scene. |
| **Source Root** | The GameObject to read the member from (when not using scene search). |

## API

| Member | Description |
|--------|-------------|
| `TryReadFloat(Component owner, out float value)` | Resolves and returns the current float value. |
| `TryGetReactiveProperty(Component owner, out ReactivePropertyFloat, out ReactivePropertyInt, out ReactivePropertyBool)` | Returns the underlying reactive property, if the bound member is one, so the caller can subscribe instead of polling. |
| `Invalidate()` | Clears cached member resolution — call after changing **Source Root**/member at runtime. |

## See also

- [NoCodeFloatBindingBehaviour](./NoCodeFloatBindingBehaviour.md)
- [NoCode README](./README.md)
