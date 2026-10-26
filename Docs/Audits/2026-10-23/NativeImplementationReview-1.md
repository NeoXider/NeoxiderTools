# Native implementation review 1

Status: source review complete for all five requested SOURCE-FINAL scopes. Four have scoped source approval; Neo.Reactive requires NI1-01 before renewed source-final approval. Target `demo` / `11.0.0`. This is not execution acceptance or a global freeze.

Only this report is written. No product source, test, documentation, original receipt/plan, configuration, isolate or user asset is changed. No Unity/MCP, compiler, runner, validator/checker, service, installer, process management or child agent is used. All execution acceptance is **UNEXECUTED**; the global source gate is closed.

## Basis and scope control

Read the current IMPLEMENTATION-CONTRACT.md and all five implementation requests/receipts in full. Source review compares each assigned changed hunk and its complete owned new/changed regression source, plus current owned documentation, to the request. Public/serialized compatibility, meaningful assertions, ownership and truthful debt are reviewed. Original native A/B/C findings in the requests are historical input, not new execution evidence.

Root's direct task identifies these owners as SOURCE-FINAL. Their receipts agree, while the shared implementation-state.json still lists their owner rows as `source-running`; `globalReviewApproved=true`, `executionAllowed=false`. This is bookkeeping lag to reconcile by root, not permission to launch anything. No active unassigned owner's source/doc hunk is reviewed or edited here. Parent-entry backlinks and conditional runtime integrations remain exact external dependencies rather than speculative fixes in another owner's files.

## NI1-01 — P1: Reactive reintroduces a known unsupported NUnit attribute

Original owner: `neo-audit-1023-b05-02` (Neo.Reactive). Current source: `Assets/Neoxider/Tests/Edit/Reactive/Audit20261023NeoReactiveRegressionTests.cs:13`, fixture declaration `[NonParallelizable]`. Current implementation receipt explicitly records adding it for shared diagnostics state.

Trigger: compile this fixture with the Unity-provided NUnit API used by both installed verification Editors. Expected: the approved XML/test correction remains compilable without new test-framework dependencies. Actual source restores exactly the attribute rejected by BOTH preliminary Unity compilers and centrally removed from 31 fixtures earlier. The current run was not compiled, so this is a source-confirmed reintroduction of a previously observed compiler incompatibility, not a claimed fresh compiler result. Historical evidence: UnityVerification.md / preliminary compile-02-local-cache (6.6) and compile-01-local-cache (6.3), plus Local/Audits/assembly-2026-10-23/central-compile-corrections.json.

Smallest repair: same original owner removes this attribute and corrects its receipt; retain the meaningful warning-gate test and its finally restoration. Required serial global-state isolation belongs to the actual supported Unity runner/job configuration already planned, without a shim/package install or unsupported replacement attribute. Later final compile/discovery and nine-case execution remain UNEXECUTED.

## Scope dispositions and full read coverage

Neo.Reactive: full request/receipt, both current source diffs, complete nine-case regression source and both complete public guides read. `REACTIVE-XML-CONTRACT` itself is correctly closed: summary/guides now separate silent collection/count cleanup from gated/throttled destroyed-target dispatch warnings. Executable audit fixes/public signatures/serialized values remain preserved. The new test observes real count/value/callback/log behavior, removes its logging delegate and restores the prior diagnostics gate in finally. Existing immediate-exception coverage still depends on the isolated test configuration enabling errors. No Observe/retention API was silently implemented. Source approval is withheld only for NI1-01; GC/AOT/native persistent events and both Unity compilers are UNEXECUTED.

### Neo.Cards.Editor — scoped source approval

Original owner `neo-audit-1023-b02-06`. Full implementation request/receipt, the complete DeckConfigEditor changed-source diff, complete twelve-case regression source and complete current `Assets/Neoxider/Docs/Cards/Editor.md` were read. Relevant owned folder discovery/ApplyAutoFill context was inspected as well. No new action ID/runtime defect was assigned, and the retained bounded fixes match the request.

Custom mode binds/draws the real serialized entries without erasing standard values; first-wins slot bookkeeping covers special sprites and deduplicates repeated references; Assets containment uses an actual AssetDatabase folder; previews borrow Unity cache textures; validation sees applied edits. The fixture checks public sprite/custom-deck outputs and conflict reports, not just property existence; real Undo/Redo assertions and owned object/Undo cleanup are present. Private importer invocation is clearly labelled and does not create a public API. No runtime dependency, field rename, sample/config mutation or extra editor framework is introduced.

Documentation now covers replacement semantics, no-op cancellation/no-result paths, private importer boundaries, explicit scene component use and deliberate special-sprite conflict migration. Canonical inbound links remain the parent Cards owner's integration responsibility; their presence is not asserted while that owner is outside this review. The stale concatenated document-meta issue is not raised again.

**UNEXECUTED:** twelve fixture cases/discovery on both Unity versions, real inspector mode switching/current-edit validation, native recursive folder/sub-asset discovery, packed atlas and delayed preview rendering, dialogs and inspector Undo. Global preview-loading repaint cost is disclosed rather than falsely measured. The optional import planner remains a proposal. No additional defect found in this scope.

### Neo.Core.Level.Data — scoped source approval

Original owner `neo-audit-1023-b01-04`. Full request/receipt, all current Data source hunks, complete twenty-case regression source and complete `CurveEvaluation.md` were read. Current source/test hashes match the receipt and earlier reviewed runtime patch. The phase adds documentation, not a new evaluator/snapshot API, and preserves all public/serialized declarations.

Upward integer preview thresholds, double comparison of integer XP, subtraction before saturated conversion, floored authored terminal level/10,000 cap and absent-Custom lookup remain coherent. Tests use independent exact values at fractional/large thresholds and terminal boundaries, observe the public model as well as asset results, and destroy temporary assets. The guide correctly distinguishes mutable configuration, a model recalculation call, same-reference component assignment, direct/scene/NoCode usage and finite nondecreasing schedule preconditions. It explicitly documents the fractional-end behavior change and scan cost; no automatic asset rewrite or cache over mutable data is implied.

**UNEXECUTED:** both compilers and twenty discovered/run cases; final Core Formula/Data parity, linear Int32 endpoint, component/NoCode propagation and package navigation. Parent backlink belongs to the Core/coordinator owner. No unfinished Core implementation is read or approved by this scoped review; it remains a final integration dependency. No additional Data defect found.

### Neo.StateMachine.Editor — scoped source approval; conditional C-17 integration reserved

Original owner `neo-audit-1023-b06-03`. Full request/receipt, every changed hunk in all five editor files, complete fifteen-case fixture and complete `Editor.md` were read. All five editor-source hashes match the request's reviewed fingerprints; no runtime rewrite, speculative API, new field or assembly reference was introduced.

Type filtering, reacquired property paths, construction-before-insertion, temporary object cleanup, all-target validation, applied manual validation, single-owner condition editing, scoped serialized handles and owner/membership checks remain coherent. The strengthened constructor-failure test now flushes pending serialized edits, verifies unchanged original contents and then proves a valid independent append still works. Other tests observe concrete predicate evaluation/JSON/Undo, selected-row targeting, endpoint preservation and refusal after detach/destroy/close. Created editors/windows/assets and their Undo records are cleaned. Reflection bridges editor types/private UI commands; it is not claimed as native IMGUI execution.

The request assigns C-17 to the runtime owner, with editor work ONLY if a serialized compatibility choice is exposed. This patch references no unshipped runtime option. Do not mark runtime duration isolation/C-17 closed from this editor approval. Root must compare the final published runtime contract: a machine-addressed C# anchor alone needs no new inspector setting; a serialized timing option requires this SAME editor actor to expose its actual field/default and update its guide. Parent README inbound link is likewise an external integration item, not permission to overlap the runtime author's files.

**UNEXECUTED:** fifteen cases, both compilers, real TypeCache/IMGUI deferred removal, native asset save/reload, actual context authoring and final C-17 compatibility. Persistent transition IDs/repair remain proposals. No additional defect found within the five reviewed editor files.

### Tests-Edit-Gameplay — scoped source approval

Original owner `neo-audit-1023-extra-03`. Full request/receipt, both complete prefixed fixture sources and complete `Docs/Testing/Gameplay.md` were read. Review scope intentionally excludes the manifest's read-only legacy inventory: those files belong to the active primary test/runtime authors. No legacy Shop/Progression/Match3/Cards source hunk is inspected or repaired here.

Four controlled probability cases inspect exact HP, every receipt's source/target, final damage amount and conserved total damage under an explicitly reaction/shield/heal-free scenario; the IRandomSource is local and does not alter Unity RNG. Seven later-entry placement rejections inspect all affected content/occupancy/flags and zero notifications; both notify modes assert complete committed peer state and actual event counts; the dice case requires two clears, all positions, one merge and one board change. Inactive owned field roots avoid intentional singleton acquisition and are destroyed in teardown. No duplicate framework or extra production API was added.

The guide accurately separates compensated profile-store failure from postcommit Shop observer faults, records best-effort legacy grants, save/clock/global-state isolation and actual lifecycle debt. These are the approved contracts to verify later, not a claim that the currently unreviewed Shop/primary fixtures already implement them. It preserves release/developer/imported sample paths, exact supplemental filters and meaningful test guidance.

**UNEXECUTED:** fourteen discovered/run cases against final Abilities/Grid/Dice and both compilers. B-18 ambient Shop isolation, real store-failure injections and the older weak Progression/Match3/card lifecycle assertions stay with their named primary owners; this supplemental receipt correctly does not claim them closed. Native lifecycle, sample import, physics/UI and Shop fault behavior require their own final drivers. No additional defect found in this prefixed scope.

## Artifact identities and ownership boundary

Read/hashing checkpoint: 2026-10-23 18:29:33 UTC. `AGENTS.md` SHA256 `3a0c5c7a70719cba5d8d9c73b2ca162d7fb0ce96efd60ef926edbebc82bd9384`; current IMPLEMENTATION-CONTRACT.md SHA256 `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab`. Hashing is source identity bookkeeping, not a checker/test result. Root's explicit five-owner scope controls this review even though shared owner-state rows lag.

All full artifact paths below are relative to `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/`:

| Module | Fully read artifact | SHA256 |
|---|---|---|
| Cards.Editor | `Neo.Cards.Editor.implementation-request.md` | `e9d7e05e7d7132775d21170555460159a6b4ac56b6a3f76dce5ddacde1394de7` |
| Cards.Editor | `Neo.Cards.Editor.implementation.md` | `f70ba2ecf1ff62de726e8563e6adbab07e0d52284559b6152011ef45cf4d9f6b` |
| Core.Level.Data | `Neo.Core.Level.Data.implementation-request.md` | `65cb6b794287fb68e3df1d74a4f37c383cb94af8287a3be901f2c98953124806` |
| Core.Level.Data | `Neo.Core.Level.Data.implementation.md` | `aa2ac83fabaf741d2d34085da4129632d2d1bfeffcaf4d49adc052bc00f4cd49` |
| Reactive | `Neo.Reactive.implementation-request.md` | `dee892ae771ce0e107bddd8c53af5f435bc5e0447aa4bb732a0230b4af2fcd09` |
| Reactive | `Neo.Reactive.implementation.md` | `50933ca5cde499a56d1516daa92ff14f0002b9ab1fb41a71c2cf4c54b406d026` |
| StateMachine.Editor | `Neo.StateMachine.Editor.implementation-request.md` | `724e425a37d53a355de321edf6caeef953e3dfafb974269180c47a2e13bdf932` |
| StateMachine.Editor | `Neo.StateMachine.Editor.implementation.md` | `561fcbc4b12993fe9c4a154543965e11103b83c9c1a297411650472f8c611712` |
| Tests-Edit-Gameplay | `Tests-Edit-Gameplay.implementation-request.md` | `bcd9ec86f255ed063b6379e147b7b2752aceb03e61248cffe0a1f8ce6a702a58` |
| Tests-Edit-Gameplay | `Tests-Edit-Gameplay.implementation.md` | `f92462507c6b21b54bc2ec2fd8f3701e81d0e742fc3c0dc4f810c02d6a5720f1` |

Current owned source/test/doc hashes, with full paths relative to `D:/Git/NeoxiderTools/Assets/Neoxider/`:

| Read path | SHA256 |
|---|---|
| `Scripts/Cards/Editor/DeckConfigEditor.cs` | `07141f4885694d101de751b352c2245d8d5aa661c9571f3db3169b1fd07494f2` |
| `Tests/Edit/Cards/Audit20261023NeoCardsEditorTests.cs` | `9b6927c7b6800bb3790dd0c9b2fc31f3407088c0504bd11d267c83e11d93b1e4` |
| `Docs/Cards/Editor.md` | `79dcd9e366270dec8fb4b0019678edc14e0272ceffae2a1a7690c9cb0aa18077` |
| `Scripts/Core/Level/Data/LevelCurveDefinition.cs` | `7dc74c2fd574a81f173af1387537a79eea886c213af55f8b00705949027ab9e9` |
| `Scripts/Core/Level/Data/LevelCurveEntry.cs` | `a99d988916153baca18f0a2bc0d438ce5eade964a9fe58e5cf1148827b0c6b1d` |
| `Tests/Edit/Level/Audit20261023NeoCoreLevelDataTests.cs` | `6dfde6b543707b27ffe605370d0abbd9e8cfbff6e8ef893de0204824d6fe9877` |
| `Docs/Core/Level/Data/CurveEvaluation.md` | `cc603a74168c5ca0a950732407c5b364b0fb564974b237649ba6dc8233638578` |
| `Scripts/Reactive/ReactiveProperty.cs` | `14df81d1e13d76b658e20e8560dfa14376ce882c716a827da80f34e03256b2a4` |
| `Scripts/Reactive/ReactiveSubscription.cs` | `4701ef9ccd860b0cb140e18bfe74934e031fa8a658a275ce071292e7087972cf` |
| `Tests/Edit/Reactive/Audit20261023NeoReactiveRegressionTests.cs` | `f968c5919a7781395efb0d670c5c2972b897e3478c8f50afb68e6bec64c8456b` |
| `Docs/Reactive/README.md` | `fcf08dca451c1a5a74054798289737bb15c3a939fe6ccfc9a5b6b0d2803cfb3f` |
| `Docs/Reactive/ReactiveProperty.md` | `af42123dd98084e01ae8e09f51aca508d2a0aeab7992822dc2b23c9e355e361d` |
| `Scripts/StateMachine/NoCode/Editor/StateDataEditor.cs` | `c06cc83966d875151fbca10f4cc47f60ddcf6d7e5d78522272f7e91e5188a7b6` |
| `Scripts/StateMachine/NoCode/Editor/StateMachineBehaviourBaseEditor.cs` | `502656b4c794463bf673abe91e74c02d03108e6f78599fbef685b7c9cea9b719` |
| `Scripts/StateMachine/NoCode/Editor/StateMachineEditor.cs` | `af4d44160335c3a72fc9e26570088adc9e2b07cf0a9a8aecf0f22875c9e22d6e` |
| `Scripts/StateMachine/NoCode/Editor/StateMachineEditorRegistrar.cs` | `c209156d8d5f9908f824f57fe992ed0e09d5b33adeadb837745b0268a5c11933` |
| `Scripts/StateMachine/NoCode/Editor/TransitionEditorWindow.cs` | `5b35f6dd83bc7e5d76f38171e2581ce397f34e20485a297fe249c2e04c13f77f` |
| `Tests/Edit/StateMachine/Audit20261023NeoStateMachineEditorTests.cs` | `3746957b75b5455e81a9a02355674368439529c96c54265a5808f3bf030fd313` |
| `Docs/StateMachine/Editor.md` | `614112b238a12b3d6674f0c4ffe14c74583775f79a610db4827dda9933c7e70d` |
| `Tests/Edit/Abilities/Audit20261023TestGameplayProbabilityTests.cs` | `d21b8b08fc0f6e20e066ac196d29d28703cea92bd462f55dbdfe30e2f63727b5` |
| `Tests/Edit/GridSystem/Audit20261023TestGameplayPlacementTests.cs` | `8725b98b527d4f022074080d6ed05c9b680535befe19358e72aaa4409f1d6342` |
| `Docs/Testing/Gameplay.md` | `217670e4ecbbc40a2f0bb9c4453d1419fab1626799f1b77aa80459eaeb00fad7` |

Ownership/read boundary: one Cards editor source, two Data sources, two Reactive sources, five StateMachine editor sources, their existing uniquely prefixed regression files and exact owned guide paths only. The supplemental Gameplay scope contains no production write. Shared asmdefs, runtime parent sources, legacy fixtures and inbound navigation are not changed here. Receipts/public proposals remain explicit and introduce no cross-owner pending API requirement in these five patches.

## Final handoff

Return NI1-01 to the SAME Reactive author through root, then review its source/receipt correction without running anything. Four other scoped source approvals can stand while their final integration debt remains open. All 70 authored cases across these scopes, native editor/serialization/lifecycle claims, current package/dependency graph and both Unity versions remain **UNEXECUTED**. Root retains the global source freeze; this report authorizes no test/checker/Unity execution.
