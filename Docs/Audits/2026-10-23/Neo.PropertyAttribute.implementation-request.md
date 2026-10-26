# Neo.PropertyAttribute: approved implementation request

Original actor: `neo-audit-1023-b04-08`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

No additional confirmed runtime defect was assigned after independent review. Preserve your coherent earlier patch. Finish canonical docs/navigation and factual API/scene/NoCode contracts in owned scope, keep at least one substantial improvement described as a proposal, and write an honest source-final receipt. Do not manufacture a runtime rewrite or duplicate tests.

## Relevant full native sections

Source: NativeReview-B.md

### B-14 / P2 — scene injection ignores stacked interface requirements

Owner: Neo.Editor / `neo-audit-1023-b03-03`; boundary owner Neo.PropertyAttribute `neo-audit-1023-b04-08`. Current `Assets/Neoxider/Editor/PropertyAttribute/ComponentDrawer.cs:90–109`, AssignComponentFromScene, always selects components[0]. Trigger: `[FindInScene]` Component/GameObject field also has one or several RequireInterface attributes; first candidate is incompatible and a later candidate satisfies all constraints. Expected: stable compatible assignment; actual invalid first assignment is cleared by the drawer and repeated on later Inspector draws, with dirty/log/search churn. Smallest repair: filter candidates through every existing RequireInterfaceDrawer.IsReferenceValid requirement before writing; preserve nonempty explicit references, original search scope and all-constraints semantics. No-match leaves the field unchanged with bounded diagnostics. No new runtime resolver API is prerequisite. Coordinator explicitly selected this bounded correction; default method-button stretch stays unchanged and width redesign is optional.
Source: NativeReview-B.md

### Neo.PropertyAttribute — full artifacts, drawers and 16 regression cases reviewed

Original owner/task: `neo-audit-1023-b04-08`. Read both complete artifacts and recovered the full sections on same-pass validation/height/constructor/color restoration. Inspected all changed runtime/editor-guarded hunks and the complete new fixture. The Component-vs-sibling semantics, mixed-reference preservation, caller-owned SerializedObject transaction and exception-safe GUI color restoration are coherent. Tests inspect applied references, original identities and expanded heights; every temporary Unity object is released in finally and SerializedObjects are disposed. Actual Inspector drag/drop and GUI exception paths remain unexecuted.

Aggregate review: declarations themselves have no Update/search work; repeated component discovery belongs to Neo.Editor's injection consumer, reviewed separately. Proposed InterfaceReference resolver is additive and should share semantics without scene search/cache in the leaf assembly. Existing private-base-method discovery and inactive legacy ButtonAttributeDrawer geometry are documented limits, not automatically confirmed active defects. Candidate selection and width handoffs require reconciliation against current Neo.Editor source rather than accepting stale report line claims. Disposition: owned fixes appear correct pending GUI/compiler checks; cross-owner dispositions follow in Neo.Editor section.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.PropertyAttribute.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.
