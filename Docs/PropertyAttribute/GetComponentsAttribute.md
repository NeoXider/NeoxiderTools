# GetComponentsAttribute

**What it is:** `[GetComponents]` (`Scripts/PropertyAttribute/InjectAttribute/GetComponentsAttribute.cs`, global
namespace, assembly `Neo.PropertyAttribute`) marks a Component array/list for editor assignment from the owning
GameObject.

**How to use:**
1. Put `[GetComponents]` (or `[GetComponents(true)]` to include children) on a serialized array or list.
2. Select the component with attribute search enabled; an empty collection is filled.

---

The editor processor also supports GameObject collections: the owner only, or its Transform hierarchy when child
search is enabled.

```csharp
[GetComponents(true)]
public UnityEngine.AudioSource[] sources;
```

`GetComponentsAttribute(bool searchInChildren = false)` stores the option in its public `SearchInChildren` field. The `Neo.Editor` Inspector processor fills missing/empty supported collections when attribute search is enabled. Existing nonempty collections are retained.

Runtime code should call `GetComponents<T>()` / `GetComponentsInChildren<T>()` explicitly when discovery is required.

See [GetComponent](GetComponentAttribute.md) and the [shared workflow](README.md).
