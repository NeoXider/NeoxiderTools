# FieldSpawner

**Что это:** спавнит префабы в центре ячеек сгенерированного поля. Требует [FieldGenerator](FieldGenerator.md). Событие OnObjectSpawned(GameObject, FieldCell). Пространство имён `Neo.GridSystem`, файл `Scripts/GridSystem/FieldSpawner.cs`.

**Как использовать:** добавить на объект с FieldGenerator; назначить **Prefabs**; вызывать `SpawnAt(cellPos, prefabIndex)` из кода. Подписаться на OnObjectSpawned при необходимости.

---

## Поля и методы

- **Prefabs** — массив префабов.
- **SpawnAt(Vector3Int cellPos, int prefabIndex = 0)** — спавн в ячейке; возвращает экземпляр или null.
- **OnObjectSpawned** — UnityEvent\<GameObject, FieldCell\>.

## См. также

- [FieldGenerator](FieldGenerator.md), [FieldObjectSpawner](FieldObjectSpawner.md)
