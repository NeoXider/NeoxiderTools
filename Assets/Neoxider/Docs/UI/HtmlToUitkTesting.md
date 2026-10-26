# HTML to UI Toolkit testing contracts

The `Neo.HtmlToUitk.Tests.Editor` assembly tests the public contracts described in [HTML/CSS to UI Toolkit](HtmlToUitk.md). Tests live in `Assets/Neoxider/Tests/Edit/HtmlToUitk`; the assembly is Editor-only and requires `UNITY_INCLUDE_TESTS`. It references the runtime, editor and Unity Test Framework assemblies. The external converter is not required for these fixtures.

See [editor tooling](HtmlToUitkEditor.md) for job authoring, `RunAsync`, bridge selection and generated-asset import. These two package guides and this testing topic provide the runtime, editor and verification entry points after UPM import; a source-repository audit checkout is not required to follow them.

## Choosing the appropriate layer

| Layer | Public API and observable result |
|---|---|
| Pure C# | `MiniJson` round trips and diagnostics; `RouterCore` plans, history, modal and tab state; `AnimTimeline`, sampling and easing; media queries, selectors, blur planning, jobs, globs and report selection |
| Editor tooling | `HtmlToUitkJob` serialization, exact generated-texture ownership and `HtmlToUitkRunner` requests, source-job preservation and temporary-file cleanup |
| UI Toolkit adapters | `UitkRouter` completion events and focus, trigger subscription lifetime, `UitkActionBindings`, `BreakpointController` class ownership and animator teardown |
| Scene components | Disabled `UitkScreen` and `BreakpointWatcher` configuration followed by enable/disable, using a real `UIDocument` and render texture |

For gameplay integration, test the C# command reached by `RegisterAction` separately from the UI trigger. For Inspector setup, assign the generated UXML to `UIDocument`, configure `PanelSettings`, and assign the optional navigation, animation and media `TextAsset` files to `UitkScreen`. The scene workflow and `ConfigureRoot` C# workflow should produce the same enabled/disabled lifetime behavior. See the module guide for setup; these fixtures do not establish visual or browser parity.

`ConfigureRoot` and `ReloadRoot` on a disabled `UitkScreen` retain the requested root until enable; they do not activate its projection. Disable releases the active root, callbacks and responsive ownership. `RegisterAction` returns a disposable registration token; the owner releases it when the action is no longer needed. If another component owns responsive layout, configure the screen with `ownResponsiveLayout: false`. Inspector configuration supplies the same metadata as the typed C# APIs; it does not introduce a JavaScript engine or another NoCode execution layer.

## Deterministic navigation checks

Use `RouterCore` for synchronous state and plan assertions. Set the policy being tested explicitly: default `NavMode.Show` clears history, while `ShowToExistingPopsToIt = true` preserves entries below an existing target. A middle stack target distinguishes these policies; returning to the first page alone does not. The audit regression covers layered and ordinary visibility with both `ShowClearsStack` values.

Use an `IPageBackend` with manually completed `IPageTransitionHandle` instances for transition lifetime tests. Verify both completion orders of hide/show, actual `OnHidden`/`OnShown` events, the focus target and `IsTransitioning`. A test that only suppresses callbacks after disposal could also pass if valid completions never ran.

Trigger tests use a real Editor window panel and pooled `NavigationSubmitEvent` instances. Counting `OnBackOnRoot` detects duplicate subscriptions even when a second navigation to the same page would otherwise be ignored. Repeated `WireTriggers`, disabled controls, `UnwireTriggers` and disposal must each have an observable callback count.

The regression `RedirectingFromNavigatedDoesNotPublishWillHideFromTheSupersededPlan` checks redirecting to home from an `OnEvent` navigation callback. The superseded plan must not emit `WillHide:home` after that redirect. The runtime generation guards and this regression require native execution before the behavior can be claimed verified.

## Files and global state

Runner tests substitute `IJobBridge` through `HtmlToUitkRunner.BridgeOverride`; they never launch the converter. Save the previous override and restore it in `finally`, including when it is non-null. Run bridge, scheduler and Editor-window fixtures in an isolated sequence controlled by the test operator. The current fixtures do not establish isolation through a `NonParallelizable` attribute; historical type-check receipts do not establish scheduling behavior. Any replaced process-wide delegate must likewise be restored to its exact prior value. Assert the original `JobDocument` and persistent job file remain unchanged when `OnlyFiles` creates a temporary job. Cover bridge failure as well as success, including deletion of that temporary job and release of `IsRunning`.

Every created temporary directory needs a cleanup path on assertion failure. Keep fixture files outside imported `Assets` unless the contract specifically requires a project-relative location. Generated-texture checks use exact paths, hashes and policy; a directory named `Generated` or matching PNG bytes alone cannot establish ownership.

Report selection tests set explicit UTC file timestamps. Malformed stdout must contain a complete but invalid JSON object to reach the parser branch: an unfinished object is treated as missing JSON. Assert the diagnostic reason as well as `FAIL`. Cover an invalid fresh report file both with valid stdout fallback and without fallback, preserving the parse reason and stderr.

Responsive ownership tests must include an authored class whose name is also controlled by a media rule. Check that inactive queries and disposal preserve it while removing classes the controller added, then verify a replacement controller actually updates the same root.

## Execution and evidence

A single Unity operator runs the complete assembly in EditMode after the implementation source barrier closes. One existing `UnityTest` explicitly enters Play Mode for the disabled-component lifecycle and exits through its owned `UnityTearDown`; other panel tests open and close temporary Editor windows. Do not run these alongside another operator using the Editor. No fixture should save a scene, prefab or project settings.

The retained `Audit20261023NeoHtmlToUitkTestsEditor` regression sources define fourteen cases in four files. These are authored acceptance cases, not pass counts:

| Source under `Tests/Edit/HtmlToUitk` | Defined cases | Required observable result |
|---|---:|---|
| `Editor/Audit20261023NeoHtmlToUitkTestsEditorRunnerRegressionTests.cs` | 1 | Injected bridge failure releases the temporary job and running flag while preserving the original document and persistent job file. |
| `Editor/Audit20261023NeoHtmlToUitkTestsEditorRuntimeRegressionTests.cs` | 6 | Both completion orders publish and focus once after all pending steps; disposal and redirect suppress stale work; authored responsive classes survive replacement; trigger counts stay `1,1,1,2,2` through dispatch, disable, unwire, rewire and disposal. |
| `PureLogic/Audit20261023NeoHtmlToUitkTestsEditorNavigationRegressionTests.cs` | 4 | Showing a middle stack page preserves earlier history with explicit pop-to-existing policy under both visibility and clear-stack settings. |
| `PureLogic/Audit20261023NeoHtmlToUitkTestsEditorReportReaderRegressionTests.cs` | 3 | Invalid fresh reports fall back to valid stdout or expose the parse reason and stderr; reading leaves the report file unchanged. |

The 11.0.0 source changes await frozen-snapshot compilation and native execution on the centrally selected Unity 6.3 and 6.6 installations. Rendering, pointer/native focus, actual PNG import, player builds and unsupported Unity-version branches remain separate acceptance evidence. Earlier pass counts and type-check receipts do not validate this source. Report the tested versions, discovered/executed cases, skips and console results after integration; fake backends and submitted UI events do not establish pixels or browser parity.
