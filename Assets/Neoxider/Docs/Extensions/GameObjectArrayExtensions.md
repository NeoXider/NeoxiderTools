# GameObjectArrayExtensions

**What it is:** static extension methods for batch operations on collections of `GameObject`s and `Component`s
(activate, destroy, parent, move, nearest, average position, combined bounds). File:
`Scripts/Extensions/GameObjectArrayExtensions.cs`, namespace `Neo.Extensions`.

**How to use:**
1. Add `using Neo.Extensions;`.
2. Call the method on an array, list or any `IEnumerable` of GameObjects or components: `enemies.FindClosest(player.position)`.
3. Prefer arrays/lists in per-frame code: they are scanned by index without allocating.

---

## Methods

Null and destroyed entries are skipped everywhere. The query methods marked "Lazy" below defer their work until the result is enumerated; `SetActiveAll` returns an `IEnumerable` too, but applies `SetActive` immediately and returns the source itself.

| Method | Description |
|--------|-------------|
| `SetActiveAll(bool)` | `SetActive` on every GameObject (or every component's GameObject). Returns the source; null returns null. |
| `SetActiveRange(IList<GameObject>, int upToIndex, bool)` | `SetActive` on indices `0 .. upToIndex-1`. |
| `SetActiveAtIndex(int index, bool)` | `SetActive` on one entry and return its GameObject. Lists/arrays index directly; other sequences are walked up to the index without copying. An invalid index (or a null source) returns null with a gated warning. |
| `DestroyAll()` | `Object.Destroy` on every GameObject (component overload destroys the components' GameObjects). Snapshots the source first. |
| `GetActiveObjects()` | Lazy: entries whose `activeSelf` is true. |
| `GetComponentsFromAll<T>()` | Lazy: `GetComponent<T>` of every GameObject, skipping misses. |
| `GetFirstComponentFromAll<T>()` | First `T` found, using `TryGetComponent`. |
| `SetPositionAll(Vector3)` | Moves every GameObject to the same world position. |
| `FindClosest(Vector3)` | Nearest GameObject/component by squared distance; null for an empty source. No allocation for lists/arrays. |
| `WithinDistance(Vector3, float)` | Lazy: entries within the distance (inclusive). |
| `SetParentAll(Transform, bool worldPositionStays = true)` | Reparents every entry. |
| `GetAveragePosition()` | Average world position; `Vector3.zero` for an empty source. No allocation for lists/arrays. |
| `GetCombinedBounds()` | Bounds enclosing every enabled `Renderer` in the entries and their children (inactive children excluded); zero bounds at the origin when none. Uses a reused renderer buffer; no allocation for lists/arrays. |

## Example

```csharp
using Neo.Extensions;
using UnityEngine;

public sealed class TargetPicker : MonoBehaviour
{
    [SerializeField] private Transform[] _targets;

    public Transform Pick()
    {
        // No garbage per call for arrays and lists.
        return _targets.FindClosest(transform.position);
    }
}
```

## Scene / NoCode workflow

There is no component. Keep the collection on a scene component and expose a public method (for example
`ShowAll()` calling `_panels.SetActiveAll(true)`) for UnityEvents.

## See Also
- [TransformExtensions](TransformExtensions.md) — `GetClosest` for a single transform
- ← [Extensions](README.md)
