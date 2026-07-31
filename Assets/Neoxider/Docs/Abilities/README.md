# Abilities

`Neo.Abilities` is a Dota-derived, data-driven ability/modifier system. Abilities cast effects; **modifiers** are the single durable-effect concept — buffs, debuffs, DoTs, shields, and stuns all share one lifecycle. New content is authored in ScriptableObjects, without touching code.

- **Abilities** resolve, per cast, into a list of effect operations (damage, heal, apply modifier, spawn, …) through a validated cast pipeline (states, costs, cooldown/charges, target, range).
- **Modifiers** attach to a unit and, while active: contribute **properties** (`{ Add | Mul | Max }`, aggregated in a fixed `add -> mul -> max` order), declare **states** (boolean, any-true-wins), run **tick effects** on an interval, and run **declarative event reactions** (on an event id -> an effect list). Expiry is guaranteed.
- The domain is pure C# and deterministic (injected time and seeded PRNG). Everything observable flows through the **event stream**, which doubles as the receipt feed for UI, presentation, and network replication.

## Contents

**Authoring assets (ScriptableObjects)**

- [AbilityDefinition](./AbilityDefinition.md) — one ability (targeting, costs, cooldown, delivery, effects)
- [ModifierDefinition](./ModifierDefinition.md) — one buff/debuff/DoT/shield/aura payload
- [UnitTemplate](./UnitTemplate.md) — a unit archetype (team, pools, base properties, abilities)
- [AbilityLibrary](./AbilityLibrary.md) — a catalog that registers many definitions at once

**Scene components (MonoBehaviours)**

- [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) — the scene hub: owns and ticks the system, world adapter, spawner
- [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) — a unit's scene presence and receipt UnityEvents
- [AbilityCasterBehaviour](./AbilityCasterBehaviour.md) — the `TryCast*` API
- [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md) — a pooled data-driven projectile

**Editor tooling**

- [Ability Designer](./AbilityDesignerWindow.md) — UI Toolkit window (`Neoxider > Windows > Ability Designer`): library, phase board, live validation

**NoCode components (inspector-only wiring)**

- [AbilityNoCodeAction](./AbilityNoCodeAction.md) — cast/grant/level/modifier/damage/heal actions from any UnityEvent
- [AbilityAutoCaster](./AbilityAutoCaster.md) — Survivor-style auto-cast with nearest-target lock-on
- [AbilityCooldownSource](./AbilityCooldownSource.md) — cooldown values for `SetProgress` / `NoCodeBindText` bindings

Source: `Assets/Neoxider/Scripts/Abilities` (`Domain/` pure C#, `Data/` ScriptableObjects, `Components/` wrappers).

## Quick start (no code)

1. Create a [UnitTemplate](./UnitTemplate.md) with a `health` pool (and `mana` if it casts).
2. Create [AbilityDefinition](./AbilityDefinition.md) and [ModifierDefinition](./ModifierDefinition.md) assets, add them to an [AbilityLibrary](./AbilityLibrary.md).
3. In the scene, add an [AbilitySystemBehaviour](./AbilitySystemBehaviour.md), assign the library, and bind any projectile prefabs under **Archetypes**. (You can skip this — the hub auto-creates itself — but you then have nowhere to assign the library and archetypes.)
4. On your unit GameObject add an [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) (assign the template) and an [AbilityCasterBehaviour](./AbilityCasterBehaviour.md) (list the castable abilities).
5. Trigger casts without code, either way:
   - **Auto:** add an [AbilityAutoCaster](./AbilityAutoCaster.md) — every granted ability fires the moment it is ready, locking onto the nearest valid target (Survivor-style).
   - **On demand:** add an [AbilityNoCodeAction](./AbilityNoCodeAction.md) (e.g. on a UI button), pick `CastById`, set the ability id, and wire the button's `OnClick` to `Execute()`.
6. Show cooldowns with an [AbilityCooldownSource](./AbilityCooldownSource.md) + a `SetProgress` binding; unit receipts (`OnDamaged`, `OnDied`, …) are UnityEvents right on the [AbilityUnitBehaviour](./AbilityUnitBehaviour.md).

The minimum for a damageable unit is just an `AbilityUnitBehaviour` + a template; add the caster only when it casts. From code the same flow is `caster.TryCast("frost_nova")`, `TryCastAtUnit`, `TryCastAtPoint`, or `TryCastTowards` — see [AbilityCasterBehaviour](./AbilityCasterBehaviour.md).

## Core concepts

| Concept | Type | What it is |
|---------|------|-----------|
| System | `AbilitySystem` | The façade: units, catalogs, casting, effect execution, tick. One per world. |
| Unit | `AbilityUnit` | Identity + team + resource pools + base properties + cached property/state aggregation. |
| Property | `PropertyContribution` / `AbilityProperties` | Numeric stat, open string registry. Contributions combine `add -> mul -> max`. |
| State | `AbilityStates` | Boolean flag (stunned, silenced, invulnerable…), any-true-wins across modifiers. |
| Ability | `AbilityBlueprint` / `AbilityDefinition` | Per-cast logic: targeting, costs, cooldown, delivery, effect nodes. |
| Modifier | `ModifierBlueprint` / `ModifierDefinition` | Durable effect: properties + states + tick effects + event reactions. |
| Effect node | `EffectNodeData` | One authored step: which op, on whom, with which params. |
| Effect op | `IEffectOperation` / `AbilityEffectOps` | The strategy that runs a node. Open registry. |
| Event | `AbilityEventArgs` / `AbilityEvents` | Typed gameplay event; the receipt stream. |
| Damage | `DamageService` / `AbilityDamageTypes` | The one evasion -> crit -> mitigation -> shield -> HP -> death -> lifesteal pipeline. |
| World seam | `IAbilityWorldAdapter` | Positions, radius queries, unit motion (`TryMoveUnit`), spawns. The hub implements it; tests fake it. |

### Effect nodes

An `EffectNodeData` is one authored step, shared by abilities (cast/impact) and modifiers (tick/reaction):

| Field | Purpose |
|-------|---------|
| `OpId` | Operation id — see the ops table below. Default `damage`. |
| `Target` | `Target` (context primary), `Caster`, `AreaAroundTarget`, `AreaAroundCaster`. |
| `TeamFilter` | For area selectors: `Any` / `Enemies` / `Allies`, relative to the caster. |
| `Radius` | Area radius (world units) for the area selectors. |
| `MaxTargets` | Cap on affected units: area selectors keep the nearest N; `chain` hops up to N targets. `0` = unlimited (`chain` treats 0 as 4). |
| `Amount` | Damage / heal / resource delta / spawn magnitude / motion distance. |
| `DamageType` | `physical` (armor), `magical` (resist, blocked by `magic_immune`), `pure` (ignores mitigation and shields), or custom. |
| `ModifierId` | For `apply_modifier` / `remove_modifier`. |
| `ResourceId` | For `resource_change` (e.g. `mana`). |
| `ArchetypeId` | For `spawn` (host maps id -> prefab). |
| `CustomParam` | Free-form string for custom ops (and `dispel` uses `"buffs"`). |
| `Chance` | `[0..1]` probability the node runs, rolled on the cast's deterministic RNG. |

Built-in ops (`AbilityEffectOps`, registered by `DefaultEffectOps`):

| Op id | Effect |
|-------|--------|
| `damage` | Damage through the full pipeline; scaled by the caster's `spell_power`. |
| `heal` | Restore health honoring `healing_received_mul`; never revives. |
| `apply_modifier` | Attach the catalog modifier `ModifierId` to each target. |
| `remove_modifier` | Remove every instance of `ModifierId` from each target. |
| `dispel` | Remove dispellable debuffs (or buffs with `CustomParam = "buffs"`). |
| `resource_change` | Add / drain a resource pool (mana burn, energy gain…). |
| `spawn` | Ask the world adapter to spawn an archetype at the target(s)/point. |
| `knockback` | Push each target away from the caster (or from the target point, when set) by `Amount` world units. |
| `pull` | Drag each target toward the caster by `Amount` world units, never overshooting past the caster. |
| `teleport` | `Target = Caster` ⇒ blink the caster to the target point / first target; `Target = Target` ⇒ pull targets to the caster; `CustomParam = "swap"` ⇒ caster and first target trade positions. |
| `execute` | Health-derived damage: `Amount` is a fraction (0.1 = 10%) of the target's `"missing"` / `"max"` / `"current"` health per `CustomParam`, or a flat amount otherwise. Full damage pipeline, node's `DamageType`. |
| `chain` | Chain lightning: damages the first target, then hops to the nearest not-yet-hit unit within `Radius` (team-filtered), up to `MaxTargets` hits (0 ⇒ 4). Each bounce multiplies damage by the falloff parsed from `CustomParam` (e.g. `"0.85"`, default 1). Deterministic hop order. |

The motion family (`knockback` / `pull` / `teleport`) displaces units through the world adapter's `TryMoveUnit` seam — units with the `unmovable` or `invulnerable` state (or without a world presence) stay put. All op amounts, including motion distances and the `execute` fraction, resolve through the leveled-value fields below.

### Combat properties (crit / evasion / lifesteal)

`DamageService` reads these unit properties on every damage application; all default to 0, so they are inert until a template or modifier contributes them:

| Property | Read from | Meaning |
|----------|-----------|---------|
| `crit_chance` | attacker | Chance `[0..1]` of a critical hit — one roll per application, pre-mitigation. Fires `critical_hit`. |
| `crit_multiplier` | attacker | Full damage multiplier on crit (2 = double), floored at 1. |
| `evasion_chance` | victim | Chance `[0..1]` to fully evade **physical** damage, rolled pre-mitigation. Fires `evaded`. |
| `lifesteal_percent` | attacker | `0..100` — percent of dealt health damage returned to the attacker as healing (fires `heal_received`). Skipped for self-damage and dead attackers. |

Crit and evasion roll on the cast's deterministic RNG and only when one is supplied — effect ops, ticks, and reactions pass it; direct `DamageService.ApplyDamage` calls without an `IRandomSource` never crit or evade.

### Leveled values (per-level scaling)

Any effect `Amount` — plus an area `Radius` and a modifier's `Duration` — can scale with **level** and with a **caster/target property**, entirely in data (Dota's `AbilitySpecial`). Every new field defaults to "no effect": an all-default node resolves to exactly its flat `Amount`, so existing assets are byte-stable. Resolution is pure and deterministic (`LeveledValueResolver`): pick the driving level, pick the base (per-level array, else flat), then add the property term.

| Field (on `EffectNodeData`) | Purpose |
|-------|---------|
| `AmountByLevel` | Per-level amounts, e.g. `[5, 8, 12, 17]`. Empty ⇒ use `Amount`. |
| `AmountLevelSource` | Which level indexes the array: `None` (⇒ 1), `AbilityLevel`, `CasterUnitLevel`, `TargetUnitLevel`. |
| `AmountScaleProperty` | Optional property id added as a term (e.g. `spell_power`). `""` ⇒ none. |
| `AmountScalePerPoint` | Additive coefficient: `amount += AmountScalePerPoint * property`. |
| `AmountScaleSource` | Read the scaling property from the `Caster` or the `Target`. |
| `AmountKey` | Reference a named `AbilitySpecialValue` on the ability's `Specials` instead of the inline fields. |
| `RadiusByLevel` | Per-ability-level area radii. Empty ⇒ use `Radius`. |

- **Ability level** is the casting `AbilitySlot.Level` — raise it with `AbilitySystem.SetAbilityLevel(unit, abilityId, level)` (clamped to ≥ 1). It is **captured onto a modifier at apply time**, so a DoT keeps ticking at the level it was applied at even after the slot later levels up (re-applying re-captures the current level).
- **Unit level** is `AbilityUnit.Level`: bridge it from a `Neo.Core.Level.LevelComponent` assigned on an [AbilityUnitBehaviour](./AbilityUnitBehaviour.md), set it via [UnitTemplate](./UnitTemplate.md) **Start Level**, or call `unit.SetLevel(...)`.
- **Named specials** (`AbilityBlueprint.Specials`, a list of `{ Name, LeveledValue }`) let several nodes share one leveled array via `AmountKey` — e.g. a damage node and a scaled slow that both read `damage`.
- **Modifiers** add `DurationByLevel`, resolved by the captured ability level.

Example — an ember bolt dealing `[50, 90, 140]` per ability level plus `0.4 × spell_power`: on the `damage` node set `AmountByLevel = [50, 90, 140]`, `AmountLevelSource = AbilityLevel`, `AmountScaleProperty = spell_power`, `AmountScalePerPoint = 0.4`, `AmountScaleSource = Caster`.

## Code-first usage

### Create a system headless

The domain runs with no Unity objects — ideal for tests and servers. Add resource pools with `ResourcePoolModel.AddPool` (from `Neo.Core.Resources`).

```csharp
using Neo.Abilities;
using Neo.Core.Resources;

var system = new AbilitySystem();

// Author an ability entirely in code (data normally lives in an AbilityDefinition asset).
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

Area effects and projectiles need world positions, so they only resolve with a world adapter installed. In a scene the [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) is that adapter; headless tests provide their own `IAbilityWorldAdapter`.

## Extensibility

The property, state, event, and effect-op registries are all **open** — any string is valid, and the constants (`AbilityProperties`, `AbilityStates`, `AbilityEvents`, `AbilityEffectOps`, `AbilityDamageTypes`) only standardize the names the built-ins read.

**Custom effect op.** Implement `IEffectOperation` and register it once at startup; designers then compose it in data forever:

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

Nodes then set `OpId = "hit_sparks"`. Re-registering a built-in id overrides it. Custom ops read `node.Amount`, `node.CustomParam`, and the other node fields as they see fit.

## Determinism

Every cast carries a seed. `CastRequest.Seed` (when non-zero) is used directly; otherwise the system derives one from the cast id and caster. All chance rolls (`EffectNodeData.Chance`) and any op randomness draw from a seeded `XorShiftRandom` (`IRandomSource`) — same seed, same sequence, on every platform. The domain never touches `UnityEngine.Random` or `Time`, so replays, server authority, and edit-mode tests are reproducible. Pass an explicit seed when you need lockstep or recorded replays.

## Worked examples

For a complete playable game assembled from these pieces (units, auto-cast abilities, slow/DoT modifiers, homing projectiles, area effects, level-up upgrades), see the [Survivor Demo](./SurvivorDemo.md).

Author these as assets (an [AbilityLibrary](./AbilityLibrary.md) holds them); the field values below map one-to-one to the inspector.

### Fireball

Projectile + area damage + burn DoT.

- **Ability** `fireball`: Targeting `Point`, Delivery `Projectile`, Projectile Archetype Id `fireball_projectile`, Range `25`, one Cost `{ mana, 50 }`, Cooldown `6`.
  - Impact Effect 1: `damage`, Target `AreaAroundTarget`, Team Filter `Enemies`, Radius `3`, Amount `120`, `magical`.
  - Impact Effect 2: `apply_modifier`, Target `AreaAroundTarget`, Team Filter `Enemies`, Radius `3`, Modifier Id `burn`.
- **Modifier** `burn`: Duration `4`, Tick Interval `1`, Is Debuff on. Tick Effect: `damage`, Target `Target`, Amount `15`, `magical`.
- Bind `fireball_projectile` -> your projectile prefab (with [AbilityProjectileBehaviour](./AbilityProjectileBehaviour.md)) on the hub. On hit, the projectile detonates and both impact nodes resolve; enemies in the blast take 120 and start burning for 15/s over 4s.

### Frost Nova

Area slow + freeze state.

- **Ability** `frost_nova`: Targeting `NoTarget`, Delivery `Instant`, one Cost `{ mana, 40 }`, Cooldown `8`.
  - Impact Effect 1: `damage`, Target `AreaAroundCaster`, Team Filter `Enemies`, Radius `5`, Amount `80`, `magical`.
  - Impact Effect 2: `apply_modifier`, Target `AreaAroundCaster`, Team Filter `Enemies`, Radius `5`, Modifier Id `frost_slow`.
- **Modifier** `frost_slow`: Duration `3`, Is Debuff on. Property `{ move_speed, Mul, 0.4 }` (60% slow). State `frozen`.
- Use `AreaAroundCaster` (not `Target`) — for `NoTarget` the primary target defaults to the caster, so a `Target` node would hit the caster. Your movement code reads the `frozen` state / `move_speed` for the actual slow.

### Magic Shield

A `shield_hp` pool with automatic absorption.

- **Modifier** `magic_shield`: Duration `10`. Property `{ shield_hp, Add, 200 }`. Dispellable as you like.
- **Ability** `magic_shield_cast`: Targeting `Self` (or `Unit`, Team Filter `Allies`), Cast Effect: `apply_modifier`, Target `Caster`, Modifier Id `magic_shield`.
- The damage pipeline absorbs against every active `shield_hp` pool **automatically** (before HP), firing `shield_absorbed` per hit and `shield_broken` + removing the modifier when the pool is spent. Pure damage bypasses shields.
- To add reactive behavior (retaliate, heal on hit), give the modifier an **Event Reaction** on `take_damage` with `Target Event Source` set so the effect targets the attacker.

### Speed Aura (idea)

There is no dedicated aura component; compose one from existing pieces. Put a **permanent** modifier `speed_aura_source` on the aura carrier (Duration `0`) with Tick Interval `0.5` and a tick effect `apply_modifier`, Target `AreaAroundCaster`, Team Filter `Allies`, Radius `8`, Modifier Id `speed_aura_buff`. Make `speed_aura_buff` a short `Refresh` modifier (Duration `0.75` > the tick interval) with `{ move_speed, Mul, 1.15 }`. Allies in range keep the buff refreshed; it fades ~0.75s after they leave.

## Design notes

Observations from documenting the current implementation (useful if the API evolves):

- **Cast results are thin; receipts are events.** `CastResult` returns only success + `CastId`. The rich per-effect outcome (`DamageResult` with mitigated/absorbed/health/killed/crit) is produced inside `DamageService` but is not surfaced from `Cast` — the observable channel is the `AbilityEventBus` stream. Consumers that need "what did this cast do" correlate events by `CastId` (carried on `AbilityEventArgs`; `0` outside a cast). A dedicated `CastReceipt` was described in the architecture but is not present.
- **Two regen paths, one flat and one buffable.** `UnitTemplate` pools with `RegenPerSecond > 0` regenerate through `ResourcePoolModel` itself — `ApplyTo` sets a fixed 0.1 s regen interval, and `AbilitySystem.Tick` ticks the pools via `unit.Resources.Tick`. Separately, the `health_regen` / `mana_regen` properties are applied per second in the same tick, and since they are properties, modifiers can contribute to them. The two stack: the template field is a flat archetype baseline, the properties are the scalable/buffable path.
- **`AbilityEventArgs` carries only ids and numbers, no reference payload** (by design, for network safety) — correlate on `CastId` / unit ids.
- **Area / projectile effects silently no-op without a world adapter or scene presence.** This is correct for the pure domain, but it means a headless test must install an `IAbilityWorldAdapter` to exercise them; single-target and self effects work without one.

## See also

- [Core / Resources](../Core/README.md) — the `ResourcePoolModel` pools reuse
- [Merge](../Merge/README.md) — another pure-C# domain in the same style
- Back: [Docs index](../README.md)
