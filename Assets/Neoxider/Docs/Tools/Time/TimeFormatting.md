# Форматирование и парсинг времени

Краткий справочник по time-расширениям и выбору компонентов для работы со временем.

## Расширения (Extensions)

### DateTimeExtensions
- **ToRoundTripUtcString(this DateTime utc)** — сохраняет DateTime в UTC-строку (ISO round-trip).
- **TryParseUtcRoundTrip(this string raw, out DateTime utc)** — парсит сохранённую строку с fallback для legacy-форматов.
- **GetSecondsSinceUtc**, **GetSecondsUntilUtc** — секунды с/до момента времени.
- **EnsureUtc** — приводит значение к UTC.

### TimeParsingExtensions
- **TryParseDuration(string raw, out float seconds, string separator = ":")** — парсит длительность из строки.
- Поддерживаемые форматы: `SS`, `MM:SS`, `HH:MM:SS`, `DD:HH:MM:SS`.

### TimeSpanExtensions
- **ToCompactString(this TimeSpan, bool includeSeconds, int maxParts)** — компактный вывод, напр. `2d 3h 15m`.
- **ToClockString(this TimeSpan, bool includeDays, string separator)** — вывод в формате `03:15:27`.

### PrimitiveExtensions.FormatTime
- **FormatTime(this float, TimeFormat, string separator, bool trimLeadingZeros)** — форматирует секунды по выбранному формату.
- **trimLeadingZeros** — убирает ведущие нули в первом токене (`01:05` → `1:05`).

## Когда что использовать

| Задача | Компонент/API |
|--------|---------------|
| Кулдаун награды с сохранением | **TimeReward** |
| UI-таймер в сцене (countUp/countDown, looping) | **TimerObject** |
| Программный async-таймер | **Timer** (UniTask) |
| Отображение времени в TMP_Text | **TimeToText** |
| Парсинг строки в секунды | **TimeParsingExtensions.TryParseDuration** |
| Сериализация UTC в строку | **DateTimeExtensions.ToRoundTripUtcString** |
| Форматирование float → строка | **float.FormatTime** / **TimeToText.FormatTime** |
