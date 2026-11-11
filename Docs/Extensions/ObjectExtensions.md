# ObjectExtensions

**What it is:** null-safe helpers for any `UnityEngine.Object`. File: `Scripts/Extensions/ObjectExtensions.cs`,
namespace `Neo.Extensions`.

**How to use:**
1. Add `using Neo.Extensions;`.
2. Call the helper on any Unity object reference, including one that may be null or destroyed.

---

## Methods

| Method | Description |
|--------|-------------|
| `void SafeDestroy(this Object, bool immediate = false)` | Destroys the object; uses `DestroyImmediate` in Edit Mode or when `immediate` is true. A null/destroyed object is ignored. |
| `bool IsValid(this Object)` | `true` while the reference is neither null nor destroyed. |
| `string GetName(this Object)` | The object's name, or an empty string for null/destroyed. |
| `void SetName(this Object, string)` | Renames the object when it is alive. |

```csharp
_spawnedPreview.SafeDestroy(); // works in Edit Mode tools and at runtime
```

## See Also
- [ComponentExtensions](ComponentExtensions.md)
- ← [Extensions](README.md)
