# ModifierDefinition

**What it is:** the ScriptableObject asset for one modifier (`Scripts/Abilities/Data/ModifierDefinition.cs`, namespace `Neo.Abilities`): a buff, debuff, DoT, aura payload, shield or stun. It wraps a pure-data `ModifierBlueprint` (`Domain/Modifiers/ModifierBlueprint.cs`). While active, a modifier adds properties, sets boolean states, runs tick effects on an interval and reacts to gameplay events.

**How to use:**
1. Create the asset with `Assets > Create > Neoxider > Abilities > Modifier`.
2. Configure the **Blueprint** fields.
3. Register it, usually through an [AbilityLibrary](./AbilityLibrary.md).
4. Apply it by **Id** from an `apply_modifier` effect node, or with `AbilitySystem.ApplyModifier(...)` from code.

---

## Fields (Inspector)

All fields live under the **Blueprint** foldout. A blank **Id** is filled from the asset name in `OnValidate`.

| Field | Type | Description |
|-------|------|-------------|
| **Id** | string | Unique modifier id, e.g. `burn`. Used for stacking, dispel and `apply_modifier`. |
| **Display Name** | string | Name for UI. |
| **Duration** | float | Seconds until expiry. `0` or negative = permanent until removed. |
| **Duration By Level** | `float[]` | Optional durations by the ability level captured at application. Empty = Duration. |
| **Stack Policy** | `ModifierStackPolicy` | What re-application does: see [Stack policies](#stack-policies). |
| **Max Stacks** | int | Cap for `Stack` and the instance cap of `IndependentCapped`. Ignored otherwise. |
| **Properties** | `List<PropertyContribution>` | Property contributions while active. |
| **States** | `List<string>` | Boolean states granted while active (any true wins), e.g. `stunned`, `frozen`. |
| **Tick Interval** | float | Seconds between effect ticks. `0` or negative = no ticking. |
| **Tick On Apply** | bool | The first tick fires on the next positive `AbilitySystem.Tick`, without waiting a full interval. |
| **Restart Ticks On Refresh** | bool | A re-application that refreshes, stacks or prolongs restarts the tick phase: the next tick comes a full interval later. Default off: the phase continues. A re-application the policy ignores changes nothing. |
| **Tick Effects** | `List<EffectNodeData>` | [Effect nodes](./AbilityDefinition.md#effect-nodes) run on every tick, e.g. the damage of a DoT. |
| **Event Reactions** | `List<ModifierEventReaction>` | On an event id, run a list of effect nodes. See [Event reactions](#event-reactions). |
| **Is Debuff** | bool | Marks the modifier as negative for UI and dispel. |
| **Dispellable** | bool | The `dispel` op can remove it. Default `true`. |
| **Presentation Cue** | string | Optional cue id for VFX and SFX listeners. |

### Stack policies

| Policy | Behavior |
|--------|----------|
| `Independent` | Every application creates a separate instance with its own duration. |
| `Refresh` (default) | One instance per unit; re-applying refreshes its duration. |
| `Stack` | One instance per unit; re-applying adds a stack (up to **Max Stacks**) and refreshes the duration. Properties scale with `PerStackValue`, tick and reaction amounts with `AmountPerStack`. |
| `Unique` | One instance per unit; re-applying while it is active does nothing: no refresh, no receipt. |
| `Prolong` | One instance per unit; re-applying adds the full (leveled) duration to the remaining time. |
| `IndependentCapped` | Independent instances up to **Max Stacks**; one more replaces the oldest. |

The decision is made by `AbilityRules.Stacking`, which a game can replace ([Rules](./Rules.md#modifier-stacking)).

### Property contributions

Each entry is `{ PropertyId, Op, Value, PerStackValue, ValueByLevel }`. Aggregation across all of a unit's modifiers is fixed and deterministic: `final = max((base + sum(Add)) * product(Mul), all Max floors)`.

| `Op` | Meaning | Example |
|------|---------|---------|
| `Add` | Flat bonus. | `{ attack_damage, Add, 20 }` |
| `Mul` | Multiplier after all Adds. `1` = no change. | `{ move_speed, Mul, 0.4 }` = 60% slow |
| `Max` | Raises the final value to at least this floor. Only the highest floor counts. | `{ move_speed, Max, 1 }` |

Well-known property ids are in `AbilityProperties`: `move_speed`, `attack_damage`, `armor`, `spell_power`, `crit_chance`, `crit_multiplier`, `evasion_chance`, `lifesteal_percent`, `incoming_damage_mul`, `shield_hp` and more. The registry is open, so any string works. How damage reads the combat properties: [Combat properties](./TriggersAndHooks.md#combat-properties).

### Strength by level

**Value By Level** makes a contribution follow the ability level captured on the instance, that is, the level of the ability that applied it; a re-application captures it again. The array is sampled like every leveled array (`ValueByLevel[clamp(level - 1)]`, empty = `Value`), and stacks add on top:

`effective = ValueByLevel[level] + PerStackValue × (stacks - 1)`

Shield capacity (`shield_hp`) follows it too. Example, a slow of 20 / 30 / 40%: `{ move_speed, Mul, Value 0.8, Value By Level = [0.8, 0.7, 0.6] }`. A stun of 1 / 1.5 / 2 s: **Duration By Level** `[1, 1.5, 2]` with the `stunned` state.

```csharp
ModifierBlueprint frost = new ModifierBlueprint { Id = "frost_slow", Duration = 2f, IsDebuff = true };
frost.Properties.Add(new PropertyContribution(AbilityProperties.MoveSpeed, PropertyOp.Mul, 0.8f)
{
    ValueByLevel = new[] { 0.8f, 0.7f, 0.6f }      // 20 / 30 / 40% slow
});
ModifierBlueprint bash = new ModifierBlueprint { Id = "bash_stun", Duration = 1f, DurationByLevel = new[] { 1f, 1.5f, 2f } };
bash.States.Add(AbilityStates.Stunned);
```

Tick and reaction effects can also scale with what the applying cast spent; such a modifier captures the paid amounts at apply time ([spent amounts](./AbilityDefinition.md#spent-amounts-travel-with-the-cast)). Every other scalable parameter is in [AbilityDefinition: Leveled and scalable values](./AbilityDefinition.md#leveled-and-scalable-values).

### Event reactions

Each reaction is `{ EventId, Effects, TargetEventSource }`. When `EventId` (an `AbilityEvents` id such as `take_damage`) fires on the owning unit, `Effects` run through the normal pipeline. The reaction's caster is the modifier's caster. The target is the modifier owner, or the **event source** (for example the attacker) when **Target Event Source** is set. Nested reactions stop at depth 3.

Optional trigger fields turn a reaction into a trigger: **Trigger** (`Event`, `EveryNth`, `HealthBelow`, `StateEnter`, `StateExit`, `Applied`, `Removed`), **Every N**, **Threshold**, **State Id**, **Tag Filter**, **Ability Filter**, **Hits Only**, **Max Triggers** with **Remove When Spent**, **Cooldown** and **Priority**. Reactions on `before_deal_damage` and `before_take_damage` can change the hit in flight with `modify_damage` and `reflect`. The defaults give a plain event reaction. Details: [Triggers](./TriggersAndHooks.md#triggers).

## Runtime rules

- **Ticks advance only while the system ticks.** DoT damage needs `AbilitySystem.Tick(dt)`; the scene hub calls it every frame, headless callers must call it themselves.
- **Only finite positive steps count.** `AbilitySystem.Tick` and `ModifierEngine.Tick` ignore NaN, infinity and nonpositive steps before changing timers. The system also ignores a step that would overflow its clock. `TickOnApply` runs on the first accepted positive step, not inside `Apply`.
- **No ticks past expiry.** A finite modifier ticks only within its remaining lifetime, including a tick exactly at expiry: Duration `1`, interval `1` and `Tick(5)` give one tick, then expiry. At most eight catch-up ticks run per modifier per call, so use a stable step for replays.
- **Bulk removal works on a snapshot.** `RemoveById`, `RemoveWhere` and `RemoveAllFrom` take the starting instances first. Removal callbacks may remove other modifiers or create replacements; replacements survive the outer removal. The returned count includes only removals made by that call. An engine cannot remove another engine's instance.
- **Shield events are synchronous.** A `shield_absorbed` callback may stack or dispel the current shield; the break check then uses its live state and capacity. An exhausted instance emits `shield_broken` at most once, stays active while its own break reaction runs, and is removed afterwards, also when a break handler throws.
- **Reapplying a shield during its break.** Reapplying the same `Refresh` or `Stack` id inside the break creates a new instance that survives the old one's removal and takes part in later hits; the current hit keeps its starting shield snapshot. The order for the exhausted instance stays `shield_absorbed` → `shield_broken` → `modifier_removed`, and the replacement's `modifier_applied` may come before the old removal. Reapplication outside the break refreshes or stacks as usual and does not restore consumed shield HP.

## Key API

| Member | Description |
|--------|-------------|
| `ModifierBlueprint Blueprint` | The wrapped data. Register it with `AbilitySystem.RegisterModifier(...)`. |
| `string Id` | Shortcut for `Blueprint.Id`. |
| `bool ModifierBlueprint.IsPermanent` | `true` when `Duration <= 0`. |
| `ModifierInstance.IsPermanent` | Permanence after resolving `DurationByLevel`. Use it for a live instance. |
| `bool ModifierBlueprint.HasTicks` | `true` with a positive tick interval and tick effects. |
| `float ModifierBlueprint.ResolveDuration(level)` | Duration at an ability level: `DurationByLevel`, else `Duration`. |
| `float PropertyContribution.ValueFor(stacks, level)` | Contribution for a stack count and a captured level. `ValueForStacks(stacks)` ignores `ValueByLevel`. |
| `ModifierInstance.AbilityLevel` / `SpentCount` / `GetSpent(id)` | The captured level and paid amounts of a live instance. |
| `ModifierEngine.Apply(blueprint, caster, owner, sourceAbilityId, level, spentSource)` | Apply overload that also captures what `spentSource`'s cast paid; custom ops pass their context. |

Apply from code, like the `apply_modifier` op:

```csharp
ModifierApplyResult r = system.ApplyModifier("burn", casterId, targetId, "fireball");
// r.Succeeded, r.CreatedNew, r.Instance.Stacks, r.Instance.RemainingDuration
```

## Example — burn DoT

**Inspector:** Duration `4`, Stack Policy `Refresh`, Tick Interval `1`, Is Debuff on. One **Tick Effect**: OpId `damage`, Target `Target`, Amount `15`, Damage Type `magical`.

An ability attaches it with an Impact Effect: OpId `apply_modifier`, Target `AreaAroundTarget`, Team Filter `Enemies`, Radius `3`, Modifier Id `burn`.

```csharp
ModifierBlueprint burn = new ModifierBlueprint
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

- **Tick targeting:** during a tick the primary target is the owner and the caster is the modifier's caster. `Target = Target` hits the owner, and kill credit goes to the caster.
- **Shields are `shield_hp` contributions, not a state.** The damage pipeline absorbs against every `shield_hp` pool and fires `shield_absorbed` and `shield_broken`; pure damage bypasses shields. See the [Magic Shield example](./README.md#magic-shield).
- **A level-up does not change a live modifier.** It keeps the level and paid amounts it was applied with until it is applied again.
- **`States` stop nothing by themselves** beyond what the pipeline reads: `stunned`, `silenced`, `untargetable`, `invulnerable`, `magic_immune`. `frozen` and `rooted` are conventions: pair them with a `move_speed` `Mul 0` contribution and let your movement code read the state.

## See also

- [AbilityDefinition](./AbilityDefinition.md) — abilities that apply modifiers
- [AbilityLibrary](./AbilityLibrary.md) — register modifiers into the system
- Back: [Abilities module](./README.md)
