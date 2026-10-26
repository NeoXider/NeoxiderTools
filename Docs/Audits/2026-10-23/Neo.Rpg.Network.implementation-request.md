# Neo.Rpg.Network: approved implementation request

Original actor: `neo-audit-1023-b05-06`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### RPG-INITIAL-SNAPSHOT-REENTRY (P1; approved-source-correction)

Server stat refresh pushes snapshot while _initialized false; CaptureProfile re-enters initialization and may recurse/rebuild during enumeration. Fix core readiness/commit before publication.

### RPG-BASE-MAX-CONTRACT (P2; approved-source-correction)

Live final Max cannot persist through derived refresh; assess additive BaseMax setter and v3 presence flag migration without treating v2 final Max as base.

Root decision: Approved final-effective-Max snapshot restore within existingv2: restore-call-only max lookup, saved Max before Current and percent regen; unsaved rows ordinary resolver, last-row-wins, no temporary low clamp/death. Keep BaseMax and later refresh semantics. Durable base fields/v3 deferred as distinct feature. Read NativeReview-C-contracts.md.

## Relevant full native sections

Source: NativeReview-C.md

### C-32 / P1 — active-server snapshot capture recursively reinitializes character state

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs:243–269` EnsureInitialized refreshes before ready; RefreshStat:1252/NotifyResource call PushSnapshotIfServer; `RpgNetwork/RpgCharacterNetworkAdapter.cs:65–71` synchronously CaptureProfile; character `CaptureProfile:1399` calls EnsureInitialized again. Active server, enabled/resolved adapter (Damage(0) resolves it), ApplyTemplate/RebuildRuntime with stat/resource reproduces recursive Build/refresh/capture before initialized, risking stack exhaustion/dictionary replacement. Expected one completed rebuild and valid final snapshot.

Minimum core initialization/rebuild publication guard acquired **before** collection clearing, finally released, readiness only after consistent state, one final network notification after required loading/derived resolution. Adapter-only recursion guard still permits the first nested rebuild and is insufficient. RuntimeRebuilt currently precedes LoadProfile, so do not relabel it as complete-profile commit without documenting order. Owner **Neo.Rpg / `neo-audit-1023-b05-03`** first; **Neo.Rpg.Network / `neo-audit-1023-b05-06`** adds active-server final-payload regression after core repair. Do not run stack-overflow repro on unfixed tree. Snapshot coalescing/protocol envelope are separate optional work.
Source: NativeReview-C.md

### Neo.Rpg.Network — reviewed

Full audit/plan, both complete changed diffs/current adapter bodies, complete nine-case fixture, new Network.md and core boundary read. Current opt-out ingress guard, late client flag refresh, source-first/parent identity fallback and lazy wrapper character lookup are coherent. C-32 is required cross-owner repair; adapter-only recursion workaround rejected. Tests exercise actual woven serialization/registered delegate boundaries, restore Transport and stop client/server, destroy owned actors/templates. They deliberately initialize core before installing adapter, so do not cover rebuild. Dummy transport/lifecycle replay proves neither packets nor ownership/reconnect; weaving/discovery/real remote client remain central debt. Full JSON publication per notification is confirmed source cost; optional dirty revision/interval publisher must retain complete initial/late-join state and zero-interval compatibility. Legacy client arbitrary command opt-in/rate/authority remains explicitly retained; no new acknowledgment/validation/protocol guarantee inferred.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Rpg.Network.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Published RPG growth and snapshot provider integration

Root personally read full final138line Neo.Rpg implementation receipt. Additive contract now READY: RpgCharacter.ApplyLevelGrowth(), RpgCharacterNetworkCommandType.ApplyLevelGrowth=23 after22. Add Execute dispatcher case _character.ApplyLevelGrowth() under SAME existing trusted-state auth/rate limits, do not leave routed command silently ignored. Existing source readiness/snapshot publication guard complete. Author promised actual active-server MIRROR adapter initial CaptureProfile after complete init+auto-load, no recursion/halfstate, correctHP130/160/XP/stats; receiver ApplyNetworkState must not replay Current grants. Preserve v2 applied-growth-anchor limitation/modes docs, no broad transport/schema rewrite. Read exact final provider receipt and your approved actions, finish own source/tests/docs. No execution until global freeze.

## Root accepted NI18 evidence wording

ROOT FULL-read NativeImplementationReview-18.md all166lines. Source closure RPG NI9 and consumer behavior accepted; one P3 NI18-01 wording correction SAME Rpg.Network owner only. First-init fixture source.NetDamage(0) also hits adapter postExecute NotifyStateChanged so it proves facade completion/data/no intermediate exposure/no grant replay but cannot independently isolate/count core finalnotify. Direct alreadyspawned core RebuildRuntime branch DOES prove core->adapter publication; core transport-neutral exact1 fixture separate. Correct comment264–266/279 and own receipt55–59 (current locations mayshift), preserve substantive both branches and assertions. No runtime/seam/framework change needed just evidence wording. No execution yet.

## Snapshot2 actual missing RpgResourceId fixture namespace

Bothactualsource2 compiles report owned Play/Network/Audit20261023NeoRpgNetworkRegressionTests.cs208/466/510/514/534/535 CS0103 RpgResourceId missing. RootcurrentRpgCharacter/RpgStatResolver/RpgStatId source showsactualNeo.Core.Resources.RpgResourceId. SAMEoriginalactor boundedcorrectnamespace/qualification/import inownedfixture; verifyactualtypeassembly dependency(sharedPlayalreadyNeo.Core) readOnly, rootownssharedasmdefs. DoNOTreplaceIDswithmagicstrings/newduplicateenum/productionwrapper/dropcases. PreserveallinitialMax120/9,v2profile/snapshot/latejoin/callback/count assertions. Updateownreceipt actualsource2priorcompilefailure andnewuncompiledfix; allproduction/nineNI9contracts/runtime/metas unchanged. Exactbothcompiler JSON under Local/Audits/assembly-2026-10-23/unity/final/f84c9007d0494cd8a421d84d70d931a0/.../Receipts/. AlljobsSTOPPED/rootgateclosed; NOUnity/compiler/tests/checkers beforeallwritersfinish/newfreeze; nootherowners/config/children.

## Snapshot3 actual RPGNETWORK-NATIVE-GRAPH-AND-WIRE

ROOT has closed source3 execution after every owned native job stopped. SAME original actor only, gpt-6.1-sol/max. No Unity/compiler/checkers/tests until ALL source writers finish and root opens a NEW freeze. Root personally read all98 graphical Edit failure-message groups and all43 Play groups; Local/full-play-failure-triage.md (70 lines) and critical-failure-triage.md (35 lines) are independently reviewed. Actual XML paths are Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (7c6d24e1...) and21-full-play-graphics.xml (2b152164...). Read your exact cases/messages/stacks and actual runtime/vendor APIs before editing. CENTRAL RESERVATION: primary Neo.Editor.Tests b03-04 alone owns ALL Editor-only native helper moves/extractions, new Neo.Tests.Fixtures, shared test asmdefs, and its owned legacy test adaptations. Do NOT move/extract Mono helpers, edit shared asmdefs, or race any file in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. If the map is not yet present, work only the explicitly disjoint runtime/Play/new helper-free fixtures below; coordinate any caller containing embedded helpers before changing it. Preserve every substantive positive/negative/count/payload/ownership oracle and all public/serialized APIs/settings. Do not make production changes to hide a fixture precondition, add ExecuteAlways, blanket Ignore/error whitelists, manual Update/mocks as native evidence, reflection into private UserCode as receive proof, or weaker comparisons without actual native-baseline/numeric evidence. No other owners/vendor assets/314 migration targets, no children/commits/push; release Samples~ and protected DocsEn.meta untouched. Update own honest implementation receipt and canonical docs only if behavior/contract actually changes; report unresolved native capabilities explicitly. Stop all source writes when done. Own Play RPGNetworkRegressionTests:8 identity NRE cases (AutoLoad false/true, ClientStart, SnapshotV2). CreateCharacter409–417 currently adds adapter after active NetworkIdentity.Awake. Compose identity/character/adapter inactive, configure actual template/intended flag, activate ONCE, assert adapter.netIdentity is exact object identity before spawn/client start. Keep deliberately inactive first-init source branch separate; preserve ALL snapshot resources/stats/progression/buff/no-grant-replay/callback/intermediate payload/refresh assertions. Do not call internal Awake twice/set netIdentity by reflection/add production identity fallback. DispatchCommand458 also leaves6 bytes: actual weaver VarInt vs fixed WriteInt on Type/Integer fields451/454; use exact native registered serializer, preserve Remaining0, trusted-toggle/disabled/rate controls. Read independently reviewed critical/full Play triage. No runtime contract/schema change is justified by these setups.
