# PoolManager

Синглтон пулов объектов: предзаполнение по конфигу (prefab + initial size + max size), получение/возврат через Get/Release, опциональное расширение пула с лимитом.

**Добавить:** Neoxider → Tools → PoolManager (или через Singleton).

## Основное

- **Preconfigured Pools** — префаб, начальный размер, возможность расширения, **max size** (макс. размер пула при expand; по умолчанию 100, при 0 тоже 100).
- **Default Initial Size**, **Default Expand Pool**, **Default Max Size** — значения по умолчанию для пулов, созданных «на лету» (Default Max Size = 100).

Получение — `PoolManager.Get(prefab, position, rotation, parent)`; возврат — `PoolManager.Release(instance)` или `instance.GetComponent<PooledObjectInfo>().Return()` / `instance.ReturnToPool()` (Neo.Tools).

## Использование в коде

```csharp
// Получить из пула
GameObject go = PoolManager.Get(bulletPrefab, firePoint.position, firePoint.rotation, parent);

// Вариант через расширение (если PoolManager нет — будет Instantiate)
GameObject go = bulletPrefab.SpawnFromPool(pos, rot, parent);

// Вернуть в пул
PoolManager.Release(go);
go.ReturnToPool(); // то же самое
```

## Объекты из пула (IPoolable)

Чтобы при взятии/возврате сбрасывать состояние, реализуйте **IPoolable** на компоненте префаба (или наследуйте **PoolableBehaviour**):

- **OnPoolCreate()** — один раз при создании экземпляра пулом (кэш компонентов).
- **OnPoolGet()** — каждый раз при выдаче из пула (сброс HP, таймеров, позиции).
- **OnPoolRelease()** — при возврате в пул (остановить эффекты, отписаться от событий).

## См. также

- [Spawner](./Spawner.md)
- [PooledObjectInfo](./PooledObjectInfo.md)
- [PoolableBehaviour](./PoolableBehaviour.md) / IPoolable
