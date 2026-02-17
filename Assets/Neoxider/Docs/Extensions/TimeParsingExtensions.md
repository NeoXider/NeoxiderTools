# TimeParsingExtensions

Расширения для парсинга текстовых длительностей в секунды.

- **Пространство имён**: `Neo.Extensions`
- **Путь**: `Assets/Neoxider/Scripts/Extensions/TimeParsingExtensions.cs`

## Методы

- **TryParseDuration(string raw, out float seconds, string separator = ":")** — парсит длительность из строки. Поддерживаемые форматы: `SS`, `MM:SS`, `HH:MM:SS`, `DD:HH:MM:SS`.
- **TryParseDuration(string raw, out float seconds, char separator)** — перегрузка с символом-разделителем.
