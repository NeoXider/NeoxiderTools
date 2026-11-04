# Extensions — Additional Types

**What it is:** types of the `Neo.Extensions` assembly that have no page of their own.

**How to use:** see the description of each type below.

---

| Type | Description |
|------|-------------|
| `CreateFromMenuAttribute` (namespace `Neo`) | Marks a MonoBehaviour for the editor's **GameObject → Neoxider** menu. `MenuPath` is the menu entry; optional `PrefabPath` (relative to the package root) instantiates that prefab instead of an empty GameObject with the component. |
| `LegacyComponentAttribute` (namespace `Neo`) | Marks a component as legacy while keeping it working in existing scenes. `Replacement` names the recommended component; `HideFromCreateMenu` (default true) hides it from the Neoxider create window. |
| `NumberFormatExtensions` | Number formatting (`ToPrettyString`, `ToIdleString`): plain, grouped, idle-short (K/M/B…) and scientific notation. See the [module README](README.md#number-formatting) and [NumericEdgeCases](NumericEdgeCases.md). |
| `NeoDiagnostics` (global namespace) | Shared runtime logging gate. See [Diagnostics](../Core/Diagnostics.md). |
| `CoroutineRunner`, `CoroutineHelper`, `CoroutineLifecycleTracker` | Hidden helper components created by [CoroutineExtensions](CoroutineExtensions.md); do not add them manually. |

## See Also
- ← [Extensions](README.md)
