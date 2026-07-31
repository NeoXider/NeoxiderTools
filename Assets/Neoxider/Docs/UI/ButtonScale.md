# ButtonScale

**Purpose:** shrinks a UI element while it is pressed and springs it back on release, using a coroutine lerp on `RectTransform.localScale`. Implements `IPointerDownHandler` / `IPointerUpHandler`, so it works on any raycast target without a `Button`.

## Setup

- Add `Neoxider > UI > ButtonScale` to the object that should react to presses.
- `_rectTransform` auto-resolves from the same object when left empty.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_rectTransform` | Target transform; auto-resolved from this object when empty. |
| `_pressedSize` | Local scale (X, Y) applied while pressed. Default `(0.85, 0.85)`. |
| `resizeDuration` | Seconds to lerp between the base and pressed scale. Default `0.15`. |

## Public API

| Member | Description |
|--------|-------------|
| `SetPressed(bool pressed)` | Drives the press effect from code or a UnityEvent (`true` = pressed scale, `false` = base scale). No-op while inactive/disabled. |

## Notes

- The base scale is captured in `Awake` and restored in `OnEnable`; the original Z scale is preserved (a press never flattens `localScale.z`).

## See Also

- [Module Root](../README.md)
