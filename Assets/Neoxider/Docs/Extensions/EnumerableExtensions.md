# Расширения EnumerableExtensions

## 1. Введение

`EnumerableExtensions` — это набор общих методов-расширений для работы с любыми коллекциями, реализующими `IEnumerable` или `IList` (например, массивы, списки). Они предоставляют удобные утилиты для итерации, безопасного доступа к элементам и форматирования.

---

## 2. Описание методов

### EnumerableExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/EnumerableExtensions.cs`

**Статические методы**
- `ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)`: Выполняет указанное действие для каждого элемента коллекции. Аналог `List<T>.ForEach`, но для любого `IEnumerable`.
- `GetSafe<T>(this IList<T> collection, int index, ...)`: Безопасно получает элемент по индексу. Если индекс выходит за пределы, возвращает значение по умолчанию, а не ошибку.
- `GetWrapped<T>(this IList<T> collection, int index)`: Получает элемент по индексу, который "заворачивается" (например, для индекса `5` в коллекции из `3` элементов вернется элемент с индексом `2`).
- `IsValidIndex<T>(this ICollection<T> collection, int index)`: Проверяет, является ли индекс допустимым для данной коллекции.
- `ToIndexedString<T>(this IEnumerable<T> collection)`: Превращает коллекцию в строку с нумерованными строками.
- `IsNullOrEmpty<T>(this IEnumerable<T> collection)`: Проверяет, является ли коллекция `null` или пустой.
- `ToStringJoined<T>(this IEnumerable<T> collection, string separator)`: Объединяет все элементы коллекции в одну строку с указанным разделителем.
- `FindDuplicates<T>(this IEnumerable<T> collection)`: Находит все повторяющиеся элементы в коллекции.
