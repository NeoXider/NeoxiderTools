# Tools / Spawner

**What it is:** Reusable prefab pools (`NeoObjectPool`, `PoolManager`, `SpawnUtility`) plus optional scene spawners and a despawner. Scripts in `Scripts/Tools/Spawner/`, namespace `Neo.Tools`, assembly `Neo.Tools.Spawner`. This is the canonical module entry.

**Contents:** see [Component and API reference](#component-and-api-reference) below.

**How to work with it:**
1. From C#, spawn with `SpawnUtility.Spawn(...)` and return with `SpawnUtility.Despawn(...)`; no scene setup is required.
2. Add [PoolManager](PoolManager.md) when you want authored prewarm sizes and storage that survives scene changes.
3. For NoCode scenes use [Spawner](Spawner.md), [SimpleSpawner](SimpleSpawner.md) and [Despawner](Despawner.md) through UnityEvents.

---

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

A destroyed `PoolManager` retires all of its pools the same way: its stored instances die with it, and a rental still out is destroyed on its later return instead of being left inactive where it stands (PlayMode `PoolManagerTeardownTests`).

## Performance contract

- `GetObject`/`ReturnObject` (and `SpawnUtility.Spawn`/`Despawn` on a warm pool) are O(1) and do not allocate. The per-instance `IPoolable` set is cached on `PooledObjectInfo` at creation.
- Double, foreign and destroyed returns are rejected by the `PooledObjectInfo.IsInPool`/owner check in every build. Unity's `collectionCheck` (an O(n) scan per return that Unity runs only in the Editor, never in Player builds) is therefore disabled: Editor and Play Mode returns no longer slow down as the pool grows, while Player builds behave as before.
- `Spawner.GetSpawnPosition()` (area sampling and deny-zone re-rolls) and `Spawner.GetActiveObjectCount()` do not
  allocate. Fixed spawner waits (equal min/max spawn delay, wave pause, destroy delay) reuse one `WaitForSeconds`.
- Covered by allocation guards: EditMode `PoolingHotPathTests` (warm Get/Return, area sampling, active count) and
  PlayMode `PoolCallbackCacheTests` (the cached `IPoolable` callbacks and a warm cycle that runs them).
- Prewarm through `PoolManager` **Preconfigured Pools** (or a `NeoObjectPool` initial size) to move `Instantiate` cost to load time.

## Scene and Inspector workflow

Add [PoolManager](PoolManager.md) to prewarm prefabs and retain returned objects across scenes. Add [Spawner](Spawner.md) for loop/wave production, or [SimpleSpawner](SimpleSpawner.md) for a single UnityEvent action. Their **Use Object Pool** setting selects pooling (manager or fallback); disabling it selects `Instantiate`. Add [Despawner](Despawner.md) to a prefab and bind `Despawn()` to a death, animation or timer event. Runtime Inspector buttons are available in Play Mode.

Pool sizes limit retained inactive objects, not simultaneous active rentals. Keep a separate gameplay limit when needed. `Spawner` tracks pooled rentals by generation so clearing an old spawner cannot return a newer rental owned elsewhere. Set `SpawnUtility.DestroyFallbackPoolsOnSceneLoad` before the first fallback spawn; see [SpawnUtility](SpawnUtility.md) for scene ownership and clearing. Objects the incoming scene spawns from its own `Awake`/`OnEnable` survive that scene's load.

## Component and API reference

| Page | Description |
|------|-------------|
| [Spawner](./Spawner.md), [SimpleSpawner](./SimpleSpawner.md), [Despawner](./Despawner.md) | Spawn/despawn |
| [PoolManager](./PoolManager.md), [NeoObjectPool](./NeoObjectPool.md), [SpawnUtility](./SpawnUtility.md) | Pool ownership and C# entry points |
| [IPoolable](./IPoolable.md), [PoolableBehaviour](./PoolableBehaviour.md), [PooledObjectInfo](./PooledObjectInfo.md) | Lifecycle and rental metadata |

## See also

- [Tools/Random](../Random/README.md)
- [GridSystem](../../GridSystem.md)
