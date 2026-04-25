# IPoolable

**Purpose:** Interface for objects managed by `PoolManager`. Defines lifecycle callbacks for pool-spawned objects.

## API

| Method | Description |
|--------|-------------|
| `void OnSpawn()` | Called when the object is taken from the pool. |
| `void OnDespawn()` | Called when the object is returned to the pool. |

## See Also
- [PoolManager](PoolManager.md)
- [PoolableBehaviour](PoolableBehaviour.md)
- ← [Tools/Spawner](README.md)
