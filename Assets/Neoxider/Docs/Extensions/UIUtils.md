# UIUtils

**What it is:** static helpers in `Scripts/Extensions/UIUtils.cs` (namespace `Neo.Extensions`) that query EventSystem raycasts at supplied screen coordinates or convert a world point to canvas-local coordinates.

**How to use:**
1. Add `using Neo.Extensions;` and reference `Neo.Extensions` from a custom asmdef. Code using `RaycastResult` also needs `using UnityEngine.EventSystems;` and the `UnityEngine.UI` assembly reference.
2. Pass the pointer's screen position to `IsPointerOverUI(screenPosition)` or `GetUIElementsAtPosition(screenPosition, results)`.
3. Convert world points for UI markers with `WorldToCanvasPoint(canvas, worldPoint, camera)`.

---

## Explicit screen coordinates

These overloads do not read legacy Input or the Input System:

| API | Contract |
| --- | --- |
| `List<RaycastResult> GetUIElementsAtPosition(Vector2 screenPosition)` | Return a new caller-owned list at the supplied screen-pixel position. No current EventSystem gives an empty list. |
| `int GetUIElementsAtPosition(Vector2 screenPosition, List<RaycastResult> results)` | Clear the caller-owned list, fill it through EventSystem.current and return the hit count. No EventSystem still clears the list and returns zero. A null list throws ArgumentNullException. Reuses one shared PointerEventData, so with a retained list the call adds no allocation of its own. |
| `bool IsPointerOverUI(Vector2 screenPosition)` | True when any raycaster reports a hit at the supplied position; false without an EventSystem. Reuses one shared PointerEventData and result buffer, so per-frame "is the finger over UI?" checks add no allocation of their own after the first call. The buffer is cleared after every query and never exposed. |

Inside a component receiving a screen position:

```csharp
private readonly List<RaycastResult> _uiHits = new();

public int PickAt(Vector2 screenPosition)
{
    return UIUtils.GetUIElementsAtPosition(screenPosition, _uiHits);
}
```

The example also requires System.Collections.Generic and UnityEngine. Each call replaces prior results; copy anything that must outlive the next use of the list. RaycastAll invokes the EventSystem's configured raycasters, which can include physics raycasters as well as UI graphics. The helper does not filter results to a particular canvas or raycaster type. Configure or filter those results when the caller requires only graphic UI hits.

Retaining a list avoids allocating a new result list per call, and both coordinate queries reuse one shared PointerEventData (a raycaster that queries again from inside the raycast gets its own pointer, so the outer query is not disturbed). Raycasters still do their own work: Unity's `EventSystem.RaycastAll` sorts the hits, which can allocate while the pointer is over UI, so the helpers promise "nothing above the raycast itself", not zero allocation. These methods never share results between callers. When only a yes/no answer is needed, `IsPointerOverUI(screenPosition)` is the cheapest path. The shared pointer belongs to the EventSystem of the last query: a query with another EventSystem replaces it, and unloading a scene drops it once its EventSystem has been destroyed, so a closed scene's EventSystem is not kept reachable (an EventSystem that survives the unload keeps its warm pointer).

## Compatible mouse input

For current mouse picking across legacy, Both and New-only configurations, use the upper adapter in assembly `Neo.Tools.Input` (`using Neo.Tools;`):

| API | Contract |
| --- | --- |
| `UIInputCompat.GetUIElementsUnderCursor()` | Return a new list using MouseInputCompat.TryGetPosition. |
| `UIInputCompat.GetUIElementsUnderCursor(List<RaycastResult> results)` | Sample the compatible mouse and pass its screen position to UIUtils. Clear the list and return zero when the mouse or EventSystem is unavailable; null list throws ArgumentNullException. |
| `UIInputCompat.IsPointerOverUI()` | Return whether the compatible query has any hits. Delegates to `UIUtils.IsPointerOverUI(Vector2)`, so it adds no list or PointerEventData of its own. |

```csharp
int count = UIInputCompat.GetUIElementsUnderCursor(_uiHits);
bool hasHit = count > 0;
```

Input sampling stays in [Neo.Tools.Input](../Tools/Input/README.md); callers with an existing touch/mouse/custom pointer position can use the coordinate overload directly. The adapter inherits the same raycaster and shared-pointer behavior. Backend selection is shared by the input compatibility helpers; InputBackendCompat.UsesLegacyInput exposes it and ResetBackendSelection resets that selection cache. See the Input guide for backend/device lifecycle details.

The original `UIUtils.GetUIElementsUnderCursor()` and `UIUtils.IsPointerOverUI()` remain available for compatibility. They still read legacy Input.mousePosition when an EventSystem exists; `IsPointerOverUI()` now delegates to the coordinate overload, which adds no allocation of its own above the raycast. Migrate those calls to UIInputCompat for New-only projects, or provide screen coordinates explicitly. Missing EventSystem gives an empty list/false without a mouse query; retaining these old signatures does not make them input-backend-neutral.

## World to canvas

`WorldToCanvasPoint(Canvas canvas, Vector3 worldPoint, Camera camera = null)` projects with the supplied world camera or Camera.main, then returns local coordinates relative to the canvas RectTransform. Supply the projection camera explicitly when the scene uses multiple cameras. Overlay conversion uses a null UI camera; other modes use canvas.worldCamera with the projection camera as fallback (a destroyed worldCamera also falls back). A missing canvas or projection camera returns zero.

The result is canvas-local, not screen pixels. A scene component can expose PickAt or a compatible-pointer method to buttons/UnityEvents while retaining its result list in C#. Use the same typed API and inspect actual hits; no additional scene controller is required.

**По-русски:** передавайте экранную позицию в `GetUIElementsAtPosition`, если ввод уже обработан. Для текущей мыши при Both/New-only используйте `Neo.Tools.UIInputCompat`. Caller-buffer очищается при каждом запросе, в том числе при отсутствии мыши/EventSystem; null-список вызывает исключение. Оба координатных запроса переиспользуют общий PointerEventData, поэтому с сохранённым списком вызов не добавляет собственных аллокаций сверх самого raycast (сортировка хитов в EventSystem может аллоцировать). Общий указатель отпускается при выгрузке сцены, если его EventSystem уничтожен. Старые cursor-методы UIUtils сохраняют legacy Input, а WorldToCanvasPoint возвращает локальные координаты Canvas.

## See Also

- [Lifecycle and ownership](LifecycleAndOwnership.md)
- [ScreenExtensions](ScreenExtensions.md)
- [Input module](../Tools/Input/README.md)
- ← [Extensions](README.md)
