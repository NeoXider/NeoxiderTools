# Neo.Shop.Bridges: approved implementation request

Original actor: `neo-audit-1023-b01-06`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### SHOP-OBSERVER-REFUND-GRANT (P1; approved-source-correction)

Inventory grant occurs before observer error; Shop refunds/rolls back own profile without undoing inventory. Independently determine minimal commit/notification boundary; do not hide confirmed economy inconsistency behind optional framework.

Root decision: Profile mutation/persistence failure rolls back/refunds; committed purchase notifications isolated/reported without refund, each bundle child continues. Trigger rollback tests with failing Save fake, add postcommit wallet/profile/inventory fault assertions. Preserve best-effort delivery; document major change.

## Relevant full native sections

Source: NativeReview-B.md

### B-09 / P1 — grant side effects can survive Shop's reported/refunded failure

Owners: Neo.Shop `neo-audit-1023-b06-01`, Neo.Shop.Bridges `neo-audit-1023-b01-06`, Neo.Tools.Inventory `neo-audit-1023-b07-05` only if a future exact reservation/rollback API is selected. Current `Shop.cs:996` catches grant errors, restores profile and compensates debit; `ShopInventoryGrantBridge.cs:142–161` first mutates inventory then invokes OnGranted. Trigger: mapped paid purchase with OnGranted or a later purchase listener throwing. Expected under a failed/refunded result: no unreconciled delivery; actual retained inventory can coexist with refunded money/unowned profile. This source-confirmed inconsistency is already documented as a legacy limitation, not fixed by subscription repairs. First add a cross-boundary wallet/profile/inventory regression; then separate transactional participants from post-commit notifications or provide exact compensated mutations. Full-delivery versus best-effort remains explicit policy; do not silently make all existing bridge grants strict or claim generic UnityEvent side effects can be undone.

**Coordinator decision during this independent review (approved for later original-owner implementation, not implemented now):** 11.0.0 uses the narrow commit/notification boundary. Keep profile mutation/SaveProfile inside debit-compensation scope; stage the item/bundle IDs before callbacks. Only after commit dispatch VisualAll/ShopChanged/purchase events as post-commit observers. Observer faults must be separately observable/logged without refunding committed ownership; notification of later bundle IDs continues. Legacy bridge delivery remains explicitly best effort. No reservation framework is prerequisite. The common pre-debit Buy/BuyBundle reentry guard from B-08 is mandatory.

Tests must retain strong assertions: inject a real failing Save/profile store for rollback/refund, and separately assert exact wallet/profile/inventory state plus a notification-fault receipt after a throwing post-commit observer. Existing newly authored tests that throw OnPurchasedId to demand rollback must migrate to this deliberate major-version contract; do not simply weaken expected balances. Document event timing migration and the remaining best-effort delivery boundary.
Source: NativeReview-B.md

### Neo.Shop.Bridges — full artifacts, entire current component and nine new tests reviewed

Owner/task `neo-audit-1023-b01-06`. Exact captured-event detachment survives destroyed Shops and event replacement; ReferenceEquals setter handling and active-state callback guard solve the stated lifecycle triggers. Tests observe actual inventory counts, retained old events, toggled component/hierarchy state and rejected capacity; they construct inactive inventories with auto-load/save disabled before first use, and manually invoke purchase events rather than pretending to prove a full debit transaction. Only the explicit fallback case activates a Shop. No new Update/search loop: Shop fallback is Awake-only; mappings scan per grant. Duplicate/callback-mutated mappings remain a documented caller configuration boundary.

Play assembly currently references Shop/Bridges/Inventory, matching direct visibility needs. The 9 lifecycle cases do not cover B-09; transactional delivery stays a coordinated future API, not a claimed implemented fix. Legacy guide navigation still needs final README/Inventory reconciliation. Actual execution remains pending.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Shop.Bridges.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.
