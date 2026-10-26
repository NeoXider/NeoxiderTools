# TimeParsingExtensions

**Purpose:** Parse a numeric duration into finite, non-negative seconds through `TryParseDuration(string raw, out float seconds, string separator = ":")`. A char-separator overload is also available.

Accepted forms are seconds, `MM:SS`, `HH:MM:SS`, and `DD:HH:MM:SS`. Examples: `"45"` → 45, `"02:30"` → 150, `"1:02:03:04"` → 93784. The first part may exceed the usual clock range; following minutes/seconds must be below 60 and following hours below 24. Fractional numeric parts are accepted.

Colon-separated parts use invariant culture. A single seconds value also accepts the current culture. Null/blank input, invalid parts, negative values, NaN, infinity and totals beyond `float.MaxValue` return `false` with `seconds = 0`. A null/empty separator uses `":"`. Unit text such as `"1h 30m"` is not supported.

**По-русски:** метод принимает числовые части через разделитель, а не буквенные обозначения единиц. При ошибке возвращает `false` и обнуляет результат; NaN и бесконечность не считаются длительностью.

## See Also
- ← [Extensions](README.md)
