# ButtonShake

**Purpose:** shakes a UI element around its start position with a coroutine that offsets `RectTransform.localPosition` each frame. Implements `IPointerDownHandler` / `IPointerUpHandler`.

## Setup

- Add `Neoxider > UI > ButtonShake` to the object that should shake.
- `_rectTransform` auto-resolves from the same object when left empty.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_rectTransform` | Target transform; auto-resolved from this object when empty. |
| `_shakeDuration` | Shake length in seconds. `0` shakes continuously until stopped. |
| `_shakeMagnitude` | Maximum per-axis offset in local units. Default `3`. |
| `_enableShake` | Master switch; when off, `Shake()` and press shakes do nothing. |
| `_shakeOnStart` | Shake automatically on enable; a press then stops it. |
| `_shakeOnEnd` | Keep shaking after the pointer is released. |

## Public API

| Member | Description |
|--------|-------------|
| `Shake()` | Starts the shake from code or a UnityEvent (respects `_enableShake`). |
| `StopShake()` | Stops the shake and restores the original position. |

## Notes

- The start position is captured in `Awake` and restored in `OnEnable` and when the shake stops.

## See Also

- [Module Root](../README.md)
