# AbilityDefinition

**What it is:** the ScriptableObject authoring asset for one ability. It wraps a pure-data `AbilityBlueprint` (targeting, costs, cooldown/charges, delivery, and the effect nodes that run on cast and on impact). Everything an ability needs is configured here — no code.

**How to use:** create the asset via `Assets > Create > Neoxider > Abilities > Ability`, fill in the **Blueprint** fields, then register it (add it to an [AbilityLibrary](./AbilityLibrary.md), a [UnitTemplate](./UnitTemplate.md), or an [AbilityCasterBehaviour](./AbilityCasterBehaviour.md)) and grant it to a unit. Cast it by id.

- Namespace: `Neo.Abilities`
- File: `Assets/Neoxider/Scripts/Abilities/Data/AbilityDefinition.cs`
- Backing data: `AbilityBlueprint` (`Assets/Neoxider/Scripts/Abilities/Domain/Casting/AbilityBlueprint.cs`)

## Fields (Inspector)

All fields live under the nested **Blueprint** foldout. If **Id** is left blank, `OnValidate` fills it from the asset name (lowercased, spaces to underscores).

| Section | Field | Type | Description |
|---------|-------|------|-------------|
| — | **Id** | string | Unique ability id, e.g. `fireball`. Used to grant and cast. |
| — | **Display Name** | string | Name for UI. |
| — | **Description** | string | Free text for UI / the Ability Designer. |
| Targeting | **Targeting** | `TargetingMode` | How the target is acquired: `NoTarget`, `Self`, `Unit`, `Point`, `Direction`. |
| Targeting | **Team Filter** | `AbilityTeamFilter` | Valid unit targets relative to the caster: `Any` / `Enemies` / `Allies`. |
| Targeting | **Range** | float | Max cast range in world units, plus the caster's `cast_range_bonus` (inclusive). `0` = unlimited. Only enforced when the world knows both positions. A `Direction` cast reaches exactly `Range + cast_range_bonus`; with `0` it reaches the aim vector's length, at least 10. |
| Targeting | **Range By Level** | `float[]` | Optional range per ability level (index = level − 1, clamped). Empty = Range. Used by the range check, the `Direction` reach and `AbilityAutoCaster`. |
| Costs & cooldown | **Costs** | `List<AbilityCost>` | Resource costs paid on cast. Each cost: **Resource Id**, **Amount**, optional **Amount By Level**, **Amount Key** (a named special) and **Mode** `Fixed` / `UpTo` / `All` with **Min Amount** ([costs](#costs-and-spend-modes)). Repeated ids are summed for affordability; mana costs honor `mana_cost_mul`. |
| Costs & cooldown | **Cooldown** | float | Seconds after cast. Reduced by `cooldown_reduction_percent` (capped at 90%). |
| Costs & cooldown | **Cooldown By Level** | `float[]` | Optional cooldown per ability level, e.g. `[10, 8, 6]`. Empty = Cooldown. |
| Costs & cooldown | **Max Charges** | int | `0`/`1` = single cooldown; `>1` = charge system. |
| Costs & cooldown | **Max Charges By Level** | `int[]` | Optional charge cap per ability level. Empty = Max Charges. |
| Costs & cooldown | **Charge Restore Time** | float | Seconds to restore one charge when `Max Charges > 1`. `0` = use Cooldown. |
| Costs & cooldown | **Charge Restore Time By Level** | `float[]` | Optional restore time per ability level; a non-positive entry uses that level's cooldown. |
| Delivery | **Delivery** | `AbilityDeliveryType` | `Instant` (impact runs immediately) or `Projectile` (impact runs on hit). |
| Delivery | **Projectile Archetype Id** | string | Spawn archetype id the host maps to a prefab (see [AbilitySystemBehaviour](./AbilitySystemBehaviour.md)). |
| Delivery | **Projectile Speed** | float | Speed hint passed to the spawned projectile (world units/second). |
| Delivery | **Projectile Speed By Level** | `float[]` | Optional projectile speed per ability level. Empty = Projectile Speed. |
| Effects | **Cast Effects** | `List<EffectNodeData>` | Nodes run immediately on cast, at the caster, before delivery. |
| Effects | **Impact Effects** | `List<EffectNodeData>` | Nodes run on impact — instantly for `Instant`, on hit for `Projectile`. |
| Presentation | **Cast Cue** / **Impact Cue** | string | Optional presentation-cue ids for VFX/SFX listeners. |

Effect node fields (`OpId`, `Target`, `TeamFilter`, `Radius`, `MaxTargets`, `Amount`, `DamageType`, `ModifierId`, `ResourceId`, `ArchetypeId`, `CustomParam`, `Chance`) plus the optional leveled and scalable fields ([below](#leveled-and-scalable-values)) are described in the [module README](./README.md#effect-nodes). The payload, cone, zone and stack fields (`AbilityId`, `Angle`, `Duration`, `Interval`, `AmountPerStack`) and the `ConeFromCaster` selector are described in [Triggers, damage hooks, zones and templates](./TriggersAndHooks.md#payload-ops-cone-selector-and-zones).

## Key API

| Member | Description |
|--------|-------------|
| `AbilityBlueprint Blueprint` | The wrapped data. Register it with `AbilitySystem.RegisterAbility(...)`. |
| `string Id` | Convenience accessor for `Blueprint.Id`. |
| `bool AbilityBlueprint.UsesCharges` | `true` when the flat `MaxCharges > 1`. Level-aware: `UsesChargesAt(level)` or `AbilitySlot.UsesCharges`. |
| `ResolveRange(level)`, `ResolveCooldown(level)`, `ResolveMaxCharges(level)`, `ResolveChargeRestoreTime(level)`, `ResolveProjectileSpeed(level)` | The authored cast parameters at an ability level (before `cast_range_bonus` and the cooldown rule). `ResolveChargeRestoreTime` falls back to the level's cooldown. |
| `AbilitySlot.Level` / `MaxCharges` / `UsesCharges` / `Range` | The slot's level and its cast parameters at that level. |
| `AbilityCost.ResolveAmount(specials, system, caster, level, target)` | A cost's authored amount at a level, before `mana_cost_mul` and the spend mode. |
| `EffectContext.GetSpent(resourceId)` / `SpentCount` / `GetSpentAt(i)` / `AddSpent(id, amount)` | What the cast of a context paid; custom ops read it, hosts running effects directly can record it. |

Registration and casting happen through the system, not the asset:

```csharp
system.RegisterAbility(myAbility.Blueprint); // or myLibrary.RegisterInto(system)
system.GrantAbility(unitId, "fireball");
CastResult result = system.Cast(CastRequest.AtPoint(unitId, "fireball", worldPoint));
```

The slot holds the blueprint object reference it was granted. Registering another blueprint under the same id changes future grants; existing slots keep their original targeting, effects, costs and timing. Repeated `GrantAbility` is idempotent. Explicit revoke/regrant selects the replacement and resets the slot state; see [AbilityLibrary](./AbilityLibrary.md#pitfalls). The held object remains mutable, with no deep copy or immutable-data guarantee.

## Example — single-target firebolt

**Inspector:** Targeting = `Unit`, Team Filter = `Enemies`, Cooldown = `2`, one Cost = `{ mana, 25 }`. One **Impact Effect**: OpId = `damage`, Target = `Target`, Amount = `60`, Damage Type = `magical`.

**Code (grant + cast):**

```csharp
AbilityCasterBehaviour caster = GetComponent<AbilityCasterBehaviour>();
// Ability already granted (via template, library, or the caster's own list).
caster.TryCastAtUnit("firebolt", enemyUnitBehaviour);
```

The same blueprint authored fully in code:

```csharp
AbilityBlueprint firebolt = new AbilityBlueprint
{
    Id = "firebolt",
    Targeting = TargetingMode.Unit,
    TeamFilter = AbilityTeamFilter.Enemies,
    Cooldown = 2f,
    Costs = { new AbilityCost(AbilityResourceIds.Mana, 25f) }
};
firebolt.ImpactEffects.Add(new EffectNodeData
{
    OpId = AbilityEffectOps.Damage,
    Target = EffectTargetSelector.Target,
    Amount = 60f,
    DamageType = AbilityDamageTypes.Magical
});
system.RegisterAbility(firebolt);
```

## Leveled and scalable values

Every important ability parameter can change with the **ability level**, a **unit level**, a **caster or target stat** and the **resource the cast actually spent**, in data, without code or a custom op (Dota's `AbilitySpecial`). Every field is optional and defaults to "off": content that does not use them resolves exactly as before, on both cores.

**Sampling rule (all `…ByLevel` arrays):** `value = array[clamp(level − 1, 0, length − 1)]`; an empty array uses the flat field. Cast parameters and cost arrays are indexed by the casting slot's ability level; node arrays by the context's ability level (the casting slot, or the level captured on a modifier for its ticks and reactions).

### What can scale

| Parameter | Where | Field(s) | Driven by |
|-----------|-------|----------|-----------|
| Cooldown | ability | `CooldownByLevel` | ability level; then `cooldown_reduction_percent` |
| Cast range | ability | `RangeByLevel` | ability level; then `+ cast_range_bonus` (range check, `Direction` reach, `AbilityAutoCaster`) |
| Charges | ability | `MaxChargesByLevel` | ability level |
| Charge restore time | ability | `ChargeRestoreTimeByLevel` (≤ 0 ⇒ the level's cooldown) | ability level; then cooldown reduction |
| Projectile speed | ability | `ProjectileSpeedByLevel` | ability level (also the payload speed of the `projectile` op) |
| Cost amount | cost | `AmountByLevel`, or `AmountKey` → a special (a full `LeveledValue`) | ability level, stats (e.g. % of max mana); then `mana_cost_mul` |
| How much a cost takes | cost | `Mode` = `Fixed` / `UpTo` / `All`, `MinAmount` | the resource available |
| Effect amount | node | `AmountByLevel` + `AmountLevelSource`, `AmountScale*`, `AmountSpent*`, or `AmountKey` → a special | any level, a property or resource of caster / target, the spent resource, stacks (`AmountPerStack`) |
| Area radius | node | `RadiusByLevel` | ability level |
| Node chance | node | `ChanceByLevel` | ability level |
| Target cap | node | `MaxTargetsByLevel` (areas keep the nearest N, `chain` hops N) | ability level |
| Zone lifetime / pulse interval | node (`zone` op) | `DurationByLevel`, `IntervalByLevel` | ability level |
| Modifier duration | modifier | `DurationByLevel` | level captured on the modifier |
| Modifier strength | modifier property | `PropertyContribution.ValueByLevel` (+ `PerStackValue` per extra stack) | level captured on the modifier |
| Named values | ability | `Specials` (`{ Name, LeveledValue }`) | whatever the `LeveledValue` says |

### How a leveled value resolves

A `LeveledValue` (a special, or a node's inline `Amount*` fields) resolves in this order:

1. **Level:** `LevelFrom` (`None` ⇒ 1, `AbilityLevel`, `CasterUnitLevel`, `TargetUnitLevel`) picks the entry of `Levels`, else `Base + PerLevel × (level − 1)`.
2. **Spent term:** when `SpentResource` (node: `AmountSpentResource`) is set, `value += SpentCoefficient × spent`, where `spent` is the total of that resource the cast paid.
3. **Scale term:** when `ScaleProperty` (node: `AmountScaleProperty`) is set, the stat is read from the caster or the target (`ScaleFrom`). `ScaleStat` chooses what is read: `Property` (default), `ResourceCurrent`, `ResourceMax` or `ResourceMissing` (the id then names a resource, e.g. `mana`, `health`). `ScaleMode` `Add` (default): `value += ScalePerPoint × stat`; `Multiply`: `value *= 1 + ScalePerPoint × stat`, applied last, so it scales the level and spent terms too.
4. **Stacks:** tick and reaction amounts add `AmountPerStack × (stacks − 1)`.

### Level changes at runtime

`system.SetAbilityLevel(unit, id, level)` (or `AbilitySlot.Level`) takes effect from the next cast. A **running cooldown keeps its remaining time** (it is not rescaled); the next cast starts the new level's cooldown. A **raised charge cap grants the added charges at once** (the new slots arrive full; a running restore keeps going); a **lowered cap clamps** the available charges and stops the restore timer when nothing is left to restore. Modifiers keep the level they were applied at until they are re-applied.

### Costs and spend modes

| Mode | Pays | The cast needs |
|------|------|----------------|
| `Fixed` (default) | exactly the resolved amount | the full amount (unchanged rules) |
| `UpTo` | `min(available, amount)` | at least `MinAmount` available |
| `All` | everything available (the amount is ignored) | at least `MinAmount` available |

"Available" is the pool's current value minus what earlier cost entries of the same resource take. The amount is resolved at the slot level before the cost rule, so `mana_cost_mul` still applies; the pool's per-operation decrease limit still applies (a cast exceeding it fails). What a cast paid, after the rule and the mode, is recorded as its **spent amounts**.

### Spent amounts travel with the cast

The spent amounts reach the cast effects, the impact effects (instant, or a delayed projectile hit: every hit of a piercing projectile), the payloads of `area_apply` and of projectiles launched by the `projectile` op, and the **tick and reaction effects of modifiers that cast applied**. A modifier captures them at apply time only when its own tick or reaction nodes use a spent term (directly or through a special); a re-application re-captures them like the level, and a re-application without paid amounts (e.g. `ApplyModifier` from code) clears them. Read them from code with `EffectContext.GetSpent(id)` or `ModifierInstance.GetSpent(id)`. **Not carried:** zone pulses (and the modifiers a zone applies), and casts started by `cast_ability` (they pay and record their own costs).

### Worked examples

**Stun 1 / 1.5 / 2 s by level.** Modifier `bash_stun`: States = `stunned`, Duration = `1`, **Duration By Level** = `[1, 1.5, 2]`. The ability applies it with `apply_modifier`; the duration follows the ability level at the moment of the hit.

**Cooldown 10 / 8 / 6.** Ability: Cooldown = `10`, **Cooldown By Level** = `[10, 8, 6]`. `cooldown_reduction_percent` still applies on top.

**Cost = 10% of max mana.** Cost `{ mana, Amount 0, Amount Key = mana_cost }` and a special `mana_cost` with Scale Property = `mana`, **Scale Stat** = `ResourceMax`, Scale Per Point = `0.1`, Scale From = `Caster`.

**"Spend all mana, damage = 2 × spent".** Cost `{ mana, Mode = All, Min Amount = 1 }`; an impact `damage` node with **Amount Spent Resource** = `mana`, **Amount Spent Coefficient** = `2` and Amount `0`. With a projectile delivery the damage lands on the hit, with the mana spent at cast.

**Slow 20 / 30 / 40%.** Modifier property `{ move_speed, Mul, Value 0.8, Value By Level = [0.8, 0.7, 0.6] }`; see [ModifierDefinition](./ModifierDefinition.md#strength-by-level).

```csharp
AbilityBlueprint burst = new AbilityBlueprint
{
    Id = "mana_burst",
    Targeting = TargetingMode.Unit,
    CooldownByLevel = new[] { 10f, 8f, 6f },
    RangeByLevel = new[] { 6f, 7f, 8f },
    Costs = { new AbilityCost(AbilityResourceIds.Mana, 0f, AbilityCostMode.All, minAmount: 1f) }
};
burst.ImpactEffects.Add(new EffectNodeData
{
    OpId = AbilityEffectOps.Damage,
    DamageType = AbilityDamageTypes.Magical,
    AmountSpentResource = AbilityResourceIds.Mana,
    AmountSpentCoefficient = 2f,                       // damage = 2 × the mana actually spent
    AmountScaleProperty = "focus",
    AmountScalePerPoint = 0.01f,
    AmountScaleMode = ScaleAmountMode.Multiply          // × (1 + 1% per point of focus)
});

AbilityBlueprint nova = new AbilityBlueprint { Id = "frost_nova", Cooldown = 12f };
nova.Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 0f) { AmountKey = "mana_cost" });
nova.Specials.Add(new AbilitySpecialValue
{
    Name = "mana_cost",                                // 10% of the caster's max mana
    Value = new LeveledValue
    {
        ScaleProperty = AbilityResourceIds.Mana,
        ScaleStat = ScaleAmountStat.ResourceMax,
        ScalePerPoint = 0.1f
    }
});
system.SetAbilityLevel(mageId, "mana_burst", 3);      // 6 s cooldown and range 8 from the next cast
```

The inline form from earlier releases still works: a firebolt dealing `[50, 90, 140]` per ability level plus `0.4 × spell_power`:

```csharp
firebolt.ImpactEffects.Add(new EffectNodeData
{
    OpId = AbilityEffectOps.Damage,
    Target = EffectTargetSelector.Target,
    DamageType = AbilityDamageTypes.Magical,
    AmountByLevel = new[] { 50f, 90f, 140f },
    AmountLevelSource = LevelSource.AbilityLevel,
    AmountScaleProperty = AbilityProperties.SpellPower,
    AmountScalePerPoint = 0.4f,          // +0.4 damage per point of spell_power
    AmountScaleSource = ScaleAmountSource.Caster
});
```

Set a unit's level via a [UnitTemplate](./UnitTemplate.md) **Start Level**, a `Neo.Core.Level.LevelComponent` on the [AbilityUnitBehaviour](./AbilityUnitBehaviour.md), or `unit.SetLevel(...)`. `AbilityContentValidator` reports negative or non-finite level entries, chances outside `[0, 1]`, unknown cost keys, a spend mode without a resource id, a spent term without a coefficient (or the reverse), a spent resource that no cost of the ability pays, and `Scale Mode` / `Scale Stat` without a scale id. The integer core and the converter support every field ([FixedCore](./FixedCore.md#leveled-and-scalable-values)).

## Pitfalls

- **`NoTarget` / `Self` resolve the primary target to the caster.** A `damage` node with `Target = Target` then hits the caster. For self-cast area nukes use `AreaAroundCaster` (or `AreaAroundTarget`) with `Team Filter = Enemies`.
- **Range is only enforced when positions are known.** Headless (no world adapter) and units without a scene presence skip the range check.
- **Costs are captured and validated before payment.** Two `{ mana, 60 }` entries require 120 mana, with case-insensitive ids. Every authored entry must fit the pool's individual decrease cap; the sum must fit its balance. NaN, infinity, overflow in effective costs/totals, a nonfinite `mana_cost_mul`, and positive costs with an empty id are rejected with `NotEnoughResources` before payment or cooldown change. Finite nonpositive costs are ignored. Payment uses the captured resource ids/effective amounts and exact `TrySpend`. If a payment callback makes a later entry unpayable, the cast fails; earlier completed payments remain spent. See [runtime contracts](./README.md#runtime-contracts).
- **Charge timers use cooldown reduction for each restored charge.** Large steps can restore several charges; a zero effective restore duration refills immediately. `AbilitySlot.NormalizedCooldown` measures the effective timer after reduction, while `IsReady` can already be true if another charge remains.
- **The level is read when the cast starts.** Cost, range, cooldown and charges use the slot level before payment; the effects read it again after the `ability_cast` receipt, so a handler that levels the ability on cast scales that cast's effects.
- **`UpTo` / `All` with `Min Amount 0` may cast with nothing to spend** (and a spent term of 0). Set a minimum when the ability must cost something.
- **`Projectile` delivery runs Impact Effects on hit, not on cast.** With no projectile prefab bound to the archetype id, the projectile never spawns and impact never fires. Bind the archetype in [AbilitySystemBehaviour](./AbilitySystemBehaviour.md).
- **The `damage` op scales by the caster's `spell_power`** (`amount * (1 + spell_power/100)`) in addition to the target's mitigation and `outgoing_damage_mul` — account for both when tuning numbers.
- **Motion ops need a scene presence.** `knockback` / `pull` / `teleport` displace units through the hub's `TryMoveUnit`; units without an [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) — or with the `unmovable` / `invulnerable` state — stay put.
- The asset only holds data. It does nothing until its blueprint is registered into a live `AbilitySystem`.

## See also

- [ModifierDefinition](./ModifierDefinition.md) — the durable effects abilities apply
- [AbilityLibrary](./AbilityLibrary.md) — register many abilities/modifiers at once
- [AbilityCasterBehaviour](./AbilityCasterBehaviour.md) — cast API
- Back: [Abilities module](./README.md)
