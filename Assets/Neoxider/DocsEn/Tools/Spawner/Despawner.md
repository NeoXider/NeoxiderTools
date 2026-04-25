# Despawner

**Purpose:** A utility to remove objects from the scene safely. It automatically detects if an object belongs to a pool (and returns it) or if it should be destroyed via `Destroy`. It can also spawn another prefab just before disappearing (e.g., an explosion effect).

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Despawn On Disable** | Should `Despawn()` be called automatically when the object is disabled (`OnDisable`). |
| **Spawn Prefab On Despawn** | (Optional) A prefab to spawn at the object's location just before it disappears. |
| **Spawn At This Transform** | Spawn the effect exactly at this object's coordinates (otherwise, at zero). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Despawn()` | Despawns this object: returns to pool or destroys it. Spawns the effect if configured. |
| `void DespawnOther(GameObject target)` | Useful for `UnityEvent` binding. Despawns the target object passed as an argument. |
| `static void DespawnObject(GameObject target)` | Static helper. Correctly despawns or pools any object passed as an argument. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnDespawn` | *(none)* | Fired right before the object is despawned/destroyed. |

## Examples

### No-Code Example (Inspector)
Attach `Despawner` to an enemy prefab. Assign an explosion VFX to `Spawn Prefab On Despawn`. Call `Despawner.Despawn()` via a UnityEvent when the enemy's health reaches zero. The enemy will disappear and leave an explosion behind.

### Code Example
```csharp
public void OnProjectileHitTarget(GameObject projectile)
{
    // Safely remove the projectile: returns to pool if pooled, else Destroy.
    Despawner.DespawnObject(projectile);
}
```

## See Also
- [PoolManager](PoolManager.md)
- [Spawner](Spawner.md)
- ← [Tools/Spawner](../README.md)
