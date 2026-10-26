# Neo.Cards.Editor: audit

Status: source audit complete; focused fixes and regressions written; static checks passed. Unity compilation, NUnit execution, and visual checks remain with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Cards/Editor/Neo.Cards.Editor.asmdef`.
- Scope: 1/1 inventoried C# files reviewed completely; nested assemblies excluded.

## Scope

- `Assets/Neoxider/Scripts/Cards/Editor/DeckConfigEditor.cs`

Read `AGENTS.md`, the complete worker contract, this report, and the exact `Neo.Cards.Editor` manifest entry before editing. Context reads covered `DeckConfig`, `DeckTypeExtensions`, `CardSpriteNameParser`, the runtime DeckConfig guide, existing Cards tests, an existing IMGUI test harness, and the editor/test asmdefs. Only the owned editor source, this report, the owned `Cards/Editor.md` topic, and one new uniquely prefixed regression file plus metadata were changed.

Проверен весь исходный файл сборки. Исправлены недоступная настройка custom deck, конфликты auto-fill, проверка папки и preview для atlas; запуск Unity и тестов оставлен координатору по контракту.

### Review coverage

| Area / methods | Review result |
| --- | --- |
| `OnEnable`, `OnInspectorGUI` | Checked all serialized property bindings, single-target lifecycle, update/apply ordering, mode switching, and preservation of existing fields. No serialized names or runtime APIs changed. Multi-object editing remains unsupported, as declared by the absence of `CanEditMultipleObjects`. |
| `DrawHeader`, `DrawDeckSettings`, `IsGameTypeValid`, `GetGameCardCount`, `GetExpectedCardCount` | Checked IMGUI groups, standard rank/count mappings against runtime extensions, custom-mode visibility, and compatibility feedback. Unknown deck values retain the existing 52-card fallback behavior. |
| `DrawAutoFill`, `AutoFillFromFolder`, `LoadSpritesInFolder` | Checked cancel/empty-folder paths, Assets boundary, recursive asset discovery, sprite-sheet sub-assets, and path deduplication. No asset, scene, or prefab generation was added. |
| `ApplyAutoFill`, `GetSuitProperty` | Checked clearing/resizing, minimum-rank offsets, out-of-range ranks, parser failures, all duplicate slot kinds, report counts, preservation of custom entries and unmatched special sprites, and serialized Undo support. Parser-produced suits are valid enum values; the existing fallback is not exposed to external input. |
| `DrawBackSprite`, `DrawSuitSection`, `DrawJokers`, `DrawValidation`, `DrawValidationBadge`, `DrawSpritePreviewGrid`, `DrawSpritePreview` | Checked null sprites, empty lists, diagnostic severity, balanced GUI/color restoration, atlas compatibility, and texture ownership. Preview textures are borrowed from Unity's cache and are not destroyed by this editor. |
| Dependencies, logging, versions, tests | Runtime dependencies remain `Neo.Cards` / `Neo.Extensions`; editor APIs stay in the editor-only assembly. Logging occurs only after an explicit auto-fill action. No scene search, scene-object dependency, direct obsolete instance-id API, new version branch, or shared configuration change was introduced. Existing Cards tests did not cover this inspector's importer. |

Reuse receipt: read the Cards section of `neoxider-tools/references/game-systems.md` and searched the extensions/modules references and local editor code for path/preview helpers. Reused `CardSpriteNameParser`, the existing deck rank helpers, and Unity's `SerializedObject`, `FileUtil.GetProjectRelativePath`, `AssetDatabase.IsValidFolder`, and `AssetPreview`. The only new production helper is a private Assets-folder predicate around the existing Unity APIs; no new gameplay service was implemented.

## Findings and fixes

### F1 — P2: custom decks could not be authored in the normal inspector

- **Trigger:** select a DeckConfig and follow the documented `Use Custom Deck` / `Custom Cards` workflow.
- **Evidence:** the original `OnEnable` cached only the nine standard properties; the complete original `OnInspectorGUI` never drew `_useCustomDeck` or `_customCards`, although both serialized fields and the runtime custom-deck APIs exist in `DeckConfig`.
- **Consequence:** the normal inspector could neither enable custom mode nor edit its entries. Existing custom assets displayed irrelevant standard controls and compatibility errors.
- **Fix:** `DeckConfigEditor.cs:36,51,107` binds and renders both properties, keeps the common back sprite, and displays standard suit/auto-fill controls only in standard mode. Changing modes preserves both datasets.
- **Verification:** all 11 bindings resolve to actual runtime fields. The regression fixture checks that an import preserves a configured custom entry and that `GenerateDeck()` still returns its custom id. Actual control visibility and editing require the coordinator's IMGUI check; no automated UI assertion or visual pass is claimed.

### F2 — P2: duplicate backs and jokers silently replaced artwork

- **Trigger:** the folder contains `card_back` plus `back`, `joker_red` plus `red_joker`, or equivalent black-joker candidates.
- **Evidence:** the original special-kind switch assigned the sprite unconditionally; only the normal-card branch checked conflicts. Thus a later special candidate replaced the earlier candidate without entering `conflicts`.
- **Consequence:** a seemingly successful import chose different special artwork while its conflict report omitted the collision. A repeated identical suit-sprite input also inflated the assigned-card count.
- **Fix:** `DeckConfigEditor.cs:261` tracks assigned serialized slot paths during the current import. The first candidate replaces any old assignment; distinct later candidates are reported and skipped for every slot kind. Repeated identical references do not inflate counts. Missing special sprites keep the existing assignments, preserving the established import behavior.
- **Regressions:** four parameterized cases assert the actual public sprite getters and conflict list, including the existing-value replacement case; another case checks one assignment for a repeated reference. Standard36 mapping, parser failures, custom data preservation, and Undo/Redo have additional state assertions.

### F3 — P2: a similarly named sibling passed the Assets containment check

- **Trigger:** select a sibling such as `D:/Project/AssetsBackup` when `Application.dataPath` is `D:/Project/Assets`.
- **Evidence:** the original plain `StartsWith(projectPath)` returns true, then concatenation produces `AssetsBackup` as the asset search folder. The directory is outside `Assets` but bypasses the displayed restriction.
- **Consequence:** the button searches with an invalid Assets-root path instead of rejecting the selection. No claim is made that Unity necessarily falls back to a project-wide search.
- **Fix:** `DeckConfigEditor.cs:229` uses Unity's project-relative conversion and requires exactly `Assets` or an `Assets/` descendant that `AssetDatabase.IsValidFolder` recognizes.
- **Regressions:** Assets root, backslash/trailing-separator normalization, two similarly named siblings, and the project root have output assertions. These cover the nonmodal predicate used by the actual folder button.

### F4 — P2: tight atlas sprites could abort inspector rendering

- **Trigger:** a previewed sprite is tightly packed in a sprite atlas.
- **Evidence:** the original `DrawSpritePreview` directly read `sprite.textureRect` after checking only null. Unity documents an exception for this input: [Sprite.textureRect](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Sprite-textureRect.html).
- **Consequence:** drawing the back, suit grid, or joker preview could interrupt the inspector with an exception.
- **Fix:** `DeckConfigEditor.cs:544` delegates rendering to cached `AssetPreview` textures, uses a thumbnail while loading, and preserves aspect ratio. `RequiresConstantRepaint` at line 90 follows asynchronous preview loading without calling version-sensitive object-id APIs. Preview generation is documented by [AssetPreview.GetAssetPreview](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetPreview.GetAssetPreview.html).
- **Verification:** the offending rectangle access is absent. No packed-atlas graphics fixture was executed; actual preview appearance/loading and atlas rendering remain explicit central checks.

### F5 — P3: validation and joker guidance could describe different settings

- **Trigger:** edit a setting on the current IMGUI event, or use sprite type Standard52 with play type Standard54.
- **Evidence:** the original validation read the target before `ApplyModifiedProperties`; the joker title considered only sprite type, whereas runtime validation considers both types.
- **Consequence:** diagnostics could show the previous value during the edit event, and the joker section called the artwork optional for an active 54-card play deck.
- **Fix:** apply the serialized edits before drawing validation (`DeckConfigEditor.cs:86`), and consider both deck types for the joker label (`:396`). These are presentation fixes; graphical verification remains with the coordinator.

## Documentation

- Added the manifest-owned narrow public topic `Assets/Neoxider/Docs/Cards/Editor.md` and one valid `TextScriptImporter` metadata record. It explains both inspector modes, Assets selection, sprite-sheet discovery, destructive suit replacement, preserved special/custom data, collision behavior, Undo, diagnostics, previews, and the runtime C# APIs.
- Kept the existing English documentation style, with a concise Russian explanation in this report. No module README or another worker's DeckConfig guide was edited.
- **Coordinator action:** link `Cards/Editor.md` from the Cards entry point / DeckConfig guide, which are outside this worker's doc ownership.
- **Cross-boundary metadata observation:** the initial context read of `Assets/Neoxider/Docs/Cards/DeckConfig.md.meta` contained two concatenated `fileFormatVersion` / `guid` / `TextScriptImporter` records (GUIDs `7838667a5e6940dbbc728b67f5cfb8cd` and `d777606e3f3f94d44b14b741d014f8fe`). This file was not changed; its owner/coordinator should recheck it before import, accounting for concurrent worker edits.

## Substantial improvement proposal

### Plan and review sprite imports before changing a DeckConfig — proposed, not implemented

**Problem.** The current button combines folder discovery, parsing, destructive suit replacement, and summary UI. A folder containing only unrelated sprites still clears the existing suit lists, as the existing documented replacement contract permits. Duplicate winners depend on discovery order. Import automation has no typed editor API, and tests must enter the private nonmodal operation through reflection. These are workflow/design limitations, distinct from the completed fixes above.

**Proposed architecture / API.** Keep runtime `DeckConfig` and `CardSpriteNameParser` unchanged. In `Neo.Cards.Editor`, introduce a small `DeckSpriteImportPlanner.CreatePlan(IEnumerable<DeckSpriteSource> sources, DeckType spriteType)` that returns a read-only `DeckSpriteImportPlan`. Each source carries the Sprite asset, asset path, and stable local file identifier; each planned assignment uses the existing parsed kind/suit/rank data. The plan contains resolved assignments, both sides of each conflict, unknown/out-of-range inputs, and the exact slots that would become empty. Resolve candidates in ordinal asset-path/name/local-id order, independent of AssetDatabase traversal. Reject standard import plans for custom-mode editing unless the caller explicitly requests the stored standard dataset.

Add `DeckSpriteImporter.Apply(DeckConfig target, DeckSpriteImportPlan plan, DeckSpriteImportMode mode)` in the same editor assembly. It performs one serialized Undo transaction. Default `ReplaceSuits` preserves today's empty-slot and unmatched-special behavior; an explicit `FillMissing` mode supports incremental artwork authoring. The existing folder button creates a plan, displays a compact preview of replacements/cleared slots/conflicts, then applies the chosen mode. The same typed API supports C# editor automation without scene references or modal-dialog interception. No generic framework or runtime dependency on editor code is needed.

**Compatibility / migration.** Preserve all DeckConfig field names, asset GUIDs, runtime methods, sprite-name parsing, and the existing button entry. No assets migrate and no background reimport occurs. Keep `ReplaceSuits` as the initial default. Document deterministic resolution as a behavior change for ambiguous folders; unique folders produce the same assignments. Require an explicit apply action for every plan. Link the new API from this editor topic; additional asmdef references belong to the coordinator.

**Acceptance scenarios.** Planning does not change the asset or Undo stack. Reversing source discovery yields the same plan. Both candidates of a collision are identified by asset path/local id. An all-unrecognized or incomplete folder clearly shows the slots it would clear before apply. `FillMissing` retains existing sprites. Standard36 filters ranks correctly. Custom data stays intact in both modes. Applying and undoing a plan restores every changed suit/back/joker value together. Pure planning tests run without a graphics window; only the preview/apply UX needs a graphical editor check.

**Effort / risk.** Approximately 1–2 developer days for the editor planner, apply adapter, preview UX, and focused tests, followed by graphical review. Medium risk around sub-asset identity, Undo grouping, and changed collision ordering; low migration risk because serialization and runtime contracts stay unchanged. Implement with the Cards/editor owners and coordinator, rather than expanding this bounded audit patch.

## Verification and remaining risks

### Actual checks performed

- `git diff --check --` with the explicit owned source/docs/tests/report paths: no whitespace errors.
- A read-only Python static check matched the complete editor source inventory to the manifest (1/1), resolved all 11 serialized-property bindings against runtime declarations, decoded the five changed/new package text files as UTF-8, checked trailing whitespace, checked balanced C# delimiters and absence of `var`/obsolete instance-id APIs in the two C# files, checked documentation links, and counted 12 NUnit cases. A final UTF-8/whitespace check also covered this completed report. Result: **PASS**. This is a lexical/source check, not compilation or test execution.
- Inspected the focused source diff and the complete new regression source. No runtime fields, asmdefs, package/settings files, scenes, prefabs, sample layouts, or existing tests were changed.
- `rg -l` for both new metadata GUIDs across `Assets/**/*.meta`: exactly their two intended metadata files matched; no duplicate of either new GUID was found.
- Checked editor-only assembly platforms and existing test references. `Neo.Editor.Tests` already references `Neo.Cards`; the fixture creates the registered editor through public `Editor.CreateEditor` and uses reflection only to bypass the native folder dialog. It does not require adding a `Neo.Cards.Editor` asmdef reference.

### Regression files and central checks

New file: `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsEditorTests.cs` (+ unique `.meta`), fixture `Neo.Editor.Tests.Audit20261023NeoCardsEditorTests`: eight methods / twelve cases. Assertions inspect public DeckConfig sprites/data and importer report outputs; Undo/Redo checks real serialized state. Tests create only temporary in-memory objects and clean up their own objects/Undo records. No test runner was started.

The coordinator must compile supported Unity versions and run this fixture, including Undo/Redo and platform path normalization. Graphical checks must cover toggling standard/custom modes and editing a custom entry, current-value validation and joker guidance, missing sprites, sliced sprites, a tightly packed atlas, and preview completion. Folder discovery itself has only source review, not an imported fixture execution. Native folder-dialog interaction is deliberately bypassed by the regression tests.

Compile/import, NUnit execution, Play Mode, builds, console observation, and screenshots were **not performed**, as explicitly prohibited by the worker contract/user. No installs, Unity/MCP operations, child agents, commits, or shared service/config changes occurred. This was one worker's review and static verification; independent QA remains central. The GUI fixes and preview rendering are source/API verified, not visually verified.
