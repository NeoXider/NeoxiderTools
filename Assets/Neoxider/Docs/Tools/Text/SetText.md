# SetText

**Purpose:** A component for displaying formatted numeric and text values in `TMP_Text`. Supports thousand separators, decimal places, notation styles (Plain, Grouped, Scientific, IdleShort), percentages, currency, `BigInteger`, and animated number transitions via DOTween.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Text** | Reference to `TMP_Text` (auto-assigned). |
| **Separator** | Thousands separator (default `.`). |
| **Decimal** | Decimal places (0–10). |
| **Number Notation** | `Plain`, `Grouped` (1,000), `Scientific`, or `IdleShort` (1K, 1M). |
| **Rounding Mode / Trim Trailing Zeros** | Numeric rounding and optional fractional zero trimming. |
| **Decimal Separator** | Fractional separator (default `.`). Choose distinct group and decimal separators for unambiguous output. |
| **Start Add / End Add** | Prefix and suffix for the final text. |
| **Index Offset** | Added exactly once to `Set(int)` using a wider intermediate. `SetInteger(long)` accepts the final value without this offset. |
| **Time Anim** | Duration of the number transition animation (DOTween). |
| **Ease** | Animation easing curve. |
| **On Enable Anim** | Default enabled: defer animation while the component/hierarchy is inactive and resume on enable; a new deferred number starts from zero. Disabled: animation can run while hidden. |
| **Log Warnings** | Opt-in diagnostics for missing targets or invalid `BigInteger` strings; also subject to `NeoDiagnostics` settings. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Set(int value)` | Animate an integer plus `IndexOffset`; commit the exact integer result on completion. |
| `void SetInteger(long value)` | Animate an already-computed final integer and retain its exact completed display. Does not apply `IndexOffset`. |
| `void Set(float value)` | Animate a float and settle the requested float target on completion using the current formatting options. |
| `void Set(string value = "0")` | Immediately replace the text and cancel the number animation; Inspector prefix/suffix still apply. |
| `void SetPercentage(float value, bool addPercentSign = true)` | Immediately display a clamped percentage (0–100), optionally with `%`. |
| `void SetCurrency(float value, string currencySymbol = "$")` | Immediately display a value with the supplied currency prefix. |
| `void SetBigInteger(BigInteger value)` | Immediately display an exact large integer using the component format. |
| `void SetBigInteger(string value)` | Parse an integer; empty input displays the default `"0"`; invalid input preserves the current display. |
| `void SetFormatted(float value, NumberFormatOptions options)` | Immediately display a number using explicit options plus the Inspector affixes. |
| `void SetFormatted(BigInteger value, NumberFormatOptions options)` | The same operation for an exact large integer. |
| `Separator`, `DecimalPlaces`, `NumberNotationStyle`, `RoundingMode` | Redraw the retained numeric value immediately; decimal places are clamped to 0–10. |
| `IndexOffset` | Offset applied to future `Set(int)` calls. |
| `currentNum` | Approximate animated float value, including after an integer finishes with exact text. Immediate string/currency/percentage/large integer writes do not replace this animation state. |
| `void Clear()` | Cancel animation and clear the text, including affixes. |

Formatting changes use the retained numeric source from `Set(int/float)`, `SetInteger`, `SetPercentage`, `SetCurrency` or `SetBigInteger`. They preserve currency/percentage decoration and exact completed integer digits, without restarting animation or parsing culture-dependent text. Literal strings and `SetFormatted` results stay as supplied; call `SetFormatted` again to change its explicit options. `Clear()` forgets the retained numeric source.

All animated numeric paths use float interpolation. In 11.0, `Set(int)` and the additive `SetInteger(long)` commit the exact integer through the existing `BigInteger` formatter when the tween completes, and subsequent formatting-only redraws keep that typed value. For example, `Set(16777216)` with `IndexOffset = 1` finishes as `16777217`, while `currentNum` can remain `16777216f`. An integer can briefly produce approximate intermediate strings; `Set(float)` keeps float-only behavior. Use `SetBigInteger` or `SetFormatted(BigInteger, ...)` for immediate exact text, including integers beyond `long`.

For `Set(float)`, completion settles `currentNum` to the supplied float before the final formatting update. This avoids losing a small target such as `9.5f` when interpolating from a much larger value. Decimal places, rounding, notation and affixes still determine the displayed string; this does not turn a float request into an integer request or change any serialized format setting. A superseded tween's setter and completion callback cannot publish through the newer value's formatter.

Immediate writes and newer animations cancel an unfinished tween and its exact final commit. Completion publishes a final `OnTextUpdated` if the exact string differs from the last interpolated text; it does not publish a redundant identical completion string. Tweens belong to the component and are released when it is destroyed. The animated Inspector button is available in Play Mode.

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnTextUpdated` | `string` | Fired after every text update. |

## Examples

### No-Code Example (Inspector)
Attach `SetText` to a TextMeshPro object and assign **Text** if it is on a different object. Set **Separator** to a space, **Decimal** to `0`, and **Number Notation** to `IdleShort`. Wire an integer score event to `SetText.Set(int)` for the exact integer completion path, or use **Dynamic float** → `Set(float)` for a float source. `SetInteger(long)` is the C# path for a final integer that already includes its offset. Wire a string event to `Set(string)` or a reset event to `Clear()` for immediate replacement. `OnTextUpdated(string)` can drive another label or a view-specific listener.

### Code Example
```csharp
using Neo.Extensions;
using Neo.Tools;
using UnityEngine;

[SerializeField] private SetText _goldText;

public void UpdateGold(int amount)
{
    _goldText.Set(amount);
}

public void ShowDisplayedLevel(int zeroBasedLevel)
{
    _goldText.SetInteger((long)zeroBasedLevel + 1); // final value; IndexOffset is not added
}

public void ShowPrice(float amount)
{
    _goldText.DecimalPlaces = 2;
    _goldText.SetCurrency(amount, "USD ");
    _goldText.Separator = ","; // redraws the same price immediately
}
```

## Derived component lifecycle

`OnEnable` and `OnDisable` are protected virtual hooks in 11.0. Derived views must call the corresponding base hook once so queued and active tweens retain the same pause/resume behavior. Keep listener registration, reactive subscription disposal and deferred-wait cleanup in the derived view; the base hooks do not subscribe to a value source.

```csharp
protected override void OnEnable()
{
    base.OnEnable();
    // Attach this view's value listener once.
}

protected override void OnDisable()
{
    base.OnDisable();
    // Remove this view's listener and stop its deferred source lookup.
}
```

## See Also
- [TimeToText](TimeToText.md)
- ← [Tools/Text](README.md)
