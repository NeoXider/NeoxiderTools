### Класс TimeReward
- **Пространство имен**: `Neo.Bonus`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs`

`TimeReward` — компонент таймера награды с сохранением времени в `SaveProvider`, расширенным публичным API управления жизненным циклом таймера и поддержкой двух режимов запуска кулдауна.

## Что улучшено
- Добавлены публичные методы управления: `StartTime()`, `StopTime()`, `PauseTime()`, `ResumeTime()`, `RestartTime()`, `SetRewardAvailableNow()`, `RefreshTimeState()`, `SetAdditionalKey(...)`.
- Добавлены статусные публичные свойства: `IsTimerRunning`, `IsTimerPaused`, `IsRewardAvailable`, `RewardTimeKey`, `SaveTimeOnTakeReward`.
- Добавлены события таймера: `OnTimerStarted`, `OnTimerStopped`, `OnTimerPaused`, `OnTimerResumed`.
- Сохранение времени переведено на UTC `round-trip` формат (`"o"`) с обратной совместимостью чтения старых сохранений.

## Ключевой режим (по вашему запросу)
- `saveTimeOnTakeReward = true` (по умолчанию): при `TakeReward()` время сразу сохраняется, и кулдаун стартует автоматически.
- `saveTimeOnTakeReward = false`: `TakeReward()` только подтверждает выдачу награды, а кулдаун стартует через `StartTime()` (если включен флаг `saveTimeOnStartWhenSaveOnTakeDisabled`).

## Основные поля (Inspector)
- `secondsToWaitForReward`: длительность кулдауна в секундах.
- `updateTime`: интервал обновления таймера.
- `startTakeReward`: попытка забрать награду при `Start()`.
- `startTimerOnStart`: запуск таймера автоматически при `Start()`.
- `saveTimeOnTakeReward`: сохранять ли время в момент взятия.
- `saveTimeOnStartWhenSaveOnTakeDisabled`: при отключенном сохранении на взятии, сохранять ли время в `StartTime()`.
- `timeLeft`: оставшееся время до награды.

## Публичные методы
- `TakeReward()`: попытка взять награду.
- `Take()`: обертка для UnityEvent.
- `CanTakeReward()`: проверка доступности награды.
- `GetSecondsUntilReward()`: оставшееся время до награды.
- `StartTime()`: запуск/возобновление отсчета.
- `StopTime()`: остановка отсчета.
- `PauseTime()`: пауза.
- `ResumeTime()`: снятие с паузы.
- `RestartTime()`: перезапуск кулдауна от текущего времени.
- `SetRewardAvailableNow()`: мгновенно сделать награду доступной (очистить сохраненный timestamp).
- `RefreshTimeState()`: принудительно пересчитать таймер и отправить события.
- `SetAdditionalKey(string addKey, bool refreshAfterChange = true)`: сменить суффикс ключа сохранения.
- `FormatTime(int seconds)` / `FormatTime(float seconds)` / `FormatTime(float, TimeFormat, string, bool trimLeadingZeros)`: форматирование времени.
- `GetFormattedTimeLeft(bool trimLeadingZeros)`: оставшееся время в формате по настройкам `_displayTimeFormat` / `_displaySeparator`.
- `TryGetLastRewardTimeUtc(out DateTime)`: чтение сохранённого timestamp последней награды.
- `GetElapsedSinceLastReward()`: секунды с момента последней награды.

## Inspector (дополнительно)
- `_displayTimeFormat`: формат вывода (TimeFormat).
- `_displaySeparator`: разделитель для отображения.

## Unity Events
- `OnTimeUpdated(float)`: периодическое обновление остатка времени.
- `OnRewardClaimed`: успешное получение награды.
- `OnRewardAvailable`: награда стала доступной.
- `OnTimerStarted`: таймер запущен.
- `OnTimerStopped`: таймер остановлен.
- `OnTimerPaused`: таймер поставлен на паузу.
- `OnTimerResumed`: таймер возобновлен.
