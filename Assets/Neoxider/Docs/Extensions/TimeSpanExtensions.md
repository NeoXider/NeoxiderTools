# TimeSpanExtensions

**What it is:** formatting helpers that turn a `TimeSpan` into compact ("2d 3h 15m") or clock ("03:15:27")
text. File: `Scripts/Extensions/TimeSpanExtensions.cs`, namespace `Neo.Extensions`.

**How to use:**
1. Add `using Neo.Extensions;`.
2. Call `remaining.ToCompactString()` or `remaining.ToClockString()` and assign the result to a label.

---

## Methods

| Method | Description |
|--------|-------------|
| `string ToCompactString(this TimeSpan, bool includeSeconds = false, int maxParts = 3)` | Largest units first, up to `maxParts` (at least 1); `"0s"` for zero. |
| `string ToCompactString(this TimeSpan, TimeFormat format)` | Units selected by a [`TimeFormat`](Enums.md). |
| `string ToClockString(this TimeSpan, bool includeDays = false, string separator = ":")` | Two-digit clock parts; without days the hours part holds the total hours. |

Negative spans are formatted by magnitude with a leading `-`; `TimeSpan.MinValue` is handled without overflow.
See [NumericEdgeCases](NumericEdgeCases.md) for the exact precision and saturation rules.

```csharp
TimeSpan left = TimeSpan.FromMinutes(135);
string compact = left.ToCompactString(); // "2h 15m"
string clock = left.ToClockString();     // "02:15:00"
```

## See Also
- [PrimitiveExtensions](PrimitiveExtensions.md) — `float.FormatTime`
- ← [Extensions](README.md)
