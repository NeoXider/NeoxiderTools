# StringExtension

**What it is:** extension methods for `string` — camelCase splitting, truncation, parsing, HEX-to-Color conversion, and rich text formatting (bold, italic, color, rainbow, gradient). File: `Scripts/Extensions/StringExtension.cs`, namespace `Neo.Extensions`.

**How to use:** add `using Neo.Extensions;` and call the method on a string: `"42".ToInt()`, `title.Bold().SetColor(Color.yellow)`.

---

## API

### Text Manipulation
| Method | Description |
|--------|-------------|
| `string SplitCamelCase(this string)` | `"MyVar"` → `"My Var"`. |
| `bool IsNullOrEmptyAfterTrim(this string)` | Null, empty or whitespace only (no trimmed copy is allocated). |
| `string ToCamelCase(this string)` | First char to lowercase. |
| `string Truncate(this string, int maxLength)` | Truncate with `...`. |
| `bool IsNumeric(this string)` | Non-empty and every character is a digit (`char.IsDigit`). Allocation-free. |
| `string Reverse(this string)` | Reverse the string. |
| `string RandomString(int length, string chars)` | Generate random string (static). |

### Parsing
| Method | Description |
|--------|-------------|
| `bool ToBool(this string)` | `"true"`, `"yes"` (ordinal, case-insensitive) and `"1"` → `true`; everything else, including surrounding spaces, → `false`. Independent of the OS culture. |
| `int ToInt(this string, int default = 0)` | Safe int parse: invariant culture first, then the current culture. |
| `float ToFloat(this string, float default = 0f)` | Safe float parse: invariant culture first (`"1.5"` works on every OS locale), then the current culture (`"1,5"` still works on a comma-decimal system). |
| `Color ToColor(this string)` | HEX string → `Color`. |
| `bool ToColorSafe(this string, out Color)` | Safe HEX → Color. |

### Rich Text (for TMP/UI)
| Method | Description |
|--------|-------------|
| `string Bold(this string)` | Wrap in `<b>` tags. |
| `string Italic(this string)` | Wrap in `<i>` tags. |
| `string Size(this string, int size)` | Wrap in `<size>` tags. |
| `string SetColor(this string, Color)` | Wrap in `<color>` tags. |
| `string Rainbow(this string)` | Each character gets a rainbow color. |
| `string Gradient(this string, Color start, Color end)` | Gradient across characters. |
| `string RandomColors(this string)` | Each character gets a random color. |

---

## Examples

### Code
```csharp
string name = "playerHealth".SplitCamelCase(); // "player Health"
string short = "Very long text here".Truncate(10); // "Very lo..."
Color c = "#FF0000".ToColor(); // Color.red
string fancy = "Hello".Bold().SetColor(Color.green); // <color=#00FF00><b>Hello</b></color>
string rainbow = "Rainbow!".Rainbow(); // each char colored
int val = "42".ToInt(); // 42
```

---

Parsing note: earlier versions of `ToFloat`/`ToInt` used only the current culture, so `"1.5"` failed on Russian systems
and silently read as `15` on German ones (where `.` is the group separator). Inputs that only parse in the current culture keep working through the fallback; the one behavioural
difference is a dot-grouped number such as `"1.500"` on a dot-grouping locale, which now reads as `1.5`.

## See Also
- [ColorExtension](ColorExtension.md) — `ToHexString()` for reverse conversion
- ← [Extensions](README.md)
