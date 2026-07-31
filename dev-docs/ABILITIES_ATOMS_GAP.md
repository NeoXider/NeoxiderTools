# Neo.Abilities — Atom & Micro-Atom Gap Analysis + Data-Driven Design Proposal

Status: design doc only. No code is changed by this document.
Scope: make `Neo.Abilities` fully DATA-DRIVEN / no-code, Dota-style — a designer authors new
abilities, modifiers and level-scaling entirely in ScriptableObjects, zero C#.

Sources verified against real code (every name below was read from source):
- `Assets/Neoxider/Scripts/Abilities/Domain/**` and `Data/**` (the pure-C# domain + SO wrappers).
- `Assets/Neoxider/Scripts/Core/Level/**` and `Core/Resources/**` (level + resource pools to integrate with).
- Target design (borrow DESIGN only, not Lua/LLM): `D:\Git\CoreAI\Docs\SPELL_GENERATOR_ARCHITECTURE.md`
  §5.1, §5.2/§5.3, §5A, §7.3, §8.1, §9; `D:\Git\CoreAI\Docs\SPELL_GENERATOR_ENGINE_FEATURES.md` §0/§1.
- Context: `dev-docs/ABILITIES_ARCHITECTURE.md`; demo data under `Samples/Demo/Survivor/Data/*`.

---

## 0. Executive summary — the top 5 to implement first (in order)

The single thing blocking Dota-parity no-code authoring is that **there is no way to make a value change
with ability level or with a caster/target stat, in data.** `EffectNodeData.Amount` is one flat float, and
`AbilitySlot.Level` exists but is read by nothing. Fix that first; everything else is incremental atoms.

1. **P0 — Leveled-value micro-atom (`LeveledValue`) on `EffectNodeData.Amount`.** Add an optional
   per-level array + property/level scaling term that resolves through a pure `LeveledValueResolver`.
   Backward-compatible: an asset with only `Amount: 5` keeps resolving to 5. This is Dota "AbilitySpecial"
   — the core no-code mechanism.
2. **P0 — Level-system integration seam.** Add `int Level` to `AbilityUnit`; pass `AbilitySlot.Level` into
   `EffectContext.AbilityLevel`; capture ability level on `ModifierInstance` at apply time so DoT ticks scale.
   Wire `Neo.Core.Level.LevelComponent`/`LevelModel` → `AbilityUnit.Level` in the Unity wrapper. Without
   this, item 1 has no level to read.
3. **P1 — `displace` / `MoveUnit` world-adapter seam + `knockback`/`pull`/`teleport` ops.** One new adapter
   method unlocks the entire motion family (knockback, pull, blink, dash) — today the domain literally
   cannot move a unit.
4. **P1 — Make declared-but-inert combat properties live in `DamageService`:** `lifesteal_percent`,
   `crit_chance`/`crit_multiplier`, `evasion_chance`, and apply `max_health_bonus`/`max_mana_bonus` to the
   pools. These constants exist and do nothing today — data that references them silently no-ops.
5. **P1 — `execute` (%-max-HP damage), `chain`/`bounce`, and a `MaxTargets` cap on area/chain nodes.**
   High-value effect atoms that turn the flat AoE into real Dota-style abilities, all pure-data.

Everything below is the evidence and the full backlog behind those five.

---

## 1. Inventory (Have) — what atoms already exist (exact names)

### 1.1 Effect operation atoms (`AbilityEffectOps`, registered by `DefaultEffectOps.RegisterAll`)

| Op id | Class | What it does | Key `EffectNodeData` fields used |
|---|---|---|---|
| `damage` | `DamageEffectOperation` | Routes `Amount` through `DamageService`; pre-scales by caster `spell_power` | `Amount`, `DamageType` |
| `heal` | `HealEffectOperation` | Restores HP honoring `healing_received_mul`; skips dead | `Amount` |
| `apply_modifier` | `ApplyModifierEffectOperation` | Attaches catalog modifier to targets | `ModifierId` |
| `remove_modifier` | `RemoveModifierEffectOperation` | Removes all instances of an id | `ModifierId` |
| `dispel` | `DispelEffectOperation` | Default removes debuffs (**this is cleanse**); `CustomParam="buffs"` purges buffs | `CustomParam` |
| `resource_change` | `ResourceChangeEffectOperation` | +/- a resource pool (mana burn/restore) | `ResourceId`, `Amount` |
| `spawn` | `SpawnEffectOperation` | Asks world adapter to spawn an archetype at point/targets | `ArchetypeId`, `Amount` (magnitude) |

Registry is **open**: `AbilitySystem.Ops.Register(IEffectOperation)` — games add custom ops in code, compose in data.

### 1.2 Damage types (`AbilityDamageTypes`, open registry)
`physical` (armor curve `0.06·a/(1+0.06·|a|)`), `magical` (`magic_resist_percent`, blocked by `magic_immune`
state), `pure` (ignores armor/resist/shields, stopped only by `invulnerable`). Custom strings skip built-in mitigation.

### 1.3 Targeting modes (`TargetingMode`)
`NoTarget`, `Self`, `Unit`, `Point`, `Direction`. Cast-time only; no toggle/channel/passive/autocast.

### 1.4 Effect target selectors (`EffectTargetSelector`)
`Target`, `Caster`, `AreaAroundTarget`, `AreaAroundCaster`. Area = sphere via `IAbilityWorldAdapter.QueryUnitsInRadius`.

### 1.5 Team filters (`AbilityTeamFilter`)
`Any`, `Enemies`, `Allies` (relative to caster team via `TeamId.IsAllyOf`/`IsEnemyOf`).

### 1.6 Delivery types (`AbilityDeliveryType`)
`Instant`, `Projectile` (host spawns `ProjectileArchetypeId`; reports hits via `AbilitySystem.NotifyProjectileHit`).

### 1.7 Property ids (`AbilityProperties`, open registry) — LIVE vs INERT
Aggregation `PropertyAggregator.Compute`: `final = max((base + ΣAdd) · ΠMul, Max-floors)`. Ops: `Add`, `Mul`, `Max`.

| Property id | Consumed by domain? | Where |
|---|---|---|
| `spell_power` | LIVE | `DamageEffectOperation` pre-scale |
| `armor` | LIVE | `DamageService` physical curve |
| `magic_resist_percent` | LIVE | `DamageService` magical |
| `outgoing_damage_mul` | LIVE | `DamageService` step 1 |
| `incoming_damage_mul` | LIVE | `DamageService` step 3 |
| `shield_hp` | LIVE | `DamageService.ConsumeShields` (per-instance pool) |
| `healing_received_mul` | LIVE | `HealEffectOperation` |
| `health_regen`, `mana_regen` | LIVE | `AbilitySystem.Tick` |
| `cooldown_reduction_percent` | LIVE | `AbilitySystem.Cast` (clamped 0..90) |
| `cast_range_bonus` | LIVE | `AbilitySystem.Cast` range check |
| `mana_cost_mul` | LIVE | `AbilitySystem.EffectiveCost` |
| `move_speed`, `attack_damage` | INERT in domain | only set as `UnitTemplate` base defaults; host reads them |
| `attack_speed`, `attack_range` | INERT | declared only; no consumer |
| `crit_chance`, `crit_multiplier` | INERT | declared only; no consumer |
| `evasion_chance` | INERT | declared only; no consumer |
| `lifesteal_percent` | **DEAD** | declared, **zero consumers anywhere** |
| `max_health_bonus`, `max_mana_bonus` | **DEAD** | declared, **zero consumers** — contributing them does nothing to the pool |

### 1.8 State ids (`AbilityStates`, open, any-true-wins via `ModifierEngine.HasState` + `AbilityUnit` permanent set)
`stunned`, `rooted`, `silenced`, `disarmed`, `invulnerable`, `untargetable`, `hidden`, `airborne`, `frozen`, `magic_immune`.
Consumed: `stunned`/`silenced` gate casting; `invulnerable`/`magic_immune` gate damage; `untargetable` gates unit targeting.
INERT (no domain consumer): `rooted`, `disarmed`, `hidden`, `airborne`, `frozen` — meaningful only to host movement/attack code.

### 1.9 Event ids (`AbilityEvents`, open, via `AbilityEventBus`)
`take_damage`, `deal_damage`, `heal_received`, `death`, `kill`, `ability_cast`, `modifier_applied`,
`modifier_removed`, `shield_absorbed`, `shield_broken`. Modifiers react declaratively via `ModifierEventReaction`
`{ EventId, Effects[], TargetEventSource }`, depth-capped at `EffectContext.MaxDepth = 3`.

### 1.10 Modifier features (`ModifierBlueprint`)
`Id`, `DisplayName`, `Duration` (≤0 ⇒ permanent), `StackPolicy` (`Independent`/`Refresh`/`Stack`), `MaxStacks`,
`Properties[]` (`PropertyContribution` with `PerStackValue`), `States[]`, `TickInterval`, `TickOnApply`,
`TickEffects[]`, `EventReactions[]`, `IsDebuff`, `Dispellable`, `PresentationCue`. Shields = `shield_hp`
contribution consumed per-instance (`ModifierInstance.ShieldConsumed`) with `shield_absorbed`/`shield_broken` events.

### 1.11 Cast-pipeline gates (`AbilitySystem.Cast` → `CastFailureReason`)
`UnknownCaster`, `UnknownAbility`, `NotGranted`, `CasterDead`, `Stunned`, `Silenced`, `OnCooldown`/`NoCharges`,
`NotEnoughResources`, `InvalidTarget`, `TargetDead`, `TargetUntargetable`, `WrongTeam`, `OutOfRange`.
Also: charges (`MaxCharges`/`ChargeRestoreTime`), cooldown, per-cost check-then-pay, `node.Chance` per-node RNG,
deterministic seed (`XorShiftRandom`).

### 1.12 Amount-scaling (what exists)
- `spell_power` multiply inside `DamageEffectOperation` (hard-coded, damage-only).
- `PropertyContribution.PerStackValue` — per-stack linear scaling of modifier property values.
- `node.Chance` — probabilistic execution.
- **Nothing else.** No per-level arrays, no scale-by-property for heal/resource/duration/radius, no named values.

### 1.13 World-adapter seams (`IAbilityWorldAdapter`)
`TryGetPosition`, `QueryUnitsInRadius` (sphere), `RequestSpawn`. **No `MoveUnit`/teleport, no LOS, no facing/velocity,
no cone/line query.**

### 1.14 Level integration (current reality)
- `AbilitySlot.Level` (int, default 1, public setter) — **read by nothing**. Comment says "for data-driven
  per-level scaling (SO layer)" but no code path uses it.
- `AbilityUnit` has **no level field** at all.
- `Neo.Core.Level` (`LevelModel`, `LevelCurveDefinition`, `LevelComponent`, `LevelCurveEvaluator`,
  `ILevelProvider`) is a complete, tested XP/level system — but **not referenced by `Neo.Abilities` anywhere**.
- `UnitTemplate` has team + resources + base properties + abilities. **No level, no curve, no stat-growth field**
  (the `ABILITIES_ARCHITECTURE.md` §2 mention of "level growth (LevelCurveDefinition)" is aspirational, not coded).

---

## 2. Gap tables (Have / Partial / Missing) vs the Dota-style target

Legend: **H** = fully in data today · **P** = partial/possible-but-awkward or code-only · **M** = missing.

### 2.A Leveled values / "special values" — THE key micro-atom  (highest priority)

| Capability (Dota term) | State | Evidence / gap |
|---|---|---|
| Per-level value array (`AbilitySpecial`) keyed by ability level | **M** | Only flat `EffectNodeData.Amount`; `AbilitySlot.Level` unused |
| Value referenced by name (`%damage%`, reused across nodes) | **M** | No named value table on `AbilityBlueprint` |
| Scale amount by a caster/target property (e.g. `spell_power`) | **P** | Only `spell_power`, only in `damage`, hard-coded; heal/resource/radius/duration cannot scale |
| Scale amount by ability level | **M** | No hook |
| Scale amount by unit level (caster/target) | **M** | `AbilityUnit` has no level |
| Scale modifier duration / tick damage / radius per level | **M** | `Radius`, `Duration`, `TickEffects[].Amount` are flat floats |

This is the biggest no-code blocker. Full design in §3.

### 2.B Effect-operation atoms (rich abilities)

| Op (proposed id) | State | Notes |
|---|---|---|
| knockback / `displace` | **M** | No adapter seam to move a unit |
| pull / `pull` | **M** | Same seam missing |
| teleport / blink / `teleport` | **M** | Same |
| dash / motion / `dash` | **M** | Needs motion seam + duration |
| chain / bounce / `chain` | **M** | No hop iteration; would need target-graph + `MaxTargets` |
| execute (%-HP) / `execute` | **M** | No %-max-HP damage; `Amount` is flat |
| lifesteal (make `lifesteal_percent` live) | **M** | Property exists, **dead**; `DamageService` never heals attacker |
| cleanse (dispel debuffs) | **H** | `dispel` default already does this |
| purge (strip buffs) | **H** | `dispel` `CustomParam="buffs"` |
| summon (typed preset beyond generic spawn) | **P** | `spawn` op exists; no ownership/caps/lifetime/behavior preset |
| stat-set (`set_property` base) | **M** | Can only add modifiers, not set a base property |
| conditional / branch (`if`) | **M** | No node predicate beyond `Chance`; no branch targets |
| kill / instant-death | **M** | Only via lethal `pure` damage |
| destroy-entity (despawn summon/zone) | **M** | No despawn command |

### 2.C Targeting / behavior atoms

| Capability | State | Notes |
|---|---|---|
| Self / Point / Direction / Unit / NoTarget | **H** | `TargetingMode` |
| Toggle abilities (on/off, per-tick cost) | **M** | No toggle state on slot |
| Channel / sustain (`sustain_begin`/`end`, interrupt) | **M** | No channel phase; cast is instantaneous |
| Passive (no cast, always-on) | **P** | Fakeable as a permanent self modifier via template; no first-class passive slot |
| Autocast | **M** | Host concern; no data flag |
| Cone / line area shapes | **M** | Only `QueryUnitsInRadius` (sphere) |
| Max-target cap | **M** | `EffectNodeData` has no `MaxTargets`; area hits everyone in radius |
| Target-point vs unit-lock nuances | **H** | Covered by `TargetingMode` + `EffectTargetSelector` |

### 2.D Property atoms (extend the open registry)

Registry is open, so any string already works. Gap = well-known ids that host/UI expect + making declared ones live.

| Property | State | Action |
|---|---|---|
| `lifesteal_percent` | **P (dead)** | Make live in `DamageService` (heal source by % of `deal_damage`) |
| `crit_chance`, `crit_multiplier` | **P (dead)** | Roll crit in `DamageService` (deterministic RNG) |
| `evasion_chance` | **P (dead)** | Roll evasion pre-mitigation |
| `max_health_bonus`, `max_mana_bonus` | **P (dead)** | Apply to `ResourcePoolModel.SetMax` on modifier change |
| `status_resistance` | **M** | New const; scales debuff duration on apply |
| `spell_amp` | **M** | Generalize the `spell_power` hook (rename/alias) |
| `bonus_strength/agility/intelligence` | **M** | New consts; optional stat→derived mapping (host/composite) |
| `attack_range`, `attack_speed`, `cast_point`, `backswing` | **M/P** | Host-facing consts; add names, host consumes |
| `%max_hp` regen | **M** | Requires resolver reading max-HP (see execute) |

### 2.E State atoms

| State | State | Notes |
|---|---|---|
| `stunned`/`rooted`/`silenced`/`disarmed`/`invulnerable`/`untargetable`/`hidden`/`airborne`/`frozen`/`magic_immune` | **H** | Declared |
| `taunted` / forced-attack | **M** | Add const; host AI consumes |
| `ethereal` (can't attack/be attacked, +magic dmg) | **M** | Add const + `DamageService` hook |
| `break` (passives disabled) | **M** | Add const; passive-modifier suppression |
| `unstoppable` / `debuff_immune` | **M** | Add const; gates debuff apply / dispel-on-apply |
| `commanded`/`invisible` split vs `hidden` | **P** | `hidden` exists; true-sight nuance is host |

### 2.F Event-hook atoms (declarative reactions)

| Event | State | Notes |
|---|---|---|
| `take_damage`, `deal_damage`, `heal_received`, `death`, `kill`, `ability_cast`, `modifier_applied/removed`, `shield_absorbed/broken` | **H** | Declared & fired |
| `on_attack_landed` (basic attack) | **M** | Domain has no basic-attack loop; host must fire it |
| `on_cast_start` (prepare, before cost) | **M** | Only `ability_cast` after cost; no prepare phase |
| `on_kill` reaction | **P** | `kill` event fires; a modifier can react to it today via `ModifierEventReaction{EventId="kill"}` — usable but undocumented as a "reaction" |
| `on_expire` (modifier natural expiry) | **P** | `modifier_removed` fires with `Amount=1` when expired; no dedicated `on_expire` id |
| `on_spell_cast_by_other` / `on_ability_cast_landed` | **M** | Only fired on caster; no "ally cast near me" |
| `on_move`, `on_stop`, `on_order` | **M** | Host movement events not surfaced |

### 2.G Modifier lifetime / lease atoms

| Lifetime (CoreAI §5.1: Instant/Duration/UntilCondition/Permanent) | State | Notes |
|---|---|---|
| Duration lease | **H** | `ModifierBlueprint.Duration` |
| Permanent | **H** | `Duration <= 0` |
| Instant (fire-and-forget effect) | **H** | Effect nodes are already instant |
| Aura (apply-to-units-in-radius while active) | **M** | No aura engine; `property_field` component missing. Fakeable only via `TickEffects` that re-`apply_modifier` in radius — leaky (no clean un-apply on leave) |
| Thinker / zone (persistent ground effect ticking a query) | **P** | `spawn` an archetype whose prefab ticks — but that pushes logic back into host C#, not data |
| Until-condition lease | **M** | No condition grammar |
| Stack policy `ReplaceByStrength` | **M** | Only `Independent`/`Refresh`/`Stack` |

### 2.H Level-system integration

| Capability | State | Notes |
|---|---|---|
| Ability level readable by effects | **M** | `AbilitySlot.Level` unused |
| Unit level readable by effects | **M** | `AbilityUnit` has no level |
| Ability leveled values keyed to ability level | **M** | §2.A |
| Unit base stats grow with unit level (data) | **M** | `UnitTemplate` has no curve/growth |
| Core `LevelModel`/`LevelComponent` feeds the ability domain | **M** | Not referenced |
| Cost / cooldown scale with level | **M** | Flat `AbilityCost.Amount`, `AbilityBlueprint.Cooldown` |

---

## 3. The leveled-value micro-atom (deep design — P0)

### 3.1 Goal
A designer sets, in the inspector, e.g. Ember damage = `[5, 8, 12, 17]` (per ability level), plus
"+0.4× caster `spell_power`", and the DoT and radius scale the same way — no code. This is Dota's
`AbilitySpecial` + `%value%` referencing, adapted to the open-registry pure-C# domain.

### 3.2 New serializable type (`Domain/Effects/LeveledValue.cs`, pure C#)

```csharp
[Serializable]
public struct LeveledValue
{
    public float Base;              // used when Levels is empty (== today's flat Amount)
    public float[] Levels;          // per-level array; value = Levels[clamp(level-1)]
    public float PerLevel;          // linear growth when Levels empty: Base + PerLevel*(level-1)
    public string ScaleProperty;    // optional property id (e.g. spell_power); "" = none
    public float   ScalePerPoint;   // value *= (1 + prop * ScalePerPoint)   [Mul form]
    public ScaleAmountSource ScaleFrom; // Caster | Target
    public LevelSource LevelFrom;   // None | AbilityLevel | CasterUnitLevel | TargetUnitLevel
}
public enum ScaleAmountSource { Caster = 0, Target = 1 }
public enum LevelSource { None = 0, AbilityLevel = 1, CasterUnitLevel = 2, TargetUnitLevel = 3 }
```

### 3.3 Resolver (`Domain/Effects/LeveledValueResolver.cs`, pure, deterministic)
`float Resolve(in LeveledValue v, EffectContext ctx, UnitId target)`:
1. Pick level per `LevelFrom` (`AbilityLevel` ⇐ `ctx.AbilityLevel`; unit levels ⇐ `AbilityUnit.Level`; `None` ⇒ 1).
2. Base value: `Levels?.Length > 0 ? Levels[clamp(level-1, 0, len-1)] : Base + PerLevel*(level-1)`.
3. If `ScaleProperty` set: `value *= 1 + unit.GetProperty(ScaleProperty) * ScalePerPoint`
   (unit = caster or target per `ScaleFrom`).
Stateless; no `UnityEngine.Random`/`Time` — matches the domain's determinism rule.

### 3.4 Wiring into `EffectNodeData` (backward compatible)
Keep the existing `public float Amount;` field (so every current asset that serializes `Amount: 5` is
untouched). Add OPTIONAL fields, all defaulting to "no effect":

```csharp
public float[]  AmountByLevel;      // null/empty ⇒ use Amount
public string   AmountKey;          // "" ⇒ inline; else look up AbilityBlueprint.Specials by name
public string   AmountScaleProperty;// "" ⇒ no scaling
public float    AmountScalePerPoint;
public ScaleAmountSource AmountScaleSource;
public LevelSource       AmountLevelSource; // None ⇒ level 1 ⇒ Amount unchanged
```

Resolution rule inside each op: replace `node.Amount` reads with
`LeveledValueResolver.Resolve(node.ToLeveledValue(blueprint, target), ctx, target)` where `ToLeveledValue`
maps the inline fields (or the named `Specials` entry) into a `LeveledValue`. When all new fields are default,
this returns exactly `node.Amount` — **zero behavior change for existing data**.

Apply the same to `Radius` (add optional `RadiusByLevel`) and to modifier `Duration`/tick amounts (§3.6).

### 3.5 Named "special values" on the ability
```csharp
// AbilityBlueprint
public List<AbilitySpecialValue> Specials = new();
[Serializable] public struct AbilitySpecialValue { public string Name; public LeveledValue Value; }
```
A node with `AmountKey = "damage"` reads `Specials["damage"]`. Lets one leveled array feed several nodes
(damage node + tooltip + a scaled slow) — exactly Dota's `%damage%` reuse. `EffectContext` carries a
reference to the current blueprint's `Specials` (or a resolved lookup delegate) so ops can dereference.

### 3.6 Level available in every effect context
- `EffectContext.AbilityLevel` (new int, default 1). Set in `AbilitySystem.Cast` from the resolved
  `slot.Level`, and in `NotifyProjectileHit` from a new `PendingProjectileCast.Level`.
- `ModifierInstance.AbilityLevel` (new int) captured from `EffectContext.AbilityLevel` at
  `ModifierEngine.Apply` time, so `OnModifierTick`/reaction contexts (which have no cast) still scale
  DoTs by the level the modifier was applied at. Feed it into the tick/reaction `EffectContext`.

### 3.7 Level on the unit (for `CasterUnitLevel`/`TargetUnitLevel`)
- `AbilityUnit.Level` (new int, default 1) + `SetLevel(int)`; bumps a version so property caches that
  depend on level growth invalidate.

---

## 4. Prioritized proposal — backlog (P0 / P1 / P2)

Each item: name · kind · exact data fields to add · new adapter seam (if any) · one-line acceptance test.
All additive; existing enums/fields keep their integer values so serialized `.asset` data is stable.

### P0 — unblock no-code scaling + level integration

**P0-1 · Leveled-value micro-atom** · *leveled-value micro-atom*
Add: `LeveledValue`, `ScaleAmountSource`, `LevelSource`, `LeveledValueResolver` (new files);
`EffectNodeData.{AmountByLevel, AmountKey, AmountScaleProperty, AmountScalePerPoint, AmountScaleSource,
AmountLevelSource}`; `AbilityBlueprint.Specials` + `AbilitySpecialValue`.
Adapter: none.
Accept: a node with `AmountByLevel:[5,10]` and `AmountLevelSource:AbilityLevel` deals 5 at slot level 1, 10 at level 2; a node with only `Amount:5` still deals 5.

**P0-2 · Ability level in the effect context** · *level-integration*
Add: `EffectContext.AbilityLevel`; `AbilitySystem.Cast` sets it from `slot.Level`;
`PendingProjectileCast.Level` + `NotifyProjectileHit` sets it; `ModifierInstance.AbilityLevel` captured in
`ModifierEngine.Apply`, fed into `OnModifierTick` + `HandleModifierReactions` contexts.
Adapter: none.
Accept: raising `slot.Level` raises a leveled DoT's per-tick damage after re-apply, with no data change beyond the level array.

**P0-3 · Unit level + Core Level bridge** · *level-integration*
Add: `AbilityUnit.Level` + `SetLevel`; `UnitTemplate.StartLevel` (int) and optional
`UnitTemplate.LevelCurve` (`LevelCurveDefinition` ref) applied in `ApplyTo`; `AbilityUnitBehaviour`
optional `LevelComponent` ref that calls `Unit.SetLevel(provider.Level)` on `OnLevelUp`.
Adapter: none (reuses `Neo.Core.Level`).
Accept: a `LevelComponent` at level 3 on the caster makes a `CasterUnitLevel`-sourced node resolve its level-3 value; unit with no LevelComponent stays level 1.

**P0-4 · Leveled radius + leveled modifier duration** · *leveled-value micro-atom*
Add: `EffectNodeData.RadiusByLevel` (+ resolver reuse); `ModifierBlueprint.DurationByLevel` and resolve at
apply using `ModifierInstance.AbilityLevel`.
Adapter: none.
Accept: an AoE grows radius and a stun grows duration with ability level from data only.

### P1 — the high-value atom families

**P1-1 · `displace` motion seam + knockback/pull/teleport/dash ops** · *effect-op atoms + world-adapter seam*
Adapter (new): `void MoveUnit(UnitId unit, Vector3 target, MoveKind kind, float speed)` (or
`RequestMotion(MotionRequest)`) on `IAbilityWorldAdapter`; `NullWorldAdapter` + `AbilitySystemBehaviour`
implement it (behaviour translates transform / drives a tween).
Add ops: `knockback` (push from caster/point by `Amount` distance), `pull` (toward caster/point),
`teleport` (to target point/unit), `dash` (caster toward point at `Amount` speed).
`EffectNodeData` fields reused: `Amount` (distance/speed, leveled), plus new `MotionKind` enum + optional
`MotionSpeed` (leveled). Grant `airborne` state during motion via a bundled modifier.
Accept: casting `knockback Amount:4` moves the target 4 units away from the caster (fake adapter asserts new position).

**P1-2 · Make dead combat properties live in `DamageService`** · *property atoms*
Change (code, not new data): after health damage, if `source` has `lifesteal_percent`, heal source by
`% · healthDamage`; roll `crit_chance`/`crit_multiplier` (pre-mitigation, deterministic via cast RNG passed
into `DamageService`); roll `evasion_chance` on the victim → `Negated`. Apply `max_health_bonus`/
`max_mana_bonus` to `ResourcePoolModel.SetMax` when a unit's modifier set changes (hook `ModifierEngine`
version bump). Add consts: `status_resistance`, `spell_amp`.
Adapter: none.
Accept: attacker with `lifesteal_percent:50` heals for half the HP damage dealt; `max_health_bonus:100` modifier raises `MaxHealth` by 100 and drops it back on expiry.

**P1-3 · `execute` (%-max-HP) + `kill` + `set_property`** · *effect-op atoms*
Add ops: `execute` (`Amount` = fraction of target max HP, dealt as chosen `DamageType`; leveled),
`kill` (lethal), `set_property` (sets a base property via `AbilityUnit.SetBaseProperty`, value leveled).
Fields: reuse `Amount`/leveled; `set_property` reuses `CustomParam` as property id (or a new `PropertyId`).
Accept: `execute Amount:0.1 magical` removes 10% of the target's max HP as magical damage.

**P1-4 · `chain`/`bounce` + `MaxTargets` cap + cone/line shapes** · *targeting atoms + world-adapter seam*
Add: `EffectNodeData.MaxTargets` (int, 0 = unlimited) honored by `CollectArea`; new
`EffectTargetSelector.ChainFromTarget` with `ChainRange`/`ChainCount`; area-shape enum
(`Sphere`/`Cone`/`Line`) with `ConeAngle`/`LineWidth`.
Adapter (new, optional): `QueryUnitsInCone`/`QueryUnitsInLine` (or a single `QueryUnits(shape)`); default
falls back to radius when unimplemented.
Accept: a `chain` node with `ChainCount:3` hits at most 3 distinct nearest enemies in order; `MaxTargets:2` on an AoE caps it at 2.

**P1-5 · Aura modifier lease (`property_field`)** · *modifier-lease atom*
Add: `ModifierBlueprint.Aura` block `{ float Radius; AbilityTeamFilter Filter; string AuraModifierId; }`.
`ModifierEngine` (or a small `AuraEngine` ticked by `AbilitySystem.Tick`) re-evaluates units in radius each
tick and applies/removes a short-refresh child modifier (clean enter/leave). Reuses `QueryUnitsInRadius`.
Accept: a unit with an aura modifier grants `AuraModifierId` to allies within radius and it drops within one tick after they leave.

**P1-6 · `on_attack_landed` / `on_cast_start` / `on_expire` event ids** · *event-hook atoms*
Add consts to `AbilityEvents`: `attack_landed`, `cast_start`, `expire`. Fire `cast_start` in `Cast` before
cost check; publish `expire` (distinct from `modifier_removed`) when a modifier expires naturally; host fires
`attack_landed` through a new `AbilitySystem.NotifyAttackLanded(attacker, victim, amount)` helper.
Accept: a modifier with `EventReactions[{EventId:"attack_landed"}]` runs its effects when the host reports a basic attack.

### P2 — completeness / polish

**P2-1 · Summon preset op (`summon`)** · *effect-op atom* — typed wrapper over `spawn` adding `OwnerId`,
lifetime, and a population cap; `SpawnRequest` gains `Lifetime`/`OwnerCap`. Accept: `summon` respects a per-caster cap and auto-despawns after its lifetime.

**P2-2 · `destroy_entity` op** · *effect-op atom* — despawn a summon/zone by owner+archetype; needs adapter
`DespawnOwned(UnitId owner, string archetypeId)`. Accept: casting it removes the caster's active zone.

**P2-3 · Channel / sustain phase** · *targeting atom* — `AbilityBlueprint.ChannelTime` + `channel` trigger
phase; `AbilitySystem` ticks channel, interrupts on `stunned`/`silenced`/move; fires `sustain_tick`. Accept: a 3s channel ticks its effect and stops early when the caster is stunned.

**P2-4 · Conditional / branch node** · *effect-op atom* — `EffectNodeData.Condition`
(`{ string Property/State; Comparator; float Value }`, closed grammar like CoreAI §8.1 receipt-predicate)
gating execution; optional `ElseOpId`. Accept: a node runs only when target HP% < 0.3.

**P2-5 · New states/props** · *state/property atoms* — add `taunted`, `ethereal`, `break`, `unstoppable`,
`debuff_immune` consts + their gates; add `status_resistance` scaling of debuff duration on apply. Accept: a `debuff_immune` unit rejects incoming debuff modifiers.

**P2-6 · Leveled cost / cooldown** · *leveled-value micro-atom* — `AbilityCost.AmountByLevel`,
`AbilityBlueprint.CooldownByLevel`. Accept: cost/cooldown change with slot level from data only.

**P2-7 · `ReplaceByStrength` stack policy + `until_condition` lifetime** · *modifier-lease atoms*. Accept: a stronger slow replaces a weaker one; a modifier persists until a named condition clears.

### Adapter-seam summary (new methods on `IAbilityWorldAdapter`)
- `MoveUnit`/`RequestMotion` (P1-1) — displacement family.
- `QueryUnitsInCone`/`QueryUnitsInLine` or unified `QueryUnits(shape)` (P1-4) — non-sphere shapes.
- `DespawnOwned` (P2-2) — destroy spawned entities.
- (optional) `HasLineOfSight` — future targeting fidelity.
All new methods get default no-op/fallback implementations in `NullWorldAdapter` so tests and headless use compile unchanged.

---

## 5. Migration note — backward compatibility

**Serialized data (`.asset`) stays valid.** Every proposal is *additive*:
- New `EffectNodeData` fields (`AmountByLevel`, `AmountKey`, `Amount*Scale*`, `RadiusByLevel`, `MaxTargets`,
  motion/shape fields) default to empty/`None`/0. Unity deserializes missing YAML keys as defaults, so the
  9 SurvivorDemo ability/modifier assets (`ab_ember`, `ab_frost_ring`, `ab_magic_bolt`, `ab_nova`,
  `mod_burn`, `mod_chill`, `mod_haste`, `mod_might`, `mod_regen`, `mod_swift`, `mod_ward`) load untouched.
- The resolver's contract is: **all-default new fields ⇒ result equals today's `node.Amount`.** So
  `mod_burn` (flat `Amount:4` tick) and `ab_ember` (flat `Amount:5` + radius `3.2`) behave identically.
- No existing enum values change (`TargetingMode`, `EffectTargetSelector`, `AbilityTeamFilter`,
  `AbilityDeliveryType`, `ModifierStackPolicy`, `PropertyOp` keep their integer members); new members are
  appended at the end, so existing integer-serialized fields keep their meaning.
- `AbilitySlot.Level` already exists and defaults to 1 → P0-2 activates it without a data migration; abilities
  never leveled continue to resolve at level 1 (index 0 / no growth).

**The 93 existing edit-mode tests** (`Tests/Edit/Abilities/*`: CastPipeline 19, ModifierEngine 17,
DamageService 13, EffectOperation 10, PropertyAggregator 11, AbilityState 6, Determinism 6,
ModifierTickAndReaction 6, ProjectileAndSpawn 5) stay green because:
- Public method signatures are preserved; new fields/overloads are additive. Where `DamageService.ApplyDamage`
  gains crit/evasion (P1-2), keep the current signature as a deterministic-RNG-free default (crit/evasion
  only fire when the property is non-zero *and* an RNG is supplied), so existing damage-math assertions are unchanged.
- Determinism tests keep passing: the resolver and all rolls use the injected `IRandomSource`/`XorShiftRandom`,
  never `UnityEngine.Random`.
- New behavior ships with new tests (leveled resolve, level bridge, motion via fake adapter, lifesteal/crit,
  execute, chain cap, aura enter/leave) rather than altering existing fixtures.

**Recommended guardrail:** add a golden test asserting `LeveledValueResolver.Resolve` on an all-default
`EffectNodeData` equals its `Amount`, and one round-tripping each SurvivorDemo asset to prove the demo's
numbers are byte-stable after the schema addition.

---

## 6. Cross-reference: CoreAI target vs Neo mapping (borrowed design only)

| CoreAI concept | Neo equivalent (have) | Neo gap |
|---|---|---|
| Macro-atom set (14): damage, heal, apply/remove_modifier, resource_change, displace, teleport, spawn_entity, destroy_entity, projectile, area_query, sustain_begin/end, presentation | 7 ops + projectile delivery + area selectors + presentation cues | displace, teleport, destroy_entity, sustain, explicit area_query op |
| §5A.2 property contributions `{id, add|mul|max, value}` | `PropertyContribution` + `PropertyAggregator` | dead ids to activate (§2.D) |
| §5A.3 states any-true-wins | `AbilityStates` + `HasState` | extra well-known states (§2.E) |
| §5A.4 declarative event reactions `{on, do[]}` | `ModifierEventReaction` | more event ids (§2.F) |
| §5.1 lifetime Instant/Duration/UntilCondition/Permanent | Instant/Duration/Permanent | UntilCondition, aura/thinker |
| §8.1 stack policy Reject/Refresh/AddStack/ReplaceByStrength | Independent/Refresh/Stack | ReplaceByStrength |
| AbilitySpecial per-level values | — | **the P0 leveled-value micro-atom** |
| §9 resource state + regen | `ResourcePoolModel` + regen in `Tick` | max-HP/mana bonus not applied |

CoreAI's Lua tier, LLM pipeline, command-buffer/receipt-hashing, sandbox budgets and multiplayer authority are
**explicitly out of scope** — Neo stays a clean pure-C# open-registry domain; we borrow only the data model.
