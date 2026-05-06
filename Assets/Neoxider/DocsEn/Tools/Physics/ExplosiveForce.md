# ExplosiveForce

**Purpose:** A one-time explosion that physically scatters objects within a specified radius (uses `Rigidbody.AddExplosionForce`). Supports delays, random force variations, and self-destruction after detonating.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Activation Mode** | When to detonate: `OnStart`, `OnAwake`, `Delayed`, `Manual` (script/event only). |
| **Delay** | Delay before detonating (used in `Delayed` or `OnStart` with delay). |
| **Force** / **Force Randomness** | Base explosion force and random variance (`±Randomness`). |
| **Force Mode** | `AddExplosionForce` (standard Unity radial explosion) or `AddForce` (linear directional impulse from center). |
| **Falloff Type** | How force weakens towards the radius edge: `Linear` or `Quadratic`. |
| **Destroy After Explosion** | Delete this GameObject from the scene immediately after exploding (or after `Destroy Delay`). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Explode()` | Detonate immediately using the base force. |
| `void Explode(float customForce)` | Detonate immediately using a custom override force. |
| `void ResetExplosion()` | Reset the `HasExploded` flag so the object can explode again. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnExplode` | *(none)* | Fired at the exact moment of detonation (great for spawning VFX/SFX). |
| `OnObjectAffected` | `GameObject` | Fired individually for each object hit by the blast. |

## Examples

### No-Code Example (Inspector)
Create a grenade prefab (just an empty object). Attach `ExplosiveForce`. Set `Activation Mode = Delayed`, `Delay = 3`, `Destroy After = true`. In the `OnExplode` event, trigger a `SimpleSpawner.Spawn()` to instantiate a visual explosion effect. When you spawn this grenade, it will wait 3 seconds, blast physics objects away, and delete itself.

### Code Example
```csharp
[SerializeField] private ExplosiveForce _mineExplosion;

public void StepOnMine()
{
    // Force the mine to detonate
    _mineExplosion.Explode();
}
```

## See Also
- [MagneticField](MagneticField.md)
- [ImpulseZone](ImpulseZone.md)
- ← [Tools/Physics](../README.md)
