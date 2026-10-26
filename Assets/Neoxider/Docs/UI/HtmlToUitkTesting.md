# HTML to UI Toolkit testing contracts

The `Neo.HtmlToUitk.Tests.Editor` assembly tests the public contracts described in [HTML/CSS to UI Toolkit](HtmlToUitk.md). Tests live in `Assets/Neoxider/Tests/Edit/HtmlToUitk`; the assembly is Editor-only and requires `UNITY_INCLUDE_TESTS`. It references runtime, editor and Unity Test Framework assemblies. Most unit fixtures use injected bridges; the real external-CLI integration explicitly requires the converter. [11.0.0 verification](HtmlToUitkVerification11.md) is the current execution record.

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

The regression `RedirectingFromNavigatedDoesNotPublishWillHideFromTheSupersededPlan` checks redirecting to home from an `OnEvent` navigation callback. The superseded plan must not emit `WillHide:home` after that redirect. Current native execution is recorded below; future changes still need this observable ordering.

## Files and global state

Unit runner tests substitute `IJobBridge` through `HtmlToUitkRunner.BridgeOverride`; those cases do not launch the converter. The separate real integration performs three actual CLI runs. Restore every prior override/delegate in `finally`, including non-null values, and run shared bridge/scheduler/window fixtures in the isolated operator sequence. No `NonParallelizable` attribute establishes isolation. Assert original JobDocument/file preservation, temporary-job deletion and release of IsRunning on success and failure.

Every created temporary directory needs a cleanup path on assertion failure. Keep fixture files outside imported `Assets` unless the contract specifically requires a project-relative location. Generated-texture checks use exact paths, hashes and policy; a directory named `Generated` or matching PNG bytes alone cannot establish ownership.

Report selection tests set explicit UTC file timestamps. Malformed stdout must contain a complete but invalid JSON object to reach the parser branch: an unfinished object is treated as missing JSON. Assert the diagnostic reason as well as `FAIL`. Cover an invalid fresh report file both with valid stdout fallback and without fallback, preserving the parse reason and stderr.

Responsive ownership tests must include an authored class whose name is also controlled by a media rule. Check that inactive queries and disposal preserve it while removing classes the controller added, then verify a replacement controller actually updates the same root.

## Execution and evidence

A single Unity operator runs the complete assembly in EditMode after the implementation source barrier closes. One existing `UnityTest` explicitly enters Play Mode for the disabled-component lifecycle and exits through its owned `UnityTearDown`; other panel tests open and close temporary Editor windows. Do not run these alongside another operator using the Editor. No fixture should save a scene, prefab or project settings.

The original `Audit20261023NeoHtmlToUitkTestsEditor` four-file subset defined fourteen cases. This table retains those original oracles, not current fixture totals or pass counts; current execution and added regressions are recorded below:

| Source under `Tests/Edit/HtmlToUitk` | Defined cases | Required observable result |
|---|---:|---|
| `Editor/Audit20261023NeoHtmlToUitkTestsEditorRunnerRegressionTests.cs` | 1 | Injected bridge failure releases the temporary job and running flag while preserving the original document and persistent job file. |
| `Editor/Audit20261023NeoHtmlToUitkTestsEditorRuntimeRegressionTests.cs` | 6 | Both completion orders publish and focus once after all pending steps; disposal and redirect suppress stale work; authored responsive classes survive replacement; trigger counts stay `1,1,1,2,2` through dispatch, disable, unwire, rewire and disposal. |
| `PureLogic/Audit20261023NeoHtmlToUitkTestsEditorNavigationRegressionTests.cs` | 4 | Showing a middle stack page preserves earlier history with explicit pop-to-existing policy under both visibility and clear-stack settings. |
| `PureLogic/Audit20261023NeoHtmlToUitkTestsEditorReportReaderRegressionTests.cs` | 3 | Invalid fresh reports fall back to valid stdout or expose the parse reason and stderr; reading leaves the report file unchanged. |

Round3 ran the complete assembly **370/370, no skips**, in each Editor6000.3.14f1/6000.6.0f1. It includes five new runtime regressions for newest projection, same-root/throw cleanup and localization, plus the Root real external-CLI case: three runs → UXML/USS/nav/media/anim and seven PNGs → native ownership import/theme → Play UIDocument pixels, navigation/modal/tabs/actions, disable-enable and tree replacement.

Fresh Round2 CLI solution: **2573 passed / 30 NotExecuted / 0 failed**, including199 real capability cases using SDK 6.6 managed/UnityEngine inputs; separate portable Root pure logic: **291/291, no skips**. The30 nonexecuted cases remain7 validate-only,22 open goldens and1 S08 expected-diagnostic debt. Source net9 used process-local Major on.NET 10.0.9; net8 used8.0.21. The portable extracted CLI used runtimeconfig LatestMajor without ENV. Four old SDK-specific diagnostic csprojs remain machine-specific.

Manual OS interaction is a separate Unity 6.3 **1280×720** scope: Settings once, Video tab, Back, Quit/Cancel, public UIDocument Profile replacement, then physical Save once; settingsClicks=1/saveClicks=1 and no screen error. See [native images and exact receipts](HtmlToUitkVerification11.md). These results do not establish all resolutions, player/mobile, performance or full JS/browser parity. Final release archive export/import remains a separate gate in [Verification 11](../../Verification11.md).
