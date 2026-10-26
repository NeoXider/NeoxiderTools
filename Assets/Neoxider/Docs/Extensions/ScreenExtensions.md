# ScreenExtensions

Use `using Neo.Extensions;` for point visibility, viewport bounds and world placement at screen edges. Supply the camera explicitly for split-screen or multiple-camera scenes. A null camera uses Camera.main; no camera returns the documented fallback for each operation.

## Coordinate contracts

| API | Contract |
| --- | --- |
| `position.IsOnScreen(camera)` / `IsOutOfScreen(camera)` | Normalized viewport X/Y and nonnegative camera depth. This is a point check, not renderer visibility, occlusion or a test against both clipping planes. Missing camera gives false/true respectively. |
| `position.IsOutOfScreenSide(side, camera)` | Left/Right/Top/Bottom viewport tests; Front means negative camera depth, Back means beyond farClipPlane. Missing camera gives false. |
| `position.GetClosestScreenEdgePoint(camera)` | Preserve an already-on-screen point; otherwise clamp projected viewport X/Y and depth. Missing camera preserves the input. It is not a general ray/plane intersection. |
| `camera.GetWorldPositionAtScreenEdge(edge, offset, depth)` | Place at an edge, corner or Center of this camera's pixelRect. Offset is in screen pixels; depth is measured forward from the near clip plane. Missing camera returns zero and uses the diagnostics error gate. |
| `camera.GetWorldScreenBounds(distance)` | World-axis-aligned bounds around all four viewport corners at a camera-relative distance. Missing camera gives zero bounds. Rotated/tilted cameras are supported. |

Screen-edge placement includes pixelRect's origin, so an offset viewport does not use the center or edges of the full display. Front/Back enum values are depth-test choices, not supported placement anchors. Layout utilities use world positions; a parent does not turn these results into local coordinates.

## Orthographic depth migration

Screen-edge depth is near-plane-relative in **both** orthographic and perspective modes. GetWorldScreenBounds keeps its separate camera-relative distance contract.

If an older orthographic call used depth as camera-relative distance, preserve the same world depth explicitly:

```csharp
float depthFromNearPlane = previousCameraDistance - camera.nearClipPlane;
Vector3 rightEdge = camera.GetWorldPositionAtScreenEdge(
    ScreenEdge.Right, Vector2.zero, depthFromNearPlane);
```

For example, a camera distance of 10 and nearClipPlane of 0.3 require a depth of 9.7. Review [ScreenPositioner](../Tools/Move/ScreenPositioner.md) configurations because that consumer passes its depth to this helper. Its own option to preserve world Z is a separate post-projection behavior; do not assume every disabled-depth configuration is unaffected. No scene/prefab values are rewritten automatically.

Use these APIs from C# components and expose a thin positioning method to scene buttons/UnityEvents when needed. For UI placement, convert the world point with [UIUtils.WorldToCanvasPoint](UIUtils.md) instead of treating screen pixels as canvas-local coordinates.

**По-русски:** offset измеряется в пикселях viewport камеры, depth — от near clip plane в обоих режимах проекции. Для старой ортографической camera-relative глубины используйте `oldDepth - camera.nearClipPlane`. Bounds принимает отдельное расстояние от камеры. Значения сцен и prefabs автоматически не мигрируются.

## See Also

- [Lifecycle and ownership](LifecycleAndOwnership.md)
- [UIUtils](UIUtils.md)
- ← [Extensions](README.md)
