# AdvancedAttackCollider (legacy)

**What it is:** a legacy hitbox `MonoBehaviour` that, while activated, damages (or heals) `IDamageable` / `IHealable` objects it touches, with layer filters, repeat-hit suppression, knockback and effects. Kept for existing scenes and C# callers; source: `Scripts/Rpg/Combat/AdvancedAttackCollider.cs`, optional assembly `Neo.Rpg.Combat`.

**How to use:**
1. Add **Neoxider / Tools / Components / AdvancedAttackCollider (Legacy)** to a hitbox or projectile with a trigger collider.
2. Set the attack damage and filters, then call `ActivateTrigger()` for each swing.
3. Give targets an `IDamageable` (for example `Health` or [RpgStatsDamageableBridge](./RpgStatsDamageableBridge.md)).

---

The complete contract (C# API, events, limits, scene setup) is the canonical
[RPG / Combat compatibility page](../../../Rpg/Combat/README.md#advancedattackcollider), which is also the component's
Inspector help target. For new projects use [RpgAttackController](../../../Rpg/RpgAttackController.md) + [RpgProjectile](../../../Rpg/RpgProjectile.md).

## See also

- [AttackSystem](./README.md)
- [RPG / Combat compatibility](../../../Rpg/Combat/README.md)
