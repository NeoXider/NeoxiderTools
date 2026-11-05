# Rules and extension points

**What it is:** `AbilitySystem.Rules` (`AbilityRules`) is the one place where a game swaps the rules of the ability system without forking the package: damage formula, mitigation, critical hits, evasion, modifier stacking, property aggregation, effect targeting, cooldowns, costs, the time source, the numeric backend and the random source. Every rule has a default that reproduces the classic Neo.Abilities behaviour; assigning `null` restores it.

**How to use:** implement the interface, assign it once after creating the system (or from a component in your scene), before gameplay starts. Rules run on hot paths, so implementations must not allocate per call.

- Namespace: `Neo.Abilities`
- Sources: `Assets/Neoxider/Scripts/Abilities/Domain/Rules/`
- Related: [Triggers, damage hooks, zones and templates](./TriggersAndHooks.md)

| Property | Interface | Default | Decides |
|----------|-----------|---------|---------|
| `DamageFormula` | `IDamageFormula` | `StandardDamageFormula` | The whole calculation from raw amount to the damage reaching shields. |
| `Mitigation` | `IMitigationRule` | `ArmorResistMitigationRule` | Armor (physical) and magic resist (magical) curves. |
| `CriticalHit` | `ICriticalHitRule` | `PropertyCriticalHitRule` | Crit roll and multiplier (`crit_chance`, `crit_multiplier`). |
| `Evasion` | `IEvasionRule` | `PropertyEvasionRule` | Dodge roll (`evasion_chance`, physical only). |
| `Stacking` | `IModifierStackingRule` | `StandardModifierStackingRule` | What re-applying a modifier does. |
| `Aggregator` | `IPropertyAggregator` | `LayeredPropertyAggregator` | How base + Add / Mul / Max contributions combine. |
| `Targeting` | `IEffectTargetResolver` | `StandardEffectTargetResolver` | Which units an effect node hits (selectors, areas, cones, caps). |
| `Cooldowns` | `ICooldownRule` | `StandardCooldownRule` | Effective cooldown / charge restore time. |
| `Costs` | `ICastCostRule` | `StandardCastCostRule` | Effective resource cost (NaN / infinity rejects the cast). |
| `Time` | `IAbilityTimeSource` | `VariableStepTimeSource` | How `AbilitySystem.Advance(hostDelta)` turns host time into domain steps. |
| `Numeric` | `IAbilityNumeric` | `FloatNumeric` | Rounding of computed amounts and property values (float or fixed point). |
| `Random` | `IRandomSourceFactory` | `XorShiftRandomFactory` | The seeded generators of casts, ticks, reactions and zones. |

`Rules.Version` changes with every swap; cached property values are recomputed after a swap. `Rules.ResetToDefaults()` restores everything.

## Damage formula, mitigation, crit, evasion

The standard formula is `(raw + FlatAdd) × Multiplier` → evasion → `outgoing_damage_mul` → crit → mitigation → `incoming_damage_mul`. Replace one step or the whole formula:

```csharp
// Armor as "amount * 100 / (100 + armor)" instead of the Dota curve; magical damage unchanged.
public sealed class FlatArmorMitigation : ArmorResistMitigationRule
{
    public override float Mitigate(DamageContext damage, AbilityUnit target, float amount)
    {
        if (!damage.IsPhysical)
        {
            return base.Mitigate(damage, target, amount);
        }

        float armor = Mathf.Max(0f, target.GetProperty(AbilityProperties.Armor));
        return amount * 100f / (100f + armor);
    }
}

system.Rules.Mitigation = new FlatArmorMitigation();
```

```csharp
// A whole custom formula: level difference scales the hit, crit adds a flat bonus.
public sealed class LevelGapFormula : IDamageFormula
{
    public float Compute(DamageContext damage, AbilityUnit source, AbilityUnit target, AbilityRules rules)
    {
        float amount = (damage.RawAmount + damage.FlatAdd) * damage.Multiplier;
        int gap = source != null ? source.Level - target.Level : 0;
        amount *= 1f + Mathf.Clamp(gap, -10, 10) * 0.05f;
        if (source != null && rules.CriticalHit.TryRollCritical(damage, source, out float multiplier))
        {
            damage.Crit = true;
            amount *= multiplier;
        }

        return rules.Mitigation.Mitigate(damage, target, amount);
    }
}
```

Set `damage.Evaded = true` and return 0 for a dodge. The context exposes `Random` (null for direct calls without a generator: no crit, no evasion by default).

## Modifier stacking

`IModifierStackingRule.Decide(blueprint, existing, activeInstances)` returns a `ModifierStackAction`: `CreateNew`, `Refresh`, `AddStack`, `Prolong`, `Ignore`, `ReplaceOldest`. `existing` is the oldest active instance with the same id (null when none).

```csharp
// "Stack up to three, refreshing the duration; the fourth application is ignored."
public sealed class CappedRefreshStacking : IModifierStackingRule
{
    public ModifierStackAction Decide(ModifierBlueprint blueprint, ModifierInstance existing, int activeInstances)
    {
        if (existing == null)
        {
            return ModifierStackAction.CreateNew;
        }

        return existing.Stacks < 3 ? ModifierStackAction.AddStack : ModifierStackAction.Ignore;
    }
}
```

## Property aggregation

`IPropertyAggregator.Compute(propertyId, baseValue, contributions)`. Defaults: `LayeredPropertyAggregator` — `max((base + ΣAdd) × ΠMul, Max floors)`, multipliers compound; `AdditivePercentPropertyAggregator` — `max((base + ΣAdd) × (1 + Σ(Mul − 1)), Max floors)`, two +50% give ×2. The aggregator receives the property id, so a game can layer only some stats differently.

```csharp
system.Rules.Aggregator = AdditivePercentPropertyAggregator.Instance;
```

## Targeting

`IEffectTargetResolver.Resolve(node, context, results)` fills the units an effect node applies to. Derive from `StandardEffectTargetResolver` to add selectors (line of sight, structures, a custom shape) or replace it. Use `context.System.World.QueryUnitsInRadius` (backed by `AbilitySpatialGrid` in the scene hub) for spatial queries and keep a scratch list as a field.

## Cooldowns and costs

```csharp
public sealed class FlatCooldownReduction : ICooldownRule
{
    public float ResolveCooldown(AbilityUnit caster, AbilityBlueprint ability, float baseSeconds)
    {
        return Mathf.Max(0.5f, baseSeconds - caster.GetProperty("cooldown_flat_reduction"));
    }
}

public sealed class HealthCostsDoubleInRage : StandardCastCostRule
{
    public override float ResolveCost(AbilityUnit caster, AbilityBlueprint ability, AbilityCost cost)
    {
        float amount = base.ResolveCost(caster, ability, cost);
        return caster.HasState("rage") && cost.ResourceId == AbilityResourceIds.Health ? amount * 2f : amount;
    }
}
```

## Time source

`AbilitySystem.Advance(hostDelta)` asks `Rules.Time` how many steps to run. `VariableStepTimeSource` (default) runs one step of the host delta, exactly like `Tick(dt)`. `FixedStepTimeSource(ticksPerSecond, maxStepsPerCall)` accumulates host time and runs whole ticks of the same length (the excess beyond `maxStepsPerCall` ticks is dropped and counted in `DiscardedSeconds`); `StepTicks(n)` runs n ticks directly for a server or lockstep driver. `TickCount` and `ElapsedTime` report the domain clock. In a scene, **Time Mode** on [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) selects Variable or Fixed Ticks.

```csharp
system.Rules.Time = new FixedStepTimeSource(ticksPerSecond: 30);
system.Advance(Time.deltaTime);   // 0..8 ticks of 1/30 s
system.StepTicks(1);              // exactly one tick (server loop)
```

## Numeric backend

`IAbilityNumeric.Quantize` rounds computed damage, absorption, healing, lifesteal, resource changes, reflected damage and aggregated property values before they are stored or published. `FloatNumeric` (default) leaves them unchanged; `FixedPointNumeric(fractionBits)` (`FixedPointNumeric.Q8` = Q8.8, 1.0 = 256 raw) rounds half away from zero onto the fixed grid and converts with `ToRaw` / `FromRaw`. It removes accumulated float noise from stored values; it does not make float arithmetic itself bit-exact across platforms, see [Deterministic mode](./DeterministicMode.md).

## Random source

`IRandomSourceFactory.Create(seed)` returns an `IReseedableRandomSource`. Each pooled effect context creates one generator and reseeds it per cast, tick, reaction and zone pulse, so a factory runs rarely and `Reseed` must not allocate.

```csharp
public sealed class GameRandomFactory : IRandomSourceFactory
{
    public IReseedableRandomSource Create(uint seed)
    {
        return new GameRandomAdapter(seed);   // wraps the game's own deterministic generator
    }
}

system.Rules.Random = new GameRandomFactory();
```

## По-русски

`AbilitySystem.Rules` — единое место, где игра заменяет правила системы способностей, не форкая пакет: формулу урона, снижение бронёй и сопротивлением, крит, уклонение, политику стаков модификаторов, сложение характеристик (слои Add / Mul / Max), выбор целей, кулдауны, стоимость, источник времени (секунды или фиксированные тики), числовой бэкенд (float или фиксированная точка Q8.8) и источник случайности. У каждого правила есть стандартная реализация, повторяющая прежнее поведение; присваивание `null` возвращает её. Правила вызываются на горячих путях, поэтому реализации не должны выделять память на вызов. Примеры выше: своя кривая брони, формула с разницей уровней, стаки «до трёх, дальше игнор», аддитивные проценты, плоское снижение кулдауна, двойная цена здоровьем в ярости, фиксированные тики и своя фабрика генераторов. Детерминированный режим описан на странице [Deterministic mode](./DeterministicMode.md).
