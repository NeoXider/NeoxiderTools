# AbilityProjectileBehaviour

**What it is:** a pooled, data-driven projectile. It homes to the target unit or flies toward the cast point/direction, detects hits with distance checks against registered units (no colliders or physics setup), reports each hit back into the domain (`AbilitySystem.NotifyProjectileHit`) so the ability's Impact Effects run, and releases itself through `PoolManager`.

**How to use:** put this component on a projectile prefab, bind that prefab to an archetype id under [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) **Archetypes**, and set an ability's **Delivery** to `Projectile` with a matching **Projectile Archetype Id**. The hub spawns and initializes it automatically.

- Namespace: `Neo.Abilities`
- File: `Assets/Neoxider/Scripts/Abilities/Components/AbilityProjectileBehaviour.cs`
- Component menu: `Neoxider/Abilities/Ability Projectile`
- Implements `ISpawnedAbilityEntity`.

## Fields (Inspector)

| Field | Type | Description |
|-------|------|-------------|
| **Speed** | float | Fallback speed when the spawn request carries none (world units/second). |
| **Max Lifetime** | float | Seconds before the projectile despawns if it never hits. |
| **Hit Radius** | float | Hit-detection radius in world units. |
| **Max Hits** | int | Units it can hit before despawning (pierce). `1` = single hit. |
| **Plane** | `AbilityZonePlane` | `GroundXZ` (default): the forward axis follows the flight, for 3D models. `ScreenXY`: the projectile turns around Z so its right (+X) follows the flight, for 2D sprites drawn pointing right. The hub uses the same facing for the spawn rotation (`AbilityZonePlaneFacing.Facing`). |
| **Motion** | `ProjectileMotion` | `Flight` (default): homes on the target unit or flies straight. `Orbit`: circles its owner until **Max Lifetime** runs out and ignores **Max Hits** (orbiting blades, a rotating shield). Give the ability `NoTarget` targeting and `Projectile` delivery. |
| **Orbit Radius** | float | Orbit: distance from the owner. Not used when **Hit Points** are assigned: the root then stays on the owner and only turns. |
| **Orbit Degrees Per Second** | float | Orbit speed; negative turns the other way. |
| **Hit Points** | `Transform[]` | Points that hit units, each with **Hit Radius**: the blades of an orbiting ring. Empty = the object itself. |
| **Rehit Interval** | float | Seconds before the same unit can be hit again. `0` = each unit once per projectile. |

The ability's **Projectile Speed** overrides **Speed** when non-zero (passed as the spawn magnitude).

## Key API

| Member | Description |
|--------|-------------|
| `void OnSpawned(SpawnRequest request, AbilitySystemBehaviour hub)` | `ISpawnedAbilityEntity` entry point. The hub calls it on spawn with the cast id, owner, target/direction, and speed. You normally never call it yourself. |
| `bool IsFlying` | `true` from `OnSpawned` until the projectile finishes, despawns or is disabled. |
| `uint CastId` | The cast whose hits this projectile reports (`0` before the first spawn). |

Behavior: each frame it advances, re-homes toward the live target if it has one, and calls `QueryUnitsInRadius`. For each enemy of the owner within **Hit Radius** it calls `NotifyProjectileHit(castId, hitUnit, position)` (running Impact Effects), counts a hit, and despawns at **Max Hits**. A homing projectile whose target is gone keeps flying straight: along its last direction, or along the launch direction when the target was gone from the start. A directional/point projectile with no homing target detonates at `SpawnRequest.Position + SpawnRequest.Direction` (reporting a hit with `UnitId.None`): the cast point for `Point` abilities, and the end of the ability's reach for `Direction` skillshots. A positive **Range** reaches `Range + cast_range_bonus` along the aim, whatever the length of the vector passed to `TryCastTowards`. With Range `0` (unlimited) the reach is that vector's length, at least `AbilitySystem.DefaultDirectionReach` (10), so `target - origin` arrives at the target. It despawns at **Max Lifetime**.

Lifecycle: despawning releases the pending cast (`ReleaseProjectileCast`) and returns the object to the pool. If the object is disabled or destroyed from outside (scene unload, pool clear, `SetActive(false)`), `OnDisable` releases the pending cast immediately, so later hit reports for that cast return `false` instead of waiting for the domain timeout. If the hub is destroyed while the projectile flies, the projectile returns itself to the pool on its next update.

Performance: the projectile caches its transform and does one radius query per frame. With many projectiles enable **Spatial Cell Size** on the hub so each query visits only nearby units.

## Example

**Inspector:** create a prefab (sprite/mesh + this component), set Speed = `20`, Max Lifetime = `4`, Hit Radius = `0.5`, Max Hits = `1`. On the hub, bind `{ Id = "fireball_projectile", Prefab = <this prefab> }`. On the `fireball` [AbilityDefinition](./AbilityDefinition.md): Delivery = `Projectile`, Projectile Archetype Id = `fireball_projectile`, and the area damage + burn nodes under **Impact Effects**.

Casting then handles the rest:

```csharp
caster.TryCastAtUnit("fireball", enemyUnitBehaviour); // spawns a homing projectile
// or
caster.TryCastAtPoint("fireball", groundPoint);       // flies to the point, detonates there
// or, for a Direction-targeted skillshot:
caster.TryCastTowards("arrow", transform.forward);    // flies Range (+ cast_range_bonus) along the aim, hitting enemies on the way
```

## Pitfalls

- **Hit detection is distance-based, not physics.** It only sees units registered in the hub (with an [AbilityUnitBehaviour](./AbilityUnitBehaviour.md)); it ignores colliders, walls, and unregistered objects.
- **It hits enemies of the owner only.** Allies are skipped; the `Team Filter` on Impact Effect nodes still applies on top for area splash.
- **Use another delivery host for an allied projectile heal/buff.** The default scene projectile's enemy-only collision filter does not follow the ability's Unit-target team filter. `AbilitySystem.NotifyProjectileHit` is the typed integration path for a custom host.
- **Movement and lifetime use scene `Time.deltaTime`.** `AbilitySystemBehaviour.Paused` suspends domain ticking and auto-casting; it does not suspend this projectile's movement, collision or impact callbacks. A game-level pause must also pause scene time or its delivery host.
- **Fast projectiles can cross a unit between frame samples.** Detection queries the current position, without a swept collision test. Choose a suitable step/speed/radius or provide a host with swept collision for fast skillshots.
- **Custom hosts own completion.** Report impacts with `NotifyProjectileHit(castId, unit, point)` and call `ReleaseProjectileCast(castId)` on completion/cancellation. The domain's `PendingProjectileTimeout` (default 30 seconds of domain tick time) is a leak guard for missing completion, not a projectile lifetime.
- **Impact Effects, not the projectile, deal damage.** An empty Impact Effects list means the projectile flies and hits but does nothing.
- **Pierce fires Impact Effects once per unit hit.** For area abilities that means the splash resolves per pierced unit — usually fine, but tune numbers with that in mind.
- Requires `PoolManager` (Neo.Tools). The prefab is pooled, so avoid one-time `Awake` setup that assumes a fresh instance — initialize in `OnSpawned`.

## See also

- [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) — binds archetypes and spawns projectiles
- [AbilityDefinition](./AbilityDefinition.md) — set Delivery = Projectile
- Back: [Abilities module](./README.md)
