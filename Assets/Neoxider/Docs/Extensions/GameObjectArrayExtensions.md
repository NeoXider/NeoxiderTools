# Расширения GameObjectArrayExtensions

## 1. Введение

`GameObjectArrayExtensions` — это набор методов-расширений для работы с коллекциями (`IEnumerable`, `IList`) `GameObject`'ов и `Component`'ов. Он позволяет выполнять массовые операции над группой объектов, такие как активация/деактивация, поиск ближайшего, вычисление общих границ и т.д.

---

## 2. Описание методов

### GameObjectArrayExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/GameObjectArrayExtensions.cs`

**Статические методы**
- `SetActiveAll(this IEnumerable<GameObject> gameObjects, bool active)`: Включает или выключает все `GameObject` в коллекции.
- `SetActiveAtIndex(this IList<GameObject> gameObjects, int index, bool active)`: Включает/выключает один `GameObject` по индексу.
- `DestroyAll(this IEnumerable<GameObject> gameObjects)`: Уничтожает все `GameObject` в коллекции.
- `GetActiveObjects(this IEnumerable<GameObject> gameObjects)`: Возвращает новую коллекцию только из активных `GameObject`.
- `GetComponentsFromAll<T>(this IEnumerable<GameObject> gameObjects)`: Собирает компоненты типа `T` со всех `GameObject` в коллекции.
- `FindClosest(this IEnumerable<GameObject> gameObjects, Vector3 position)`: Находит `GameObject`, ближайший к указанной точке.
- `WithinDistance(this IEnumerable<GameObject> gameObjects, Vector3 position, float distance)`: Возвращает `GameObject`'ы, находящиеся в пределах указанной дистанции.
- `SetParentAll(this IEnumerable<GameObject> gameObjects, Transform parent, ...)`: Устанавливает родителя для всех `GameObject` в коллекции.
- `GetAveragePosition(this IEnumerable<GameObject> gameObjects)`: Вычисляет среднюю позицию (центр масс) всех `GameObject`.
- `GetCombinedBounds(this IEnumerable<GameObject> gameObjects)`: Вычисляет единый `Bounds`, который охватывает все `Renderer` в коллекции.

*Примечание: Большинство методов также имеют перегрузки для работы с коллекциями `Component`'ов (например, `IEnumerable<Transform>`).*
