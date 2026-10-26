# ComponentFloatBinding

**What it is:** a `[Serializable]` (not a component) that resolves a `float` — or a compatible `ReactiveProperty*` — from a field/property on another component, using `ReflectionCache`. Embedded as a field inside NoCode binding components (e.g. `NoCodeFloatBindingBehaviour` subclasses) rather than added to a GameObject directly. Path: `Scripts/NoCode/ComponentFloatBinding.cs`, namespace `Neo.NoCode`.

**How to use:**
1. Declare it as a `[SerializeField]` on your own binding component, or use it via an existing subclass of `NoCodeFloatBindingBehaviour`.
2. In the Inspector, either assign **Source Root** + pick a component/member, or enable **Use Scene Search** to resolve the object by name (`GameObject.Find`) at runtime.
3. Call `TryReadFloat(host, out value)` (or let the owning `NoCodeFloatBindingBehaviour` do it) to pull the current value.

---

## Fields

| Field | Description |
|-------|-------------|
| **Use Scene Search** | Resolve the source object via `GameObject.Find` (same as NeoCondition's "Find By Name") instead of **Source Root**. |
| **Search Object Name** | Name passed to `GameObject.Find` when **Use Scene Search** is on. |
| **Wait For Object** | Suppress the "object not found" warning — useful for prefabs or objects that spawn later. |
| **Find Retry Interval Seconds** | Throttle between `GameObject.Find` retries while the object is missing. `0` = retry every check. |
| **Prefab Preview** | Editor-only prefab used to preview available components/members when the target isn't in the scene. |
| **Source Root** | The GameObject to read the member from (when not using scene search). If unassigned, the current host's GameObject is used. A destroyed explicit root fails to resolve until replaced or explicitly cleared. |
| **Component Occurrence** | Zero-based occurrence among components matching **Component Type Name**, in `GetComponents` order. `0` selects the first match. Unrelated component types do not consume an occurrence. Negative/missing occurrences fail resolution; they do not select the first component. |

## API

| Member | Description |
|--------|-------------|
| `TryReadFloat(MonoBehaviour host, out float value)` | Resolves and returns the current float value. |
| `TryReadRaw(MonoBehaviour host, out object raw)` | Reads the field/property without numeric conversion. A null member value can be a successful raw read but fails numeric conversion. |
| `EnsureCache(MonoBehaviour host, out string error)` | Resolves the component/member and reports a resolution error without throwing for an ordinary missing source. A null/destroyed host fails. |
| `TryGetReactiveProperty(MonoBehaviour host, out ReactivePropertyFloat, out ReactivePropertyInt, out ReactivePropertyBool)` | Returns the underlying reactive property, if the bound member is one, so the caller can subscribe instead of polling. |
| `SourceRoot`, `ComponentTypeName`, `MemberName` | Source configuration; each setter invalidates cached member resolution on change. |
| `ComponentOccurrence` | Selects a matching component instance, default `0`. The setter invalidates cached resolution. Invalid values are retained and explicitly rejected by `EnsureCache`/read methods. |
| `TryResolveSupportedSourceMember(Type, string, out MemberInfo)` | Resolves readable fields/non-indexed properties. Methods, write-only properties and indexers, including overloaded indexers, are rejected. |
| `Invalidate()` | Clears cached resolution and missing-source diagnostics. Use after changing serialized configuration outside the public setters. |

Resolved members are cached for the current host; reusing a binding with another host resolves against that host's context. Values are read anew, so replacing a reactive property instance is visible on the next read. This object does not subscribe on its own.

Existing serialized descriptors without **Component Occurrence** keep the first-match behavior. This is an additive selector, independent of the Editor's legacy component-list index; an old UI index is never interpreted as an occurrence. Occurrences use the matching-component order at resolution time. After changing component order/composition, call `Invalidate()` and refresh the owning view; the binding does not rescan the component list on every value read.

```csharp
// Inside a MonoBehaviour; sourceObject contains two AudioSource components.
ComponentFloatBinding binding = new()
{
    SourceRoot = sourceObject,
    ComponentTypeName = typeof(UnityEngine.AudioSource).FullName,
    ComponentOccurrence = 1, // The second AudioSource; other component types are ignored.
    MemberName = nameof(UnityEngine.AudioSource.volume)
};
if (binding.TryReadFloat(this, out float volume))
{
    // Consume volume in your view or service adapter.
}
```

The example returns `false` if `sourceObject` has fewer than two AudioSources. Use `ComponentOccurrence = 0` for the first. For a live `NoCodeBindText` or `SetProgress`, change `view.Binding.ComponentOccurrence`, then refresh from a subclass or disable/re-enable the view to transfer its reactive subscription.

Supported numeric reads include float, int, double, supported reactive values (bool maps to 0/1), and values accepted by `Convert.ToSingle`. Conversion or getter failures return `false`; the float output remains zero. Large integers lose precision when converted to float, so use a typed API for exact currency/count values. Reflection-only members need preservation in stripped/IL2CPP builds; verify the consuming project's player build.

## See also

- [NoCodeFloatBindingBehaviour](./NoCodeFloatBindingBehaviour.md)
- [NoCode README](./README.md)
