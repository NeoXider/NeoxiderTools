# LoadFromResourcesAttribute

**Namespace:** Global. **Assembly:** `Neo.PropertyAttribute`.

`[LoadFromResources("Audio/Click")]` marks a Unity Object field for editor asset assignment. Use a path relative to a `Resources` folder, without the file extension.

```csharp
[LoadFromResources("Audio/Click")]
public UnityEngine.AudioClip click;
```

`LoadFromResourcesAttribute(string resourcePath = "")` exposes the supplied value through its read-only `ResourcePath` property. The `Neo.Editor` Inspector processor tries `Resources.Load(path, fieldType)` for a nonempty path. If no asset is found, or the path is empty, it falls back to the first already loaded object of that type through `Resources.FindObjectsOfTypeAll`.

That fallback can select an unintended loaded object. Prefer an explicit asset reference when identity matters. Assignment fills missing references when attribute search is enabled and retains existing values. For runtime loading, call `Resources.Load<T>()` explicitly or use the project's asset-loading service.

See [LoadAllFromResources](LoadAllFromResourcesAttribute.md) and the [shared workflow](README.md).
