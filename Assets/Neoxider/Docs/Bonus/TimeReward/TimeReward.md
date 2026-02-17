### Класс TimeReward
- **Пространство имен**: `Neo.Bonus`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs`

`TimeReward` — компонент таймера награды с сохранением времени в `SaveProvider`, расширенным публичным API управления жизненным циклом таймера и поддержкой накопления наград (несколько наград за один Take).

---

## Полная схема работы

```mermaid
flowchart TD
    subgraph Start["Start()"]
        A[Старт компонента] --> B{Есть сохранённое время?}
        B -->|Нет| C{_rewardAvailableOnStart?}
        C -->|false| D[Сохранить UtcNow как lastRewardTime]
        C -->|true| E[Ничего не сохраняем]
        B -->|Да| E
        D --> F[startTakeReward?]
        E --> F
        F -->|Да| G[TakeReward]
        F -->|Нет| H[_startTimerOnStart?]
        G --> H
        H -->|Да| I[StartTime]
        H -->|Нет| J[RefreshTimeState]
        I --> J
    end

    subgraph Take["TakeReward()"]
        T1[GetClaimableCount] --> T2{count >= 1?}
        T2 -->|Нет| T3[return false]
        T2 -->|Да| T4[OnRewardClaimed × count]
        T4 --> T5[OnRewardsClaimed(count)]
        T5 --> T6{_saveTimeOnTakeReward?}
        T6 -->|Да| T7[lastUtc.AdvanceLastClaimTime(count, cooldown)]
        T7 --> T8[SaveLastRewardTime]
        T8 --> T9[RefreshTimeState]
        T6 -->|Нет| T10[_waitingForManualStart = true]
        T9 --> T11[return true]
        T10 --> T11
    end

    subgraph Timer["Таймер (каждые updateTime сек)"]
        R1[RefreshTimeState] --> R2[GetSecondsUntilReward → timeLeft]
        R2 --> R3[GetClaimableCount >= 1?]
        R3 -->|Да и не было| R4[OnRewardAvailable]
        R3 -->|Нет| R5[canTakeReward = false]
        R4 --> R6[OnTimeUpdated(timeLeft)]
        R5 --> R6
    end
```

**Кратко по шагам:**

1. **Первый запуск (нет сохранённого времени)**  
   - `_rewardAvailableOnStart == false`: в `Start()` сохраняется `UtcNow` → полный кулдаун до первой награды.  
   - `_rewardAvailableOnStart == true`: награда доступна сразу, при первом `TakeReward()` сохраняется время.

2. **Расчёт доступных наград**  
   - `elapsed = (now - lastRewardTime)`; `accumulated = floor(elapsed / cooldown)`;  
   - `claimsToGive = CapToMaxPerTake(accumulated, _maxRewardsPerTake)` (−1 = все, 1 = одна, N = не больше N за раз).

3. **TakeReward()**  
   - Вызывается `GetClaimableCount()` → выдаётся до этого количества наград;  
   - `OnRewardClaimed` вызывается по разу за каждую награду, `OnRewardsClaimed(count)` — один раз с общим числом;  
   - При `_saveTimeOnTakeReward` время сдвигается: `lastRewardTime += claimsToGive * cooldown`.

4. **Таймер**  
   - Каждые `updateTime` сек (по умолчанию 0.2) вызывается `RefreshTimeState()`: пересчёт `timeLeft`, при появлении хотя бы одной награды — `OnRewardAvailable`.

---

## Какую механику выбрать — таблица настроек

| Механика | rewardAvailableOnStart | maxRewardsPerTake | saveTimeOnTakeReward | Пример |
|----------|------------------------|-------------------|----------------------|--------|
| Классический кулдаун (одна награда раз в N сек) | false | 1 | true | Бонус раз в 30 сек |
| Бонус сразу при первом заходе | true | 1 | true | Ежедневный бонус при первом входе в день |
| Пассивная/кликер: накапливать и забирать все | false | -1 | true | Раз в час накапливаются награды, игрок забирает все |
| Ограниченный стек (не больше K за раз) | false | N | true | Раз в 5 мин, но за раз не больше 3 |
| Ручной старт кулдауна | любой | любой | false | Кулдаун стартует только после StartTime() |

---

## Примеры настроек

- **Ежедневная награда (24 ч)**  
  `secondsToWaitForReward = 86400`, `_rewardAvailableOnStart = true`, `_maxRewardsPerTake = 1`.

- **Кликер: накопление за час, забрать все**  
  `secondsToWaitForReward = 3600`, `_rewardAvailableOnStart = false`, `_maxRewardsPerTake = -1`.  
  В UI показывать `GetClaimableCount()` и вызывать `TakeReward()` по кнопке.

- **Один бонус раз в 30 сек, первый раз через 30 сек**  
  `secondsToWaitForReward = 30`, `_rewardAvailableOnStart = false`, `_maxRewardsPerTake = 1`, `updateTime = 0.2f`.

---

## Что улучшено
- Добавлены публичные методы управления: `StartTime()`, `StopTime()`, `PauseTime()`, `ResumeTime()`, `RestartTime()`, `SetRewardAvailableNow()`, `RefreshTimeState()`, `SetAdditionalKey(...)`.
- Добавлены статусные публичные свойства: `IsTimerRunning`, `IsTimerPaused`, `IsRewardAvailable`, `RewardTimeKey`, `SaveTimeOnTakeReward`.
- Добавлены события таймера: `OnTimerStarted`, `OnTimerStopped`, `OnTimerPaused`, `OnTimerResumed`.
- Сохранение времени переведено на UTC `round-trip` формат (`"o"`) с обратной совместимостью чтения старых сохранений.
- **Накопление наград**: `_maxRewardsPerTake` (−1 / 1 / N), `GetClaimableCount()`, `OnRewardsClaimed(int)`; интервал обновления по умолчанию `updateTime = 0.2f`.
- **Первый запуск**: `_rewardAvailableOnStart` — при `false` и отсутствии сохранённого времени в `Start()` сохраняется текущее время (полный кулдаун до первой награды).

## Ручной запуск таймера
- Чтобы **запустить отсчёт вручную** (без автостарта при `Start`), выключите `startTimerOnStart` и вызывайте **`StartTime()`** когда нужно начать обновления. Тогда же при необходимости сохранится текущее время (если `saveTimeOnTakeReward == false` и `saveTimeOnStartWhenSaveOnTakeDisabled == true`).
- **`OnTimeUpdated(float)`** вызывается при каждом пересчёте. Если автостарт выключен, в `Start()` всё равно один раз вызывается `RefreshTimeState()` — в этот момент вы получите **один** вызов `OnTimeUpdated` с текущим остатком (при отсутствии сохранения и `rewardAvailableOnStart == false` это будет **полное время** кулдауна). Чтобы получать обновления времени дальше (каждые `updateTime` сек), нужно вызвать `StartTime()`.

## Ключевой режим
- `saveTimeOnTakeReward = true` (по умолчанию): при `TakeReward()` время сдвигается на число выданных наград, кулдаун идёт автоматически.
- `saveTimeOnTakeReward = false`: `TakeReward()` только подтверждает выдачу, кулдаун стартует через `StartTime()` (если включён флаг `saveTimeOnStartWhenSaveOnTakeDisabled`).

## Основные поля (Inspector)
- `secondsToWaitForReward`: длительность кулдауна в секундах.
- `updateTime`: интервал обновления таймера (по умолчанию 0.2).
- `_rewardAvailableOnStart`: при отсутствии сохранения — награда доступна сразу (true) или после полного кулдауна (false).
- `_maxRewardsPerTake`: −1 = забрать все накопленные; 1 = одна за раз; N = не больше N за раз.
- `startTakeReward`: попытка забрать награду при `Start()`.
- `startTimerOnStart`: запуск таймера автоматически при `Start()`.
- `saveTimeOnTakeReward`: сохранять ли время при взятии (сдвиг на число выданных наград).
- `saveTimeOnStartWhenSaveOnTakeDisabled`: при отключённом сохранении на взятии — сохранять ли время в `StartTime()`.
- `timeLeft`: оставшееся время до следующей награды.

## Публичные методы
- `TakeReward()`: попытка взять награду(ы); возвращает true, если выдана хотя бы одна.
- `Take()`: обертка для UnityEvent.
- `CanTakeReward()`: проверка доступности хотя бы одной награды.
- `GetClaimableCount()`: количество наград, которые можно выдать сейчас (с учётом _maxRewardsPerTake).
- `GetSecondsUntilReward()`: оставшееся время до следующей награды (для первой в очереди).
- `StartTime()`: запуск/возобновление отсчёта.
- `StopTime()`: остановка отсчёта.
- `PauseTime()`: пауза.
- `ResumeTime()`: снятие с паузы.
- `RestartTime()`: перезапуск кулдауна от текущего времени.
- `SetRewardAvailableNow()`: мгновенно сделать награду доступной (очистить сохранённый timestamp).
- `RefreshTimeState()`: принудительно пересчитать таймер и отправить события.
- `SetAdditionalKey(string addKey, bool refreshAfterChange = true)`: сменить суффикс ключа сохранения.
- `FormatTime(...)` / `GetFormattedTimeLeft(...)`: форматирование времени.
- `TryGetLastRewardTimeUtc(out DateTime)`: чтение сохранённого timestamp последней награды.
- `GetElapsedSinceLastReward()`: секунды с момента последней награды.

## Inspector (дополнительно)
- `_displayTimeFormat`: формат вывода (TimeFormat).
- `_displaySeparator`: разделитель для отображения.

## Unity Events
- `OnTimeUpdated(float)`: периодическое обновление остатка времени.
- `OnRewardClaimed`: успешное получение одной награды (вызывается столько раз, сколько наград выдано).
- `OnRewardsClaimed(int)`: один раз при TakeReward с количеством выданных наград.
- `OnRewardAvailable`: награда стала доступной (хотя бы одна).
- `OnTimerStarted`, `OnTimerStopped`, `OnTimerPaused`, `OnTimerResumed`.
