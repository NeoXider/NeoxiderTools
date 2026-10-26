# LoadAllFromResourcesAttribute

**Namespace:** Global. **Assembly:** `Neo.PropertyAttribute`.

`[LoadAllFromResources("Audio")]` marks a Unity Object array/list for editor asset assignment.

```csharp
[LoadAllFromResources("Audio")]
public UnityEngine.AudioClip[] clips;
```

`LoadAllFromResourcesAttribute(string resourcePath = "")` stores the supplied value in its public `ResourcePath` field. The `Neo.Editor` Inspector processor uses `Resources.LoadAll(path, elementType)` for a nonempty path. An empty path searches already loaded objects with `Resources.FindObjectsOfTypeAll(elementType)`; it does not load every asset in Resources.

Assignment fills missing/empty supported collections when attribute search is enabled and leaves nonempty collections intact. The metadata does not load assets in a player. Use an explicit runtime loading API when needed, and use asset references rather than scene references in ScriptableObject configuration.

See [LoadFromResources](LoadFromResourcesAttribute.md) and the [shared workflow](README.md).
