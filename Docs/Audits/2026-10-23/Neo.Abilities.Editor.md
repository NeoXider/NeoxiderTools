# Neo.Abilities.Editor: audit

Status: source audit and focused fixes complete; compilation, EditMode execution and interactive editor QA are pending with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Abilities/Editor/Neo.Abilities.Editor.asmdef`.
- Scope: every one of the 4 inventoried C# files reviewed in full (1,469 source lines at the assigned baseline); nested assemblies excluded. No nested asmdef exists under this editor root.
- Constraints honored: no commits, shared configuration/asmdef edits, installs, Unity/MCP operations, builds, test runners or child agents. Verification below is local static review, not independent QA.

## Scope

| Owned file | Full review coverage and outcome |
| --- | --- |
| [AbilityDefinitionEditor.cs](../../../Assets/Neoxider/Scripts/Abilities/Editor/AbilityDefinitionEditor.cs) | CustomEditor registration, target selection, optional stylesheet, default property binding, header lifecycle and shortcut into the window. Unity owns this inspector's SerializedObject. No separate behavioral defect found here; replaced inferred locals with explicit types as required by AGENTS.md. |
| [AbilityDesignerUI.cs](../../../Assets/Neoxider/Scripts/Abilities/Editor/AbilityDesignerUI.cs) | Stylesheet lookup/cache, chip/initial helpers, gradient mesh boundaries and event ownership, every built-in/custom operation summary, tracked inspector header, default serialized fields, all ability/modifier validation paths, ids, null entries, references and leveled radii. Fixed the chain hit wording and radius validation. |
| [AbilityDesignerWindow.cs](../../../Assets/Neoxider/Scripts/Abilities/Editor/AbilityDesignerWindow.cs) | Both public Open overloads, menu registration, enable/disable/focus/Undo lifecycle, asset creation/cancel, full library discovery/filter/sort/selection, binding and SerializedObject ownership, effect insertion/move/removal, card editing, every modifier board section, full Inspector and status navigation. Fixed initialization, stale views, destructive inline rebuilding, null handling, incomplete node reads and wrapper release. |
| [ModifierDefinitionEditor.cs](../../../Assets/Neoxider/Scripts/Abilities/Editor/ModifierDefinitionEditor.cs) | Same inspector lifecycle/binding and shortcut checks for modifiers, including buff/debuff header inputs. No independent behavioral defect found; explicit local types restored. |

Read-only dependency context: `AbilityDefinition`, `ModifierDefinition`, `AbilityLibrary`, `AbilityBlueprint`, `ModifierBlueprint`, `EffectNodeData`, leveled-value enums/resolver, property contributions/aggregator, relevant effect/chain and modifier-engine call sites, existing test support, test asmdef and existing Ability Designer documentation. Runtime code, existing tests and other owners' docs were not edited.

Cross-cutting results: the assembly is Editor-only and continues to reference only `Neo.Abilities`. No public method declaration, serialized field, existing asset GUID, scene, prefab or sample path was changed. No runtime logging or new scene search was introduced. There are no direct obsolete instance-id calls or version conditionals in this assembly. Both Unity version families still require central compilation.

## Findings and fixes

### F1 — P2: adding an effect inherited unrelated serialized data

- Trigger: add a node after an existing effect with a nondefault target, amount, scaling array, modifier/resource/archetype id or custom parameter.
- Evidence: `AbilityDesignerWindow.AddNode` inserted an array element and initialized only `OpId` and `Chance`. Unity does not guarantee default contents on insertion; copied previous fields therefore survived into the new operation.
- Observable consequence: a newly added heal/damage/motion/etc. node could immediately use another effect's target, magnitude or references, and that unintended configuration could be saved into the asset.
- Fix: assign a complete `new EffectNodeData { OpId = opId }` through the inserted element's `boxedValue`, retaining the serialized Undo path and the domain class's defaults, including future fields.
- Regression: `AddEffect_UsesFreshDefaults_AndSupportsUndoRedo`, for both Cast and Impact. Asserts previous-node preservation, fresh scalar/reference/scaling defaults, and real list contents after Undo/Redo.

### F2 — P2: the right Inspector did not refresh dependent views

- Trigger: change an id, cooldown or node in the window's right-hand Inspector.
- Evidence: only card foldouts subscribed to `SerializedPropertyChangeEvent`; the Inspector pane had no change handler. `RefreshAfterEdit` also omitted library rebuilding.
- Observable consequence: the center board and diagnostics retained old values, and library ids/warning chips remained stale until focus or Undo caused a later refresh.
- Fix: register a change handler on the Inspector pane and share validation/library/status refresh logic across edit paths.
- Regression: `InspectorEdit_RefreshesBoardLibraryAndValidation` changes serialized data, dispatches the real property-change event and asserts updated amount, row id, warning marker and negative-cooldown diagnostic.

### F3 — P2: inline editing replaced the controls being edited

- Trigger: change any property inside a card's Edit foldout.
- Evidence: the foldout called `RefreshAfterEdit`, which cleared and rebuilt the entire center pane, creating every foldout again with `value = false`.
- Observable consequence: editing collapsed/replaced the active card and removed its focused controls, interrupting continued typing.
- Fix: update the existing card's operation label, operation color class and summary in place; refresh validation/library/status without rebuilding the center. Cache the old color class separately from the mutable node value.
- Regression: `InlineEdit_RefreshesSummaryWithoutReplacingTheActiveFoldout` asserts the same expanded foldout instance, changed heal summary/op/color and updated chance warning. Native keyboard focus still needs interactive verification.

### F4 — P2: malformed modifiers could prevent the editor from showing the selected asset

- Trigger: a scripted/malformed modifier has a null blueprint, or its tick list contains a null node followed by valid entries.
- Evidence: `BuildModifierBoard` dereferenced `blueprint.Properties` and `node.OpId` unconditionally, although validation and runtime effect execution already tolerate/reject the corresponding null inputs.
- Observable consequence: a NullReferenceException interrupts board rebuilding before the right Inspector can finish switching to that asset.
- Fix: display a missing-blueprint HelpBox and skip null tick entries while retaining valid summaries.
- Regressions: `ModifierBoard_MissingBlueprintShowsADiagnostic` asserts visible board/status diagnostics; `ModifierBoard_SkipsNullTickNodesAndKeepsValidSummaries` asserts the surviving heal card's content and count. These are state/output assertions, not only no-throw checks.

### F5 — P3: chain cards displayed the wrong total hit limit

- Trigger: an ability effect uses `chain` with a nondefault `MaxTargets`.
- Evidence: `ReadNode` reconstructed only selected scalar fields and omitted `MaxTargets`, so `NodeSummary` always saw zero and displayed the four-hit fallback. The label also called total hits "hops", despite the runtime including the initial target in its limit.
- Observable consequence: a configured two-hit chain was shown as four hops, misrepresenting both the number and meaning of the setting.
- Fix: read the complete serialized node via `boxedValue`; label the limit as hits.
- Regression: `ChainCard_DisplaysTheAuthoredTargetLimit` asserts two displayed hits and unchanged authored `MaxTargets`.

### F6 — P2: radius validation ignored overriding levels

- Trigger: an area/chain node has `Radius = 6` and `RadiusByLevel = [0, 3]`, or a later zero level alongside a positive level.
- Evidence: `HasPositiveRadius` accepted a positive flat radius before considering the array, or any one positive array entry. `LeveledValueResolver.SampleByLevel` uses the array whenever it is nonempty; the positive flat radius does not rescue those zero levels.
- Observable consequence: the status could report valid data even though the area/chain had no usable radius at an authored level.
- Fix: require every entry of a nonempty overriding array to be positive; use the flat value only when the array is absent/empty. Diagnostics explain the override rule. NaN also fails the positive comparison.
- Regressions: six `RadiusValidation_UsesEveryOverridingLevel` cases cover area/chain, zero at either level despite positive fallback, and wholly positive arrays with zero fallback. The assertions inspect emitted diagnostics and compare the first level to the actual runtime sampler.

### F7 — P3: window-owned SerializedObjects were abandoned without deterministic release

- Trigger: repeated selection changes, selection replacement after refresh, GUI recreation or window disable.
- Evidence: the window repeatedly assigned new SerializedObjects and never disposed the old wrapper; `OnDisable` only unsubscribed Undo. Cleanup therefore depended on GC/native finalization rather than the window's ownership lifecycle.
- Fix: unbind before releasing the owned wrapper, dispose it on target replacement/selection loss/disable, and unbind old GUI before rebuilding it. Unity-owned SerializedObjects in the two custom inspectors remain Unity-owned.
- Validation: static ownership/control-flow inspection; `SelectionChange_CommandsEditOnlyTheNewTarget` protects correct editing after replacement and reselection. A native allocation/leak measurement was not performed, and this test does not claim to measure one.

### Reuse and compatibility receipt

- Existing package editor primitives, default node construction, SerializedObject/PropertyField binding and runtime `LeveledValueResolver` were reused. Relevant Neoxider skill references (`abilities.md`, editor/module entries in `modules.md`, and the helper catalog in `extensions.md`) were checked; none provides a competing ability-editor serialization command. No new gameplay subsystem or general-purpose utility was introduced.
- Unity's [array insertion contract](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SerializedProperty.InsertArrayElementAtIndex.html) requires explicitly initializing inserted contents. Its [boxedValue API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SerializedProperty-boxedValue.html) supports by-value objects and individual list elements; `EffectNodeData` has no fixed buffers. This removes the incomplete manual field projection rather than extending it field by field.
- Existing public Open/inspector/CreateGUI entry points and the no-argument private refresh command were preserved. All owned C# locals now use explicit types. There is no conditional branch added for a Unity version.

## Regressions added

New file: [Audit20261023NeoAbilitiesEditorTests.cs](../../../Assets/Neoxider/Tests/Edit/Abilities/Audit20261023NeoAbilitiesEditorTests.cs), with its own unique GUID metadata. Eight test methods define 14 cases. Existing tests/asmdefs were not modified.

The fixture owns temporary in-memory definitions and a new window instance; it does not create persistent assets or reuse/close a user's existing designer window. Teardown clears Undo records only for its own definitions and closes its own window. A narrow reflection bridge loads the Editor-only window, selects test definitions, isolates its per-window library, and invokes existing UI commands. Assertions observe blueprint data, Undo results, actual UI labels/markers and control identity. This avoids adding an assembly reference to the shared test asmdef. No tests were executed in this worker.

## Documentation

Added the exact owned topic [Assets/Neoxider/Docs/Abilities/Editor.md](../../../Assets/Neoxider/Docs/Abilities/Editor.md), which did not exist when the audit began, plus unique `.meta`. It covers the public editor C# API, asset/scene/NoCode workflow, fresh-node defaults, Undo/Redo, inline/right-Inspector updates, chain hit semantics, null behavior, level-radius precedence and the limits of project-wide validation. The main text is English, with a concise Russian explanation. It links back to the existing Ability Designer layout guide and runtime entry point.

Ownership follow-up: the existing `Assets/Neoxider/Docs/Abilities/AbilityDesignerWindow.md` is outside this entry's `docs` allowlist. Its Validation bullet still describes the old permissive radius test (a positive flat radius or any positive level). The coordinator/module-doc owner should replace that bullet with the every-authored-level/array-override rule and link `Editor.md` from the canonical module/Designer entry. No root README or foreign guide was edited here.

## Substantial improvement proposal

### P1 — context-aware, typed authoring diagnostics shared with catalog checks (proposal only)

**Problem.** The current internal scanner searches all project assets, returns only asset/message pairs and duplicates selected runtime rules. A modifier can exist somewhere in the project yet be absent from the library actually registered by a scene. Conversely, unrelated catalogs can intentionally reuse ids and trigger misleading project-wide duplicate warnings. Custom operations, named specials, resource declarations and archetype bindings cannot participate in validation. Diagnostics cannot select the exact offending field, and every edit repeats a whole-project validation pass.

**Proposed architecture/public API.** Introduce a pure C# `AbilityDefinitionValidator.Validate(AbilityValidationContext)` contract that consumes explicit blueprint catalogs, their registration order and optional declared operation/resource/archetype ids. Return immutable `AbilityDiagnostic` records with stable `Code`, `Severity`, definition kind/id, `PropertyPath` and message. Reuse the existing level resolver and registry rules rather than inventing a second runtime simulation. Keep UnityEngine.Object/scene references out of the core diagnostic model. An Editor adapter maps definition keys to assets, offers Project and selected-AbilityLibrary contexts, highlights the property when an issue is clicked, and maintains a reverse index for modifier/special references so an edit revalidates its dependents. Optional scene binding checks receive a snapshot from scene adapters; ScriptableObject definitions never hold scene object references.

Example contract shape (not implemented):

```csharp
IReadOnlyList<AbilityDiagnostic> Validate(AbilityValidationContext context);
// AbilityDiagnostic: Code, Severity, DefinitionKind, DefinitionId, PropertyPath, Message.
// Context: ordered ability/modifier catalogs and optional declared dependency ids.
```

**Compatibility/migration.** Add the validator in cooperation with the `Neo.Abilities` owner; keep `AbilityDesignerWindow.Open` and all serialized blueprints unchanged. Preserve Project scope as the initial default. Library scope is opt-in, with explicit override/last-registration policy and advisory diagnostics where duplicate registration is intentional. Adapt existing messages/status rows onto diagnostic records before adding more rules. Runtime registration and existing assets keep working without a validator; no automatic asset rewrite or new required scene component.

**Acceptance scenarios.**

1. A modifier found elsewhere in the project but absent from the selected library produces an unresolved-reference diagnostic in Library scope, with the exact node path.
2. Separate libraries may reuse ids without a Library-scope collision; a collision/override within the chosen ordered catalog is reported according to its configured policy.
3. Mixed valid/zero radii identify the exact overriding level; custom registered operation ids are accepted without executing effect code.
4. Named-special typos and missing declared archetypes are reported, while open resource/state/property registries remain usable without mandatory declarations.
5. Headless validation and the editor adapter produce the same sorted diagnostic codes/paths for the same input, without scene searches. Clicking a row reveals the correct field while preserving active text editing elsewhere.
6. Editing one definition revalidates that definition and indexed dependents, not every asset; Undo/Redo and asset deletion invalidate the same index correctly.

**Effort/risk.** Estimate 3–5 developer days plus focused integration/QA with the runtime owner. Main risks are matching registration/override semantics, stale dependency indexes, incomplete plugin declarations and accidentally presenting advisory checks as runtime guarantees. Start with an uncached deterministic validator and equivalence tests, then add Editor indexing. This proposal was not implemented as part of the bug fixes.

## Verification and remaining risks

Actual local checks:

- Read the complete root AGENTS.md, worker contract, exact manifest entry and preexisting own report; reconciled the recursive editor inventory against all four manifest paths. No nested assembly was present.
- Inspected every inventoried file completely, not only changed hunks; reviewed the new regression fixture and scoped source diff.
- Ran Python static checks with strict UTF-8 decoding, replacement/control-character rejection, C# lexical delimiter balancing, explicit-local/obsolete-id scans and preserved-public-method-declaration comparison against Git HEAD. These checks do not compile C#.
- Statically discovered eight regression methods / 14 cases, checked their late-bound command names against the window, and confirmed the existing test assembly already references `Neo.Abilities`; no shared reference change is needed.
- Resolved all eight distinct local documentation links; checked both new `.meta` GUID formats and verified each GUID occurs exactly once under `Assets/Neoxider`.
- Ran `git diff --check --` with the explicit owned source/docs/tests/report paths: exit **0**. A separate trailing-whitespace/newline/UTF-8 check also passed for all **9 owned files**, including newly created untracked files.

Final static receipt: **5 C# files** passed lexical delimiter/type/obsolete-id checks; **4 owned source files** retained their public method declarations (comparison normalizes whitespace/line endings); **14 regression cases** and their command names were checked statically, not executed; **8 documentation links + 6 report links** resolved; **2 new metadata GUIDs** were individually verified unique. No static check remains failing.

Central validation still required: compile on Unity 6.0–6.4 and 6.5+, run `Audit20261023NeoAbilitiesEditorTests` in EditMode, and smoke-test ordinary bound Inspector events, continuous inline typing, Undo/Redo, asset creation/cancel, window close/reopen and malformed scripted definitions. The regression fixture dispatches real property-change events after serialized edits, but this worker did not exercise Unity's native event emission, panel update timing or keyboard focus. There is no screenshot, native-memory profile, compiled version branch or executed-test pass claimed here.

Cross-boundary actions: update/link the existing Ability Designer guide as described above; evaluate the typed validator with the runtime owner only as a future proposal. No new cross-assembly code/config dependency is required by the completed fixes.

**По-русски:** все четыре исходника просмотрены полностью. Исправлены реальные ошибки редактора и добавлены регрессии с проверкой данных/вывода. Выполнены только статические проверки; запуск Unity, компиляция и EditMode-тесты остаются у координатора.
