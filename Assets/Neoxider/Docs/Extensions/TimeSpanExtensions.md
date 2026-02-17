# TimeSpanExtensions

Расширения для форматирования `TimeSpan`.

- **Пространство имён**: `Neo.Extensions`
- **Путь**: `Assets/Neoxider/Scripts/Extensions/TimeSpanExtensions.cs`

## Методы

- **ToCompactString(this TimeSpan value, bool includeSeconds = false, int maxParts = 3)** — компактный вывод, напр. `2d 3h 15m`.
- **ToClockString(this TimeSpan value, bool includeDays = false, string separator = ":")** — вывод в формате `03:15:27` или `02:03:15:27` при `includeDays = true`.
