# Neo.StateMachine.Editor: audit

Status: source audit and scoped fixes complete; Unity compilation, Edit Mode execution, and visual checks await the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/Neo.StateMachine.Editor.asmdef`.
- Scope: 5 C# files; nested assemblies excluded.

## Scope and review evidence

Read the repository `AGENTS.md`, the entire worker contract, the exact manifest entry, and this report before editing. All five inventory files were reviewed completely; the source root has exactly these five C# files. No nested assembly source was included.

| Inventory file under `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/` | Reviewed behavior | Final lines |
|---|---|---:|
| `StateDataEditor.cs` | Action type discovery, managed references, menu callback lifetime, constructor failures, insertion/removal and serialization | 206 |
| `StateMachineBehaviourBaseEditor.cs` | All serialized settings/references/events/runtime fields, context slot authoring, Play Mode control guards and state selection boundaries | 141 |
| `StateMachineEditor.cs` | Multi-object selection, validation timing, serialized state/transition edits, initial-state legacy compatibility, transition insertion/reordering and condition window ownership | 319 |
| `StateMachineEditorRegistrar.cs` | Delayed initialization, registration detection, temporary Unity object lifetime and failure cleanup | 70 |
| `TransitionEditorWindow.cs` | Header/predicate configuration, subtype fields, Undo/Redo, stale/removed/destroyed targets, menu types, null entries, GUI group balance and temporary serialization handles | 426 |

Read-only context included the assembly definition, runtime `StateMachineData`, `StateData`, `StateAction`, `StateTransition`, `StatePredicate`, `ConditionEntryPredicate`, component fields/lifecycle, `CustomEditorBase`, the module entry/setup guide, and existing tests/test assembly references. Applied `unity-game-agent` and `neoxider-tools` guidance within the explicit file-only/no-child-agent restrictions; verification here is a local review, not independent QA.

Reuse receipt: kept `CustomEditorBase`, `NeoxiderEditorGUI`, Unity `SerializedObject`/`SerializedProperty` and Undo, and the existing typed `StateAction`/`StatePredicate` contracts. Checked the Neoxider module/reference catalog and extension catalog for relevant existing helpers before adding narrow editor glue. No parallel gameplay system, runtime helper, external dependency, or NoCode runtime logic was introduced.

## Findings and completed fixes

These defects are demonstrated by source control flow and field coverage. Unity reproduction was not executed in this worker.

### 1. High: scene context overrides could not be authored

**Trigger:** use a condition/action with `Override1..5` on `StateMachineBehaviourBase` and select its normal custom inspector. `DrawReferencesSection` previously drew only `stateMachineData`; `contextOverrides` was excluded from every custom section, while the custom inspector replaces default property drawing.

**Consequence:** the documented scene object mapping could not be configured through the normal inspector; slot-based conditions/actions had no supplied scene target.

**Fix/evidence:** `StateMachineBehaviourBaseEditor.cs:51` now draws the existing serialized array with an explicit index-to-slot tooltip. No runtime fields or serialization names changed. Replaced both existing implicit local declarations with explicit types. Actual inspector visibility requires the central visual check; a serialized-field existence assertion alone would not protect this bug, so none was added.

### 2. High: most predicate types had no editable configuration

**Trigger:** add `BoolPredicate`, a numeric/string comparison, `StateDurationPredicate`, or a custom serialized predicate in the transition window.

**Consequence:** the old window exposed only inversion for most types. Only `ConditionEntryPredicate` received special serialized fields. A boolean condition stayed at its default false value, duration remained at its default, and custom configuration was inaccessible.

**Fix/evidence:** `TransitionEditorWindow.cs:200` resolves the selected predicate's real serialized property; `DrawPredicateFields` at line 220 renders that property with all children. The owner is updated before property lookup and disposed after use. Names, inversion, concrete fields and existing custom property drawers share Unity's normal serialization/Undo path. Tests change built-in/custom boolean fields and an integer comparison, then assert actual transition evaluation and JSON round-trip behavior.

### 3. Medium: transition mutations bypassed Undo and stale windows edited detached data

**Trigger:** change header values, add/remove a condition, or keep the window open while its selected transition is removed/reconstructed through another inspector or Undo.

**Consequence:** direct setters/list mutations only dirtied the asset, with no Undo snapshot. A detached `StateTransition` reference could then be edited while the asset was marked dirty, without changing the asset's live transition.

**Fix/evidence:** `SetTransitionDetails` (`TransitionEditorWindow.cs:241`) records Undo before changing all three values; add/remove commands record the owning asset before list changes. `TryGetCurrentTransition` at line 55 verifies membership and refreshes the index by reference, so ordinary row reordering still follows the selected transition. Undo callbacks invalidate a selection whose original object no longer belongs to the asset; there is no fallback to a potentially different row. `OnDisable` unsubscribes the callback. Closed-window commands and destroyed-owner sessions are rejected; deliberately ownerless sessions keep their legacy in-memory behavior.

**Regression evidence:** added Undo/Redo assertions for condition addition, Undo assertions for header edits and removal, JSON persistence checks, reorder targeting, stale replacement rejection, destroyed-owner rejection, and late-menu rejection after window destruction. A reconstructed selection must be reopened from the inspector; this explicit limitation is documented.

### 4. Medium: removing a condition broke IMGUI scope balance

**Trigger:** click a predicate's Remove button. The old loop called `break` before its matching `EditorGUILayout.EndVertical`.

**Consequence:** the open predicate group remained on the layout stack and later section/scroll-view endings consumed the wrong groups, producing layout errors and unreliable remaining controls.

**Fix/evidence:** removal is deferred until every row has closed its GUI group (`TransitionEditorWindow.cs:164`). `RemovePredicateAt` removes the requested index, including a null managed-reference entry. Its regression asserts the retained condition's real evaluation, the removed null entry, and Undo restoration. Visual/layout event execution remains centrally pending.

### 5. Medium: failed action construction left an inserted array element

**Trigger:** a discovered custom `StateAction` has a public parameterless constructor which throws. The old `AddAction` inserted/duplicated the array element before calling `Activator.CreateInstance`.

**Consequence:** the exception left a pending inserted element in the inspector's serialized object; a later apply could persist a duplicate or null action. Type menus also offered non-serializable subclasses which cannot survive managed-reference serialization.

**Fix/evidence:** `StateDataEditor.cs:151` reacquires a property by its saved path, validates the live editor/target and serializable concrete type, and constructs the action before inserting anything. Constructor failures log an editor error and leave the list untouched. Both action and predicate menus now reject non-serializable/open-generic types; predicate construction failures are also reported without mutation. Menu callbacks retain a property path instead of a potentially stale `SerializedProperty` handle.

**Regression evidence:** constructor failure preserves the original count/content; unsupported action types are neither inserted nor discovered. Three lifecycle-list cases assert independent action instances, preservation of existing configuration and actual Undo behavior.

### 6. Medium: multi-selection validated and opened conditions for only the first asset

**Trigger:** select a valid machine followed by an invalid machine, or click Edit Conditions while multiple machines are selected. The inspector advertises `CanEditMultipleObjects`, but direct operations previously used only its first `data` target.

**Consequence:** validation could report success for an invalid selection, and condition editing silently affected only the first asset. Manual validation also ran before applying current serialized edits.

**Fix/evidence:** `StateMachineEditor.cs:69` validates every selected configuration without short-circuiting. The manual button applies pending edits first. The condition button is disabled for multi-selection (`StateMachineEditor.cs:232`) with a one-asset selection explanation. Common serialized field editing remains available. The regression asserts failure with a second invalid asset and success after both assets become valid.

### 7. Low: registration failure leaked its temporary editor

**Trigger:** registration validation creates a non-null editor of an unexpected type, or validation logging throws.

**Consequence:** the old success-only cleanup skipped destruction of that editor; exceptions could also skip the temporary configuration's destruction.

**Fix/evidence:** `StateMachineEditorRegistrar.cs:59` now destroys both temporary objects in `finally`, including the wrong-editor branch. Per-predicate temporary `SerializedObject` handles are now scoped with `using` as part of finding 2. No meaningful isolated wrong-registration test was added because it would require replacing shared editor registration; static cleanup-path inspection was performed.

## Documentation and tests

- Added the owned narrow guide [Editor.md](../../../Assets/Neoxider/Docs/StateMachine/Editor.md): scene context mapping, action/predicate settings, Play Mode controls, multi-selection, Undo/reopen behavior, extension requirements, and an editor C# `ShowWindow` example. The module overview/setup guide are linked rather than duplicated or edited outside ownership.
- Added [Audit20261023NeoStateMachineEditorTests.cs](../../../Assets/Neoxider/Tests/Edit/StateMachine/Audit20261023NeoStateMachineEditorTests.cs): **15 parameterized/ordinary cases**, asserting serialized content, evaluation output, selection isolation, and Undo state. Tests use reflection for editor command entry points because the shared test asmdef lacks a `Neo.StateMachine.Editor` reference. No shared assembly definition was changed.
- Added unique `.meta` files for both new package files. No folder metadata or shared configuration was changed.

## Substantial improvement proposal — persistent transition identities

**Proposed; not implemented.**

**Problem:** transitions are inline serialized classes without stable identity. Undo can reconstruct them, and indexes change during reordering/removal. This audit safely refuses to continue editing a detached object, but users must reopen the condition window after reconstruction/domain reload. A future graph editor or persistent selection cannot reliably reconnect by name/index because names and endpoints may be duplicated.

**Architecture/public API:** add an optional serialized identity to `StateTransition`, exposed as read-only `string Id`, unique within its owning `StateMachineData`. Add an editor-side lookup/selection model storing the owning asset reference and transition ID; resolve the live transition after every Undo/Redo, reload, or structural change. Preserve `ShowWindow(StateTransition, StateMachineData)` and add an overload such as `ShowWindow(StateMachineData owner, string transitionId)` for persistent tools. Keep ID generation/migration in an explicit Undoable authoring operation; runtime evaluation never generates IDs or dirties assets. A reusable editor selection model could later serve a graph view and list view without moving game rules into either view.

**Compatibility/migration:** retain every existing serialized field and API. Old assets deserialize with an empty ID and continue running. An explicit upgrade/repair command assigns IDs to missing/duplicate entries, reports counts, and records Undo. Transition duplication receives a new ID; asset duplication can preserve IDs because uniqueness is scoped to each asset. Assets without IDs retain the current safe selection invalidation until upgraded. This requires coordination with the `Neo.StateMachine` owner; no cross-boundary runtime change was made here.

**Acceptance scenarios:** open the second of two identically named transitions, reorder it, undo/redo, and edit only that transition; restore selection after domain reload; delete the selected transition and disable its window rather than selecting its neighbor; undo deletion and reconnect by ID; migrate legacy assets without changing endpoints, predicates, priorities, actions, or runtime transition results; undo the migration; repair deliberately duplicated IDs deterministically in editor tests.

**Effort/risk:** approximately 1–2 developer days plus central Unity verification. Medium risk is limited to serialized migration/duplication semantics and editor lifecycle ordering. Runtime evaluation behavior and player assembly dependencies should remain unchanged. Dedicated identity/selection regressions and a fixture containing legacy serialized assets are required before release.

## Actual validation and remaining work

Executed successfully:

1. A read-only Python inventory check matched the exact manifest's five C# files to the owned assembly root and checked all five public declaration sets against `git show HEAD:<file>`; no public declarations changed.
2. Strict UTF-8 decoding, replacement-character scans and trailing-whitespace checks passed for all **nine** edited/new package files, including metadata. C# lexical delimiter checks passed for five sources and the new regression file. These are static consistency checks, not compilation.
3. Explicit-type and deprecated identity API scans found no `var`, `GetInstanceID`, or `objectReferenceInstanceIDValue` in those six C# files. No version-specific branches or new assembly references were added.
4. Metadata GUID matching across `Assets` found exactly the two new metadata files, with distinct GUIDs. Both local documentation links resolve.
5. `git diff --check -- <the five owned source files, owned guide/metadata, new regression/metadata, this report>` exited **0**. The separate whitespace scan includes new untracked package files, which ordinary `git diff --check` does not inspect.
6. Scoped git status showed only the five owned source edits and new owned guide/tests/metadata/report in this worker's path set. No commits, installs, services, Unity/MCP, build/test runners or child agents were used.

**Cross-boundary reconciliation:** initial read-only inspection found that `StateMachineData.Validate` could warn about a missing initial state without returning false. Before handoff, the runtime owner's concurrent source now sets `isValid = false` for that case and returns the combined result. This worker did not modify runtime source and does not claim that fix. The editor regression intentionally uses the stable empty-states invalid case.

**Coordinator actions:** link the new narrow `Editor.md` topic from the module entry; compile on supported Unity branches; run `Audit20261023NeoStateMachineEditorTests` in Edit Mode; inspect actual context array visibility, built-in/custom/Condition Entry settings, balanced Remove events, multi-selection controls, Undo and saved-asset reopening. No asmdef reference addition is required by these reflection-based tests.

**Limits:** none of the 15 cases has been executed here. Compilation, Unity serialization/Undo behavior, actual IMGUI layout, and visual workflow correctness remain unverified. Persistent selection after transition reconstruction/domain reload remains a documented future improvement. No independent QA was performed under the explicit no-child-agent constraint.

Кратко: все пять исходников просмотрены; исправлены потеря настроек в инспекторе, Undo/владение переходами, сбой удаления, небезопасная вставка actions и multi-edit. Unity-проверки переданы координатору, тесты не запускались.
