# Neo.Shop: approved implementation request

Original actor: `neo-audit-1023-b06-01`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### SHOP-OBSERVER-REFUND-GRANT (P1; approved-source-correction)

Inventory grant occurs before observer error; Shop refunds/rolls back own profile without undoing inventory. Independently determine minimal commit/notification boundary; do not hide confirmed economy inconsistency behind optional framework.

Root decision: Profile mutation/persistence failure rolls back/refunds; committed purchase notifications isolated/reported without refund, each bundle child continues. Trigger rollback tests with failing Save fake, add postcommit wallet/profile/inventory fault assertions. Preserve best-effort delivery; document major change.

### SHOP-BRIDGE-NAVIGATION (P3; approved-source-correction)

Link canonical Shop/Bridges.md from Shop entry; old Inventory/ShopInventoryGrantBridge.md becomes compatibility pointer; align lifecycle/fallback semantics.

### SHOP-PURCHASE-REENTRY (P1; approved-source-correction)

Nested Buy/BuyBundle inside A callback can succeed then be erased by A rollback leaving debit. Common pre-debit/finally guard all four item/bundle combinations.

### SHOP-MONEY-CLIENT-OPERATIONS (P1; approved-source-correction)

Money accepts authority-free reward/set command with numeric checks only; assess explicit server policy and valid named-authority behavior separately from optional purchase request/ack.

Root decision: Approve one serialized _clientMutationAuthority + typed ClientMutationAuthority existing enum, ServerOnly default. Command validates isNetworked/known0..6 opcode/payload/known policy/IsAuthorized(gameObject,sender,policy) BEFORE budget, denies Refund7; direct offline/server APIs unchanged. ExplicitNone cooperative/OwnerOnly wallet identity migration. Private rate clock unscaled double. No HasServerAuthority sender shortcut. Read NativeReview-B-contracts.md.

## Relevant full native sections

Source: NativeReview-B.md

### B-08 / P1 — nested purchase rollback erases a different successful purchase

Owner: Neo.Shop / `neo-audit-1023-b06-01` (plan reentry path confirmed). `Assets/Neoxider/Scripts/Shop/Shop.cs:357,447,996`, Buy/BuyBundle/TryCommitPurchase. Trigger: start with 200; A costs 50; A's grant listener buys B for 50, then throws only for A. Expected: reject/serialize nested mutation or preserve B with a coherent debit/profile. Actual: B spends and commits, outer A restores its pre-A profile (erasing B) and refunds only A, leaving 150 with B unowned. Smallest repair: common transaction/reentry guard before any debit/user callback, released in finally, with item↔bundle combinations and subsequent-independent-purchase assertions. Large ShopRuntime extraction is optional and must not delay this guard.
Source: NativeReview-B.md

### B-09 / P1 — grant side effects can survive Shop's reported/refunded failure

Owners: Neo.Shop `neo-audit-1023-b06-01`, Neo.Shop.Bridges `neo-audit-1023-b01-06`, Neo.Tools.Inventory `neo-audit-1023-b07-05` only if a future exact reservation/rollback API is selected. Current `Shop.cs:996` catches grant errors, restores profile and compensates debit; `ShopInventoryGrantBridge.cs:142–161` first mutates inventory then invokes OnGranted. Trigger: mapped paid purchase with OnGranted or a later purchase listener throwing. Expected under a failed/refunded result: no unreconciled delivery; actual retained inventory can coexist with refunded money/unowned profile. This source-confirmed inconsistency is already documented as a legacy limitation, not fixed by subscription repairs. First add a cross-boundary wallet/profile/inventory regression; then separate transactional participants from post-commit notifications or provide exact compensated mutations. Full-delivery versus best-effort remains explicit policy; do not silently make all existing bridge grants strict or claim generic UnityEvent side effects can be undone.

**Coordinator decision during this independent review (approved for later original-owner implementation, not implemented now):** 11.0.0 uses the narrow commit/notification boundary. Keep profile mutation/SaveProfile inside debit-compensation scope; stage the item/bundle IDs before callbacks. Only after commit dispatch VisualAll/ShopChanged/purchase events as post-commit observers. Observer faults must be separately observable/logged without refunding committed ownership; notification of later bundle IDs continues. Legacy bridge delivery remains explicitly best effort. No reservation framework is prerequisite. The common pre-debit Buy/BuyBundle reentry guard from B-08 is mandatory.

Tests must retain strong assertions: inject a real failing Save/profile store for rollback/refund, and separately assert exact wallet/profile/inventory state plus a notification-fault receipt after a throwing post-commit observer. Existing newly authored tests that throw OnPurchasedId to demand rollback must migrate to this deliberate major-version contract; do not simply weaken expected balances. Document event timing migration and the remaining best-effort delivery boundary.
Source: NativeReview-B.md

### Neo.Shop — full artifacts, all changed hunks, memory provider and complete 31-case fixture reviewed

Owner/task `neo-audit-1023-b06-01`. Free/paid profile rollback, debit-specific refund (no lifetime-income/cap distortion), finite numeric checks, startup notifications, category validation, precise price affordability and UI binding fixes match current source. Memory provider now implements the actual ISaveProvider API; fixture sets it before constructing wallets/shops and destroys objects before restoring the previous provider/diagnostics. No unsupported NonParallelizable attribute remains in the current fixture; the audit's old nonparallel statement is stale. Event assertions are meaningful, but reflection-based EditMode lifecycle calls do not certify actual Start/enable/tween scheduling.

B-08/B-09 need owner corrections independently of the large transaction proposal. Mirror refund opcode is appended (7), old op values retained; server rejects client refunds/nonfinite deposits. Client balance is initially synchronized and later operations replay; weaving/server-policy/late-join evidence is pending. Public reward/set commands without authority are a documented configurable-game authorization gap, not automatically changed to a universal server-only policy. Request/ack and durable receipts remain proposals.

Aggregate review: UI refresh is event-driven rather than per-frame. However each row resolves currency independently; `Money.cs:112` falls back to FindObjectsByType for an unresolved save key, so many rows sharing one missing key can repeat an identical scene scan during one refresh. Record lookup counts before choosing a pass-scoped cache/registry invalidation policy; preserve fallback and runtime wallet registration. ShopItem binding/selection and Shop events can also refresh the same purchase view repeatedly; batch only identical pass context, without hiding different wallet/item policies. Source lifetimes with SetText base hooks remain a cross-owner execution risk. No public/serialized declaration removal found in changed hunks.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Shop.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Final NI7/NI8 integration handoff

ROOT personally FULL-read and accepts NI7-02/P2 in NativeImplementationReview-7.md. Shared authoritative positive effective-float-decrease admission for CanSpend/TrySpend and server Command before budget/events. Preserve finite/sign/affordability order, zero-cost behavior, effective ordinary fractional amounts and nominal input-cap/float-rounding semantics; do NOT require exact nominal delta equality or new double backend. CRITICAL pure-client TrySpend must still return RequestedServerAuthority BEFORE local affordability/effective-debit checks; its balance may be stale. Add meaningful price1 at Current1e8 Shop grant rejection, direct/server ingress denied/no-budget state/event controls, stale-client pending control, ordinary fractional positive control and rounding docs. Preserve all approved transaction/policy contracts. Source/docs/receipt only; no execution.
