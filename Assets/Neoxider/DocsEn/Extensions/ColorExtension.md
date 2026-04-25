# ColorExtension

**Purpose:** Extension methods for Unity `Color` — modify individual channels, darken, lighten, and convert to HEX string.

---

## API

| Method | Description |
|--------|-------------|
| `Color WithAlpha(this Color, float alpha)` | New color with modified alpha (0–1). |
| `Color With(this Color, float? r, float? g, float? b, float? a)` | New color with selectively replaced RGBA channels. |
| `Color WithRGB(this Color, float r, float g, float b)` | Replace RGB, keep alpha. |
| `Color Darken(this Color, float amount)` | Darker version of the color (amount 0–1). |
| `Color Lighten(this Color, float amount)` | Lighter version of the color (amount 0–1). |
| `string ToHexString(this Color)` | Convert to HEX string `#RRGGBBAA`. |

---

## Examples

### Code
```csharp
Color c = Color.red;
Color semiTransparent = c.WithAlpha(0.5f);    // Red with 50% alpha
Color darker = c.Darken(0.3f);                 // 30% darker
Color lighter = c.Lighten(0.2f);               // 20% lighter
string hex = c.ToHexString();                   // "#FF0000FF"
Color custom = c.With(g: 0.5f);                 // Red + half green
```

---

## See Also
- [StringExtension](StringExtension.md) — `ToColor()` for reverse conversion
- ← [Extensions](README.md)
