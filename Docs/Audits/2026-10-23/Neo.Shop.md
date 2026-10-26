# Neo.Shop: audit

Status: source audit and scoped fixes complete; Unity compilation and test execution pending coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Shop/Neo.Shop.asmdef`.
- Scope: 23 C# files; nested assemblies excluded.

Проверены все 23 файла сборки. Исправлены откат бесплатных покупок, возвраты валюты, восстановление экипировки и подписки UI. Добавлены 31 регрессионный сценарий и актуальная документация. Проверки в этом проходе статические; Unity и тестовые раннеры не запускались.

## Ownership and complete source review

Read `AGENTS.md`, the entire worker contract, the exact `Neo.Shop` manifest entry, and this report before editing. Source paths below are relative to `Assets/Neoxider/Scripts/Shop/`; every listed file was read completely, including conditional code. Inventory reconciliation found exactly 23 owned C# files and one excluded nested-assembly C# file.

| Owned file | Review evidence / outcome |
| --- | --- |
| `ButtonPrice.cs` | Enum validation, state precedence, optional visual arrays, formatting, null TMP references, Inspector preview callbacks. Integer caption precision remains documented. |
| `Data/ShopBundleData.cs` | Serialized identity, nonnegative purchase price handling, null item slots, single/repeat purchase semantics, asset-only references and currency key. No additional focused edit. |
| `Enums/ShopPurchaseFlow.cs` | All four purchase/selection modes checked against item and bundle entry points; enum values preserved. |
| `Equipment/EquipItemDefinition.cs` | Stable id/category/sprite configuration; no scene-object dependency. Invalid ids handled by the manager. |
| `Equipment/EquipmentManager.cs` | Slotted/state-only persistence, category validation, null catalogs/slots/categories, sprite enable/native sizing, event idempotence. Fixed restore and empty-state defects. |
| `Events/ShopBundleEvent.cs` | Serializable typed UnityEvent contract; no state or resource ownership. |
| `Events/ShopStringEvent.cs` | Serializable stable-id UnityEvent contract; compatibility retained. |
| `InterfaceMoney.cs` | Global-namespace wallet interfaces, result/status semantics, authority versus affordability, optional refund/query capabilities. Existing public contracts preserved. |
| `Money.cs` | Local/capped/overflow deposits, spending, totals, level payout, persistence, startup/reload, registry cleanup, optional Mirror command/RPC/late-join paths. Fixed compensation, numeric validation and initial notification. |
| `Runtime/ShopProfileData.cs` | Ownership, price overrides, duplicate ordering, JSON clone, nullable collections and sanitization. Fixed missing collections and invalid numeric overrides. |
| `Runtime/ShopRuntimePriceEntry.cs` | Value-type serialization, mutable id/price and profile-level validation. No layout changes. |
| `Shop.cs` | Typed/string/legacy APIs; all purchase modes, free/repeat/owned items, bundles, rollback/save failures, selection/preview, currency resolution, delegate ownership, catalog replacement and view events. Fixed free rollback, refunds, clear-selection visuals and wallet-change notification. |
| `ShopCategoryButton.cs` | Button enable/disable listeners, explicit target then shared resolver, ShowAll/category API. No additional demonstrated defect. |
| `ShopCategorySelector.cs` | Wrap-around, selection, startup index, null entries/list, optional icon/text/button references. Fixed null boundaries. |
| `ShopItem.cs` | Item/bundle rebinding, clear, optional text/images/renderers, price/selection order and legacy view id. Added dependent-view notification. |
| `ShopItemData.cs` | Stable id assignment, immutable authoring accessors, currency/category keys and asset-only sprite references. Existing serialization preserved. |
| `ShopListView.cs` | Filters, reused/spawned row ownership, button delegate cache, Shop replacement, disabled setters and cached re-enable. Fixed listener lifecycle. |
| `ShopListViewCategoryBar.cs` | Category rebuild, distinct catalog order, show-all entry, subscription cleanup and adapter boundary. Replaced existing implicit local type. |
| `ShopPurchaseButtonView.cs` | Binding/selection ordering, wallet identity, fractional runtime prices, balance/Shop/row subscriptions, disabled refresh. Fixed stale state and listener ownership. |
| `ShopResolver.cs` | Parent-first then scene fallback, null context, generic component boundary and Unity 6 API usage. No new scene searches introduced. |
| `ShopVariantStateView.cs` | Optional visual groups, typed interface, state events and null-safe existing `SetActiveAll` extension. No additional focused edit. |
| `ShopVariantsPanel.cs` | Ownership/equipped queries, forwarding, buy-then-equip, unequip, reusable view dispatch and source replacement. Fixed old-Shop subscription retention. |
| `TextMoney.cs` | Explicit/key/singleton sources, delayed initialization, reactive modes and disable/re-enable source switching. Fixed inactive subscription and implicit local type. |

`Bridges/ShopInventoryGrantBridge.cs`, its asmdef, and `Docs/Shop/Bridges.md` belong to the nested `Neo.Shop.Bridges` worker and were not edited. Existing tests were read as context only. No scenes, prefabs, samples, package/settings/asmdef files, existing tests, or shared audit configuration were changed.

Read relevant `neoxider-tools` Shop/equipment references in `references/game-systems.md` and `references/modules.md`, plus the actual wallet, delegate-cache, reactive, save-provider and text contracts. Reused `ShopProfileData.Clone`, `SaveProvider`, `UnityEventDelegateCache`, `ShopResolver`, `NeoDiagnostics` and existing reactive/event APIs. The catalog/source search found no debit-compensation API; added a narrowly scoped internal `Money.RefundSpend`.

## Findings and completed fixes

Evidence below is a source-level counterexample and a regression definition, not a claim that the Unity tests were executed. Locations refer to the current owned source root above.

| ID / severity | Trigger, observable consequence, source | Completed fix / regression |
| --- | --- | --- |
| S01 High | A new free item has a throwing purchase listener. `Shop.Buy` previously saved ownership outside `TryCommitPurchase`, propagated the exception and left the item owned despite failed delivery. Source: `Shop.cs:357`. | Free and paid new grants now share rollback/failure reporting. `FailedFreePurchase_RollsBackPersistedOwnershipAndCanBeRetried` checks the runtime profile, persisted JSON, failure event and successful retry. |
| S02 High | Balance 150, cap 100, purchase 50, then grant failure: refund through `Add(50)` restores only 100. With no cap, the refund incorrectly increases `AllMoney`. Applies to items and bundles. Source: `Shop.cs:1049`, `Money.cs:317`. | Internal debit compensation bypasses the reward cap, preserves lifetime income and publishes the refund delta. Four item/bundle and capped/unlimited cases in `FailedPurchase_RestoresBalanceWithoutCountingRefundAsIncome` check balance, totals, profile and failure id. Mirror gets an appended refund operation; clients cannot request it. |
| S03 Medium | `TrySpend(NaN/+Infinity)` classified invalid input as insufficient funds and could forward it from a client. A negative/non-finite level accumulator bypassed deposit validation through `AddLocal`, reducing or corrupting the main balance. Source: `Money.cs:268`, `Money.cs:537`. | Invalid spends return `RejectedInvalidAmount`; affordability rejects infinities. Invalid level payout returns zero, leaves both balances/accumulator intact, and emits the existing gated diagnostic. Two three-case parameterized regressions check statuses and monetary state. |
| S04 High | A malformed remote deposit reaches `CmdMoneyOp` directly, bypassing public `Add` validation; negative/non-finite amounts reached local mutation. Source: `Money.cs:686`. | Command dispatch rejects non-finite payloads, negative deposit payloads and client-originated refunds before mutation/rate limiting. Public invalid-number regressions cover shared boundaries; actual command/RPC validation remains pending Mirror integration testing. |
| S05 Medium | A view subscribes in `OnEnable` before wallet `Start`. Silent save loading changes the balance without notifying it, leaving HUD/affordability stale until another mutation. Source: `Money.cs:362`. | Startup loading notifies current/lifetime reactive properties. `StartupLoad_NotifiesViewsThatSubscribedBeforeWalletStart` observes the initial notification and loaded balances. |
| S06 Medium | Profile collections are null after deserialization/manual data manipulation; `Sanitize` previously left them null, so ownership/override operations dereferenced them. Non-finite override values could also poison purchase/UI comparisons. Source: `Runtime/ShopProfileData.cs:48`, `:133`. | Sanitization initializes missing lists and removes invalid prices; setters ignore invalid prices while preserving valid overrides. Null-collection behavior plus three invalid-price cases exercise usable ownership/price state. |
| S07 Medium | `Select(null)` / `Select("")` clears the saved equipped id but returns before deselecting Shop-owned rows. Source: `Shop.cs:523`. | Clearing selection now propagates the empty selection. `ClearSelection_RemovesHighlightFromShopOwnedViews` asserts the actual `ButtonPrice` state. |
| S08 Medium | A Head save key points to a Feet item. Both visual-slot and state-only restore accepted any catalog id, potentially applying the wrong sprite/category state. Repeating `Unequip` on a configured empty slot also emitted duplicate state-change events. Source: `Equipment/EquipmentManager.cs:178`, `:217`, `:139`. | Restore checks the item's category, empty unequip is idempotent, and null collections/categories plus missing item ids are handled. Two restore cases, one event-idempotence test and one state-only boundary test observe equipped ids, sprite visibility and events. |
| S09 Medium | A category reuses an already-active row. Its purchase view was notified only by Shop events; row binding alone left stale affordability. Subsequent row selection could overwrite its button visual. Source: `ShopItem.cs:40`, `ShopPurchaseButtonView.cs:89`. | Additive `ShopItem.OnViewChanged` fires after binding/clearing/selection. The purchase view subscribes while enabled and reapplies state. `ReusedRow_UpdatesButtonOnBindingAndAfterSelectionVisuals` checks both logical and rendered button states/interactability. |
| S10 Medium | Default wallet changes while the item id stays unchanged. `SetMoneySpendSource` did not notify views; the view's id-only subscription cache also retained the former wallet after refresh. Source: `Shop.cs:693`, `ShopPurchaseButtonView.cs:153`. | Notify `OnShopChanged` and compare the resolved wallet identity as well as the item id. `ChangeWalletForSameItem_RefreshesAndMovesBalanceSubscription` exercises source change and subsequent replacement-wallet mutations. |
| S11 Medium | Runtime price 0.25 rounds to integer caption 0. The purchase button treated it as free and enabled it despite an empty wallet. Source: `ShopPurchaseButtonView.cs:105`. | Buy/Select/affordability use the full float price; integer caption behavior is preserved/documented. `FractionalPrice_IsNotTreatedAsFreeByThePurchaseButton` checks both insufficient and exact balances. |
| S12 Medium | Public setters/refresh calls on disabled list/purchase/text views could reattach listeners. Conversely, a list with `Refresh On Enable = false` lost its cached button actions after disable/enable. Source: `ShopListView.cs:236`, `:360`; `ShopPurchaseButtonView.cs:89`; `TextMoney.cs:115`. | Subscription creation respects enabled state; cached buttons reconnect on enable. Dedicated purchase-view, list setter, cached re-enable and two TextMoney source-switch cases observe button actions, refresh counts and displayed amount. |
| S13 Medium | `ShopListView.SetShop` replaces the source while a variants panel is enabled. The panel kept old purchase handlers and removed handlers from the new source on disable, leaking old behavior. Source: `ShopVariantsPanel.cs:246`. | Track and detach the actual subscribed Shop; synchronize with list refresh/source changes. `VariantsPanel_FollowsShopReplacementAndDetachesTheOldShop` verifies old/new purchase effects and post-disable isolation. |
| S14 Low | A null category entry or null serialized list reaches `CurrentCategoryId` / navigation. Direct indexing dereferenced it. Source: `ShopCategorySelector.cs:51`. | Safe empty result and inert invalid-entry application, with wrap-around preserved. `CategorySelector_NullEntryReportsEmptyAndNavigationStillWraps` asserts boundary and subsequent valid state. |

Also replaced the two existing `var` locals in the owned inventory (`TextMoney`, `ShopListViewCategoryBar`) to meet the repository's explicit-type rule. Existing serialized fields, enum values, public signatures and legacy APIs were retained. Public additions: `ShopItem.OnViewChanged`; internal addition: `Money.RefundSpend`. The appended private network opcode preserves existing operation values.

## New regression assets

- `Assets/Neoxider/Tests/Edit/Shop/Audit20261023NeoShopRegressionTests.cs`: **20 methods / 31 NUnit cases**.
- `Assets/Neoxider/Tests/Edit/Shop/Audit20261023NeoShopMemoryProvider.cs`: isolated save-provider fixture support.
- Both new C# files have unique GUID `.meta` files; no folder metadata or test asmdef changes were needed.

Tests assert wallet balances, lifetime totals, persisted ownership, selection/visual state, subscriptions through observable behavior, category state and failure events. Reflection is confined to serialized setup and explicit lifecycle invocation in EditMode. Each fixture restores the previous SaveProvider and diagnostic setting; it uses no real player save data. The fixture is nonparallel because it temporarily replaces shared provider state. No existing tests were edited.

## Documentation

Updated existing English public pages under the owned `Assets/Neoxider/Docs/Shop/` root:

- `README.md`: canonical entry, typed C# example, scene/NoCode setup, missing component links, corrected test path and regression entry; removed the malformed duplicate index.
- `Shop.md`: currency-resolution behavior, direct-selection contract, free/paid rollback and external side-effect limits, profile repair/finite-price contract, retained legacy API documentation and canonical module link.
- `Money.md`: invalid-input results, level payout, initial reactive notifications and debit-refund semantics.
- `ShopItem.md`, `ShopPurchaseButtonView.md`, `ShopListView.md`: binding notifications, reused-row order, actual-price versus caption behavior, source changes and enabled-state listener ownership.
- `TextMoney.md`, `Equipment/EquipmentManager.md`, `ShopCategorySelector.md`, `ShopVariantsPanel.md`: source lifecycle and restored/null-boundary contracts.

No redundant guide or DocsEn tree was created. Nested-assembly and package/root documentation was left to its owners.

## Substantial improvement proposal — typed transaction core and authoritative purchase completion

**Not implemented in this audit.**

### Problem and evidence

`Shop` currently combines catalog, persistence, wallet lookup, purchase orchestration and scene views. A snapshot can restore its profile and compensate Money, but cannot undo an already completed inventory/equipment grant in a UnityEvent listener. A bundle can grant one external item before a later listener throws. A notification exception can therefore produce both free external goods and a refunded wallet. Reentrant callbacks can also perform another purchase before the outer snapshot is restored.

Pending server authority has no purchase request id/completion contract: the current synchronous Shop entry points return without granting when local confirmation is unavailable. Wallet networking replays operations, while late join synchronizes only the current balance. Reward/set commands also accept requests without object authority, so numeric validation alone does not supply game-specific economy authorization. These limitations need a designed contract rather than additional UnityEvent rollback assumptions.

### Proposed architecture and public API

Introduce a plain C# `ShopRuntime` with `ShopPurchaseResult TryPurchase(ShopPurchaseRequest request)`. A request identifies item/bundle and a unique request id; the result distinguishes confirmed, rejected, pending authority, and compensation failure, carrying the effective price/currency and reason.

Inject a catalog, profile store and wallet resolver. Introduce a debit reservation/receipt contract with idempotent commit/refund. Add optional typed grant participants that validate/prepare before debit, commit the grant, and compensate in reverse order on failure. Keep ordinary purchase notifications after commit; notification exceptions should be reported without reversing an already committed transaction. A per-runtime transaction guard and request-id cache prevent nested or duplicate completion.

Keep `Shop` as the MonoBehaviour authoring/save/view adapter, and keep `ShopListView`, buttons and optional NoCode bridges as consumers of the typed result/events. An opt-in server adapter should validate catalog ownership, effective price and authorized reward sources, then return a request-correlated result plus authoritative wallet/profile snapshot. Clients submit purchase intent; they do not select arbitrary wallet operations for purchase completion.

### Compatibility and migration

Retain existing item ids, profile v1 fields, serialized scene fields, purchase modes, int proxies and current UnityEvents. Introduce the typed runtime as an opt-in path first. Preserve legacy callback-based granting with its documented compensation limits; migrate grants that need atomicity to typed participants. Adapt `Money` through its debit-compensation path and custom wallets through explicit receipts. Make strict missing-currency rejection opt-in so existing keyed-wallet fallback behavior does not silently change. Version any pending-request journal separately and coordinate Inventory bridge migration with its owner.

### Acceptance scenarios

1. Free/paid item and bundle failures restore profile, debit and every participating inventory grant; cap 100 / starting balance 150 remains 150 and lifetime income does not grow.
2. A second bundle participant fails after the first committed: compensation runs in reverse order and the result explicitly reports any compensation failure.
3. A post-commit analytics/UI listener throws: the committed purchase stays committed and the error is observable separately.
4. A nested purchase and a duplicate request id cannot charge or deliver twice; pending authority has an eventual typed success/rejection result.
5. A missing currency in strict mode never charges the default wallet; legacy fallback remains available through the compatibility policy.
6. Host, client-only and late-join clients agree on current balance, totals and ownership after capped spending/refund; a client cannot mint currency by choosing a reward/set operation.
7. The core runs headless EditMode tests without GameObjects, scene searches or private-field setup; existing scene/NoCode workflows keep their serialized data and events.

**Effort/risk:** approximately 5–8 developer days for the core, receipts, adapters, migration docs and regression coverage; another 3–5 days for the request/acknowledgement and authoritative-snapshot integration. Medium-to-high risk is concentrated in legacy callback ordering, custom wallet semantics, persistence failure recovery and Mirror authority/lifecycle. Ship in opt-in stages with the compatibility scenarios above.

## Actual verification and remaining risks

Executed read-only scoped inspection and Python static-consistency checks, plus `git diff --check -- <owned sources, edited docs, new regression assets, report>`:

- Inventory equality: **23/23** owned C# files; the one nested-assembly source excluded.
- Strict UTF-8 decoding, replacement-character/NUL checks and trailing whitespace: **38 scoped files passed**.
- Lexical delimiter balance, `#if`/`#endif` balance and no implicit `var` locals: **25 C# files passed**. This is not a C# compiler or Unity semantic check.
- Baseline declaration scan: **129 serialized field/type pairs and 205 public member names retained**; focused diff review confirmed existing signatures/layout were not intentionally changed.
- Documentation links: **44 local targets resolved**.
- New metadata: **2 unique GUIDs**, with no duplicate in package metadata.
- Regression inventory: **20 methods / 31 cases authored**, zero executed by this worker.
- Scoped `git diff --check`: **exit 0**. Git emitted line-ending conversion notices for two documentation files, without whitespace errors.

No Unity/MCP, build or test runner, install, commit or child agent was started. Verification was performed locally by this worker; no independent QA pass is claimed. Compilation, runtime timing, Mirror weaving and Unity tests remain centrally pending by explicit worker contract.

### Coordinator handoff / cross-boundary actions

- Run `Neo.Editor.Tests.Audit20261023NeoShopRegressionTests`, the existing Shop/Money/Equipment EditMode suites, `ShopPurchasePlayModeTests`, and affected `MoneyNetworkTests` centrally. No new asmdef references are required by the added EditMode files.
- Compile both supported Unity version ranges (6.0–6.4 and 6.5+) and both optional Mirror configurations. Owned code contains no version-sensitive instance-id calls. `Neo.Shop.asmdef` still directly references `Mirror`; verify the no-Mirror assembly closure centrally because shared asmdef edits are excluded from this worker.
- Exercise the new refund RPC on host/client and invalid command payloads on an actual server, including late join and startup persistence. Static preprocessing checks do not establish these paths work under Mirror.
- Coordinate typed/compensatable inventory grants with `Neo.Shop.Bridges` / Inventory owners if the proposal proceeds. Current external callback effects and post-grant selection are outside profile rollback; custom `IMoneyAdd` refund implementations can still fail or clamp.
- Coordinate with the `Neo.Tools.Text` owner on inherited animation lifecycle integration: `TextMoney` and its `SetText` base both declare enable/disable callbacks. This audit verifies amount/subscription behavior; base text tween pause/resume was not executed.
- Remaining compatibility behavior: item/bundle ids must be unique; generated missing ids depend on catalog order; a missing keyed wallet falls back to the default; float prices retain integer captions; `Select` deliberately does not gate ownership. These are documented constraints, not changed silently here.
