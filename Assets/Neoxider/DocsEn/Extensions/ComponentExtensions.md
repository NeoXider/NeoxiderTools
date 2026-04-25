# ComponentExtensions

**Purpose:** Extension methods for Unity `Component` — getting or adding components, and hierarchy path retrieval.

---

## API

| Method | Description |
|--------|-------------|
| `T GetOrAdd<T>(this Component)` | Get existing component or add one if it doesn't exist. |
| `string GetPath(this Component)` | Full hierarchy path: `"Parent/Child/GameObject"`. |

---

## Examples

### Code
```csharp
// Ensure Rigidbody exists
Rigidbody rb = this.GetOrAdd<Rigidbody>();

// Debug hierarchy path
Debug.Log(transform.GetPath()); // "Canvas/Panel/Button"
```

---

## See Also
- [ObjectExtensions](ObjectExtensions.md)
- ← [Extensions](README.md)
