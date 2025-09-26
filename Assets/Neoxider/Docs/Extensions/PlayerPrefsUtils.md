# Утилиты PlayerPrefsUtils

## 1. Введение

`PlayerPrefsUtils` — это статический класс-помощник, который расширяет стандартный `PlayerPrefs` возможностью сохранять и загружать массивы примитивных типов (`int[]`, `float[]`, `string[]`, `bool[]`).

Стандартный `PlayerPrefs` умеет работать только с одиночными значениями, а эта утилита решает проблему, сохраняя массив в виде одной строки с разделителями.

---

## 2. Описание методов

### PlayerPrefsUtils
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/PlayerPrefsUtils.cs`

**Статические методы**
- `SetIntArray(string key, int[] array)`: Сохраняет массив `int`.
- `GetIntArray(string key, ...)`: Загружает массив `int`.
- `SetFloatArray(string key, float[] array)`: Сохраняет массив `float`.
- `GetFloatArray(string key, ...)`: Загружает массив `float`.
- `SetStringArray(string key, string[] array)`: Сохраняет массив `string`.
- `GetStringArray(string key, ...)`: Загружает массив `string`.
- `SetBoolArray(string key, bool[] array)`: Сохраняет массив `bool`.
- `GetBoolArray(string key, ...)`: Загружает массив `bool`.

*Примечание: После вызова Set-методов необходимо вручную вызвать `PlayerPrefs.Save()`, чтобы записать изменения на диск.*
