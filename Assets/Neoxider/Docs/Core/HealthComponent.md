# Health Component

**What it is:** A MonoBehaviour that implements `IResourcePoolProvider`. Manages multiple resource pools by id: HP, Mana, and arbitrary ones (rage, energy). The primary scenario is health and mana; the same APIs and events apply to any pool. Path: `Scripts/Core/Resources/Components/HealthComponent.cs`, namespace `Neo.Core.Resources`.

**How to use:**
1. Add the component to a GameObject (Add Component → Neoxider/Core/Health Component).
2. In the pool list, define entries with an id (e.g. "HP", "Mana"), current/max, and optionally regen and per-operation limits.
3. From code, call GetCurrent(id), Decrease(id, amount), Increase(id, amount), TrySpend(id, amount, out reason). For skills with a resource cost — RpgAttackDefinition.CostResourceId / CostAmount.
4. `RpgCharacter` uses the same universal-resource approach and provides an RPG API on top of HP/Mana/Stamina/custom pools.
5. Events are configured **per pool entry** in the inspector: OnChanged(current, max), OnDepleted, and for HP — OnDamage, OnHeal, OnDeath, OnChangeMax. At the component level there is only the global **OnPoolsChanged** (when the pool list changes). For NeoCondition, use the HpCurrentValue, HpPercentValue, ManaCurrentValue, ManaPercentValue properties.

---

## Fields and Properties

| Name | Type | Purpose |
|-----|-----|------------|
| HpCurrentValue, HpPercentValue | float | Current HP and 0–1 ratio (for NeoCondition); read from the HP pool. |
| ManaCurrentValue, ManaPercentValue | float | Current mana and 0–1 ratio (for NeoCondition); read from the Mana pool. |

**Each pool entry** in the inspector has: **CurrentState**, **PercentState** (ReactivePropertyFloat) — the current value and 0–1 ratio; subscribe via `entry.CurrentState.OnChanged`, bind UI to the pool field. Other pool fields: id, current, max, regenPerSecond, regenInterval, maxDecreaseAmount, maxIncreaseAmount (-1 = no limit), restoreOnAwake, ignoreCanHeal, healAmount, healDelay. Entry events: **OnChanged** (current, max; depletion is detected via current <= 0), and for HP — **OnDamage**, **OnHeal**, **OnDeath**, **OnChangeMax**.

## Methods (IResourcePoolProvider)

| Signature | Returns | Description |
|-----------|---------|----------|
| GetCurrent(string resourceId) | float | Current pool value. |
| GetMax(string resourceId) | float | Pool maximum. |
| TrySpend(string resourceId, float amount, out string failReason) | bool | Check and deduct; false if insufficient, reason — the cause. |
| Decrease(string resourceId, float amount) | float | Decrease (damage, etc.); returns the amount actually removed. |
| Increase(string resourceId, float amount) | float | Increase (healing, etc.); returns the amount actually added. |
| IsDepleted(string resourceId) | bool | true if current ≤ 0. |
| Restore(string resourceId) | void | Fill the pool to maximum. |
| SetMax(string resourceId, float max) | void | Set the pool maximum. |
| SetMaxHp(float max) | void | Set max HP (convenience alias). |

Id constants: `RpgResourceId.Hp`, `RpgResourceId.Mana`.

## Events (UnityEvent)

**Per pool entry (ResourceEntryInspector):** ReactivePropertyFloat **CurrentState**, **PercentState** (current value and 0–1 ratio). Events: OnChanged(current, max) — depletion via current <= 0; for HP — OnDamage(actual), OnHeal(actual), OnDeath, OnChangeMax(max).

**On the component only (global):** OnPoolsChanged — when the pool list is rebuilt (init, pool set changes).

## Examples

```csharp
IResourcePoolProvider res = go.GetComponent<HealthComponent>();
res.Decrease(RpgResourceId.Hp, 15f);
if (res.TrySpend(RpgResourceId.Mana, 25f, out string reason))
    CastSpell();
```

Using as Mana: add a pool with id = "Mana", set current/max and optionally regen. Skills specify CostResourceId = "Mana", CostAmount in RpgAttackDefinition.

## See Also

- [Level.md](./Level.md) — level and XP.
- [Rpg/README.md](../Rpg/README.md) — RpgCharacter, resources, attack costs.
