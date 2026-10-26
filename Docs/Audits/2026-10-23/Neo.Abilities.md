# Neo.Abilities: audit

Status: source audit complete; fixes and documentation prepared. Unity compilation, EditMode and PlayMode verification pending coordinator execution.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Abilities/Neo.Abilities.asmdef`.
- Scope: 72 C# files; nested assemblies excluded.

## Scope

- `Assets/Neoxider/Scripts/Abilities/Bridge/AbilityNoCodeAction.cs`
- `Assets/Neoxider/Scripts/Abilities/Bridge/AbilityNoCodeActionType.cs`
- `Assets/Neoxider/Scripts/Abilities/Components/AbilityAutoCaster.cs`
- `Assets/Neoxider/Scripts/Abilities/Components/AbilityCasterBehaviour.cs`
- `Assets/Neoxider/Scripts/Abilities/Components/AbilityCooldownSource.cs`
- `Assets/Neoxider/Scripts/Abilities/Components/AbilityProjectileBehaviour.cs`
- `Assets/Neoxider/Scripts/Abilities/Components/AbilitySystemBehaviour.cs`
- `Assets/Neoxider/Scripts/Abilities/Components/AbilityUnitBehaviour.cs`
- `Assets/Neoxider/Scripts/Abilities/Components/ISpawnedAbilityEntity.cs`
- `Assets/Neoxider/Scripts/Abilities/Data/AbilityDefinition.cs`
- `Assets/Neoxider/Scripts/Abilities/Data/AbilityLibrary.cs`
- `Assets/Neoxider/Scripts/Abilities/Data/ModifierDefinition.cs`
- `Assets/Neoxider/Scripts/Abilities/Data/SpawnArchetypeEntry.cs`
- `Assets/Neoxider/Scripts/Abilities/Data/UnitPropertyDefault.cs`
- `Assets/Neoxider/Scripts/Abilities/Data/UnitResourceConfig.cs`
- `Assets/Neoxider/Scripts/Abilities/Data/UnitTemplate.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/AbilitySystem.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Adapters/IAbilityWorldAdapter.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Adapters/IRandomSource.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Adapters/NullWorldAdapter.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Adapters/SpawnRequest.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Adapters/XorShiftRandom.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Casting/AbilityBlueprint.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Casting/AbilityCost.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Casting/AbilityDeliveryType.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Casting/AbilitySlot.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Casting/CastFailureReason.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Casting/CastRequest.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Casting/CastResult.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Casting/TargetingMode.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Combat/DamageResult.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Combat/DamageService.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/AbilityDamageTypes.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/AbilityEffectOps.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/AbilityTeamFilter.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/EffectContext.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/EffectNodeData.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/EffectOpRegistry.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/EffectTargetSelector.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/IEffectOperation.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/LeveledValue.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/LeveledValueResolver.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/ApplyModifierEffectOperation.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/ChainEffectOperation.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/DamageEffectOperation.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/DefaultEffectOps.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/DispelEffectOperation.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/ExecuteEffectOperation.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/HealEffectOperation.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/MotionEffectOperations.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/RemoveModifierEffectOperation.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/ResourceChangeEffectOperation.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/SpawnEffectOperation.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Events/AbilityEventArgs.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Events/AbilityEventBus.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Events/AbilityEvents.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Modifiers/ModifierApplyResult.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Modifiers/ModifierBlueprint.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Modifiers/ModifierEngine.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Modifiers/ModifierEventReaction.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Modifiers/ModifierInstance.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Modifiers/ModifierStackPolicy.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Properties/AbilityProperties.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Properties/PropertyAggregator.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Properties/PropertyContribution.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Properties/PropertyOp.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Properties/ResolvedContribution.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/States/AbilityStates.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Units/AbilityResourceIds.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Units/AbilityUnit.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Units/TeamId.cs`
- `Assets/Neoxider/Scripts/Abilities/Domain/Units/UnitId.cs`

## Findings and fixes

Changed runtime files (all under `Assets/Neoxider/Scripts/Abilities/`):

- `Bridge/AbilityNoCodeAction.cs`
- `Components/AbilityAutoCaster.cs`
- `Components/AbilityCasterBehaviour.cs`
- `Components/AbilitySystemBehaviour.cs`
- `Components/AbilityUnitBehaviour.cs`
- `Domain/AbilitySystem.cs`
- `Domain/Casting/AbilitySlot.cs`
- `Domain/Combat/DamageService.cs`
- `Domain/Effects/EffectContext.cs`
- `Domain/Effects/Ops/ChainEffectOperation.cs`
- `Domain/Modifiers/ModifierEngine.cs`
- `Domain/Modifiers/ModifierInstance.cs`
- `Domain/Units/AbilityUnit.cs`

All 72 owned source files above were read in full, including public contracts, serialized data, effect operations and component lifecycle bodies. Nested `Neo.Abilities.Editor` is excluded. Context reviewed: assembly references, `ResourcePoolModel` / `ResourcePoolEntry`, `LevelComponent` / `LevelModel`, canonical documentation and existing cast/modifier/reaction/auto-cast tests. The older 2026-10-12 read-only audit was used as a lead; current source was independently checked.

The reproductions below describe the baseline code paths and are encoded in new regression tests. They have not yet been executed in Unity. P2 means an incorrect ordinary gameplay/API outcome; P3 means a lower-impact authoring or maintenance issue.

| Id / severity | Baseline reproduction and result | Fix / regression coverage |
|---------------|----------------------------------|---------------------------|
| AB-01 / P2 | Caster has 100 mana; costs contain `{ mana, 60 }` and `{ MANA, 60 }`. Each independent precheck passes, payment clamps to 100, and the unaffordable 120-mana cast succeeds. A positive cost with a null id throws in the resource dictionary. Independent QA also caught a NaN-poisoning regression in the first cumulative-check patch; see correction pass below. | Validate cumulative effective costs case-insensitively before payment; reject nonfinite effective amounts/totals, nonfinite mana multipliers and empty positive-cost ids. Tests `DuplicateResourceCosts_RejectUnaffordableTotalWithoutPaying`, `DuplicateManaCosts_UseEffectiveAmountsAndAllowExactPayment`, `EmptyCostResourceId_IsRejectedAndEmptyRevokeIsANoOp`, `NaNResourceCost_CannotBypassLaterUnaffordableCost`, `NonFiniteEffectiveCost_RejectsBeforeAnyResourcePayment` (eight cases). Empty `RevokeAbility` ids now also return false consistently with slot queries. |
| AB-02 / P2 | Consume three charges with 2 s restore and call `Tick(6)`: only one charge restores and the negative timer stops progressing. Zero restore time consumes charges permanently. With 50% cooldown reduction, later charge intervals revert to the unreduced duration; normalized UI starts at 0.5. | Capture the actual effective cooldown duration in the slot, restore all elapsed intervals arithmetically, retain the remainder and refill zero-duration charges immediately. Tests `ChargeRestoration_CatchesUpAllElapsedEffectiveIntervals` (two cases), `ChargeRestoration_PreservesPartialIntervalAfterLargeStep`, `ZeroChargeRestoreTime_DoesNotPermanentlyConsumeCharges`, `NormalizedCooldown_UsesTheReducedDurationThatActuallyStarted`. |
| AB-03 / P2 | Three independent modifiers exist. The first `Removed` callback calls `RemoveAllFrom`; the outer `RemoveById` / `RemoveWhere` then indexes the mutated, empty list. | Per-call, nesting-safe snapshots for bulk removal, with active-instance checks and scratch cleanup in `finally`. Tests `BulkRemoval_CallbackCanRemoveRemainingInstances` (both APIs) and `BulkRemoval_DoesNotConsumeReplacementCreatedByCallback`. Return counts belong to the current call; callback-created replacements survive. |
| AB-04 / P2 | Pass an active instance from engine A to engine B's `Remove`: B marks the foreign instance inactive and reports success without removing A's indexed entry or bumping A's version. | Require membership in the receiving engine before deactivation. Test `Remove_ForeignEngineCannotDeactivateAnInstance`. |
| AB-05 / P2 | A duration-1 s DoT with a 1 s interval receives `Tick(5)`. The baseline applies five periodic effects before expiring the modifier. | Accumulate only the portion of the step inside the remaining lifetime. Test `FiniteDot_LargeStepDoesNotTickPastItsLifetime`. The existing eight-effect catch-up bound remains. |
| AB-06 / P2 | During unit regeneration, an `OnResourceChanged` listener executes an area effect. Its radius query clears the same `_unitScratch` used by the outer unit-tick loop, skipping or repeating remaining units. | Keep the unit-tick snapshot separate from area-query scratch. Test `ResourceCallbackAreaEffect_DoesNotClobberRemainingUnitTicks`, independent of dictionary iteration order. |
| AB-07 / P2 | Set a base `max_health_bonus` or `max_mana_bonus`: the property changes but the pool maximum does not until an unrelated modifier lifecycle event. Also, `SetMax` publishes callbacks before the old applied delta was recorded, allowing a reentrant bonus change to compound or lose the base maximum. | Synchronize relevant base-property changes and record each applied delta before publishing resource callbacks. Tests `BaseMaxResourceBonuses_ImmediatelyUpdatePoolsWithoutCompounding`, `MaxBonusResourceCallback_CanUpdateBonusWithoutLosingTheBaseMaximum`. No implicit healing is added. |
| AB-08 / P2 | The first shield's `shield_absorbed` listener dispels a second shield while the hit still has damage remaining. The second, already-removed instance remains in scratch and incorrectly absorbs that damage. A callback can also stack the current shield above the captured capacity, dispel it, or recursively exhaust it; the old comparison can wrongly remove it or emit another break. Reapplication of a `Refresh` shield from its break callback refreshes the exhausted old instance, which is then removed. | Check live activity before consumption and again after callbacks, then re-read capacity. During break delivery, an internal flag excludes the exhausted instance from consumption and refresh/stack reuse while preserving its own reaction; remove only that old instance in `finally`. Tests `ShieldReceiptCallback_RemovedShieldCannotAbsorbTheRemainingHit`, `ShieldAbsorbedCallback_StackingCurrentShieldPreservesExpandedCapacity`, `ShieldAbsorbedCallback_DispellingCurrentShieldDoesNotPublishBreak`, `ShieldAbsorbedCallback_NestedDamagePublishesOnlyOneBreakForTheInstance`, `ShieldBrokenReapplication_ReplacesExhaustedInstanceAndPreservesOwnReaction` (callback and own-reaction cases). |
| AB-09 / P2 | `CreateNested` from a cast context or a modifier reaction triggered by a cast produces damage/heal receipts with `CastId = 0`. Consumers cannot correlate those effects to the accepted cast. | Retain the parent/trigger cast id in both paths. Tests `NestedEffectContext_KeepsTheCastReceiptId`, `ModifierReaction_KeepsTheTriggeringCastReceiptId`. Modifier lifecycle and shield receipts still do not carry a cast id. |
| AB-10 / P2 | Registered unit uses level source A; call `SetLevelSource(B)`, disable, then re-enable. Disable unsubscribes B rather than A. A's stale listener can drive the newly registered unit and retains the behaviour. | Track the source actually subscribed at registration and detach that source on disable. Preserve the documented next-enable setter timing. Test `SetLevelSourceWhileRegistered_UnsubscribesOriginalSourceBeforeReenable`. |
| AB-11 / P2 | A nearer enemy is `untargetable`, while a slightly farther target is valid. Auto-cast repeatedly selects and fails against the nearer unit instead of using the valid one. Separately, a target inside `Range + cast_range_bonus` but outside base `Range` is never considered. | Skip invalid unit-target candidates while preserving point/direction behavior, and use effective cast range in both shared and per-ability searches. Tests `AutoCast_SkipsUntargetableNearestUnit`, `AutoCast_TargetSearchUsesCastRangeBonus`. |
| AB-12 / P2 | Apply positive damage to an otherwise alive utility unit with no health pool. The decrease is a no-op, but the subsequent `Health <= 0` check publishes death/kill and marks the utility unit dead. | Negate hits when both maximum and current health are nonpositive. Test `Damage_UnitWithoutHealthPoolDoesNotDieOrProduceDamageReceipts`. This retains support for uncapped pools with positive current health. |
| AB-13 / P3 | Twelve non-anonymous `var` declarations remained in this runtime assembly; four Inspector buttons invoking runtime actions were available outside Play Mode. | All owned runtime source now uses explicit types. Mark `Execute`, `CastFirstAbility`, `CastReadyAbilities`, and `DebugDamage25` buttons `PlayModeOnly`. No public or serialized fields were renamed. |

New test files, with unique Unity metadata:

- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesRuntimeTests.cs`: 24 methods / 34 cases.
- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesSceneTests.cs`: 3 methods / 3 cases. Uses the existing `AbilitySceneRig`; level sources disable load/save so tests do not depend on or write the user's saved profile.

Reuse decision: adapt the existing domain services, `AbilitySlot`, pool APIs, `FakeWorldAdapter` and `AbilitySceneRig`. No parallel gameplay framework, dependencies, generated scene builders or runtime test hooks were introduced.

### Correction pass after independent source QA

Completed in this original worker session before the later, separate planning phase. The QA reproductions were source-traced, not executed in Unity.

- **Cost correction.** The first cumulative-cost patch allowed `{ mana, NaN }` to poison the total for a later `{ MANA, 120 }` entry from a 100-mana pool. That introduced a regression: the original independent-entry precheck rejected the finite 120 cost. The correction rejects nonfinite effective amounts before the nonpositive-cost skip and checks cumulative totals; it also rejects a nonfinite mana multiplier before clamping it. Public regressions assert no payment, resource-change event, consumed charge, cooldown or cast receipt for NaN, positive/negative infinity and multiplication overflow, including a valid earlier cost in another resource.
- **Shield correction.** With a one-stack 10 HP shield and `PerStackValue = 10`, a `shield_absorbed` callback adding stack two must leave the same instance active at capacity 20. A later 5-damage hit is absorbed. Self-dispel must not produce a break after removal; nested damage exhausting the same instance must produce one break/removal. These now use live post-callback state/capacity checks.
- **Break-reapplication compatibility.** Removing the shield before `shield_broken` would suppress its own declarative break reaction, because reaction routing queries active modifiers. Event order therefore remains unchanged: the old instance stays active during the receipt, but is marked as breaking internally and cannot absorb again or be reused by `FindActive`. `Refresh` / `Stack` reapplication creates a replacement; only the exhausted old instance is removed in `finally`. A two-case public regression covers both a subscriber callback and the shield's own `apply_modifier` reaction, replacement identity, subsequent absorption and single break/removal. No public/serialized API or event-order migration is required.

The independent QA worker inspected the runtime correction and all new regression bodies and reported no further actionable issue in that delta. Actual compilation and execution remain central verification work.

## Documentation

The existing `Assets/Neoxider/Docs/Abilities/README.md` remains the canonical entry. It now includes a Russian runtime overview, explicit C# examples, a runtime-contract table, scene lifecycle guidance, callback/payment constraints, resource maximum behavior, cast-id limits and a precise determinism contract. It distinguishes running without GameObjects from running without Unity assemblies.

Updated existing pages:

- `AbilityDefinition.md`: cumulative/finite costs, effective charge timing and explicit example types.
- `ModifierDefinition.md`: leveled duration, effective instance permanence, first-tick timing, expiry boundary, removal snapshots and synchronous shield callback/reapplication semantics.
- `AbilityUnitBehaviour.md`: level source field, deferred setter timing, actual subscription teardown, registration and heal APIs.
- `AbilityAutoCaster.md`: targetability, positive/negative range bonuses and unlimited-range search fallback.
- `AbilityCooldownSource.md`: effective normalized timer, slot readiness versus full cast validation, and next-charge countdown semantics.
- `UnitTemplate.md`: base maximum bonuses and no implicit heal.
- `AbilityProjectileBehaviour.md`: documented enemy-only collision limitation, pause scope, discrete collision sampling and custom-host completion/timeout contract.
- `AbilityNoCodeAction.md`: validation failures versus exceptions from user callbacks/custom operations.
- `SurvivorDemo.md`: actual `Demo Scenes` sample name, release `Samples~`, developer `Samples`, and imported `Assets/Samples/NeoxiderTools/<version>/Demo Scenes` paths.

Editor-specific documentation and all samples/package metadata remain outside this change.

## Substantial improvement proposal

### Composable, explicitly stepped projectile delivery

**Problem.** The domain supports Unit/Point/Direction targeting and any team filter, but the standard scene projectile infers its endpoint from `SpawnRequest.Direction`, collides with enemies only, samples hits at its current position and advances independently of domain pause/tick time. Direction magnitude can therefore determine a detonation distance; an allied Unit-targeted payload cannot use the stock collision policy. Completion on external disable/destruction relies on timeout rather than an explicit delivery lifecycle. These are documented implementation limits; no scene runtime reproduction or performance measurement is claimed in this audit.

**Design/API.** Add a value-only `AbilityDeliveryRequest` carrying `CastId`, owner, targeting mode, explicit `HasTargetPoint`/`TargetPoint`, direction, range, collision team filter and presentation archetype. Add an opt-in `IAbilityDeliveryHost` with `Begin(request)`, `Step(deltaTime)` and `Cancel(castId)` (or equivalent typed handles), and an optional world segment-query seam for deterministic swept hits. Resolve candidates by segment distance then unit id, retain distinct pierced targets and route impact/completion to the existing `NotifyProjectileHit` / `ReleaseProjectileCast` APIs. Let hosts explicitly choose the domain clock or scene clock.

**Compatibility.** Keep `SpawnRequest`, `IAbilityWorldAdapter`, current enums, asset fields and prefab components. Adapt legacy requests to the current component by default; opt in per host/archetype to the new delivery path. Preserve legacy enemy-only collision and scene-time behavior until a project explicitly selects the new policy. No existing scene or ScriptableObject migration is required for the initial API addition.

**Acceptance.** Headless tests and a focused PlayMode fixture must cover allied healing projectiles; identical directional range for normalized/scaled vectors; point detonation at the explicit endpoint; fast projectiles crossing small targets; deterministic equal-distance hit order; one hit per pierced unit; pause/manual stepping; cancellation, external disable/destruction and pooling; exactly one release per cast; no retained pending entry after cancellation. Compare full per-cast impact receipts under repeated seeded runs and measure allocations/CPU for a stated projectile/target count.

### Follow-on: validated runtime catalog and payment planning

Introduce an optional validator/compiler returning diagnostics and an immutable catalog snapshot, plus a value-only cast plan containing captured costs/level/cooldown/targets before payment. A transaction-capable resource adapter could then reserve and commit all costs once, with explicit health-cost and callback policies. Keep mutable blueprint registration as a compatibility facade. Acceptance should include duplicate/empty ids, null collections, unknown referenced modifiers/ops, capped pools, reentrant payment callbacks, live catalog replacement and deterministic structured failure receipts. This addresses remaining ambiguity without silently changing current custom-pool behavior in a bug-fix patch.

## Verification and remaining risks

Executed by this worker:

- Exact inventory check: the report lists 72 files and matches all C# files owned by `Neo.Abilities` with nested Editor excluded.
- Full source review, focused diff review and existing API/asmdef/test support inspection.
- Scoped `git diff --check` for owned source, docs and tests: clean at the recorded check (repeated after final edits before handoff).
- Strict UTF-8 decoding, replacement-character and trailing-whitespace checks: 91 scoped source/docs/new-test/metadata/report files, no issues. This includes reading existing Russian text as UTF-8; no console-driven encoding rewrite was performed.
- Newly added relative Markdown link targets exist; existing unrelated links were not treated as a new regression.
- Explicit-type scan: zero `var` matches in the 72 owned runtime files and both new test files.
- New metadata GUIDs have valid shape and occur only in their respective `.meta` files under `Assets`.
- Static NUnit inventory after the QA correction pass: 27 methods / 37 cases. **No compilation or test execution was performed by this worker.**

Central verification still required: compile/import under supported Unity branches, execute all `Neo.Editor.Tests.Abilities` tests including both new classes, then relevant PlayMode checks for scene registration, level-source pooling and auto-casting. No Unity/MCP session, batch compiler, build process, dependency install or shared package/settings/sample mutation was started by this worker.

Remaining risks and limits:

- The documented stock projectile filtering, frame-step collision, inferred direction endpoint and independent scene clock need the delivery proposal / PlayMode evidence; this patch does not change that serialized prefab behavior.
- Resource payment still uses pool `Decrease` and synchronous callbacks; arbitrary custom caps or callbacks mutating resources/catalogs are not an atomic transaction. Health-cost death policy is not formalized. Default `UnitTemplate` pools use unlimited per-operation caps.
- Catalogs expose mutable blueprints. Registering an id again can leave already-granted slots referring to older data while casts use the replacement catalog entry; configure before granting until a validated replacement contract exists.
- Modifier tick/reaction RNG is re-created from stable instance-derived seeds, so repeated events on one instance can repeat chance outcomes. Independent temporal rolls require an explicit instance RNG/counter contract and replay tests.
- The eight-tick catch-up limit and uncapped area-query order constrain replay equivalence. No cross-platform lockstep, network snapshot or performance guarantee was verified.
- Public callbacks can still re-enter whole `Tick` / `Cast` operations or throw; the repaired scratch/removal cases do not claim arbitrary transaction or exception isolation. `AbilityEventBus.Clear` also clears the system's installed reaction subscription; use targeted unsubscribe for consumers.
- Per-owner modifier version entries are retained after unit churn; no long-session allocation/retention benchmark was run. Direct engine application is lower-level and can refer to ids outside the system catalog/world.
- No samples were imported or played, no visuals were reviewed, and no Unity 6.0–6.4 / 6.5+ compiler branch was executed locally. Parent coordinator owns those checks and may append actual results here.
