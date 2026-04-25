# SimpleSpawner

**Purpose:** A lightweight version of the spawner. Ideal for one-off spawning (like a projectile when shooting or a particle effect) without complex wave or delay logic. Compatible with `PoolManager`.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Prefab** | The single prefab to spawn. |
| **Offset** | Position offset relative to this spawner object. |
| **Euler Angle** | Fixed rotation angle for the spawned object. |
| **Use Parent** | Should the spawned object be a child of this spawner. |
| **Use Object Pool** | Should it use `PoolManager`. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Spawn()` | Spawns the `Prefab` with the specified `Offset` and `Euler Angle`. Perfect for calling from a UnityEvent. |

## Examples

### No-Code Example (Inspector)
Attach `SimpleSpawner` to a button or chest, and assign a "Coin" prefab. Bind the `SimpleSpawner.Spawn()` method to the click/open event. A coin will appear!

### Code Example
```csharp
[SerializeField] private SimpleSpawner _bulletSpawner;

public void Fire()
{
    _bulletSpawner.Spawn(); // Perfect for simple, parameterless calls
}
```

## See Also
- [Spawner](Spawner.md)
- [PoolManager](PoolManager.md)
- ← [Tools/Spawner](../README.md)
