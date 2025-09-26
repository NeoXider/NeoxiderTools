# Расширения ComponentExtensions

## 1. Введение

`ComponentExtensions` — это набор методов-расширений для базового класса `UnityEngine.Component`. Они добавляют полезные шорткаты для частых операций с компонентами.

---

## 2. Описание методов

### ComponentExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/ComponentExtensions.cs`

**Статические методы**
- `GetOrAdd<T>(this Component component)`: Пытается получить компонент типа `T` с `GameObject`. Если компонент не найден, он будет добавлен. Возвращает найденный или созданный компонент `T`.
- `GetPath(this Component component)`: Возвращает полный путь к `GameObject`'у этого компонента в иерархии сцены (например, `"Parent/Child/MyObject"`). Возвращает `string`.
