# Neo.Cards.Editor — implementation receipt

Status: **source-final**, `demo`, target `11.0.0`. Scope: the exact `Neo.Cards.Editor` manifest entry and its prefixed regression source. Historical audit and plan remain unchanged. No execution approval is inferred from this status; the global source-writing barrier remains in effect.

## Approved actions and additive API

The exact implementation request assigns **no new numbered review-action IDs** and no additional confirmed runtime defect. Its instructions are completed: preserve the independently reviewed patch and meaningful regressions, finish owned documentation/navigation, retain a substantial proposal, and record actual evidence debt. F1–F5 below are historical audit finding IDs, not invented review-action IDs.

**Additive API for dependents: none.** No new planner, importer service, runtime method, serialized field, assembly reference, or initialization dependency is introduced. Existing `DeckConfigEditor.RequiresConstantRepaint()` overrides the Unity editor hook. `ApplyAutoFill(...)` and `TryGetAssetFolder(...)` remain private implementation details; reflection in the existing fixture does not make them supported public APIs.

## Exact files and completed work

| File | Result |
| --- | --- |
| `Assets/Neoxider/Scripts/Cards/Editor/DeckConfigEditor.cs` | Earlier coherent source patch retained; no additional source rewrite in this phase. Entire owned C# inventory consists of this file and was reviewed. |
| `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsEditorTests.cs` | Existing eight test methods / twelve cases retained; no duplicate regression fixture added. Authored source only, **UNEXECUTED**. |
| `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsEditorTests.cs.meta` | Existing regression asset identity retained. |
| `Assets/Neoxider/Docs/Cards/Editor.md` | Finished canonical-module navigation, single-asset authoring scope, folder cancellation/no-result behavior, component/UnityEvent workflow, C# and private-importer boundaries, preview ownership/repaint contract, and upgrade behavior. Corrected wording from saved values to applied serialized values. |
| `Assets/Neoxider/Docs/Cards/Editor.md.meta` | Existing documentation identity retained. |
| `Docs/Audits/2026-10-23/Neo.Cards.Editor.implementation.md` | This source-final/dependency receipt. |

Preserved confirmed fixes:

| Audit ID | Concrete source result |
| --- | --- |
| F1 | `_useCustomDeck` and `_customCards` are bound and drawn. Custom mode exposes entries and the shared back; standard settings remain serialized while their controls and auto-fill are hidden. |
| F2 | Per-import slot tracking keeps the first candidate for suit/back/red-joker/black-joker slots. Distinct later candidates are conflicts; repeated references do not inflate assignments. Missing special sprites preserve previous assignments. |
| F3 | Folder conversion accepts exactly `Assets` or `Assets/…`, requires an existing AssetDatabase folder, and normalizes separators. Sibling prefixes cannot pass the containment check. |
| F4 | Cached `AssetPreview`/thumbnail rendering replaces direct sprite texture-rectangle access. Borrowed preview textures are not destroyed. |
| F5 | Serialized edits are applied before validation. Joker requirement labels consider both sprite-deck and play-deck settings. |

The importer continues to reuse `CardSpriteNameParser` and `SerializedObject`; no parallel parser or hand-written asset transaction layer was created. The preview uses Unity's cache. Documentation points to existing runtime `DeckConfig`, `CardComponent` and `DeckComponent` contracts rather than introducing editor dependencies into gameplay.

## Compatibility and migration

No public runtime API, serialized field name, prefab, scene, sample layout, package metadata or shared asmdef changes were made by this owner. Existing configuration data remains available when changing authoring modes. Standard imports still resize and clear all four suit lists; this documented replacement behavior is preserved, not reclassified as a defect. Absent back/joker candidates and custom entries remain intact.

The deliberate behavioral corrections are first-wins conflict handling for special sprites, rejection of Assets-prefixed sibling folders, available custom authoring, current-value validation and safe preview access. Users whose folders depended on special sprites silently overwriting each other must keep one candidate per slot or review conflicts and assign artwork explicitly. No asset migration or automatic rewrite is required.

## Authored acceptance — UNEXECUTED

Existing fixture: `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsEditorTests.cs`.

| Case source | Observable assertion after the execution barrier |
| --- | --- |
| `AutoFill_DuplicateSlots_KeepFirstSpriteAndReportTheOther` (4 cases) | Actual public suit/back/red-joker/black-joker getters retain the first sprite; the second distinct name is reported; suit-only counts remain correct. |
| `AutoFill_RepeatedSpriteReference_CountsOneAssignedCard` | Repeating one reference assigns one suit card without a conflict. |
| `AutoFill_Standard36_MapsRanksAndPreservesCustomEntriesAndMissingSpecials` | Nine rank slots map six through ace; unsupported names/ranks are reported; configured custom entries and absent special replacements survive. |
| `AutoFill_UndoAndRedo_RestoreSuitAndBackAssignments` | Real Undo/Redo restores and reapplies suit sizes/assignments and the back sprite. |
| `AssetFolder_ProjectAssetsRoot_IsAcceptedAsAnAssetDatabasePath` | The project Assets root is accepted and returns `Assets`. |
| `AssetFolder_BackslashesAndTrailingSeparator_NormalizeToAssets` | Backslashes and the trailing separator normalize to the valid Assets path. |
| `AssetFolder_SiblingWithAssetsPrefix_IsRejected` (2 cases) | Both `AssetsBackup` and `Assets-Sprites` prefixes are rejected. |
| `AssetFolder_ProjectRootOutsideAssets_IsRejected` | The project root is rejected. |

The fixture creates owned in-memory objects, uses public configuration outputs plus importer reports, and cleans those objects and its Undo records. It is not evidence of test discovery or execution. It does not automate custom inspector interaction, tightly packed atlas rendering, asynchronous preview readiness, or actual recursive AssetDatabase folder discovery.

## Actual work and remaining evidence

This phase used source/document reads, targeted declaration/reference enumeration and diff inspection only. PowerShell initialization failed with an out-of-memory error; read-only inspection continued through `cmd.exe`. No compiler, Unity/MCP, runner, product checker, service, installation, commit or child agent was launched. Historical audit checks are historical evidence only and are not claimed against the integrated source snapshot.

After every writer finishes, the coordinator's verification operator must compile/discover/run on the contract's Unity 6.3 and 6.6 hosts. This assembly still needs the twelve fixture cases executed and native acceptance of custom/standard mode switching, current-edit diagnostics, both joker labels, tightly packed atlas previews, delayed previews, folder cancellation/no sprites, recursive sprite-sheet discovery and Undo through the inspector. Preview loading is global; unrelated loads may request repaint. No measured performance defect or unsupported-version pass is claimed.

The old `DeckConfig.md.meta` concatenation observation is stale and was centrally corrected according to the exact implementation request. It is not a remaining repair dependency.

## Cross-owner dependencies and proposal

**No pending additive API or source dependency blocks this assembly.** The existing reflection fixture needs no typed `Neo.Cards.Editor` reference. Runtime `DeckConfig`/parser/component APIs are consumed as existing contracts; their implementation remains owned by `Neo.Cards`.

**Documentation integration dependency:** parent owner `Neo.Cards` / `neo-audit-1023-b02-05` must add a link to `./Editor.md` from `Assets/Neoxider/Docs/Cards/README.md` and the authoring section of `Assets/Neoxider/Docs/Cards/DeckConfig.md` if absent at integration. The implementation contract assigns parent backlinks to that owner. This receipt does not change either file. Our editor topic already links back to both canonical pages and to the relevant component/data guides.

**Substantial proposal, not implemented:** the historical plan's deterministic sprite-import planning/preview service remains optional. Separate discovery and parsing from mutation; produce a slot/conflict/unrecognized report without modifying the asset; give conflicts a stable ordering and explicit resolution before one Undo-supported apply. Reuse the current parser and serialized asset contract, preserve default replacement semantics, and require preview/apply equivalence and no-mutation acceptance. Exact proposed public types/signatures remain design candidates in `Neo.Cards.Editor.plan.md`, not shipped APIs. Future typed planner tests would require coordination with `Neo.Editor.Tests` over its shared asmdef; no such change is necessary now.
