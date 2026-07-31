# ModifierDefinition

**What it is:** the ScriptableObject authoring asset for one modifier — the single durable-effect concept covering buffs, debuffs, DoTs, aura payloads, shields, and stuns. A modifier contributes properties, declares boolean states, ticks effects on an interval, and reacts to gameplay events declaratively. It wraps a pure-data `ModifierBlueprint`.

**How to use:** create the asset via `Assets > Create > Neoxider > Abilities > Modifier`, configure the **Blueprint**, register it (usually through an [AbilityLibrary](./AbilityLibrary.md)), and reference it by **Id** from an ability's effect node (`apply_modifier`) or apply it directly with `AbilitySystem.ApplyModifier(...)`.

- Namespace: `Neo.Abilities`
- File: `Assets/Neoxider/Scripts/Abilities/Data/ModifierDefinition.cs`
- Backing data: `ModifierBlueprint` (`Assets/Neoxider/Scripts/Abilities/Domain/Modifiers/ModifierBlueprint.cs`)

## Fields (Inspector)

All fields live under the nested **Blueprint** foldout. A blank **Id** is filled from the asset name in `OnValidate`.

| Field | Type | Description |
|-------|------|-------------|
| **Id** | string | Unique modifier id, e.g. `burn`, `frost_slow`. Used for stacking, dispel, and `apply_modifier`. |
| **Display Name** | string | Name for UI. |
| **Duration** | float | Seconds until expiry. `0` or negative = permanent until removed. |
| **Stack Policy** | `ModifierStackPolicy` | Re-application behavior: `Independent`, `Refresh`, `Stack`. See below. |
| **Max Stacks** | int | Cap for the `Stack` policy. Ignored otherwise. |
| **Properties** | `List<PropertyContribution>` | Property contributions while active (per-stack scaling supported). |
| **States** | `List<string>` | Boolean states granted while active (any-true-wins), e.g. `stunned`, `frozen`. |
| **Tick Interval** | float | Seconds between effect ticks. `0` or negative = no ticking. |
| **Tick On Apply** | bool | If true, the first tick fires immediately on application. |
| **Tick Effects** | `List<EffectNodeData>` | Nodes run every tick (e.g. periodic damage for a DoT). |
| **Event Reactions** | `List<ModifierEventReaction>` | Declarative reactions: on an event id, run an effect node list. |
| **Is Debuff** | bool | Marks the modifier negative for UI and dispel logic. |
| **Dispellable** | bool | Can be removed by the `dispel` op. Default `true`. |
| **Presentation Cue** | string | Optional cue id for VFX/SFX listeners. |

### Stack policies

| Policy | Behavior |
|--------|----------|
| `Independent` | Every application creates a separate instance with its own duration. |
| `Refresh` (default) | One instance per unit; re-applying refreshes its duration. |
| `Stack` | One instance per unit; re-applying adds a stack (up to **Max Stacks**) and refreshes duration. Per-stack property scaling uses `PropertyContribution.PerStackValue`. |

### Property contributions

Each entry is `{ PropertyId, Op, Value, PerStackValue }`. Aggregation across all of a unit's modifiers is fixed and deterministic: `final = max((base + sum(Add)) * product(Mul), all Max floors)`.

| `Op` | Meaning | Example |
|------|---------|---------|
| `Add` | Flat additive bonus. | `{ attack_damage, Add, 20 }` |
| `Mul` | Multiplier after all Adds. `1` = no change. | `{ move_speed, Mul, 0.4 }` = 60% slow |
| `Max` | Raises the final value to at least this floor. | `{ move_speed, Max, 1 }` |

Well-known property ids are in `AbilityProperties` (`move_speed`, `attack_damage`, `armor`, `spell_power`, `crit_chance`, `crit_multiplier`, `evasion_chance`, `lifesteal_percent`, `incoming_damage_mul`, `shield_hp`, …); the registry is open, so any string works. See [combat properties](./README.md#combat-properties-crit--evasion--lifesteal) for how the damage pipeline reads the crit / evasion / lifesteal ones.

### Event reactions

`{ EventId, Effects, TargetEventSource }`. When `EventId` (e.g. `take_damage`, from `AbilityEvents`) fires on the owning unit, `Effects` run through the normal pipeline. Reaction caster = the modifier's caster; target = the modifier owner, or the **event source** (e.g. the attacker) when `Target Event Source` is set. Nested reactions are depth-capped at 3 to prevent loops.

## Key API

| Member | Description |
|--------|-------------|
| `ModifierBlueprint Blueprint` | The wrapped data. Register with `AbilitySystem.RegisterModifier(...)`. |
| `string Id` | Convenience accessor for `Blueprint.Id`. |
| `bool ModifierBlueprint.IsPermanent` | `true` when `Duration <= 0`. |
| `bool ModifierBlueprint.HasTicks` | `true` when it has a positive tick interval and tick effects. |

Apply from code (parallel to the `apply_modifier` op):

```csharp
ModifierApplyResult r = system.ApplyModifier("burn", casterId, targetId, "fireball");
// r.Succeeded, r.CreatedNew, r.Instance.Stacks, r.Instance.RemainingDuration
```

## Example — burn DoT

**Inspector:** Duration = `4`, Stack Policy = `Refresh`, Tick Interval = `1`, Is Debuff = on. One **Tick Effect**: OpId = `damage`, Target = `Target`, Amount = `15`, Damage Type = `magical`.

An ability then attaches it with an Impact Effect: OpId = `apply_modifier`, Target = `AreaAroundTarget`, Team Filter = `Enemies`, Radius = `3`, Modifier Id = `burn`.

Same burn authored in code:

```csharp
var burn = new ModifierBlueprint
{
    Id = "burn",
    Duration = 4f,
    StackPolicy = ModifierStackPolicy.Refresh,
    TickInterval = 1f,
    IsDebuff = true
};
burn.TickEffects.Add(new EffectNodeData
{
    OpId = AbilityEffectOps.Damage,
    Target = EffectTargetSelector.Target,
    Amount = 15f,
    DamageType = AbilityDamageTypes.Magical
});
system.RegisterModifier(burn);
```

## Pitfalls

- **Ticks only advance while the system ticks.** DoT damage needs `AbilitySystem.Tick(dt)` (the scene hub does this each frame; headless callers must call it).
- **Tick effect targeting:** during a tick the primary target is the owner and the context caster is the modifier's caster — so `Target = Target` hits the owner and kill credit goes to the caster.
- **Shields are `shield_hp` contributions, not a state.** The damage pipeline absorbs against every `shield_hp` pool automatically and fires `shield_absorbed` / `shield_broken`; pure damage bypasses shields. See the [Magic Shield example](./README.md#magic-shield).
- **`Max` floors are applied last and take the single highest floor**, not a sum — two `{ x, Max, 1 }` and `{ x, Max, 2 }` give a floor of `2`.
- **`States` do not by themselves stop movement or attacks** beyond what the pipeline reads (`stunned`, `silenced`, `untargetable`, `invulnerable`, `magic_immune`). `frozen`/`rooted` are gameplay conventions — pair them with a `move_speed` `Mul 0` contribution for an actual freeze, and have your movement code read the state.

## See also

- [AbilityDefinition](./AbilityDefinition.md) — abilities that apply modifiers
- [AbilityLibrary](./AbilityLibrary.md) — register modifiers into the system
- Back: [Abilities module](./README.md)
