# Neo.Rpg.Demo: approved implementation request

Original actor: `neo-audit-1023-b05-05`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Preserve release Samples~. Integrate exact-source Spawner retirement, Quest same-instance detach, Audio exact-owned clip/playback cleanup after their additive APIs land. Context owner overload+Dispose is idempotent and removes only owned UI/native resources. Root receives dependency request if API not yet present; finish independent source first.

## Assigned review actions

### EDITOR-LOBBY-PERSISTENCE (P1; approved-source-correction)

Editor C2: Join/Ready unwired, AddListener callbacks not persistent, scene RoomPlayer assigned as prefab

### DEMO-OWNED-SHELL-ASSETS (P2; approved-source-correction)

NeoDemoShell.Build creates unparented Canvas and repeated texture/sprite overwrites static gradient. Idempotent exact-owned Context disposal/bounded art ownership, migrate shell users preserving borrowed objects.

### DEMO-OWNED-AUDIO-CONFIG (P2; approved-source-correction)

Audio demo leaks6created clips/own playback and Level curve survives controller. Dispose own created assets and exact owned playback, detach Quest/State registrations before destroying runtime definitions.

### DEMO-EXACT-POOL-RETIREMENT (P1; approved-source-correction)

Survivor deletes prefab.name root affecting another pool yet retains registration. Use exact unique automatic source retirement shared with Draw; never delete shared manager roots by name.

### QUEST-REGISTRATION-OWNERSHIP (P2; approved-source-correction)

Demo must detach owned runtime Quest configs before destroying them. Add minimal same-instance unregister preserving saved progress/other replacement config, no general registry framework.

## Relevant full native sections

Source: NativeReview-C.md

### C-34 / P2 — demo shell/controllers leak their generated UI and native assets

`D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Shell/NeoDemoShell.cs:54–81` builds unparented Canvas; `GetGradient:191–219` creates Texture2D/Sprite every call and overwrites static reference; Context:226 has no lifetime API. AudioDemoController BuildClips:156 makes six clips, OnDestroy:147 only unhooks events; LevelDemoController creates curve:86, OnDestroy:69 only unhooks. Create then remove only the demo controller repeatedly: old UI/callback targets/native assets survive their owner (some audio still referenced by persistent AM). Expected generated session resources release while unrelated shared camera/manager/audio/assets survive.

Minimum additive owned Context.Dispose/owner overload and controller OnDestroy disposal, plus exact created clips/curve consumer teardown before object destruction. Stop only demo-owned playback, including outgoing voices if necessary via narrow Audio-owned release, never global Stop as ownership inference. No generic resource/session framework prerequisite. Test twenty create/dispose cycles after shared-cache warmup, deferred-destroy counts, component removal, two concurrent demos and double Dispose. Owner **Neo.Rpg.Demo / `neo-audit-1023-b05-05`**, Audio **`neo-audit-1023-b02-03`** only if existing playback API cannot release exact owned voices; Quest registry detach contract is separate before quest-definition disposal. Current plan correctly treats native ownership as required despite calling broader session work proposal.
Source: NativeReview-C.md

### C-35 / P1–P2 — Survivor teardown deletes pools by name and leaves their registrations

`D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorGame.cs:84–107`: tracks `template.name + "_Pool"`, destroys manager child root by name. PoolManager:80–90 can share that root for different same-name prefab identities and `_pools` still retains both entries. Dispose one Survivor with same-named unrelated prefab: its storage can be deleted; repeated scenes retain dead template keys/pools. Expected retire exact owned registrations after returning own rentals, preserve other prefab pools.

Reuse bounded retirement from C-23, store actual template identities/registration ownership, do not infer ownership from names or reflect into dictionaries. Shared root destruction must be reference-safe; outstanding rental late return must follow retired policy. Owners **Spawner `neo-audit-1023-b08-03` → Demo `neo-audit-1023-b05-05`**, joint acceptance with Draw. Test distinct same-named templates, active rental teardown, repeated recreation and unaffected external pool. General shared lease framework remains optional.
Source: NativeReview-C.md

### Neo.Rpg.Demo — reviewed

Full audit/plan, all eleven sample source diffs, affected view rebind/resource/controller/camera bodies and actual shell/Audio/Level/Survivor teardown, both complete new fixtures and canonical Examples README read. XP threshold widening/clamping, preservation of authored Dice assets, generated fallback with visible value, sample-root-relative prefab lookup, view subscription cleanup and generated sprite cache, configured Condition amounts, exact death-listener removal and optional health-bar resource cleanup are coherent. C-34/C-35 remain. Preserve release Samples~; fixes must also work from developer and imported root without moving package content.

Seventeen prepared cases use optional assembly discovery/reflection at public boundary and real state/renderers/events; absent sample/import and unsupported new-input-only Dice case explicitly skip. A skip is not acceptance; central run must import sample and count executed cases. Play fixture restores disabled existing cameras and destroys owned hierarchy; Condition test avoids best-score persistence but globally selected singleton/provider and native TMP still require isolation. External canvas/root, drag cancellation, Dice fallback disposal and player-without-editor-loader scenarios remain unexecuted debt. No assertion of compile, sample import, screenshots or input support made here.

Aggregate: sample health bars throttle scene-target scans but project/update visible targets each frame; unique per-target projection/LOS should remain. Views execute picking only on their configured active board/input, not a demonstrated N-per-target identical physics ray; shared raw query design applies if multiple identical contexts are active. Shell clips/textures are startup allocations and leaks, not invented frame-rate evidence. Data/session disposal should reuse existing services rather than migrate example combat/modules. Direct Mirror reference/new-input-only support are packaging/input contract risks for root's isolated import matrix; do not silently remove references/change project settings.

Full read fingerprints for Neo.Rpg.Demo:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.Demo.md` | `c92d9de45dbb8ea0561094225c858f0d9ae48500d719557078554986250680a1` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Rpg.Demo.plan.md` | `331be2a6e5ec439f32299838191c01f3f5480c9a1a324e1702ee31a587ad0190` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Condition/ConditionDemoSetup.cs` | `9e650f4784f4881a7eed2128fd2b51c2b5f3b83b07eae5451acdc65914a4026d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Condition/ConditionDemoUI.cs` | `14693bcc63678764b84ac6bf93db609d45772ff5aa9f7839ab31a4d941b55231` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceDieView.cs` | `7c9f3984e00a425613d1361de2760985979539320dabbd4f87610037bab34a35` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceMergeDemoController.cs` | `fe5b58a277d00c51400f26653d4e0c2038fd0e48b2318b7fd3e33502c0bd41d3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceMergeDemoSetup.cs` | `f9753f5496852833dc1cc7afd92d22cba8b660e4f8c3862a7a5729b14549ce25` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/Dice/DiceMergeDemoView.cs` | `475ae0aee57507c22ea9fc39782ab689be06a258c695f7284328ab7b43541c25` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemMatch3BoardView.cs` | `ac2d66a84e776ee5497599e37fa89ee777af6ca7c455eb72c7e2416b7bc53e07` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/GridSystem/GridSystemTicTacToeBoardView.cs` | `ad3bb6c1180fe4bff96285409ffef74a1f02bd2daf8d2563cad7d6d73edded5d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/RpgCombatDemoController.cs` | `436f4135f47147bf9b075e741912d22de48d4064f0a359b4700bcd440f4ae6c7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/RpgWorldHealthBars.cs` | `1f74bdf80797361409274c075bcb79e07f0990c7c48a1e1dd2ff4a9607fbb49e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/Survivor/SurvivorConfig.cs` | `2acdac259d3a33306feea6f5d78e2fcb01c983d9158c09a1956a620600578368` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoRpgDemoLifecycleTests.cs` | `841d86fdce059d68563d39e15af5566413c8e1576266f54de12e0cc219ebea51` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoRpgDemoTests.cs` | `a677ef63970d30544b5d93ae4eccf71abf01ebf2b12d10fab06e34ffa4103871` |

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Rpg.Demo.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Actual Unity6.6 and6.3 QuestDemoView compiler failure

FIRST ACTUAL FROZEN compile on both Unity6000.6.0f1 and6000.3.14f1 failed identically: Samples~/Demo/Scripts/QuestDemoView.cs111 CS0103 NeoDemoShell missing. Root read exactsource: QuestDemoView namespaceNeo.Demo.Quest, actual NeoDemoShell namespaceNeo.Samples (same demoassembly). SAMEoriginalactor bounded namespace/import/qualification correction, preserve internal cleanuphelper exactquestunregister/objectownership/publicAPI and all9 regression bodies. Fullactual compiler-errors.json at Local/Audits/assembly-2026-10-23/unity/final/ff4e1aaceb2e4320a3f882f61c262b15/u6000_6_0f1/mirror-on-dev/Receipts/. Bothdiagnosticjobs STOPPED and rootexecutiongateclosed beforethisresume. No tests/Unity/compiler/checkers untilALLsourcewritersfinish/newfreeze. Updateownreceipt honestactualpriorcompilefailure/newsourcecorrection/nativeoutcomespending; doNOTeditotherowners/sharedconfig/metas/samplelayout. AllunrelatedsourceapprovedearlierNI27 remains.

## Snapshot3 actual DEMO-NULLABLE-LEVEL-CLEANUP

ROOT has closed source3 execution after every owned native job stopped. SAME original actor only, gpt-6.1-sol/max. No Unity/compiler/checkers/tests until ALL source writers finish and root opens a NEW freeze. Root personally read all98 graphical Edit failure-message groups and all43 Play groups; Local/full-play-failure-triage.md (70 lines) and critical-failure-triage.md (35 lines) are independently reviewed. Actual XML paths are Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (7c6d24e1...) and21-full-play-graphics.xml (2b152164...). Read your exact cases/messages/stacks and actual runtime/vendor APIs before editing. CENTRAL RESERVATION: primary Neo.Editor.Tests b03-04 alone owns ALL Editor-only native helper moves/extractions, new Neo.Tests.Fixtures, shared test asmdefs, and its owned legacy test adaptations. Do NOT move/extract Mono helpers, edit shared asmdefs, or race any file in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. If the map is not yet present, work only the explicitly disjoint runtime/Play/new helper-free fixtures below; coordinate any caller containing embedded helpers before changing it. Preserve every substantive positive/negative/count/payload/ownership oracle and all public/serialized APIs/settings. Do not make production changes to hide a fixture precondition, add ExecuteAlways, blanket Ignore/error whitelists, manual Update/mocks as native evidence, reflection into private UserCode as receive proof, or weaker comparisons without actual native-baseline/numeric evidence. No other owners/vendor assets/314 migration targets, no children/commits/push; release Samples~ and protected DocsEn.meta untouched. Update own honest implementation receipt and canonical docs only if behavior/contract actually changes; report unresolved native capabilities explicitly. Stop all source writes when done. Confirmed real runtime hazard: LevelDemoController.OnDestroy82 dereferences nullable borrowed LevelManager.OnChangeLevel/OnChangeMaxLevel, aborting owned host/curve/shell cleanup86–90. Use null-safe exact own listener detach and ensure failed/partial setup still releases OWN resources. Inspect matching AddListener135/136 with a freshly constructed/partially initialized borrowed manager; do not replace its events/global service/state. Retain existing LevelController_ComponentRemoval resource oracles and add bounded partial/null-event control in own Play fixture if needed. Do not edit/move Editor demo helpers or314 prefabs; other Demo Editor lifetime failures await primary/native plan.

## S3 remaining Edit board-view sprite lifetime

Continue SAME original Demo actor b05-05,6.1-sol/max. Root FULL-read your current169-line receipt ac0375ff... and NI39 runtime/Play source closure; retain nullable-Level cleanup and its tests unchanged. Your receipt explicitly leaves TWO own Edit GridRebuild_ReplacesEveryCellAndOwnsOneSprite cases unresolved: Match3 eight renderers/TicTacToe four renderers, actual source3 line125 expects generated sprite destroyed, observedfalse. ROOT NOW assigns this remaining bounded followup. Read exact E28/XML and actual board view resource callbacks; distinguish unadmitted Edit OnDestroy/never-Awake component from a genuine sprite ownership leak. Arrange valid native lifecycle via top-level Play transition/public supported disposal or correct fixture setup, preserving positive exact renderer/rebuild counts, one owned sprite, old cells replaced, owner destruction releasing ONLY generated sprite and other resources/art surviving. If genuine runtime lifecycle bug is shown, fix only owned board-view code with all public/serialized settings/GUIDs retained. No ExecuteAlways merely for tests, expected-error/Ignore, fake private lifecycle as native proof, dropping generated-asset assertions or unrelated framework. Helpers/newFixtures/shared refs stay primary-owned; your new helper-free Demo Edit fixture is released, check reservations first. Main product test gate remains CLOSED; no Unity/compiler/tests/checkers/children/commit/push/vendor314 targets. Update source-final receipt exact S3 evidence/current correction and stop; root/native review/newfreeze verification later. No user approval needed for this already authorized residual.

## Root E28 actual native frames and acquired Play cleanup

SAME original b05-05/session6.1sol/max. Root personally read the complete latest receipt88043db8... and actual Audit20261023NeoRpgDemoTests.cs1-205; see Docs/Audits/2026-10-23/RootDemoFrameBarrierReview.md. Bounded source precondition gaps: two null yields167/168 and179/180 in Edit-assembly UnityTest do not prove two native frames; teardown23-39 exits any Application.isPlaying without acquired-session ownership. Change ONLY your helper-free owned Demo Edit fixture/receipt: observe at least two genuine Time.frameCount increments at each deferred-destruction boundary using a bounded realtime wait; reject preexisting Play before Enter and mark acquired only after top-level successful entry; teardown must destroy only owned resources, exit only acquired Play directly at top level even when cleanup/barrier faults, then preserve/rethrow genuine first fault. No nested mode instruction, manual PlayerLoop/lifecycle, new Mono helper, shared ref, fake frame, Ignore or widened oracle. Keep all14 logical cases, both explicit Match3/TicTacToe names, native OnEnable positive, all3 rebuilds/exact8or4renderers/sameownedSprite/oldcell and ownerdeath, otherowner/art/whiteTexture/generator survival. Do not touch NI39 Level runtime/Play, public APIs/settings/metas/GUIDs/other owners/helper moves. Update own honest receipt/current fingerprints; native remains UNEXECUTED. No Unity/compiler/test/checker/process control/new agents/commit/push. Primary helper extraction is completed, this original caller released. Finish source and stop.
