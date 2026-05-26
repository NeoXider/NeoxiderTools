# CooldownReward

**Что это:** награда по cooldown на базе `TimerObject`. Компонент хранит время последней выдачи через `SaveProvider`, считает накопленные награды по UTC и умеет выдавать одну или несколько наград за раз.

Файл: `Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs`

`CooldownReward` заменяет legacy `TimeReward` для новых сцен.

## Основные настройки

| Поле | Назначение |
|------|------------|
| `_cooldownSeconds` | Длительность cooldown в секундах. |
| `_updateInterval` | Частота обновления `RemainingTime`. |
| `_rewardAvailableOnStart` | Если нет сохранения, награда доступна сразу. |
| `_maxRewardsPerTake` | `-1` = выдать все накопленные, `1` = одну, `N` = не больше N за раз. |
| `_addKey` | Суффикс save key, чтобы несколько reward-компонентов не конфликтовали. |
| `_startTakeReward` | Попробовать забрать награду в `Start`. |
| `_startTimerOnStart` | Запустить таймер в `Start`. |
| `_saveTimeOnTakeReward` | Сохранять новое время при успешном `TakeReward`. |
| `_saveTimeOnStartWhenSaveOnTakeDisabled` | При ручном старте сохранять время, если save-on-take выключен. |
| `_displayTimeFormat`, `_displaySeparator` | Формат строки для `GetFormattedTimeLeft`. |

## События

| Событие | Когда вызывается |
|---------|------------------|
| `OnRewardClaimed` | Один раз на каждую выданную награду. |
| `OnRewardsClaimed(int)` | Один раз за `TakeReward` с количеством выданных наград. |
| `OnRewardAvailable` | Когда награда стала доступна. |
| `RemainingTime.OnChanged` | При изменении оставшегося времени. |

## Публичный API

```csharp
bool ok = reward.TakeReward();
bool canTake = reward.CanTakeReward();
int count = reward.GetClaimableCount();
float seconds = reward.GetSecondsUntilReward();
string label = reward.GetFormattedTimeLeft(trimLeadingZeros: true);

reward.StartTime();
reward.StopTime();
reward.PauseTime();
reward.ResumeTime();
reward.RestartTime();
reward.SetRewardAvailableNow();
reward.SetAdditionalKey("DailyLogin");
```

`Take()` оставлен как UnityEvent-friendly wrapper над `TakeReward()`.

## Типовые сценарии

- Daily reward: `_cooldownSeconds = 86400`, `_rewardAvailableOnStart = true`, `_maxRewardsPerTake = 1`.
- Классический cooldown: `_cooldownSeconds = 30`, `_rewardAvailableOnStart = false`, `_startTimerOnStart = true`.
- Idle/clicker накопление: `_maxRewardsPerTake = -1`, UI показывает `GetClaimableCount()`.
- Ручной cooldown: `_saveTimeOnTakeReward = false`, стартуйте ожидание через `StartTime()` после внешнего условия.

## Save

Ключ времени строится как `LastRewardTime + _addKey`. Компонент использует real-time режим и UTC, поэтому cooldown продолжает идти между сессиями.

## См. также

- [TimeReward](./TimeReward.md)
- [TimerObject](../../Tools/Time/TimerObject.md)
