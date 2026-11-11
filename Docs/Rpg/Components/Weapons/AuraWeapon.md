# AuraWeapon

**What it is:** a `MonoBehaviour` (`Scripts/Rpg/Components/Weapons/AuraWeapon.cs`, extends `MeleeWeapon`)
that damages every valid `RpgCharacter` inside a sphere on each pulse. Damage, hit layers and target tag
come from the base fields.

**How to use:**
1. Add the component; a `SphereCollider` (forced to trigger) defines the aura radius.
2. Set `hitLayers` (the query only searches these layers; the default `Nothing` hits nobody) and `targetTag`.
3. Add a looping `TimerObject` on the same GameObject - its `OnTimerCompleted` is wired to
   `ApplyAuraDamage()` in `Awake` - or call `ApplyAuraDamage()` from code or a UnityEvent.

## Key API

| Member | Description |
|--------|-------------|
| `ApplyAuraDamage()` | Damages all valid targets currently inside the aura bounds. |

The query uses `SphereCollider.center` in world space and the largest absolute world scale component,
so offset colliders and mirrored objects keep their authored radius. Each `RpgCharacter` is damaged
once per call even when it has several hurtbox colliders. Pulses reuse a growable collider buffer and
allocate nothing once warm (a zero-GC play-mode test guards this); a pulse started from a damage callback of a running pulse is skipped. Destroying the weapon removes its automatic
timer listener; a scene UnityEvent can also drive the same public method directly. Each hit passes the
aura's wielder (the `RpgCharacter` above it) as `RpgDamageInfo.Source`, so the target's damage modifiers and
`DamageApplied` see who hit (see [MeleeWeapon](MeleeWeapon.md#damage-pipeline)).

## Key Fields (Inspector)

Inherited from `MeleeWeapon`:

| Field | Description |
|-------|-------------|
| `damage` | Damage per pulse and target, scaled by the wielder's outgoing damage multiplier. |
| `hitLayers` | Layers the aura query searches. |
| `targetTag` | Required tag of the hit collider; empty accepts any tag. |
| `Damage Type` (`DamageType`) | Damage type of every pulse (`Fire`, `Cold`, ...); empty deals untyped damage. |

## See Also

- [MeleeWeapon](MeleeWeapon.md)
- [Module Root](../../README.md)
