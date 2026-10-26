# Numeric, time and random edge cases

Use these helpers through `using Neo.Extensions;` and reference `Neo.Extensions` from a custom asmdef. This topic supplements the [Extensions entry point](README.md).

## Number formatting

`ToPrettyString(NumberFormatOptions)` supports `int`, `long`, `float`, `double`, `decimal` and `BigInteger`. Choose Plain, Grouped, IdleShort or Scientific notation; decimal places are clamped to 0–12. `ToIdleString` is the short-notation convenience API.

`NumberFormatOptions` is a value type. Formatting adjusts a local copy; the caller's notation, rounding and separators remain unchanged. Preserve the original numeric type when passing a value: converting a large integer to float before formatting loses digits that the formatter cannot recover.

```csharp
NumberFormatOptions floor = new(NumberNotation.IdleShort, 1, NumberRoundingMode.ToNegativeInfinity);
string balance = (-1251m).ToPrettyString(floor); // "-1.3K"

NumberFormatOptions scientific = new(NumberNotation.Scientific, 2);
string tiny = (1.23e-30).ToPrettyString(scientific); // "1.23e-030"

NumberFormatOptions grouped = new(NumberNotation.Grouped, 4, trimTrailingZeros: true);
string price = 1234.5m.ToPrettyString(grouped); // "1,234.5"

NumberFormatOptions whole = new(NumberNotation.Plain, 0);
string exactCount = 16777217L.ToPrettyString(whole); // "16777217"
```

| Contract | Behavior |
| --- | --- |
| Directed rounding | Ceiling and floor apply to the signed number, including idle/scientific mantissas. `-1251` with one idle decimal is `-1.2K` for ceiling and `-1.3K` for floor. |
| BigInteger rounding | Discarded digits participate in rounding. Scientific `12501` with one decimal rounds to `1.3e4`; the exact midpoint `12500` rounds to `1.2e4` with ToEven. |
| Decimal boundaries | Directed rounding scales the fractional part, so formatting `decimal.MinValue`/`MaxValue` does not overflow a temporary product. |
| Small/large doubles | Values below the decimal precision needed by scientific formatting, and values above decimal range, retain a double formatting path. Subnormals such as `double.Epsilon` keep their scientific significand. |
| Exponent spelling | The double fallback retains signed, at least three-digit exponents such as `e-030` and `e+100`; the decimal/BigInteger path uses `e3`. |
| Trailing zeros | Plain, Grouped, IdleShort and Scientific honor `TrimTrailingZeros`. Trimming precedes insertion of custom separators, so group digits are preserved even when separators overlap. |
| Affixes/nonfinite values | Prefix and suffix apply consistently to `NaN`, `Infinity` and `-Infinity`, including float/double extension and core entry points. |
| Integer formatting | BigInteger Plain/Grouped and BigInteger idle values below 1000 preserve integer-only spelling. Int/long `ToIdleString` delegates to BigInteger; use the decimal path for fixed fractional padding below 1000. |

Floating-point values retain their usual precision limits. For zero, or a magnitude of at least `1e-15` with a representable decimal cast, the float/double overloads use the decimal path; other finite values use the double fallback. Use `decimal` from the start for an explicitly decimal quantity and `BigInteger` for exact arbitrary-size integer balances. Custom separators control presentation, including potentially ambiguous combinations; these strings are display values, not a numeric serialization format.

## Float duration display

`seconds.FormatTime(format, separator, trimLeadingZeros)` truncates whole seconds and fractional units. Negative values, negative infinity and NaN display as zero. Values above `int.MaxValue` seconds, including positive infinity, saturate to exactly `2147483647` seconds before conversion. For example, the maximum DaysHoursMinutesSeconds result is `24855:03:14:07`.

Existing fractional formats are preserved:

- `TimeFormat.Milliseconds`: the fractional second as two-digit **centiseconds** (00–99).
- `TimeFormat.SecondsMilliseconds`: total seconds and two-digit centiseconds.
- `TimeFormat.MinutesSecondsMilliseconds`: total minutes, seconds and three-digit milliseconds.

An empty separator falls back to `:`. Trimming affects the first token of a multi-token clock. A float loses fractional precision as its magnitude grows; use `TimeSpan` when exact whole milliseconds or longer durations are needed.

`TimeParsingExtensions.TryParseDuration` accepts single seconds or 2–4 parts: MM:SS, HH:MM:SS, DD:HH:MM:SS. Lower seconds/minutes must be below 60, and hours in the four-part form below 24. Nonfinite, negative and overflowing values return `false` with zero output. Single seconds also have a current-culture fallback; separated tokens use invariant culture. Fractional display tokens are not a general parser round-trip format.

## TimeSpan formatting

All `ToCompactString` overloads and `ToClockString` support `TimeSpan.MinValue` and `MaxValue`. A leading minus sign applies to the complete duration. Total-unit output uses integer tick division with `long` totals rather than float conversions or `int` casts.

```csharp
string total = TimeSpan.MaxValue.ToCompactString(TimeFormat.Seconds); // "922337203685s"
string clock = TimeSpan.MinValue.ToClockString(); // "-256204778:48:05"
string days = TimeSpan.MinValue.ToClockString(true); // "-10675199:02:48:05"
```

Unlike float `FormatTime`, TimeSpan `TimeFormat.Milliseconds` means total real milliseconds; SecondsMilliseconds uses total seconds plus the real millisecond component. TimeSpan display discards sub-millisecond ticks. The unrepresentable positive magnitude of MinValue is displayed using MaxValue: their absolute magnitudes differ by one tick, with identical whole-millisecond and larger components.

## Normalization, snapping and decimal places

`NormalizeToUnit`, `NormalizeToRange`, `Denormalize` and `Remap` retain finite-input validation and support ranges spanning `-float.MaxValue` to `float.MaxValue`. Intermediate range arithmetic uses double precision; midpoint normalization is 0.5 and midpoint denormalization is zero.

`RoundToDecimal` keeps Unity rounding for ordinary inputs, uses a double scaling fallback when a finite float product overflows, and returns the original value beyond 45 places. Negative places throw. Nonfinite input remains nonfinite.

Integer `Snap` calculates quotient/remainder without converting the integer to float. Midpoints round to an even multiple. An unrepresentable rounded result saturates to the nearest int bound, which may itself not be a multiple of the step. Float `Snap` keeps ordinary Unity rounding, uses a double fallback when necessary and saturates overflow to a finite float bound. Both return the original value for a non-positive step.

## UTC timestamps and accumulated rewards

`EnsureUtc` preserves UTC values, converts Local values and treats Unspecified values as already UTC. `ToRoundTripUtcString` writes invariant round-trip ISO text. `TryParseUtcRoundTrip` accepts that representation and intentional legacy fallbacks, including current culture. Use round-trip text for new saved values.

`GetAccumulatedClaimCount` returns full elapsed cooldown cycles, capped at `int.MaxValue`; non-positive/nonfinite cooldowns and a future last-claim time yield zero. `CapToMaxPerTake` accepts a negative maximum for no limit.

`AdvanceLastClaimTime` normalizes UTC, multiplies claim count in double precision, discards sub-tick fractions and saturates at UTC `DateTime.MaxValue`. Invalid cooldowns or non-positive claim counts leave the normalized timestamp unchanged. Advancing 16777217 claims of one second preserves every claim; the count is not first rounded to float. DateTime cannot advance by less than one tick, so choose cooldowns representable at the precision required by your reward system.

```csharp
DateTime now = DateTime.UtcNow;
int available = lastClaimUtc.GetAccumulatedClaimCount(60f, now);
int given = CooldownRewardExtensions.CapToMaxPerTake(available, 3);
DateTime nextLastClaim = lastClaimUtc.AdvanceLastClaimTime(given, 60f);
```

The caller persists the returned time and applies reward transactions. Pass the application's trusted UTC clock instead of `DateTime.UtcNow` when rewards use one. Consumers should delegate both accumulated-count calculation and last-claim advancement to these helpers, so UTC normalization, tick precision and saturation stay consistent. These helpers do not own a clock or a save provider.

## Random selection and shape sampling

`GetRandomWeightedIndex` accepts finite, non-negative float weights with a positive total. Null/empty lists, invalid weights or an all-zero distribution return `-1`. A nonfinite entry invalidates the entire distribution. Totals accumulate in double precision, so two `float.MaxValue` weights remain selectable. Zero-weight entries remain disabled, including at Unity Random's inclusive 1.0 endpoint. Element overloads return `default` when selection fails; the parallel list must match the item count.

`Chance` rejects NaN and probabilities outside [0, 1]. Zero is always false and one always true. It still consumes a random sample for these valid endpoints. `RandomizeBetween(int.MinValue)` samples the full representable integer range without negation overflow; ordinary integer ranges retain Unity's exclusive upper-bound convention.

Collection randomization and shape sampling use Unity's shared random state and belong on its supported Unity thread. The enum-value cache resets at subsystem registration. In-place Shuffle requires a writable list; `Shuffle(false)` and `GetRandomElements` use a copy. A weight selector is evaluated once per item per selection and allocates a temporary weights array.

For mutable chance entries, explicit sample evaluation or an injected random source, reuse [ChanceManager](../Tools/Random/ChanceManager.md) (`Neo.Tools`, assembly `Neo.Tools.Random`): `GetId(float)` accepts a sample, and `RandomProvider` supplies draws. Its entry sanitization and normalization policies differ from the strict validation above; choose them explicitly when adapting existing data. Extensions callers need no dependency on that module for the one-shot weighted helpers.

Circle interior samples are uniform by area; sphere interior samples use Unity's volume sampler. Bounds surface sampling chooses a coordinate axis and side equally, so it does not weight faces by surface area for unequal side lengths. Collider overloads sample their world-space axis-aligned bounds, which can include space outside the actual collider. Collider references must be valid.

## Scene and NoCode use

Call the same typed methods from scene components. For a timer label, configure the existing [TimeToText](../Tools/Text/TimeToText.md) component and call `Set(float)` from C#, or connect a `UnityEvent<float>` to its dynamic `Set` method in the Inspector. Format exact numeric balances through the original int/long/decimal/BigInteger overload and assign the resulting text. Number options remain serializable data; these helpers do not require scene references or an action graph.

`TimeToText` has its own input contract: `Set` rejects nonfinite or unsupported durations before changing the accepted time/display. Clock limits the non-negative display value to `int.MaxValue` seconds; Compact uses the TimeSpan range. With Allow Negative enabled, the magnitude is checked and displayed with a leading minus; otherwise negative input becomes zero. This differs from calling raw float `FormatTime`, which sanitizes or saturates invalid/overflowing values. Clock uses the legacy fractional-unit rules above; Compact uses TimeSpan's real millisecond units.

## Related APIs

- [Extensions entry point](README.md): module setup and canonical navigation.
- [PrimitiveExtensions](PrimitiveExtensions.md), [TimeSpanExtensions](TimeSpanExtensions.md) and [TimeParsingExtensions](TimeParsingExtensions.md): duration formatting and parsing.
- [DateTimeExtensions](DateTimeExtensions.md) and [CooldownRewardExtensions](CooldownRewardExtensions.md): UTC persistence and reward calculations.
- [RandomExtensions](RandomExtensions.md) and [RandomShapeExtensions](RandomShapeExtensions.md): selection and geometry sampling.

## Кратко по-русски

Направленное округление учитывает знак, малые double сохраняют значащие цифры, а веса обязаны быть конечными и неотрицательными. Float-формат времени ограничен `int.MaxValue` секунд; для больших длительностей используйте `TimeSpan`. Его форматирование поддерживает оба крайних значения. Продвижение времени награды проверяет cooldown, сохраняет точность количества и ограничивает результат максимальной UTC-датой.
