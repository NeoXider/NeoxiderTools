# Расширения ColorExtension

## 1. Введение

`ColorExtension` — это набор методов-расширений для структуры `UnityEngine.Color`. Он предоставляет удобные способы для создания новых цветов на основе существующих, не изменяя исходный цвет.

---

## 2. Описание методов

### ColorExtension
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/ColorExtension.cs`

**Статические методы**
- `WithAlpha(this Color color, float alpha)`: Создает копию цвета с измененным значением альфа-канала.
- `With(this Color color, float? r, float? g, float? b, float? a)`: Создает копию цвета, изменяя только указанные каналы (например, только `r` и `a`).
- `WithRGB(this Color color, float r, float g, float b)`: Создает копию цвета с новыми значениями RGB, но сохраняя старую альфу.
- `Darken(this Color color, float amount)`: Затемняет цвет на указанный процент.
- `Lighten(this Color color, float amount)`: Осветляет цвет на указанный процент.
- `ToHexString(this Color color)`: Конвертирует цвет в его HEX-представление (например, `"#FF0000FF"`).
