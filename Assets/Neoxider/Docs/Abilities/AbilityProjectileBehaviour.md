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

The ability's **Projectile Speed** overrides **Speed** when non-zero (passed as the spawn magnitude).

## Key API

| Member | Description |
|--------|-------------|
| `void OnSpawned(SpawnRequest request, AbilitySystemBehaviour hub)` | `ISpawnedAbilityEntity` entry point. The hub calls it on spawn with the cast id, owner, target/direction, and speed. You normally never call it yourself. |

Behavior: each frame it advances, re-homes toward the live target if it has one, and calls `QueryUnitsInRadius`. For each enemy of the owner within **Hit Radius** it calls `NotifyProjectileHit(castId, hitUnit, position)` (running Impact Effects), counts a hit, and despawns at **Max Hits**. A directional/point projectile with no homing target detonates at its target point (reporting a hit with `UnitId.None`). It despawns at **Max Lifetime**.

## Example

**Inspector:** create a prefab (sprite/mesh + this component), set Speed = `20`, Max Lifetime = `4`, Hit Radius = `0.5`, Max Hits = `1`. On the hub, bind `{ Id = "fireball_projectile", Prefab = <this prefab> }`. On the `fireball` [AbilityDefinition](./AbilityDefinition.md): Delivery = `Projectile`, Projectile Archetype Id = `fireball_projectile`, and the area damage + burn nodes under **Impact Effects**.

Casting then handles the rest:

```csharp
caster.TryCastAtUnit("fireball", enemyUnitBehaviour); // spawns a homing projectile
// or
caster.TryCastAtPoint("fireball", groundPoint);       // flies to the point, detonates there
```

## Pitfalls

- **Hit detection is distance-based, not physics.** It only sees units registered in the hub (with an [AbilityUnitBehaviour](./AbilityUnitBehaviour.md)); it ignores colliders, walls, and unregistered objects.
- **It hits enemies of the owner only.** Allies are skipped; the `Team Filter` on Impact Effect nodes still applies on top for area splash.
- **Impact Effects, not the projectile, deal damage.** An empty Impact Effects list means the projectile flies and hits but does nothing.
- **Pierce fires Impact Effects once per unit hit.** For area abilities that means the splash resolves per pierced unit — usually fine, but tune numbers with that in mind.
- Requires `PoolManager` (Neo.Tools). The prefab is pooled, so avoid one-time `Awake` setup that assumes a fresh instance — initialize in `OnSpawned`.

## See also

- [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) — binds archetypes and spawns projectiles
- [AbilityDefinition](./AbilityDefinition.md) — set Delivery = Projectile
- Back: [Abilities module](./README.md)
