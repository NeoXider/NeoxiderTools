# PoolManager

**Purpose:** Central object pool manager. Automatically creates pools upon first request (or pre-warms configured ones at start), preventing costly `Instantiate` and `Destroy` calls.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Default Initial Size** | The default starting size of a pool if created dynamically (not preconfigured). |
| **Default Expand Pool** | Selects the inactive retention policy: enabled uses Max Size; disabled retains up to Initial Size (at least one). Both can create new active rentals when empty. |
| **Default Max Size** | Maximum retained inactive objects when expansion is enabled. `0` = unlimited retention; this does not limit simultaneous active objects. |
| **Preconfigured Pools** | A list of prefabs (and their limits) to instantiate immediately when the scene starts. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)` | Requests an active object at a world pose. Creates a pool if missing. A null or retired source returns null, including retirement during initial prewarm; a missing manager logs a diagnostic and returns null. |
| `static void Release(GameObject instance)` | Returns the object to its pool. If the object isn't from a pool, it is destroyed (`Destroy`). |
| `static bool RetirePool(GameObject source)` | Removes and retires the exact source registration in the current manager, without creating/searching for a manager. True means a registration was removed; later Get in that manager returns null for this retired source. |

The manager and its inactive storage survive scene changes. Active objects use the requested parent (null leaves them unparented), so scene ownership still applies until returned. Null/empty preconfiguration entries are ignored; duplicate singleton managers do not prewarm pools. See [NeoObjectPool](NeoObjectPool.md) for callback order, prewarm behavior, and retention limits. Use [SpawnUtility](SpawnUtility.md) when a manager is optional.

For an exclusively owned automatic source, prefer `SpawnUtility.RetirePool(source)` so both manager and fallback registrations are retired if routing changed. Retirement removes the exact registration before releasing retained objects, blocks recreation in the current manager, and leaves valid outstanding rentals alive until a destructive late return. It never deletes a root found by name; equal prefab names do not establish shared ownership. An unrelated same-named prefab and its rentals remain usable. Retired-source markers have weak keys. This is terminal source disposal, not a replacement for reusable `NeoObjectPool.Clear()` or a shared-prefab reference counter.

Both dynamic creation and Inspector-configured startup prewarm register the fully initialized pool before the first `OnPoolCreate`/`OnPoolGet` callback. `RetirePool(exactSource)` inside that callback removes the in-flight registration and returns true. The outer request returns null, remaining prewarm stops, and duplicate startup entries cannot recreate that source. Normal prewarm counts and create/get/release order are unchanged; other source identities continue normally.

## Examples

### No-Code Example (Inspector)
Add `PoolManager` to an empty `Managers` object. Expand `Preconfigured Pools` and add a bullet prefab with `Initial Size = 50`. The game will spawn 50 bullets right at start, preventing lag during shooting.

### Code Example
```csharp
[SerializeField] private GameObject _bulletPrefab;
[SerializeField] private Transform _firePoint;

public void Shoot()
{
    // Grabs a bullet from the pool (creates the pool if it doesn't exist yet)
    GameObject bullet = PoolManager.Get(_bulletPrefab, _firePoint.position, _firePoint.rotation);
}
```

## See Also
- [Spawner](Spawner.md)
- [PooledObjectInfo](PooledObjectInfo.md)
- [Despawner](Despawner.md)
- [Tools/Spawner](README.md)
