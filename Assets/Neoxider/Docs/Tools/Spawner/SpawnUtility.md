# SpawnUtility

**Purpose:** Static spawn/despawn API in `Neo.Tools`. It is not a component. Spawns use `PoolManager` when available; otherwise an automatic pool is created for each prefab (initial size 10, maximum 100 retained inactive objects).

## Runtime API

| API | Contract |
|-----|----------|
| `Spawn(prefab)` / `Spawn(prefab, position)` / `Spawn(prefab, position, rotation)` | Spawn at a world pose; omitted rotation is identity and omitted position is zero. |
| `Spawn(prefab, position, rotation, parent)` | Spawn at a world pose and attach to an optional parent, preserving world pose. |
| `Spawn(prefab, parent)` | Spawn at the parent's world position/rotation (local zero/identity), or world zero/identity when null. |
| `Despawn(instance)` | Return a pooled rental to its owner; destroy an unpooled object. Null is ignored. This does not invoke a `Despawner` event; call `Despawner.DespawnObject` when those effects are required. |
| `IsPoolAvailable` | Always true: fallback pooling is available without a manager. |
| `ClearFallbackPools()` | Destroy inactive fallback entries and the fallback root. Manager-owned pools are unaffected. |
| `bool RetirePool(GameObject source)` | Retire only this exact generated source's manager/fallback registrations, permanently block unified Spawn for it, and preserve every storage root. True means an existing registration was removed. |

All `Spawn` overloads return null for a null or retired source, including retirement from its first prewarm callback. Handle this null result before using the rental. `ReturnToPool()` and `SpawnFromPool(...)` extension methods delegate to this API. Destruction uses `Destroy` during play and `DestroyImmediate` in Edit Mode.

## Disposing an owned generated source

Return the rentals you still own, call `SpawnUtility.RetirePool(generatedSource)`, then destroy your source. A source reference that Unity already reports as destroyed can still remove its exact registration. Missing/null registrations are harmless; a non-null source is marked retired for unified routing even when no pool exists. Future unified Spawn calls for that source return null; a new, distinct source with the same name works normally.

Automatic fallback pools are initialized and registered before prewarm callbacks, just like manager pools. Calling `RetirePool(exactSource)` from the first `OnPoolCreate` or `OnPoolGet` callback removes that in-flight pool and cancels the original Spawn with a null result. The factory does not reinsert it after callbacks. Acquired prewarm clones are released and destroyed; during play, physical destruction completes at Unity's destruction boundary. Ordinary prewarm still acquires the configured initial set before releasing it.

Outstanding rentals held elsewhere remain alive until their valid return, which runs normal release/network callbacks and generation cleanup, then destroys them. Never retire an assigned/shared prefab unless you own its complete source lifetime. No prefab-name root is deleted: multiple exact sources may share it. Retirement markers use weak keys, so markers do not retain destroyed generated sources. They reset on a new play session. `ClearFallbackPools()` keeps its separate global storage-clear semantics.

## Scene ownership

Set `DestroyFallbackPoolsOnSceneLoad` **before the first fallback spawn**. The default is true: fallback storage belongs to the current scene and a Single scene load clears fallback pools. Additive loads leave them intact. Set false to make the root persistent across scene changes; this setting resets to true when entering a new play session, including when domain reload is disabled.

Both prewarmed instances and returned fallback rentals live under the fallback root. Active fallback rentals without an explicit parent also live there. `ClearFallbackPools()` therefore destroys these objects too. An active rental parented outside that root stays caller-owned; return it before clearing if it must be destroyed. With `PoolManager`, a null parent instead leaves an active rental unparented; returned rentals move to the manager's persistent storage.

## Scene / Inspector workflow

Use [SimpleSpawner](SimpleSpawner.md), [Spawner](Spawner.md), or [Despawner](Despawner.md) as UnityEvent adapters. Leave **Use Object Pool** enabled for this API path. Add [PoolManager](PoolManager.md) when you need authored prewarm sizes and storage settings.

## See also

- [NeoObjectPool](NeoObjectPool.md)
- [Module entry](README.md)
