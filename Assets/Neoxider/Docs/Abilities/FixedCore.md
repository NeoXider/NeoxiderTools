# Fixed core (Neo.Abilities.Fixed)

**What it is:** an engine-free, float-free implementation of the ability domain for deterministic games: lockstep, server-authoritative simulation, replays. The assembly `Neo.Abilities.Fixed` has no references and `noEngineReferences: true`; no type in it uses `float`, `double` or `decimal`. It shares the content model, ids and random seeding of the float `AbilitySystem` and plays converted content the same way within the Q8.8 grid.

**How to use:**
1. Create a `FixedAbilitySystem` from `FixedConversionSettings.CreateRules()`.
2. Convert ability and modifier assets with `FixedContentConverter`, or build `Fixed*Blueprint`s in code.
3. Create units with raw pools and grant abilities.
4. Cast with `FixedCastRequest` and call `Step()` once per simulation tick.
5. Compare `FixedStateHasher.Compute(system)` between peers.

- Namespace: `Neo.Abilities.Fixed` (the converter is in `Neo.Abilities`)
- Path: `Assets/Neoxider/Scripts/Abilities/Fixed/` (converter: `Scripts/Abilities/Domain/Fixed/FixedContentConverter.cs`)
- Related: [Deterministic mode](./DeterministicMode.md), [Rules and extension points](./Rules.md), [Triggers, damage hooks, zones and templates](./TriggersAndHooks.md)

---

## Quick start

```csharp
using Neo.Abilities;
using Neo.Abilities.Fixed;

FixedConversionSettings settings = new FixedConversionSettings { TicksPerSecond = 30, FractionBits = 8 };
FixedAbilitySystem system = new FixedAbilitySystem(settings.CreateRules());

List<FixedConversionIssue> issues = new List<FixedConversionIssue>();
FixedContentConverter.Convert(library, settings, issues).RegisterInto(system);   // AbilityLibrary asset
foreach (FixedConversionIssue issue in issues) Debug.LogWarning(issue);           // what moved off the grid

FixedFormat q = system.Rules.Format;                                               // Q8.8: one = 256
FixedUnit hero = system.CreateUnit(team: 1);
hero.AddPool(FixedResources.Health, q.FromInt(300));
hero.AddPool(FixedResources.Mana, q.FromInt(100));
hero.Position = new FixedVec3(0, 0, 0);
FixedUnit golem = system.CreateUnitWithId(42, team: 2);                            // the game's entity id
golem.AddPool(FixedResources.Health, q.FromInt(600));
golem.SetBaseProperty(FixedProperties.Armor, q.FromInt(5));

system.GrantAbility(hero.Id, "strike");
FixedCastRequest cast = FixedCastRequest.AtUnit(hero.Id, "strike", golem.Id, seed: 7u);
system.Cast(in cast);
system.Step();                                                                     // one tick
ulong hash = FixedStateHasher.Compute(system);                                     // compare between peers
```

The default world adapter reads `FixedUnit.Position` and scans units linearly in creation order. A game with its own spatial structure plugs an `IFixedWorldAdapter` (`TryGetPosition`, `QueryUnitsInRadius`, `RequestSpawn`) into `system.World`.

## Numbers and time

| Type | Meaning |
|------|---------|
| `FixedFormat` | Fraction bits (0..16) and `One` (Q8.8: 256). `FromInt`, `FromRatio`, `FromPercent`, `Mul`, `Div`, `ToIntRounded`, `TryParse("0.5")` (decimal text without floating point). `FixedRules.Format`, default `FixedFormat.Q8`. |
| raw `int` | Every amount, pool, property, multiplier, radius and fraction (Q8.8: 1.5 = 384, 25% = 64). APIs that take one end in `Raw`. |
| `FixedValue` | Optional wrapper over a raw int with saturating `+ −` and comparisons; products go through the format. |
| `FixedMath` | Saturating add, subtract, negate and multiply; `DivRoundHalfAwayFromZero`; `MulDiv` with an exact 128-bit product that saturates instead of wrapping; `SqrtFloor`; `Gcd`. |
| ticks | Durations, intervals, cooldowns, charge restores, reaction cooldowns and zone lifetimes are whole ticks. `Step()` advances one, `Step(n)` n, `Advance(pending)` at most `MaxStepsPerAdvance` (8). |
| per-second properties | `health_regen` and `mana_regen` convert through `FixedRules.TicksPerSecond`; regeneration carries the remainder, so 1 per second adds exactly one per 30 ticks. |
| `FixedVec3` | Raw coordinates. Squared distances are unsigned 64-bit and saturate; keep coordinates within ±2^30 raw for exact dot products. Cones use whole degrees and an integer cosine table (`FixedCone`). |
| `IFixedRandomSource` | Seeded integer draws: `NextRaw(one)` in [0, one), `NextInt(max)`. The default xorshift32 is the float core's generator with the same seeds, so a chance on the grid rolls exactly as in the float core. |

## Content

`FixedAbilityBlueprint`, `FixedModifierBlueprint`, `FixedModifierReaction`, `FixedEffectNode` and `FixedPropertyContribution` mirror the float blueprints field by field, with raw values and ticks. A `Mul` contribution with a positive `Denominator` is an exact ratio (`ValueRaw / Denominator`, e.g. 110 / 100), so multipliers need no rounding. `FixedModifierBlueprint.StackingKind` is free for a game's own stacking rule.

`FixedContentConverter` converts `AbilityLibrary`, `AbilityDefinition`, `ModifierDefinition`, blueprints and template bundles:

- numbers to raw, rounded half away from zero and saturated at the int range; NaN and infinity are reported as errors;
- seconds to ticks, rounded up (a positive time is at least one tick), so a timer ends on the same step as in the float core stepped at the same rate;
- `Mul` multipliers that are off the grid but whole percents (1.1, 0.7) to exact ratios (`ExactPercentMultipliers`, on by default);
- angles to whole degrees; a `Chance` of 1 or more to always.

Each `FixedConversionIssue` has a severity, content id, field, original value, converted value and message. The converter reports every value that moves by more than half a raw unit: times that are not a whole number of ticks, fractional angles, saturated or non-finite numbers, and every operation the fixed core lacks. `ReportOffGridValues` also lists, as Info, values rounded within half a step.

## Leveled and scalable values

The fixed blueprints carry every leveled and scalable field of the float content and resolve them with the same rules ([AbilityDefinition](./AbilityDefinition.md#leveled-and-scalable-values)), in raw units and ticks. Only the names and units differ:

| Float field | Fixed field | Conversion |
|-------------|-------------|------------|
| `RangeByLevel`, `ProjectileSpeedByLevel` | `RangeByLevelRaw`, `ProjectileSpeedByLevelRaw` | raw |
| `CooldownByLevel`, `ChargeRestoreTimeByLevel` | `CooldownByLevelTicks`, `ChargeRestoreByLevelTicks` | ticks, rounded up |
| `MaxChargesByLevel`, `MaxTargetsByLevel` | same names | copied |
| `AbilityCost.AmountByLevel`, `AmountKey`, `Mode`, `MinAmount` | `FixedAbilityCost.AmountByLevelRaw`, `AmountKey`, `Mode` (`FixedCostMode`), `MinAmountRaw` | raw |
| `LeveledValue.ScaleStat`, `ScaleMode`, `SpentResource`, `SpentCoefficient` | `FixedLeveledValue.ScaleStat`, `ScaleMode`, `SpentResource`, `SpentCoefficientRaw` | enums copied, raw coefficient |
| node `AmountScaleStat`, `AmountScaleMode`, `AmountSpentResource`, `AmountSpentCoefficient` | node `AmountScaleStat`, `AmountScaleMode`, `AmountSpentResource`, `AmountSpentCoefficientRaw` | same |
| node `ChanceByLevel` | `ChanceByLevelRaw` | raw; an entry of 1 or more = `Always` |
| node `DurationByLevel`, `IntervalByLevel` (zone) | `DurationByLevelTicks`, `IntervalByLevelTicks` | ticks, rounded up |
| `PropertyContribution.ValueByLevel` | `FixedPropertyContribution.ValueByLevelRaw` | raw; with an exact-percent `Mul` every entry shares the `Denominator` (0.8 / 0.7 / 0.6 become 80 / 70 / 60 over 100) |

Resolve helpers:

- `FixedAbilityBlueprint.ResolveRangeRaw`, `ResolveCooldownTicks`, `ResolveMaxCharges`, `ResolveChargeRestoreTicks`, `ResolveProjectileSpeedRaw(level)`;
- `FixedAbilitySlot.MaxCharges`, `UsesCharges`, `RangeRaw`;
- `FixedEffectNode.ResolveChanceRaw`, `ResolveMaxTargets`, `ResolveRadiusRaw`, `ResolveDurationTicks`, `ResolveIntervalTicks(level)`;
- `FixedPropertyContribution.ValueFor(stacks, level)`, `FixedAbilityCost.ResolveAmountRaw(...)`;
- `FixedValueResolver.Resolve(in value, system, caster, level, target)`, without a context and without the spent term.

Level changes follow the float rules. What a cast paid is recorded raw (`FixedEffectContext.GetSpent`, `FixedModifierInstance.GetSpent`) and travels as in the float core.

**Integer rounding.** The spent term is `SpentCoefficientRaw × spent / one`; the multiplicative scale term is `value + value × term / one`. Each is one `MulDiv`, rounded half away from zero. The state hash folds a modifier's captured paid amounts only when it has any. A coefficient off the Q8.8 grid keeps its rounded value: 0.1 (10% of max mana) becomes 26/256 = 0.1016, so 10% of 200 mana costs 20.3 in the fixed core. When the exact number matters, use a coefficient on the grid (0.125), Q16 or a cost by level.

## What the core provides

| Area | Fixed core |
|------|------------|
| Units | ids from the game (`CreateUnitWithId`) or sequential, team (0 neutral), level, position, pools with max bonuses, base properties, permanent states, death and revive |
| Properties | aggregated by `FixedRules.Aggregator`, cached per unit until modifiers, base values or rules change |
| Modifiers | duration and interval ticks, `TickOnApply`, `RestartTicksOnRefresh`, all six stack policies through `FixedRules.Stacking`, states, shields (`shield_hp`), dispel |
| Triggers | Event, EveryNth, HealthBelow (exact ratio edge, re-armed by healing), StateEnter / StateExit, Applied / Removed; charges with `RemoveWhenSpent`, cooldown ticks, priority, tag, ability and hits-only filters |
| Damage | the float pipeline on raw values: immunity, before hooks (`multiply`, `add`, `absorb`, `cancel`, `reflect`), formula, shields, receipts, lifesteal, reflection, depth caps |
| Ops | damage, heal, apply_modifier, remove_modifier, dispel, resource_change, spawn, execute, chain, modify_damage, reflect, projectile, area_apply, zone, cast_ability; add more with `IFixedEffectOperation` |
| Casts | grant, liveness, stun and silence, cooldown and charges, target, team, range, costs, cast and impact effects, projectile delivery with host-reported hits and a leak guard |
| Zones | auras and formations: spawn pulse, interval pulses, lifetime, anchor follow and death, team filter, modifier and payload per pulse |
| Events | the float event ids; per-id handlers, modifier reactions, then handlers of every event (`FixedEventHandler(in FixedEventArgs)`) |
| Hash | `FixedStateHasher` over raw ints in the field order of `AbilityStateHasher` |

Not in the fixed core: the motion ops (`knockback`, `pull`, `teleport`; the host owns movement), the trace, and the scene and NoCode layer (`AbilitySystemBehaviour`, `AbilityUnitBehaviour`, auto-casters, views). A host drives the fixed system from its own simulation loop.

## Rules

`FixedRules` holds the replaceable rules. Assigning null restores the default; every change bumps `Version`, and cached properties are recomputed.

| Property | Interface | Default |
|----------|-----------|---------|
| `DamageFormula` | `IFixedDamageFormula` | `FixedDamageFormula`: (raw + flat) × multiplier → evasion → × outgoing → crit → mitigation → × incoming; the steps before mitigation keep 16 extra bits |
| `Mitigation` | `IFixedMitigationRule` | `FixedArmorResistMitigation`: physical × 100 / (100 + 6·armor), magical × (100 − resist%) / 100, as exact ratios |
| `CriticalHit` | `IFixedCriticalHitRule` | `crit_chance` / `crit_multiplier` |
| `Evasion` | `IFixedEvasionRule` | `evasion_chance`, physical only |
| `Stacking` | `IFixedStackingRule` | by `StackPolicy` |
| `Aggregator` | `IFixedPropertyAggregator` | `FixedLayeredPropertyAggregator` (multipliers compound); alternative `FixedAdditivePercentPropertyAggregator` |
| `Targeting` | `IFixedTargetResolver` | caster, targets, areas, cones; nearest N by squared distance, then id |
| `Cooldowns` | `IFixedCooldownRule` | × (100 − cooldown_reduction_percent, at most 90) / 100, rounded up |
| `Costs` | `IFixedCastCostRule` | mana × `mana_cost_mul` |
| `Random` | `IFixedRandomFactory` | `FixedXorShiftRandomFactory` |

A game plugs its own combat rules the same way. Shortened example; real implementations must not allocate per call:

```csharp
sealed class ArmorRatioMitigation : IFixedMitigationRule            // amount × 100 / (100 + armor), Q8.8 armor
{
    public int Mitigate(FixedDamageContext damage, FixedUnit target, int amountRaw)
    {
        int one = damage.System.Rules.Format.One;
        if (!damage.IsPhysical) return amountRaw;
        int armor = FixedMath.Clamp(target.GetProperty(FixedProperties.Armor), 0, 1000 * one);
        return FixedMath.MulDivToInt(amountRaw, 100L * one, 100L * one + armor);
    }
}

sealed class RationalStatAggregator : IFixedPropertyAggregator      // (base + ΣAdd) × ΠN / ΠD, rounded once
{
    public int Compute(string id, int baseRaw, List<FixedResolvedContribution> list, FixedFormat format)
    {
        long sum = baseRaw, n = 1, d = 1;
        foreach (FixedResolvedContribution c in list)
        {
            if (c.Op == FixedPropertyOp.Add) sum += c.ValueRaw;
            else if (c.Op == FixedPropertyOp.Mul)
            {
                n *= c.ValueRaw; d *= c.Denominator > 0 ? c.Denominator : format.One;
                long g = FixedMath.Gcd(n, d); n /= g; d /= g;
            }
        }
        return FixedMath.ClampToInt(FixedMath.MulDiv(sum, n, d));
    }
}

sealed class GameStackingRule : IFixedStackingRule                  // DenyNew / RefreshDuration / StackUpTo3
{
    public FixedStackAction Decide(FixedModifierBlueprint bp, FixedModifierInstance existing, int active) =>
        existing == null ? FixedStackAction.CreateNew
        : bp.StackingKind == 1 ? FixedStackAction.Refresh
        : bp.StackingKind == 2 ? FixedStackAction.AddStack                     // MaxStacks = 3 on the blueprint
        : FixedStackAction.Ignore;
}

FixedRules rules = new FixedRules
{
    Mitigation = new ArmorRatioMitigation(), Aggregator = new RationalStatAggregator(), Stacking = new GameStackingRule()
};
```

## Determinism

- **Numbers:** every value is an int and every time a tick count; rounding is half away from zero; overflow saturates.
- **Order:** units tick in creation order; modifiers by owner, in the order owners first received a modifier, then by application; reactions by priority, then application, then list order; zones in spawn order; area hits in query order (nearest N by squared distance, then id); projectile seeds by hit index.
- **Seeds:** the float core's: the cast seed, or cast id × 2654435761 ^ caster, mixed per tick, reaction, projectile hit and zone pulse.
- **State hash:** `FixedStateHasher.Compute` folds the version, tick count, units (id, team, alive, level, health and mana current and max, modifiers with trigger counters, slots), zones and pending projectile casts. Compare it every N ticks.

## Conformance with the float core

A shared conformance suite plays each scenario on the float core with `DeterministicAbilityProfile` (30 ticks per second, Q8.8 rounding) and on the fixed core with the same content converted. Health, mana, properties, stacks, remaining ticks, charges and receipt counts agree, apart from these known differences:

| Difference | Size | What to do |
|------------|------|------------|
| A float timer subtracts the float step (0.033333335 s at 30 ticks per second) and the error accumulates, so a timer of a whole number of ticks may end one tick later in the float core. The fixed core counts ticks exactly. | one tick at an exact expiry | compare one tick away from expiries; the fixed result is the exact one |
| Effect fractions and multipliers off the grid keep their rounded value: a reflect of 0.3 is 77/256 = 0.30078. | 0.4 raw per unit of damage scaled (4 raw on a 20 hit) | author fractions on the grid (multiples of 1/256), use Q16, or properties with exact percents |
| Each multiplicative step after the pre-mitigation block rounds once (mitigation, incoming multiplier, chain falloff, heal multiplier). | at most 1 raw per step | — |
| Regeneration: the fixed core adds exactly the per-second value each second; the float core adds `regen × step` every tick without rounding. | 0.02 raw after 3 s | — |
| Modifier ticks of different owners run in first-modifier order (fixed) or dictionary order (float). They differ only if an owner lost all modifiers and got new ones while others kept theirs. | order only | do not depend on cross-owner tick order |
| An interval that is not a whole number of ticks (0.25 s at 30 ticks per second) alternates 7 and 8 steps in the float core and is a fixed 8 in the fixed core. The converter warns. | cadence | author intervals in whole ticks |

## Performance

Steps, hooks, triggers, zone pulses, receipts and the state hash allocate nothing after warm-up. Measured on the float core's benchmark workloads (Unity 6000.3, editor Mono, p50 per step):

| Workload | Float core | Fixed core |
|----------|-----------|-----------|
| DoT on 64 units | 0.60 ms | 0.34 ms |
| DoT on 256 units | 3.07 ms | 1.44 ms |
| DoT on 1024 units | 13.6 ms | 5.9 ms |
| 256 units × 1 modifier | 1.42 ms | 0.84 ms |
| 256 units × 4 modifiers | 2.15 ms | 1.32 ms |
| 1024 units × 4 modifiers | 9.73 ms | 5.72 ms |
| 1024 units × 8 modifiers | 13.97 ms | 8.62 ms |

## Migrating a deterministic game

1. Keep the game's own simulation loop and call `system.Step()` from it, or `Advance(pending)` for catch-up.
2. Create units with the game's entity ids (`CreateUnitWithId`). Copy positions into `FixedUnit.Position`, or plug an `IFixedWorldAdapter` over the game's spatial structure.
3. Plug the game's formulas in as rules (mitigation, aggregation, stacking, crit, costs) instead of changing content.
4. Convert or author content. Read the conversion report and fix every Warning: times off the tick grid, fractional angles, unsupported ops.
5. Replace one system at a time, comparing its outcomes with the old one, and keep the state hash in the desync check.

## По-русски

`Neo.Abilities.Fixed` — отдельная сборка без UnityEngine и без чисел с плавающей точкой. Это тот же домен способностей,
но все числа — целые `int` в формате `FixedFormat` (по умолчанию Q8.8, единица = 256), а время — целые тики (`Step()`).
Округление «половина от нуля», переполнение насыщается. Генератор и сиды те же, что во float-ядре.

Контент переводит `FixedContentConverter`: числа округляются к сетке, секунды — вверх до целого тика, множители в целых
процентах становятся точными дробями, углы — целыми градусами. Конвертер сообщает о каждом значении, сдвинувшемся больше
чем на полшага, и об операциях, которых нет в ядре (перемещения остаются хосту). Уровневые и масштабируемые параметры
переносятся все. Правила подключаются через `FixedRules`. Известные расхождения с float-ядром перечислены в таблице выше;
на тех же нагрузках фиксированное ядро в редакторе в 1,6–2,3 раза быстрее.
