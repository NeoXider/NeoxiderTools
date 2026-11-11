# RpgAttackDefinition

**What it is:** a `ScriptableObject` (`Scripts/Rpg/Data/RpgAttackDefinition.cs`, namespace `Neo.Rpg`) that
describes a combat payload for [RpgAttackController](RpgAttackController.md): delivery, power, range, timing,
targets, projectile and effects.

**How to use:**
1. Create an asset via **Create > Neoxider > RPG > Rpg Attack Definition**.
2. Choose the delivery (`Direct`, `Area`, `Projectile`) and hit mode, then set power, range, cooldown and targets.
3. Add it to the `Attacks` list of an `RpgAttackController`, or wrap it in an [RpgAttackPreset](RpgAttackPreset.md).

**Navigation:** [← RPG](./README.md)

---

## Main fields

- `DeliveryType`, `HitMode`
- `Power`, `Range`, `Radius`
- `CastDelay`, `Cooldown`
- `TargetLayers`, `MaxTargets`
- `ProjectilePrefab`, `ProjectileSpeed`, `ProjectileLifetime`, `ProjectileMaxHits`
- `Effects` for self buffs, target buffs, and target statuses
