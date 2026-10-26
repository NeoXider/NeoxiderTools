# Neo.Tools.Managers: approved implementation request

Original actor: `neo-audit-1023-b07-06`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Implement transition-generation stale broadcast suppression; coordinate GM pause ordering with UI, no general pause platform.

## Assigned review actions

### MANAGERS-STALE-STATE-BROADCAST (P2; approved-source-correction)

B module disposition: OnStateChange(Pause) calls Resume, then outer EM.Pause publishes obsolete Pause. Transition generation suppresses stale old broadcasts and repeated nested state payloads.

## Relevant full native sections

Source: NativeReview-B.md

### Neo.Tools.Managers — full artifacts, all four source diffs/current ownership code and three complete fixtures reviewed

Owner/task `neo-audit-1023-b07-06`. Guarded Bootstrap drains preserve batch order and re-check registration/Unity lifetime; local finally releases the drain guard. GM captures/restores the exact owned time scale before state notifications and on destruction, without changing event-only pause; duplicate Pause assignment no longer erases the resume target. Singleton filters explicitly doomed instances, retains cache-only observation and applies persistence at lazy initialization/Awake. Serialized/public declarations remain intact. Tests inspect traces, state/time, actual DDOL ownership and deferred destruction; protected seams do not certify real GM destruction delivery. Unique singleton probe isolates the Play fixture; time-scale and created objects are restored, while Edit EM probes still require the normal isolated test world.

Aggregate review: optional Bootstrap scene discovery and singleton miss resolution are one-time/fallback scans; cached misses avoid repeated absent-singleton scans. Bootstrap Contains checks plus sorting/scanning each batch can become quadratic for large registration streams, and unlimited callback-generated registrations can retain the synchronous drain. Budgeted queue/recovery is an opt-in future proposal, with the plan correctly preserving the legacy retry behavior. No per-frame physics query loop exists. Reflection reset sweeps/IL2CPP and reload-disabled keyed registry repopulation remain needs-evidence risks, not presumed passed branches.

**Additional state-notification question for coordinator/original owner:** current GM.Pause sets State, then unconditionally EM.Pause. An OnStateChange(Pause) listener that calls Resume yields Resume notification followed by a stale Pause notification while State is Game. The new PauseStateListener regression checks final State/time but not this event trace. A consumer driving audio/UI from pause/resume may therefore end paused. Exact source sequence is confirmed (`GM.cs:202–203`), but the plan explicitly leaves arbitrary nested-transition ordering outside its guarantee. Decide a minimal transition-version fence/abandon-old-broadcast policy and add event-trace assertions before claiming broader reentry semantics; do not silently introduce a global pause arbiter or reject the narrower time-restoration fix.
Source: NativeReview-C.md

### C-20 / P1–P2 — PausePage and GM have conflicting snapshot ownership

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/PausePage.cs:54–88`, `OnEnable/OnDisable`; current `Tools/Managers/GM.cs:190–233`. Default page opens at scale1, writes0, GM.Pause then saves0; close page restores1, GM.Resume restores0. Expected a single ordinary pause returns original nonzero time and resume observers see it. Minimum capture/order reconciliation with the actual final GM adapter, preserving its corrected event-before/after state contracts. P2 same source unconditionally calls current GM.I.Resume even if that GM was already paused or a different instance replaced it: retain actual acquired GM and whether this page performed its transition; release only its own pause.

Separate confirmed overlapping-pages case: A/B capture1/0; closing A first resumes prematurely, closing B last restores0. Reordering local calls cannot fix arbitrary owners. Use existing shared pause owner/root as immediate documented policy or a scoped common ownership registry; a full cross-module lease/cursor platform is optional, not prerequisite for single-page/foreign-GM fixes. Owner **Neo.UI / `neo-audit-1023-b08-07`**, coordinate **Neo.Tools.Managers / `neo-audit-1023-b07-06`**, preserve serialized defaults and GM public methods. Test baselines1/0.35, existing GM pause, replacement manager and both closure orders when claiming overlap support. No runtime execution here.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Tools.Managers.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Exact GM version handoff to UI

Root personally read full current131line Neo.UI implementation receipt. IMPORTANT exact pending consumer contract: Neo.UI PausePage requires public ulong TransitionVersion { get; } on Neo.Tools.GM, incremented once BEFORE each effective State transition and BEFORE callbacks, normal Pause() advances exactly unchecked(previous+1UL), reentrant transitions advance further, no rewind. You already own MANAGERS-STALE-STATE-BROADCAST monotonic transition fence; publish this compatible read-only API from that same version rather than separate counters. Preserve State setter/public/signatures, no callbacks on no-op behavior beyond existing accepted contract, captured exact manager/time baseline. Keep your remaining approved work/tests/docs. Publish API early and source-final receipt. Neo.UI is independently finished, waiting for this exact property. No execution/children/sharedconfig edits.

## Actual native NeoxiderPages prefab import integrity

SAME original b07-06/session6.1sol/max, existingPM/UIPage parentowner. RootFULL-read actualba515nativeDiagnostic88lines6203de2d... bothnatural6.3importsfound7NeoxiderPagesexampleprefabbrokenlocalPPtrs: PageLoad,Game,Lose,Pause,Privacy,Win,End. Source3frozen/fullmetadata logsretainedunderLocal/.../unity/m/f169d9f1/{c,l}/Receipts/Editor.log, exactmessages/IDsinthecontrollerdiagnostic-results.json. Ownbounded sourceONLY Assets/Neoxider/Samples~/NeoxiderPages/**/*.prefab +ownManagersreceipt, optionalcanonicalPages/PM guideifcontractreallychanges. ReadrootrawPageLoad201-295: Loading_1 componentlistpoints to7970048788368920216 and5822085909133773776 withNO matching !u!document; nativeimportdropsduplicates/missingentries. InspectALLreleaseNeoxiderPagesprefabm_Component localrefs, repairONLY demonstrablydangling/duplicatelocalcomponententries; preserveeveryvalidcomponent/YAMLlocalID/GUID/hierarchy/RectTransform/material/sprite/publicfield/event/wiring. No blanket removevalidscripts or assetreserialization/manualversionbump. Also concreteoptionalPro defect: valid m_Script{fileID40306178,guid aa0b1eebb5db27a419fa4564bbe5c9c5} isDOTweenPro.dll (actualmeta Assets/Plugins/Demigiant/DOTweenPro/DOTweenPro.dll.meta), remainsin7prefabs incl_Pagebase/Shop/Load/End/Lose/Pause/Win despitecanonicalREADMEclaimNeoxiderPagesimportswithoutPro. Determineexacttype/behaviourfromlocalvendorAPI/XML/metadata/source. IfinertVisualManageronlycontrolsnonexistentanimationdrivers andno externalrefs, removeTHATinertProrecord+componententry deliberately preservingactualbehaviour; ifactiveanimationfunctionexists reuseexistingNeo/coreDOTweenpath preservingconfiguredtiming/events orreportpreciseboundedhandofftoRootbeforeunsafechange, don'tsilentlystripactiveeffect. No inventednewframework.No duplicateIDs/no unresolvedm_Component aftercorrection bysourceenumeration, butnativeadmissionstillUNEXECUTEDuntilnewfreeze. DoNOTtouchAssets/Samplesoldimports/ThirdParty/vendorDLLs/metas/GUIDs/runtimeAPI/otherowners/sharedrefs. NoUnity/compiler/tests/checkers/productvalidation/processcontrol/install/children/commit/push whilewriterswork. NewfullnativeUPM3sample/noPro/nativePrefabloadacceptanceRootlater. Updateownhonestreceipt exactchangedpaths/hash/localIDretention/Prodependencydecision/remainingnativeproof andstop. MainSamples~layout/protectedDocsEn.meta untouched. Thisisnewactualnativeassetdefect, previoussourceclosureishistorical.
