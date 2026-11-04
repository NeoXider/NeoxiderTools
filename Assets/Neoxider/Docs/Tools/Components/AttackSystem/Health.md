# Health (legacy)

**What it is:** a legacy `MonoBehaviour` health component (`Neo.Tools.Health`) implementing `IDamageable`, `IHealable` and `IRestorable`, with reactive HP, per-hit caps, auto-heal and death events. Kept for existing scenes and C# callers; source: `Scripts/Rpg/Combat/Health.cs`, optional assembly `Neo.Rpg.Combat`.

**How to use:**
1. Add **Neoxider / Tools / Components / AttackSystem / Health (Legacy)** to an existing actor.
2. Set **Max Hp** and the heal/damage limits in the Inspector.
3. Call `TakeDamage`, `Heal`, `Restore`, `SetHp`, `SetMaxHp` from code or UnityEvents; subscribe to `Hp` or `OnDeath`.

---

The complete contract (C# API, events, limits, scene setup) is the canonical
[RPG / Combat compatibility page](../../../Rpg/Combat/README.md#health), which is also the component's
Inspector help target. For new projects use [RpgCharacter](../../../Rpg/RpgCharacter.md).

## See also

- [AttackSystem](./README.md)
- [RPG / Combat compatibility](../../../Rpg/Combat/README.md)
