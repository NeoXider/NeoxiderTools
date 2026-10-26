# Neo.HtmlToUitk.Tests.Editor: implementation plan

Phase 2, 2026-10-23. Original auditor/session; `gpt-6.1-sol`, reasoning `max`. Planning only: this file is the sole phase-2 write. Read the complete [plan contract](PLAN-CONTRACT.md), [completed audit](Neo.HtmlToUitk.Tests.Editor.md), exact manifest entry and current relevant sources.

Path abbreviations: `T = Assets/Neoxider/Tests/Edit/HtmlToUitk`; `R = Assets/Neoxider/Scripts/UI/HtmlToUitk`; `D = Assets/Neoxider/Docs/UI`. Each relative path below expands against that exact root.

## Current outcome: completed work

All 12 original C# files were audited. Already fixed: the report-reader test's unconditional temporary-directory leak; loss of the previous `BridgeOverride` and runner-fixture isolation; malformed-stdout coverage reaching the wrong branch; and misleading pop-to-existing coverage. Source-job preservation assertions and 14 regression cases in four owned prefixed files are present. `D/HtmlToUitkTesting.md` and five unique package metadata files are complete. Do not reimplement these fixes.

Phase-2 inspection confirms the 16 owned C# files retain audit receipt `45944f1c727524f7660ec345b84412c163150d6cb3a68cf869197f16aa3ce8e0`. The runtime generation guards for audit F5 remain present in `R/Runtime/Navigation/UitkRouter.cs`; their correction belongs to `Neo.HtmlToUitk.Runtime`. Current runner source also snapshots async inputs and resolves absolute paths; existing failure cleanup assertions remain relevant.

The audit passed scoped diff, lexical C#, UTF-8, JSON, metadata and link checks. Compilation, NUnit/Unity execution and native behavior were not executed. No additional confirmed owned-source fix is outstanding.

## Prioritized debt and cross-assembly dependencies

| Priority/status | Evidence, owner and smallest next action | Observable acceptance |
|---|---|---|
| P1: verification debt | All new cases remain unexecuted. Coordinator compiles and runs `Neo.HtmlToUitk.Tests.Editor` after source-changing audits finish; this owner corrects only demonstrated fixture failures. | All 14 new cases are discovered and pass, alongside the integrated assembly; no silent skips or unexpected console errors. |
| P1: F5 closure, source correction already present | `UitkRouter.Execute` checks generation before will-events and after callbacks. Runtime owner integrates its change before the existing redirect regression runs. | Redirect from `Navigated:next` to home leaves stack `[home]` and emits only `WillHide:next`, never stale `WillHide:home`. |
| P1: editor integration | `R/Editor/Bridge/HtmlToUitkRunner.cs` and `R/Editor/Logic/ReportReader.cs` belong to `Neo.HtmlToUitk.Editor`. Integrate that owner's changes before runner/report cases; forward production failures with their reproduction. | Success and injected bridge failure preserve the document/permanent job, delete the temporary job and release `IsRunning`; invalid fresh reports either use valid stdout or retain useful failure diagnostics. |
| P2: confirmed documentation integration | `D/HtmlToUitk.md` still lacks the testing-guide link. Runtime owner/coordinator adds it and replaces historical verification claims only with current receipts. | One canonical entry exposes the narrow testing guide; historical pass counts are clearly separate. |

Native focus, pointer dispatch, actual PNG import, rendering and GPU behavior remain unverified module risks, not newly confirmed defects. Their runtime/editor owners retain acceptance responsibility. These test-only changes do not establish player-build or visual parity.

## P2 proposal: deterministic backend conformance scenarios

**Problem and reuse.** Numeric handle indices in `RuntimeOwnershipTests.DeferredBackend` make ordering tests brittle. Separate synchronous and deferred fixtures can miss differences in completion or reentry. Reuse that backend/handle logic, `CompletedTransition`, public `IPageBackend`, `IPageTransitionHandle`, `IPlanAwareBackend`, `IRouterHook` and `UitkRouter`. The runtime owner's `T/Editor/Audit20261023NeoHtmlToUitkRuntimeRouterTests.cs` already supplies a synchronous visibility/focus recorder: consult its scenarios without editing or duplicating its cases.

**Focused design.** Introduce an internal disposable `RouterContractScenario` test helper. It owns one router, records dispatched operations and returns copied event/focus snapshots. Select immediate per-step, deferred per-step or deferred whole-plan backends. Proposed helper methods are `Complete(NavStepKind kind, string id, int occurrence)`, `CompletePlan(int occurrence)` and `RedirectOnce(RouterEventKind kind, string eventId, string target, NavMode mode)`. Repeated operation occurrences disambiguate transitions without reading private router generations. Expected state/event traces are literal scenario expectations, not another implementation of `RouterCore`.

No runtime public C# API or Inspector behavior changes are required. Existing C# navigation APIs remain the exercised contract. Do not add a configurable reentry policy, runtime service or NoCode layer merely to support tests.

**Exact proposed files, owned by this assembly:**

- Add `T/Editor/Audit20261023NeoHtmlToUitkTestsEditorRouterScenario.cs` and its unique `.meta`.
- Add `T/Editor/Audit20261023NeoHtmlToUitkTestsEditorRouterConformanceTests.cs` and its unique `.meta`.
- Migrate deferred tests in `T/Editor/RuntimeOwnershipTests.cs` and `T/Editor/Audit20261023NeoHtmlToUitkTestsEditorRuntimeRegressionTests.cs`, retaining existing fixture/method identities.
- Extend `D/HtmlToUitkTesting.md` with helper usage and explicit separation of public C# contracts from real-panel/scene checks. The runtime owner maintains its canonical guide link.

Minimum useful new scenarios cover immediate/deferred/plan-aware completion, both hide/show completion orders, and redirect/dispose from will-events and tab callbacks. Reuse existing F5, disposal, responsive ownership and trigger cases rather than multiplying equivalent assertions. Keep one real-panel subscription check and the existing disabled-component Play Mode lifecycle scenario; neither becomes a fake-backend assertion.

Estimate: 1-2 developer days plus centralized validation. Main risk is a helper that reproduces production logic or hides callback errors. Keep it internal, small and explicitly disposable; broader harness export remains a separate product/design proposal.

## Implementation order and compatibility

1. Preserve completed fixes, integrate runtime/editor owner changes, and establish a passing central baseline. Repair reproduced failures before adding the proposed helper.
2. Extract existing deferred behavior, add named completion selection, then add the immediate and plan-aware adapters. Compare event/focus outcomes before migrating existing cases.
3. Add only missing conformance scenarios. No production API, scene component or editor adapter work is currently justified.
4. Update testing documentation and the canonical link through its owner. Retain English public documentation and actual UTF-8 text.
5. Coordinator validates Editor-only packaging, test discovery, GUID uniqueness and receipts. Preserve serialized fields, public signatures, asmdefs and assets. No sample rename or fixture dependency on samples is needed; release `Samples~`, developer `Samples` and versioned imported sample layouts remain unchanged.

## Acceptance and stop conditions

Coordinator compiles the real assemblies under `UNITY_INCLUDE_TESTS` on a Unity 6.0-6.4 version and a 6.5+ version, recording exact versions and branch symbols. Existing `T/Verification~/6000.3.14f1.Tests.csproj`, `6000.6.0f1.Tests.csproj` and `PureLogic.Tests.csproj` are reusable supplementary checks: their globs include new files, but aggregate source compilation and standalone NUnit do not prove asmdef boundaries or native behavior. Path/reference changes, if necessary, belong to the coordinator.

Run the full integrated EditMode assembly; the existing lifecycle test explicitly enters/exits Play Mode. Observe completion/focus exactly once after the final pending operation; no stale events after supersession/disposal; trigger counts `1,1,1,2,2` across dispatch/disable/unwire/rewire/dispose; authored responsive classes surviving disposal; and middle-stack history preserved under both visibility policies. Confirm windows, temporary files, overrides and owned Play Mode state are restored.

A compile failure, missing case, unexpected skip, leaked resource or contradictory event trace prevents verified closure. Reduce it to a concrete reproduction and assign the appropriate source owner; never weaken assertions or edit another owner's files to force a pass. Keep version adaptation in existing runtime/editor compatibility locations. Unavailable native checks stay explicitly unverified. The optional helper may be deferred without reopening already completed fixes. No Unity/build/test process or other file modification was performed during planning.
