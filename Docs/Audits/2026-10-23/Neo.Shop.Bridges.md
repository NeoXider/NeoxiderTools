# Neo.Shop.Bridges: audit

Status: source audit and scoped fixes complete; Unity compilation and regression execution pending coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Shop/Bridges/Neo.Shop.Bridges.asmdef`.
- Scope: 1/1 inventoried C# file reviewed completely; nested assemblies excluded.

## Scope

- `Assets/Neoxider/Scripts/Shop/Bridges/ShopInventoryGrantBridge.cs` — complete review of every method,
  serialized field, public member and `GrantMapping` field.

Read `AGENTS.md`, `WORKER-CONTRACT.md`, the exact manifest entry and the original report. Ownership is
limited to this source, `Assets/Neoxider/Docs/Shop/Bridges.md`, this report and new tests prefixed
`Audit20261023NeoShopBridges`. No nested assembly source, existing tests or shared configuration edited.

Read-only dependency context: Shop purchase and bundle event emission/rollback; `ShopResolver`;
Inventory initialization, constraints, `AddItemData` and `GetCount`; Singleton registration; existing
bridge purchase/bundle Play Mode tests and resolver tests; Editor/Play test assembly references.

Reuse receipt: consulted `neoxider-tools` references `game-systems.md`, `modules.md`, `tools.md` and the
existing bridge guide. Reused `ShopResolver.ResolveShop`, `ShopStringEvent` and
`InventoryComponent.AddItemData`; no replacement gameplay service or new runtime helper introduced.
Applied the `unity-game-agent` file-only workflow. User instructions reserve Unity, runners and
independent agent QA for the coordinator, so this worker performed source/static review locally.

### Review coverage

| Area | Result |
| --- | --- |
| Lifecycle and event ownership | Fixed enable/disable handling, destroyed source replacement and exact event-instance cleanup. |
| Explicit dependencies/fallback | Serialized Shop/Inventory retained; parent-first Shop resolver reused; singleton fallback still only uses an already registered inventory. |
| Input boundaries | Null/empty IDs return immediately; unknown and case-mismatched IDs do not grant; null item assets and non-positive amounts do not grant. Every matching mapping remains supported. |
| Inventory output | Uses the real inventory API, returns actual added count, emits `OnGranted` only for positive additions; capacity rejection/partial delivery remain best effort. |
| Serialization/public compatibility | All serialized field declarations, public signatures, namespace, component menus, mapping layout and existing script GUID retained. Only nonserialized subscription bookkeeping replaced. |
| Resource ownership | Owns one listener on a captured event; does not create inventories, items, wallets, tasks, coroutines or persistent storage. |
| Version compatibility | No obsolete instance-ID APIs or Unity-version branches. New calls use existing dependency APIs; no asmdef changes. Compilation on supported Unity versions remains pending. |
| Logging/NoCode | No runtime log added. Automatic scene wiring uses typed public grant APIs; explicit UnityEvent/C# grants remain callable while disabled. |
| Reentrancy | Mapping list remains mutable as before; documentation directs callers to configure it outside grant callbacks. Transactional delivery is proposed below. |

## Findings and fixes

### F1 — P1: replacement Shop silently stops granting after the original source is destroyed

**Trigger:** an active bridge subscribes to Shop A; A is destroyed; code calls `SetShop(B)`.
**Observed from source:** the old `Subscribe(false)` returned on Unity's destroyed-object null check,
leaving `_subscribed == true`. `Subscribe(true)` then refused to attach to B. B could complete
purchases without any inventory grant. `SetShop(null)` also returned prematurely because Unity's
overloaded equality considered the destroyed A equal to null.

**Fix:** retain the actual `ShopStringEvent`, detach it independently of the Shop's lifetime and
clear the reference. Compare assigned references with `ReferenceEquals` in `SetShop`. This also
removes the correct listener when the Shop's public event field was replaced before detaching.
Final source: `Subscribe` at line 82 and `SetShop` at line 175.

**Regressions:** `SetShop_AfterSourceDestroyed_GrantsFromReplacementAndDetachesRetainedEvent` verifies
inventory counts 3 -> 6 and no further grant through the retained old event;
`SetShop_NullAfterSourceDestroyed_DetachesRetainedEvent` verifies zero inventory changes and a cleared
source; `DisablingAfterShopEventReplaced_DetachesTheOriginalEvent` verifies the original event cannot
grant and the new event grants after enabling. These tests are authored, not executed.

### F2 — P2: disabled/inactive bridges continue mutating inventories

**Trigger:** disable the component or its GameObject while the source Shop remains alive; alternatively
call `SetShop` on a bridge configured while inactive or initially disabled.
**Observable consequence:** purchase events still add inventory items. The old bridge subscribed in
`Awake`/`SetShop` and detached only in `OnDestroy`, with no disable handling.

**Fix:** subscribe in `OnEnable`, detach in `OnDisable` and retain idempotent destruction cleanup.
`SetShop` attaches only when `isActiveAndEnabled`. The event handler also checks the active state in
case an earlier listener disabled the bridge during the same notification. Direct public grant calls
retain their previous behavior while disabled. Final source: lifecycle methods at lines 59–79,
`HandlePurchasedId` at line 108 and `SetShop` at line 175.

**Regressions:** `DisabledBridge_IgnoresPurchasesAndRebindsOnceWhenEnabled` covers component and
GameObject disabling, swapping sources while disabled, and repeated enable cycles with exact final
counts. `InitiallyDisabledBridge_DoesNotSubscribeUntilEnabled` checks initial activation;
`DisabledDuringPurchaseNotification_DoesNotGrantTheInFlightEvent` checks disabling from an earlier
purchase listener. The direct grant contract test separately verifies intentional manual grants on
a disabled component.

### F3 — P2: advertised scene fallback is absent

**Trigger:** leave Shop unassigned on a bridge outside the Shop hierarchy while an active Shop exists
elsewhere in the scene. The serialized Tooltip promised parent/scene fallback, but `Awake` only called
`GetComponentInParent`, leaving the bridge silently disconnected.

**Fix:** use the module's existing `ShopResolver.ResolveShop(this)` at line 63. Explicit assignment
continues to win; the shared resolver preserves parent precedence before its scene fallback.
**Regression:** `Awake_WithoutParentShop_UsesSceneFallbackAndGrants` checks the resolved reference and
an actual addition of three inventory items after the Shop purchase event.

### D1 — documentation corrections

The summary incorrectly advertised a separate `OnPurchasedBundle` listener. Shop already emits
`OnPurchasedId` for each bundle item; the bridge correctly listens only to that event. Corrected the
summary. The amount Tooltip incorrectly promised runtime clamping to one; corrected it to describe
the existing non-positive-input rejection, preserving runtime behavior.

### Existing contracts protected

`GrantDirect_OnDisabledBridge_ReportsActualCapacityAndIgnoresInvalidRequests` uses the real
InventoryComponent with a total capacity of two. It checks empty/case-mismatched IDs before filling
capacity, a request of five adding exactly two, the exact event item/amount, and no additional
notification or inventory mutation for full capacity, null item or non-positive requests.

New test file: `Assets/Neoxider/Tests/Play/Audit20261023NeoShopBridgesTests.cs`: eight test methods,
nine NUnit cases including the parameterized lifecycle cases. Existing Play Mode tests already
cover full item and bundle purchases; none were edited.

## Documentation

Created the manifest-owned narrow public guide `Assets/Neoxider/Docs/Shop/Bridges.md` because it did
not exist. It documents assembly/namespace boundaries, scene setup, both fallback policies, lifecycle
behavior, C# configuration, all public members, NoCode string-event entry points, bundle behavior,
actual added counts and best-effort delivery limits. Updated this component's `NeoDoc` to that guide.
New guide and test file each have a unique GUID `.meta`; no folder metadata was needed.

**Coordinator documentation action:** link `Shop/Bridges.md` from the Shop module entry. The existing
`Docs/Tools/Inventory/ShopInventoryGrantBridge.md` is outside this worker's documentation ownership
and was left unchanged. Its owner should make it a compatibility pointer to the new narrow guide
or align its fallback/lifecycle text. Shop README/Shop.md also still point to that legacy page.

Кратко: исправлены жизненный цикл подписки, замена уничтоженного магазина и поиск магазина по сцене.
Ручные C# вызовы выдачи сохранены; атомарность покупки и инвентаря требует общего контракта модулей.

## Substantial improvement proposal

### Transactional inventory delivery through an optional purchase participant

**Status: proposal only, not implemented. Requires Neo.Shop and Neo.Tools.Inventory owners.**

**Problem and concrete evidence:** the bridge is a post-purchase event adapter. `GrantDirect` calls
`InventoryComponent.AddItemData` before `OnGranted`; capacity may reject all or part of a request,
yet Shop receives no failure result. Separately, Shop's grant-exception handler snapshots/restores
its profile and refunds currency (`Shop.cs`, grant rollback block around lines 1006–1037), but cannot
reverse an inventory mutation that already happened. A throwing `OnGranted` listener, or a later
purchase listener, can therefore leave inventory granted after a refunded purchase. These are
source-confirmed integration risks, not runtime reproductions performed by this worker. A local
exception-swallowing change would not solve later-listener failures or partial bundle delivery.

**Architecture/public API:** keep the bridge assembly optional and keep Shop free of Inventory
references. Add neutral `IShopPurchaseParticipant` and `IShopPurchaseReservation` contracts to Shop:
`TryPrepare(ShopPurchaseContext, out IShopPurchaseReservation, out ShopPurchaseFailure)`, with explicit
`Commit` and `Rollback` operations. Provide typed registration on Shop; scene adapters register on
enable rather than storing scene references in catalog ScriptableObjects. Implement inventory
preparation in a plain C# service inside the bridge assembly, using Inventory's storage/contracts
and an inventory-owned reservation or version-checked mutation API. The scene bridge provides the
mapping data and target inventory to this service.

Preparation aggregates all matching item/bundle grants and reserves required capacity before money
is charged. Commit produces immutable receipts with purchase ID and requested/added/rejected counts.
Keep inventory change notifications deferred until all participants commit; rollback must release
reservations and undo only changes owned by that purchase. Dispatch presentation events after commit
so listener exceptions cannot turn an already committed reward into a refunded retry. Transaction IDs
must prevent duplicate delivery on retries and integrate with deferred authoritative wallet results.

**Compatibility/migration:** preserve all serialized fields, public bridge methods, event types and
namespace. Existing scenes keep the current best-effort mode. Introduce a serialized delivery policy
defaulting to that mode; opt into `RequireFullDelivery` through C# or Inspector after inventory and
wallet support is available. Keep `GrantDirect` explicitly best effort. No new base-module dependency
on the bridge or inventory and no automatic replay of historical purchases.

**Acceptance scenarios:** full inventory rejects a strict purchase without spending or changing
ownership; partial delivery is allowed only under its explicit policy with an accurate receipt;
duplicate mappings and bundles reserve the combined demand; a later participant failure restores
all purchase-owned changes; exceptions in post-commit UI feedback neither refund nor grant again;
retrying one transaction ID delivers once; local and deferred authoritative wallets use the same
completion protocol; legacy scenes retain their current behavior. Pure C# contract tests cover
reservation/rollback and Unity integration tests cover scene registration and wallet completion.

**Effort/risk:** estimated 4–6 engineering days plus integration QA, assuming inventory reservation
primitives can be added compatibly. High risks are callback reentrancy, persistence ordering, partial
multi-item rollback and asynchronous authority. Start with local atomicity and receipts; schedule
authority integration jointly with the wallet owner. This exceeds a safe one-file audit fix.

## Verification and remaining risks

Executed checks:

- `git diff --check --` with the explicit owned source/docs/tests/report paths: exit 0.
- Scoped Python static consistency check: actual nearest-asmdef inventory equals the manifest (1/1
  C# file); all six owned text/metadata files decode as UTF-8 and contain no trailing whitespace,
  replacement characters or C1 control characters; both C# files contain explicit local types and
  no obsolete Unity ID API calls.
- Compared original/current public declarations and serialized private field declarations against
  `ca198556`: identical. Inspected the complete scoped source diff.
- Checked every relative Markdown link in the new guide and its `NeoDoc` target exists.
- Verified `Neo.Tests.Play` already references `Neo.Shop.Bridges`, `Neo.Shop` and
  `Neo.Tools.Inventory`; no shared asmdef change is required for these tests. The Editor test assembly
  lacks the bridge reference, so regressions deliberately reside under the existing Play assembly.
- Verified nine declared NUnit cases and the required unique filename prefix. Searched package
  metadata for each new GUID: each occurs only in its own new `.meta` file.

**Not executed:** Unity import/compilation, Play Mode, NUnit tests, builds, console checks or independent
agent QA. These are centrally pending by explicit user/worker contract. Static checks do not establish
C# compilation or runtime success. No runners, Unity/MCP, installs, commits, shared settings or child
agents were used.

**Residual risks/actions:** coordinator must compile supported Unity branches and execute the nine
new Play Mode cases plus existing bridge item/bundle purchase cases. Lifecycle behavior intentionally
changes: automatic grants now pause when disabled and missed events are not replayed. Verify that
integration scenes keep required bridges enabled. Scene fallback can select any first active Shop
when several exist; assign the intended source explicitly. Route the transactional delivery issue
and proposal to Shop/Inventory owners, and coordinate the documentation entry/legacy-pointer update.
