# Native implementation review 16 — production Editor and Bonus

Status: **SOURCE REVIEW COMPLETE. Neo.Editor's assigned production phase is coherent in the reviewed scope; Bonus has one remaining accepted bounded correction NI16-01.** The Bonus trusted-clock consumer seam closes NI10-01 at source level. Every compiler/import/discovery/test/native/performance result remains **UNEXECUTED**. Only this review MD is written; no product/tests/public docs, original receipts/plans, metadata/configuration, isolates or other writer's source is edited. No Unity/MCP, compiler/runner/checker, service/install/process management, child agent or commit is used. The global execution gate remains closed until ALL writers finish and root explicitly freezes/resumes verification.

Branch/target: `demo` / `11.0.0`; contract base `ca198556`; local review date 2026-10-24. This is a working-tree source read, not final frozen-snapshot acceptance.

## Scope gate and complete read coverage

Read both complete final implementation receipts and approved requests. Neo.Editor receipt is SOURCE-FINAL for original `neo-audit-1023-b03-03`. Bonus receipt independently says SOURCE-FINAL; original actor is `neo-audit-1023-b02-04`. The cached root CLI liveness record read during this pass contains no Bonus actor, consistent with root's explicit last-liveness steering. Its file timestamp is `2026-10-23T20:23:10.509853Z`, SHA256 `9040b92eca9188f92a16f9d3d83dad25b5d3f4a4463fabc7da73ac237a5a166c`. This is recorded root evidence, not a fresh process probe or a claim of live polling. Source-final receipt plus that owner gate allowed Bonus review; no active unrelated owner scope was opened.

Editor: read every phase hunk in all seven owned production files, the complete lobby creator, addressed backup writer, occurrence/preview and all-interface filtering call paths, all 12 added cases in the five changed/new fixtures plus relevant helpers/cleanup, both new metadata files and all four owned documentation phase diffs. Existing older utility/missing-script fixes and optional maintenance frameworks are outside re-audit. Runtime Condition/NoCode and RequireInterface predicate are dependency context, not approval of concurrent runtime owners.

Bonus: read all phase hunks in the four changed runtime files and relevant full Box/Collection claim flow, exact trusted UTC override/helper consumer, all three complete changed regression fixtures (Collection 331, Reward 245, Play 614 lines), all seven new cases/strengthened controls and all four owned documentation deltas. Retained roulette/slots/wheel bodies are not broadly re-audited or newly accepted by reading old fixture scenarios. Current Network presenter API and NI7 publication correction were already independently rechecked in NI12; the creator consumes that exact source-final API.

## NI16-01 — P2: save-key reentry overwrites the latest Box reactive mirror

Owner: SAME `Neo.Bonus` / `neo-audit-1023-b02-04`. Current `Assets/Neoxider/Scripts/Bonus/Collection/Box.cs:53–62`, progress setter; public AddProgress/ChangeProgress at 84–100; the new settlement path invokes it at 160–164. Root explicitly accepted a bounded private write-revision correction. Source control-flow evidence only; no probe/test was executed.

Trigger: configure progress 20 and threshold 10. Attach a one-shot public SaveProvider.OnKeyChanged listener for this Box's progress key that calls `box.AddProgress(5f)` when TakePrize settles the first prize. The outer setter stores `_progress=10`, then SaveProvider.SetFloat exposes the callback. The nested command commits raw/saved/reactive 15. After it returns, the obsolete outer setter still executes `Progress.Value = value` with 10. Final raw `_progress` and saved progress are 15, while reactive Progress/ProgressValue are 10; later collection observers see inconsistent paid-progress mirrors. The prize threshold was consumed and the independent nested credit must remain; reconstructing a live before/after delta would misattribute it.

Smallest repair: capture a private monotonic write revision before the external save call, and suppress obsolete outer publication when a nested write supersedes it. A value-only check has an ABA loophole. Preserve the threshold cost, the independent nested credit, existing claim guard, manual APIs and event order; no storage/transaction framework is required. Keep ordinary setter/bar/text publication and nonreentrant take behavior.

Required same-owner controls: actual public key-event reentry with the one-shot handler removed in finally; current 20/threshold 10/nested+5 must leave raw/saved/reactive 15, one accepted item and collection observers seeing 15. Include ordinary no-reentry exact 10 state, a subsequent independent progress command, and a same-value/ABA revision control if the implementation would otherwise compare only values. Existing new throwing-save observer targets the ITEM key and checks exception settlement; it does not exercise a nested progress-key write.

## Editor action closure

`EDITOR-COMPONENT-INSTANCE`: shared resolver counts only matching component types and explicitly rejects negative/missing occurrences. Popup-row mapping counts duplicates independently of interleaved unrelated types; both Condition sides and float-binding/reactive preview paths consume the exact occurrence. UI indices remain separate serialized fields. Only explicit popup changes write a new selected occurrence; both Condition and NoCode popups retain the unresolved index rather than displaying a clamped valid choice and silently snapping. New/reset entries use zero, and old two-argument resolution still means occurrence zero.

The four new binding cases observe interleaved BoxCollider/SphereCollider instance selection and the second actual trigger preview, negative/missing failure, EditorJson occurrence roundtrip and independent legacy 91/92 indices, and a real runtime float binding read. The final removal check observes the EDITOR resolver rejecting the removed occurrence; it is not an additional runtime cache-invalidation execution. Condition/NoCode runtime topology contracts remain with their owners' fixtures/reviews. No public or serialized member is removed/reinterpreted.

`EDITOR-INTERFACE-CANDIDATES`: before writing a null FindInScene field, FindCompatibleSceneCandidate checks EVERY stacked requirement through the existing IsReferenceValid predicate. A Component itself must satisfy all interfaces; a GameObject may contain different components satisfying different requirements. Stable original search order/scope and explicit references remain. Assignment records Undo, then dirty; no match writes nothing and warns once per object/field until automatic success, with weak target ownership. Three new cases distinguish partial versus complete candidates, GameObject multi-component requirements and repeated no-match without assignment/dirty/repeated diagnostic. No general cache or resolver service was added.

`EDITOR-BACKUP-IDENTITY`: SaveSceneClone delegates to the same internal SaveSceneBackup(Scene,out string) addressed writer. GetBackupPath uses the source asset GUID directory and source basename, then SaveScene(...,saveAsCopy:true). It creates only the needed backup directory, without touching scheduler/settings or invoking global SaveAssets. Existing flat/renamed backups are retained, and a source must first have a valid scene GUID.

The new one-case driver saves two GUID-owned same-basename source scenes, makes distinct UNSAVED contents dirty, writes backups through production, opens the actual copies, then rewrites one and requires the other's bytes unchanged. Source bytes/paths/dirty state and active scene are checked; owned additive scenes/folders are released. This is a meaningful native writer driver once executed, not a mocked path-only assertion. Native SaveAsCopy behavior, active/dirty preservation and file/asset import remain unexecuted.

`EDITOR-TEST-GLOBAL-SAVE`: the collection resource fixture no longer calls AssetDatabase.SaveAssets. It creates/deletes only its GUID-owned fixture assets and retains substantive resource identity assertions. No alternate global save was added. `EDITOR-TEST-REMAINING-ISOLATION` owned seam is addressed HasRepaintLease(double), reusing production expiry without advancing shared clocks/list membership. Its new case retains a foreign live editor and exact membership while probing only the owned future expiry. Primary legacy fixture integration remains separate; NI10 changes are not self-approved here.

## Persistent lobby creator — source closure and native boundaries

CreateLobbyDemoScene retains the menu/public entry and calls the typed internal driver. Save/continue cancellation returns before destination selection; destination cancellation returns before folder, scene or prefab creation. The two cancellation cases retain unsaved sentinel handles/content/dirty state, active scene, scene/preview counts and empty owned output directory. Their injected decisions exercise the actual production branch, but do not prove a graphical modal-dialog interaction.

After approval, the driver creates a unique adjacent asset folder, real saved RoomPlayer prefab with NeoLobbyPlayer+NetworkIdentity and a distinct GamePlayer prefab with NetworkIdentity. Temporary prefab-source objects are destroyed in finally. Room/offline/online paths refer to the lobby; GameplayScene refers to the actual generated LobbyGame.unity. Its additive construction scene is closed in finally and the former active scene restored. No scene room object is assigned as a prefab and no package sample layout is mutated.

Host/Join/Ready/Leave buttons use persistent listeners to the scene-canvas NeoLobbyPresenter. Address and discovery feed SetAddress; discovery also updates the input without another notify. Status/count outputs persist through the existing SetText.Set(string) adapter. The gameplay scene has its own persistent Leave presenter and no cross-scene serialized manager reference; it resolves the surviving singleton at runtime. Presenter Manager/SetAddress/HostLobby/JoinLobby/ToggleReady/LeaveLobby and status/count events are available, including NI7's changed-only revision guard. No runtime/source API is pending for this authoring integration.

The saved/reopened case closes the generated scene, reopens actual assets, checks typed persistent prefab references, all four target/method identities, address callback invocation and real label output callbacks, then opens the saved gameplay scene and checks its Leave target. The case does NOT start a network session or certify RoomPlayer→GamePlayer replacement. KCP is assigned if its existing transport type is available; otherwise a warning asks for explicit setup. Native prefab network ids, Input System module compatibility, Build Profiles containing both scenes, actual two-peer Join/Ready/game transition/Leave and return-to-lobby remain real product acceptance. StandaloneInputModule is generated; the selected project's input backend still needs that compatibility check.

The 12 added Editor cases are 3 collection + 4 binding + 1 addressed lease + 1 backup + 3 MIRROR lobby. The receipt's 40-case seven-fixture inventory includes retained prior cases; no discovery/run is inferred. New fixture metadata exists. Four owned guides explain programmer/Inspector paths, occurrence defaults/explicit repair, all constraints/Undo, GUID backup migration and persistent lobby setup with Russian summaries. Old backup files/scenes/prefabs are not automatically rewritten.

Aggregate Editor cost remains scoped: the confirmed incompatible assignment loop is removed, but unmatched fields can still reflect/search on later draws; broad shared candidate caching remains a proposal with invalidation requirements. Backup writer addresses one scene; this pass does not claim batching of unrelated repair tools or an FPS/IO benchmark. Lobby authoring is explicit menu work, not a runtime per-frame service.

## Bonus source closure with NI16-01 remaining

`BONUS-CLOCK-BOUNDARY`: current CooldownReward:154–157 overrides protected DateTime GetUtcNow and returns NeoTrustedTime.UtcNow. Inherited TimerObject's three RealTime writes/two reads use that hook, matching eligibility. Plain TimerObject remains device-UTC; keys/formats and Seconds mode remain. This closes the concrete missing override source dependency described by NI10-01/NI8, without relabelling old receipts or declaring global clock behavior verified.

`BONUS-ADVANCE-PRECISION`: the legacy consumer now delegates private AdvanceLastClaimTime to the existing extension. It no longer multiplies claim count and cooldown in float before adding seconds. The bounded public 1.4f/three-claim test requires the independently specified 41,999,999 tick advancement already visible to every grant, batch 3 and no immediate replay. It exercises the actual persisted consumer without millions of events. Large-range saturation/helper arithmetic remains Extensions-owner acceptance, not rerun here.

`BONUS-BOX-COMMITTED-PRICE`: Collection selects/captures the actual prize before callbacks and uses its existing TryAddItem selection/unlock logic. Once the in-memory item is accepted, its existing save write runs; the internal settlement callback executes in finally before Collection UnityEvents. Thus a postacceptance save-key observer failure still attempts the Box price. No prize calls no settlement. Box's take guard spans selection/settlement/visuals/events and releases in finally, including no-prize/exception paths. New progress/threshold-before-collection notification order and unchanged public signatures/index keys are documented. NI16-01 remains in the progress setter's obsolete mirror publication.

This is a bounded commit/notification boundary, not an atomic transaction between independent provider keys or arbitrary observer rollback. The same-operation accepted price remains consumed when an observer throws; later notifications/animation may stop. Independent provider failures and external catalog/state changes retain their disclosed limits. No storage/journal or reward-session framework is required by the approved actions.

The seven added cases are meaningful: both OnItemAdded/OnGetItem faults inspect actual id 0, persisted unlock and zero raw/reactive/saved progress BEFORE throwing, retry cannot unlock id 1, then a new threshold succeeds; exhausted collection preserves progress across two calls and catalog extension is a guard-release positive control; an actual static item-key save observer throws after storage acceptance and is removed in finally; fixed trusted 2030 sequential claims reject immediate/+59-second takes and permit exact expiry; fractional legacy timestamp is exact; real disable/save/destroy/reopen/recreate restores the unchanged trusted deadline and later permits exactly the second grant. Existing reentry now starts with TWO thresholds, so the guard rather than insufficient funds prevents recursion. NI16-01's progress-key nested credit is a distinct missing control.

The three changed Bonus fixtures contain 11 Collection + 13 Reward + 16 Play = 40 authored cases, of which seven are new in this phase. Edit reward scope restores exact lazy clock/cache/provider state without invoking the mutating Clock getter first. Play scope captures timeScale, sets 1 for native progress, uses a manual UTC/monotonic clock and a GUID file root, destroys owned objects/assets before restoring three clock fields/provider/time, and deletes only its private temporary store. Existing restore/auto-claim cases use trusted deadlines and actual Start plus public Tick; frame countdowns allow elapsed frame tolerance while timestamps/counts remain exact. These are authored drivers, not native results.

No new Bonus asset/fixture/meta was added this phase; old metadata remains. Four owned public guides disclose the clock distinction, precision helper, Box event order and independent-key limitations. RewardSchedule/runtime-core extraction remains a proposal. No broader old roulette/slots/wheel functional or performance approval follows from this bounded pass.

## Remaining acceptance and handoff

Editor is scoped source-ready; root should return NI16-01 to the SAME Bonus actor, obtain its updated source/receipt and recheck that bounded delta. Do not weaken deterministic reward fixtures or default the clock/provider to mask the integration. Full Bonus acceptance remains open while NI16-01 is unresolved; the trusted override alone does not prove all reward behavior.

After ALL source writers finish and root freezes a snapshot, the original verification operator must obtain both installed Unity compiler branches, nonempty Edit/Play discovery, actual MIRROR dependencies/conditional counts, all owned cases and relevant primary reward/Editor slices. Native scene/save-reopen/GUID/Undo/GUI cancellation, current prefab ids/input module/transport, real peers and runtime transitions, exact timestamp behavior and thrown-observer conservation require actual execution. Skips/capabilities and primary/harness pending work remain separate. No test, compiler, checker or planned verification is requested/launched now.

## Source identity and read ledger

SHA256 entries identify this source-read snapshot, not validator/compiler/test receipts. Full implementation receipts/requests and new regression bodies were read; production/docs coverage is every phase hunk plus the call paths above. Dependency contexts and cached liveness are explicitly scoped. No unfinished owner's source is represented as approved.

| Reviewed artifact | SHA256 |
| --- | --- |
| `Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `Docs/Audits/2026-10-23/Neo.Editor.implementation-request.md` | `34d9c9305e2ae9fec1f79bf2936456df5f429d4c7b87dd11495e123d5d0306c0` |
| `Docs/Audits/2026-10-23/Neo.Editor.implementation.md` | `b9f73534b6771c8da01562ff95c3fa40344fc337885f1b4f5b924f120c31e756` |
| `Docs/Audits/2026-10-23/Neo.Bonus.implementation-request.md` | `5ab9525d83ba30ad6effc23e547872919c846f449f94d66c2dd554a09bd6c397` |
| `Docs/Audits/2026-10-23/Neo.Bonus.implementation.md` | `e41ba1b71beb412ca57213aac9ab3921afe45f3acba89f38ff0d646af29c5ff4` |
| `Assets/Neoxider/Editor/Binding/ComponentBindingInspectorShared.cs` | `3f1a109533c06acf265fc5062bf2154899ea49c6cfc9636795adc3b317ba894e` |
| `Assets/Neoxider/Editor/Condition/NeoConditionEditor.cs` | `6bed19f4d8a7afee14ad09c2ede3106af8e3b9948dbeac077ea9ba886fac9536` |
| `Assets/Neoxider/Editor/NoCode/ComponentFloatBindingDrawer.cs` | `ccccb735cfb6cde1b761e0a4f45cd2b377a7d4070caf4f545cd64a75e5eb5fd2` |
| `Assets/Neoxider/Editor/PropertyAttribute/ComponentDrawer.cs` | `612e2b22a76802b9f95d189f91204320f0bf7e4962d583d0ce0fdb5a05ba1813` |
| `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorBase.cs` | `197630949f6741f703239771c00e99f7dbc46bdf392929671a602eb56761f4c6` |
| `Assets/Neoxider/Editor/GUI/SceneSaverGUI.cs` | `1501ddf7ea9e1914eeba97533f966c52141d4d2ef27e214003ae8d9f79d536a4` |
| `Assets/Neoxider/Editor/Network/LobbyDemoSceneCreator.cs` | `94c8a2908f16e651ac2f29365e857237d9ebf3f7df8d94be2c17f53fe6cbb018` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorCollectionTests.cs` | `d7141f1d1f3ab55ed448005784782212bd289381ed7167ef3c9bbc77eb429e9e` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorBindingInspectorTests.cs` | `fa6c05f1732079d9d027df217b4b0f003e3e4f51d91af3c6950c82676ff1b6e9` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorInspectorTests.cs` | `b5c37e334071f39b32453811f71f1473b59d7117e73fcdc8c360b1467e326da9` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorSceneSaverTests.cs` | `e821b3d13eb184b53a9e97aa78cdfc9b5a06f6a95f615477420c91e184f9a01b` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorSceneSaverTests.cs.meta` | `69a33f6590c56b9f46fc2fbbae2261e6ff54903204a84f9e3373b281b8fee30f` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorLobbyTests.cs` | `dcfcebac71342e1aa6d2ed1ba5caa2cdb04251ea9ae3f5b42552fa3d6ccca49b` |
| `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorLobbyTests.cs.meta` | `98ec969a6930b821bd7d464584c93f84839d3c1db6d843444a78f8341b991663` |
| `Assets/Neoxider/Docs/Editor/README.md` | `ff13dc2500cae7cf52f7016e656cd7740ce9a28dc2de186f777d3adec44ff3ba` |
| `Assets/Neoxider/Docs/Editor/ComponentDrawer.md` | `d6944a62d5ea6854ea34f3065ebe70a0453418aaf36ff8b826b121b08db36022` |
| `Assets/Neoxider/Docs/Editor/SceneSaver.md` | `ab52347b6d2396520ae00c39e81bc427f738c5b0e9eb080f867ac2bcdca737ca` |
| `Assets/Neoxider/Docs/Editor/EditorWindows.md` | `24f5728786f817d76b331989362f2b0f5730b59e061a7318b6e3f09d08ccb91a` |
| `Assets/Neoxider/Scripts/Bonus/Collection/Box.cs` | `81b4e6765f934d38239ba24814cc32a7b4d63b2cb9dc5f354522d68a180949ff` |
| `Assets/Neoxider/Scripts/Bonus/Collection/Collection.cs` | `6e2d743101da4278cef7c311dcc97464253b9e786a9e87c7e6f2af03ce3ca8e3` |
| `Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs` | `0ff8635f120bfb92ab7f3262f8c802160485a8c0e53c9aa53dea33d28025e32a` |
| `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs` | `220b92794d82091794b218d3038b9f0002dd3f8413a4548d2c480d2b031b8897` |
| `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusCollectionTests.cs` | `82dd5367f2297a6a379ee5a72ba49cbdfbd1d96761b8869ee453b10436af6ab7` |
| `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusRewardTests.cs` | `b70b71c5b158e7439032ad5b315bbddad780ff38d8fe9603592853e29a1f25e3` |
| `Assets/Neoxider/Tests/Play/Bonus/Audit20261023NeoBonusLifecycleTests.cs` | `019d5c5676d4e180e2dcc1e2546ebb0ac18fcb42b2eb5c23d545f480f4c42439` |
| `Assets/Neoxider/Docs/Bonus/Collection/Box.md` | `c435c203abd562002f3214b4828ed7cdadb547e9c387008a304ada9ad9366c72` |
| `Assets/Neoxider/Docs/Bonus/Collection/README.md` | `a25befa6de02b5d00b06609f1d9e03125ea32c9b8e2455c2d7c247b6d49787ea` |
| `Assets/Neoxider/Docs/Bonus/TimeReward/CooldownReward.md` | `acf931d4181192162f4194de71f8f08425785c48c7efaeb28a942cd8e60f3329` |
| `Assets/Neoxider/Docs/Bonus/TimeReward/TimeReward.md` | `943b5164adf3b4741ac28353c382bee96025ef25988b641369945088e426ca37` |
| `Assets/Neoxider/Scripts/Network/Lobby/NeoLobbyPresenter.cs` | `d1d01fa0a49a7ccf414190ce9024720971477aaad95d4bd68b8b10bc13b06d7e` |
| `Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs` | `2131a3f3d8fc214490bd06b12b47e52c5bb3b60bd74257c3041301eab8840489` |
| `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoEditorTestsClockScope.cs` | `9e5852c9a2c9ff63d94f6e6a78c88b50e0f30fb0eea1157a5ba27a77c497d4cb` |
