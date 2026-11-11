# Time Formatting and Parsing

**What it is:** A quick reference for the time formatting/parsing extensions (`Scripts/Extensions/`) and the allocation-free `TimerTextFormatter` (`Scripts/Tools/Time/TimerTextFormatter.cs`, namespace `Neo.Tools`), plus which component to pick for a time task.

**How to use:** pick the API from the table at the end; for per-frame labels use `TimerTextFormatter` instead of `string.Format`.

---

## Extensions

### DateTimeExtensions
- `ToRoundTripUtcString(this DateTime utc)` — serializes a `DateTime` to a UTC string (ISO round-trip format).
- `TryParseUtcRoundTrip(this string raw, out DateTime utc)` — parses a saved string with a fallback for legacy formats.
- `GetSecondsSinceUtc`, `GetSecondsUntilUtc` — seconds elapsed since / remaining until a point in time.
- `EnsureUtc` — coerces a value to UTC.

### TimeParsingExtensions
- `TryParseDuration(string raw, out float seconds, string separator = ":")` — parses a duration from a string.
- Supported formats: `SS`, `MM:SS`, `HH:MM:SS`, `DD:HH:MM:SS`.

### TimeSpanExtensions
- `ToCompactString(this TimeSpan, bool includeSeconds, int maxParts)` — compact output, e.g. `2d 3h 15m`.
- `ToClockString(this TimeSpan, bool includeDays, string separator)` — clock-style output, e.g. `03:15:27`.

### PrimitiveExtensions.FormatTime
- `FormatTime(this float, TimeFormat, string separator, bool trimLeadingZeros)` — formats seconds using the chosen format.
- `trimLeadingZeros` — removes leading zeros from the first token (`01:05` → `1:05`).

### TimerTextFormatter
- `bool Format(string format, float arg0, float arg1, float arg2)` / `Format(string format, int arg0, int arg1, int arg2)` — formats with a .NET composite format (`"{0:F1}s"`, `"{0:00}:{1:00}"`, alignment `{0,6:F1}`, escaped `{{ }}`) into a reused buffer; returns `true` only when the text changed.
- `char[] Buffer`, `int Length` — the last text; pass them to `TMP_Text.SetText(char[], int, int)`.
- `bool IsAllocationFree` — `false` when the format needs the `string.Format` fallback (unusual syntax); output stays identical.
- Used by `TimerObject` for its **Time Text** label. That label is assigned explicitly; leave it empty when a `TimeToText` on the same label formats the time, so only one component writes it.
- Formats like `{3}` or `{time}` throw `FormatException`, exactly like `string.Format`; `TimerObject` catches it and shows its default format instead.

```csharp
private readonly TimerTextFormatter _formatter = new TimerTextFormatter();

private void LateUpdate()
{
    if (_formatter.Format("{0:F1}s", _cooldown.GetRemainingTime(), 0f, 0f))
    {
        _label.SetText(_formatter.Buffer, 0, _formatter.Length);
    }
}
```

## When to Use What

| Task | Component / API |
|---|---|
| Reward cooldown with save/load | **CooldownReward** (Bonus; `TimeReward` is the legacy component) |
| In-scene UI timer (countUp/countDown, looping) | **TimerObject** |
| Programmatic async timer | **Timer** (UniTask) |
| Display time in `TMP_Text` | **TimeToText**; per-frame custom labels: **TimerTextFormatter** |
| Parse a string to seconds | **TimeParsingExtensions.TryParseDuration** |
| Serialize UTC to string | **DateTimeExtensions.ToRoundTripUtcString** |
| Format `float` → string | **float.FormatTime** / **TimeToText.FormatTime** |
