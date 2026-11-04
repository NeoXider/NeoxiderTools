# ComponentExtensions

**What it is:** extension methods for Unity `Component`: get-or-add and hierarchy path. File:
`Scripts/Extensions/ComponentExtensions.cs`, namespace `Neo.Extensions`.

**How to use:**
1. Add `using Neo.Extensions;`.
2. Call `GetOrAdd<T>()` where a component is optional on the prefab but required at runtime.

---

## Methods

| Method | Description |
|--------|-------------|
| `T GetOrAdd<T>(this Component)` | Existing `T` on the same GameObject, or a newly added one. Uses `TryGetComponent`, so a miss does not allocate the editor-only placeholder. |
| `string GetPath(this Component)` | Full hierarchy path `"Parent/Child/GameObject"`; empty for a null component. |

## Example

```csharp
Rigidbody body = this.GetOrAdd<Rigidbody>();
Debug.Log(transform.GetPath()); // "Canvas/Panel/Button"
```

## See Also
- [ObjectExtensions](ObjectExtensions.md)
- ← [Extensions](README.md)
