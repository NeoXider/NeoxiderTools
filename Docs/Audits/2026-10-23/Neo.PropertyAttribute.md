# Neo.PropertyAttribute: audit

Status: source audit and scoped changes complete; Unity compilation, test execution and IMGUI integration verification remain centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/PropertyAttribute/Neo.PropertyAttribute.asmdef`.
- Ownership: all 13 manifest C# files, existing `Assets/Neoxider/Docs/PropertyAttribute/*.md`, this report and one new uniquely prefixed regression file with metadata. No nested assemblies were present under this source root.
- No Unity/MCP, build/test runner, installation, shared configuration edit, commit or child agent was used. Concurrent workers' files were left intact.

Проверены все 13 исходников. Исправлены проверка Component-ссылок, потеря смешанных значений при множественном выборе, высота вложенных полей и обработка null-типа. Тесты добавлены, но не запускались: запуск и компиляция остаются за координатором.

## Complete source review

All paths in this table are relative to `Assets/Neoxider/Scripts/PropertyAttribute/`.

| File | Review and disposition |
|------|------------------------|
| `ButtonAttribute.cs` | Reviewed method target restriction, constructor/defaults, optional label/width metadata and named `PlayModeOnly` compatibility. Metadata performs no runtime invocation or runtime safety check. No change. |
| `ButtonAttributeDrawer.cs` | Reviewed all reflection discovery, metadata sorting, label/Play Mode helpers, parameter storage/defaults, drawing and invocation. This legacy adapter is not the active method-button rendering path: a method-only attribute has no ordinary SerializedProperty. Retained its public helper behavior; see limitations below. |
| `GUIColorAttribute.cs` | Reviewed RGBA double-to-float conversion, default alpha, read-only color metadata and absence of runtime state/resource ownership. Preserved unclamped values and existing API. |
| `GUIColorAttributeDrawer.cs` | Reviewed nested properties, height contract and global GUI state lifetime. Fixed expanded-field drawing/height and exception-safe color restoration. Replaced the existing inferred local type while changing this method. |
| `InjectAttribute/FindAllInSceneAttribute.cs` | Reviewed default sort mode, mutable metadata, global namespace and existing constructor parameter spelling `sottMode`. Preserved named-argument and namespace compatibility. |
| `InjectAttribute/FindInSceneAttribute.cs` | Reviewed marker-only behavior and its actual `Neo.Editor.ComponentDrawer` consumer. No runtime injection exists in this type. No source change. |
| `InjectAttribute/GetComponentAttribute.cs` | Reviewed default/child-search metadata, global namespace and external editor consumer. No source defect in the declaration. |
| `InjectAttribute/GetComponentsAttribute.cs` | Reviewed child-search option and collection assignment boundary. Collection creation/Undo/search is owned by `Neo.Editor`. No source change. |
| `InjectAttribute/LoadAllFromResourcesAttribute.cs` | Reviewed empty/path metadata, mutable field and resource ownership boundary. The declaration loads and retains no resources itself. No source change. |
| `InjectAttribute/LoadFromResourcesAttribute.cs` | Reviewed optional path and immutable property. Kept empty/null input metadata behavior; editor fallback semantics are now documented accurately. |
| `NeoDocAttribute.cs` | Reviewed null input, trimming/separator normalization, class-only/non-inherited usage and read-only metadata. It does not validate file existence; lookup belongs to `Neo.Editor`. No source change. |
| `RequireInterface.cs` | Reviewed field/multiple-attribute usage, immutable Type metadata and constructor failure behavior. Added a named null-argument check without changing the signature or valid input behavior. |
| `RequireInterfaceDrawer.cs` | Reviewed all reference categories, empty/destroyed references, unsupported properties, mixed selections, assignment timing, diagnostics, GUI state and fallback height. Fixed the defects below and exposed a reusable editor validation operation. |

The assembly definition, existing `PropertyAttributeEditModeTests.cs` and test assembly references were read for context. Relevant `Neo.Editor` assignment and action-rendering paths were read without modification. The `neoxider-tools` module reference was consulted before adding the narrow editor helper: the existing `IsValidProperty` / `IsReferenceValid` primitives were reused rather than introducing another validation framework. Test fixtures follow existing module test conventions.

There are no lifecycle subscriptions, coroutines, owned assets, scene-object metadata references or persistent runtime caches in the attribute declarations. All three UnityEditor drawers remain entirely inside `UNITY_EDITOR` guards. Runtime/editor ID compatibility helpers are unnecessary here: no owned code calls the obsolete ID APIs.

## Demonstrated defects and completed fixes

Evidence below is source-level control-flow evidence; no Unity reproduction or test execution is claimed.

### 1. Medium: Component references bypassed interface validation

**Trigger:** an Object/Component field with `[RequireInterface]` refers to a Component that does not implement the interface. A sibling may implement it, but the referenced Component still does not.

**Observable consequence:** the original `RequireInterfaceDrawer.CheckProperty` only handled GameObjects and ScriptableObjects, so invalid Components remained serialized even though the existing public `IsReferenceValid` helper rejected them.

**Fix:** `RequireInterfaceDrawer.cs:78` adds `ClearInvalidReference(SerializedProperty, Type)`, using both existing validation primitives for every Object category. The standard drawer calls the same path. It clears invalid values without logging or applying the transaction itself; the drawing caller supplies contextual feedback. Valid and empty references remain intact.

**Regression:** six cases cover valid/invalid GameObject, Component and ScriptableObject values, inspect the actual SerializedProperty, apply it and assert the holder's resulting reference. The invalid Component case deliberately has an implementing sibling.

### 2. High: mixed selections could erase valid references

**Trigger:** a multi-object SerializedProperty has different references and its first selected value is an invalid GameObject or ScriptableObject.

**Observable consequence:** the old check read the first value without consulting `hasMultipleDifferentValues`, then assigned null through the shared property. That assignment could clear a different, valid target merely while drawing the Inspector.

**Fix:** `ClearInvalidReference` leaves mixed values untouched. Existing mixed references can be checked individually; the helper never guesses that one selected value represents every target.

**Regression:** two cases reverse the invalid/valid target order and assert both original object identities after applying the multi-object transaction.

### 3. Medium: validation preceded the user's new assignment

**Trigger:** a previously empty/valid field receives an invalid value during `EditorGUI.PropertyField`.

**Observable consequence:** the old `OnGUI` had already completed validation, so it accepted that value until a subsequent draw.

**Fix:** `RequireInterfaceDrawer.cs:35-43` draws first and validates the resulting value within the same normal GUI pass. Diagnostics occur only when a value is actually cleared.

**Coverage limit:** the serialized-state regressions cover the validation operation used by OnGUI. Drag/drop event ordering and Inspector feedback still require central IMGUI verification; the authored tests do not pretend to simulate GUI input.

### 4. Medium: expanded fields lost child controls or layout space

**Trigger:** `[GUIColor]` decorates an expandable serialized class/struct. The RequireInterface fallback for an unsupported expandable field had the same layout issue.

**Observable consequence:** the drawers called the child-excluding PropertyField overload and inherited the default single-line height, leaving expanded content inaccessible or without matching layout space.

**Fix:** both drawers use `includeChildren: true` behavior for drawing and `GetPropertyHeight`. The fallback remains a normal property view for unsupported types.

**Regression:** four cases compare collapsed/expanded drawer heights against an equivalent ordinary serialized property, asserting expanded height exceeds one line. Both drawers are covered.

### 5. Low: null interface type produced an unrelated NullReferenceException

**Trigger:** `new RequireInterface(null)`.

**Observable consequence:** the old constructor dereferenced `requireType.IsInterface`, obscuring which argument was invalid.

**Fix:** `RequireInterface.cs:41` throws `ArgumentNullException(nameof(requireType))`; non-interface inputs still throw ArgumentException.

**Regression:** verifies both exception type and `ParamName`. Two additional validation cases ensure null/non-interface target types do not discard an existing reference. A separate case preserves an empty reference and an unsupported numeric property.

### 6. Low: nested GUI exceptions leaked the field tint

**Trigger:** a nested property drawer throws, including a GUI-exit exception, while the surrounding GUIColor/RequireInterface drawer has replaced `GUI.color`.

**Observable consequence:** the old trailing assignment restoring the color was skipped; subsequent controls could inherit the wrong tint.

**Fix:** both drawers restore the original color in `finally`. RequireInterface also copies the label before adding its tooltip instead of mutating a caller-owned GUIContent.

**Coverage limit:** exception restoration and label ownership were inspected in source. An IMGUI exception-path smoke check remains central; no fabricated GUI test result is reported.

## Regression files

Added `Assets/Neoxider/Tests/Edit/PropertyAttribute/Audit20261023NeoPropertyAttributeRegressionTests.cs` and its unique-GUID `.meta`.

Six test methods expand to **16 NUnit cases**. Assertions observe serialized reference contents, applied holder state, preserved target identity, argument diagnostics and measured expanded layout height. All temporary GameObjects/ScriptableObjects are cleaned up in `finally`, and SerializedObjects are disposed. The existing `Neo.Editor.Tests` asmdef already references `Neo.PropertyAttribute`; no reference/configuration edit is required.

## Public documentation completed

Updated all 12 existing module pages, retaining their paths and metadata:

- `README.md`: canonical index, namespaces/assembly, Inspector processing conditions, runtime C# fallback example and assembly ownership boundary.
- `ButtonAttribute.md`: method workflow, supported main-renderer parameter types, direct calls, PlayModeOnly semantics, fallback-inspector integration and width metadata limitation.
- `ColorAttribute.md`: replaced non-existent Color/ColorEnum examples with the actual GUIColor constructor, numeric RGBA examples, expanded-field behavior and editor/runtime distinction.
- `RequireInterfaceAttribute.md`: actual GameObject-versus-Component semantics, optional references, mixed selection, runtime checks and all three public editor helpers.
- `RequireInterface.md`: removed the erroneous Add Component workflow and generated example-field table; points to the canonical attribute guide.
- Six injection guides: exact constructor options/namespaces, missing/empty assignment rules, Resources fallback semantics and explicit runtime equivalents.
- `InternalTypes.md`: correct drawer/Neo.Editor responsibilities, legacy button helper limits and NeoDoc path normalization/API.

Existing BOM and line-ending conventions were preserved. No DocsEn, folder metadata, package/root documentation or excluded assembly topic was changed.

## Cross-boundary handoff and retained limitations

These are coordinator actions for **Neo.Editor**, not edits to another worker's assembly:

1. **Medium — interface-unaware scene search:** `Editor/PropertyAttribute/ComponentDrawer.cs:90-109` chooses `components[0]` by field type without consulting RequireInterface. An incompatible first candidate can be repeatedly assigned, dirtied and cleared even if another candidate satisfies the interface. The old public guide incorrectly promised filtering; the guides now state the current limitation. Coordinate candidate filtering with the editor owner, including stacked requirements and repeated-search/log behavior.
2. **Low — width metadata ignored by the main action layout:** `Editor/PropertyAttribute/Internal/CustomEditorBase.Actions.cs:309` passes `0f` to DrawGradientButton instead of the ButtonInfo width. Width is retained in the attribute; its current layout limitation is documented. Decide with the editor owner whether to honor it or formally specify stretch layout.
3. **Legacy button adapter:** ButtonAttribute targets methods, so ButtonAttributeDrawer's property-based OnGUI is not the ordinary Inspector entry point. It retains problematic parameter/button rectangle arithmetic and narrow parameter editors, but changing this inactive adapter would not repair the actual method workflow. Both the helper and current main reflection discovery also omit private base-class methods. Prefer an explicit coordinated consolidation/deprecation decision rather than creating another method-action engine in this leaf assembly.

Mixed-reference validation intentionally preserves differing values rather than clearing each target separately. Individual-target validation or a future explicit repair command can address existing invalid members without destructive implicit edits. Reflection/injection consumers in Neo.Editor and GUI rendering were reviewed only as context; they were not independently audited in full here. Source references identify the state observed during this concurrent audit and should be reconciled with the editor worker before implementation.

## Substantial improvement proposal — shared typed interface-reference contract

**Status: proposed, not implemented.**

**Problem:** the attribute describes a reference contract, but its reusable validation is currently editor-only. Runtime callers must repeat GameObject/component/asset resolution rules. Meanwhile the scene injection processor selects by field type without applying that same contract. This splits an important library rule across editor code, runtime gameplay and Inspector convenience, causing the concrete mismatch described above.

**Proposed architecture/public API:** add a small stateless `Neo.InterfaceReference` utility in Neo.PropertyAttribute with additive APIs such as:

```csharp
public static bool IsValid(UnityEngine.Object reference, Type contractType);
public static bool TryResolve<TContract>(UnityEngine.Object reference, out TContract value)
    where TContract : class;
```

It must own only explicit-reference semantics: Unity-null handling, GameObject component lookup and direct Component/ScriptableObject interface assignability. It must not search scenes, retain scene references, introduce a singleton, run reflection-based field injection or depend on Neo.Editor. Specify optional-null validation separately from successful resolution: an empty optional reference can be valid while TryResolve returns false.

Keep existing `RequireInterfaceDrawer.IsReferenceValid` as a compatibility wrapper, including its current invalid-type/null behavior. Keep ClearInvalidReference as the serialized-property adapter. In Neo.Editor, apply all declared interface constraints while selecting candidates, before writing a field. The editor continues to own Undo, serialized transactions, search scope, diagnostics and mixed-selection handling. Runtime C# consumers gain the same typed resolution path without requiring the Inspector or NoCode chains.

**Compatibility/migration:** preserve all existing attribute names, global namespaces, constructor signatures/defaults, public metadata fields and serialized fields. Existing scenes need no migration. Introduce the runtime helper first; then delegate the editor helper with behavior parity tests. Integrate candidate filtering as a coordinated editor change, documenting that selection remains ambiguous among several valid candidates. Reuse Unity's component lookup and existing module checks; avoid a second reflection/injection framework and new dependency edges.

**Acceptance scenarios:**

1. Direct runtime C# resolves a valid GameObject, implementing Component or implementing asset; it rejects a non-implementing Component even with an implementing sibling.
2. Empty/destroyed references have documented optional-validation versus resolution results; invalid contract types have consistent diagnostics.
3. Existing editor helper results and valid serialized assignments remain compatible.
4. Scene search skips an incompatible candidate and selects a compatible one; all stacked RequireInterface constraints participate.
5. No compatible candidate yields one bounded diagnostic and no repeated assign/clear/dirty loop.
6. Mixed selections preserve distinct references; an explicit user repair applies changes through per-target serialized/Undo operations.
7. Player compilation has no UnityEditor dependency; Edit Mode tests verify parity and integration. Both supported Unity version branches are compiled centrally.

**Effort/risk:** approximately 2–3 developer days including runtime/adapter tests and editor integration verification. Runtime API risk is low because it is additive and stateless. Editor risk is medium: candidate order, stacked constraints, Undo and multi-selection need explicit scenarios. No package/asmdef migration should be required if the existing editor-to-attribute dependency is retained.

## Actual verification and remaining risks

Completed read-only/static checks:

- Inventory comparison: all 13 manifest files exist and exactly match the source-root C# inventory; no additional/nested assembly code was silently included.
- Lightweight lexical checks on all 13 source files plus the new test: balanced delimiters/preprocessor guards, explicit local types, no obsolete instance-ID API calls, and editor imports fully guarded. These are syntax-consistency checks, not compilation.
- Public/protected declaration comparison against HEAD: no existing declaration removed or changed; three additive editor declarations (two height overrides and ClearInvalidReference).
- Documentation: all 12 existing pages have balanced fenced code blocks; all 32 local links resolve.
- Strict UTF-8 decoding of 27 scoped package files (13 sources, test, metadata and 12 docs); no occurrence of the project's 21 encoded mojibake markers, replacement characters or C1 controls.
- New test/metadata and rewritten docs have no trailing whitespace. Whole-file source whitespace scans initially exposed existing unchanged XML-comment whitespace; those lines were retained. `git diff --check` verifies additions instead.
- New test filename matches `Audit20261023NeoPropertyAttribute`; its GUID has exactly one match under Assets using a CRLF-aware metadata scan. The first non-CRLF-aware GUID search matched nothing and was corrected; no source mutation was needed.
- Existing test asmdef has the required Neo.PropertyAttribute reference. Final scoped `git diff --check -- <owned paths>` passed with exit 0. The completed report also passed strict UTF-8/encoding-marker, whitespace, fenced-block and 13-entry review-completeness checks; no queued placeholders remain.

**Not run by explicit worker contract:** Unity import/compilation, NUnit/Edit Mode execution, Play Mode, IMGUI drag/drop/rendering, exception-path GUI smoke checks and player builds. Coordinator should run the existing PropertyAttributeEditModeTests together with the new 16 cases, then verify expanded colored fields, mixed Inspector selection, invalid Component drop and color restoration after a nested GUI exit. Compile on Unity 6.0–6.4 and 6.5+ before claiming version compatibility. No branch or test is declared passing from source inspection alone.
