# Расширения StringExtension

## 1. Введение

`StringExtension` — это большой набор методов-расширений для типа `string`. Он включает в себя утилиты для парсинга, форматирования, валидации и создания Rich Text тегов для Unity.

---

## 2. Описание методов

### StringExtension
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/StringExtension.cs`

**Статические методы**

#### Парсинг и конвертация
- `ToColor(this string hex)`: Конвертирует HEX-строку (например, `"#FF0000"`) в `Color`.
- `ToBool(this string input)`: Конвертирует строку (`"true"`, `"1"`, `"yes"`) в `bool`.
- `ToInt(this string input, ...)`: Безопасно парсит строку в `int`. Возвращает значение по умолчанию в случае ошибки.
- `ToFloat(this string input, ...)`: Безопасно парсит строку в `float`.

#### Форматирование и манипуляция
- `SplitCamelCase(this string input)`: Превращает `"MyVariableName"` в `"My Variable Name"`.
- `Truncate(this string input, int maxLength)`: Обрезает строку до максимальной длины, добавляя `"..."`.
- `Reverse(this string input)`: Переворачивает строку.

#### Утилиты и генерация
- `IsNullOrEmptyAfterTrim(this string input)`: Проверяет, является ли строка пустой после удаления пробелов.
- `IsNumeric(this string input)`: Проверяет, состоит ли строка только из цифр.
- `RandomString(int length, ...)`: Генерирует случайную строку указанной длины.

#### Rich Text (для Unity UI)
- `Bold(this string input)`: Оборачивает строку в тег `<b>`.
- `Italic(this string input)`: Оборачивает строку в тег `<i>`.
- `Size(this string input, int size)`: Оборачивает строку в тег `<size>`.
- `SetColor(this string input, Color color)`: Оборачивает строку в тег `<color>`.
- `Rainbow(this string input)`: Применяет к строке эффект радуги, окрашивая каждую букву.
- `Gradient(this string input, ...)`: Применяет к строке градиент между двумя цветами.
