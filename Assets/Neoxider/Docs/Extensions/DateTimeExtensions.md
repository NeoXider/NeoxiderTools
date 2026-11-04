# DateTimeExtensions

**What it is:** UTC helpers for persisting timestamps and measuring elapsed or remaining seconds. File:
`Scripts/Extensions/DateTimeExtensions.cs`, namespace `Neo.Extensions`.

**How to use:**
1. Save timestamps with `DateTime.UtcNow.ToRoundTripUtcString()`.
2. Load them with `raw.TryParseUtcRoundTrip(out DateTime utc)`.
3. Pass the current time explicitly (`nowUtc`) to the measuring helpers, which keeps them testable.

---

## Methods

| Method | Description |
|--------|-------------|
| `string ToRoundTripUtcString(this DateTime)` | ISO round-trip (`"o"`) string in UTC, invariant culture. |
| `bool TryParseUtcRoundTrip(this string, out DateTime utc)` | Parses the round-trip format, then invariant and current-culture fallbacks for legacy saves; the result is UTC. |
| `float GetSecondsSinceUtc(this DateTime utc, DateTime nowUtc)` | Seconds from `utc` to `nowUtc` (negative when `utc` is in the future). |
| `float GetSecondsUntilUtc(this DateTime targetUtc, DateTime nowUtc)` | Seconds left until `targetUtc`. |
| `DateTime EnsureUtc(this DateTime)` | Local values are converted, unspecified values are treated as UTC. |

```csharp
string saved = DateTime.UtcNow.ToRoundTripUtcString();
if (saved.TryParseUtcRoundTrip(out DateTime lastClaim))
{
    float waited = lastClaim.GetSecondsSinceUtc(DateTime.UtcNow);
}
```

## See Also
- [CooldownRewardExtensions](CooldownRewardExtensions.md)
- ← [Extensions](README.md)
