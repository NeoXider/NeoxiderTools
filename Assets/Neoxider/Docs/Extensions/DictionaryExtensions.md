# Расширения DictionaryExtensions

**Что это:** См. описание ниже.

**Как использовать:** см. разделы ниже.

---


## 1. Введение

`DictionaryExtensions` — набор методов-расширений для `IDictionary<TKey, TValue>`, закрывающих самые частые паттерны «получить-или-создать» и счётчики, чтобы не переписывать их в каждом месте вызова. Метод `GetValueOrDefault` уже есть в стандартной библиотеке — здесь добавлены недостающие `GetOrCreate` и `Increment`.

---

## 2. Описание методов

### DictionaryExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/DictionaryExtensions.cs`

**Статические методы**
- `GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)` (где `TValue : new()`): Возвращает значение по ключу либо создаёт `new TValue()`, сохраняет и возвращает. Удобно для «корзин»: `dict.GetOrCreate(id).Add(item)`.
- `GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)`: То же, но значение создаётся фабрикой.
- `Increment<TKey>(this IDictionary<TKey, int> dictionary, TKey key, int amount = 1)`: Прибавляет `amount` к целочисленному счётчику по ключу (создаёт с нуля при отсутствии), возвращает новую сумму. Заменяет `dict[k] = dict.GetValueOrDefault(k) + 1`.
- `Increment<TKey>(this IDictionary<TKey, float> dictionary, TKey key, float amount = 1f)`: То же для `float`-счётчика.
