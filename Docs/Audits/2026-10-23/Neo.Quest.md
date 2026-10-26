# Neo.Quest: audit

Status: full source review and scoped fixes complete; Unity compilation/tests remain centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Quest/Neo.Quest.asmdef`.
- Scope: 8 C# files; nested assemblies excluded.

## Scope and review coverage

Read `AGENTS.md`, the complete worker contract, the exact `Neo.Quest` manifest entry, and this report before source edits. Reviewed all **8/8 owned C# files, 1,488 baseline lines**, without nested assembly edits. The current Quest subtree has one asmdef and exactly the manifest's eight source files.

| Owned file under `Assets/Neoxider/Scripts/Quest/` | Complete review |
|---|---|
| `Bridge/QuestNoCodeAction.cs` | Every action, manager resolution, required config/index validation, success/failure messages, flow bypass on restart, Unity Reset lifecycle, Inspector mutation safety. Fixed false results and action button availability. |
| `QuestConfig.cs` | Serialized identity/display/objectives/start conditions/next IDs, asset creation, generated ID behavior, reusable asset constraints. No source change; documented unique IDs, positional objectives, and metadata-only next IDs. |
| `QuestContext.cs` | Marker-only behavior, absence of registration/lifecycle work, explicit manager context assignment. No source defect; replaced placeholder public documentation. |
| `QuestFlowConfig.cs` | Strict-chain predecessor selection, null slots, duplicate references, standalone ordering, manager eligibility. Fixed condition/ID checks and null-slot bypass. |
| `QuestManager.cs` | Full API, singleton initialization, save/load, state/config registry, accept/fail/reset/restart, both event surfaces, all three notification loops, editor entry points, context fallback, logging. Fixed progress, callback reentrancy, load normalization, late registration, and Inspector actions. |
| `QuestObjectiveData.cs` | All objective types and setters, counter semantics, nullable condition and text/target normalization. No source change; counters require gameplay notifications; null custom conditions intentionally need external completion. |
| `QuestState.cs` | Constructor, status, count/progress/completion bounds, internal mutation, serialization and list migration. Fixed missing/unequal list recovery. |
| `QuestStatus.cs` | Enum values and persisted integer compatibility. No source change; values preserved. |

Context read: all 13 owned Quest doc pages, the existing `QuestManagerTests.cs`, runtime/test asmdefs, relevant `Singleton<T>`, `SaveProvider`, `ConditionEntry`, and `ButtonAttribute` APIs. Reused `QuestState` mutation APIs, `QuestManager.CanAcceptQuest`, `SaveProvider`, `NeoDiagnostics`, and existing `Button.PlayModeOnly`; no parallel quest/save/helper subsystem was introduced. Consulted the Neoxider module/quest and extension references before edits. Unity skills were applied as file-only work under the explicit worker restrictions; there was no independent child-agent QA.

No commits, shared config edits, installations, Unity/MCP access, builds, test runners, scene/prefab/sample edits, or child agents were used.

Final change scope: four owned source files, ten owned public doc pages, one new regression fixture and its `.meta`, and this report. The other four owned source files were reviewed and left unchanged.

## Findings and fixes

The following are demonstrated source-path defects. Regression scenarios below are authored for central execution; none were run by this worker.

### Q1 — P2: completed binary objectives report progress zero

- Trigger: complete a `CustomCondition`, `ReachPoint`, or `Talk` objective.
- Before: `CompleteObjective` set the completion flag and published progress `1`, but never stored that value. `GetObjectiveProgress` and saved JSON retained `0`, contradicting the callback and UI.
- Source: `QuestManager.cs:352`, `Load()` at line 102.
- Fix: store progress before events. Known-config loads also reconstruct progress `1` for completed binary objectives in legacy saves.
- Evidence: `BinaryObjective_StoresReportedProgressAndPersistsIt` (three types), `Load_LegacyCompletedBinaryObjectiveReconstructsProgress`; assert callback-visible state, round-trip value, flag, and quest status.

### Q2 — P1: reentrant callbacks duplicate completion/rewards or overwrite failure

- Trigger: a progress callback credits the same counter again; an objective-completed callback completes another objective; or a callback fails/restarts the quest.
- Before: counter flags were set after progress events. A bounded callback could credit a one-count objective twice and emit completion twice. Nested completion of the second objective caused both the inner and outer call to emit quest completion. Outer calls could also overwrite Failed with Completed or complete a detached state after restart.
- Source: `QuestManager.cs:352` (`CompleteObjective(QuestConfig, int)`).
- Fix: commit objective progress/completion before callbacks; finish the quest only if the original state is still the current active state. Public signatures and event channel order remain unchanged.
- Evidence: `CounterProgressCallback_RepeatedCreditCompletesOnlyOnce` tests both UnityEvent and C# callbacks; nested-objective, fail-from-progress, and restart-from-progress regressions assert exact completion counts and current state identity/status.

### Q3 — P1: an old notification credits a replacement quest state

- Trigger: an objective callback restarts a quest with another matching objective, or replaces another state still in the notification snapshot.
- Before: snapshots prevented collection mutation exceptions but retained old objects. Subsequent `CompleteObjective(config, index)` resolved the newly accepted state by ID and credited it using the old notification.
- Source: `QuestManager.cs:419`, `:455`, `:672` (`NotifyKill`, `NotifyCollect`, `EvaluateObjectiveConditions`).
- Fix: verify current-state reference identity and Active status both at snapshot entry and before each objective. Replaced states stop receiving that notification.
- Evidence: `Notification_RestartedQuestDoesNotReceiveRemainderOfOldNotification` covers all three notification paths and asserts the replacement remains active with zero progress and unfinished objectives.

### Q4 — P2: loaded duplicate/invalid IDs and unequal lists break registry invariants

- Trigger: saved states contain duplicate/empty IDs, absent lists, or progress and completion lists of different sizes, including a temporarily unregistered config.
- Before: all entries were published immediately; null entries could throw after publication, duplicate IDs made lookup ambiguous, and unknown configs never had their lists repaired. Completion writes could remain out of bounds indefinitely.
- Source: `QuestManager.cs:102`; `QuestState.cs:89` (`EnsureObjectiveCount`).
- Fix: normalize a temporary list, skip null/empty IDs, retain the first duplicate, repair both lists to their largest saved/config count, preserve unknown IDs, then publish the registry. Exceptions leave the previous registry untouched.
- Evidence: `Load_NormalizesDuplicateIdsAndUnequalListsWhileKeepingUnknownQuests` asserts exact registry size, first-entry progress, retained unknown completion, repaired sizes, and eventual completion of the known quest. Unity's precise handling of null JSON fields remains part of central test execution.

### Q5 — P2: a config registered after load cannot complete appended objectives

- Trigger: auto-load restores an unknown quest with one saved objective; game code later registers its runtime config with two objectives.
- Before: list migration ran only for configs already known during `Load`. `RegisterQuest` did not reconcile the saved state, so completion of the added objective was silently discarded by bounds checks.
- Source: `QuestManager.cs:269` (`RegisterQuest`).
- Fix: a newly registered config grows the restored state's lists through the existing migration API.
- Evidence: `RegisterQuest_AfterLoadExpandsStateForRuntimeConfig` loads before registration and then completes both objectives through the public manager API.

### Q6 — P2: NoCode success events fire when no action occurred

- Trigger: CompleteObjective/Fail on an unaccepted or completed quest, invalid/already completed objective, empty quest ID, or unknown serialized action enum value.
- Before: CompleteObjective/Fail unconditionally emitted success after void manager methods silently ignored the operation; unknown enum values emitted no result at all.
- Source: `Bridge/QuestNoCodeAction.cs:200`, `:226`, `Execute`, `TryValidateQuest`.
- Fix: validate active state/objective eligibility, reject empty IDs, and emit failure for unsupported actions. Counter credit still reports success for a valid increment before the threshold.
- Evidence: ten bridge cases assert success/failure counts, messages, and actual state/progress, including successful counter and Fail paths followed by rejected duplicate calls.

### Q7 — P2: flow availability ignores manager eligibility and null slots bypass order

- Trigger: query flow availability for an empty-ID quest or one whose start conditions fail; or define `[first, null, last]` in a strict chain while first is incomplete.
- Before: availability could return true although the manager would reject acceptance. An immediately preceding null slot made last look like a chain's first quest.
- Source: `QuestFlowConfig.cs:25`, `:65`.
- Fix: delegate base eligibility to `manager.CanAcceptQuest`; resolve the nearest preceding non-null quest consistently with the ordered list's null filtering.
- Evidence: two `FlowGate_RejectsManagerIneligibleQuest` cases plus `StrictChain_NullSlotDoesNotUnlockUnfinishedPredecessor`, which verifies locked and unlocked states and the predecessor output.

### Q8 — P1: runtime Inspector actions can mutate persisted progress in Edit Mode

- Trigger: click the manager/bridge action buttons outside Play Mode, particularly Reset All with Auto Save enabled.
- Before: Inspector buttons could invoke runtime operations and overwrite `Settings_Quests` without entering gameplay. The separate Unity Reset callback was already guarded, but other buttons were not.
- Source: all nine `[Button]` declarations in `QuestManager.cs` and `Bridge/QuestNoCodeAction.cs`.
- Fix: use the existing `PlayModeOnly = true` attribute on every runtime action button. Preserve public methods and the existing Reset lifecycle guard.
- Verification: static attribute/API inspection; actual Inspector enable/disable behavior needs the coordinator's Unity check. No reflection-only attribute regression was added.

Also replaced the owned Save method's existing `var` with an explicit `QuestSaveData` type, as required by the repository contract.

## Documentation

Updated ten existing English pages in `Assets/Neoxider/Docs/Quest/`:

- `README.md`: canonical links to code/scenario/flow/save guides, binary/counter/custom-condition semantics, metadata-only next IDs, Play Mode actions, and release/developer/imported sample roots.
- `QuestManager.md`, `Code.md`, `QuestState.md`: typed API, config registration, callback-visible progress, state replacement semantics, synchronous nested events, explicit condition evaluation, reset/restart consequences.
- `QuestBridge.md`, `Scenarios.md`: active/index/completion validation, result events, partial counter success, Play Mode buttons, and the existing availability query API.
- `Saving.md`: normalization, legacy binary progress repair, late configs, index migration limits, no event replay, and distinction between SetString and provider disk flush.
- `QuestFlowConfig.md`: optional gate usage, manager eligibility, nearest non-null predecessor, ordered list behavior, and first matching chain semantics.
- `QuestContext.md`, `QuestConfig.md`: explicit context assignment, marker-only behavior, reusable asset guidance, stable IDs/indices, and next-ID metadata.

No duplicate guide, root/package documentation, excluded topic, or shared doc metadata was edited. Only the new regression C# asset needed a new package `.meta`.

## Substantial improvement proposal

### Proposed, not implemented: a plain C# quest runtime with stable objective identities

**Problem.** State transitions, config lookup, UnityEvent dispatch, condition evaluation, and persistence currently share one MonoBehaviour. Programmatic content still needs ScriptableObjects/private serialized authoring data. Objective identity is positional, so inserting/reordering objectives can silently remap saved progress. Registry lookups are linear; synchronous nested event delivery can interleave callbacks even after the concrete duplication/state-replacement fixes above. Empty objectives, duplicate config IDs, and conflicting chain memberships lack a typed content validation contract.

**Architecture and public API.** Introduce a focused, engine-independent `QuestRuntime` operating on immutable `QuestDefinition`/`QuestObjectiveDefinition` data. Give objectives stable string IDs and keep state/config dictionaries keyed by IDs. Example API:

```csharp
QuestValidationResult ValidateDefinition(QuestDefinition definition);
QuestResult TryAccept(string questId, IQuestConditionEvaluator evaluator);
QuestResult TryCredit(string questId, string objectiveId, int amount = 1);
QuestResult TryFail(string questId);
QuestResult TryRestart(string questId, IQuestConditionEvaluator evaluator);
QuestSnapshot ExportSnapshot();
QuestImportResult ImportSnapshot(QuestSnapshot snapshot);
event Action<QuestChange> Changed;
```

`QuestResult` should carry typed failure reasons plus the affected state revision. `QuestChange` should be immutable and include a state generation/revision. Queue event delivery until the current change finishes; operations requested by listeners run after that event batch, so later listeners cannot receive an older progress value after a nested newer value. Keep sequence rules in a separate optional flow policy. Keep SaveProvider/Unity JSON outside this core; import/export returns data only.

`QuestManager` remains the scene adapter: convert `QuestConfig` to definitions, evaluate existing `ConditionEntry` through an explicit scene-context evaluator, relay existing UnityEvents/C# events, and map existing Save/Load to snapshots. `QuestNoCodeAction` uses typed results instead of duplicating manager eligibility checks. ScriptableObjects remain reusable authoring data; the scene adapter owns scene references.

**Compatibility/migration.** Preserve existing serialized component fields, GUIDs, enum values, manager signatures, and default save key. Introduce a versioned v2 snapshot with objective IDs while continuing to read the current index format. Assign and persist authoring IDs for existing objectives in an explicit content migration; map legacy index entries using that recorded mapping, never generate fresh IDs at every load. Keep index overloads as adapter methods. Unknown quest/objective IDs should survive import until content is registered. Define/review nested event ordering and failure semantics before changing them; retain legacy Reset/Restart behavior or add an opt-in atomic restart method rather than silently changing loss-on-failure behavior.

**Acceptance scenarios.** (1) Run runtime tests without GameObjects/ScriptableObjects/SaveProvider. (2) Load legacy progress, register content later, append and reorder objectives while retaining the correct objective's progress. (3) Restart/accept another quest inside a callback without duplicate rewards, stale trigger credit, or reversed progress notifications. (4) Expose actionable validation errors for duplicate IDs, invalid counter counts, empty objectives, and ambiguous strict chains. (5) Existing manager, bridge, and save round-trip tests continue passing; scene references remain on adapters. (6) Both Unity compatibility versions compile through the adapter with unchanged public bindings.

**Effort/risk.** Estimate 4–6 engineering days for runtime extraction/adapters/tests plus 2–3 days for explicit content/save migration and compatibility verification. Main risks are event ordering changes, preserving old objective identity, and hidden integrations that mutate `QuestState.Status` directly. Stage the work behind the existing manager, ship the legacy importer first, and avoid bulk asset rewriting during extraction. No implementation of this proposal is included in this audit.

Кратко: предлагается выделить тестируемое C# ядро, оставив QuestManager сценовым адаптером; устойчивые ID целей позволят безопасно развивать контент и сохранения.

## Verification and remaining risks

### Actual file-level validation

- Inventory comparison: exact manifest equality, **8/8** C# files reviewed; no nested assembly source in scope.
- All nine C# files (eight owned plus the new fixture): strict UTF-8 reading, absence of U+FFFD, lexical delimiter balance, and no `var` tokens outside comments/strings.
- Compared public/protected/internal declaration lines and private/protected field names against `ca198556` for all eight owned files: unchanged. This is a static compatibility check, not a compilation claim.
- Checked local Markdown link targets across all 13 owned Quest doc pages; verified the demo scene/controller paths under the active `Samples~` root.
- Existing `Neo.Editor.Tests` references already include Neo.Quest, Neo.Condition, Neo.Save, and Neo.Tools.Managers; no asmdef changes are needed for the fixture.
- New fixture: **27 NUnit cases authored**, `Assets/Neoxider/Tests/Edit/Quest/Audit20261023NeoQuestRegressionTests.cs`. Reflection is limited to serialized fixture setup; assertions observe public state/output. Setup uses a unique save key; cleanup deletes only that key, never all user saves.
- New `.meta`: 32-character hex GUID `9538fbfe5ab848e8874930fa672dfba4`, unique in the package scan; importer shape, trailing whitespace, and final newline checked. An initial static check caught a 33-character draft GUID; it was corrected before handoff.
- Final static scan passed: 8/8 inventory and declaration compatibility, 9/9 C# lexical/type checks, 24 UTF-8/newline checks, 13 documentation link checks, and existing test references. These are file-level checks, not C# compilation.
- Scoped `git diff --check -- <owned source/docs/new test/meta/report>` passed with exit code **0**. New untracked test/meta/report whitespace was additionally checked directly because git diff does not include untracked file contents.

### Remaining verification and boundaries

Unity compile/import, Edit Mode tests, Play Mode, Inspector button behavior, and builds were **not run**, explicitly reserved for the coordinator. Run the new fixture and existing `QuestManagerTests` centrally, with particular attention to JsonUtility null-list deserialization and synchronous callback behavior. Verify Unity 6.0–6.4 and 6.5+ centrally; the owned source has no direct obsolete object-ID API or version branch, and this audit adds neither.

No production fix requires another worker's source or shared configuration edit. Cross-boundary action is central validation only: confirm the shared PropertyAttribute Inspector honors the existing PlayModeOnly flag and run the test assembly. Dependency implementations were inspected as context and left untouched.

Remaining content/API limits: objectives still use indices; definitions should have at least one objective, unique non-empty quest IDs, valid counters, and unambiguous chain membership. The current manager does not enforce every authoring invariant. Restart still removes old progress before rechecking conditions; flow rules are opt-in; NextQuestIds remains metadata. Events remain synchronous and nested calls may interleave delivery. Unknown saved enum/count values are not comprehensively sanitized. The proposal addresses these larger contracts separately from the focused fixes.
