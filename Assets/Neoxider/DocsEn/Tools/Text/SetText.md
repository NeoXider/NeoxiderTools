# SetText

**Purpose:** A component for displaying formatted numeric and text values in `TMP_Text`. Supports thousand separators, decimal places, notation styles (Grouped, Scientific, Short), percentages, currency, `BigInteger`, and animated number transitions via DOTween.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Text** | Reference to `TMP_Text` (auto-assigned). |
| **Separator** | Thousands separator (default `.`). |
| **Decimal** | Decimal places (0–10). |
| **Number Notation** | Notation style: `Grouped` (1,000), `Scientific`, `Short` (1K, 1M). |
| **Start Add / End Add** | Prefix and suffix for the final text. |
| **Time Anim** | Duration of the number transition animation (DOTween). |
| **Ease** | Animation easing curve. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Set(int value)` | Set an integer value (animated). |
| `void Set(float value)` | Set a float value (animated). |
| `void Set(string value)` | Set a string value (no animation). |
| `void SetPercentage(float value, bool addSign)` | Set a percentage (0–100). |
| `void SetCurrency(float value, string symbol)` | Set a currency value with symbol. |
| `void SetBigInteger(BigInteger value)` | Set a large number. |
| `void Clear()` | Clear the text. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnTextUpdated` | `string` | Fired after every text update. |

## Examples

### No-Code Example (Inspector)
Attach `SetText` to a `TextMeshPro` object. Set `Separator = " "`, `Decimal = 0`, `Number Notation = Short`. Wire `ScoreManager.OnScoreChanged` to `SetText.Set(int)`. Score changes will now animate smoothly.

### Code Example
```csharp
[SerializeField] private SetText _goldText;

public void UpdateGold(int amount)
{
    _goldText.Set(amount);
}
```

## See Also
- [TimeToText](TimeToText.md)
- ← [Tools/Text](README.md)
