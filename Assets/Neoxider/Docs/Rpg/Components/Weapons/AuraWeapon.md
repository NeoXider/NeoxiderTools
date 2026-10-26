# AuraWeapon

**Purpose:** Damages every valid target inside a sphere trigger on a timer tick. Extends `MeleeWeapon`, so damage, hit layers and target tag come from the base fields.

## Setup

- Add the component; a `SphereCollider` (forced to trigger) defines the aura radius.
- Add a `TimerObject` on the same GameObject with looping enabled — its `OnTimerCompleted` is wired to `ApplyAuraDamage()` automatically in `Awake`.
- Alternatively call `ApplyAuraDamage()` from your own code or a UnityEvent.

## Key API

| Member | Description |
|--------|-------------|
| `ApplyAuraDamage()` | Damages all valid targets currently inside the aura bounds. |

The query uses `SphereCollider.center` in world space and the largest absolute world scale component,
so offset colliders and mirrored objects keep their authored radius. Each `RpgCharacter` is damaged
once per call even when it has several hurtbox colliders. Destroying the weapon removes its automatic
timer listener; a scene UnityEvent can also drive the same public method directly.

## Key Fields (Inspector)

Inherited from `MeleeWeapon`:

| Field | Description |
|-------|-------------|
| `damage` | Damage per tick. |
| `hitLayers` | Hit Layers. |
| `targetTag` | Target Tag. |

## See Also

- [MeleeWeapon](MeleeWeapon.md)
- [Module Root](../../README.md)
