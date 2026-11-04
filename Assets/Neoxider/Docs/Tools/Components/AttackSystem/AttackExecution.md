# AttackExecution (legacy)

**What it is:** a legacy `MonoBehaviour` that runs attack cycles (wind-up, hit event, cooldown driven by attack speed). Kept for existing scenes and C# callers; source: `Scripts/Rpg/Combat/AttackExecution.cs`, optional assembly `Neo.Rpg.Combat`.

**How to use:**
1. Add **Neoxider / Tools / Components / AttackExecution (Legacy)** to an existing attacker.
2. Configure the wind-up delay and attack speed.
3. Call `Attack()` (returns `false` when busy or blocked) and react to `OnStartAttack` / `OnAttack` / `OnEndAttack`.

---

The complete contract (C# API, events, limits, scene setup) is the canonical
[RPG / Combat compatibility page](../../../Rpg/Combat/README.md#attackexecution), which is also the component's
Inspector help target. For new projects use [RpgAttackController](../../../Rpg/RpgAttackController.md) + [RpgAttackDefinition](../../../Rpg/RpgAttackDefinition.md).

## See also

- [AttackSystem](./README.md)
- [RPG / Combat compatibility](../../../Rpg/Combat/README.md)
