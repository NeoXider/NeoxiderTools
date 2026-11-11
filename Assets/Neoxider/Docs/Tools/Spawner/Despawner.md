# Despawner

**What it is:** `MonoBehaviour` that removes its GameObject safely: a pooled rental returns to its pool, anything else is destroyed. It can spawn an effect prefab first. `Scripts/Tools/Spawner/Despawner.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add `Despawner` to a prefab (enemy, projectile, pickup).
2. Optionally assign **Spawn Prefab On Despawn** (explosion, puff) and **Despawn On Disable**.
3. Call `Despawn()` from a UnityEvent (death, timer, animation event), or `Despawner.DespawnObject(target)` from C#.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Despawn On Disable** | Should `Despawn()` be called automatically when the object is disabled (`OnDisable`). |
| **Despawn After Seconds** | Despawns the object this many seconds after it is enabled, every time it is enabled: pooled effects, projectiles, timed pickups. 0 = off. Scaled time. C#: `DespawnAfterSeconds`. |
| **Spawn Prefab On Despawn** | (Optional) A prefab to spawn at the object's location just before it disappears. |
| **Spawn At This Transform** | Spawn the effect exactly at this object's coordinates (otherwise, at zero). |
| **Spawn Parent** | Optional parent for the effect. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Despawn()` | Despawns this object: returns to pool or destroys it. Spawns the effect if configured. |
| `void DespawnOther(GameObject target)` | Useful for `UnityEvent` binding. Despawns the target object passed as an argument. |
| `static void DespawnObject(GameObject target)` | Static helper. Correctly despawns or pools any object passed as an argument. |

Inactive objects can be despawned, including through **Despawn On Disable**. One despawn request emits one effect/event even when a listener calls `Despawn()` again. A normal pool return does not emit that effect/event again through `OnDisable`. A listener that returns and reissues the same instance keeps the newer rental active. Re-enabling the object resets the request guard for its next lifetime. The Inspector button is available in Play Mode.

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
- [Tools/Spawner](README.md)
