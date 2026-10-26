# Neo.Tools.Text: approved implementation request

Original actor: `neo-audit-1023-b08-04`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Publish exact additive typed-integer final API and protected virtual OnEnable/OnDisable hooks early in implementation receipt for Level/Components/Shop. Preserve existing signatures and float currentNum/intermediate behavior; formatting-only redraw retains typed final value.

## Assigned review actions

### TEXT-INTEGER-PRECISION (P2; approved-source-correction)

Set(int) converts exact integer+offset to float, losing 16777217; preserve exact final display through existing BigInteger formatting. Coordinate TextLevel use.

### TEXT-LIFECYCLE-HOOKS (P2; approved-source-correction)

Derived private TextLevel.OnEnable/OnDisable may hide updated SetText lifecycle cleanup/resume. Confirm and coordinate base hooks without duplicate listeners.

### TEXT-SCORE-LIFECYCLE (P2; approved-source-correction)

TextScore derived callbacks hide updated private SetText callbacks; compose protected hooks/base calls and preserve reactive subscription ownership.

### TEXT-MODE-ATOMICITY (P2; approved-source-correction)

DisplayMode assigns Clock before large Compact current time rejected redraw. Validate proposed mode before mutation; rejected switch retains mode/time/text/events, accepted90sec formats Clock without time event.

## Relevant full native sections

Source: NativeReview-C.md

### C-10 / P2 — TimeToText commits a mode that cannot display its current value

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs:148–155` (`DisplayMode`) writes `_displayMode` then redraws; `Set:245` rejects the value against the new mode. Compact/dynamic `Set(3000000000f)` then `DisplayMode = Clock` leaves Clock state with old Compact text/time. Expected rejected transition retains mode/time/text and publishes no time event; after Set(90), transition succeeds as 01:30. Validate proposed mode before commit, preserving existing formatter and rejecting rather than clamping accepted time. Owner **Neo.Tools.Text / `neo-audit-1023-b08-04`**; already openly pending D1 in plan, independently confirmed. Shared duration-core API is optional.
Source: NativeReview-C.md

### C-12 / P2 — integer level display drops valid digits through float

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Text/SetText.cs:253–261` and `Level/TextLevel.cs:97–106`. Current wider addition fixes overflow but `(float)((long)value + indexOffset)` loses unit precision; level 16777216 plus offset one cannot finish as 16777217. Expected exact final integer text with existing number options/affixes; interpolation may remain float if final commit retains exact typed value. Reuse `SetBigInteger`/existing ToPrettyString, no second formatter or deep new service. Text owner **`neo-audit-1023-b08-04`** establishes additive exact final integer path (and explicit animation/currentNum approximation); Level owner **`neo-audit-1023-b04-02`** integrates it. Keep mutable formatting updates exact too. Document any animation contract decision and test grouped/plain output plus both signed/offset limits. This is an existing documented limitation requiring a deliberate 11.0 contract correction, not a claim that the earlier widening patch failed at its stated purpose.
Source: NativeReview-C.md

### C-13 / P2 — TextLevel hides the new SetText tween lifecycle messages

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/TextLevel.cs:32–51` declares private OnEnable/OnDisable; base `Tools/Text/SetText.cs:29–43` also declares private messages and owns tween pause/resume. There is no base invocation/overridable hook. Derived-label dispatch therefore cannot reliably honor the base's advertised enabled-state lifecycle; component-only disable during an active tween can keep writing while disabled, and a deferred tween can remain paused on re-enable. Source message collision is confirmed; exact Unity dispatch/output is a required engine regression, not an executed result. Minimum repair: protected virtual base messages/hooks and matching TextLevel overrides calling base, preserving listener/wait cleanup and ordering. Owners **Text `neo-audit-1023-b08-04` → Level `neo-audit-1023-b04-02`**. Test active and queued tween on the derived type; plain SetText tests do not cover it.
Source: NativeReview-C.md

### Neo.Tools.Text — reviewed

Full audit/plan, both complete changed source diffs, both complete new fixtures and all three factual guide diffs read; related formatter boundaries/SetBigInteger examined. Immediate replacement/cancellation, retained formatting, event baseline before reentry and finite/display range guards are coherent. C-10 remains; C-12/C-13 require cross-owner coordination. Thirty-one cases assert exact text, retained numbers, event histories and real enter/exit-Play lifecycle. Teardown kills owned tween, destroys hierarchy and exits Play; defaultUpdateType is restored in finally. ManualUpdate is global: execution must use an isolated test context, not assume unrelated tweens are harmless. Format-only redraw can notify text by design, but time events deduplicate. No Update/scene scan/Physics; per-number tween and numeric-format delegate allocations follow requests. Pure duration Try API is optional reuse of Extensions rather than a parallel formatter.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Tools.Text.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Snapshot3 actual TEXT-NATIVE-REPLACEMENT-TWEEN

SAME original source actor, gpt-6.1-sol/max. Root closed execution after all owned Unity jobs stopped. NO compiler/Unity/checkers/tests until ALL writers finish and a new global freeze. Root personally read all98 graphical Edit failure messages/counts and all43 Play groups, independent full-edit-failure-triage.md and full-play-failure-triage.md. Read exact assigned cases in Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (SHA7c6d24e1...) and21-full-play-graphics.xml (SHA2b152164...) plus Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json. PRIMARY b03-04 exclusively owns ALL helper moves/extractions, Fixtures assembly/shared refs and files in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. Check map before every edit. Do not race/extract helpers or change reserved fixture callers; work only disjoint source now, explicitly report deferred reserved work for root to resume after release. Preserve public/serialized API/settings/GUIDs and EVERY substantive positive/negative/count/ownership/payload oracle. Establish a real positive precondition before inferring runtime defect. Native mode failures cannot be hidden with ExecuteAlways, manual Update/private lifecycle as native evidence, blanket Ignore, expected-error whitelist, arbitrary tolerance/default change, reflection UserCode bypass or lost cases. Use supported TOP-LEVEL UnitySetUp/UnityTearDown/UnityTest Enter/ExitPlayMode for true lifecycle, or existing public API for explicit pure contract tests; nested ExitPlayMode is unsupported. No user/foreign scenes/providers/sessions may be reset/deleted; only owned test resources. No other owners/shared asmdefs/vendor314 migration targets, no children/commits/push. Update own canonical docs if actual contract changes and own honest implementation receipt; no test pass inferred from source. Stop writes when done; name unresolved native evidence. Six own SetText failures: hidden-parent animation never advances, immediate replacement has stale later update count3 vs2, integer completion float replacement expected9.5 actual10, disabled set remainsidle. Establish actual component/tween/Play and time/update baselines, then fix bounded genuine replacement generation/scheduling fault if demonstrated. Preserve OnInt/Float ordering, exact replacement formatted text, retained isInteger/format serialized setting meaning, no stale later writes, hidden pause/reenable and destroy behavior. No broad rounding/default change or weaker event count.

## P5 actual superseding numeric tween ownership

Read Local/Audits/assembly-2026-10-23/p5-source-correction-common.md. SAME b08-04, E25. Actual mid-tween replacement cases267/270 see TWO active target tweens at Animate424, before9.5/16777219 final endpoint checks, both engines. StopAnimation50-57 callsKill; inspect actual owner/active/kill/query semantics and lifecycle competing writes from native setup to distinguish real lifetime bug from DOTween pending/query bookkeeping. Preserve exactly one OWNED active tween, original float endpoint/integer formatting and late retired-tween guard, all old cases/callback payloads. No arbitrary kill-all/global ManualUpdate/assertion-count weakening or simply ignoring a real active tween. Coordinate Level b04-02 if shared SetText cause; do not overlap other's files. Own runtime/fixtures/docs/receipt; root shared Migration/CHANGELOG only by handoff. No execution.

## P5 E25 exact diagnostic-source handoff readiness

SAME original b08-04/session6.1sol/max, RECEIPT-ONLY consistency/handoff task. Root FULLread your122linecomplete receipt delta41d932426292d67f96671c1b83a73cdb01ae85963621b46c15d51d5737b64570 and independent NI71 a2d5c6d0ea8a5bb02145758230998c10108d4db93c4a41eb2b026aad1222935d. Root ACCEPTS your finished bounded diagnostic-source delivery, NOT an E25 runtime fix. Next frozen execution will FIRST run exact strengthened E25 cases/retained51matrix and collect retiredidentity/kill/rawactive-entry payloads BEFORE choosinganyproductionfix/fullrerun. Update ONLY own implementation receipt first status/handoff to state explicitly SOURCE-FINAL FOR DIAGNOSTIC FIXTURE DELIVERY ONLY; E25 ownership failure OPEN/native diagnosis REQUIRED/no runtime fix or pass. Preserve all historicalfailureevidence/pins/nativeoracles and state all owner writes finished/no source/API dependency apartfromactualnativeE25evidence. Do NOT modify runtime/fixture/guide or addplans/performtests/Unity/compiler/checkers/process/newagents/git. Do not pretendfailureclosed, merely separate finishedsourceauthoring frompendingnativeverdict. Send current receiptSHA and STOP. Rootowns nextfreeze/execution and later SAME-sessionactualfixifdiagnosticsproveone.

## P6 actual Text native Play initialization prerequisite

SAME original b08-04/session gpt6.1sol/max, SOURCE ONLY after Root closes P6 execution/all jobs stop. Read Local/Audits/assembly-2026-10-23/p6-text-native-context-assessment.md SHA038e28f0c4d2a7eb2d8f871d1d6ed22c50cdb2a85293a09e3fb15074c933a2fb and actual bothSDK p6 Text51 XML: u63 18d4a4957f040d418762749d073c16880e89d9deec90d06890ba2279147322fa, u66 c02b19885a08a7fa29c3f2c935371946dd03f17f5dc6b1f9c150449bbcdb9588. Both51=40P11F; float/integer rawentries2 DISTINCT2, retiredactiveTRUE,7immediate+string/clearretiredactive. Actualkillcount unreached; don'tclaim0 or completedfix. DLL8e90Init/InitCheckREFUSEuninitializednonPlay; Kill06000085returnsifinitializedfalse; ManualUpdate/ApplyTo cancontinue. Current33SetTextcases plainSetUp/TestCaseoutsidePlay have no nativeSDKinit prerequisite;2genuinePlaylifecyclecasesPASS. CompiledNeo.Tools.TextPDBbindsexactc5b01runtime, no staleassembly theory. Correct ONLY fixture native context FIRST: supportedtop-level UnitySetUp/UnityTearDown acquiring OWN genuinePlay session, successfulPUBLIC DOTween.Init()/active native driver admission before creating/animatingview; reject borrowedalready-runningPlay and exitonly acquiredsession evenpartialsetupfailure. Adapt2existinglifecyclemethods tosameownedcontext withoutnested/duplicateEnterPlayMode, keepactualnativeenable/disable/parent/destroyoracles. PreserveALL33SetText+18TimeToText=51identities/parameters,7immediate+4superseding, strictretiredinactive/kill1/rawactiveentry1/nondedup/differentnonrecyclablereplacement/exact9.5and16777219/eventpayload/laterstablevalues, no ManualUpdateflushbeforecount. ManualclockmayremainONLYforownedtweeninrealPlay aftersuccessfulSDKinit. NO privateinitializedoverride/globalclockreset/globaltweenkill/EditorPreviewcallbackclearing/assertweakening/speculativeproductionKillchange. Runtime/TimeToText/pureformattertests unchanged unless newactualnativeevidenceprovesseparatedefect; currentstepprerequisitefixture notruntimefix. Ownfixture+ownreceipt, canonicalguideonly usefulclarificationofPlay/runtime animationcontract. PreserveallmetadataGUIDs/sharedassemblies/otherowners/userDocsEn; no newfixtureframework/helpermigrations. No Unity/compiler/tests/productcheckers/processcontrol/install/children/git. Finishsource-finalexacthashes/statusnativeacceptancePENDING, no implementationdependencyexceptnewfrozenbothSDKverdict, STOP. Root/nativeBreviewthennewfreeze. Historical11failures remain raw; do notclaimall11fixedfromsource.
