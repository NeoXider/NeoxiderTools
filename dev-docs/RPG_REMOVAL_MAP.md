# Neo.Rpg Removal & Migration Map (v10)

Scope: `Assets/Neoxider/Scripts/Rpg` (asmdef **Neo.Rpg**, 55 `.cs` files, namespaces `Neo.Rpg`,
`Neo.Rpg.Components`, `Neo.Rpg.Components.Weapons`, `Neo.Rpg.Runtime`) is deleted in v10 and replaced
by **Neo.Abilities** (`Assets/Neoxider/Scripts/Abilities`, asmdef `Neo.Abilities`, namespace `Neo.Abilities`).

This document is the exhaustive removal inventory + migration recipes + safe deletion order + risks.
Source of truth for design intent: `dev-docs/ABILITIES_ARCHITECTURE.md`.

Important scoping note: `RpgResourceId` (constants `Hp="HP"`, `Mana="Mana"`, `Stamina`, `Shield`) lives in
**`Neo.Core.Resources`** (`Scripts/Core/Resources/RpgResourceId.cs`), **not** in the Neo.Rpg module — it is
**not deleted**. Any `RpgResourceId.*` reference keeps compiling. It matters only as a data-value mismatch
(see Risk R3): Abilities uses `AbilityResourceIds.Health = "health"`, not `"HP"`.

---

## (a) Blast-radius summary

| Area | Files / assets | Real compile dependency? | Effort | Verdict |
|---|---|---|---|---|
| **asmdef refs to Neo.Rpg** | 6 external asmdefs (+ Neo.Rpg itself) | yes | Low | Drop `Neo.Rpg` ref; add `Neo.Abilities` where still needed |
| **NPC combat** | `NpcRpgCombatBrain.cs`, `NpcCombatPreset.cs` (asmdef Neo.NPC) | yes (types) | High | Rewrite → `NpcAbilityBrain` (does not exist yet) |
| **Tools/AttackSystem** | 5 files (asmdef Neo.Tools.Components) | **1 of 5** (`RpgStatsDamageableBridge`) | Low–Med | 4 are string-only attrs (no break); bridge → `AbilityUnitDamageableBridge` |
| **Editor/Rpg** | 4 files (asmdef Neo.Editor) | yes (types) | Low | Delete with module; author new Abilities inspectors |
| **Samples/Demo scripts** | 6 files (asmdef Neo.Rpg.Demo + 1 editor fixer) | yes (types) | High | Rebuild demos on Abilities or delete |
| **Tests** | 6 files (Neo.Editor.Tests + Neo.Tests.Play) | yes (types + string type-names) | High | Delete RPG suites; port coverage to Abilities tests |
| **Scenes/Prefabs/.asset (GUID refs)** | 25 assets | yes (missing-script if deleted first) | High | Rebuild/replace components; delete demo assets |
| **SO assets of Rpg types** | 11 `.asset` (templates/attacks/presets/growth) | n/a (data) | Med | Recreate as Abilities SOs or delete |
| **Docs** | whole `Docs/Rpg/` (24 md) + ~20 cross-ref docs + README/PROJECT_SUMMARY/MODULE_STRUCTURE/CHANGELOG | no (compile) | Med | Delete `Docs/Rpg/`; rewrite cross-refs |

Headline counts: **23** `.cs` files outside `Scripts/Rpg` touch Neo.Rpg; **6** external asmdefs; **25**
scenes/prefabs/assets carry Rpg script GUIDs; **11** are ScriptableObject assets of Rpg SO types.

Key relief: **4 of the 5** `Tools/AttackSystem` files (`AdvancedAttackCollider`, `AttackExecution`,
`Evade`, `Health`) reference `Neo.Rpg` **only inside `[LegacyComponent("…")]` / `[Obsolete("…")]` string
literals** — deleting the module does **not** break their compile. Only `RpgStatsDamageableBridge.cs` is a
real type dependency, and it is the sole reason the `Neo.Tools.Components` asmdef references `Neo.Rpg`.

---

## Neo.Abilities replacement API (as it exists today)

Verified from source. This is what recipes below target.

- **Scene hub** `AbilitySystemBehaviour` (`.I` auto-singleton, `DefaultExecutionOrder(-500)`) → owns one
  pure-C# `AbilitySystem`, ticks it, is the `IAbilityWorldAdapter` (positions, radius queries, pooled spawns).
- **Unit component** `AbilityUnitBehaviour` → creates/owns an `AbilityUnit` from a `UnitTemplate`; exposes
  `Unit`, `UnitId`, `CurrentHealth`, `MaxHealth`, `HealthNormalized`, `IsAlive`, `ApplyDamage(float)`, and
  UnityEvents `OnDamaged/OnHealed/OnDied/OnModifierApplied/OnModifierRemoved/OnAbilityCast`.
- **Domain unit** `AbilityUnit` → `Health`/`MaxHealth`, `Resources` (`ResourcePoolModel` from Neo.Core),
  `GetProperty(id,default)`, `SetBaseProperty(id,val)`, `GetBaseProperty`, `HasState(id)`,
  `SetPermanentState(id,bool)`, `IsAlive`.
- **Damage** `DamageService.ApplyDamage(system, sourceUnitId, targetUnitId, amount, damageType, abilityId=null)`
  → full pipeline (outgoing mul → armor/resist → incoming mul → shields → health → death/kill events).
- **Cast** `AbilityCasterBehaviour` (`RequireComponent(AbilityUnitBehaviour)`): `TryCast(id)`,
  `TryCastAtUnit`, `TryCastAtPoint`, `TryCastTowards`, `GetCooldownNormalized`; events `OnCastSuccess/Failed`.
  Domain: `AbilitySystem.Cast(CastRequest)` → `CastResult`.
- **Projectile** `AbilityProjectileBehaviour : ISpawnedAbilityEntity` (pooled via `PoolManager`, distance-based
  hits, homing/pierce; no colliders).
- **Data SOs** `UnitTemplate` (team, `UnitResourceConfig[]`, `UnitPropertyDefault[]`, `AbilityDefinition[]`),
  `AbilityDefinition` (wraps `AbilityBlueprint`), `ModifierDefinition` (wraps `ModifierBlueprint`), `AbilityLibrary`.
- **Registries (open, string-keyed)**: `AbilityProperties` (move_speed, attack_damage, armor,
  magic_resist_percent, incoming_damage_mul, outgoing_damage_mul, shield_hp, evasion_chance, max_health_bonus…),
  `AbilityResourceIds` (`Health="health"`, `Mana="mana"`), `AbilityStates` (Invulnerable, Stunned, Silenced,
  MagicImmune, Untargetable…), `AbilityEffectOps` (damage, heal, apply_modifier, remove_modifier, dispel,
  resource_change, spawn), `AbilityDamageTypes` (Physical, Magical, Pure).
- **Modifier model** `ModifierBlueprint` (Id, Duration, StackPolicy, MaxStacks, `PropertyContribution[]`,
  `States[]`, TickInterval, `TickEffects`, `EventReactions`, IsDebuff, Dispellable) — buffs + statuses unified.

### Capabilities that do NOT yet exist in Neo.Abilities (gaps → new adapter API needed)

| Missing capability (RPG had it) | Minimal adapter Neo.Abilities should add |
|---|---|
| Heal method on the scene unit | `AbilityUnitBehaviour.Heal(float)` (route through a `heal` effect op / `Resources.Increase(Health)`; already fires `OnHealed`) |
| Reactive props (`HpState`, `HpPercentState`, `ManaState`…) | Optional `ReactivePropertyFloat` mirrors on `AbilityUnitBehaviour`, or document "subscribe to OnDamaged/OnHealed + poll" |
| Progression: level, XP, upgrade points, manual stat upgrades (`SetLevel/AddXp/AddUpgradePoints/UpgradeStat`, `RpgProgressionDefinition`, `RpgStatUpgradeRule`, `RpgStatGrowthDefinition`) | New `UnitProgression`/`AbilityLevelBinding` reusing `Neo.Core.Level`; growth-per-level curve → base-property setter. No equivalent exists today |
| Persistence (`SaveProfile/LoadProfile/ResetProfile`, `RpgCharacterProfileData`) | New `AbilityUnitPersistence` adapter over `Neo.Save` (serialize resources/properties/modifiers) |
| Mirror networking (`Net*` cmds, SyncVar snapshot, `NetworkAuthorityMode`) | `NetworkAbilityAuthority` (architecture §4, not built) — until then multiplayer RPG parity is lost |
| Contact / aura melee components (`RpgContactDamage`, `AuraWeapon`, `MeleeWeapon`) | `AbilityContactDamage` component or an aura `ModifierDefinition` + area effect op |
| Auto-attack loop (`RpgAutoAttackController`) | `AbilityAutoCaster` component (interval → `TryCast`) or game code |
| Scene target selector (`RpgTargetSelector`) | `AbilityTargetSelector` component (domain has `EffectTargetSelector` for effects only) |
| Dodge/evade with cooldown (`RpgEvadeController`) | `AbilityEvadeController`, or lean on `evasion_chance` property + `Invulnerable` state |
| Death handler (`RpgDeathHandler`) | Trivial: subscribe `AbilityUnitBehaviour.OnDied` → SetActive/Destroy; can ship as `AbilityDeathHandler` |
| IDamageable/IHealable bridge (`RpgStatsDamageableBridge`) | `AbilityUnitDamageableBridge : IDamageable, IHealable` → `AbilityUnitBehaviour.ApplyDamage/Heal` |
| Shared combat-receiver interface (`IRpgCombatReceiver`, `RpgDamageInfo`) | Not needed — Abilities addresses units by `UnitId` through `DamageService`; drop the interface |
| NoCode bridge (`RpgConditionAdapter`, `RpgNoCodeAction`) | `AbilityNoCodeAction` (architecture Bridge layer, not built) |
| Editor SO inspectors/drawers (`Editor/Rpg/*`) | UITK Ability Designer + SO inspectors (architecture §5, not built) |

---

## Core type/member migration recipes

| Old (Neo.Rpg) | New (Neo.Abilities) recipe |
|---|---|
| `RpgCharacter` (component) | `AbilityUnitBehaviour` (+ `AbilityCasterBehaviour` for casting) |
| `character.Damage(amt)` | `DamageService.ApplyDamage(unit.System, UnitId.None, unit.Id, amt, AbilityDamageTypes.Pure)` or `abilityUnit.ApplyDamage(amt)` |
| `character.DamageType(type, amt)` | `DamageService.ApplyDamage(system, sourceId, targetId, amt, type)` (type ∈ Physical/Magical/Pure or custom) |
| `character.Heal(amt)` | **gap** → `AbilityUnitBehaviour.Heal(amt)` (add) / `unit.Resources.Increase(AbilityResourceIds.Health, amt)` |
| `character.HpValue` / `MaxHpValue` | `abilityUnit.CurrentHealth` / `MaxHealth` (domain `unit.Health` / `unit.MaxHealth`) |
| `character.HpPercentValue` | `abilityUnit.HealthNormalized` |
| `character.IsDead` | `!abilityUnit.IsAlive` |
| `character.IsInvulnerable` | `unit.HasState(AbilityStates.Invulnerable)` |
| `character.LockInvulnerable()` / `SetInvulnerable(true)` | `unit.SetPermanentState(AbilityStates.Invulnerable, true)` |
| `character.GetStat(id)` | `unit.GetProperty(propertyId)` (map `RpgStatPreset`→`AbilityProperties` id) |
| `character.AddStatBase/SetStatBase(id, v)` | `unit.SetBaseProperty(propertyId, v)` |
| `character.Spend(res, amt)` | check `unit.Resources.GetCurrent(res) >= amt` then `unit.Resources.Decrease(res, amt)` (or ability `AbilityCost`) |
| `character.Refill/Increase/Decrease(res, amt)` | `unit.Resources.Increase/Decrease(res, amt)` |
| `character.SetMaxResource/AddMaxResource` | modifier w/ `max_health_bonus` (or resource pool Max via `ResourcePoolModel`) |
| `character.GetOutgoingDamageMultiplier()` | `unit.GetProperty(AbilityProperties.OutgoingDamageMul, 1f)` |
| `character.GetIncomingDamageMultiplier(type)` | `unit.GetProperty(AbilityProperties.IncomingDamageMul, 1f)` + armor/`magic_resist_percent` (baked into `DamageService`) |
| `character.GetMovementSpeedMultiplier()` | **semantic change** → `unit.GetProperty(AbilityProperties.MoveSpeed)` is an absolute value, not a multiplier |
| `character.ApplyBuff(BuffDefinition)` / `ApplyBuffById(id)` | `system.ApplyModifier(modifierId, casterId, targetId)` (`ModifierDefinition`) |
| `character.ApplyStatus(StatusEffectDefinition)` | same — statuses are `ModifierDefinition` with `States[]` + `IsDebuff` |
| `character.RemoveBuff/RemoveStatus(id)` | `remove_modifier` effect op or `system.Modifiers.Remove(instance)` |
| `character.HasBuff/HasStatus(id)` | query `system.Modifiers.GetModifiers(unitId, list)` |
| `character.OnDamagedEvent/OnHealedEvent/OnDeathEvent` | `AbilityUnitBehaviour.OnDamaged/OnHealed/OnDied` (UnityEvents) |
| `character.OnBuffApplied/OnStatusApplied` | `OnModifierApplied/OnModifierRemoved` (string id) |
| `character.SetLevel/AddXp/AddUpgradePoints/UpgradeStat` | **gap** → new progression adapter over `Neo.Core.Level` |
| `character.SaveProfile/LoadProfile` | **gap** → new `AbilityUnitPersistence` over `Neo.Save` |
| `RpgCharacterTemplate` (SO) | `UnitTemplate` (SO) |
| `RpgResourceDefinition` | `UnitResourceConfig` |
| `RpgStatDefinition` | `UnitPropertyDefault` |
| `RpgStatId` / `RpgStatPreset` (enum) | string property id + `AbilityProperties` constants (open registry) |
| `BuffDefinition` / `InlineBuffEntry` | `ModifierDefinition` / inline `ModifierBlueprint` |
| `BuffStatModifier` / `BuffStatType` | `PropertyContribution {PropertyId, Op(Add/Mul/Max), Value}` |
| `StatusEffectDefinition` (`BlocksActions`) | `ModifierDefinition` w/ `States` (e.g. `AbilityStates.Stunned`) |
| `RpgProgressionDefinition/RpgStatUpgradeRule/RpgStatGrowthDefinition` | **gap** — no equivalent; new progression system |
| `RpgAttackController` | `AbilityCasterBehaviour` + `AbilityDefinition` cast pipeline |
| `RpgAttackDefinition` / `RpgAttackPreset` | `AbilityDefinition` |
| `RpgAttackDeliveryType` (Direct/Projectile/Area) | `AbilityDeliveryType` + `TargetingMode` |
| `RpgHitMode` (Damage/Heal) | effect ops (`damage`/`heal`) in `AbilityDefinition` |
| `RpgProjectile` | `AbilityProjectileBehaviour` (`ISpawnedAbilityEntity`, pooled) |
| `RpgContactDamage` | **gap** → `AbilityContactDamage` or aura modifier + area op |
| `RpgAutoAttackController` | **gap** → auto-caster component / game code |
| `RpgTargetSelector` | **gap** → target-selector component (`EffectTargetSelector` is effect-scoped) |
| `RpgEvadeController` | **gap** → evade component / `evasion_chance` + `Invulnerable` state |
| `RpgDeathHandler` | subscribe `OnDied` → SetActive/Destroy (or ship `AbilityDeathHandler`) |
| `AuraWeapon` / `MeleeWeapon` | `AbilityDefinition` Area delivery + damage op, or aura `ModifierDefinition` |
| `IRpgCombatReceiver` / `RpgDamageInfo` | drop; use `UnitId` + `DamageService` |
| `RpgCombatMath` (static) | `DamageService` pipeline + `PropertyAggregator` |
| `RpgStatsDamageableBridge` | **gap** → `AbilityUnitDamageableBridge` |
| `RpgGameObjectEvent/RpgStringEvent/RpgAttackEvent` | plain `UnityEvent<GameObject>/<string>` or `AbilityEventArgs` |

---

## (b) Per-area migration plans

### Area 1 — asmdef references (6 external + Neo.Rpg self)

Referenced by name **and** by GUID `30327754e4b90f7498231f1ac8c4355d` (Neo.Rpg asmdef). Remove every ref
when the module is deleted; add `Neo.Abilities` where the area still needs combat.

| asmdef | Ref form | Action |
|---|---|---|
| `Scripts/Rpg/Neo.Rpg.asmdef` | (the module) | Delete file + `.meta` |
| `Editor/Neo.Editor.asmdef` | name `"Neo.Rpg"` | Remove ref; add `"Neo.Abilities"` (new inspectors) |
| `Scripts/NPC/Neo.NPC.asmdef` | name `"Neo.Rpg"` | Replace with `"Neo.Abilities"` |
| `Scripts/Tools/Components/Neo.Tools.Components.asmdef` | name `"Neo.Rpg"` | Replace with `"Neo.Abilities"` (for the new bridge) |
| `Samples/Demo/Scripts/Neo.Rpg.Demo.asmdef` | name `"Neo.Rpg"` | Rename asmdef → `Neo.Abilities.Demo`, swap ref to `"Neo.Abilities"` (or delete with demos) |
| `Tests/Edit/Neo.Editor.Tests.asmdef` | **GUID** `30327754…` (already also refs `Neo.Abilities`) | Remove the GUID ref |
| `Tests/Play/Neo.Tests.Play.asmdef` | name `"Neo.Rpg"` | Remove ref; add `"Neo.Abilities"` if porting tests |

### Area 2 — NPC combat (asmdef Neo.NPC) — HIGH

- `Scripts/NPC/Combat/NpcRpgCombatBrain.cs` — uses `RpgTargetSelector`, `RpgAttackController`, `RpgCharacter`,
  `RpgGameObjectEvent`, `RpgStringEvent`, `IRpgCombatReceiver`. Rewrite as **`NpcAbilityBrain`**: resolve
  `AbilityUnitBehaviour` + `AbilityCasterBehaviour`, acquire target (new target-selector), call `TryCastAtUnit`,
  react to `OnCastFailed`. Replace `RpgGameObjectEvent`/`RpgStringEvent` with `UnityEvent<GameObject>`/`<string>`.
- `Scripts/NPC/Combat/NpcCombatPreset.cs` — holds `RpgAttackPreset`. Repoint to `AbilityDefinition`.
- Doc: `Docs/NPC/Combat/NpcRpgCombatBrain.md`, `Docs/NPC/Combat/NpcCombatScenarios.md`, `Docs/NPC/README.md`.

### Area 3 — Tools/Components/AttackSystem (asmdef Neo.Tools.Components) — LOW/MED

- `RpgStatsDamageableBridge.cs` — **only real type dependency here**. `TakeDamage(int)`→`ch.Damage(...)`,
  `Heal(int)`→`ch.Heal(...)` over a serialized `RpgCharacter`. Replace with **`AbilityUnitDamageableBridge`**
  forwarding to `AbilityUnitBehaviour.ApplyDamage/Heal`. This is what forces the asmdef's Neo.Rpg ref.
- `AdvancedAttackCollider.cs`, `AttackExecution.cs`, `Evade.cs`, `Health.cs` — reference `Neo.Rpg` **only in
  `[LegacyComponent("…")]` / `[Obsolete("…")]` string literals**. **No compile break.** Update the strings to
  point at Abilities components (cosmetic; do in same pass to avoid stale guidance).
- Docs: `Docs/Tools/Components/AttackSystem/RpgStatsDamageableBridge.md`, `…/AttackSystem/README.md`.

### Area 4 — Editor/Rpg (asmdef Neo.Editor) — LOW (delete)

Delete all four with the module (they are custom editors/drawers for deleted types):
- `Editor/Rpg/RpgCharacterEditor.cs` (`CustomEditor(RpgCharacter)`)
- `Editor/Rpg/RpgStatIdDrawer.cs` (`PropertyDrawer(RpgStatId)`, `RpgStatPreset`)
- `Editor/Rpg/Data/RpgStatGrowthDefinitionEditor.cs` (`CustomEditor(RpgStatGrowthDefinition)`)
- `Editor/Rpg/Data/RpgStatGrowthRuleDrawer.cs` (`PropertyDrawer(RpgStatGrowthRule)`, `RpgStatGrowthMode`, `RpgStatFormulaType`)

New Abilities inspectors live under `Scripts/Abilities/Editor` (architecture §5, not built yet). Remove the
`Neo.Rpg` name ref from `Neo.Editor.asmdef`.

### Area 5 — Samples/Demo — HIGH (rebuild or delete)

Runtime scripts (asmdef `Neo.Rpg.Demo`), all in namespace `Neo.Rpg.Demo`:
- `RpgCharacterQuickDemo.cs` — heavy: `RpgCharacterTemplate` build, `RpgResourceDefinition`, `RpgStatId`,
  `RpgProgressionDefinition`, `RpgStatUpgradeRule`, `RpgResourceModifier`, `UpgradeStat`. Depends on the
  **progression gap** — cannot port cleanly until progression adapter exists.
- `RpgCombatDemoController.cs` — `RpgCharacter`, `RpgAttackController` orchestration.
- `RpgDemoUI.cs` — `AddMaxResource`/`RestoreResource`/`Damage`/`Heal` (uses `RpgResourceId.Hp` from Neo.Core).
- `RpgWorldHealthBars.cs` — tracks `RpgCharacter` list → `AbilityUnitBehaviour` + `HealthNormalized`.
- `VampireSurvivorDemoController.cs` — `RpgCharacter`, `RpgContactDamage`, `RpgDeathHandler` (contact/death gaps).
- `Samples/Demo/Editor/DemoSceneFixerExtra.cs` — namespace `Neo.Editor.Rpg`, **not under any asmdef** (compiles
  into predefined `Assembly-CSharp-Editor`); scene-fixer utility referencing `RpgAttackController`,
  `RpgProjectile`, `RpgContactDamage`, `RpgCharacter`, `RpgCharacterTemplate`, `RpgAttackDefinition`. Delete with demos.

Recommendation: delete the RPG demos and replace with the planned `Samples/Demo/Scenes/Abilities/AbilityShowcase`
(architecture §7). Rename asmdef `Neo.Rpg.Demo` → `Neo.Abilities.Demo`.

Related demo docs: `Docs/Samples.md`, `Docs/VampireSurvivor_Guide.md`, `Docs/GettingStarted.md`.

### Area 6 — Tests — HIGH (delete + re-port coverage)

| Test file | asmdef | Verdict |
|---|---|---|
| `Tests/Edit/RPG/RpgCharacterTests.cs` | Neo.Editor.Tests | **Delete**; port to `Tests/Edit/Abilities` (resource/property/level/save math) |
| `Tests/Edit/RPG/RpgCombatEdgeTests.cs` | Neo.Editor.Tests | **Delete**; port (`RpgCombatMath`→`DamageService`, buff/status→modifier, `RpgEffectShelf` persistence) |
| `Tests/Play/RPG/RpgCombatPlayModeTests.cs` | Neo.Tests.Play | **Delete**; port melee/projectile/contact to `AbilityCasterBehaviour`/`AbilityProjectileBehaviour` |
| `Tests/Play/RPG/RpgCombatTargetingPlayModeTests.cs` | Neo.Tests.Play | **Delete**; port auto-attack/contact once those components exist |
| `Tests/Edit/Editor/SampleSceneCoverageTests.cs` | Neo.Editor.Tests | **Rewrite** — references demo/component types by **string name** via `FindType("Neo.Rpg.Components.RpgCharacter")` etc; retarget to Abilities scene/types or drop RPG scene checks |
| `Tests/Play/SampleRuntimeSceneSmokeTests.cs` | Neo.Tests.Play | **Rewrite** — loads `Scenes/RpgCombatNpcDemo.unity` and `FindRequiredComponent("Neo.Rpg.Demo.*")`; retarget to the new AbilityShowcase scene |

Note the two coverage/smoke tests use **string** type names, so they compile even after deletion but will
**fail at runtime** (types/scenes missing). They must be rewritten, not just left.

### Area 7 — Scenes / Prefabs / .asset carrying Rpg script GUIDs (25 assets)

Deleting the scripts first turns these into **"missing script"** references. Migrate/replace components in each
asset (or delete the asset) **before** deleting the module. GUIDs harvested from `Scripts/Rpg/**/*.cs.meta`.

| Asset | Rpg components referenced |
|---|---|
| `Samples/Demo/Prefabs/CubeEnemy.prefab` | RpgCharacter, RpgContactDamage |
| `Samples/Demo/Prefabs/CubeEnemy_New.prefab` | RpgCharacter, RpgContactDamage, RpgDeathHandler |
| `Samples/Demo/Prefabs/SphereEnemy_Ranged.prefab` | RpgCharacter, RpgContactDamage, RpgAttackController, RpgDeathHandler, RpgAutoAttackController |
| `Samples/Demo/Prefabs/EnemyEnergySphere.prefab` | RpgProjectile |
| `Samples/Demo/Prefabs/EnemyProjectile.prefab` | RpgProjectile |
| `Samples/Demo/Data/RpgCombatNpcDemo/Assets/ArrowProjectile.prefab` | RpgProjectile |
| `Samples/Demo/Scenes/RpgCombatNpcDemo.unity` | RpgCharacter ×3, RpgTargetSelector, RpgContactDamage, RpgAttackController ×2 |
| `Samples/Demo/Scenes/RpgCharacterQuickDemo.unity` | RpgCharacter ×2 |
| `Samples/Demo/Scenes/VampireSurvivorMCP.unity` | RpgCharacter, AuraWeapon |
| `Samples/Demo/Scenes/Demo_RPG.unity/RPG_Demo.unity` | RpgCharacter ×2 (note: scene inside a folder literally named `Demo_RPG.unity`) |
| `Assets/Scenes/Demoi.unity` | RpgAttackController |
| `Assets/Scenes/First Person Controller.prefab` | RpgAttackController |
| `Assets/Scenes/AutoSaves/Demoi_AutoSave.unity` | RpgAttackController |
| `Assets/Scenes/AutoSaves/RPG_Demo_AutoSave.unity` | RpgCharacter ×2 |
| `Assets/Scenes/AutoSaves/VampireSurvivorMCP_AutoSave.unity` | AuraWeapon |

Watch-outs: (1) `Assets/Scenes/First Person Controller.prefab` and `Assets/Scenes/Demoi.unity` are **outside the
Samples tree** — real user/project content, not disposable demo. (2) `Assets/Scenes/AutoSaves/*` are editor
autosaves; safe to delete outright.

### Area 8 — ScriptableObject assets of Rpg SO types (11 `.asset`)

Recreate as Abilities SOs (or delete with the demos). Type ← script GUID.

| Asset | Rpg SO type → Abilities SO |
|---|---|
| `Samples/Demo/Data/RpgCombatNpcDemo/Assets/PlayerCharacterTemplate.asset` | RpgCharacterTemplate → UnitTemplate |
| `Samples/Demo/Data/RpgCombatNpcDemo/Assets/MeleeNpcCharacterTemplate.asset` | RpgCharacterTemplate → UnitTemplate |
| `Samples/Demo/Data/RpgCombatNpcDemo/Assets/RangedNpcCharacterTemplate.asset` | RpgCharacterTemplate → UnitTemplate |
| `Samples/Demo/Data/RpgCombatNpcDemo/Assets/PlayerSwordSlash.asset` | RpgAttackDefinition → AbilityDefinition |
| `Samples/Demo/Data/RpgCombatNpcDemo/Assets/EnemyArrow.asset` | RpgAttackDefinition → AbilityDefinition |
| `Samples/Demo/Data/EnemyRangedAttack.asset` | RpgAttackDefinition → AbilityDefinition |
| `Samples/Demo/Data/RpgCombatNpcDemo/Assets/EnemyArrowPreset.asset` | RpgAttackPreset → AbilityDefinition |
| `Samples/Demo/Data/PlayerStatGrowth.asset` | RpgStatGrowthDefinition → **gap** (no growth SO) |
| `Samples/Demo/Data/EnemyMeleeStatGrowth.asset` | RpgStatGrowthDefinition → **gap** |
| `Samples/Demo/Data/EnemyRangedStatGrowth.asset` | RpgStatGrowthDefinition → **gap** |

### Area 9 — Docs

- **Delete whole folder** `Assets/Neoxider/Docs/Rpg/` (24 `.md`, incl. `Data/BuffDefinition.md`, `InternalTypes.md`,
  `RpgCombatant.md`, `RpgStatsManager.md`, and per-component pages) + `Docs/Rpg.meta`.
- **Delete** `Docs/NPC/Combat/NpcRpgCombatBrain.md`, `Docs/Tools/Components/AttackSystem/RpgStatsDamageableBridge.md`.
- **Rewrite / de-reference** cross-linking docs: `Docs/README.md`, `Docs/GettingStarted.md`, `Docs/Samples.md`,
  `Docs/UsefulComponents.md`, `Docs/VampireSurvivor_Guide.md`, `Docs/DEPRECATED_OR_REMOVAL_CANDIDATES.md`,
  `Docs/Core/HealthComponent.md`, `Docs/Core/Level.md`, `Docs/NPC/README.md`, `Docs/NPC/Combat/NpcCombatScenarios.md`,
  `Docs/Progression/{README,ProgressionManager,Scenarios}.md`, `Docs/Network/{README,Multiplayer_Guide,NoCode_Network_Spec}.md`,
  `Docs/Tools/{README,InternalTypes}.md`, `Docs/Tools/Components/AttackSystem/README.md`.
- **Package-level**: `Assets/Neoxider/README.md` (5), `PROJECT_SUMMARY.md` (3), `MODULE_STRUCTURE.md` (**34** — the
  biggest single doc rewrite), `CHANGELOG.md` (add migration entry), and the shipped skill
  `Skill/neoxider-tools/SKILL.md` (8) + `references/modules.md` (2) + `references/{idioms,tools,avoid-nocode}.md`.
- **Out of scope but references RPG** (`MultiplayerMirrorCourse/*`, top-level `README.md`, `dev-docs/*`) — update
  opportunistically; not required for compile.

---

## (c) Recommended deletion sequence (compile never red for long)

The dependency chain is: **Data/assets → scene/prefab component instances → dependent code (NPC, bridge, demos,
tests) → editors → the module + asmdef**. Build the Abilities replacements first, migrate consumers, delete last.

1. **Build the missing Neo.Abilities adapters first** (unblocks everything): `AbilityUnitDamageableBridge`,
   `AbilityContactDamage`/aura modifier, `AbilityDeathHandler`, target-selector + auto-caster, and the
   progression + persistence adapters. Add `Heal(float)` to `AbilityUnitBehaviour`. Compile stays green
   (pure additions to Neo.Abilities). Author the new demo SOs (`UnitTemplate`/`AbilityDefinition`/`ModifierDefinition`).
2. **Migrate `Neo.Tools.Components`**: add `AbilityUnitDamageableBridge`, keep `RpgStatsDamageableBridge`
   temporarily, flip the asmdef to reference **both** `Neo.Rpg` and `Neo.Abilities`. Green.
3. **Migrate `Neo.NPC`**: add `NpcAbilityBrain` + repoint `NpcCombatPreset` to `AbilityDefinition`; asmdef refs
   both modules. Green.
4. **Rebuild/replace scene & prefab instances** (Area 7) and recreate SO assets (Area 8) onto Abilities
   components while the old scripts still exist — so no missing-script gap opens. Update `First Person Controller.prefab`
   and `Demoi.unity` (non-demo project assets) or confirm they're disposable.
5. **Swap the demos** (Area 5): delete RPG demo scripts/scenes/assets, add AbilityShowcase; rename asmdef
   `Neo.Rpg.Demo`→`Neo.Abilities.Demo` referencing only `Neo.Abilities`.
6. **Port tests** (Area 6): add Abilities edit/play suites; delete the 4 RPG test files; rewrite the 2
   string-name coverage/smoke tests to the new scene/types. Remove the Neo.Rpg GUID ref from `Neo.Editor.Tests`,
   the name ref from `Neo.Tests.Play`.
7. **Update the leftover attribute strings** in the 4 AttackSystem files (no compile impact) and drop the
   now-unused `Neo.Rpg` ref from `Neo.Tools.Components`.
8. **Delete `Editor/Rpg/*`** (Area 4) and remove the `Neo.Rpg` ref from `Neo.Editor.asmdef`.
9. **Delete `Scripts/Rpg/`** (module + `Neo.Rpg.asmdef` + `.meta`). Now nothing references it.
10. **Docs pass** (Area 9): delete `Docs/Rpg/`, rewrite cross-refs, update README/PROJECT_SUMMARY/MODULE_STRUCTURE,
    add the CHANGELOG migration table below.

Validate each step with the compile-check script (`artifacts/compilecheck`, per project convention) + the
EditMode suite before proceeding.

---

## CHANGELOG migration table (top 20 public types)

| Old type (Neo.Rpg) | New type / path (Neo.Abilities) |
|---|---|
| `Neo.Rpg.Components.RpgCharacter` | `Neo.Abilities.AbilityUnitBehaviour` (`Scripts/Abilities/Components`) |
| `Neo.Rpg.RpgCharacterTemplate` | `Neo.Abilities.UnitTemplate` (`Scripts/Abilities/Data`) |
| `Neo.Rpg.RpgResourceDefinition` | `Neo.Abilities.UnitResourceConfig` |
| `Neo.Rpg.RpgStatDefinition` | `Neo.Abilities.UnitPropertyDefault` |
| `Neo.Rpg.RpgStatId` / `RpgStatPreset` | string property id + `Neo.Abilities.AbilityProperties` |
| `Neo.Rpg.BuffDefinition` | `Neo.Abilities.ModifierDefinition` |
| `Neo.Rpg.StatusEffectDefinition` | `Neo.Abilities.ModifierDefinition` (with `States`) |
| `Neo.Rpg.BuffStatModifier` / `BuffStatType` | `Neo.Abilities.PropertyContribution` / `PropertyOp` |
| `Neo.Rpg.InlineBuffEntry` | inline `Neo.Abilities.ModifierBlueprint` |
| `Neo.Rpg.Components.RpgAttackController` | `Neo.Abilities.AbilityCasterBehaviour` + `AbilityDefinition` |
| `Neo.Rpg.RpgAttackDefinition` | `Neo.Abilities.AbilityDefinition` |
| `Neo.Rpg.RpgAttackPreset` | `Neo.Abilities.AbilityDefinition` |
| `Neo.Rpg.Components.RpgProjectile` | `Neo.Abilities.AbilityProjectileBehaviour` |
| `Neo.Rpg.RpgContactDamage` | `Neo.Abilities.AbilityContactDamage` *(new adapter)* |
| `Neo.Rpg.Components.RpgAutoAttackController` | `Neo.Abilities.AbilityAutoCaster` *(new adapter)* |
| `Neo.Rpg.Components.RpgTargetSelector` | `Neo.Abilities.AbilityTargetSelector` *(new adapter)* |
| `Neo.Rpg.Components.RpgEvadeController` | `evasion_chance` property + `Invulnerable` state *(or new adapter)* |
| `Neo.Rpg.RpgDeathHandler` | `AbilityUnitBehaviour.OnDied` → handler *(or `AbilityDeathHandler`)* |
| `Neo.Rpg.Runtime.RpgCombatMath` | `Neo.Abilities.DamageService` + `PropertyAggregator` |
| `Neo.Tools.RpgStatsDamageableBridge` | `Neo.Abilities.AbilityUnitDamageableBridge` *(new adapter)* |
| `Neo.Rpg.RpgProgressionDefinition` / `RpgStatUpgradeRule` / `RpgStatGrowthDefinition` | *no direct equivalent* — new progression adapter over `Neo.Core.Level` |

---

## (d) Risks

- **R1 — Missing-script corruption in scenes/prefabs (HIGH).** 25 assets embed Rpg script GUIDs; 3 live
  **outside** the demo tree (`Assets/Scenes/First Person Controller.prefab`, `Assets/Scenes/Demoi.unity`, plus
  autosaves). Deleting `Scripts/Rpg` before migrating these silently drops components and their serialized
  wiring (UnityEvents, refs). Migrate/replace instances **before** step 9, and treat non-Samples assets as real
  project content, not disposables.

- **R2 — Feature gaps have no drop-in target (HIGH).** Progression (XP/level/upgrade-points/stat-growth),
  persistence (`SaveProfile/LoadProfile`), and **Mirror networking** (`Net*`, SyncVar snapshot, authority modes)
  exist in `RpgCharacter` but **not** in Neo.Abilities. `RpgCharacterQuickDemo`, the multiplayer RPG course, and
  any project relying on saved/networked RPG state cannot be ported until these adapters are built. This is the
  critical-path blocker, not the mechanical deletion.

- **R3 — Resource-id value mismatch corrupts saved/authored data (MED-HIGH).** `RpgResourceId.Hp = "HP"`,
  `"Mana"`, `"Stamina"` vs Abilities `AbilityResourceIds.Health = "health"`, `Mana = "mana"`. Pool keys and any
  persisted `RpgCharacterProfileData` won't line up after migration — health bars/regen read the wrong pool.
  Needs an explicit id-remap in the data migration (and in `UnitTemplate` authoring), not a rename-and-hope.

- **R4 — String-referenced tests/scenes fail silently, not at compile (MED).** `SampleSceneCoverageTests` and
  `SampleRuntimeSceneSmokeTests` resolve `Neo.Rpg.*` types and RPG scenes by **string name** (`FindType`,
  `FindRequiredComponent`, scene paths). They keep compiling after deletion but break at runtime — easy to miss
  in a "does it compile?" check. Also the 4 `AttackSystem` `[Obsolete]/[LegacyComponent]` strings will point at
  non-existent `Neo.Rpg.*` types, shipping misleading guidance. Both need explicit updates.

- **R5 — Semantic mismatches in the damage/stat model (MED).** `GetMovementSpeedMultiplier()` (a multiplier)
  maps to `AbilityProperties.MoveSpeed` (an absolute value); RPG buff/status resist math (`GetIncomingDamageMultiplier`,
  typed resists, `BlocksActions`) is folded into `DamageService`'s armor/`magic_resist_percent`/state pipeline
  with different formulas. Naive 1:1 field copies will change balance. Buff+status→single Modifier and
  two-array effect shelves→`ModifierEngine` also need behavior re-validation (stacking, expiry, DoT ticks) —
  port the RPG edge tests (`RpgCombatEdgeTests`) as Abilities tests to lock parity.
