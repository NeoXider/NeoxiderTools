# Neo.Editor: source audit and fixes

Status: audit phase complete; Unity compilation and test execution remain centrally pending.

- Contract baseline: `ca198556`, package `10.19.0`.
- Assigned model/session: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Editor/Neo.Editor.asmdef`.
- Coverage: **75/75 manifest C# files reviewed**, 19,284 baseline source lines. Nested assemblies excluded.
- Changes: **15 owned source files, 9 existing public doc pages, 5 new regression files (28 NUnit cases), 5 unique test metadata files**, and this report.
- No commits, child agents, installs, Unity/MCP, builds, test runners, scene/prefab/sample changes, package/asmdef/settings edits, or other workers' files.
- Work resumed from the same audit phase after the watchdog interruption; completed source review and edits were retained. The manifest's separate plan path was not edited.

Проверено всё содержимое инвентаря Neo.Editor. Исправления сохраняют публичные API и сериализованные поля; запуск Unity и тестов оставлен координатору. Статические проверки не означают успешную компиляцию.

## Review scope

The review covered public/editor/NoCode paths, serialization and Undo, null/boundary inputs, callbacks and cancellation, asset/scene/native-resource ownership, dependencies, optional Mirror paths, logging and Unity version compatibility. Existing dependencies and tests were read as context only. `neoxider-tools` and `unity-game-agent` reuse rules were applied locally; user restrictions prohibit delegated/Unity QA. No independent QA was performed.

Reuse receipt: existing `ComponentDrawer` collection assignment is shared by `ResourceDrawer`; existing repaint/inspector infrastructure, `InternalsVisibleTo("Neo.Editor.Tests")`, Unity preview/additive scene APIs, prefab contents APIs and BCL typed arrays/lists/compression are reused. The skills' extensions/modules references were searched before introducing local glue; they contain no replacement for these Editor asset/serialization operations. No new runtime framework was introduced.

All paths in this table are relative to `Assets/Neoxider/Editor/`. “No edit” records review, not an execution claim.

| File | Review evidence / disposition |
|---|---|
| `AutoBuildName.cs` | Android artifact extension, path normalization, collision suffixes; no edit. |
| `Binding/ComponentBindingInspectorShared.cs` | Type/member enumeration and binding defaults; generic-method filter fixed (F15). |
| `Condition/ConditionEntryDrawer.cs` | Default SerializedProperty forwarding and height contract; no edit. |
| `Condition/NeoConditionEditor.cs` | Condition authoring, both sources, runtime previews, resets; narrow numeric conversion fixed (F14). |
| `Core/LevelCurveDefinitionEditor.cs` | Formula/custom/curve authoring and bounded preview; no edit. |
| `Create/CreateMenuObject.cs` | Creation discovery, parent/stage selection, Canvas/EventSystem fallback, Undo; no edit. |
| `Create/LegacyUiCreateMenuObject.cs` | Legacy API forwarding and missing-prefab behavior; no edit. |
| `Create/NeoxiderPresetCreateMenu.cs` | Preset lookup, multi-selection and parent handling; no edit. |
| `Create/SingletonCreator.cs` | Template generation and modal completion; cancellation/identifier/focus fixed (F11). |
| `FindAndRemoveMissingScriptsWindow.cs` | GUI lifetime and OnDisable forwarding; no edit. |
| `GUI/EditorWindowGUI.cs` | EditorWindowGUI abstraction and namespace; documentation example corrected. |
| `GUI/FindAndRemoveMissingScriptsWindowGUI.cs` | Full scan, scene ownership, restart/cancel, selection and asset repair; fixed F02-F04. |
| `GUI/NeoxiderSettingsWindowGUI.cs` | Folder and hierarchy settings UI; balanced removal row fixed (F12). |
| `GUI/SceneSaverGUI.cs` | Manual clone save, dirty checks and scheduler handoff; backup identity follow-up C3. |
| `GUI/TextureMaxSizeChangerGUI.cs` | Texture filtering, bulk reimport and progress/error cleanup; no edit. |
| `Main/CreateSceneHierarchy.cs` | Container lookup, active-scene sorting, prefab stage and Undo; no edit. |
| `Main/NeoxiderSettings.cs` | Settings load/save/defaults, path helpers and ScriptableObject lifetime; no edit. |
| `Main/NeoxiderSettingsWindow.cs` | Window initialization, validation and GUI disposal; no edit. |
| `Neo.Editor.InternalsVisibleTo.cs` | Existing Neo.Editor.Tests friend access verified; no edit. |
| `NeoxiderEditorAssets.cs` | Asset lookup and logo fallback; no edit. |
| `Network/LobbyDemoSceneCreator.cs` | Mirror-gated demo construction and event wiring; coordinated follow-up C2. |
| `Network/NeoMirrorScenePostProcess.cs` | Mirror postprocess filtering and optional-networked reactivation; no edit. |
| `Network/NetworkContextActionRelayEditor.cs` | Relay dropdowns compared with runtime numeric argument binding; fixed F16. |
| `NoCode/ComponentFloatBindingDrawer.cs` | Float-binding source, member defaults, property height and reactive hint; limitation C1. |
| `PackageHealthCheck.cs` | Version/doc/source checks and package-root assumptions; no edit. |
| `Progression/LevelCurveDefinitionEditor.cs` | Level validation, serialized authoring and summary; no edit. |
| `Progression/PerkTreeDefinitionEditor.cs` | Perk validation, serialized authoring and summary; no edit. |
| `Progression/ProgressionEditorHelpers.cs` | Null/empty validation message handling; no edit. |
| `Progression/UnlockTreeDefinitionEditor.cs` | Unlock validation, serialized authoring and summary; no edit. |
| `PropertyAttribute/ComponentDrawer.cs` | Lookup attributes, fake null and collection assignment; typed collections fixed (F01). |
| `PropertyAttribute/CustomEditorBase.cs` | Inspector dispatch, shared repaint leases, caches and package lookup; no edit. |
| `PropertyAttribute/CustomEditorSettings.cs` | Inspector settings facade and persistence calls; no edit. |
| `PropertyAttribute/GradientButtonSettings.cs` | Gradient schemes and sizing constants; no edit. |
| `PropertyAttribute/Internal/CustomEditorBase.Actions.cs` | Button metadata, defaults, static invocation and overload storage; fixed F07. |
| `PropertyAttribute/Internal/CustomEditorBase.Docs.cs` | Doc preview, fallback viewer and scroll state; no edit. |
| `PropertyAttribute/Internal/CustomEditorBase.Events.cs` | UnityEvent discovery, filtering, counts and listener previews; no edit. |
| `PropertyAttribute/Internal/CustomEditorBase.Mascot.cs` | Mascot assets, Game view discovery and health display; no edit. |
| `PropertyAttribute/Internal/CustomEditorBase.Properties.cs` | Property traversal, foldouts, header suppression and event metadata; fixed F06. |
| `PropertyAttribute/Internal/CustomEditorBase.Visuals.cs` | Gradient input handling and disabled scope; fixed F05. |
| `PropertyAttribute/Internal/NeoComponentHealth.cs` | Bounded health scans, console attribution, throttling and centralized version branches; no edit. |
| `PropertyAttribute/NeoCustomEditor.cs` | Fallback/exact-type registrations and assignment dispatch; no edit. |
| `PropertyAttribute/NeoCustomEditorRegistrar.cs` | Editor callbacks and repaint registration; no edit. |
| `PropertyAttribute/NeoDocHelper.cs` | Doc lookup/cache invalidation, rich preview and optional renderer fallback; no edit. |
| `PropertyAttribute/NeoEditorAsmdefFixer.cs` | Asmdef selection, name/GUID references, test/editor cycles and JSON insertion; fixed F08. |
| `PropertyAttribute/NeoEditorAutoRegister.cs` | Domain-load assembly/type enumeration; no edit. |
| `PropertyAttribute/NeoInspectorAnimation.cs` | Animation policy and FPS bounds; no edit. |
| `PropertyAttribute/NeoInspectorSettings.cs` | Serialized settings, migration and project persistence; no edit. |
| `PropertyAttribute/NeoInspectorStyles.cs` | Skin-aware styles, cached labels and widths; no edit. |
| `PropertyAttribute/NeoInspectorTheme.cs` | Gradient texture ownership, draw-only guards and shared colors; lifecycle proposal P2. |
| `PropertyAttribute/NeoUpdateChecker.cs` | Update requests, fallback tags, throttling and version parsing; lifecycle proposal P2. |
| `PropertyAttribute/NeoxiderEditorGUI.cs` | Shared cards, toggles, sections, GUI state and layout; no edit. |
| `PropertyAttribute/NeoxiderModuleInspectorHeader.cs` | Package banner and cached update state; no edit. |
| `PropertyAttribute/NeoxiderModulePackageInfo.cs` | UPM/Assets package discovery, negative cache and invalidation; no edit. |
| `PropertyAttribute/NeoxiderSettingsWindow.cs` | Visual settings UI and inspector repaint; no edit. |
| `PropertyAttribute/NeoxiderUpdateChecker.cs` | Per-module update requests, tag prefixes and session state; lifecycle proposal P2. |
| `PropertyAttribute/ResourceDrawer.cs` | Resources paths, fake null and collection assignment; shared typed setter fixed (F01). |
| `Quest/QuestConfigEditor.cs` | Quest identity, ID generation, serialized lists and validation; no edit. |
| `Quest/QuestFlowConfigEditor.cs` | Quest chains, standalone references and duplicate detection; no edit. |
| `Rpg/Data/RpgStatGrowthDefinitionEditor.cs` | Growth authoring and bounded runtime-value preview; no edit. |
| `Rpg/Data/RpgStatGrowthRuleDrawer.cs` | Growth mode/formula controls and reserved height; fixed F13. |
| `Rpg/RpgCharacterEditor.cs` | Character authoring, runtime summary, events and networking fields; no edit. |
| `Rpg/RpgStatIdDrawer.cs` | Stat preset/custom ID field and height; no edit. |
| `SaveProjectZip.cs` | Recursive archive traversal, output exclusion and failure preservation; fixed F09. |
| `Scene/SceneSaver.cs` | Auto-save callbacks, delayed execution, guards and subscription lifecycle; no edit. |
| `Scene/SceneSaverAutoSaveScheduler.cs` | Dirty epochs, revisions, timing and handled markers; no edit. |
| `Scene/SceneSaverCheckContext.cs` | Immutable scheduler inputs and revision contract; no edit. |
| `Scene/SceneSaverSettings.cs` | EditorPrefs load, clamps, defaults and reset; no edit. |
| `TextureMaxSizeChanger.cs` | Texture window initialization and GUI delegation; no edit. |
| `Tools/Dialogue/DialogueEditorWindow.cs` | UI Toolkit target binding, list mutations, reset and selection; no edit. |
| `Tools/Inventory/InventoryItemDataEditor.cs` | Item serialized fields, icon generation and persistence; no edit. |
| `Tools/Inventory/PrefabToSpriteUtility.cs` | Readback, render-target restoration and temporary texture cleanup; fixed F10. |
| `Tools/Inventory/PrefabToSpriteWindow.cs` | Preview cache, source ownership and export workflow; no edit. |
| `Tools/Physics/MagneticFieldEditor.cs` | Scene handle conversions, Undo and serialized application; no edit. |
| `Tools/PrefabMissingScriptUtility.cs` | Prefab/root repair traversal, inactive children and saves; service/sample-layout proposal P1. |
| `Tools/SetTextEditor.cs` | Inspector extension hook and Undo component addition; no edit. |

## Demonstrated defects and completed fixes

The evidence below is source-level before/after analysis. Added tests have not been executed in this worker.

| ID / severity | Trigger, source and observable consequence | Completed fix / regression |
|---|---|---|
| F01 High | `ComponentDrawer.SetFieldValueForCollection` assigned `Component[]`/`Object[]` directly to fields such as `BoxCollider[]`. Both drawers constructed `List<object>` for `List<T>`, causing reflection assignment failures on ordinary collection attributes. | Create the declared array element type or `List<T>`; share the setter with Resources lookup and record collection Undo. Three collection tests assert actual assigned components/assets and preservation of existing references. |
| F02 High | `FindAndRemoveMissingScriptsWindowGUI.SearchAll` opened each scene with `OpenSceneMode.Single`, replacing loaded/unsaved work. Its progress step was not reset, and repeated starts could stack callbacks. | Scan closed scenes in preview scenes with `finally` cleanup; inspect loaded scenes in place. Restart removes the prior callback and resets progress; close/cancel stops work and clears progress. Tests assert scene handles/count, dirty unsaved content and preview count, plus restart/cancel behavior. |
| F03 High | The same window removed components but never saved repaired prefabs/scenes. Reopening scenes during bulk removal discarded previous repairs; the result list still reported success. | Repair closed scenes additively, save and close them; load/save/unload prefab contents. Already open scenes stay dirty with Undo, retaining unrelated unsaved changes. Failed results remain; failed scene saves leave the repaired scene open. Tests reopen saved assets and inspect missing-script counts. |
| F04 Medium | Name paths and `Transform.Find` selected the first duplicate root/child and interpreted `/` inside names as hierarchy separators. | Store sibling-index paths and verify the final object name. Tests contain healthy and broken objects with identical slash-containing names and verify the actual repaired assets. Rescan is still required after hierarchy restructuring; see P1. |
| F05 High | `CustomEditorBase.DrawGradientButton` manually handled mouse events without checking `GUI.enabled`, bypassing the disabled scope used for PlayModeOnly actions. | Respect disabled state before capturing/activating input; release a previously captured control when disabled. A real IMGUI window test compares disabled and enabled clicks. |
| F06 Medium | `RunWithHeaderDecoratorsSuppressed` caught exceptions from the drawing callback and called it again, including after `ExitGUIException`. Serialized edits/callbacks could run twice and IMGUI exit was swallowed. | Separate optional decorator setup from callback invocation; draw once and restore decorators in `finally`. Two cases assert exception identity, one invocation and restored property height. |
| F07 Medium | Action state keys used only `method.Name` and parameter name. Calling `Apply(int value=12)` then `Apply(string value="saved")` reused the integer for the string overload and failed invocation. | Key foldouts and parameter state by full method signature. A regression invokes both real overloads and observes both output fields. |
| F08 High | `NeoEditorAsmdefFixer` ignored existing name-based references, could select `Neo.Editor.Tests.asmdef` from a partial search, and added editor/test assemblies back into Neo.Editor. The test assembly already references Neo.Editor, producing a cycle. Empty reference arrays also received a trailing comma. | Select the exact asmdef filename; recognize names and GUIDs; conservatively add unrestricted runtime candidates, excluding editor/tests and test constraints. Preserve unrelated JSON fields and valid empty/non-empty arrays. Two tests inspect emitted JSON and idempotence. **No asmdef was changed or repair command invoked.** |
| F09 High | Choosing a ZIP under Assets included the open output itself, causing a sharing/read failure. The old backup was deleted before compression, so any source read failure lost it. | Exclude destination/staging files; finish beside the target, then replace/move it. Cleanup incomplete output in `finally`. Tests inspect archive entries/content and preservation of a previous backup under a locked-source failure. |
| F10 Medium | `PrefabToSpriteUtility` released render targets/readable copies only on the success path. Directory creation, blit/readback or encoding failures could leak native resources or leave the active render target changed. | Scoped cleanup for temporary RTs/copies, while preserving caller-owned readable textures. A graphics regression forces a directory failure and inspects texture ownership/count and active RT. |
| F11 Medium | `EditorInputDialog.Show` initialized its result to the default name, so Cancel/close could create `NewSingleton.cs`. Digit-leading names generated invalid C# identifiers, and the initial-focus branch was unreachable. | Return no result until Submit, normalize a valid leading character, use one template builder and repair initial focus. Six template cases inspect generated declarations or absent output. Actual modal Cancel/close/focus remains a manual central check. |
| F12 Medium | Removing a folder in `NeoxiderSettingsWindowGUI.DrawFolderSettings` broke out before `EndHorizontal`, leaving GUILayout unbalanced. | Close the row before breaking. Source control-flow check performed; manual GUI check pending to avoid tests modifying shared project settings. |
| F13 Medium | Power growth draws both base and exponent, but `RpgStatGrowthRuleDrawer.GetPropertyHeight` reserved only one extra row. The last control overlapped the following property. | Reserve the additional control row. A height-contract test compares Power with Exponential. Visual check pending. |
| F14 Medium | Condition preview invoked `byte`/`short` methods with a boxed `int`, although runtime `ConditionValueSource` converts to the declared parameter type. Inspector Current Value disappeared for valid bindings. | Apply the same invariant `Convert.ChangeType` conversion before preview invocation. Two regressions observe returned values and the invoked component's recorded argument. |
| F15 Medium | `BuildConditionMemberLists` advertised open generic methods returning a supported type; they cannot be invoked without generic type arguments. | Exclude generic methods. Regression checks actual dropdown candidate output alongside valid byte/short methods. |
| F16 Medium | Relay editor classified `int` and `long` parameters as Float mode, while runtime looks up a method using a float argument type. These integral signatures require narrowing and cannot bind. A double parameter can bind by widening and remains supported. | Offer compatible float/double signatures and exclude integral signatures. A regression checks real candidate methods; runtime Network source remains untouched. |

Public methods and serialized field names are preserved. New test seams are internal and use the existing friend assembly. Overload state is inspector-session state, so no asset migration is needed. ZIP menu names and contents roots remain unchanged. Saved missing-script asset repair is now explicit in the guide; open scene edits are deliberately left for normal Save.

## Regression inventory

All files are new, uniquely prefixed and under `Assets/Neoxider/Tests/Edit/Editor/`; existing tests/asmdefs were not edited.

| File | Cases | Protected behavior |
|---|---:|---|
| `Audit20261023NeoEditorCollectionTests.cs` | 3 | Typed component/resource arrays and lists; preserve non-empty assignments. |
| `Audit20261023NeoEditorMissingScriptsTests.cs` | 5 | Preserve dirty scenes; preview cleanup; durable scene/prefab repair; duplicate names; restart/cancel. |
| `Audit20261023NeoEditorInspectorTests.cs` | 4 | One callback on failure/ExitGUI; decorator restoration; disabled clicks; independent overload defaults. |
| `Audit20261023NeoEditorUtilityTests.cs` | 12 | Safe emitted asmdef references; valid/no-output templates; Power height; archive contents/failure preservation; sprite export ownership. |
| `Audit20261023NeoEditorBindingInspectorTests.cs` | 4 | Numeric preview conversion; no open generics; compatible relay method signatures. |

Tests generate isolated temporary fixture assets/files only when centrally executed. Missing-script tests inject zero-script references into freshly generated temporary YAML; fixture/import behavior must be verified in Unity. IMGUI and graphics cases can be skipped by their explicit environment assumptions in headless runs; a skip is not a pass for interactive behavior.

## Public documentation

Updated nine existing English pages under `Assets/Neoxider/Docs/Editor`:

- `README.md`: canonical entry links; Editor-only C# APIs versus runtime initialization; scene/NoCode workflows; binding signature limits; sprite ownership.
- `ComponentDrawer.md`, `ResourceDrawer.md`: typed arrays/lists, preservation, Undo, explicit Editor invocation and runtime C# equivalents.
- `FindAndRemoveMissingScriptsWindow.md`: preview/additive ownership, repair persistence, open-scene Save/Undo, failure behavior and rescan requirement.
- `CustomEditorBase.md`: disabled action input, overload state, one-shot drawer exceptions; corrected the contradictory “no repaints” statement when animation is off.
- `SingletonCreator.md`: cancellation and normalized class names; typed runtime extension points.
- `SaveProjectZip.md`: output exclusion and retention of the old backup on failure.
- `NeoCustomEditor.md`: conservative name/GUID-aware assembly repair and dependency limits.
- `EditorWindows.md`: corrected the sample namespace from the nonexistent `Neo.Editor.GUI` to `Neo.Editor.Windows`.

No root/package README, shared guide, nested-owner documentation or DocsEn metadata was changed.

## Substantial improvement proposals (not implemented)

### P1: Public asset-maintenance service with durable identity and per-asset transactions

**Problem.** Missing-script scan/repair still lives in a GUI controller; the separate prefab repair utility repeats traversal/persistence logic. Sibling indices solve duplicate names for an unchanged hierarchy but do not identify an object durably after same-named siblings are reordered. Bulk repair loads/saves a closed scene once per result, making a scene with many errors unnecessarily expensive. The prefab utility's default roots also require an explicit policy for developer Samples, release Samples~ and imported UPM samples.

**Architecture / public API.** Add an Editor-only `MissingScriptScanService` yielding `ScanStep` records from `Scan(AssetScanRequest request)` so the caller owns main-thread scheduling and cancellation. Return immutable `MissingScriptFinding` records with asset GUID, stable local object identity, display path and asset revision/hash. Add `BuildRepairPlan(findings)` and `ApplyRepairPlan(plan, OpenSceneSavePolicy.LeaveDirty)` returning a typed `RepairReport` with per-asset outcomes/counts. Group work by asset, load once, resolve identities/recheck revision, remove selected components, save at most once, and close only resources the operation opened. Keep the plain planning/reporting model independently testable; Unity scene/prefab operations sit behind a narrow adapter. GUI and prefab menu utilities call this shared service. Sample roots are request data, not hard-coded assumptions; release Samples~ is inspected as package content or handled after import rather than pretending it is an AssetDatabase folder.

**Compatibility / migration.** Preserve `ShowWindow`, existing menu commands and repair counts as adapters. Keep the current default that already open scenes remain dirty. Do not rename serialized fields or move sample folders. Introduce the public service additively; old private result lists are transient and need no asset migration. Reject stale plans rather than silently repairing a different object.

**Acceptance scenarios.** Multiple dirty/unsaved scenes retain handles, active selection and unrelated edits; duplicate/slash-containing names resolve correctly; reorder/rename/delete after scan produces a stale-plan outcome; 100 findings in one scene cause one load/save; cancellation and exceptions release all owned previews/prefab contents; failed saves retain recoverable work; read-only assets produce typed failures; developer/release/imported sample requests resolve the intended roots. Unit tests exercise grouping/staleness with a fake adapter, followed by Edit Mode scene/prefab scenarios on Unity 6.0-6.4 and 6.5+.

**Effort / risk.** Approximately 2-4 developer days plus cross-version Editor validation. Medium risk around stable IDs in preview/prefab contexts and Undo/save semantics; verify these APIs before choosing the final identity adapter. No new runtime scene-only dependency is needed.

### P2: One bounded inspector/update lifecycle

**Problem.** Two update checkers duplicate HTTP/version/session-state code, while generated theme/doc textures are owned by static fields. This spreads cancellation, reload cleanup and error-state responsibilities across the inspector implementation.

**Proposal.** One package-keyed update service with injected clock/transport, a bounded timeout, a tracked request through release-to-tags fallback, explicit terminal states and reload disposal. Keep existing checker facades/session keys as adapters. Add one owner for generated inspector textures with deterministic cleanup before assembly reload; borrowed AssetDatabase/AssetPreview textures remain borrowed. Acceptance: failure, tags fallback, cancellation and reload leave no active request; stale session Checking recovers; repeated domain reloads do not accumulate generated textures. Approximately 2-3 days, medium risk; preserve update intervals and visual settings. This is an architectural proposal, not a claim that network/graphics execution was verified here.

## Coordinator actions and remaining findings

1. **C1 Medium — component instance selection, Neo.Condition / Neo.NoCode coordination.** `ConditionEntry.ComponentIndex` and `_otherComponentIndex` are serialized/exposed, but `ConditionValueSource` resolves the first component matching the stored type (`ConditionValueSource.cs`, component loop around lines 198-208). The editor lists duplicates with identical full type names, so the second instance cannot be selected consistently. A runtime/schema decision is required before implementing occurrence indices or explicit scene component references. Public Editor docs now state the current first-instance behavior; other workers' runtime files were not changed.
2. **C2 High — lobby demo needs a Network/sample follow-up.** `Network/LobbyDemoSceneCreator.cs` creates Join and Ready buttons without handlers, registers Host/Leave and status callbacks through non-persistent `AddListener` calls, and creates a scene RoomPlayer assigned to `playerPrefab`. Saving/reopening cannot preserve those runtime-only callbacks, and the room-player asset/runtime presenter contract needs Network ownership. Coordinate a typed lobby presenter plus imported sample/prefab workflow, or replace this generator with a supported sample entry. No scene/prefab assets or runtime Network files were changed. A full builder repair would exceed this worker's ownership.
3. **C3 Medium — Scene Saver backup identity.** `GUI/SceneSaverGUI.cs` derives the backup filename only from the source scene basename. Two saved scenes named `Level.unity` in different folders target the same backup. A GUID-based backup path needs an explicit migration/documentation decision and coordination with existing Scene Saver tests owned by another worker. Existing paths were preserved in this audit.
4. **Central verification.** Compile Neo.Editor and the tests on both Unity version families; run the 28 new cases and relevant existing inspector/Scene Saver/create-menu tests. No new assembly references should be required. Exercise modal Cancel/close/focus, folder removal layout, Power fields, missing-script Undo and failure recovery interactively. Verify ZIP replacement/locked-source handling on the target filesystem and sprite readback with graphics enabled.

## Actual validation and limits

- Scoped `git diff --check -- <owned source/docs/report/new test paths>`: **exit 0**. New untracked files also receive direct trailing-whitespace/conflict-marker checks.
- Strict UTF-8 decoding and replacement-character scan: **102 scoped files**, including all 75 inventory files, 16 owned doc pages, 5 test files, 5 metadata files and this report.
- All relative Markdown links in the 16 owned Editor pages resolve.
- Five new GUIDs are unique across `Assets/**/*.meta`; every new test has metadata and resolves to the existing `Neo.Editor.Tests` assembly.
- Added C# lines and all new tests contain no `var` declarations or newly introduced obsolete instance-ID calls. Owned source changes are a subset of the exact 75-file manifest inventory.
- Final owned-source SHA-256 checkpoint (ordered path + NUL + bytes): `189bbb67d05cb2a30b40245a8fb9757864a65c9e681deb84dcf59967a7c72701`.
- Read-only PowerShell reflection metadata inspection: a Single lookup resolves `Math.Sqrt(Double)`, while `Char.ConvertFromUtf32(Int32)` and `DateTime.FromBinary(Int64)` are not selected for Single input. This confirmed widening behavior and corrected the relay candidate filter; it is not a Unity execution claim.
- **Not executed:** Unity compilation, NUnit/Edit/Play Mode tests, builds, real IMGUI/graphics inspection, network requests or asset maintenance commands. Source review/static checks are the only completed validation; the new tests are centrally pending. There is no claim that either Unity version branch compiled or that any regression test passed.
