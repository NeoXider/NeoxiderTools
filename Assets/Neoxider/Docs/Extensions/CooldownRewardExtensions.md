# CooldownRewardExtensions

**What it is:** pure helpers for cooldown rewards that accumulate while the player is away: how many cycles
elapsed, how many to give at once and the new last-claim time. File:
`Scripts/Extensions/CooldownRewardExtensions.cs`, namespace `Neo.Extensions`.

**How to use:**
1. Count elapsed cycles with `lastClaimUtc.GetAccumulatedClaimCount(cooldownSeconds, DateTime.UtcNow)`.
2. Cap them with `CapToMaxPerTake(count, maxPerTake)` (`-1` = no cap).
3. Grant the rewards, then store `lastClaimUtc.AdvanceLastClaimTime(given, cooldownSeconds)`.

---

## Methods

| Method | Description |
|--------|-------------|
| `int GetAccumulatedClaimCount(this DateTime lastClaimUtc, float cooldownSeconds, DateTime nowUtc)` | Full cycles elapsed, saturated at `int.MaxValue`; zero for a future timestamp or a non-positive/non-finite cooldown. |
| `int CapToMaxPerTake(int accumulated, int maxPerTake)` | `0` for no claims; `maxPerTake < 0` returns all. |
| `DateTime AdvanceLastClaimTime(this DateTime lastClaimUtc, int claimsGiven, float cooldownSeconds)` | Moves the timestamp forward by whole cycles so partial progress is kept; saturates at `DateTime.MaxValue`. |

Advancing by the given cycles (instead of resetting to "now") keeps the remainder of the current cycle, so a
player who claims late does not lose progress. `CooldownSchedule` in [Bonus/TimeReward](../Bonus/TimeReward/README.md) is built on these helpers.

## See Also
- [DateTimeExtensions](DateTimeExtensions.md)
- ← [Extensions](README.md)
