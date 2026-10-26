# HTML to UI Toolkit: editor tooling

This is the editor API and authoring guide for `Neo.HtmlToUitk.Editor`, namespace `Neo.HtmlToUitk.Tooling`. See [HTML/CSS → UI Toolkit](HtmlToUitk.md) for the converter outputs and runtime components. The editor assembly references `Neo.HtmlToUitk.Runtime`; it is excluded from player builds.

[11.0.0 verification](HtmlToUitkVerification11.md) records **370/370, no skips**, for the complete Unity assembly in each tested Editor, including the real portable CLI/native integration. Export/import acceptance of the final release archives is separate in [Verification 11](../../Verification11.md).

## Author a job

1. Open **Neoxider → UI → HTML to UI Toolkit** and choose **New**, **Open...**, or a `HtmlToUitkJob` asset.
2. Add HTML files, folders or glob patterns. Set CSS, USS pass-through and partial inputs in the foldout. Paths in the job resolve from the job JSON directory.
3. Set the output root, run **Validate Only**, inspect the findings, then use **Convert Selected** or **Convert All**. Force and Strict are separate run options; a failed run does not count as a successful import.
4. Save the `.htujob.json` when editing is complete. Open, New, Reload and selecting another job ask **Save / Cancel / Discard** before replacing unsaved edits. Cancel or a failed save keeps the current document, path and unsaved state; New writes its file only after the decision. Invalid candidate JSON is read before a replacement decision and leaves the current document intact. Save followed by Reload reads the just-saved contents. The window can run the current document without overwriting its source JSON.
5. Enable **Watch inputs** for debounced conversion. Editing HTML, CSS, USS or partial inputs updates the watched roots, including external directories. Only matching authored inputs reach conversion; unrelated project imports and generated output are filtered. Normalized absolute paths also handle output roots containing `..`. Renaming a supported input to an unsupported extension reports removal of the old input. Editor import notifications include USS and both sides of a move, with duplicate paths removed per batch.

A `.htujob.json` imports as a read-only job asset. Its JSON is the source of truth; edit it in the window or a text editor. A regular `.asset` job has editable Inspector fields and Save JSON / Load JSON buttons. Saving that job in the window persists its JSON and its addressed job asset; unrelated dirty project assets retain their unsaved changes. This also applies to Save before replacing a document and the window's native SaveChanges path. `SetJsonPath` can select a separate project-relative or absolute JSON location.

`HtmlToUitkConfig` optionally supplies a converter config path and `PanelSettings` overrides. The window config field requires a job asset. The config asset holds project assets and configuration; it needs no scene object references.

Inspector `configOverridesJson` and `perFileJson` must contain a JSON object, blank text or JSON `null`. Invalid JSON and arrays/scalars are rejected: `ToDocument` / `ToJson` throw `JsonParseException`; `SaveJson(out error)` returns false and leaves the existing file intact. Blank text and `null` clear the optional block. A job JSON root must be an object. Unknown extension keys are retained by normal document round trips; this is not full CLI schema validation.

## Run from C#

Use this API in an editor assembly referencing `Neo.HtmlToUitk.Editor`. Call submission and asset refresh on the Unity editor thread. Supply a non-null document and options, and keep document collections non-null.

```csharp
using System.IO;
using Neo.HtmlToUitk.Tooling;

string jobPath = HtmlToUitkPaths.ToAbsolute("Assets/UI/menu.htujob.json");
JobDocument document = JobDocument.Parse(File.ReadAllText(jobPath));
RunOptions options = new RunOptions { ValidateOnly = false, Strict = true };
JobRunResult result = await HtmlToUitkRunner.RunAsync(document, jobPath, options);

if (result.Succeeded)
    HtmlToUitkRunner.RefreshAssets(document, jobPath);
```

The example retains its document until completion. For mutable editor state, retain a separate document snapshot for `RefreshAssets`, too. `RunAsync` captures the submitted document, nested JSON, option flags and selected-file list before returning; subsequent caller edits do not change a queued conversion. `Run` is the synchronous alternative. Both entry points normalize the job path before execution; asynchronous submission resolves it on the editor thread. Runs serialize through one gate and use a temporary job beside the source JSON, preserving relative input resolution. Temporary jobs are removed after bridge execution.

The window retains its own submitted output/job context for refresh. Opening another job during a run does not apply the previous job's report to the newly opened document. Completion for the previous job is shown as a note. Closing the window releases watchers; there is currently no cancellation API for an in-flight conversion.

`RunOptions.OnlyFiles` replaces the HTML entries for that run; it does not edit the original document. `JobRunResult` exposes diagnostics, exit code, stdout/stderr, bridge name, elapsed time and `Succeeded`. Inspect the result before importing generated assets. `RunAsync` does not refresh Unity assets itself. File-system or caller-input failures outside a bridge can still throw or fault the task; callers should handle those errors.

For automation, `HtmlToUitkRunner.RunFromCommandLine` accepts `-htujob <path>`, `-htuValidateOnly`, `-htuForce` and `-htuStrict` through Unity's `-executeMethod` workflow. Coordinate batch execution with the owner of the Unity editor process.

## Paths, bridges and diagnostics

`HtmlToUitkPaths.ToAbsolute` resolves project-relative paths and collapses `.` / `..`. `ToProjectRelative` applies that normalization before checking the project boundary; paths outside the project return null. `HtmlToUitkRunner.OutputRootAbsolute` resolves output relative to the job directory. These operations are lexical and do not resolve symbolic links.

The default external runner discovery checks `Tools/HtmlToUitk/bin/htmltouitk.dll`, the Release `net9.0` build, compatible legacy DLL names, then `htmltouitk` on PATH. **CLI...** / `HtmlToUitkSettings.CliPath` selects an explicit executable or DLL. For Unity-package-only installs, extract the separate `HtmlToUitk.Cli.v11.0.0.zip` delivery and select its `htmltouitk.dll`, keeping all adjacent publish files. It targets net9.0 and uses `LatestMajor` on the tested.NET 10 runtime without an environment override. SDK 10.0.301 can build the source target; no runtime9 is bundled or loaded into the player.

`SelectBridge` can select the optional reflected in-process entry point or `CliProcessBridge`. `BridgeOverride` is a global integration/testing hook; restore the prior value after using it. The external bridge redirects both streams. Its timeout has a five-second minimum and saturates at `Int32.MaxValue` milliseconds before process creation, so extreme integer seconds do not overflow. On timeout or a failure after start, cleanup attempts to stop the owned process and drain its streams within a shared two-second wait budget. Unconfirmed termination or an unfinished stream produces a failed result with an explicit stderr reason. The bridge does not promise termination of an arbitrary descendant process tree, or cancellation of an optional in-process plugin.

`ReportReader` prefers a valid diagnostics object from the current stdout. This is essential for Validate Only and refused conversion: the CLI prints their current report but does not replace the output file. Validate Only never reads an output-folder report. Identical successful conversions remain valid through stdout even when the CLI's WriteIfChanged leaves the file timestamp unchanged. For legacy text-only conversion runners, a valid file with a timestamp at or after run start remains a fallback; an older file is rejected without the previous two-second slack. `FreshnessSlack` remains a public compatibility field but no longer admits old files. Timestamp equality, coarse/remote clocks and concurrent external writers cannot prove report identity; a future run-ID protocol would close that legacy limitation. Malformed stdout can fall back only under these non-validation file rules.

`DiagnosticsReport` provides findings, per-file status and counts. `InputListView` and `FindingsView` expose selection/open events for editor tools. `JobInputPaths.IsInput(document, baseDirectory, path)` and `GetWatchDirectories(document, baseDirectory, fileView)` expose the same focused dependency/output policy used by the window; they reuse `IFileView`, glob matching and lexical path normalization. Non-null document collections and an absolute base directory are required. Missing input directories use an existing ancestor so later file creation can be observed. This policy covers the authored HTML/CSS/USS/partial lists; automatic converter-config dependency discovery remains outside this workflow.

`HtmlToUitkWatcher` owns its file-system handles and editor subscriptions. Construct, configure and dispose it on the editor thread. Call `Watch` with existing directories; dispose it when its owner closes. `Watch` after disposal throws `ObjectDisposedException` and allocates no new handles. Background file events are queued for the editor update; callbacks stop after disposal. `Notify` allows an editor-side source to join the same debounce queue.

## Generated assets and scene integration

Texture import requires an exact `generated-assets.json` ownership entry, matching SHA256 and PNG signature, and the supported texture policy. A folder name alone does not confer ownership. `HtmlToUitkConfig` can narrow the generated-folder policy or disable managed import. Existing managed texture markers and user-authored importer settings need review when changing that policy; reimport is not a general migration mechanism.

The converter reserves actual UXML/USS names and shared/per-page CSS references, including suffix collisions. Portable slash/dot/case aliases share a canonical asset destination: identical payloads are idempotent; conflicting bytes/kinds, reserved outputs and path escapes reject before commit. A distinct page or stylesheet is not silently discarded for an equal stem.

After conversion, assign the generated UXML and the metadata to the runtime components described in [the module guide](HtmlToUitk.md). Scene and Inspector workflows consume the same generated assets as C# callers. The editor assembly does not add a runtime NoCode execution layer or run JavaScript.

## Verification

Regressions prefixed `Audit20261023NeoHtmlToUitkEditor` cover job-root rejection, preservation of files on invalid Inspector JSON, explicit null overrides, normalized output boundaries, immutable submissions, dirty-document choices, current-report precedence and watch dependency/import behavior. The existing replacement fixture also creates persistent regular-job and foreign ScriptableObject assets: both SaveChanges and the shared Save choice must persist the owned job/JSON while foreign disk bytes and dirty state remain unchanged. Shared preferences and bridge hooks are restored by their owners; shared-state fixtures require the coordinator's isolated, exclusive runner. No unavailable NUnit parallelization attribute is required.

The native process fixture requires the task-owned console probe under `Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~`. For future runs after the source barrier, build `Audit20261023NeoHtmlToUitkEditorProcessProbe.csproj` with a compatible SDK (validated SDK 10.0.301 builds net9 targets) and set `NEO_HTMLTOUITK_PROCESS_PROBE` to its absolute executable or DLL path. Its obj/bin paths are distinct. Missing explicit probe configuration reports a skip. Cases inspect real PID/start-time ownership, diagnostics, stream completion and timeout termination; cleanup never kills a reused PID with a different start time.

Round3 executed the complete Unity HtmlToUitk assembly **370/370, no skips**, in6000.3.14f1 and6000.6.0f1. The external-CLI integration performs three real runs, native ownership import/theme and Play UIDocument checks; the portable publish folder also ran outside the repository on.NET 10 with exit0 and no roll-forward ENV. See [actual scope and manual Unity 6.3 OS clicks at1280×720](HtmlToUitkVerification11.md). Future changes still require execution; the broader execution-session/cancellation service remains a proposal.
