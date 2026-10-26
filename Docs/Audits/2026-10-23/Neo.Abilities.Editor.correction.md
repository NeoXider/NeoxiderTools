# Neo.Abilities.Editor: correction R1

Status: **source-final**. The confirmed identifier-case mismatch from the [phase 2 plan](./Neo.Abilities.Editor.plan.md) is corrected. Unity compilation and test execution remain pending with the coordinator. This continues the original owner/session (`gpt-6.1-sol`, reasoning `max`).

The [original audit](./Neo.Abilities.Editor.md) and phase 2 plan remain unchanged phase snapshots. This document supersedes their pending R1 status only. The future typed/context-aware validator proposal was not implemented.

## Confirmed evidence before correction

Read the current root AGENTS.md and current source rather than assuming the runtime contract remained unchanged:

- `Assets/Neoxider/Scripts/Abilities/Domain/AbilitySystem.cs:18`: the modifier and ability dictionaries use `StringComparer.OrdinalIgnoreCase`. `RegisterAbility`/`RegisterModifier` assign by key; `TryGetAbility`/`TryGetModifier` query those dictionaries. Re-registering a case variant replaces the catalog value.
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/EffectOpRegistry.cs`: its operation dictionary also uses `OrdinalIgnoreCase`. `AbilitySystem.ExecuteEffects` dispatches through `Ops.TryGet(node.OpId, ...)`.
- `AbilitySystem` installs defaults through `DefaultEffectOps.RegisterAll`; `Domain/Effects/AbilityEffectOps.cs` and `Domain/Effects/Ops/DefaultEffectOps.cs` define/install the same 12 built-ins.
- Before this correction, `Editor/AbilityDesignerUI.cs` used `Ordinal` in its known-modifier set and both duplicate-id dictionaries. Its summary/color switches and chain/apply/remove validation compared the authored operation id directly with lowercase constants.

Observable mismatches demonstrated by those paths: `burn` referencing `Burn` received a dangling-reference warning despite a successful runtime catalog lookup; `Fireball`/`fireball` were not flagged as overlapping registrations; uppercase built-ins displayed custom summaries/colors and bypassed built-in-only reference/hop-radius checks. These were verified from source. No Unity reproduction is claimed.

## Focused changes

Only the following existing files changed in this correction:

1. [AbilityDesignerUI.cs](../../../Assets/Neoxider/Scripts/Abilities/Editor/AbilityDesignerUI.cs): all three id collections now use `OrdinalIgnoreCase`. A small internal `CanonicalBuiltinOpId` classifier compares whole strings against the 12 existing `AbilityEffectOps` constants using `StringComparison.OrdinalIgnoreCase`. The color switch, summary switch and node validator share that classifier.
2. [Audit20261023NeoAbilitiesEditorTests.cs](../../../Assets/Neoxider/Tests/Edit/Abilities/Audit20261023NeoAbilitiesEditorTests.cs): seven new test methods define 27 correction cases. The existing window fixture now includes all fixture-owned in-memory definitions, allowing real cross-definition reference and duplicate checks without persistent assets or shared AssetDatabase writes.
3. [Editor.md](../../../Assets/Neoxider/Docs/Abilities/Editor.md): English and Russian documentation now explains case-insensitive ability/modifier/built-in lookup, duplicate-id behavior, raw spelling preservation and custom-operation boundaries for C# and Inspector/scene/NoCode workflows.

The classifier returns canonical constants only for a whole-string built-in match; otherwise it returns the original input. It does not trim, lowercase or write into the blueprint. The raw operation label and diagnostic inputs retain authored spelling. Therefore `CHAIN` is recognized, while `CHAIN_CUSTOM`, `APPLY_MODIFIER_CUSTOM`, ` HEAL ` and `CuStOm.Spell` retain custom behavior. Generic area-selector validation remains applicable independently of operation kind.

Existing public declarations, serialized fields, menu commands, Undo/binding behavior and dependency references were preserved. The added classifier is internal to the existing Editor assembly and uses existing constants/.NET string comparison; it does not create a runtime system or access scenes. No new package files or GUIDs were needed. Existing metadata is unchanged.

No runtime/other-owner source, shared asmdef/configuration, sample layout or historical audit/plan was edited. The other three Editor C# files were also unchanged. No commits, installs, Unity/MCP, build/test processes or child agents were used.

## Exact authored regressions

Central filter: `Neo.Editor.Tests.Abilities.Audit20261023NeoAbilitiesEditorTests`.

| New method | Cases | Observable assertions |
| --- | ---: | --- |
| `BuiltinCards_RecognizeMixedCaseWithoutRewritingIds` | 12 | Runtime `Ops.TryGet` recognizes each mixed/uppercase built-in; the real window shows the operation-specific summary/color, raw op label and unchanged blueprint string. Covers damage, heal, apply/remove modifier, dispel, resource change, spawn, knockback, pull, teleport, execute and chain. |
| `ModifierReferences_AcceptRuntimeCaseVariantsWithoutRewriting` | 3 | `BuRn` resolves through the runtime catalog with a different-case reference; the window emits no dangling warning, displays that reference and preserves both original strings. Includes lowercase and uppercase/mixed apply/remove operation ids. |
| `ModifierOperations_ReportInvalidReferencesRegardlessOfCase` | 4 | Mixed/uppercase apply/remove operations still report exactly one missing-id or dangling-reference diagnostic. The raw operation id and reference remain unchanged. |
| `AbilityIds_DetectCaseVariantDuplicatesWithoutRewriting` | 1 | Runtime lookup resolves to the second case-variant registration; the editor emits one duplicate-ability diagnostic and retains both spellings. |
| `ModifierIds_DetectCaseVariantDuplicatesWithoutRewriting` | 1 | Equivalent last-registration/duplicate warning assertions for modifiers. |
| `ChainCards_ValidateUppercaseHopRadius` | 2 | `CHAIN` with `Target` selector reports a chain-specific invalid overriding radius despite positive flat fallback; positive per-level radii with zero fallback pass. Both display two total hits and preserve `CHAIN`. Using `Target` prevents generic area validation from masking a missing chain check. |
| `UnknownOperations_KeepCustomSummaryAndSkipBuiltinOnlyChecks` | 4 | Unknown/suffixed/padded ids retain custom summary/color and original op label, without built-in-only modifier or chain warnings, despite deliberately missing modifier ids and zero radius. |

The original eight test methods / 14 cases remain textually unchanged. Together the fixture now contains **15 test methods / 41 cases**. These are authored regressions, **not executed passes**. Reflection remains limited to the existing window command/library bridge; assertions observe runtime lookup results, authored state and real UI output, rather than inspecting the classifier's implementation.

## Actual static validation

Completed locally, without a compiler or test runner:

- Strict UTF-8, final-newline, trailing-whitespace and control/replacement-character checks on the changed source, test fixture and public documentation: passed.
- C# lexical delimiter balance, explicit-type/no-`var` and obsolete-instance-id scans on changed C# files: passed. These are structural checks, not C# compilation.
- Checked classifier coverage against current `AbilityEffectOps` constants: exactly 12 distinct known operations. Verified both display consumers and validation call the classifier, all three id collections use `OrdinalIgnoreCase`, and the validator no longer compares raw `node.OpId` to lowercase built-in constants.
- Static fixture discovery: 15 methods / 41 cases, including the exact seven correction groups and 27 case counts above. Compared the existing fixture prefix against the correction baseline: original 14 cases are unchanged.
- Compared existing public method declarations with the correction baseline: unchanged. Confirmed all eight distinct local links in `Editor.md` resolve.
- SHA-256 comparison against the correction baseline identified exactly the three authorized existing-file changes. Twelve protected files remained unchanged, including the audit/plan snapshots, the other Editor sources, both inspected asmdefs, existing metadata and the three inspected runtime contract files.
- Scoped `git diff --check --` on the changed source/test/docs: exit **0**. The final correction-report text is also checked separately for UTF-8/whitespace/newline and scoped diff consistency.

Source-final checksums for the changed C# files:

| File | SHA-256 |
| --- | --- |
| `AbilityDesignerUI.cs` | `07bc4131a23bdd9f5b4b399922a7445623aafd4b1b448226b18f370a84ed4c52` |
| `Audit20261023NeoAbilitiesEditorTests.cs` | `91eeaba257471240b4ccab0dfc545352a501bd2b308d384d4a37f3c961707193` |

## Central verification still required

Compile on Unity 6.0–6.4 and separately 6.5+, then run the full 41-case fixture using the filter above. Confirm ordinary binding-driven Inspector edits and continuous typing in a live Editor; direct event dispatch in the preexisting fixture does not prove native panel event timing/focus. The correction adds no Unity-version directives, obsolete instance-id calls or new assembly dependency.

There is no Unity compilation, executed test result, screenshot or native-memory profile from this correction. Runtime registration behavior remains last-wins; project-wide duplicate warnings remain advisory. The broader library-scope validator, plugin dependency declarations and indexing proposal remain deferred. No cross-owner source action is required to integrate R1 itself.

**По-русски:** R1 исправлен только в редакторе: регистр согласован с runtime, строки ассетов не переписываются, неизвестные операции не становятся встроенными. Добавлено 27 регрессионных случаев; все 41 случая предстоит выполнить координатору. Аудит и план сохранены как снимки фаз.
