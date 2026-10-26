# GetComponentAttribute

**Namespace:** Global. **Assembly:** `Neo.PropertyAttribute`.

`[GetComponent]` marks a Component field for assignment from the owning MonoBehaviour's GameObject. `[GetComponent(true)]` also searches children.

```csharp
[UnityEngine.SerializeField, GetComponent(true)]
private UnityEngine.AudioSource source;
```

`GetComponentAttribute(bool searchInChildren = false)` stores the option in its public `SearchInChildren` field. The `Neo.Editor` Inspector processor fills missing references when attribute search is enabled; it retains assigned references.

For runtime initialization, use `GetComponent<AudioSource>()` / `GetComponentInChildren<AudioSource>()` explicitly or accept a dependency through an initialization API. The attribute does not run in `Awake`.

See [GetComponents](GetComponentsAttribute.md) and the [shared workflow](README.md).
