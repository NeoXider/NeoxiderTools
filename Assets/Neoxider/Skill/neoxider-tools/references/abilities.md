# Neo.Abilities — data-driven combat (v10 headliner)

`Neo.Abilities` (namespace + asmdef `Neo.Abilities`) is a Dota-derived, data-driven ability/modifier
system: new abilities are authored **entirely in ScriptableObjects**, no code. It **supersedes `Neo.Rpg`**
— for any NEW combat (damage, buffs/debuffs, DoTs, shields, stuns, projectiles, auras, crit/lifesteal)
build on Abilities; touch `Neo.Rpg` only when maintaining an existing Rpg scene (it is slated for removal).
Docs: `Assets/Neoxider/Docs/Abilities/` (README + per-type pages). Source: `Assets/Neoxider/Scripts/Abilities/`
(`Domain/` pure C#, `Data/` ScriptableObjects, `Components/` Unity wrappers, `Bridge/` NoCode).

## The pieces

**Authoring assets (ScriptableObjects)** — create via `Create -> Neoxider -> Abilities`:

| Asset | What it authors |
|---|---|
| `AbilityDefinition` | One ability: targeting (`NoTarget/Self/Unit/Point/Direction`), team filter, costs, cooldown/charges, range, delivery (Instant / homing Projectile + archetype id), cast + impact effect nodes, named `Specials` |
| `ModifierDefinition` | One durable effect — buff/debuff/DoT/shield/stun are ALL modifiers: duration, stack policy (`Independent/Refresh/Stack` + per-stack scaling), property contributions, boolean states, interval tick effects, declarative event reactions (e.g. absorb/retaliate on `take_damage`) |
| `UnitTemplate` | Unit archetype: team, resource pools (`health`, `mana`, …), base properties, granted abilities, start level |
| `AbilityLibrary` | Catalog registering many definitions at once (assign on the hub) |

**Scene components** (`Neoxider/Abilities/...` in Add Component):

| Component | Role / key API |
|---|---|
| `AbilitySystemBehaviour` | Scene hub, singleton `AbilitySystemBehaviour.I` (auto-creates; add manually to assign **Libraries** + **Archetypes**). `AbilitySystem System` (domain entry: `Cast`, `Events`, `GrantAbility`, `SetAbilityLevel`, `Ops`), `bool Paused` (freeze tick for menus/level-up), `AddLibrary(lib)`, `AddArchetype(id, prefab)`, `GetBehaviour(UnitId)`, `RegisteredUnitCount`. **Spatial Cell Size** / `float SpatialCellSize` (0 = exact linear scan; set it for many units) + `InvalidateSpatialCache()` (after teleporting units by script when same-frame queries must see it). Implements `IAbilityWorldAdapter` (positions, radius queries, `TryMoveUnit`, spawns via `PoolManager`) |
| `AbilityUnitBehaviour` | A unit's scene presence. Assign **Template**; `AbilityUnit Unit` (domain: `GetProperty`, `HasState`, `Resources`, `SetLevel`), `UnitId UnitId`, `CurrentHealth/MaxHealth/HealthNormalized/IsAlive`, `ApplyDamage(float)` (pure, no source), `ApplyHeal(float)`, `SetTemplate(...)`/`SetTeamOverride(...)` for pooled spawning. Receipt UnityEvents: `OnDamaged<float>`, `OnHealed<float>`, `OnDied`, `OnModifierApplied/Removed<string>`, `OnAbilityCast<string>` |
| `AbilityCasterBehaviour` | The cast API (`[RequireComponent(AbilityUnitBehaviour)]`). `TryCast(id)`, `TryCastAtUnit(id, unit)`, `TryCastAtPoint(id, point)`, `TryCastTowards(id, dir)` — pick the one matching the ability's Targeting; `CastResult Cast(CastRequest)` for a custom seed (the request's caster is replaced by this unit); `CastResult LastCastResult` (typed `Success`/`Failure`/`CastId` of the latest call); `GetCooldownNormalized(id)` (1 = just used, 0 = ready); events `OnCastSuccess/OnCastFailed<string>` (`CastFailureReason` name) |
| `AbilityProjectileBehaviour` | Pooled data-driven homing projectile; bind its prefab to the archetype id under the hub's **Archetypes**. `IsFlying`, `CastId`; releases its pending cast when disabled or destroyed and returns to the pool when the hub is gone |

Units exist only between `OnEnable`/`OnDisable`. The minimum damageable unit = `AbilityUnitBehaviour` +
template; add the caster only when it casts. Grant-at-runtime goes through
`AbilitySystemBehaviour.I.System.GrantAbility(unitId, abilityId)` (the caster's list only grants on enable).

## Effect ops (open registry, `AbilityEffectOps`)

Each effect node: `OpId`, `Target` (`Target/Caster/AreaAroundTarget/AreaAroundCaster`), `TeamFilter`,
`Radius`, `MaxTargets`, `Amount`, `DamageType` (`physical`/`magical`/`pure`), `Chance`, op-specific ids.

`damage`, `heal`, `apply_modifier`, `remove_modifier`, `dispel`, `resource_change`, `spawn`, plus motion/
utility atoms: `knockback` (push from caster), `pull` (drag toward caster), `teleport` (blink / pull-to-caster /
`CustomParam="swap"`), `execute` (Amount = fraction of `missing`/`max`/`current` HP per `CustomParam`),
`chain` (nearest-first bounces, `MaxTargets` hops, falloff in `CustomParam`, deterministic). Motion routes
through the world adapter's `TryMoveUnit` seam; `unmovable`/`invulnerable` units stay put. Custom op:
implement `IEffectOperation`, `AbilitySystemBehaviour.I.System.Ops.Register(new MyOp())` once — then it's
authorable in data (see the lifetime contract below).

**Live combat properties** read by `DamageService` at damage time (default 0 — contribute via template or
modifier, no wiring): `crit_chance`/`crit_multiplier` (attacker), `evasion_chance` (victim, physical only),
`lifesteal_percent` (attacker, 0..100). Shields: a `shield_hp` property contribution absorbs automatically
before HP (pure damage bypasses).

**Leveled values**: every `Amount` (plus `Radius`, modifier `Duration`) can scale per level and per property
in data — `AmountByLevel[]`, `AmountLevelSource` (`AbilityLevel`/`CasterUnitLevel`/`TargetUnitLevel`),
`AmountScaleProperty` + `AmountScalePerPoint` + `AmountScaleSource`, or `AmountKey` referencing a named
`Specials` entry shared by several nodes. Raise ability level via `System.SetAbilityLevel(unit, id, level)`;
unit level bridges from `Neo.Core.Level.LevelComponent` or `unit.SetLevel(...)`.

**Everything else scales too (12.3)** — arrays sampled as `array[clamp(level - 1)]`, empty = flat field, all off by
default (old content and fixed-core hashes unchanged):
- Cast parameters by ability level: `CooldownByLevel` (10/8/6), `RangeByLevel`, `MaxChargesByLevel`,
  `ChargeRestoreTimeByLevel`, `ProjectileSpeedByLevel`; read with `blueprint.ResolveCooldown(level)` etc. or
  `slot.MaxCharges/UsesCharges/Range`. A level change keeps a running cooldown; a raised charge cap grants charges.
- Costs: `AbilityCost.AmountByLevel`, `AmountKey` (special, e.g. `ScaleStat = ResourceMax`, `ScalePerPoint = 0.1`
  = 10% of max mana), `Mode` `Fixed`/`UpTo`/`All` + `MinAmount`
  (`new AbilityCost(AbilityResourceIds.Mana, 0f, AbilityCostMode.All, 1f)` = spend all mana).
- Spent term: node `AmountSpentResource = "mana"`, `AmountSpentCoefficient = 2` ⇒ damage = 2 × mana actually paid
  (also `LeveledValue.SpentResource/SpentCoefficient`); travels to projectile hits and to DoT ticks of modifiers whose
  nodes use it; not to zone pulses. Code: `context.GetSpent(id)`, `modifierInstance.GetSpent(id)`.
- Scale term: `AmountScaleMode = Multiply` (`value *= 1 + k * stat`, applied last) and `AmountScaleStat`
  `Property`/`ResourceCurrent`/`ResourceMax`/`ResourceMissing` (e.g. 5% of the target's missing health).
- Modifiers: `PropertyContribution.ValueByLevel` (slow `Mul [0.8, 0.7, 0.6]` = 20/30/40%), `DurationByLevel`
  (stun 1/1.5/2 s); nodes: `ChanceByLevel`, `MaxTargetsByLevel`, zone `DurationByLevel`/`IntervalByLevel`.
- Full table: `Docs/Abilities/AbilityDefinition.md#leveled-and-scalable-values`; the converter maps every field to
  the fixed core.

## Code-first usage

```csharp
using Neo.Abilities;

// Scene flow: hub + assets already set up in the Inspector.
[SerializeField] private AbilityCasterBehaviour caster;
[SerializeField] private AbilityUnitBehaviour target;

if (!caster.TryCastAtUnit("firebolt", target))
{
    CastFailureReason why = caster.LastCastResult.Failure;   // OnCooldown, OutOfRange, NotEnoughResources, ...
}
cooldownImage.fillAmount = caster.GetCooldownNormalized("firebolt");

// Receipts: per-unit UnityEvents, or the global stream:
AbilitySystem system = AbilitySystemBehaviour.I.System;
system.Events.Subscribe(AbilityEvents.TakeDamage, e => SpawnHitNumber(e.Target, e.Amount));
system.Events.SubscribeAny(e => Log(e.EventId, e.Target, e.Amount));   // all events

// Runtime grants / leveling (upgrade cards etc.):
system.GrantAbility(unit.UnitId, "ember");
system.SetAbilityLevel(unit.UnitId, "ember", 2);
```

The domain is pure C# and deterministic (seeded `XorShiftRandom`, injected time — never
`UnityEngine.Random`/`Time`): it runs headless for tests/servers via `new AbilitySystem()` +
`CreateUnit`/`RegisterAbility`/`Cast(CastRequest.AtUnit(...))`/`Tick(dt)`. Area/projectile effects need a
world adapter (the hub in scenes; a fake `IAbilityWorldAdapter` headless). Resource pools reuse
`Neo.Core.Resources.ResourcePoolModel` (`AddPool`, `SetMax`, `Restore`; `SetCurrent(id, value)` is the
direct clamped setter for loads/revives that bypasses the heal gate).

## Performance and lifetime contracts (12.0.0)

- **Allocation-free once warm:** casts (instant and projectile delivery, including costs), projectile hits
  and releases, modifier ticks, event reactions, event publication, `chain`, `dispel`, `remove_modifier`.
  Remaining allocation is per lifetime (creating a unit, applying a new modifier instance, subscribing).
  Scene spawns of projectile/zone/summon prefabs go through `PoolManager` — give archetype prefabs a pool
  with a suitable initial size.
- **Many units:** set **Spatial Cell Size** (about the typical query radius, e.g. 3) so radius queries,
  projectile hit tests, area effects and the auto-caster search visit only nearby cells (results identical
  to the linear scan). Custom world adapters reuse `AbilitySpatialGrid` (`new AbilitySpatialGrid(cellSize)`,
  `Clear()`, `Add(unitId, position)`, `Query(point, radius, results)` — insertion order, no allocation when
  warm). Example adapter: `Docs/Abilities/README.md` → "Spatial queries for many units".
- **Pooled `EffectContext`:** contexts the system creates for casts, projectile hits, modifier ticks and
  reactions are recycled as soon as their effects finish. A custom `IEffectOperation` must not keep the
  context, its `PrimaryTargets`, its `Random` or a `CreateNested` child after `Execute` returns — copy the
  values and build a new `EffectContext` for delayed/coroutine effects. `IsReleased` reports an expired
  context; `ExecuteEffects` ignores it with a gated warning. Keep `Execute` allocation-free.
- **Determinism:** every modifier tick and event reaction rolls with its own derived seed (a 50% DoT node
  fires on about half the ticks). Replays recorded before 12.0.0 diverge after a modifier's second
  tick/reaction — re-record them.
- `ModifierEngine.VersionOf` values come from one engine-wide counter: compare them for equality only;
  they read 0 after `DestroyUnit` (which now forgets the unit's bookkeeping).
- `AbilityEventBus.Publish` ignores an event without an id (it used to throw).

## Reactive layer and replaceable rules (12.1)

Read `Docs/Abilities/TriggersAndHooks.md` and `Docs/Abilities/Rules.md` before hand-rolling any of this.

- **Damage hooks:** every hit runs `before_deal_damage` (attacker) then `before_take_damage` (victim). In a handler
  read/adjust `system.ActiveDamage` (`Multiplier`, `FlatAdd`, `Absorb`, `ReflectFraction`, `Cancel()`,
  `DamageType`); in data use reactions with ops `modify_damage` (`CustomParam` multiply/add/absorb/cancel) and
  `reflect`. Optional receipts (hooks, `attack_landed`/`attack_received`) are only produced for a handler of that
  exact event id, a reacting modifier or the trace — `SubscribeAny`/`SubscribeTarget` alone do not request them.
- **Triggers on `ModifierEventReaction`:** `Trigger` = Event / EveryNth (`EveryN`) / HealthBelow (`Threshold`,
  edge on `take_damage`) / StateEnter / StateExit (`StateId`) / Applied / Removed; filters `TagFilter`,
  `AbilityFilter`, `HitsOnly`; `MaxTriggers` + `RemoveWhenSpent` (charges, "next N hits"), `Cooldown` (internal
  cooldown), `Priority`. `attack_landed` = direct hit of a cast/projectile (DoT ticks do not count).
  Animation moments: `system.RaiseAnimationEvent(unit, "hit_frame")` → reactions on `animation_event` + TagFilter.
- **Payload ops:** `projectile` / `area_apply` / `zone` / `cast_ability` take `EffectNodeData.AbilityId` (a
  payload ability; its Impact effects run). Selector `ConeFromCaster` + `Angle`. Zones = auras (selector Caster,
  follows) and formations (point): `Radius`, `Duration`, `Interval`, `ModifierId`, `ArchetypeId` visual with
  `AbilityZoneView`. Code: `system.SpawnZone(new AbilityZoneSpec { ... })`.
- **Stack policies:** + `Unique`, `Prolong`, `IndependentCapped`. `AmountPerStack` scales tick/reaction amounts.
  `RestartTicksOnRefresh` = a refresh / stack / prolong restarts the tick phase (both cores).
- **Templates first:** `AbilityTemplates.AutoCastTechnique/AttackEnhancer/AutoDefenseShield/Mark/StackingPoison/
  Thorns/Aura/FormationZone(...).SetUp(system, unitId)` — tune the returned blueprints instead of writing ops.
- **Rules (never fork the package):** `system.Rules.DamageFormula / Mitigation / CriticalHit / Evasion /
  Stacking / Aggregator (AdditivePercentPropertyAggregator) / Targeting / Cooldowns / Costs / Time
  (FixedStepTimeSource + system.Advance / StepTicks) / Numeric (FixedPointNumeric.Q8) / Random
  (IRandomSourceFactory)`. Implementations must not allocate per call; `null` restores the default.
- **Debug "why did this fire":** `system.Trace.Enabled = true;` then
  `system.Trace.Explain(entry.Sequence)` (find entries with `TryFindLatest(AbilityTraceKind.Reaction, id, ...)`;
  `Explain(seq, 12, id => name)` prints unit names). Shield bar: `DamageService.RemainingShield(instance)`.
- **Passives on a template:** `UnitTemplate` **Innate Modifiers** (thorns, auto-defense) are applied on
  registration with the unit as caster. 2D zone visuals: `AbilityZoneView` **Plane = ScreenXY**.
  `AbilityContentValidator.Validate(abilities, modifiers, system.Ops)` lists data mistakes.

## Deterministic games: the fixed core (12.1)

- **Float game, replays / server authority:** `DeterministicAbilityProfile.Apply(system.Rules, 30)` + compare
  `AbilityStateHasher.Compute(system)`; same runtime only.
- **No floats / no UnityEngine in the domain (lockstep):** assembly `Neo.Abilities.Fixed` (namespace
  `Neo.Abilities.Fixed`, engine-free). `FixedAbilitySystem` with raw Q8.8 ints (`FixedFormat.Q8`, one = 256) and
  ticks: `CreateUnitWithId(entityId, team)`, `unit.AddPool(FixedResources.Health, raw)`, `unit.Position = FixedVec3`,
  `Cast(FixedCastRequest.AtUnit(...))`, `Step()` per tick, `FixedStateHasher.Compute(system)` for desync checks.
- **Content once:** author float assets/templates, convert with
  `FixedContentConverter.Convert(library | blueprints, new FixedConversionSettings { TicksPerSecond = 30 }, issues)
  .RegisterInto(system)`; fix every Warning in `issues` (times off the tick grid, fractional angles, motion ops).
- **Game rules:** `FixedRules` (`Mitigation`, `Aggregator`, `Stacking`, `CriticalHit`, `Evasion`, `DamageFormula`,
  `Targeting`, `Cooldowns`, `Costs`, `Random`); exact ratio multipliers via
  `FixedPropertyContribution.Denominator`; `FixedModifierBlueprint.StackingKind` for custom stacking.
  Math: `FixedMath.MulDiv` (128-bit, half away from zero), never `float` in that layer.

## NoCode trio (designer wiring — see avoid-nocode.md)

`AbilityNoCodeAction` (enum-driven `Execute()` bridge: cast/grant/revoke/level/modifier/damage/heal from
UnityEvents), `AbilityAutoCaster` (auto-cast every ready ability with nearest-target lock-on, `WhenReady`
or `Interval` mode, failure backoff — real logic, fine to reuse from code-first survivor-style games
instead of hand-rolling a fire-when-ready loop), `AbilityCooldownSource` (poll-friendly
`CooldownNormalized`/`ReadyNormalized`/`SecondsRemaining`/`IsReady` for `SetProgress`/`NoCodeBindText`).
When writing C#, call `TryCast*`/`GetCooldownNormalized`/`LastCastResult` directly instead of the
action/source bridges.

## Survivor kit — the reference implementation

`Assets/NeoxiderDemos/Demo` ships **SurvivorDemo.unity**: a complete Vampire-Survivors game on top of Neo.Abilities +
Core resources (player/enemy `UnitTemplate`s, auto-cast weapon `AbilityDefinition`s and slow/DoT/stat
`ModifierDefinition`s are assets). The saved scene is a NoCode game with no game
scripts: `KeyboardMover` + `AbilityUnitBehaviour` + `AbilityAutoCaster` + 2D kinematic `MagneticField` on the
player; `Spawner` (ring around the player, weights, unlock times, delay ramp, cap) cloning enemy templates with
`Follow`, a bite ability, `Loot` and `Despawner`; `LevelComponent.OnLevelUp` opening a
`ChanceSystemBehaviour.GenerateUnique` card draft whose buttons run `AbilityNoCodeAction`; `TimerObject` wins,
`SceneFlowController` pauses and restarts. **Clip a similar game by copying the scene, authoring new data assets and re-wiring
the components in the Inspector.** Read `Docs/Abilities/SurvivorDemo.md` before building any survivor-like/auto-battler game; copy
its idioms (enemy-as-presentation: `Follow` steers at the unit's `OnMoveSpeedChanged` speed, the ability domain owns health/combat;
config-driven setup; upgrade kinds PermanentModifier/GrantAbility/MaxHealth).

## Pitfalls

- **Spawns/projectiles need an archetype binding** on the hub (`ProjectileArchetypeId`/`ArchetypeId` ->
  prefab) or `RequestSpawn` silently no-ops and impact never happens.
- **`NoTarget` abilities**: the primary target defaults to the caster — use `AreaAroundCaster` (not
  `Target`) for the nuke-around-self pattern, or the caster nukes itself.
- **One hub per world**; a second `AbilitySystemBehaviour` destroys itself.
- With **Auto Tick** off (or `Paused = true`) nothing advances — cooldowns/DoTs freeze until
  `System.Tick(dt)`.
- `AbilityUnitBehaviour.ApplyDamage` is `pure` damage with no source (bypasses armor/resist, no kill
  credit) — route real combat through abilities.
- Regen has two paths: `UnitTemplate.RegenPerSecond` ticks the pool at a fixed rate (works since the
  v10 fix — ignore older docs calling it inert), while the `health_regen`/`mana_regen` **properties** are
  applied per second by `System.Tick` and can be contributed by modifiers. Use the properties when regen
  must scale or be buffable.
