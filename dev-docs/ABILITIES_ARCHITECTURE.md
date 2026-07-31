# Neo.Abilities — Dota-derived ability/modifier system (v10)

Status: implementation baseline for the v10 replacement of `Neo.Rpg`.
Idea source: Dota 2 ability/modifier architecture, adapted from the CoreAI spell-generator plan
(`D:\Git\CoreAI\Docs\SPELL_GENERATOR_ARCHITECTURE.md` §5A) — **gameplay core only**: no Lua, no LLM,
no sandbox. Goal per package identity: a universal ready-made module you plug in and build a game on;
new abilities are authored **in data, without touching code**; NoCode and multiplayer stay optional layers.

## 1. What replaces what

| Old (removed in v10) | New |
|---|---|
| `Neo.Rpg` (RpgCharacter, RpgStatRuntime, BuffDefinition, StatusEffectDefinition, RpgAttackController, RpgProjectile, RpgCombatMath…) | `Neo.Abilities` module (`Scripts/Abilities`, asmdef `Neo.Abilities`) |
| `RpgStatId` / closed stat enums | extensible string-keyed **Property registry** |
| Buffs + statuses as two systems | one **Modifier** concept (buff/debuff/aura/DoT/channel/shield) |
| Per-attack C# controllers | data-driven **AbilityDefinition** cast pipeline |
| `Tools/AttackSystem` Rpg bridges | thin adapters onto `Neo.Abilities` |
| NPC `NpcRpgCombatBrain` | `NpcAbilityBrain` casting abilities via the same pipeline |

Kept and reused (already clean pure-C# domains): `Neo.Core.Resources.ResourcePoolModel` (HP/mana pools),
`Neo.Core.Level` (`LevelModel`, curves) — the unit binds them; ability costs spend through the pool model.
`HealthComponent` stays as the scene-facing HP wrapper and becomes a view over the unit's `health` pool.

## 2. Layer architecture (package rules: testable core → Unity wrappers → optional NoCode/demo)

```text
Neo.Abilities (asmdef, runtime)
  Domain/          pure C#, no UnityEngine (like Neo.Merge, DiceBoard)
    Units/         AbilityUnit, UnitId, TeamId, unit registry
    Properties/    PropertyId, PropertyContribution {Add|Mul|Max}, PropertyAggregator (add→mul→max, deterministic)
    States/        StateId, StateAggregator (any-true-wins)
    Modifiers/     ModifierInstance, ModifierEngine (durations, interval ticks, stack policies, guaranteed expiry)
    Events/        AbilityEventBus: typed gameplay events (TakeDamage, DealDamage, AttackLanded, Death, Kill,
                   AbilityCast, ModifierApplied/Removed/Expired, HealReceived, ProjectileLaunched/Hit…)
    Effects/       EffectOp descriptors (versioned registry): damage, heal, apply_modifier, remove_modifier,
                   resource_change, displace, teleport, spawn_entity, projectile, area_query, custom…
                   Each op: Validate(ctx) → Execute(world) → typed Receipt
    Casting/       CastPipeline: request → validate (states/costs/cooldown/charges/range/target filter)
                   → phases Prepare/Channel/Release → delivery → impact effects → receipts
                   CooldownModel, ChargesModel, deterministic seed per cast
    Targeting/     TargetingMode: None/Self/Unit/Point/Direction/Area; team filters (ally/enemy/any),
                   ITargetQueryAdapter for world queries
    Receipts/      CastReceipt, DamageReceipt, ModifierReceipt… — the ONLY source for presentation and network
    IAbilityWorldAdapter  host seam (positions, spawn, physics queries, time) — fake in tests
  Data/            ScriptableObjects (authoring; zero scene refs — package rule)
    AbilityDefinition      targeting, delivery, costs, cooldown/charges, cast/channel times,
                           effect nodes per phase (trigger: cast/release/travel/impact/tick/expire/interrupt),
                           presentation cues (ids into presentation layer)
    ModifierDefinition     duration, stack policy (Independent/Refresh/Stack+max), property contributions,
                           states, tick interval + tick effect nodes, event reactions (declarative:
                           on TakeDamage → effect list), presentation cues
    UnitTemplate           base property values, resource pools, team, level growth (LevelCurveDefinition)
    PropertyCatalog        optional SO listing project properties for editor dropdowns (registry stays open)
  Components/      MonoBehaviour wrappers
    AbilityUnitBehaviour   hosts AbilityUnit; bridges ResourcePoolModel/HealthComponent/LevelComponent
    AbilityCasterBehaviour cast API + input binding; exposes UnityEvents fed from receipts
    AbilityProjectileBehaviour  pooled via PoolManager; data-configured (speed/homing/pierce)
    AbilityAreaBehaviour   zones/auras (property_field)
    SummonBehaviour        owner id, caps, lifetime lease
    AbilityPresentationBehaviour  receipt → VFX/SFX/AnimationFly/AM hooks (no gameplay authority)
  Bridge/          optional NoCode bridge (AbilityNoCodeAction) — thin, wraps typed API (package rule)
Neo.Abilities.Editor (asmdef, editor)
  UITK Ability Designer window + SO inspectors — see §5
Neo.Network bridge (inside existing Neo.Network, #if MIRROR)
  NetworkAbilityAuthority: server validates casts, replicates receipts — see §4
Tests: Assets/Neoxider/Tests/Edit/Abilities (domain, deterministic), Play (scene wrappers)
Demo:  Samples/Demo/Scenes/Abilities/AbilitiesNoCodeDemo.unity (NoCode caster/auto-cast/cooldown showcase)
       + Samples/Demo/Scenes/SurvivorDemo.unity (full playable game on the system)
```

## 3. Core model decisions (Dota-derived, improved)

1. **Ability vs Modifier split.** Ability = per-cast logic resolved by the cast pipeline into effect ops.
   Modifier = durable effect object attached to a unit with lifecycle (OnApplied/OnTick/OnRemoved) and
   guaranteed expiry. All buffs/debuffs/DoTs/auras/shields/stuns are modifiers.
2. **Properties.** `{propertyId, op: Add|Mul|Max, value}` contributions aggregated per property in fixed
   order add→mul→max. Registry is **open** (string ids + optional catalog SO), not a closed enum — better
   than Dota's `MODIFIER_PROPERTY_*`. Well-known ids in `AbilityProperties` constants (move_speed,
   attack_damage, attack_speed, armor, damage_taken_mul, shield_hp…).
3. **States.** Boolean, any-true-wins: stunned, rooted, silenced, disarmed, invulnerable, untargetable,
   hidden… open registry, `AbilityStates` constants. Cast pipeline and movement/attack adapters consume them.
4. **Events + declarative reactions.** Modifiers subscribe to typed events and run **effect node lists**
   (same schema as ability effects) — no arbitrary callbacks in data, C# listeners remain available in code.
   Magic shield = `shield_hp` property + on TakeDamage absorb op + break event. Reaction depth capped (≤3)
   to prevent loops; re-entrancy guarded.
5. **Receipt-driven presentation.** Gameplay emits typed receipts; presentation components subscribe.
   No effect executes visuals directly → no visual without gameplay, and network replication rides receipts.
6. **Determinism.** Casts carry a seed; domain uses injected time/PRNG (no UnityEngine.Random/Time in Domain).
7. **Stacking.** First-class stack policies with per-source tracking (Independent per caster, Refresh,
   Stack with max + per-stack property scaling) — Dota does this ad hoc per modifier.
8. **No Lua, no sandbox.** Effect ops are C# strategy descriptors in an open registry; games register custom
   ops in code once, then designers compose them in data forever. Reaction/graph budgets are validation
   rules in the editor (cycle detection, depth caps), not runtime sandboxes.

## 4. Multiplayer (Mirror-optional, like the rest of the package)

- Domain is authority-agnostic: cast envelopes `{castId, seq, casterUnitId, target(unitId|point|dir), abilityId, seed}`
  and receipts are serializable, contain **no Unity object refs** (stable UnitId ↔ NetworkIdentity mapping in bridge).
- Solo (no Mirror): `LocalAbilityAuthority` executes immediately — zero config, works out of the box.
- With Mirror (`#if MIRROR` in Neo.Network, pattern of NetworkPropertySync/NetworkActionRelay):
  `NetworkAbilityAuthority` — client sends cast request command → server validates full pipeline → server
  executes → receipts broadcast → clients apply presentation + replicated state (modifier lists, property
  snapshots piggyback on receipts). Client-side prediction limited to presentation cues.
- Same AbilityDefinition assets on both sides; no second pipeline.

## 5. UITK editor (the v10 flagship editor — ties into the v10 inspector redesign)

`Neoxider → Windows → Ability Designer` (UI Toolkit, USS-themed dark, "very cool" per v10 direction):
- left: searchable library (abilities/modifiers/units) with create/duplicate/delete;
- center: phase timeline (Cast→Release→Travel→Impact) with effect nodes per phase, drag-reorder;
- right: inspector for selected node (op params, modifier picker, computed preview: DPS, total damage,
  stack math), live validation (missing refs, cycles, cost>pool, depth caps) shown inline;
- bottom: simulate panel — pick caster/target templates, run N casts headless in domain, show receipts log.
SO custom inspectors reuse the same USS so Project-window editing looks identical.

## 6. Migration and blast radius

- `NPC/Combat/NpcRpgCombatBrain`, `NpcCombatPreset` → `NpcAbilityBrain` + preset referencing AbilityDefinitions.
- `Tools/AttackSystem` (`Health`, `AttackExecution`, `Evade`, bridges) → keep public surface, re-route through
  a unit adapter; `RpgStatsDamageableBridge` → `AbilityUnitDamageableBridge`.
- Demos: `RpgCombatNpcDemo`, `RpgCharacterQuickDemo`, `VampireSurvivorMCP` rebuilt on Abilities (bright uGUI + tutorial).
- `Scripts/Rpg` **deleted** in v10 (breaking release; CHANGELOG migration table maps old type → new path).
- Editors under `Editor/Rpg` deleted with it; new editor lives in `Scripts/Abilities/Editor` per module structure.
- Tests: RPG edit/play tests replaced by Abilities domain tests (target: ≥ the old coverage: level sync,
  duplicate death events, regen-from-zero, projectile hits, stack persistence + new property/state/event math).

## 7. Implementation waves

1. **W1 Domain core**: units, properties, states, modifiers, events, receipts + edit-mode tests (pure C#).
2. **W2 Effects + casting**: op registry with built-in op set, cast pipeline, cooldowns/charges, targeting + tests.
3. **W3 Data + components**: SO definitions, Unity wrappers, pooled projectile, presentation listener, bridges (Health/ResourcePool/Level).
4. **W4 Migration**: NPC brain, AttackSystem adapters, delete Neo.Rpg, fix package compile + tests.
5. **W5 Editor**: UITK Ability Designer + SO inspectors.
6. **W6 Network**: Mirror bridge (solo path first-class without it).
7. **W7 Demo + docs**: AbilityShowcase scene, Docs/Abilities/*, README/PROJECT_SUMMARY/CHANGELOG.

Each wave ends green: compile via artifacts/compilecheck script + EditMode suite via Unity MCP.
