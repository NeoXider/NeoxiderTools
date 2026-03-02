# FieldObjectSpawner

**Что это:** спавнер объектов по ячейкам сгенерированного поля с учётом занятости ячеек и привязкой к ячейке. Требует [FieldGenerator](FieldGenerator.md). Событие OnObjectSpawned со SpawnedObjectInfo. Пространство имён `Neo.GridSystem`, файл `Scripts/GridSystem/FieldObjectSpawner.cs`.

**Как использовать:** добавить на объект с FieldGenerator; назначить **Prefabs**; вызывать спавн через API (по позиции ячейки и индексу префаба). Подписаться на OnObjectSpawned при необходимости.

---

## Поля и события

- **Prefabs** — массив префабов для спавна.
- **OnObjectSpawned** — UnityEvent\<SpawnedObjectInfo\>.

## См. также

- [FieldGenerator](FieldGenerator.md), [FieldSpawner](FieldSpawner.md)
