# Tools / Spawner

Reusable prefab pools and optional scene spawners. Namespace: `Neo.Tools`; assembly: `Neo.Tools.Spawner`. This is the canonical module entry.

## C# entry points

Use `SpawnUtility.Spawn(prefab, position, rotation, parent)` and `SpawnUtility.Despawn(instance)` for ordinary gameplay. Both work without a `PoolManager`: missing managers use automatic per-prefab fallback pools. A null prefab returns null; despawning null does nothing.

```csharp
GameObject projectile = SpawnUtility.Spawn(projectilePrefab, muzzle.position, muzzle.rotation);
// At the end of this rental's lifetime:
SpawnUtility.Despawn(projectile);
```

For explicit ownership, construct a [NeoObjectPool](NeoObjectPool.md), call `GetObject`/`ReturnObject`, return outstanding rentals, then `Clear`. Implement [IPoolable](IPoolable.md), or inherit [PoolableBehaviour](PoolableBehaviour.md), to reset state on reuse. Prewarming also invokes get/release callbacks.

For an exclusively owned generated source, return your current rentals, call `SpawnUtility.RetirePool(source)`, then destroy the source. Retirement is terminal: no new rentals, inactive objects are cleared, outstanding external rentals are destroyed on valid return. Shared storage roots and unrelated same-named sources survive. Assigned/shared prefabs must keep their existing lifetime. Resources created after acquisition can use the one-shot `PooledObjectInfo.TryRegisterReleaseCleanup(owner, generation, callback)` hook; delayed and old-owner operations still need their captured owner/generation checks.

Retirement also works from the first prewarm create/get callback: the pool is already registered, and the original Spawn/Get returns null. Startup prewarm does not recreate that source through duplicate entries. Handle a null rental when your callbacks can retire its source; see [PoolManager](PoolManager.md) and [SpawnUtility](SpawnUtility.md).

## Scene and Inspector workflow

Add [PoolManager](PoolManager.md) to prewarm prefabs and retain returned objects across scenes. Add [Spawner](Spawner.md) for loop/wave production, or [SimpleSpawner](SimpleSpawner.md) for a single UnityEvent action. Their **Use Object Pool** setting selects pooling (manager or fallback); disabling it selects `Instantiate`. Add [Despawner](Despawner.md) to a prefab and bind `Despawn()` to a death, animation or timer event. Runtime Inspector buttons are available in Play Mode.

Pool sizes limit retained inactive objects, not simultaneous active rentals. Keep a separate gameplay limit when needed. `Spawner` tracks pooled rentals by generation so clearing an old spawner cannot return a newer rental owned elsewhere. Set `SpawnUtility.DestroyFallbackPoolsOnSceneLoad` before the first fallback spawn; see [SpawnUtility](SpawnUtility.md) for scene ownership and clearing.

## Component and API reference

| Page | Description |
|------|-------------|
| [Spawner](./Spawner.md), [SimpleSpawner](./SimpleSpawner.md), [Despawner](./Despawner.md) | Spawn/despawn |
| [PoolManager](./PoolManager.md), [NeoObjectPool](./NeoObjectPool.md), [SpawnUtility](./SpawnUtility.md) | Pool ownership and C# entry points |
| [IPoolable](./IPoolable.md), [PoolableBehaviour](./PoolableBehaviour.md), [PooledObjectInfo](./PooledObjectInfo.md) | Lifecycle and rental metadata |

## See also

- [Tools/Random](../Random/README.md)
- [GridSystem](../../GridSystem.md)
