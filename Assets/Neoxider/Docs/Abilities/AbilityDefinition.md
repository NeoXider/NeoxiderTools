# AbilityDefinition

**What it is:** the ScriptableObject asset for one ability (`Scripts/Abilities/Data/AbilityDefinition.cs`, namespace `Neo.Abilities`). It wraps a pure-data `AbilityBlueprint` (`Domain/Casting/AbilityBlueprint.cs`): targeting, costs, cooldown or charges, delivery and the effect nodes that run on cast and on impact. This page is also the reference for effect nodes, built-in operations and leveled values.

**How to use:**
1. Create the asset with `Assets > Create > Neoxider > Abilities > Ability`.
2. Fill in the **Blueprint** fields and add effect nodes.
3. Register it through an [AbilityLibrary](./AbilityLibrary.md), a [UnitTemplate](./UnitTemplate.md) or an [AbilityCasterBehaviour](./AbilityCasterBehaviour.md).
4. Grant it to a unit and cast it by id.

---

## Fields (Inspector)

All fields live under the **Blueprint** foldout. A blank **Id** is filled from the asset name in `OnValidate`: lowercased, spaces to underscores.

| Section | Field | Type | Description |
|---------|-------|------|-------------|
| — | **Id** | string | Unique ability id, e.g. `fireball`. Used to grant and cast. |
| — | **Display Name** | string | Name for UI. |
| — | **Description** | string | Free text for UI and the Ability Designer. |
| Targeting | **Targeting** | `TargetingMode` | `NoTarget`, `Self`, `Unit`, `Point`, `Direction`. |
| Targeting | **Team Filter** | `AbilityTeamFilter` | Valid unit targets relative to the caster: `Any`, `Enemies`, `Allies`. |
| Targeting | **Range** | float | Maximum cast range in world units, plus the caster's `cast_range_bonus`, inclusive. `0` = unlimited. `Direction` reach: see [Cast rules](#cast-rules). |
| Targeting | **Range By Level** | `float[]` | Optional range per ability level. Empty = Range. |
| Costs & cooldown | **Costs** | `List<AbilityCost>` | Resource costs paid on cast: **Resource Id**, **Amount**, optional **Amount By Level** and **Amount Key**, **Mode** with **Min Amount** ([spend modes](#costs-and-spend-modes)). |
| Costs & cooldown | **Cooldown** | float | Seconds after cast, reduced by `cooldown_reduction_percent` (at most 90%). |
| Costs & cooldown | **Cooldown By Level** | `float[]` | Optional cooldown per ability level, e.g. `[10, 8, 6]`. Empty = Cooldown. |
| Costs & cooldown | **Max Charges** | int | `0` or `1` = single cooldown; `>1` = charges. |
| Costs & cooldown | **Max Charges By Level** | `int[]` | Optional charge cap per ability level. Empty = Max Charges. |
| Costs & cooldown | **Charge Restore Time** | float | Seconds to restore one charge when `Max Charges > 1`. `0` = use Cooldown. |
| Costs & cooldown | **Charge Restore Time By Level** | `float[]` | Optional restore time per ability level; a non-positive entry uses that level's cooldown. |
| Delivery | **Delivery** | `AbilityDeliveryType` | `Instant`: impact runs immediately. `Projectile`: impact runs on hit. |
| Delivery | **Projectile Archetype Id** | string | Spawn archetype the hub maps to a prefab ([AbilitySystemBehaviour](./AbilitySystemBehaviour.md)). |
| Delivery | **Projectile Speed** | float | Speed passed to the spawned projectile, world units per second. |
| Delivery | **Projectile Speed By Level** | `float[]` | Optional projectile speed per ability level. |
| Effects | **Cast Effects** | `List<EffectNodeData>` | Nodes run on cast, at the caster, before delivery. |
| Effects | **Impact Effects** | `List<EffectNodeData>` | Nodes run on impact: at once for `Instant`, on hit for `Projectile`. |
| Presentation | **Cast Cue** / **Impact Cue** | string | Optional cue ids for [AbilityCuePlayer](./AbilityCuePlayer.md) and other listeners. |

## Effect nodes

An `EffectNodeData` is one authored step. Abilities use nodes as cast and impact effects; modifiers use them as tick effects and reactions.

| Field | Purpose |
|-------|---------|
| `OpId` | Operation id, see [Built-in operations](#built-in-operations). Default `damage`. |
| `Target` | `Target` (the context's primary target; default), `Caster`, `AreaAroundTarget`, `AreaAroundCaster`, `ConeFromCaster` (uses `Angle`). |
| `TeamFilter` | For area selectors: `Any`, `Enemies` (default) or `Allies`, relative to the caster. |
| `Radius` | Area radius in world units. |
| `MaxTargets` | Cap on affected units: areas keep the nearest N, `chain` hops up to N. `0` = unlimited (`chain`: 4). |
| `Amount` | Damage, heal, resource change, spawn magnitude or motion distance. |
| `DamageType` | `physical` (armor), `magical` (resist, blocked by `magic_immune`; default), `pure` (ignores mitigation and shields) or custom. |
| `ModifierId` | For `apply_modifier` and `remove_modifier`. |
| `ResourceId` | For `resource_change`, e.g. `mana`. |
| `ArchetypeId` | For `spawn`: the hub maps the id to a prefab. |
| `CustomParam` | Free-form string for custom ops; `dispel` reads `"buffs"`. |
| `Chance` | Probability `[0..1]` that the node runs (default 1), rolled on the cast's deterministic generator. |
| `AbilityId` | Payload ability of `projectile`, `area_apply` and `zone`; the ability that `cast_ability` casts. |
| `Angle`, `Duration`, `Interval` | Cone angle in degrees; zone lifetime and pulse interval in seconds. |
| `AmountPerStack` | Added per extra stack of the modifier running the node (ticks, reactions). |

The scaling fields (`AmountByLevel`, `RadiusByLevel` and others) are in [Leveled and scalable values](#leveled-and-scalable-values). The payload ops, the cone selector and zones are explained in [TriggersAndHooks](./TriggersAndHooks.md#payload-ops-cone-selector-and-zones).

### Built-in operations

`AbilityEffectOps` ids, registered by `DefaultEffectOps`:

| Op id | Effect |
|-------|--------|
| `damage` | Damage through the full pipeline, scaled by the caster's `spell_power`: `amount × (1 + spell_power / 100)`. |
| `heal` | Restores health, honoring `healing_received_mul`. Never revives. |
| `apply_modifier` | Attaches the catalog modifier `ModifierId` to each target. |
| `remove_modifier` | Removes every instance of `ModifierId` from each target. |
| `dispel` | Removes dispellable debuffs, or buffs with `CustomParam = "buffs"`. |
| `resource_change` | Adds to or drains a resource pool (mana burn, energy gain). |
| `spawn` | Asks the world adapter to spawn an archetype at the targets or the point. |
| `knockback` | Pushes each target away from the caster, or from the target point when set, by `Amount` world units. |
| `pull` | Drags each target toward the caster by `Amount`, never past the caster. |
| `teleport` | `Target = Caster`: the caster blinks to the target point or first target. `Target = Target`: targets are pulled to the caster. `CustomParam = "swap"`: caster and first target trade places. |
| `execute` | Damage from the target's health: `Amount` is a fraction (0.1 = 10%) of the `"missing"`, `"max"` or `"current"` health named by `CustomParam`, else a flat amount. Uses the node's `DamageType`. |
| `chain` | Damages the first target, then hops to the nearest unhit unit within `Radius` (team-filtered), up to `MaxTargets` hits (0 = 4). Each hop multiplies damage by the falloff in `CustomParam` (e.g. `"0.85"`, default 1). Hop order is deterministic. |
| `modify_damage` | In a `before_deal_damage` or `before_take_damage` reaction: `CustomParam` `multiply` (default), `add`, `absorb` or `cancel` with `Amount`. |
| `reflect` | In a `before_take_damage` reaction: reflects `Amount` (a fraction) of the mitigated damage to the attacker as `DamageType`. |
| `projectile` | Launches a projectile per target carrying the impact effects of payload `AbilityId`; the host reports the hit. |
| `area_apply` | Applies `ModifierId` to every resolved unit and runs payload `AbilityId` once on them. |
| `zone` | Spawns a domain zone (an aura following the caster, or a formation at a point) that pulses `ModifierId` and payload `AbilityId` for `Duration`. |
| `cast_ability` | The caster casts its granted `AbilityId` through the normal cast pipeline (auto-cast passives, "on hit, cast X"). |

`knockback`, `pull` and `teleport` move units through the world adapter's `TryMoveUnit`. Units with the `unmovable` or `invulnerable` state, or without an [AbilityUnitBehaviour](./AbilityUnitBehaviour.md), stay put. All amounts, including motion distances and the `execute` fraction, can be leveled. To add your own op, see [Extensibility](./README.md#extensibility).

## Leveled and scalable values

Every important ability parameter can change with the **ability level**, a **unit level**, a **caster or target stat** and the **resource the cast actually spent** (Dota's `AbilitySpecial`). It is all data, with no custom op. Every field is optional: a node with default scaling fields resolves to exactly its flat `Amount`, on both cores. Resolution is pure and deterministic (`LeveledValueResolver`).

**Sampling rule for every `…ByLevel` array:** `value = array[clamp(level − 1, 0, length − 1)]`; an empty array uses the flat field. Cast parameters and costs use the casting slot's ability level. Node arrays use the context's ability level: the casting slot, or the level captured on a modifier for its ticks and reactions.

**Levels.** The ability level is `AbilitySlot.Level`; set it with `AbilitySystem.SetAbilityLevel(unit, abilityId, level)`, clamped to at least 1. A unit level is `AbilityUnit.Level`: set it with [UnitTemplate](./UnitTemplate.md) **Start Level**, a `Neo.Core.Level.LevelComponent` on the [AbilityUnitBehaviour](./AbilityUnitBehaviour.md), or `unit.SetLevel(...)`.

### What can scale

| Parameter | Where | Field(s) | Driven by |
|-----------|-------|----------|-----------|
| Cooldown | ability | `CooldownByLevel` | ability level; then `cooldown_reduction_percent` |
| Cast range | ability | `RangeByLevel` | ability level; then `+ cast_range_bonus` (range check, `Direction` reach, `AbilityAutoCaster`) |
| Charges | ability | `MaxChargesByLevel` | ability level |
| Charge restore time | ability | `ChargeRestoreTimeByLevel` (≤ 0 = the level's cooldown) | ability level; then cooldown reduction |
| Projectile speed | ability | `ProjectileSpeedByLevel` | ability level; also the payload speed of the `projectile` op |
| Cost amount | cost | `AmountByLevel`, or `AmountKey` naming a special | ability level, stats (e.g. % of max mana); then `mana_cost_mul` |
| How much a cost takes | cost | `Mode` (`Fixed`, `UpTo`, `All`), `MinAmount` | the resource available |
| Effect amount | node | `AmountByLevel`, `AmountScale*`, `AmountSpent*`, or `AmountKey` | any level, a caster or target stat, the spent resource, stacks (`AmountPerStack`) |
| Area radius | node | `RadiusByLevel` | ability level |
| Node chance | node | `ChanceByLevel` | ability level |
| Target cap | node | `MaxTargetsByLevel` | ability level |
| Zone lifetime and interval | node (`zone`) | `DurationByLevel`, `IntervalByLevel` | ability level |
| Modifier duration | modifier | `DurationByLevel` | level captured on the modifier |
| Modifier strength | modifier property | `PropertyContribution.ValueByLevel` ([details](./ModifierDefinition.md#strength-by-level)) | level captured on the modifier |
| Named values | ability | `Specials`: a list of `{ Name, LeveledValue }` | whatever the `LeveledValue` says |

Named specials let several nodes share one value through `AmountKey`, for example a damage node and a slow that both read `damage`.

### How a leveled value resolves

A `LeveledValue` (a special) and a node's inline amount fields carry the same terms:

| `LeveledValue` field | Node field | Meaning |
|----------------------|------------|---------|
| `Base` | `Amount` | The flat value. |
| `Levels` | `AmountByLevel` | Per-level values. |
| `PerLevel` | — | Linear term, used when `Levels` is empty. |
| `LevelFrom` | `AmountLevelSource` | `None` (level 1), `AbilityLevel`, `CasterUnitLevel`, `TargetUnitLevel`. |
| `SpentResource`, `SpentCoefficient` | `AmountSpentResource`, `AmountSpentCoefficient` | Spent term. |
| `ScaleProperty`, `ScalePerPoint` | `AmountScaleProperty`, `AmountScalePerPoint` | Scale term: the stat id and its coefficient. Empty id = no term. |
| `ScaleFrom` | `AmountScaleSource` | `Caster` (default) or `Target`. |
| `ScaleStat` | `AmountScaleStat` | `Property` (default), `ResourceCurrent`, `ResourceMax`, `ResourceMissing`; with a resource stat the id names a resource such as `mana`. |
| `ScaleMode` | `AmountScaleMode` | `Add` (default) or `Multiply`. |

A node with an `AmountKey` uses that special instead of its inline fields. The value resolves in this order:

1. **Level:** `LevelFrom` picks the entry of `Levels`; with no array, `Base + PerLevel × (level − 1)`.
2. **Spent term:** `value += SpentCoefficient × spent`, where `spent` is the total of that resource the cast paid.
3. **Scale term:** `Add`: `value += ScalePerPoint × stat`. `Multiply`: `value *= 1 + ScalePerPoint × stat`, applied last, so it also scales the level and spent terms.
4. **Stacks:** tick and reaction amounts add `AmountPerStack × (stacks − 1)`.

### Level changes at runtime

A new level takes effect from the next cast. A **running cooldown keeps its remaining time**; the next cast starts the new level's cooldown. A **raised charge cap grants the added charges at once**, and a running restore continues. A **lowered cap clamps** the available charges and stops the restore timer when nothing is left to restore. A modifier keeps the ability level it was applied with until it is applied again.

### Costs and spend modes

| Mode | Pays | The cast needs |
|------|------|----------------|
| `Fixed` (default) | exactly the resolved amount | the full amount |
| `UpTo` | `min(available, amount)` | at least `MinAmount` available |
| `All` | everything available; the amount is ignored | at least `MinAmount` available |

"Available" is the pool's current value minus what earlier cost entries of the same resource take. The amount is resolved at the slot level first, so `mana_cost_mul` still applies, and so does the pool's per-operation decrease limit (a cast exceeding it fails). What the cast paid after the cost rule and the mode is recorded as its **spent amounts**.

### Spent amounts travel with the cast

Spent amounts reach:

- the cast and impact effects, including a delayed projectile hit and every hit of a piercing projectile;
- the payloads of `area_apply` and of projectiles launched by the `projectile` op;
- the tick and reaction effects of modifiers that the cast applied.

A modifier captures them at apply time only when its own tick or reaction nodes use a spent term, directly or through a special. Re-application captures them again, like the level; a re-application without paid amounts, such as `ApplyModifier` from code, clears them. Read them with `EffectContext.GetSpent(id)` or `ModifierInstance.GetSpent(id)`. **Not carried:** zone pulses and the modifiers a zone applies, and casts started by `cast_ability`, which pay and record their own costs.

### Worked examples

- **Stun 1 / 1.5 / 2 s.** Modifier `bash_stun`: States `stunned`, Duration `1`, **Duration By Level** `[1, 1.5, 2]`. An `apply_modifier` node applies it; the duration follows the ability level at the moment of the hit.
- **Cooldown 10 / 8 / 6.** Cooldown `10`, **Cooldown By Level** `[10, 8, 6]`. `cooldown_reduction_percent` still applies on top.
- **Cost = 10% of max mana.** Cost `{ mana, Amount 0, Amount Key = mana_cost }` and a special `mana_cost` with Scale Property `mana`, **Scale Stat** `ResourceMax`, Scale Per Point `0.1`, Scale From `Caster`.
- **Spend all mana, damage = 2 × spent.** Cost `{ mana, Mode = All, Min Amount = 1 }` and an impact `damage` node with **Amount Spent Resource** `mana`, **Amount Spent Coefficient** `2`, Amount `0`. With projectile delivery the damage lands on the hit, using the mana spent at cast.
- **Slow 20 / 30 / 40%.** Modifier property `{ move_speed, Mul, Value 0.8, Value By Level = [0.8, 0.7, 0.6] }`.

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

Inline node fields: a firebolt dealing `[50, 90, 140]` by ability level plus `0.4 × spell_power`:

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

`AbilityContentValidator` reports:

- negative or non-finite level entries and chances outside `[0, 1]`;
- unknown cost keys and a spend mode without a resource id;
- a spent term without a coefficient, or the reverse, and a spent resource that no cost of the ability pays;
- `Scale Mode` or `Scale Stat` without a scale id.

The integer core and its converter support every field ([FixedCore](./FixedCore.md#leveled-and-scalable-values)).

## Cast rules

- **Costs are checked before any payment.** Repeated resource ids are summed (ids are case-insensitive) and `mana_cost_mul` applies: two `{ mana, 60 }` entries need 120 mana. The cast fails with `NotEnoughResources`, with no payment, cooldown or `ability_cast`, when the total is unaffordable or when a positive cost has an empty id. The same happens when an effective cost, the total or `mana_cost_mul` is NaN, infinite or overflows. Finite nonpositive costs are ignored.
- **Custom resource pools.** Resource ids and effective amounts are captured before payment. Each positive entry must pass `ResourcePoolModel.CanSpend`, including the pool's per-operation decrease cap; repeated ids must fit the balance together. Payment uses exact `TrySpend` on the captured entries. If a payment callback makes a later entry unpayable, the cast fails with no charges, cooldown or effects, but earlier payments stay spent: this is not a multi-resource transaction.
- **Cooldowns and charges.** Cooldown reduction applies to the cooldown and to every charge restore. `AbilitySlot.NormalizedCooldown` measures the effective timer: `1` right after a positive cooldown starts, `0` at completion. A large tick restores every elapsed charge interval and keeps the remainder. A zero effective restore time refills charges at once.
- **Readiness.** A charged slot can have charges left while the next one is still restoring. `IsReady` describes only the slot: it does not check death, states, target or resources. `Cast` checks those.
- **Direction casts.** The cast aims along the normalized `CastRequest.Direction`, or world forward when it is zero or not finite. With a positive **Range** it reaches `Range + cast_range_bonus` (at least 0), whatever the aim vector's length. With Range `0` it reaches the aim vector's length, but at least `AbilitySystem.DefaultDirectionReach` (10): `transform.forward` flies 10 units, while `target - origin` arrives at the target. The reach is computed once per cast, so the target point and a projectile's `SpawnRequest.Direction` end at the same place.
- **Range checks.** The range check, `QueryUnitsInRadius`, `AbilitySpatialGrid` and `AbilityAutoCaster` all use `AbilitySpatialGrid.IsWithinRadius`: inclusive, exact distance in double precision. A unit the auto-caster picks on the boundary, or far away where float squares overflow, is never rejected as `OutOfRange`. A negative effective range reaches nothing. Range is enforced only when the world knows both positions; headless casts and units without a scene presence skip it.
- **When the level is read.** Cost, range, cooldown and charges use the slot level before payment. Effects read it again after the `ability_cast` receipt, so a handler that levels the ability on cast scales that cast's effects.
- **Grants hold a reference.** A slot keeps the blueprint object it was granted; registering another blueprint under the same id changes only future grants ([runtime contracts](./README.md#runtime-contracts), [AbilityLibrary](./AbilityLibrary.md#pitfalls)).

## Key API

| Member | Description |
|--------|-------------|
| `AbilityBlueprint Blueprint` | The wrapped data. Register it with `AbilitySystem.RegisterAbility(...)`. |
| `string Id` | Shortcut for `Blueprint.Id`. |
| `bool AbilityBlueprint.UsesCharges` | `true` when the flat `MaxCharges > 1`. Level-aware: `UsesChargesAt(level)` or `AbilitySlot.UsesCharges`. |
| `ResolveRange(level)`, `ResolveCooldown(level)`, `ResolveMaxCharges(level)`, `ResolveChargeRestoreTime(level)`, `ResolveProjectileSpeed(level)` | Authored cast parameters at a level, before `cast_range_bonus` and the cooldown rule. `ResolveChargeRestoreTime` falls back to the level's cooldown. |
| `AbilitySlot.Level` / `MaxCharges` / `UsesCharges` / `Range` | The slot's level and its cast parameters at that level. |
| `AbilityCost.ResolveAmount(specials, system, caster, level, target)` | A cost's amount at a level, before `mana_cost_mul` and the spend mode. |
| `EffectContext.GetSpent(resourceId)` / `SpentCount` / `GetSpentAt(i)` / `AddSpent(id, amount)` | What the context's cast paid. Custom ops read it; hosts that run effects directly can record it. |

Registration and casting go through the system, not the asset:

```csharp
system.RegisterAbility(myAbility.Blueprint); // or myLibrary.RegisterInto(system)
system.GrantAbility(unitId, "fireball");
CastResult result = system.Cast(CastRequest.AtPoint(unitId, "fireball", worldPoint));
```

## Example — single-target firebolt

**Inspector:** Targeting `Unit`, Team Filter `Enemies`, Cooldown `2`, one Cost `{ mana, 25 }`. One **Impact Effect**: OpId `damage`, Target `Target`, Amount `60`, Damage Type `magical`.

```csharp
AbilityCasterBehaviour caster = GetComponent<AbilityCasterBehaviour>();
// Ability already granted (via template, library, or the caster's own list).
caster.TryCastAtUnit("firebolt", enemyUnitBehaviour);
```

The same blueprint written in code is in the [module README](./README.md#create-a-system-headless).

## Pitfalls

- **`NoTarget` and `Self` make the caster the primary target.** A `damage` node with `Target = Target` then hits the caster. For self-cast area nukes use `AreaAroundCaster` or `AreaAroundTarget` with `Team Filter = Enemies`.
- **`UpTo` and `All` with `Min Amount 0` can cast with nothing to spend**, and the spent term is then 0. Set a minimum when the ability must cost something.
- **`Projectile` delivery runs Impact Effects on hit, not on cast.** Without a prefab bound to the archetype id the projectile never spawns and the impact never fires. Bind it in [AbilitySystemBehaviour](./AbilitySystemBehaviour.md).
- **The asset only holds data.** It does nothing until its blueprint is registered into a live `AbilitySystem`.

## See also

- [ModifierDefinition](./ModifierDefinition.md) — the durable effects abilities apply
- [AbilityLibrary](./AbilityLibrary.md) — register many abilities and modifiers at once
- [AbilityCasterBehaviour](./AbilityCasterBehaviour.md) — cast API
- Back: [Abilities module](./README.md)
