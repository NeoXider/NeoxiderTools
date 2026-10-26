# Neo.Core.Level.Components: audit

Status: source audit and focused fixes complete; static validation passed; Unity compilation and NUnit execution pending centrally.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Core/Level/Components/Neo.Core.Level.Components.asmdef`.
- Scope: 1 owned C# file; nested assemblies and their documentation excluded.

## Scope

Reviewed **1/1 inventory files completely**: `Assets/Neoxider/Scripts/Core/Level/Components/LevelComponent.cs` (337 lines after fixes).

Read the complete root `AGENTS.md`, worker contract, exact manifest entry, and original report before editing. The assembly's asmdef was inspected read-only. Documentation ownership is limited to `Assets/Neoxider/Docs/Core/Level/Components/`. No nested source, existing tests, other workers' reports/docs, shared configuration, scenes, prefabs, or sample layouts were edited.

| Reviewed area | Outcome |
|---|---|
| Serialized configuration, defaults, attributes, public contracts | Preserved all 13 explicitly serialized field names and 25 public declarations, including public reactive fields. No serialization migration. The existing `ResetProgress` naming avoids Unity's editor `Reset` message. |
| `LevelCurveDefinition` setter and curve application | Confirmed lazy assignment before model creation, reevaluation after creation, null fallback to the model's linear curve, and reactive refresh. Curve reassignment does not publish a gameplay level-change event. |
| `Awake`, initialization, all provider getters | Fixed callback re-entry; initialization remains lazy and idempotent. A failed initialization can be retried. Raw reactive convenience getters intentionally remain mirror reads and require prior initialization. |
| `AddXp`, `SetLevel`, event handlers | Boundary arithmetic and clamping are delegated to the existing `LevelModel`; fixed stale reactive values seen inside callbacks. Nonpositive additions and direct-level mode were traced through the domain implementation. |
| Reset and snapshots | Fixed deferred initial loading overwriting resets or being omitted from snapshots. Reset retains the configured start values and cap; snapshots are independent DTOs. |
| `Save`, `Load`, missing/invalid data | Fixed restore order and completion-event state. Retained SaveProvider JSON format, sanitization, whitespace-key guard, explicit flush, and gated diagnostics. Save/Load still require model creation; before Awake callers should initialize first. |
| `OnDisable`, `OnDestroy`, resource ownership | Auto-save remains guarded by configuration, initialization, and a nonblank key. Model events are detached on destruction. No coroutines, asynchronous operations, scene searches, native resources, or static component subscriptions exist. |
| Scene/NoCode and dependencies | Read `LevelNoCodeAction` as context; it delegates to this public API. Read the relevant domain/profile/curve, SaveProvider/ISaveProvider and reactive contracts, and existing level/progression test coverage. No bridge edits were necessary. |
| Unity version compatibility | No instance/entity ID APIs or version branches occur in the owned source or new tests. No editor-only dependency was added to runtime. Actual compilation under both Unity version families remains pending. |

Reuse receipt: read the `neoxider-tools` skill and relevant level/module references, and searched its helper references before adding the private restore helper. Reused `LevelModel.SetMaxLevel`/`SetState`, `LevelProfileData.Sanitize`, `SaveProvider`, and existing reactive setters/notifications. No replacement progression or persistence system was introduced. The helper only coordinates the existing model operations and its component event forwarding. `unity-game-agent` was applied in file-only scope; child agents, Unity, builds, test runners, installs, and shared-state workflows were explicitly prohibited by the task. There was no independent QA run.

## Findings and fixes

Findings below are demonstrated by the inspected control/data flow. Regression cases are authored, **not executed**; none of the behavioral outcomes below is presented as a Unity test result.

### F1 — High: initialization recursively re-enters from ordinary observer reads

**Source:** `LevelComponent.EnsureInitialized`, `Load`, `SyncReactiveFromModel`; fixed source around lines 187–212 and 240–266.

**Trigger and consequence:** `_initialized` previously became true only after loading and reactive notifications. An `OnProfileLoaded` listener reading `component.Level` called `EnsureInitialized` again, which called `Load` and the same listener again. An unbounded listener caused stack exhaustion. With loading disabled, a `LevelState` listener reading `Level` had the equivalent recursion through `SyncReactiveFromModel`. Even bounded listeners caused duplicate initialization and repeated provider reads. Missing/invalid saved data also published `OnProfileLoaded` before the initial mirrors were synchronized.

**Fix:** mark initialization before notifying consumers, restore the flag on an escaping exception, and make the load completion path synchronize mirrors before its event for valid, missing, and malformed profiles. Avoid a second initial refresh after Load already refreshed the state.

**Regressions:** `EnsureInitialized_LoadListenerReadsCurrentState_WithoutLoadingAgain` covers saved, missing, and malformed data; its listener bounds re-entry so the old implementation fails assertions instead of crashing the runner. It observes exactly one load and all six provider/mirror values. `EnsureInitialized_ReactiveListenerReadsLevel_WithoutRepeatingInitialization` covers the no-load path. `EnsureInitialized_WhenProviderReadFails_CanRetryAndLoadProgress` protects recovery after moving the flag earlier.

### F2 — Medium: direct-level restore irreversibly clamps against the previous cap

**Source:** `ResetProgress`, `Load`, new `RestoreModelState`; fixed source around lines 91–96 and 240–282.

**Trigger and consequence:** with XP disabled and an existing cap of 3, loading saved level 8 with cap 10 previously executed `SetState(..., 8)` under cap 3, then raised the cap to 10. The result remained level 3, and a subsequent save persisted the wrong progress. Removing the cap had the same problem. Resetting a profile with a loaded cap of 3 back to configured start level 8/cap 10 also ended at level 3.

**Fix:** restore the incoming/configured cap before setting state. Temporarily detach this component's level-change forwarding while restoring, then reattach in `finally`; lowering the incoming cap must not emit a notification for an intermediate clamp of the previous profile. Publish the final state through the existing reactive refresh.

**Regressions:** `Load_DirectLevel_UsesIncomingCapAndPersistsTheRestoredProfile` covers higher, removed, and lower incoming caps. It checks the real component state, serialized profile after Save, save count, absence of intermediate events, and functioning subsequent level-change forwarding. `ResetProgress_DirectLevel_RestoresConfiguredLevelAfterLoadingALowerCap` checks the reset path and XP/level mirrors.

### F3 — Medium: pre-Awake reset and snapshot bypass initial saved progress

**Source:** `ResetProgress`, `GetProfileSnapshot`; fixed source around lines 91–96 and 215–225.

**Trigger and consequence:** both methods previously called only `EnsureModel`, leaving `_initialized` false. A snapshot taken on an inactive/unawakened component omitted its saved profile and exposed configured defaults; saving that created model could overwrite existing progress. Resetting to 50 XP before Awake appeared successful, but the next `TotalXp` getter initialized and loaded the old 750 XP again.

**Fix:** route these public operations through `EnsureInitialized`. The snapshot includes saved progress; reset performs initialization before applying the explicit reset, so subsequent reads do not undo it. Reset still requires an explicit Save or configured disable-save to persist.

**Regressions:** `GetProfileSnapshot_BeforeAwake_IncludesAutomaticallyLoadedProgress` observes the persisted XP/level/cap and exactly one read. `ResetProgress_BeforeAwake_IsNotOverwrittenByDeferredAutoLoad` observes the reset through provider getters, the reactive mirror, and another snapshot.

### F4 — Medium: XP/level callbacks expose mutually inconsistent reactive state

**Source:** `HandleLevelChanged`, `HandleXpGained`; fixed source around lines 318–335.

**Trigger and consequence:** at 0 XP, `SetLevel(5)` updates the model to 400 XP but formerly invoked `LevelState` listeners and `OnLevelUp` while `XpStateValue` still held 0. `AddXp(250)` formerly notified XP listeners/`OnXpGained` while `LevelStateValue` remained 1 and remaining XP remained 100, although the model was already level 3 with 50 remaining. Final synchronization hid this discrepancy from tests that only inspected state after the call; UI/reward listeners ran against inconsistent values.

**Fix:** stage all three mirrors from the model before each existing selective event/notification sequence. Preserve the existing gameplay event names, arguments, and notification routing.

**Regressions:** `SetLevel_LevelObserversSeeTheSynchronizedXp` checks snapshots captured inside every reactive level notification and the gameplay event. `AddXp_XpObserversSeeTheSynchronizedLevelAndRemainingXp` checks both a 25-XP within-level addition and a 250-XP multi-level addition inside reactive/gameplay XP callbacks. Assertions run after collecting values, so reactive exception handling cannot swallow assertion failures.

Кратко: исправлены рекурсивная инициализация из обработчиков, потеря уровня при смене лимита, несогласованная работа до Awake и устаревшие reactive-значения внутри событий. Публичные сигнатуры и serialized-поля сохранены; выполнение Unity-тестов остаётся за координатором.

## Documentation

Updated the existing canonical `Assets/Neoxider/Docs/Core/Level/Components/LevelComponent.md` rather than adding a competing entry point. It now explains:

- runtime C# usage, assembly/namespace, lazy initialization and retry behavior;
- the actual default linear curve (the previous claim that omitting the asset keeps the level flat was incorrect);
- XP versus direct-level mode, cap replacement, snapshot/reset semantics, and persistence prerequisites;
- synchronized callback values, completion-event semantics, repeated reactive refreshes, and the historical `OnLevelUp` name for direct level decreases;
- actual Inspector fields, unique save keys, scene UnityEvent wiring, reactive binding, and the optional existing `LevelNoCodeAction` path.

All relative links resolve. No sibling/bridge docs, module/root entry points, DocsEn, or metadata belonging to other owners were modified.

## Added regression files

- `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelComponentsTests.cs`: **14 NUnit cases across 9 test methods**; fixture is nonparallel because `SaveProvider` is static.
- Matching `.cs.meta`, unique GUID `dc852ebbf7694e80a1f90cf7d137c46b`.

The fixture uses inactive GameObjects and explicitly drives the public API. Reflection only configures existing private Inspector inputs; no assertions inspect `_model`, `_initialized`, or implementation layout. A private string-profile provider isolates storage and supports observable load/save counts and a deliberate read failure. The prior provider is restored in teardown, and auto-save is disabled for test objects. Existing tests were not edited. The current `Neo.Editor.Tests` asmdef already has all required references.

## Substantial improvement proposal

### Explicit initialization options and an injectable profile store — proposed, not implemented

**Problem:** the component exposes useful runtime behavior through `ILevelProvider`, but XP/direct mode, starting values, cap, save key, and loading policy are private Inspector-only inputs. Runtime factories for multiple players, battle passes, previews, or independently persisted entities therefore need different prefab authoring or reflection to configure otherwise generic instances. The global SaveProvider/default key couples profiles and complicates failure testing; the new fixture needs Inspector-input reflection and a global provider swap for this reason. The pure `LevelModel` remains available, but it does not replace the need for a configurable scene/reactive adapter.

**Proposed architecture/public API:** introduce immutable `LevelComponentOptions` containing curve (`ILevelCurveDefinition`), XP mode, start XP/level, max level, key, and initial-load/disable-save policies. Add `bool TryInitialize(LevelComponentOptions options, ILevelProfileStore store = null)` for an inactive/uninitialized component. Add a small typed store contract:

```csharp
public interface ILevelProfileStore
{
    bool TryLoad(string key, out LevelProfileData profile);
    void Save(string key, LevelProfileData profile);
}
```

The default adapter delegates to the existing SaveProvider and JsonUtility; it does not implement another save backend. Store calls exchange sanitized/cloned data, not scene references. A failed provider operation remains observable and retryable. The scene component continues to own the existing LevelModel and reactive/events projection. Keep the store/options in the narrow component integration boundary; involve the Core owner only if a shared contract is later required.

**Compatibility/migration:** retain every existing serialized field, UnityEvent, and public method. Awake/parameterless EnsureInitialized construct options from serialized values and select the default adapter, preserving current scenes and JSON keys/format. Existing callers require no migration. Explicit initialization is opt-in, before activation or before any lazy getter; return false if initialization already occurred, without resetting progress or silently replacing dependencies. Document the ordering alongside the factory example. Do not add scene references to ScriptableObjects or change the meaning of the persisted cap.

**Acceptance scenarios:** two components use different explicit keys in one store without overwriting each other; runtime factories configure XP and direct modes without reflection; initial-load callbacks can read coherent state; provider failure is retried without replaying successful initialization; the existing reset/cap/snapshot regressions pass through both serialized and explicit initialization; old prefabs and existing saved JSON behave identically; disabled persistence performs no store calls. Tests inject separate stores, avoiding global provider swaps.

**Effort/risk:** approximately 2–3 developer days including default adapter, validation, EditMode coverage, and English/Russian usage notes. Main risks are the first-initialization ordering, accidental double loading, and compatibility of failure/event semantics. Keep this separate from the completed fixes and obtain module/API review before implementation. No shared asmdef change is currently requested.

## Verification and remaining risks

Actual checks performed:

| Check | Result |
|---|---|
| Manifest inventory versus assembly directory | PASS: exactly the single owned C# file; reviewed in full. |
| Public declarations versus `ca198556` | PASS: 25 declarations unchanged. |
| Explicit serialized field names versus `ca198556` | PASS: all 13 retained in order. Public reactive fields also retained. |
| Python strict UTF-8 decoding, replacement/C1 character and trailing-whitespace checks | PASS for changed source, canonical doc, new test, test metadata, and this report. |
| Lexical C# delimiter balance, explicit local types, obsolete ID API scan | PASS for owned source and new fixture. This is a text-level consistency check, not compilation. |
| Existing test assembly references | PASS: Core, Components, Save, Reactive, and NUnit already referenced; no configuration edits. |
| Canonical doc relative links | PASS. |
| New metadata GUID lookup under Assets | PASS: one occurrence, in the new test's metadata. |
| `git diff --check -- <owned source/doc/test/meta/report paths>` | PASS, exit code 0. Untracked package files were covered separately by the whitespace checks. |
| Scoped final source/doc diff review | PASS: focused behavior/docs changes only; serialized and public contracts retained. |

Source snapshot: initial SHA-256 `470b381fe17a4de274efc57fa5853ce44af4868ab27f73f4fb1e7393270288f6`; after fixes `616dd0235e0c7f2de3b4a074739dd135fb6227b164910fbb52066aa93fa31cab`. New fixture SHA-256 `7eccf6bc0221da025bda78ba9343a8656b79f80034f22ccfa3494233ddae9669`.

**Not run:** Unity import/compilation, NUnit/EditMode or PlayMode execution, console checks, builds, or cross-version compilation. These were prohibited for workers and remain coordinator-owned. Text-level checks do not prove C# compilation or runtime event behavior. The coordinator should run the new `Audit20261023NeoCoreLevelComponentsTests` fixture and existing Level/Progression integration tests, then perform the campaign's Unity 6.0–6.4 and 6.5+ compilation checks against the final combined filesystem state.

**Remaining compatibility considerations:** reactive refreshes can still notify repeatedly during one mutation; `OnLevelUp` still reports direct decreases; plain reactive convenience reads and direct Save/Load before model creation still require explicit initialization. These behaviors are now documented rather than changed speculatively. Public reactive fields remain mutable, and callers should treat them as output. Save/load provider failures outside JSON restoration still propagate; JSON/profile restoration failures use the existing gated warning path. The shared Core/Save/Reactive implementations are being audited independently, so integration verification must use the coordinator's final combined changes.

No outstanding cross-boundary code/config edit is required for the fixes. No commits, installs, child agents, Unity operations, or build/test runners were used.
