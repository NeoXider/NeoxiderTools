# Neo.HtmlToUitk.Editor — implementation receipt

Status: **SOURCE-FINAL; protected-setter regression sources corrected after the first failed frozen compile. Repeat compilation/execution awaits the new global writer barrier.**

Target: approved `demo` / `11.0.0`. Same original auditor/session, `gpt-6.1-sol`, reasoning `max`. All four assigned bounded actions and the accepted NI23-01/P2 follow-up have source, regression-source and owned documentation changes. There is no pending cross-owner API needed to finish this source batch. This receipt does not claim compilation or test success.

The completed audit and individual plan remain historical and unchanged. Their existing path normalization, submission snapshot, completion-context, JSON preservation/root validation, rename and disposal corrections were retained. Centrally removed NUnit parallelization attributes were not reintroduced; shared-state fixtures require the exclusive verification runner and restore their hooks/preferences.

Paths below use `E = Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/` and `T = Assets/Neoxider/Tests/Edit/HtmlToUitk/`.

## Completed approved actions

### HTML-EDITOR-DIRTY-DOCUMENT

Changed `E/Window/HtmlToUitkWindow.cs`.

- Open, Reload, New and first JSON materialization from a regular job asset use one `TryReplaceDocument` guard. Existing JSON is parsed into a candidate before asking or committing replacement.
- Native decision order is Save / Cancel / Discard, with Cancel retaining the previous document/path/unsaved state. Invalid or unknown decisions also cancel. The object picker resets to the actual retained job after a cancelled selection.
- New/materialized JSON is written only after the decision and any requested old-document save succeeds. The private optional decision callback drives this same transition in fixtures without opening a native modal dialog.
- `TrySave(out error)` prepares a document copy and performs file/asset work before publishing the saved document or clearing unsaved flags. Failure returns an actionable note and leaves current document/path/flags in place. `SaveChanges` throws before calling the base implementation when saving fails, so it does not falsely clear unsaved state.
- Save followed by Reload reparses the just-saved current path, preventing the earlier candidate from overwriting newly saved edits. Existing submitted-run identity/output handling remains in place.

This is a focused private authoring correction, not an execution-session framework or a new serialized asset format. File/asset persistence is not advertised as a multi-artifact transaction: an IO/import/asset-save failure can occur after an earlier write succeeded, while the editor still retains the unsaved document for recovery.

### HTML-EDITOR-PROCESS-TIMEOUT

Changed `E/Bridge/CliProcessBridge.cs`.

- Seconds-to-milliseconds conversion uses `long` and saturates at `Int32.MaxValue` before process launch, preserving the existing five-second minimum.
- A post-start `finally` owns cleanup for timeout and exceptions. An active owned process is killed and termination is checked with the remaining cleanup budget; termination/inspection failures include explicit stderr reasons rather than being swallowed.
- stdout/stderr EOF completion is tracked separately. Process termination and stream drain share a two-second wait budget. The previous unbounded parameterless `WaitForExit` was removed. Incomplete reads are cancelled, and drain/cancellation failures make the result faulted with nonzero exit status.
- Successful process completion still preserves the complete JSON diagnostics and stderr through the existing report/result APIs.

The bounded waits do not promise control over arbitrary descendant process trees, an OS call that itself fails, or cancellation of an optional in-process plugin. Failure to confirm termination is reported; actual PID and pipe behavior is still unexecuted.

### HTML-EDITOR-REPORT-PROVENANCE

Changed `E/Logic/ReportReader.cs`.

- A valid diagnostics object from the current stdout wins before any file read, preserving the actual rules/messages/counts for ValidateOnly and refused conversion.
- ValidateOnly cannot reuse an output-folder report, even if that file has a recent/future timestamp. Missing/malformed current diagnostics produce a failure with stderr/context.
- Identical successful conversions work through stdout without requiring the CLI to rewrite identical `diagnostics.json` bytes.
- Legacy file-only non-validation fallback accepts only a readable recognized report with write time at or after run start. The two-second older-file allowance no longer applies. The public `FreshnessSlack` field is retained for source compatibility but no longer controls acceptance.
- File parse/read failures retain their reason. Unauthorized file access is also converted to a diagnostics failure/fallback reason.

Intentional 11.0 behavior: current stdout is authoritative; file-only validation runners and older-than-run files no longer inherit old diagnostics. Equal timestamps, coarse/remote clocks and concurrent external writers remain explicit legacy provenance limits. A CLI run-ID protocol remains optional; no CLI source/API change is required for this correction.

### HTML-EDITOR-WATCH-DEPENDENCIES

Changed `E/Window/HtmlToUitkWindow.cs`, `E/Import/GeneratedAssetPostprocessor.cs`, and added `E/Logic/JobInputPaths.cs` plus metadata.

- CSS/USS/partial text changes immediately rebuild active watch roots, as HTML entry changes already did.
- Import batches collect imported/deleted/moved/moved-from HTML/HTM/CSS/USS/job paths, normalizing and deduplicating each batch before publishing the existing `InputsChanged` event.
- Both native filesystem and project-import paths consult the same focused authored-input predicate. Unrelated files, removed dependencies and generated output are filtered before conversion/requeue.
- File/folder/glob roots resolve canonically, including parent segments. Missing authored directories fall back to an existing ancestor so later file creation can be observed. Existing debounce/disposal/rename handling is reused.

Additive public API, namespace `Neo.HtmlToUitk.Tooling`:

```csharp
bool JobInputPaths.IsInput(JobDocument document, string baseDirectory, string path);
IReadOnlyList<string> JobInputPaths.GetWatchDirectories(
    JobDocument document, string baseDirectory, IFileView files);
```

The helper uses existing `JobDocument`, `IFileView`, `InputExpander`, `GlobMatcher` and framework path APIs. It adds no Unity/runtime dependency or asmdef reference and is included by existing pure-logic source globs. It covers authored HTML/CSS/USS/partial lists; converter-config/include graph discovery is not implemented. The broad dependency index/session proposal remains a proposal.

### NI23-01 / P2 — addressed regular-job persistence

Fully read `NativeImplementationReview-23.md`, lines 1–121, and actual `TrySave`, lines 512–549. Replaced its global `AssetDatabase.SaveAssets()` with `AssetDatabase.SaveAssetIfDirty(_jobAsset)`. JSON writing/import, prepared result publication, Undo, errors/unsaved state and regular versus imported job branches are otherwise unchanged. No framework, public API, serialized format or other owner's source was added or changed for this follow-up.

Extended only the existing `Editor/Audit20261023NeoHtmlToUitkEditorWindowReplacementTests.cs`: three authored cases cover an ordinary persistent regular-job save and dirty-foreign isolation through both public `SaveChanges` and the shared replacement Save choice. Each creates actual task-owned `.asset` job/config objects, explicitly saves baseline assets, captures their disk bytes and the JSON bytes, then saves edited job content. The isolation cases change/mark the independent foreign config dirty before the real command. Assertions require changed owned asset/JSON bytes, edited JSON content and owned job content after synchronous reimport, with foreign bytes unchanged and its changed in-memory value/dirty flag retained. Fixture setup uses addressed saves only and cleanup deletes only its GUID-owned assets. All seven existing replacement/cancel/discard/failure/recovery cases remain.

The owned editor guide now states the addressed-save behavior. This is **SOURCE-FINAL, UNEXECUTED**: native persistent-asset serialization, reimport and foreign dirty-state assertions still require the root's post-freeze Unity 6.3/6.6 execution. No Unity/compiler/tests/checkers/process probe/services/children/commits/push or shared-tracker edits were performed for NI23-01; actual evidence here is cmd.exe/Python source reads, scoped git status, the narrow source change, regression source and manual review.

### Frozen compile follow-up — protected unsaved-state setter

Root's first actual frozen compilation on both `6000.6.0f1` and `6000.3.14f1` failed with eight owned CS0272 errors. Read the supplied `Local/Audits/assembly-2026-10-23/unity/final/ff4e1aaceb2e4320a3f882f61c262b15/u6000_6_0f1/mirror-on-dev/Receipts/compiler-errors.json`: seven writes in `Editor/Audit20261023NeoHtmlToUitkEditorWindowReplacementTests.cs` and one cleanup write in `Editor/Audit20261023NeoHtmlToUitkEditorWatchDependenciesTests.cs` addressed the inaccessible native setter. Other owners' errors in that receipt were read only.

Changed only those two existing owned fixtures and this receipt for the compatibility follow-up:

- Replacement `Seed` now invokes the actual private `HtmlToUitkWindow.MarkDirty` through the existing asserted command-reflection helper, then asserts the public `hasUnsavedChanges` getter is true. That production command sets both the real document dirty state and native unsaved flag. The six direct test setter writes were removed; all later Save/Cancel/Discard, failed-save/recovery and native-unsaved assertions remain.
- Both fixture cleanups call the supported public `DiscardChanges()` before destroying/closing only their owned window. Read the actual installed SDK pages `Editor/Data/Documentation/en/ScriptReference/EditorWindow.DiscardChanges.html` and `EditorWindow-hasUnsavedChanges.html` under **both** `C:/Program Files/Unity/Hub/Editor/6000.6.0f1/` and `6000.3.14f1/`: `DiscardChanges()` is public and its base implementation resets the native unsaved flag. Current `HtmlToUitkWindow` has no discard override and its disable path only releases subscriptions/watchers. Cleanup intentionally discards fixture-owned edits without saving files/assets.
- The watch fixture additionally asserts that its real CSS text-field edit makes the native window unsaved. No protected-property reflection, private-field write, mock result, production setter/API, framework, shared configuration or metadata edit was introduced.

All **49 authored Editor cases** remain, including the three persistent addressed-save controls referenced by NI23/NI28: ordinary owned-job/JSON persistence and foreign on-disk bytes/dirty-state isolation through public SaveChanges and shared replacement Save. Their real persistence assertions are unchanged. This test API correction changes no public authoring contract, so the existing owned guide remains unchanged for this follow-up.

Actual work: contract/request, compiler-error artifact, installed SDK documentation and relevant source reads, focused source edits and manual reference review. This worker launched no Unity/compiler/test/checker/probe/service/install, child, commit or push. The earlier failed compile is recorded as a failure, not execution success. Both versions still require a new frozen compile and nonempty discovery/execution after **all** writers finish; these revised sources are **UNCOMPILED / UNEXECUTED**.

## Authored regression sources — UNEXECUTED

New uniquely prefixed fixture files:

| File under T | Observable acceptance authored |
|---|---|
| `PureLogic/Audit20261023NeoHtmlToUitkEditorReportProvenanceTests.cs` | Old PASS/current FAIL retains current rule/message/count; old FAIL/current PASS succeeds; validation without stdout cannot borrow a file; identical successful stdout does not rewrite the old file; file-only older fallback rejects and genuinely newer fallback still works. |
| `PureLogic/Audit20261023NeoHtmlToUitkEditorInputPathsTests.cs` | Edited external CSS replaces old root/policy; files/folders/globs and partials match; unrelated/unsupported/generated paths reject; parent-segment boundaries remain canonical; missing roots use an existing ancestor and duplicate roots collapse. |
| `Editor/Audit20261023NeoHtmlToUitkEditorWindowReplacementTests.cs` | Actual MarkDirty command establishes and asserts native unsaved state. Save/Cancel/Discard outcomes observed through actual file bytes and public unsaved state; no New file before Cancel/failed save; public close-save failure retains unsaved state; invalid candidate never prompts/saves; Save+Reload cannot regress saved edits. NI23-01 adds ordinary persistent owned-job save plus public SaveChanges/shared Save-choice controls requiring actual job/JSON persistence and unchanged foreign disk bytes/dirty state. Reflection only drives private editor commands/choice callbacks; assertions do not mirror private fields. |
| `Editor/Audit20261023NeoHtmlToUitkEditorWatchDependenciesTests.cs` | A real window CSS edit asserts native unsaved state and watches a new external directory; native file edit submits that CSS through IJobBridge; old/unrelated/generated writes submit no additional run; actual USS import and rename to TXT publish old input paths without per-batch duplicates. Window/hooks/preferences/watchers are cleaned up. |
| `Editor/Audit20261023NeoHtmlToUitkEditorProcessTests.cs` | Real fast probe succeeds with maximum/negative integer seconds and complete redirected diagnostics; hanging owned probe times out, returns failure and has no live matching PID/start-time identity. PID ambiguity is an evidence failure; cleanup never kills a reused PID with a different start time. |

Process fixture support, also authored but **not built or launched**:

- `T/Verification~/Audit20261023NeoHtmlToUitkEditorProcessProbe.cs`
- `T/Verification~/Audit20261023NeoHtmlToUitkEditorProcessProbe.csproj`

The standalone net9.0 console probe records PID/start time in the fixture's unique temp directory and either returns JSON or waits for termination. It has task-specific intermediate/extensions/output paths, no packages and no edits to existing verification projects. `Verification~` excludes the console entry point from Unity compilation. New package artifacts have generated GUID metadata; central package/GUID validation is still pending.

After **all writers finish**, the verification owner must build that task-owned project and set `NEO_HTMLTOUITK_PROCESS_PROBE` in the isolated Unity verification process to its absolute executable/DLL path. The DLL output is under `Verification~/bin/Audit20261023NeoHtmlToUitkEditorProcessProbe/net9.0/`. Without explicit configuration, these process cases report a skip, never a pass. No compiler, fixture, process probe or CLI was started by this worker.

## Documentation, ownership and compatibility

Updated owned `Assets/Neoxider/Docs/UI/HtmlToUitkEditor.md` for dirty choices, save errors, timeout/cleanup, stdout provenance, legacy limitations, additive dependency APIs, watch/import filtering and the post-barrier probe prerequisite. It retains C# submission and scene/runtime handoff guidance. The parent owner's canonical `HtmlToUitk.md` already links the editor and testing topics; that file was read only, not edited here.

No existing public method/serialized field/menu path was removed or renamed. Runtime assets, generated formats, scenes, prefabs and release `Samples~` layout are unchanged. Existing shared test fixtures/asmdefs, review trackers, manifest, package settings, historical audit/plan, DocsEn and other owners' code were not edited. Existing test assembly references and verification globs suffice for the authored source; no cross-owner additive API is pending.

## Actual work/evidence and next gate

Actually performed in this implementation phase: contract/request and relevant source reads, source/reference enumeration, focused edits, manual source/diff review, public-doc update, regression/probe-source authoring and GUID metadata bookkeeping. Small reads used cmd.exe/Python. No Unity/MCP, compiler, test runner, product checker/validator, installation/service, commit/push or child agent was launched by this worker. Root's first failed frozen compile and the ensuing source-only compatibility correction are recorded above. Earlier static audit results were not relabelled as verification of this implementation.

After the global source freeze, root's same verification operator owns:

1. Frozen-snapshot compilation on installed Unity 6.3 and 6.6, plus explicit unsupported-version limits for any other Unity 6.0–6.4 / 6.5+ branches. No new version-specific branch was added here.
2. Original and new nonempty EditMode fixture discovery/execution; process probe build/configuration and actual PID/stream/deadline evidence. Post-start wait/read/termination failure paths still need native fault/capability evidence beyond the authored timeout case.
3. Native Save/Cancel/Discard modal behavior, failed close-save, NI23-01 persistent own/foreign asset save controls, A→B completion isolation, external root changes, USS/move notifications and generated-output feedback/cadence. A thrown save failure must not let the native close flow discard the dirty editor document.
4. Package/docs/UTF-8/meta/GUID checks and .NET tool suite only after writers finish. No PlayMode change is introduced by this editor-only batch; runtime integration evidence remains with the runtime/verification owners.

Known deferred policies: window-close completion/cancellation service; arbitrary descendant-process ownership; robust run-ID provenance; symlink/case policy; full CLI schema/config/include dependency discovery. None is represented as implemented or verified. All assigned bounded corrections are source-final, with these verification gates explicit.
