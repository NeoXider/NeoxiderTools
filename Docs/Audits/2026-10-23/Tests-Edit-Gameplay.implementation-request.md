# Tests-Edit-Gameplay: approved implementation request

Original actor: `neo-audit-1023-extra-03`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep your prefixed new-file scope. Add or strengthen supplemental positive/conservation acceptance only where useful; all legacy existing fixture repairs go to Neo.Editor.Tests. Do not duplicate corrections or author broad support framework.

## Assigned review actions

No additional confirmed runtime defect was assigned after independent review. Preserve your coherent earlier patch. Finish canonical docs/navigation and factual API/scene/NoCode contracts in owned scope, keep at least one substantial improvement described as a proposal, and write an honest source-final receipt. Do not manufacture a runtime rewrite or duplicate tests.

## Relevant full native sections

Source: NativeReview-B.md

### B-18 / P1 — three legacy Shop fixtures still write or erase the ambient profile store

Original owner Neo.Editor.Tests / `neo-audit-1023-b03-04`; leads verified from Tests-Edit-Gameplay `neo-audit-1023-extra-03`. `Assets/Neoxider/Tests/Edit/Shop/ShopPurchaseRollbackTests.cs:25`, `ShopAffordabilityTests.cs:27` create Shop without SaveScope; disabling Money persistence does not disable Shop.SaveProfile (`Scripts/Shop/Shop.cs:896–900`, default key Shop at line 68). Successful purchase/price changes therefore write the configured provider. `ShopVariantsPanelTests.cs:46,55,82` uses fixed Test_ShopVariantsPanel and deletes it from that provider. Trigger: existing developer profile/sentinel values before these tests. Expected: exact provider identity and data survive success and failure. Actual: overwrite/delete is reachable on ordinary test paths, not just exceptional teardown. Smallest correction: acquire the existing Audit20261023NeoEditorTestsSaveScope before creating any components, destroy them before disposing it in finally; preserve exact wallet/profile assertions. Approved 11.0 Shop boundary also requires updating old rollback tests to throw from the actual profile store rather than a post-commit observer. No new scope framework is prerequisite.
Source: NativeReview-B.md

### Tests-Edit-Gameplay — full artifacts, both complete new fixtures and public guide reviewed

Owner/task `neo-audit-1023-extra-03`; existing-file repairs go to the primary owner above. Four controlled Ability rolls (below/at/above threshold) assert health, damage value, source/target receipts and caster preservation. Seven later-footprint rejection cases assert all touched cells/events unchanged; both notify modes inspect complete committed peer state, and the Dice case requires actual cell/logical change notifications. Fourteen added cases exercise real package methods with inactive owned field roots, controlled IRandomSource and failure-safe cleanup; they are not executions. F05/F06's important weak-assertion gaps are meaningfully supplemented.

Current remaining quality debt is explicit: ProgressionManagerTests.cs:65–68 uses >=2, line 394 permits a range, and ActivatePremium...RetroactiveRewards at 422 never configures earned premium rewards or observes their receipt. Strengthen exact hand-calculated level/perk/reward/idempotence assertions; do not infer a Progression runtime bug from those names. AuditFixesMatch3BoardServiceTests' coroutine-handle assumption remains an Edit-mode skip, not cancellation proof. Cards hover's private nonnull tween is not activity/disposal evidence. Their Play-mode acceptance belongs to original runtime/test owners after source completion. Gameplay guide accurately explains public/scene/NoCode boundaries and Save isolation; composite support environment remains additive proposal, not prerequisite.

## Completion receipt

Write `Docs/Audits/2026-10-23/Tests-Edit-Gameplay.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Oct25 terminal reconciliation of historical failed wrapper

ROOT bounded terminal reconciliation on2026-10-25: latest original CLI metadata remains error/exit1 after historical Windows memory/fork interruption. Your existing source-final receipt has already undergone earlier root/native review, but cannot certify successful terminal completion of that failed step. SAME original session6.1sol/max, inspect exact current owned sources/receipt and original approved request, retain all work. Do NOT restart the audit or expand runtime scope. Verify source-only whether all assigned approved work is present and cross-owner API dependencies are now available; explicitly distinguish historical native failures from UNEXECUTED current source. Finish only actual outstanding owned correction or precise unresolved dependency, update own receipt with current source paths/hashes and honest completion status, then end normally. No product tests/compiler/Unity/checkers/new agents/install/process control/commit/push. Runtime Fixtures extraction is completed; helper files/shared references remain primary-exclusive. Avoid heavy PowerShell under current memory pressure, prefer small cmd/Python reads. Foreign Unity and the protected DocsEn.meta/release Samples~ are untouched.

## Oct25 one bounded recovery after empty launcher exit127

SAME exact original extra-03 session6.1sol/max. Your Oct25 terminal reconciliation launch ended127 before any model answer/source write (wrapper log has only header, empty output, error127), different from historical memory/fork exit1. Current available commit recovered to6.66GiB. One bounded same-session retry, preserve all source. Only reconcile owned completed work and accurate current receipt, as already requested; do NOT redo audit or broaden source. Read your request/current receipt and current owned relevant sources; if approved work/dependencies complete, update own receipt/current fingerprints, explicitly retain historical failed CLI status and UNEXECUTED current native acceptance, then stop normally. No Unity/compiler/tests/checkers/new agents/process control/install/commit/push/shared helper/reference edits. Runtime Fixture extraction is already complete, references remain primary-owned. If launch/provider fails again, report exact error and leave artifacts; do not claim completion or substitute model. Small cmd/Python reads only.
