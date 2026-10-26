# Neo.Settings — implementation receipt

Status: **SOURCE-FINAL** for the original correction and the Snapshot3 follow-up. The latest regression-source correction remains **UNEXECUTED**, pending a new global freeze. Archived Snapshot3 results are recorded separately below.

- Approved action: `SETTINGS-QUALITY-VSYNC` (P2), source portion completed; native reproduction/acceptance still pending.
- Follow-up action: `SETTINGS-NATIVE-VIEW-DEFERRED-FLUSH`, source correction completed for failure groups `E29`–`E33` / index actions `L`, `U`; rerun pending.
- Target: `demo`, package `11.0.0`; original actor `neo-audit-1023-b05-08`, model `gpt-6.1-sol`, reasoning `max`.
- Additive public APIs: **none**. Existing `SetQualityLevel(int, SettingsPersistMode)` and `SetGraphicsPreset(GraphicsPreset, SettingsPersistMode)` signatures remain unchanged. No dependent owner needs a new API.

## Completed implementation

| Exact file | Work completed in this phase |
|---|---|
| `Assets/Neoxider/Scripts/Settings/GameSettings.cs` | Both quality-changing setter paths now restore `QualitySettings.vSyncCount = s_vSync ? 1 : 0` immediately after `QualitySettings.SetQualityLevel`, before `OnSettingsChanged` and persistence. This follows the existing load/reset application policy in `ApplyAllEngine`. |
| `Assets/Neoxider/Tests/Edit/Settings/Audit20261023NeoSettingsTests.cs` | Added `QualityChange_WithOppositeProfileVSync_RestoresRequestBeforeNotificationAndSave`, with four cases covering direct quality / named preset and VSync off / on. Removed the accidentally reintroduced `[NonParallelizable]` as explicitly required by the updated contract; assertions and cleanup are retained. |
| `Assets/Neoxider/Docs/Settings/GameSettings.md` | Documented quality-profile application order, the independent requested VSync taking precedence, and unchanged `Custom` semantics. |
| `Assets/Neoxider/Docs/Settings/README.md` | Updated the canonical module entry to explain the same behavior for quality dropdowns and named presets, including platform presentation limits. |
| `Docs/Audits/2026-10-23/Neo.Settings.implementation.md` | Published the API/dependency notice early, then completed this source-final receipt. |

Source evidence: both setters previously called `SetQualityLevel` without the restoration already present in `ApplyAllEngine`. Read-only inspection of `ProjectSettings/QualitySettings.asset` found six profiles: Very Low/High/Very High/Ultra use `vSyncCount: 1`, while Low/Medium use `0` (current index `2`). Thus differing authored profiles exist in this checkout. **This is configuration/source evidence, not an executed engine reproduction or proof of actual frame presentation.** No project configuration was edited.

The historical S1–S7 source fixes remain intact. The optional settings controller/edit-session framework remains a proposal; it was not implemented.

## Compatibility and reuse

No public signatures, serialized fields, enum numbers, save keys, defaults, assembly definitions or assets were migrated. The intentional behavior correction is that the requested performance VSync wins after a quality switch, consistently with load/reset. `SetGraphicsPreset(Custom)` still keeps the current quality level. Event order, persistence modes, all-enabled-group writes, provider durability separation, reserved `ApplyButton = 1` and resolution identity are unchanged. Existing facade/state, quality application and provider/test infrastructure are reused; no service, scheduler or framework was introduced.

## Original authored acceptance — latest source UNEXECUTED

The new four-case regression prepares a different native quality level with the opposite VSync value, returns to the source level and requests VSync through `GameSettings`. It then changes quality through each public setter and asserts:

- Native and public quality indices match the selected target; reverse preset mapping remains correct.
- Requested VSync and engine `vSyncCount` agree, including inside the single settings notification.
- The notification occurs before the provider write; afterward the provider contains the requested VSync and selected quality.
- `finally` removes the observer and restores the target profile's previous VSync, source quality and original source VSync. Existing teardown restores the provider, engine flags and warning gate, resets facade state and destroys only fixture-created objects.

Fixture inventory remains **20 test methods / 28 NUnit cases**, including the earlier regressions. Fewer than two available quality levels produce the existing explicit ignore for the VSync cases; an ignored case is not acceptance evidence. The fixture requires the isolated/exclusive runner contract. The Snapshot3 correction below rejects a borrowed settings service before mutation and restores captured facade state/events instead of discarding prior listeners. No serial-execution guarantee is claimed from an NUnit attribute.

## Actual checks and remaining dependencies

- Read the updated full implementation contract and exact request after the resource interruption; retained the already-applied patches. Reviewed current source, the complete prefixed fixture and scoped source/docs diffs.
- Scoped `git diff --check` completed with exit code **0**. The prefixed fixture is untracked, so its contents were inspected directly; this diff result does not validate untracked content or compilation.
- Source search found no `NonParallelizable` attribute or `var` declaration in the prefixed fixture after correction. No compiler or test discovery was used to make that statement.
- Historical audit SHA-256 remains `82c25b739e3e3e2604db8fdddd9a7e15b09dcafe3e126364ed6aed426c5b04c3`; historical plan remains `d622b76ef6c59797ca8a9b428f963edc561a0c923f0361796bc84230d24e104e`. Neither artifact was rewritten.

**Pending additive API dependency: none.** Existing dependencies on `Neo.Save`, `Neo.Tools.Managers`, `Neo.Extensions` and UI are unchanged; no other owner's source, legacy fixture, receipt or shared tracker/configuration was edited.

**Remaining acceptance dependency:** after every source writer finishes, the coordinator's isolated Unity 6.3/6.6 hosts must compile/discover and execute `Neo.Editor.Tests.Audit20261023NeoSettingsTests`, including the new native quality/VSync cases, and the existing Settings fixtures. Confirm actual quality-profile VSync replacement and the restored state on those engines; do not close the native action from source inspection alone. Previously recorded bootstrap/domain-reload, realtime debounce/pause/lifecycle and OS/platform display evidence debt remains central acceptance work. Shared assembly-reference questions remain compilation debt, not a claimed compile error. No Unity/compiler/test/product-checker, service, install, commit/push or child agent was launched in this phase.

## Snapshot3 follow-up — SETTINGS-NATIVE-VIEW-DEFERRED-FLUSH

### Exact archived evidence read

- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`, SHA-256 `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`: the owned fixture has **28 cases, 23 passed / 5 failed**. All five failure messages/stacks and all assigned `E29`–`E33` records in `full-edit-failure-action-index.json` were read, including action definitions `L` and `U`. The four VSync cases passed on that older snapshot; this does not establish acceptance of the latest integrated source or actual display presentation.
- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml`, SHA-256 `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`: **zero cases from the owned fixture**. It provides no Neo.Settings lifecycle evidence.

The failed setup ran ordinary runtime components from synchronous Edit tests. Activating an ordinary `SettingsView` there did not establish native `OnEnable` subscription/binding/population; likewise the service test did not establish native disable/coroutine behavior. Empty options, minimum slider values and absent saves therefore do not establish an additional runtime defect. No runtime source correction or default/range change was made to fit these failures.

### Source correction completed

Only `Assets/Neoxider/Tests/Edit/Settings/Audit20261023NeoSettingsTests.cs` and this receipt changed in this follow-up. The reservation map was checked before each edit; neither file is reserved. No helper was moved/extracted, no reserved consumer/assembly reference was changed, and no GUID changed. Prior runtime and public-doc changes remain intact; this follow-up changes no public contract, so canonical module documentation needs no further change.

The fixture now has top-level `UnitySetUp` / `UnityTearDown`. Exactly six existing, unchanged test names select `EnterPlayMode` / `ExitPlayMode`: the five failures below plus `DisablingFormerService_DoesNotSaveReplacementContext`. The remaining 22 cases retain their explicit public-API Edit contracts. Setup creates its memory provider/state scope **after** native entry. Existing `Attach` configuration remains deliberate: these cases verify real view callbacks and service disable behavior, not the full singleton bootstrap. No private lifecycle method, manual Update, ExecuteAlways, new ignore or expected-error whitelist was added.

| Group / unchanged test name | Positive arrangement and retained substantive oracles |
|---|---|
| E29 — `ActiveView_TracksApiChangesWithoutUiCallbacksOrRebuildingOptions` | Require Play Mode, active owned service/view/control, exact existing slider bounds and six populated preset options before indexing. Retain sensitivity `4.5`, zero UI callbacks and identical first option object. |
| E30 — `Deferred_DisablingActiveService_FlushesPendingValue` | Require active attached service and public sensitivity `6.25` with no saved key before native disable. Retain exact saved `6.25`; additionally require inactive service and a present key afterward. |
| E31 — `DisabledView_ReleasesControlsAndRebindsOnceWithoutRemovingExternalListeners` | Establish a populated active panel, then verify the separate control remains active while the view is disabled. Retain runtime/persisted `2`, external count `1`; re-enable/rebind and retain sensitivity `5`, exactly one settings notification and external count `2`. |
| E32 — `NullFrameratePresetArray_ShowsEmptyOptionsAndStillRefreshesOtherControls` | Require native activation and populated independent graphics controls. Retain empty FPS options, sensitivity exactly `6`, and six graphics options; no fallback value or relaxed comparison. |
| E33 — `QualityControlChange_RefreshesNamedPresetControl` | Require nonempty native quality names and matching actual dropdown options/first label before invoking its public UnityEvent. Retain `Minimal` state and selection; additionally assert quality index `0`. |

The former-service case now establishes an actual active pending owner before replacement rather than relying on an Edit-mode disable. Its existing replacement-prefix negative and sensitivity `7` assertions remain; an additional negative checks the outgoing prefix was not written either.

Setup rejects a preexisting attached service instead of resetting or destroying it. It captures/restores the facade's mutable private static state, including listeners/localization, and retains prior provider/engine/diagnostic restoration. Teardown destroys only `_objects` created by this fixture; no scene is cleared/unloaded, no provider data is deleted, and no borrowed session is stopped. Top-level Exit is requested only for the Play Mode entry this fixture acquired.

### Actual source review and unresolved evidence

Read-only comparison with the exact Snapshot3 fixture confirmed unchanged test names/case attributes and retained original assertions; only native arrangement, positive preconditions and scoped restoration were added. Reviewed the installed test-framework source example supporting top-level UnitySetUp/UnityTearDown with ordinary `[Test]` methods. Scoped `git diff --check` returned **0**; this is source whitespace evidence only and does not cover untracked file contents. No compiler/Unity/test/checker was run by this actor during the follow-up.

**Deferred reserved work: none. Pending source/API dependency: none.** Stop source writes here. Root must freeze all writers, then rerun the unchanged 28-case fixture on the isolated supported hosts. In particular, native entry/setup/teardown, actual OnEnable/OnDisable dispatch, option identity, listener counts, exact provider payloads and stale-owner cancellation remain unverified on this corrected source. Timed debounce completion, pause, ordinary singleton bootstrap/domain reload and platform display evidence remain unresolved; the archived failures are not marked passed from this patch.
