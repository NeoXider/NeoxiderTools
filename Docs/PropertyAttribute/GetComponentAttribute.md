# GetComponentAttribute

**What it is:** `[GetComponent]` (`Scripts/PropertyAttribute/InjectAttribute/GetComponentAttribute.cs`, global
namespace, assembly `Neo.PropertyAttribute`) marks a Component field for editor assignment from the owning
MonoBehaviour's GameObject. `[GetComponent(true)]` also searches children.

**How to use:**
1. Put `[GetComponent]` (or `[GetComponent(true)]`) on a serialized Component field.
2. Select the component with attribute search enabled; a missing reference is filled and recorded with Undo.

---

```csharp
[UnityEngine.SerializeField, GetComponent(true)]
private UnityEngine.AudioSource source;
```

`GetComponentAttribute(bool searchInChildren = false)` stores the option in its public `SearchInChildren` field. The `Neo.Editor` Inspector processor fills missing references when attribute search is enabled; it retains assigned references. The assignment is recorded with Undo (prefab instances keep it as an override), and with several objects selected every selected object is filled.

For runtime initialization, use `GetComponent<AudioSource>()` / `GetComponentInChildren<AudioSource>()` explicitly or accept a dependency through an initialization API. The attribute does not run in `Awake`.

See [GetComponents](GetComponentsAttribute.md) and the [shared workflow](README.md).
