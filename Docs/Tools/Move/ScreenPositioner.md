# ScreenPositioner

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/ScreenPositioner.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Move`) that places an object at a camera screen edge with a pixel offset and a world XY offset.

**How to use:**
1. Add **Neoxider > Tools > ScreenPositioner**, assign `Target Camera` (or rely on `Camera.main`) and choose `Screen Edge`.
2. Set the pixel offset, `Depth` / `Use Depth`, and an optional world `Offset`.
3. Enable `Update Every Frame` for moving cameras or resolution changes, or call `Configure` from code.

---

## Setup

- Add the component, assign `Target Camera` (or create/tag `Camera.main` before startup), and choose
  `Screen Edge`. The pixel offset is measured inside that camera's `pixelRect`, including its origin.
- `Use Screen Position` chooses `Position Screen`; otherwise `Offset Screen` supplies the pixel offset.
- `Use Depth` keeps the projected 3D point. With it off, the component restores the object's previous
  **world Z** after projection. `Offset` is then added in world XY.
- Enable `Update Every Frame` for camera/resolution changes, or use **Update Position** while authoring.
  Each update computes the final position once and writes the transform only when it changed, and a warm update
  makes no managed allocations (guarded by the EditMode test
  `ScreenPositioner_WarmEveryFrameUpdate_DoesNotAllocateOrRewriteAnUnmovedTransform`).

## C# API

`Configure(ScreenEdge edge, Vector2 offset, float depth)` configures the edge, pixel offset and depth,
then applies the position through the assigned/resolved camera. It uses `Offset Screen` mode; if
`Use Screen Position` is enabled, that separate authored offset still takes precedence.

```csharp
positioner.Configure(ScreenEdge.TopRight, new Vector2(-24f, -24f), 8f);
```

The same fields can be configured in the Inspector; `Configure` can be called by gameplay code after
the camera reference has been initialized.

## 11.0 depth migration

`Depth` / `Configure(..., depth)` is **camera-space Z beyond the camera's near clip plane**. The shared
`ScreenExtensions.GetWorldPositionAtScreenEdge` projection uses `nearClipPlane + depth` as its
`ScreenToWorldPoint` depth. This is not world Z or Euclidean distance along an off-center perspective ray.

Older orthographic projection used a camera-relative depth directly. To preserve an old orthographic
3D position at the same edge/offset, set `newDepth = oldDepth - camera.nearClipPlane` manually.
For an arbitrary existing point, the equivalent value is
`camera.WorldToScreenPoint(oldWorldPosition).z - camera.nearClipPlane`.
Review perspective and rotated-camera placement separately: unchanged XY in an axis-aligned
orthographic example does not prove full 3D pose parity.

No serialized depth, scene or prefab values are rewritten automatically. With `Use Depth` off,
preserving the previous world Z does not guarantee the same full position under a rotated/perspective
camera. Native 3D placement and manual migration acceptance remain part of integration verification.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_targetCamera` | Projection camera; `Camera.main` when empty. |
| `_screenEdge` | Anchor edge or corner of the camera's `pixelRect`. |
| `_useScreenPosition` / `_positionScreen` | Use `_positionScreen` as the pixel offset instead of `_offsetScreen`. |
| `_offsetScreen` | Pixel offset from the edge (also set by `Configure`). |
| `_offset` | World XY offset added after projection. |
| `_useDepth` / `_depth` | Keep the projected Z at camera-space depth beyond the near clip plane; when off, the previous world Z is kept. |
| `_updateEveryFrame` | Reapply the placement in every `LateUpdate`. |
| `_logMissingCameraWarnings` | One-time warning when no camera is available. |

## See Also

- [Tools/Move](./README.md)
