# SimpleSpawner

**What it is:** Lightweight `MonoBehaviour` spawner for one prefab at this transform (plus offset), without waves or delays; pooled by default. `Scripts/Tools/Spawner/SimpleSpawner.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add `SimpleSpawner` to a muzzle, chest or button object and assign **Prefab**.
2. Bind `Spawn()` to a UnityEvent (click, open, shot).
3. From C#, call `SpawnInstance()` to get the spawned object back.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Prefab** | The single prefab to spawn. |
| **Offset** | World-space offset added to this spawner's world position. |
| **Euler Angle** | Fixed rotation angle for the spawned object. |
| **Use Parent** | Should the spawned object be a child of this spawner. |
| **Use Object Pool** | Use `PoolManager` or automatic fallback pools. Disable to use `Instantiate`. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Spawn()` | Spawns the `Prefab` with the specified `Offset` and `Euler Angle`. UnityEvent entry point. |
| `GameObject SpawnInstance()` | Typed C# path: same spawn, returns the instance (null and an error log when no prefab is assigned). |

## Examples

### No-Code Example (Inspector)
Attach `SimpleSpawner` to a button or chest, and assign a "Coin" prefab. Bind the `SimpleSpawner.Spawn()` method to the click/open event. A coin will appear!

### Code Example
```csharp
[SerializeField] private SimpleSpawner _bulletSpawner;

public void Fire()
{
    GameObject bullet = _bulletSpawner.SpawnInstance();
    if (bullet != null)
    {
        bullet.GetComponent<Rigidbody>().linearVelocity = _bulletSpawner.transform.forward * 20f;
    }
}
```

## See Also
- [Spawner](Spawner.md)
- [PoolManager](PoolManager.md)
- [Tools/Spawner](README.md)
