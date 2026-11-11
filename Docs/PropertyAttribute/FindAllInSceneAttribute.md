# FindAllInSceneAttribute

**What it is:** `[FindAllInScene]` (`Scripts/PropertyAttribute/InjectAttribute/FindAllInSceneAttribute.cs`, global
namespace, assembly `Neo.PropertyAttribute`) marks an array/list for editor scene lookup by its element type.
Supported scene elements are Components and GameObjects.

**How to use:**
1. Put `[FindAllInScene]` on a serialized array or list field.
2. Select the component with attribute search enabled; an empty collection is filled from the open scene.

---

```csharp
[FindAllInScene(UnityEngine.FindObjectsSortMode.None)]
public UnityEngine.AudioSource[] sources;
```

The constructor accepts a `FindObjectsSortMode` and stores it in the public `SortMode` field; the default is `InstanceID`. The constructor's existing parameter spelling is `sottMode`; use a positional argument as above.

The `Neo.Editor` Inspector processor populates missing/empty supported collections when attribute search is enabled. Existing nonempty values are retained. This is editor assignment; use an explicit scene query or initialization method when runtime discovery is required.

See [FindInScene](FindInSceneAttribute.md) and the [shared workflow](README.md).
