# ExplosiveForce

**Purpose:** Applies an impulse to nearby 3D rigidbodies, with custom falloff, optional random force variation, and optional destruction of the emitter. Automatic modes fire once; `Manual` permits repeated calls.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Activation Mode** | When to detonate: `OnStart`, `OnAwake`, `Delayed`, `Manual` (script/event only). |
| **Delay** | Scaled seconds before detonating. `Delayed` schedules from `Start`; `OnStart` and `OnAwake` also honor a positive delay. A nonpositive delay detonates immediately at that lifecycle point. |
| **Force** / **Force Randomness** | Base explosion force and random variance (`±Randomness`). |
| **Force Mode** | `AddExplosionForce` uses Unity's closest surface point for direction/application; `AddForce` uses the direction from the emitter to the body's world center of mass. Both use `UnityEngine.ForceMode.Impulse`. |
| **Falloff Type** | At center-of-mass distance `d` within radius `r`: `Linear = 1 - d/r`, `Quadratic = 1 - (d/r)^2`. Applied exactly once in either force mode; zero at or beyond `r`. |
| **Radius** / **Affected Layers** | Overlap radius and layer mask, checked against collider GameObjects. |
| **Add Rigidbody If Needed** | Add a Rigidbody to a detected collider GameObject if it has no attached body. |
| **Destroy After Explosion** | Delete this GameObject from the scene immediately after exploding (or after `Destroy Delay`). |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void Explode()` | Detonate immediately using the base force. |
| `void Explode(float customForce)` | Positive values override the base force; zero or negative values use the base force. Random variance still applies. |
| `void SetForce(float newForce)` / `SetRadius(float newRadius)` | Change base force/radius; negative values clamp to zero. |
| `float CurrentForce` / `CurrentRadius` | Read the configured base values. |
| `bool HasExploded` | Whether a detonation has occurred since the last reset; also set in `Manual` mode. |
| `void ResetExplosion()` | Clear `HasExploded`, permitting another explicit call in automatic modes. Does not schedule a new delayed explosion. |

Compound colliders receive one impulse per Rigidbody and one `OnObjectAffected` notification, carrying a representative detected collider GameObject. The emitter's own attached Rigidbody is excluded, including its child colliders. The **Explode Now** Inspector button is available in Play Mode.

If an `OnObjectAffected` listener immediately destroys the emitter component or its GameObject, the current explosion stops before affecting another body or emitting `OnExplode`. Already submitted impulses remain applied. If an `OnExplode` listener destroys the emitter, the old automatic destruction step is skipped; destroying only the component leaves its GameObject alive. Explicit calls on a live disabled component remain supported, subject to the normal activation/repeat rules. A queued `Destroy` follows Unity's deferred lifetime timing; these checks respond to actual destruction.

Automatic scheduling happens once from `Awake`/`Start`. Disabling the GameObject cancels Unity coroutines; enabling it again does not rearm the delay. For pooled effects, use `Manual` and explicitly call `Explode()` when needed.

Compatibility: selected falloff is no longer multiplied by Unity's additional linear falloff. Blasts in `AddExplosionForce` mode may therefore be stronger at the same settings. Radial distance now uses the body's center of mass consistently for compound objects.

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnExplode` | *(none)* | Fired at the exact moment of detonation (great for spawning VFX/SFX). |
| `OnObjectAffected` | `GameObject` | Once per affected Rigidbody, after submitting its impulse; payload is a detected collider GameObject. Kinematic bodies do not move. |

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
- ← [Tools/Physics](README.md)
