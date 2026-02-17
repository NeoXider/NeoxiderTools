# DateTimeExtensions

Расширения для безопасной сериализации и расчётов с `DateTime` в UTC.

- **Пространство имён**: `Neo.Extensions`
- **Путь**: `Assets/Neoxider/Scripts/Extensions/DateTimeExtensions.cs`

## Методы

- **ToRoundTripUtcString(this DateTime utc)** — сохраняет `DateTime` в UTC-строку формата ISO round-trip (`"o"`).
- **TryParseUtcRoundTrip(this string raw, out DateTime utc)** — парсит сохранённую строку с поддержкой legacy-форматов.
- **GetSecondsSinceUtc(this DateTime utc, DateTime nowUtc)** — секунды от `utc` до `nowUtc`.
- **GetSecondsUntilUtc(this DateTime targetUtc, DateTime nowUtc)** — секунды от `nowUtc` до `targetUtc`.
- **EnsureUtc(this DateTime value)** — приводит значение к UTC (Local/Unspecified → UTC).
