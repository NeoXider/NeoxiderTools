# Neo.Rpg: audit

Status: full source review completed; focused fixes and regression tests authored. Compilation and test execution remain centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Rpg/Neo.Rpg.asmdef`.
- Scope: 59 C# files; nested assemblies excluded.

Проверены все 59 файлов инвентаря. Исправлены ошибки сохранения стаков, восстановления upgrades,
регенерации, боевых множителей, повторной привязки UI и lifecycle контроллеров. Проверки Unity
не запускались по контракту; ниже отдельно указаны реальные статические проверки и остаточные риски.

## Scope

- `Assets/Neoxider/Scripts/Rpg/Bridge/RpgConditionEvaluationMode.cs`
- `Assets/Neoxider/Scripts/Rpg/Bridge/RpgNoCodeAction.cs`
- `Assets/Neoxider/Scripts/Rpg/Bridge/RpgNoCodeActionType.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/RpgAttackController.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/RpgAutoAttackController.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/RpgDeathHandler.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/RpgEvadeController.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/RpgProjectile.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/RpgResourceBinding.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/RpgStatBinding.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/RpgTargetSelector.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/Weapons/AuraWeapon.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/Weapons/MeleeWeapon.cs`
- `Assets/Neoxider/Scripts/Rpg/Components/Weapons/RpgContactDamage.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/BuffDefinition.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/BuffStatModifier.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/InlineBuffEntry.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgAttackDefinition.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgAttackEffectRefs.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgAttackPreset.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgButtonBinding.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgCharacterTemplate.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgLevelGrowthMode.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgProgressionDefinition.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgRegenDefinition.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgRegenMode.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgResourceDefinition.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgResourceModifier.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgStatDefinition.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgStatGrowthDefinition.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgStatId.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgStatPreset.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgStatUpgradeRule.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/RpgTargetQuery.cs`
- `Assets/Neoxider/Scripts/Rpg/Data/StatusEffectDefinition.cs`
- `Assets/Neoxider/Scripts/Rpg/Enums/BuffStatType.cs`
- `Assets/Neoxider/Scripts/Rpg/Enums/RpgAttackDeliveryType.cs`
- `Assets/Neoxider/Scripts/Rpg/Enums/RpgHitMode.cs`
- `Assets/Neoxider/Scripts/Rpg/Enums/RpgInputTriggerType.cs`
- `Assets/Neoxider/Scripts/Rpg/Enums/RpgMouseButton.cs`
- `Assets/Neoxider/Scripts/Rpg/Enums/RpgTargetSelectionMode.cs`
- `Assets/Neoxider/Scripts/Rpg/Events/RpgAttackEvent.cs`
- `Assets/Neoxider/Scripts/Rpg/Events/RpgGameObjectEvent.cs`
- `Assets/Neoxider/Scripts/Rpg/Events/RpgStringEvent.cs`
- `Assets/Neoxider/Scripts/Rpg/Interfaces/IRpgAttackNetworkAdapter.cs`
- `Assets/Neoxider/Scripts/Rpg/Interfaces/IRpgCharacterNetworkAdapter.cs`
- `Assets/Neoxider/Scripts/Rpg/Interfaces/IRpgCombatReceiver.cs`
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgActiveEffectEntries.cs`
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterNetworkCommand.cs`
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterProfileData.cs`
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterProfileService.cs`
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs`
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgCombatMath.cs`
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgEffectShelf.cs`
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgResourceRuntime.cs`
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgStatResolver.cs`
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgStatRuntime.cs`
- `Assets/Neoxider/Scripts/Rpg/Runtime/RpgTargetingUtility.cs`

## Findings and fixes

All 59 files listed above were read completely: 7,100 baseline source lines. The assembly definition
was read as dependency context and was not edited. No source under `Combat/`, `Bridge/Condition/`,
`RpgNetwork/`, or samples was edited. Existing tests were read only as context; regressions use three
new, exact filenames, avoiding neighboring owners' longer prefixes.

### Review coverage

| Area | Files reviewed | Contracts checked |
| --- | ---: | --- |
| Bridge | 3 | Every action switch arm, missing target/controller behavior, typed API routing, primitive/ID arguments, enum stability. |
| Components and weapons | 12 | Initialization, disable/reactivation, delayed casts, receiver identity, physics query deduplication, pooled targets, timer/listener ownership, reactive binding, damage/death/revival. |
| Data | 21 | Serialized field/API preservation, asset-only configuration, ID canonicalization, progression/upgrade rules, growth formulas, resource limits, regen units, attack cost and prefab configuration. |
| Enums | 6 | Serialized numeric values, intended consumers, legacy aliases, unconsumed modifier kinds. |
| Events | 3 | Serializable UnityEvent payloads and assembly identity; no changes required. |
| Interfaces | 3 | Transport-neutral boundaries, custom receiver path, authority/cost hooks; no interface changes. |
| Runtime | 11 | Resource/stat invariants, resolver order, effect stack projection and callback mutation, profile clone/sanitize/roundtrip, UTC versus simulation clocks, standalone service usage. |

The runtime retains existing `SaveProvider`, reactive properties, `RpgStatResolver`, `RpgEffectShelf`,
`RpgCombatMath`, input compatibility wrappers, and `TimerObject`. Skill references for combat,
extensions, systems, and scene tools were consulted; no new parallel combat/timer/binding framework
or dependency was introduced. Runtime logs remain gated/existing; no new logs, scene searches,
version-specific Unity IDs, package references, or conditional compilation branches were introduced.

### Demonstrated defects fixed

Evidence below is source-path analysis with public-behavior regressions authored for central execution;
it is not a claim that those tests have been run. Source paths are relative to
`Assets/Neoxider/Scripts/Rpg/`.

| ID / severity | Trigger, previous observable consequence, source | Implemented fix and regression |
| --- | --- | --- |
| R01 / high | Serialize a profile containing a three-stack buff. `Runtime/RpgCharacterProfileData.cs:Clone` omitted `Stacks`; `RpgCharacterProfileService.Serialize` always cloned, reducing saved/replicated stacks to one. | Deep copies include buff stacks. `ProfileSerialization_PreservesBuffStacksAndClonesEntries` checks the roundtrip value and independence of the copied entry. |
| R02 / medium | Profile JSON has null collections or any public profile list contains null rows. `Sanitize` dereferenced rows/lists; `Clone` ran before sanitize and also dereferenced them. Valid neighboring state could fail to load/save. | Initialize null collections and discard null rows without mutating the serializer input. `ProfileSerialization_DropsNullRowsWithoutMutatingSource` and `ProfileDeserialization_NullCollectionsBecomeUsableEmptyLists` exercise real JSON and retained values. |
| R03 / high | Apply an older profile with no upgrade row after upgrading a stat. `Components/RpgCharacter.cs:ApplyProfile` cleared investments but left `RpgStatRuntime.UpgradeCount`; final stat/resource bonuses survived the rollback. | Reset all runtime stat upgrade counts before restoring rows. `ApplyProfile_RemovesUpgradeBonusesMissingFromRestoredProfile` checks Wisdom, HP Max, upgrade level, and Mana regen. |
| R04 / medium | Buff a tick-regenerated pool. `Runtime/RpgCharacterResourceService.cs:TickRegen` used raw definition values, ignoring `ResolvedRegenPerSecond`. A 10-per-half-second pool with +4/s and +50% still regenerated 10 per tick instead of 18. | Tick amounts use resolved rate times interval. Runtime construction supplies the base rate, preserving standalone `Build` + tick usage and existing invalid-interval clamping. `TickRegen_UsesFlatAndPercentBuffsWithoutChangingTickCadence` covers both tick modes. |
| R05 / medium | Invest in `RpgResourceModifierKind.AddRegenFlat`. `Runtime/RpgStatResolver.cs:ResolveRegen` never read upgrade rules. Change a scaling stat base and `RpgCharacter` refreshed only that stat, leaving resource regen stale. | Add a four-argument regen overload and call it from the facade; keep the original overload. Base-stat mutations refresh dependent values. `RegenResolver_AppliesInvestedUpgradeBonusBeforeBuffPercent`, the profile upgrade regression, and `BaseStatChanges_ImmediatelyUpdateDependentResourceRegen` check resolved outputs. |
| R06 / medium | Change a percent-regenerated pool's Max. Cached continuous regen could stay at the old percentage; tick regen now also needs its resolved rate refreshed. `RpgCharacterResourceService.SetMax` / `AddMax`. | Retain the configured regen resolver, refresh it after Max changes, and clear it on Build/Clear. `ResourceMaxChange_RefreshesPercentTickRegen` checks 20 then 30 units after Max becomes 200 then 300. |
| R07 / high | Disable a caster GameObject during cast delay. Unity stops its coroutine, but `_castCoroutine` stayed non-null forever. A start listener could also reenter before the original coroutine was assigned or deactivate before it was started. `Components/RpgAttackController.cs:BeginAttack`, delayed cast, missing disable cleanup. | Track an accepted cast version/busy state, stop and clear on disable, guard event-driven cancellation, and clear delayed state in `finally`. `DeactivatedCaster_CancelsPendingCastAndCanCastAfterReactivation` and `AttackStartedListener_CannotReenterOrResolveAfterDeactivation` check resolution counts, cost, busy state, and reuse. Accepted cost/cooldown remain on cancellation. |
| R08 / high | Evade ends after Update already released its lock, is disabled, changes invulnerability setting, or is disabled by its start listener. `Components/RpgEvadeController.cs:FinishEvade` removed a lock unconditionally from a newly resolved receiver; there was no disable cleanup. This could leak invulnerability or consume another skill's lock. | Track the actual lock receiver, release only the owned lock once, cancel pending completion on disable, clear reactive state, and finish setup before start callbacks. Three expiry/disable/start-listener Play Mode regressions assert exact profile lock counts. |
| R09 / medium | Cast a projectile with no prefab or denied spawn authority. Validation formerly happened in `SpawnProjectile`, after resources/cooldown were consumed, and the public cast had already reported success. | Preflight prefab, active controller, and adapter spawn permission in `CanUseAttack`, before costs. `ProjectileWithoutPrefab_FailsBeforeCostOrCooldown` checks failure reason, untouched Mana, zero cooldown, and null cooldown queries. Network adapter integration is centrally pending. |
| R10 / medium | UI enables before character initialization, or `RebuildRuntime` replaces its reactive objects. Bindings either never subscribed or stayed attached to old objects; unsubscribe looked up replacement objects instead of the original subscriptions. `Components/RpgResourceBinding.cs` / `RpgStatBinding.cs`. | Add additive `RpgCharacter.RuntimeRebuilt` C# event; retain exact subscribed states, detach them, and reconnect after initialization/rebuild. `Bindings_ReconnectAfterLateInitializationAndDetachReplacedStates` checks initial UI values, detached stale values, current updates, and one notification per change. |
| R11 / medium | Selector source owns child colliders. `Runtime/RpgTargetingUtility.cs:AddCandidate` ignored only the source GameObject, selecting its own child; different hurtboxes also weighted random selection multiple times. The selector ignored its explicit source and only resolved `RpgCharacter`. | Filter/deduplicate by receiver, pass explicit source identity, and reuse the attack controller's generic receiver resolution. `TargetSelector_IgnoreSelfExcludesChildColliders` verifies selection of the external actor and public target state. Generic/custom receiver and 2D integration remain central validation cases. |
| R12 / medium | Stack a legacy damage/defense/movement buff or apply a slow status. `RpgCharacter` multiplied universal modifiers by stacks but omitted them for four legacy aliases; movement never read statuses. | Apply clamped stack counts to all four aliases and multiply status movement factors. `LegacyBuffAliases_ApplyEveryActiveStack` covers all four; `StatusMovementMultipliers_ComposeAndDisappearWhenRemoved` checks 0.4, 0.8, then 1.0 after removal. |
| R13 / high | Restore HP with `Increase`, `Refill`, or `RestoreResource` after death. Only `Heal` / `Restore` explicitly revived, so positive HP could coexist with `IsDead=true` and blocked actions. `RpgCharacter.NotifyResource`. | Update revival at the HP notification boundary. `GenericHpRestore_RevivesExactlyOnce` checks both generic increase and full restore, reactive death state, actions, and one revival event. |
| R14 / high | Upgrade rule has zero/negative cost or refers to a missing stat. `CanUpgradeStat` allowed the purchase; negative cost minted points, and missing stat rules consumed points without a stat. | Reject nonpositive costs and absent runtime stats. `UpgradeStat_InvalidCostCannotCreateFreeUpgradesOrPoints` and `UpgradeStat_RuleWithoutRuntimeStatCannotConsumePoints` check points, upgrade levels, and final stat values. |
| R15 / medium | Construct resource/stat runtime directly with capped, overfilled, negative-current, or bounded definitions. Constructors bypassed their own setters: initial state contradicted limits/reactive normalization. | Initialize through existing setters; preserve base values for resolution. Resource construction and stat construction regressions check actual and reactive values, caps, overfill, below-zero support, and normalized percentages. |
| R16 / medium | Null ID reaches resource/stat query/mutation or cooldown lookup. Dictionary lookups threw instead of the module's unknown-ID no-op/default result. | Guard resource lookups and normalize null stat IDs; null cooldown returns zero. `ResourceService_InvalidIdsReturnUnknownResourceResults`, `NullStatId_UsesUnknownStatResultsWithoutChangingOtherStats`, and the projectile regression assert defaults and unchanged valid state. |
| R17 / high | Aura target has several colliders, sphere has a center offset, or object has negative scale. `Components/Weapons/AuraWeapon.cs:ApplyAuraDamage` damaged once per collider and queried the wrong/negative sphere. Its automatic timer listener was not removed on destruction. | Deduplicate characters per call, transform the authored center, use absolute scale, and remove the timer listener. `Aura_UsesColliderCenterAndNegativeScaleAndDamagesEachActorOnce` checks exactly 10 damage per call over two hurtboxes. Timer cleanup is inspected statically. |
| R18 / high | Cached auto-attack target or explicit contact receiver is pooled/deactivated before the next search. Auto attack checked only null after its retry gate; contact bypassed activity checks for an override. Old targets still consumed costs or took hidden damage. Clearing an override could retain its cached receiver. | Recheck activity before actions and reset cached target/receiver before resolving replacements. Two Play Mode regressions check unchanged Mana/HP, target-lost event, and cleared attacking/range state. |
| R19 / high | Effect expiry callback clears effects while `Tick` walks its list, or damage callback removes the current status. `Runtime/RpgEffectShelf.cs:Tick` could index a cleared list or apply more damage after removal. | Mutation version guards stop the current pass after callback-driven mutation; tick accumulator is updated before damage delivery. `EffectExpiry_CallbackMayClearRemainingEntries` covers buff/status expiry; `StatusDamage_CallbackMayRemoveStatusAndStopsFurtherDamage` checks exactly one 0.5-damage tick and empty state. |
| R20 / medium | Lower a level with automatic growth enabled after raising it. `RpgCharacter.SetLevel` recalculated only inside the upward-change branch; the level changed but stats retained the higher value. | Recalculate auto-growth in either direction while granting points only upward. `SetLevel_DownwardChangeRecalculatesAutomaticStatGrowth` checks Wisdom 14 at level 3 and 12 at level 2, including reactive output. |

Three additional source documentation corrections preserve runtime behavior: `RpgRegenMode` percentage
example now uses 5 for 5%; `RpgAttackDefinition` tooltip explains empty-resource Mana fallback rather
than promising a free attack; `RpgStatUpgradeRule` references the existing progression definition.

### Compatibility and limits of the fixes

- Serialized field names, public interfaces, existing public methods, enums, assembly references,
  assets, profile version 2, and sample layout remain intact. Additions are the rebuild event and regen overload.
- Regen pause windows keep the existing frame-quantized contract explicitly covered by existing
  `SpendPauseAndPerSecondRegen_KeepExistingTimingContract`: a frame crossing the pause boundary is skipped.
- Mutating effects from a tick callback stops that pass. Remaining entries resume on the next call;
  this prevents invalid traversal without introducing a per-frame snapshot allocation.
- Corrections intentionally change erroneous balance/lifecycle results: legacy stacked buffs now scale,
  slow statuses affect movement multipliers, invalid upgrades fail, inactive actors stop attacking, and
  generic HP restoration revives. Review existing scene events against these corrected contracts.

## Documentation

Updated 11 existing owned English guides:

- `README.md`: standalone C# resource example, resolved regen units, snapshot service, and scene binding path.
- `RpgCharacter.md`: canonical HP ID, C# versus UnityEvent call shapes, derived regen, upgrade validation,
  downward growth, revival, stacks/slow factors, actual SaveProvider behavior, and rebuild subscriptions.
  Corrected nonexistent `ApplyStatusByName` to `ApplyStatusById`.
- `RpgAttackController.md`, `RpgEvadeController.md`, `RpgTargetSelector.md`: readiness, cost timing,
  cancellation, lock ownership, receiver identity, public API, and NoCode workflows.
- `RpgResourceBinding.md`, `RpgStatBinding.md`: C# setup, late initialization, replacement/unsubscription.
- `Components/Weapons/AuraWeapon.md`, `RpgContactDamage.md`, `RpgAutoAttackController.md`: actual geometry,
  per-character damage, target lifetime behavior, code APIs, and scene setup.
- `InternalTypes.md`: corrected invented enum/API descriptions; documented actual config, runtime services,
  snapshot/effect behavior, and the retained regen overload.

A single invalid character pair in the Character guide was verified by UTF-8 decoding and
`unicode_escape` (U+0412 followed by U+00B7) before a targeted replacement with a normal separator.
No Russian text was rewritten based on terminal appearance. No neighboring Combat, ConditionBridge,
Network, root/package, or sample docs were changed.

## Substantial improvement proposal

### Proposal A: durable resource maxima and explicit profile base state (not implemented)

**Problem and evidence.** `RpgCharacter.SetMaxResource` changes live `Max` but not `BaseMax`.
`RefreshAllDerived` recomputes from `BaseMax`, so any later buff/stat refresh can undo an explicit Max
mutation. `CaptureProfile` stores final Max; `ApplyProfile` briefly restores it and then recomputes it
from the template. Example: set unmodified HP Max from 100 to 160, capture, then apply/refresh: Max
returns to 100. Blindly copying saved final Max to BaseMax would double-count active buff/upgrade bonuses.

**Proposed architecture/API.** Add explicit base mutations such as
`SetResourceBaseMax(string id, float value)` and `AddResourceBaseMax(string id, float delta)` in the
resource service and facade. Keep one resolver responsible for final Max from base + upgrades + buffs.
Profile v3 resource rows should contain a presence flag and `BaseMax` alongside Current and optional
final-Max diagnostics. If final overrides are needed, model them as a separate named layer rather than
writing into the derived result. All mutation paths then refresh percent regen through the existing resolver.

**Compatibility/migration.** Preserve v2 deserialization and existing `SetMaxResource` behavior until an
explicit migration policy is chosen. A v2 final Max cannot reliably reconstruct its base when effects
are present; default to authored base and surface a documented migration option instead of guessing.
New base setters are additive. Snapshot consumers in `Neo.Rpg.Network` need coordinated v3 handling;
no shared config/transport changes belong in this worker's patch.

**Acceptance scenarios.** Base Max 160 survives save/load and unrelated stat/buff refresh; a +20 buff
temporarily yields 180 and expiration returns to 160; a simultaneous upgrade is counted once; repeated
profile application is idempotent; v2 snapshots retain their documented migration behavior; percent regen
uses the resolved Max; two characters sharing a template do not mutate the asset or each other.

**Effort/risk.** Approximately 2-3 developer days plus central offline/network regression passes.
Medium-high compatibility risk because version 2 does not distinguish authored and final Max.

### Proposal B: explicit simulation clock and transactional character state (not implemented)

**Problem and evidence.** `RpgEffectShelf` expires effects using UTC while damage/regen use supplied
simulation delta, mixing offline persistence with paused gameplay. `RpgCharacter` still owns growth,
upgrades, state restoration, and notification order. Arbitrary resource listeners can rebuild dictionaries
inside `RestoreAll`, regen, or derived refresh; state and snapshot callbacks may observe partial transitions.
Growth mode gates apply at SetLevel time but `ResolveStat` independently applies `affectedByLevel` on
other refreshes, making `None`, manual, and deferred-growth policies inconsistent.

**Proposed architecture/API.** Introduce a plain C# character state service composing the existing resource
service, stat resolver, effect shelf, and profile service. Use an explicit tick context containing
simulation delta and UTC now; effect policy chooses persisted-UTC or simulation-time duration.
Mutations and `ApplyProfile` run as batches: validate input, change base state, resolve once, then publish
queued resource/death/effect/progression receipts and one snapshot notification. Make applied growth level
an explicit state input rather than consulting the current level during every unrelated refresh.
Keep `RpgCharacter` as the serialized scene adapter and `RpgNoCodeAction` as typed API dispatch.
Evaluate reuse of the existing Core resource/progression contracts rather than introducing another pool model.

**Compatibility/migration.** Preserve all facade APIs and fields. Default existing effects to persisted
UTC semantics; simulation time is opt-in. Version any additional applied-growth/timer profile state.
Retain explicit resource/stat reactive objects or issue the existing `RuntimeRebuilt` event when replaced.
Network transport stays outside the runtime service and consumes completed batch receipts.

**Acceptance scenarios.** Simulated pause freezes opted-in effects while legacy UTC effects retain their
old persistence behavior; fixed clock inputs give identical ticks; callbacks may remove effects or request
template rebuilds without stale enumeration; profile load publishes no partial upgrade/max states; manual/
None/deferred growth cannot be reapplied by an unrelated buff; repeated batches emit one final snapshot;
independent character instances remain isolated.

**Effort/risk.** Approximately 4-6 developer days plus API/serialization and central integration review.
Medium-high event-order and migration risk. This is a future implementation proposal, not part of the fixes.

## Verification and remaining risks

### Checks actually performed

- Entire exact 59-file inventory reviewed. Read-only Python validation matched current files to the
  manifest and confirmed their nearest assembly is `Neo.Rpg` (nested assemblies excluded).
- Compared owned files with baseline `ca198556`: 19 owned C# files changed (16 behavior files and
  three documentation-only source corrections). Public method signature / serialized private-field
  retention scan passed; no `var` remains in changed C# or new tests. This is a source scan, not binary API validation.
- Lexical delimiter and `#if` / `#endif` balance passed on 59 owned files and exactly three new test files.
  This check does not type-check C# or compile Unity version branches.
- Strict UTF-8 decoding and replacement-character/known separator scan passed on owned source and
  changed docs/tests/metadata. Relative links in all 11 changed owned guides resolved.
- All three new script GUIDs were verified unique across package `.meta` files. New test files passed
  explicit type/prefix and trailing-whitespace checks in the existing Edit/Play test assemblies.
- Scoped `git -c core.safecrlf=false diff --check -- <owned sources/docs/tests/report>` exited **0**.
  New untracked tests were additionally checked directly for trailing whitespace.
- After the final downward-growth fix, repeated its lexical/type-style checks and the final scoped
  diff, UTF-8, guide-link, report-inventory, and exact-test-count checks: all passed. Final ordered
  59-file source SHA-256 aggregate (manifest order, `path:SHA256(bytes)` joined by newlines):
  `50af8cdb70066794a47151cae945cbb57267999278f78edc71f04ba760216e7a`.
- One initial validation glob also matched neighboring workers' longer test prefixes; its expected
  three-metadata assertion failed. It wrote nothing. The corrected validation used the three exact owned
  filenames and passed; neighboring files were not edited or treated as this worker's tests.

### Authored regressions, not executed

| New file under `Assets/Neoxider/Tests/` | Test methods | Expanded cases |
| --- | ---: | ---: |
| `Edit/RPG/Audit20261023NeoRpgRuntimeTests.cs` | 12 | 17 |
| `Edit/RPG/Audit20261023NeoRpgCharacterTests.cs` | 12 | 17 |
| `Play/RPG/Audit20261023NeoRpgLifecycleTests.cs` | 8 | 8 |
| Total | 32 | 42 |

Each has a new unique-GUID `.meta`. Existing `Neo.Editor.Tests` / `Neo.Tests.Play` already reference
the used assemblies; no asmdef edits or installs are needed for these files.

**Not executed by instruction:** Unity/MCP, compilation, Edit Mode or Play Mode test runners, builds,
package installation, child agents, and commits. All runtime and compile outcomes remain unverified
until the coordinator executes them. No baseline or post-change Unity console result is claimed.

### Remaining issues / integration actions

- **Medium, open:** durable Max/profile base-state ambiguity is demonstrated above; Proposal A supplies
  the necessary compatibility/migration design rather than silently changing existing setter/save semantics.
- **Medium, open:** growth policy across manual/None/deferred modes and unrelated refreshes requires
  explicit applied-growth state (Proposal B). The narrow downward automatic-growth bug is fixed separately.
- **Medium, open:** `AddResourceCurrentFlat`, `AttackSpeedPercent`, and the facade's legacy
  `HpRegenPerSecond` modifier lack complete runtime consumers. Do not promise these in new content;
  implementing one-shot application versus refresh semantics needs an explicit contract. Disabled regen
  remains a master gate; buff-only rate calculation does not by itself enable resource ticking.
- **Medium, open:** resource callbacks rebuilding/clearing resource state during service enumeration
  still require transactional dispatch (Proposal B). New effect-shelf mutation guards cover effects only.
- **Medium, open:** UTC expiry versus scaled simulation ticks, clipped final status ticks, invalid
  non-finite numeric inputs, extreme integer overflow, and repeated runtime template/provider swaps need
  explicit future contracts. No claim of general hostile-input validation or deterministic wall-clock tests.
- **Low/medium, open:** query result ordering and non-alloc projectile buffer truncation remain existing
  physics behavior. Generic receiver selection, duplicated hurtboxes in 2D, explicit external query origins,
  timer-listener teardown, and projectile network preflight need central integration coverage.
- **Coordinator:** compile with Unity 6.0-6.4 and 6.5+; run the exact three new fixtures and existing
  `RpgCharacterTests`, `RpgCharacterResourceServiceTests`, `RpgCharacterProfileServiceTests`,
  `RpgCombatEdgeTests`, `RpgCombatPlayModeTests`, and `RpgCombatTargetingPlayModeTests`.
- **Coordinator / Neo.Rpg.Network owner:** run snapshot roundtrips with stacked buffs/upgrades and
  projectile authority rejection. Source contracts/interfaces and profile version 2 were preserved;
  the adapter was not edited. Review snapshot notification frequency/order during bulk derived refresh.
- **Coordinator / nested owners:** integrate with `Neo.Rpg.Combat` receiver bridges and
  `Neo.Rpg.ConditionBridge` readiness checks, including newly rejected inactive casts/evades.
  Nested source/docs/tests were left to their owners.
