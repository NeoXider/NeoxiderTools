# Neo.Settings: audit

Status: source audit complete; focused fixes and regressions authored. Unity compilation and test execution remain centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Settings/Neo.Settings.asmdef`.
- Scope: 10 C# files; nested assemblies excluded.

## Scope and complete source review

Read the repository `AGENTS.md`, the complete worker contract, the exact `Neo.Settings` manifest entry and this report before editing. Ownership is the ten manifest C# files plus `Assets/Neoxider/Docs/Settings`, this report and new `Audit20261023NeoSettings` regression files. No nested assembly occurs in the Settings source directory. No other worker's files were edited.

All paths below are relative to `Assets/Neoxider/Scripts/Settings/`.

| File reviewed in full | Review and disposition |
|---|---|
| `GameSettings.cs` | Static initialization/reset, attach/detach ownership, nine setting values, load/save keys, four persistence groups, all mutation/reset paths, events, fullscreen/resolution interactions, engine calls, localization and boundary clamps. Fixed inconsistent validation and fullscreen state; kept public signatures and keys. |
| `GameSettingsComponent.cs` | Singleton integration, authoring defaults, mapped quality levels, resolution filtering/deduplication/sort, FPS lookup, real-time debounce, disable/destroy/pause ownership and public UnityEvent entry points. Fixed deferred-save loss, stale-owner writes and null FPS list exposure. |
| `GameSettingsSaveKeys.cs` | Checked every key suffix against actual loads/writes. Existing `Persist*` constants are reserved; persist flags currently come from component configuration. No data format changes. |
| `GraphicsPreset.cs` | Checked all six explicit numeric values and dropdown mapping order. Preserved. |
| `GraphicsPresetLevelMapping.cs` | Checked serializable data-only structure, nonnegative authoring hint, forward clamping and first-match reverse lookup in the consumer. Preserved serialized fields. |
| `ISettingsLocalization.cs` | Checked optional provider contract, unknown-key fallback obligation and empty-key handling in the facade. No scene references or localization package dependency. Preserved. |
| `ResolutionPresetEntry.cs` | Checked positive-dimension authoring hints and runtime filtering in the component. Data-only serializable structure; preserved. |
| `SettingsGroup.cs` | Checked four explicit values against persistence and reset dispatch. Preserved. |
| `SettingsPersistMode.cs` | Checked all three explicit values against dispatch, debounce and global flush behavior. Preserved; clarified scheduling versus transaction semantics in public docs. |
| `SettingsView.cs` | Checked every optional reference and eleven callbacks, subscription lifecycle, handler suppression, dropdown population/index conversion, platform root visibility, resets and both commit modes. Fixed stale UI, retained disabled listeners and ordinary-change option rebuild risk. Removed existing `var` declarations in the two touched files containing them. |

Relevant read-only context: `Singleton<T>`, `SaveProvider` getters/setters/provider replacement, `ISaveProvider`, `NeoDiagnostics` gating/version adapter, existing Settings tests, Edit/Play test assembly references and the module documentation. The existing resolution demo paths were verified against the active `Samples~` release layout. No Unity Editor, MCP, compiler, build or test runner was started.

Reuse: checked the skill's Settings/module catalogs and extension catalog before adding private validation/integration methods. Reused the existing facade, provider, singleton, gated diagnostics, debounce coroutine and UI notification suppression. No parallel settings manager, persistence backend or generic utility was added.

## Findings and fixes

### S1 — P2: NaN sensitivity and unvalidated reset defaults escape the runtime contract (fixed)

- Trigger: call `SetMouseSensitivity(float.NaN)`, load a saved NaN, or reset an Input group with a negative/oversized/NaN configured default.
- Before: `Mathf.Clamp` comparisons do not turn NaN into a valid number; reset copied the raw default without clamping. Current state and persisted sensitivity could become NaN or exceed the setter's documented bounds, propagating invalid input scaling.
- Source: `GameSettings.SetMouseSensitivity`, `LoadState`, `ResetGroup(SettingsGroup.Input)`.
- Fix: one private clamp handles all three paths. Setter NaN preserves the current value; saved NaN uses the configured default; a NaN default uses `2`. All paths clamp to `0.01..50`, including infinities.
- Regressions: `MouseSensitivity_NaN_PreservesCurrentValueAndPersistsFiniteValue`, `LoadState_NaN_UsesConfiguredSensitivityDefault`, three `ResetInput_ValidatesSerializedDefault` cases. Assertions inspect public state and provider output.

### S2 — P2: load/reset and enum setters can publish invalid settings (fixed)

- Trigger: saved/default FPS below `-1`; invalid serialized enum values; invalid casts passed to graphics/fullscreen setters.
- Before: framerate load/reset bypassed the setter's lower bound; graphics fallback trusted an invalid configured default; fullscreen integers were cast directly. Invalid enum setters could update public state, notify listeners, persist invalid values and pass them to Unity.
- Source: `GameSettings.LoadState`, `ResetGroup`, `SetGraphicsPreset`, `SetFullScreenMode`.
- Fix: normalize framerate in load/reset, validate default graphics enum with `High` as final fallback, validate fullscreen defaults, and reject invalid enum setter values before side effects. New diagnostics use the existing warning gate.
- Regressions: two `LoadState_InvalidSavedOrDefaultFramerate_UsesUnlimited` cases, `ResetPerformance_ClampsSerializedFramerateDefault`, `LoadState_InvalidSavedAndDefaultEnums_UsesValidFallbacks`, and both `Invalid…Mutation_DoesNotChangeStateNotifyOrPersist` tests. Valid fullscreen-mode preservation is also covered.

### S3 — P2: manual resolution can undo a windowed selection (fixed)

- Trigger: start fullscreen, call `SetFullScreen(false)`, then select a manual resolution; the inverse is possible after `SetFullScreenMode(Windowed)` followed by `SetFullScreen(true)`.
- Before: the Boolean setter updated only `s_fullScreen`, leaving `s_fullScreenMode` contradictory. `ApplyCurrentResolutionFromIndex` passes the stale mode to `Screen.SetResolution`, requesting fullscreen again while the API still reports `false`. The same conflict survives load/reset of inconsistent flag/mode pairs.
- Source: `GameSettings.SetFullScreen`, `ApplyCurrentResolutionFromIndex`, display load/reset.
- Fix: maintain one consistent requested display state. False means `Windowed`; true from `Windowed` means `FullScreenWindow`. Valid explicit fullscreen modes are retained. Existing save keys and fields are unchanged; inconsistent legacy values normalize on load and are written on the next save.
- Regressions: `WindowedToggle_ThenManualResolution_PersistsConsistentDisplayState`, `FullScreenToggle_AfterExplicitWindowedMode_SelectsFullscreenMode`, `ResetDisplay_NormalizesConflictingSerializedDefaults`, and three `LoadState_NormalizesFullScreenFlagAndMode` cases. They verify the public/requested state and persisted mode. Actual OS display transitions remain unverified.
- Compatibility note: toggling off now stores the effective `Windowed` mode. Toggling back on chooses borderless fullscreen; callers requiring exclusive fullscreen should use `SetFullScreenMode(ExclusiveFullScreen)` explicitly.

### S4 — P2: deferred persistence is lost when the service cannot run its callback (fixed)

- Trigger: a deferred mutation while the attached service is inactive, or disabling/destroying its GameObject before the debounce expires. Explicitly replacing a context can also leave a former service's scheduled callback active.
- Before: inactive scheduling returned without saving; the service had no disable flush; a surviving callback called the global facade and could save a replacement context instead of its own.
- Source: `GameSettingsComponent.ScheduleDeferredSave`, `DeferredSaveCo`, lifecycle callbacks.
- Fix: inactive owners write synchronously. Disable flushes a pending owned save and cancels stale-owner callbacks. Delayed and application-pause callbacks check context ownership. Normal disable precedes destruction; `Detach` retains its existing ownership check.
- Regressions: `Deferred_WithInactiveService_PersistsInsteadOfDroppingRequest`, `Deferred_DisablingActiveService_FlushesPendingValue`, `DisablingFormerService_DoesNotSaveReplacementContext`. Timed completion and application-pause behavior still need central Unity validation.
- API documentation correction: `FlushDeferredSaveCoroutine()` cancels the callback; the existing method does not save. `GameSettings.FlushPendingSettingsSave()` cancels and writes. Behavior of this public cancellation method was preserved.

### S5 — P2: enabled settings panels become stale after ordinary changes (fixed)

- Trigger: change sensitivity from C#, use a second panel, or change quality such that the mapped graphics preset changes.
- Before: views listened only to `OnAfterSettingsLoaded`. Ordinary `Set*` calls never refreshed peer controls or the preset/quality relationship.
- Source: `SettingsView.OnEnable`, settings subscriptions and `RefreshFromSettings`.
- Fix: listen to `OnSettingsChanged` for value refresh. Keep full option rebuilding for load, enable and explicit public refresh, so slider changes do not recreate dropdown options. Continue using suppression and `Set…WithoutNotify` APIs.
- Regressions: `ActiveView_TracksApiChangesWithoutUiCallbacksOrRebuildingOptions` asserts displayed value, zero UI callbacks and retained option identity; `QualityControlChange_RefreshesNamedPresetControl` asserts the actual preset and its displayed selection.

### S6 — P2: disabled views retain control callbacks (fixed)

- Trigger: disable a view while its assigned controls remain active or are shared, then change a control. Destroying the view can also leave delegate references on surviving controls.
- Before: `OnDisable` released only the settings-load subscription. Control events could still mutate/persist settings through the disabled view.
- Source: all eleven `BindAll` control listeners and `SettingsView.OnDisable`.
- Fix: remove this view's eleven listeners on disable/destroy, unsubscribe from both settings events, preserve unrelated listeners, and retain idempotent rebind behavior.
- Regression: `DisabledView_ReleasesControlsAndRebindsOnceWithoutRemovingExternalListeners` verifies unchanged runtime/persisted values while disabled, preservation of an external callback and exactly one settings notification after re-enable/repeated bind.

### S7 — P2: null FPS preset array interrupts all UI refresh (fixed)

- Trigger: a null serialized FPS array with an FPS dropdown assigned.
- Before: `PopulateFramerateDropdown` dereferenced `FramerateCapPresets.Count`; the other FPS lookup methods already handled null. Refresh could stop before synchronizing unrelated controls.
- Source: `GameSettingsComponent.FramerateCapPresets`, `SettingsView.PopulateFramerateDropdown`.
- Fix: expose an empty read-only list when the array is null.
- Regression: `NullFrameratePresetArray_ShowsEmptyOptionsAndStillRefreshesOtherControls` asserts empty FPS choices, the sensitivity control's actual value and six available graphics choices.

### Observations retained as documented behavior

- `ApplyButton = 1` is reserved and currently saves immediately; there is no Apply/Cancel API. This is not presented as an implemented transaction.
- `SkipUntilFlush` suppresses scheduling only. Every save writes all enabled groups, so another mutation or lifecycle flush can capture that value.
- Persist flags gate writes only; old saved values are still loaded. `SaveState` writes provider values, not an explicit durable `SaveProvider.Save()`.
- Auto resolution stops indexed applications and does not restore a previous desktop resolution. Resolution identity is a sorted-list index, so hardware/list changes can select a different size after restart.
- Reverse quality lookup uses the first matching mapping. Empty/missing lists and unmapped quality values have existing fallbacks; no scene search or extra singleton was added.

## Documentation

Updated five existing owned English documents:

- `Assets/Neoxider/Docs/Settings/README.md`: canonical entry, real release/development/imported sample paths, C# example, event synchronization and save boundaries.
- `GameSettings.md`: public APIs/events, mutation/load/reset validation, fullscreen normalization, provider load/save distinction, group persistence and runtime/scene usage.
- `GameSettingsComponent.md`: authoring configuration, typed UnityEvent entry points, null/empty FPS behavior, deferred lifecycle and ownership.
- `SettingsView.md`: optional controls, lifecycle/rebind behavior, value refresh without option rebuilding, localization refresh and the reserved Apply mode.
- `SettingsPersistMode.md`: immediate/deferred/skip semantics and lack of transaction isolation.

No new documentation entry point, DocsEn, package/root README, sample, scene, prefab, assembly definition or shared configuration was changed.

## Substantial improvement proposal

### Proposal P1: instance-based settings state with an opt-in edit session (not implemented)

**Problem.** The static facade combines validation, global current state, engine side effects, SaveProvider access and a scene coroutine owner. Ordinary contract tests require Unity objects and global state. A menu cannot implement a real Apply/Cancel flow with `SkipUntilFlush`: its edits already affect the live state, any other save can persist them, and disabling the view flushes them. Current `ApplyButton` naming invites this mistaken assumption.

**Proposed architecture/public API.** Introduce a small ordinary C# `SettingsState` value and `SettingsController` instance holding validated `Current` state and a `Changed` event. Separate the existing side-effect boundaries with `ISettingsApplicator.Apply(SettingsState, SettingsGroup)` and `ISettingsStore.Load/Save`; production adapters use the current Unity APIs and SaveProvider. `GameSettingsComponent` supplies defaults/adapters and optional debounce scheduling; it remains the scene convenience layer. No ScriptableObject stores scene references.

Add `SettingsEditSession BeginEdit()` with a copied draft, typed setters, `Commit()` and `Cancel()`. Draft edits neither change the engine nor write storage. Commit validates once, applies the changed groups, raises one change notification and schedules/persists according to an explicit commit mode. A session records the state version: conflicting external changes return a conflict result rather than silently overwriting them. Keep preview as a separate future opt-in; it is not required for the first session API.

**Compatibility/migration.** Keep all static `GameSettings` signatures as forwarding adapters over the default controller, retain current key suffixes and serialized component fields, and preserve the live `DebouncedLive = 0` behavior. Keep legacy `ApplyButton = 1` behavior for existing serialized scenes; introduce a new opt-in `StagedApply` enum value and explicit Apply/Cancel methods instead of silently changing those scenes. Existing save data loads through the same normalization path. Upgrade examples incrementally; the runtime core must not reference the sample UI.

**Acceptance scenarios.**

1. Ordinary C# tests exercise setters, invalid values, reset and store round trips using in-memory adapters without a GameObject or Unity engine side effects.
2. Editing a session leaves `Current`, engine state, provider values and other panels unchanged until Commit; Cancel and closing a staged panel discard the draft without a save.
3. Commit produces one coherent state/event and writes only the selected enabled groups. Closing a separate live panel cannot save another panel's draft.
4. Concurrent external changes produce a visible conflict result; two sessions cannot overwrite each other silently.
5. Existing static callers, immediate/deferred saves and saved keys retain their current behavior. Deferred disable/pause ownership regressions remain covered.
6. A Unity adapter contract test checks quality/VSync ordering and fullscreen/manual-resolution consistency, while desktop/device QA verifies actual display support.

**Effort/risk.** Estimated 3–5 engineering days plus platform QA, assuming the current nine settings remain the initial schema. Medium risk: event ordering, persistence timing and compatibility of existing serialized commit modes. Land the pure state/adapters first, then the opt-in session/UI integration; do not combine this with key migrations or a new localization system.

## Verification and remaining risks

### Actual checks performed

- A read-only Python inventory comparison against the exact manifest entry confirmed all **10/10** owned C# files are present and reviewed; no extra/nested C# files were missed.
- Compared owned source against `ca198556`: existing public declaration lines and component field types/names are preserved. All seven unchanged enum/data/key/interface files compare equal after UTF-8 decoding.
- Static checks found no `var` declarations, `GetInstanceID`/obsolete property calls or scattered version `#if` branches in the owned inventory. No new version-sensitive API was introduced.
- Final static validation strictly decoded **18 files** (ten owned C# files, five edited docs, the new test and metadata, this report) as UTF-8, with no replacement/NUL characters or trailing whitespace. All local links in the five edited docs resolve. The new package test has a valid `.meta` file; its GUID `a9ce2b14f6754d77be30f5b3d24fa108` occurs exactly once under `Assets`.
- Listener parity inspection confirmed all **eleven** UI callbacks and both settings subscriptions have matching lifecycle removal. This is static consistency evidence, not executed UI behavior.
- Final standalone `git diff --check -- Assets/Neoxider/Scripts/Settings Assets/Neoxider/Docs/Settings Assets/Neoxider/Tests/Edit/Settings/Audit20261023NeoSettingsTests.cs Assets/Neoxider/Tests/Edit/Settings/Audit20261023NeoSettingsTests.cs.meta Docs/Audits/2026-10-23/Neo.Settings.md` completed with **exit code 0**. The separate whitespace scan also includes the untracked new test, metadata and report, which `git diff` does not inspect.
- New regression inventory: `Assets/Neoxider/Tests/Edit/Settings/Audit20261023NeoSettingsTests.cs`, **19 test methods / 24 NUnit cases**. Tests inspect public settings/provider/UI output; reflection is limited to existing private serialized configuration and the existing static test reset hook. The fixture uses an in-memory provider, restores prior provider/engine flags and destroys only its own objects. No runner executed these tests.

### Coordinator actions and remaining risks

- **Compile and run centrally:** new fixture `Neo.Editor.Tests.Audit20261023NeoSettingsTests` plus existing `Neo.Tools.Tests.GameSettingsTests` and `Neo.Editor.Tests.GameSettingsStaticResetTests`. Verify the active-service coroutine/disable case in the Unity test environment; timed debounce and actual pause events still need Play Mode validation.
- **Static assembly reference gap:** both `Neo.Settings.asmdef` and `Neo.Editor.Tests.asmdef` use `UnityEngine.UI` types but do not explicitly list `UnityEngine.UI`; `Neo.Tests.Play.asmdef` does list it. Existing UI tests already use these types. The coordinator should verify/repair the direct reference graph during compilation; no shared asmdef was edited and no compile failure is claimed from inspection alone.
- **Platform QA:** public/requested fullscreen state and stored values are covered by authored tests; actual OS resolution/fullscreen transitions, console/WebGL restrictions and quality/VSync interaction are not executed here. Compile on supported Unity 6.0–6.4 and 6.5+ centrally; neither branch is claimed validated by this worker.
- Replacing an attached service is not a profile migration. Flush before replacement if outgoing state must be retained. Durable backend flushing remains provider-owned.
- No commits, shared configuration changes, installs, Unity/MCP operations, builds, runners, services or child agents were used.

Кратко: проверены все десять исходников; исправлены подтверждённые нарушения валидации, согласованности режима экрана, отложенного сохранения и жизненного цикла UI. Добавлены поведенческие регрессии и обновлены существующие документы. Исполнение Unity-тестов и проверка платформ остаются за координатором.
