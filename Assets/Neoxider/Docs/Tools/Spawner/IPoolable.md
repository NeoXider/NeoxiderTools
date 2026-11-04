# IPoolable

**What it is:** Interface with the pool lifecycle callbacks of `NeoObjectPool`, used by both `PoolManager` and `SpawnUtility` fallback pools. `Scripts/Tools/Spawner/IPoolable.cs`, namespace `Neo.Tools`.

**How to use:**
1. Implement `IPoolable` on a `MonoBehaviour` of the pooled prefab (or inherit [PoolableBehaviour](PoolableBehaviour.md)).
2. Cache components in `OnPoolCreate`, reset state in `OnPoolGet`, stop effects in `OnPoolRelease`.
3. Spawn the prefab through a pool (`SpawnUtility.Spawn`, `PoolManager.Get`, `NeoObjectPool.GetObject`). No UnityEvent wiring is required.

---

## API

| Method | Description |
|--------|-------------|
| `void OnPoolCreate()` | Called once when the pool creates an instance; cache components here. |
| `void OnPoolGet()` | Called on every retrieval, before activation and before the requested world pose is assigned; reset reusable state here. |
| `void OnPoolRelease()` | Called on return after `IsInPool` becomes true, before automatic Mirror server unspawn and pool reparenting/deactivation; stop effects and release external subscriptions here. |

Prewarming also invokes get/release callbacks. Components are discovered once, including inactive children, and cached on the instance's [PooledObjectInfo](PooledObjectInfo.md), so later get/return calls do not search the hierarchy. Use Inspector spawners with pooling enabled to receive the same callbacks; `Instantiate` paths do not invoke this interface.

## See Also
- [PoolManager](PoolManager.md)
- [PoolableBehaviour](PoolableBehaviour.md)
- ← [Tools/Spawner](README.md)
