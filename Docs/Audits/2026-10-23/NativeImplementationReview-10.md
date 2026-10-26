# Native implementation review 10 — primary Edit and Play tests

Status: **source review complete; bounded sample-evidence and async failure-cleanup corrections remain, plus the explicitly queued runtime clock integration**. No compiler/import/test/validator execution. Only this review MD is written. Target `demo` / `11.0.0`; implementation base `ca198556`; local review date 2026-10-24. The global source/execution gate remains closed. Authored cases and entrypoints are not run results. No product source, tests, owned public docs, original receipts/plans, configuration, metadata or isolate is edited; no Unity/MCP, compiler, test/checker, process/service/install or child agent is launched.

Owners: SAME `Neo.Editor.Tests` / `neo-audit-1023-b03-04` and `Neo.Tests.Play` / `neo-audit-1023-b06-04`. Runtime dependencies, supplemental peer harness and manual native capabilities receive no approval from this source pass.

## Complete read coverage

Read both complete implementation receipts and requests, including the appended early Environment/real UPM sample helper contract. Read the complete ValidationHost (460 lines), Paths (122), ClockScope (61), NetworkTestHelper/NetworkTestSession (305), new Play isolation fixture (323), existing Play wait fixture (107), native Physics fixture (150), current dispatcher/relay fixtures and all phase hunks in the other 11 legacy Network fixtures. Read local Mirror/Neo manager OnDestroy ownership context. The Play source branch remains tied to actual Mirror/test assembly dependencies; no absent-Mirror compilation is implied.

Read every implementation-phase hunk in all 44 Editor C# paths listed in its receipt, including all Animation/Audio/Bonus/Cards/Grid/Level/Progression/Shop/PropertyAttribute/Tools/UI/Extensions groups. Read the complete current Edit isolation fixture (670 lines), both sample fixtures (430/473), SaveScope and PreviewSceneScope support, all new support bodies/metas and both changed testing guides. Source-traced the relevant Typewriter runtime cancellation/GetResult path, known-input Progression arrangements and TransformAnimator's actual RandomizeTime call. Earlier coherent audit hunks are distinguished from new phase work; other owners' prefixed fixtures/configuration are not claimed as primary-owned changes.

## NI10-01 — concrete OPEN runtime integration dependency: cooldown persistence clock

Original runtime owner: `Neo.Bonus` (same original actor through root), dependency of primary Edit deterministic Bonus fixtures. At this dependency read, `Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs` declared `Neo.Bonus.CooldownReward : Neo.Tools.TimerObject` and consumed NeoTrustedTime.UtcNow for reward eligibility, but had NO GetUtcNow override. `Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs:189` already publishes `protected virtual DateTime GetUtcNow()`; inherited SaveState/LoadState use it for persisted real-time deadlines.

Required bounded integration signature on CooldownReward: `protected override DateTime GetUtcNow() => NeoTrustedTime.UtcNow;`. The deterministic fixtures arrange fixed trusted 2030 UTC and assert exact 60/1-second deadlines, so device UTC persistence is an actual mismatched clock dependency, not a reason to weaken the cases or reset to a default provider/clock. Source absence was independently confirmed and sent to root. During this review root queued the SAME Bonus owner and that runtime correction became active; this report does not inspect/approve its unfinished replacement. Its later source-final receipt/current source still require reconciliation. No dependent runtime source was edited or test executed.

## NI10-02 — P2: hidden YAML can still pass native/Playable sample cases; root count is not sample identity

Owner: SAME `Neo.Editor.Tests` / `neo-audit-1023-b03-04`. Root explicitly accepted this bounded correction.

Current `Assets/Neoxider/Tests/Edit/Editor/SampleSceneCoverageTests.cs:48–109`, `RequiredSmokeDemoScenes_OpenWithDemoInfoAndNoMissingScripts`: hidden paths at64–68 call CheckHiddenSampleSceneYaml and continue to a successful result, without opening a scene or resolving native sample types. `RpgRuntimeDemoScenes_AreWiredForPlayableSmoke:113–173` similarly executes only hidden YAML at126–136 and returns success. That contradicts the implementation receipt/Testing.md statement that hidden-only native integration is explicitly inconclusive. It can report named Open/Playable coverage when sample scripts/scenes were never imported. Source-confirmed control flow; no test was executed.

Minimal repair: retain nonempty hidden YAML/metadata/required-marker packaging coverage as distinct packaging evidence; native/Playable named cases must explicitly Ignore/inconclusive when their required developer/imported assets and compiled sample types are unavailable. Native cases must fail real load/type/reference errors once their active prerequisites are present. No source layout rename, copy fallback, scene fabrication or weakened content assertions.

Completeness guard: `SampleAssetValidationTests.FindAssetsInAssetDatabaseSampleRoots:317–329` compares root COUNT to manifest.samples.Length. `Audit20261023NeoEditorTestsPaths.SampleRoots:79–98` can return both developer and imported roots for the SAME sample. Thus duplicate roots of one identity can inflate the count while a different manifest sample has no active root. Replace that count proxy with explicit required current-version manifest identity coverage; select/record each active root deliberately and preserve all supported layouts. Add controls for duplicate developer/import roots plus a missing different sample, all declared identities present, and hidden-only native inconclusive versus real packaging checks. A count of at least three arbitrary roots is not full sample coverage.

## NI10-03 — P2: an async observation failure can bypass yielded Play Mode exit

Owner: SAME `Neo.Editor.Tests` / `neo-audit-1023-b03-04`. Current `Assets/Neoxider/Tests/Edit/Tools/AuditFixesTypewriterTests.cs:135–140`, `StopAndObserveEveryRun`; fault/timeout paths at143–170 in DrainOperations/Complete. Source-confirmed exceptional control flow; no failure injection was run.

Trigger: a retained asynchronous operation faults when public GetResult is observed, or the three-second settlement assertion fails during UnityTearDown. The teardown's finally clears only `_effects`; `yield return new ExitPlayMode()` is AFTER the try/finally, so an observation error bypasses mode exit. The fixture can contaminate later Editor tests precisely when its SUT/regression fails. The intended async failure must remain a failed test rather than being swallowed, but cleanup must still settle/release what it can and yield mode restoration.

Smallest repair: same owner restructures bounded teardown so fault/timeout observation cannot bypass owned-run cleanup and a yielded ExitPlayMode, then propagates the original failure. Do not manually exhaust Unity teardown, replace GetResult with a no-throw/skip, or introduce a general async framework. Add a control with a genuinely faulted operation retained for teardown and a bounded nonsettlement/failure path where feasible, asserting that the failure is preserved and the following Editor context is restored. Existing normal cancellation is correctly consumed rather than discarded; that success path does not cover this exceptional tail.

## Early host helpers — scoped source closure and execution conditions

Exact public static execute-method entrypoints exist in the existing Editor test assembly:

- `Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.ProbeEnvironment`
- `Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.ImportPackageSamples`

Before its preference mutation/import work, RequireOwnedHost checks the actual project path contains `/Local/Audits/assembly-2026-10-23/unity/final/`, explicit company/product arguments match strict task prefixes and Application values, effective persistentDataPath ends in that exact company/product, and output is within that project. The operator must set unique company/product BEFORE first Unity launch and retain the frozen source/profile ledger. These checks align with final host/persistence plan constraints; they are source guards, not yet observed identities.

ProbeEnvironment records effective paths/provider/platform/version, rejects non-WindowsEditor or unknown/out-of-root file-provider state, then roundtrips a GUID-owned PlayerPrefs sentinel and observes the actual HKCU Editor/player branch, value name and encoding. Finally deletes only that key, saves the already proven task namespace and checks in-memory/observed-registry removal. It does not reset the provider, change PlayerSettings, invent persistentDataPath CLI overrides or establish separate-player namespaces. Windows players/other native platforms still need their own actual probe.

ImportPackageSamples requires the registered exact local/embedded frozen package root, version 11.0.0, all three manifest identities and absent import destinations. Public UPM Sample.FindByPackage/Import(None) is invoked through reflection. SessionState resumes domain reload, ImportUpdate revalidates the host/output and uses a ten-minute monotonic Editor deadline. It records native return observation separately from a reload during Import, actual UPM imported state, imported paths/GUIDs/dependency/reference counts and native prefab missing-script resolution. Manual copying is not its fallback. Actual public API/assembly availability, import/domain reload, full scene integration and CMF duplicate handling remain unexecuted capabilities, with operator orchestration still required under the existing verification plan.

Both entrypoints return void and exit batch mode themselves; no early `-quit` for the asynchronous path. Arguments are exactly `-neoValidationCompany`, `-neoValidationProduct`, `-neoValidationOutput`, plus `-neoValidationPackageRoot` for import. The source initializer resumes only an existing SessionState import; ordinary package load does not run a probe/import. These are suitable explicit owner-authored drivers within the current scope, not general environment isolation proof. The operator must actually register the frozen root supplied in the argument, retain snapshot SHA externally, prove fresh destinations/CMF ownership, and separately verify three peer persistence identities. The helper does not establish those facts by receiving a path string.

Practical profile condition: these methods live inside the Editor-only `Neo.Editor.Tests` asmdef gated by UNITY_INCLUDE_TESTS, with broad hard references including Mirror. A release-consumer must enable the package's test assembly through the actual supported testables/define configuration before executeMethod is available, without running save-mutating fixtures first. A dependency-absent profile may expose the existing assembly graph blocker; it cannot silently copy the helper into a fake standalone assembly or remove product references. Actual early compilation/API availability and all profile namespace receipts remain unexecuted. Root added the explicit UnityEngine.UI test-assembly reference during this review; the current hash is recorded as root integration context, not this test owner's edit or a compile result.

## Edit action closure and observable controls

`EDITOR-TEST-REMAINING-ISOLATION`: the three legacy Shop fixtures acquire SaveScope before components and destroy them while it remains installed; exact provider restoration is in finally. The rollback fixture now wraps the isolated provider with a one-shot Shop SetString failure BEFORE storage mutation, retaining exact debit refund, unowned/failure and retry assertions. Its separate postcommit case checks paid ownership/persisted id, wallet400 and notification fault/no purchase failure after a throwing observer. This consumes the deliberate 11.0 boundary rather than weakening the old rollback assertions. All custom provider members match ISaveProvider, including Bool. Missing shared Shop/Network runtime fixes still require the final integrated snapshot.

SaveScope captures the exact provider and owns a GUID temporary FileSaveProvider directory. It does not promise to restore every global singleton/event. The six new Shop ambient-profile normal/failure cases retain foreign Shop/variant values and verify subsequent writes to the restored provider. Timer uses its own GUID keys under a temporary provider; destruction precedes deleting keys and restoring provider, with explicit no-residue assertions and old fixed-key sentinels. PlayerPrefs array keys are GUID-owned and no longer flush/delete four fixed ambient keys on unrelated cases.

`TEST-BONUS-CLOCK-DETERMINISM`: ClockScope captures `_clock`, `_restored`, `_persistedHighWater` via exact fields without invoking the mutating Clock getter, installs its own save/TrustedClock and advances UTC/monotonic together. Dispose restores the exact borrowed objects/lazy fields before restoring provider. Four normal/failure reward-adapter cases preserve nondefault clock/lazy/high-water/provider sentinels. Exact accumulated claims/timestamp and 60/1-second countdown controls are meaningful once NI10-01 is integrated; do not accept them as run now.

`TEST-GAMEPLAY-STATE-RESTORE`: SceneFlow restores the exact injected loader, cursor fixture restores management/lock/visibility after object destruction, and diagnostics restore captured flags. A fractional 12.5 fr-FR roundtrip uses invariant write/read in the Money test double. RNG capture/restore now covers the touched audio/animation/slot/selector/chance/component/other/leaderboard fixtures. The new TransformAnimator sentinel actually calls Component_Stop_RestoresBasePose, whose current body invokes RandomizeTime at217; it is not a vacuous unchanged-state check. Slot reconfiguration destroys the prior ScriptableObject and explicitly tests fake-null retirement.

`TEST-GAMEPLAY-ASSERTIONS`: the 120-XP test uses a separate Core linear 100 curve and exact level 2/points 2; spending the only earned point reloads as 0. Premium first earns level 2, then grants only achieved premium 3+7 atop ordinary 1, requires 11, excludes future 1000 and proves repeated activation/reload cannot duplicate. Match3 enters real Play Mode, creates a real matching board, observes a Clear/Changed phase before disabling the component, then requires no later Collapse/Refill/completion and cleared cells. This replaces the fabricated handle/Assume path; native scheduling/scaled-clock prerequisites still need the run. Four Card cases observe active captured tween targets, advance ONLY those tweens to nonrest/return poses, check enter retirement and destruction with/without prior disable. No global DOTween tick/kill is used.

`TEST-UTILITY-CAPABILITY-ORACLES`: AM clip/loop/state assertions no longer skip based on failed SUT playback; playback-only behavior first probes a separate native looping AudioSource, and subsequent AM failure must fail. Native probe unavailability remains explicitly inconclusive and does not prove audible output. Reflected Update/Repaint methods must exist; throttle has before/after positive controls. Timer.Dispose now starts a live unscaled timer, observes updates, then requires no callbacks/progress/end afterwards. Typewriter obtains public awaiters, checks completion and calls GetResult, so asynchronous errors become failures; restart/external cancellation/no-later-text/end controls use the actual player loop. NI10-03 remains in exceptional teardown exit. The current comment claiming no UniTask reference is stale relative to the root-integrated asmdef; reflected awaiter consumption is still valid and no additional reference request is needed.

`TEST-UTILITY-FAILURE-CLEANUP`: locally owned board/parent/target roots are registered or protected in finally; random-music controllers, native clips/sprite/texture and partially advanced spawner IEnumerator receive cleanup. Condition/Unity-extension objects are tracked. Inspector lease expiry queries only the owned client, while a live foreign client survives with exact counts. Mesh previews use addressed SetTime/StopPreview, preserve a foreign preview and borrowed update subscription; menu cleanup distinguishes new objects from baseline and restores selection. Empty preview normal/failure tests preserve loaded scene setup/dirty state but do not establish every imported scene deserialization path. No whole-editor future tick/StopAllPreviews remains in these changed cases.

## Play source closure and evidence boundaries

NetworkTestSession refuses borrowed active sessions, active/inactive managers and scene identities before mutation, captures exact globals, owns explicit roots/templates/body allocations, yields bounded graceful shutdown and restores captured transport/background/frame/VSync/time/random values after destruction. Replacement-session detection prevents unconditional shutdown/reset/restoration from taking over a later session. Money keeps its isolated provider until session destruction and restores it last. Mult scene remains an exclusive Single load; manager/root ownership is scoped to its own loaded scene, not a discovered-manager sweep. Native exceptional runner cleanup and Single-load timing still need execution.

WaitUntil uses a real-time assertion deadline; the 26 formerly scaled waits now use observed state plus sequenced reliable controls. Fresh barrier sequences cannot be satisfied by a prior control. Dispatcher cases enable networking explicitly and demand exactly four sent/received RPC envelopes plus exact event payloads; local fallback has a separate source case. Relay negative cases have configured channels, observed barriers and positive controls; playerless handshake checks explicitly add a player afterwards; event-channel exclusion retains an exact same-channel positive payload. Host loopback remains host evidence, not remote admission.

Physics now moves a real kinematic Rigidbody through a trigger in a local PhysicsScene, explicitly simulates, observes exactly one native enter/exit and collider identity, and distinguishes network envelopes from local zero-envelope mode. New paused/failure regressions yield real teardown, dispose suspended bodies and preserve unrelated sentinels. Caught simulated failures are not a demonstration of Unity runner automatic cleanup after an uncaught failure. No native callback, physics, diagnostics envelope or transport claim is accepted as executed.

The Play receipt says all 14 Network fixtures adopt the helper; the source includes NetworkOwnerFilterTests too, while that path is omitted from its exact-files list. This is an inventory/receipt reconciliation note for the same owner, not a runtime defect or a claim of foreign ownership.

## Deferred execution and scope limits

The owned sources have meaningful positive/state/payload controls within the boundaries above; all actual compiler/discovery/runner/capability outcomes remain UNEXECUTED. Native SetUp/TearDown on uncaught assertions, borrowed globals/order repetition, audio availability, GUI/Undo/repaint/preview behavior, render/input backends, real UPM imports and scene script/reference resolution, asynchronous cancellation faults, physics callbacks and RPC diagnostics require both installed Unity versions on the frozen source. The new Play isolation fixture adds 13 authored cases to the retained 17 audit cases; Edit isolation currently contains 45 authored parameter cases. These are source counts, not selected/passed tests. Default versus domain/scene-reload-disabled lifecycle remains unproved.

Host/DummyTransport loopback is not separate-peer authority or production transport. Supplemental harness and manual/native capabilities remain outside approval. The helper's strict empty-scene acquisition and Mult Single-load assumptions require centrally isolated worlds; a source check cannot certify actual native load timing. All 26 old scaled-wait replacements were read, preserving original payload/balance/count assertions rather than converting them to no-throw. No test-only broad framework, optional package installation or shared assembly split is required by this review.

Only root's later global source freeze can open execution. Preserve the real Bonus clock dependency and return NI10-02/03 to the SAME primary Edit actor; update the Play exact-file receipt inventory separately if root requests. No reviewer source shim, native fake, manual sample copy or capability pass is authorized here.

## Artifact identity and read ledger

SHA256 rows identify the reviewed source snapshot; they are bookkeeping, not compiler/test/checker evidence. The 44 primary Edit paths and all 14 legacy Play fixtures were reviewed at current implementation hunks; complete new helpers/regression bodies and both guides are identified above. Context files are labelled by ownership in the prose. Concurrent Bonus correction is explicitly pending and is not hashed as if its unfinished state were approved.

| Reviewed artifact | SHA256 |
| --- | --- |
| `Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `Docs/Audits/2026-10-23/Neo.Editor.Tests.implementation-request.md` | `0e87e00a460dc0eaa67af891d613d657491308f249cbd1d45130192cb4ed6244` |
| `Docs/Audits/2026-10-23/Neo.Editor.Tests.implementation.md` | `753b136a359f27d3440943a01547baf7f34377cccc22f7fea9c245de60f907b0` |
| `Docs/Audits/2026-10-23/Neo.Tests.Play.implementation-request.md` | `893b82d75cdff37108991bd95181e4930970fb64718b939c084d2c6e0105c62c` |
| `Docs/Audits/2026-10-23/Neo.Tests.Play.implementation.md` | `de94fe0f1755701c652c8da5c084e80243cde264623d96a08c87e2293fc392d2` |
| `Docs/Audits/2026-10-23/UnityVerification.plan.md` | `0385ed8259c5629b0a8d20d311533fb393d3f3fad96ae2180e1f2a54dec86fc4` |
| `Assets/Neoxider/Tests/Edit/Animations/TransformAnimatorTests.cs` | `f8b0030739c10ce0048b0624edb88906f3983281037cc1706e2f47c4e9c65eb7` |
| `Assets/Neoxider/Tests/Edit/Audio/AmChannelEditModeTests.cs` | `99ce808027bbf6645c9134511649cae1702278a4ff6b29ea06b41ea408af9376` |
| `Assets/Neoxider/Tests/Edit/Audio/AmEditModeTests.cs` | `1dd971be286936741af7ec4224c52dd2b008cf9707f8a9097edde082ffc9b792` |
| `Assets/Neoxider/Tests/Edit/Audio/AmEntryEditModeTests.cs` | `9b1eb77ee55126a9978bcf784cb3236d0378263b4f9239ebf536e14ef29e6225` |
| `Assets/Neoxider/Tests/Edit/Audio/RandomMusicControllerEditModeTests.cs` | `f101829e97d5b68751f676e1d591ee8464830d851824835199f1e4c3aaf69702` |
| `Assets/Neoxider/Tests/Edit/Bonus/BonusSystemTests.cs` | `551d82244154c08944608da42a2c7c16937dda83bf72465c684e0ed65af6a433` |
| `Assets/Neoxider/Tests/Edit/Bonus/CooldownRewardAutoClaimTests.cs` | `a32baff5eadf78c788d967d9d8be29cc9427c64f2241ad4810d9617d0e94e75d` |
| `Assets/Neoxider/Tests/Edit/Bonus/SlotEconomyDefinitionTests.cs` | `918f39b6a6baee8bb1f756cf3f28e84faded35f6dfb1602e73683e504ca73616` |
| `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoEditorTestsClockScope.cs` | `9e5852c9a2c9ff63d94f6e6a78c88b50e0f30fb0eea1157a5ba27a77c497d4cb` |
| `Assets/Neoxider/Tests/Edit/Cards/CardSystemTests.cs` | `ee095b18a7eb07cd2b25c192380280de52465f889a3a839e2d9f0d5986b113b0` |
| `Assets/Neoxider/Tests/Edit/Editor/SampleAssetValidationTests.cs` | `bcd3ce36f0d2b4f6ac5b352292037087d29f74e926c28b8c8c3a13d3b12c586f` |
| `Assets/Neoxider/Tests/Edit/Editor/SampleSceneCoverageTests.cs` | `cb3030a0913c85f4b2eb2b8219651ad989a52aec61d42526893dc2d4ee7ad046` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsIsolationTests.cs` | `2f1344626b2c815ca385e4454a6456464902656d25e0ce79bbfa236009da3d86` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPaths.cs` | `b57193c6c6bdfe4988af0a25dd43d369f4a7e39daa7f44cf1b3b78b5ec70a505` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.cs` | `95d5e0628c6186ac954407a91323617e3258174f56ebe31464c2b040cd8736c7` |
| `Assets/Neoxider/Tests/Edit/Extensions/ExtensionsTests.cs` | `27b183641dbb6bdafcb9cf8549be3268f3503bbe0e8d2b644e69684adedd5c66` |
| `Assets/Neoxider/Tests/Edit/Extensions/UnityExtensionsTests.cs` | `5a1693313ec3ee0e0154fe289e0fcab1841307d990f0ecbd696a486ddb808aa6` |
| `Assets/Neoxider/Tests/Edit/GridSystem/AuditFixesMatch3BoardServiceTests.cs` | `52d9639aecf36a41f1b7d689b4435eba465d733429f88c98303463bbc0807c6c` |
| `Assets/Neoxider/Tests/Edit/GridSystem/Match3BoardServiceCoverageTests.cs` | `3ab7e250a76ac1b15024b3b7f1ac2ee9494caf46504af3b23fe2466937206533` |
| `Assets/Neoxider/Tests/Edit/Level/SceneFlowControllerTests.cs` | `09df5a629e24472eb2eafa2afe7485ee770877e7ae26aaf2d2c5af8f26c34c53` |
| `Assets/Neoxider/Tests/Edit/Progression/ProgressionManagerTests.cs` | `04829a82ccfb2a9389a5000c3114e113002b77e6c7539b9784ced661c1eee537` |
| `Assets/Neoxider/Tests/Edit/PropertyAttribute/InspectorControlIdStabilityTests.cs` | `69ad3fb2554b4105a05f7a80693f0d5d282d0883192bd8335eacf848f42ce7f6` |
| `Assets/Neoxider/Tests/Edit/PropertyAttribute/NeoInspectorAnimationTests.cs` | `ab8d517b5ecdd31d3c406eff5b6444b517cbadca564a94b91114861b853e4b4e` |
| `Assets/Neoxider/Tests/Edit/Shop/MoneyPersistenceEditModeTests.cs` | `9e12573e187dcfb564b963a64cfd2ed16e6d4ceb6db46830d12a7e1e80f13220` |
| `Assets/Neoxider/Tests/Edit/Shop/ShopAffordabilityTests.cs` | `3b2437b9360d657852af2181533acae6e4514c320e5013c225fffe00b003d2e5` |
| `Assets/Neoxider/Tests/Edit/Shop/ShopPurchaseRollbackTests.cs` | `dadb9d4f48c3b7eeae97b40683f75026aafb6feacf15cec25e5322e412b750cd` |
| `Assets/Neoxider/Tests/Edit/Shop/ShopResolverTests.cs` | `7fb2d8cf9d865efe32fddf8db9e7bc078af01e3d394501da2030557f2d38ccdd` |
| `Assets/Neoxider/Tests/Edit/Shop/ShopVariantsPanelTests.cs` | `3493f983e7ef8c3ed8c88198bb18ca25cdf5f854da8ceec2b9111c9dec7642e0` |
| `Assets/Neoxider/Tests/Edit/Tools/CursorLockPlayerControlTests.cs` | `4770cf38d762318b2a54de2a4b6cc273572e180ce0aeace012bb857142a6a718` |
| `Assets/Neoxider/Tests/Edit/Tools/TimerTests.cs` | `ab6f4f664a355212196e5d176ae04fe59e306368f7b316033a25be23c74d9c98` |
| `Assets/Neoxider/Tests/Edit/Tools/NeoConditionThrottleTests.cs` | `3563347dcf957cc65f1a7e6d3ccbf9109a22ea45d339a84fcb3a07669401bddd` |
| `Assets/Neoxider/Tests/Edit/Tools/NeoConditionTests.cs` | `a337c79a92d8a42d3f4668b109c45b7f6bb5a8d8fe89002c4c652fa3372a74b6` |
| `Assets/Neoxider/Tests/Edit/Tools/AuditFixesTypewriterTests.cs` | `20c91a5ec85de618dd37a75a77d9762967cbe64d5c8d5265de535c2606c0d205` |
| `Assets/Neoxider/Tests/Edit/Tools/AuditFixesSpawnerTests.cs` | `ff83dc7d44e8a37bfa7eb7d0dc487c8b1a237ecef48e4bae49706f810e99b840` |
| `Assets/Neoxider/Tests/Edit/Tools/Move/TransformMoveTests.cs` | `c924b5eda451ff0e381a2d2dff3086a8f22349e1721076cc1eb74a8ced0c04b0` |
| `Assets/Neoxider/Tests/Edit/Tools/InteractionQueryMathTests.cs` | `210e83f506d641186521d540f3e8e59f912b3716e78adfb8e6c9eccc47beb079` |
| `Assets/Neoxider/Tests/Edit/Tools/MoverPrefabColliderOffsetTests.cs` | `2f239ca34d4a55ebdb14bac313b31c610ba02fff06578213246d0dae31c21f10` |
| `Assets/Neoxider/Tests/Edit/Tools/SelectorTests.cs` | `f41b801873281e10f3e3fd77c8d1cd078876a5bb802a181f180637c7407bfffa` |
| `Assets/Neoxider/Tests/Edit/Tools/Other/OtherToolsBehaviorTests.cs` | `586f21ce787c131097334be5ee061d81b7850909efc7b5ab511b0b248ed4a92c` |
| `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/FakeLeaderboardBehaviorTests.cs` | `e57604896b744538cb4b4673e1ee20ab933564a544db8abf90f5232a2dbf56da` |
| `Assets/Neoxider/Tests/Edit/Tools/ToolsComponentsTests.cs` | `a628de80a34c12e734f704800d106da705e7068dbad40e15dc6753b6bd93acf2` |
| `Assets/Neoxider/Tests/Edit/Tools/Random/ChanceManagerTests.cs` | `8c1a652db7f008288c2014b71fcbc296849cb844b11d35a6403272a48843a8c0` |
| `Assets/Neoxider/Tests/Edit/UI/AuditFixesAnimationFlyTests.cs` | `07c0f6f8e35c2f465d7d8e97e33f8cb8c90f4c729c06cb95e948daed32ba257d` |
| `Assets/Neoxider/Tests/Edit/UI/UIMeshRigEditorTests.cs` | `ffc6f68f2ecc0248a35747045cbee5ad7da7f96493365a887883e1dbfd540d6e` |
| `Assets/Neoxider/Tests/Play/Network/NetworkTestHelper.cs` | `09c518b1bf11feac9f9592c96d178d0f91c01aaf1497ef4a5bdc5f241cbe2f81` |
| `Assets/Neoxider/Tests/Play/Network/NetworkEventDispatcherTests.cs` | `b68f226ef3d89f040a081565a43e178b3ba24076e22c63b782297108aa112c8b` |
| `Assets/Neoxider/Tests/Play/Network/NetworkActionRelayTests.cs` | `7c2b5458c94f918cf37bb2231473151086fe0d7b132c8c525a8608e8784136e9` |
| `Assets/Neoxider/Tests/Play/Network/NetworkToolsTests.cs` | `102fd5d489786819125b7575d7c61fe959cc06de8955dcaff361ce86db94ac95` |
| `Assets/Neoxider/Tests/Play/Network/MoneyNetworkTests.cs` | `e1218ff56d435135eeecd1624bdba2df842b878f8b8b591a25705703d8505a19` |
| `Assets/Neoxider/Tests/Play/Network/NetworkSingletonTests.cs` | `f801f80c53bcf1c33c3916ba5b21f99092101a59e447d62b19d9d184cd2b7bd3` |
| `Assets/Neoxider/Tests/Play/Network/InteractiveObjectNetworkTests.cs` | `ab445b447dc85eafe3f0ee367013bf1c73fd2eb1eaeb1b91dc21c68ac1b0a834` |
| `Assets/Neoxider/Tests/Play/Network/PhysicsEventsNetworkTests.cs` | `8048a7545990a049a262b6bffba4306b125243fbe90030b13f40652d18e46a56` |
| `Assets/Neoxider/Tests/Play/Network/NeoNetworkBootstrapTests.cs` | `3532767a6d568ffb5c9da471ac9c59e8b75129ee0af2e225a1a8484d87983550` |
| `Assets/Neoxider/Tests/Play/Network/NeoNetworkManagerScenePlayerTests.cs` | `f224489c3ccd841c10b6a9ee433b034cdf162b2f00babd9dbc96b680c57aed64` |
| `Assets/Neoxider/Tests/Play/Network/NetworkContextActionRelayTests.cs` | `43937d576a1b4d0090d2af0888c199e0367ea2d57856aa86da4cee2a5aa43f4e` |
| `Assets/Neoxider/Tests/Play/Network/NeoNetworkManagerSessionTests.cs` | `7cee12dddcd3f3cf7f492a3cb9e6ccd2f7d8ddf7e6d84c9125626e2ea920730b` |
| `Assets/Neoxider/Tests/Play/Network/MultSceneNetworkTests.cs` | `ae2cd9b2f6ae5c32929a51abd17a19ff1839d1213b48787f3eb6173168313ece` |
| `Assets/Neoxider/Tests/Play/Network/RpgCharacterNetworkAdapterTests.cs` | `56a7d78fca4cbb8d91ff5f1fc3b51c075019578b81f53705ed41b8a12bbfe641` |
| `Assets/Neoxider/Tests/Play/Network/NetworkOwnerFilterTests.cs` | `5f6fb7763f9b417a9f570c73a127dcdb071950c5dd8cdd6687905b1cf39ba248` |
| `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoEditorTestsSaveScope.cs` | `91353881cf6b61059bc59d3f7421241ccf1aa1a3b54db2a5ce0c96152cb3d43c` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPreviewSceneScope.cs` | `5d201b58807f061d2d6076fb49550a891b41c12edae7147a19acd72b585b68cb` |
| `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoEditorTestsClockScope.cs.meta` | `8b8de342878076e7f2988024671e0d3768e091161e888e4e76adbc99b114ee13` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPaths.cs.meta` | `ae7e33058f16d538a76e33c90e8768b9f8add8940a1eefce1731e7bb64818139` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.cs.meta` | `661a0e8d6fe8fa725956875664a8005bc496a4aa562041a9e5707d035704e96f` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoTestsPlayNetworkIsolationTests.cs` | `c8d982992548d6033af1b261488737eb6700cd747eaf602a632bb691ae837d80` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoTestsPlayNetworkIsolationTests.cs.meta` | `6f6b2e2f1ed8aa9921f30d26d27730e02f5eeca3df5598c4efec457d3c4fa6ca` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoTestsPlayNetworkWaitTests.cs` | `686568fc853b0a9d68f3198fc79890e2dbba24189fe8cd0c8ca116b1351bdd46` |
| `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |
| `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` | `1856ba6d704ec0852b65cf22f63fdc0427818c49259f6f2032488b02745d62ad` |
| `Assets/Neoxider/Docs/Testing.md` | `cfe5a63e1a928e9450641da32a7f1b7bb05972008bfc46afba2520e063a7a2a8` |
| `Assets/Neoxider/Docs/PlayModeTesting.md` | `a3d4e70c0f67c4e6ae4c5d1ece8c5a2b2ccbde2f7773f8b0a3edf804d6dc2fda` |
| `Assets/Neoxider/Scripts/Tools/Components/TypewriterEffect.cs` | `5386f59e62dee379738488bbb801c02829d718fe4b747d71f881aacc54287fe1` |
| `Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs` | `2131a3f3d8fc214490bd06b12b47e52c5bb3b60bd74257c3041301eab8840489` |
| `Assets/Neoxider/Scripts/Animations/TransformAnimator.cs` | `4a77548e4560056478e1629bcb8d324339b990552be18c35029339e2ec7daba4` |
