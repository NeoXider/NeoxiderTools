# Neo.Abilities.Editor: implementation receipt for 11.0.0

Status: **source-final; latest USS correction native acceptance UNEXECUTED**. Approved actions: **ABIL-EDITOR-CASE (P2)**, **ABILITIES-EDITOR-MODIFIER-BOARD**, and the queued unsupported `:last-child` USS correction (2026-10-25). Same original owner/session, `neo-audit-1023-b02-01`, `gpt-6.1-sol`, reasoning `max`. Branch remains `demo`; target remains `11.0.0`.

Read `IMPLEMENTATION-CONTRACT.md`, the individual implementation request, the original audit/plan and relevant current source/documentation. The coherent R1 correction survived the Windows resource interruption and was not applied again. The initial resume wrote only this receipt. The subsequent Snapshot3 correction below changes only the owned helper-free regression fixture and this receipt; it preserves production source and historical phase reports.

The latest 2026-10-25 correction changes only the existing editor toolbar builder, its USS selector and this receipt. Its evidence and new source hashes are recorded in the final section; earlier hashes remain historical snapshots.

## Completed source and documentation

| Exact existing file | Completed behavior |
| --- | --- |
| `Assets/Neoxider/Scripts/Abilities/Editor/AbilityDesignerUI.cs` | Known modifier ids and ability/modifier duplicate-id dictionaries use `StringComparer.OrdinalIgnoreCase`, matching current runtime catalogs. The bounded internal `CanonicalBuiltinOpId(string)` classifier recognizes only the 12 known `AbilityEffectOps` constants using whole-string `OrdinalIgnoreCase` equality. Color, summary and node-validation paths share it. |
| `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023NeoAbilitiesEditorTests.cs` | Preserved the original 14 cases and the 27 mixed-case correction cases, corrected the two null-content setups at the serialization boundary, and added one normal serialized-refresh case for a nonnull zero-amount node. The fixture includes its own in-memory definitions, closes only its own window, clears owned Undo records and destroys owned definitions. It does not write persistent assets. |
| `Assets/Neoxider/Docs/Abilities/Editor.md` | Contains English/Russian editor C# and Inspector/scene/NoCode workflows, exact identifier-case rules, spelling preservation, custom-id boundaries, Undo and radius precedence, with links to canonical module/runtime topics. |

Current source inspection found the classifier at `AbilityDesignerUI.cs:31`, its display consumers at lines 127/154, the id collections at lines 431/442/490 and the validation consumer at line 596. Runtime `AbilitySystem` catalog dictionaries and `EffectOpRegistry` still use `OrdinalIgnoreCase`.

The main Abilities documentation integration is now present: `Assets/Neoxider/Docs/Abilities/README.md:30` links `Editor.md`; `AbilityDesignerWindow.md:30–35` describes every overriding radius level and links the editor topic, with a further entry at line 46. These parent-owned files were read only in this session. The historical R2 backlink/radius-documentation follow-up is therefore no longer a source dependency for this owner.

The original four-file editor audit fixes remain preserved. No runtime/other-owner source, shared asmdef, release metadata, samples, scenes or prefabs were changed on this resume. Historical [audit](./Neo.Abilities.Editor.md), [plan](./Neo.Abilities.Editor.plan.md) and [correction](./Neo.Abilities.Editor.correction.md) remain phase evidence; their old checks are not presented as integrated verification for 11.0.0.

## API and compatibility

- Existing public `AbilityDesignerWindow.Open()` / `Open(ScriptableObject)` and asset inspector entry points remain unchanged. No new public runtime API, required dependency or scene wiring was introduced. The classifier is internal to the Editor assembly.
- Serialized ids are never rewritten, trimmed or lowercased by this correction. `burn` resolves `Burn`; `Fireball`/`fireball` produce an advisory duplicate warning consistent with runtime last-registration-wins behavior. `CHAIN` receives the ordinary built-in summary and radius checks.
- Unknown ids such as `CHAIN_CUSTOM`, `APPLY_MODIFIER_CUSTOM`, ` HEAL ` and `CuStOm.Spell` retain custom summaries/colors and avoid built-in-only modifier/chain validation. Generic target-selector checks remain independent of operation kind.
- Intentional 11.0.0 editor behavior change: diagnostics now recognize case-variant catalog collisions/references and built-in operations consistently with runtime. No asset/GUID/serialized-field migration is necessary. Runtime registration semantics are unchanged.
- No Unity-version directives or obsolete instance-id APIs were added. Existing centralized version compatibility remains intact; successful compilation on either installed Editor is still unproven here.
- No unsupported NUnit parallelization annotation was added. Execution requires the coordinator's isolated/exclusive runner contract and the fixture's existing exact cleanup.

The optional typed/context-aware validator and dependency index remain proposals in the individual plan. They are not required by this action and were not implemented.

## Exact regression source and acceptance — UNEXECUTED

Fixture/filter: `Neo.Editor.Tests.Abilities.Audit20261023NeoAbilitiesEditorTests`.

The current authored fixture contains **16 methods / 42 cases**, including these unchanged **27 R1 correction cases**:

| Method | Cases | Expected observable result |
| --- | ---: | --- |
| `BuiltinCards_RecognizeMixedCaseWithoutRewritingIds` | 12 | Each runtime-recognized mixed-case built-in shows its proper summary/color; the raw operation label and blueprint string remain unchanged. |
| `ModifierReferences_AcceptRuntimeCaseVariantsWithoutRewriting` | 3 | Mixed-case modifier references agree with runtime catalog lookup and emit no false dangling warning; authored strings remain unchanged. |
| `ModifierOperations_ReportInvalidReferencesRegardlessOfCase` | 4 | Upper/mixed-case apply/remove operations still report one missing-id or dangling-reference diagnostic. |
| `AbilityIds_DetectCaseVariantDuplicatesWithoutRewriting` | 1 | Runtime resolves the second registration; the editor reports a case-variant ability duplicate without changing either id. |
| `ModifierIds_DetectCaseVariantDuplicatesWithoutRewriting` | 1 | Equivalent behavior for modifier definitions. |
| `ChainCards_ValidateUppercaseHopRadius` | 2 | Uppercase `CHAIN` validates overriding radii and displays the authored total hit limit. The `Target` selector prevents an unrelated area warning from hiding a missing chain check. |
| `UnknownOperations_KeepCustomSummaryAndSkipBuiltinOnlyChecks` | 4 | Unknown/suffixed/padded operation ids preserve custom output and do not inherit built-in-only checks. |

Original cases still cover fresh node defaults, Undo/Redo, target switching, Inspector refresh, inline card identity, null modifier content, chain hit counts and per-level radius precedence. Synthetic property events do not establish native typing/focus/panel timing; these remain explicit live-Editor acceptance tasks.

## Resume evidence and execution barrier

Actual initial-resume work was limited to small `cmd.exe`/Python source reads, reference enumeration, current-branch inspection and artifact SHA-256 bookkeeping. Snapshot3 additionally inspected the existing XML and source diff, consulted official Unity serialization documentation, and changed only the owned fixture and receipt. No Unity, compiler, tests, product validators/checkers, installers, services, child agents, commits or pushes were launched by this actor on either resume. No shared manifest/review-action/state tracker was edited.

Earlier resume SHA-256 values (before the Snapshot3 fixture correction):

| File | SHA-256 |
| --- | --- |
| `AbilityDesignerUI.cs` | `07bc4131a23bdd9f5b4b399922a7445623aafd4b1b448226b18f370a84ed4c52` |
| `Audit20261023NeoAbilitiesEditorTests.cs` | `91eeaba257471240b4ccab0dfc545352a501bd2b308d384d4a37f3c961707193` |
| `Editor.md` | `4c46dfbb459b5c500819e3b7a9edeb42b06b671b8459eb3dce975d04ff28588b` |

Those earlier source/fixture hashes matched the prior source-final correction and the fixture hash cited in the approved request. They are preservation evidence, not compilation, test discovery or a pass result. Historical static-check receipts remain historical; no product checker was rerun during the implementation barrier. Current Snapshot3 correction hashes are recorded below.

## Snapshot3 actual graphical failures and bounded correction

Read the exact owned test-case messages and stacks from `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`, SHA-256 `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`. This is existing root-run native evidence, not a run by this actor. Its 41 owned cases contain **39 Passed / 2 Failed**, including passes for all 27 R1 cases. It does not validate the subsequently changed fixture.

| Exact case in `Neo.Editor.Tests.Abilities.Audit20261023NeoAbilitiesEditorTests` | Actual graphical Edit failure |
| --- | --- |
| `ModifierBoard_MissingBlueprintShowsADiagnostic` | `Expected: not null` / `But was: null`; stack ends at `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Assets/Neoxider/Tests/Edit/Abilities/Audit20261023NeoAbilitiesEditorTests.cs:235`. |
| `ModifierBoard_SkipsNullTickNodesAndKeepsValidSummaries` | `Expected: 1` / `But was: 2`; same snapshot fixture path, line 218. |

Current source/API evidence:

- `ModifierDefinition.cs:13` serializes `_blueprint` inline using `[SerializeField]`. `ModifierBlueprint.TickEffects` and `EventReactions` are ordinary lists of serializable classes, without `[SerializeReference]`. A managed null is therefore not a persistent serialized asset state. [Unity 6.3 serialization rules](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html) and [SerializeReference API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializeReference.html) explicitly document replacement of inline nulls with objects containing unassigned fields.
- `SelectAsset` constructs a `SerializedObject`; `RebuildCenter` calls `_serialized.Update()` before the modifier board. [SerializedObject.Update](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SerializedObject.Update.html) documents serialization at construction and reserialization during Update. The old tick test inserted nulls before `OpenWindow`; the old missing-blueprint test inserted null before `RefreshAfterEdit`, which calls `RebuildCenter`. Neither test asserted that its required null survived that boundary.
- `AbilityDesignerWindow.cs:567` already renders a real Error HelpBox for a managed null blueprint. It already skips null tick nodes before creating any card and skips null event reactions. Tick summaries are the only `Label` elements given the exact `nad-card` class in the modifier board; reaction cards are `VisualElement`s. The header uses `nad-header-card`, library rows use `nad-row`, and the final hint uses `nad-empty-note`. There is no source basis for hiding a second nonnull tick node as an unrelated default Inspector label.
- `BuildModifierBoard` appends a fresh `nad-mod-board` to the retained center pane; it does not clear an existing board. Direct renderer tests must remove the prior board and query the newly retained board. `RefreshValidationAndLibrary` reads managed data and rebuilds library/status UI without invoking serialization.

The serialization boundary explains why the old null preconditions are invalid. The two XML failures are consistent with inline placeholders, but XML does not contain a visual-tree dump or post-binding node payloads; the exact point at which each placeholder materialized was not instrumented and is not claimed as observed native evidence. No production defect requiring a runtime/serialized-contract change was demonstrated. Filtering zero/default-looking nodes would also hide valid authored content.

Only `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023NeoAbilitiesEditorTests.cs` changed for this correction:

1. `ModifierBoard_SkipsNullTickNodesAndKeepsValidSummaries` (line 207) now opens/binds a valid modifier first, inserts managed nulls afterward, and invokes the existing board renderer before another serialization boundary. It still requires **exactly one** tick card containing `3 healing`; it additionally requires heal styling, no reaction card, two unchanged underlying tick slots, the first slot still null, the same valid node instance in the second slot, and an unchanged null reaction.
2. `ModifierBoard_MissingBlueprintShowsADiagnostic` (line 260) still requires a real nonnull HelpBox with the exact missing-blueprint text and a validation issue. It now asserts the managed null before drawing, calls the existing serialization-free validation/library path and board renderer, scopes the query to the fresh board, and additionally requires Error severity, no tick cards, a library warning, and a blueprint that remains null afterward. This tests null handling before serialization; it deliberately makes no promise that an inline null survives full Inspector refresh.
3. New `ModifierBoard_RendersNonNullZeroAmountNodesAfterSerializedRefresh` (line 238) uses the ordinary `OpenWindow` selection/binding/full-refresh path. It requires **exactly two** cards, in order: `0 magical damage` and `3 healing`. It also requires the two authored data slots and the nonnull damage/zero payload. This prevents a false fix that drops default-looking or zero-amount nodes.
4. Private fixture helper `RebuildModifierBoardWithoutSerialization` (line 551) removes only the previous retained `nad-mod-board`, invokes the existing production renderer, and requires exactly one newly created board with a different identity. It creates no MonoBehaviour/helper assembly, fake renderer, mock, or manual Update path. Reflection remains limited to the existing editor-window command bridge and deliberately malformed in-memory asset setup.

Every original positive/count/diagnostic oracle in the two failing tests remains; no assertion was relaxed, no ignore/whitelist was added, and no runtime or public/serialized API changed. `Editor.md` therefore needs no new behavior statement. The original audit, plan and R1 correction report remain untouched. The fixture path has zero occurrences in the central relocation reservation map read for this task; no shared asmdef, reserved helper/caller, vendor, migration target, sample or protected metadata was edited.

Actual correction validation is source inspection and a `git diff --no-index` against the source3 native fixture copy, plus XML evidence reading and SHA-256 bookkeeping. The diff contains only the two setups/queries, their stronger assertions, the new zero-amount case and the fixture helper. No checker/compiler/test run occurred; new acceptance remains **UNEXECUTED** until root opens the next global freeze.

| Snapshot3 correction artifact, before the later USS correction | SHA-256 |
| --- | --- |
| Corrected `Audit20261023NeoAbilitiesEditorTests.cs` | `50cff18a4695c1fe5986fbacf67dd2adf20dc43432bb150c3bd56c378a796f36` |
| `AbilityDesignerWindow.cs` at that boundary | `24a66bbc533e3437a649c0deff73c9d0cc8e0cb5b8a2c3034387bf9a4f88df29` |

No source-blocking dependency remains. Root should rerun the exact three modifier-board methods above and the complete **42-case** fixture on the new frozen snapshot. The two managed-null cases establish renderer/validation behavior at that boundary; the added case and existing selection/Inspector/Undo cases cover ordinary serialized paths. Real continuous typing/focus timing, native memory/lifetime and version-family acceptance remain distinct evidence debts. Source writes stop after this receipt.

## Dependencies and remaining evidence debt

**No source-blocking additive API or cross-owner documentation dependency remains for ABIL-EDITOR-CASE.** The completed code consumes existing `Neo.Abilities` constants/catalog APIs; runtime delivery/event-bus work is owned elsewhere and does not require another editor change for this action.

After every writer finishes, root can include this owner in the global source freeze and hand the unchanged fixture to the same verification operator. Required current integrated evidence remains:

1. Compile and obtain nonempty EditMode discovery on the isolated Unity 6.3 and 6.6 validation hosts; this covers the installed representatives of the two supported version families, not an executed claim for all versions.
2. Execute the full 42-case fixture and attribute any failures to the current frozen snapshot. Source3's 39/41 pass result and its two now-corrected fixture failures do not substitute for a new result.
3. Verify ordinary bound Inspector edits, continuous typing in the active foldout, Undo/Redo timing, window close/reopen, selection changes and SerializedObject lifetime in the real Editor. Native-memory profiling is evidence debt if a residual allocation/lifetime concern is reproduced; no post-fix leak is asserted.
4. Let root run the integrated package/document/UTF-8/meta/GUID checks after source freeze, including current parent-owned backlinks. Do not start those processes while other owners are still writing.

No PlayMode gameplay change was introduced by this editor action. Broader runtime/scene acceptance follows the coordinator's integrated pipeline. Final push remains solely root's action after later complete verification.

## 2026-10-25: unsupported last-child USS correction — source-final

Reread `IMPLEMENTATION-CONTRACT.md` and the complete current `Neo.Abilities.Editor.implementation-request.md`. Root reports that both ba515 Unity 6.3 native logs identify the unsupported `:last-child` selector in `AbilityDesigner.uss`. That log result is attributed to root; this actor did not run or newly inspect those native jobs. Current source inspection confirmed the offending selector at line 182 and the base create-button `margin-right: 6px` at line 179.

Exact source changes:

- `Assets/Neoxider/Scripts/Abilities/Editor/AbilityDesignerWindow.cs:157`: `BuildLeftToolbar` adds the module-owned `nad-create-btn--last` class to `newModifier`, immediately after its existing `nad-create-btn` class. The builder creates exactly two buttons and appends `newModifier` last. `+ Ability` still precedes `+ Modifier`; their existing `CreateAsset<AbilityDefinition>("New Ability")` and `CreateAsset<ModifierDefinition>("New Modifier")` callbacks, enabled state and insertion order were not edited.
- `Assets/Neoxider/Scripts/Abilities/Editor/AbilityDesigner.uss:182`: replace `.nad-create-btn:last-child` with `.nad-create-btn.nad-create-btn--last`. The declaration remains `margin-right: 0`. The compound class rule keeps a more specific final-button override of the base class without structural or sibling pseudo selectors. Existing `flex-grow: 1`, `flex-basis: 0`, base 6px spacing and all other style declarations remain unchanged.

This is one added C# class assignment and one USS selector replacement. No framework, public API, serialized field/default, asset creation behavior or authoring workflow changed, so the canonical editor guide needs no update. No regression fixture was edited for this bounded styling correction; the existing 42-case fixture and all previous source fixes are preserved. Historical audit/plan/correction reports, other owners, shared asmdefs, Mono helpers, samples, GUIDs and metas were untouched.

Actual allowed validation: inspected the existing builder and applicable USS declarations, reviewed the source diff, enumerated the exact class/selector references with `rg`, decoded the changed source as strict UTF-8, and computed raw-byte SHA-256 values. The marker appears on the final create button and in its matching class rule; the unsupported selector is absent from the two source files. The stylesheet diff is exactly the selector replacement. The window diff against repository HEAD also contains the already completed audit changes; those were preserved, not reapplied. No Unity/compiler/tests/product checkers/process control/installers/services/children/commits/pushes were executed.

| Exact file | Before this correction SHA-256 | Source-final SHA-256 |
| --- | --- | --- |
| `Assets/Neoxider/Scripts/Abilities/Editor/AbilityDesignerWindow.cs` | `24a66bbc533e3437a649c0deff73c9d0cc8e0cb5b8a2c3034387bf9a4f88df29` | `e34a578a9151d2a524f375b1c4f9cdf740c2810e8bb28c69b70355c7d6d1a888` |
| `Assets/Neoxider/Scripts/Abilities/Editor/AbilityDesigner.uss` | `2901f78ffd1472acfb27665b703e7d452c8bb2f913ccd1b42141a5c6e1bbfbb5` | `1223433939ed5ca79e4a46ac32607b405e4e41e3f558e2c63cab06ba48b01c14` |
| `Assets/Neoxider/Scripts/Abilities/Editor/AbilityDesigner.uss.meta` (read-only preservation evidence) | `18568a649756fbc453387ca801641069f069a2b350f19700a60232533f46666d` | `18568a649756fbc453387ca801641069f069a2b350f19700a60232533f46666d` |
| `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023NeoAbilitiesEditorTests.cs` (unchanged) | `50cff18a4695c1fe5986fbacf67dd2adf20dc43432bb150c3bd56c378a796f36` | `50cff18a4695c1fe5986fbacf67dd2adf20dc43432bb150c3bd56c378a796f36` |

**Required root integration dependency: NEW sealed current-source baseline.** This USS is one of the 314 earlier metadata-whitelist targets, and its source body has intentionally changed under the explicit request. The hashes above are owner bookkeeping, not a sealed baseline. After all writers finish, root must capture a new sealed current-source baseline containing the corrected stylesheet body (`1223433939ed5ca79e4a46ac32607b405e4e41e3f558e2c63cab06ba48b01c14`) before later metadata writes/equivalence comparisons. Old source3 before-captures are diagnostic history and cannot supply the asset SHA for this body. Any subsequent body change requires another current capture. This actor did not touch old Local inputs/profiles, frozenSource3, whitelist records or the metadata helper, and did not perform the metadata rewrite or create a shared seal.

Remaining acceptance — **UNEXECUTED**, root only after all writers/new freeze:

1. Clean native USS import on the supported Unity validation hosts must produce no unsupported `last-child` diagnostic for `AbilityDesigner.uss`; source matching alone is not native importer evidence.
2. Open Ability Designer and verify the create row still has `+ Ability`, then `+ Modifier`, with the existing enabled behavior and creation callbacks. Visually confirm the existing two-column flex layout and 6px inter-button spacing, with zero trailing right margin on the final button. Inspect resolved margins if the visual result is ambiguous.
3. Bind all later metadata equivalence evidence to the new sealed current-source baseline above; do not combine old source3 asset hashes with the corrected body. Existing fixture/native interaction acceptance debt remains as recorded earlier, without a new pass claim.

Source and receipt are complete. No owner source dependency remains; the baseline/import/visual evidence belongs to root's subsequent freeze. Source writes stop here.
