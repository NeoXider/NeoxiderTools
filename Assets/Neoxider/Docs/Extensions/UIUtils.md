# UIUtils

Use `Neo.Extensions.UIUtils` to query EventSystem raycasts at supplied screen coordinates or convert a world point to canvas-local coordinates. Add `using Neo.Extensions;` and reference `Neo.Extensions`. Code using `RaycastResult` also needs `using UnityEngine.EventSystems;` and the `UnityEngine.UI` assembly reference in a custom asmdef.

## Explicit screen coordinates

These overloads do not read legacy Input or the Input System:

| API | Contract |
| --- | --- |
| `List<RaycastResult> GetUIElementsAtPosition(Vector2 screenPosition)` | Return a new caller-owned list at the supplied screen-pixel position. No current EventSystem gives an empty list. |
| `int GetUIElementsAtPosition(Vector2 screenPosition, List<RaycastResult> results)` | Clear the caller-owned list, fill it through EventSystem.current and return the hit count. No EventSystem still clears the list and returns zero. A null list throws ArgumentNullException. |

Inside a component receiving a screen position:

```csharp
private readonly List<RaycastResult> _uiHits = new();

public int PickAt(Vector2 screenPosition)
{
    return UIUtils.GetUIElementsAtPosition(screenPosition, _uiHits);
}
```

The example also requires System.Collections.Generic and UnityEngine. Each call replaces prior results; copy anything that must outlive the next use of the list. RaycastAll invokes the EventSystem's configured raycasters, which can include physics raycasters as well as UI graphics. The helper does not filter results to a particular canvas or raycaster type. Configure or filter those results when the caller requires only graphic UI hits.

Retaining a list avoids allocating a new result list per call, but PointerEventData is still constructed for each query with an EventSystem. Raycasters can perform their own work/allocations. These methods do not share results between callers or provide a zero-allocation guarantee.

## Compatible mouse input

For current mouse picking across legacy, Both and New-only configurations, use the upper adapter in assembly `Neo.Tools.Input` (`using Neo.Tools;`):

| API | Contract |
| --- | --- |
| `UIInputCompat.GetUIElementsUnderCursor()` | Return a new list using MouseInputCompat.TryGetPosition. |
| `UIInputCompat.GetUIElementsUnderCursor(List<RaycastResult> results)` | Sample the compatible mouse and pass its screen position to UIUtils. Clear the list and return zero when the mouse or EventSystem is unavailable; null list throws ArgumentNullException. |
| `UIInputCompat.IsPointerOverUI()` | Return whether the compatible query has any hits. This convenience method uses the allocating-list path. |

```csharp
int count = UIInputCompat.GetUIElementsUnderCursor(_uiHits);
bool hasHit = count > 0;
```

Input sampling stays in [Neo.Tools.Input](../Tools/Input/README.md); callers with an existing touch/mouse/custom pointer position can use the coordinate overload directly. The adapter inherits the same raycaster and per-query PointerEventData behavior. Backend selection is shared by the input compatibility helpers; InputBackendCompat.UsesLegacyInput exposes it and ResetBackendSelection resets that selection cache. See the Input guide for backend/device lifecycle details.

The original `UIUtils.GetUIElementsUnderCursor()` and `UIUtils.IsPointerOverUI()` remain available for compatibility. They still read legacy Input.mousePosition when an EventSystem exists. Migrate those calls to UIInputCompat for New-only projects, or provide screen coordinates explicitly. Missing EventSystem gives an empty list/false without a mouse query; retaining these old signatures does not make them input-backend-neutral.

## World to canvas

`WorldToCanvasPoint(Canvas canvas, Vector3 worldPoint, Camera camera = null)` projects with the supplied world camera or Camera.main, then returns local coordinates relative to the canvas RectTransform. Supply the projection camera explicitly when the scene uses multiple cameras. Overlay conversion uses a null UI camera; other modes use canvas.worldCamera with the projection camera as fallback. A missing canvas or projection camera returns zero.

The result is canvas-local, not screen pixels. A scene component can expose PickAt or a compatible-pointer method to buttons/UnityEvents while retaining its result list in C#. Use the same typed API and inspect actual hits; no additional scene controller is required.

**По-русски:** передавайте экранную позицию в `GetUIElementsAtPosition`, если ввод уже обработан. Для текущей мыши при Both/New-only используйте `Neo.Tools.UIInputCompat`. Caller-buffer очищается при каждом запросе, в том числе при отсутствии мыши/EventSystem; null-список вызывает исключение. Переиспользование списка не устраняет создание PointerEventData. Старые cursor-методы UIUtils сохраняют legacy Input, а WorldToCanvasPoint возвращает локальные координаты Canvas.

## See Also

- [Lifecycle and ownership](LifecycleAndOwnership.md)
- [ScreenExtensions](ScreenExtensions.md)
- [Input module](../Tools/Input/README.md)
- ← [Extensions](README.md)
