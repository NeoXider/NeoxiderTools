# TimeToText

**Purpose:** A component for formatting and displaying time values (in seconds) on a `TMP_Text`. Supports two modes: `Clock` (05:30, 01:05:30) and `Compact` (1d 5h 30m). Ideal for timers, countdowns, and elapsed time displays.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Text** | Reference to `TMP_Text` (auto-assigned). |
| **Display Mode** | `Clock` (fixed-format separators) or `Compact` (11d 5h). |
| **Time Format** | Format for Clock mode: `Seconds`, `MinutesSeconds`, `HoursMinutesSeconds`, etc. |
| **Zero Text** | Whether to show text when time = 0. |
| **Allow Negative** | Allow negative values (displayed with `-`). |
| **Separator** | Separator for Clock mode (default `:`). |
| **Start / End Add Text** | Prefix and suffix text. |
| **Compact Include Seconds** | Include seconds in Compact mode. |
| **Compact Max Parts** | Maximum number of units in Compact mode (1–N). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Set(float time)` | Set time (seconds) and update the text. |
| `bool TrySetFromString(string raw, string separator)` | Parse a string like SS / MM:SS / HH:MM:SS. |
| `float CurrentTime { get; }` | The currently displayed time value. |
| `static string FormatTime(float time, TimeFormat format, string separator)` | Static formatting method. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnTimeChanged` | `float` | Time value has changed. |

## Examples

### No-Code Example (Inspector)
Attach `TimeToText` to a `TextMeshPro` object. Set `Time Format = MinutesSeconds`. Wire `TimerObject.OnTimeChanged` → `TimeToText.Set(float)`. The timer will now display remaining time as `05:30`.

### Code Example
```csharp
[SerializeField] private TimeToText _timer;

void Update()
{
    _timer.Set(Time.timeSinceLevelLoad);
}
```

## See Also
- [SetText](SetText.md)
- [TimerObject](../Time/TimerObject.md)
- ← [Tools/Text](README.md)
