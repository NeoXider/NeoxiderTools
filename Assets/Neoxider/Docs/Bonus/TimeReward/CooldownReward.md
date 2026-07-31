# CooldownReward

`CooldownReward` is a persistent real-time cooldown built on `TimerObject`. It supports manual claims, continuous auto-claim regeneration, offline accumulation, capped claims per take, and a reactive countdown for UI.

## Typical uses

- Energy, lives, or stamina regeneration.
- Timed gifts and recurring bonuses.
- Offline reward accumulation.
- A claim button that becomes available after a cooldown.

## Inspector setup

| Field | Description |
|-------|-------------|
| **Cooldown Seconds** | Seconds between reward claims. |
| **Update Interval** | How frequently the reactive countdown updates. |
| **Reward Available On Start** | Makes a new unsaved reward immediately claimable. |
| **Max Rewards Per Take** | `-1` takes every accumulated reward, `1` takes one, and positive `N` caps a take at `N`. |
| **Add Key** | Suffix used to isolate this reward's persistent save keys. It must be unique per independent cooldown. |
| **Start Take Reward** | Attempts an accumulated/manual claim during `Start`. |
| **Auto Claim** | Claims immediately when the timer completes and then re-arms the cooldown. |
| **Start Timer On Start** | Starts or resumes the cooldown automatically. |
| **Save Time On Take Reward** | Persists a new real-time end whenever a claim succeeds. Keep enabled for continuous regeneration. |

Wire `OnRewardClaimed` to the effect that should happen once per granted reward, such as `Money.Add(1)`. `OnRewardsClaimed(int)` reports the batch size once after an accumulated take.

## Runtime API

| Member | Description |
|--------|-------------|
| `bool TakeReward()` | Claims available rewards and returns whether at least one was granted. |
| `void Take()` | UnityEvent-friendly wrapper around `TakeReward`. |
| `bool CanTakeReward()` | Returns whether at least one reward is currently claimable. |
| `void RestartTime()` | Starts a fresh cooldown from now and persists its real-time end. |
| `void SetRewardAvailableNow()` | Clears the saved cooldown and exposes an immediately available state. |
| `void RefreshTimeState()` | Recomputes availability and publishes the current remaining time. |
| `float GetSecondsUntilReward()` | Current remaining seconds. |
| `string GetFormattedTimeLeft(bool trimLeadingZeros = false)` | Remaining time formatted with the component settings. |
| `int GetClaimableCount()` | Number of accumulated rewards after applying `MaxRewardsPerTake`. |
| `ReactivePropertyFloat RemainingTime` | Reactive countdown updated by the inherited `TimerObject` clock. |
| `bool AutoClaim` | Runtime get/set for continuous regeneration. |
| `float CooldownSeconds` | Runtime get/set for cooldown duration. |

## Code-first example

```csharp
using Neo.Bonus;
using Neo.Shop;
using UnityEngine;

public sealed class EnergyRegenSetup : MonoBehaviour
{
    [SerializeField] private CooldownReward reward;
    [SerializeField] private Money energy;

    private void Awake()
    {
        reward.AutoClaim = true;
        reward.OnRewardClaimed.AddListener(() => energy.Add(1f));
        reward.RemainingTime.AddListener(seconds =>
            Debug.Log($"Next energy in {seconds:0.0}s"));
    }
}
```

For a ready wallet + countdown binding, use `ResourceRegen` on the same object as `CooldownReward` and `Money`.

## Inheritance behavior

`CooldownReward` inherits the normal `TimerObject` Unity update. No project-side polling component is required. If a custom subclass overrides a timer lifecycle hook, it must call the corresponding `base` implementation unless it intentionally replaces that behavior.

## See also

- [ResourceRegen](ResourceRegen.md)
- [TimerObject](../../Tools/Time/TimerObject.md)
- [TimeToText](../../Tools/Text/TimeToText.md)
