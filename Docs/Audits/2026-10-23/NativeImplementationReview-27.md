# Native implementation review 27 — Cards handoff / Demo ownership consumers

Status: **COMPLETE — Cards I21-01 and assigned Demo implementation source-closed; all execution UNEXECUTED**. Same independent native `gpt-6.1-sol` / `max` session. Only this report was written. NI21/22/25 remain historical/final and untouched. No product/config/other-report changes, Unity/compiler/tests/checkers/services/process control/children/commits.

## Intake

Complete SOURCE-FINAL receipts read: Cards66 lines, SHA256 `d618f4b4ce8e7ef59356b05b390d2707070d8186f98ec70e4344c8fef4c9ed6a`; Rpg.Demo101 lines, SHA256 `5d84fbf824297b1482396ee66aee92f41dfaabcc56f4ffeed9420fc097e06cd4`. Fresh local cli-liveness at `2026-10-23T21:35:19Z`, SHA256 `6bd000b814286103dd8c5d02657a36fdcc64a04d360843dd024a1b16e59871c6`, contains neither exact Cards actor b02-05 nor Demo b05-05. Recorded process identities are checked before admitting source. Root independently confirmed Cards PID22136 exited and Demo absent. Other writing correction scopes are not reviewed as final.

## Exact-owner gate and dispositions

Cards intake is admitted: recorded CLI22136 and original/recovery wrappers40852/14444 were absent at21:36:20Z. Demo intake is also admitted after resolving a reused PID: old launch102480 now belongs to exact other task `neo-audit-1023-b03-07`, whose CLI resumes UUID `01a16770-5d6d-7f40-9a15-1e1163dcca5b` (sanitized read-only process identity,21:38:43Z). It is not evidence of a Demo writer. Demo recovery25168 and exact b05-05 CLI were absent; root confirms original-session done0 metadata and stable final receipt. No process was controlled; the unrelated writer is outside this approval.

Closing read-only enumeration at21:49:15Z found recorded PIDs22136/40852/14444/25168/102480 absent. Refreshed cli-liveness at21:45:33Z, SHA256 `c955a2a09fe66810a83ce86cfa784fa65a66700ae2b33e9a2956a58be00b1918`, contains other independent reviewers, neither original actor. Receipts and all central scoped source/fixture/guide fingerprints were stable at21:49:00Z. Root's original Demo session-pointer/done0 metadata and exact-UUID CLI absence supplement the sanitized reused-PID resolution above. No wrapper/unrelated process was controlled. This is an exact-owner gate, not a claim every repository writer stopped.

| Original owner/task | Source disposition |
|---|---|
| Neo.Cards / `neo-audit-1023-b02-05` | **I21-01 source-closed.** Commitment → cleanup release → native parenting → public model notification now has the required order for normal/bottom insertion. No additional confirmed bounded defect. |
| Neo.Rpg.Demo / `neo-audit-1023-b05-05` | **Assigned shell/audio/config/quest/pool consumer actions source-closed.** All13 implementation runtime deltas/current teardown branches, complete new nine-case fixture and guide read. No additional confirmed bounded defect. |
| Other providers/consumers | Exact consumed API bodies checked only. No new approval of writing Grid/Progression/Physics/Html corrections, Editor lobby persistence, actual peers or unrelated provider runtime. |

No additional source correction is requested. Execution/coverage debts below remain acceptance work, not newly confirmed bugs.

## Cards I21-01 closure

`Assets/Neoxider/Scripts/Cards/Components/HandComponent.cs:188–243`, TryAddCardAsync, rejects null/full/duplicate before mutation. Component membership/version commit before HandModel.Add/Insert. The existing accepted callback at202–208 now releases deck cleanup **before** SetParent. `Model/HandModel.cs:98–101,144–147` commits data before that callback and public added/changed notifications after it. Genuine hierarchy Reset therefore sees an actually accepted hand/model member already absent from deck cleanup; ordinary public model observers still see the final parent. Bottom insertion uses the same seam. Admission-history, click ownership, cancellation and postcallback/postawait membership/version guards remain. No new mandatory API/setting/equality/serialized migration.

Read all four new cases/shared body at `Tests/Play/Audit20261023NeoCardsLifecycleTests.cs:491–675`, complete42-line observer and both complete updated guides. Cases cover parent-change Reset/regular, parent-change reinitialize/bottom, children-change Reset/bottom and children-change reinitialize/regular. An existing member makes bottom ordering meaningful. Real one-shot native callbacks assert exact committed hand/model order and final parent before public model admission notification; actions are consumed before invocation, disarmed in finally before cleanup. No SendMessage/direct Unity-message invocation supplies the trigger.

After deferred-destruction yields and bounded real-time quiescence, each requires exact accepted card alive in the exact hand/model/parent, replacement deck unchanged, one draw/admission, zero obsolete deal/visual success and no late transform. Each then proves an ordinary deal with correct insertion/model/events/click ownership and full-hand rejection preserving source/membership/events. Public OnClick invocation proves subscription ownership, not native pointer input. Reset supplies destructive repro, reinitialize generation parity. All are authored native controls, not executed native results.

Compared all13 Cards fingerprints pinned in NI21: only expected Hand, Lifecycle and two guides differ. HandModel/Card/Deck/Board/presenter/Order/README/Board guide/HandView guide remain byte-identical. Removing **only the new186-line hierarchy block** reproduces exact NI21 Lifecycle SHA `c9836523a2f1fea11baf841c0b8e86c0f727ffc8f084292af6d3068a641ee6f2`; prior83 cases/helpers are retained unchanged. New observer/meta are additive. Deck/Hand guides accurately describe the native boundary. The87-case total is authored inventory, not discovery/pass evidence. Prior NI12/21 remains historical source review, not fresh whole-module execution.

## Demo runtime and dependency coverage

Read complete original191-line audit/55-line plan, latest79-line request/101-line receipt, all13 implementation runtime hunks/current construction/disable/destruction context, complete591-line Session fixture and117-line Examples entry/diff. Original49-file audit/F1–F8 are retained historical evidence; unchanged lines of all49 files were not freshly reread. Older eleven fixes/two fixtures were not reapplied or claimed executed. Name-status against `ca198556` shows scoped modifications without source deletions/layout moves. Broader enumeration includes earlier audit/other correction work and does not independently approve it.

Here `S = Assets/Neoxider/Samples~/Demo/Scripts/`.

| Current source / relevant lines | Verified control flow and ownership |
|---|---|
| `S/Shell/NeoDemoShell.cs:53–96,205–312,335–418,490` | Full545-line current source read. Old Build forwards null owner, preserving root placement; additive owner overload. Canvas/native gradient recorded before later setup; construction catch disposes partial resources. Dispose marks disposed/nulls handles before native disable, removes generated listeners, releases captured owned objects in finally, idempotently. Exact private gradient survives borrowed backdrop replacement. Hidden/disposed callbacks guarded; AddRow after disposal throws. Camera/EventSystem remain shared scene services. |
| `S/ProgressionDemoUI.cs:38–86,125` | Owner shell hide/show/dispose; remove exact XP handlers and newly created bridge. Existing manager/save workflow retained. |
| `S/Shell/AnimatorsDemoController.cs:33–47` | Destroy only newly created disc/frame, preserve shared SurvivorArt. |
| `S/Shell/ParallaxDemoController.cs:28–43` | Track new layers before setup, disable/destroy only those roots, retain camera. No new Update search/name cleanup. |
| `S/Shell/NetworkDemoController.cs:24–28` | UI lifetime only; removal does not Stop borrowed network session. Explicit user Stop unchanged. |
| `S/Shell/SaveDemoController.cs:30–44` | Base registration/autosave preserved; hide in finally after base disable. Base has no OnDestroy to bypass. |
| `S/Shell/SettingsDemoController.cs:27–30,80` | Remove exact settings handler/dispose frame, retain shared settings. |
| `S/Shell/AudioDemoController.cs:33–79,150–181` | Three SFX paths use clip-addressable PlayAt(clip,Vector3.zero,1f,0f); fourth parameter is spatialBlend, preserving2D. Remove own handlers, snapshot/clear six owned clip fields, selective ReleaseClips before destruction; clip/frame cleanup in finally after notification fault. No global Stop/borrowed track or manager destruction. |
| `S/Shell/LevelDemoController.cs:73–108` | Deactivate owned host/detach curve before destruction; remove exact Level/manager listeners. Borrowed manager/owner survive component-only removal. |
| `S/Shell/StateMachineDemoController.cs:103–165,190–248,438–440` | Definitions hold data; scene listener recolors. Stop owned machines, clear caches, detach NoCode data before owned host/three definitions/views destruction. Attack exit guards prior dot destruction. Existing typed rules/settings retained. |
| `S/Shell/QuestDemoController.cs:95–124,467–479` | Record generated configs immediately; disable/detach/destroy own bridge, remove own handlers; unregister expected instance before config destruction. Preserve saved state/same-ID replacement. |
| `S/QuestDemoView.cs:91–112,325–398,761–785` | Exact RemoveListener delegates preserve foreign listeners. Only generated fallback configs owned/unregistered/destroyed; resource/flow definitions borrowed. |
| `S/Survivor/SurvivorGame.cs:69–98,267–316,761–782` | Track exact generated projectile/enemy/orb sources, return tracked actors, finally retire each through SpawnUtility, including destroyed Unity wrappers without Unity-null filtering. No shared root deletion/private registry reflection. General world/session teardown explicitly excluded; component removal does not claim disposal of every world/art/hub object. |

Provider bodies read: `SpawnUtility.RetirePool:149–169` uses ReferenceEquals-null/unified manager+fallback retirement without singleton creation; `PoolManager.RetirePool:148–168` removes exact registration/preserves shared roots; `NeoObjectPool.ReturnObject:289–317/Retire:323–331` preserve active rentals until valid return then destroy. `QuestManager.UnregisterQuest:265–289` checks ReferenceEquals-current without deleting state/save; GetObjectiveProgress665–668 reads retained state by ID. `AM.PlayAt:845–863` accepts explicit2D spatialBlend; ReleaseClips1790–1903 settles current/outgoing/ambience/pooled voices/legacy references before optional notification. AM hash matches NI22. `SaveableBehaviour` complete29 lines confirms base lifecycle. These are consumed API checks, not whole-provider reapproval.

Complete Examples entry/diff accurately document both programmer/scene lifecycle, retained overload, ownership, English/Russian guidance, release/developer/imported roots and unexecuted status. Existing serialized fields/signatures/IDs, scenes/prefabs, save behavior, sample layout and global input settings are preserved. New shell APIs additive. No competing entry point, required registry/controller, optional framework or newly unbounded Update work introduced.

## Complete new Demo fixture and remaining acceptance

All nine UnityTest bodies plus setup/helpers/teardown read:

1. Twenty warmed Context cycles require live button/slider/toggle callbacks, hidden/disposed silence, double-dispose, deferred Canvas/gradient destruction, exact generated Sprite/Texture count return and borrowed services/art survival.
2. Two frames prove old root overload/owner parenting, borrowed backdrop replacement and surviving frame interaction.
3. Twenty real Network Start/component-removal cycles prove frame/gradient reuse across disable-enable, deferred destruction and final whole-owner destruction. No network session starts.
4. Audio requires actual playing borrowed track, real clip-addressable demo SFX and outgoing-pad crossfade; removal destroys six clips/clears outgoing own clip, preserves borrowed current music/AM.
5. Public Level XP button adds10 before deletion; exact curve/consumer disappear, borrowed manager survives.
6. Quest accepts/progresses configs/saves nonempty JSON; same-ID replacement survives removal, own configs disappear, saved bytes unchanged/public Load retains progress/state.
7. QuestView fallback cleanup preserves unrelated borrowed registration/manager. Resource/flow definitions are source-reviewed, not dynamically exercised here.
8. Public StateMachine Alert succeeds; deletion stops captured machine, destroys owned consumer/three definitions/views, retains borrowed art/owner.
9. Real Survivor source and unrelated same-name source share storage before removal. Own inactive clone disappears, active rental survives until return, retired source cannot recreate, duplicate retirement false, unrelated exact rental/root/manager usable.

Isolation snapshots SaveProvider/Random/AM+Pool auto-create flags/AudioListener.pause, supplies in-memory provider/isolated key, restores borrowed camera after native cleanup. FreshService skips ambient managers instead of taking ownership. Optional Demo discovery precedes borrowing state; absence is explicit skip, not pass. Survivor skips unsupported new-input-only setup. Reflection crosses optional assembly/authors Inspector fields; public behavior/native references, not private registry counters, are oracles. Shared Play asmdef has direct Audio/Level/Spawner/Cards/UniTask/UI/TMP references used; optional Demo/Quest/Abilities types reflected, no hidden Samples~ reference added.

**Central acceptance remains UNEXECUTED:** compile/discover/run frozen source on contracted Unity hosts with sample genuinely imported; execute retained Cards suites/new genuine hierarchy cases/observer import and nine Demo cases with functioning audio/deferred destruction. Gradient counts are scoped named Sprite/Texture counts, not native-byte/GC/FPS proof. Twenty-cycle coverage is Context/Network, not every controller. Per-controller repeated reopening, construction/postcleanup-notification failure injection, resource/flow borrowed quest paths, external UI roots, scene reload, actual input/presentation, pool fallback/dead-wrapper paths and supported sample roots still need appropriate provider/scene/manual checks. Audio assertions do not establish audio quality/transport results. Editor save/reopen lobby and actual peers remain other-owner central work. Unexecuted scenarios are not newly confirmed runtime bugs or prerequisites for an optional framework.

## Read fingerprints

SHA256 identifies actual source reads, not validation passes. Artifact paths relative to `Docs/Audits/2026-10-23/`; product paths relative to `Assets/Neoxider/`.

| Artifact | SHA256 |
|---|---|
| Neo.Cards.implementation-request.md | `03adc425b3ac987eb075eff99d61f3737179ec4d89dab9607282c90835d4bfe7` |
| Neo.Cards.implementation.md | `d618f4b4ce8e7ef59356b05b390d2707070d8186f98ec70e4344c8fef4c9ed6a` |
| NativeImplementationReview-21.md | `2945a80912a76a6cc453fe513dff972ce84e0ae408e7ba734c677e21b544b259` |
| Neo.Rpg.Demo.implementation-request.md | `e3f40c37b667c0176850c1441987a7015e7275cab5ea1b18631b6a8e18463895` |
| Neo.Rpg.Demo.implementation.md | `5d84fbf824297b1482396ee66aee92f41dfaabcc56f4ffeed9420fc097e06cd4` |
| Neo.Rpg.Demo.md | `c92d9de45dbb8ea0561094225c858f0d9ae48500d719557078554986250680a1` |
| Neo.Rpg.Demo.plan.md | `331be2a6e5ec439f32299838191c01f3f5480c9a1a324e1702ee31a587ad0190` |

| Product/fixture/guide | SHA256 |
|---|---|
| Scripts/Cards/Components/HandComponent.cs | `38d1c9e0cf284802091be2d4d030d18c61319443988f2121633648cf5dca3ce4` |
| Scripts/Cards/Model/HandModel.cs | `8377fff257efbb6d0f9d85d93ec4bd2c22653d61bf476f002d269f412814c47f` |
| Tests/Play/Audit20261023NeoCardsLifecycleTests.cs | `411312ba99cc11795e43e374b1ceba6a599fec5afff57710c0d21c84475a1514` |
| Tests/Play/Audit20261023NeoCardsHierarchyObserver.cs | `aed239406a4a02cff375baedffb28f83ebd37a001b5b640123004109bef27db6` |
| Tests/Play/Audit20261023NeoCardsHierarchyObserver.cs.meta | `4418a001f8c10cb161ed80ed2d028f3fb9587eb96118bcfe369e50a29a1ba2b3` |
| Docs/Cards/DeckComponent.md | `61cb21d930e3566519932ef0aaba34591d49318e7a4f329cb26d8edf281a137f` |
| Docs/Cards/HandComponent.md | `7580e756aba960c634edadf159bd879f916ce1db237f4087f39c9bd207e41fa2` |
| Samples~/Demo/Scripts/Shell/NeoDemoShell.cs | `cca83ecaf07c8ddc39cc7c89a57d82b1c75aa8087e336b68e5c9eefc47c8cd4a` |
| Samples~/Demo/Scripts/ProgressionDemoUI.cs | `bbb594c00b07ad634a1df1293b525e8d1feefb66383fa524c980a9a093cfb307` |
| Samples~/Demo/Scripts/Shell/AnimatorsDemoController.cs | `0b7686083be98a4b9d60323b8285ef1f3df8a95c5c010a34a6a608980cd336e5` |
| Samples~/Demo/Scripts/Shell/AudioDemoController.cs | `242e5795a9135b5de404e314a8b40ee6542c53698d585be0b11bb2a2167047cf` |
| Samples~/Demo/Scripts/Shell/LevelDemoController.cs | `8f7ac0b4d2b6b951d24e96e52572d6000e74dc789afa502e7e4f8ba5ba957473` |
| Samples~/Demo/Scripts/Shell/NetworkDemoController.cs | `090d80da646ac2ae35765deb6700e3e5a79ebe88bcf5d791583a5c4c90431513` |
| Samples~/Demo/Scripts/Shell/ParallaxDemoController.cs | `cf9d92c8da0a0e2a59b5e4ba1ac4d0322a07bb2ceb10877182a99be998b88e66` |
| Samples~/Demo/Scripts/Shell/QuestDemoController.cs | `e095221b5d2c3107bfa43cc91883a1b62f9d4da675d24565e6b73b27888b95e1` |
| Samples~/Demo/Scripts/Shell/SaveDemoController.cs | `e5d561dbea4ac16d430201dd002094d382692b4f602496b71fea1d8e765c9137` |
| Samples~/Demo/Scripts/Shell/SettingsDemoController.cs | `ea64d528e4380b0d5b6a8dadbeaded990facc95509f1c250a7551ca6df9c8722` |
| Samples~/Demo/Scripts/Shell/StateMachineDemoController.cs | `ea82e14435a684c6dcf03b3098d63cbbbd1061be644bcadd392ba07038575738` |
| Samples~/Demo/Scripts/QuestDemoView.cs | `2a54b98cf3ef0badcb04985007aa4842db0cb4c469be4271b096652bb4b9f02d` |
| Samples~/Demo/Scripts/Survivor/SurvivorGame.cs | `e7ea97346aa4dc365186442e97300e2b15254d3d955414cfcbe31caf1ae1c569` |
| Tests/Play/Audit20261023NeoRpgDemoSessionTests.cs | `8b71ca7f999d28c05d410808ae5c2e1e5d9a3bff6cf01cab1ebbc76f9bdc0d9b` |
| Tests/Play/Audit20261023NeoRpgDemoSessionTests.cs.meta | `2195c0dd81fd1afac344cfff0cb5a2263f98ce4d808f63edbb614d73b1208e61` |
| Docs/Examples/README.md | `913a43665daee995159e06e92fba2c417c6dfecf7138bd52aa20f7d92b0ce680` |
| Scripts/Tools/Spawner/SpawnUtility.cs | `fe579c07db75db1ddc5f6459688d7b00283ca4bf74933a9212e9deb53556c851` |
| Scripts/Tools/Spawner/PoolManager.cs | `8632e8f54ec4a01ca3c49168ce7f60f2f25536cb1f32dec137b981d45a19cd3c` |
| Scripts/Tools/Spawner/NeoObjectPool.cs | `062d273781f826da1449da1d1b357547f81ebdc3ea340d7109ea90e7d2b85577` |
| Scripts/Quest/QuestManager.cs | `35f34f807bcf2256ffa2816838592004bc0a31e5d71252f0fbb90d5adf8a3e8f` |
| Scripts/Audio/AudioSimple/AM.cs | `6f39f3bb90404bba1e3d27baa9fd82ecbb107ec3ac02331d8a386bc8d83a1f3b` |
| Scripts/Save/SaveableBehaviour.cs | `bc894d3c03d07617e05dd8a8164e5fbdcaa6c4e9a2c1ce861675bdb9b65d7bba` |
| Tests/Play/Neo.Tests.Play.asmdef (read-only context) | `2518b0df939933514e55de55be5313226107a2382fe89d31b5361565aea50fe4` |

Observer GUID `471e257c021e4c97ab7dfedec85c58aa` and Demo fixture GUID `8137da6cbe0d4cb592f57a10bbf68e77` read from metadata. No global GUID/package validation or Unity script import run. Source closure is limited to these frozen reads/contracts; release approval awaits central execution.
