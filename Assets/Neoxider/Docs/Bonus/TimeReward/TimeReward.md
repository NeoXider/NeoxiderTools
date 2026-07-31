# TimeReward

**Purpose:** Legacy time reward component kept for existing scenes and prefabs.

Use [CooldownReward](./CooldownReward.md) for new setups. `TimeReward` is hidden from new `Add Component` / `CreateFromMenu` flows so new scenes do not accidentally depend on the legacy implementation.

## Setup

- Keep the component only on old scenes/prefabs that already use it.
- For new scenes, add `CooldownReward` instead.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `IsRewardAvailable` | Is Reward Available. |
| `IsTimerPaused` | Is Timer Paused. |
| `IsTimerRunning` | Is Timer Running. |
| `OnRewardAvailable` | On Reward Available. |
| `OnRewardClaimed` | On Reward Claimed. |
| `OnRewardsClaimed` | On Rewards Claimed. |
| `OnTimeUpdated` | On Time Updated. |
| `OnTimerPaused` | On Timer Paused. |
| `OnTimerResumed` | On Timer Resumed. |
| `OnTimerStarted` | On Timer Started. |
| `OnTimerStopped` | On Timer Stopped. |
| `RewardTimeKey` | Reward Time Key. |
| `SaveTimeOnTakeReward` | Save Time On Take Reward. |
| `_addKey` | Add Key. |
| `_displaySeparator` | Display Separator. |
| `_displayTimeFormat` | Display Time Format. |
| `_rewardAvailableOnStart` | Reward Available On Start. |
| `accumulated` | Accumulated. |
| `lastRewardTimeStr` | Last Reward Time Str. |
| `secondsToWaitForReward` | Seconds To Wait For Reward. |
| `startTakeReward` | Start Take Reward. |
| `timeLeft` | Time Left. |
| `updateTime` | Update Time. |

## See Also

- [Module Root](../README.md)
