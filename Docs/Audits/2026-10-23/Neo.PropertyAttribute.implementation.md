# Neo.PropertyAttribute: implementation receipt

Status: **source-final**. Date: 2026-10-24. Branch observed: `demo`; target release: `11.0.0`. Original actor: `neo-audit-1023-b04-08`, same audit/planning session. Source-final means the assigned source, regression source and owned documentation are ready for later integration verification; it is not a compilation or test pass.

## Approved scope and disposition

Read `IMPLEMENTATION-CONTRACT.md`, the complete individual implementation request, the original audit/plan and the manifest entry. The independent review assigned no additional confirmed runtime defect to this assembly. Preserved the coherent earlier patch instead of applying it again or creating duplicate regression fixtures.

- **B-14 / P2, boundary documentation complete:** reconciled the owned guides against the current `Neo.Editor` implementation. `ComponentDrawer.AssignComponentFromScene` now calls `FindCompatibleSceneCandidate`, which checks every stacked requirement through `RequireInterfaceDrawer.IsReferenceValid` before writing a reference. Successful assignment records Undo; unmatched fields are not assigned/dirtied and their warning is bounded per target/field until success. This code belongs to Neo.Editor and was read without modification here.
- **Default button stretch retained:** the current action renderer still calls `DrawGradientButton(title, 0f, 22f)`. The owned Button guide accurately describes Width as metadata under that layout. No width redesign, legacy drawer removal or private-base-method discovery expansion was implemented.
- **Substantial improvement retained as a proposal:** the historical audit and plan describe `Neo.InterfaceReference.IsValid` / `TryResolve<TContract>`. No runtime resolver, framework, service or extra dependency was introduced; that proposal is not a prerequisite for B-14.

## Source and API delivered

The following existing owned edits remain intact:

| Exact source path | Delivered behavior |
|---|---|
| `Assets/Neoxider/Scripts/PropertyAttribute/RequireInterface.cs` | Explicit `ArgumentNullException(nameof(requireType))` for a null type; existing interface metadata and constructor signature retained. |
| `Assets/Neoxider/Scripts/PropertyAttribute/RequireInterfaceDrawer.cs` | Uses the common validator for all Object reference categories; validates after the field is drawn; preserves mixed selections; restores GUI color through `finally`; copies the label before changing its tooltip; reserves expanded-property height. |
| `Assets/Neoxider/Scripts/PropertyAttribute/GUIColorAttributeDrawer.cs` | Draws expanded children, reports their normal property height and restores GUI color through `finally`. |

The additive editor-only API already implemented during the audit is:

```csharp
public static bool ClearInvalidReference(SerializedProperty property, Type targetType);
```

It returns true only when an invalid non-mixed Object reference is cleared. Unsupported properties, invalid contract types, empty references and mixed values remain unchanged. It neither logs nor applies the caller's serialized transaction. Existing `IsValidProperty` and `IsReferenceValid` signatures remain available to Neo.Editor/custom inspectors. Both drawer height overrides are additive. No new API or source-body patch was required during this resumption.

## Regression source retained

- `Assets/Neoxider/Tests/Edit/PropertyAttribute/Audit20261023NeoPropertyAttributeRegressionTests.cs`
- `Assets/Neoxider/Tests/Edit/PropertyAttribute/Audit20261023NeoPropertyAttributeRegressionTests.cs.meta`

The complete existing fixture was read: six methods describe **16 authored, UNEXECUTED NUnit cases**. They cover one null-type diagnostic case; six valid/invalid GameObject, Component and ScriptableObject cases; two mixed-target orderings; two invalid contract-type cases; one empty/unsupported-property case; and four collapsed/expanded height cases. Assertions inspect serialized values, applied holder state, object identity, argument diagnostics and measured height. Unity objects are cleaned up in `finally`; SerializedObjects are disposed.

The fixture was not duplicated or rewritten. It relies on the coordinator's isolated/exclusive validation runner and exact cleanup. Existing unrelated legacy tests and shared test configuration were left to their owners.

## Owned documentation completed

All paths below are under `Assets/Neoxider/Docs/PropertyAttribute/`.

**This resumption updated four existing pages:**

- `README.md`: replaced the stale statement that scene lookup ignores interface constraints; linked the scene contract and existing NeoCustomEditor, ComponentDrawer and Editor module pages.
- `FindInSceneAttribute.md`: added a complete stacked-interface authoring example, Component-versus-GameObject semantics, retained explicit references, existing search ordering/scope, Undo, bounded no-match feedback and the custom-Editor/runtime distinction.
- `RequireInterfaceAttribute.md`: describes candidate filtering before assignment, all-requirements semantics and the single-reference scope of B-14; links the editor processor contract.
- `InternalTypes.md`: documents reuse of the existing validation helper by Neo.Editor and adds backlinks to component assignment, NeoCustomEditor and CustomEditorBase.

**Earlier coherent guide edits were retained** in `ButtonAttribute.md`, `ColorAttribute.md`, `FindAllInSceneAttribute.md`, `GetComponentAttribute.md`, `GetComponentsAttribute.md`, `LoadFromResourcesAttribute.md`, `LoadAllFromResourcesAttribute.md` and `RequireInterface.md`. Together with the four pages above, these are the 12 existing owned module pages. Constructor options/namespaces, Resources fallback behavior, direct C# use and Inspector authoring remain documented. No runtime injection or automatic NoCode chain is claimed.

## Compatibility and dependencies

No serialized field, attribute namespace, constructor name/default, prefab, scene, sample layout, package/asmdef or shared tracker was changed in this resumption. Existing global injection namespaces and the `sottMode` constructor spelling remain compatible. The null-type exception change is an intentional diagnostic correction from the original patch. Runtime declarations stay independent of Neo.Editor; drawers remain editor-guarded. No new Unity-version-specific ID calls or guards were added.

**No pending additive API dependency blocks this owner.** B-14 source is present in `Assets/Neoxider/Editor/PropertyAttribute/ComponentDrawer.cs`; its test references are in `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorCollectionTests.cs`, owned by Neo.Editor. The dependency consumed is the existing public `RequireInterfaceDrawer.IsReferenceValid(UnityEngine.Object, Type)`. Collection/local-component injection has not been advertised as receiving the same new single-reference scene filter. Future changes by that owner still need reconciliation in the frozen integration snapshot.

The original audit and plan remain historical evidence and were not rewritten. No optional proposal is represented as completed implementation.

## Actual work and outstanding evidence

During this source phase, performed small cmd/Python reads, manifest/source/doc inspection, reference enumeration, ordinary Git status/diff-stat review, scoped documentation edits and this completion receipt. No Unity, compiler, test process, product checker/validator, installer, service, commit, push or child agent was launched. Earlier audit checks do not certify the later integrated 11.0.0 snapshot.

After **all** source writers finish, coordinator-owned acceptance remains **UNEXECUTED**:

1. Compile the frozen Editor/player dependency graph on the installed Unity 6.3 and 6.6 hosts, covering the supported 6.0–6.4 and 6.5+ branches without claiming untested versions passed.
2. Discover and run `PropertyAttributeEditModeTests` and the new 16-case fixture; confirm real nonempty discovery and applied reference/height assertions.
3. Exercise actual Inspector input: an invalid Component is cleared even when a sibling implements the interface; valid GameObject/Component/asset references survive; repainting either mixed-target ordering preserves both values.
4. Expand and edit a colored nested field without overlap. Trigger a nested GUI exception/exit and observe the original GUI color and caller label afterward. The authored serialized-state tests do not simulate these GUI paths.
5. Verify B-14 integration with an incompatible earlier candidate, a later candidate satisfying every constraint, no-match repetition, later candidate availability and a retained explicit reference. Expect stable assignment, no incompatible assign/clear loop and bounded warnings.
6. Run the centralized package/docs/UTF-8/GUID/sample-path checks after the source barrier. No package or sample rewrite is required by this module.

No owned source work is left waiting for another actor. Remaining compiler, regression and GUI outcomes belong to the later verified integration pass.
