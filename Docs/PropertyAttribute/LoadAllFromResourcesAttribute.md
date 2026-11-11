# LoadAllFromResourcesAttribute

**What it is:** `[LoadAllFromResources]` (`Scripts/PropertyAttribute/InjectAttribute/LoadAllFromResourcesAttribute.cs`,
global namespace, assembly `Neo.PropertyAttribute`) marks a Unity Object array/list for editor asset assignment
from a `Resources` path.

**How to use:**
1. Put `[LoadAllFromResources("Audio")]` on a serialized array or list of assets.
2. Select the object with attribute search enabled; an empty collection is filled from that `Resources` folder.

---

```csharp
[LoadAllFromResources("Audio")]
public UnityEngine.AudioClip[] clips;
```

`LoadAllFromResourcesAttribute(string resourcePath = "")` stores the supplied value in its public `ResourcePath` field. The `Neo.Editor` Inspector processor uses `Resources.LoadAll(path, elementType)` for a nonempty path. An empty path searches already loaded objects with `Resources.FindObjectsOfTypeAll(elementType)`; it does not load every asset in Resources.

Assignment fills missing/empty supported collections when attribute search is enabled and leaves nonempty collections intact. The metadata does not load assets in a player. Use an explicit runtime loading API when needed, and use asset references rather than scene references in ScriptableObject configuration.

See [LoadFromResources](LoadFromResourcesAttribute.md) and the [shared workflow](README.md).
