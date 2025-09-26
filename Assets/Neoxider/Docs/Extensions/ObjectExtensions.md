# Расширения ObjectExtensions

## 1. Введение

`ObjectExtensions` — это набор методов-расширений для базового класса `UnityEngine.Object`. Они предоставляют удобные и безопасные способы для выполнения общих операций, таких как уничтожение объектов и проверка на `null`.

---

## 2. Описание методов

### ObjectExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/ObjectExtensions.cs`

**Статические методы**
- `SafeDestroy(this Object obj, bool immediate = false)`: Безопасно уничтожает объект. Автоматически использует `Destroy` в режиме игры и `DestroyImmediate` в редакторе. Возвращает `void`.
- `IsValid(this Object obj)`: Проверяет, что объект не является `null` и не был уничтожен. Это более надежная проверка, чем `obj != null`. Возвращает `bool`.
- `GetName(this Object obj)`: Безопасно возвращает имя объекта или пустую строку, если объект `null`. Возвращает `string`.
- `SetName(this Object obj, string newName)`: Устанавливает имя объекта, если он не `null`. Возвращает `void`.
