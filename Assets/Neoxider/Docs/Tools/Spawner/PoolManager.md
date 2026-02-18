# PoolManager

Синглтон пулов объектов: предзаполнение пулов по конфигу (prefab + initial size), получение/возврат через Get/Return, опциональное расширение пула.

**Добавить:** Neo → Tools → PoolManager (или через Singleton).

## Основное

- **Preconfigured Pools** — список префабов и начальный размер пула.
- **Default Initial Size**, **Default Expand Pool** — значения по умолчанию для новых пулов.

Получение объекта из пула — через API менеджера; возврат — обычно через **PooledObjectInfo** на объекте.

## См. также

- [Spawner](./Spawner.md)
- [PooledObjectInfo](./PooledObjectInfo.md)
