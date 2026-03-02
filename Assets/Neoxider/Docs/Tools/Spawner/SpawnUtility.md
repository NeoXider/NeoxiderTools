# SpawnUtility

**Что это:** статический класс `SpawnUtility` — единая точка входа для спавна и деспавна объектов. Всегда работает через пул: если на сцене есть [PoolManager](./PoolManager.md), используются его пулы; иначе для каждого префаба создаётся свой пул. Пространство имён: `Neo.Tools`. Файл: `Assets/Neoxider/Scripts/Tools/Spawner/SpawnUtility.cs`.

**Как с ним работать:**
1. Спавн: `GameObject go = SpawnUtility.Spawn(prefab, position, rotation, parent);` или расширение `prefab.SpawnFromPool(position, rotation, parent)`.
2. Деспавн: `SpawnUtility.Despawn(go);` или `go.ReturnToPool();`.
3. Не вызывать Destroy для объектов, созданных через Spawn — возвращать в пул через Despawn.
4. PoolManager можно добавить на сцену позже — SpawnUtility подхватит его при следующем Spawn.

---

## Поведение

- **Spawn(prefab, position, rotation, parent)** — создаёт объект из пула. Если есть `PoolManager`, используется он (пул для префаба создаётся при первом запросе). Если PoolManager нет — для префаба создаётся внутренний пул при первом вызове. Все последующие спавны/деспавны идут через пул.
- **Despawn(instance)** — возвращает объект в пул или уничтожает (если объект не из пула).
- **IsPoolAvailable** — всегда `true` (пул либо от PoolManager, либо свой).
- **DestroyFallbackPoolsOnSceneLoad** — по умолчанию `true`: при смене сцены fallback-пулы и их объекты уничтожаются. Если `false`, корень пулов не уничтожается при смене сцены (DontDestroyOnLoad).
- **ClearFallbackPools()** — вручную очищает все fallback-пулы и уничтожает их корень.

## Перегрузки Spawn

- `Spawn(prefab)` — позиция (0,0,0), поворот identity.
- `Spawn(prefab, position)` — заданная позиция.
- `Spawn(prefab, position, rotation)` — позиция и поворот.
- `Spawn(prefab, position, rotation, parent)` — полный вариант.
- `Spawn(prefab, parent)` — спавн как дочерний объект parent (локально в нуле).

## Использование

```csharp
// Спавн (пул или Instantiate)
GameObject go = SpawnUtility.Spawn(prefab, position, rotation, parent);

// Или через расширение
GameObject go = prefab.SpawnFromPool(position, rotation, parent);

// Деспавн
SpawnUtility.Despawn(go);
go.ReturnToPool();
```

Все спавнеры ([Spawner](./Spawner.md), [SimpleSpawner](./SimpleSpawner.md)) и [Despawner](./Despawner.md) используют эту логику. Пул можно добавить на сцену позже — спавн продолжит работать, а при появлении `PoolManager` начнёт использоваться пул.
