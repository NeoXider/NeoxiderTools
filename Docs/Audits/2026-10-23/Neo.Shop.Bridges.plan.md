# Neo.Shop.Bridges — implementation plan

Phase 2, same original auditor/session; requested profile: `gpt-6.1-sol`, reasoning `max`.
Planning only. Read the completed [audit](Neo.Shop.Bridges.md), manifest, current owned files and
relevant Shop/Inventory contracts. Integrate against other owners' final revisions.

## Current outcome

The owned file `Assets/Neoxider/Scripts/Shop/Bridges/ShopInventoryGrantBridge.cs` is fully reviewed.
F1–F3 are already fixed: destroyed-Shop replacement/clearing, captured-event cleanup,
enabled/active subscription lifecycle including disabling during notification, and scene fallback
through `ShopResolver.ResolveShop`.

Nine NUnit cases exist in `Assets/Neoxider/Tests/Play/Audit20261023NeoShopBridgesTests.cs`.
`Assets/Neoxider/Docs/Shop/Bridges.md` documents C#, Inspector/UnityEvent setup and delivery limits;
`NeoDoc` points there. Serialized fields/public declarations are preserved. Phase-1 static checks
passed; current source/test/guide hashes match that evidence. Compilation and tests remain **unexecuted**.

## Remaining work and evidence

**P1 verification debt — coordinator.** Run the nine new cases, plus
`Buy_WithBridgeMapping_GrantsToInventory` and
`BuyBundle_WithBridgeMappings_GrantsInventoryForEachItem` in
`Assets/Neoxider/Tests/Play/ShopPurchasePlayModeTests.cs`. Observe replacement counts 3 → 6,
no grants through detached events/disabled bridges, one resumed subscription, scene resolution, and
capacity five → two with one accurate notification. Execute
`Assets/Neoxider/Tests/Edit/Shop/ShopResolverTests.cs` for parent precedence. Correct only demonstrated
failures in the owning files.

**P1 cross-assembly consistency defect, source-confirmed; runtime reproduction pending — Neo.Shop,
Neo.Tools.Inventory and this bridge.** Current `GrantDirect` mutates inventory before `OnGranted`.
Current `Shop.TryCommitPurchase` restores its profile and refunds currency after a grant exception,
without undoing inventory. Reproduce a paid mapped purchase with a throwing `OnGranted` listener:
source ordering predicts a refunded wallet, unowned item and retained inventory reward. A later
purchase listener or bundle child can cause the same inconsistency. Correction requires purchase
participants and reversible inventory mutation; swallowing exceptions cannot solve it. The proposal
addresses opted-in transactions; legacy handlers remain outside its guarantees until migrated.

**P2 confirmed documentation debt — Neo.Shop/Neo.Tools.Inventory owners and coordinator.** Current
`Assets/Neoxider/Docs/Shop/README.md` and `Shop.md` still link the legacy
`Assets/Neoxider/Docs/Tools/Inventory/ShopInventoryGrantBridge.md`, whose setup still specifies only
parent lookup. Replace duplicated setup with a compatibility pointer to `Shop/Bridges.md`.
Acceptance: both entry paths describe the same fallback and lifecycle.

No additional confirmed defect remains exclusively within the owned source. Callback mutation of
`Mappings`, several scene Shops and deferred authority remain risks, not demonstrated bugs.
Keep configuration outside callbacks and references explicit for multiple Shops.

## Substantial proposal: optional transactional delivery

**Not implemented.** Reject purchases before payment when rewards cannot fit; undo only that
purchase's mutations when another participant fails.

Reuse `ShopProfileData`, existing wallet spend/refund paths, `ShopResolver`, `InventoryConstraints`,
`IInventoryStorage`, aggregated/slotted backends and snapshot/instance records. Storage has no
reservation contract; the inspected `InventoryTransferService` handles physical-slot transfers,
not purchase rollback. Preserve module independence.

Proposed public surface:

- Neutral `IShopPurchaseParticipant.TryPrepare(ShopPurchaseContext context,
  out IShopPurchaseReservation reservation, out ShopPurchaseFailure failure)` in `Neo.Shop`.
- `Shop.TryPurchase(ShopPurchaseRequest request, out ShopPurchaseReceipt receipt)` accepts an explicit
  transaction ID and item/bundle request. Register/unregister participants through typed Shop methods;
  reservations expose
  validation, commit, rollback and post-commit notification stages. Immutable receipts carry the
  transaction ID and requested/added/rejected quantities.
- Optional inventory reservation contracts alongside `IInventoryStorage`, rather than adding required
  methods to that existing interface. Preserve exact instance identities/payloads and slot positions.
- Bridge `DeliveryPolicy`: `LegacyBestEffort = 0` and `RequireFullDelivery = 1`, exposed through a typed
  property. Keep `GrantDirect` best effort and existing mapping fields/events intact.

Snapshot mappings once per transaction; combine duplicate demands and bundle children with checked
arithmetic. Prepare capacity before charging; validate again after wallet callbacks. Commit without
external notifications, keep an exact mutation journal, then publish events after all participants
succeed. Listener failures after commit must not refund committed rewards. In strict mode register
the participant instead of the automatic `OnPurchasedId` grant listener, preventing double delivery.
Direct manual grants remain explicit operations outside purchases.

Start with synchronous refundable wallets. Reject unsupported strict wallet flows before spending.
Deferred authority needs an explicit completion/cancellation protocol from the wallet owner;
crash-safe persistence and replay require durable receipts and are a separate extension. Do not
advertise those guarantees for an in-memory transaction.

### Exact proposed files and owners

Paths below are relative to `Assets/Neoxider/`; new files are proposals, not existing implementations.

| Owner | Files and purpose |
| --- | --- |
| Neo.Shop | Modify `Scripts/Shop/Shop.cs` purchase/bundle paths; add `Scripts/Shop/Runtime/ShopPurchaseContracts.cs` and `Scripts/Shop/Runtime/ShopPurchaseCoordinator.cs` for neutral contracts/orchestration. |
| Neo.Tools.Inventory | Add `Scripts/Tools/Inventory/Core/InventoryGrantReservation.cs` and `Scripts/Tools/Inventory/Runtime/InventoryComponent.Reservations.cs`; reuse existing storage and mutation helpers with notifications deferred until publication. |
| Neo.Shop.Bridges | Modify `Scripts/Shop/Bridges/ShopInventoryGrantBridge.cs`; add `Scripts/Shop/Bridges/ShopInventoryGrantService.cs` for plain C# mapping/preparation. |
| Bridge tests / coordinator | Add `Tests/Edit/Shop/Audit20261023NeoShopBridgesTransactionTests.cs` and `Tests/Play/Audit20261023NeoShopBridgesTransactionPlayTests.cs`; coordinator adds the currently missing `Neo.Shop.Bridges` reference to `Tests/Edit/Neo.Editor.Tests.asmdef`. Play already references it. |
| Documentation owners | Update `Docs/Shop/Bridges.md`, `Docs/Shop/Shop.md`, `Docs/Shop/README.md`, `Docs/Tools/Inventory/InventoryComponent.md` and the legacy bridge page named above. |

Base assemblies must not reference the bridge. Preserve serialized names/defaults, APIs, events,
menus and GUIDs; old assets deserialize policy zero. Preserve legacy event semantics outside strict
purchases. Document typed registration, Inspector policy and post-commit events. No custom editor or
ScriptableObject scene references are needed. New package files need unique `.meta` GUIDs. Preserve
the active `Samples`/`Samples~` or imported sample layout; no rename is needed.

## Implementation order, acceptance and stop conditions

1. Coordinator compiles finalized audit revisions and executes the existing regression scope; fix
   real failures first. Reproduce the transaction inconsistency with wallet/profile/inventory assertions.
2. Shop/Inventory owners settle neutral contracts, implement the local reservation/coordinator core,
   then add the bridge service and opt-in scene adapter. No transaction claims before both owners'
   guarantees hold. Complete the documentation pointer correction independently.
3. Minimum EditMode tests: full-capacity rejection before spending; aggregated/slotted reservations;
   duplicate/bundle demand; exact instance rollback; another participant failing; mutation during
   wallet callbacks; duplicate transaction ID within the supported receipt lifetime.
4. PlayMode integration: strict success charges/grants once; rollback restores wallet/profile/inventory;
   throwing presentation callbacks do not refund; source replacement/disable unregisters future purchases; legacy
   behavior and manual grants remain compatible. Verify saved legacy assets need no migration.
5. Coordinator compiles Unity 6.0–6.4 and 6.5+ configurations, runs relevant EditMode/PlayMode tests,
   checks new console errors, metadata, links and active package sample paths. Keep version gates in
   existing shared compatibility helpers; no per-call obsolete-ID branches. Use targeted package/build
   validation only when integration changes packaging or exposes a build risk.

Stop strict-mode rollout if exact rollback, post-commit notification isolation, wallet capability or
idempotency cannot be demonstrated. Report unresolved product choices: partial-delivery policy,
durable receipt lifetime and deferred-authority completion. These do not block verification and
documentation of completed fixes. Estimated proposal effort: 4–6 engineering days plus joint QA,
assuming reusable inventory reservation primitives; storage identity, persistence and authority are
the main risks. No Unity/build/test process, shared configuration edit or implementation is part of
this planning phase.
