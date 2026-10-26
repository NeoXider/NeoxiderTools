# Neo.Shop.Bridges — implementation receipt

Status: **SOURCE-FINAL for this owner; all new acceptance cases UNEXECUTED.**
Target: `demo`, package `11.0.0`. Same original actor/session `neo-audit-1023-b01-06`;
requested model/effort remain `gpt-6.1-sol` / `max`.

## Approved action and result

**SHOP-OBSERVER-REFUND-GRANT / NativeReview-B B-09 (P1).** Applied the approved narrow
commit/notification decision in the bridge integration and documentation. The current `Neo.Shop`
owner's source already contains the required profile-only commit/debit-compensation boundary,
staged bundle IDs, post-commit observer reporting and shared pre-debit Buy/BuyBundle reentry guard.
That source was read as a dependency, not patched or duplicated by this owner.

The bridge deliberately continues using `OnPurchasedId` and `InventoryComponent.AddItemData`.
Accepted inventory precedes `OnGranted`. An exception from that callback reaches Shop's
post-commit observer boundary: it is reported without refunding payment or reverting committed
profile/inventory. Later bundle child dispatches remain eligible. Profile-save failure happens
before purchase delivery events and compensates the debit instead.

The earlier exact-event subscription, destroyed-source replacement/clearing, active-state guard and
Awake-only ShopResolver fallback fixes are preserved. The original nine lifecycle/grant cases were
not rewritten. No reservation service, strict-delivery policy, new scene wiring or assembly
dependency was added. The substantial transaction proposal remains in the historical plan; its
claim that a framework was needed before correcting B-09 is superseded by root's approved 11.0
commit/notification decision. Full atomic delivery remains a separate proposal.

## Exact owned files

- `Assets/Neoxider/Scripts/Shop/Bridges/ShopInventoryGrantBridge.cs`: preserved prior behavioral
  fixes and added `GrantDirect` XML remarks stating accepted-before-feedback behavior, direct-call
  exception propagation and automatic purchase fault reporting. No additional public API or
  serialized-field change in this implementation pass.
- `Assets/Neoxider/Docs/Shop/Bridges.md`: added the deliberate 11.0 event-timing migration,
  profile-save compensation versus post-commit faults, fault receipt fields, continued later bundle
  dispatch, pre-debit nested-purchase rejection, partial/full capacity behavior, per-UnityEvent
  listener/mapping limits, and direct C#/UnityEvent exception semantics.
- `Assets/Neoxider/Tests/Play/Audit20261023NeoShopBridgesPurchaseTests.cs`: new bridge-owned
  fixture with seven concrete purchase acceptance cases. Uses actual Money, Shop, InventoryComponent,
  bridge mappings, persisted ShopProfileData and ShopNotificationFault receipts.
- `Assets/Neoxider/Tests/Play/Audit20261023NeoShopBridgesPurchaseTests.cs.meta`: new file GUID
  `672c9f3f593945928e82715998c23c1d`; no folder or shared assembly metadata changed.
- This receipt. Original audit/plan, other actors' files and shared trackers remain untouched.

## Authored acceptance sources — UNEXECUTED

All cases are in `Audit20261023NeoShopBridgesPurchaseTests`:

1. `PaidPurchase_OnGrantedThrows_KeepsCommittedWalletProfileAndInventory`: starting balance 100,
   price 20, actual accepted inventory 3; confirms saved ownership is already visible inside
   `OnGranted`, then a thrown feedback error leaves balance 80 and ownership/grant committed.
   Asserts exactly one fault receipt with matching purchase/item IDs, `OnPurchasedId` stage and
   error message, plus no purchase-failed notification.
2. `BundlePurchase_OnGrantedThrows_StillDeliversLaterChildAndBundleNotification`: price 30;
   first child's feedback throws after accepting 3, second child still accepts 2, bundle event
   runs once, balance remains 70, and saved item/bundle ownership stays committed.
3. `ProfileSaveFails_RefundsBeforeInventoryDelivery_AndRetryGrantsOnce(false/true)` — two cases:
   a provider decorator throws before writing the actual Shop profile key. It records balance
   80/70 at the failed save, proving the debit occurred, then asserts refund to 100, empty inventory,
   restored runtime/saved ownership, one failure and no notification-fault receipt. The next real
   purchase succeeds with exactly one delivery of each eligible mapped reward.
4. `CapacityRejectsPartOrAll_PurchaseRemainsCommittedWithActualGrant(false/true)` — two cases:
   capacity two accepts 2 of 3 or, when already full, accepts zero; payment/ownership remain
   committed and `OnGranted` reports only successful additions without a fault/refund.
5. `OnGranted_NestedItemAndBundlePurchases_AreRejectedBeforeAnotherDebit`: feedback attempts both
   purchase entry points; only the original 20 is charged, the second item/bundle stay unowned,
   and no second reward appears.

The new fixture calls real purchase APIs on explicitly configured inactive Shop/Money components
with an active bridge; it does not simulate a debit by manually invoking a purchase event.
Existing `Audit20261023NeoShopCommitPlayModeTests` independently covers active scene item/bundle
purchases and a later purchase observer throwing after bridge delivery. Those sources were read,
not edited. Existing nine bridge cases remain authored lifecycle coverage, giving sixteen owned
NUnit cases across the two bridge fixtures; this is a source count, not an execution result.

Reuse/cleanup: the failure decorator delegates ordinary storage to the existing internal
`ShopPurchasePlayModeTests.MemorySaveProvider`. Bundle field reflection is setup only; observable
assertions use public APIs and persisted output. Inventory automatic load/save and wallet persistence
are disabled in the fixture before use. Owned objects are destroyed and the previous SaveProvider
and diagnostics error setting restored in `finally`. No unsupported NUnit isolation attribute is
introduced; execution requires the coordinator's exclusive runner contract.

## Cross-owner dependencies and compatibility

Required `Neo.Shop` APIs are already present in current source:

- `Shop.OnNotificationFault : UnityEvent<ShopNotificationFault>`;
- `Shop.LastNotificationFault` and `ShopNotificationFault.PurchaseId`, `ItemId`, `Observer`, `Message`;
- `Shop.SetMoneySpendSource(GameObject)` and the approved guarded commit/notification behavior.

`Neo.Tests.Play` already references Shop, Bridges, Inventory, Save and Core; no asmdef edit is
requested. The primary Play test owner must retain the currently accessible internal
`ShopPurchasePlayModeTests.MemorySaveProvider` or provide its equivalent when integrating shared
test internals. Existing primary rollback fixtures belong to their owners and must exercise real
profile-save failure rather than demand refunds after observer failures.

Read-only navigation review confirms the Shop README/Shop guide now link `Shop/Bridges.md`, and
the Inventory legacy bridge page now points to it. No cross-owner backlink edit remains required
for this topic at the inspected snapshot.

Compatibility change is intentional: throwing a purchase observer no longer rejects/refunds an
already committed purchase. Direct grant methods still propagate listener exceptions after accepted
inventory changes. Best effort means absent/full inventories, earlier throwing listeners or stopped
mapping loops can leave some rewards undelivered; no automatic replay or strict-capacity guarantee
is claimed. Serialized fields, public bridge methods, component identity, script GUID and sample
layout remain intact. Package/changelog/migration-guide integration remains root-owned.

## Work/evidence boundary and handoff

Read the complete implementation contract/request and historical audit/plan, current owned source,
current Shop commit/reporting APIs, relevant existing regression sources and test references.
Reviewed source changes and dependency signatures. No Unity, compiler, tests, product validators,
installs, services, commits, pushes or child agents were launched. Historical static-check results
are not presented as evidence for these new sources.

There is no pending runtime API dependency preventing source handoff. Root must wait for every
writer, then compile/discover/execute the sixteen bridge-owned cases and relevant Shop/Inventory
fixtures on frozen Unity 6.3/6.6 hosts, including the supported MIRROR configurations. Verify expected
fault logs, exact cleanup, persisted profiles, no unexpected console errors, new GUID uniqueness and
package/docs checks centrally. No supported-version branch or acceptance case is marked passed here.
