# AudioExtensions

**What it is:** extension methods for `AudioSource` providing smooth volume fades (fade in, fade out, fade to target) on the global coroutine helper. File: `Scripts/Extensions/AudioExtensions.cs`, namespace `Neo.Extensions`.

**How to use:** add `using Neo.Extensions;`, call `source.FadeOut(1f)` and keep the returned handle if the fade may need to be stopped.

## API

| Method | Description |
|--------|-------------|
| `FadeTo(this AudioSource, float targetVolume, float duration)` | Smoothly fade volume to target. Returns `CoroutineHandle`. |
| `FadeOut(this AudioSource, float duration)` | Fade volume to zero. |
| `FadeIn(this AudioSource, float duration, float targetVolume = 1f)` | Fade volume up to target. |

## See Also
- ← [Extensions](README.md)
