# Neo.HtmlToUitk.Editor: approved implementation request

Original actor: `neo-audit-1023-b03-07`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### HTML-EDITOR-DIRTY-DOCUMENT (P1; approved-source-correction)

Load/Open/New/Reload replaces dirty doc without save decision. Shared Save/Discard/Cancel guard, candidate before replace, failed save/cancel retains current doc/path/dirty.

### HTML-EDITOR-PROCESS-TIMEOUT (P2; approved-source-correction)

TimeoutSeconds*1000 int overflow after process starts; widen/clamp before launch and bounded owned process cleanup for timeout/exception, no silent termination failures.

### HTML-EDITOR-REPORT-PROVENANCE (P2; approved-source-correction)

Stale file accepted within2s ahead of current stdout diagnostics. Prefer valid current stdout, ValidateOnly cannot reuse output file; preserve WriteIfChanged unchanged-success behavior, explicit legacy provenance limits.

### HTML-EDITOR-WATCH-DEPENDENCIES (P2; approved-source-correction)

Postprocessor misses movedFrom/USS; changing CSS/USS/partial list leaves old watcher roots. Dedupe old/new, rebuild edited roots, retain generated-output/unrelated filtering.

## Relevant full native sections

Source: NativeReview-C.md

### C-37 / P1 — opening/reloading a job discards dirty edits without a decision

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs:369–452`, LoadJson:427. Edit A, Open B/Reload/New, return A: LoadJson unconditionally replaces _doc and clears dirty/hasUnsavedChanges after parsing, with no Save/Discard/Cancel guard. Expected user choice before replacement; Cancel/failing save preserves document/path/dirty. Minimum common replacement guard and result-bearing internal save with candidate load before commit; New must not write its file before cancel decision. Existing queued run source-identity fix stays. Owner **Neo.HtmlToUitk.Editor / `neo-audit-1023-b03-07`**; current plan confirms. UI interactions unexecuted.
Source: NativeReview-C.md

### C-38 / P2 — CLI timeout overflows after process creation and error cleanup does not own termination

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Bridge/CliProcessBridge.cs:104–127` (timeout at 110), Run. Public TimeoutSeconds=int.MaxValue; int multiplication by1000 wraps negative before WaitForExit, causing argument failure after Start. Dispose closes Process handle but catch has no guaranteed termination of its running child; TryKill also swallows failures. Expected bounded validated wait and known cleanup outcome. Minimum long/clamped timeout calculation before launch; finally-safe bounded termination/drain on post-start failures, report inability to terminate. Source arithmetic/cleanup gap confirmed; actual PID lifetime requires controlled native probe after freeze. Owner **Neo.HtmlToUitk.Editor / `neo-audit-1023-b03-07`**. No session abstraction required for this fix.
Source: NativeReview-C.md

### C-39 / P2 — stale diagnostics file wins over the current run's stdout

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/ReportReader.cs:12–17,49–57`, FromRun/TryReadFile. Prior PASS diagnostics timestamp within2s, current ValidateOnly/refused conversion writes FAIL JSON stdout but no output report. Reader accepts prior file first and loses current rules/messages/counts. Converter source `D:/Git/NeoxiderTools/Tools/HtmlToUitk/src/HtmlToUitk.Convert/Jobs/JobRunner.cs:103` returns before report write, `Jobs/JobEntry.cs:124` emits current JSON. Expected current parsed stdout preferred, ValidateOnly never inherits old output report; file fallback needs explicit provenance. Keep legitimate WriteIfChanged/unchanged successful outputs valid; timestamp equality alone is not identity. Owner **Neo.HtmlToUitk.Editor / `neo-audit-1023-b03-07`**. Public regression old PASS/current FAIL and old FAIL/current PASS plus identical repeated success; new CLI run-ID protocol optional, not prerequisite.
Source: NativeReview-C.md

### C-40 / P2 — changed CSS/USS/partials roots and moved-from/USS imports are not watched

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs:302–319` MakeLines/Edit changes dependency lists without StartWatching (HTML entry edits at568 do restart); roots only assembled at734–752. Existing Watch, add CSS outside old directory, edit it: no filesystem subscription exists. `Editor/Import/GeneratedAssetPostprocessor.cs:79–108` collects imported/deleted/moved but omits movedFrom and .uss. Expected one relevant conversion for actual updated dependencies, old path removal and USS pass-through, no generated/unrelated loop. Minimum rebuild roots after dependency edits and collect/dedup both move paths/.uss before filtering through existing normalized output/dependency policy. Broader unified index is optional. Owner **Neo.HtmlToUitk.Editor / `neo-audit-1023-b03-07`**. Callback/source omission confirmed; native event cadence and feedback acceptance unexecuted.
Source: NativeReview-C.md

### Neo.HtmlToUitk.Editor — reviewed

Full audit/plan, all six changed diffs and affected runner/window/job/watcher bodies, current process/report/postprocessor and actual CLI provenance boundary, all three complete fixtures and new editor guide read. Canonical paths, submission-time deep copy/flags/list snapshot, submitted completion context, fail-before-write JSON, object-root rejection and old/new rename/disposal gates are coherent. C-37–C-40 remain required; no execution-session/DI framework prerequisite. Fifteen prepared cases observe real temporary/permanent bytes, captured bridge requests/options and directory handles/OS rename; bridge override restored, worker gate synchronized by events rather than race sleeps, watcher disposed before temp deletion. Blocking waits exist inside future tests only; no process/CLI/service ran here. Central native watcher/window/import verification still required.

Aggregate: filesystem events queue into one editor-update/debounce owner; source snapshots allocate once per submission and one global gate serializes conversion. Output normalization avoids generated feedback but dependency-root reconstruction still incomplete. Closing window removes polling while process can continue; explicit retained completion/cancel owner is optional broader session policy after mandatory save/timeout/report/watch fixes. Huge timeout source overflow is correctness, not speculative performance. Filesystem lexical path case/symlink policy remains documented limitation; do not alter global package/player behavior to fix editor tooling.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.HtmlToUitk.Editor.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Root accepted NI23 addressed regular job save

ROOT personally FULL-read all121lines NativeImplementationReview-23.md complete1–85/86–121 andactualTrySave512–549. Acceptsole NI23-01/P2. SAMEoriginalEditorowner replace global AssetDatabase.SaveAssets529 after_jobAssetonlymutation withaddressed AssetDatabase.SaveAssetIfDirty(_jobAsset). Preserve JSON/import/result/Undo/errors/unsavedstate/regularvsimportedjob semantics. Existingnewautomaticdirtyreplacement/nativeSaveChanges mustnotflushforeignprojectassets. Meaningful realpersistentownjob+foreignScriptableObject fixtures baselineactualdiskbytes, markforeignvaluechangeddirty, realSaveChanges/sharedSavechoice: ownedjob+JSONactuallypersist, foreignfilebytesunchanged/stilldirty. Ordinaryownedpositive+existingSaveCancelDiscard/failure/recovery retained. Ownexistingreplacementfixture+guide/receipt only; no newframework/publicAPI orotherownerfiles. NoUnity/compiler/tests/checkers beforeglobalfreeze.

## Actual Unity6.6 and6.3 protected unsaved setter in test source

FIRST ACTUAL frozen compile both Unity6000.6.0f1 and6000.3.14f1 failed identically: EditorWindow.hasUnsavedChanges setter inaccessible in owned WatchDependenciesTests104 +WindowReplacementTests34,55,75,96,113,134,181 (8errors). SAMEoriginalactor boundedtestsourcecompatibilityfix withrealunsavedstateoracles retained; no newproductionpublicsetter/framework. Rootread actual sealed HtmlToUitkWindow: existingprivateMarkDirty331–334 setsnativeflag andrealdirty; existingprivate-command reflection helper alreadypresent inownedfixture. Prefer arrange editedstate throughactual existingcommand; forcleanup useactualsupported publicDiscardChanges ifavailableandsemanticallycorrect orone assertedownedtest helper forprotected nativeproperty, readactualinstalledSDK API beforechoosing. DoNOTremovehasUnsavedChanges assertions/dropcases/changeSave/Cancel/Discard policy/mockresult/privateflags solelytoerasefailure. Preserveall49cases/NI28addressed-saveasset persisted controls. RootactualErrorsJSON Local/Audits/assembly-2026-10-23/unity/final/ff4e1aaceb2e4320a3f882f61c262b15/u6000_6_0f1/mirror-on-dev/Receipts/compiler-errors.json. BothjobsSTOPPED/rootgateclosed; noUnity/compiler/tests/checkers untilALLwritersfinish/newfreeze. Onlyownedtwofixtures/supportifneeded+ownreceipt/docsifcontractchanged, nootherowners/sharedconfig/metas/children.
