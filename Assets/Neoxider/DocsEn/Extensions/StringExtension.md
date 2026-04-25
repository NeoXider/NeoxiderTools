# StringExtension

**Purpose:** Extension methods for `string` — camelCase splitting, truncation, parsing, HEX-to-Color conversion, and rich text formatting (bold, italic, color, rainbow, gradient).

---

## API

### Text Manipulation
| Method | Description |
|--------|-------------|
| `string SplitCamelCase(this string)` | `"MyVar"` → `"My Var"`. |
| `bool IsNullOrEmptyAfterTrim(this string)` | Null/empty check after trim. |
| `string ToCamelCase(this string)` | First char to lowercase. |
| `string Truncate(this string, int maxLength)` | Truncate with `...`. |
| `bool IsNumeric(this string)` | Contains only digits. |
| `string Reverse(this string)` | Reverse the string. |
| `string RandomString(int length, string chars)` | Generate random string (static). |

### Parsing
| Method | Description |
|--------|-------------|
| `bool ToBool(this string)` | `"true"`, `"yes"`, `"1"` → `true`. |
| `int ToInt(this string, int default = 0)` | Safe int parse. |
| `float ToFloat(this string, float default = 0f)` | Safe float parse. |
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

## See Also
- [ColorExtension](ColorExtension.md) — `ToHexString()` for reverse conversion
- ← [Extensions](README.md)
