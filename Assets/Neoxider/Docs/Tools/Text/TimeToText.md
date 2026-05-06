# TimeToText

**Назначение:** Компонент для форматирования и отображения времени (в секундах) в `TMP_Text`. Поддерживает два режима: `Clock` (05:30, 01:05:30) и `Compact` (1д 5ч 30м). Используется для таймеров, обратного отсчёта и отображения прошедшего времени.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Text** | Ссылка на `TMP_Text` (авто-назначение). |
| **Display Mode** | `Clock` (разделители, фиксированный формат) или `Compact` (11д 5ч). |
| **Time Format** | Формат для Clock-режима: `Seconds`, `MinutesSeconds`, `HoursMinutesSeconds`, и т.д. |
| **Zero Text** | Показывать ли текст, когда время = 0. |
| **Allow Negative** | Разрешить отрицательные значения (отображение с `-`). |
| **Separator** | Разделитель для Clock-режима (по умолчанию `:`). |
| **Start / End Add Text** | Префикс и суффикс. |
| **Compact Include Seconds** | Включать ли секунды в Compact-режиме. |
| **Compact Max Parts** | Максимум единиц в Compact-режиме (1–N). |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void Set(float time)` | Установить время (секунды) и обновить текст. |
| `bool TrySetFromString(string raw, string separator)` | Парсить строку формата SS / MM:SS / HH:MM:SS. |
| `float CurrentTime { get; }` | Текущее отображаемое время. |
| `static string FormatTime(float time, TimeFormat format, string separator)` | Статический метод форматирования. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnTimeChanged` | `float` | Значение времени изменилось. |

## Примеры

### Пример No-Code (в Inspector)
Повесьте `TimeToText` на `TextMeshPro`. Выберите `Time Format = MinutesSeconds`. Подключите `TimerObject.OnTimeChanged` → `TimeToText.Set(float)`. Теперь таймер будет выводить оставшееся время в формате `05:30`.

### Пример (Код)
```csharp
[SerializeField] private TimeToText _timer;

void Update()
{
    _timer.Set(Time.timeSinceLevelLoad);
}
```

## См. также
- [SetText](SetText.md)
- [TimerObject](../Time/TimerObject.md)
- ← [Tools/Text](README.md)
