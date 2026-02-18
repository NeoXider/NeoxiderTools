# CooldownReward

Компонент наград по кулдауну на базе [TimerObject](../Tools/Time/README.md). Один компонент объединяет настройки кулдауна, сохранение по UTC и логику наград.

**Добавить в сцену:** GameObject → Neoxider → Bonus → CooldownReward.

## Основное

- **Таймер:** `duration` (кулдаун в секундах), `updateInterval`, пауза/старт/стоп через базовый TimerObject (режим RealTime).
- **Сохранение:** автоматически по ключу `LastRewardTime` + суффикс; корректно после перезапуска игры.
- **Награды:** `TakeReward()`, `GetClaimableCount()`, `GetSecondsUntilReward()`, `GetFormattedTimeLeft()`, события `OnRewardAvailable`, `OnRewardsClaimed`, `OnTimeUpdated` и др.

## Поля (Inspector)

- **Reward Settings:** кулдаун, интервал обновления, лимит наград за раз (`_maxRewardsPerTake`), суффикс ключа сохранения.
- **Save:** сохранение времени при взятии награды, при старте и т.д.
- **Events:** подписка на обновление времени и на выдачу наград.

## См. также

- [TimerObject](../Tools/Time/TimerObject.md) — базовый таймер.
- [README модуля](./README.md) — сравнение с устаревшим TimeReward.
