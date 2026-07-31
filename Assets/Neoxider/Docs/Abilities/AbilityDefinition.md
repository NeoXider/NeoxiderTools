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
| Targeting | **Range** | float | Max cast range in world units. `0` = unlimited. Only enforced when the world knows both positions. |
| Costs & cooldown | **Costs** | `List<AbilityCost>` | Resource costs paid on cast (id + amount). Mana costs honor `mana_cost_mul`. |
| Costs & cooldown | **Cooldown** | float | Seconds after cast. Reduced by `cooldown_reduction_percent` (capped at 90%). |
| Costs & cooldown | **Max Charges** | int | `0`/`1` = single cooldown; `>1` = charge system. |
| Costs & cooldown | **Charge Restore Time** | float | Seconds to restore one charge when `Max Charges > 1`. `0` = use Cooldown. |
| Delivery | **Delivery** | `AbilityDeliveryType` | `Instant` (impact runs immediately) or `Projectile` (impact runs on hit). |
| Delivery | **Projectile Archetype Id** | string | Spawn archetype id the host maps to a prefab (see [AbilitySystemBehaviour](./AbilitySystemBehaviour.md)). |
| Delivery | **Projectile Speed** | float | Speed hint passed to the spawned projectile (world units/second). |
| Effects | **Cast Effects** | `List<EffectNodeData>` | Nodes run immediately on cast, at the caster, before delivery. |
| Effects | **Impact Effects** | `List<EffectNodeData>` | Nodes run on impact — instantly for `Instant`, on hit for `Projectile`. |
| Presentation | **Cast Cue** / **Impact Cue** | string | Optional presentation-cue ids for VFX/SFX listeners. |

Effect node fields (`OpId`, `Target`, `TeamFilter`, `Radius`, `MaxTargets`, `Amount`, `DamageType`, `ModifierId`, `ResourceId`, `ArchetypeId`, `CustomParam`, `Chance`) plus the optional leveled-value fields (below) are described in the [module README](./README.md#effect-nodes).

## Key API

| Member | Description |
|--------|-------------|
| `AbilityBlueprint Blueprint` | The wrapped data. Register it with `AbilitySystem.RegisterAbility(...)`. |
| `string Id` | Convenience accessor for `Blueprint.Id`. |
| `bool AbilityBlueprint.UsesCharges` | `true` when `MaxCharges > 1`. |

Registration and casting happen through the system, not the asset:

```csharp
system.RegisterAbility(myAbility.Blueprint); // or myLibrary.RegisterInto(system)
system.GrantAbility(unitId, "fireball");
CastResult result = system.Cast(CastRequest.AtPoint(unitId, "fireball", worldPoint));
```

## Example — single-target firebolt

**Inspector:** Targeting = `Unit`, Team Filter = `Enemies`, Cooldown = `2`, one Cost = `{ mana, 25 }`. One **Impact Effect**: OpId = `damage`, Target = `Target`, Amount = `60`, Damage Type = `magical`.

**Code (grant + cast):**

```csharp
var caster = GetComponent<AbilityCasterBehaviour>();
// Ability already granted (via template, library, or the caster's own list).
caster.TryCastAtUnit("firebolt", enemyUnitBehaviour);
```

The same blueprint authored fully in code:

```csharp
var firebolt = new AbilityBlueprint
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

## Leveled values

Any effect `Amount` (and area `Radius`, and a modifier's `Duration`) can scale with **ability/unit level** and with a **caster/target property** — no code, no custom op. This is Dota's `AbilitySpecial`. All the fields are optional and default to "no effect", so a node that only sets a flat `Amount` is unchanged.

- **Per level:** set **Amount By Level** (e.g. `[50, 90, 140]`) and **Amount Level Source** = `AbilityLevel` (the casting slot's level), `CasterUnitLevel`, or `TargetUnitLevel`. Out-of-range levels clamp to the array ends.
- **Per property:** set **Amount Scale Property** (e.g. `spell_power`), **Amount Scale Per Point** (additive coefficient — `amount += perPoint * property`), and **Amount Scale Source** (`Caster` / `Target`).
- **Reuse:** define named leveled values under the ability's **Specials** and point several nodes at one via **Amount Key** (Dota `%value%` reuse).
- **Radius:** **Radius By Level** scales an area with ability level; modifiers add **Duration By Level**.

Raise an ability's level from code with `system.SetAbilityLevel(unitId, "firebolt", 2)` (clamped to ≥ 1); set a unit's level via a [UnitTemplate](./UnitTemplate.md) **Start Level**, a `Neo.Core.Level.LevelComponent` on the [AbilityUnitBehaviour](./AbilityUnitBehaviour.md), or `unit.SetLevel(...)`. A modifier captures the ability level **at apply time**, so its DoT/reaction values keep scaling at the level it was applied at.

```csharp
// A firebolt whose damage grows per ability level and with spell power.
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
system.SetAbilityLevel(mageId, "firebolt", 3);   // now deals 140 (+ scaling)
```

## Pitfalls

- **`NoTarget` / `Self` resolve the primary target to the caster.** A `damage` node with `Target = Target` then hits the caster. For self-cast area nukes use `AreaAroundCaster` (or `AreaAroundTarget`) with `Team Filter = Enemies`.
- **Range is only enforced when positions are known.** Headless (no world adapter) and units without a scene presence skip the range check.
- **`Projectile` delivery runs Impact Effects on hit, not on cast.** With no projectile prefab bound to the archetype id, the projectile never spawns and impact never fires. Bind the archetype in [AbilitySystemBehaviour](./AbilitySystemBehaviour.md).
- **The `damage` op scales by the caster's `spell_power`** (`amount * (1 + spell_power/100)`) in addition to the target's mitigation and `outgoing_damage_mul` — account for both when tuning numbers.
- **Motion ops need a scene presence.** `knockback` / `pull` / `teleport` displace units through the hub's `TryMoveUnit`; units without an [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) — or with the `unmovable` / `invulnerable` state — stay put.
- The asset only holds data. It does nothing until its blueprint is registered into a live `AbilitySystem`.

## See also

- [ModifierDefinition](./ModifierDefinition.md) — the durable effects abilities apply
- [AbilityLibrary](./AbilityLibrary.md) — register many abilities/modifiers at once
- [AbilityCasterBehaviour](./AbilityCasterBehaviour.md) — cast API
- Back: [Abilities module](./README.md)
