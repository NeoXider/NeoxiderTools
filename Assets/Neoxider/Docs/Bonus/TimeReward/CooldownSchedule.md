# CooldownSchedule

**What it is:** a plain C# cooldown rule (`Neo.Bonus.CooldownSchedule`, `Scripts/Bonus/TimeReward/CooldownSchedule.cs`): how many rewards are claimable, how long until the next one, and claiming. Time comes from an injected `INeoClock` (the Tools/Time clock seam); without one it reads the tamper-resistant `NeoTrustedTime`. It needs no scene object, so code-first games, servers and tests can drive cooldowns directly. [CooldownReward](CooldownReward.md) and the legacy [TimeReward](TimeReward.md) count claims with the same rule (`CooldownSchedule.CountClaimable`).

**How to use:**
1. Create it: `new CooldownSchedule(cooldownSeconds: 3600f, maxClaimsPerTake: 1, availableInitially: true)`.
2. Restore a saved last claim with `TryLoadFrom(key)` (or start the first cooldown with `Restart()`).
3. Show `GetSecondsUntilNext()`; claim with `TryClaim(out int granted)` and grant `granted` rewards.
4. Persist after a claim with `SaveTo(key)` so the cooldown survives a restart.

---

## Members

| Member | Description |
|--------|-------------|
| `CooldownSchedule(float cooldownSeconds, int maxClaimsPerTake = 1, bool availableInitially = false, INeoClock clock = null)` | A non-positive or non-finite cooldown is never claimable. `maxClaimsPerTake`: `-1` all accumulated, `N` cap, `0` nothing. |
| `float CooldownSeconds`, `int MaxClaimsPerTake`, `bool AvailableInitially` | Settings (mutable). |
| `CooldownBacklogMode BacklogMode` | `ResetFromClaimTime` (default, like CooldownReward) or `PreserveElapsedRemainder` (like TimeReward). |
| `bool HasLastClaim`, `DateTime LastClaimUtc` | Time the current cooldown counts from. |
| `DateTime UtcNow` | Injected clock or `NeoTrustedTime.UtcNow`. |
| `int GetClaimableCount()` / `GetClaimableCount(DateTime nowUtc)` | Claimable rewards. Neither changes state; both return 0 while a `Claimed` listener of the current claim runs. |
| `double GetSecondsUntilNext()` / `GetSecondsUntilNext(DateTime nowUtc)` | 0 when a reward is claimable. |
| `bool TryClaim(out int granted)` / `TryClaim(DateTime nowUtc, out int granted)` | Consumes before `Claimed` runs; a nested claim from a listener is rejected. |
| `void Restart()`, `void MakeAvailableNow(DateTime nowUtc)`, `void Restore(DateTime)`, `void Clear()` | State control. |
| `void SaveTo(string key)`, `bool TryLoadFrom(string key)` | Round-trip UTC string through `SaveProvider` (same format as `TimeReward`'s `LastRewardTime<key>`). |
| `static int CountClaimable(...)` | The shared counting rule. |

Event: `Action<int> Claimed` with the number of granted rewards.

A schedule that was never claimed or started is claimable only when `AvailableInitially` is true; otherwise call `Restart()` to begin the first cooldown.

## Example

```csharp
CooldownSchedule daily = new(24f * 60f * 60f, maxClaimsPerTake: 1, availableInitially: true);
daily.TryLoadFrom("DailyGift");

if (daily.TryClaim(out int granted))
{
    daily.SaveTo("DailyGift"); // Persist the consumed cooldown before granting.
    wallet.Add(100 * granted);
}

label.text = TimeSpan.FromSeconds(daily.GetSecondsUntilNext()).ToString(@"hh\:mm\:ss");
```

Deterministic test with a fake clock:

```csharp
sealed class FakeClock : INeoClock
{
    public DateTime UtcNow { get; set; } = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public double MonotonicSeconds { get; set; }
}

FakeClock clock = new();
CooldownSchedule schedule = new(60f, maxClaimsPerTake: -1, clock: clock);
schedule.Restart();
clock.UtcNow = clock.UtcNow.AddSeconds(150);
Assert.That(schedule.GetClaimableCount(), Is.EqualTo(2));
```

## See also

- [CooldownReward](CooldownReward.md) — scene component (TimerObject based).
- [TimeReward README](README.md)
- [Tools/Time](../../Tools/Time/README.md)
