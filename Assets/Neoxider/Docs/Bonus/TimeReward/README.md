# Модуль "Награды по времени" (TimeReward / CooldownReward)

Модуль предоставляет систему выдачи наград по истечении кулдауна (таймер).

## Рекомендуемый компонент: CooldownReward

**CooldownReward** наследует [TimerObject](../Tools/Time/README.md) и использует его движок таймера в режиме RealTime: один компонент объединяет настройки кулдауна, сохранение по UTC и логику наград (накопление, лимит за раз, события). Добавить в сцену: GameObject → Neoxider → Bonus → CooldownReward.

- Таймер: `duration` (кулдаун в секундах), `updateInterval`, пауза/старт/стоп через базовый TimerObject.
- Сохранение: автоматически по ключу `LastRewardTime` + суффикс; режим RealTime (корректно после перезапуска игры).
- Награды: `TakeReward()`, `GetClaimableCount()`, `GetSecondsUntilReward()`, `GetFormattedTimeLeft()`, события `OnRewardAvailable`, `OnRewardsClaimed`, `OnTimeUpdated` и др.

## Устаревший компонент: TimeReward

**TimeReward** помечен как устаревший (`[Obsolete]`), но продолжает работать. Для нового кода рекомендуется **CooldownReward**. Отличия: TimeReward использует собственную корутину и отдельный расчёт времени из SaveProvider; CooldownReward строится на TimerObject и переиспользует его сохранение и обновление.

## Оглавление

- **CooldownReward** (рекомендуется): наследник TimerObject, награды по кулдауну с сохранением.
- [**TimeReward**](./TimeReward.md) (устарел): прежний класс модуля; оставлен для обратной совместимости.
