# Neo.Editor.Tests: source audit

Status: source audit, focused fixes, regression authoring, and static validation complete. Unity compilation and test execution were not performed and remain assigned to the coordinator.

- Date: 2026-10-23. Contract baseline: `ca198556`, package `10.19.0`.
- Assembly: `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef`.
- Ownership: the exact 169 C# files in this assembly's manifest entry, `Assets/Neoxider/Docs/Testing.md`, this report, and new `Audit20261023NeoEditorTests*` regression/support files.
- Exclusions respected: nested HtmlToUitk assembly, other workers' new audit tests, runtime/editor implementations, shared asmdefs/configuration, root/module documentation owned by other workers, scenes/prefabs/sample layout. No commits, installs, services, child agents, Unity/MCP, builds, or test runners.
- Initial scoped git status contained only this untracked report; the 169 existing source files were unchanged before this worker's edits.

Выполнен статический аудит всех 169 закреплённых C# файлов. Исправления относятся к сохранности данных, владению сценовыми объектами и native assets, а также к достоверности sample-проверок. Новые регрессии ещё не запускались в Unity.

## Review coverage and method

Every inventory file was read in full by the static inspection scripts. Manual review used test-contract/API-call projections for each file, setup/teardown bodies, allocation/cleanup and global-mutation sites, and full source around demonstrated defects. The review considered observable assertions, boundary/null cases, test determinism, lifecycle, resource ownership, reflection seams, serialization, optional dependencies, logging, conditional compilation, sample layout, and Unity identity compatibility. This is source evidence, not a claim that all tested runtime behavior works.

Coverage includes all 28 inventory areas: Abilities (domain effects, casting, modifiers, RNG and scene/NoCode adapters); Animations; Audio; Bonus; Cards; Core and architecture checks; Dialogue; Editor authoring/sample validators; Extensions; GridSystem/Dice; Level; Merge; NPC; Network; NoCode binding; Parallax; Progression; PropertyAttribute/inspectors; Quest; RPG; Reactive; Save; Settings; Shop; StateMachine; Tools; UI; and View. Existing pure math/model tests, event/output checks, lifecycle regressions, explicit platform assumptions, source/YAML validators, and tests that enter Play Mode were distinguished during review.

The inventory ledger below records every original file, its final content digest, and the applicable finding. `S` means reviewed statically without an additional demonstrated defect selected for a code change; it does not mean an executed test pass. The `Attrs` column counts NUnit test/case attributes in source, not discovered or executed cases.

## Demonstrated findings and completed fixes

### F1 — P1: sample inspection could discard the editor's work

Trigger: run either sample-scene validator with a dirty/untitled scene or an additive scene setup. Baseline `SampleAssetValidationTests.SampleScenes_HaveNoMissingMonoBehaviours` opened each sample using `OpenSceneMode.Single`, then restored only the original active path (or created a default scene). `SampleSceneCoverageTests` used the same pattern in its smoke and RPG paths. This unloads the user's scene contents and does not reconstruct additive/unsaved state; even the hidden-YAML path executed the destructive restoration block.

Fix: use an owned disposable preview scene for each loadable sample. Hidden `Samples~` YAML checks do not open or restore editor scenes. Closing the preview also runs on inspection failure. New helper: `Editor/Audit20261023NeoEditorTestsPreviewSceneScope.cs`. Regression: normal/failing preview scopes verify preview lifetime, loaded-scene setup, active scene, and dirty flag. The synthetic regression exercises resource disposal; actual sample loading still requires Unity execution.

### F2 — P1: UI mesh fixture destroyed unrelated scene objects

Trigger: run any `UIMeshRigEditorTests` case while the scene contains Canvas, EventSystem, or rig components. Baseline teardown enumerated and destroyed all matching objects, including inactive user objects. Menu tests could also select an existing rig through `FindFirstObjectByType`, producing false positives or deleting that rig in a test's `finally`.

Fix: capture pre-test scene objects and selection; remove only objects created during the synchronous fixture invocation and restore selection. Menu assertions find newly created components. Teardown without a captured baseline is harmless. Regression preserves inactive Canvas/EventSystem instances and selection while checking that a new Canvas is actually destroyed. This snapshot boundary is intended for these synchronous menu tests, not general asynchronous ownership tracking.

### F3 — P1: settings fixture could delete an existing settings service

Trigger: `GameSettingsTests.TearDown` selected the first global `GameSettingsComponent`, regardless of whether the test created it. This could delete the user's service while retaining a different fixture instance, and it left the test save provider active.

Fix: track every settings GameObject created by the fixture and clean up those references; restore the previous provider in `finally`. Regression verifies preservation of an existing inactive service. The fixture still deliberately resets the static settings facade; it is not a general snapshot of all host settings.

### F4 — P1: interaction cleanup could delete the wrong camera

Trigger: the editor contains another GameObject named `MainCamera`, or the test camera is renamed. `InteractiveObjectTests.TearDown` performed a name search instead of using an owned reference, so it could delete the existing camera and leak its own.

Fix: retain the created camera GameObject and destroy that reference. Regression renames the owned camera, then checks that it is destroyed and the other `MainCamera` survives.

### F5 — P1: reward/quest/slot fixtures could erase application saves

Trigger: `BonusSystemTests` or `QuestManagerTests` setup/teardown, or `SaveSlotTests.Setup`, called `SaveProvider.DeleteAll()` before installing an isolated provider. The active provider could be the user's PlayerPrefs or file store. Slot setup additionally deleted fixed files under `Application.persistentDataPath` even though its test uses separate provider instances.

Fix: reward and quest fixtures use a unique temporary file provider; cleanup restores the exact prior provider. Cooldown reward key writes/deletes are isolated by the same scope. Slot tests use the existing public `FileSaveProviderOptions.PersistenceRoot` API and a unique temporary directory, without changing the global provider. Entire slot directories are cleaned so `.bak`/`.tmp` files cannot survive as stale test inputs. New helper: `Save/Audit20261023NeoEditorTestsSaveScope.cs`.

### F6 — P2: save-provider changes contaminated later tests and host state

Trigger: run persistence tests with a custom provider already active, or follow them with another fixture. Progression, RPG, inventory, selector and settings tests left an in-memory provider installed; money/shop teardown replaced any prior provider with a newly created PlayerPrefs provider. Save facade/manager reset fixtures also lost the original provider.

Fix: capture and restore the exact `ISaveProvider` reference in all these owned fixtures, with temporary storage where a test may otherwise write before choosing a fake. Keep reset-contract tests' intentional reset calls, then restore the provider after their cleanup. Fourteen parameterized lifecycle regressions retain an unrelated sentinel and verify subsequent facade writes reach the original provider. A separate failure regression checks that temporary values do not leak and that the prior provider is restored. Static facade events/other module registries reset intentionally by existing tests are outside this provider scope's snapshot contract.

### F7 — P2: smoke validators missed missing scripts on children

Trigger: a sample root has no missing component, but an active or inactive descendant does. Both the required-scene loop and RPG helper counted missing components only on root GameObjects, allowing invalid scenes to pass.

Fix: reuse the existing recursive checker from `SampleAssetValidationTests`, exposing it only internally to this test assembly. Regression constructs an inactive descendant with a null serialized component slot and verifies a diagnostic containing the scene path and missing count. The synthetic missing-slot setup needs confirmation by the central Unity run.

### F8 — P2: player lookup depended on unrelated loaded scenes

Baseline RPG checks used the global `FindGameObjectWithTag("Player")`. A root-scoped validator cannot safely use that lookup, particularly when sample inspection happens in preview scenes. It could read the user's player or miss the sample's player.

Fix: search only transforms beneath the inspected roots, including inactive descendants, matching the hierarchy-based validation policy. Regression supplies an unrelated tagged player and an inactive local player, and verifies both the correct match and a local no-match result.

### F9 — P2: generated native assets leaked across successful test runs

Trigger: run the audio fixtures or configured-multiplier payline test repeatedly. Four audio fixture factories returned `AudioClip.Create` objects without releasing them; destroying AudioSources does not own those clips. The channel fixture had two additional direct clip allocations. `SpinControllerPaylineTests.AssignMultiplierData` likewise created a ScriptableObject with no cleanup.

Fix: retain and destroy generated clips in the four fixture teardowns, route the two one-second clip allocations through the tracked factory, and retain/destroy the multiplier asset. Audio clip length and existing behavior assertions are preserved. Four audio lifecycle regressions and one payline lifecycle regression observe the actual generated native references and assert they become Unity-null after teardown.

## Regression and documentation deliverables

New C# files, all with the required unique prefix and `.meta` GUIDs:

- `Editor/Audit20261023NeoEditorTestsIsolationTests.cs`: 27 authored NUnit cases (7 ordinary tests and 20 TestCase attributes), checking actual object/data/lifetime state.
- `Editor/Audit20261023NeoEditorTestsPreviewSceneScope.cs`: preview-scene ownership.
- `Save/Audit20261023NeoEditorTestsSaveScope.cs`: exact-provider restoration and optional unique temporary save storage, reusing `FileSaveProviderOptions` rather than adding another fake provider implementation.

`Assets/Neoxider/Docs/Testing.md` did not exist at intake. It is now the owned canonical testing topic, covering public C# API tests, an isolated file-save example, component/NoCode assertions, native asset ownership, scene/sample workflows, provider restoration, layout limitations, and honest verification reporting. Its `.meta` is included. No unowned documentation entry point was edited; the coordinator can link this topic from the docs index.

## Substantial improvement proposal — modular tests with explicit ownership support

**Not implemented in this audit.** The assembly couples 169 cross-module files and many runtime/editor dependencies, including an unconditional Mirror reference in the shared asmdef. Optional dependency changes can prevent unrelated pure-model tests from compiling. Several fixtures intentionally reset static registries/facades, source checks assume an `Assets/Neoxider` checkout, and cleanup conventions vary across modules. Repeated ad hoc fakes and native asset factories make it difficult to run tests in shuffled order or confidently repeat them in a host project.

Proposed architecture:

1. Introduce a small test-only `Neo.Tests.Support` assembly with an explicit `OwnedUnityObjects.Own<T>(T)` lifetime, an asset-folder lease using unique paths, and preview-scene ownership. Keep it independent of gameplay modules. Put save-provider leases in `Neo.Save.Tests.Support` so pure modules do not acquire an unnecessary Save dependency. Promote the narrow helpers added here only after the coordinator reviews their contract and naming.
2. Split domain tests into `Neo.<Module>.Tests` assemblies, with a separate integration assembly for cross-module scenarios and optional `Neo.Network.Mirror.Tests` for transport-dependent cases. Keep all support Editor/test-only; add no mandatory runtime API or global singleton.
3. Resolve package/sample roots through one injectable test context. It should recognize repository, UPM package, developer sample, release sample, and versioned imported-sample roots. Filesystem/YAML checks must report exactly which root was validated; they must not import or mutate samples.
4. Give tests that deliberately reset global state an explicit category and an isolated host-project job. For ordinary tests, restore exact provider/selection/diagnostic state and own all generated objects. Where a module already provides a clock/RNG seam, inject it; request a small typed seam from the module owner before changing wall-clock-dependent reward APIs.

Compatibility/migration: preserve existing test names and namespaces initially; move a module at a time. Update CI assembly filters and required references centrally, retaining a transition integration filter. Do not silently change package runtime assembly references, serialized fields, samples, or consumers' APIs. New support contracts should replace audit-prefixed helpers with temporary forwarding wrappers if another test already adopts them.

Acceptance scenarios:

- A pure Cards/Grid/model test slice compiles and runs with Mirror absent; the transport slice reports a clear excluded integration capability.
- Focused suites pass twice in shuffled order, including assertion-failure cleanup probes; provider identity/data, editor selection, scene setup, and native-object baselines remain unchanged outside the owned scope.
- A missing script on an inactive grandchild is reported in both imported/developer scene validation and hidden YAML validation as appropriate.
- All supported sample layouts and a package installed outside `Assets/Neoxider` resolve without a rename/import operation.
- Run the relevant slices in Unity 6.0–6.4 and Unity 6.5+, with actual compilation and test receipts for both compatibility branches.

Estimated effort: 3–5 developer days for support, an initial domain split, root resolution and CI migration; converting the entire suite is a larger incremental follow-up. Risk is moderate: assembly references/test discovery, global reset semantics, generated asset behavior, and optional package availability need coordinator-owned integration work. The payoff is independent module verification and repeatable cleanup, rather than a new runtime framework.

## Actual validation and limits

Completed without starting Unity or a runner:

- Strict UTF-8 reads and known-marker scanning of all 169 original plus 3 new C# files; no encoding issue found.
- Complete-source lexical delimiter and preprocessor-balance checks across 172 C# files: no issue. This is not a C# compiler.
- All 169 manifest files exist and resolve to the owned assembly rather than a nested asmdef.
- Checked all 40 new regression lifecycle/exercise method-group references against their fixture declarations, with method-local variable scopes: no missing reference.
- No direct legacy `GetInstanceID()`/`objectReferenceInstanceIDValue` usage in the scanned C# inventory; no `var` in new C# files.
- Four new package metadata GUIDs are correctly formed and unique under `Assets/Neoxider`; new files have no trailing whitespace.
- The five preview APIs used by the helper are present in the installed Unity `6000.3.14f1` XML API reference. The editor was not invoked.
- The active root is `Assets/Neoxider/Samples~/Demo`; all 9 required smoke-scene files exist. No sample rename/import/layout change was performed.
- `git diff --check -- <owned inventory and additions>` exited 0. Untracked new files also received explicit whitespace/UTF-8 checks because git diff does not inspect their content.

Unity compilation, NUnit discovery/execution, actual preview loading, Play Mode, native audio behavior, and version-branch compilation are unverified. In particular, the central run must confirm the synthetic missing-component serialized-array fixture and preview-scene behavior on both supported editor generations. Existing global-reset fixtures still require a dedicated clean test host; provider restoration does not claim to preserve every static event or registry. Some legacy reward assertions still depend on real UTC time. Some existing Editor-assembly UnityTests explicitly enter Play Mode and must be scheduled accordingly.

Coordinator actions: compile this assembly with all concurrent workers' additions, run the new isolation fixture and affected Audio/Bonus/Editor/Quest/Save/Settings/Shop/Inventory/Selector/Progression/RPG slices, then run the integration suite on the requested Unity versions. Link the new testing topic from the unowned docs index. Treat optional-dependency assembly separation and portable package-root resolution as proposed follow-up, not completed changes.

## Complete owned source ledger

Paths are relative to `Assets/Neoxider/Tests/Edit`. Digests are SHA-256 prefixes of the audited final source snapshot.

| File | Attrs | SHA-256 | Review |
| --- | ---: | --- | --- |
| `Abilities/AbilityAutoCastAndCooldownTests.cs` | 8 | `a24f03c4cdd3` | S |
| `Abilities/AbilityNoCodeBridgeTests.cs` | 16 | `9e889b0073f8` | S |
| `Abilities/AbilityStateTests.cs` | 6 | `7ef8785de7db` | S |
| `Abilities/AbilityTestSupport.cs` | 0 | `2b88c7a03fcd` | S |
| `Abilities/AuditFixesAbilitiesTests.cs` | 4 | `0a05e4667dfe` | S |
| `Abilities/CastPipelineTests.cs` | 19 | `453603cb5266` | S |
| `Abilities/ChainAndExecuteOpTests.cs` | 6 | `d2de74491eee` | S |
| `Abilities/DamageServiceTests.cs` | 13 | `8583fb1c82ac` | S |
| `Abilities/DeterminismTests.cs` | 6 | `03003a4d0961` | S |
| `Abilities/EffectOperationTests.cs` | 10 | `717cc9ba8df1` | S |
| `Abilities/LeveledValueTests.cs` | 12 | `07ac16c1fb9c` | S |
| `Abilities/ModifierEngineTests.cs` | 19 | `5dd17a4c5168` | S |
| `Abilities/ModifierTickAndReactionTests.cs` | 8 | `076564e269c4` | S |
| `Abilities/MotionOpTests.cs` | 12 | `fbb5507447d4` | S |
| `Abilities/ProjectileAndSpawnTests.cs` | 8 | `910f9b33a65b` | S |
| `Abilities/PropertyAggregatorTests.cs` | 11 | `011e878fbd88` | S |
| `Animations/TransformAnimatorTests.cs` | 17 | `b1faf5db5f11` | S |
| `Audio/AmChannelEditModeTests.cs` | 32 | `e3a7199e23a4` | F9 |
| `Audio/AmEditModeTests.cs` | 30 | `ce7a0ffef06b` | F9 |
| `Audio/AmEntryEditModeTests.cs` | 47 | `6fabefa0905b` | F9 |
| `Audio/AudioEditModeTests.cs` | 2 | `f371099080d1` | S |
| `Audio/AudioFocusMuteEditModeTests.cs` | 7 | `e55f8a3103e1` | S |
| `Audio/RandomMusicControllerEditModeTests.cs` | 3 | `9a07a7684883` | F9 |
| `Bonus/AuditFixesSlotRowTests.cs` | 2 | `0f362bd7e026` | S |
| `Bonus/BonusSystemTests.cs` | 4 | `7abba09d96f6` | F5 |
| `Bonus/CheckSpinTests.cs` | 11 | `32b7a436ac95` | S |
| `Bonus/CooldownRewardAutoClaimTests.cs` | 2 | `b266df53343b` | F5 |
| `Bonus/SlotEconomyDefinitionTests.cs` | 10 | `09bba5d7cee9` | S |
| `Bonus/SlotSymbolWeightOverridesTests.cs` | 13 | `77945f717332` | S |
| `Bonus/SpinControllerEconomyTests.cs` | 5 | `3cb7948dc584` | S |
| `Bonus/SpinControllerPaylineTests.cs` | 6 | `ddc238fa2a43` | F9 |
| `Bonus/WheelFortuneTests.cs` | 6 | `a0a41cf204aa` | S |
| `Cards/AuditFixesCardsTests.cs` | 3 | `89ebf1c371da` | S |
| `Cards/CardSpriteNameParserTests.cs` | 36 | `c55074fbebae` | S |
| `Cards/CardSystemTests.cs` | 21 | `865bc6a04b56` | S |
| `Cards/DeckAndHandModelTests.cs` | 12 | `732d500f0c51` | S |
| `Core/AuditFixesStaticsTests.cs` | 11 | `2842266308bc` | S |
| `Core/BootstrapTests.cs` | 2 | `ce2a9e072e45` | S |
| `Core/ModulePrinciplesTests.cs` | 6 | `59b2334afd13` | S |
| `Core/NeoDiagnosticsTests.cs` | 7 | `75a9d07d3349` | S |
| `Core/PackageVersionParityTests.cs` | 2 | `dd405878c850` | S |
| `Core/ResourcePoolModelTests.cs` | 5 | `6d16359d3785` | S |
| `Core/SubsystemRegistrationStaticResetEditModeTests.cs` | 5 | `e85a53bef58c` | S |
| `Dialogue/DialogueControllerEditModeTests.cs` | 1 | `e14494cb74cd` | S |
| `Editor/CreateMenuObjectLegacyTests.cs` | 2 | `42178aeb4619` | S |
| `Editor/NeoxiderCreateMenuTests.cs` | 10 | `b21d59cf7e30` | S |
| `Editor/SampleAssetValidationTests.cs` | 7 | `297de6c19860` | F1, F7 |
| `Editor/SampleSceneCoverageTests.cs` | 3 | `d77de360be5d` | F1, F7, F8 |
| `Editor/SceneSaverAutoSaveTests.cs` | 9 | `418634927ed7` | S |
| `Extensions/ExtensionsLifecycleTests.cs` | 4 | `ff22afe426c0` | S |
| `Extensions/ExtensionsTests.cs` | 89 | `76f42db06add` | S |
| `Extensions/UnityExtensionsTests.cs` | 25 | `6b6fff71a9ce` | S |
| `GridSystem/AuditFixesMatch3BoardServiceTests.cs` | 2 | `6ce675ad153f` | S |
| `GridSystem/Dice/DiceBoardCoreTests.cs` | 5 | `8b422ecf64d7` | S |
| `GridSystem/Dice/GridMergeAndDiceTests.cs` | 23 | `7a6f643d5186` | S |
| `GridSystem/FieldGeneratorCoverageTests.cs` | 9 | `bda3e89da714` | S |
| `GridSystem/GridPlacementServiceTests.cs` | 8 | `5c734cebc524` | S |
| `GridSystem/GridSlotAllocatorTests.cs` | 11 | `05ffff32e58e` | S |
| `GridSystem/GridSystemTests.cs` | 10 | `3a4438f2d1fd` | S |
| `GridSystem/Match3BoardServiceCoverageTests.cs` | 2 | `197f5b75e338` | S |
| `GridSystem/SlidingMergeResolverCoverageTests.cs` | 3 | `4880eef02157` | S |
| `Level/LevelCurveDefinitionTests.cs` | 28 | `6dc731087fa8` | S |
| `Level/LevelManagerTests.cs` | 2 | `b57eac6aada0` | S |
| `Level/SceneFlowControllerTests.cs` | 7 | `b4ac36597878` | S |
| `Merge/MergeResolverTests.cs` | 7 | `7ffcdb7c0add` | S |
| `NPC/NpcCombatDecisionCoreTests.cs` | 8 | `12175140127e` | S |
| `Network/LocalPredictionModelTests.cs` | 17 | `f4b5683a593f` | S |
| `Network/NeoNetworkManagerHardeningTests.cs` | 9 | `c7951365d881` | S |
| `Network/NeoStartupCommandLineTests.cs` | 25 | `f4ea66da7510` | S |
| `Network/NetFragmentationTests.cs` | 18 | `e800edb756b4` | S |
| `Network/NetQuantizationTests.cs` | 22 | `215608961075` | S |
| `Network/NetRealtimeMirrorTests.cs` | 17 | `85883947fd77` | S |
| `Network/NetTrafficMeterTests.cs` | 10 | `24197a9c79fa` | S |
| `Network/NetworkContextActionIngressTests.cs` | 22 | `7419dc491116` | S |
| `Network/NetworkDiagnosticsTests.cs` | 3 | `2e227f452637` | S |
| `Network/NetworkDocsConsistencyTests.cs` | 3 | `056478a0ee6c` | S |
| `Network/NetworkRateLimitTests.cs` | 1 | `6517cb68783f` | S |
| `Network/NetworkSessionHelpersTests.cs` | 9 | `d80c37839808` | S |
| `Network/SnapshotBufferTests.cs` | 14 | `01569036c734` | S |
| `Network/SnapshotTimelineTests.cs` | 16 | `8df34c0e3af5` | S |
| `NoCode/NoCodeBindEditModeTests.cs` | 11 | `c353bffb514c` | S |
| `NoCode/NoCodeBindLateSpawnTests.cs` | 1 | `a8d3791e5601` | S |
| `NoCode/NoCodeBindTextLazyResolveTests.cs` | 3 | `16633ff3fff2` | S |
| `Parallax/ParallaxLayerEditModeTests.cs` | 2 | `57458c580478` | S |
| `Progression/ProgressionManagerTests.cs` | 11 | `e3eb6196dad4` | F6 |
| `PropertyAttribute/InspectorButtonCoverageTests.cs` | 2 | `e87b754bf772` | S |
| `PropertyAttribute/InspectorControlIdStabilityTests.cs` | 3 | `7eeb1b3b8109` | S |
| `PropertyAttribute/NeoComponentHealthTests.cs` | 9 | `eb81f433eb45` | S |
| `PropertyAttribute/NeoInspectorAnimationTests.cs` | 24 | `a26dd90d8ec1` | S |
| `PropertyAttribute/PropertyAttributeEditModeTests.cs` | 11 | `a7f19d189367` | S |
| `Quest/QuestManagerTests.cs` | 10 | `9732081ac806` | F5, F6 |
| `RPG/HealthComponentTests.cs` | 15 | `41111c2dcdee` | S |
| `RPG/RpgCharacterProfileServiceTests.cs` | 4 | `2cbb48550fae` | S |
| `RPG/RpgCharacterResourceServiceTests.cs` | 6 | `b53cd91d60fa` | S |
| `RPG/RpgCharacterTests.cs` | 7 | `a93662efea04` | F6 |
| `RPG/RpgCombatEdgeTests.cs` | 9 | `05e87ad10e3d` | S |
| `Reactive/ReactivePropertyConcurrencyTests.cs` | 5 | `90ef53ef59eb` | S |
| `Reactive/ReactivePropertyLifetimeTests.cs` | 13 | `3560b4eb5c59` | S |
| `Reactive/ReactivePropertyTests.cs` | 9 | `633374fafae5` | S |
| `Save/FileSaveProviderDurabilityTests.cs` | 7 | `dbd303184a45` | S |
| `Save/SaveEncryptionEditModeTests.cs` | 15 | `3824f1164006` | S |
| `Save/SaveIdentityUtilityTests.cs` | 3 | `b73452ee1b5c` | S |
| `Save/SaveManagerReadModifyWriteTests.cs` | 5 | `27dcd20e426e` | F6 |
| `Save/SaveProviderTests.cs` | 6 | `306d40c7180c` | F6 |
| `Save/SaveSlotTests.cs` | 1 | `4e535a06cfc6` | F5 |
| `Save/ShopManagerTests.cs` | 8 | `f98aa5d8efde` | F6 |
| `Settings/GameSettingsStaticResetTests.cs` | 4 | `8f6124309cb7` | S |
| `Settings/GameSettingsTests.cs` | 6 | `1b063d456a77` | F3, F6 |
| `Shop/AuditFixesMoneyTests.cs` | 3 | `51ceaa0a23d3` | F6 |
| `Shop/EquipmentManagerTests.cs` | 7 | `cbd1853392fc` | S |
| `Shop/MoneyCapEditModeTests.cs` | 3 | `97dbf61702e0` | S |
| `Shop/MoneyDepositGuardTests.cs` | 6 | `ec7ca70f659d` | S |
| `Shop/MoneyPersistenceEditModeTests.cs` | 5 | `cc51e8297fee` | F6 |
| `Shop/ShopAffordabilityTests.cs` | 13 | `c1c473bd657d` | S |
| `Shop/ShopCategorySelectorTests.cs` | 6 | `276adf0345e0` | S |
| `Shop/ShopListViewCategoryBarTests.cs` | 3 | `d5a0805dcbb2` | S |
| `Shop/ShopProfileDataTests.cs` | 10 | `ea6f2edbe097` | S |
| `Shop/ShopPurchaseRollbackTests.cs` | 4 | `e898641d2b75` | S |
| `Shop/ShopResolverTests.cs` | 3 | `badaba9f823f` | S |
| `Shop/ShopVariantsPanelTests.cs` | 5 | `bba73bd974aa` | S |
| `StateMachine/StateDurationPredicateTests.cs` | 5 | `af54320331fc` | S |
| `StateMachine/StateMachineOptimizedTests.cs` | 28 | `0620aeae9f3d` | S |
| `Tools/AuditFixesAttackSystemTests.cs` | 4 | `a066215f458d` | S |
| `Tools/AuditFixesDrawerTests.cs` | 3 | `094e878567ce` | S |
| `Tools/AuditFixesInteractiveObjectTests.cs` | 4 | `9a4476ba9023` | S |
| `Tools/AuditFixesSelectorTests.cs` | 3 | `fcf613628492` | S |
| `Tools/AuditFixesSpawnerTests.cs` | 2 | `4cd1ffa09f8f` | S |
| `Tools/AuditFixesTimeTests.cs` | 4 | `a0dedfba26f2` | S |
| `Tools/AuditFixesTypewriterTests.cs` | 2 | `dd937bf29f56` | S |
| `Tools/BillboardUniversalEditModeTests.cs` | 2 | `800d54379f81` | S |
| `Tools/CharacterControllerInputTests.cs` | 25 | `d4aed4c8a9bc` | S |
| `Tools/CursorLockPlayerControlTests.cs` | 16 | `a6a961fc3d26` | S |
| `Tools/Debug/DebugToolsBehaviorTests.cs` | 4 | `99a3b0adf029` | S |
| `Tools/FakeLeaderboard/FakeLeaderboardBehaviorTests.cs` | 4 | `fd9293f04b99` | S |
| `Tools/FreeFlyCameraControllerTests.cs` | 3 | `9e8d8bbcf113` | S |
| `Tools/ImageFillAmountAnimatorTests.cs` | 17 | `8e189c15e929` | S |
| `Tools/InteractionQueryMathTests.cs` | 12 | `d5d12068354f` | S |
| `Tools/InteractiveObjectAimSymmetryTests.cs` | 5 | `4dd5c46e6511` | S |
| `Tools/InteractiveObjectMouseTrigger2DTests.cs` | 4 | `30f5bdca16d2` | S |
| `Tools/InteractiveObjectTests.cs` | 12 | `35ffa63aedc8` | F4 |
| `Tools/Inventory/InventorySystemTests.cs` | 8 | `15e6b01fd81d` | F6 |
| `Tools/Move/TransformMoveTests.cs` | 4 | `7288a8950862` | S |
| `Tools/MoverPrefabColliderOffsetTests.cs` | 1 | `e73ef5dcda0d` | S |
| `Tools/NeoConditionTests.cs` | 19 | `57dc7ebf7357` | S |
| `Tools/NeoConditionThrottleTests.cs` | 3 | `03956996e369` | S |
| `Tools/Other/OtherToolsBehaviorTests.cs` | 3 | `b357c911be68` | S |
| `Tools/PhysicsToolsTests.cs` | 6 | `7971f6107bca` | S |
| `Tools/Random/ChanceManagerTests.cs` | 13 | `b4a7ba949834` | S |
| `Tools/SelectorTests.cs` | 17 | `0df5ac4991cb` | F6 |
| `Tools/SpawnerDenyZoneTests.cs` | 4 | `9d892046bccc` | S |
| `Tools/SpawnerTests.cs` | 6 | `0f20f966afd7` | S |
| `Tools/TimerTests.cs` | 18 | `b5e4c19bdeab` | S |
| `Tools/ToolsComponentsTests.cs` | 4 | `fa6567831640` | S |
| `Tools/ToolsDiagnosticsDefaultsTests.cs` | 14 | `6feb52614b96` | S |
| `Tools/TrustedClockTests.cs` | 7 | `02c8770e183d` | S |
| `UI/AnimationFlyEditModeTests.cs` | 5 | `b890b862d796` | S |
| `UI/AuditFixesAnimationFlyTests.cs` | 3 | `575fcf64aa1e` | S |
| `UI/ButtonChangePageTests.cs` | 2 | `8be1dc4948bd` | S |
| `UI/ButtonScaleTests.cs` | 1 | `dac1d07f6bf7` | S |
| `UI/CategoryBarTests.cs` | 13 | `f1f3bf44caa4` | S |
| `UI/SetTextTests.cs` | 5 | `047eee845d8a` | S |
| `UI/TimeToTextTests.cs` | 9 | `fbaded1dc535` | S |
| `UI/UIMeshRigEditorTests.cs` | 13 | `8adc3d874cc0` | F2 |
| `UI/UIMeshRigGeometryTests.cs` | 4 | `a2706259dfd3` | S |
| `UI/UIMeshRigMotionTests.cs` | 18 | `0def854f02bd` | S |
| `UI/UIMeshRigSpriteRendererTests.cs` | 6 | `d727338a0b39` | S |
| `UI/UIMeshRigTests.cs` | 11 | `024162d0390e` | S |
| `UI/VisualToggleEditModeTests.cs` | 1 | `b51880ca2565` | S |
| `View/AnimationsTests.cs` | 7 | `a11e1514cc6e` | S |
