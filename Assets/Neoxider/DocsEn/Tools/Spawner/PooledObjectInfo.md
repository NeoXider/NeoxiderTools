# PooledObjectInfo

**Purpose:** A utility script. Automatically attached by `NeoObjectPool` to every spawned object so it "remembers" which pool it belongs to. Contains a convenient `Return to pool` method.

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Return()` | Returns the current `GameObject` back to its pool (via `PoolManager.Release()`). Can be called from a UnityEvent (e.g., at the end of an animation). |

## Examples

### No-Code Example (Inspector)
Attach a `TimerObject` to a pooled prefab, and in its `OnTimerEnd` UnityEvent, drag and drop itself (the `PooledObjectInfo` component) and select the `Return` method. The object will automatically return to the pool after X seconds.

## See Also
- [PoolManager](PoolManager.md)
- [Despawner](Despawner.md)
- ← [Tools/Spawner](../README.md)
