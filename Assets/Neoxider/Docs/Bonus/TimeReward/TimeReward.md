# TimeReward

**What it is:** the legacy, `[Obsolete]` time reward component (`MonoBehaviour`, `Scripts/Bonus/TimeReward/TimeReward.cs`) kept for existing scenes and prefabs. It counts claims with the shared `CooldownSchedule.CountClaimable` rule and preserves unclaimed cooldown remainder between takes.

**How to use:**
1. Keep the component only on old scenes/prefabs that already use it; for new scenes add [CooldownReward](./CooldownReward.md), or use [CooldownSchedule](./CooldownSchedule.md) with `BacklogMode = PreserveElapsedRemainder` from code.
2. Bind a claim button to `Take()` and grant through `OnRewardClaimed` / `OnRewardsClaimed`.

---

`TimeReward` is hidden from new `Add Component` / `CreateFromMenu` flows so new scenes do not accidentally depend on the legacy implementation.

Deactivating the object (or disabling the component) only suspends the update loop: `IsTimerRunning` and
`IsTimerPaused` keep their values, and reactivating resumes ticking. `StartTime()`, `PauseTime()`,
`ResumeTime()` and `StopTime()` called while the object is inactive take effect immediately (with their events)
and are honoured on reactivation, so a timer stopped while inactive stays stopped. Only one update loop ever
runs, and it reuses one `WaitForSecondsRealtime` instead of allocating one per tick.

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
| `lastRewardTimeStr` | Last Reward Time Str. |
| `secondsToWaitForReward` | Seconds To Wait For Reward. |
| `startTakeReward` | Start Take Reward. |
| `timeLeft` | Time Left. |
| `updateTime` | Update Time. |

## Runtime claim contract

`TakeReward()` returns true when it grants at least one reward; `Take()` is its UnityEvent wrapper.
`GetClaimableCount()` and `CanTakeReward()` support custom claim buttons. Timestamps use
`LastRewardTime<Add Key>` and advance by the number of consumed cooldown cycles, retaining unclaimed
accumulation. Extremely large counts saturate at `int.MaxValue`; configure a practical per-take cap.

Timestamp advancement delegates to `CooldownRewardExtensions.AdvanceLastClaimTime`: multiplication
is widened before conversion to ticks, fractional cycles retain tick precision, and values beyond the
DateTime range saturate at UTC `DateTime.MaxValue`. The timestamp still advances from the previous
claim, rather than resetting to now. The existing key and round-trip timestamp format are unchanged.

Сдвиг времени выполняется общим helper с точностью до тиков и ограничением `DateTime.MaxValue`.
Неиспользованный остаток времени сохраняется; задавайте разумный лимит выдач за один вызов.

Consumption is recorded before `OnRewardClaimed` / `OnRewardsClaimed` run, and recursive takes are
rejected. When `SaveTimeOnTakeReward` is false, a successful take blocks subsequent direct claims until
`StartTime`, `RestartTime`, or `SetRewardAvailableNow` explicitly re-arms the reward. A zero claim cap also
blocks the first available reward. An exception in a grant listener does not restore the consumed claim.

`CooldownReward` uses an end timestamp with an `_rt` suffix and restarts a full cooldown on a take.
Migration between these components needs an explicit saved-state conversion.

## See Also

- [Module Root](../README.md)
