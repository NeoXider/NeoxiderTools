# Расширения PrimitiveExtensions

## 1. Введение

`PrimitiveExtensions` — это набор методов-расширений для базовых типов данных, таких как `float`, `int` и `bool`. Они добавляют полезные функции для форматирования, конвертации и математических операций.

---

## 2. Описание методов

### PrimitiveExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/PrimitiveExtensions.cs`

**Статические методы для `float`**
- `RoundToDecimal(this float value, int places)`: Округляет число до указанного количества знаков после запятой.
- `FormatTime(this float timeSeconds, ...)`: Форматирует время в секундах в строку (например, `"MM:SS"`).
- `FormatWithSeparator(this float number, ...)`: Форматирует число, добавляя разделители разрядов (например, `1 000 000.00`).
- `NormalizeToUnit(this float x, ...)`: Нормализует значение в диапазон от 0 до 1.
- `Remap(this float value, ...)`: Переводит значение из одного диапазона в другой (например, из `0-100` в `0-1`).

**Статические методы для `int`**
- `ToBool(this int value)`: Конвертирует `int` в `bool` (`0` = `false`, все остальное = `true`).
- `FormatWithSeparator(this int number, ...)`: Форматирует целое число, добавляя разделители разрядов.

**Статические методы для `bool`**
- `ToInt(this bool value)`: Конвертирует `bool` в `int` (`true` = `1`, `false` = `0`).
