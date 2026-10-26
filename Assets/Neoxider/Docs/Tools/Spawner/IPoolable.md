# IPoolable

**Purpose:** Typed lifecycle contract for `NeoObjectPool`, used by both `PoolManager` and `SpawnUtility` fallback pools. Implement it on a `MonoBehaviour`, or inherit `PoolableBehaviour`. No UnityEvent wiring is required.

## API

| Method | Description |
|--------|-------------|
| `void OnPoolCreate()` | Called once when the pool creates an instance; cache components here. |
| `void OnPoolGet()` | Called on every retrieval, before activation and before the requested world pose is assigned; reset reusable state here. |
| `void OnPoolRelease()` | Called on return after `IsInPool` becomes true, before automatic Mirror server unspawn and pool reparenting/deactivation; stop effects and release external subscriptions here. |

Prewarming also invokes get/release callbacks. Components are discovered in inactive children and cached per instance. Use Inspector spawners with pooling enabled to receive the same callbacks; `Instantiate` paths do not invoke this interface.

## See Also
- [PoolManager](PoolManager.md)
- [PoolableBehaviour](PoolableBehaviour.md)
- ← [Tools/Spawner](README.md)
