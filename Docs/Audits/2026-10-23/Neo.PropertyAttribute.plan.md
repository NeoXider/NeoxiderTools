# Neo.PropertyAttribute: implementation plan

Phase 2, 2026-10-23. Same audit session; requested model `gpt-6.1-sol`, reasoning `max`. This document proposes future implementation; planning changes only this file.

Path abbreviations below are exact repository-relative roots: **P** = `Assets/Neoxider/Scripts/PropertyAttribute/`, **E** = `Assets/Neoxider/Editor/PropertyAttribute/`, **T** = `Assets/Neoxider/Tests/Edit/PropertyAttribute/`, **D** = `Assets/Neoxider/Docs/PropertyAttribute/`.

## Current outcome: completed work

All 13 manifest C# files were reviewed. Changes already present in **P/RequireInterface.cs**, **P/RequireInterfaceDrawer.cs** and **P/GUIColorAttributeDrawer.cs** fix null-argument diagnostics, Component validation, destructive mixed-selection validation, validation timing, expanded-property height/drawing and GUI-color restoration. `ClearInvalidReference(SerializedProperty, Type)` is already implemented; do not introduce it again.

**T/Audit20261023NeoPropertyAttributeRegressionTests.cs** already supplies 16 cases and unique metadata. All 12 existing **D** pages were updated, including runtime/Inspector distinctions and actual injection limitations. Static inventory, declaration compatibility, UTF-8/encoding markers, documentation links, metadata and scoped diff checks passed. Compilation, NUnit execution and IMGUI behavior were not verified. The current relevant source still matches these conclusions; proposals below are not completed work.

## Priorities, owners and remaining evidence

| Priority / kind | Evidence and smallest next action | Owner / observable acceptance |
|---|---|---|
| P1 verification debt | Existing mixed-selection regressions call the serialized helper, not the real Inspector event path. Run both test fixtures and verify repaint plus a new assignment on a mixed selection. | Coordinator, then Neo.PropertyAttribute for demonstrated corrections. Neither target changes merely from selection/repaint; an invalid assignment is cleared without erasing a distinct untouched value. |
| P2 confirmed cross-assembly mismatch | **E/ComponentDrawer.cs**, `AssignComponentFromScene`, still writes `components[0]` without interface checks. An incompatible candidate can be assigned and cleared repeatedly despite another valid candidate. Filter candidates against every declared RequireInterface before assignment, using existing `RequireInterfaceDrawer.IsReferenceValid` initially. Keep unmatched fields unchanged and bound repeated diagnostics. | **Neo.Editor**, independent of the runtime proposal. A compatible candidate persists across draws; no match causes no assign/clear/dirty loop. Preserve existing explicit references and search settings. |
| P2 verification debt | Child height and `finally` restoration are implemented but lack actual GUI-event verification. | Coordinator / Neo.PropertyAttribute. Expand and edit nested values without overlap; after a throwing child drawer or GUI exit, the original GUI color and caller-owned label remain intact. |
| P3 confirmed metadata mismatch, design decision pending | **E/Internal/CustomEditorBase.Actions.cs**, `DrawMethodButtons`, passes `0f` to DrawGradientButton despite storing Width. Existing default width is 120, so simply passing it changes every default button's presentation. | **Neo.Editor** plus coordinator: choose honoring widths or an additive opt-in while preserving default stretch. If widths are honored, configured 88/160 values produce distinct bounded widths; invocation and PlayModeOnly remain unchanged. |

No additional confirmed defect remains in an active owned production path. **P/ButtonAttributeDrawer.cs** has overlapping parameter/button rectangle arithmetic, but its method-only attribute has no ordinary SerializedProperty entry point. Its practical reachability remains unconfirmed. Preserve public helpers; fix geometry only with a demonstrated supported consumer. Private-base-method discovery is a documented limitation, not a newly established regression. Do not remove/deprecate the adapter or expand discovery implicitly in this patch.

## Substantial improvement: one typed interface-reference contract

**Proposal, P2 after verification; approximately 2–3 developer days; medium integration risk.** Runtime consumers currently repeat reference-resolution rules that exist only in an editor helper. Scene search and validation already disagree. Introduce a stateless utility in the existing leaf assembly:

```csharp
public static class InterfaceReference
{
    public static bool IsValid(UnityEngine.Object reference, System.Type contractType);
    public static bool TryResolve<TContract>(UnityEngine.Object reference, out TContract value)
        where TContract : class;
}
```

Specify optional validation separately from resolution: IsValid accepts Unity-null/destroyed optional references, preserving the existing helper's null-first behavior. For a non-null reference, an invalid contract type returns false. TryResolve returns false and null for empty/destroyed references, a non-interface generic argument or an incompatible reference. A GameObject resolves an implementing component; a Component or asset must itself implement the contract. Never return a destroyed managed wrapper through an interface cast.

Reuse the existing IsReferenceValid rules, Unity component lookup, IsValidProperty and ClearInvalidReference. Current module/source and skill-reference searches found no shared typed resolver to adopt. No scene search, cache, singleton, field-injection framework or dependency on Neo.Editor belongs in this utility.

Exact implementation scope:

- Add **P/InterfaceReference.cs** and unique `.meta`; retain the existing assembly definition.
- Delegate **P/RequireInterfaceDrawer.cs** validation to the utility, preserving old signatures, null/invalid-type behavior, mixed-value safeguards and serialized transaction ownership.
- Add **T/Audit20261023NeoPropertyAttributeInterfaceReferenceTests.cs** and `.meta`: assert resolved identity for GameObject/Component/asset, rejection of an incompatible sibling, null/destroyed handling, invalid contracts and editor/runtime parity. Keep existing 16 regressions.
- **Neo.Editor** subsequently switches **E/ComponentDrawer.cs** candidate filtering to the shared runtime validator. Its owner supplies **Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorInterfaceSearchTests.cs** and metadata, covering valid later candidates, stacked constraints, unmatched candidates and retained explicit references. Collection-filter semantics need separate acceptance before extending this single-reference fix.
- Update **D/README.md**, **D/RequireInterfaceAttribute.md**, **D/InternalTypes.md** and **D/FindInSceneAttribute.md** with typed C# resolution and scene setup. Explain that metadata does not run runtime injection. Neo.Editor owns any changes to **Assets/Neoxider/Docs/Editor/**.

Compatibility: additive APIs only; preserve global injection namespaces, constructor parameter spelling/defaults, public fields, serialized names and existing assets. Do not change RequireInterface constructor acceptance of other undocumented Type forms without a reproduction. No component wrapper, ScriptableObject scene reference or new NoCode bridge is needed. No sample content/layout change is planned; retain release `Samples~`, existing package sample paths and imported-sample expectations.

## Implementation order, acceptance and stop conditions

1. Coordinator first reconciles concurrent Neo.Editor work and runs `Neo.Editor.Tests.PropertyAttributeEditModeTests` plus `Neo.Editor.Tests.Audit20261023NeoPropertyAttributeRegressionTests`. Correct only demonstrated failures; do not repeat completed mutations.
2. Neo.Editor implements the bounded scene-filter correction using existing APIs and its focused regressions. It need not wait for the optional public API proposal.
3. Implement the runtime utility and parity tests, then delegate the serialized adapter and editor filter. Update documentation after behavior settles. Optional Inspector regression host: **T/Audit20261023NeoPropertyAttributeInspectorTests.cs** with unique metadata; observe actual serialized state, nested editing and post-exception GUI state rather than screenshots alone.
4. Coordinator validates Editor and player compilation on Unity 6.0–6.4 and 6.5+, then focused EditMode/IMGUI checks. No new version-specific calls are expected; any necessary ID compatibility must reuse NeoDiagnostics.StableId / NeoComponentHealth.StableId, never scattered version guards. Existing asmdef references suffice; any unexpected configuration change belongs to the coordinator.
5. Finish scoped diff/UTF-8/metadata/public-API checks and package/sample-path validation without renaming samples. PlayMode testing is unnecessary for editor-only fixes; add a focused runtime smoke only if resolver lifecycle behavior cannot be established by EditMode object-state tests. Player compilation must confirm no UnityEditor dependency.

Accept only with correct resolved identities, preserved mixed references, stable constrained assignments and verified nested GUI behavior. A compile/test failure stops the affected integration; missing verification stays explicitly pending. Width/default-layout and legacy-adapter removal require a coordinator product decision, but do not block routine fixes or verification. Planning ran no Unity, build or test process and changed no implementation, tests, public docs, audit report or shared configuration.
