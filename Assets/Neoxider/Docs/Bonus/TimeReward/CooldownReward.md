# CooldownReward

- **Пространство имён:** `Neo.Bonus`
- **Путь к файлу:** `Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs`

Компонент наград по кулдауну на базе [TimerObject](../Tools/Time/TimerObject.md): один скрипт объединяет таймер (режим RealTime), сохранение по UTC и логику наград (накопление, лимит за раз, события). Рекомендуется вместо устаревшего [TimeReward](./TimeReward.md).

**Добавить в сцену:** GameObject → Neoxider → Bonus → CooldownReward.

---

## Схема работы

```mermaid
flowchart TD
    subgraph Init["Init()"]
        A[SyncTimerConfig: duration, updateInterval, RealTime] --> B{Есть сохранённое время?}
        B -->|Нет| C{_rewardAvailableOnStart?}
        C -->|false| D[SetTime(duration) — полный кулдаун]
        C -->|true| E[Ничего не сохраняем]
        B -->|Да| E
    end

    subgraph Start["Start()"]
        F{_startTakeReward?} -->|Да| G[TakeReward]
        F -->|Нет| H[_startTimerOnStart?]
        G --> H
        H -->|Да| I[StartTime / Play]
        H -->|Нет| J[RefreshTimeState]
        I --> J
    end

    subgraph Take["TakeReward()"]
        T1[GetClaimableCount] --> T2{count >= 1?}
        T2 -->|Нет| T3[return false]
        T2 -->|Да| T4[OnRewardClaimed × count, OnRewardsClaimed(count)]
        T4 --> T5{_saveTimeOnTakeReward?}
        T5 -->|Да| T6[SetTime, SaveState]
        T6 --> T7[RefreshTimeState, return true]
        T5 -->|Нет| T8[_waitingForManualStart = true]
        T8 --> T7
    end

    subgraph Timer["Таймер (каждые updateInterval сек)"]
        R1[OnTimeChanged → OnTimeUpdated(remaining)]
        R2[OnTimerCompleted → OnRewardAvailable]
    end
```

**Кратко:**

1. **Первый запуск (нет сохранения)**  
   - `_rewardAvailableOnStart == false`: в `Init()` вызывается `SetTime(duration)` — полный кулдаун до первой награды.  
   - `_rewardAvailableOnStart == true`: награда доступна сразу; при первом `TakeReward()` сохраняется время.

2. **Расчёт накопленных наград**  
   - По сохранённому UTC и текущему времени считается, сколько интервалов прошло; количество ограничивается `_maxRewardsPerTake` (−1 = все, 1 = одна, N = не больше N за раз).

3. **TakeReward()**  
   - Выдаётся до `GetClaimableCount()` наград; вызываются `OnRewardClaimed` (по разу) и `OnRewardsClaimed(count)`.  
   - При `_saveTimeOnTakeReward` время сдвигается и сохраняется; иначе кулдаун стартует только после `StartTime()`.

4. **Таймер**  
   - Базовый TimerObject в режиме RealTime обновляет остаток; при завершении — `OnRewardAvailable`, при каждом тике — `OnTimeUpdated(remaining)`.

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

## Примеры применения

### Ежедневная награда (24 ч)

- **Cooldown (Reward Settings):** `3600 * 24` (86400 сек).  
- **Reward Available On Start:** включено — при первом входе в день награда доступна сразу.  
- **Max Rewards Per Take:** 1.  
- В UI: кнопка «Забрать» вызывает `TakeReward()`; текст остатка — `GetFormattedTimeLeft()` в `OnTimeUpdated`.

### Классический кулдаун (раз в 30 сек)

- **Cooldown:** 30.  
- **Reward Available On Start:** выключено — первый раз награда через 30 сек.  
- **Max Rewards Per Take:** 1.  
- **Start Timer On Start:** включено.  
- Подписаться на `OnRewardAvailable` для подсветки кнопки; на `OnTimeUpdated` — обновление текста «До награды: MM:SS».

### Кликер: накопление за час, забрать все

- **Cooldown:** 3600.  
- **Reward Available On Start:** выключено.  
- **Max Rewards Per Take:** -1 (забрать все накопленные).  
- В UI показывать `GetClaimableCount()` и вызывать `TakeReward()` по кнопке; при `count > 0` кнопка активна.

### Ограниченный стек (не больше 3 за раз)

- **Cooldown:** 300 (5 мин).  
- **Max Rewards Per Take:** 3.  
- Игрок может накопить несколько наград, но за одно нажатие забирает не больше 3. В `OnRewardsClaimed(int)` обновить счётчик выданных наград.

### Ручной старт кулдауна

- **Save Time On Take Reward:** выключено.  
- **Start Timer On Start:** выключено.  
- После выполнения условия (например, завершение задания) вызвать `StartTime()` — кулдаун начнётся с этого момента. При `Save Time On Start When Save On Take Disabled` время сохранится при `StartTime()`.

---

## Основные поля (Inspector)

- **Reward Settings**  
  - `_cooldownSeconds` — длительность кулдауна в секундах (внутри синхронизируется с `duration` TimerObject).  
  - `_updateInterval` — интервал обновления таймера (по умолчанию 0.2).  
  - `_rewardAvailableOnStart` — при отсутствии сохранения награда доступна сразу (true) или после полного кулдауна (false).  
  - `_maxRewardsPerTake` — −1 = забрать все накопленные; 1 = одна за раз; N = не больше N за раз.  
  - `_addKey` — суффикс ключа сохранения (разные экземпляры — разные ключи).  
  - `_startTakeReward` — при Start() сразу вызвать TakeReward().  
  - `_startTimerOnStart` — при Start() запускать таймер (Play).  
  - `_saveTimeOnTakeReward` — при TakeReward() сохранять время и сдвигать кулдаун.  
  - `_saveTimeOnStartWhenSaveOnTakeDisabled` — при выключенном сохранении на взятии сохранять время при StartTime().  
- **Display:** `_displayTimeFormat`, `_displaySeparator` — формат вывода времени (для GetFormattedTimeLeft).

---

## Публичные методы

- **TakeReward()** — попытка забрать награду(ы); возвращает true, если выдана хотя бы одна.  
- **Take()** — обёртка для UnityEvent.  
- **CanTakeReward()** — доступна ли хотя бы одна награда.  
- **GetClaimableCount()** — сколько наград можно выдать сейчас (с учётом _maxRewardsPerTake).  
- **GetSecondsUntilReward()** — оставшееся время до следующей награды.  
- **GetFormattedTimeLeft(bool trimLeadingZeros)** — строка вида «HH:MM:SS» по настройкам компонента.  
- **StartTime()** — запуск/возобновление отсчёта (при ручном режиме — старт кулдауна).  
- **StopTime()** — остановка таймера.  
- **PauseTime()** / **ResumeTime()** — пауза и снятие с паузы.  
- **RestartTime()** — перезапуск кулдауна от текущего момента.  
- **SetRewardAvailableNow()** — очистить сохранённое время и сделать награду доступной сразу.  
- **RefreshTimeState()** — принудительный пересчёт и отправка событий.  
- **SetAdditionalKey(string addKey, bool refreshAfterChange)** — сменить суффикс ключа сохранения.  
- **TryGetLastRewardTimeUtc(out DateTime)** — прочитать время последней выданной награды.  
- **GetElapsedSinceLastReward()** — секунды с момента последней награды.

---

## Unity Events

- **OnTimeUpdated(float)** — при каждом обновлении таймера (остаток в секундах).  
- **OnRewardClaimed** — при успешной выдаче одной награды (вызывается столько раз, сколько наград выдано за один TakeReward).  
- **OnRewardsClaimed(int)** — один раз при TakeReward с количеством выданных наград.  
- **OnRewardAvailable** — награда стала доступной (хотя бы одна).

---

## Режим сохранения и ручной старт

- **saveTimeOnTakeReward = true** (по умолчанию): при TakeReward() время сдвигается на число выданных наград, кулдаун идёт автоматически.  
- **saveTimeOnTakeReward = false**: TakeReward() только подтверждает выдачу; кулдаун стартует при вызове **StartTime()** (если включён **saveTimeOnStartWhenSaveOnTakeDisabled**, при StartTime() сохранится текущее время).

---

## См. также

- [TimerObject](../Tools/Time/TimerObject.md) — базовый таймер (duration, RealTime, SaveState/LoadState).  
- [README модуля](./README.md) — сравнение CooldownReward и устаревшего TimeReward.  
- [TimeReward](./TimeReward.md) — устаревший класс; логика сходная, новый код — на CooldownReward.
