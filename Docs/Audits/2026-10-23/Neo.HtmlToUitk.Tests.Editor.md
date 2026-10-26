# Neo.HtmlToUitk.Tests.Editor: audit

Status: source audit complete; scoped fixes, regression sources and documentation ready for integration. Compilation and test execution remain pending with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Tests/Edit/HtmlToUitk/Neo.HtmlToUitk.Tests.Editor.asmdef`.
- Scope: all 12 manifest C# files reviewed completely, 2,460 original source lines; four new regression files added. Nested assemblies and other workers' prefixed regressions excluded from ownership.
- Constraints observed: no commits, shared configuration/asmdef changes, Unity/MCP, builds, test runners, installs or child agents.

## Scope

Paths below are relative to `Assets/Neoxider/Tests/Edit/HtmlToUitk`.

| Owned file | Full review and outcome |
|---|---|
| `Editor/JobAssetEditModeTests.cs` | Reviewed JSON/unknown-key preservation, ScriptableObject destruction, configured folder policy, exact manifest/hash/path checks and filesystem cleanup. Existing positive and negative ownership assertions retained. No demonstrated contract defect requiring an edit. |
| `Editor/RouterBackendEditModeTests.cs` | Reviewed initial visibility, synchronous detached-tree completion, event ordering, modal/tab state, missing elements and trigger discovery. Existing discovery checks do not dispatch triggers; the new panel regression covers actual subscriptions. |
| `Editor/RunnerEditModeTests.cs` | Reviewed bridge injection, strict flag, output path, selected-file job contents, temporary-file deletion and global state. Fixed override restoration/isolation; added source-document and persistent-job preservation assertions. Extended the existing stub for failure injection. |
| `Editor/RuntimeOwnershipTests.cs` | Reviewed Play Mode entry/owned teardown, disabled components, real UIDocument/render texture cleanup, window panels, action token ownership, responsive leases, transition/focus suppression, animator subscriptions and inline-style restoration. Changed only the class declaration to `partial` to reuse its backend/panel helpers from the new regression file. |
| `PureLogic/AnimationTests.cs` | Reviewed parsed metadata, timeline phases/directions/iteration boundaries, fill, stagger, instance event state, interpolation/keyframes, reduced motion, easing and premultiplied color assertions. No demonstrated test defect requiring an edit. Native scheduler/render behavior remains a separate integration gate. |
| `PureLogic/EditorLogicTests.cs` | Reviewed fake filesystem semantics, glob/path cases, diagnostic counts/status, job round trips, CLI quoting/extraction, debounce timing and report freshness/fallback. Fixed unconditional directory leak and the malformed-output test's wrong branch. Made only the report-reader fixture partial for new cases. |
| `PureLogic/JsonTests.cs` | Reviewed ordered nested values, missing/null chains, stable writes, Unicode/BOM handling, syntax errors/line numbers and replacement semantics. No demonstrated test defect requiring an edit. |
| `PureLogic/NavigationTests.cs` | Reviewed start/stack/history policies, layered visibility, modals, tab persistence, focus memory, transition precedence/direction, reduced motion and visit limits. Corrected misleading default-Show scenario identification; added explicit middle-stack pop coverage in a new file. |
| `PureLogic/OwnershipLogicTests.cs` | Reviewed fail-closed diagnostic verdicts, process/report contradictions, malformed media constraints, nonfinite viewports and exact generated-texture ownership. Signature-only fixture bytes exercise the ownership validator, not PNG decoding or actual import. |
| `PureLogic/PrototypeConformanceTests.cs` | Reviewed every sequential scenario row and assertions for state/steps/transitions, modal rejection and reduced motion. This is a documented C# contract replay, not evidence that JavaScript or rendered browser behavior was executed. |
| `PureLogic/ResponsiveSelectorTests.cs` | Reviewed inclusive bounds/orientation/aspect, ordered media sets, selector node ancestry and rejected syntax, blur iteration caps and level sizes. No demonstrated test defect requiring an edit. |
| `PureLogic/RouterInvariantTests.cs` | Reviewed all six seeded 4,000-operation scenarios, independent visible/modal model, failed-plan invariants, stack/history/tab/visit checks, and every UiKitMapping case. Those are defined workloads, not executed results from this audit. |

Read-only dependency context included runner/bridge/report APIs, `CliCommandLine.ExtractJsonObject`, `InputExpander`, generated-texture validation, `RouterCore` options, `IPageBackend`, `UitkRouter`, `UiDocumentPageBackend`, `BreakpointController`, the assembly definition and the canonical module guide. Relevant skill references and the test-value gate were consulted. Existing stubs/backends and public APIs were reused; no new runtime utility or dependency was introduced.

Across the inventory, checked lifecycle/resource ownership, failure cleanup, null and boundary assertions, serialization preservation, dependency isolation, logging and Unity-version exposure. The changes add no runtime logs, serialized fields or obsolete Unity object-ID calls. The assembly's existing references cover all new source dependencies; no reference request is needed.

## Findings and fixes

### F1 - Low: report-reader test leaks its directory on every invocation - fixed

Source: `EditorLogicTests.cs`, `ReportReaderTests.MissingReportBecomesAFailureCarryingStderr`.

`NewTemp()` created a directory directly inside the request initializer; no variable or cleanup path retained it. Both a passing test and a failing assertion left that directory behind. The test now retains the path and deletes it in `finally`. All three report-reader tests that call the existing `NewTemp()` helper now pair the allocation with cleanup; the new file does the same.

### F2 - Medium: runner fixture overwrites preexisting global bridge state - fixed

Source: `RunnerEditModeTests.cs`, `SelectedFilesRunThroughATemporaryJobThatIsDeletedAfterwards`.

The test always assigned `BridgeOverride = null` on exit. If a containing fixture/operator supplied a non-null bridge, subsequent runs silently selected another backend, potentially the external CLI. The test now restores the exact saved `IJobBridge`. Its fixture is `[NonParallelizable]` because the override is process-wide. Both the original test and new bridge-failure regression restore the override before filesystem cleanup.

The success path also now creates a real persistent job file and checks that both its contents and the original `JobDocument` survive an `OnlyFiles` run unchanged.

### F3 - Medium: malformed-report regression did not reach the parser - fixed

Source: `EditorLogicTests.cs`, `MalformedStandardOutputJsonIsReportedNotThrown`; context: `CliCommandLine.cs:60`, `ExtractJsonObject`.

The old stdout had an opening object brace but no closing brace. `ExtractJsonObject` therefore returned null, and the assertion accepted the generic missing-report failure. Removing the malformed-report catch would not have failed that test. The payload is now a complete invalid object, and assertions require one error plus the malformed-report diagnostic. New cases cover an invalid fresh report file with valid stdout fallback, missing verdict, and malformed-file diagnostics retaining stderr without fallback.

### F4 - Medium: named pop-to-existing scenario exercised default stack clearing - fixed coverage

Source: `NavigationTests.cs`, formerly `ShowToPageAlreadyOnStackPopsBackToIt`; context: `RouterOptions.ShowToExistingPopsToIt` defaults to false.

The test used default options and navigated to the first stack page. Clearing the whole stack produced the same asserted result as popping to that first page, so the test could not protect preservation of earlier history. It now accurately names/asserts the default clear-history scenario. Four new public `RouterCore` cases explicitly enable pop-to-existing, target a middle page, retain earlier history and verify plan visibility for ordinary/layered hosts with either `ShowClearsStack` value.

Cleanup/restore fixes were verified by scoped source inspection rather than tests that call other test methods. The new tests observe actual public outputs, state, files, callbacks and focus; none is a source-text or no-throw NUnit test.

### F5 - Medium: stale WillHide after redirect from Navigated - runtime-owner change observed

Initial dependency inspection showed this control flow: `Start(home)`; `Navigate(next)` raises `Navigated:next`; an `OnEvent` subscriber redirects to `home` with `NavMode.Show`; the outer execution then raised `WillHide:home` before checking its generation. The newly current home page could consequently receive a stale hide notification and start an unintended animation.

Added `RedirectingFromNavigatedDoesNotPublishWillHideFromTheSupersededPlan`: it checks the final stack and requires only `WillHide:next`. This is a source-confirmed reproduction, not an executed failure receipt.

During integration inspection, the concurrent runtime worker had already added the generation check before `RaiseStep` in `UitkRouter.Execute`, plus guards after external callbacks. The read-only diff confirmed the relevant fix; this worker did not modify runtime source. Inspected runtime SHA256: `afb5f6f9e27a37490174d12882f5aa9f205d9b6fd7856631ef988cf4173ce717`. The coordinator must integrate that owner change with this regression and run it; the issue is not claimed verified at runtime.

### Added regressions

All four filenames begin with the exact assigned prefix `Audit20261023NeoHtmlToUitkTestsEditor`. Counts are source-discovered cases, not pass counts.

| New file | Cases | Observable contracts |
|---|---:|---|
| `Editor/Audit20261023NeoHtmlToUitkTestsEditorRunnerRegressionTests.cs` | 1 | Injected bridge exception after reading the temporary job; selected input and unknown keys; temp deletion, released running flag, unchanged document/permanent file, restored override. Accepts propagation of the injected exception or a structured fault, never success. |
| `Editor/Audit20261023NeoHtmlToUitkTestsEditorRuntimeRegressionTests.cs` | 6 | Both hide/show completion orders with positive completion/focus assertions; disposal releases backend handles without late events; navigation-event redirect; authored responsive classes sharing managed names and actual replacement-owner updates; real-panel trigger counts across rewire/disable/unwire/dispose. Includes one `UnityTest`. |
| `PureLogic/Audit20261023NeoHtmlToUitkTestsEditorNavigationRegressionTests.cs` | 4 | Explicit pop-to-existing with a middle target, preserved earlier history, correct layered/ordinary steps, and working Back afterward. |
| `PureLogic/Audit20261023NeoHtmlToUitkTestsEditorReportReaderRegressionTests.cs` | 3 | Malformed/invalid fresh report fallback, unchanged file contents, and useful malformed-file failure diagnostics. Fixed UTC timestamps avoid wall-clock freshness races. |

Total: 14 new cases, 13 ordinary NUnit cases and one Editor-panel `UnityTest`; nine new test methods. Existing test fixture helpers are shared through partial declarations instead of duplicating backend/stub implementations.

## Documentation

Created the owned narrow guide `Assets/Neoxider/Docs/UI/HtmlToUitkTesting.md` because that manifest-assigned file did not exist. It links to the canonical module usage guide and explains public C# versus scene-component checks, explicit routing policies, completion/focus observations, real trigger dispatch, bridge/filesystem isolation, responsive class ownership and centralized execution limits. The canonical parent guide remains owned elsewhere; the coordinator should link this testing topic from it.

Added unique `.meta` files for the four new C# files and the guide. No new folders or folder metadata were needed. No other public docs were edited.

Кратко: проверены все 12 исходных файлов; исправлены утечка временной папки, восстановление глобального bridge и ложное покрытие двух сценариев. Новые регрессии проверяют реальные файлы, состояние, события и фокус; запуск Unity остаётся за координатором.

## Substantial improvement proposal

### Deterministic router/backend conformance scenarios - proposed, not implemented

**Problem.** Pure `RouterCore` plan tests and the native ownership fixture protect different layers. Negative stale-completion assertions can miss a router that never publishes valid completion. Testing only the first stack target can miss a history-policy regression. Native fixtures also combine many lifecycle operations, making ordering failures harder to isolate. F3-F5 demonstrate why explicit branch and callback-order contracts matter.

**Architecture/API.** Add a small internal test-support scenario type, shared by this assembly's tests, using the existing public `IPageBackend`, `IPageTransitionHandle`, `IRouterHook` and `UitkRouter` APIs. Proposed operations: `Complete(TransitionSlot slot)`, `RedirectOn(RouterEventKind kind, string pageId, NavMode mode)` and immutable event/focus snapshots. `TransitionSlot` identifies a pending page/modal operation by kind and id, avoiding fragile numeric handle indices. Offer immediate and manually completed backend implementations, plus an `IPlanAwareBackend` variant. Keep the fixture oracle to explicit expected state/events; do not reimplement `RouterCore`.

Run the same small conformance scenarios through these backends and a focused real-panel adapter fixture. No runtime public API addition is required. If a reentry-policy API is later desired, its owner must specify the behavior separately; the harness must not silently impose one by copying current implementation details.

**Compatibility/migration.** Introduce test support incrementally, migrate transition/focus tests first, and retain existing case identities or document any rename in the central test selection. Keep runtime signatures, assets, serialized fields and asmdefs unchanged. Continue separating pure C# tests from operations needing a real panel or explicit Play Mode ownership. Existing historical verification receipts stay historical until rerun.

**Acceptance scenarios.** Both hide/show completion orders publish completion once and focus only after the final pending operation. Supersession/disposal completes or releases backend work without stale events/focus. Redirect/dispose from `Navigated`, will-events and tab events cannot dispatch remaining obsolete operations. Plan-aware and per-step backends agree on final public state. Rewiring invokes one callback, and authored responsive classes survive ownership replacement. Runner/report scenarios retain isolated files and global overrides even on injected failure.

**Effort/risk.** Approximately 1-2 developer days for the focused helper and migration, followed by one centralized Unity validation session on each supported verification version. Main risks are overgeneralizing the harness, duplicating the production state machine, and treating a fake backend as rendering proof. Limit it to existing public contracts, retain a small native-panel suite, and keep screenshot/build acceptance separate.

## Verification and remaining risks

### Checks actually performed

- Read `AGENTS.md`, `WORKER-CONTRACT.md`, the exact manifest entry and the original dedicated report completely; reviewed every original inventory file.
- Ran an inline Python static source inspection over the 12 manifest files plus four owned prefixed files: strict UTF-8 decoding, balanced C# lexical delimiters/literals, explicit-local policy, absence of obsolete object-ID API calls, final newlines and trailing whitespace. Exit code 0. This is lexical inspection, not compilation.
- Independently decoded the new valid JSON fixtures with Python's JSON parser; confirmed the corrected malformed stdout is extractable as a complete object but fails JSON parsing. Source case count: 14. Exit code 0.
- Validated the existing asmdef's runtime/editor references without editing it. New partial fixtures reuse existing accessible private helpers and require no added assembly reference.
- Checked all relative links in the testing guide, all five new metadata GUID formats, UTF-8/newlines/whitespace. Exit code 0. An `rg` search across `Assets/**/*.meta` found each new GUID exactly once.
- Inspected the tracked source diff and the runtime-owner diff for the redirect finding. No other worker's source, tests, docs or shared configuration were changed.
- Ran `git diff --check --` restricted to the owned source files, new files/metadata, guide and dedicated report: exit code 0. New/untracked file whitespace was additionally checked directly because ordinary git diff does not inspect untracked content.

The first static inventory guard rejected additional files created concurrently by other assembly workers. Validation was corrected to use the manifest plus this worker's exact prefix; seven foreign-prefixed regression files were present at that check and remained read-only.

Owned C# receipt: sort the 16 owned relative paths, concatenate each `path:SHA256(file bytes)` plus newline, then SHA256 that UTF-8 text. Result: `45944f1c727524f7660ec345b84412c163150d6cb3a68cf869197f16aa3ce8e0`.

Testing guide SHA256: `1597db83a583712401403206c47e4083aebf7f84128ec573d4e4470d328d1976`.

### Pending integration checks and coordinator actions

1. Compile and run `Neo.HtmlToUitk.Tests.Editor`, including the 14 new cases, with the concurrent runtime/editor owner changes. Neither NUnit/Unity tests nor source-check builds were executed here. Verify both supported Unity API branches centrally; lexical checks establish neither branch compiles.
2. Retain the runtime generation guards for F5 and verify the redirect regression. The recorded source snapshot had the relevant owner fix; behavior remains unexecuted.
3. Link `HtmlToUitkTesting.md` from the canonical module guide. Refresh any historical test-count claims only after central execution.

Play Mode, console inspection, pointer/native focus behavior, rendered appearance, screenshots, actual texture import, player builds and performance were not exercised under the worker contract. No assembly references, installs or other shared changes are requested.
