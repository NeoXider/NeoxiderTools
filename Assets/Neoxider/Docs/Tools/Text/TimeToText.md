# TimeToText

**Purpose:** A component for formatting and displaying time values (in seconds) on a `TMP_Text`. Supports two modes: `Clock` (05:30, 01:05:30) and `Compact` (1d 5h 30m). Ideal for timers, countdowns, and elapsed time displays.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Text** | Reference to `TMP_Text` (auto-assigned). |
| **Display Mode** | `Clock` (fixed-format separators) or `Compact` (11d 5h). |
| **Time Format** | Format for Clock and fixed Compact mode: `Seconds`, `MinutesSeconds`, `HoursMinutesSeconds`, etc. |
| **Zero Text** | Whether to show text when time = 0. |
| **Allow Negative** | Allow negative values (displayed with `-`). |
| **Separator** | Separator for Clock mode (default `:`). |
| **Start / End Add Text** | Prefix and suffix text. |
| **Compact Use Time Format** | Default enabled: use the selected `TimeFormat`. Disable for dynamic unit selection. |
| **Compact Include Seconds** | Include seconds in dynamic Compact mode. |
| **Compact Max Parts** | Maximum number of units in dynamic Compact mode; at least 1. |
| **Log Warnings** | Opt-in missing-target/range diagnostics, also subject to `NeoDiagnostics` settings. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Set(float time = 0)` | Set seconds and update immediately. Negative finite values clamp to zero unless `AllowNegative` is enabled. |
| `bool TrySetFromString(string raw, string separator = null)` | Parse non-negative SS / MM:SS / HH:MM:SS / DD:HH:MM:SS using the supplied separator or the component separator. Returns `false` if the parsed value cannot be displayed or the text target is missing. |
| `float CurrentTime { get; }` | The currently displayed time value. |
| `static string FormatTime(float time, TimeFormat format, string separator)` | Static formatting method. |
| `TimeFormat`, `ZeroText`, `AllowNegative`, `Separator` | Update formatting and redraw `CurrentTime`. |
| `DisplayMode` | Redraw in the requested mode if it supports `CurrentTime`; otherwise preserve the previous mode, time, text and events. |
| `CompactUseTimeFormat`, `CompactIncludeSeconds`, `CompactMaxParts` | Configure Compact output and redraw immediately. |

`Set` rejects NaN, infinity and durations outside the active formatter's range without changing text, `CurrentTime` or notifications. Clock formatting requires the absolute displayed seconds to fit `Int32`; the largest accepted positive float is `2147483520`. Both fixed and dynamic Compact formatting require a representable `TimeSpan` and can display larger durations than Clock mode. Fixed Compact totals use wide integers, so `3000000` seconds in `Milliseconds` format displays `3000000000ms`. Parsing a finite float alone does not guarantee that the display format supports it.

Clock `Milliseconds` and `SecondsMilliseconds` retain the extension's existing centisecond convention; `MinutesSecondsMilliseconds` uses three-digit milliseconds. The static `FormatTime` method delegates directly to the Clock extension, which converts NaN/negative values to zero and saturates excessive seconds (including positive infinity) at `Int32.MaxValue`. The component rejects excessive inputs instead, preserving a consistent `CurrentTime` and the last valid display.

Display-mode changes are atomic. After dynamic Compact `Set(3000000000f)`, assigning `DisplayMode = TimeDisplayMode.Clock` is rejected: the mode remains Compact, the duration and text remain intact, and no time-change event is published. Assign a Clock-supported duration first if that transition is desired. After `Set(90f)`, switching to Clock succeeds as `01:30` without a time-change event. Read `DisplayMode` after assigning it when the caller needs to detect rejection.

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnTimeChanged` | `float` | The accepted, clamped value changed according to `Mathf.Approximately`. |

The initial zero does not notify. Accepted state and the notification baseline are committed before listeners run: a listener can change a formatting property without receiving the same value recursively, or call `Set` with a new time while preserving that latest value. Format-only updates redraw text without a time-change event.

## Examples

### No-Code Example (Inspector)
Attach `TimeToText` beside a TextMeshPro component, or assign **Text** explicitly. Set **Display Mode** to `Clock` and **Time Format** to `MinutesSeconds`. Wire a timer's float seconds event to **Dynamic float** → `TimeToText.Set(float)`. An input field can call the parsing API from C#; the `bool` result indicates whether an update was possible. For dynamic `1d 05h 30m` output, select `Compact`, disable **Compact Use Time Format**, then configure unit count and seconds.

### Code Example
```csharp
using Neo.Extensions;
using Neo.Tools;
using UnityEngine;

[SerializeField] private TimeToText _timer;

void Update()
{
    _timer.Set(Time.timeSinceLevelLoad);
}

public bool ApplyDurationInput(string input)
{
    return _timer.TrySetFromString(input); // "02:30" -> 150 seconds in the configured mode
}

public bool TryShowClock()
{
    _timer.DisplayMode = TimeDisplayMode.Clock;
    return _timer.DisplayMode == TimeDisplayMode.Clock;
}
```

## See Also
- [SetText](SetText.md)
- [TimerObject](../Time/TimerObject.md)
- ← [Tools/Text](README.md)
