# Dota 2 Ability & Modifier Architecture — Data-Driven Reference

> Research reference for the NeoxiderTools **Neo.Abilities** ScriptableObject ability system (package v10.0.0).
> Focus: the **data-driven** model that lets designers author new abilities *without engine code* — the KV/Lua
> schema, the standard action catalog, and the modifier property/state/event vocabulary. Cross-checked against
> ModDota, the Valve Developer Community wiki, community enum dumps, and the Dota 2 gameplay wiki (see **Sources**).

Dota exposes two authoring layers on top of the same C++ engine:

| Layer | BaseClass | Author with | What it is |
|---|---|---|---|
| **DataDriven** | `ability_datadriven` / `item_datadriven` | KeyValues (`.txt`) only | Declarative *event blocks* → *action list*. No code. The closest analogue to Neo's effect-node graph. |
| **Lua** | `ability_lua` / `modifier` classes | KV *header* + Lua *behavior* | Same modifier vocabulary, but properties/states/events are implemented as Lua callback methods. Richer (motion controllers, custom math). |

Neo.Abilities is architecturally a **DataDriven-style** system (data node graph + open string registries) but borrows the
**modifier property/state/event enums** from the Lua layer as its `AbilityProperties` / `AbilityStates` / `AbilityEvents`
constant sets. Both layers are covered below because Neo draws from both.

---

## 1. Ability KeyValues Anatomy

A DataDriven ability is a nested KV block. The header keys below configure the engine's cast pipeline before any
event/action runs.

### 1.1 AbilityBehavior flags

`AbilityBehavior` is a `|`-OR'd bitmask describing *how* the ability is cast and executed. The engine uses it to drive
the order UI, cursor, cast pipeline and passivity.

| Flag | Meaning |
|---|---|
| `DOTA_ABILITY_BEHAVIOR_NO_TARGET` | Casts instantly, no target. |
| `DOTA_ABILITY_BEHAVIOR_UNIT_TARGET` | Requires a unit target. |
| `DOTA_ABILITY_BEHAVIOR_POINT` | Targets a ground point. |
| `DOTA_ABILITY_BEHAVIOR_AOE` | Draws a radius indicator (usually paired with POINT). |
| `DOTA_ABILITY_BEHAVIOR_CHANNELLED` | Channels until finished/interrupted (needs `AbilityChannelTime`). |
| `DOTA_ABILITY_BEHAVIOR_TOGGLE` | On/off toggle (fires `OnToggleOn`/`OnToggleOff`). |
| `DOTA_ABILITY_BEHAVIOR_PASSIVE` | Never cast manually; only modifier/event hooks run. |
| `DOTA_ABILITY_BEHAVIOR_AUTOCAST` | Can be set to auto-trigger on valid targets. |
| `DOTA_ABILITY_BEHAVIOR_IMMEDIATE` | No cast animation/interrupt (instant, ignores queue). |
| `DOTA_ABILITY_BEHAVIOR_HIDDEN` | Not shown in the ability bar. |
| `DOTA_ABILITY_BEHAVIOR_DIRECTIONAL` | Cast returns a direction vector (skillshot). |
| `DOTA_ABILITY_BEHAVIOR_ATTACK` | Counts as an attack action for orb/attack-modifier logic. |
| `DOTA_ABILITY_BEHAVIOR_IGNORE_BACKSWING` | Skips cast backswing. |
| `DOTA_ABILITY_BEHAVIOR_DONT_RESUME_MOVEMENT` / `_ATTACK` | Don't auto-resume prior order after cast. |
| `DOTA_ABILITY_BEHAVIOR_ROOT_DISABLES` | Cast is blocked while rooted. |
| `DOTA_ABILITY_BEHAVIOR_UNRESTRICTED` | Castable while otherwise command-restricted. |

### 1.2 Target acquisition

Targeting is three independent axes; all three are `|`-OR'd bitmasks.

**`AbilityUnitTargetTeam`** — which side is valid:

| Value | Meaning |
|---|---|
| `DOTA_UNIT_TARGET_TEAM_ENEMY` | Enemies. |
| `DOTA_UNIT_TARGET_TEAM_FRIENDLY` | Allies. |
| `DOTA_UNIT_TARGET_TEAM_BOTH` | Both. |
| `DOTA_UNIT_TARGET_TEAM_NONE` | No restriction / default. |
| `DOTA_UNIT_TARGET_TEAM_CUSTOM` | Uses a custom filter. |

**`AbilityUnitTargetType`** — what class of unit:
`DOTA_UNIT_TARGET_HERO`, `_BASIC` (creeps/summons), `_CREEP`, `_BUILDING`, `_MECHANICAL`, `_TREE`, `_COURIER`,
`_OTHER`, `_CUSTOM`, `_ALL`. Combine e.g. `HERO | BASIC`.

**`AbilityUnitTargetFlags`** — validation modifiers (the interesting filters):

| Flag | Effect |
|---|---|
| `DOTA_UNIT_TARGET_FLAG_MAGIC_IMMUNE_ENEMIES` | Can hit spell-immune enemies (pierces BKB). |
| `DOTA_UNIT_TARGET_FLAG_NOT_MAGIC_IMMUNE_ALLIES` | Skip spell-immune allies. |
| `DOTA_UNIT_TARGET_FLAG_NOT_ILLUSIONS` | Ignore illusions. |
| `DOTA_UNIT_TARGET_FLAG_NOT_SUMMONED` | Ignore summons. |
| `DOTA_UNIT_TARGET_FLAG_INVULNERABLE` | Allow invulnerable targets. |
| `DOTA_UNIT_TARGET_FLAG_NO_INVIS` | Reject invisible units. |
| `DOTA_UNIT_TARGET_FLAG_DEAD` | Allow dead units. |
| `DOTA_UNIT_TARGET_FLAG_MELEE_ONLY` / `_RANGED_ONLY` | Attack-class filter. |
| `DOTA_UNIT_TARGET_FLAG_FOW_VISIBLE` | Must be visible in fog. |
| `DOTA_UNIT_TARGET_FLAG_NOT_NIGHTMARED` / `_NOT_ATTACK_IMMUNE` / `_PLAYER_CONTROLLED` | Misc. exclusions. |

**`SpellImmunityType`** — pierce rule at cast time (independent of the target flag above, used by non-targeted AoE):
`SPELL_IMMUNITY_NONE`, `SPELL_IMMUNITY_ALLIES_YES`, `SPELL_IMMUNITY_ALLIES_NO`,
`SPELL_IMMUNITY_ENEMIES_YES`, `SPELL_IMMUNITY_ENEMIES_NO`.

**`SpellDispellableType`** — how the applied debuff reacts to dispel:
`SPELL_DISPELLABLE_YES`, `SPELL_DISPELLABLE_YES_STRONG` (basic dispel), `SPELL_DISPELLABLE_NO`.

### 1.3 Cast/economy keys

| Key | Purpose | Per-level? |
|---|---|---|
| `AbilityCastRange` | Max target distance. | yes (space-separated) |
| `AbilityCastRangeBuffer` | Grace distance before cast cancels. | yes |
| `AbilityCastPoint` | Windup delay (s) before `OnSpellStart`. | yes |
| `AbilityCastAnimation` | Animation act to play. | no |
| `AbilityChannelTime` | Channel duration (s). | yes |
| `AbilityCooldown` | Cooldown (s). | yes |
| `AbilityManaCost` | Mana cost. | yes |
| `AbilityChannelledManaCostPerSecond` | Drain per channel second. | yes |
| `AbilityDamage` | Tooltip damage. | yes |
| `AbilityDuration` | Nominal effect duration. | yes |
| `AoERadius` | UI radius indicator. | no |
| `AbilityCharges` | Max charges (charge-based cooldown). | yes |
| `AbilityChargeRestoreTime` | Seconds to regenerate one charge. | yes |
| `MaxLevel` | Max learnable ranks. | — |
| `RequiredLevel` / `LevelsBetweenUpgrades` | Learn-gating (e.g. `-4` / `7` for talents). | — |

**Charges** are an alternate cooldown model: each cast spends one charge; charges refill one at a time every
`AbilityChargeRestoreTime`, up to `AbilityCharges`. Implemented in DataDriven either via these native keys or via a
charge modifier (`max_count` / `start_count` / `replenish_time`) plus the `SpendCharge` action.

---

## 2. AbilitySpecial / AbilityValues (per-level scaling) — vs Neo `LeveledValue`

Designers never hardcode numbers in actions; they declare them once and reference them by name. Two syntaxes exist:

**Legacy `AbilitySpecial`** — an ordered list of typed rows:

```
"AbilitySpecial"
{
    "01" { "var_type" "FIELD_INTEGER" "damage"  "100 150 200 250" }   // one entry per level
    "02" { "var_type" "FIELD_FLOAT"   "radius"  "300" }               // single value = same all levels
}
```

**Modern `AbilityValues`** — a flatter block; each key is a value (array or scalar), with optional inline talent linking:

```
"AbilityValues"
{
    "damage"          "100 150 200 250"
    "slow_duration"   "2.0"
    "bonus_radius"    { "value" "0" "special_bonus_unique_hero_x" "+250" }  // talent hook, folded in automatically
}
```

Values are referenced from actions/modifiers with the `%name` token (e.g. `"Damage" "%damage"`), or read in Lua via
`ability:GetSpecialValueFor("damage")`. Per-level arrays index off the **ability's current level** (1-based, clamped).

**Talent hooks:** talents are just hidden `special_bonus_*` abilities. A `special_bonus_...` sub-key inside an
`AbilityValues` entry, or a `LinkedSpecialBonus` on an `AbilitySpecial` row, makes the engine add the talent's value to
that special automatically when learned — no code.

### Comparison with Neo `LeveledValue`

| Dota mechanism | Neo.Abilities equivalent | Notes |
|---|---|---|
| `AbilitySpecial` / `AbilityValues` named block | `AbilityBlueprint.Specials` (`List<AbilitySpecialValue>`), referenced by `EffectNodeData.AmountKey` | 1:1 concept. Neo keys by string name. |
| Per-level array `"100 150 200 250"` | `LeveledValue.Levels[]` (indexed by `LevelFrom`), or `EffectNodeData.AmountByLevel[]` | Neo clamps `level-1` like Dota. |
| Single scalar (same all levels) | `LeveledValue.Base` (with empty `Levels`) | — |
| Level source = ability level | `LevelSource.AbilityLevel` | Neo *also* supports `CasterUnitLevel` / `TargetUnitLevel` — a superset (Dota specials only key off ability level). |
| Linear growth (not native; authored as array) | `LeveledValue.PerLevel` (`Base + PerLevel*(lvl-1)`) | Neo convenience Dota lacks. |
| Scaling on a stat (Lua `+ spellpower*0.5`) | `LeveledValue.ScaleProperty` + `ScalePerPoint` + `ScaleFrom` | Neo makes stat-scaling **declarative**; Dota needs Lua for this. |
| Talent `special_bonus_*` folded into a special | *(none)* | **Gap** — no talent/upgrade layer that patches a special from another source. |

**Verdict:** Neo's `LeveledValue` is a *superset* of `AbilitySpecial` for the numeric axis (adds linear + stat-scaling +
non-ability level sources). The only missing piece is the **talent-hook** pattern (an external upgrade mutating a named
special).

---

## 3. DataDriven Event Blocks + Standard Action Catalog

### 3.1 Ability event blocks

Each block is a bag of actions run when the engine fires that phase. `%value` tokens and target keys resolve inside.

| Event block | Fires when |
|---|---|
| `OnSpellStart` | After `AbilityCastPoint`, on active cast. Primary block for most abilities. |
| `OnAbilityPhaseStart` / `OnAbilityPhaseInterrupted` | Cast point begins / cast point cancelled. |
| `OnChannelSucceeded` / `OnChannelFinish` / `OnChannelInterrupted` | Channel outcome (needs `AbilityChannelTime`). |
| `OnToggleOn` / `OnToggleOff` | Toggle abilities. |
| `OnProjectileHitUnit` / `OnProjectileFinish` | Projectile impact / expiry (from `TrackingProjectile`/`LinearProjectile`). |
| `OnUpgrade` | Ability ranked up in the HUD. |
| `OnOwnerSpawned` / `OnOwnerDied` | Owner respawn / death. |
| `OnOrbFire` / `OnOrbImpact` | Orb (attack-modifier) fire/impact. |
| `OnEquip` / `OnUnequip` | Item only (`item_datadriven`). |

### 3.2 Modifier event blocks

Declared *inside* a `Modifiers` entry; each is an action bag. These are the reaction hooks Neo mirrors.

| Modifier event | Fires when |
|---|---|
| `OnCreated` / `OnRefresh` / `OnDestroy` | Modifier applied / re-applied / removed. |
| `OnIntervalThink` | Every `ThinkInterval` seconds (periodic DoT/aura tick). |
| `OnAttack` / `OnAttackStart` / `OnAttackLanded` / `OnAttacked` / `OnAttackFailed` / `OnAttackAllied` | Attack phases. |
| `OnDealDamage` / `OnTakeDamage` | Owner deals / receives damage. |
| `OnDeath` / `OnKill` / `OnHeroKill` / `OnRespawn` | Death/kill/respawn. |
| `OnHealReceived` / `OnHealthGained` / `OnManaGained` / `OnSpentMana` | Resource changes. |
| `OnOrder` / `OnUnitMoved` / `OnTeleported` / `OnTeleporting` | Order/movement. |
| `OnAbilityExecuted` / `OnAbilityEndChannel` | Owner cast detection. |
| `OnStateChanged` / `OnProjectileDodge` | State change / projectile dodged. |

### 3.3 Standard action catalog

Cross-checked from the ModDota reference, the Valve wiki and the community action dump. Parameters are the KV keys
inside each action block. `Target` keys accept `CASTER`, `TARGET`, `POINTTARGET`, or a `{ Center / Teams / Types / Flags / Radius }` search sub-block.

| Action | Key parameters | Purpose |
|---|---|---|
| `ApplyModifier` | `Target`, `ModifierName`, (`Duration`) | Apply a named modifier. |
| `RemoveModifier` | `Target`, `ModifierName` | Remove a named modifier. |
| `Damage` | `Target`, `Type`, `Damage`, (`MinDamage`/`MaxDamage`) | Deal damage of a `DAMAGE_TYPE_*`. |
| `Heal` | `Target`, `HealAmount` | Restore HP. |
| `LifeSteal` | `Target`, `LifestealPercent` | Heal source for a % of damage. |
| `Stun` | `Target`, `Duration` | Apply a generic stun. |
| `Knockback` | `Target`, `Center`, `Distance`, `Height`, `Duration`, (`IsFixedDistance`) | Displace along a vector (a canned motion controller). |
| `MotionController` (Lua/native) | horizontal/vertical + `Priority` | General displacement controller (see §6). |
| `TrackingProjectile` | `Target`, `EffectName`, `MoveSpeed`, `Dodgeable`, `ProvidesVision`, `VisionRadius`, `SourceAttachment`, `IsAttack` | Homing projectile → `OnProjectileHitUnit`. |
| `LinearProjectile` | `EffectName`, `MoveSpeed`, `StartRadius`, `EndRadius`, `FixedDistance`, `StartPosition`, `TargetTeams`, `TargetTypes`, `TargetFlags`, `HasFrontalCone`, `ProvidesVision`, `VisionRadius` | Straight skillshot → `OnProjectileHitUnit`. |
| `ActOnTargets` | `Target` (search block), `Action` (nested actions) | Run actions on every unit matching a team/type/flags/radius search — the AoE fan-out. |
| `CreateThinker` | `ModifierName`, `Target` | Spawn an invisible dummy that carries a modifier — the standard **ground-persistent AoE** (wards, fields, auras). |
| `CreateThinkerWall` | `ModifierName`, `Width`, `Length`, `Rotation`, `Target` | Line of thinkers (walls). |
| `SpawnUnit` | `UnitName`, `Target`, `Duration`, `UnitCount`, `OnSpawn` | Summon units. |
| `ReplaceUnit` | `UnitName`, `Target` | Morph/replace a unit. |
| `CleaveAttack` | `CleavePercent`, `CleaveRadius`, `CleaveEffect` | Splash a portion of attack damage. |
| `CreateBonusAttack` | `Target` | Trigger an extra instant attack. |
| `Blink` | `Target` | Teleport the unit to a point. |
| `MoveUnit` / `MoveCamera` | `Target` | Reposition unit / camera. |
| `FireSound` | `EffectName`, `Target` | Play a sound. |
| `FireEffect` / `AttachEffect` | `EffectName`, `EffectAttachType`, `Target`, (`ControlPoints`) | Play/attach a particle. |
| `CreateItem` | `ItemName`, `ItemCount`, `SpawnRadius`, `LaunchDistance` | Drop/give an item. |
| `SpendMana` / `SpendCharge` | `Mana` / — | Pay resource / consume a charge. |
| `ReduceCooldown` | `Target`, `Ability`, `Seconds` | Refresh/cut a cooldown. |
| `Interrupt` | `Target` | Cancel channel/order. |
| `Random` | `Chance`, `PseudoRandom`, `OnSuccess` [actions], `OnFailure` [actions] | Probabilistic branch. |
| `DelayedAction` | `Delay`, nested actions | Run actions after N seconds. |
| `RunScript` | `ScriptFile`, `Function` | Escape hatch into Lua. |

**Mapping to Neo's op registry:**

| Dota action | Neo effect op (`AbilityEffectOps`) | Status |
|---|---|---|
| `ApplyModifier` | `apply_modifier` | ✅ |
| `RemoveModifier` | `remove_modifier` | ✅ |
| `Damage` | `damage` | ✅ |
| `Heal` | `heal` | ✅ |
| *(dispel — no single Dota action; RemoveModifier/Purge)* | `dispel` | ✅ (Neo has a first-class dispel op) |
| `SpendMana` / resource change | `resource_change` | ✅ |
| `SpawnUnit` / `CreateThinker` | `spawn` | ⚠️ Neo spawns units; **no thinker/aura carrier** semantics. |
| `ActOnTargets` | `EffectTargetSelector.AreaAround{Target,Caster}` on any node | ✅ (fan-out is a selector, not an op) |
| `Random` / chance | `EffectNodeData.Chance` (per-node roll) | ✅ |
| `Knockback` / `MotionController` | *(planned `knockback`/`pull`/`teleport`)* | ❌ **missing** |
| `CleaveAttack` / `CreateBonusAttack` / `TrackingProjectile` / `LinearProjectile` | delivery = `Projectile` (archetype+speed) | ⚠️ projectile exists but no cleave / linear-cone / dodgeable |
| *(planned execute/chain)* | — | ❌ **missing** |

---

## 4. Modifier Model

A modifier is a stateful buff/debuff attached to a unit. It contributes **properties** (numeric stat deltas), sets
**states** (booleans), reacts to **events**, and can **think** on an interval or project an **aura**.

### 4.1 MODIFIER_PROPERTY_* (the numeric layer — important members)

There are ~200 properties; the ones that matter for a general RPG system, grouped:

**Attack / damage output:** `PREATTACK_BONUS_DAMAGE`, `BASE_ATTACK_BONUS_DAMAGE`, `BASEDAMAGEOUTGOING_PERCENTAGE`,
`DAMAGE_OUTGOING_PERCENTAGE`, `TOTAL_DAMAGE_OUTGOING_PERCENTAGE`, `PREATTACK_CRITICALSTRIKE`,
`PROC_ATTACK_BONUS_DAMAGE_{PHYSICAL,MAGICAL,PURE}`, `OVERRIDE_ATTACK_MAGICAL`, `DISABLE_AUTOATTACK`.

**Attack rate/range:** `ATTACKSPEED_BONUS_CONSTANT`, `ATTACKSPEED_BASE_OVERRIDE`, `FIXED_ATTACK_RATE`,
`BASE_ATTACK_TIME_CONSTANT`, `ATTACK_POINT_CONSTANT`, `ATTACK_RANGE_BONUS`, `PROJECTILE_SPEED_BONUS`.

**Movement:** `MOVESPEED_BONUS_CONSTANT`, `MOVESPEED_BONUS_PERCENTAGE`, `MOVESPEED_ABSOLUTE`, `MOVESPEED_LIMIT`,
`MOVESPEED_ABSOLUTE_MIN`, `TURN_RATE_PERCENTAGE`, `IGNORE_MOVESPEED_LIMIT`.

**Incoming damage / mitigation:** `INCOMING_DAMAGE_PERCENTAGE`, `INCOMING_PHYSICAL_DAMAGE_PERCENTAGE`,
`INCOMING_SPELL_DAMAGE_CONSTANT`, `ABSOLUTE_NO_DAMAGE_{PHYSICAL,MAGICAL,PURE}`, `MIN_HEALTH`.

**Armor / resist / block / evasion:** `PHYSICAL_ARMOR_BONUS`, `MAGICAL_RESISTANCE_BONUS`, `EVASION_CONSTANT`,
`MISS_PERCENTAGE`, `PHYSICAL_CONSTANT_BLOCK`, `TOTAL_CONSTANT_BLOCK`, `MAGICAL_CONSTANT_BLOCK`, `AVOID_DAMAGE`, `AVOID_SPELL`.

**Health / mana / regen:** `HEALTH_BONUS`, `MANA_BONUS`, `CONSTANT_HEALTH_REGEN`, `HEALTH_REGEN_PERCENTAGE`,
`CONSTANT_MANA_REGEN`, `TOTAL_PERCENTAGE_MANA_REGEN`, `HP_REGEN_AMPLIFY_PERCENTAGE`, `MANA_REGEN_AMPLIFY_PERCENTAGE`,
`DISABLE_HEALING`.

**Spell / cast economy:** `SPELL_AMPLIFY_PERCENTAGE`, `COOLDOWN_REDUCTION_CONSTANT`, `PERCENTAGE_COOLDOWN`,
`CAST_RANGE_BONUS`(`_STACKING`/`_TARGET`), `MANACOST_REDUCTION_CONSTANT`, `PERCENTAGEMANACOST`, `PERCENTAGE_CASTTIME`,
`SPELL_LIFESTEAL_REGEN_AMPLIFY_PERCENTAGE`, **`STATUS_RESISTANCE_STACKING`** (see §5).

**Stats:** `BONUS_STATS_{STRENGTH,AGILITY,INTELLECT}`, `EXTRA_{STRENGTH,HEALTH,MANA}_BONUS`, `EXTRA_HEALTH_PERCENTAGE`.

**Vision / detection / illusion:** `INVISIBILITY_LEVEL`, `PERSISTENT_INVISIBILITY`, `BONUS_{DAY,NIGHT}_VISION`,
`PROVIDES_FOW_VISION`, `IS_ILLUSION`, `ILLUSION_LABEL`, `SUPER_ILLUSION`.

**Presentation:** `OVERRIDE_ANIMATION`(`_RATE`/`_WEIGHT`), `MODEL_CHANGE`, `MODEL_SCALE`, `VISUAL_Z_DELTA`,
`ATTACK_SOUND`, `PROJECTILE_NAME`.

**Property arithmetic:** each property is combined by an engine-defined rule — `_CONSTANT`/`_BONUS` = additive,
`_PERCENTAGE` = additive-then-applied, `_OVERRIDE`/`ABSOLUTE`/`_LIMIT` = replace/clamp. Neo generalizes this to an
explicit `PropertyOp { Add, Mul, Max }` per contribution.

### 4.2 MODIFIER_STATE_* (the boolean layer — **full list**, enum order)

States aggregate **any-true-wins** across all modifiers via `CheckState()`; a modifier returns a table of
`state → true/false`.

| # | State | # | State |
|---|---|---|---|
| 0 | `ROOTED` | 18 | `FROZEN` |
| 1 | `DISARMED` | 19 | `COMMAND_RESTRICTED` |
| 2 | `ATTACK_IMMUNE` | 20 | `NOT_ON_MINIMAP` |
| 3 | `SILENCED` | 21 | `NOT_ON_MINIMAP_FOR_ENEMIES` |
| 4 | `MUTED` (items disabled) | 22 | `LOW_ATTACK_PRIORITY` |
| 5 | `STUNNED` | 23 | `NO_HEALTH_BAR` |
| 6 | `HEXED` | 24 | `FLYING` |
| 7 | `INVISIBLE` | 25 | `NO_UNIT_COLLISION` |
| 8 | `INVULNERABLE` | 26 | `NO_TEAM_MOVE_TO` |
| 9 | `MAGIC_IMMUNE` | 27 | `NO_TEAM_SELECT` |
| 10 | `PROVIDES_VISION` | 28 | `PASSIVES_DISABLED` |
| 11 | `NIGHTMARED` | 29 | `DOMINATED` |
| 12 | `BLOCK_DISABLED` | 30 | `BLIND` |
| 13 | `EVADE_DISABLED` | 31 | `OUT_OF_GAME` |
| 14 | `UNSELECTABLE` | 32 | `FAKE_ALLY` |
| 15 | `CANNOT_TARGET_ENEMIES` | 33 | `FLYING_FOR_PATHING_PURPOSES_ONLY` |
| 16 | `CANNOT_MISS` | 34 | `TRUESIGHT_IMMUNE` |
| 17 | `SPECIALLY_DENIABLE` | | |

(`MODIFIER_STATE_LAST = 35` is the sentinel count.)

### 4.3 Modifier metadata keys / Lua callbacks

| KV key (DataDriven) | Lua callback | Meaning |
|---|---|---|
| `Duration` | — | Lifetime (s); absent/`-1` = permanent. |
| `Passive` | — | Always-on vs manual. |
| `IsBuff` / `IsDebuff` | `GetTexture`, dispel logic | UI classification. |
| `IsHidden` | `IsHidden()` | Hide from the buff bar. |
| `IsPurgable` | `IsPurgable()` | Removable by basic dispel. |
| `IsPurgeException` / `RemoveOnDeath` | `IsPurgeException()`, `RemoveOnDeath()` | Purge/death exceptions. |
| `ThinkInterval` | `OnIntervalThink()` + `StartIntervalThink(t)` | Tick cadence. |
| `Properties { }` | `DeclareFunctions()` + `GetModifier*()` | Numeric contributions. |
| `States { }` | `CheckState()` | Boolean states. |
| `Attributes` | `GetAttributes()` | See below. |
| `Aura` / `Aura_Radius` / `Aura_Teams` / `Aura_Types` / `Aura_Flags` | `IsAura()`, `GetModifierAura()`, `GetAuraRadius()`, `GetAuraSearchTeam/Type/Flags()` | **Aura projection**: re-applies a child modifier to units in radius. |
| — | `SetStackCount()`/`GetStackCount()`/`IncrementStackCount()` | Stack count (properties can scale by it). |

**`DOTAModifierAttribute_t` (`Attributes`):**

| Attribute | Effect |
|---|---|
| `MODIFIER_ATTRIBUTE_NONE` | Default (single instance, refresh on re-apply). |
| `MODIFIER_ATTRIBUTE_MULTIPLE` | Each application is an independent stacking instance. |
| `MODIFIER_ATTRIBUTE_PERMANENT` | Survives most dispels / never times out. |
| `MODIFIER_ATTRIBUTE_IGNORE_INVULNERABLE` | Applies even to invulnerable targets. |
| `MODIFIER_ATTRIBUTE_AURA_PRIORITY` | Wins aura arbitration when multiple auras of a kind overlap. |

**Stacking/refresh:** default = one instance, re-apply refreshes duration; `MULTIPLE` = one instance per application
(independent timers); stack count is a separate integer for scaling. **Auras** are modeled as a permanent "carrier"
modifier that continuously (re)applies a short-duration child modifier to units within `Aura_Radius`; when a unit leaves
the radius the child expires after its brief linger — no explicit "remove" needed.

### 4.4 Comparison with Neo modifier model

| Dota | Neo (`ModifierBlueprint` / `ModifierInstance`) | Status |
|---|---|---|
| `Properties { }` | `Properties: List<PropertyContribution>` (per-stack scaling supported) | ✅ |
| `States { }` (any-true) | `States: List<string>` (any-true aggregate) | ✅ |
| `ThinkInterval` + `OnIntervalThink` | `TickInterval` + `TickOnApply` + `TickEffects` | ✅ |
| Modifier event blocks (`OnTakeDamage`…) | `EventReactions: List<ModifierEventReaction>` (`EventId` + `Effects` + `TargetEventSource`) | ✅ |
| `IsDebuff` | `IsDebuff` | ✅ |
| `IsPurgable` | `Dispellable` | ✅ |
| `Duration` + per-level | `Duration` + `DurationByLevel[]` (`ResolveDuration(level)`) | ✅ (Neo per-level; Dota needs Lua) |
| `MULTIPLE` vs refresh vs stack | `ModifierStackPolicy { Independent, Refresh, Stack }` + `MaxStacks` | ✅ clean 3-way |
| `IsHidden` | *(none — no buff-bar visibility flag)* | ⚠️ minor gap |
| `Attributes` (`IGNORE_INVULNERABLE`, `PERMANENT`, `AURA_PRIORITY`) | `IsPermanent` derived only | ⚠️ partial |
| `Aura` projection (carrier→child in radius) | *(none)* | ❌ **missing** |
| `CheckState` extra states (24 vs Neo's 10) | 10 well-known + open registry | ⚠️ open, but no engine reads flying/no-collision/etc. |

---

## 5. Damage Types, Status Resistance, Spell Amp & Lifesteal

### 5.1 Damage types & flags

| `DAMAGE_TYPE_*` | Reduced by | Neo `AbilityDamageTypes` |
|---|---|---|
| `PHYSICAL` (1) | Armor (diminishing formula) | `physical` ✅ |
| `MAGICAL` (2) | Magic resistance %; blocked by spell immunity | `magical` ✅ |
| `PURE` (4) | Nothing (ignores armor & resist) | `pure` ✅ |
| `HP_REMOVAL` (8) | Bypasses everything incl. shields; not "damage" | *(none)* ❌ |

**`DOTADamageFlag_t`** modifies a single instance (Neo has no equivalent flag axis):
`IGNORES_PHYSICAL_ARMOR`, `IGNORES_MAGIC_ARMOR`, `BYPASSES_BLOCK`, `BYPASSES_INVULNERABILITY`, `HPLOSS`,
`NON_LETHAL` (can't kill — leaves at 1 HP), `REFLECTION`, `NO_DAMAGE_MULTIPLIERS`, `NO_SPELL_AMPLIFICATION`,
`NO_DIRECTOR_EVENT`, `USE_COMBAT_PROFICIENCY`.

The full pipeline order (simplified): base → outgoing amp (spell amp) → reductions (armor/resist) → constant blocks →
incoming % modifiers → shields/absorbs → `MIN_HEALTH` clamp → apply.

### 5.2 Status resistance

A single stat (`MODIFIER_PROPERTY_STATUS_RESISTANCE_STACKING`) that scales the **duration of most debuffs/disables**
applied by enemies. Key rules (cross-checked against the gameplay wiki):

- **Multiplicative** stacking (grouped, with diminishing returns) — cannot reach 100% from stacking alone.
- `Final duration = Base × (1 − StatusResistance) × (1 − DebuffDurationModifiers…)`.
- **Does NOT affect:** auras & their linger, permanent modifiers, effect delays (e.g. respawn), DoT tick intervals
  (total damage unchanged), and movement slows (governed by the separate **Slow Resistance**).
- Countered by outgoing **Debuff Duration Amplification** on the caster's side.

### 5.3 Spell amplification & spell lifesteal

- **Spell Amplification** (`SPELL_AMPLIFY_PERCENTAGE`): additive % that multiplies *outgoing* spell damage (unless the
  instance has `NO_SPELL_AMPLIFICATION`). Applied on the source side of the pipeline.
- **Spell Lifesteal** (`SPELL_LIFESTEAL_REGEN_AMPLIFY_PERCENTAGE` + item-provided lifesteal): heals the caster for a %
  of spell damage dealt, scaled by target type (hero vs creep) and amplifiable by HP-regen amp.
- **Attack Lifesteal** is a separate `LifeSteal` action / attack-modifier path.

**Neo status:** has `spell_power` (a *scaling term*, not a global outgoing multiplier), `magic_resist_percent`,
`incoming_damage_mul`, `outgoing_damage_mul`, `lifesteal_percent` (attack-oriented), `shield_hp`, `evasion_chance`.
**Missing:** a **status-resistance stat that scales applied modifier durations**, damage **flags** (non-lethal /
hp-removal / bypass-block / no-amp), and a dedicated **outgoing spell-amp multiplier + spell-lifesteal** step in the
magical branch.

---

## 6. Motion Controllers

Displacement (knockback, pull, leaps, forced marches) is not ad-hoc position writes — it goes through **motion
controllers** so concurrent movement forces arbitrate deterministically.

- **Types:** `LUA_MODIFIER_MOTION_{NONE, HORIZONTAL, VERTICAL, BOTH}` — a modifier declares which axes it drives, and
  implements `UpdateHorizontalMotion(units, dt)` / `UpdateVerticalMotion(units, dt)`; `OnHorizontal/VerticalMotionInterrupted`
  fires when overridden.
- **Application:** `ApplyHorizontalMotionController(...)` / `ApplyVerticalMotionController(...)` returns success; the
  `Knockback` DataDriven action is a pre-baked horizontal controller (`Distance` / `Height` / `Duration`).
- **Priority arbitration — `DOTA_MOTION_CONTROLLER_PRIORITY_*`:** `LOWEST(0)`, `LOW(1)`, `MEDIUM(2)`, `HIGH(3)`,
  `HIGHEST(4)`. **Highest priority wins**; on a tie, the **last-applied** controller overrules. This prevents two
  knockbacks from stacking into teleportation and lets hard displaces (e.g. a channelled pull) override soft ones.
- **Interaction with states:** displaced units usually also get `MODIFIER_STATE_STUNNED`/`COMMAND_RESTRICTED` and
  optionally `NO_UNIT_COLLISION` for the duration.

**Neo status:** ❌ none. The planned `knockback` / `pull` / `teleport` ops should be built as a **motion-controller
subsystem with a priority enum + interrupt callback**, not as raw transform writes — otherwise overlapping displaces
will fight. This is the single largest missing subsystem.

---

## 7. Neo.Abilities Parity Checklist

Legend: ✅ present · ⚠️ partial · ❌ missing. "Neo" columns reference the actual v10.0.0 source in
`Assets/Neoxider/Scripts/Abilities`.

### 7.1 Ability header / cast pipeline

| Dota mechanism | Neo has | Missing / worth adding |
|---|---|---|
| `AbilityBehavior` NO_TARGET/UNIT/POINT/DIRECTIONAL | ✅ `TargetingMode { NoTarget, Self, Unit, Point, Direction }` | — |
| `AbilityBehavior` PASSIVE/TOGGLE/CHANNELLED/AUTOCAST/IMMEDIATE | ❌ | **Toggle + channel lifecycle** (`OnChannelFinish/Interrupted`), autocast, passive-only. |
| Target team | ✅ `AbilityTeamFilter { Any, Enemies, Allies }` | — |
| `AbilityUnitTargetType` (hero/creep/building/…) | ❌ | Unit-class target filter. |
| `AbilityUnitTargetFlags` (pierce spell-immune, not-illusions, invuln…) | ❌ | **Target-flags filter + spell-immunity piercing rule.** |
| `AbilityCastPoint` (windup) | ❌ (delivery is instant/projectile only) | **Cast-point / backswing timing** hook before impact. |
| `AbilityCastRange` | ✅ `Range` | — |
| `AbilityCooldown` + cooldown reduction | ✅ `Cooldown` + `cooldown_reduction_percent` property | — |
| `AbilityManaCost` / resource costs | ✅ `Costs: List<AbilityCost>` (any pool) | ✅ superset (multi-resource). |
| `AbilityCharges` / `AbilityChargeRestoreTime` | ✅ `MaxCharges` + `ChargeRestoreTime` (`UsesCharges`) | — |

### 7.2 Per-level values

| Dota | Neo | Missing |
|---|---|---|
| `AbilitySpecial` / `AbilityValues` named block | ✅ `Specials` + `AmountKey` | — |
| Per-level arrays keyed by ability level | ✅ `Levels[]` / `AmountByLevel[]` (`LevelSource.AbilityLevel`) | — |
| Level source = *only* ability level | ✅ **superset**: also `CasterUnitLevel` / `TargetUnitLevel` | — |
| Stat scaling (Lua-only in Dota) | ✅ **declarative** `ScaleProperty` + `ScalePerPoint` + `ScaleFrom` | — |
| Talent `special_bonus_*` folded into a special | ❌ | **Talent/upgrade layer** patching a named special from an external source. |

### 7.3 Effect actions

| Dota action | Neo op | Missing |
|---|---|---|
| ApplyModifier / RemoveModifier | ✅ `apply_modifier` / `remove_modifier` | — |
| Damage / Heal | ✅ `damage` / `heal` | Damage **flags** (non-lethal, hp-removal, bypass-block/invuln, no-amp). |
| Purge/dispel | ✅ `dispel` (first-class) | Strong vs basic dispel distinction. |
| SpendMana / resource | ✅ `resource_change` | — |
| SpawnUnit | ✅ `spawn` | — |
| `CreateThinker` (ground-persistent AoE / ward / field) | ⚠️ `spawn` only | **Thinker/field carrier** that hosts a modifier + ticks on the ground. |
| `ActOnTargets` AoE fan-out | ✅ `AreaAround{Target,Caster}` selector + `TeamFilter` + `Radius(ByLevel)` | — |
| `Random` chance branch | ✅ `EffectNodeData.Chance` (deterministic RNG) | OnSuccess/OnFailure sub-branches. |
| `Knockback` / `MotionController` | ❌ (planned) | **Motion controllers + priority** (§6). |
| `TrackingProjectile` vs `LinearProjectile` | ⚠️ `Delivery=Projectile` (archetype+speed) | Linear/cone skillshots, dodgeable, start/end radius, pierce, vision. |
| `CleaveAttack` / `CreateBonusAttack` | ❌ | Attack-splash / bonus-attack (if attack model is in scope). |
| `ReduceCooldown` / `Interrupt` | ❌ | Cooldown manipulation + channel/order interrupt op. |
| *(planned)* execute / chain | ❌ | **Execute (%-HP / threshold kill)** and **chain/bounce** ops. |
| DelayedAction | ⚠️ ticks approximate it | A one-shot delayed-effect node. |

### 7.4 Modifier model

| Dota | Neo | Missing |
|---|---|---|
| Properties (numeric contributions) | ✅ `PropertyContribution` + `PropertyOp { Add, Mul, Max }` + per-stack | — |
| States (any-true boolean) | ✅ `States` (open) + 10 well-known in `AbilityStates` | Engine-read extras: flying, no-collision, unselectable, command-restricted. |
| Modifier events (OnTakeDamage/OnAttackLanded/OnDeath/OnIntervalThink…) | ✅ `EventReactions` + `TickEffects` (11 events in `AbilityEvents`) | Attack-phase granularity (OnAttackStart/Landed/Failed), OnKill vs OnDeath split (Neo has both). |
| Stack policy (MULTIPLE/refresh) | ✅ `ModifierStackPolicy` 3-way + `MaxStacks` | — |
| Duration per-level | ✅ `DurationByLevel[]` | — |
| `IsDebuff` / `IsPurgable` | ✅ `IsDebuff` / `Dispellable` | — |
| `IsHidden` (buff bar) | ❌ | UI-visibility flag. |
| `Attributes` (IGNORE_INVULNERABLE / PERMANENT / AURA_PRIORITY) | ⚠️ `IsPermanent` only | **Ignore-invulnerable apply rule**, aura-priority arbitration. |
| **Aura** projection (carrier→child in radius) | ❌ | **Aura subsystem** (radius + team/type search re-applying a child modifier). |

### 7.5 Combat math

| Dota | Neo | Missing |
|---|---|---|
| Physical (armor) / Magical (resist) / Pure | ✅ `physical` / `magical` / `pure` | — |
| `HP_REMOVAL` type | ❌ | Un-mitigable HP removal (for %-max-HP effects). |
| Damage flags | ❌ | **Non-lethal, bypass-block, bypass-invuln, no-amp, reflection.** |
| Spell amplification (outgoing %) | ⚠️ `spell_power` scaling term only | **Global outgoing spell-amp multiplier** in the magical branch. |
| Spell lifesteal | ⚠️ `lifesteal_percent` (attack) | **Spell lifesteal** (heal caster % of spell damage). |
| Status resistance (scales disable durations) | ❌ | **Status-resistance stat** scaling `ResolveDuration` at apply. |
| Evasion / miss / block | ⚠️ `evasion_chance` / `shield_hp` properties exist | Attack-miss & block **roll pipeline** that consumes them. |
| Slow resistance | ❌ | Separate resist axis for movement slows. |

---

## Executive Summary — most valuable still-missing atoms for Neo

1. **Motion-controller subsystem (with a priority enum).** Build the planned `knockback` / `pull` / `teleport` ops as
   controllers arbitrated by a `MotionPriority { Lowest…Highest }` (highest wins, last-applied breaks ties) with an
   interrupt callback — not raw transform writes. Overlapping displaces otherwise fight. **Single biggest gap.**
2. **Aura projection on modifiers.** Dota's `Aura` (carrier modifier re-applies a short-duration child to units in
   `Aura_Radius` filtered by team/type) plus `CreateThinker` ground fields are the backbone of survivor/RPG kits. Neo
   has ticks + reactions but **no aura and no ground-persistent modifier carrier**.
3. **Status-resistance stat.** One property that multiplicatively scales `ModifierBlueprint.ResolveDuration` for
   enemy-applied debuffs at apply time (excluding auras/DoT ticks). Cheap to add, high design leverage.
4. **Damage flags axis.** Add `non_lethal`, `hp_removal`, `bypass_block`, `bypass_invulnerable`, `no_amplification`,
   `reflection` to the damage op. `non_lethal` + `hp_removal` are prerequisites for clean **execute / %-max-HP** ops.
5. **Target-acquisition flags + spell-immunity rule.** Unit-class target types and flags (pierce-magic-immune,
   not-illusions, not-summoned, allow-invulnerable) plus a per-ability spell-immunity pierce policy — Neo currently
   filters by team only.
6. **First-class spell amplification + spell lifesteal.** A global outgoing spell-amp multiplier and a spell-lifesteal
   step in the magical branch (distinct from attack `lifesteal_percent`). `spell_power` today is only a scaling term.
7. **Cast lifecycle: cast-point windup + channel/toggle.** A windup delay before impact and channel outcome hooks
   (`OnChannelFinish` / `OnChannelInterrupted`) plus toggle on/off — needed for anything beyond instant/projectile casts.
8. **Talent/upgrade hook on named specials + `IGNORE_INVULNERABLE` modifier attribute.** An external upgrade layer that
   patches a `Specials` entry (Dota `special_bonus_*`), and an apply rule that lets chosen modifiers ignore invulnerable
   targets. Both are small, well-scoped additions.

---

## Sources

- ModDota — Ability KeyValues: https://moddota.com/abilities/ability-keyvalues
- ModDota — DataDriven Ability Events & Modifiers: https://moddota.com/abilities/datadriven/datadriven-ability-events-modifiers
- ModDota — Extending Hero/NPC API with Lua modifiers: https://moddota.com/abilities/lua-modifiers/1/
- ModDota — Making any ability use charges: https://moddota.com/abilities/making-any-ability-use-charges/
- ModDota — Using Modifier Properties in tooltips: https://moddota.com/abilities/modifier-properties-in-tooltips
- Valve Developer Community — Abilities Data Driven: https://developer.valvesoftware.com/wiki/Dota_2_Workshop_Tools/Scripting/Abilities_Data_Driven
- Valve Developer Community — Dota 2 Actions and Modifiers: https://developer.valvesoftware.com/wiki/Dota_2_Actions_and_Modifiers
- Valve Developer Community — Lua Abilities and Modifiers: https://developer.valvesoftware.com/wiki/Dota_2_Workshop_Tools/Lua_Abilities_and_Modifiers
- Valve Developer Community — Data Driven Motion Controller Example: https://developer.valvesoftware.com/wiki/Dota_2_Workshop_Tools/Scripting/Data_Driven_Motion_Controller_Example
- Valve Developer Community — ApplyVerticalMotionController / SetMotionPriority / GetPriority (motion controller API pages)
- Community action reference (ApplyModifier … catalog): https://pastebin.com/raw/hXHpnTy5
- Enum dump (modifierstate / motion priority / attributes / damage flags), lolleko/dota-lua-autocomplete scripthelp2.txt: https://github.com/lolleko/dota-lua-autocomplete/blob/master/scripthelp2.txt
- ModDota/API script_help2 dump: https://github.com/ModDota/API/blob/master/dump/script_help2.lua
- TypeScriptToLua/Dota2Declarations — dota-modifier-properties.d.ts: https://github.com/TypeScriptToLua/Dota2Declarations/blob/master/dota-modifier-properties.d.ts
- Liquipedia Dota 2 — Status Resistance & Charge Abilities: https://liquipedia.net/dota2/Status_Resistance , https://liquipedia.net/dota2/Charge_Abilities
- Pizzalol/SpellLibrary (DataDriven reference reimplementations): https://github.com/Pizzalol/SpellLibrary

_Cross-checked: KV/behavior/target enums (ModDota × Valve wiki), action catalog (ModDota × community dump × Valve wiki),
modifier state/attribute/damage enums (scripthelp2 dump × Dota2Declarations), status resistance (Liquipedia × wiki)._
