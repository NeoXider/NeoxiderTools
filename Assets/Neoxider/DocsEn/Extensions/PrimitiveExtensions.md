# PrimitiveExtensions

**Purpose:** Extension methods for `float`, `int`, and `bool` — rounding, time formatting, normalization, remapping, and type conversion.

---

## API

### Bool
| Method | Description |
|--------|-------------|
| `int ToInt(this bool)` | `true` → `1`, `false` → `0`. |

### Int
| Method | Description |
|--------|-------------|
| `bool ToBool(this int)` | Non-zero → `true`, zero → `false`. |
| `string FormatWithSeparator(this int, string sep)` | `1000000` → `"1 000 000"` (with `" "` separator). |

### Float
| Method | Description |
|--------|-------------|
| `float RoundToDecimal(this float, int places)` | Round to N decimal places. |
| `string FormatTime(this float, TimeFormat, string sep)` | Format seconds into `"MM:SS"`, `"HH:MM:SS"`, etc. |
| `string FormatWithSeparator(this float, string sep, int places)` | Format with thousands separator. |
| `float NormalizeToUnit(this float, float min, float max)` | Normalize to `[0, 1]`. |
| `float NormalizeToRange(this float, float min, float max)` | Normalize to `[-1, 1]`. |
| `float Denormalize(this float, float min, float max)` | `[0, 1]` → `[min, max]`. |
| `float Remap(this float, float fromMin, float fromMax, float toMin, float toMax)` | Remap between two ranges. |

---

## Examples

### Code
```csharp
float time = 125.7f;
string display = time.FormatTime(TimeFormat.MinutesSeconds); // "02:05"

float hp = 75f;
float normalized = hp.NormalizeToUnit(0f, 100f); // 0.75

float val = 0.5f;
float remapped = val.Remap(0f, 1f, -10f, 10f); // 0.0

int score = 1500000;
string text = score.FormatWithSeparator(" "); // "1 500 000"
```

---

## See Also
- [StringExtension](StringExtension.md) — `ToInt()`, `ToFloat()` from strings
- ← [Extensions](README.md)
