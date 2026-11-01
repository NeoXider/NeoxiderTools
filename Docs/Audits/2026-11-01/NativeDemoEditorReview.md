# Native Velora demo and HTML-to-UITK Editor: independent review

Date: 2026-11-01. Baseline: `demo`, `d271531103210ebcd71c3a5d5bbafdb06ff425a5`.

Scope: delivered Velora source snapshots, HTML/CSS, generated UXML/USS/animation metadata, scene/PanelSettings YAML, typed controller, Play Mode tests, existing Editor window and current `Assets/Neoxider/Docs/UI` plans. This reviewer owns only this audit file.

**Result: source review identifies remaining parity gaps; visual acceptance is blocked / NotRun.** Unity is currently reserved for VampireDota. This review did not start or control an Editor/Player, run Unity tests, change the scene, take desktop captures, or capture browser/Unity video. Source evidence below proves implementation differences, not what the current pixels or motion look like.

## Source-confirmed differences and limits

Paths in this section are repository-relative; line numbers refer to the baseline unless a follow-up section says otherwise.

| Priority | Finding | Source evidence | Consequence / required check |
|---|---|---|---|
| P1 | The moving dashboard outline is absent from the native composition. The feature outline is replaced with a straight top-edge sweep. | `Assets/HtmlToUitkShowcase/Source~/Original/src/components/demo/hero-mockup.tsx:48` places `BorderBeam size={96} duration={10}` in the dashboard. `Resources/Velora/Screens/Velora.uxml:112–169` contains the dashboard frame/content without a beam element. Original `Source~/Original/src/app/templates/saas/page.tsx:295–296` places a separate BorderBeam in a feature card. Adapted `Source~/velora.css:102,187` defines a 72×3 px line at top/left zero and translation from x=0 to x=430 with alternate direction. | A perimeter-following outline cannot result from that straight-line channel. Record both dashboard and feature card through a complete cycle; top/right/bottom/left edge coverage, corner continuity, duration and direction must be measured. Rendering visibility remains NotRun. |
| P1 | Orbit composition, radius and timing differ. | Original `Source~/Original/src/app/templates/saas/page.tsx:282–286` supplies three children to `OrbitingCircles`, radius 90 and duration 24 s. Adapted `Resources/Velora/Screens/Velora.uxml:219–226` supplies one rotating wrapper and one planet. `Source~/velora.css:95,98` uses a 134×134 px ring/wrapper and an 18 s animation. | One rotating text symbol is not the original three-icon composition. Check actual centre/radius, planet count, phase spacing, clipping, angular velocity and icon counter-rotation in video. Do not infer successful rotation from the animation JSON. |
| P1 | Meteor population and construction differ. | Original `Source~/Original/src/app/templates/saas/page.tsx:334–338` includes a DotPattern and `Meteors number={10}`. Adapted `Resources/Velora/Screens/Velora.uxml:255–260` has four meteor nodes. `Source~/velora.css:105–106,189` defines a solid stage, 2×96 px gradient trails, 35° static rotation, 5 s travel and a 750 ms stagger. | The original population/pattern is not preserved. Actual visible density, diagonal direction, trail/head orientation, entry/exit, opacity curve and repeat gap require frame-by-frame comparison. The upstream meteor implementation itself is not included in the snapshot, so its precise timing cannot be asserted here. |
| P1 | Marquee hover/focus pause was not carried into the authored native bindings. | Original `Source~/Original/src/app/templates/saas/page.tsx:247,309–312` enables `pauseOnHover` for both marquees. The supplied `Source~/Original/src/components/velora/marquee.tsx:47,53` pauses on focus-within and optionally hover. Adapted `Source~/velora.css:84,103,185,188` and `Resources/Velora/Screens/Velora.anim.json:407–429` only define unconditional on-show loops; the typed controller has no hover/focus pause callbacks. | Verify hover/focus during active motion and resume position after exit. CSS color hover and reduced-motion toggling are separate behaviors and cannot pass this pause requirement. |
| P1 | Dashboard bar appearance and reveal differ from the captured upstream source. | Original `Source~/Original/src/components/demo/hero-mockup.tsx:110–123` uses 128/160 px chart height, per-bar opacity `0.5 + height / 200`, 0.3 s initial delay plus 0.05 s per bar, 0.5 s duration and a while-in-view reveal. Adapted `Source~/velora.css:77–78,184,194` uses 150/110 px chart height, 0.7 s scale animation and 45 ms stagger; `Source~/geometry.css:54–65` supplies fixed heights/gradients without the original per-bar opacity. | Compare dashboard bounds and each bar's rendered height/opacity at the same CSS-pixel viewport. Record the reveal while it happens and when scrolled into view; a populated final chart does not prove reveal parity. |
| P1 | Dark/light palettes are explicitly adapted rather than preserved by the original tokens. | Upstream `Source~/Original/src/app/globals.css:71–92,111–133` uses neutral OKLCH surfaces and distinct light/dark brand tokens. Adapted `Source~/velora.css:2–5,48–51,162–175` hardcodes hex palettes, uses blue-tinted dark surfaces and leaves primary CTA colors common to both themes. The README explicitly disclaims full browser pixel parity. | This is source evidence for a color-system difference, not a measured pixel failure. Sample matched flat surface/CTA/chart regions in both captures; record sRGB values, contrast and accepted deviations. Avoid diagnosing gamma or tint from a screenshot alone. |
| P1 | Several header items deliberately launch a browser; local coverage is four pages. | `Runtime/VeloraShowcaseController.cs:163–180,416–420` calls `OpenExternal` for Components/Blocks/Templates/Themes/Changelog and their mobile equivalents, ultimately `Application.OpenURL`. Pricing/About/Contact use local `Navigate`; `ScrollHomeTo` is local (`314–321`). `README.md` describes the external links and missing full catalogue. | Current behavior is explicit, but it does not establish an all-local site import. Test every successfully delivered local route inside Unity; external destinations must remain visibly marked. Missing catalogue pages must be reported, not counted as local navigation passes. |
| P2 | The period control is a local presentation demo, not a different chart dataset. | `Runtime/VeloraShowcaseController.cs:542–555` changes the period/stat/title text and replays `bar-rise` on existing bars. It does not change their heights, gradients, year or growth labels. Original `hero-mockup.tsx:73–75` uses a static span rather than a period button. | Do not claim browser interaction parity or a real quarterly chart. Verify labels and the explicit demo action, and decide whether the extra interaction needs clearer product wording. |
| P1 | The baseline Editor flow starts with jobs and simultaneous settings/results, not a single-page source flow. | `Editor/Window/HtmlToUitkWindow.cs:154–193` places Job/New/Open/Save/Reload/Config/Runner/CLI in one toolbar. `196–267` adds Inputs+Findings and settings simultaneously. `378–386` creates `pages/*.html`; `628–655` rejects adding source before creating/opening a job. There are no source/settings/results tabs in this baseline. | The user's requested default is missing at baseline. Review the incoming tab patch for an actual single-file default, source-first launch, persistent advanced jobs, honest unsupported URL scope, visible results and retained unsaved-change behavior. Window fit needs real UI inspection. |

## Camera and CTA evidence

- **Camera source state: present.** `Assets/HtmlToUitkShowcase/VeloraShowcase.unity:222–281` contains `Showcase Background Camera`, enabled at line 245, with target texture null and target display 0 at 279–280. `VeloraPanelSettings.asset:17,21,24,31` targets the display with Constant Pixel Size / scale 1. This does not prove that the normal Game View no longer displays `No cameras rendering`.
- **Latest known test receipt: 14/16, not rerun here.** `README.md` and `Assets/Neoxider/Docs/UI/HtmlToUitkRuntimeTODO.md:11–12` report two CTA pixel failures and diagnostics added after the last run. Those historical results are context, not a new pass on this revision.
- **The test panel does not exercise the scene camera path.** `Tests/PlayMode/VeloraShowcaseInteractionTests.cs:48–61` constructs a fresh GameObject/UIDocument and renders to an independent RenderTexture via PanelSettings. It never loads `VeloraShowcase.unity` or its camera.
- **Assertions protect actual paint.** `VeloraShowcaseInteractionTests.cs:450–479` requires native hit-test visibility, opaque colors, at least 4.5:1 resolved contrast, painted pixels and a modal after a pointer event. `482–567` currently reads the RenderTexture, counts raw/linear/gamma/mirrored diagnostic matches, but asserts only the original expected-space background (>45% of inner region) and visible glyph pixels (>30). Keep the paint requirements; diagnostics alone are not a fix and alternate-space counters must not become an unrestricted pass.
- **Synthetic input has a deliberate limit.** `PointerClick` (`576–599`), wheel (`602–619`) and thumb drag helpers dispatch UI Toolkit events. They do not prove the physical mouse/touch input path. Scene Game View and input verification are separately NotRun.

## Required operator checks after source freeze

Every row below is **NotRun by this reviewer**. Use the already-owned single Unity operator once VampireDota releases the Editor. Do not alter user settings or start another Editor against this workspace.

| Check | Exact scenario and evidence | Pass / failure signal |
|---|---|---|
| G1: ordinary scene Game View | Open the delivered scene; confirm its actual PanelSettings and camera; Play at 1280×900 and 390×844; capture the ordinary Game View and console baseline/delta. | UI is nonblank and correctly framed, no `No cameras rendering` overlay, no new errors. An isolated panel capture cannot pass this row. |
| G2: CTA diagnostic rerun | Run only `DesktopHeroCta_IsPaintedReadableAndPointerOperable` and `MobileHeroCta_IsPaintedReadableAndPointerOperable` with graphics, retaining XML/logs, Unity/backend/color-space/format/orientation data and the actual readback image/crop. Compare normal Game View before choosing a correction. | Original contrast/background/glyph/input assertions pass. A failure includes pixel values and whether the frame is nonblank, correctly cropped/oriented and in the declared color space. Never lower thresholds to green a run. |
| G3: matched realtime hero video | Browser original and Unity separately, browser zoom 100%, CSS viewport equal to Game View pixels, no forced scaling, identical theme and motion state; record viewport and device pixel ratio. Capture 30 s each at realtime speed with the hero/dashboard visible, then scroll by an agreed sequence. | Nonblank usable recordings; CTA/shimmer, typewriter/cursor, aurora, dashboard reveal and outline visible in their active phases. Capture resolution/duration/frame timing accompany evidence. No timelapse or post-animation still may substitute. |
| G4: effects video | Stop at features and record at least 30 s at 1280×900, then mobile 390×844; include a full original 24 s orbit, multiple meteor and beam cycles, hover over marquees, switch Motion off/on, navigate away/home. | Compare element counts and measured centres/trajectories/periods/brightness/fade; note intentional deviations. No disappearing loops after return, duplicate playback, hover mismatch or reduced-motion leftovers. Source metadata is not a visual pass. |
| G5: color and dashboard geometry | At matching light/dark state, retain full frame plus hero/dashboard/features crops. Measure bounding boxes relative to viewport and sample flat CTA, text, background, card, border and bar interiors. Inspect widths 1280, 1101/1100, 901/900, 641/640 and 390. | No clipping/overlap/illegible labels; all differences are measured and reported. Publish actual values before proposing tolerance for intended adaptations; do not derive a parity score from unlike viewport sizes. |
| G6: physical local interaction | Physical mouse: Home→Pricing→footer About→Contact→Home, header/menu Search→chart result, FAQ open/close, CTA→copy/close/Escape, theme/motion, period, invalid/valid forms, wheel and thumb to footer. Repeat mobile menu and touch on the supported target. | Delivered local pages/modal actions stay inside Unity, one activation causes one state change, forms remain local, footer reachable, focus and Escape usable. Explicit `Original`/external catalogue links are a separate observed browser action. Log each control/result. |
| G7: Editor workflow | Fresh/reopened window at minimum size and normal desktop size, narrow docking, light/dark Editor; single file and local folder, existing JSON/asset job, missing runner, validate/convert, selected/all inputs, watch, findings/open source/reveal outputs, save/reload and unsaved Save/Cancel/Discard. | Initial Source tab is useful without prior hidden state. A single-page default converts exactly one explicitly selected page; advanced multi-input jobs remain accessible. Settings/results have separate tabs, no clipped toolbar/actions, errors are visible and every claimed mode is executable or explicitly unsupported. Persisted paths/jobs and dirty ownership remain correct. |

After a production or readback fix, rerun the failed CTA selection and affected scenarios. The remainder of the 16-test assembly can cite an unchanged valid receipt only when its source/dependencies really remain unchanged; source changes affecting those tests require a rerun. Real camera/input/video checks remain separate gates.

## Incoming diff review

Source-final follow-up reviewed on 2026-11-01. The full two-file Editor diff, reused path/runner/job/view implementations and affected existing tests were read. `CTAReadback.md` and `EditorWindowUX.md` were treated as handoff context, not proof of runtime outcomes. Read-only Git status also showed other owners' runtime/web work and historical-record changes; they are outside this Editor/Velora review and were not changed here.

Reviewed SHA256:

- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs`: `183687221a3ed3eb20a0dd84f54f00dfdb9f89f22840b71354ff26e856761478`.
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.uss`: `dce5e9ad25e93a2b8ce19e2ae610cbc8384a2e9ab1b4ee5ffc3766a10a76aab3`.
- CTA test unchanged: `5a90b705abc5bf75b9d99be9e5270453a1ea114567a39c004e8a3a736f08c4cb`. CLI2 made no speculative sampling correction and did not weaken assertions. The two tests still need their first diagnostic run.

**Source disposition: no confirmed new blocking implementation defect found in the supported saved-job and ordinary draft paths. Acceptance remains blocked by missing new-path regressions and native execution.** This is a bounded source conclusion, not compile, persistence, layout or input certification. The baseline Editor finding above is superseded for tab structure: Sources/Settings/Results now exist, the initial in-memory document accepts local HTML without creating JSON, and URL/JavaScript import is explicitly unsupported. It still does not implement the future URL/current-page/crawl feature or establish browser parity.

### Ownership and persistence assessment

| Path | Source evidence in the updated window | Assessment / exact remaining edge |
|---|---|---|
| CreateGUI / rebuild | `120–176` unbinds old view events, stops the watcher, clears/rebuilds the tree, reuses `_doc` before considering disk and restores serialized JSON when needed. `179–211` deduplicates Editor update/import subscriptions and releases view subscriptions on disable. | No duplicate subscription is apparent in this path. A real repeated rebuild/domain reload must prove one callback, one watcher, retained dirty edits and retained unknown fields; existing saved-file replacement tests do not cover it. |
| Draft base and run | `530–541` supplies a virtual job location in the project root and a default-output snapshot. `998–1018` clones run data and applies Config to the clone. Reused runner `HtmlToUitkRunner.cs:53–67,139–167` snapshots the request, writes a unique temporary job beside that virtual path and cleans it up. | The virtual Untitled file is not written by this code. Verify default and blank output through a stub bridge; confirm output is the advertised location and no persistent job is created merely by opening/adding/rebuilding. |
| First draft Save | `718–780` chooses a path, clones, rebases HTML/CSS/USS/partials/config/output using a directory prefix, writes and imports the owned JSON, then updates window state. `794–796` leaves rooted entries unchanged and does not call `GetFullPath` on wildcard tails. | Ordinary relative/absolute entry semantics trace correctly. Add a nested-directory Save test observing actual resolved files and output before/after, including `../`, globs, blank/default output and failed/cancelled selection. The current tests never enter this draft-native-dialog branch. |
| Saved Save / replacement | `654–705` retains the Save/Cancel/Discard decision callback, pre-read JSON and same-path reread after Save; New now uses `CreateNew`. `739–780` still persists only the addressed regular asset with `SaveAssetIfDirty`. | Existing test command signatures and foreign-dirty-asset boundary remain intact by inspection. A failed write retains unsaved flags before the success assignments. The branch still requires the existing replacement fixture to execute on this diff. |
| Unknown JSON fields | Window cloning/restoration goes through `JobDocument.Parse/ToJson`; `JobDocument.cs:11–14,55,90,110,116,119` preserves the existing unknown top/input/output/watch blocks. `HtmlToUitkJob.cs:75,102` retains its source JSON. | No new dropping of these supported extension blocks was found. This is not future-schema compatibility: pre-existing `JobDocument.cs:48,72` ignores input `schema` and always writes 1. The existing future-schema TODO remains open; the tab change must not be described as resolving it. |
| Run completion / replace / cancelled task | `982–1018,1036–1095,1107–1113` retains task ownership until Poll, handles faulted/cancelled tasks, retains the submission snapshot for asset refresh and rejects applying an old document's report by reference identity. `1151–1168` requeues watched changes while a task remains owned. | No new duplicate task or old-document overwrite is apparent. Need a controllable pending/completed/faulted/cancelled bridge case, especially UI rebuild and replacement before Poll. A cancelled-task branch is defensive handling; there is no user-facing cancel feature in this patch. |
| Watch and output | `812–844` rebuilds views then restarts only enabled watch. `1126–1148` disposes/replaces the watcher and classifies changes with the effective default output. The output-change callback restarts an active watcher; existing CSS list edits do likewise. | Source boundaries still reuse `JobInputPaths`. Verify first draft Save moves watch dependencies to the equivalent resolved locations, generated output is excluded and one changed input yields exactly one run. |
| Diagnostics / layout | `898–903` clears the selected-file filter when a global runner finding exists; `1081–1093` retains selectable full stdout/stderr/exit code in Results; USS adds wrapping/scrolling and a 400×420 minimum. | Error visibility and minimum-size usability need native checks, especially fixed-height virtualized finding rows and long paths. Source layout values are not a screenshot pass. |

### Exact advanced-draft path defect and pre-existing limit

**P2, conditional contract defect: rebasing a draft's HTML identities leaves `output.perFile` keys stale.** `HtmlToUitkWindow.cs:744–750` rebases HTML/CSS/USS/partial/config/output paths but does not rekey `prepared.PerFile`. This is not just an unknown-JSON byte-preservation question:

- `Tools/HtmlToUitk/src/HtmlToUitk.Convert/Config/ConfigLoader.cs:264–277` copies per-file keys verbatim into an ordinal dictionary.
- `Tools/HtmlToUitk/src/HtmlToUitk.Convert/Pipeline/LoadStage.cs:59–62,108–113` uses the normalized input path as `PageInput.Path`.
- `Tools/HtmlToUitk/src/HtmlToUitk.Convert/Pipeline/Assembler.cs:228,242` selects per-file folder/name with an exact `TryGetValue(inputPath)`.

Static reproduction: a draft based at the project root contains HTML `pages/home.html` and `output.perFile["pages/home.html"] = { "name": "Landing", "folder": "Special" }`; first Save to `Assets/Jobs/site.htujob.json` rebases HTML to `../../pages/home.html` but retains the `pages/home.html` override key. The same source file resolves, while its name/folder override no longer matches. The emitted destination can silently change from `Special/Landing.uxml` to the normal default screen path. This behavior was traced from source; the reproduction has not been executed.

Reachability is bounded: the fresh-draft UI does not currently author per-file overrides; ordinary loaded saved jobs have a nonempty path and skip draft rebasing. A supplied/restored advanced draft can contain them. Either preserve that supported document contract with rebased keys and a regression, or explicitly constrain/reject this advanced draft state; do not describe all draft payload semantics as preserved before deciding this.

**Do not rebase output-relative folder payloads.** `output.folders` and the per-file value's `folder` are relative paths inside the output root (`Assembler.cs:26,87–94`; `Jobs/JobRunner.cs:100–105`). Once `output.root` is rebased to the same physical root, these values must remain unchanged. Only the per-file source-identity keys need the matching source path transformation; `name`, folder values and unrelated nested extension payloads must retain their existing meaning. Absolute source keys need to stay absolute and keys should match the converter's normalized identities.

An actual assembly/domain reload during an in-flight runner is a separate, pre-existing limit: it loses nonserialized task fields (`_task`, `_taskDocument`, `_taskSource`, `_taskJobPath`). This differs from rebuilding CreateGUI in the same managed instance. Background completion, cleanup and asset refresh across that reload are not proved by serialization of the document. Do not claim conversion cancellation/resumption across domain reload.

### Narrow affected verification selection

All tests below are **NotRun** in this review; no compiler or runner was invoked. Run this focused existing selection after the source barrier, rather than the unrelated full package suite:

1. `Audit20261023NeoHtmlToUitkEditorWindowReplacementTests` — its full small fixture: Cancel (both branches), failed Save, Save→Reload, invalid candidate, Save/Discard and all three regular-asset/foreign-dirty ownership cases. These commands are directly changed or called by changed Save/UI initialization paths.
2. `Audit20261023NeoHtmlToUitkEditorWatchDependenciesTests.EditingCssInTheWindowWatchesTheNewExternalDirectoryAndIgnoresUnrelatedOutput` — directly exercises window controls and watcher rebinding. The separate postprocessor rename case is unaffected and is not required by this two-file diff alone.
3. `RunnerEditModeTests.SelectedFilesRunThroughATemporaryJobThatIsDeletedAfterwards`; `Audit20261023NeoHtmlToUitkEditorPipelineTests.ParentSegmentsResolveToTheSameOutputAndProjectAssetPath`, `.ProjectRelativeSubmissionGivesTheBridgeAbsolutePaths`, `.QueuedAsyncRunUsesTheDocumentAndOptionsAtSubmission` — narrow seam checks for virtual draft location, relocated paths, selected files and immutable run submission. They do not substitute for the missing draft-window cases.
4. `JobDocumentTests.RoundTripPreservesUnknownKeysAndNestedBlocks`, `.EditedFieldsAreWrittenInSchemaOrder`; `InputExpanderTests.FileFolderAndGlobEntriesExpandRelativeToBase`, `.AbsoluteEntryOutsideBaseKeepsAbsolutePath`, `.MakeRelativeAndCombine` — narrow reused serialization/path contracts newly depended on during restoration/first Save.
5. The unchanged desktop/mobile CTA tests from G2 only for Velora diagnosis. Editor tab edits do not themselves require rerunning the unrelated Velora interaction tests.

**Missing regression coverage blocks source acceptance of the new draft lifecycle.** Add meaningful window-level cases in the test owner's scope, observing files, submitted snapshots, dirty state and callback count rather than exact row structure:

- Fresh window + Add file/folder + repeated CreateGUI: no job/output disk writes; edits and extensions retained; a single-file input remains one explicitly chosen page, without a hidden glob placeholder.
- First Save to a nested job directory: equivalent resolved HTML/CSS/USS/partial/config/output locations, globs unchanged in meaning, extensions retained; cancel/failure leaves original in-memory document/path/dirty state and disk bytes intact. Add a narrow path-selection seam if needed without changing existing replacement command signatures.
- Serialized dirty-window reconstruction and reopening a regular asset with missing JSON: data retained, no implicit persistent job creation, explicit Save persists only the owned asset/JSON.
- Pending run → rebuild/replacement/Save → completion; fault/cancel/startup failure and selected-file global diagnostic: exactly one submission, correct report owner, no hidden failure, temporary files cleaned, output refresh uses the submission snapshot.
- Draft watch → edit dependencies → first Save/rebuild: exactly one relevant rerun and no rerun for output/unrelated files.

Minimal executable cases for the new fixture (the names below are proposed, not tests that already exist):

| Case | Drive the production path | Observable assertion |
|---|---|---|
| `FreshDraft_AddAndRebuild_DoesNotWriteAndKeepsOneHandler` | Clear/restorably save LastJobPath, create the real window, add one task-owned HTML using the existing InputListView entry path, rebuild CreateGUI twice, edit once, run against a capturing bridge. | Exactly one submitted file/run; dirty state and current input retained; no virtual Untitled JSON, saved job or output artifact exists before the explicit run; no duplicate callbacks/watchers. |
| `DraftFirstSave_RebasesKnownPathsWithoutLosingPayload` | Seed the real draft with relative/absolute entries, `../`, a glob, Config, blank/default/explicit output (parameterized), known output folders, extensions and a perFile source key; invoke the actual first Save with a deterministic path-selection seam into a task-owned nested project folder. | Saved JSON still resolves every known source and output to its original physical location; perFile key follows the converter-normalized source identity and still yields `Special/Landing`; folder values/name/extensions unchanged; owned job survives reimport. A real small conversion can assert emitted `Special/Landing.uxml` where an available test backend supports it. |
| `DraftFirstSave_CancelOrWriteFailure_PreservesOwner` | Return cancellation from that seam, then choose a task-owned invalid target; separately exercise replacement Save→cancel with the existing decision seam. | False/clear failure, no new job, current path/document/dirty state and existing bytes unchanged. The test must reach `draft == true`; existing saved-file failure cases are insufficient. |
| `DirtyRebuildAndMissingJsonAsset_RetainDocumentAndOwnedPersistence` | Reconstruct a serialized dirty window, rebuild repeatedly, then open a regular task-owned job asset with no JSON and explicitly Save. | Dirty/extension contents survive; opening creates no JSON/output; explicit Save persists only the addressed asset/JSON, foreign dirty bytes/state unchanged. |
| `RunResultAndWatch_KeepSubmissionOwnerAcrossUiChanges` | Use a gated capturing bridge; submit, rebuild/replace or Save while pending; release completion before the next Poll; parameterize fault/cancel/startup error; select files when report has a global failure. Then edit one watched input before/after draft Save. | Exactly one owned submission/cleanup; refresh uses submitted paths; previous-job report cannot overwrite replacement; global failure remains visible; exactly one relevant watch run, none from generated output. This case exercises same-instance UI reconstruction only, not domain-reload resumption. |

These should reuse the existing replacement fixture's temporary directories, bridge hooks and targeted asset cleanup patterns. A small path-picker seam can make first Save deterministic; retain `TryReplaceDocument`/`LoadJson` signatures relied on by existing tests. Avoid native file-dialog automation, source-string assertions or a replacement runner harness.

Visual G1–G7 and the initial native parity findings remain open. Neither worker's source-final handoff closes those gates.

## Correction review: per-file preservation and new draft fixture

Second source-final handoff reviewed independently on 2026-11-01. This section supersedes the earlier missing-test-source status and the conditional per-file implementation finding; all execution/visual gates remain NotRun. The complete new fixture, its meta, the production correction, final window diff and stylesheet diff were read. No product/test source was edited by this reviewer.

| File | SHA256 reviewed | Source disposition |
|---|---|---|
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs` | `9d4f74cdf423c04e316ea14a4f3b1d8028766370a5e8a7377438deada5fee52b` | Accept the production correction at source level, pending compile/behavior checks. |
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.uss` | `dce5e9ad25e93a2b8ce19e2ae610cbc8384a2e9ab1b4ee5ffc3766a10a76aab3` | Accept source; native layout remains NotRun. Unchanged since prior review. |
| `Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261101NeoHtmlToUitkEditorDraftTests.cs` | `4f1a626aba686c22f530bded2052f24588e93f42883d0124ceffb37e8cee6f89` | **Return for the two narrow findings below; do not open the combined source gate yet.** |
| `Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261101NeoHtmlToUitkEditorDraftTests.cs.meta` | `1f9c65bc74dcb09fe998262059ac3c8c4db74d56ede31d9ab35ab99c8c7395bd` | Accept source metadata. |

### P2 disposition: implementation corrected, execution pending

`HtmlToUitkWindow.cs:746–759,806–855` now builds a separate normalized per-file map before cloning/writing. Relative source keys receive the same directory prefix as HTML; normalized dot/slash identities match the converter's ordinal lookup; absolute keys remain absolute. `JsonValue.Has` is ordinal (`Runtime/Logic/Json/JsonValue.cs:101–109`), matching the converter dictionary. The new normalizer was compared directly with `Tools/HtmlToUitk/src/HtmlToUitk.Core/Text/PathUtil.cs:9–36`; it handles slash normalization, root/drive prefixes and dot segments without adding the optional Core assembly as a mandatory dependency.

The map rejects unsupported shapes, non-object override values, invalid known value types, wildcard/blank/invalid-character keys and normalized duplicate identities before `File.WriteAllText`. Validation precedes `JobDocument.ToJson`, whose existing serializer omits a non-object map. A new map is built without mutating the original map/keys; no later preparation step mutates override payload objects. Config is applied to the prepared document, and window owner/path/dirty assignments remain after the write/import boundary. Values such as `name`, `folder`, `assemble` and nested extensions are preserved. `output.folders` and override folder values remain output-relative as required. Ordinary saved jobs skip the draft rekeying.

The new nonserialized private picker callback (`70–71,731–732`) only chooses the native Save destination in tests; it preserves existing command signatures and does not replace saving, importing, rebasing, runner ownership or cleanup. This is an appropriate narrow command seam.

### Remaining source gate findings

1. **P1 — unavailable NUnit attribute prevents the new fixture from compiling against the installed package.** `Audit20261101NeoHtmlToUitkEditorDraftTests.cs:20` adds `[NonParallelizable]`. The installed `Library/PackageCache/com.unity.ext.nunit@d8c07649098d/package.json` declares version **2.0.5**; its README and `Documentation~/ext.nunit.md` describe the custom build as based on NUnit 3.5. `Neo.HtmlToUitk.Tests.Editor.asmdef:15–18` explicitly references `nunit.framework.dll`. Read-only binary metadata-string inspection of that exact package DLL finds `ParallelizableAttribute`, `ParallelismAttribute` and `TestFixtureAttribute`, and no `NonParallelizableAttribute`. The unavailable identifier is therefore a source compatibility blocker; this is not a reported compiler run. Remove it and retain the coordinator's exclusive isolated runner for shared bridge/preferences, as already required by `Assets/Neoxider/Docs/UI/HtmlToUitkEditor.md:70`. Do not replace it with another unverified attribute or claim the fixture itself enforces global isolation.
2. **P2 — the cancellation case can pass through the fault branch.** `Audit20261101NeoHtmlToUitkEditorDraftTests.cs:394–404` throws `OperationCanceledException("Owned cancelled run")` from the bridge and only asserts a resulting diagnostic contains `cancelled`. The window's fault branch can preserve that exact exception text as well (`HtmlToUitkWindow.cs:1109–1111`); the test never establishes `Task.IsCanceled` or requires the branch's distinct `The run was cancelled.` result. Thus it cannot prove the claimed cancelled-task branch even if green. Add an actual task-status discriminator and distinct expected result; if this bridge path produces an OCE fault rather than a cancelled Task, name it accordingly and test genuinely cancelled completion separately at the window result boundary. Keep the real runner/temp-job cleanup assertion for the real runner case; do not present an injected result as cancellation of a CLI process or a new user-facing cancel feature.

No other production-source blocker was found in this correction. The unavailable attribute and cancellation coverage issue are the complete narrow return list for the same CLI5 session; root keeps the combined source/test gate closed until that correction is reviewed.

### Test validity and remaining execution scope

The authored fixture contains **11 methods / 22 parameterized cases, all NotRun**. It drives the real window commands, native view entry APIs, actual first-Save branch, real runner snapshot/temp-file path and real Editor JSON serialization. Reflection is used to invoke commands and seed owned state/seams, rather than to reimplement the production normalizer. Assertions observe saved bytes/imported data, resolved file/output identities, extension blocks, dirty/path ownership, captured submissions, temporary-job cleanup and visible findings.

The first-Save test uses `Path.GetRelativePath` and actual resolved paths as independent expectations, including slash/dot aliases and absolute entries; it protects the original per-file bug. The six invalid/collision cases verify an existing destination is untouched and live per-file state/path/dirty flags survive. Saved-job, failed/cancelled picker, missing-JSON regular asset and foreign-dirty asset cases cover material data boundaries. Pending-run cases prove same-instance rebuild/replace/Save and completed-before-Poll ownership; they do not establish an actual domain reload while running. Watch uses public `Notify` to keep debouncing deterministic while the existing dependency fixture remains responsible for OS event delivery. Cleanup/restoration is task-scoped and restores bridge/CLI/LastJob settings.

These are useful regressions, not visual or successful-converter evidence. The `Special/Landing.uxml` assertion measures the preserved destination identity; it does not assert the real converter emitted that file. The bridge deliberately returns failure, so successful asset refresh/import after conversion still needs the integrated smoke path. The fixture's cancellation claim must be narrowed/fixed as above. Run the corrected 22-case selection plus the focused existing list once root opens the source barrier, then inspect G7 natively. Keep the two unchanged CTA diagnostic tests and G1–G6 as separate evidence gates.

## Final source-gate recheck after test compatibility correction

Final follow-up on 2026-11-01: **both returned findings are resolved in source; no remaining Editor/draft source blocker was found. The source gate may close for this review's scope.** This permits root's coordinated compile/test phase; it is not a claim of compilation, test passage, visual acceptance, a successful converter run, physical input or runtime video parity. The final `EditorWindowUX.md` accurately records **12 methods / 23 authored cases, all NotRun**, and distinguishes the tested boundaries.

- The unavailable `NonParallelizable` attribute has been removed (`Audit20261101NeoHtmlToUitkEditorDraftTests.cs:20` now describes the coordinator's exclusive sequence). No unverified replacement attribute was added.
- The two real bridge-exception cases (`394–409`) now require the actual task to be `Faulted`, `IsCanceled == false`, the same original exception object and the exact resulting diagnostic. They still assert real runner temporary-job cleanup. An OCE containing the word “cancelled” can no longer pass as proof of task cancellation.
- The separate completion-only case (`413–439`) supplies `Task.FromCanceled<JobRunResult>`, establishes `Canceled`/`IsCanceled`/`!IsFaulted` before Poll and requires the exact `The run was cancelled.` diagnostic plus restored conversion availability, hidden progress, failure styling and retained dirty state. It explicitly does not claim cancellation of a runner/CLI/in-flight conversion. Seeding the completion boundary while asserting these observable outputs is a useful branch regression, not a replacement runner or normalizer implementation.
- Production window source, USS and meta match the previously accepted hashes exactly. Only the fixture and its report changed for compatibility. Existing lifetime/data-ownership findings and the useful test assertions remain as reviewed.

Final source-accepted SHA256:

| File | SHA256 |
|---|---|
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs` | `9d4f74cdf423c04e316ea14a4f3b1d8028766370a5e8a7377438deada5fee52b` |
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.uss` | `dce5e9ad25e93a2b8ce19e2ae610cbc8384a2e9ab1b4ee5ffc3766a10a76aab3` |
| `Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261101NeoHtmlToUitkEditorDraftTests.cs` | `548b7d6d47ddc93e46c3c0a8166b4536e52077dc9a08e0153c38d6e006070a25` |
| `Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261101NeoHtmlToUitkEditorDraftTests.cs.meta` | `1f9c65bc74dcb09fe998262059ac3c8c4db74d56ede31d9ab35ab99c8c7395bd` |

Next owner: root / single Unity operator. Compile/import the frozen combined source, run the corrected **23-case** draft fixture in the isolated exclusive sequence plus the narrow existing affected selection above, and perform G7. Then perform the unchanged CTA diagnostic experiment and the separate scene/video/input checks G1–G6. No compiler, test runner or Unity operation was performed by this reviewer during this recheck.

## Blocking conditions and handoff

1. Required visual/runtime evidence is unavailable while the designated Editor is occupied. Owner: root / single Unity operator; action: schedule G1–G7 after source freeze and current Editor release.
2. The two CTA failures have no rerun diagnostic result yet. Owner: CLI2 + Unity operator; action: identify the confirmed defect before changing sampling and preserve the paint/readability contract.
3. Realtime browser-versus-Unity video does not exist in this review. Owner: operator; action: capture matched recordings and measure active phases, not just final stills.
4. Source confirms missing/changed effects and limited local page coverage; these must be fixed or explicitly accepted as remaining native adaptation gaps. Owner: implementation lead.

Verification performed: repository/source reads only; no Unity import/compile, console query, Play Mode, tests, build, browser navigation, screenshot or video capture. Product source and historical documentation were not changed by this reviewer.

## Independent review of the managed API typecheck receipt

Receipt reviewed on 2026-11-01: **PASS for the stated API-only compilation scope on the two exact managed SDK profiles below.** Root ran the compiler; this reviewer only read the preparation script/projects/logs/manifests and independently recalculated file/archive hashes. No compiler, Unity or test runner was started by this reviewer. This receipt does not close the 23 native draft cases or G1–G7.

Evidence: [HtmlApiTypecheck-20261101.zip](Evidence/HtmlApiTypecheck-20261101.zip), SHA256 `09cc9986e1051b6383ca25114b29256c32121bfe0e43956649b839b99626599d`. Its `manifest.json`, `results.json`, `prepare-api-typecheck.py`, per-profile projects/logs/exit files, generated compiler metadata and source snapshots were reviewed against `D:/Temp/Neoxider-Audit-20261101/verification/api-typecheck` and the live repository.

| Managed SDK profile | Authored compilation scope | Retained result |
|---|---|---|
| `6000.3.14f1` | Runtime: 33 C# files; Editor: 23; Tests: the 2 new fixtures, linked to those freshly built Runtime/Editor projects. | Exit 0, 0 errors, 1 CS0649 warning for serialized `UitkScreen.reducedMotion`. |
| `6000.6.0f1` | The same 58 authored files in the same 3 project groups, against this profile's managed Unity modules. | Exit 0, 0 errors, the same 1 CS0649 warning. |

The projects explicitly target **C# 9 / netstandard2.1**, disable implicit usings/default compile items/shared compilation and build in sequence; the receipt records .NET SDK `10.0.301`. Both Tests projects include the exact snapshot paths for `HtmlRuntimeWebContractsTests.cs` and `Audit20261101NeoHtmlToUitkEditorDraftTests.cs`. The latter matches the source-accepted `548b7d6d47ddc93e46c3c0a8166b4536e52077dc9a08e0153c38d6e006070a25`, so this is evidence of API binding for the corrected fixture rather than its obsolete pre-compatibility version. The window source matches `9d4f74cdf423c04e316ea14a4f3b1d8028766370a5e8a7377438deada5fee52b`.

Independent integrity/scope checks:

- All **58 live source hashes and 58 snapshot hashes** match the manifest. The runtime/editor file inventories exactly match the current 33/23 authored files, and all six projects' explicit Compile sets contain their intended snapshots without extra authored sources.
- All **143 direct reference hashes** for 6.3 and **186** for 6.6 match the current installed DLLs. The fresh project references resolve Runtime → Editor → Tests; they do not substitute the old package Runtime/Editor DLL for these 56 authored sources.
- All **6 output DLL hashes**, both log hashes and both retained zero exit files match `results.json`. The raw logs name all three produced assemblies per profile and independently show the reported warning/error totals. A warning appears again in each log's summary, but the reported total is one per profile.
- The ZIP hash and CRC are valid. All **83 payload member sizes/SHA256 values** match `archive-members.json`; that self-manifest is the 84th ZIP file. Archive manifests, preparation script, projects, logs, exit files and all 58 source snapshots match their corresponding retained local bytes. Output DLLs and direct SDK/dependency DLLs are identified by hashes in the receipt, not packaged as payload binaries.
- The preparation script was read, not run. Its referenced legacy recipe hash also matches the retained recipe. It adapts that managed-DLL approach to fresh, explicit Runtime/Editor/Test projects outside Assets and takes a source snapshot before compilation.
- Profile defines match the current version-dependent source and Runtime asmdef: Editor/test/Windows-host symbols in both; `UNITY_6000_0_OR_NEWER` through `_3` for 6.3; through `_6` plus `HTU_BACKDROP_FILTER` for 6.6. The latter corresponds to the Runtime asmdef's Unity `6000.6` version define. No unsupported blanket backdrop define was used for 6.3.

Qualifications that remain part of this PASS:

1. `Neo.Extensions` and Unity TestRunner DLLs were **precompiled/reused** and hash-checked, not rebuilt or certified for these SDKs. NUnit is the exact installed custom package DLL. Version-unification warnings **CS1701/CS1702 are suppressed** by the inspected projects, as in the earlier recipe; do not call the run warning-free or use it to certify dependency/runtime resolution.
2. These are managed API/compiler checks, not Unity's asmdef/import pipeline, serialization/asset loading, USS/UXML parsing, Test Runner discovery/execution, Player build, IL2CPP/AOT or physical input. The supplied fixture assertions did not execute; in particular the expected tokenless-OCE fault state and genuinely cancelled-task UI result still require native test execution.
3. The exact 6.3 and 6.6 profiles do not certify every Unity 6 version. In particular `UitkRootConnection.cs:53,73–86` has a distinct **6.5 PanelRenderer callback branch** (`UNITY_6000_5_OR_NEWER` true, `_6` false), which neither of these profiles compiles. No broader 6.0–6.6 compatibility claim is supported by this receipt alone.
4. Scope is these 56 module source files and 2 new fixtures. It is not a full NeoxiderTools dependency/package compilation; `VeloraShowcaseController` and the graphical `VeloraShowcaseInteractionTests` are outside the project Compile sets. C# compilation also does not validate the updated window's USS or native layout.

Disposition: retain this qualified API-only pass as valid evidence for the frozen source hashes. Unity-native execution of the **23-case draft fixture**, affected existing tests, two unchanged CTA diagnostic tests and G1–G7 remains pending under root's single-operator coordination.

## First actual Unity 6.3 execution: independent failure triage

Reviewed on **2026-11-02** local time. Root/operator executed the first actual graphics-enabled batch Edit Mode selection; this reviewer parsed its retained XML/log/launch record and read the implicated source, without launching Unity, a compiler or tests. Receipt directory: `D:/Temp/Neoxider-Audit-20261101/native-20261102/edit-new-contracts-and-drafts-u63-r1`.

The XML records **88 executed: 79 passed / 9 failed / 0 skipped**. `HtmlRuntimeWebContractsTests` is **65/65 passed**; the draft fixture is **14/23 passed, 9 failed**. `launch.json` records Unity `6000.3.14f1`, EditMode, explicit two-fixture filter, D3D11 with no `-nographics`, process exit **2**. The raw Editor log confirms the Unity version and real D3D11 initialization. XML execution ran from `2026-11-01 20:53:28Z` to `20:53:42Z` (2026-11-02 local time).

| Retained artifact | SHA256 independently read |
|---|---|
| `results.xml` | `b714cc341f9c17fcd66f370e65a6c060e06511e4917aedca1fa6f222deb9a02f` |
| `editor.log` | `a26270fcc2e6cf44e3a83f9ba729c87f9a8d9687ce52ba227741cb2d196568bd` |
| `launch.json` | `b41d445b2426f1398f07715c6fcbe76c1e1f697670ae6d0cc8b558e773d991a7` |

The 14 positive draft cases are: ordinary saved-job key preservation; both real bridge exception cases; genuinely cancelled-task completion; all three first-Save output variants; both fresh file/folder variants; missing-JSON regular-asset ownership; all three pending-run transitions; and startup failure visibility. This is real Unity Test Runner evidence, including the formerly unexecuted OCE-fault versus cancelled-task distinction. It remains evidence for those test contracts, not physical input or a rendered layout comparison.

### Eight failures are in the path observation helper

All six `DraftFirstSave_InvalidPerFileOrNormalizedCollisionFailsBeforeOverwriting` variants, `DraftFirstSave_CancelFailureAndReplacementSaveCancelKeepTheLiveOwner`, and `SerializedDirtyDraft_ReconstructsWithoutSavingOrLosingEdits` stop at `Assert.That(PathFromWindow(), Is.Empty)` (pre-fix fixture lines **239, 262, 300**). Each XML stack reports `NUnit.Framework.Constraints.EmptyConstraint.ApplyTo` rejecting a null actual value with `System.ArgumentException`. This is not an assertion that a nonempty job path appeared.

The shared helper at pre-fix line **521** parses `EditorJsonUtility.ToJson(_window)["_jobPath"]` and calls `AsString()` without establishing that the key is present and has string type. The raw serialized JSON was not captured. The receipt therefore establishes a null result in this observation chain, but does not distinguish a missing key, JSON null or another non-string value; it does not establish the actual `_jobPath` field's value. Calling the product Save/rebase path broken from these eight stacks would overstate the evidence.

Do not treat the full failed cases as passes or credit assertions that never ran:

| Failing group | Assertions reached successfully before helper failure | Assertions still unexecuted after that failure |
|---|---|---|
| Invalid/collision ×6 | Save rejected with the expected per-file diagnostic; existing destination bytes, live per-file payload and dirty state retained. | LastJobPath assertion at 240. NUnit teardown still runs after an assertion failure; the case remains Failed. |
| Cancel/write failure ×1 | Picker cancellation and replacement Save→cancel rejected; failed write rejected; live document JSON equality at 261. | Dirty flag 263, destination byte comparison 264 and LastJobPath 265. |
| Serialized dirty restore ×1 | Restored document equality 298 and dirty state 299. | No-JSON assertion 301, explicit restored Save and extension persistence 303–305. |

Recommended correction: observe the serialized field directly through `SerializedObject.FindProperty("_jobPath")`, require the property to exist and be `SerializedPropertyType.String`, then read `stringValue` without modifying it. These APIs explicitly address a serialized field and its string value. Keep an exact empty-path assertion. Do not replace it with `AsString("")`, a null coalescing fallback or `Is.Null.Or.Empty`, which would conceal failure to observe the field. `EditorJsonUtility` may still serve the separate full-document reconstruction test, with its observed round-trip assertions preserved. See [Unity 6.3 FindProperty](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/serializedobject/findproperty), [stringValue](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/serializedproperty/stringvalue), and [EditorJsonUtility.ToJson](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/editorjsonutility/tojson).

### Watch failure: fixture attachment is the leading source-supported cause

`DraftWatch_RebuildAndFirstSaveRunOnceAndIgnoreOutputAndUnrelatedFiles` fails in its **second** `AwaitRuns` at pre-fix line **549**, expected 2 submitted runs, actual 1. The first awaited run and the quiet-period assertion after output/unrelated notifications completed. XML contains the deliberate capturing bridge's one global failure report, but no captured panel state, live CSS document entries, watcher identity/directory list or relevance result at the second notification. It does not currently prove a production watcher defect.

The pre-fix fixture creates an EditorWindow instance and manually invokes `CreateGUI` in SetUp (`80–81`), without `ShowUtility` or another panel attachment. It later assigns `cssField.value` (`476`) and assumes the registered callback edited the document/restarted watching. Unity's exact **6000.3.14f1** `BaseField.value` implementation updates the field but dispatches its ChangeEvent only when a panel exists (`211–225`). A detached field can therefore display the new value while leaving `Document.Css` empty; after Save/rebuild, `JobInputPaths.IsInput` correctly regards that CSS notification as unrelated. This is an inference from the fixture and engine source, not a runtime panel measurement retained in the failing receipt. [Version-matched Unity BaseField source](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3.14f1/Modules/UIElements/Core/Controls/InputField/BaseField.cs#L211-L225)

The existing `Audit20261023NeoHtmlToUitkEditorWatchDependenciesTests` already uses the reusable solution: it calls `ShowUtility` and waits for the constructed controls before setting the same CSS field (`54–67`). No new watch implementation or longer timeout is justified by the current failure.

Required discriminators in the correction/recheck:

1. Attach the real test-owned window/panel, and establish `cssField.panel != null` before assigning `value`. Retain the production field callback path; do not green the test by assigning `Document.Css` directly or bypassing the watcher with `OnWatchFired`.
2. Immediately after the edit, assert the live CSS entries, dirty state and replacement of the prior watcher before Save. This separates a lost ChangeEvent from later path/watch failures.
3. After first Save and CreateGUI, assert that saved/live CSS still resolves to the same file, the current watcher is active and the public input classification includes CSS/excludes output and unrelated paths. Capture these states in a failure diagnostic if the second run still does not occur.
4. Keep duplicate current-watcher Notify calls, disposed-watcher Notify, exact run counts, temporary-job cleanup and the same debounce/timeout contract. OS filesystem delivery remains a separate existing test; public Notify does not establish it.

Disposition: **Editor acceptance remains FAIL; the source gate has reopened for the same worker's narrow correction and independent diff review.** The unchanged Web contracts retain their real 65/65 native Edit Mode pass. That result is not a real WebView/browser/backend or Player rendering pass, and does not close the Editor failures, CTA diagnostics or G1–G7. Rerun the corrected failed cases and all fixture behavior affected by attachment/lifecycle changes only after root freezes/reviews the new source; a shared SetUp change can warrant rerunning all 23 draft cases. The current evidence does not justify a production watcher change or weakened assertions.

## Source review of the native-r1 fixture correction

Reviewed on 2026-11-02 against `tasks/05-editor-native-fix.md`, the raw r1 failures above and the frozen pre-run fixture. **Source correction ACCEPTED; no remaining blocker found in this diff. Root may close the source gate for its affected Editor rerun.** The existing r1 outcome stays **14/23 passed / 9 failed** until a new executed receipt exists; this source review is not a corrected native pass.

Only the owned draft fixture and `EditorWindowUX.md` changed for this correction. Window/USS/meta hashes match their previously accepted values. Independent static comparison confirms all **12 test methods / 23 parameterized cases**, method names and parameter values are retained. All three exact `PathFromWindow(), Is.Empty` assertions remain; the OCE-fault and genuinely cancelled-task checks are unchanged. No unavailable attribute has returned.

The path helper now uses a scoped, read-only `SerializedObject`, calls Update, requires `_jobPath` to exist and have `SerializedPropertyType.String`, and returns `stringValue`. It neither accepts null permissively nor writes/apply-modifies the property. The original full-document Editor JSON reconstruction remains a separate test. Watch's successful Save now also checks the exact selected path, so the new observer covers both empty and nonempty serialized state.

Only the watch UnityTest attaches its own created window with the existing `ShowUtility` pattern; shared SetUp remains unchanged. A five-second bounded attachment wait and explicit root/field panel requirements fail clearly if the native prerequisite is unavailable. No `GetWindow` or other-window capture is added. The teardown flag closes this shown test-owned window after DiscardChanges, resets in finally and retains the previous destruction path for detached tests. It does not change production window or watch semantics.

The new assertions exercise the intended chain rather than skipping it:

- Before Save, native `cssField.value` must immediately change live `Document.Css`, retain dirty state, replace the prior watcher and leave that prior watcher with zero watched directories. No direct assignment to `Document.Css`, field callback invocation or direct Run bypass is added.
- First Save and rebuild must preserve the exact CSS physical target in both live and persisted documents, watch-enabled/debounce values and an active replacement watcher. Public input classification must accept the CSS file and reject output/unrelated paths. These are meaningful expected outcomes for known test-owned paths, not a mirror of the private path-normalization implementation.
- All disposed watcher references are notified and must remain silent. Duplicate current notifications, exact one-run counts, 0.2-second quiet periods, the original five-second AwaitRuns deadline and real runner temporary-job cleanup remain. Failure messages now include panel attachment, live CSS, watch/debounce, directory count, pending task status and runner state; they add diagnosis without changing acceptance.

`EditorWindowUX.md` correctly retains the failed native receipt, identifies the watch mechanism as source-supported with missing runtime panel measurement, records the observer correction without claiming the precise old JSON-envelope cause, and marks corrected execution Pending. Its native Web 65/65 result remains explicitly qualified as contract tests executed in the Editor.

Final reviewed hashes for root's new source freeze:

| File | SHA256 |
|---|---|
| `Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261101NeoHtmlToUitkEditorDraftTests.cs` | `4ec82c72929b747bef780fc657ebab1de96068029747a3220e2543ba06f632c2` |
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.cs` | `9d4f74cdf423c04e316ea14a4f3b1d8028766370a5e8a7377438deada5fee52b` |
| `Assets/Neoxider/Scripts/UI/HtmlToUitk/Editor/Window/HtmlToUitkWindow.uss` | `dce5e9ad25e93a2b8ce19e2ae610cbc8384a2e9ab1b4ee5ffc3766a10a76aab3` |
| `Assets/Neoxider/Tests/Edit/HtmlToUitk/Editor/Audit20261101NeoHtmlToUitkEditorDraftTests.cs.meta` | `1f9c65bc74dcb09fe998262059ac3c8c4db74d56ede31d9ab35ab99c8c7395bd` |
| `Docs/Audits/2026-11-01/EditorWindowUX.md` | `83001b40ff3885e85c3a4c599a10350e208e2b84be435b23f96cdabfad9ce603` |

Next evidence: root's coordinated graphical Edit Mode rerun of the nine failed cases (or the small full 23-case fixture), then relevant existing window/watch checks. The unchanged unrelated 65-case Web contract receipt need not be rerun for these test-only changes. All production visual/input checks, CTA diagnosis and G1–G7 remain separate open gates. This reviewer ran no compiler, Unity or tests and altered no product source.

## Actual Unity 6.3 affected rerun r2: independently verified PASS

Reviewed on 2026-11-02. **The corrected 23-case draft fixture and the selected existing affected contracts now have a real native Edit Mode PASS: 301/301, zero failed, skipped or inconclusive cases.** Root/operator ran a separate hidden Unity `6000.3.14f1` D3D11 batch; this reviewer only parsed the retained records, checked source/input identities and read relevant log sections. The r1 **79/88, including nine Editor failures**, remains retained above and is not reclassified.

Receipt: `D:/Temp/Neoxider-Audit-20261101/native-20261102/edit-drafts-affected-u63-r2/{launch.json,results.xml,editor.log,result-summary.json}`. Pre-run inputs: sibling `edit-drafts-affected-u63-r2-inputs/manifest.json` and `sources/`. Launch records PID 45772 and natural process exit **0**; the log independently ends with Test Runner completion/exit code 0. XML executes `2026-11-01 21:17:51Z`–`21:18:06Z` (2026-11-02 local time), with D3D11 initialization confirmed in the log.

| Actual selected group | Passed / executed |
|---|---|
| `Audit20261101NeoHtmlToUitkEditorDraftTests` | **23/23** |
| `Audit20261023NeoHtmlToUitkEditorWindowReplacementTests` | **10/10** |
| `Audit20261023NeoHtmlToUitkEditorWatchDependenciesTests` | **2/2** |
| `RunnerEditModeTests` | **2/2** |
| `Audit20261023NeoHtmlToUitkEditorPipelineTests` | **8/8** |
| `Neo.HtmlToUitk.PureLogicTests` fixtures | **256/256** |
| Total | **301/301** |

Independent receipt checks:

- Raw XML contains exactly 301 test-case nodes, all Passed, no failure nodes, no skipped/inconclusive outcomes and exact agreement with the summary's fixture counters. The executed draft method coverage matches all 12 retained method names / 23 parameterized cases in the frozen source. No test was silently removed to close r1.
- The frozen fixture is the accepted `4ec82c72929b747bef780fc657ebab1de96068029747a3220e2543ba06f632c2`. It retains all three strict empty-path assertions, required SerializedObject property existence/type, attached native field editing, immediate live CSS update, watcher replacement/disposal, saved/live path/debounce/classification checks, exact notification counts and temporary-job cleanup. All 23 executing successfully therefore covers the previously unreached post-helper assertions and the second watched CSS submission on this revision.
- The input manifest was written before launch (`21:17:05.704980Z`, launch `21:17:15.487604Z`). All **332 current input files and all 332 pre-run copies** independently match its SHA256 and byte-size records. These are module/test/showcase input files, not 332 C# source files. The current 12 protected path existence/hash states also match the pre-main-merge baseline, with zero differences; intentionally absent protected paths remain absent.
- Both raw log/result hashes and launch hash agree with the summary. No compiler error or teardown failure was found; the XML's zero failures and successful final Test Runner exit also cover fixture cleanup. Deliberate mock-runner `FAIL` debug messages are expected fixture output, not failed NUnit cases. This is not a claim that every Editor/plugin log line is warning-free.
- The non-private restoration receipt records exact original EditorPrefs name/type/data restoration and `PrivateValuesPublished: false` at `21:19:31.8893147Z`. No private before/after snapshots or preference values were copied into this report. This review did not query or alter live preferences while root's separate CTA batch was active.

| Evidence file | SHA256 independently checked |
|---|---|
| `launch.json` | `41e246308d8b93bd5f0ca814500065012eb931a6465566e173a7da669dd5d395` |
| `results.xml` | `94f0c59aea97bcc38a30d856ddc2968aeaf1c29ce69af4431ef96f2c1e382823` |
| `editor.log` | `b601e3cbbec796be46bb3109300f5fd557fcf92ec5cbb47332fe2a15aeb4660b` |
| `result-summary.json` | `9bcde345092304115db7fb43371a7fe1408009409eafb798fdf29d0e2ebbbd6e` |
| Pre-run input `manifest.json` | `aa111e797c219a95ef921a5f7c3ac3f8bd241b38a69666e52b2423bed0f84571` |

Disposition: **the r1 Editor test blockers are closed by the corrected source plus this native rerun**, within the exact Unity 6.3 Edit Mode selection above. The separate unchanged Web contract **65/65** native receipt remains valid and was not redundantly included in r2. The new batch is not an additional 301 unique cases on top of overlapping historical Editor results.

Remaining scope: the attached-window test proves its programmed callback/watch path, not physical input or a visual review of Sources/Settings/Results at 400×420 and other sizes. Successful real converter output/asset refresh, normal scene Game View/camera, Player/IL2CPP, other Unity-version native execution, realtime browser-versus-Unity video and visual parity remain separate. **G1–G7 are not closed as a whole by this run.** Root's exactly-two-CTA batch is a different active receipt and was neither inspected nor controlled in this review. No compiler, Unity process or test was launched by this reviewer; product source was not changed.

## CTA baseline r1 and exact-readback instrumentation source review

Reviewed on 2026-11-02 against `tasks/02-cta-native-capture.md`. **Optional exact-readback instrumentation is source ACCEPTED, with no blocker found. Root may freeze this source for its capture-enabled two-test rerun.** This is diagnostic instrumentation, not a product correction or a CTA pass. This reviewer launched no compiler, Unity/process, test or registry operation and changed no product/test source.

The separate actual CTA r1 receipt was parsed directly: `D:/Temp/Neoxider-Audit-20261101/native-20261102/play-cta-u63-r1/{launch.json,results.xml,editor.log,result-summary.json}`. XML contains exactly the desktop and mobile CTA tests, **0 passed / 2 failed / 0 skipped / 0 inconclusive**; launch records natural exit 2, Unity `6000.3.14f1`, D3D11, Linear. Raw evidence hashes agree with the summary:

- `launch.json`: `b889d1ff35082790e3b116ae653a84996f378237cf0dcd0634d9c318b17439cc`.
- `results.xml`: `bc039f4f9d1ae21fab1b1d91d092fc6cfc45c3d194b7c4c4912b5dae9771f7fa`.
- `editor.log`: `d713af09d6ca035a4f462fc8022dfa10365b8ce88a2f4e6814b0f2776e19937e`.

Both fail the painted-background assertion with **0/8664** expected-color matches. Raw/linear/gamma/mirrored counters are all zero in both; target and readback report `R8G8B8A8_SRGB`, target sRGB true. The sparse samples are all opaque and include varied colors (101/256 desktop, 101/257 mobile); flipped samples are white and unflipped samples are pale blue. This is evidence against treating the sampled readback as simply empty, not a confirmed whole-frame/color-space/orientation diagnosis. The diagnostic foreground counter is 5244 in both, but its assertion and the subsequent pointer/modal checks are **unexecuted after the background failure**. White sampled pixels are not proof of rendered label glyphs.

The source diff was reviewed in full:

- Two standard-library imports, one capture call and one new helper are the only changes. Removing exactly those additions independently reproduces the retained original fixture bytes and SHA256 **`5a90b705abc5bf75b9d99be9e5270453a1ea114567a39c004e8a3a736f08c4cb`**. All 16 UnityTest methods remain. Consequently the original thresholds, format constructors, frame waits, index math, diagnostic counters, picking, pointer/modal expectations and cleanup were not edited.
- `CaptureCtaReadbackIfRequested(image)` runs immediately after the existing ReadPixels/Apply and encodes **that same full-frame Texture2D**. It does not read another target, crop, redraw, apply a color conversion or add a render/scheduling barrier. The subsequent assertions continue reading the same texture. Encode/write time is optional diagnostic overhead after sampling, not a rendering-performance measurement.
- Blank/unset `NEO_VELORA_CAPTURE_DIR` returns before path/filesystem work. Normalized project-root and descendant paths are rejected before writing. Root's stated contract is a fresh task-owned outside-project directory for each run; fixed filenames include actual width/height and distinguish desktop/mobile. This review does not convert the test helper into a general storage/security API.
- PNG bytes are written directly and their SHA256, dimensions, length and full destination are logged. Capture/path/write errors propagate as explicit test failures; original finally still restores `RenderTexture.active` and destroys the texture. No silent skip or acceptance fallback is introduced.
- The code reuses the existing `EncodeToPNG`/System.IO capture convention in `Release11HtmlToUitkEndToEndTests.CaptureAndAssertGradient` and standard SHA256 usage. No new dependency, asmdef, product UI or configuration change is present.

Source-accepted identities:

| File | SHA256 |
|---|---|
| `Assets/HtmlToUitkShowcase/Tests/PlayMode/VeloraShowcaseInteractionTests.cs` | `19c360a07b0eee2482ee5749d9cd33add2f494b76e24545f92817c5d478736e1` |
| `Docs/Audits/2026-11-01/CTAReadback.md` | `5ff7dc0b1f03e1de64677dd46b21e5e8c692c8a03e604faa3d0a192c7c112f94` |

The worker report accurately distinguishes root's executed **2 FAIL** baseline from source-only capture work and the pending capture-enabled run. Root should use its exact two-test graphics filter, set the environment variable only for the child run, retain both full-frame PNGs and their log lines, then verify PNG dimensions/length/hash and inspect the actual crop/image before choosing a confirmed fix. Merely adding this helper does not resolve G2 or any parity/input gate.

The native Editor **301/301** receipt remains valid: this test-only CTA instrumentation does not modify the draft fixture or its module dependencies, and its accepted draft fixture hash remains `4ec82c72929b747bef780fc657ebab1de96068029747a3220e2543ba06f632c2`. G1–G7 and physical/video/Player scope remain separately open.

## CTA depth/stencil correction: independent source review before r3

Reviewed on 2026-11-02 against `tasks/02-cta-stencil.md` and executed source `19c360…`. **Source ACCEPTED; no blocker. Root may freeze the exact-two-test D3D11 r3 experiment.**

- Both initial and resized targets request depth 24. Created-target, actual depth and stencil prerequisites fail explicitly; clearing defaults remain unchanged. Diagnostics record actual depth/format/clearing.
- The exact Unity 6.3 public API declarations and depth/stencil masking rationale support the proposed prerequisite. This remains a harness hypothesis pending r3, not confirmed causation. [PanelSettings](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3.14f1/Modules/UIElements/Core/GameObjects/PanelSettings.cs#L499-L537), [GraphicsFormatUtility](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3.14f1/Runtime/Export/Graphics/GraphicsFormatUtility.bindings.cs)
- Reversing only the new import/comment, two depth arguments, prerequisites and diagnostic fields reproduces **`19c360a07b0eee2482ee5749d9cd33add2f494b76e24545f92817c5d478736e1`** exactly. All old thresholds, formats, indices, timing, pointer/modal checks, capture/cleanup and **16 tests** are retained. No product edits.
- Preserved r2 XML/log hashes and both PNG dimensions/bytes/hashes were independently checked; both local images were viewed and show the white CTA capsule without readable text. This confirms sampled-target symptoms, not normal Game View behavior. `CTAReadback.md` retains r1/r2 failures and r3 Pending accurately.

Accepted fixture SHA256: **`bdb01e74490b7442b326a1ab46f115a0559e22faf405e58981ed9df862196be6`**. Reviewed report SHA256: `46c85e203ec69ca4c8b66c9c0735e76c22a1001a140b7cb149a5c26f7eb66084`.

If r3 is 2/2 PASS, run the full shared-allocation **16-test fixture**. Existing Editor 301/Web results remain valid and need no redundant rerun. No compiler/process/test was launched by this reviewer. Physical input, normal Game View/camera, video and browser parity remain separate open gates.

## Native CTA r3 and full affected fixture: verified PASS

Reviewed on 2026-11-02 from `native-20261102/play-cta-u63-r3` and `play-velora-full-u63-r1`. **r3 is 2/2 PASS; the subsequent full affected fixture is 16/16 PASS**, both natural exit 0, zero failed/skipped/inconclusive cases, Unity `6000.3.14f1` / D3D11 / Linear. All 16 frozen UnityTest method names executed. Source is accepted **`bdb01e74490b7442b326a1ab46f115a0559e22faf405e58981ed9df862196be6`**; thresholds, color formats, scheduling and pointer/modal assertions remain unchanged.

| Receipt | Desktop painted background / glyph pixels | Mobile painted background / glyph pixels |
|---|---|---|
| Preserved depth-zero r2: 2 FAIL | 0/8664; diagnostic 5244 | 0/8664; diagnostic 5244 |
| Depth/stencil r3: 2 PASS | 7708/8664; 624 | 7707/8664; 630 |
| Full 16-test run: PASS | 7708/8664; 631 | 7707/8664; 631 |

Actual framebuffer diagnostics in both passing runs are **depth 32 / `D32_SFloat_S8_UInt` / `clearDepthStencil=True`**. The requested depth-24 allocation selected that supported combined format. Original pixel assertions pass, and subsequent synthetic pointer/modal assertions complete. Both r3 exact readbacks were visually reviewed against preserved r2: blue CTA with readable white text replaces the white capsule. Full-fixture capture metadata/hashes also match the XML and PNGs; its mobile image was viewed. This confirms the missing framebuffer prerequisite as the isolated harness failure in this exact profile, rather than requiring product styling changes. It does not establish normal Game View behavior.

Independent checks: raw receipt hashes match summaries; PNG IHDR dimensions, byte counts and SHA256 match `captures.json` and XML capture output; both 332-file pre-run snapshots/current inputs match their manifests; 12 protected path states remain unchanged. No compiler/teardown failure was found. r3 XML SHA256 is `81f9ce318b89765d1f35f03653a14bed366c5efc9b0de450de9dc1e574d6b4d5`; full-fixture XML is `1f4a6be75bd5471e61ff5724b38ef1f860b33f6eced4c0751e2aa4629d85b9fc`. R1/r2 failed receipts remain preserved. Archive integrity is assigned separately to NativeClosure; this is semantic acceptance of the raw executions, not a second ZIP audit.

The 16-case pass covers local four-page/modal navigation, forms and invalid-email checks, search/scroll return, binding/reload lifecycle, theme/reduced-motion state, breakpoints, localization, aurora scheduler/gradient-resource contracts, **390 px Pricing natural stacking without overlap**, and **synthetic native wheel/slider-thumb paths reaching the footer at desktop/mobile sizes**. It does not prove every menu/FAQ/clipboard action, physical input, all animation trajectories/timing, camera/Game View, browser parity or realtime video. The two CTA tests are included in the full 16 and must not be counted as 18 unique tests.

Disposition: **CTA readback and the shared-allocation Play Mode regression gate are closed for this Unity 6.3 profile.** Existing Editor 301/Web 65 receipts remain valid in their own scopes. G1 and the remaining G3–G7 visual/manual/device scope remain open; this is qualified demo/WIP evidence, not release/main or full historical-plan closure. No process, compiler, Unity or test was launched or controlled by this reviewer; no product source was changed.
