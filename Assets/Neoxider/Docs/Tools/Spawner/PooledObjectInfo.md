# PooledObjectInfo

**Purpose:** A utility script. Automatically attached by `NeoObjectPool` to every spawned object so it "remembers" which pool it belongs to. Contains a convenient `Return to pool` method.

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Return()` | Returns the current `GameObject` back to its pool (via `PoolManager.Release()`). Can be called from a UnityEvent (e.g., at the end of an animation). |
| `NeoObjectPool OwnerPool` | Pool that created the instance. Treat this reference as pool-owned metadata. |
| `bool IsInPool` | True after return begins and while the object is retained inactive. |
| `int SpawnGeneration` | Increments on each retrieval, including prewarming. Capture this alongside a reference when scheduling work that must not affect a future rental. |
| `bool TryRegisterReleaseCleanup(NeoObjectPool expectedOwner, int expectedGeneration, Action<GameObject> cleanup)` | Registers one pending cleanup for the exact outstanding rental; returns false for a wrong/stale owner or generation, an already returned/destroyed instance, or an occupied cleanup slot. Null callback throws `ArgumentNullException`. |

## Resources attached during a rental

Capture owner and generation immediately after acquisition. The one-shot cleanup slot is independent of the `IPoolable` component cache, so it can be registered after the object was created. It runs after cached `OnPoolRelease` and Mirror server unspawn, before storage/reuse/eviction. It also runs if the object is destroyed externally. The slot is detached before invocation: duplicate return, capacity eviction and `OnDestroy` cannot invoke the same registration again.

```csharp
PooledObjectInfo info = instance.GetComponent<PooledObjectInfo>();
NeoObjectPool owner = info.OwnerPool;
int generation = info.SpawnGeneration;
bool registered = info.TryRegisterReleaseCleanup(owner, generation,
    target => ReleaseResourcesOwnedByThisRental(target));
```

On return/destruction the callback sees `IsInPool = true`; it may restore its exact borrowed material and free its owned clone. It must not throw, return/re-rent the transitioning object, or destroy borrowed resources. A rejected registration never replaces the pending callback. This hook does not authorize an old caller to mutate a newer rental: check captured owner/generation and current rental state before every later renderer, geometry, material or return operation. The resource owner must also make its local cleanup record idempotent.

## Examples

### No-Code Example (Inspector)
For prefab authoring, attach [Despawner](Despawner.md) and bind a timer/animation event to `Despawner.Despawn()`. `PooledObjectInfo` is normally added at runtime, so it does not need an Inspector reference. From C#, call `instance.GetComponent<PooledObjectInfo>().Return()` or `SpawnUtility.Despawn(instance)`; repeated returns are ignored. The runtime metadata's Return button is Play Mode only.

## See Also
- [PoolManager](PoolManager.md)
- [Despawner](Despawner.md)
- [Tools/Spawner](README.md)
