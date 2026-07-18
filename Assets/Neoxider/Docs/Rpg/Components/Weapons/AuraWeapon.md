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
