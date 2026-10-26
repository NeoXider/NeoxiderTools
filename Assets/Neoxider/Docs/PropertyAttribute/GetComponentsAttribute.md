# GetComponentsAttribute

**Namespace:** Global. **Assembly:** `Neo.PropertyAttribute`.

`[GetComponents]` marks a Component array/list for assignment from the owning GameObject. `[GetComponents(true)]` includes children. The editor processor also supports GameObject collections: the owner only, or its Transform hierarchy when child search is enabled.

```csharp
[GetComponents(true)]
public UnityEngine.AudioSource[] sources;
```

`GetComponentsAttribute(bool searchInChildren = false)` stores the option in its public `SearchInChildren` field. The `Neo.Editor` Inspector processor fills missing/empty supported collections when attribute search is enabled. Existing nonempty collections are retained.

Runtime code should call `GetComponents<T>()` / `GetComponentsInChildren<T>()` explicitly when discovery is required.

See [GetComponent](GetComponentAttribute.md) and the [shared workflow](README.md).
