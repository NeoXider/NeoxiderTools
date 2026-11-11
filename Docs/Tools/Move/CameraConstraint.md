# CameraConstraint

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/CameraConstraint.cs`, namespace `Neo.Tools`) that clamps its camera object inside the bounds of a `SpriteRenderer`, `BoxCollider2D` or `BoxCollider`, accounting for the visible viewport size, in `LateUpdate`.

**How to use:**
1. Add **Neoxider > Tools > CameraConstraint** to the camera object.
2. Choose `boundsType` and assign the matching bounds source; pick the constrained axes and `edgePadding`.
3. Keep it after any follow script (both run in `LateUpdate`; order them with Script Execution Order if needed).

## Demo scene

`Scenes/Tools/CameraConstraint.unity` in the Demo download (`Assets/NeoxiderDemos/Demo`). Everything is saved in the scene:

| Object | What to look at |
|--------|-----------------|
| `Main Camera` | `CameraConstraint` (**Bounds Type** BoxCollider2D, source `9-Sliced`) and `MouseMover2D` (**Delta Raw**): hold the left button and drag to move the camera; the constraint clamps it every `LateUpdate`. The scene starts at orthographic size 6 so both axes can scroll. |
| `9-Sliced` | The level background; its `BoxCollider2D` is the bounds source. |
| `[Demo] World` | Corner rings, top/bottom edge labels and height ticks that make the clamp visible. |
| `[Demo] Frame/Content` | Live `GetConstraintBounds` / `IsAtEdge` values. **Edge padding** calls `SetEdgePadding` directly (persistent UnityEvent); **Zoom**, **Clamp X / Y** (`SetAxisConstraint`) and **Jump up / Center / Jump down** go through `CameraConstraintDemoController`. Zoom out past the bounds width and X is pinned to the centre. |

The scene is saved with its UI and wiring.

---

## Setup

- Add the component to the camera object, assign a matching bounds source and choose constrained axes.
- The current viewport calculation assumes an XY view along Z. `cam` supplies projection settings;
  the component constrains its own Transform. Use a camera on that same object.
- With `autoUpdateBounds` off, call `UpdateBounds()` after moving/changing the source.

## C# API

Call `SetBoundsSource(type, source)` to assign a SpriteRenderer, BoxCollider2D or BoxCollider.
`SetEdgePadding(float)` and `SetAxisConstraint(bool x, bool y, bool z)` recalculate cached bounds
immediately. In particular, turning Z constraints on replaces the previous fixed-depth cache even
when automatic bounds updates are disabled. Read results with `GetConstraintBounds(out min, out max)`.

| Member | Description |
|--------|-------------|
| `bool HasValidBounds { get; }` | True once bounds were calculated from a valid camera and source, and that source still exists. Check it before trusting `GetConstraintBounds`. |
| `void GetConstraintBounds(out Vector3 min, out Vector3 max)` | The clamp range of the camera position (viewport extents and padding already applied). |
| `bool IsAtEdge(out bool atMinX, out bool atMaxX, out bool atMinY, out bool atMaxY)` | Which edges the camera touches (within 0.01). Only constrained axes report edges, and every flag is false while `HasValidBounds` is false. When the view is wider (or taller) than the bounds, the camera is pinned to the centre and **both** edges of that axis report true: it cannot move either way. |
| `void UpdateBounds()` | Recalculate bounds now (needed after moving the source when `autoUpdateBounds` is off). |
| `bool SetBoundsSource(BoundsType type, Object source = null)` | Select the source type and assign a matching source; returns false when `source` does not match `type`. |

**Missing references.** A camera or bounds source missing when the component starts disables it, logs one gated
warning and invokes `onConstraintFailed`; assign valid references before activation. A bounds source that
disappears later (a destroyed level, a pooled bounds object) only pauses clamping: `HasValidBounds` turns false,
the camera is left where it is, and clamping resumes as soon as `SetBoundsSource` (or the Inspector) assigns a
live source. Paused bounds are recalculated on the first `LateUpdate` that finds a source again, even with
`autoUpdateBounds` off. An oversized viewport is centered on an axis where it cannot fit.

**Cost.** The camera transform is written only when the clamp actually moves it, so a camera resting inside the
bounds does not dirty its hierarchy every frame. A warm `LateUpdate`, including the per-frame recalculation of
`autoUpdateBounds`, makes no managed allocations (guarded by the EditMode test
`CameraConstraint_WarmLateUpdateInsideTheBounds_DoesNotAllocate`).

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `boundsType` | `SpriteRenderer`, `BoxCollider2D` or `BoxCollider`. |
| `spriteRenderer` / `boxCollider2D` / `boxCollider` | Bounds source for the selected type. |
| `cam` | Camera supplying projection settings; defaults to the camera on this object. |
| `edgePadding` | Extra inset from the bounds edge. |
| `constraintX` / `constraintY` / `constraintZ` | Axes to clamp. |
| `autoUpdateBounds` | Recalculate bounds every `LateUpdate` (moving/animated sources). When off, bounds refresh only on screen/aspect/size/FOV changes or `UpdateBounds()`. |
| `onConstraintFailed` | Raised when no camera or bounds source is available at start (or the camera is lost); the component then disables itself. A bounds source lost at runtime only pauses clamping. |
| `showDebugGizmos` and gizmo colors | Scene view visualization of source bounds, clamp area and clamped position. |

## See Also

- [Follow](./Follow.md)
- [Tools/Move](./README.md)
