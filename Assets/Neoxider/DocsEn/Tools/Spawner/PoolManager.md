# PoolManager

**Purpose:** Central object pool manager. Automatically creates pools upon first request (or pre-warms configured ones at start), preventing costly `Instantiate` and `Destroy` calls.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Default Initial Size** | The default starting size of a pool if created dynamically (not preconfigured). |
| **Default Expand Pool** | Defines whether the pool is allowed to expand if all instances are currently in use. |
| **Preconfigured Pools** | A list of prefabs (and their limits) to instantiate immediately when the scene starts. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)` | Requests an object from the pool. Creates a new pool automatically if one doesn't exist for the prefab. |
| `static void Release(GameObject instance)` | Returns the object to its pool. If the object isn't from a pool, it is destroyed (`Destroy`). |

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
- ← [Tools/Spawner](../README.md)
