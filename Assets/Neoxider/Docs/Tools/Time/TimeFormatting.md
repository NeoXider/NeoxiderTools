# Time Formatting and Parsing

**Purpose:** Quick reference for time extensions and component selection for working with time.

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

## When to Use What

| Task | Component / API |
|---|---|
| Reward cooldown with save/load | **TimeReward** |
| In-scene UI timer (countUp/countDown, looping) | **TimerObject** |
| Programmatic async timer | **Timer** (UniTask) |
| Display time in `TMP_Text` | **TimeToText** |
| Parse a string to seconds | **TimeParsingExtensions.TryParseDuration** |
| Serialize UTC to string | **DateTimeExtensions.ToRoundTripUtcString** |
| Format `float` → string | **float.FormatTime** / **TimeToText.FormatTime** |
