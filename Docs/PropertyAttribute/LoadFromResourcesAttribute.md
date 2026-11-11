# LoadFromResourcesAttribute

**What it is:** `[LoadFromResources]` (`Scripts/PropertyAttribute/InjectAttribute/LoadFromResourcesAttribute.cs`,
global namespace, assembly `Neo.PropertyAttribute`) marks a Unity Object field for editor asset assignment from a
`Resources` path.

**How to use:**
1. Put `[LoadFromResources("Audio/Click")]` on a serialized asset field; the path is relative to a `Resources`
   folder, without the file extension.
2. Select the object with attribute search enabled; a missing reference is filled and recorded with Undo.

---

```csharp
[LoadFromResources("Audio/Click")]
public UnityEngine.AudioClip click;
```

`LoadFromResourcesAttribute(string resourcePath = "")` exposes the supplied value through its read-only `ResourcePath` property. The `Neo.Editor` Inspector processor tries `Resources.Load(path, fieldType)` for a nonempty path. If no asset is found, or the path is empty, it falls back to the first already loaded object of that type through `Resources.FindObjectsOfTypeAll`.

That fallback can select an unintended loaded object. Prefer an explicit asset reference when identity matters. Assignment fills missing references when attribute search is enabled and retains existing values; it is recorded with Undo (prefab instances keep it as an override). When nothing is found, the warning is logged once per object and field, and the Inspector retries after the next project change (or at most every 0.5 s) instead of on every repaint. For runtime loading, call `Resources.Load<T>()` explicitly or use the project's asset-loading service.

See [LoadAllFromResources](LoadAllFromResourcesAttribute.md) and the [shared workflow](README.md).
