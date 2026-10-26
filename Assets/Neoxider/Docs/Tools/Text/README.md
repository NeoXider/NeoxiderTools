# Tools / Text

Text and time formatting for TextMeshPro UI. Scripts live in `Scripts/Tools/Text/`, assembly `Neo.Tools.Text`, namespace `Neo.Tools`. This is the canonical module entry.

## Components

| Page | Description |
|------|-------------|
| [SetText](./SetText.md) | Animated numbers, immediate strings, percentages, currency and exact `BigInteger` values. |
| [TimeToText](./TimeToText.md) | Clock or compact duration text, parsing and time change notifications. |

## C# and scene workflows

Assign the public `text` reference explicitly, or place the component beside a `TMP_Text` for the `Awake`/Inspector fallback. From C#, call `SetText.Set(float)` for an animated number, `SetText.Set(string)` for immediate text, or `TimeToText.Set(float)` for seconds. The same methods can receive compatible UnityEvents in the Inspector; neither component requires a NoCode binding.

`SetText` uses DOTween. `Set(int)` applies `IndexOffset` once and commits an exact integer when animation completes. `SetInteger(long)` animates an already-computed final integer without another offset. Interpolation and `currentNum` remain approximate floats; formatting-only updates retain the exact completed integer. An immediate write or `Clear()` cancels the previous animation and its final commit. With **On Enable Anim** enabled, animation waits while the component or its hierarchy is inactive, then resumes on enable. Destruction releases the animation. Derived components override the protected virtual `OnEnable`/`OnDisable` hooks and call the base hook once alongside their own subscription handling.

`TimeToText` updates immediately. Unsupported duration inputs preserve the last valid display; `TrySetFromString` returns `false` if parsing, range validation or the target reference prevents an update. A display-mode change that cannot represent the current time preserves the previous mode, time, text and events. Change notifications are safe to use for a format refresh or a nested `Set` call.

For formatting without scene components, use `Neo.Extensions.NumberFormatExtensions`, `TimeParsingExtensions` and the time formatting extensions. A custom assembly needs references to `Neo.Tools.Text` and `Unity.TextMeshPro`, plus `Neo.Extensions` when using format enums/options or extension APIs. DOTween must be available for `SetText`.

## See also

- [UI](../../UI/README.md)
- [Tools/Time](../Time/README.md)
