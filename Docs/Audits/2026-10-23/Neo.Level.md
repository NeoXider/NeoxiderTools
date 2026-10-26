# Neo.Level: audit

Status: source audit and scoped fixes complete; static checks passed. Unity compilation and regression execution remain pending with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Level/Neo.Level.asmdef`.
- Scope: 5 C# files; nested assemblies excluded.

## Scope

All **5/5 manifest C# files were read completely**, including public APIs, serialized fields, private execution paths, and conditional/version dependencies. The current assembly directory contains exactly these five C# files; no nested assembly was included.

| File | Review coverage |
| --- | --- |
| `Assets/Neoxider/Scripts/Level/LevelButton.cs` | Runtime click registration/removal, Unity destroyed-object checks, optional icons/text/manager, visual IDs/events, runtime-created components. |
| `Assets/Neoxider/Scripts/Level/LevelManager.cs` | Singleton initialization, map loading/discovery/selection, Inspector collection, UI ownership, all selection/completion methods, looping and integer limits, event payloads, invalid/null configuration. |
| `Assets/Neoxider/Scripts/Level/Map.cs` | Save key compatibility, load/save failures, signed progress, overflow, finite/infinite completion, serialized data contract. |
| `Assets/Neoxider/Scripts/Level/SceneFlowController.cs` | All load overloads/modes, loader injection/reset, manual activation, coroutine ownership/reentry, GameObject/component lifetime, UI progress, event order, pause/quit/restart. |
| `Assets/Neoxider/Scripts/Level/TextLevel.cs` | Current/max/legacy `_best` selection, explicit source/singleton fallback, inherited text API, deferred initialization, disable/re-enable and event cleanup. |

Read-only context: `Neo.Level.asmdef`, both test asmdefs, existing `LevelManagerTests` / `SceneFlowControllerTests`, relevant `Singleton<T>`, `SetText`, `SaveProvider` / `ISaveProvider`, `CoroutineExtensions.CoroutineHandle`, `PrimitiveExtensions.Wrap`, and `NeoDiagnostics`. Existing lifecycle test patterns were checked. Applied `neoxider-tools` and `unity-game-agent` in file-only mode; package references were consulted before choosing helpers. Reused `int.Wrap`, `WaitWhile` / `CoroutineHandle.Stop`, existing `SetText`, `SaveProvider`, and Unity hierarchy queries.

No commits, installations, shared configuration/asmdef edits, Unity/MCP calls, runners, scene/prefab/sample changes, or child agents. Existing tests and other workers' files were left untouched.

## Findings and fixes

Evidence below is source analysis against `ca198556`, with new regressions prepared for central execution. No Unity reproduction or NUnit result is claimed.

### F1 — High: optional UI prevents runtime initialization

**Trigger/consequence:** a dynamically created `LevelManager` has `_lvlBtns == null`; `Init()` dereferences `.Length` before completing initialization. Null button entries fail during manager assignment. A fresh `LevelButton.SetVisual()` dereferences unassigned visual arrays or null entries before delivering its visual events.

**Sources/fix:** `LevelManager.Init/UpdateVisual` now support missing button arrays and entries. `LevelButton.SetVisual` supports missing arrays and individual icons. New defaults are empty arrays; existing serialized field names/types remain intact.

**Regressions:** `SingletonInitialization_AllowsNoButtonsAndSkipsEmptySlots`, `MapButtons_EmitPassedCurrentLockedStatesInLevelOrder`, `LevelButton_OptionalVisualEntriesDoNotPreventStateAndEventUpdates` assert persisted progress, complete visual output, and event payloads.

### F2 — Medium: empty map slots crash discovery or invalidate selection

**Trigger/consequence:** a null map entry is dereferenced by Inspector validation, initialization, or `GetLastIdMap`. `SetMapId` formerly accepted null slots; when all maps were complete, `SetLastMap` selected the array's last index even if that slot was empty.

**Fix:** skip empty map entries while retaining original map indices/save keys; reject empty selections without mutating `_mapId` or emitting selection events; select the last configured map when all configured maps are complete. Completion-driven map advancement also skips empty/already completed intermediate maps; the old adjacency-only check could leave a completed map selected even with valid later content. Switching to a different map from a progress callback is retained.

**Regressions:** `SingletonInitialization_AllowsNoButtonsAndSkipsEmptySlots`, `SetLastMap_SelectsLastConfiguredCompletedMapAndRejectsEmptySlot`, and two `CompletingMap_AutomaticallySkipsCompletedOrEmptyIntermediateMap` cases assert selected IDs, map identity, event sequence, and stored progress.

### F3 — High: looping progress selects the wrong frontier and stops saving

**Trigger/consequence:** `SetLastLevel()` used `NextLevel()` when `countLevels >= map.level`, so a fresh loop selected level 1 instead of 0. With three unique levels, the old equality `map.level == _currentLevel` stopped advancing progress once absolute progress reached 3 and selection wrapped to 0.

**Fix:** always select the saved frontier through `SetLevel(map.level)`; compare selection with the wrapped frontier when recording a looping completion. A loop with no positive level count cannot record completions. Absolute saved progress and its existing key format are preserved.

**Regressions:** `SetLastLevel_SelectsStoredLoopFrontier` covers progress 0, 2, and 5; `SaveLevel_AdvancesAndPersistsAcrossMultipleLoops` checks eight completions, selected level on every step, stored values, and all progress event payloads. Empty-loop and finite replay cases prevent accidental progress grants.

### F4 — Medium: invalid negative indices and integer overflow

**Trigger/consequence:** `(idLevel + count) % count` remains negative for sufficiently negative inputs and can overflow for positive inputs. Non-looping selection accepted negative levels. `NextLevel`, the infinite selection limit, and `Map.SaveLevel` overflowed at `int.MaxValue`. Negative saved progress also entered the model unchanged.

**Fix:** `GetLoopLevel` reuses the existing negative-safe `int.Wrap(0, count)`; non-looping selection clamps to nonnegative bounds, including invalid finite counts. Counters/selection saturate instead of overflowing; loaded negative progress becomes zero. The legacy non-looping `countLevels == 0` upper-bound behavior remains `MaxLevel + 1`.

**Concrete arithmetic evidence:** a Python model of unchecked C# addition/remainder produced baseline/fixed values: `(-11, 5): -1 / 4`; `(int.MinValue, 3): -2 / 1`; `(int.MaxValue, 2): -1 / 1`; `(int.MaxValue, int.MaxValue): -2 / 0`. This validates the arithmetic model, not compiled C# execution.

**Regressions:** seven wrap cases, three finite selection/event cases, `Map_LoadRepairsNegativeProgressBeforeNextSave`, and `InfiniteProgress_AtIntegerLimitDoesNotWrapSelectionOrSave` check actual public state and saved values.

### F5 — High: rejected save advances in-memory progress

**Trigger/consequence:** `Map.SaveLevel()` preincremented `_level` inside the provider call. If `SetInt` threw before writing, the map still considered the level complete; `LevelManager` would no longer save that completion on retry because its frontier had moved.

**Fix:** retain the previous counter, preserve the existing update-before-provider-callback behavior on success, and restore the counter before rethrowing a write exception. This restores model state, not arbitrary provider writes or event side effects.

**Regression:** `Map_SaveFailureRestoresProgressAndRetryRecordsOneCompletion` uses a rejecting memory provider, checks unchanged model/storage after failure, then checks a successful retry records exactly one completion.

### F6 — Medium: current and passed button events are reversed

**Trigger/consequence:** `UpdateVisual()` supplied ID 1 for passed levels and ID 2 for the frontier, while `LevelButton.Events()` and its public guide define 1 as current and 2 as passed. Inspector listeners received the wrong event for both states.

**Fix:** manager now sends **2 = passed, 1 = current frontier, 0 = locked**. Inspector collection now uses one `GetComponentsInChildren<LevelButton>(true)` query in hierarchy order, replacing repeated descendant scans and an unordered `HashSet`; inactive buttons remain included.

**Regression:** `MapButtons_EmitPassedCurrentLockedStatesInLevelOrder` asserts `[2, 1, 0]`, current/passed event counts, and selectable states. Inspector collection itself still requires central Editor verification; no serialized assets were rewritten.

### F7 — Medium: selection API and runtime button binding are incomplete

**Trigger/consequence:** `LevelManager.SetLevel` was `internal`, preventing level selection from a consumer assembly even though the library exposes scene controls for it. Runtime-created `LevelButton`s relied on `OnValidate` to discover their UI Button, so no runtime click handler was attached without that Editor pass. Clicking an active unbound button threw.

**Fix:** widen `SetLevel(int)` to public without changing its signature; resolve the optional UI Button on `Start`, remove the listener on destruction, and ignore clicks without a live manager. Use Unity equality for stale/destroyed component references in validation.

**Regressions:** consumer test assembly calls `SetLevel` directly. `RuntimeLevelButton_BindsMissingButtonReferenceAndUnsubscribesOnDestroy` exercises a real UI click before and after component destruction; `LevelButton_CanBeBoundAfterAnUnboundClick` verifies late manager assignment through selected level output.

### F8 — Medium: TextLevel deferred subscription outlives its enabled state

**Trigger/consequence:** disabling only the component did not stop its pending `WaitWhile`; the callback could bind and update a disabled label when a manager appeared. Deactivating the GameObject stopped the coroutine, but re-enabling before manager creation never restarted the wait because only `Start` scheduled it.

**Fix:** retain and stop the existing package coroutine handle on disable. Enabled initialization schedules a fresh wait when needed, checks `isActiveAndEnabled`, and continues to remove old event listeners before rebinding.

**Regressions:** two real lifecycle cases hide/show or disable the label before creating a manager, then assert actual formatted text/current numeric output and absence of updates while disabled.

### F9 — High: overlapping/manual scene requests lose operation ownership

**Trigger/consequence:** every load request could replace `_currentOperation`; an earlier coroutine then cleared a later request's operation/panel. Manual operations could become unreachable by `ProceedScene()`, leaving Unity's loading queue waiting indefinitely. Destroying/deactivating a manual controller also left activation disabled.

**Fix:** one pending async request per controller, guarded before calling injected loader code. `IsLoading` exposes that state. Request identity protects cleanup from old coroutines and callback reentry. Failure/finally cleanup clears state; GameObject deactivation or component destruction releases native activation and clears the panel. Async calls on inactive objects are ignored. Unity cannot cancel the native load, so cleanup permits it to activate; this behavior is explicitly documented. Component-only disabling leaves its coroutine running.

**Regressions:** `SceneFlow_RejectsReentrantLoadAndCanRetryAfterNullOperation` observes busy state inside loader dispatch, blocks a nested sync request, and verifies retry after two null-operation failures with no false completion event. `SceneFlow_InactiveAsyncRequestDoesNotReachLoaderOrBlockLaterLoad` verifies no inactive dispatch and a successful later sync dispatch. Native manual-operation cleanup/completion remains a required central engine scenario because the existing loader contract returns concrete `AsyncOperation` objects.

### F10 — Medium: loading UI fails to preserve its promised state

**Trigger/consequence:** Plain mode never assigned loading text; Percent mode overwrote the manual ready prompt on the next frame. The loop exited without reporting progress 1 or updating the final UI.

**Fix:** Plain displays `_progressPrefix`; retain the ready prompt while activation waits; publish/apply progress 1 before successful completion. UI and callback paths check request ownership when callbacks stop or replace a request.

**Validation limit:** reviewed statically. Automated native progress/prompt/completion coverage requires the central engine scenario or the operation adapter proposed below; no fake completed Unity operation or private-method-only assertion was added.

## Documentation

Updated existing English public pages only:

- `Assets/Neoxider/Docs/Level/README.md`: canonical entry, C# and Inspector paths, frontier semantics, corrected release/import/development sample locations.
- `LevelManager.md`: public direct selection, looping persistence, bounds/null slots, save flushing, map-switch semantics, canonical module link.
- `Map.md`: standalone API, exact compatible save key, absolute progress, overflow/rejected-write behavior.
- `LevelButton.md`: visual IDs, optional UI/manager references, dynamic binding and typed C# calls.
- `TextLevel.md`: explicit source, deferred lifetime, max/offset semantics, inherited API.
- `SceneFlowController.md`: injectable loader, single request ownership, `IsLoading`, async events, progress formats, manual continuation, native cleanup limitations.

`InternalTypes.md` was read but left unchanged: its listed types belong to other `Neo.Core.Level*` owners. No duplicate entry page, DocsEn files, or folder metadata was added.

Кратко: проверены все пять файлов. Исправлены циклический прогресс, границы индексов, необязательные ссылки UI, состояния кнопок, откат прогресса при ошибке сохранения и жизненный цикл отложенных подписок/загрузок. Unity-проверки не запускались; они остаются у координатора.

## Added regressions

New files, each with a new unique GUID `.meta`, under `Assets/Neoxider/Tests/Edit/Level/`:

- `Audit20261023NeoLevelProgressTests.cs`: **26 cases** observing selection, map discovery/advancement, real save-key values, rejected-save recovery, events, and button output. Uses an isolated memory provider restored in teardown.
- `Audit20261023NeoLevelLifecycleTests.cs`: **5 cases** using the existing Edit test assembly and `EnterPlayMode` / `ExitPlayMode` instructions for real lifecycle/coroutine behavior. Uses public behavior and formatted TMP output, real UI Button dispatch, and the public loader seam. Reflection is limited to configuring existing private serialized authoring fields, not invoking implementation methods or asserting private state.

**31 cases prepared; zero executed by this worker.** Existing test files/asmdefs were not edited. Used namespaces are already exercised by existing tests in this test assembly; central compilation remains authoritative.

## Substantial improvement proposal

### Proposal A — Separate a progression model from scene/persistence/UI adapters (not implemented)

**Problem:** `LevelManager` combines scene singleton lifecycle, mutable map configuration/progress, global persistence, level rules, event dispatch, and button refresh. Absolute completion count and reusable level index have different meanings but both are represented by `int` values and a legacy `level` name. Configuration has no explicit runtime initialization path outside serialized fields. Repeated completion of a one-level loop cannot be distinguished from a duplicate notification. Save errors cannot provide transaction guarantees through the current void/global provider API.

**Proposed architecture/public API:** introduce a plain C# `LevelProgression` model constructed from immutable `LevelMapDefinition` data and explicit persisted snapshots. Coordinate with `Neo.Core.Level` / `Neo.Progression` owners before selecting the shared implementation; reuse compatible contracts instead of building parallel level models. Suggested API:

```csharp
LevelSelection Selection { get; }       // Map ID, zero-based content index, absolute completed count.
bool TrySelectMap(int mapId, out LevelSelection selection);
bool TrySelectLevel(int levelIndex, out LevelSelection selection);
LevelSelection SelectFrontier();
bool TryComplete(LevelRunId runId, out LevelProgressChange change);
event Action<LevelProgressChange> ProgressChanged;
```

`LevelRunId` identifies one accepted gameplay completion and makes repeated callbacks idempotent. The model validates positive loop counts and explicit finite/infinite definitions once. An optional `ILevelProgressStore` adapter handles snapshot persistence and success/error results. The MonoBehaviour exposes an additive `Initialize(...)` method, forwards to the model, and projects changes into existing UnityEvents; optional button/text views consume those changes independently. Prefer `long` absolute progress in the new model, keeping content indices as `int`.

**Compatibility/migration:** retain every existing serialized field, `Map` facade, misspelled `GetCopmplete`, method, and UnityEvent. Existing scenes get definitions/snapshots via the wrapper; default behavior and event payloads remain as documented here. Continue reading `Map_<saveKey>_<id>__level` without renumbering maps. Any storage upgrade for progress beyond `int.MaxValue` must be versioned/opt-in and document clamped legacy `int` projections. Do not migrate prefabs or stored keys implicitly. Preserve the no-UI path.

**Acceptance scenarios:** a plain C# caller initializes and selects/completes levels without GameObjects or global saves; three levels repeat over eight completions while absolute progress reaches eight; finite chapters transition at their frontier; duplicate run IDs advance once, including one-level loops; rejected persistence has an explicit retry result; old progress 4 with three loop levels loads as content index 1; empty map slots retain stable save identities; replacing the view does not change gameplay; progress beyond the old integer limit is covered with explicit legacy behavior.

**Effort/risk:** approximately 3–5 focused developer days plus central compatibility tests; medium risk from save migration, completion identity, and UnityEvent ordering. Deliver the pure model and adapter tests first, then adapt the component behind preserved APIs. This is a proposal, not completed code.

### Proposal B — Make asynchronous scene operations observable without a native scene (not implemented)

**Problem:** `ISceneFlowSceneLoader` can fake dispatch but returns Unity's concrete `AsyncOperation`; tests cannot drive pending/0.9/activation/completion/failure states independently. Shared static loader configuration also affects every controller, and queue blocking across different controllers remains Unity-global.

**Design:** add an optional operation abstraction with `Progress`, `IsDone`, activation control, and a typed failure result, implemented by a Unity `AsyncOperation` adapter. A per-controller injected operation factory should take precedence over the existing static loader fallback. Preserve the current interface and static property through adapters; do not change serialized scene targets or existing event types. Explicitly document whether a future service coordinates all scene loads or only one controller.

**Acceptance:** deterministic tests cover repeated waiting frames, ready prompt persistence, `ProceedScene`, destruction/deactivation, retry after failures, callback reentry, and exactly one terminal notification without loading real scenes. Keep one central real-scene smoke test for Unity activation behavior.

**Effort/risk:** 1–2 developer days plus a central Play Mode pass; low-to-medium API/ordering risk if introduced additively.

## Verification and remaining risks

### Checks actually run

- Scoped `git diff --check -- Assets/Neoxider/Scripts/Level Assets/Neoxider/Docs/Level <four new test/meta paths> Docs/Audits/2026-10-23/Neo.Level.md`: exit 0. New untracked files also receive direct trailing-whitespace checks, since normal `git diff` does not inspect them.
- Read-only Python consistency validation: exact manifest/directory inventory **5/5**, strict UTF-8 and trailing whitespace for **17 reviewed/changed text files**, balanced C# delimiters outside strings/comments for the **7 owned/new C# files**, no `var` declarations or direct obsolete instance-ID APIs.
- Baseline `git show ca198556:<owned path>` comparisons: serialized private field names and existing public method/field declarations preserved; `SetLevel` visibility and `IsLoading` are additive API changes.
- Both new test GUIDs found exactly once across `Assets` using `rg`; all local links in the seven Level documentation pages resolve.
- Arithmetic model described in F4 confirmed the four concrete baseline failures/fixed results. Case declaration count: **26 + 5 = 31**. This is source/static validation, not C# compilation or test execution.
- Verified the referenced `Samples~/Demo/Scenes/Level/LevelFlowDemo.unity` and demo controller exist. No sample import/rename was performed.

### Coordinator execution / residual risks

1. **Pending:** compile under supported Unity branches, then run existing `Neo.Level.Tests.LevelManagerTests`, `SceneFlowControllerTests`, and both `Audit20261023NeoLevel*` fixtures. The lifecycle fixture intentionally enters Play Mode from Edit tests; use an isolated test scene with TMP resources. No compilation, console, Play Mode, tests, or build result is claimed here.
2. **Pending native scene scenario:** manual async load reaches 0.9; leave it waiting across frames and confirm ready text remains visible; call `ProceedScene`, observe terminal progress 1 and one completion. Repeat with controller GameObject deactivation/component destruction and another queued operation to confirm native activation is released. Check duplicate requests and progress callback reentry. Controller destruction can precede the final completion frame in a normal single-scene load, so surviving completion UI should live outside the departing scene or use an explicit persistent owner.
3. Visual IDs now match the component's existing public event contract. Projects that worked around the previous inversion should review their listeners. Inspector collection is deterministic but may rebuild a previously unordered button list on the next validation; review authored hierarchy order. No prefab/scene was modified by this worker.
4. Per-controller loading guards do not coordinate separate controllers. Native loads cannot be canceled; cleanup may still activate the requested scene. Stopping the controller's coroutine through arbitrary external `StopAllCoroutines` calls is outside the supported lifecycle contract. Proposal B addresses stronger operation observability/ownership.
5. **Cross-boundary follow-up for `Neo.Tools.Text`:** the read `SetText.Set(int)` snapshot adds `indexOffset` in `int` and then converts to `float`; very large `TextLevel` values can overflow the display offset or lose precision. Recheck after that owner's concurrent audit and cover exact large-integer text/animation behavior there. Its tween lifetime also belongs to that owner. No dependency source was edited.
6. `Map` rollback restores its counter after an exception but cannot roll back provider writes or save-listener side effects; disk flush remains the provider's responsibility. Legacy map selection does not automatically normalize the current level on a map switch; call `SetLastLevel` explicitly. Proposal A gives these contracts a clearer runtime boundary.
7. `InternalTypes.md` contains a legacy catalog of other `Neo.Core.Level*` topics. Coordinator may route/consolidate it with those documentation owners; it was excluded from edits here.
