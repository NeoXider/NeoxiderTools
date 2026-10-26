# CameraConstraint

**Purpose:** See Inspector fields below for configuration.

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

Missing camera/source disables the component and invokes `onConstraintFailed`; assign valid references
before activation. An oversized viewport is centered on an axis where it cannot fit.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `BoundsType` | Bounds Type. |
| `boundsType` | Bounds Type. |
| `boxCollider` | Box Collider. |
| `boxCollider2D` | Box Collider2D. |
| `cam` | Cam. |
| `clampedPositionColor` | Clamped Position Color. |
| `constraintBoundsColor` | Constraint Bounds Color. |
| `constraintZ` | Constraint Z. |
| `edgePadding` | Edge Padding. |
| `onConstraintFailed` | On Constraint Failed. |
| `sourceBoundsColor` | Source Bounds Color. |
| `spriteRenderer` | Sprite Renderer. |

## See Also

- [Module Root](../README.md)
