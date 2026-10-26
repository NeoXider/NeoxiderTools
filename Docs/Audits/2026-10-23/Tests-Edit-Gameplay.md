# Tests-Edit-Gameplay: supplemental deep audit

Status: static audit and owned additions complete; central compilation and Unity verification pending.

Assignment baseline `ca198556`, package `10.19.0`. Reviewed `AGENTS.md`, `WORKER-CONTRACT.md`, the exact supplemental manifest entry and all 65 assigned files below. Existing tests, including `AbilityTestSupport.cs`, are READ-ONLY. No runtime source, existing test, module owner's documentation, asmdef or shared configuration was edited. No child agents, commits, installs, Unity/MCP, build or test processes were used. Local review is not independent QA.

Кратко: проверены все 65 файлов; добавлены 14 детерминированных сценариев для вероятностного урона, атомарного размещения и событий dice merge. Дефекты чужих тестов переданы ниже с воспроизведением. Запуск Unity остаётся координатору.

## Assignment

Test assembly helper: listed existing tests are READ-ONLY to avoid primary owner overlap. Audit assertion quality, cleanup/isolation, incorrect skips, fixture coverage; add only meaningful NEW tests prefixed Audit20261023TestGameplay*.cs. Runtime source is owned by other module agents: record concrete bugs and repro in report for coordinator, do not mutate runtime. Own public docs only Assets/Neoxider/Docs/Testing/Gameplay.md.

## Files

- `Assets/Neoxider/Tests/Edit/Abilities/AbilityAutoCastAndCooldownTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/AbilityNoCodeBridgeTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/AbilityStateTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/AbilityTestSupport.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/AuditFixesAbilitiesTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/CastPipelineTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/ChainAndExecuteOpTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/DamageServiceTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/DeterminismTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/EffectOperationTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/LeveledValueTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/ModifierEngineTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/ModifierTickAndReactionTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/MotionOpTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/ProjectileAndSpawnTests.cs`
- `Assets/Neoxider/Tests/Edit/Abilities/PropertyAggregatorTests.cs`
- `Assets/Neoxider/Tests/Edit/Animations/TransformAnimatorTests.cs`
- `Assets/Neoxider/Tests/Edit/Bonus/AuditFixesSlotRowTests.cs`
- `Assets/Neoxider/Tests/Edit/Bonus/BonusSystemTests.cs`
- `Assets/Neoxider/Tests/Edit/Bonus/CheckSpinTests.cs`
- `Assets/Neoxider/Tests/Edit/Bonus/CooldownRewardAutoClaimTests.cs`
- `Assets/Neoxider/Tests/Edit/Bonus/SlotEconomyDefinitionTests.cs`
- `Assets/Neoxider/Tests/Edit/Bonus/SlotSymbolWeightOverridesTests.cs`
- `Assets/Neoxider/Tests/Edit/Bonus/SpinControllerEconomyTests.cs`
- `Assets/Neoxider/Tests/Edit/Bonus/SpinControllerPaylineTests.cs`
- `Assets/Neoxider/Tests/Edit/Bonus/WheelFortuneTests.cs`
- `Assets/Neoxider/Tests/Edit/Cards/AuditFixesCardsTests.cs`
- `Assets/Neoxider/Tests/Edit/Cards/CardSpriteNameParserTests.cs`
- `Assets/Neoxider/Tests/Edit/Cards/CardSystemTests.cs`
- `Assets/Neoxider/Tests/Edit/Cards/DeckAndHandModelTests.cs`
- `Assets/Neoxider/Tests/Edit/GridSystem/AuditFixesMatch3BoardServiceTests.cs`
- `Assets/Neoxider/Tests/Edit/GridSystem/Dice/DiceBoardCoreTests.cs`
- `Assets/Neoxider/Tests/Edit/GridSystem/Dice/GridMergeAndDiceTests.cs`
- `Assets/Neoxider/Tests/Edit/GridSystem/FieldGeneratorCoverageTests.cs`
- `Assets/Neoxider/Tests/Edit/GridSystem/GridPlacementServiceTests.cs`
- `Assets/Neoxider/Tests/Edit/GridSystem/GridSlotAllocatorTests.cs`
- `Assets/Neoxider/Tests/Edit/GridSystem/GridSystemTests.cs`
- `Assets/Neoxider/Tests/Edit/GridSystem/Match3BoardServiceCoverageTests.cs`
- `Assets/Neoxider/Tests/Edit/GridSystem/SlidingMergeResolverCoverageTests.cs`
- `Assets/Neoxider/Tests/Edit/Level/LevelCurveDefinitionTests.cs`
- `Assets/Neoxider/Tests/Edit/Level/LevelManagerTests.cs`
- `Assets/Neoxider/Tests/Edit/Level/SceneFlowControllerTests.cs`
- `Assets/Neoxider/Tests/Edit/Merge/MergeResolverTests.cs`
- `Assets/Neoxider/Tests/Edit/NPC/NpcCombatDecisionCoreTests.cs`
- `Assets/Neoxider/Tests/Edit/Progression/ProgressionManagerTests.cs`
- `Assets/Neoxider/Tests/Edit/Quest/QuestManagerTests.cs`
- `Assets/Neoxider/Tests/Edit/RPG/HealthComponentTests.cs`
- `Assets/Neoxider/Tests/Edit/RPG/RpgCharacterProfileServiceTests.cs`
- `Assets/Neoxider/Tests/Edit/RPG/RpgCharacterResourceServiceTests.cs`
- `Assets/Neoxider/Tests/Edit/RPG/RpgCharacterTests.cs`
- `Assets/Neoxider/Tests/Edit/RPG/RpgCombatEdgeTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/AuditFixesMoneyTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/EquipmentManagerTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/MoneyCapEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/MoneyDepositGuardTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/MoneyPersistenceEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/ShopAffordabilityTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/ShopCategorySelectorTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/ShopListViewCategoryBarTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/ShopProfileDataTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/ShopPurchaseRollbackTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/ShopResolverTests.cs`
- `Assets/Neoxider/Tests/Edit/Shop/ShopVariantsPanelTests.cs`
- `Assets/Neoxider/Tests/Edit/StateMachine/StateDurationPredicateTests.cs`
- `Assets/Neoxider/Tests/Edit/StateMachine/StateMachineOptimizedTests.cs`


## Findings, fixes and regression checks

### Complete scope review

Inspected every listed file's test bodies and support code: assertions and observable state, rejected-operation immutability, lifecycle assumptions, asset ownership/cleanup, static/provider pollution, timing/RNG, null/boundary inputs, skip quality and public API coverage. The grouped ledger below covers every file in the assignment list; finding references distinguish established defects from coverage opportunities. No finding is an executed Unity failure in this worker's session.

| Assigned group | Review result |
| --- | --- |
| Abilities: all 16 files | Cast rejection/targeting/costs/cooldowns/charges, state composition, damage/shields/kill/revive, effect selectors, leveled values, modifiers/reactions/reentrancy, RNG and projectile/spawn receipts checked. Scene rig owns its GameObjects; pure fixtures retain no external resources. Probability branching coverage defect F5 addressed by new tests. Existing ability-owner additions already cover duplicate costs, so they were not duplicated. Optional reflection invocation in support helpers can silently do nothing; caller state assertions are essential. Missing-world motion smoke proves no exception only; actual projectile travel still needs Play Mode. |
| Animations: `TransformAnimatorTests` | Channel evaluation, seed behavior, impulse boundaries, stop/base restore, target replacement and pool lifecycle checked. Manual lifecycle calls cover handlers. Explicit `RandomizeTime` changes global RNG without restoration: F4. No incorrect claim that `RandomizeStartTime=false` disables an explicit `RandomizeTime()` call. |
| Bonus: all 9 files | Slot row recovery/order, legacy reward arithmetic, auto-claim rearm, paylines, spending, special conversion, weighted overrides/sync/normalization and wheel boundaries checked. Most RNG tests have one possible positive-weight outcome or explicit rolls, so do not need statistical assertions. Asset leaks F2 and wall-clock issue F9 remain. Primary owner fixed initial provider-wide cleanup concurrently. |
| Cards: all 4 files | Card/custom identity, parser language/invalid cases, deck/hand order and duplicate multiplicity, capacity/bulk atomicity, async queue completion, listener preservation, model replacement and poker rules checked. Object/asset finally blocks generally present. Shuffle tests correctly assert conservation without requiring a random permutation to differ. Hover-reference test overstates cleanup proof: F10. |
| GridSystem: all 9 files including Dice | Generation/origin/conversion, passability/path length, placement policies/atomicity, slot indices/preference/release, merge shapes/cascades/caps, dice generation, match3, sliding barriers and event forwarding checked. Most roots are fixture-owned. Exception cleanup gap F3, vacuous notification assertion F6 and coroutine skip F8 remain. Existing placement rejection only checks one cell's content; new cases assert both cells and events. Path length alone does not validate every path node. |
| Level: all 3 files | Formula/custom/animation curve boundaries and roundtrips, level/XP facade and NoCode events, map payload and sync/additive/restart routing checked. Assets mostly use finally; some arrangement allocations precede it. Scene loader static restoration F4 remains. Async loader double returns null and is not async-loading coverage. |
| Merge: `MergeResolverTests` | Connected graph, custom predicates/result outside group, dry-run immutability, changed items and exact cascade-limit distinction checked. Domain dictionaries are local; no native cleanup needed. |
| NPC: `NpcCombatDecisionCoreTests` | Acquire/chase/attack/hold/lose priority and zero lose-distance checked. Exact pure decisions. Distance-equality cases are a coverage opportunity, not a demonstrated runtime bug. |
| Progression: `ProgressionManagerTests` | XP jumps/rewards, save flush/reset/load, missing-provider diagnostics, definition validation and premium checked. Provider restoration added concurrently. Permissive reward assertions and absent retroactive reward observation remain: F7. |
| Quest: `QuestManagerTests` | Accept/duplicates, objective accumulation/idempotence, save/load/migration, reentrant completion and singleton reset/replacement checked. Provider scope added concurrently. The scene-reload-named test recreates GameObjects rather than loading a scene. |
| RPG: all 5 files | Resource amounts/receipts, spend/regen gates and time inputs, profile roundtrip/sanitize, custom IDs, reactive HP, upgrades, inactive initialization, combat/effect stacks, target-before-cost, receiver dedupe and projectile reset checked. Provider scope added to character fixture concurrently. Death-once health test issues one lethal call; repeated dead hits/revive are additional coverage. Projectile private-state reset is not flight coverage. |
| Shop: all 12 files | Wallet caps/nonfinite guards/persistence, equipment, affordability/subscriptions/currencies, category adapters, profile sanitize/clone, purchase refund/retry, resolver and variants checked. Default profile writes F1, native/failure cleanup F3 and culture-double issue F4 remain. Wallet persistence scopes added concurrently. Clone test covers ownership only; nested override copying is a coverage opportunity. MIRROR money dispatch smoke is not a network-exchange assertion. |
| StateMachine: both files | Entry/exit/update/stop/event/cache semantics, typed/global/priority transitions, cached/uncached selection, disable handler and nested duration anchors checked. Duration tests arrange explicit past enter times without sleeping. No demonstrated additional defect established. |

Relevant dependencies read as context: `AbilitySystem.ExecuteEffects`, `EffectContext`, `IRandomSource`, `XorShiftRandom`, `GridPlacementService` and entry/results, `FieldGenerator` lifecycle, `DiceBoard`, `TransformAnimator.RandomizeTime`, `SaveProvider`, `Shop.SaveProfile`, `SceneFlowController.SceneLoader`, and `Neo.Editor.Tests.asmdef`. This was not an independent full runtime-module audit.

Reuse receipt: inspected applicable `neoxider-tools` game-system/ability references and existing support. Reused `AbilityTestSupport`, `EventLog`, `IRandomSource`, `AbilitySystem`, `FieldGenerator`, `GridPlacementService` and `DiceBoard`. The fixed-roll class is a tiny adapter for the existing RNG injection seam; it does not duplicate gameplay/random/placement implementations. All new C# uses explicit types.

The shared worktree changed during review. Initial assigned-file digest was `d96bca0b361459cce77a595c1abe2b7f4f76bdb16d2262b47ea85f4f43ce5fbb`; it identifies the initial inspection snapshot, not unchanged files. Targeted follow-up reads confirmed the remaining findings. Primary-owner changes are credited separately below. Line numbers refer to that follow-up snapshot; method names are stable anchors.

### F1 — P1: shop fixtures can overwrite the active provider's profile

Sources: `Shop/ShopPurchaseRollbackTests.cs:25–46`, especially `SuccessfulPurchase_TakesTheMoneyAndGrantsTheItem`; `Shop/ShopAffordabilityTests.cs:27–47`; `Shop/ShopVariantsPanelTests.cs:46–82`.

Runtime evidence: `Scripts/Shop/Shop.cs:68` defaults `_keySave` to `Shop`; `SaveProfile:899–904` unconditionally writes serialized profile data using `SaveProvider.SetString`. Disabling `_persistMoney` affects the wallet, not the shop.

The affordability fixture also persists through `CanAfford_RuntimePriceOverride_IsRespected`: `SetRuntimePrice`/`ClearRuntimePrice` call `SaveProfile` (`Shop.cs:646–665`), even without a successful purchase.

Repro: install a sentinel provider/value at `Shop`, execute a successful rollback-suite purchase, then read that key. The fixture lacks provider/key restoration and the purchase overwrites the sentinel. The variants fixture similarly deletes any pre-existing `Test_ShopVariantsPanel` value during teardown. Which real store is affected depends on the provider installed on entry.

Consequence: developer/test data pollution and order-dependent persistence assertions. Primary test owner should scope the provider before creating components and dispose after destroying them. Do not rely on preceding fixtures installing memory. No source edit made here.

### F2 — P2: slot fixtures leak native ScriptableObjects

- `Bonus/SlotEconomyDefinitionTests.Configure:40–43` always allocates. `ApplySpecialRule_DoesNothing_WhenDisabledOrNoSpecialOnLine` calls it twice; teardown only destroys the second definition. The first instance is lost.
- `Bonus/SpinControllerPaylineTests.AssignMultiplierData:213–230` allocates `SpriteMultiplayerData` with no teardown/finally ownership.

Repro: repeat the respective test and inspect live instances by type before/after; at least one definition/table remains per relevant test even on the success path. Consequence: native editor objects accumulate across local runs. Primary owners should reuse the definition or register every allocation immediately for destruction. Existing files are read-only here.

### F3 — P2: failed assertions leave scene objects that affect later tests

- `Shop/ShopResolverTests.Resolve_PrefersParentHierarchyOverSceneFallback:36–51`: local `parent` is destroyed after assertions; teardown only tracks `_contextObject` and `_sceneRoot`. A failure leaves a parent with `ShopResolverProbe`, available to a later scene fallback.
- `GridSystem/Match3BoardServiceCoverageTests.Match3BoardService_MethodCalls_DoNotCrash_WhenNoBoardConfigured:25–47`: destruction also follows assertions without finally/fixture ownership.

Repro: force an assertion to fail before final destruction and inspect scene roots/run the next resolver case. The untracked root remains. Owners should register roots before assertions and clean them in finally/teardown. New placement tests use tracked teardown ownership.

### F4 — P2: global restoration and numeric-double contracts are incomplete

- `Level/SceneFlowControllerTests:17–28` replaces `SceneLoader` then sets it to null. The runtime setter substitutes a new Unity loader, losing a previously injected loader. Repro: install a sentinel loader, run a fixture case and compare reference identity afterward. Capture/restore the exact instance.
- `Animations/TransformAnimatorTests.Component_Stop_RestoresBasePose:209` and `Component_Shake_ImpulseInfluencesNextApply:240` call `RandomizeTime`. Runtime `TransformAnimator.cs:301–305` advances `UnityEngine.Random` twice. No RNG state is saved/restored; later scenarios can observe a changed sequence. This is fixture isolation, not a defect in `RandomizeTime`.
- `Shop/MoneyPersistenceEditModeTests.MemorySaveProvider.GetFloat:140–143` parses with current culture; `SetFloat:145–148` writes invariant strings. Repro under `de-DE`: roundtrip `12.5f` through this double; the period is a grouping separator rather than the writer's decimal separator. Integer-valued existing cases miss it. Owner should parse with `NumberStyles.Float`/`CultureInfo.InvariantCulture` and add a fractional culture case.

### F5 — P2 coverage defect: branching test accepts an unbranched implementation

`Abilities/DeterminismTests.FullCast_ProbabilisticNode_ActuallyBranches:74–79` performs one fixed-seed cast and accepts either 50 or 80 damage. Always executing or never executing the bonus both pass. Same-seed replay also passes either broken implementation.

Completed owned fix: `Audit20261023TestGameplayProbabilityTests` supplies rolls `0`, `0.499`, `0.5`, `0.999` against chance `0.5`, requiring health 20/20/50/50 and two/two/one/one damage receipts. Source/target identity and untouched caster health are asserted. This closes the effect-decision gap through authorized new tests; the read-only test remains. Limit: effect-seam coverage does not replace cast seed propagation/cross-platform PRNG verification.

### F6 — P2 coverage defect: notification consistency can pass without a callback

`GridSystem/Dice/GridMergeAndDiceTests.DiceBoardService_NotifiesCellsWithConsistentOccupancy:415–435` initializes `consistent = true`, only clears it inside the callback, and asserts true. Suppressing all cell notifications passes; that test does not assert a successful placement either.

Completed owned fix: a new dice-core merge case requires notifications for both cleared cells and the placed anchor, two cleared-cell receipts, one merge receipt and one logical board change, callback-time content/occupancy consistency and exact final state. New placement cases also require positive notifications when enabled, complete footprint state before observer delivery, and unchanged state/events after seven rejection kinds. Limit: scene-wrapper scheduling/forwarding remains its owner's and central Unity verification's responsibility.

### F7 — P2 coverage defect: progression permits lost/duplicate rewards

- `Progression/ProgressionManagerTests.AddXp_UpdatesLevelAndPerkPoints:65–68` allows any level/perk count at or above two for a 120-XP linear setup. Over-awarding passes.
- `SaveAndLoad_RestoresUnlockedNodesAndPurchasedPerks:394–395` accepts zero or one remaining point after a one-point reward and one-point purchase, allowing the purchased point to reappear.
- `ActivatePremium_SetsHasPremium_AndGrantsRetroactiveRewards` checks premium persistence but neither earns levels before activation nor observes retroactive rewards. Removing retroactive dispatch can pass.

Owner action: arrange an explicit level/reward contract, assert exact spent/remaining points and dispatch counts, and check repeated premium activation grants nothing twice. These are test defects; no runtime reward defect is inferred solely from weak assertions.

### F8 — P2 verification gap: cancellation may be inconclusive in Edit Mode

`GridSystem/AuditFixesMatch3BoardServiceTests.OnDisable_ClearsPendingResolveRoutine:48–60` uses `StartCoroutine` and `Assume.That(handle, Is.Not.Null)`. An Edit Mode environment without a running coroutine handle makes the actual regression inconclusive. Manual `OnDisable` plus private handle inspection does not prove work stopped across frames.

Owner action: keep the handler smoke if useful and add a Play Mode case that observes work, disables through Unity state, advances frames, then proves no more work/callbacks. Do not treat an environment skip as cancellation evidence. No runner started here.

### F9 — P2 determinism issue: legacy reward tests depend on real elapsed time

`Bonus/BonusSystemTests.TimeReward_GetClaimableCount_CalculatesCorrectlyBasedOnSave` stores UTC-now minus 25 seconds for a 10-second reward and requires exactly two. Pausing five seconds before the call changes the count. `TimeReward_TakeReward_UpdatesSaveKeyAndEvents` requires a real-time residual between three and seven seconds; scheduler pauses/UTC changes can fail it despite correct logic.

Owner action: use an existing clock seam where available or add a narrow clock input to the reward core and test fixed instants, retaining scene APIs. Neither these repros nor clock changes were executed here.

### F10 — P3 coverage gap: hover references do not establish tween cleanup

`Cards/CardSystemTests.CardViews_HoverTweens_AreOwnedAndClearedOnExit:438–463` only checks private tween fields are non-null after enter/exit. A dead or unowned tween can remain referenced; valid replacement exit tweens can also be non-null. The title overstates its proof. No card-tween runtime bug is established.

Owner action: inspect tween activity/target ownership after disable/destruction and use controlled updates/Play Mode to observe return pose, separately from field-presence smoke.

### Concurrent owner fixes observed, not performed here

Initial inspection found provider-wide `DeleteAll` in `BonusSystemTests` and provider replacement/leaks in progression, RPG and money fixtures. Follow-up reads found `Audit20261023NeoEditorTestsSaveScope` wrapping `BonusSystemTests`, `QuestManagerTests`, `ProgressionManagerTests`, `RpgCharacterTests`, `AuditFixesMoneyTests` and `MoneyPersistenceEditModeTests`, with finally-based disposal where relevant. Those old provider observations are therefore not current findings for these fixtures. F1's remaining shop exposure was still present at follow-up.

No skip/ignore attribute or unconditional `Assert.Pass` was found in the assigned-scope risk scan. The relevant conditional skip was Match3's `Assume.That` in F8. Real UTC use was confined to reward tests; no blocking sleep was introduced.

### Owned regression additions

- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023TestGameplayProbabilityTests.cs`: four controlled chance cases, addressing F5 at the public effect seam.
- `Assets/Neoxider/Tests/Edit/GridSystem/Audit20261023TestGameplayPlacementTests.cs`: seven later-entry rejection cases (disabled, blocked, occupied, out-of-bounds, duplicate, null, predicate), both notification modes and one dice merge. Checks content, occupancy, enabled/walkable flags, result collections and receipts; addresses relevant F6 contracts.
- Both reuse the current editor-test assembly. The grid host stays inactive, generates through public `GenerateField` and is destroyed in teardown. No provider, global RNG or culture is modified.
- Reasoned mutation sensitivity: always/never bonus execution violates exact health; incremental writes violate rejection-state assertions; removed notifications violate counts/positions; early notifications violate callback-time peer-cell state; ignoring `OccupiesCell=false` violates final state. These are review arguments, not executed mutation results.

## Documentation

Created the assigned `Assets/Neoxider/Docs/Testing/Gameplay.md` entry point. It explains public runtime APIs and useful output assertions; fixed time/roll inputs; save/asset/global-state isolation; scene and NoCode handler checks versus real Play Mode scheduling; all supported sample layouts; and central Unity-version verification. Its six links point to existing module READMEs. No other owner's guide/index was edited. Coordinator can link this topic from its owned testing/package index.

The two new C# files and this public document each have unique `.meta` GUIDs. Coordinator owns folder metadata for the newly created `Assets/Neoxider/Docs/Testing` directory, per contract.

## Substantial improvement proposal

**Proposal, not implemented: shared gameplay resource scope and typed scenario snapshots.**

**Problem.** Fixture-specific cleanup and save doubles repeatedly lose exact global state, leak Unity assets and allow callback assertions to pass without delivery. Domain and scene tests express overlapping scenarios with different, sometimes weaker observations. A modular library needs repeatable contracts across editor settings and genres.

**Architecture/API.** Extend/reuse the primary owner's test-only save scope rather than creating another provider implementation. A small test-support `GameplayTestScope : IDisposable` can expose `Own<T>(T instance)` for Unity objects and `Restore(Action cleanup)` for explicit restoration. Snapshot/restore opted-in provider, RNG, scene-loader and culture state. Destroy owned objects in reverse order, then restore dependencies in finally. Keep singleton restoration explicit per module; do not automatically reset unrelated scene state.

Use a few typed, test-only snapshots rather than a universal reflection driver: placement snapshots contain position/content/occupancy/flags; cast snapshots contain resources/slot state and ordered receipts; progression snapshots contain points/unlocks. A helper such as `AssertRejectedWithoutChanges(before, after, receipts)` compares independently captured public state and an expected failure reason. Run relevant scenarios against existing runtime APIs and separately against component/NoCode adapters. Inputs stay explicit delta times, rolls and commands. Production APIs and scene wiring stay in their existing modules.

**Compatibility/migration.** Add support inside the existing editor test assembly with no runtime dependency, serialized migration or package configuration change. Migrate high-risk fixtures first, retaining names and behavior assertions. Keep `AbilitySceneRig` until its callers are deliberately migrated. The new narrow tests in this audit do not depend on this proposal.

**Acceptance scenarios.** (1) Sentinel provider/loader references and a saved key survive both a passing test and a forced assertion failure unchanged. (2) Replacing a definition twice leaves no owned native objects after teardown. (3) Reversing fixture order and repeating runs preserves RNG/culture and outputs/receipts. (4) Removing a cell event or duplicating a perk reward fails a concrete case. (5) Typed runtime and scene/NoCode paths agree on state, while lifecycle scheduling is verified separately in Play Mode. (6) Support compiles under centrally checked Unity 6.0–6.4 and 6.5+ without new production version branches.

**Effort/risk.** Approximately 2–4 engineer days for scope support, initial typed snapshots and high-risk migration, plus central Edit/Play Mode checks. Risks: cleanup masking the original failure, globals restored before saving components are destroyed, and overgeneralizing a harness. Keep support small, restore explicitly, preserve the first failure and aggregate cleanup failures.

## Verification and risks

Executed static checks only:

- Strict UTF-8 decoding of inspected files and owned additions; no encoding rewrite of existing docs.
- New C# contains no `var`, delimiters balance, and NUnit attributes describe 14 scenarios: four probability, seven rejection, two notification-mode, one dice merge.
- Existing asmdef already references `Neo.Abilities`, `Neo.Core`, `Neo.GridSystem`, `Neo.GridSystem.Dice`; reused support is in the same test assembly. No reference edits required.
- Matched relevant public signatures and event/data types to current source. `FieldGenerator.Awake` owns singleton assignment; inactive generation does not invoke it.
- Scanned package `.meta` files: each new GUID occurs exactly once. All six relative documentation links resolve.
- `git diff --check -- <owned files>` passed. Untracked new-file whitespace is additionally checked with no-index diffs because ordinary git diff ignores additions until tracked.
- Risk-pattern inspection covered all 65 files. One scan counted 554 `[Test]` and 40 `[TestCase]` attributes; these are inspection counts, not pass counts, and primary-owner edits can change them.

Not executed: C# compilation, Unity import/console, Edit/Play Mode, timing/coroutine/tween behavior, alternate-culture execution, leak reproductions or Unity-version compatibility. User and worker contract reserve all Unity/build/test processes for the coordinator. Static delimiters/signatures cannot establish compilation or runtime correctness.

Coordinator actions: run both new fixture filters after writers finish; route F1–F4 and F7–F10 to primary test/module owners; verify fixes centrally; link the guide and create `Docs/Testing.meta`. Supplemental permissions supplied no mutable existing test/runtime source, so no runtime fix is claimed. Completed fixes address demonstrated test-coverage defects through authorized new regressions.
