# Native implementation review 24 — primary Editor NI17 follow-up and real discovery helper

Status: **SOURCE REVIEW COMPLETE; no new confirmed bounded code gap found**. NI17-01 is corrected in the reviewed source. The new discovery/Editor-graph helper uses the actual installed UTF API and is suitable for the operator's later native acceptance. This is not a compile, import, discovery, test or persistence-isolation pass.

Reviewer: SAME `/root/audit_abilities`, native `gpt-6.1-sol` / `max`, repository `D:/Git/NeoxiderTools`, target `demo` / `11.0.0`. Only this Markdown is written. No product/config/public-doc/test/other-report edit, Unity/MCP, compiler/test/checker/validator execution, installer, service/process management, children, commit or push. The global execution barrier remains closed.

## Admission and scope

Read the complete latest primary request and final receipt first. Exact original owner metadata `neo-audit-1023-b03-04` reads `state=done`, `exit=0`, consistent with root's exact-writer completion notice. No process was queried or controlled. First receipt/request/NI17 identities were captured at **2026-10-23T21:22:02.424543+00:00**.

Main capture at **2026-10-23T21:24:01.779264+00:00** admits **53 paths**, including 47 owner C# sources. **43 owner C# files exactly match NI17**, retaining earlier full coverage: async failure/yielded ExitPlayMode, deterministic clock scopes/fixtures, sample paths/named scene coverage, state-duration setters and other legacy corrections were not weakened. Changed/new source bodies are SampleAssetValidation, SampleEvidence, ValidationHost main partial and new Discovery partial; Testing/request/receipt/meta update with them. The post-read gate at **2026-10-23T21:38:25.858426+00:00** matched **53/53**. Final handoff gate at **2026-10-23T21:42:03.568Z** matched **53/53 main plus 15/15 SDK/shared-context identities**. Later scoped edits require renewed review.

Read the complete new 447-line Discovery partial, complete current main ValidationHost and ten-case SampleEvidence fixture, actual generic native caller/helper bodies and changed hunks, complete Testing guide and new metadata. Prior NI17 is this same reviewer's full report at unchanged SHA `8a0523ddcca902eb3473a6fdb3358373d1f4c8aa2cf84c4ebd13b2b32f92a53e`; no unrelated old fixture/runtime re-audit is claimed. Current test definitions, preview support and installed UTF source were read for exact visibility/API/behavior context.

## Exact early operator handoff — UNEXECUTED

The actual method is public static void:

`Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.DiscoverCompiledTests()`

It was published to root early after source presence/host entry checks, then verified against installed SDK source. Exact later invocation:

```text
"<Unity.exe>" -batchmode -projectPath "<isolated-final-host>" -executeMethod Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.DiscoverCompiledTests -neoValidationCompany "NeoQA_20261023_<run>" -neoValidationProduct "NeoTools_<profile>" -neoValidationOutput "<host>/Artifacts/discovery.json" -logFile "<host>/Artifacts/discovery.log"
```

Omit `-quit`, `-runTests` and unsupported `-listTests`. This is a separate batch invocation outside Play Mode, after actual import/compilation, with no sample import pending. Existing ProbeEnvironment and ImportPackageSamples entrypoints remain. Import alone additionally requires `-neoValidationPackageRoot`; discovery records the actual registered package path/source/version rather than declaring it matches an unprovided frozen-root argument. The operator must compare that observation to its frozen-source ledger. Publication is neither execution nor permission to bypass the barrier.

## NI17-01 — generic native scene caller is corrected in source

`D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/SampleAssetValidationTests.cs:180` now obtains `t:Scene` GUIDs through the same identity-selected native helper at line 312 used by other native asset cases. It no longer enumerates all filesystem layouts. The existing selector admits each exact current manifest identity once, preferring imported over development, and reports missing identities explicitly. The generic case:

- requires nonempty native discovery;
- resolves each GUID and requires an actual SceneAsset load;
- opens its actual disposable preview scene, requires roots and recursively checks missing scripts;
- asserts the inspected count equals every discovered scene count and is positive.

Missing current native identity takes the existing Ignore/inconclusive branch. Broken admitted native loads/scripts remain failures. Hidden YAML packaging and named Open/Playable prerequisites remain separate and unchanged by hash. No layout rename/copy, fake scene, weakened script assertion or broad asset framework was added.

`D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsSampleEvidenceTests.cs` adds one real caller-path control: it calls that native helper, checks all identities selected, unique/nonempty actual GUIDs, selected-root membership/current layout, and native scene loading. The earlier nine identity/YAML/prerequisite controls remain, yielding **10 authored cases**, all UNEXECUTED. Those pure negative controls exercise missing/duplicate/stale/hidden/invalid inputs; the new positive native path does not pretend it ran when roots are unavailable. Preview ownership remains the existing addressed preview-scene scope; no ambient scene replacement/save occurs.

Testing.md now accurately includes the generic caller in its all-identity admission claim. **NI17-01 source closure supported.** Actual UPM import/provenance, scene deserialization, missing-script/type behavior and Ignore versus failure results still need the real isolated host.

## Actual UTF 1.6 API, trees and callback lifetime

Installed `Library/PackageCache/com.unity.test-framework@aebcb98460c6/package.json` declares **1.6.0**. Current public instance signature is exactly:

```csharp
void TestRunnerApi.RetrieveTestList(TestMode testMode, Action<ITestAdaptor> callback)
```

ITestRunnerApi is internal, but the concrete TestRunnerApi method used here is public. The current source does not place an Obsolete-as-error attribute on this method. TestMode.EditMode/PlayMode and all used ITestAdaptor fields exist. No guessed ExecutionSettings overload/static signature or nonexistent list-tests CLI is substituted.

The actual TestRunnerApi creates a TestListJob, whose Start subscribes to EditorApplication.update and whose callback supplies the real SDK adaptor tree. Factory/cache source builds parent links and test-assembly ancestry; the SDK clears its cache after script reload. The helper uses those callbacks, not file counts, attributes, saved test XML, synthetic adaptors or a test Execute call.

`D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.Discovery.cs:126` validates before scheduling. It retains one ScriptableObject API and one in-flight request, requests Edit then Play, and rejects callbacks belonging to an older state by reference identity. Tree traversal at line 345 counts actual nonsuite leaves, requires unique/nonempty IDs/full names, resolves actual TypeInfo assembly identity or explicitly labelled assembly-node-name fallback, and retains raw SDK assembly-node identity. It records raw SDK RunState/SkipReason, UniqueName, root reported count and separately computed per-mode/per-assembly leaf counts.

Both modes must have leaves; Neo.Editor.Tests must appear in Edit and Neo.Tests.Play in Play. Ignored/Explicit/Skipped/NotRunnable discovery leaves are not passing tests or effective runtime capability results. Runtime inherited/conditional skip behavior and actual test outcomes remain execution evidence, not inferred from raw discovery states. No source-presence/no-op oracle can satisfy these counters without an actual SDK tree.

The monotonic Editor-time deadline is checked before waiting, graph work and callback traversal. On failure/timeout or completion, the helper removes only its own update hook, clears its own SessionState, drops stale callbacks and destroys its own API object. Normal completion requires both successful tree captures. SDK job cancellation is not claimed; this is a batch-only action whose terminal path exits the process.

Domain reload stores only output/deadline/restart bookkeeping; graph/trees are nonserialized and both modes restart in the new domain. The original deadline survives. Revalidated host/output and state identity prevent combining old-domain callbacks with new data. Actual callback scheduling/reload/timeout/native object cleanup remain unexecuted.

## Native Editor graph and dependency receipt boundaries

CaptureCompiledEditorGraph at line 272 reads native `CompilationPipeline.GetAssemblies(AssembliesType.Editor)`, waits while compiling/updating, and records each assembly's actual name, flags, defines, script references, compiled reference paths and output DLL. Output must exist and its AssemblyName must match the native graph; path/length/UTC write time/SHA-256/full identity are recorded.

It separately records currently loaded assembly full names/locations/module IDs and registered package name/version/packageId/source/asset/resolved paths. Requested dependency versions are labelled separately from actual registered package identities. The registered NeoxiderTools package must be present at 11.0.0. Active build target and Unity/environment observations are native values, not profile names or assumed CLI symbols.

This is explicitly the **Editor graph plus current loaded test trees**. It does not trigger a compilation, prove fresh source-to-DLL correspondence, build a player, infer Mirror absence from a define or attest a frozen package/source SHA. Root must pair this receipt with its actual successful frozen-snapshot compilation/import/source/profile ledger and compare package path/version, target, actual defines/references and DLL/loaded identities. The source does not claim otherwise.

The existing Editor test assembly still has its broad hard references including Mirror/CMF; no reference was removed and no separate fake Mirror-free driver was created. A dependency-absent profile that cannot compile/load the existing helper must report that real outcome. It cannot reuse counts from a different profile or copy the helper into an artificial standalone assembly. Actual per-host SDK/API/compiled-graph availability remains unexecuted.

## Guards, truthful terminal results and mutation boundary

The new action reuses RequireOwnedHost: actual final-host path segment, strict task company/product arguments matching Application values, effective persistent path suffix and output confined inside that project. RequireDiscoveryHost additionally requires batch Editor outside Play and rejects pending sample import. Every update/callback/persist/terminal path rechecks the relevant host/output contract.

ReadDiscoveryEnvironment does not call the persistence probe or SaveProvider getter/reset. The discovery driver does not invoke test execution, load scenes, mutate PlayerSettings or touch save/preferences. It writes only its guarded receipt/own SessionState and calls native discovery/metadata APIs. Standard SDK test discovery remains an operation for the task-owned isolated host; this is not a promise to sandbox arbitrary test-build/data-source code.

Status remains discovering until both real trees and graph are captured. Null/invalid/empty trees, absent required primary assembly leaves, missing output DLL/identity, package mismatch, callback exception, timeout or output failure cannot produce a successful complete receipt. Finish emits structured complete/failed JSON to the log, writes the guarded output when possible and explicitly exits batch mode 0/1. Invalid host/output may safely leave no output file; failure is still logged with nonzero exit. Receipt-write failure changes status to failed rather than reporting the old complete result.

These branches were source-traced, not executed. Native compilation/discovery/cache/reload behavior, graph/loaded-DLL reconciliation, invalid-host/output/empty-required failure controls, nonempty profile-specific results and actual SDK exceptions still require operator evidence. No production API, serialized data, assembly split, new runtime dependency or general validation framework was introduced.

## Documentation, retained tests and disposition

Full latest request/receipt and Testing guide match the implemented API/CLI, graph scope, actual counts, status/exit semantics, sample identity correction and execution limits. The new partial's metadata was read individually; global GUID/link/text/package validation was not run.

The 43 exact NI17 C# matches preserve async GetResult fault/timeout cleanup with yielded ExitPlayMode, deterministic clock restoration, both legacy machine timing setters and other fixture/source assertions. They are not newly passing tests. The same broad assembly/optional-dependency, native lifecycle/audio/GUI/persistence and import capabilities remain root-owned verification debt.

**No further source correction requested within this bounded review.** Optional helper/support assembly splits remain proposals. After every writer finishes, root/operator must compile, invoke the exact helpers and retain complete nonempty per-profile discovery plus actual native/sample/async/clock/legacy fixture results. Early API publication and this report do not open execution.

## Main identity ledger

Main **53/53** captured identities matched post-read gate. Source coverage above distinguishes changed/new full reads from **43 unchanged owner C# sources** retained by NI17 fingerprints. Raw SHA-256 retains BOM/newlines; inventory equality is not compilation/discovery evidence.

| Full artifact path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Animations/TransformAnimatorTests.cs` | 14972 | `f8b0030739c10ce0048b0624edb88906f3983281037cc1706e2f47c4e9c65eb7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/AmChannelEditModeTests.cs` | 37011 | `99ce808027bbf6645c9134511649cae1702278a4ff6b29ea06b41ea408af9376` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/AmEditModeTests.cs` | 22215 | `1dd971be286936741af7ec4224c52dd2b008cf9707f8a9097edde082ffc9b792` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/AmEntryEditModeTests.cs` | 26390 | `9b1eb77ee55126a9978bcf784cb3236d0378263b4f9239ebf536e14ef29e6225` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Audio/RandomMusicControllerEditModeTests.cs` | 4058 | `f101829e97d5b68751f676e1d591ee8464830d851824835199f1e4c3aaf69702` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/BonusSystemTests.cs` | 4430 | `551d82244154c08944608da42a2c7c16937dda83bf72465c684e0ed65af6a433` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/CooldownRewardAutoClaimTests.cs` | 3466 | `a32baff5eadf78c788d967d9d8be29cc9427c64f2241ad4810d9617d0e94e75d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/SlotEconomyDefinitionTests.cs` | 7178 | `918f39b6a6baee8bb1f756cf3f28e84faded35f6dfb1602e73683e504ca73616` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoEditorTestsClockScope.cs` | 2356 | `9e5852c9a2c9ff63d94f6e6a78c88b50e0f30fb0eea1157a5ba27a77c497d4cb` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Cards/CardSystemTests.cs` | 28018 | `ee095b18a7eb07cd2b25c192380280de52465f889a3a839e2d9f0d5986b113b0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/SampleAssetValidationTests.cs` | 17294 | `7bbe9a97dbc75e0ba63c9691f1cf0bfa83766eab58c8c322ad9c846c7f1ef02a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/SampleSceneCoverageTests.cs` | 19718 | `a29767467faeb659d5d5697bc5d88b06b47d9c70d69b63004188f601b9379ec9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsIsolationTests.cs` | 29711 | `2f1344626b2c815ca385e4454a6456464902656d25e0ce79bbfa236009da3d86` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPaths.cs` | 7339 | `586593af17694e6bfe3cb922392139aab830602e57c6f05d501c629a0f1188eb` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsSampleEvidenceTests.cs` | 9592 | `50a2b0242b0e48123d6c5b2e20d3b73d72bbfcba3e80efc0d0166e483e879aa7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.cs` | 24412 | `1c8e5d91c76f6c88b9d06ae589eb651a8bd089d214c766f18d77c84e117dc207` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.Discovery.cs` | 22528 | `80aff970b750b597b63834f4bee5b9d492e84ac34887e7fecc8b1f59144cff78` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Extensions/ExtensionsTests.cs` | 32233 | `27b183641dbb6bdafcb9cf8549be3268f3503bbe0e8d2b644e69684adedd5c66` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Extensions/UnityExtensionsTests.cs` | 11906 | `5a1693313ec3ee0e0154fe289e0fcab1841307d990f0ecbd696a486ddb808aa6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/GridSystem/AuditFixesMatch3BoardServiceTests.cs` | 5350 | `52d9639aecf36a41f1b7d689b4435eba465d733429f88c98303463bbc0807c6c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/GridSystem/Match3BoardServiceCoverageTests.cs` | 1858 | `3ab7e250a76ac1b15024b3b7f1ac2ee9494caf46504af3b23fe2466937206533` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Level/SceneFlowControllerTests.cs` | 6315 | `09df5a629e24472eb2eafa2afe7485ee770877e7ae26aaf2d2c5af8f26c34c53` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Progression/ProgressionManagerTests.cs` | 28132 | `04829a82ccfb2a9389a5000c3114e113002b77e6c7539b9784ced661c1eee537` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/PropertyAttribute/InspectorControlIdStabilityTests.cs` | 8662 | `69ad3fb2554b4105a05f7a80693f0d5d282d0883192bd8335eacf848f42ce7f6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/PropertyAttribute/NeoInspectorAnimationTests.cs` | 12324 | `ab8d517b5ecdd31d3c406eff5b6444b517cbadca564a94b91114861b853e4b4e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/MoneyPersistenceEditModeTests.cs` | 6993 | `9e12573e187dcfb564b963a64cfd2ed16e6d4ceb6db46830d12a7e1e80f13220` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/ShopAffordabilityTests.cs` | 13204 | `3b2437b9360d657852af2181533acae6e4514c320e5013c225fffe00b003d2e5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/ShopPurchaseRollbackTests.cs` | 8203 | `dadb9d4f48c3b7eeae97b40683f75026aafb6feacf15cec25e5322e412b750cd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/ShopResolverTests.cs` | 2434 | `7fb2d8cf9d865efe32fddf8db9e7bc078af01e3d394501da2030557f2d38ccdd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/ShopVariantsPanelTests.cs` | 8560 | `3493f983e7ef8c3ed8c88198bb18ca25cdf5f854da8ceec2b9111c9dec7642e0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/StateMachine/StateDurationPredicateTests.cs` | 4661 | `87fea940630326d7efe1ac7b4d3ea9dc3deb54e320f13c4bd192c47a04829ad6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/CursorLockPlayerControlTests.cs` | 8428 | `4770cf38d762318b2a54de2a4b6cc273572e180ce0aeace012bb857142a6a718` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/TimerTests.cs` | 9371 | `ab6f4f664a355212196e5d176ae04fe59e306368f7b316033a25be23c74d9c98` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/NeoConditionThrottleTests.cs` | 4000 | `3563347dcf957cc65f1a7e6d3ccbf9109a22ea45d339a84fcb3a07669401bddd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/NeoConditionTests.cs` | 10035 | `a337c79a92d8a42d3f4668b109c45b7f6bb5a8d8fe89002c4c652fa3372a74b6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/AuditFixesTypewriterTests.cs` | 13370 | `71450b95a7a168b2920b371524aa08b9848a9a9b7bbe62b8e60e9b205ec20b89` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/AuditFixesSpawnerTests.cs` | 3801 | `ff83dc7d44e8a37bfa7eb7d0dc487c8b1a237ecef48e4bae49706f810e99b840` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Move/TransformMoveTests.cs` | 4967 | `c924b5eda451ff0e381a2d2dff3086a8f22349e1721076cc1eb74a8ced0c04b0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/InteractionQueryMathTests.cs` | 6452 | `210e83f506d641186521d540f3e8e59f912b3716e78adfb8e6c9eccc47beb079` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/MoverPrefabColliderOffsetTests.cs` | 4220 | `2f239ca34d4a55ebdb14bac313b31c610ba02fff06578213246d0dae31c21f10` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/SelectorTests.cs` | 25594 | `f41b801873281e10f3e3fd77c8d1cd078876a5bb802a181f180637c7407bfffa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Other/OtherToolsBehaviorTests.cs` | 5828 | `586f21ce787c131097334be5ee061d81b7850909efc7b5ab511b0b248ed4a92c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/FakeLeaderboardBehaviorTests.cs` | 4600 | `e57604896b744538cb4b4673e1ee20ab933564a544db8abf90f5232a2dbf56da` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/ToolsComponentsTests.cs` | 3645 | `a628de80a34c12e734f704800d106da705e7068dbad40e15dc6753b6bd93acf2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Random/ChanceManagerTests.cs` | 4801 | `8c1a652db7f008288c2014b71fcbc296849cb844b11d35a6403272a48843a8c0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/AuditFixesAnimationFlyTests.cs` | 3317 | `07c0f6f8e35c2f465d7d8e97e33f8cb8c90f4c729c06cb95e948daed32ba257d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/UIMeshRigEditorTests.cs` | 24576 | `ffc6f68f2ecc0248a35747045cbee5ad7da7f96493365a887883e1dbfd540d6e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.Discovery.cs.meta` | 251 | `1047595684b12b9918371fed826f610573a859f9a67b1570a23e84009c039cb4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Testing.md` | 14947 | `2a63d7106c6c0f5302812041aa4a8d82022a88295c39ca8c8623abda09896ef2` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Editor.Tests.implementation.md` | 22573 | `d69aef035dfc81cb9686437400bddcfffcff6d45c985ac8f495de8952c0c5832` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Editor.Tests.implementation-request.md` | 19376 | `f33f526f83dc2f78d9b89bb3c45e63e67135df856cd5c987ae7c33bc5766083a` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeImplementationReview-17.md` | 40593 | `8a0523ddcca902eb3473a6fdb3358373d1f4c8aa2cf84c4ebd13b2b32f92a53e` |
| `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b03-04.meta` | 233 | `842e0f7ee08fd1c12430afffc244465351851654bc25f3b0eb9f1f8f57f71327` |

## SDK/shared support context identities

Checkpoint **2026-10-23T21:38:24.345545+00:00**. Full SDK API/interface/job/provider/cache/factory/TestMode/package and relevant TestAdaptor/settings-parser members were read. Shared definitions and preview support establish current visibility/ownership context; they are not a fresh approval of unrelated runtime/tests or native compilation.

| Full context path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/package.json` | 9937 | `9a867972b8d4572c590f5fba2058666ec2d93383eba0881dd6401676fcc84a6e` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/UnityEditor.TestRunner/Api/TestRunnerApi.cs` | 10702 | `010c0755decca8de18c1356c1067b0e0ba8a2e1896524af18e1c82022b7089fd` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/UnityEditor.TestRunner/Api/ITestRunnerApi.cs` | 424 | `de3673c6dd21b4535bcc0b2467001527b2b03944ab8ae9693a108a49f620f857` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/UnityEditor.TestRunner/Api/ITestAdaptor.cs` | 4201 | `f57a2e4a505c08713aa3976b06c1a9c08961e54b9d008f1828a5907467fadfe4` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/UnityEditor.TestRunner/Api/TestAdaptor.cs` | 5558 | `effd6a9577bc3e5188881a1a9643d71d3a262ef2f7b628af7251301259f87ff0` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/UnityEditor.TestRunner/Api/TestAdaptorFactory.cs` | 4391 | `5b7de5873968d6e26d3423f03ffe0793b18ff2d612f60dee17b502b77fdb185a` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/UnityEditor.TestRunner/Api/TestMode.cs` | 432 | `b2fd19483daadb2539cced375a8e47334ec1fed967a0e5585d23607894fdb818` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/UnityEditor.TestRunner/TestRunner/Utils/TestListJob.cs` | 1120 | `c503202bc33cd2b90cd5ed94eb961d69a17e538cfebc4d24cc85820ad084982a` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/UnityEditor.TestRunner/TestRunner/Utils/EditorLoadedTestAssemblyProvider.cs` | 5173 | `283eb21e4b48339902c6bea53c9f6ecbda10e01c1b746cdb165517ca4543cb7a` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/UnityEditor.TestRunner/TestRunner/Utils/CachingTestListProvider.cs` | 1724 | `adacdb668bc5114b3ccb998bd2e03d9f62ce92ed7da63ebdcd4c540df64b88e1` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/UnityEditor.TestRunner/TestRunner/Utils/TestListCache.cs` | 1978 | `f3f205a3b0bcb62381fd15efc8a4e1b5640fe6a2676577f37a49dd57642b8855` |
| `D:/Git/NeoxiderTools/Library/PackageCache/com.unity.test-framework@aebcb98460c6/UnityEditor.TestRunner/CommandLineTest/SettingsBuilder.cs` | 7904 | `ef14a0d1ad44ce5d0ca842dacfc91b39bf24d1f3826d2a76ec92791aa26ef5fa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` | 2098 | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` | 1505 | `2518b0df939933514e55de55be5313226107a2382fe89d31b5361565aea50fe4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPreviewSceneScope.cs` | 788 | `5d201b58807f061d2d6076fb49550a891b41c12edae7147a19acd72b585b68cb` |
