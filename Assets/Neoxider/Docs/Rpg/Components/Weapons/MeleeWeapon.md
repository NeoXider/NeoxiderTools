# MeleeWeapon

**What it is:** the abstract base class for simple RPG melee weapons (`Neo.Rpg.Components.Weapons`), shared by
[AuraWeapon](AuraWeapon.md). It filters colliders by layer, tag and own hierarchy and damages the
`RpgCharacter` that owns a hit collider.

**How to use:**
1. Use a concrete weapon such as `AuraWeapon`, or derive your own and call `IsValidTarget(collider)` and
   `DealDamage(collider)` from its hit detection.
2. Set **Hit Layers**, **Target Tag** (empty = any tag), **Damage** and optionally **Damage Type** in the Inspector.
3. Put the weapon under the wielder's `RpgCharacter` so its outgoing multiplier and identity apply.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `damage` | Base damage per hit, multiplied by the wielder's outgoing damage multiplier. |
| `hitLayers` | Layers that can be hit. |
| `targetTag` | Required tag of the hit collider; empty accepts any tag. |
| `Damage Type` (`DamageType`) | Damage type of every hit (`Fire`, `Cold`, ...), matched by the target's `DamageTypeResistPercent` buffs. Empty (the default) deals untyped damage, as before 12.0.0. |

## Damage pipeline

`DealDamage(collider)` finds the `RpgCharacter` in the collider's parents and calls
`RpgCharacter.ApplyDamage(new RpgDamageInfo(amount, DamageType, wielder))`. The wielder is the first
`IRpgCombatReceiver` in the weapon's parents: it scales `damage` by `GetOutgoingDamageMultiplier()` and is
passed as `RpgDamageInfo.Source`, so `IRpgDamageModifier` hooks (thorns, shields) and the target's
`DamageApplied` event see who hit. Colliders in the weapon's own root hierarchy are never valid targets.

Set `DamageType` from code (`weapon.DamageType = "Fire";`, null is stored as empty) to switch the element at
runtime, for example for an enchanted blade. Override `DealDamage` in a subclass for a custom receiver.

## See Also

- [AuraWeapon](AuraWeapon.md)
- [RpgCharacter damage pipeline](../../RpgCharacter.md#damage-pipeline)
- [Module Root](../../README.md)
