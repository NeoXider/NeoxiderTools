# NeoObjectPool

**What it is:** Plain C# pool for one prefab that wraps Unity's `ObjectPool<GameObject>` and drives [IPoolable](IPoolable.md) callbacks; no scene manager is required. `Scripts/Tools/Spawner/NeoObjectPool.cs`, namespace `Neo.Tools`.

**How to use:**
1. Create it with a prefab and sizes: `new NeoObjectPool(prefab, initialSize, expandPool, maxSize, storageRoot)`.
2. Rent with `GetObject(position, rotation)` and give back with `ReturnObject(instance)` (or `SpawnUtility.Despawn`).
3. Call `Clear()` to drop retained instances, or `Retire()` when you dispose the source. The caller owns the pool and outstanding rentals.

---

## Runtime API

```csharp
NeoObjectPool pool = new(prefab, initialSize: 8, expandPool: true, maxSize: 64, storageRoot: storage);
GameObject instance = pool.GetObject(position, rotation);
pool.ReturnObject(instance);
pool.Clear();
```

| API | Contract |
|-----|----------|
| Constructor | Requires a live prefab; null throws `ArgumentNullException` with parameter `prefab`. Synchronously prewarms the initial set. Negative initial sizes become zero. An optional `storageRoot` owns inactive objects. |
| `GetObject(Vector3, Quaternion)` | Returns an active instance at the requested world pose. Destroyed inactive entries are discarded and replaced. Detaches the instance from its storage root; in Play Mode it then lands unparented in the active scene, like `Object.Instantiate` without a parent, so a storage root kept in DontDestroyOnLoad does not carry it across scene loads. |
| `ReturnObject(GameObject)` | Returns only instances whose `PooledObjectInfo.OwnerPool` is this pool. Null, destroyed, foreign objects and duplicate returns are ignored. O(1) in every build: the guard is `PooledObjectInfo.IsInPool`, not Unity's Editor-only `collectionCheck` scan of the inactive set. |
| `int CountInactive` | Number of retained entries waiting to be rented. Externally destroyed entries are removed when retrieved. |
| `GameObject Prefab` | Source object this pool clones. |
| `bool IsRetired` | True after terminal retirement. |
| `Retire()` | Permanently rejects retrieval, destroys retained inactive instances, and destroys outstanding rentals on their later valid return. It never destroys a storage root. |
| `Clear()` | Destroys retained inactive instances. Active rentals remain caller-owned and can still return; return them before clearing if they should also be destroyed. The pool remains usable. |

`maxSize` is an **inactive retention limit**, not a cap on active objects. With `expandPool = true`, a positive maximum limits retention; zero (or negative) means unlimited retention. With `expandPool = false`, retention is limited to the initial size, with a minimum of one so an initially empty pool remains usable. Either setting can create an object when no inactive instance is available.

After retirement `GetObject` returns null, including after the source itself has been destroyed. Outstanding rentals remain active and keep their owner/generation until returned. Their valid return still runs cached `IPoolable` release callbacks, registered-server Mirror unspawn, and the generation cleanup hook, then destroys the instance instead of retaining it. Duplicate returns/retirement do nothing. `Clear()` remains reusable and keeps its existing inactive-only behavior.

Use `SpawnUtility.RetirePool(source)` when a generated source has manager/fallback registrations. Retiring a raw pool alone does not remove a registry entry. Retirement is for exclusively owned temporary sources; it is not reference counting for shared assigned prefabs. Different exact sources can share a prefab-name storage root, which retirement deliberately preserves.

## Lifecycle and scene integration

The pool finds `IPoolable` components on the instance and inactive children once, caches that set on the instance's `PooledObjectInfo` (so the cache dies with the instance and get/return never search the hierarchy), and calls `OnPoolCreate()` once. Each retrieval increments `SpawnGeneration`, clears `IsInPool`, invokes `OnPoolGet()`, then activates the object. `GetObject` assigns the requested pose after those callbacks; use the returned instance for work that depends on its final position. Prewarming runs get/release callbacks as well.

Normal prewarm acquires the entire initial set before returning it: each new instance receives create/get, then the acquired set receives release. The public constructor keeps this synchronous behavior. Manager and fallback factories register a fully initialized pool before starting the same callback sequence. Retirement from its first create/get callback therefore reaches that pool, stops further prewarm, destroys acquired prewarm instances, and makes the original factory request return null. A startup configuration cannot recreate the retired source through a later duplicate entry. Retirement does not return or re-rent the instance inside its callback; the current acquisition completes before its destructive return.

Returning marks `IsInPool` before invoking `OnPoolRelease()`, reparents to `storageRoot` when supplied, then deactivates. Avoid changing pool ownership or adding/removing lifecycle components on a live rental. For Inspector configuration, use [PoolManager](PoolManager.md); for state reset scripts, use [PoolableBehaviour](PoolableBehaviour.md).

Lifecycle callbacks should complete without exceptions and should not rent/return their own object while its get/release transition is still running.

With Mirror installed, returning a rental registered in the active server's spawned registry calls `NetworkServer.UnSpawn` after local release callbacks and before storage. Callbacks retain their existing network context. Unspawn removes the old server/client registration and allows a subsequent `NetworkServer.Spawn` when reusing the instance. Client-side pooling still requires Mirror's spawn/unspawn handlers; returning a client replica locally does not request a server-authoritative removal.

For resources attached after acquisition (for example, a private line material), register `PooledObjectInfo.TryRegisterReleaseCleanup(owner, generation, callback)`. This runs outside cached component discovery after existing release/network callbacks and before storage, reuse or eviction. It is consumed before invocation and cannot be replayed by a subsequent eviction/OnDestroy. External destruction also consumes it. See [PooledObjectInfo](PooledObjectInfo.md) for the exact generation and callback contract.

## See also

- [SpawnUtility](SpawnUtility.md)
- [IPoolable](IPoolable.md)
- [Module entry](README.md)
