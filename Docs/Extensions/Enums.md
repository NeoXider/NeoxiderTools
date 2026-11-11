# Enums

**What it is:** shared enums of the Extensions module: `TimeFormat`, `TimeDisplayMode` and `ScreenEdge`. File:
`Scripts/Extensions/Enums.cs`, namespace `Neo.Extensions`.

**How to use:** pick the value in a component's Inspector field or pass it to the helper that consumes it.

---

| Enum | Values | Used by |
|------|--------|---------|
| `TimeFormat` | `Milliseconds`, `SecondsMilliseconds`, `Seconds`, `Minutes`, `MinutesSeconds`, `MinutesSecondsMilliseconds`, `Hours`, `HoursMinutes`, `HoursMinutesSeconds`, `Days`, `DaysHours`, `DaysHoursMinutes`, `DaysHoursMinutesSeconds` | `float.FormatTime` ([PrimitiveExtensions](PrimitiveExtensions.md)), `TimeSpan.ToCompactString` ([TimeSpanExtensions](TimeSpanExtensions.md)), time text components |
| `TimeDisplayMode` | `Clock` ("01:23:45"), `Compact` ("2h 30m") | Time-to-text components |
| `ScreenEdge` | `Left`, `Right`, `Top`, `Bottom`, `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`, `Center`, `Front`, `Back` | [ScreenExtensions](ScreenExtensions.md) |

Numeric values are part of serialized scene data; do not reorder them.

## See Also
- ← [Extensions](README.md)
