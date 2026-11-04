# SpawnUtility

**What it is:** Static spawn/despawn API (not a component). Spawns use `PoolManager` when available; otherwise an automatic pool is created for each prefab (initial size 10, maximum 100 retained inactive objects). `Scripts/Tools/Spawner/SpawnUtility.cs`, namespace `Neo.Tools`.

**How to use:**
1. Call `SpawnUtility.Spawn(prefab, position, rotation, parent)` wherever you would call `Instantiate`.
2. Call `SpawnUtility.Despawn(instance)` instead of `Destroy`; unpooled objects are destroyed.
3. Optionally add [PoolManager](PoolManager.md) for authored prewarm; the calls stay the same.

---

## Runtime API

| API | Contract |
|-----|----------|
| `Spawn(prefab)` / `Spawn(prefab, position)` / `Spawn(prefab, position, rotation)` | Spawn at a world pose; omitted rotation is identity and omitted position is zero. |
| `Spawn(prefab, position, rotation, parent)` | Spawn at a world pose and attach to an optional parent, preserving world pose. |
| `Spawn(prefab, parent)` | Spawn at the parent's world position/rotation (local zero/identity), or world zero/identity when null. |
| `Despawn(instance)` | Return a pooled rental to its owner; destroy an unpooled object. Null is ignored. This does not invoke a `Despawner` event; call `Despawner.DespawnObject` when those effects are required. |
| `IsPoolAvailable` | Always true: fallback pooling is available without a manager. |
| `ClearFallbackPools()` | Destroy inactive fallback entries and the fallback root, and retire the dropped pools (a rental still out is destroyed on its later return). Prefabs stay spawnable through fresh pools. Manager-owned pools are unaffected. |
| `bool RetirePool(GameObject source)` | Retire only this exact generated source's manager/fallback registrations, permanently block unified Spawn for it, and preserve every storage root. True means an existing registration was removed. |

All `Spawn` overloads return null for a null or retired source, including retirement from its first prewarm callback. Handle this null result before using the rental. `ReturnToPool()` and `SpawnFromPool(...)` extension methods delegate to this API. Destruction uses `Destroy` during play and `DestroyImmediate` in Edit Mode.

## Disposing an owned generated source

Return the rentals you still own, call `SpawnUtility.RetirePool(generatedSource)`, then destroy your source. A source reference that Unity already reports as destroyed can still remove its exact registration. Missing/null registrations are harmless; a non-null source is marked retired for unified routing even when no pool exists. Future unified Spawn calls for that source return null; a new, distinct source with the same name works normally.

Automatic fallback pools are initialized and registered before prewarm callbacks, just like manager pools. Calling `RetirePool(exactSource)` from the first `OnPoolCreate` or `OnPoolGet` callback removes that in-flight pool and cancels the original Spawn with a null result. The factory does not reinsert it after callbacks. Acquired prewarm clones are released and destroyed; during play, physical destruction completes at Unity's destruction boundary. Ordinary prewarm still acquires the configured initial set before releasing it.

Outstanding rentals held elsewhere remain alive until their valid return, which runs normal release/network callbacks and generation cleanup, then destroys them. Never retire an assigned/shared prefab unless you own its complete source lifetime. No prefab-name root is deleted: multiple exact sources may share it. Retirement markers use weak keys, so markers do not retain destroyed generated sources. They reset on a new play session. `ClearFallbackPools()` keeps its separate global storage-clear semantics: it retires the dropped pools but not their sources, so the next `Spawn` builds a fresh pool.

## Scene ownership

Set `DestroyFallbackPoolsOnSceneLoad` **before the first fallback spawn**. The default is true: fallback storage belongs to the current scene and a Single scene load clears fallback pools. Additive loads leave them intact. Unity raises `sceneLoaded` after the incoming scene's `Awake`/`OnEnable`; a fallback root those callbacks created already belongs to the loaded scene, so it and its fresh rentals are kept and only pools whose storage died with the previous scene are dropped. A source that already had a fallback pool in the previous scene gets a fresh pool under the live root when the incoming scene spawns it, even before `sceneLoaded`. A dropped pool is retired (the source is not): its rentals that are still out, for example under a persistent parent, are destroyed on their return instead of being parked inactive with no storage. Set false to make the root persistent across scene changes; this setting resets to true when entering a new play session, including when domain reload is disabled.

Both prewarmed instances and returned fallback rentals live under the fallback root. Active fallback rentals without an explicit parent also live there. `ClearFallbackPools()` therefore destroys these objects too. An active rental parented outside that root stays alive and caller-owned; since 12.0 its later return destroys it (before, it was deactivated in place and never reused). With `PoolManager`, a null parent instead leaves an active rental unparented in the active scene, which a Single scene load destroys (before 12.0 it stayed in the manager's DontDestroyOnLoad scene and survived the load); returned rentals move to the manager's persistent storage.

## Scene / Inspector workflow

Use [SimpleSpawner](SimpleSpawner.md), [Spawner](Spawner.md), or [Despawner](Despawner.md) as UnityEvent adapters. Leave **Use Object Pool** enabled for this API path. Add [PoolManager](PoolManager.md) when you need authored prewarm sizes and storage settings.

## See also

- [NeoObjectPool](NeoObjectPool.md)
- [Module entry](README.md)
