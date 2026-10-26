# FindAllInSceneAttribute

**Namespace:** Global. **Assembly:** `Neo.PropertyAttribute`.

`[FindAllInScene]` marks an array/list for scene lookup by its element type. Supported scene elements are Components and GameObjects.

```csharp
[FindAllInScene(UnityEngine.FindObjectsSortMode.None)]
public UnityEngine.AudioSource[] sources;
```

The constructor accepts a `FindObjectsSortMode` and stores it in the public `SortMode` field; the default is `InstanceID`. The constructor's existing parameter spelling is `sottMode`; use a positional argument as above.

The `Neo.Editor` Inspector processor populates missing/empty supported collections when attribute search is enabled. Existing nonempty values are retained. This is editor assignment; use an explicit scene query or initialization method when runtime discovery is required.

See [FindInScene](FindInSceneAttribute.md) and the [shared workflow](README.md).
