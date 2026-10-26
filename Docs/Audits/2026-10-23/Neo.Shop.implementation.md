# Neo.Shop — implementation receipt

Status: **source-final**. Original actor/session: `neo-audit-1023-b06-01`. Branch observed: `demo`; target: `11.0.0`. Existing audit/plan and coherent earlier edits preserved. No shared trackers or historical verification receipts rewritten.

Read the complete `IMPLEMENTATION-CONTRACT.md`, this actor's implementation request, `NativeReview-B-contracts.md`, original audit/plan context and relevant current source. All assigned bounded actions below are implemented in source. The large optional transaction/request framework remains a proposal.

Resumed 2026-10-24 for root-approved `NI7-02/P2` in `NativeImplementationReview-7.md`. Re-read the complete current implementation contract/request and receipt; read NI7-02 and current relevant wallet, Shop consumer, regression and Mirror weaving/dispatch source. Earlier completed patches were retained. This continuation changes only Money admission, owned regression sources, Money documentation and this receipt.

## Approved actions and actual changes

| Action | Source-final change |
| --- | --- |
| `SHOP-OBSERVER-REFUND-GRANT` | `Shop.cs` now compensates only profile mutation/persistence failures. Ownership and automatic BuyAndEquip selection are written before the profile save. After commit, views/ShopChanged/purchase/select/preview stages are isolated observers; their exceptions publish a fault and never refund or erase committed state. Bundle ids and ownership decisions are staged before external callbacks; later child ids and the final bundle notification remain attempted after an earlier observer fault. |
| `SHOP-PURCHASE-REENTRY` | Common `ExecutePurchase` guard surrounds both public string entry points before currency queries/debit or callbacks and releases through `finally`. Typed/int entry points delegate through it. Nested item/item, item/bundle, bundle/item and bundle/bundle calls perform no debit/grant and are not queued. A subsequent independent call is permitted. |
| `SHOP-MONEY-CLIENT-OPERATIONS` | One serialized `_clientMutationAuthority`, default `NetworkAuthorityMode.ServerOnly`, exposed through typed `ClientMutationAuthority`. Command ingress checks `isNetworked`, client opcode 0–6, finite/sign/affordability, named policy and existing `NeoNetworkState.IsAuthorized(gameObject,sender,policy)` before rate budget/mutation/sync. Refund7 and unknown opcode/policy always reject. No HasServerAuthority sender shortcut. Direct public offline/server/host paths retained. Limiter now uses `Time.unscaledTimeAsDouble` and double per-peer timestamps. |
| `NI7-02/P2` | `CanSpend` now validates finite/sign input, affordability, then an actual representable float decrease for a positive cost. Existing authoritative `TrySpend` and Command admission share it, so a zero-effective positive spend rejects before mutation/events/persistence/sync/RPC/budget. The pure-client pending branch remains before affordability/effective-debit checks. Zero-cost behavior, nominal receipts/input-cap semantics and ordinary float rounding remain. No new public API or numeric backend. |
| `SHOP-BRIDGE-NAVIGATION` | Shop README and controller guide link the canonical `Shop/Bridges.md`, including correct parent/active-scene Shop fallback, existing-singleton Inventory fallback and lifecycle semantics. The explicitly assigned legacy Inventory guide path is now a compatibility pointer. Canonical nested-owner Bridges documentation/source was not edited. |
| Text dependency consumption | `TextMoney` overrides the now-landed protected virtual `SetText.OnEnable/OnDisable`, calls the base enable hook and guarantees the base disable hook with `finally`. Wallet subscription fixes from the audit remain in place. |

## Exact files and additive API

Paths below are relative to `Assets/Neoxider/`.

Runtime:

- `Scripts/Shop/Shop.cs` — commit/observer separation, staged notification arguments, common purchase guard, independent observer fault reporting.
- `Scripts/Shop/Events/ShopNotificationFault.cs` **new**, with unique `.meta` generated — immutable runtime payload properties `PurchaseId`, `ItemId`, `Observer`, `Message`.
- Public additions on Shop: `UnityEvent<ShopNotificationFault> OnNotificationFault`, read-only `ShopNotificationFault LastNotificationFault` (null before the first fault). A fault-report listener exception is logged without recursive fault publication.
- `Scripts/Shop/Money.cs` — serialized `_clientMutationAuthority`; public `NetworkAuthorityMode ClientMutationAuthority`; private admission predicate and unscaled double limiter. NI7-02 adds the shared representable-decrease rule in existing `CanSpend`, retaining the client-first `TrySpend` path. Existing opcode values 0–7 retained.
- `Scripts/Shop/TextMoney.cs` — base lifecycle integration.

Regression sources:

- `Tests/Edit/Shop/Audit20261023NeoShopMemoryProvider.cs` — targeted failing string-write injection before storage mutation.
- `Tests/Edit/Shop/Audit20261023NeoShopRegressionTests.cs` — migrated rollback cases to genuine failing profile saves; extended observer/reentry assertions. Current authored inventory: **44 cases**.
- `Tests/Edit/Shop/Audit20261023NeoShopMoneyAuthorityTests.cs` **new + `.meta`** — **35 cases including MIRROR-conditional cases**; policy/default import, direct offline behavior, local admission-unit truth tables and NI7-02 controls described below. The continuation adds 11 cases.
- `Tests/Play/Audit20261023NeoShopCommitPlayModeTests.cs` **new + `.meta`** — **5 cases including MIRROR-conditional limiter cadence**; actual bridge/inventory conservation, native TextMoney lifecycle, paused limiter windows and the NI7-02 price-1 grant rejection. The continuation adds one case.

Documentation: `Docs/Shop/README.md`, `Shop.md`, `Money.md`, `TextMoney.md`; the specifically approved `Docs/Tools/Inventory/ShopInventoryGrantBridge.md` compatibility pointer. No package/settings/asmdef, scene, prefab, sample layout or DocsEn edits were performed by this actor.

## Intentional 11.0 compatibility changes

- A throwing `OnPurchasedId` or other post-commit observer no longer requests a refund. Eligibility failures belong before purchase; profile-save failures still roll back/refund. Automatic selection is already persisted when a successful item's observers run; select notifications retain their later position.
- Legacy bridge delivery remains **best effort**. There is no capacity reservation or grant acknowledgement. A single UnityEvent retains normal exception behavior within its listener batch; subsequent independent stages/bundle child ids still run. Partial/rejected/missing delivery does not reverse a committed shop purchase.
- Nested purchase calls during the active call are ignored, without a deferred queue. Ordinary calls after return keep their synchronous behavior.
- Remote Money operations now default to ServerOnly. Explicit None restores trusted cooperative amount operations; OwnerOnly means this wallet identity's `connectionToClient`, not Money.I/SaveKey/the sender's unrelated player. Helper allowances for trusted null/exact host-local sender remain, after opcode/payload/policy validation.
- This change supplies no correlated purchase/reward request acknowledgement. Client `TrySpend` can still report RequestedServerAuthority for a request later rejected by the server. Shop's existing pending path is not a new request/ack implementation.
- Positive costs now reject as `RejectedInsufficientFunds` when subtraction cannot lower stored Current, even if nominal affordability passes. `100000000f - 1f` is rejected; `100000000f - 5f` may still confirm with stored decrease 8. Amount/LastChange retain nominal input; no exact-delta equality is required. Affordable zero-cost spends keep succeeding. A pure-client `TrySpend(1f)` still returns pending with a stale local balance of either 0 or 100000000; finite/sign input validation still precedes that branch. Shop's existing local affordability precheck is retained and documented.

## Authored acceptance — all UNEXECUTED

1. Real profile-save failure for free/paid item/bundle: old ownership/selection JSON restored, debit refunded above cap, AllMoney unchanged, no purchase/grant observer called; retry succeeds.
2. Faults in VisualAll, ShopChanged, legacy purchase, string purchase or select observer: exact paid balance/profile/selection remain committed; failure event stays absent; fault payload identifies the stage. A throwing fault reporter cannot escape/refund/recurse.
3. First bundle observer changes the source bundle array and throws: staged later ids and final bundle event still run, with the entire profile committed before the first observer.
4. All four nested item/bundle combinations plus nested wallet-balance callback are rejected before another debit. A failure observer throwing after rollback cannot leave the guard held.
5. Real `ShopInventoryGrantBridge` grants before a later purchase observer throws: wallet=150 from 200 with price50, AllMoney=200, exact inventory counts and persisted ownership conserved. Later bundle delivery continues.
6. Actual prefab save/import with the authority token removed must produce ServerOnly, not enum-zero None. The fixture uses a unique temporary prefab and cleans it; no actual asset import was executed during writing.
7. Local admission-unit cases cover op0–6 and each named policy, owner/foreign/unowned/trusted-null context, refund7/unknown op/unknown policy/local-only and invalid payloads; direct offline APIs retain cap/overflow/spend/level behavior.
8. Native TextMoney disable pauses its base tween and subscriptions; enable reads current balance. Private limiter cadence advances for separate peer ids while game time is paused.
9. Direct `CanSpend`/`CanConfirmSpendNow`/`TrySpend`/`Spend`: balance100000000 and positive cost1 reject; result keeps nominal amount and before/after balances. Current/All/Level/LastChange, both stored currency values and all observed wallet/save events remain unchanged.
10. Four MIRROR-conditional local **woven Command-body** denial controls cover remote and trusted-null sender with fresh or pre-existing budget. The zero-effective spend changes neither wallet/persistence/events, sync sentinel, peer dictionary nor trusted timestamp. A rounded effective price5 remains admissible and the next eligible limiter call still has its window. These are local server-body/budget controls, not production-transport evidence.
11. Two MIRROR-conditional stale-client controls make the client active/connecting but not ready, expect the exact dispatch warning, and restore the original static state/ready flag in `finally`. Locally unaffordable balance0 and zero-effective balance100000000 both return RequestedServerAuthority with unchanged wallet/save/events. No packet is sent; this isolates branch order, not request delivery.
12. Four positive controls cover ordinary fractional `10 - 0.1`, rounded `100000000 - 5 = 99999992`, and zero-cost spends at 0/100000000. Assert nominal Amount/LastChange, exact stored balances, unchanged lifetime/level state and persisted result. MIRROR admission must also accept each payload without consuming budget. Existing overflow above a soft cap remains spendable.
13. Actual Shop/Inventory bridge: uncapped AddOverflow100000000 above cap100, item price1 and mapping of three inventory items. Assert CanAfford=false, one ordinary failure, zero grant/purchase/wallet/save events, zero actual inventory, unchanged profile JSON/currency saves and no notification fault. This is authored as native Play source; it has not been run.

## Dependencies and remaining evidence debt

**No source dependency currently blocks this actor.** The protected virtual SetText hooks and existing NetworkAuthorityMode/IsAuthorized APIs are present in current source and consumed without editing their owners' files. Existing Play/Edit asmdef references cover the authored source; no new reference was added. Primary Neo.Editor.Tests has already migrated `ShopPurchaseRollbackTests.cs` to a failing-profile provider and the matching `OnNotificationFault`/log contract; this was observed through source reads, not test execution.

Root must still run frozen-snapshot compilation/import/discovery and acceptance on Unity6.3/6.6, with actual MIRROR-on/off evidence. Missing-field prefab import/default, weaving, native tween timing/TMP capability, refund RPC and late join remain unverified. The authority and limiter unit sources are **not** remote admission evidence: actual dedicated server + two independent clients, production transport, per-op/policy/owner cases, denial conservation including persistence/events/RPC/rate budget and trusted host/null behavior belong to the approved separate-peer integration pass. Direct server/host public paths also require that pass.

NI7-02 is source-final with no cross-owner source dependency. Add its server-current balance100000000/amount1 denial, same-peer next-valid amount5 budget control, fractional positive and stale-replica pending cases to the original separate-peer acceptance matrix. The new Command-body fixture requires actual Mirror weaving and directly invokes generated user code; its sync sentinel and unchanged budgets do not prove remote receipt, dispatch or absence of network replication. The new client control intentionally stops at Mirror's not-ready warning. Root's peer harness/primary test owners retain their files; none was edited here.

Other actors retain canonical Bridges/Inventory/Network/Text source and release migration/package integration. Root should include both the new Shop observer timing and Money pure-client authority migration in its 11.0 release guide. No reservation framework, durable journal, purchase ack or lookup-cache optimization is claimed implemented.

## Operations actually performed

Small cmd.exe/Python source reads, reference enumeration, scoped `git diff` review, branch read, metadata/artifact bookkeeping and scoped edits only. Case counts above enumerate authored attributes, including conditional blocks; they are not discovered or passing tests. **No Unity, compiler, test runner, product checker/validator, installer, service, child actor, commit or push was launched.** Old audit static-check results remain historical and were not relabeled as validation of this new source.

NI7-02 continuation: source/API reads and a scoped diff read were the only inspection operations; regression additions are source only. Current authored inventory is **44 regression + 35 authority + 5 Play = 84 cases** (59 outside MIRROR blocks). No execution/validation result is claimed for this continuation.
