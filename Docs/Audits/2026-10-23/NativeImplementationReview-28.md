# Native implementation review 28 — final Progression, Physics, Grid and addressed Html job save corrections

Status: **SOURCE REVIEW COMPLETE — Progression NI20-02, Physics emitter follow-up, Grid NI20/root duplicate correction and Html.Editor NI23-01 source closure supported. No further bounded defect found. All execution UNEXECUTED.**

Reviewer: SAME `/root/audit_abilities`, native `gpt-6.1-sol` / `max`, `D:/Git/NeoxiderTools`, target `demo` / `11.0.0`. This report is independent of those original owners. Only this Markdown is written. No Unity/MCP, compiler/build/test/checker/validator, install, service/process management, product/public-doc/config/other-report edit, child agent, commit or push. The global execution barrier remains closed.

## Admission and retained coverage

NI24 was completed and published to root first: `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeImplementationReview-24.md`, 26,302 bytes / 172 lines, SHA-256 `4d6b5f0e27964b6bc41fdeba7406e115e5c7a35eb4e5f4c4e496e20bfb10023c`. NI28 does not extend that helper's source approval into execution.

Root supplied exact final completion notices for Progression actor 17620 and Physics actor 2428. Independently read their complete final receipts first: Progression 109 lines / SHA `c5883021ae3250d850885c239089a040bdb0a8044dbea7beafe4a3580be3724c`; Physics 75 lines / SHA `4294ec6f0c91f4dca23c85689848c64dd0a9461d082b5aa22a34b9fc02cba235`. Both match the assigned hashes and say source-final. Exact original owner metadata `neo-audit-1023-b04-07.meta` and `neo-audit-1023-b08-01.meta` read done/0; no process was queried. Complete approved requests, RootPhysicsEmitterReview and relevant implementation/native contracts were read.

The initial ready-scope gate at **2026-10-23T21:43:09.010Z** captures **36 paths** below. Twelve of thirteen Progression runtime C# files match the same reviewer's NI20 individual identities, as do the module definition, original audit/plan and reward-track guide. Their existing coverage is retained; only the manager, owned fixture, two guides, request/receipt and owner metadata need the current correction read. Full prior NI20 remains unchanged at SHA `21bd0232d99a522f5084afdd2f4c784f4cd2c64452076644043729b3552d699e`. Physics prior full-module review is this reviewer's NativeReview-C; the current narrow continuation and new controls were examined rather than treating an earlier unexecuted receipt as a pass.

Grid and Html.Editor were initially held, then admitted only after root's exact final notices for Grid64368 and Html92836, fresh final receipts and exact original metadata reads. The old Grid `1211ac27…` receipt predates the latest duplicate fix and is not used to close it. Later admission and unchanged-source fingerprints are recorded in their sections below.

## Progression NI20-02 / P1 — correction supported in source

Original owner: `neo-audit-1023-b04-07`. Prior failure was a readiness latch before a throwing dependency/bind/read, allowing corrected public retry to skip saved points 7 and autosave 1. Current `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Components/ProgressionManager.cs:245` separates completed readiness from an in-progress attempt: completed ensures return; unfinished reentrant public ensures throw before profile use. Init at line 680 sets the in-progress guard, captures the attempt's binding generation, binds and reads/applies the profile, and always clears the guard in finally.

ApplyProfile at line 751 applies/sanitizes the profile, default/imported sets, active key and incoming reward watermark before **base.Init and private readiness commit at lines 769–770**, then publishes external runtime state. The base Singleton.Init only marks base readiness and invokes no observer. Consequently a precommit read/bind/dependency fault remains retryable; a postcommit state/profile-loaded observer fault propagates without rewinding initialization, rewards or the active watermark.

BindLevelProvider at line 1005 captures its generation before provider.EnsureInitialized, checks current generation/provider before publication, owns the exact reward delegate plus three existing non-immediate Reactive.Subscribe handles, and cleans only its current failed generation. Init's catch also releases only its still-current unfinished attempt. A callback-created newer binding survives an older failure. Mirror and reward delegates require committed readiness; provider replacement during setup refreshes silently. Unbind increments generation and disposes only owned handles. Current Core EnsureInitialized/read/getter and Reactive Subscribe/default/handle behavior were read, including the earlier Core read-failure retry path.

No general rollback/session framework, public/serialized field change, save-schema change or Save provider relaxation was added. Normal same-key/reset/new-key watermark rules and A22 notification/reward/save boundaries remain. The former required failure is **closed in this source**, with no additional bounded defect found in the reviewed delta.

### Progression authored acceptance read — 9 new methods / 13 scenarios, UNEXECUTED

Read every new body at `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Progression/Audit20261023NeoProgressionRegressionTests.cs:724` through 1090, complete new provider fault/read/callback controls and shared fixture support/cleanup. The current whole fixture declares **39 methods / 50 cases**, retaining the earlier 37.

| Cases | Public source oracle |
| ---: | --- |
| 3 | One-shot Progression GetString failure with no Core, assigned Core, or corrected Save provider: no first save/loaded event; corrected retry reads7, repeated ensure adds no read/binding/event, +1 saves V2 points 8; detached provider remains unchanged. |
| 1 | Earlier Core profile read failure: no manager subscriptions/Progression read survive; retry restores Core XP 25/remaining 75 and Progression 7, then saves 8. |
| 1 | Partial binding fault after XP handle: the three external handles/reward listener survive; manager-owned partial handles are removed; retry binds once and next level grants 3 exactly once. |
| 1 | Initial read reentry: nested ensure/+1/save throw without recursive reads/default writes; outer load settles7 and next +1 stores 8. |
| 1 | Initial read swaps provider then fails: newer binding survives, precommit updates publish/save nothing; retry uses that provider once; old source remains detached. |
| 2 | State or profile-loaded observer throws after committed reward/downgrade: points 12, existing three saves, no reread/rebind/replay; recovery retains watermark and only command save. |
| 1 | Postcommit loaded observer swaps then throws: saved 7 and newer binding survive, only new provider notifies, +1 saves 8. |
| 1 | Older provider setup observer creates newer binding then throws: no stale cleanup removes newer handles or overwrites XP 60; new +10 publishes70 without save. |
| 2 | Ordinary loaded/fresh initialization is idempotent, binds once, preserves expected read/loaded counts and saves 8/1 only after +1. |

Tests use public commands/events/balances/listener counts and real owned memory-provider payloads. Reflection configures serialized flags/dependency faults, not the outcome. Setup captures exact prior Save provider/singleton/search/create state; finally restores it and destroys only owned inactive objects. No unavailable NUnit serial attribute was added. Exclusive isolated execution is still required. README and full manager guide correctly disclose retry, precommit rejection, allowed setup provider configuration, postcommit observer failure and non-atomic delivery. Real Resources-encryption correction/Awake/native disposal remains later integration debt; memory-provider tests do not claim it ran.

## Physics root emitter continuation — correction supported in source

Original owner: `neo-audit-1023-b08-01`. Complete RootPhysicsEmitterReview and approved request distinguish the confirmed ExplosiveForce native-object tail from the **unproven exact destroyed-Behaviour getter outcome** in MagneticField. This report preserves that distinction.

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/ExplosiveForce.cs:163` rejects Unity-null at explicit entry. Immediately after OnObjectAffected (line 232) and OnExplode (line 239), Unity-null fences return before the next loop's emitter gameObject access or old automatic cleanup. Force already submitted remains committed; no later target/completion is run after affected-listener destruction. Component-only completion destruction leaves its GameObject alive. Live disabled manual calls remain accepted; activation/repeat guards and deferred Destroy meaning are unchanged.

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/MagneticField.cs:353` checks Unity-null before isActiveAndEnabled at field entry and after entry listeners (line 400). Both Update/FixedUpdate mode-listener continuations flow through that entry fence. Existing OnDisable membership/cache/exit cleanup remains untouched. No claim is made that the old getter was observed to throw or that merely queued Destroy is already Unity-null.

The earlier C01 ImpulseZone post-enter fence at line 217 and its in-progress finally remain; explicit disabled calls keep their contract. C02 entry now distinguishes COM falloff/directional AddForce/magnet math from native AddExplosionForce closest-surface behavior. Read exact changed hunks, full explosion body, magnetic callback/force/cache/lifecycle context, impulse continuation and all four guides. No new serialized/public signature, force-mode/mask/default, scheduling or broad lifecycle redesign was introduced. **No additional bounded source defect found.**

### Physics authored acceptance read — 15 new cases plus retained C01 controls, UNEXECUTED

Read every new fixture body and owned hierarchy/force helpers in `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsPhysicsRegressionTests.cs`. Fixture declares 38 cases: previous 23 retained,15 new.

| Cases | Public/native-force source oracle |
| ---: | --- |
| 2 | Native AddExplosionForce affected callback destroys component/GameObject with two independent bodies: exactly one affected, no completion; the observed target retains its10 impulse and the other remains0, without assuming query order. |
| 4 | Completion callback destroys component/GameObject with cleanup delay0/0.25: affected/affected/complete order, both opposite10 impulses; old automatic cleanup cancelled and component-only object's GO remains. |
| 2 | Live active/disabled explicit explosion: both bodies pushed before exactly one completion, HasExploded true. |
| 2 | Magnet enter callback destroys component/GameObject: one entry and zero force on both independent bodies, no old target tail. |
| 4 | Actual mode callback via selected Update/FixedUpdate destroys component/GameObject: one phase callback, zero entries/forces. Serialized zero duration is deliberately callback-logic injection, not normal cadence proof. |
| 1 | Ordinary two-pass magnet positive: two entries remain two, membership 2, forces 10 then20. |

The earlier component/GameObject impulse-enter destruction cases and live-disabled manual12 impulse positive were also read. New tests call public explosion/events and actual DestroyImmediate; magnet/trigger SendMessage is explicit callback-logic driving, not native scheduling evidence. Real GetAccumulatedForce and configured fixedDeltaTime impulse observations are substantive oracles, not no-throw only. The remote owned hierarchy isolates colliders, layer 31 filters queries and teardown destroys the root; no global simulation settings are changed. No NonParallelizable attribute is added.

Native force/scheduling/contact outcomes, queued destruction, live-emitter delayed automatic cleanup and valid positive-duration toggles remain unexecuted. The previous full-body/native-version/compiler evidence is not supplied by these authored controls. Per-field queries, fixed-capacity omission and cadence/shared-discovery proposals remain explicitly documented limits/proposals; no FPS/completeness claim follows from these guards.

## Grid NI20-01 and root published-blocker-removal follow-up — source closure supported

Original owner: `neo-audit-1023-b03-05`. After root confirmed writer 64368 exited, read the complete **97-line** current receipt at SHA `76901694cfb14449273bc71138734298e61a7c347b7a33950d9d7f610b1c2500`, complete updated request and exact owner metadata done/0. Gate **2026-10-23T21:46:27.911Z** captures the 44 Grid paths and 34 NI23 Editor comparison paths plus 3 prior-report/owner metadata paths below. Of Grid's30 runtime C# files, **29 exactly match NI20**; boundary 39, original regression, module definition, other three implementation guides and original artifacts also retain exact fingerprints. Only FieldObjectSpawner, the lifecycle fixture, its guide and request/receipt changed.

Read the full current `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldObjectSpawner.cs`, complete 614-line lifecycle fixture, full spawner guide, current field SetOccupied callback and relevant placement admission. At line 114, wasOccupiedAtEntry is captured **before Instantiate**; at119, notifyExistingOccupancy is captured **before this registration**, using entry occupancy and absence of a prior registered blocker. After the original registration/OnObjectSpawned order, line 129 still requires a current live cell/registration and blocking metadata. Publication now needs a free cell, or the retained initially-occupied legacy eligibility without another blocker.

The original initially-free nested A/B failure is corrected: nested B can publish despite in-flight A; returning A sees the already occupied cell and does not duplicate. The root's later valid nested publication/removal reproduction is also corrected: A starts free, B publishes, A's callback removes B while registered A retains occupancy, and returning A has neither a free cell nor legacy eligibility. Ordinary sequential B cannot gain legacy first-registration eligibility by removing already-published A inside B's callback. Entry/current-state locals suffice; no new ownership registry or framework is required.

The legacy initially-occupied first registration still calls SetOccupied/occupied notification even though the flag was already true, deliberately preserving the earlier observable policy. A second sequential blocker does not duplicate. Registered/current-cell checks, object registration/removal event order, deferred destruction and post-field-callback identity/state checks remain. Removing detached old-generation metadata cannot affect the new board. External occupancy writers, public metadata mutation, pooling and arbitrary callback rollback remain explicitly outside this bounded contract.

### Grid acceptance — all 9 added lifecycle cases read, UNEXECUTED

Read all nine new public UnityTest entrypoints at lines 228–279, both complete shared verification enumerators, placement helper, owned creation and yielded cleanup; also reread all seven original lifecycle bodies in the changed file. Cases assert actual current FieldCell flag/identity, both registries/query, rejected RequireUnoccupied CanPlace **and Place** without content writes, exact state/spawn/return/remove/occupied/freed order, and real deferred destruction after a frame.

- Two nested blocking-pair cases cover each removal order: registry entries are visible while initial callbacks still see the free flag, then one occupied publication and one last free.
- Two ordinary sequential-pair controls preserve second-blocker admission, first/last occupancy and both removal orders.
- Two mixed nested blocker/decoration controls preserve publication ownership and allow unoccupied placement after the blocker is gone while decoration remains.
- NestedPublishedBlockerRemoval and SequentialPublishedBlockerRemoval observe one published occupation, retired-body deferred death, a live remaining registration across a frame and exactly one last-free transition.
- InitiallyOccupiedCell preserves the explicitly documented first-registration legacy notification and second-blocker admission, with no duplicate and one last free.

The current lifecycle fixture declares **16 cases**, plus the unchanged39 boundary cases = **55 implementation cases**; the original audit fixture remains separate. No source-count is discovery or execution. The guide accurately explains the nested event arrival inside an outer callback, published-blocker removal, legacy initially-occupied exception, generation/lifetime limits and C#/scene use. Root's existing shared Play Neo.GridSystem reference is present; no stale missing-reference request is repeated. **NI20-01 and the later root duplicate correction are source-closed; no additional bounded defect found.**

## HtmlToUitk.Editor NI23-01 / P2 — addressed job save source closure supported

Original owner: `neo-audit-1023-b03-07`. Admission followed root's final writer 92836 exit notice, complete **113-line** receipt at SHA `4e59152e96faa68d320d81895b328f4f209ce85ea4d883b7cd43fc277f3771cd`, updated complete request, exact metadata done/0 and the same later gate. Read the complete prior121-line NI23 report, complete current replacement fixture/guide and real save/replacement/native SaveChanges/regular-job serialization context.

The runtime delta is exactly `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs:529`: `AssetDatabase.SaveAssetIfDirty(_jobAsset)` replaces the global SaveAssets after only that job is changed/marked dirty. Readonly reversal of just that one line reproduces NI23's exact whole-file SHA `93f947e7bf8eb021373320304e6ba63dd193ee3a0d8e42df437a5a41d65fed92`. This establishes the precise correction boundary without reapproving unrelated process/watch/runtime modules.

TrySave still prepares/copies document/config and writes JSON, then updates/saves only a regular job asset, imports the addressed JSON if in Assets, and only then publishes saved document/clears unsaved state. Imported JSON assets retain their source-of-truth branch. SaveChanges still throws before base success on failure. Shared replacement Save choice retains candidate-before-decision, Cancel/Discard, failure recovery and same-path reparse. Public/serialized/API/menu/schema compatibility and disclosed partial-write/nontransactional limits remain.

Of 34 NI23 Editor/context comparison paths, **29 are exact matches**. Thirteen relevant Editor C# bodies retain12 whole-file hashes; the window differs only as above. Other unchanged pipeline/watcher/provenance/input/process/probe sources and metadata retain the prior native review rather than being given new unrelated approval. Runtime C36 and other owners are outside this correction review.

### Html acceptance — all 3 new persisted-asset cases plus retained 7 read, UNEXECUTED

At `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorWindowReplacementTests.cs:144`, the three cases exercise ordinary SaveChanges, SaveChanges with dirty foreign config, and shared replacement Save choice with dirty foreign config. Each creates actual GUID-owned persistent regular HtmlToUitkJob and foreign HtmlToUitkConfig assets, uses **addressed baseline saves**, captures actual owned/foreign serialized file bytes and JSON bytes, then edits the job. Foreign-isolation cases mark an independent changed config dirty before invoking the real command.

The oracle requires changed owned asset/JSON bytes, edited parsed JSON, synchronous own-asset reimport and persisted jobName, while foreign disk bytes stay identical and its unsaved value/dirty flag survive. The replacement case observes the real save decision count and retained candidate content. This would catch the former global SaveAssets branch; it is not an in-memory-only or mocked-save test. The normal no-dirty positive keeps foreign baseline clean.

The complete fixture now declares **10 cases**, retaining all seven Save/Cancel/Discard/invalid/failure/recovery cases. The Editor authored inventory is correspondingly 46+3 = **49**, still UNEXECUTED. Test cleanup clears only owned Undo, deletes only its unique asset folder, destroys nonpersistent remnants/window, restores exact prior LastJobPath and removes its unique temp files. No global SaveAssets setup or unavailable NUnit serial attribute was added. Native SaveAssets/SaveAssetIfDirty serialization, synchronous reimport and native close-dialog behavior still require actual Unity evidence. Full guide accurately documents addressed persistence for ordinary/shared/native save paths. **NI23-01 is source-closed; no additional bounded defect found.**

## Disposition, costs and remaining evidence

All four assigned correction groups are supported in the reviewed source. No further source correction is requested by NI28. Existing public APIs/serialized fields, save profiles, native force modes/masks, Grid manual spawn/event compatibility and Html imported-versus-regular job policy are retained or explicitly documented. No optional session, transaction, lease, catalog, shared physics backend, reward ledger or process framework is silently made required or claimed complete.

No new per-frame search/query/catch-up loop was introduced by these corrections. Progression adds only attempt/generation checks on existing setup/observer paths; Physics adds local liveness branches without changing its per-field query work or fixed-buffer limits; Grid adds per-call locals and retains registry scans on explicit spawn/remove; Html narrows the save target. This is a structural source observation, not measured allocation/FPS/native behavior.

The post-read gate at **2026-10-23T21:48:50.542Z** matched **117/117 scoped identities plus 10/10 provider/shared-context identities**, including all four exact owner metadata records. Final handoff gate at **2026-10-23T21:51:01.172Z** again matched **117/117 main and 10/10 context identities**. No scoped artifact/owner metadata changed; later scoped edits require renewed review. Other readonly QA workers do not authorize runtime execution.

After **every** source writer finishes and root explicitly freezes, the same operator must compile both installed Unity 6.3/6.6 hosts, obtain actual nonempty profile-specific Edit/Play discovery and execute the updated fixtures plus retained legacy suites. Required native debt includes Progression Resources-encryption corrected-dependency/Awake/disposal and postcommit/reentrant observers; Physics true scheduling/contacts/deferred destruction/live delayed cleanup/valid cadence; Grid native callback/deferred destruction/placement lifecycle; Html actual persistent-asset save/isolation/reimport and native Save/Cancel/Discard/close failure. Shared definitions, optional vendors/MIRROR profiles, supported-version branches, package/GUID/UTF8 checks and external tool receipts remain root-owned. No historical test, compile, count, read hash or authored test presence is relabelled as a current pass.

## Later-scope raw identities

Capture **2026-10-23T21:46:27.911Z**. Full paths retain raw bytes/newlines; unchanged rows preserve earlier NI20/NI23 source coverage as described.

| Full path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.GridSystem.implementation.md` | 15917 | `76901694cfb14449273bc71138734298e61a7c347b7a33950d9d7f610b1c2500` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.GridSystem.implementation-request.md` | 8374 | `bdac5157bcb35a24c26a07a6c292da83aa8299e7fdfa57892f40822f9d804831` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldCell.cs` | 2979 | `20576842a7f3de0613477c5c5cb8e8f9bf1c6178b0e29a0dabe4e2a2da125ee9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldDebugDrawer.cs` | 4452 | `76424ee1aa8729df7f93b981733af3d3d1f53d00454fe02aeea87ebc6b1c858b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldGenerator.cs` | 40927 | `af277d868af63771321ae96db12a209aaed1c9ac09635a01801eb00b91c4d609` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldGeneratorConfig.cs` | 3159 | `3a564b4729b86b7db594164981adbfba8e2635934eda80b286827d2853b328b5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldGizmoSettings.cs` | 892 | `26478280d0ee80f75df04672e0e7e48603a6adbd1d2e458ceb7b56bfc5c4f958` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldObjectSpawner.cs` | 9596 | `9dae40be0fcfe4c9da70bfc5bddb0eb3c0107aa2c3cb983ac7a181d49320e608` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/FieldSpawner.cs` | 2850 | `d147065cc03856dbbfa96dea6093a5759612e431cb882279382cef832a34a161` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridCellMarker.cs` | 1168 | `5a813c73e08d666af840a8f4ed04fdaaf9a8895d64ecee88594c79b86e60c3ae` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridGameBuilder.cs` | 2474 | `05e2e2b5f340f8decdfbc83aa6139a4b98f7cbdc2e0d273d9585d02548df273d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridPathfinder.cs` | 5851 | `e3e057a7a1051fe4196509e80c37df3d0c34c8e76ae75cf24b6399721dcd4259` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridPlacementEntry.cs` | 531 | `9612bb16880c734c1f39593d21d074829885da9b3cc612d7e5fb3d4ce9688e7e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridPlacementResult.cs` | 407 | `6f099a51c66e37825e77867be5b6d720d72bfd310ab5fe595ed46569376a98db` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridPlacementService.cs` | 8113 | `3a9366505499dc32aed169523f2f084c4336ec2758bf7febd95e9c5ca96df957` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridShapeMask.cs` | 2275 | `ce9c1cf6f2625a617d2533bf25c58470d46a445f174480d92dc0a383a129ddae` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/GridSlotAllocator.cs` | 7735 | `b43dc21c71a7c167eca82162b388b77651b45e267cc7d111f75573abdf6a1fe2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Match3/Match3BoardService.cs` | 24425 | `c107c61b82c0869a1f2e1848afd035e7a2d6a72f9ed983451108738aabf1b370` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Match3/Match3MatchFinder.cs` | 3015 | `5e9a7083e3a98f1bfa5b246e172f0f6481a658cfc72cd369bd615cceb562908f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Match3/Match3TileState.cs` | 306 | `a7aa9c151fdd4f68cb30711120dff665bf733910d92f3efa3c2849ead8642f58` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeGroupResult.cs` | 557 | `5809bfdb1b747e556a98b7eba3235dba3b0f9c2c932ea754e1173445314c71d5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeRequest.cs` | 3753 | `965772b96dc8a3278269b877cc640cf88183a74a9a156cfc6c50501e10107fb6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeResolver.cs` | 5699 | `8aa75b9e497279aa6d2520227d78bb9449fa5dd4c9456a38d61099dd8896481f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Merge/GridMergeResult.cs` | 730 | `bbb3b9f0d54d80e1ac1b3adfc5717028e65de089d0bb8133d7992f126ec3b304` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/MovementRule.cs` | 4514 | `9f6ef2432b1c2a51ae97ce541268dc21c97c72b346a9a3f10186a80b031897d8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeBoardService.cs` | 4491 | `62ebc849562bfc5a3479548ca2e7002515c372036250dfaa469a63d28061d6d8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeDirection.cs` | 1116 | `ff593bbaf7197472d6e3f03a8a2de722e7eb20e4d8e058723df6efe40c69d45c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeResolver.cs` | 9681 | `08cd07ae8f6ba186dd1f000bc0e6a10101c7e8d0e850cbb23ba66544edaa92e1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/SlidingMerge/SlidingMergeResult.cs` | 606 | `acc1f0662e54acfb38f75697fc0e732e78a1b20bb3fe415c2a53048da6417fe4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeBoardService.cs` | 6053 | `e531e9f0b9ffdf9f93ecd8ca350436d62f0c964c265f069f4bbaf97a5cc22043` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeCellState.cs` | 253 | `e4862c510f714eb63c854faadd0703c7c93dae7288f1f00d4fb957e80aee0c6f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeWinChecker.cs` | 4496 | `e18de91124f8f05aceafb973b94d9f834f3f03a693d14b4e0d82325962978884` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/GridSystem/Neo.GridSystem.asmdef` | 141 | `b7999cbbc27ba7e3d2153b6d0fc8ae01182fde8bd91313c3f675ee0a8754b313` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.GridSystem.md` | 27509 | `4835bb77089394439008f322359b1bc8ccc6284a5ec85968e8665bdc612a580b` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.GridSystem.plan.md` | 8817 | `c6b3241cce1fcb4977f441da450a2efde8b7ed218b5e666165d9e80b7f592bcd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/GridSystem/Audit20261023NeoGridSystemBoundaryTests.cs` | 14430 | `487495c8db0c549941f7c140108a5d48348f6ca8fdddfe43e20c87b53fcb7262` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/GridSystem/Audit20261023NeoGridSystemBoundaryTests.cs.meta` | 60 | `a5ab82f460a44aa9f0bb97275fa3c0e599da93c5c569e0f96d8eac3db7ad5bc6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoGridSystemLifecycleTests.cs` | 27883 | `98ac46a3818545afd312adfac497185c6878221a538d5f27c93a41ac397ad846` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoGridSystemLifecycleTests.cs.meta` | 60 | `e93904316d90b5421aaa8fca8fb6e342d0f8950517af54c10dd74ab1fde63aa7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/GridSystem/Audit20261023NeoGridSystemRegressionTests.cs` | 23693 | `9e3a5d3c7a69f98e44eef0e1e4a1b5d0729e629bf3bcb6910847b0e8186d69a5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/GridSystem/README.md` | 5853 | `8e38b2273bfec7cca061ab8671cb6f0fdc51a3813a099ee38fba5e2ca4774769` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/GridSystem/FieldGenerator.md` | 5942 | `7418a0be81312747ed69f5ca0d28868a0767d21f55e190f6773460e0c5fb1840` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/GridSystem/GridPlacementService.md` | 2871 | `22193ca47695512dd0649b24b1d04385258873d24c5d590df20453c15b10b14c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/GridSystem/FieldObjectSpawner.md` | 6162 | `aa6c09a0857d4a401863fcb06a0418e503d8eed2da8f0d0763c599dc393b484c` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.HtmlToUitk.Editor.implementation.md` | 14828 | `4e59152e96faa68d320d81895b328f4f209ce85ea4d883b7cd43fc277f3771cd` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.HtmlToUitk.Editor.implementation-request.md` | 8347 | `a39df775ebb46f5b8d2c5e9258b7406ba04174949a3a78fa54e60da8348e589e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs` | 35614 | `020ac2d612d14f5e093c9b8b7115f7facd847e0b84891eafb476a37ae51745ac` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Bridge/CliProcessBridge.cs` | 9948 | `9810d7c66bb781a8ba0b81073eac29fd067a7e0290b154223d5503d1bfc65973` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Bridge/HtmlToUitkRunner.cs` | 8524 | `3e94e33f4e4cfa6f653074bf49fd541235c753e2aece7ab1907b514a2ce955ff` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Bridge/HtmlToUitkSettings.cs` | 4700 | `9a66e561185b431cc81a26a1e670c704a61857b860e974a80122f9ba73ba916c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Import/GeneratedAssetPostprocessor.cs` | 5423 | `124e04dda5949b6a22ade52172203b49394f55fbab59001fe198d66372e61956` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Jobs/HtmlToUitkJob.cs` | 7254 | `4e5d8062dd681d27f7742920a7b9425d9deed04479b5a7781e8c1277b1fe8b8b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/JobDocument.cs` | 8647 | `895c24558cae52b86d74eb61877566556f334c704a2b6a7d5a9739a08953aca9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/JobInputPaths.cs` | 5399 | `e4909a58c51055251c846b06067f9fa97d69c1dc06cdf35c5d313823ab09a477` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/ReportReader.cs` | 3674 | `d113eb636462786abb0900f799d264b17786eeb76544015b755481be0d180bfa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/DiagnosticsReport.cs` | 7530 | `82f917ebbb4da8354ca46cf86ab807d558ecd4a14ad855ede3669c155e81cd6c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/IJobBridge.cs` | 1764 | `e8983dac503cae1f893f3929e13714f195731abfc6f0b688db2d78667c9ee5f6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/CliCommandLine.cs` | 2217 | `d4a5de7a945a1bed10dce0da803258b514ef475499af2960353087b82a136356` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Watch/HtmlToUitkWatcher.cs` | 4172 | `89a7ea4fc141d4cc811d8f2a73c32a83bdcb58f69a6315476a496129ae5e3244` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/UI/HtmlToUitkEditor.md` | 11577 | `832167b1733cb7d84d2feb0311464124a88d578867ed8608bf2bf8873f994335` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorPipelineTests.cs` | 10766 | `a84f8bd507c37315aae6de5f00133519420c8da4d5850c7545e21842a3a2b04c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorWatcherTests.cs` | 2426 | `263594df15c3a6d6751a7df13bc98eb65f443a9902db355def812c40b8ad56b4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/PureLogic/Audit20261023NeoHtmlToUitkEditorDocumentTests.cs` | 885 | `4aec7bfbee4896a531bfbb82bc44a5576e1a7a267cb22a2c8ccef9fdb8dd526f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorWindowReplacementTests.cs` | 11872 | `cad57ee5353d329bf1fe9a755e0f89d3b5d0d26c7252b1d46ccb45c56180fd16` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorWatchDependenciesTests.cs` | 8879 | `655491d6c1fc6cd153812ec0918bd31bb59991d7799dfe2894a9021167d3d7e2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorProcessTests.cs` | 5042 | `c688008d46fbd52dd5550c9b709725a5a3c36c4327fe92fa4883da2ff407db4b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/PureLogic/Audit20261023NeoHtmlToUitkEditorInputPathsTests.cs` | 3878 | `8625d468c97f4168008f6e86b504f4f95f194edfc9844f331d76e910aa5e2006` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/PureLogic/Audit20261023NeoHtmlToUitkEditorReportProvenanceTests.cs` | 5184 | `17a574303e845db8427ba218d9316aad8e103b2aa27e247dd9dd4549f6ee4be7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/Audit20261023NeoHtmlToUitkEditorProcessProbe.cs` | 905 | `830926e36b2cddb41475a8608e85a94235efd12aaa84e8eea5f82a713f8408fa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/Audit20261023NeoHtmlToUitkEditorProcessProbe.csproj` | 868 | `b7f237326b212db57ef3561cf5bbfdf188c1dd38ae413e886ba6300e92edb298` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/JobInputPaths.cs.meta` | 251 | `79fdb47b1acc8dd7ebb292e90325fa862f63cd07eb84a36682fe8c018965c540` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorWindowReplacementTests.cs.meta` | 251 | `b7612ab5ef8b38c727b6dfb9f29c1ac26463d1e710011451e5d7ff31d632b959` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorWatchDependenciesTests.cs.meta` | 251 | `0a55146228fcea3d9b6bc6f6dc521d974883987ae99faebbcf29f67e9f2e844f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261023NeoHtmlToUitkEditorProcessTests.cs.meta` | 251 | `945c0846137e3ee614076dfd2484eec0531884b07ac48a9708407e2522ea23b8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/PureLogic/Audit20261023NeoHtmlToUitkEditorInputPathsTests.cs.meta` | 251 | `6835a189aa1c92bf5dcdd9981699f8a75eebd4cf13a8566ea2551b65a2f91b72` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/PureLogic/Audit20261023NeoHtmlToUitkEditorReportProvenanceTests.cs.meta` | 251 | `f701172de9add1349297c6edb66285f31de862b5c8934b3430fce5b7c5690ccc` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/Audit20261023NeoHtmlToUitkEditorProcessProbe.cs.meta` | 251 | `3c5c25f8b6414c1b9000a527485b84aa3edd58bf0c4f1c18c9db3d81d47c1798` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/Audit20261023NeoHtmlToUitkEditorProcessProbe.csproj.meta` | 162 | `0cc750bff98b4257b8667ba7c99c444bb87f3cc716b8f570772a135295fddf19` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeImplementationReview-23.md` | 25825 | `7dc18aee4deca5c45eea766ff608a9fa3c89d746dc4f7b0fe5ef6dd0ff9bf1fd` |
| `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b03-05.meta` | 233 | `387fdbedeae4c84f646124794103d36931d38ef5dbf7d6abc4bc882c10aa336a` |
| `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b03-07.meta` | 234 | `5e370b40151f7dc33bf3280d660264fcedd8ce93d47f5940296295e045452ad9` |

## Ready-scope raw identities

Capture **2026-10-23T21:43:09.010Z**. Full absolute roots below are raw-byte SHA-256 bookkeeping, not validator or execution results.

| Full path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Progression.implementation.md` | 14924 | `c5883021ae3250d850885c239089a040bdb0a8044dbea7beafe4a3580be3724c` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Progression.implementation-request.md` | 6593 | `6e1858d4df04ec050d315c8f29d1bb88c1894fcecc6c918e502169a084576e75` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Bridge/ProgressionConditionAdapter.cs` | 2944 | `231100a60563f1f1b50f2dec4edd946a65af099725d5e3d9acfce0e461744e35` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Bridge/ProgressionConditionEvaluationMode.cs` | 324 | `376160364fa2fea6bba5b0ce6b4c8769baef84a0bd1fcd2bf8536e6c82748891` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Bridge/ProgressionNoCodeAction.cs` | 6761 | `c3c1f494d61749b0c06ba004025edb35544e00aca28fe4068cffc80676a67407` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Bridge/ProgressionNoCodeActionType.cs` | 338 | `1cadf60200acacebc289bcbaada8b0626cf0654247bd1870b8bd6a8c21de7741` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Components/ProgressionManager.cs` | 38222 | `3388c56b1e7ff148a9bd2aac1043ff223c83639bec962dc4459d182bb03d84ef` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Data/LevelCurveDefinition.cs` | 6578 | `fc9e450fbba5dd6b67415985dd5e0d89a672366b5b0545789b402ed66e5e4d00` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Data/PerkTreeDefinition.cs` | 9616 | `68d5b6b41775c8b39afc2780150a65616004607543d7199755c2c621e695e26d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Data/ProgressionReward.cs` | 2822 | `b9a241d1afa991bbbe5465be531ca726f1ad2cdefd4ecadd65b0b979f5d45b46` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Data/ProgressionRewardType.cs` | 340 | `99cfb41bbb14697a8293b130a7ce28bc46c41c9eda83699486f231abf8dc32d5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Data/UnlockTreeDefinition.cs` | 7467 | `effabdee846152ff0711628c62904c34a1a9ed1f8e06f529e7a804a3d7a9269d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Runtime/ProgressionEvents.cs` | 324 | `261babc4564ba658c38cad5a296c267f984f69f62430526d81dadd4c35af8b4f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Runtime/ProgressionProfileData.cs` | 3224 | `48c80ae1d1e8d636e525b8c423e4631add57f64869eff858adc06e803bb3c3c2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Runtime/ProgressionRewardDispatcher.cs` | 4328 | `42df92155f4a5689dcb80b3bc5a152cc9d112f48c2061f140de28e2a46c0f893` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Progression/Neo.Progression.asmdef` | 879 | `b96d110e1cdac4a124f5db2a2546eb3ce77b637d4eafbbbfb3a7f50dba04bce1` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Progression.md` | 22461 | `5e3c2163f2e9981d99c72308f00543cb9ca1d8f98cb57152a5da5e22e373acf4` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Progression.plan.md` | 14765 | `07a9545bdafb7369078bfd64577a50d5198af851ef39a2e5fe5c7fdbf3d6f58a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Progression/Audit20261023NeoProgressionRegressionTests.cs` | 55825 | `9df70c09570e4fa7b6be7a74d20d7cf492114a97d7417daa341994130ece39f8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Progression/README.md` | 7610 | `87a1297035c04d804d00cbb74a82b3dad9be1ac34a9e822ff0e827b002b306a4` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Progression/ProgressionManager.md` | 9558 | `00e7290bc463b908d5577ee37565345ba46cf52b9a830f25e0793a6a11eea30c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Progression/Data/LevelCurveDefinition.md` | 3007 | `d4b0ade2c04d63dc31c1db744e1c1ce607385f9e2ead84e690d1f7694b7ec680` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/ExplosiveForce.cs` | 10026 | `6bdccab0266c69320bd7af150bd8340d7226459af30ee779bea11de8e27cab45` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/ImpulseZone.cs` | 9467 | `1e5f6c128be08a17aead88d1e69a56c572b8eec5778af52c0bbdf310de4637e7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/MagneticField.cs` | 18412 | `83b08621cd01af8e41f6d6e20a9a8780309bde8e674d6ffb2cf07b56ea011af2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsPhysicsRegressionTests.cs` | 29525 | `95063b22ea3edf08aebb5c8c9f2e63eb298d6c10b9a57fcc207b8c5243ff6c43` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Physics/README.md` | 2343 | `be83c82c693c6889773acbdddabe1db5389bd07b97808cfeff66aa2c3a02e9b1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Physics/ImpulseZone.md` | 3705 | `1ce4e4ef74ba364c9da7cf8751f37312131b62eaae5309dd2ce10f45ff04f83c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Physics/ExplosiveForce.md` | 4732 | `24bb198205a9699bbdc38bc43ce841ed0d5241b3708a314584b8a85ce63b8996` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Physics/MagneticField.md` | 5741 | `f7a835d2b4f781c1f86393e6a41f44eb1701a9b76878549c0f2aa2faef96d701` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Physics.implementation.md` | 11088 | `4294ec6f0c91f4dca23c85689848c64dd0a9461d082b5aa22a34b9fc02cba235` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Physics.implementation-request.md` | 7883 | `ba85b55cfe41dcb593d72f13442df0b4f27a71e4c54e9dd0126518846525cd0f` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/RootPhysicsEmitterReview.md` | 2679 | `0f27ea34cd014853a4a4ac24c8d770e46c4b20fdf85ec6f7928e3f130dea1e00` |
| `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b08-01.meta` | 233 | `0aee245cca4ca68adec57a951ad8349b55f1c6c05fcc516f0c7518ff37d041e4` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeImplementationReview-20.md` | 43546 | `21bd0232d99a522f5084afdd2f4c784f4cd2c64452076644043729b3552d699e` |
| `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b04-07.meta` | 233 | `6733bf18249d75cac15d00626e1b328abe430fbbe610ab12221088ff98d87e1c` |

## Provider/shared context identities

These establish the exact readonly contracts used above; context coverage is the relevant method bodies/definitions, not an unrelated full-owner re-audit.

| Full path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Managers/Singleton.cs` | 6132 | `fcc91f59e26dec884c2604943d0de58aee7c8023c2a1fae20ac89b113e71c874` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Core/Level/Components/LevelComponent.cs` | 10802 | `616dd0235e0c7f2de3b4a074739dd135fb6227b164910fbb52066aa93fa31cab` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Reactive/ReactiveProperty.cs` | 21394 | `14df81d1e13d76b658e20e8560dfa14376ce882c716a827da80f34e03256b2a4` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | 15329 | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NATIVE-REVIEW-CONTRACT.md` | 4739 | `c1bb559c242ee21a44542abacc013cba2f7955d64c154350a4f336cbd11f1edd` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeReview-C.md` | 152380 | `118bffb374e28cc2fc75f525e3292ac38e000d996b6c42b3d71a89edbb9cb075` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` | 2098 | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/Neo.Tools.Physics.asmdef` | 461 | `d0e50d13c1aa4a383eb2f8656eb6545cd660ba545b501ef715af2b7d86ce0cf1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Jobs/HtmlToUitkConfig.cs` | 2718 | `bd99719ec31705b72f52e1989608852dd090271ad6fe326cb9bfa7981058656a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` | 1505 | `2518b0df939933514e55de55be5313226107a2382fe89d31b5361565aea50fe4` |
