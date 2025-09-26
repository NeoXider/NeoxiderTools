# Расширения RandomExtensions

## 1. Введение

`RandomExtensions` — это набор методов-расширений и утилит для работы со случайными числами и коллекциями. Он упрощает такие частые задачи, как получение случайного элемента из списка, перемешивание коллекции или генерация случайного цвета.

---

## 2. Описание методов

### RandomExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/RandomExtensions.cs`

**Статические методы**

#### Для коллекций
- `GetRandomElement<T>(this IList<T> collection)`: Возвращает случайный элемент из списка или массива.
- `Shuffle<T>(this IList<T> collection, ...)`: Перемешивает элементы в списке или массиве.
- `GetRandomElements<T>(this IList<T> collection, int count)`: Возвращает указанное количество случайных элементов из коллекции.
- `GetRandomIndex<T>(this ICollection<T> collection)`: Возвращает случайный допустимый индекс для коллекции.

#### Для чисел
- `Chance(this float probability)`: Возвращает `true` с указанной вероятностью (от 0.0 до 1.0).
- `RandomRange(this Vector2 vector)`: Возвращает случайное число `float` в диапазоне между `vector.x` и `vector.y`.

#### Утилиты
- `RandomBool()`: Возвращает случайный `bool` (`true` или `false`).
- `RandomColor(float alpha = 1f)`: Генерирует случайный цвет.
- `GetRandomEnumValue<T>()`: Возвращает случайное значение из любого перечисления (`enum`).
- `GetRandomWeightedIndex(this IList<float> weights)`: Возвращает случайный индекс из списка "весов", учитывая их значения (чем больше вес, тем выше шанс).
