# Abilities

<!-- module-art:begin -->
<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/abilities.jpg" alt="Concept art for the Abilities module" width="100%" /></p>
<!-- module-art:end -->

**What it is:** `Neo.Abilities` is a data-driven ability system. An **ability** is an asset: targeting, cost, cooldown, delivery and a list of effects. A **modifier** is anything that stays on a unit: a buff, a debuff, a DoT, a shield, a stun. New abilities are built by combining effects in data; code is needed only for a new kind of effect.

<!-- demo-shots:begin -->
<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/SurvivorDemo.jpg" alt="SurvivorDemo: a survivor arena with waves, upgrades and auto-cast abilities" width="49%" /> <img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/AbilitiesNoCodeDemo.jpg" alt="AbilitiesNoCodeDemo: NoCode ability buttons, HP and cooldown bars, auto caster" width="49%" /></p>
<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/AbilitiesTriggersDemo.jpg" alt="AbilitiesTriggersDemo: thorns, attack enhancer, mark, stacking poison, formation zone and auto-defense shield with a why-did-this-fire panel" width="98%" /></p>

*Demo scenes: `Assets/NeoxiderDemos/Demo/Scenes/SurvivorDemo.unity`, `Assets/NeoxiderDemos/Demo/Scenes/Abilities/AbilitiesNoCodeDemo.unity`, `Assets/NeoxiderDemos/Demo/Scenes/Abilities/AbilitiesTriggersDemo.unity` (Demo download). The scenes are saved with their UI and wiring, so you can inspect them without entering Play Mode.*
<!-- demo-shots:end -->

## Start here

| I want to | Read |
|-----------|------|
| Make my first ability in the Inspector | [Quick start](#quick-start-no-code) below |
| See every field of an ability and what can scale by level | [AbilityDefinition](./AbilityDefinition.md) |
| Make buffs, DoTs, shields and stuns | [ModifierDefinition](./ModifierDefinition.md) |
| Build weapons that behave differently: homing, orbit, zone, chain, pierce | [SurvivorDemo](./SurvivorDemo.md), the weapons table |
| Use plain prefabs as projectiles and zones; get the object an ability spawned | [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) |
| Add effects and sounds to casts and hits | [AbilityCuePlayer](./AbilityCuePlayer.md) |
| React to casts, hits and kills from my code or the Inspector | [AbilityEventListener](./AbilityEventListener.md) |
| Summon helpers that know their owner | [AbilitySummonBehaviour](./AbilitySummonBehaviour.md) |
| Cast automatically or from UI buttons | [AbilityAutoCaster](./AbilityAutoCaster.md), [AbilityNoCodeAction](./AbilityNoCodeAction.md) |
| Run lockstep or server-authoritative | [FixedCore](./FixedCore.md), [DeterministicMode](./DeterministicMode.md) |
| Check a scene for wiring that fails silently | [Editor](./Editor.md), the scene validator |

**По-русски:** способность — это ассет (наведение, стоимость, откат, доставка и список эффектов), модификатор — всё,
что висит на юните (бафф, дебафф, урон во времени, щит, стан). Новые способности собираются из эффектов в данных.
Таблица выше ведёт от задачи к нужной странице.

## Contents

| Group | Pages |
|-------|-------|
| Assets | [AbilityDefinition](./AbilityDefinition.md) (an ability, effect nodes, ops), [ModifierDefinition](./ModifierDefinition.md), [UnitTemplate](./UnitTemplate.md), [AbilityLibrary](./AbilityLibrary.md) |
| Scene components | [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) (the hub), [AbilityUnitBehaviour](./AbilityUnitBehaviour.md), [AbilityCasterBehaviour](./AbilityCasterBehaviour.md), [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md) |
| NoCode components | [AbilityNoCodeAction](./AbilityNoCodeAction.md), [AbilityAutoCaster](./AbilityAutoCaster.md), [AbilityCooldownSource](./AbilityCooldownSource.md), [AbilityEventListener](./AbilityEventListener.md), [AbilitySummonBehaviour](./AbilitySummonBehaviour.md), [AbilityCuePlayer](./AbilityCuePlayer.md) |
| Editor | [Ability Designer](./AbilityDesignerWindow.md) (`Neoxider > Windows > Ability Designer`), [Editor API and authoring contracts](./Editor.md) (validation, scene validator) |
| Guides | [Triggers, damage hooks, zones and templates](./TriggersAndHooks.md), [Rules and extension points](./Rules.md), [Fixed core](./FixedCore.md), [Deterministic mode](./DeterministicMode.md), [Survivor Demo](./SurvivorDemo.md), [Building a Vampire Survivors game](../VampireSurvivor_Guide.md) |

Source: `Assets/Neoxider/Scripts/Abilities` (`Domain/` pure C#, `Data/` ScriptableObjects, `Components/` wrappers).

## Quick start (no code)

1. Create a [UnitTemplate](./UnitTemplate.md) with a `health` pool, plus `mana` if the unit casts.
2. Create [AbilityDefinition](./AbilityDefinition.md) and [ModifierDefinition](./ModifierDefinition.md) assets and add them to an [AbilityLibrary](./AbilityLibrary.md).
3. Add an [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) to the scene, assign the library and bind projectile prefabs under **Archetypes**.
4. On the unit add an [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) with the template, and an [AbilityCasterBehaviour](./AbilityCasterBehaviour.md) that lists the castable abilities.
5. Trigger casts. An [AbilityAutoCaster](./AbilityAutoCaster.md) fires every ready ability at the nearest valid target. An [AbilityNoCodeAction](./AbilityNoCodeAction.md) set to `CastById` casts from a UnityEvent, for example `Button.OnClick → Execute()`.

The hub creates itself on first use if you skip step 3, but then there is no place to assign the library and archetypes. A unit that only takes damage needs just `AbilityUnitBehaviour` and a template. Show cooldowns with an [AbilityCooldownSource](./AbilityCooldownSource.md) and a `SetProgress` binding; unit receipts such as `OnDamaged` and `OnDied` are UnityEvents on `AbilityUnitBehaviour`. From code, call `caster.TryCast("frost_nova")`, `TryCastAtUnit`, `TryCastAtPoint` or `TryCastTowards` ([AbilityCasterBehaviour](./AbilityCasterBehaviour.md)).

**Try it:** open `Assets/NeoxiderDemos/Demo/Scenes/Abilities/AbilitiesNoCodeDemo.unity` (Demo download). Press **Hit Player 20**, then **Cast Heal**, then Heal again during its cooldown. **Zap dummy** and **Next level** show damage that follows the caster's unit level (12, 20, 30). **Auto Zap** starts off; **Reset demo** restores everything. The wiring is saved in the scene:

- Each button has its own `AbilityNoCodeAction` (`ApplyDamage` 20, `CastById` `heal_pulse`); **On Result Message** feeds the log.
- The bars are `SetProgress` bindings on `AbilityUnitBehaviour.HealthNormalized` and `AbilityCooldownSource.CooldownNormalized`; the Auto Zap toggle sets `AbilityAutoCaster.enabled`.
- **Zap dummy**, **Next level** and **Reset demo** are the C# path: `AbilitiesDemoGuide` calls `TryCastAtUnit` and `SetLevel`.

## What is inside

- **Abilities** turn each cast into a list of effect operations: damage, heal, apply modifier, spawn and more. The cast pipeline first checks states, costs, cooldown or charges, target and range.
- **Modifiers** stay on a unit. While active they add **properties** (`Add`, then `Mul`, then `Max`), set boolean **states** (any true wins), run **tick effects** on an interval and run **event reactions**. Expiry is guaranteed.
- **The runtime** is plain C# with injected time and a seeded random generator. It needs no GameObjects but uses Unity value types and `Neo.Core.Resources`. Every gameplay result is published on the **event stream** for UI, presentation and networking.
- **Reactive layer:** damage hooks, triggers, payload ops, zones, templates, a content validator and a trace of why something fired ([TriggersAndHooks](./TriggersAndHooks.md)).
- **Replaceable rules:** damage formula, mitigation, crit, evasion, stacking, aggregation, targeting, cooldowns, costs, time, numbers and random ([Rules](./Rules.md)).
- **Fixed core:** `Neo.Abilities.Fixed` is the same domain in integers and ticks, without UnityEngine, for lockstep and server-authoritative games ([FixedCore](./FixedCore.md), [DeterministicMode](./DeterministicMode.md)).

## Core concepts

| Concept | Type | What it is |
|---------|------|-----------|
| System | `AbilitySystem` | The façade: units, catalogs, casting, effect execution, tick. One per world. |
| Unit | `AbilityUnit` | Identity, team, resource pools, base properties and cached property and state aggregation. |
| Property | `PropertyContribution` / `AbilityProperties` | A numeric stat with an open string id. Contributions combine `add -> mul -> max`. |
| State | `AbilityStates` | A boolean flag (stunned, silenced, invulnerable...). Any true contribution wins. |
| Ability | `AbilityBlueprint` / `AbilityDefinition` | Per-cast logic: targeting, costs, cooldown, delivery, effect nodes. |
| Modifier | `ModifierBlueprint` / `ModifierDefinition` | A durable effect: properties, states, tick effects, event reactions. |
| Effect node | `EffectNodeData` | One authored step: which op, on whom, with which parameters. |
| Effect op | `IEffectOperation` / `AbilityEffectOps` | The strategy that runs a node. Open registry. |
| Event | `AbilityEventArgs` / `AbilityEvents` | A typed gameplay event; the receipt stream. |
| Damage | `DamageService` / `AbilityDamageTypes` | The one pipeline: evasion, crit, mitigation, shield, HP, death, lifesteal. |
| World seam | `IAbilityWorldAdapter` | Positions, radius queries, unit motion (`TryMoveUnit`), spawns. The hub implements it; tests fake it. |

### Effect nodes

An `EffectNodeData` is one authored step, shared by abilities (cast and impact effects) and modifiers (tick effects and reactions). It names an op id, a target selector, a team filter and the op's parameters. The built-in ops are `damage`, `heal`, `apply_modifier`, `remove_modifier`, `dispel`, `resource_change`, `spawn`, `knockback`, `pull`, `teleport`, `execute`, `chain`, `modify_damage`, `reflect`, `projectile`, `area_apply`, `zone` and `cast_ability`. Every node field and every op is described in [AbilityDefinition: Effect nodes](./AbilityDefinition.md#effect-nodes).

### Damage

All damage runs through one pipeline: evasion, crit, mitigation, shields, health, death, lifesteal, reflection. The steps, the damage hooks and the `crit_chance`, `evasion_chance` and `lifesteal_percent` properties are in [TriggersAndHooks: Damage pipeline](./TriggersAndHooks.md#damage-pipeline-with-hooks).

### Leveled and scalable values

Effect amounts, radii, chances, target caps, durations, cooldowns, range, charges, costs and modifier strength can scale with the ability level, a unit level, a caster or target stat and the resource a cast actually spent. Everything is data and every field defaults to "off". The full table, the resolution order and examples are in [AbilityDefinition: Leveled and scalable values](./AbilityDefinition.md#leveled-and-scalable-values); modifier strength is in [ModifierDefinition: Strength by level](./ModifierDefinition.md#strength-by-level).

## Code-first usage

### Create a system headless

The domain runs without Unity objects, which suits tests and servers. Add resource pools with `ResourcePoolModel.AddPool` from `Neo.Core.Resources`.

```csharp
using Neo.Abilities;
using Neo.Core.Resources;

AbilitySystem system = new AbilitySystem();

// Author an ability entirely in code (data normally lives in an AbilityDefinition asset).
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

AbilityUnit mage = system.CreateUnit(new TeamId(1), "Mage");
mage.Resources.AddPool(AbilityResourceIds.Mana,
    new ResourcePoolEntry { Current = 100f, Max = 100f, MaxDecreaseAmount = -1f, MaxIncreaseAmount = -1f });
system.GrantAbility(mage.Id, firebolt.Id);

AbilityUnit goblin = system.CreateUnit(new TeamId(2), "Goblin");
goblin.Resources.AddPool(AbilityResourceIds.Health,
    new ResourcePoolEntry { Current = 200f, Max = 200f, MaxDecreaseAmount = -1f, MaxIncreaseAmount = -1f });
```

### Cast from code and read the receipt stream

```csharp
// Subscribe to a single event id...
system.Events.Subscribe(AbilityEvents.TakeDamage,
    e => Console.WriteLine($"{e.Target} took {e.Amount} {e.DamageType} damage"));

// ...or to every event (receipt stream for UI / logging / network).
system.Events.SubscribeAny(e => Log(e.EventId, e.Target, e.Source, e.Amount));

CastResult result = system.Cast(CastRequest.AtUnit(mage.Id, "firebolt", goblin.Id));
// result.Success == true, result.CastId identifies the cast.
// goblin.Health is now 140; a take_damage (and deal_damage) event fired.

if (!result.Success)
    HandleFailure(result.Failure); // CastFailureReason, e.g. OnCooldown

// Advance time so cooldowns, durations, and DoTs progress.
system.Tick(0.016f);
```

Area effects and projectiles need world positions, so they resolve only with a world adapter. In a scene the [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) is that adapter; headless tests provide their own `IAbilityWorldAdapter`.

### Runtime contracts

The cast rules (costs, custom pools, cooldowns and charges, direction reach, range) are in [AbilityDefinition: Cast rules](./AbilityDefinition.md#cast-rules). Modifier timing, removal and shield callbacks are in [ModifierDefinition: Runtime rules](./ModifierDefinition.md#runtime-rules). The system-wide contracts:

- **Registration and grants.** Catalog ids are case-insensitive. A grant holds the catalog `Blueprint` reference it received; casts, cooldowns, charges and auto-targeting use it. Re-registering an id affects only future grants. A repeated `GrantAbility` returns the existing slot. An explicit revoke and regrant binds the replacement and resets level, cooldown and charges. Blueprints are not copied, so treat registered data as read-only.
- **Resource maximum bonuses.** Set up pools before base properties. `max_health_bonus` and `max_mana_bonus` update their pool maximum when set or changed by modifiers. Raising a maximum does not heal.
- **Cast correlation.** Nested effect contexts and modifier reactions keep the triggering `CastId`. Modifier lifecycle and shield receipts use `0`, so they cannot be attributed to a cast.
- **Effect contexts.** The `EffectContext` objects the system creates for casts, projectile hits, ticks and reactions are pooled and recycled as soon as their effects finish. A custom op must not keep the context, its `PrimaryTargets` list or its `Random` after `Execute` returns; copy what it needs. A `CreateNested` child shares the parent's `Random` and lifetime. After release `IsReleased` is `true` and `ExecuteEffects` ignores the context with a throttled `NeoDiagnostics` warning. Contexts you create with `new EffectContext(...)` are never pooled.
- **Unit destruction.** `DestroyUnit` removes the unit, its slots and modifiers and calls `ModifierEngine.ForgetOwner`, so sessions that churn thousands of units do not accumulate bookkeeping. Call `ForgetOwner(unitId)` yourself only for a standalone `ModifierEngine`; owners that still hold modifiers are kept. `ModifierEngine.TrackedOwnerCount` reports the count. `ModifierEngine.VersionOf` values come from one engine-wide counter: compare them only for equality; they read `0` once the owner is forgotten.
- **Scene units and threads.** Disabling a scene unit destroys its domain instance; enabling it creates a new one. Set its template, team override and level source before enable ([AbilityUnitBehaviour](./AbilityUnitBehaviour.md)). Keep your event delegates and unsubscribe them when the listener is disposed. The event bus and the domain are meant for one calling thread.

### Target observers and dispatch counters

| Call | Receives |
|------|----------|
| `Subscribe(eventId, handler)` | one event id, for any unit |
| `SubscribeAny(handler)` | every event |
| `SubscribeTarget(unitId, handler)` | every event whose **Target** is that unit; the Source does not count. `UnitId.None` can be subscribed. |

```csharp
System.Action<AbilityEventArgs> onGoblin = receipt =>
    Log(receipt.EventId, receipt.Target, receipt.Source, receipt.Amount);
system.Events.SubscribeTarget(goblin.Id, onGoblin);
// When the observer is disposed or this domain unit is replaced:
system.Events.UnsubscribeTarget(goblin.Id, onGoblin);
```

Dispatch order of one publication:

1. Handlers of that event id run first.
2. Matching Target and global observers are captured together, in their common registration order.
3. The system's internal modifier reactions run.
4. The captured observers run.

A handler from step 1 that changes observer subscriptions affects the current publication. Changes made later apply to future and nested publications only. A handler exception propagates and can stop later steps. `Events.Clear()` removes all user subscriptions (specific, global and Target) but keeps the internal reaction route; a standalone `new AbilityEventBus()` has only user subscriptions. Duplicate registrations stay distinct, and each unsubscribe removes the first match in its own scope. An event with `EventId == null` is ignored.

Scene unit wrappers use Target subscriptions, so a publication copies only the matching Target observers plus the global ones, not every unit wrapper. Warm publications allocate nothing; a registration allocates its record. The bus has read-only cumulative `long` counters of dispatch work (not CPU time or bytes):

| Counter | Counts |
|---------|--------|
| `ObserverSnapshotCount` | nonempty observer snapshots |
| `ObserverSnapshotEntryCount` | copied snapshot entries |
| `ObserverInvocationCount` | attempted global and Target calls, including ones that throw |

They include nested publications, exclude handlers of specific event ids and survive `Clear`. Compare them before and after a workload: with one matching unit wrapper and one global observer, each publication copies and invokes two observers whether 1 or 1000 units are registered.

## Extensibility

The property, state, event and effect-op registries are **open**: any string is valid. The constants `AbilityProperties`, `AbilityStates`, `AbilityEvents`, `AbilityEffectOps` and `AbilityDamageTypes` only standardize the names the built-ins read. Formulas and systems are swapped through `AbilitySystem.Rules` ([Rules and extension points](./Rules.md)).

**Custom effect op.** Implement `IEffectOperation` and register it once at startup; designers then use it in data:

```csharp
public sealed class HitSparksOp : IEffectOperation
{
    public string Id => "hit_sparks";

    public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (context.System.World.TryGetPosition(targets[i], out Vector3 pos))
                context.System.World.RequestSpawn(
                    new SpawnRequest("hit_sparks_fx", context.Caster, pos, default,
                        targets[i], context.AbilityId, node.Amount));
        }
    }
}

// once, after the system exists:
AbilitySystemBehaviour.I.System.Ops.Register(new HitSparksOp());
```

Nodes then set `OpId = "hit_sparks"`. Registering a built-in id again overrides it. Custom ops read `node.Amount`, `node.CustomParam` and the other node fields as they need. On hot paths:

- The `context` and its `CreateNested` children are valid only during `Execute` (see **Effect contexts** above). For a delayed effect, store the ids and values and build a fresh `EffectContext` later. Check `context.IsReleased` when unsure.
- `targets` is a scratch list owned by the system: read it, do not keep it.
- Keep `Execute` allocation-free when it runs per tick or per hit. Reuse scratch lists in fields; if the op can re-enter itself through reactions, keep one scratch per nesting level, as `ChainEffectOperation` does.

## Determinism

Every cast carries a seed: `CastRequest.Seed` when non-zero, otherwise one derived from the cast id and the caster. Chance rolls and combat randomness use `XorShiftRandom`, and the domain never reads `UnityEngine.Random` or `Time`. Seeds per tick and reaction, ordering rules, the deterministic profile and the state hash are in [Deterministic mode](./DeterministicMode.md). Replays recorded with an older package version may need recording again: see [Migration to 12](../Migration12.md).

## Performance for many units

The domain is built for survivor-scale combat: hundreds of enemies, projectiles and DoTs.

- **No allocation once warm:** casts (instant and projectile, with costs and capped areas), projectile hits, modifier ticks, reactions, event publication, `chain`, `dispel`, `remove_modifier`, hub radius queries, `AbilitySpatialGrid` operations and an `AbilityAutoCaster` pass.
- **Per-lifetime allocation only:** creating a unit, applying a new modifier instance (the owner's list is reused), subscribing a handler.
- **Spawns** of projectile, zone and summon prefabs go through `PoolManager`, outside that guarantee. Give those archetypes a pool with a suitable initial size.
- **Property values** are cached per unit; `GetProperty` is a dictionary lookup.
- **Ids** are case-insensitive everywhere. The domain hashes them with an ASCII fast path, because an `OrdinalIgnoreCase` hash costs about ten times an ordinal one in Mono. Prefer ordinal keys in your own per-frame lookups.

### Spatial queries for many units

Radius queries (area selectors, `chain` hops, projectile hit tests, auto-caster searches) go through `IAbilityWorldAdapter.QueryUnitsInRadius`. The scene hub scans every registered unit by default. For large scenes set **Spatial Cell Size** on [AbilitySystemBehaviour](./AbilitySystemBehaviour.md), about the typical query radius: the hub then keeps a per-frame `AbilitySpatialGrid` and each query visits only nearby cells. Knockback and kills update the grid in place; a newly registered unit triggers one rebuild. To use the same grid in your own world adapter, see [AbilitySystemBehaviour: Spatial grid in a custom adapter](./AbilitySystemBehaviour.md#spatial-grid-in-a-custom-adapter).

## Worked examples

A complete game assembled from these pieces is the [Survivor Demo](./SurvivorDemo.md). Author the examples below as assets held by an [AbilityLibrary](./AbilityLibrary.md); the values map one-to-one to the Inspector.

### Fireball

Projectile, area damage and a burn DoT.

- **Ability** `fireball`: Targeting `Point`, Delivery `Projectile`, Projectile Archetype Id `fireball_projectile`, Range `25`, Cost `{ mana, 50 }`, Cooldown `6`.
  - Impact Effect 1: `damage`, Target `AreaAroundTarget`, Team Filter `Enemies`, Radius `3`, Amount `120`, `magical`.
  - Impact Effect 2: `apply_modifier`, Target `AreaAroundTarget`, Team Filter `Enemies`, Radius `3`, Modifier Id `burn`.
- **Modifier** `burn`: Duration `4`, Tick Interval `1`, Is Debuff on. Tick Effect: `damage`, Target `Target`, Amount `15`, `magical`.
- On the hub, bind `fireball_projectile` to a prefab with [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md). On hit, enemies in the blast take 120 and burn for 15 per second over 4 s.

### Frost Nova

Area slow and a freeze state.

- **Ability** `frost_nova`: Targeting `NoTarget`, Delivery `Instant`, Cost `{ mana, 40 }`, Cooldown `8`.
  - Impact Effect 1: `damage`, Target `AreaAroundCaster`, Team Filter `Enemies`, Radius `5`, Amount `80`, `magical`.
  - Impact Effect 2: `apply_modifier`, Target `AreaAroundCaster`, Team Filter `Enemies`, Radius `5`, Modifier Id `frost_slow`.
- **Modifier** `frost_slow`: Duration `3`, Is Debuff on. Property `{ move_speed, Mul, 0.4 }` (60% slow). State `frozen`.
- Use `AreaAroundCaster`, not `Target`: with `NoTarget` the primary target is the caster. Your movement code reads `frozen` or `move_speed` for the actual slow.

### Magic Shield

A `shield_hp` pool with automatic absorption.

- **Modifier** `magic_shield`: Duration `10`. Property `{ shield_hp, Add, 200 }`.
- **Ability** `magic_shield_cast`: Targeting `Self` (or `Unit` with Team Filter `Allies`). Cast Effect: `apply_modifier`, Target `Caster`, Modifier Id `magic_shield`.
- The damage pipeline absorbs against every active `shield_hp` pool before HP. It fires `shield_absorbed` per hit, then `shield_broken` and removes the modifier when the pool is spent. Pure damage bypasses shields.
- For retaliation or heal-on-hit, add an **Event Reaction** on `take_damage` with **Target Event Source** set, so the effect targets the attacker.

### Speed Aura

- **Ability** `speed_aura`: Targeting `Self`. Cast Effect: `zone`, Target `Caster`, Team Filter `Allies`, Radius `8`, Interval `0.5`, Duration `0` (until removed or the caster dies), Modifier Id `speed_aura_buff`.
- **Modifier** `speed_aura_buff`: Stack Policy `Refresh`, Duration `0.75` (longer than the interval), Property `{ move_speed, Mul, 1.15 }`.
- Allies inside keep the buff refreshed; it fades about 0.75 s after they leave. `AbilityTemplates.Aura` builds a ready version ([templates](./TriggersAndHooks.md#templates)).

## Design notes

- **Cast results are thin; receipts are events.** `CastResult` holds only `Success`, `Failure` and `CastId`. The outcome of each effect is published on the event stream; correlate events by `CastId` (`0` outside a cast). `AbilityEventArgs` carries only ids and numbers, so it is safe to send over the network.
- **Two regeneration paths stack:** a pool's `RegenPerSecond` from the template is a flat baseline, and the `health_regen` / `mana_regen` properties can be buffed by modifiers ([UnitTemplate](./UnitTemplate.md)).

## По-русски

Основной C# API: `AbilitySystem`, `AbilityUnit`, `ModifierEngine` и `IEffectOperation`; компоненты сцены и NoCode
вызывают тот же API. Зарегистрируйте определения, создайте юнитов с пулами ресурсов, выдайте способности и продвигайте
время через `Tick(dt)`. Области, движение и снаряды требуют `IAbilityWorldAdapter`; в сцене это `AbilitySystemBehaviour`.
Для сотен юнитов включите **Spatial Cell Size** на хабе.

## See also

- [Core / Resources](../Core/README.md) — the `ResourcePoolModel` pools reuse
- [Merge](../Merge/README.md) — another pure-C# domain in the same style
- Back: [Docs index](../README.md)
