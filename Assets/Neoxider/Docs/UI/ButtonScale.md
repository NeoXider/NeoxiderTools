# ButtonScale

**What it is:** `Neo.UI.ButtonScale` (`Scripts/UI/Animation/ButtonScale.cs`) is a `MonoBehaviour` that shrinks a UI element while it is pressed and springs it back on release, using a coroutine lerp on `RectTransform.localScale`. It implements `IPointerDownHandler` / `IPointerUpHandler`, so it works on any raycast target without a `Button`.

**How to use:**
1. Add `Neoxider > UI > ButtonScale` to the object that should react to presses.
2. Leave `_rectTransform` empty to use the object's own `RectTransform`.
3. Tune `_pressedSize` and `resizeDuration`; call `SetPressed(bool)` to drive the effect from code or a UnityEvent.

---

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_rectTransform` | Target transform; auto-resolved from this object when empty. |
| `_pressedSize` | Local scale (X, Y) applied while pressed. Default `(0.85, 0.85)`. |
| `resizeDuration` | Seconds to lerp between the base and pressed scale. Default `0.15`. |
| `_useUnscaledTime` | Animate with unscaled time so the press works while `Time.timeScale` is `0` (pause menus). Default on. |

## Public API

| Member | Description |
|--------|-------------|
| `SetPressed(bool pressed)` | Drives the press effect from code or a UnityEvent (`true` = pressed scale, `false` = base scale). No-op while inactive/disabled. |
| `UseUnscaledTime` | Reads or switches the time mode of the press animation. |

## Notes

- A press from rest takes the button's current scale as the release target, so a button rescaled by a layout or code after `Awake` returns to its new size; `OnEnable` restores the last base scale. The original Z scale is preserved (a press never flattens `localScale.z`).
- With the default unscaled time a button inside a [PausePage](./PausePage.md) still animates; switch `_useUnscaledTime` off only for buttons that should follow slow motion.
- An empty or destroyed `_rectTransform` reference falls back to the object's own `RectTransform`.
- Upgrade note: `_useUnscaledTime` is new and defaults to on, so existing buttons now ignore `Time.timeScale`
  (slow motion included). Untick `Use Unscaled Time` or set `UseUnscaledTime = false` to keep scaled time.

## See Also

- [Module Root](../README.md)
