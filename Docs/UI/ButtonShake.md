# ButtonShake

**What it is:** `Neo.UI.ButtonShake` (`Scripts/UI/Animation/ButtonShake.cs`) is a `MonoBehaviour` that shakes a UI element around the position it had when the shake started, with a coroutine that offsets `RectTransform.localPosition` each frame. It implements `IPointerDownHandler` / `IPointerUpHandler`.

**How to use:**
1. Add `Neoxider > UI > ButtonShake` to the object that should shake.
2. Leave `_rectTransform` empty to use the object's own `RectTransform`.
3. Pick the trigger (`_shakeOnStart`, press, or `Shake()` from code/UnityEvents) and the length (`_shakeDuration`, `0` = until stopped).

---

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_rectTransform` | Target transform; auto-resolved from this object when empty. |
| `_shakeDuration` | Shake length in seconds. `0` shakes continuously until stopped. |
| `_shakeMagnitude` | Maximum per-axis offset in local units. Default `3`. |
| `_enableShake` | Master switch; when off, `Shake()` and press shakes do nothing. |
| `_shakeOnStart` | Shake automatically on enable; a press then stops it. |
| `_shakeOnEnd` | Keep shaking after the pointer is released. |
| `_useUnscaledTime` | Count a timed shake in unscaled time so it still ends while `Time.timeScale` is `0`. Default on. |

## Public API

| Member | Description |
|--------|-------------|
| `Shake()` | Starts the shake from code or a UnityEvent (respects `_enableShake`). |
| `StopShake()` | Stops the shake and restores the position captured when it started. |
| `IsShaking` | True while a shake is running. |
| `UseUnscaledTime` | Reads or switches the time mode of a timed shake. |

## Notes

- The origin is captured when a shake starts and restored when it stops or ends, so a button moved after
  `Awake` (layout group, animation, code) keeps its current place instead of jumping back.
- The full local position, including Z, is preserved. The random shake offsets affect X/Y only.
- Disabling the component stops its shake. `Shake()` ignores disabled or inactive components.
- Upgrade note: `_useUnscaledTime` is new and defaults to on, so the length of a timed shake now ignores
  `Time.timeScale` (slow motion included). Untick `Use Unscaled Time` or set `UseUnscaledTime = false` to keep
  scaled time.

```csharp
buttonShake.Shake();
// Later, or through another UnityEvent:
buttonShake.StopShake();
```

## See Also

- [Module Root](../README.md)
