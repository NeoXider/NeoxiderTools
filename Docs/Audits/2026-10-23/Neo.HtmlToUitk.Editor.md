# Neo.HtmlToUitk.Editor: audit

Status: source audit and scoped fixes complete; central compilation and native tests pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Neo.HtmlToUitk.Editor.asmdef`.
- Scope: 22 C# files; nested assemblies excluded.
- Changed: six source files, three new regression files, the assigned editor guide and this report. New package files have GUID metadata.
- No commits, shared config/asmdef edits, Unity/MCP operations, builds/test runners, installs or child agents.
- Кратко: прочитаны все 22 файла; исправлены потеря JSON-настроек, несовпадение путей Watch, гонка данных запуска, обработка завершения другого задания и ошибки владения watcher. Проверки Unity остаются у координатора.

## Scope

- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Bridge/CliProcessBridge.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Bridge/HtmlToUitkRunner.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Bridge/HtmlToUitkSettings.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Bridge/InProcessBridge.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Import/GeneratedAssetPostprocessor.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Jobs/HtmlToUitkConfig.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Jobs/HtmlToUitkJob.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Jobs/HtmlToUitkJobEditor.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Jobs/HtmlToUitkJobImporter.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/CliCommandLine.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/DebounceGate.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/DiagnosticsReport.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/GeneratedTextureManifest.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/GlobMatcher.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/IJobBridge.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/InputExpander.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/JobDocument.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Logic/ReportReader.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Watch/HtmlToUitkWatcher.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/FindingsView.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/InputListView.cs`

## Findings and fixes

### Complete source review

Every C# file in the scope above was read completely. The manifest was checked against the directory: 22/22 files, no additional nested assembly under this root. Paths in the table are relative to the owned `Editor/` directory.

| File | Review evidence and outcome |
|---|---|
| `Bridge/CliProcessBridge.cs` | Executable/DLL/PATH lookup, quoting, stream locks, timeout/kill/disposal, report validation. No edits. External process integration remains unexecuted; extreme timeouts/cancellation need the proposal below. |
| `Bridge/HtmlToUitkRunner.cs` | Selection/fallback, gate, temporary job cleanup, subset conversion, batch exit and import gating. Fixed mutable queued submissions and job/output paths. |
| `Bridge/HtmlToUitkSettings.cs` | Preference defaults, CLI precedence, Unity-only path access, file view and project boundaries. Fixed normalization and exact Assets/Packages roots. |
| `Bridge/InProcessBridge.cs` | Optional discovery/reset, expected type/signature, exception/stdout capture and fallback. No edits; plugin invocation not verified. |
| `Import/GeneratedAssetPostprocessor.cs` | Manifest proof before config narrowing; hash/signature delegation; importer marker, event and cache lifetimes. No edits. Existing one-time marker behavior is documented. |
| `Jobs/HtmlToUitkConfig.cs` | Project-asset references, folder policy, deterministic default discovery and typed PanelSettings overrides. No scene references added. No edits. |
| `Jobs/HtmlToUitkJob.cs` | Serialization/public fields, JSON source/path choice, extension-key retention, save/load and optional JSON text. Fixed silent loss of malformed settings. |
| `Jobs/HtmlToUitkJobEditor.cs` | Imported/editable inspector, Undo/dirty, save/load status and refresh. No edits; now receives meaningful SaveJson errors. |
| `Jobs/HtmlToUitkJobImporter.cs` | Multi-part suffix, imported object ownership, malformed-input diagnostics and inspector. No edits; non-object roots now enter its existing error path. |
| `Logic/CliCommandLine.cs` | Flags, Windows quoting/backslashes and noisy output extraction. No edits; existing quoting tests inspected. Expects one report object, not arbitrary JSON streams. |
| `Logic/DebounceGate.cs` | Quiet period, deduplication, one-shot firing and Clear. No edits; existing debounce tests inspected. |
| `Logic/DiagnosticsReport.cs` | Verdict validation, counts, severity, normalized file grouping/status. No edits. Mutable public findings and supplied-count consistency remain model limits. |
| `Logic/GeneratedTextureManifest.cs` | Schema, exact paths/boundaries, texture policy, signature, positive sizes, size bound, SHA256 and stream ownership. No edits; existing ownership tests inspected. |
| `Logic/GlobMatcher.cs` | Literal escaping, segment/recursive wildcards and static prefixes. No edits; null patterns are unsupported inputs. |
| `Logic/IJobBridge.cs` | Request/result contracts, success gating and exit/verdict reconciliation. No edits; existing contradictory-result tests inspected. |
| `Logic/InputExpander.cs` | Files/folders/globs, filtering, deterministic deduplication, absolute inputs, blanks and file-view seam. No edits; collections are expected non-null. |
| `Logic/JobDocument.cs` | Typed fields, unknown keys, schema output, optional blocks and shape checks. Fixed non-object roots. Full CLI schema validation is not implemented. |
| `Logic/ReportReader.cs` | File/stdout fallback, freshness slack, malformed/missing reports and bounded error tail. No edits; per-run report identity is proposed below. |
| `Watch/HtmlToUitkWatcher.cs` | Queue/debounce, rename filtering, reload/unsubscribe and handles. Fixed rename-away, post-disposal allocation and late callbacks. |
| `Window/FindingsView.cs` | Selection/severity filters, sorting, row reuse, details and source/related opening. No edits. |
| `Window/HtmlToUitkWindow.cs` | All 804 original lines: serialized path, GUI lifecycle, dirty/save/load, config, async completion, watch/output suppression and opening files. Fixed completion context and path suppression. |
| `Window/InputListView.cs` | Row reuse, status/selection, add/remove, drag/drop and callback capture. No edits; consumes existing non-null collection/expansion contracts. |

Context read: full worker contract and exact manifest entry; editor/test asmdefs; canonical module guide; existing `EditorLogicTests`, `OwnershipLogicTests`, `JobAssetEditModeTests`, `RunnerEditModeTests`; runtime JSON value/parser contract; CLI `JobLoader`; verification project compile globs. None of those other owners' files were edited. The Neoxider extensions reference has no matching path/JSON/process helper; existing MiniJson, InputExpander, IJobBridge and framework APIs were reused.

### F1 — P1: parent segments defeat output exclusion and path boundaries

- Trigger: a job in `Jobs/` outputs to `../Assets/UI/Generated`, inside a watched tree.
- Before: `OutputRootAbsolute` preserved `Jobs/../Assets/...`; notifications used canonical `Assets/...`. `IsUnderOutput` accepted generated USS as an input event and could repeatedly launch conversion. Project-relative mapping also retained dot segments; `Assets/../Outside` could be classified as inside Assets.
- Source: `HtmlToUitkRunner.OutputRootAbsolute`, `HtmlToUitkPaths.ToAbsolute` / `ToProjectRelative` / `IsInsideAssets`, `HtmlToUitkWindow.IsUnderOutput`.
- Fix: normalize before comparisons; return canonical absolute output and job paths; handle exact Assets/Packages roots without accepting siblings. RunAsync resolves its job path on submission, avoiding Unity project-path access in the worker for relative submissions. Relative CLI working/job paths no longer risk resolving the job directory twice.
- Regression: `ParentSegmentsResolveToTheSameOutputAndProjectAssetPath` observes public path/boundary results. `ProjectRelativeSubmissionGivesTheBridgeAbsolutePaths` observes the real request/document through IJobBridge.
- Executed evidence: PowerShell using `System.IO.Path.GetFullPath` reproduced the raw output/event exclusion as false and normalized exclusion as true. This is framework path evidence, not execution of the Unity fixture.

### F2 — P1: queued async work observes later caller edits

- Trigger: one run holds the gate; submit another RunAsync, then mutate its document, nested JSON, flags or selected-file list.
- Before: the worker closure retained live references and serialized only after acquiring the gate, using later edits or potentially enumerating a changing collection.
- Source: `HtmlToUitkRunner.RunAsync` / `Execute`.
- Fix: capture the document through the existing JSON round trip and copy flags/selection synchronously before Task.Run.
- Regression: `QueuedAsyncRunUsesTheDocumentAndOptionsAtSubmission` holds the actual gate with a blocking bridge, changes every relevant input after submission, then observes temporary job JSON, flags/output and cleanup through a recording bridge. It does not depend on a sleep-based race.

### F3 — P2: job A completion can overwrite job B's report and refresh the wrong output

- Trigger: start conversion, then open another job or edit the output root before completion.
- Before: `OnRunComplete` refreshed current `_doc` / `_jobPath` and displayed findings regardless of which job ran.
- Source: `HtmlToUitkWindow.Run` / `OnRunComplete`.
- Fix: retain submitted document/path and source identity; refresh the submitted output, apply findings only to the same source document, otherwise show a previous-job completion note.
- Validation: source/control-flow review only. The snapshot fixture protects the shared data contract but does not exercise the window interaction. Central UI verification is explicitly required below.

### F4 — P1: malformed Inspector JSON is silently dropped on Save

- Trigger: malformed `configOverridesJson` / `perFileJson`, or an array/scalar instead of an object.
- Before: `ParseOrNull` converted parse failure to null; document serialization omitted non-object values. Save could succeed while deleting conversion overrides or mappings.
- Source: `HtmlToUitkJob.ToDocument`, former `ParseOrNull`, existing `SaveJson` exception boundary.
- Fix: strict parse with object/null validation. Blank/null still clear optional blocks. ToDocument/ToJson throw JsonParseException; SaveJson returns false/error before opening the existing file for writing.
- Regression: four `InvalidInspectorJsonRefusesSaveAndPreservesTheExistingFile` cases assert failure, retained file bytes and retained Inspector text; `ExplicitNullInspectorJsonStillClearsOptionalOverrides` protects compatibility.

### F5 — P2: non-object JSON replaces a loaded job with empty defaults

- Trigger: load `[]`, `null`, a string, number or boolean as a job.
- Before: JsonValue.Members is empty for non-objects, so Parse returned an empty job; loading could replace the active document and a later save overwrite the input with defaults. CLI JobLoader already rejects these roots.
- Source: `JobDocument.Parse` and its load/import callers.
- Fix: reject with JsonParseException and reuse existing caller error handling.
- Regression: five `NonObjectJobCannotReplaceAnExistingDocument` cases observe rejection and retained previous name, HTML entries and output.

### F6 — P2: renaming HTML to an unsupported extension is invisible

- Trigger: rename `menu.html` to `menu.txt` in a watched directory.
- Before: OnRenamed accepted only the new path, then extension filtering discarded it. The old input removal did not enter the debounce callback.
- Source: `HtmlToUitkWatcher.OnRenamed` / `Accept`.
- Fix: check both old/new paths; register handlers before enabling notifications.
- Regression: `RenameAwayFromASupportedExtensionReportsTheRemovedInput` performs a real rename and observes the old path, absence of the unsupported new path, and file state. Native OS/editor execution is pending.

### F7 — P2: disposed watchers can allocate handles they can never dispose

- Trigger: retain a watcher and call Watch after Dispose.
- Before: new FileSystemWatchers were created after editor subscriptions were removed; subsequent idempotent Dispose returned early forever. A captured update callback could also consume late events.
- Source: `HtmlToUitkWatcher.Watch`, `Dispose`, `Accept`, `Poll`.
- Fix: reject reconfiguration after disposal; gate ingestion and polling on a volatile disposed flag. Double-dispose stays supported.
- Regression: `DisposedWatcherCannotAllocateNewDirectoryHandles` observes ObjectDisposedException and zero owned handles.

## Documentation

Created the exact assigned topic, [HtmlToUitkEditor.md](../../../Assets/Neoxider/Docs/UI/HtmlToUitkEditor.md), because it did not exist. It explains C# submission/refresh, snapshots, paths, JSON errors, imported versus editable jobs, window/Inspector authoring, bridge setup, watcher ownership, texture proof and the runtime scene handoff. It links the canonical module guide. Coordinator should add the reciprocal link from the other owner's `HtmlToUitk.md`.

New tests under the existing `Neo.HtmlToUitk.Tests.Editor` asmdef:

- `PureLogic/Audit20261023NeoHtmlToUitkEditorDocumentTests.cs`: five root-rejection cases.
- `Editor/Audit20261023NeoHtmlToUitkEditorPipelineTests.cs`: eight cases for paths, relative submission, file preservation, null compatibility and deterministic queued submission. Global bridge overrides are restored; the fixture is nonparallel.
- `Editor/Audit20261023NeoHtmlToUitkEditorWatcherTests.cs`: two native rename/disposal cases.

Total: three files, 15 declared cases. Existing verification globs include them. The existing asmdef already references runtime/editor assemblies, NUnit and both Unity test runners; no shared reference changes are needed. All four new package artifacts have unique GUID metadata.

## Substantial improvement proposal

### Explicit execution sessions and dependency ownership — not implemented

**Problem.** The static gate serializes work, but window-owned polling still controls completion/import. Closing the window releases watchers without cancelling the process or transferring completion ownership. There is no cancellation/run identity contract, dirty-document switching remains separate from execution state, and input changes can require rebuilding watcher roots. Global postprocessor events lack a per-job dependency index.

Concrete limits for this workstream:

- ReportReader's two-second freshness slack can admit a preceding report when the latest run writes none. Timestamp precision is not proof of run identity.
- CliProcessBridge multiplies the public integer timeout by 1000 before waiting. Extreme values can overflow; an exception after process start has no guaranteed termination path.
- GeneratedAssetPostprocessor does not collect movedFrom and omits USS. Normal watched filesystem events cover common cases, but editor dependency updates should not rely on a second notification channel.

**Architecture/public API.** Introduce an immutable, data-only `JobExecutionRequest`, created on the editor thread with canonical paths, deep input/options snapshots and a unique ID. `JobExecutionService.Submit(request)` returns `IJobExecutionSession : IDisposable`, exposing `Id`, `State`, `Task<JobRunResult> Completion` and `Cancel()`.

The service owns queueing, temporary files, bounded process lifetime and run-specific diagnostics. A small Unity editor adapter owns main-thread completion/import independently of a window. Closing explicitly cancels a session or transfers it to that adapter. Keep dirty/save decisions in one window-session adapter using Unity's save hooks.

Add `IJobDependencyIndex` for expanded HTML/CSS/USS/partial/config inputs and canonical output exclusions. Filesystem and postprocessor events consult the same index, including old/new move paths. Updating source/config entries rebuilds roots deliberately. Reuse existing file-view, JSON, debounce and bridge APIs; no new runtime module or general DI framework is needed.

**Compatibility/migration.** Keep Run, RunAsync, RunOptions, IJobBridge and serialized fields intact. Existing entry points delegate to the service with current defaults. Make cancellation an optional bridge capability, preserving custom IJobBridge implementations; mark legacy bridges non-cancellable where necessary. Report IDs need coordinated CLI support; retain a clearly identified legacy fallback. Runtime/player assemblies need no migration.

**Acceptance scenarios.**

1. Submitted state remains immutable; the new snapshot regressions continue to pass.
2. Cancelling queued work creates no temp file/process. Active cancellation/timeout confirms bounded termination, releases the gate, cleans temp files and performs no success import.
3. Window close/switch has an explicit tested completion policy; job A cannot modify job B's report/dirty state. Failed save retains unsaved state.
4. Consecutive runs in one timestamp tick cannot reuse diagnostics accidentally; a new failure cannot be hidden by an old PASS file.
5. Config/input edits update roots. Rename-away, moved-from and USS changes each trigger one relevant job; unrelated imports and generated output trigger none.
6. Extreme timeout values cannot overflow; process-start, stream, wait and termination errors leave no active owned process.

**Effort/risk.** Estimate 2–4 engineering days plus a central Unity/CLI validation pass. Medium risk: cancellation and report identity cross the CLI boundary. Stage service integration, dependency indexing, then report protocol support. Any new assembly references require coordinator review; this proposal does not change shared configuration.

## Verification and remaining risks

Actually executed:

- Manifest/on-disk inventory: 22/22 owned C# files, all reviewed.
- Framework path reproduction: raw Jobs/../Assets exclusion false; normalized exclusion true.
- Explicit-type scan: no var local declarations in owned/new C#; no direct obsolete instance-ID APIs found.
- Strict UTF-8 decoding, trailing whitespace and known malformed-encoding marker scans: passed for owned source/new tests/docs.
- New metadata presence, 32-hex GUID format and unique new GUIDs checked.
- Existing test asmdef references and pure/native verification compile globs inspected: fixtures are included without edits.
- Scoped `git diff --check` and final new-file static consistency: passed. No build or test command was run.

Not executed: Unity compilation on supported versions, NUnit/UnityTest fixtures, CLI/in-process execution, AssetDatabase import or window interaction. No new Unity version branches/obsolete API calls were added; this is not a compilation result.

Coordinator actions:

1. Compile and run the three new fixtures plus existing HtmlToUitk regressions centrally, including native filesystem notifications and supported Unity versions.
2. With a slow bridge, run A, open B and complete A: refresh A only, retain B's report and show the previous-job note. Also edit an output root during a run and confirm refresh uses the submitted root. This window fix has source evidence only.
3. Verify Watch with ../Assets output inside a watched tree: generated USS must not retrigger conversion; an authored input edit must trigger it.
4. Link the assigned new editor topic from the other owner's canonical module guide. No existing docs outside this entry were changed.

Remaining limits: lexical path normalization preserves existing case-insensitive policy and does not resolve symlinks; full CLI schema checks and defensive handling of null public collections remain outside this parser contract. Process cancellation/lifetime, same-tick report identity, narrow dependency tracking and window-close behavior are proposed above, not claimed fixed or tested.
