# AudioExtensions

**Purpose:** Extension methods for `AudioSource` providing smooth volume fade effects (fade in, fade out, fade to target).

## API

| Method | Description |
|--------|-------------|
| `FadeTo(this AudioSource, float targetVolume, float duration)` | Smoothly fade volume to target. Returns `CoroutineHandle`. |
| `FadeOut(this AudioSource, float duration)` | Fade volume to zero. |
| `FadeIn(this AudioSource, float duration, float targetVolume = 1f)` | Fade volume up to target. |

## See Also
- ← [Extensions](README.md)
