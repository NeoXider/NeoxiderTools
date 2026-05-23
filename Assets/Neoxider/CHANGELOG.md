
## [Unreleased]

## [8.5.8] - 2026-05-23

### Fixed
- **Shop / Money UnityEvents:** added `Money.SpendFromButton(float)` as a `void` wrapper around `Spend(float)` so uGUI `Button.onClick` can call money spending from the Inspector while the code API keeps the `bool Spend(float)` result.
- **Condition / method arguments:** `NeoCondition` now reads method-call arguments fresh on every evaluation instead of reusing cached argument arrays, so methods like `Money.CanSpend(100)` reflect the current Inspector argument during Play Mode.
- **Condition / bool compare editor:** bool conditions now persist only `==` / `!=` operators when switching from numeric members to bool members, preventing hidden stale comparison operators.
- **Extensions / PlayerPrefsUtils:** restored legacy CSV string-array saves, added comma validation for string-array values, invariant numeric array parsing, warning-level invalid-data fallback, and strict bool-array validation.

### Changed
- **Package dependencies:** removed hard package dependency on `com.unity.render-pipelines.universal`; URP-specific integrations remain optional instead of forcing URP installation.
- **Package tests layout:** moved package tests from top-level `Assets/Tests` into `Assets/Neoxider/Tests` so test assets live with the Neoxider package.
- **Root documentation:** refreshed root `README.md` and `README_RU.md` as synchronized English/Russian landing pages with current version, install flow, optional URP note, module map, samples, and test layout.

## [8.5.7] - 2026-05-21

### Fixed
- **Docs (all modules):** fixed text artifacts in markdown documentation across `Assets/Neoxider/**/*.md` and normalized encoding to UTF-8 (including `Docs`/`DocsEn` module READMEs).
- **Docs navigation:** rechecked module documentation links after text normalization.


## [8.5.6] - 2026-05-21

### Added
- **Docs (all modules):** added top-level docs indexes: `Docs/index.md`, `Docs/summary.md`, `DocsEn/index.md`, `DocsEn/summary.md`.
- **Module coverage:** verified and confirmed `README.md` exists for all 25 module folders in both `Assets/Neoxider/Docs` and `Assets/Neoxider/DocsEn`.

### Fixed
- **Docs (RU):** fixed broken markdown links and ensured there are no unresolved relative links across `Assets/Neoxider/Docs` (including module readmes and cross-references).
- **Docs (EN):** fixed broken markdown links and ensured there are no unresolved relative links across `Assets/Neoxider/DocsEn`.
- **Docs navigation:** corrected link mapping for card/pool/spine/network/rpg/navigation-related pages and module-README discovery paths.

### Changed
- **Docs quality audit:** completed full pass over all module docs and module README mapping as requested, with link validation summary included in `summary.md` files.

## [8.5.5] - 2026-05-21

### Fixed
- **NeoxiderPages / page transitions**: exclusive page switches keep the outgoing page visible until the incoming page finishes its Forward animation, so animated pages (for example Shop over Menu) no longer reveal an empty backdrop mid-transition.
- **NeoxiderPages / Back animation**: closing a non-popup page now routes through `UIPage.EndActive()`, reliably creates/reuses the DOTween tween, plays it backwards when `BackwardOnly` or `ForwardAndBackward` is selected, and disables the page after the Back animation. Pages without Back animation still disable immediately.
- **NeoxiderPages / previous page restore**: `SwitchToPreviousPage()` enables the previous page at the start of the current page's close animation, preserving the expected backdrop while the current page collapses.
- **NeoxiderPages / popup cleanup**: added `PM.closePopupsOnExclusivePageChange` (default `true`) so opening an exclusive non-popup page closes active popup pages through `UIPage.EndActive()` and plays their Back animation. This fixes Win/Lose popups staying visible when buttons trigger `G.Start()` / `PageGame`.

### Changed
- **NeoxiderPages sample package**: bumped `Samples~/NeoxiderPages/package.json.bak` from `1.2.0` to `1.2.1`.

## [8.5.4] - 2026-05-20

### Fixed
- **NeoxiderPages / exclusive page transitions (complete)**:
  - Other pages hide **after** the incoming show tween finishes (`UIPage.WaitForShowAnimation`, DOTween `WaitForCompletion`), not in the same frame as the open пїЅ fixes empty backdrop when opening animated pages (e.g. Shop over Menu).
  - `UIPage.Popup` pages are **not** deactivated on exclusive switches; popup open still uses `ActivePage` without touching the stack underneath.
  - `HasShowAnimation` / `ShowAnimationDuration` detect show tweens reliably (no false skip when `DOTweenAnimation` was disabled in Inspector).
  - No `StartCoroutine` on inactive page objects (`EndActive` / `SetPageActive(false)` guards) пїЅ fixes `Coroutine couldn't be started... 'Shop Page' is inactive` after `ActivateAll(false)` or fast switches.
- **NeoxiderPages / `SwitchToPreviousPage`**: same show-tween wait before hiding the outgoing page when the restored page animates in.

### Added
- **UIPage**: `WaitForShowAnimation()` IEnumerator for `PM` transition timing.

### Changed
- **Docs (NeoxiderPages)**: [UIPage.md](Docs/NeoxiderPages/UIPage.md) / EN пїЅ popup + end-of-show behaviour on exclusive switches.

## [8.5.3] - 2026-05-20

### Fixed
- **NeoxiderPages / exclusive page transition**: opening a page with a DOTween show animation (e.g. Shop over Menu) no longer hides the previous page in the same frame пїЅ `PM` shows the incoming page first and defers `EndActive()` on other pages until the show tween finishes, so the old page stays visible as a backdrop instead of an empty canvas. `SwitchToPreviousPage` uses the same deferred hide when the restored page has a show animation.

### Added
- **UIPage**: `HasShowAnimation` and `ShowAnimationDuration` пїЅ used by `PM` to time exclusive transitions.

### Changed
- **Docs (NeoxiderPages)**: [UIPage.md](Docs/NeoxiderPages/UIPage.md) and EN mirror describe backdrop behaviour during exclusive switches.

## [8.5.2] - 2026-05-20

### Fixed
- **Shop / default equipped item**: with `Activate Saved Equipped` enabled (`BuyAndEquip` / `EquipOnly`), an empty or invalid `EquippedId` in save no longer leaves the storefront with no active skin пїЅ `TryActivateEquippedOnLoad()` selects the **first** catalog item via `Select(...)`, updates `ShopListView` selection, and persists the choice.

### Changed
- **Docs (Shop)**: `_activateSavedEquipped` tooltip and [Shop.md](Docs/Shop/Shop.md) / EN mirror describe the first-item fallback.

## [8.5.1] - 2026-05-20

### Fixed
- **Shop / empty item Ids**: when several `ShopItemData` assets left `Id` empty, every slot shared the same lookup key пїЅ `IsOwned`, `EquippedId`, and `ShopListView` could mark **all** cells as owned/equipped (e.g. **USED** with no price). `Shop` now backfills unique ids in **`Awake` before `LoadProfile()`** so saves and UI resolve per item; `SetItems(...)` runs the same pass when the catalog changes at runtime.
- **Network / scene NetworkIdentity offline reactivation**: Mirror's `NetworkScenePostProcess` (callback order 1) force-disables every scene `NetworkIdentity` during scene processing so `NetworkServer.SpawnObjects()` can spawn them. In offline scenes (Mirror installed, no session) the spawn step never runs, so any Neo component required to live on a `NetworkIdentity` (e.g. `Money`, `RpgCharacter`, `Selector`, `Counter`, `InteractiveObject`, both `PlayerController{2D,3D}Physics`, all `NeoNetworkComponent` subclasses) stayed disabled forever. Fix is two-tier:
  - **Editor `NeoMirrorScenePostProcess`** (`Editor/Network/`, callback order 100) runs **after** Mirror's post-processor in the same scene-processing pass пїЅ both at build time (the corrected state is baked into the built scene file) and at Play Mode entry (`Awake` sees objects already active). This covers 99% of cases with zero runtime overhead.
  - **Runtime `NeoMirrorSceneReactivator`** (`Scripts/Network/Core/`) listens to `SceneManager.sceneLoaded` as a safety net for dynamic additive scene loads that bypass `[PostProcessScene]`. Opt out via `NeoMirrorSceneReactivator.Enabled = false`.
  - Components opt in by implementing the new `INeoOptionalNetworked` interface (implemented by `NeoNetworkComponent`, `Money`, `PlayerController{2D,3D}Physics`).
  - Removed the old `Money.OnStopClient` / `MoneyMirrorReactivateHost` coroutine workaround, which only covered post-session shutdown and never fired in offline play.

### Added
- **Shop / runtime Id backfill**: `ShopItemData.AssignIdIfEmpty(string)` writes `_id` only while it is empty; `Shop.EnsureMissingItemIds()` derives ids from `nameItem`, then the asset file name, then `{base}_{indexInShopArray}` so duplicate display names in one catalog still get distinct keys.

### Changed
- **Docs (Shop)**: [Shop.md](Docs/Shop/Shop.md), [ShopItemData.md](Docs/Shop/ShopItemData.md) and EN mirrors document editor `OnValidate` fill plus runtime backfill (since **8.5.1**). Recommend setting explicit `Id` on assets before shipping so saves stay stable across builds.

## [8.5.0] - 2026-05-18

### Added
- **Shop / dynamic storefront views**: added `ShopListView` as an optional category/filter layer that owns `ShopItem` creation and reuse. `Shop` can now run as the catalog/purchase controller only (`Auto Spawn Items = false`), while one or more views render categories, owned/unowned filters, and button actions (`Buy`, `Preview`, `Select`).
- **Shop / NoCode category tabs**: added `ShopCategoryButton`, a small Button helper with serialized category string / Show All toggle so category tabs can be configured in the Inspector without manual UnityEvent string parameters.
- **Shop / runtime API**: added `Shop.SetItems(...)`, `SetBundles(...)`, `SetMoneySpendSource(...)`, `SetAutoSpawnItems(...)`, `RefreshVisuals()`, `GetCategories(...)`, and `OnShopChanged` for dynamic catalogs and external views.
- **ShopItem**: now exposes `BoundItemId`, `BoundItemData`, `BoundBundleData`, `LegacyId`, and `Clear()` so spawned/reused views can be inspected and reset safely.
- **Money / currency keys**: `Money` instances can now be resolved by `SaveKey` (`FindBySaveKey`, `TryFindBySaveKey`). `ShopItemData` and `ShopBundleData` use `CurrencyOverrideSaveKey` for asset-safe multi-currency purchases.
- **TextMoney**: added optional money save key selection and `SetMoneySaveKey(string)` so UI can display a specific wallet by key; empty key keeps the previous fallback to explicit `Money Source` then `Money.I`.
- **UI / AnimationFly**: added explicit coordinate-space controls (`Auto`, `World`, `Canvas`, `Screen`) and spawn-space controls (`Auto`, `World`, `Canvas`) so effects can cleanly fly World -> Canvas, Canvas -> Canvas, Canvas -> World, or World -> World. Added NoCode/API helpers `PlayByTypeWorldToCanvas`, `PlayByTypeCanvasToCanvas`, `PlayByTypeCanvasToWorld`, `PlayByTypeWorldToWorld`, plus generic `Play(...)` overloads with explicit spaces and callbacks.
- **NeoxiderPages / UIPage animation mode**: added `UIPageAnimationMode` (`ForwardOnly`, `BackwardOnly`, `ForwardAndBackward`) for explicit show/hide animation behavior. Page `DOTweenAnimation` is forced to unscaled time and `autoKill = false` so page animations can restart reliably during pause/menu flows.
- **Shop**: stable `string Id` on `ShopItemData` (auto-filled from `nameItem` in `OnValidate`, mirroring the [`QuestConfig`](Scripts/Quest/QuestConfig.cs) pattern). Item identity now survives reordering of `_shopItemDatas` in the inspector. New string API: `Shop.Buy(string)`, `BuyBundle(string)`, `Select(string)`, `ShowPreview(string)`, `IsOwned(string)`, `IsBundleOwned(string)`, `GetPrice(string)`, `SetRuntimePrice / ClearRuntimePrice`, `GetItemsInCategory(string)`, `EquippedId`, `PreviewIdString`. New events `OnSelectId`, `OnPurchasedId`, `OnPurchaseFailedId`, `OnPurchasedBundle`, `OnInventoryGranted`.
- **Shop / `ShopProfileData`**: serializable profile (owned item IDs, owned bundle IDs, runtime price overrides, equipped ID) persisted as a single JSON blob through `SaveProvider.SetString` under `_keySave` (default `"Shop"`). Replaces the legacy index-based keys (`Shop0..ShopN`, `ShopEquipped`). Includes `Sanitize()` / `Clone()` and helper APIs (`TryAddOwnedпїЅ`, `SetPriceOverride`, `GetPriceOrDefault`, etc.).
- **Shop / `ShopBundleData`**: ScriptableObject for bundles пїЅ a set of `ShopItemData` sold for a single price. `Shop.BuyBundle(id)` charges the bundle price, adds every contained item to `OwnedItemIds`, grants per-item `InventoryItemData` to the resolved inventory, fires `OnPurchasedBundle`. Bundle-level `CurrencyOverrideSaveKey` and `isSinglePurchase` are supported.
- **Shop / `ShopPurchaseFlow`**: enum (`BuyAndEquip` / `BuyOnly` / `EquipOnly` / `Browse`) replacing legacy bool combinations (`_useSetItem`, `_activateSavedEquipped`) for the top-level mode. Old bools remain serialized for back-compat (`_useSetItem` migrated to `_propagateSelectionVisual` via `FormerlySerializedAs`).
- **Shop / categories**: optional `string Category` on `ShopItemData` and `Shop.GetItemsInCategory(category)` for filtering / tabs.
- **Shop / multi-currency**: optional `CurrencyOverrideSaveKey` on both `ShopItemData` and `ShopBundleData`. Save-key currency beats the shop default (`moneySpendSource` > `Money.I`) and avoids invalid scene GameObject references inside ScriptableObject assets.
- **Shop - Inventory integration (optional)** via the new `ShopInventoryGrantBridge` MonoBehaviour in `Neo.Tools.Inventory`. The bridge listens to `Shop.OnPurchasedId` (which fires per-item, including items unpacked from bundles) and grants the configured `InventoryItemData + amount` mapping to the resolved `InventoryComponent` (explicit field or singleton fallback). Public NoCode entry point: `bridge.GrantForShopItemId(string)`. Public code entry: `bridge.GrantDirect(InventoryItemData, int)`, `bridge.SetShop(...)`, `bridge.SetInventory(...)`. **Direction is reversed on purpose**: `Neo.Tools.Inventory.asmdef` now references `Neo.Shop`, and `Neo.Shop.asmdef` does NOT reference `Neo.Tools.Inventory` пїЅ this avoids the asmdef cycle `Neo.Shop > Neo.Tools.Inventory > Neo.Tools.View > Neo.Tools.Components > Neo.Shop` (Tools.View needs `ScoreManager` from Tools.Components, which needs Money from Shop).
- **Shop / `ShopItem`**: new `Visual(ShopBundleData, int, int)` overload that mirrors the regular item visual using the bundle's name / description / sprite / icon пїЅ bundle UIs can reuse the same `ShopItem` prefabs.
- **Tests (PlayMode)**: rewrote `Assets/Tests/Play/ShopPurchasePlayModeTests.cs` to cover the new string API: free-item legacy `int` proxy, paid-item owned bookkeeping, single-purchase idempotence, runtime price overrides, bundles, per-item currency override, reorder stability of owned ids, `Browse` / `EquipOnly` flows, and inventory grants (single item + bundle).
- **Tests (EditMode)**: new `Assets/Tests/Edit/ShopProfileDataTests.cs` пїЅ JSON round-trip, sanitize/dedupe, price override CRUD, clone independence.
- **Docs**: added [Docs/Shop/ShopBundleData.md](Docs/Shop/ShopBundleData.md) + EN mirror, refreshed [Docs/Shop/Shop.md](Docs/Shop/Shop.md), [Docs/Shop/ShopItemData.md](Docs/Shop/ShopItemData.md), [Docs/Shop/README.md](Docs/Shop/README.md), [DocsEn/Shop/README.md](DocsEn/Shop/README.md), [DocsEn/Shop/Shop.md](DocsEn/Shop/Shop.md), [DocsEn/Shop/ShopItemData.md](DocsEn/Shop/ShopItemData.md) with the new API, optional Inventory section, and the int-API obsolete notice.

### Changed
- **Shop / free purchases**: free unowned items now fire `OnPurchasedId` too, so `ShopInventoryGrantBridge` can grant free consumables/items just like paid purchases.
- **Shop / view safety**: `ShopListView` clears reused `ShopItem` cells before hiding them; `ShopBundleData.Items` returns an empty list instead of null; `ButtonPrice` ignores invalid visual ids; `TextMoney` can switch source via `SetMoneySource(Money)` and re-subscribes safely.
- **UI / AnimationFly**: UI-prefab movement can now use `RectTransform.anchoredPosition`, supports random offsets for start/end/middle, optional rotation, optional `SetAsLastSibling`, and configurable destroy-on-complete. Legacy `Execute(...)` methods remain available and route through the new resolver without applying `Count Multiplier` twice.
- **NeoxiderPages / UIPage**: legacy `_playBackward` / `_onlyPlayBackward` settings migrate to the new `Animation Mode`. Closing animations now restart from the end before playing backward, and delayed deactivation uses realtime waiting instead of scaled `Invoke`.
- **Shop**: heavy rewrite. Public int-id API (`Id`, `PreviewId`, `Buy()`, `Buy(int)`, `ShowPreview(int)`, `Prices`) is preserved as `[Obsolete]` proxies that resolve through `_shopItemDatas[i].Id`. UnityEvent subscriptions to `OnSelect<int>` / `OnPurchased<int>` / `OnPurchaseFailed<int>` keep firing in parallel with the new `пїЅId` string events, so existing scene wiring (Demo, Demoi, ClickerExample) continues to work.
- **Shop / inspector layout**: fields are now grouped under headers `Flow / Items + Bundles / Spawn / Save / Currency / Inventory integration / Advanced / Legacy`; advanced toggles (`_autoSubscribe`, `_changePreviewOnPurchaseFailed`, `_propagateSelectionVisual`) are separated from the high-level `ShopPurchaseFlow` mode selector.

### Removed (Breaking)
- **Shop / save format**: legacy save keys `Shop0`, `Shop1`, пїЅ, `ShopN`, `ShopEquipped` are no longer read. Persisted purchases from older versions do **not** migrate пїЅ on first launch after 8.5.0 the shop starts with an empty `ShopProfileData`. Wipe is intentional (see plan in `Local/plans` and PR notes); migration would have shipped permanent legacy reads.
- **Shop / OnValidate-driven price clobbering**: `Shop.OnValidate` no longer rewrites `_prices` from `ShopItemData.price`. Prices come from `ShopItemData.price` + `ShopProfileData.PriceOverrides`. The `_prices` field stays serialized for scene compatibility and is ignored at runtime.

### Notes
- This release does **not** touch the Mirror-side `Shop.Buy > Money.Spend > CmdMoneyOp(true)` race condition пїЅ that is tracked separately and was deliberately out of scope for this refactor.

## [8.4.2] - 2026-05-17

### Added
- **Samples**: `Samples~/Demo/Scenes/RpgCombatNpcDemo.unity` (with companion `RpgCombatNpcDemo/Assets/` folder) пїЅ self-contained end-to-end RPG combat scene with a `KeyboardMover`-driven Player (HP 100, `PlayerSwordSlash` direct attack), a `MeleeNpc` (HP 80) that drains the player's HP via `RpgContactDamage` when in range, and a `RangedNpc` (HP 60) that fires `ArrowProjectile`s through `RpgAttackController` + `RpgTargetSelector` + `NpcRpgCombatBrain`. Includes character templates, attack definitions/presets, NPC combat preset, and projectile prefab as sibling assets.
- **Tests / PlayMode**: `RpgCombatPlayModeTests` пїЅ covers (1) player direct attack hits NPC and reduces HP by full power, (2) melee NPC `RpgContactDamage` drains player HP while in range, (3) ranged NPC `RpgAttackController` projectile delivery reduces player HP by full power. Test asmdef now references `Neo.Rpg`.

## [8.4.1] - 2026-05-17

### Fixed
- **Movement / PlayerController2DPhysics**: when Mirror is installed but no Mirror session is active, the 2D controller now accepts offline input instead of waiting for `isLocalPlayer`. The same offline authority fallback was applied to `PlayerController3DPhysics`.
- **Editor / GameObject -> Neoxider**: prefab-backed menu entries now instantiate the prefab `GameObject` and find the requested component inside it, instead of falling back to an empty GameObject when the component is not loaded directly from the prefab root.
- **Editor / GameObject -> Neoxider**: menu creation now auto-discovers prefabs under `Prefabs/` that contain the requested component when `CreateFromMenu` has no explicit prefab path; `SpinController` is also explicitly mapped to `Prefabs/Bonus/Slot/SlotUI.prefab`.

## [8.4.0] - 2026-05-17

### Added
- **RPG**: added `RpgCharacter` as the unified RPG facade for resources, stats, level/XP/upgrades, buffs, statuses, regen, profile persistence, NoCode APIs, and Mirror-aware synchronization.
- **RPG data**: added `RpgCharacterTemplate`, progression rules, inline buffs, custom resource/stat IDs, reactive resource/stat bindings, and focused RPG tests.
- **Samples**: added `RpgCharacterQuickDemo` under `Samples~/Demo` for quick Damage/Heal/Stamina/DarkMana/Upgrade smoke checks.

### Changed
- **RPG combat and UI**: attack controllers, no-code actions, condition adapters, HP/level UI, legacy damage bridge, and NPC integration now resolve `RpgCharacter` instead of duplicated `RpgCombatant` / `RpgStatsManager` state.
- **HealthComponent**: remains the low-level resource pool backend and exposes UnityEvent-friendly resource methods used by `RpgCharacter`.
- **Documentation**: RPG docs now point at `RpgCharacter`, keep migration stubs for removed legacy pages, and document the sample scene path.

### Removed
- **RPG legacy duplication**: removed `RpgCombatant` and `RpgStatsManager` runtime scripts and their duplicate HP/level/buff/status logic.

### Fixed
- **NetworkSingleton**: moved `RuntimeInitializeOnLoadMethod` reset out of the generic class into `NetworkSingletonRuntimeReset`, removing Unity's generic runtime-init error.

## [8.3.1] - 2026-05-17

### Fixed
- **NetworkContextActionRelay**: replicated action was lost when the trigger's `NetworkIdentity` had no observers (AOI / early-spawn) пїЅ replaced the observer-driven `[ClientRpc]` path with direct `NetworkConnection.Send`. Every connected client now receives the action regardless of observer state.
- **NetworkContextActionRelay**: two relays on one `NetworkIdentity` (e.g. pickup-self + bonus-on-player) used to resolve to the first relay on receipt пїЅ added `relayComponentIndex` to `NetworkContextActionMessage` and resolution now indexes `NetworkIdentity.NetworkBehaviours[idx]` deterministically.
- **NetworkContextActionRelay**: server-side application + skip-host-local in broadcast removes the host's double-apply that happened with the old RPC echo.
- **Duplicate serialized `_lastCmdTime`**: cleaned up shadowed private fields in `NeoCondition`, `Selector`, `Counter`, `InteractiveObject`, `RandomRange`, `NetworkActionRelay`, `NetworkPropertySync` пїЅ they now rely on the inherited `NeoNetworkComponent.RateLimitCheck()`. Removes Unity warnings `The same field name is serialized multiple timesпїЅ`.

### Added
- **NetworkContextActionRelay**: `Trigger Only For Local Context` (default `true`) пїЅ input-side filter that lets only the client owning the entering collider dispatch a trigger event, eliminating N-fold duplicate sends from every peer's physics simulation.
- **NetworkContextActionRelay**: dedicated custom inspector (`NetworkContextActionRelayEditor`) пїЅ Neoxider-styled, reflection-driven dropdowns for component / method selection (shared `ComponentBindingInspectorShared` helpers with `NeoCondition`), argument field shown conditionally on selected method, collapsible Events block, Diagnostics section with `Verbose Logging` toggle, Editor Preview Target.
- **NetworkContextActionRelay**: structured verbose logs at every hop пїЅ `Trigger`, `Client > Server`, `OnServerMessage`, `DispatchOnServer`, `Broadcast complete`, `Client RECEIVED`, `OnClientMessage`, `ApplyResolved` пїЅ for diagnosing multiplayer dispatch issues.
- **EN docs**: `DocsEn/Network/NetworkContextActionRelay.md` (mirrors RU page).
- **Editor / NeoCustomEditor**: explicit `[CustomEditor]` registrations for `NetworkActionRelay`, `NetworkPropertySync`, `NetworkOwnerFilter`, `NetworkEventDispatcher` so they keep the Neoxider inspector look instead of falling back to Mirror's `NetworkBehaviourInspector`.

### Changed
- **Mult.unity** Trigger Cube (1) now uses two relays: pickup-self (Context = `Self`, `SetActive(false)` on the cube) + bonus-on-player (Context = `EventArgument`, `SetActive(true)` on the Sphere child). The Sphere was also moved out of `First Person Camera` (which is in `NeoNetworkPlayer._localOnlyObjects`) so it stays visible on remote players.
- **NetworkContextActionRelay**: `NetworkContextActionMessage` moved from a nested struct to namespace scope so Mirror's weaver reliably generates Read/Write extensions; `_triggerOnlyForLocalContext` filter checks the event argument rather than the resolved context (works for both `Self` and `EventArgument` modes).

## [8.2.1] - 2026-05-14

### Added
- **NetworkContextActionRelay**: NoCode пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`Trigger` / `Trigger(Collider)`): пїЅпїЅ `netId` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ runtime-пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (пїЅпїЅпїЅ/пїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ); bridge-`UnityEvent`; scope пїЅпїЅпїЅ пїЅ `NetworkActionRelay`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `InvokeComponentMethod` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Core>Tools.
- **NoCode Multiplayer**: `NetworkAuthorityMode` (`None`, `OwnerOnly`, `ServerOnly`) with sender-based validation for scene objects.
- **NeoNetworkManager**: `Scene Player Template` mode for NoCode scene-authored players; the scene object is disabled and spawned as network copies through a stable Mirror spawn handler.
- **Selector**: network authority mode, SyncVar late-join state for fill mode/excluded indices/active snapshot, and regression coverage for host duplicate events.
- **Editor**: replicated UnityEvents and replicated reactive values are highlighted when `isNetworked` is enabled.
- **Tests**: coverage for `InteractiveObject`, `NetworkEventDispatcher`, `NetworkActionRelay`, **`NetworkContextActionRelay`**, authority filtering, offline fallback, and reactive network bridge updates.

### Changed
- **пїЅпїЅпїЅпїЅпїЅ `Mult.unity`**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Trigger Cube (1)` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `NetworkContextActionRelay.Trigger(Collider)` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `SetActive` пїЅпїЅ `Sphere` пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `Sphere` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ runtime-пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ.
- **NoCode Multiplayer**: default authority is `None`; components keep offline/single-player behavior unless `isNetworked` and a network session are active.
- **NoCode Multiplayer**: non-singleton networked NoCode components now inherit from `NeoNetworkComponent`; singleton managers inherit through `NetworkSingleton<T>` without per-component class-level `#if MIRROR`.
- **NetworkActionRelay**: `OthersOnly` uses `TargetRpc` to exclude the sender correctly; `ServerOnly` no longer sends RPC.

### Fixed
- **InteractiveObject**: keyboard interaction in `ViewOrMouse` mode no longer falls back to distance-only activation when the look source is missing; pressing `E` requires the look ray to hit the object collider.
- **InteractiveObject / NetworkEventDispatcher**: host/server path runs before pure-client command path, preventing host duplicate/missed replication issues.
- **NetworkEventDispatcher**: authority check now uses the Mirror `sender` connection instead of local `HasAuthority(gameObject)`.

## [8.2.0] - 2026-05-10

пїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Added
- **Bonus / Slot** пїЅ `PaylineLineGeometry` (пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ); `WinLineRendererPlayback` + пїЅпїЅпїЅпїЅ **`SpinController`** пїЅWin line (optional LineRenderer)пїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ).

### Changed
- **Bonus / Slot** пїЅ `SpinController`: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ API пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`Rows`, `ActivePaylineCount`, `VisibleWindowRows`, `BetSelectionIndex`, `ConfigureSlotRuntime`, `WinLinePlayback`, `CurrentSpinPrice`, `GetRuntimeSnapshot` / `SpinRuntimeSnapshot`); `CheckSpin`: `LinesDataAsset`, `SpritesMultiplierData`, `SetSequenceLength`, `SetFallbackPaylineWindowRows`.
- **Bonus / Slot** пїЅ `CheckSpin`: fallback пїЅпїЅпїЅ `Lines Data` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (`Fallback Window Row Min` / `Max`, **?1** = пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ 0пїЅ2 пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ 3); пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅ пїЅ `Docs/Bonus/Slot/CheckSpin.md`; пїЅпїЅпїЅпїЅпїЅпїЅ `SlotUI`.
- **Bonus / Slot** пїЅ `WinLineRendererPlayback`: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (`AccentGlow`, `SolidFlat`, `LinearGradient`, пїЅпїЅпїЅпїЅ `Gradient`); пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ).
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅ `Docs/Bonus/Slot/SpinController.md` (Payline API, `SpinRuntimeSnapshot`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, Win Line Playback), `Docs/Bonus/Slot/CheckSpin.md` (`SequenceLength`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ), `Docs/Bonus/Slot/README.md`; `DocsEn/Bonus/Slot/SpinController.md` (`SpinRuntimeSnapshot`, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ), `DocsEn/Bonus/CheckSpin.md` (API пїЅпїЅпїЅпїЅпїЅпїЅпїЅ); пїЅпїЅпїЅпїЅпїЅ `Docs/Bonus.md`; `DocsEn/Bonus/README.md`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `Docs/README.md` (`v8.2.0`); пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `README.md` пїЅ `Assets/Neoxider/README.md` (badge пїЅпїЅпїЅпїЅпїЅпїЅ); `PROJECT_SUMMARY.md` (`8.2.0`).

---

## [8.1.0] - 2026-05-08

пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, NoCode-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Added
- **NeoNetworkComponent** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `isNetworked`, `RateLimitCheck()`, `ApplyNetworkState()`, `ShouldDispatchToServer()`, `ShouldBroadcastRpc()`. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ boilerplate пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **NetworkPropertySync** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ NoCode пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Reflection. пїЅпїЅпїЅпїЅ: Float/Int/Bool/String/Vector3. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: ServerToClients/OwnerToServer. Rate-limiting, threshold, SyncVar late-join.
- **NetworkActionRelay** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ NoCode broadcast UnityEvent пїЅпїЅ пїЅпїЅпїЅпїЅ (void/float/string). пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ scope: `AllClients`, `ServerOnly`, `OthersOnly`. Rate-limiting. Offline fallback.
- **NetworkOwnerFilter** пїЅ NoCode пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (`LocalPlayerOnly`, `ServerOnly`, `Everyone`). Offline fallback (пїЅпїЅпїЅпїЅпїЅпїЅ allowed).
- **NeoNetworkDiscovery** пїЅ NoCode пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ Mirror `NetworkDiscovery` пїЅпїЅпїЅ LAN-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, `ConnectToFirstServer()`.
- **NeoLobbyManager** пїЅ NoCode пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ Mirror `NetworkRoomManager`. пїЅпїЅпїЅпїЅпїЅ пїЅ ready-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, `MinPlayersToStart`, UnityEvents пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ lifecycle.
- **NeoLobbyPlayer** пїЅ NoCode пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ Mirror `NetworkRoomPlayer`. `ToggleReady()`, `SetReady(bool)`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnReadyChanged`, `OnBecameLocalPlayer`.
- **пїЅпїЅпїЅпїЅпїЅ (PlayMode)**: `NetworkActionRelayTests` (6 пїЅпїЅпїЅпїЅпїЅпїЅ), `NetworkOwnerFilterTests` (3 пїЅпїЅпїЅпїЅпїЅ), `BonusPlayModeTests` (пїЅпїЅпїЅпїЅ `Row` + `WheelFortune`).
- **пїЅпїЅпїЅпїЅпїЅ (Edit Mode)**: `CheckSpinTests` (пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ `ResolveSectorIndex`).
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ**: `NeoNetworkComponent.md`, `NetworkPropertySync.md`, `Lobby.md`, `NetworkActionRelay.md`, `NetworkOwnerFilter.md` пїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ DOCUMENTATION.md (пїЅпїЅпїЅ пїЅпїЅпїЅ / пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ / пїЅпїЅпїЅпїЅ / пїЅпїЅпїЅпїЅпїЅпїЅ / пїЅпїЅпїЅпїЅпїЅпїЅпїЅ / пїЅпїЅпїЅпїЅпїЅпїЅпїЅ / пїЅпїЅ. пїЅпїЅпїЅпїЅпїЅ).
- **NoCode_Network_Spec**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ 8пїЅ11 (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, Late-Join SyncVar, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, NeoNetworkComponent).
- **Multiplayer_Guide**: пїЅпїЅпїЅпїЅпїЅпїЅ 5 пїЅNoCode пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ 12 пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Changed
- **Bonus / WheelFortune** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅ Legacy/Obsolete; пїЅпїЅпїЅпїЅпїЅ `WheelFortuneImproved` пїЅ пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅ `WheelFortuneNew`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `WheelFortune.ResolveSectorIndex` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- **Bonus / Slot** пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`Row.Spin` + пїЅпїЅпїЅпїЅпїЅпїЅпїЅ id); `CheckSpin` пїЅ fallback-пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `LinesData`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ null пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; `SpinController.ChanceWin`, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ 1; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `Start`.
- **Money** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: 12 пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Cmd/Rpc пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (6 пїЅпїЅпїЅ) пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `MoneyOp` enum + пїЅпїЅпїЅпїЅпїЅпїЅ `CmdMoneyOp` + пїЅпїЅпїЅпїЅпїЅпїЅ `RpcMoneyOp` + `ExecuteOp` switch. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `CanSpend()`.

### Security (P0)
- **Counter**: `[SyncVar] _syncValue`, rate-limiting (50ms), `NetworkConnectionToClient sender`.
- **Money**: `[SyncVar] _syncCurrentMoney`, rate-limiting, `CanSpend()` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, `OnStartClient()` late-join.
- **Selector**: `[SyncVar] _syncIndex` + `_syncFillMode` + `_syncDeactivateNonSelected`, rate-limiting, `OnStartClient()` late-join.
- **NeoCondition**: `[SyncVar] _syncResult`, rate-limiting (50ms), `OnStartClient()` late-join. пїЅпїЅпїЅпїЅпїЅ `ConditionAuthority` enum (`ServerRevalidate` / `TrustClient`).

### Fixed
- **[P0] NeoCondition: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ bool пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`CmdCheckResult(bool)` > `CmdRequestCheck()`). пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `ServerRevalidate` (default) пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅ `TrustClient` пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (UI, input).
- **[P0] ReactiveProperty: ConcurrentModification** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ snapshot count + bounds guard пїЅ `NotifySubscribers()`. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ skip/crash пїЅпїЅпїЅ `AddListener`/`RemoveListener` пїЅпїЅпїЅпїЅпїЅпїЅ callback.
- **[P0] NoCodeBindText: GetComponent пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅ `SetText`, `TimeToText`, `TMP_Text` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `OnEnable`, пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `ApplyFloat`.
- **[P0] SaveProvider: static events leak** пїЅ `OnDataSaved`, `OnDataLoaded`, `OnKeyChanged` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `ResetStaticState` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ Domain Reload OFF.
- **[P1] RandomRange**: `[SyncVar] _syncValue`, rate-limiting (50ms), `OnStartClient()` late-join.
- **[P1] InteractiveObject**: rate-limiting (50ms) пїЅпїЅ `CmdInteractDown/Up/Click`.
- **[P1] NeoCondition: EveryFrame throttle** пїЅ `CheckMode.EveryFrame` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ 60hz (16ms) пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ reflection overhead.
- **[P1] GameSettings: пїЅпїЅпїЅпїЅпїЅпїЅ ResetStaticState** пїЅ `[RuntimeInitializeOnLoadMethod]`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ static пїЅпїЅпїЅпїЅпїЅ + event delegates.

## [8.0.0] - 2026-05-02

пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ: **Neo.NoCode**, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **NeoCondition** / пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`GameObject.Find`**, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ **PlayerController3DPhysics**, пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Save/Shop/пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **7.15.0**.

### Breaking changes
- **Binding / Find пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ** (`BindingSourceGameObjectResolver`): пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`GameObject.Find`** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅ пїЅ **`Find Retry Interval (sec)`** (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **1**; **0** = пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **Neo.NoCode / `ComponentFloatBinding` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ)**: пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **Find By Name** пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **Source Root** пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ **Prefab Preview** (пїЅпїЅпїЅ пїЅ **NeoCondition**). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅFind + Source RootпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ** пїЅ **`ConditionEntry`** (`_findRetryIntervalSeconds`, `_otherFindRetryIntervalSeconds`) пїЅ **`ComponentFloatBinding`** (`_findRetryIntervalSeconds`, `_prefabPreview`) пїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ Unity пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Added
- **Neo.NoCode** (`Assets/Neoxider/Scripts/NoCode/`): **`ComponentFloatBinding`**, **`NoCodeBindText`** (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ **`SetText`** пїЅпїЅпїЅ fallback **`TMP_Text`**), **`SetProgress`** (**`Slider.normalizedValue`** / **`Image.fillAmount`**); пїЅпїЅпїЅпїЅпїЅпїЅ **Once** / **Reactive** / **Poll**.
- **ConditionEntry** / **NeoCondition** / **ComponentFloatBinding**: **`Find Retry Interval (sec)`** (пїЅ **Other:** пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ **Other Object**); пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **NeoCondition** пїЅ **NoCode** пїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **ComponentFloatBinding**: **`PrefabPreview`** (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ UI пїЅ **NeoCondition** (**Find By Name**, **Wait For Object**, пїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ).
- **PlayerController3DPhysics**: **`Enable Cursor Control`** (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ.) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`CursorControlEnabled`** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ**: **`Docs/NoCode/README.md`**; **PlayerController3DPhysics** RU/EN; **NeoCondition** (Find/Wait/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ); **Move/README**; пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ **`Docs/README.md`**, **`DocsEn/README.md`**; **`NO_CODE_AUDIT.md`**.
- **Tests (EditMode)**: **`NoCodeBindEditModeTests`**; **`MoneyPersistenceEditModeTests`**; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Save/Dialogue/Visual/Subsystem; **PlayMode**: **`ShopPurchasePlayModeTests`**.
- **Shop / Money**: пїЅпїЅпїЅпїЅпїЅ **`_persistMoney`**; **`ClearSavedMoneyAndReset`**, **`ReloadBalanceFromSave`**, **`SetCurrentMoney`**; **`SetMoney`** пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **Save / File**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **AES-CBC** пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ plain/cipher; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ key/IV пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- **UPM** (`package.json`): пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`com.unity.inputsystem`**.

### Changed
- **BindingSourceGameObjectResolver**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`GameObject.Find`**; **`Wait`** пїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ **Warning** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- **PlayerController3DPhysics**: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅ **`Start()`**; пїЅпїЅпїЅ **`Enable Cursor Control = off`** пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **Start**-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, Escape, **`SetCursorLocked`**, пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ **`SetLookEnabled`**.
- **Docs**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`NO_CODE_AUDIT.md`**; Save encryption (RU); **`Local/Audits/`** пїЅ **`.gitignore`**.
- **NeoxiderPages / PM**: `GetComponentsInChildren<UIPage>(true)` пїЅпїЅпїЅ PM.
- **Save / settings**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ key/IV.

### Fixed
- **Tests**: `InternalsVisibleTo("Neo.Editor.Tests")` пїЅпїЅпїЅ **Neo.Save** пїЅ **Neo.Tools.Input** (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ EditMode).
- **Save / File encryption**: пїЅпїЅпїЅпїЅпїЅпїЅ key+IV > пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ/IV пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- **GM / State**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `State` (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ).
- **Unity / Domain reload**: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **SaveManager** / **MouseInputManager** пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ generic **`Singleton<T>`**.
- **Singleton**: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `RuntimeInitializeOnLoadMethod` пїЅ generic-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; bootstrap пїЅ пїЅпїЅ-generic пїЅпїЅпїЅпїЅпїЅ.

## [7.15.0] - 2026-05-03
*пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ **[8.0.0]** (пїЅпїЅ. пїЅпїЅпїЅпїЅ).*

## [7.13.21] - 2026-05-03
### Changed
- **Save / File encryption**: When encryption is enabled but Key and IV fields are both empty, **`SaveFileEncryption.DefaultEncryptionKey`** / **`DefaultEncryptionIv`** are used (override anytime by setting both custom strings). Partial fill (only key or only IV) is rejected with an error. File encryption remains **off** by default in **Save Provider Settings**.

### Tests
- **EditMode**: Expanded **`SaveEncryptionEditModeTests`** пїЅ built-in defaults, whitespace > defaults, partial-key validation, disabled config, plain-json migration with built-in cipher config.

### Documentation
- **Save**: `SaveFileEncryption.md`, `SaveProviderSettings.md`, `FileSaveProvider.md` пїЅ default-off encryption, built-in key behaviour (**RU**).
- **Planning**: `Docs/NO_CODE_AUDIT.md` пїЅ пїЅпїЅпїЅпїЅпїЅ No-Code / Inspector UX (пїЅпїЅпїЅпїЅпїЅпїЅпїЅ UI, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, roadmap); пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `Docs/README.md`.

## [7.13.20] - 2026-05-03
### Added
- **Save / File**: AES-CBC + Base64 encryption for **File** saves (`SaveFileEncryption`, `FileSaveEncryptionConfig`, optional `FileSaveProviderOptions`). Configure key/IV in **Save Provider Settings** when provider type is **File**. Plain JSON files saved **without** encryption remain readable after encryption is enabled (migration-friendly load path).

### Changed
- **UPM package** (`package.json`): added **`com.unity.inputsystem`** dependency (aligned with template `Packages/manifest.json`).

### Tests
- **EditMode**: `SaveEncryptionEditModeTests`, `DialogueControllerEditModeTests`, `VisualToggleEditModeTests`.
- **PlayMode**: `ShopPurchasePlayModeTests` (Shop free-item purchase flow).

### Documentation
- **Save**: `FileSaveProvider.md`, `SaveProviderSettings.md`, `SaveFileEncryption.md` (**RU**).

## [7.13.19] - 2026-05-02
### Changed
- **NeoxiderPages / PM**: `FindAllScenePages` no longer uses `Resources.FindObjectsOfTypeAll` (global loaded-object scan). It now collects `UIPage` only under the PM GameObject via `GetComponentsInChildren<UIPage>(true)` пїЅ faster at runtime and matches the intended hierarchy (pages must live under PM).

### Documentation
- **NeoxiderPages**: `PM.md`, `Docs/NeoxiderPages/README.md`, `DocsEn/NeoxiderPages/README.md` пїЅ document that all managed pages must be descendants of the PM object.

## [7.13.18] - 2026-04-30
### Fixed
- **Unity / Domain reload**: Moved `[RuntimeInitializeOnLoadMethod]` static reset/bootstrap for **`SaveManager`** and **`MouseInputManager`** out of **`Singleton<T>`** subclasses into non-generic **`SaveManagerSubsystemRegistration`** and **`MouseInputManagerSubsystemRegistration`**. This removes Editor startup errors (пїЅmethod `ResetStaticState` пїЅ in a generic classпїЅ) while preserving the same subsystem behaviour.

### Tests
- **EditMode**: **`SubsystemRegistrationStaticResetEditModeTests`** covers **`SaveManager.ClearSubsystemCaches`**, **`MouseInputManager.ResetSubsystemPollingState`**, and **`EnableAutoCreateForRuntime`**.

### Documentation
- **Save**: `SaveManager.md` пїЅ domain reload / subsystem registration note (**RU**/**EN**).
- **Tools / Input**: `MouseInputManager.md` (**RU**/**EN**) пїЅ bootstrap lives in **`MouseInputManagerSubsystemRegistration`**.
- **Managers**: `Singleton.md` (**RU**/**EN**) пїЅ rule: no `[RuntimeInitializeOnLoadMethod]` on **`Singleton<T>`** subclasses.

## [7.13.17] - 2026-04-30
### Fixed
- **Tools / Managers / GM**: Fixed `State` setter пїЅ it now assigns `_state = value` so transitions (`StartGame`, `Menu`, `End`, etc.) persist. Previously the internal state never updated, so `G.Start`/`EM.GameStart` could appear stuck (e.g. perpetual `NotStarted`), `G.End` could no-op, and UI (`PM` via `G.OnEnd`) might not switch.

## [7.13.16] - 2026-04-30
### Fixed
- **Tools / Managers**: Removed invalid `RuntimeInitializeOnLoadMethod` usage from generic manager classes (`Singleton<T>`, `SingletonById<T>`). Added a non-generic runtime reset bootstrap (`SingletonRuntimeReset`) to keep static-state reset behavior across Play sessions without Unity startup errors.
- **Runtime Stability**: Normalized object destruction in play mode to use `Destroy(...)` (instead of `DestroyImmediate(...)`) in runtime code paths (`ObjectExtensions`, `NeoObjectPool`, `MeshEmission`, `ParallaxLayer`) to prevent startup spam: `Destroying GameObjects immediately is not permitted...`.

## [7.13.15] - 2026-04-25
### Documentation
- **Quality Standardization**: Rewrote 20+ key module docs (StateMachine, Save, Reactive, Extensions) from source code пїЅ full API tables, real Inspector fields, No-Code + Code examples, cross-references.
- **Placeholder Cleanup**: Removed all auto-generated `| ... |` field descriptions (193 files across RU/EN). Every field now has a meaningful description derived from source.
- **Header Cleanup**: Replaced all `## пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ)` headers with `## пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ` (42 RU files).
- **Purpose Cleanup**: Removed all `пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ` / `Auto-Generated documentation` placeholders (137 EN files, 1 RU file) пїЅ replaced with component-specific descriptions.
- **StateMachine NoCode**: Fully documented `StateData`, `StateMachineData`, `StateTransition`, `ConditionEntryPredicate` with real API from source (RU + EN).
- **EN Parity**: Major Extensions docs (CoroutineExtensions, ColorExtension, StringExtension, ComponentExtensions, PrimitiveExtensions) rewritten with full API tables and examples.
- **Save Module**: `ISaveProvider`, `PlayerPrefsSaveProvider`, `SaveProviderExtensions` пїЅ complete API documentation (RU + EN).

## [7.13.14] - 2026-04-13
### Fixed
- **Tools / Text**: Fixed formatting of compact time string in `TimeSpanExtensions` (`ToCompactString`) to strictly use zero-padded format (`00h 00m`). Fixed missing assembly references and ambiguous namespaces in EditMode tests.

## [7.13.13] - 2026-04-12
### Updated
- **Documentation**: Standardized `Progression` and `RPG` module documentation. Key files (`README.md`, `ProgressionManager.md`, `RpgStatsManager.md`, `RpgCombatant.md`) now feature anchored TOCs, standardized headers, API tables, and improved intra-doc navigation.

## [7.13.12] - 2026-04-12
### Updated
- **Documentation**: Standardized `VampireSurvivor_Guide.md` according to project documentation guidelines (added TOC, purpose, standardized headers, and back-links).
- **Documentation Index**: Registered the Vampire Survivor 3D guide in the main `Docs/README.md` index under a new "Guides" section.

## [7.13.11] - 2026-04-12
### Updated
- **Documentation**: Completely refreshed the `VampireSurvivor_Guide.md` with instructions for the new numeric NPC HUD, stable billboard configurations, and automatic damage scaling.

## [7.13.9] - 2026-04-12
### Fixed
- **RPG System**: Fixed `MeleeWeapon` and `AuraWeapon` not applying the player's level-based damage multiplier. They now correctly deal scaled damage matching the HUD values.

## [7.13.7] - 2026-04-12
### Fixed
- **Demo HUD**: Changed NPC HP bar billboard mode to `AwayFromCamera` and enabled `ignoreY` to prevent "spinning" and "flipping" artifacts during movement.

## [7.13.6] - 2026-04-12
### Added
- **Demo HUD**: Added numeric HP display (Current / Max) to NPC world-space health bars for better combat feedback.

## [7.13.5] - 2026-04-12
### Changed
- **Demo HUD**: Enhanced damage indicator to show final calculated damage value (Base * Multiplier) in real-time.

## [7.13.3] - 2026-04-12
### Added
- **Demo HUD**: Added "Damage Bonus" indicator to `DemoPlayerUI` to visualize player power scaling from level.

## [7.13.2] - 2026-04-12
### Changed
- **RPG Stats**: Increased base HP regeneration scaling by 3x (from 2 to 6 per level in `PlayerStatGrowth`).

## [7.13.1] - 2026-04-12
### Fixed
- **RPG XP Grant**: Improved player detection in `RpgCombatant` to support damage from child objects (weapons, auras, projectiles).
- **RPG XP Grant**: Fixed missing damage source in `MeleeWeapon` (and `AuraWeapon`) which prevented XP attribution.
- **RPG XP Grant**: Improved `RpgAttackController` source resolution to check parent hierarchy for combat receivers.

## [7.13.0] - 2026-04-12
### Added
- **RPG**: NoCode support for `RpgCombatant` with `OnXpRewardGenerated` UnityEvent and `AutoGrantXpToPlayer` setting.
- **Progression**: Reactive binding for `XpToNextLevelState` in `ProgressionBarUI` for instant UI updates.

### Fixed
- **RPG**: XP reward granting logic to use GameObject comparison instead of strict reference comparison (resolves component mismatch issues).
- **Demo**: Fixed `Level Curve Definition` in the RPG demo showing `Next: 0` by switching to Formula mode.
- **Demo**: Fixed persistence issues where Level/XP would not reset upon game restart.

## [Unreleased]

## [7.12.0] - 2026-04-12
### Added
- Integrated **RPG Experience (XP)** system into `RpgCombatant`.
- NPCs now grant XP upon defeat based on their level and a new data-driven growth rule.
- Added `XpReward` rule to `RpgStatGrowthDefinition` with preview support in Editor.
- Added `RpgProgressionHelper` utility for manual XP injection (e.g. from consumables or quests).
- Added `_xpRewardOverride` and attacker tracking to `RpgCombatant`.

## [7.11.1] - 2026-04-11

### Rpg System пїЅ Stat Growth Custom Formulas
- **RpgStatGrowthDefinition** now supports advanced mathematical formulas (`Linear`, `Exponential`, `Quadratic`, `Power`, `Flat`) and custom Animation Curves, removing the restriction to pure linear growth.
- **RpgStatGrowthRuleDrawer**: Implemented a custom PropertyDrawer for cleaner configuration in the Inspector. Unused options fade dynamically based on the selected formula.
- **RpgStatGrowthDefinitionEditor**: Added real-time Level Preview tables in the inspector to easily review 1-100 level stat progressions.
- **Npc & Demo**: Updated `EnemyMeleeStatGrowth` and `EnemyRangedStatGrowth` assets to use the new formula-based system and applied them to NPC prefabs.
- **RPG Demo Scene**: Added a "You Lose" overlay with global Restart (R key) logic.
- Documentation `RpgStatGrowth.md` updated with new formulas and examples.

## [7.11.0] - 2026-04-11

### Rpg System пїЅ Advanced Combat & Damage Routing
- **RpgDamageInfo** struct introduced to pass contextual combat data (Amount, Source, DamageType) instead of raw floats.
- `RpgCombatant` and `RpgStatsManager` now accept and route `RpgDamageInfo` through the combat pipeline.
- Added **Elemental Resistances**:
  - `SpecificDefensePercent` added to `BuffStatType`.
  - `SpecificDamageType` field added to `BuffStatModifier`.
  - `RpgCombatMath` now calculates damage reduction dynamically based on `DamageType` and active buffs.
- `RpgAttackController` fully refactored to populate and send `RpgDamageInfo` with its configured settings.
- Restructured `RpgCombatMath` visibility and parameters for unit testing and modular use.
- **Stat Growth**: Introduced `RpgStatGrowthDefinition` to automatically scale `MaxHp`, `Regen`, `DamagePercent`, and `DefensePercent` based on `Level`.
- **Demo Scripting & Survival Scene**:
  - `RpgWaveSpawner`: Added a flexible wave survival spawner to instantly prototype combat scaling logic.
  - `PlayerCombatSwitcher`: Created utility class for fast swapping of `RpgAttackPreset`s in testing environments.
  - Added `GenerateSurvivalDemoScene` Editor script (**Neoxider/Tools/Generate RPG Survival Demo Scene**) to construct an RPG combat playground end-to-end.

### Progression System пїЅ Premium Tracks & Decoupled Architecture
- `ProgressionManager` converted into a modular, decoupled `MonoBehaviour` (removed legacy Singleton).
- Added **Premium Track Support (BattlePass)**:
  - Added `HasPremium` flag to progression tracking.
  - Added `ProgressionManager.ActivatePremium()` to retroactively grant missed premium rewards.
  - Set `ProgressionReward.IsPremiumOnly` flag.
  - Updated `ProgressionRewardDispatcher` to intercept and manage selective payouts.
- Extended public API for event integration and manual overrides for designer/NoCode tools.

### Testing & Infrastructure
- Extensive Unit Tests added for Premium rewards logic, Retroactive payout systems, and `RpgCombatMath` elemental resistances mechanics.
- Replaced legacy static teardowns (`Object.DestroyImmediate`) to match the new component dependency patterns in `ProgressionManagerTests`.
- All XML Documentation and Markdown Guides (`RpgCombatant.md`, `ProgressionManager.md` for both EN & RU) updated with the latest API code examples and use cases.

## [7.10.0] - 2026-04-11

### InteractiveObject пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **InteractionRayProvider** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `InteractiveObject` пїЅ пїЅпїЅпїЅпїЅпїЅ:
  - `Mouse` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ)
  - `ScreenCenter` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ / crosshair)
  - `Both` пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ hover, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ click (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ)
  - **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ**: пїЅпїЅпїЅпїЅ пїЅпїЅ `Camera.main` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Both`
- **drawDebugRay** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ debug-пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
  - пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅ пїЅпїЅпїЅпїЅ, ЖёпїЅпїЅпїЅпїЅ пїЅ hover, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ interact
- `useScreenCenterRay` пїЅпїЅ `InteractiveObject` пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ per-object fallback

### PlayerController3DPhysics пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ

- **SetMoveInput(Vector2?)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅ `null` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ)
- **SetLookInput(Vector2?)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- **SetJumpInput()** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
- **SetRunInput(bool)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ UI-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

### PlayerController2DPhysics пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ

- **SetMoveInput(float?)** / **SetMoveInput(Vector2?)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- **SetJumpInput()** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
- **SetRunInput(bool)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ UI-пїЅпїЅпїЅпїЅпїЅпїЅ

### Tests

- **InteractiveObjectPlayTests** пїЅ 12+ PlayMode пїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ, hover, distance events

## [7.9.0] - 2026-04-10

### RPG & Progression

- **RpgCombatant** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `SetMaxHp(float)` пїЅ `IncreaseMaxHp(float)` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ).
- **LevelComponent** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅ: OnLevelUp пїЅ OnXpGained пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ).
- **пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅ** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `RPG_Demo` пїЅ `Progression_Demo` пїЅпїЅ пїЅпїЅпїЅпїЅ OnGUI (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `Samples~/Demo/`).

### Inventory (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ)

- **InventoryComponent** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `partial` пїЅпїЅпїЅпїЅпїЅ: `InventoryComponent.Grid.cs`, `Operations.cs`, `Persistence.cs` пїЅ `Queries.cs` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Core Modules & Tools

- **NPC / NpcNavigation** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `NpcNavigation.Behaviours.cs` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ).
- **ReflectionCache** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `ReflectionCache.cs`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `QuestManager` пїЅ `ConditionValueSource` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **пїЅпїЅпїЅпїЅпїЅ (Coverage)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `EditMode` пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ 100% пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `NeoCondition`, `QuestManager`, `UnityExtensions`, `Animations`, `ReactiveProperty`, `InteractiveObject`.

### Fixes & Stability

- **NeoObjectPool** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `Destroy may not be called from edit mode!` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Destroy` пїЅ `DestroyImmediate` пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `Application.isPlaying`).
- **AiNavigation (Deprecated)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ warning пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ target пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ Combined, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **StateMachine** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.

## [7.8.1] - 2026-04-10

### Architecture / Domain Reload

- **SaveProvider**, **SingletonById**, **SpawnUtility** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Domain Reload. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.

### Memory / Performance

- **PoolManager** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ SceneLoaded, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `MissingReferenceException` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`DontDestroyOnLoad`) пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.
- **Spawner** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `Spawner._spawnedObjects`: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ) пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `DelayedDestroy` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Refactoring

- **NpcNavigation**, **InventoryComponent** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `partial` пїЅпїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (Queries, Operations пїЅ пїЅпїЅ.) пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
### Tools / Debug

- **FPS** пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **`Time.unscaledDeltaTime`**, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ delta time; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **`sampleSize`**; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ UI пїЅпїЅ **`unscaledTime`**. пїЅпїЅпїЅпїЅпїЅ **`unlockFramerateOnAwake`** (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.): пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ FPS / vSync пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Tools / InteractableObject

- **InteractiveObject** пїЅ пїЅпїЅпїЅ **`checkObstacles = false`** пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ hover/click пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ-trigger пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: **`Collider` / `Collider2D` пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ**; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- **InteractiveObject** пїЅ hover пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `onHoverEnter` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ **пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ**, пїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ `CanMouseInteractAtPoint`, пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅ `wasHoveredByRaycast` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `IsHovered` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ hover пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ).

### Docs / Meta

- **DOCUMENTATION_GUIDELINES.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ [пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ_пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.md](Docs/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ_пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.md); пїЅ пїЅ1 пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅ пїЅпїЅпїЅпїЅ XML/`Tooltip`/`Header` пїЅ **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ**, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅ `Docs/`.
- **CursorLockController.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ H1 пїЅ пїЅпїЅпїЅпїЅпїЅ **пїЅпїЅпїЅ пїЅпїЅпїЅ** / **пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ [DOCUMENTATION.md](DOCUMENTATION.md).

## [7.8.0] - 2026-03-28

### Settings (пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ Neo.Settings)

- **`GameSettings`** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ API: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (**`GraphicsPreset`**) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`QualitySettings`**, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (**Auto** + пїЅпїЅпїЅпїЅпїЅпїЅ), пїЅпїЅпїЅпїЅпїЅ FPS пїЅ VSync; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **`SaveProvider`**; пїЅпїЅпїЅпїЅпїЅпїЅ **`SettingsPersistMode`** (Immediate / Deferred / SkipUntilFlush); **`ResetGroup`**; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`OnSettingsChanged`**, **`OnAfterSettingsLoaded`**.
- **`GameSettingsComponent`** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ>пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ FPS, debounce пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- **`SettingsView`** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **Unity UI** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ **`ISettingsLocalization`**; пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **`PlayerController3DPhysics`** пїЅ пїЅпїЅпїЅпїЅпїЅ **`Use Game Settings Mouse Sensitivity`** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: **`com.unity.render-pipelines.universal`** (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ URP); пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`Packages/manifest.json`** пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **`Docs/Settings/`**, **`DocsEn/Settings/`** пїЅ README пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `GameSettings`, `GameSettingsComponent`, `SettingsView`, `GraphicsPreset`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`Docs/README.md`**, **`DocsEn/README.md`** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; **`PlayerController3DPhysics.md`** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ **`GameSettings`**.

### пїЅпїЅпїЅпїЅпїЅ

- **`GameSettingsTests`** (EditMode) пїЅ Immediate/Deferred пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`ISaveProvider`**.

## [7.7.25] - 2026-03-28

### Tools / View

- **Selector** пїЅ `CountActive` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `_items` (пїЅ `_count <= 0`) пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`activeSelf` / `SelectorItem.ActiveValue` пїЅ notify-пїЅпїЅпїЅпїЅпїЅпїЅ); пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `_count` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`OnCountActiveChanged` (`UnityEvent<int>`)** пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **`ToggleIndex`**.

### Editor / Tests

- **SelectorTests** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `CountActive` пїЅпїЅпїЅ additive random, `OnCountActiveChanged`, fill mode, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `_count`, notify+`SelectorItem`, `ToggleIndex`.

### Docs

- **Selector.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `CountActive`, `SetRandom(bool)`, `_keepOthersActiveOnRandom`, `OnCountActiveChanged`.

## [7.7.24] - 2026-03-28

### Tools / View

- **Selector** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅ **`Keep Others Active On Random`** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`SetRandom(bool deactivateOthers)`** (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅпїЅпїЅ **fill** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ effective index пїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Editor / Tests

- **SelectorTests** пїЅ пїЅпїЅпїЅпїЅ additive `SetRandom(false)` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ exclusive пїЅпїЅпїЅпїЅпїЅ `SetRandom(true)`.

## [7.7.23] - 2026-03-28

### Tools / View

- **Selector** пїЅ пїЅпїЅпїЅ **`startOnAwake == false`** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ **`RefreshItemsFromChildren`** пїЅпїЅпїЅпїЅпїЅпїЅ **пїЅпїЅ** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`UpdateSelection()`** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Play Mode пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **`RefreshItems()`** пїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ. пїЅ **`OnValidate`** пїЅпїЅпїЅ **`_changeDebug`** пїЅпїЅпїЅпїЅпїЅ **`UpdateSelection`** пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ **`startOnAwake`**.

### Docs

- **Selector.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `startOnAwake` + пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.

## [7.7.22] - 2026-03-28

### Tools / View

- **Selector** пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`Control Game Object Active`** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`SelectorItem.SetActive`** (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ **Notify Selector Items Only**). пїЅпїЅпїЅпїЅпїЅпїЅ, random/unique пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.

### Editor / Tests

- **SelectorTests** пїЅ пїЅпїЅпїЅпїЅпїЅ: random+unique пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `SelectorItem` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ control; `Set()` пїЅпїЅпїЅ `GameObject.SetActive` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ control.

### Docs

- **Selector.md**, **SelectorItem.md**, **Examples/AnomalyGame.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`_controlGameObjectActive`** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ **Notify Selector Items Only**.

## [7.7.21] - 2026-03-28

### Tools / Time

- **TimerObject** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`SetDuration(float)`** (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ); пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`SetDuration(float, bool)`** пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `bool`.

### Docs

- **TimerObject.md** (RU/EN) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `SetDuration(10f)`.

## [7.7.20] - 2026-03-28

### Tools / Time

- **TimerObject** пїЅ пїЅ XML пїЅ `SetProgress` пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **`SetDuration`** / **`StartTimer`** (пїЅпїЅпїЅпїЅпїЅ `SetDuration` пїЅпїЅпїЅ пїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ).

### Docs

- **TimerObject.md** (RU/EN) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `SetDuration`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ.

## [7.7.19] - 2026-03-28

### Tools / InteractableObject

- **PhysicsEvents3D** / **PhysicsEvents2D** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`filterByTag`** пїЅ **`filterByLayer`** (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **пїЅпїЅпїЅпїЅпїЅпїЅ** (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ). пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ.
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:** пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `requiredTag` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`filterByTag`** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Docs

- **PhysicsEvents3D.md**, **PhysicsEvents2D.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## [7.7.18] - 2026-03-25

### Tools / Components

- **Counter** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnRepeatByCounterValue`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ N пїЅпїЅпїЅ (`N = пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ clamp пїЅ `>= 0`).
- **Counter** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `_invokeRepeatEventOnValueChanged` пїЅ `_invokeRepeatEventOnSend`:
  - пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnRepeatByCounterValue` N пїЅпїЅпїЅ;
  - пїЅпїЅпїЅ `Send()` пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnRepeatByCounterValue` N пїЅпїЅпїЅ;
  - пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Docs

- **Counter.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnRepeatByCounterValue` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Repeat Event пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## [7.7.17] - 2026-03-25

### Tools / InteractableObject

- **InteractiveObject** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `useHoverDetection` (hover) пїЅ `useMouseInteraction` (click/down/up); пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `onHoverChanged(bool)`; hover пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `interactionDistance` (0 = пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ); click пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ collider; пїЅпїЅпїЅпїЅпїЅ trigger-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ hover/click пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ collider пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ; `ViewOrMouse` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ hover.

### UI

- **VisualToggle** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `Set(bool value)` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ UnityEvent / пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Tools / View

- **ImageFillAmountAnimator** пїЅ пїЅпїЅпїЅпїЅпїЅ **Invert Value** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅ `SetValue(float)` (\(value = 1 - value\)).

## [7.7.16] - 2026-03-25

### Code style (EN)

- пїЅпїЅ пїЅпїЅпїЅпїЅ `Assets/Neoxider/**/*.cs` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Scripts/`, `Editor/`, `Samples~/`) пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `///` XML, `//` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, `[Tooltip]` / `[Header]`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Debug.*` пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ [пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ_пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.md](Docs/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ_пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.md).

### Cards (breaking)

- **`CardData`**: `ToRussianString()` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ **`ToLongEnglishString()`** (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, `"Queen of Hearts"`).
- **`Suit` / `Rank` / `PokerCombination`**: **`ToRussianName()`** пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ **`ToEnglishName()`**.

### Docs

- **CardData.md** (RU/EN) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## [7.7.15] - 2026-03-25

### Tools / Move

- **CursorLockController** пїЅ **Lifecycle snapshot**: `LifecycleSnapshotMode` (**None** / **SaveOnEnable** / **SaveOnDisable**), **AfterLifecycleDisableCursorBehavior** (пїЅ пїЅ.пїЅ. **RestorePrevious**, **ForceLockedHidden**), **AfterLifecycleEnableCursorBehavior**; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`SnapshotMode`**. пїЅпїЅпїЅпїЅпїЅпїЅ **UI_Page_ShowCursorWhileActive** пїЅ **SaveOnEnable** + **RestorePrevious** пїЅпїЅ disable; **UI_MenuScene_Standalone** пїЅ snapshot **None**, `Apply On Disable` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Docs

- **CursorLockController.md** (RU/EN) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ lifecycle.

## [7.7.14] - 2026-03-25

### Tools / Move

- **CursorLockController** пїЅ inspector **Preset** (`Gameplay_Default`, `UI_Page_ShowCursorWhileActive`, `UI_MenuScene_Standalone`); static stack **sanitizes** destroyed controllers; **`OnDestroy`** releases stack position and reapplies the controller below; **`SceneManager.sceneLoaded`** re-sanitizes and reapplies top state; subsystem registration clears stack/hook on domain reload. Property **`Preset`** (read-only).

### UI

- **PausePage** пїЅ **`AfterPauseCursor`**: default **`RestorePrevious`** (restore `lockState` / `visible` from before pause); **`ForceLockedHidden`** keeps the old пїЅalways lock after pauseпїЅ FPS behavior. **Breaking (default):** projects that relied on always locking after pause must set **ForceLockedHidden**.

### Docs

- **CursorLockController.md** (RU/EN), **PausePage.md**, **PlayerController3DPhysics.md** пїЅ presets, stack/scene load, UI-only scene, PausePage cursor modes.

## [7.7.13] - 2026-03-22

### Tools / Dialogue

- **TypewriterEffect** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `AudioSource` / `AudioClip`, пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ N-пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (`1`, `5` пїЅ пїЅ.пїЅ.). Rich text-пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.

### Audio

- **PlayAudioBtn** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ enum `TriggerMode`: `PointerClick`, `PointerEnter`, `PointerExit`, `PointerDown`, `PointerUp`, `Select`, `Deselect`, `Submit`, `Manual`.

### Docs

- **TypewriterEffect.md**, **DialogueController.md**, **PlayAudioBtn.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ trigger mode пїЅпїЅпїЅ UI-пїЅпїЅпїЅпїЅпїЅпїЅ.

## [7.7.12] - 2026-03-22

### Tools / Dialogue

- **TypewriterEffect** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ; rich text-пїЅпїЅпїЅпїЅ (`<b>`, `<color>`, пїЅ пїЅ.пїЅ.) пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- **DialogueController docs** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `TypewriterEffect`, пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ rich text пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.

## [7.7.11] - 2026-03-22

### Tools / Move

- **PlayerController3DPhysics** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `MovementEnabled`, `JumpEnabled`; `SetJumpEnabled(bool)`; пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `_movementEnabled` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `_canJump`. пїЅпїЅпїЅ `SetMovementEnabled(false)` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- **PlayerController2DPhysics** пїЅ пїЅпїЅ пїЅпїЅ API; пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅ пїЅ 3D). пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.

### Docs

- **PlayerController3DPhysics.md**, **PlayerController2DPhysics.md** пїЅ API пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ APIпїЅ пїЅ 3D.

## [7.7.10] - 2026-03-22

### Audio

- **AMSettings** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `SaveProvider` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **`Init()`** (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Singleton<T>`), пїЅпїЅпїЅпїЅпїЅпїЅ `Awake`.

### Docs

- **AMSettings.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ `Init()`, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `Singleton.md`.

## [7.7.9] - 2026-03-22

### Docs

- **AMSettings.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `SaveProvider`, **Persist Volume** пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ **пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ**, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **Save Key** пїЅпїЅпїЅ Master/Music/Efx, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- **AudioControl.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ AMSettings пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **AMSettings** (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Tooltip пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ Persist пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## [7.7.8] - 2026-03-22

### Audio

- **AMSettings** пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Master / Music / Efx (`0..1`) пїЅпїЅпїЅпїЅпїЅ `SaveProvider`: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `Awake`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Master пїЅ `Start` пїЅпїЅпїЅпїЅпїЅ `ApplyStartVolumes`, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ `ToggleMaster` / `ToggleAllAudio`. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `[DefaultExecutionOrder(-100)]` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ UI.
- **Neo.Audio** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `Neo.Save` пїЅпїЅпїЅ `SaveProvider`.
- **AudioControl** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Start` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅ пїЅ `OnEnable`; пїЅпїЅпїЅ Master-пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `MuteMaster` (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ Music/Efx).

### Docs

- **AMSettings.md**, **AudioControl.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ UI.

## [7.7.7] - 2026-03-22

### Tools / InteractableObject

- **Obstacle checks** пїЅ `InteractiveObject.checkObstacles` now disables obstacle blocking consistently for both distance validation and keyboard direct-look ray checks. When obstacle checks are disabled, interaction no longer remains blocked by `requireDirectLookRay`.
- **Trigger control** пїЅ added `includeTriggerCollidersInObstacleCheck` so obstacle ray checks can optionally include or ignore trigger colliders in both 3D and 2D scenes.
- **Docs** пїЅ updated `Docs/Tools/InteractableObject/InteractiveObject.md` with the new obstacle/trigger behavior.

## [7.7.6] - 2026-03-22

### Tools / Inventory

- **Universal inventory backend** пїЅ `InventoryComponent` now supports two runtime storage modes: `Aggregated` (legacy behavior) and `Slot Grid` (physical slots for hotbars, backpacks, and chests). Added pure C# backends `AggregatedInventory` and `SlotGridInventory`, common records/slot DTOs, and `InventoryTransferService` for slot-to-slot transfers between containers.
- **Stateful item instances** пїЅ inventory can now store per-item payload for unique non-stackable items such as upgraded weapons, wallets with coins, durability-based items, or any custom object state. Added `InventoryItemInstance`, `IInventoryItemState`, `InventoryItemStateBehaviour`, and `InventoryItemStateUtility`; `PickableItem` captures payload on pickup, `InventoryDropper` restores it on spawned world objects, and container save/load keeps instance payload inside the main `InventorySaveData` blob.
- **Hotbar / hand sync** пїЅ `InventoryHand` now supports physical slot indices for Minecraft-style hotbars, including empty slots, and restores instance payload on the in-hand spawned prefab.
- **Slot UI** пїЅ added `InventorySlotGridView` and `InventorySlotView` for fixed-slot inventory grids and simple click-based transfer between slot inventories.

### Tests

- **Inventory EditMode tests** пїЅ added `InventorySystemTests` covering aggregated backend behavior, slot-grid non-stackable behavior, instance-state capture/restore, save/load migration into slot-grid, and slot transfer with instance payload.

### Docs

- **Inventory docs** пїЅ updated `InventoryComponent.md`, `InventoryHand.md`, and inventory `README.md` for storage modes, slot-grid workflow, stateful items, and hotbar behavior.

## [7.7.5] - 2026-03-15

### Tools / RandomRange

- **RandomRange** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`Scripts/Tools/Components/RandomRange.cs`): пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ [Min, Max]. пїЅпїЅпїЅпїЅпїЅпїЅ Int (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ) пїЅ Float. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Value`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `ValueInt` / `ValueFloat` пїЅпїЅпїЅ NeoCondition. пїЅпїЅпїЅпїЅпїЅ `Generate()`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnGeneratedInt` / `OnGeneratedFloat`. пїЅпїЅпїЅпїЅпїЅпїЅ `SetMin`/`SetMax` (int/float) пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ 0 пїЅпїЅ N пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Docs / Examples

- **RandomRange.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ NeoCondition.
- **Selector.md** пїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ 3.6 пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅ `IncludeAllIndices()`, `ResetAll()` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **NeoCondition.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ RandomRange (ValueInt / ValueFloat).
- **AnomalyGame.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ 6 пїЅпїЅпїЅпїЅпїЅпїЅ: 0пїЅ5 пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `Selector` пїЅ `RandomRange`, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## [7.7.4] - 2026-03-15

### Core / Level пїЅ Resources

- **Level (Core)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ XP: `LevelComponent` (MonoBehaviour, `ILevelProvider`), пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (Linear, Quadratic, Exponential, Custom), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ. ReactiveProperty: LevelState, XpState, XpToNextLevelState; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ LevelStateValue, XpStateValue, XpToNextLevelStateValue пїЅпїЅпїЅ NeoCondition.
- **Resources (Core)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `HealthComponent` (HP, Mana пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ id), пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ OnDamage/OnHeal/OnDeath/OnResourceChanged. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ HpCurrentValue, HpPercentValue, ManaCurrentValue, ManaPercentValue пїЅпїЅпїЅ NeoCondition.
- **NoCode Level** пїЅ `LevelNoCodeAction` (AddXp, SetLevel), `LevelConditionAdapter` (LevelAtLeast, XpAtLeast, XpToNextLevelAtMost).

### Progression

- **Progression** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ XP пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `ILevelProvider` (LevelComponent). пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ TotalXp, CurrentLevel. ProgressionManager пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ OnLevelUp пїЅ LevelComponent пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### RPG

- **RPG** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `HealthComponent` пїЅ `LevelComponent` пїЅ RpgStatsManager пїЅ RpgCombatant; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ HP/Level/TakeDamage/Heal/TrySpendResource. пїЅ `IRpgCombatReceiver` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ TrySpendResource. пїЅ RpgAttackDefinition: CostResourceId, CostAmount (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ). RpgAttackController пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **Reactive-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ NeoCondition (LevelStateValue, HpStateValue, HpPercentStateValue пїЅ пїЅ.пїЅ.).

### пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **пїЅпїЅпїЅпїЅпїЅ RPG** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ RpgStatsManagerTests (TrySpendResource пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ HealthComponent), RpgCombatTests (Combatant TrySpendResource, пїЅпїЅпїЅпїЅпїЅ пїЅ CostAmount пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ), HealthComponentTests (Decrease, Increase, TrySpend, IsDepleted, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ).
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Docs/Core/README.md, Docs/Core/Level.md, Docs/Core/HealthComponent.md пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ DOCUMENTATION_GUIDELINES; Core пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Docs/README.md.

пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ 7.7.4: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Unity, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ EditMode-пїЅпїЅпїЅпїЅпїЅ (Window > General > Test Runner).

## [7.7.3] - 2026-03-15

### Tools / TimerObject

- **Time scale** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `timeScale` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `deltaTime` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `timeScale` (1 = пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, 2 = пїЅ 2 пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- **пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅ 00:00пїЅ24:00)** пїЅ пїЅпїЅпїЅ `dayLengthSeconds > 0` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ 86400 пїЅпїЅпїЅ 24 пїЅ) пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `[0, dayLengthSeconds)`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `DayTimeHours`, `DayTimeMinutes`, `DayTimeSeconds` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `dayTimeFormat`. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ.
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ** пїЅ `OnNewDay` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅ `dayLengthSeconds > 0` пїЅ `looping`); `OnDayTimeChanged (float hours, float minutes, float seconds)` пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `infiniteDuration` пїЅ OnValidate.

### Tools / View

- **SelectorItem** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Selector. пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Selector, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `SetActive(bool)` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ NotifySelectorItemsOnly. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Active`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnActivated`, `OnDeactivated`, `OnActivatedInverse`, `OnDeactivatedInverse`. пїЅпїЅпїЅпїЅпїЅпїЅ `ExcludeFromSelector()` пїЅ `IncludeInSelector()` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ Selector.
- **Selector** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: `ExcludeIndex(int)`, `IncludeIndex(int)`, `IncludeAllIndices()`, `IsExcluded(int)`, `ExcludedCount`. пїЅпїЅпїЅ `SetRandom()` пїЅ пїЅ unique-пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **Selector** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `CountActive` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: 0/1 пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, effectiveIndex+1 пїЅ fill). пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnSelectionChangedGameObject (GameObject)`. пїЅпїЅпїЅпїЅпїЅ **Notify Selector Items Only** (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ): пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Selector пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `GameObject.SetActive`, пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `SelectorItem` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ `SetActive(true)`/`SetActive(false)`. пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `SelectorItem.Index`.

### Docs / Examples

- **TimerObject.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (timeScale), пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (dayLengthSeconds, DayTimeHours/Minutes/Seconds, dayTimeFormat), пїЅпїЅпїЅпїЅпїЅпїЅпїЅ OnNewDay пїЅ OnDayTimeChanged.
- **Selector.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ ExcludeIndex, IncludeIndex, IncludeAllIndices, CountActive, OnSelectionChangedGameObject, пїЅпїЅпїЅпїЅпїЅпїЅ NotifySelectorItemsOnly, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **SelectorItem.md** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Selector.
- **Docs/Examples/AnomalyGame.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; Selector + SelectorItem; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ CountActive).

## [7.7.2] - 2026-03-15

### Tools / Selector (patch)

- **Selector** пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `startOnAwake` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: пїЅ `Start()` пїЅпїЅпїЅпїЅпїЅ `UpdateSelection()` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `startOnAwake && Count > 0`. пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`OnTransformChildrenChanged` / `RefreshItemsFromChildren`) пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `_autoUpdateFromChildren` (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `true`). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Selector.md пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### RPG / Progression / Legacy / Documentation

- **RPG module** пїЅ added `Neo.Rpg` module with `RpgStatsManager`, persistent `RpgProfileData`, `BuffDefinition`, `StatusEffectDefinition`, HP/level/buffs/status effects, regen, no-code bridges (`RpgNoCodeAction`, `RpgConditionAdapter`), and profile save/load through `SaveProvider`.
- **RPG combat layer** пїЅ added `RpgCombatant`, `RpgAttackDefinition`, `RpgAttackController`, `RpgProjectile`, and `RpgEvadeController` so the RPG module now covers melee, ranged, area, and evade flows with one runtime architecture.
- **RPG input/save quality** пїЅ `RpgAttackController` now supports built-in configurable input with LMB primary attack by default, `RpgEvadeController` supports configurable built-in evade input, and `RpgStatsManager` now has optional `Auto Save` (disabled by default) plus explicit provider flush on manual save.
- **RPG targeting/presets** пїЅ added `RpgTargetSelector` and `RpgAttackPreset` for AI, skills, and spells; `RpgAttackController` can now use presets, `RpgNoCodeAction` can trigger preset execution, and inspector testing coverage was improved with extra `UnityEvent` hooks and `[Button]` methods on key RPG `MonoBehaviour` scripts.
- **NPC + RPG integration** пїЅ added `NpcCombatPreset`, `NpcRpgCombatBrain`, and `NpcCombatDecisionCore` so combat NPCs can be assembled from small components (`NpcNavigation` + `RpgTargetSelector` + `RpgAttackController` + `RpgCombatant`) instead of a single large custom script; the brain supports melee and ranged enemies through preset-driven chase/hold/attack flow and restores the previous navigation mode when combat ends.
- **RPG API** пїЅ `TakeDamage`, `Heal`, `SetMaxHp`, `SetLevel`, `TryApplyBuff`, `TryApplyStatus`, `RemoveBuff`, `RemoveStatus`, `HasBuff`, `HasStatus`, reactive state (`HpState`, `HpPercentState`, `LevelState`), and events for damage, heal, death, buff/status apply/expire.
- **RPG docs** пїЅ expanded RU/EN docs for the full combat architecture: profile, combatants, attack definitions, attack controller, projectile, evade, no-code actions, and condition adapters.
- **RPG/NPC tests** пїЅ added edit mode tests for combatant invulnerability, direct attacks, preset forced-target execution, NPC combat decision rules, evade invulnerability, plus previous damage/heal/save-load coverage.
- **Legacy AttackSystem** пїЅ `Health`, `AttackExecution`, `Evade`, `AdvancedAttackCollider` marked as `[Obsolete]` and `[LegacyComponent]` with explicit replacements in the new RPG combat layer; `RpgStatsDamageableBridge` now bridges legacy `IDamageable/IHealable` calls into either `RpgStatsManager` or `RpgCombatant`.
- **Docs** пїЅ main README, Docs/README, NPC docs, and AttackSystem README updated to feature RPG combat/NPC composition and deprecate legacy combat components.
- **Progression V2** пїЅ added a new `Neo.Progression` module with `ProgressionManager`, persistent `ProgressionProfileData`, `LevelCurveDefinition`, `UnlockTreeDefinition`, `PerkTreeDefinition`, reward dispatch, no-code bridges, and custom inspectors for validation.
- **Meta progression API** пїЅ new runtime flow supports `XP`, levels, `perk points`, unlock nodes, perk purchases, and profile save/load through `SaveProvider`, with both reactive state and UnityEvent entry points.
- **Legacy policy** пїЅ added `LegacyComponentAttribute` and updated the Neoxider create window so legacy components are excluded from the custom create menu.
- **Deprecated components** пїЅ `TimeReward`, `WheelFortune`, `UIReady`, and `AiNavigation` are now consistently marked as legacy, hidden from `Add Component`, and documented with explicit replacements.
- **Progression docs** пїЅ added RU/EN module docs for `ProgressionManager`, `ProgressionNoCodeAction`, `ProgressionConditionAdapter`, and scenario guides with recommended settings for arcade, RPG, strategy, narrative, and roguelite projects.
- **Quality** пїЅ added edit mode tests for progression save/load flow and legacy create-menu filtering.

### Save / Tools / Quality

- **SaveManager** пїЅ component save keys now use stable scene-based identity via `SaveIdentityUtility` instead of `GetInstanceID()`, and stale registrations are cleaned before save/load passes.
- **SaveableBehaviour** пїЅ automatic unregister on `OnDisable()` is restored, so disabled or destroyed saveable components no longer remain in the static registry.
- **SaveProvider** пїЅ provider event forwarding now uses stable handlers, so replacing the active provider no longer leaves stale event subscriptions behind.
- **LevelManager** пїЅ map switching validates indices, protects against empty map arrays, emits the selected map id in `OnChangeMap`, and avoids modulo-by-zero in `GetLoopLevel`.
- **Input compatibility** пїЅ reflection-based Input System access is centralized in `OptionalInputSystemAdapter`, with cached metadata reused by both `KeyInputCompat`, `MouseInputCompat`, and movement controllers.
- **Bootstrap / Singleton** пїЅ bootstrap keeps pending runtime registrations ordered by `InitPriority`, and `Singleton<T>` now exposes `HasInstance` / `TryGetInstance(out T)` for safer optional dependencies.
- **Quality pipeline** пїЅ added a root `.editorconfig`, explicit `com.unity.test-framework` dependency, and new edit mode tests covering save identity, provider event forwarding, level map payloads, and bootstrap ordering.

## [7.4.0] - 2026-03-06

### Package / Documentation

- **Version bump** пїЅ package version raised from `7.3.2` to `7.4.0`; public entry points (`package.json`, `README.md`, `Docs/README.md`, `PROJECT_SUMMARY.md`) are now aligned.
- **Package metadata** пїЅ added `documentationUrl`, `changelogUrl`, and `licensesUrl` to `package.json`; sample descriptions were rewritten for package-consumer clarity.
- **Documentation entry points** пїЅ rewritten root README, docs index, tools index, save overview, and NeoxiderPages overview to remove stale paths, broken links, and outdated package structure references.
- **English onboarding** пїЅ refreshed `DocsEn/README.md` and added initial English module entry pages for key user flows.

### Tools / Runtime

- **Pooling** пїЅ pooled objects are no longer implicitly kept under `PoolManager` while active; pool storage is separated from spawned scene instances to make scene transitions safer.
- **InventoryDropper** пїЅ drop flow is now validated before removal and restores inventory items if spawning fails after removal.
- **DialogueController** пїЅ `autoNextMonolog` and `autoNextDialogue` now actually control automatic progression instead of forcing an immediate next step when disabled.
- **GM / Singleton** пїЅ `GM.Init()` now respects the base singleton initialization contract.
- **InteractiveObject / Input** пїЅ reduced hidden runtime scene mutations and tightened `onInteractUp` semantics so release events only fire after a matching press on the same object.
- **Inventory auto-find** пїЅ `InventoryComponent.FindDefault()` no longer silently picks an arbitrary inventory when multiple candidates exist.
- **InventoryDatabase** пїЅ duplicate `itemId` entries are now reported deterministically and no longer overwrite each other silently in the runtime cache.
- **CursorLockController** пїЅ added `ControlMode` with default `AutomaticAndManual`, ownership handoff via `ReleaseControl()`, support for multiple active controllers, and an optional `Cursor Access Key` with `HoldToShowCursor` / `ToggleShowCursor` modes (for example, temporary cursor on `Z` during gameplay). The access-key shortcut is disabled by default.

## [7.3.2] - 2026-02-23

### Tools / Components

- **AnimatorParameterDriver** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (Trigger/Bool/Float/Int Parameter Name); пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: **SetTrigger()**, **SetBool(bool)**, **SetFloat(float)**, **SetInt(int)** пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ UnityEvent: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ SetTrigger(), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ SetBool(bool) / SetFloat(float) / SetInt(int). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## [7.3.1] - 2026-02-23

### Shop / Level / Components (patch)

- **TextLevel** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Start (пїЅпїЅ OnEnable), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ **Level Source** (пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ LevelManager пїЅ пїЅпїЅпїЅпїЅпїЅ).
- **TextScore** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Start, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ **Score Source**; пїЅ OnDisable пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (_scoreManager), пїЅ пїЅпїЅ пїЅпїЅ ScoreManager.I.

## [7.3.0] - 2026-02-23

### Shop / Money

- **Singleton<T>** пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `I` пїЅпїЅ Awake пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ T, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ Set Instance On Awake**. пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `_instance` пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Awake пїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ Money (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ) пїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **TextMoney** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ **Start** (пїЅпїЅ OnEnable), пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Awake. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ **Money Source**: пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Money (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ); пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Money.I`. пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ OnEnable пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Singleton.md (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ Set Instance On Awake), TextMoney.md (Money Source, Start), Money.md (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ).

## [7.2.0] - 2026-02-23

### Tools / Input

- **KeyInputCompat** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (Input Manager) пїЅ пїЅпїЅпїЅпїЅпїЅ (Input System Package) пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ: `GetKeyDown(KeyCode)`, `GetKeyUp(KeyCode)`, `GetKey(KeyCode)`. пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ legacy Input; пїЅпїЅпїЅ `InvalidOperationException` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ MultiKeyEventTrigger, InventoryHand, InventoryDropper, CursorLockController пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- **MultiKeyEventTrigger** пїЅ пїЅпїЅпїЅпїЅпїЅ **Debug** (bool): пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Tools / Inventory

- **HandView** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅ пїЅпїЅ, пїЅпїЅпїЅ WorldDropPrefab): пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (**Position Offset**), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (**Rotation Offset**) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (**Scale In Hand**). InventoryHand пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ HandView пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (Fixed пїЅпїЅпїЅ Relative).
- **InventoryHand** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ enum **HandScaleMode** (Fixed / Relative); пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **Relative** (пїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ = пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅ HandView пїЅпїЅпїЅ 1) ? handScale. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ -1** (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ): пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **Allow Empty Slot** пїЅ **Allow Empty Effective Index** пїЅ Selector пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ SetSlotIndex(-1) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ -1 пїЅпїЅпїЅпїЅпїЅ Selector.Previous() пїЅ 0; пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, EquippedItemId = ?1.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: HandView.md, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ InventoryHand.md, Tools/Inventory/README.md, UsefulComponents.md.

### Tools / View

- **Selector** пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ CountпїЅ (пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ Items) пїЅ UpdateSelection пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ _currentIndex пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (GetCurrentBounds), пїЅпїЅпїЅпїЅпїЅ Next/Previous пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Count.

## [7.0.0] - 2026-02-23

### BREAKING: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ

пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ?пїЅпїЅ **breaking changes**:

- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:** пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (UnityEvent&lt;float&gt;, UnityEvent&lt;int&gt;, UnityEvent&lt;bool&gt;). пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (ReactivePropertyFloat, ReactivePropertyInt, ReactivePropertyBool). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ `.OnChanged` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `counter.Value.OnChanged`, `money.Money.OnChanged`). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `OnValueChanged => Value.OnChanged` пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ):** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ TimeReward, AiNavigation, WheelFortune, UIReady пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅ. `Docs/Plan_RemoveDeprecatedScripts.md`.

пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (`.OnChanged`) пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ TimeReward, AiNavigation, WheelFortune, UIReady.

**пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (breaking):** пїЅ **ScoreManager** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `ScoreValue`, `BestScoreValue`, `TargetScoreValue` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `Score`, `BestScore`, `TargetScore`, `Progress`, `CountStarsReactive`). пїЅ **Health** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ HP пїЅпїЅпїЅпїЅпїЅ `HpValue` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Hp`, `HpPercent`. пїЅ **Drawer** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `DistanceValue`, пїЅ **TypewriterEffectComponent** пїЅ пїЅпїЅпїЅпїЅпїЅ `ProgressValue`.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (ReactiveProperty)

- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **ReactivePropertyFloat**, **ReactivePropertyInt**, **ReactivePropertyBool** (API пїЅ пїЅпїЅпїЅпїЅпїЅ R3: CurrentValue, Value, OnChanged, OnNext, SetValueWithoutNotify, ForceNotify, AddListener, RemoveListener, RemoveAllListeners). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ **Neo.Reactive**: пїЅпїЅпїЅпїЅпїЅ `Scripts/Reactive/`, пїЅпїЅпїЅпїЅ `ReactiveProperty.cs`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `Neo.Reactive`. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ Neo.Reactive пїЅ `using Neo.Reactive;` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.
- **Counter** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `Value` (ReactivePropertyFloat); пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ OnValueChangedInt/Float пїЅ Send пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ + UnityEventпїЅ: ToggleObject, Money, LightAnimator, CooldownReward, Evade, TimerObject, Drawer, TypewriterEffectComponent, AMSettings, VisualToggle, FloatAnimator, DistanceChecker, MagneticField, ItemCollection, Box, Health, ScoreManager, HandComponent, Selector, FakeLoad, TicTacToeBoardService, DrunkardGame, LevelManager, UI (Simple), NpcNavigation, NeoCondition, LeaderboardItem, TextLevel, LevelButton, DialogueData, TimeToText, SpinController (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ `.OnChanged`.

## [6.0.7] - 2026-02-19

### Condition пїЅ NeoCondition: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (int/float/string)

- **ConditionEntry** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ.
  - пїЅпїЅпїЅпїЅпїЅ enum `ArgumentKind { Int, Float, String }` пїЅ пїЅпїЅпїЅпїЅ: `_isMethodWithArgument`, `_propertyArgumentKind`, `_propertyArgumentInt`, `_propertyArgumentFloat`, `_propertyArgumentString`.
  - пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ Property пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (int/float/string) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ int/float/bool/string, пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `GetCount (int) > Int [method]`.
  - пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Property пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (Argument int/float/string).
  - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ **Other Object** (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ): `_otherIsMethodWithArgument`, `_otherPropertyArgumentKind`, `_otherPropertyArgumentInt/Float/String`.
- **NeoConditionEditor** пїЅ пїЅ `DrawPropertyDropdown` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ; пїЅпїЅпїЅ Other Object пїЅ пїЅпїЅпїЅ пїЅпїЅ dropdown пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅ `ResetConditionEntry` пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ; пїЅ Play Mode пїЅ preview пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ**: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅ).
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ**: пїЅ NeoCondition.md пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Property/Argument, пїЅпїЅпїЅпїЅпїЅпїЅ 8 пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ idпїЅ (InventoryComponent.GetCount(int), Argument = itemId).

### Tools / Inventory пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **InventoryComponent** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `[Button]` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Inspector:
  - Add: пїЅAdd 1пїЅ, пїЅAdd NпїЅ (AddItemById, AddItemByIdAmount), пїЅAdd 1 (Selected Id)пїЅ пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Condition Helper id.
  - Remove: пїЅRemove 1пїЅ, пїЅRemove NпїЅ, пїЅRemove 1 (Selected Id)пїЅ.
  - Drop: пїЅDrop SelectedпїЅ, пїЅDrop By IdпїЅ, пїЅDrop FirstпїЅ, пїЅDrop LastпїЅ.
  - пїЅClearпїЅ, пїЅSaveпїЅ, пїЅLoadпїЅ пїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `using Neo` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `[Button]`.

## [6.0.6] - 2026-02-19

### Editor пїЅ Presets пїЅ пїЅпїЅпїЅпїЅ Create Neoxider Object

- пїЅ пїЅпїЅпїЅпїЅ **GameObject > Neoxider > Create Neoxider Object...** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ **Presets (пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ)**.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: System (System Root), Combat (Simple Weapon, Bullet), Player (First Person Controller), Interaction (Interactive Sphere, Trigger Cube, Toggle Interactive).
- пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ.
- пїЅ `NeoxiderPresetCreateMenu` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `CreatePreset(string relativePrefabPath)` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Editor пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Tools > Neoxider > Create Sprite from Prefab...** пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ Sprite-пїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, PNG + TextureImporter Sprite).
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `PrefabToSpriteUtility`: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ-readable пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ RenderTexture, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ PNG, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ Sprite.

### Shop пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Money** пїЅ namespace пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `Neo.Shop`; пїЅ `Save()` пїЅпїЅпїЅпїЅпїЅ SetFloat пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `SaveProvider.Save()`; пїЅ ChangeMoneyEvent/ChangeLevelMoneyEvent/SetText пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ null.
- **Shop** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (RemoveListener пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ id пїЅ Buy(id), Visual(), VisualPreview(); Load() пїЅпїЅпїЅ null/пїЅпїЅпїЅпїЅпїЅпїЅ _prices пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ; пїЅ Save() пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ SaveProvider.Save(). пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `moneySpendSource` пїЅ `[FormerlySerializedAs("IMoneySpend")]`.
- **TextMoney** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Money.I пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ WaitWhile (пїЅпїЅпїЅ пїЅ TextScore), пїЅпїЅпїЅпїЅпїЅ Init() пїЅ RemoveListener пїЅпїЅпїЅпїЅпїЅ AddListener.
- **ButtonPrice** пїЅ пїЅ SetButtonText/SetPriceText пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ _textButton/_textPrice пїЅпїЅ null.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: Money.md, Shop.md, SHOP_IMPROVEMENTS.md пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## [6.0.5] - Unreleased

### Tools / Inventory пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **Inventory** (`Neo.Tools.Inventory`) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ Data/Core/Runtime/UI.
- **Core API**: `InventoryManager`, `InventoryEntry`, `InventorySaveData` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ C# пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ MonoBehaviour (Add/Remove/Has/GetCount, пїЅпїЅпїЅпїЅпїЅпїЅ, snapshot/load).
- **Data**: `InventoryItemData` (id, пїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ, maxStack, category) пїЅ `InventoryDatabase` (lookup/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ id, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ maxStack).
- **No-Code Runtime**:
  - `InventoryComponent` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnItemAdded`, `OnItemRemoved`, `OnItemCountChanged`, `OnCapacityRejected`, `OnInventoryChanged`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ SaveProvider, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ NeoCondition (`TotalItemCount`, `UniqueItemCount`, `SelectedItemCount`, `IsEmpty`).
  - `PickableItem` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ trigger 2D/3D пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `Collect()` пїЅпїЅпїЅпїЅпїЅ UnityEvent, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (disable colliders / deactivate / destroy).
  - `InventoryPickupBridge` пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ UnityEvent (`InteractiveObject`, `PhysicsEvents` пїЅ пїЅпїЅ.).
- **UI binders**: `InventoryItemCountText`, `InventoryTotalCountText` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ **TextMeshPro** (`TMP_Text`).
- **UI Views**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `InventoryView` пїЅ `InventoryItemView` (пїЅпїЅпїЅпїЅпїЅ auto-spawn пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ manual-пїЅпїЅпїЅпїЅпїЅ), пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **Drop module**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `InventoryDropper` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ InventoryComponent), пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Rigidbody`/`Rigidbody2D`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `PickableItem`.
- `InventoryComponent` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Singleton<InventoryComponent>` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ multi-instance пїЅпїЅпїЅпїЅпїЅ `Set Instance On Awake`.
- **Initial state**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `InventoryInitialStateData` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `UseSaveIfExists`, `MergeSaveWithInitial`, `InitialOnlyIgnoreSave`.
- **Runtime events/API**: пїЅ `InventoryComponent` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnBeforeLoad` пїЅ `GetSnapshotEntries()` пїЅпїЅпїЅ UI пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- **InventoryView**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (`DatabaseItems`, `SnapshotItems`, `Hybrid`), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `OnLoaded` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ refresh пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **Prefab preview/icon fallback**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ extension `PrefabPreviewExtensions` (`GetPreviewTexture`, `GetPreviewSprite`); пїЅпїЅпїЅпїЅ пїЅ `InventoryItemData` пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `Icon`, пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `WorldDropPrefab` (sprite/preview).
- **InventoryDropper input**: пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `G`, master bool `CanDrop`, пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `DropByIdOne`, `DropConfiguredById`, `SetDropEnabled`, `SetDropItemId`.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `InventoryDropper.md`, `InventoryView.md`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Docs/Tools/README.md` пїЅ `Docs/README.md` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Editor / Create пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ preset-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ

- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ editor-пїЅпїЅпїЅпїЅпїЅпїЅ `NeoxiderPresetCreateMenu` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **GameObject > Neoxider > Presets** (пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ).
- пїЅ Presets пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
  - System Root (`--System--`)
  - Player (`Player (First Person Controller)`)
  - Combat: `Simple Weapon`, `Bullet`
  - Player: `First Person Controller`
  - Interaction: `Interactive Sphere`, `Trigger Cube`, `Toggle Interactive`
- пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **Presets** (пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ), пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `Create Neoxider Object...` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.

### Tools / Move пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ

- **UniversalRotator** пїЅ `using UnityEditor` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `#if UNITY_EDITOR` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ).
- **ScreenPositioner** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `_targetCamera == null` пїЅ ApplyScreenPosition пїЅ пїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅ пїЅuse screen positionпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `_screenEdge` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ BottomLeft; пїЅпїЅпїЅпїЅпїЅ `_updateEveryFrame` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ LateUpdate пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `[SerializeField] private`.
- **DistanceChecker** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`hysteresisOffset`, пїЅпїЅпїЅпїЅпїЅпїЅ approach/depart); пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `continuousEventThreshold` пїЅпїЅпїЅ onDistanceChanged; пїЅпїЅпїЅпїЅпїЅ `SetCurrentObject(Transform)`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Awake/OnValidate/SetDistanceThreshold.
- **Follow** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ (`findTargetByTag`, `targetTag`); пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `onTargetLost` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `activationDistance`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `GetFollowPosition()`, `GetFollowRotation()`.
- **AdvancedForceApplier** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnApplyFailed` (пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Rigidbody) пїЅ `OnApplyForce`; пїЅпїЅпїЅпїЅпїЅ `clampSpeedEveryFixedUpdate`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `SetTarget(Transform)`, `SetDirectionMode(DirectionMode)`, `SetCustomDirection(Vector3)`.
- **CameraConstraint** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `onConstraintFailed` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Debug.LogWarning` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; `SetBoundsSource(Object)` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `bool`.
- **CameraRotationController** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (`mouseButton`) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`modifierKey`); пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `mouseSensitivity`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `onRotateStart`, `onRotateEnd`.
- **CursorLockController** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: **LockAndHide**, **OnlyHide**, **OnlyLock** (enum `CursorStateMode`); пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ New Input System (пїЅпїЅпїЅпїЅпїЅ `SetCursorLocked`/`ToggleCursorState` пїЅпїЅ callback).
- **PlayerController2DPhysics / PlayerController3DPhysics** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnMoveStart`, `OnMoveStop` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅ `_groundCheck == null` пїЅ Awake пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Debug.LogWarning`; `Teleport` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ.
- **KeyboardMover** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `horizontalAxis`, `verticalAxis`; пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **Input Backend** (Legacy / New Input System / AutoPreferNew) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OptionalInputSystemBridge.ReadMove()`.
- **ConstantMover / ConstantRotator** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `useDeltaTime` (units per second vs per frame); пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `SetSpeed(float)` пїЅ `SetDegreesPerSecond(float)`.
- **MouseMover2D / MouseMover3D** пїЅ пїЅ Awake пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Camera.main`, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Debug.LogWarning`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `mouseButton` пїЅ `arrivalThreshold`; **MouseMover3D** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `IMover` (`MoveDelta(Vector2)`, `MoveToPoint(Vector2)` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ); пїЅ `RaycastCursor` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `cam == null`.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: CursorLockController.md, KeyboardMover.md, MouseMover3D.md, ConstantMover.md, IMover.md, PlayerController2DPhysics.md, PlayerController3DPhysics.md, README Move.

### Tools / Components пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **ScoreManager** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `SetBestScore(int?)`: пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `OnValueChange` пїЅ `ResetScore()` (пїЅпїЅпїЅпїЅпїЅпїЅ Score пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ).
- **Counter** пїЅ пїЅ `SaveValue()` пїЅпїЅпїЅпїЅпїЅ `SetFloat` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `SaveProvider.Save()` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Money).
- **Loot** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `lootItems == null || lootItems.Length == 0` пїЅ `DropLoot()` пїЅ `GetRandomPrefab()` (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ NullReferenceException). Namespace пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `Neo.Tools`.
- **TextScore** пїЅ пїЅ `Init()` пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `RemoveListener(Set)` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **Health** пїЅ пїЅ XML-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ IDamageable, IHealable, IRestorable пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ AttackExecution.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: ScoreManager.md (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ SetBestScore), SCRIPT_IMPROVEMENTS.md пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ).

## [6.0.4] - Unreleased

### Tools / Random пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ No-Code пїЅ API

- **ChanceSystemBehaviour** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ:
  - **On Index And Weight Selected (int, float)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (0..1).
  - **On Roll Complete** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (UI, пїЅпїЅпїЅпїЅ).
  - **Events By Index** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ UnityEvent пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ N пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ N (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ).
  - **LastSelectedIndex**, **LastSelectedEntry** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
  - **EvaluateAndNotify()** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ Entry.
  - **SetResultAndNotify(int)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
  - **GetNormalizedWeight(int)**, **GetOrAddEventForIndex(int)** пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.
  - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: **OnIdGenerated** пїЅ **OnIndexSelected** пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **ChanceManager** пїЅ **TryEvaluate(out int index, out Entry entry)** пїЅ **TryEvaluate(float randomValue, out int index, out Entry entry)** пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅ пїЅ.пїЅ. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ).
- **ChanceData** пїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ ChanceSystemBehaviour пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: ChanceSystemBehaviour.md, ChanceManager.md, README Random пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## [6.0.3] - Unreleased

### UI: SceneFlowController пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **SceneFlowController** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: LoadScene(int), LoadScene(string), LoadScene() пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅ Sync, Async, AsyncManual, Additive; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Text/TMP, Slider, Image пїЅ UnityEvent\<float\> OnProgress; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ OnLoadStarted, OnReadyToProceed, OnLoadCompleted; Quit, Restart, Pause, ProceedScene. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: Docs/UI/SceneFlowController.md.
- **UIReady** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `[Obsolete]` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ SceneFlowController; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **DEPRECATED_OR_REMOVAL_CANDIDATES.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (TimeReward, AiNavigation, HandLayoutType, HandComponent.LegacyLayoutType, UIReady) пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.

### Create Neoxider Object (пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ)

- **пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅ пїЅ пїЅпїЅпїЅпїЅ Create Neoxider Object пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (UI, Tools, Bonus, Shop, Audio, Level,
  Save, Condition, Animations, GridSystem, Parallax, NPC) пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Tools** пїЅ пїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅ пїЅпїЅпїЅпїЅ Tools пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: **Physics**, **Movement**, **Spawner**, *
  *Components**, **Dialogue**, **Input**, **View**, **Debug**, **Time**, **Text**, **Interact**, **Random**, **Other**,
  **State Machine**, **FakeLeaderboard**, **Managers**, **Camera**. пїЅпїЅпїЅпїЅ CreateFromMenu пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  Neoxider/Tools/Movement/PlayerController3DPhysics, Neoxider/Tools/Physics/ExplosiveForce).
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅ UsefulComponents.md пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Tools пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ.

## [6.0.2] - Unreleased

### GameObject > Neoxider (CreateFromMenu)

- **CreateFromMenu** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `[CreateFromMenu("Neoxider/пїЅ", PrefabPath = "пїЅ")]` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ
  AddComponentMenu: UI, Tools, Shop, Audio, Bonus (пїЅ пїЅ.пїЅ. TimeReward, SpinController, SlotElement, Row, Box,
  ItemCollection, ItemCollectionInfo, WheelMoneyWin), Condition (NeoCondition), Animations (ColorAnimator,
  FloatAnimator, Vector3Animator), GridSystem (FieldGenerator, FieldSpawner, FieldDebugDrawer, FieldObjectSpawner,
  Match3BoardService, TicTacToeBoardService), Bootstrap. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ Add Component, пїЅ
  пїЅпїЅпїЅпїЅпїЅ **GameObject > Neoxider > Create Neoxider ObjectпїЅ**; пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (fallback).
- **UsefulComponents.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅGameObject > NeoxiderпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅ Create пїЅ Add Component пїЅпїЅпїЅпїЅпїЅ Neoxider

- пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (ScriptableObject) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ Add Component пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ **Neoxider** пїЅпїЅпїЅпїЅпїЅпїЅ *
  *Neo**: пїЅCreate > Neo > пїЅпїЅ > пїЅCreate > Neoxider > пїЅпїЅ, пїЅAdd Component > Neo > пїЅпїЅ / пїЅGameObject > Neo > пїЅпїЅ > пїЅNeoxiderпїЅ.
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ IMPROVEMENTS.md (CreateAssetMenu пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ), RainbowSignature.md, README пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (Collection, Cards, Shop,
  StateMachine, Level, NeoxiderPages, NPC, ChanceManager, DeckConfig, ItemCollectionData пїЅ пїЅпїЅ.), пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (GM, EM, Bootstrap, Money, Leaderboard, PoolManager, UIPage, PageSubscriber,
  StateMachineBehaviour пїЅ пїЅ.пїЅ.), NeoCondition.md, UI Extension README.

### UI / Tools

- **PausePage** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
    - пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`_useTimeScale`), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ GM (`_sendPause`), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (
      `_controlCursor`).
    - пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **Control Cursor** пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
      пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ CursorLockController пїЅ PlayerController 2D/3D).
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `_timeScaleOnPause` (0 = пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ), пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
      пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Time.timeScale`.
- **CursorLockController** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ:
    - **Apply On Enable** / **Lock On Enable** пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (OnEnable) пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ lock/unlock пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
    - **Apply On Disable** / **Lock On Disable** пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (OnDisable) пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ lock/unlock.
    - пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ PausePage пїЅ PlayerController3DPhysics /
      PlayerController2DPhysics пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ PausePage.md пїЅ CursorLockController.md.

## [6.0.1] - Unreleased

### пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (PoolManager, Spawner)

- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ max size пїЅпїЅпїЅпїЅ** пїЅ 100 (пїЅпїЅпїЅпїЅпїЅпїЅ 10000). пїЅ **PoolConfig** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ **maxSize
  **; пїЅпїЅпїЅ 0 пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ 100.
- **PooledObjectInfo** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ **NeoObjectPool.CreatePooledObject()**, пїЅ пїЅпїЅ пїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅ Get. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **Return()** пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ PoolManager.Release.
- **Spawner** пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ **PoolManager.Get(..., parent)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ SetParent.
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (Neo.Extensions)** пїЅ **ReturnToPool()** пїЅпїЅпїЅ GameObject, **SpawnFromPool(position, rotation, parent)** пїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ PoolManager SpawnFromPool пїЅпїЅпїЅпїЅпїЅпїЅ Instantiate.
- **PoolableBehaviour** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ OnPoolCreate/OnPoolGet/OnPoolRelease пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ.
- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ PoolManager.md, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ PooledObjectInfo.md, PoolableBehaviour.md.

## [6.0.0] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- пїЅ пїЅпїЅпїЅпїЅпїЅ **Documentation** (пїЅ пїЅпїЅпїЅпїЅпїЅ Events/Actions) пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ .md пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ **Open in window**. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `[NeoDoc("path/from/Docs.md")]` пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `TypeName.md` пїЅ Docs.
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **Markdown Renderer** пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ Markdown пїЅ пїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **Package Manager > Add package
  from git URL** > `https://github.com/NeoXider/MarkdownRenderer.git`. пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ .md-пїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅ Project.

### пїЅпїЅпїЅпїЅпїЅпїЅ

- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: 5.8.x > 6.0.0.

## [5.8.15] - 2025

### Bonus пїЅ CooldownReward пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ TimeReward

- **CooldownReward** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **TimerObject**: пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (
  RealTime), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (TakeReward, GetClaimableCount, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, max per take). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅ: GameObject > Neoxider > Bonus > CooldownReward.
- **TimeReward** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `[Obsolete]` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ CooldownReward; пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **Docs** пїЅ пїЅ Bonus/TimeReward/README.md пїЅпїЅпїЅпїЅпїЅпїЅ CooldownReward пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, TimeReward пїЅ пїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### Tools/Time пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **TimerObject** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `protected virtual string GetSaveKey() => saveKey` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅ пїЅ SaveState/LoadState), `protected virtual void SaveState()` (пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ). пїЅпїЅпїЅпїЅ
  `saveProgress` пїЅ `saveMode` пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `protected` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Neo.Bonus.asmdef** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ Neo.Tools.Time (пїЅпїЅпїЅ CooldownReward).

## [5.8.14] - Unreleased

### Bonus/Collection пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **CollectionVisualManager** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `RemoveListener` пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅ пїЅ `AddListener` (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ).
- **Collection** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `PlayerPrefs.SetInt` пїЅпїЅ
  `SaveProvider.SetInt` пїЅ `AddItem`, `RemoveItem`, `ClearCollection` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `Load()`/`Save()`.
- **Collection.GetPrize** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ O(n?) пїЅпїЅ O(n): пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- **Collection** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `UnlockAllItems` пїЅ `ClearCollection`: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `_enabledItems` пїЅ
  `_itemCollectionDatas`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `_itemCollectionDatas` пїЅ `ClearCollection`.
- **Box** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `TakePrize` (clamp ? 0); пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `progress` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  `SaveProvider.Save()` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- **ItemCollectionData** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ PascalCase: `ItemName`, `Description`, `Sprite`,
  `ItemType`, `Rarity`, `Category` (пїЅпїЅпїЅпїЅпїЅпїЅ `itemName`, `description`, `sprite` пїЅ пїЅ.пїЅ.).
- **Box** пїЅ пїЅпїЅпїЅпїЅ `addProgress`/`maxProgress` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `[SerializeField] private` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  `AddProgressAmount`, `MaxProgress`.
- **ItemCollection** пїЅ пїЅпїЅпїЅпїЅ `button` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Button`.

## [5.8.13] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **NeoCondition** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
    - пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ **Compare With** > **Other Object**: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅ) пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
      пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ op пїЅпїЅпїЅпїЅпїЅпїЅ
      пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (==, !=, >, <, >=, <=). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Find By Name пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ GameObject.

- **TimerObject** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ):
    - `saveProgress`: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ/пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
      пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ.
    - `saveMode`: **Seconds** (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ; *
      *RealTime** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (UTC), пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
    - `saveKey`: пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ SaveProvider. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ OnDisable, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Awake.

- **InteractiveObject** пїЅ пїЅпїЅпїЅпїЅпїЅ `includeTriggerCollidersInMouseRaycast` (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `true`): пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ trigger-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (hover). пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **NeoCondition** пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ **Other Source Object** (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ) пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ **пїЅпїЅпїЅ пїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ**. пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `Health.Hp == Health.MaxHp` пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ GameObject пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ NeoCondition пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ).
- **TimeReward** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ
  `CooldownRewardExtensions` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅ пїЅ.пїЅ. UPM).
- **InteractiveObject** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Input System (пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ Active Input Handling =
  Input System Package); пїЅпїЅпїЅпїЅ trigger-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ mouse hover raycast (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
  `includeTriggerCollidersInMouseRaycast`).

## [5.8.9] - 2025-02-17

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **TimeReward** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ:
    - `_rewardAvailableOnStart` (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `false`): пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (true) пїЅпїЅпїЅ
      пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (false).
    - `_maxRewardsPerTake`: -1 = пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, 1 = пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅ, N = пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ N пїЅпїЅ пїЅпїЅпїЅ.
    - `GetClaimableCount()` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
    - `OnRewardsClaimed(int)` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ Take.
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `updateTime = 0.2f`.
- **CooldownRewardExtensions** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅ:
    - `GetAccumulatedClaimCount(DateTime, float, DateTime)` пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ;
    - `CapToMaxPerTake(int, int)` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ;
    - `AdvanceLastClaimTime(DateTime, int, float)` пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **TimeReward** пїЅ `TakeReward()` пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `GetClaimableCount()` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅ; пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (flowchart), пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ > пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ,
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **Docs** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `CooldownRewardExtensions.md`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `TimeReward.md`, пїЅпїЅпїЅпїЅпїЅпїЅ `Extensions/README.md`.

## [5.8.8] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Extensions/Time** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
    - `DateTimeExtensions`: `ToRoundTripUtcString`, `TryParseUtcRoundTrip`, `GetSecondsSinceUtc`, `GetSecondsUntilUtc`,
      `EnsureUtc`;
    - `TimeParsingExtensions`: `TryParseDuration` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ SS, MM:SS, HH:MM:SS, DD:HH:MM:SS;
    - `TimeSpanExtensions`: `ToCompactString`, `ToClockString`.
- **TimeReward** пїЅ `GetFormattedTimeLeft`, `TryGetLastRewardTimeUtc`, `GetElapsedSinceLastReward`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  `_displayTimeFormat`, `_displaySeparator`.
- **Timer** пїЅ `Play()`, `SetRemainingTime(float)`, `SetProgress(float)`.
- **TimerObject** пїЅ `SetDuration(float newDuration, bool keepProgress = true)`.
- **TimeToText** пїЅ `TrySetFromString(string raw, string separator = null)`, пїЅпїЅпїЅпїЅпїЅ `_allowNegative`.
- **PrimitiveExtensions.FormatTime** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `trimLeadingZeros` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `01:05` > `1:05`).

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **TimeReward** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `DateTimeExtensions` пїЅ `TimeParsingExtensions`;
  `FormatTime(float, TimeFormat, string, bool)`.
- **Docs** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `TimeFormatting.md`, `DateTimeExtensions.md`, `TimeParsingExtensions.md`,
  `TimeSpanExtensions.md`; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `TimeReward`, `Timer`, `TimerObject`, `TimeToText`, `PrimitiveExtensions`,
  `Tools/Time/README.md`, `Extensions/README.md`.

## [5.8.7] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **GridSystem** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ:
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ: `IsEnabled`, `IsOccupied`, `ContentId`, `FieldCellFlags`;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ shape pipeline: `GridType`, `GridShapeMask` (SO), пїЅпїЅпїЅпїЅпїЅпїЅ override (`DisabledCells`, `ForcedEnabledCells`,
      `BlockedCells`, `ForcedWalkableCells`);
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ origin-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `Origin2D`, `OriginDepth`, `OriginOffset` (пїЅпїЅ
      пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ);
    - pathfinding пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `GridPathfinder` пїЅ `GridPathRequest`, `GridPathResult`, `NoPathReason`;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
        - `Neo.GridSystem.Match3` (`Match3BoardService`, `Match3MatchFinder`, `Match3TileState`);
        - `Neo.GridSystem.TicTacToe` (`TicTacToeBoardService`, `TicTacToeWinChecker`, `TicTacToeCellState`);
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ demo-пїЅпїЅпїЅпїЅпїЅ:
        - `~Samples/Demo/Scenes/GridSystem/GridSystemMatch3Demo.unity`
        - `~Samples/Demo/Scenes/GridSystem/GridSystemTicTacToeDemo.unity`
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ demo setup/UI пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ.
- **Extensions/NumberFormatExtensions** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `int`, `long`, `float`, `double`,
  `decimal`, `BigInteger`:
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `Plain`, `Grouped`, `IdleShort`, `Scientific`;
    - пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `ToEven`, `AwayFromZero`, `ToZero`, `ToPositiveInfinity`, `ToNegativeInfinity`;
    - пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `NumberFormatOptions`;
    - extension API: `ToPrettyString(...)`, `ToIdleString(...)`.
- **Tools/Move** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
    - `PlayerController3DPhysics` пїЅ 3D пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `Rigidbody` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ, mouse-look, lock пїЅпїЅпїЅпїЅпїЅпїЅпїЅ);
    - `PlayerController2DPhysics` пїЅ 2D пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `Rigidbody2D` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ, coyote time, jump buffer,
      optional camera follow);
    - `CursorLockController` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ toggle пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ;
    - `PlayerController3DAnimatorDriver` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ 3D пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (idle/walk/run/jump +
      directional blend);
    - `PlayerController2DAnimatorDriver` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ 2D пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (idle/walk/run/jump + BlendTree
      пїЅпїЅпїЅпїЅпїЅпїЅ HorizontalOnly/TwoAxis).

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **GridSystem API docs** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ XML-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (EN) пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  `Assets/Neoxider/Scripts/GridSystem/**`.
- **GridSystem docs** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Docs/GridSystem.md`: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, shape/passing rules, pathfinding,
  Match3/TicTacToe API, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ demo.
- **PROJECT_SUMMARY** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `GridSystem` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ runtime-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`GridPathfinder`, `GridShapeMask`,
  `Match3/*`, `TicTacToe/*`).
- **Extensions** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
    - `NumberFormatExtensions.ApplySeparators` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Replace`;
    - `RandomExtensions.GetRandomEnumValue` пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ enum;
    - `RandomExtensions.GetRandomWeightedIndex` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ;
    - `StringExtension` пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `ToColorSafe(...)`, пїЅ `ToColor(...)` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ;
    - `PrimitiveExtensions.NormalizeToUnit/Denormalize` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `NaN/Infinity`;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ null-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ XML-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `TransformExtensions` пїЅ `EnumerableExtensions`.
- **SetText** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `Set(float)` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `NumberFormatOptions`;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Inspector;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `SetBigInteger(BigInteger)`, `SetBigInteger(string)`, `SetFormatted(...)`.
- **Docs** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Docs/Tools/Text/README.md` пїЅ `Docs/Tools/Text/SetText.md` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ API пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **Versioning** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `5.8.7` (`package.json`, `Assets/Neoxider/README.md`, `Docs/README.md`,
  `PROJECT_SUMMARY.md`).
- **Tools/Move/CameraConstraint** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ bounds пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ 3 пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `SpriteRenderer` / `BoxCollider2D` / `BoxCollider`;
    - `constraintZ` пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ bounds пїЅпїЅпїЅ perspective-пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ debug gizmo;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `autoUpdateBounds` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ runtime пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅ.
- **Tools/Move player controllers** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ dual input backend:
    - пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ New Input System (`AutoPreferNew`), пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Legacy Input Manager;
    - пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `_inputBackend` (`AutoPreferNew` / `NewInputSystem` / `LegacyInputManager`).
- **Tools docs (Move)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `README` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `CameraConstraint`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
  `PlayerController3DPhysics`, `PlayerController2DPhysics`, `CursorLockController`.
- **InteractiveObject** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ view-gate пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
    - `requireViewForKeyboardInteraction`, `requireDirectLookRay`;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `keyboardInteractionMode` (`ViewOrMouse` / `DistanceOnly`).
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ debug-пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`drawInteractionRayForOneSecond`, `interactionRayDrawDuration`) пїЅ
      пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.
- **Bonus/TimeReward** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ runtime API пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `StartTime`, `StopTime`, `PauseTime`, `ResumeTime`, `RestartTime`,
      `SetRewardAvailableNow`, `RefreshTimeState`, `SetAdditionalKey`;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `IsTimerRunning`, `IsTimerPaused`, `IsRewardAvailable`, `RewardTimeKey`,
      `SaveTimeOnTakeReward`;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `OnTimerStarted`, `OnTimerStopped`, `OnTimerPaused`, `OnTimerResumed`;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅ `saveTimeOnTakeReward = false` пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ
      `StartTime()`;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ UTC round-trip пїЅпїЅпїЅпїЅпїЅпїЅ (`"o"`) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ legacy-пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## [5.8.5] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **TextMoney** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Money`, `LevelMoney`, `AllMoney` (пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `_displayMode`), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ.
- **TextLevel / TextScore** пїЅ UI-пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ enum (`Current/Max` пїЅ `Current/Best`) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `_best`.
- **Docs** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Shop/TextMoney.md`, `Shop.md`, `Docs/README.md`, `PROJECT_SUMMARY.md` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ.
- **Cards** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `DeckComponent`, `HandComponent` пїЅ `BoardComponent`:
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `CardLayoutType` (`Fan`, `Line`, `Stack`, `Grid`, `Slots`, `Scattered`) пїЅ `CardLayoutCalculator`
      пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `CardLayoutSettings` пїЅ `CardAnimationConfig` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ layout/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ;
    - `DeckComponent` пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ pipeline: `BuildVisualStackAsync`, `ShuffleVisualAsync`, `DealToHandAsync` +
      `[Button]`-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`OnVisualStackBuilt`, `OnShuffleVisualStarted`, `OnCardDealt` пїЅ пїЅпїЅ.);
    - `BoardComponent` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `BoardMode` (`Table/Beat`) пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅ `CardLayoutType`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
      пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `Scattered` пїЅпїЅпїЅ "пїЅпїЅпїЅпїЅ";
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ enum: `CardLayoutType`, `ShuffleVisualType`, `StackZSortingStrategy`, `BoardMode`;
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ docs: `Docs/Cards/DeckComponent.md`, `Docs/Cards/BoardComponent.md`.

## [5.8.4] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **NeoxiderPages** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ DOTween Pro (`Runtime/Plugins/DOTweenPro`). пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ,
  пїЅпїЅпїЅ DOTween Pro пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ CS0433 (пїЅпїЅпїЅ `DOTweenAnimation` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ). DOTween Pro
  пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## [5.8.3]

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **UPM Samples** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `package.json`: `"path"` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `Samples~/Demo` пїЅ
  `Samples~/NeoxiderPages` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Unity). пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Samples~/`, пїЅпїЅ-пїЅпїЅ пїЅпїЅпїЅпїЅ Unity пїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ 0 KB).

## [5.8.2]

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **UPM Samples** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ CS0101 (duplicate definition) пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ Demo Scenes пїЅ NeoxiderPages.
  пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ `Samples~`: Unity пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Import пїЅпїЅпїЅпїЅпїЅпїЅ (
  BtnChangePageEditor, PMEditor пїЅ пїЅпїЅ.) пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

---

## [5.8.1] - 2025-02-06

- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ.

## [5.8.0] - 2025-02-06

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **UPM Samples** пїЅ Demo Scenes пїЅ NeoxiderPages пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Package Manager (пїЅпїЅпїЅпїЅпїЅпїЅ Import пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
  Samples). пїЅпїЅпїЅпїЅпїЅ `Samples` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `Assets/Samples/Neoxider Tools/<version>/`.

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **NeoxiderPages (v1.1.0)** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ PageId пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
    - пїЅпїЅпїЅпїЅпїЅпїЅ Buttons пїЅ Dropdown пїЅ PM, UIPage пїЅ BtnChangePage пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ PageId пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (пїЅ пїЅ.пїЅ. пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
      Sample пїЅ `Assets/Samples/...`).
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ PageId (Generate & Assign, Generate Default PageIds, Reset пїЅпїЅ BtnChangePage) пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
      пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ PageId пїЅпїЅпїЅ `Assets/NeoxiderPages/Pages`.

---

## [5.7.0] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **NeoCondition** пїЅ No-Code пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ `Neo.Condition`)
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Inspector пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ dropdown пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - AND/OR пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (NOT) пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: Manual, EveryFrame, Interval
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ: int, float, bool, string
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: OnTrue, OnFalse, OnResult(bool), OnInvertedResult(bool)
    - Only On Change пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - Play Mode: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Inspector
    - **Source Mode** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ:
        - `Component` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ)
        - `GameObject` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: activeSelf, activeInHierarchy, isStatic, tag, name, layer
    - **Find By Name** пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ GameObject пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (`GameObject.Find`):
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        - Preview пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Edit Mode пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        - **Wait For Object** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ Warning (пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ)
        - **Prefab Preview** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ Project пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅ Editor)
    - **Check On Start** пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `true` (пїЅпїЅпїЅпїЅпїЅ `false`)
    - **пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ null** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Warning-пїЅпїЅпїЅпїЅ (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ EveryFrame)
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `false`
        - пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - **пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Inspector: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (Component), пїЅпїЅпїЅпїЅпїЅ (GameObject), пїЅпїЅпїЅпїЅпїЅпїЅ (Find By Name),
      пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (NOT)
    - пїЅпїЅпїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `ConditionDemoUI`, `ConditionDemoSetup`, `HealthTextDisplay` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Health` пїЅ
      `ScoreManager`)
- **Counter** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (Int/Float), Add/Subtract/Multiply/Divide/Set, Send пїЅпїЅ Payload, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ,
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **[Button] пїЅпїЅпїЅпїЅпїЅпїЅпїЅ** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅ `#if ODIN_INSPECTOR` пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ 43 пїЅпїЅпїЅпїЅпїЅпїЅ. пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
  `[Button]` (Neo.ButtonAttribute), пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅ Odin Inspector, пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ
- **MagneticField** пїЅ Toggle пїЅпїЅпїЅпїЅпїЅпїЅ bool-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (Attract, Repel, ToTarget, ToPoint,
  Direction)
- **MagneticFieldEditor** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ CustomEditorBase (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ Neo), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ serializedObject пїЅ
  OnSceneGUI
- **NeoUpdateChecker** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅ 10 пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ ? пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ 10 пїЅпїЅпїЅпїЅпїЅпїЅ;
  пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ GitHub API rate limit (403); пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ package.json; пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- **CustomEditorBase** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ; пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
  пїЅпїЅпїЅпїЅпїЅпїЅ
- **README.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ UPM
- **Docs/README.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Tools пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- **PROJECT_SUMMARY.md** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (Shapes.cs, Enums.cs), пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ Counter/Loot
- **package.json** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `unityRelease`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ keywords (no-code, state-machine)

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **SingletonCreator** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ CS0108 warning (`title` hides inherited member)
- **GM.set_State** пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ NullReferenceException пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `EM.I` пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ null-conditional
  `?.`)

---

## [5.5.2] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Tools/View/Selector**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
    - `SetRandom()`
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `_useRandomSelection`, `_useNextPreviousAsRandom`
- **NeoxiderPages (Neo.Pages)**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ PageManager пїЅ `Assets/NeoxiderPages/`
    - 2 asmdef: `Neo.Pages` (runtime) пїЅ `Neo.Pages.Editor`
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `Assets/NeoxiderPages/Docs/README.md`
- **Level/TextLevel**: UI пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅ пїЅпїЅпїЅпїЅ `Neo.Tools.SetText`)
- **Tools/Components/TextScore**: UI пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (пїЅпїЅ пїЅпїЅпїЅпїЅ `Neo.Tools.SetText`)

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Tools/View/Selector**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (`_autoUpdateFromChildren = true`)

---

## [5.4.2] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Physics/MagneticField**: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `Direction` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ)
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `direction`, `directionIsLocal`, `directionGizmoDistance`
    - API: `SetDirection(Vector3 newDirection, bool local = true)`
    - Scene View handle: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ)
- **Physics/MagneticField**: Scene View handle пїЅпїЅпїЅ `ToPoint` (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `targetPoint` пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ)
- **Tools/Time/TimerObject**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `looping`
    - `useRandomDuration` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ `looping` (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ), пїЅ пїЅпїЅпїЅ `looping` пїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅ `duration` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ [`randomDurationMin`, `randomDurationMax`]
- **Tools/Time/TimerObject**: пїЅпїЅпїЅпїЅпїЅ `infiniteDuration`
    - пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `looping` пїЅ `useRandomDuration` (пїЅ `OnValidate`)

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Physics/MagneticField**: пїЅпїЅпїЅпїЅпїЅпїЅ `ToTarget`/`ToPoint` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ
- **Docs**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `MagneticField.md` пїЅ `Physics/README.md`
- **Tools/View/Selector**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ `SetFirst()` пїЅпїЅпїЅпїЅпїЅ `SetLast()`

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Tools/Time/TimerObject**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ Auto Actions (no-code пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ)

---

## [5.4.0] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **NPC**: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ CinemachineпїЅ)
    - `NpcNavigation` пїЅ пїЅпїЅпїЅпїЅ?пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ NPC пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅ: `NpcNavAgentModule`, `NpcFollowTargetModule` (пїЅ `movementBounds`), `NpcPatrolModule` (points/zone),
      `NpcAggroFollowModule` (Combined), `NpcAnimationModule`
    - Core?пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ `MonoBehaviour`/пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- **AiNavigation**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ `NpcNavigation`
- **AiNavigation**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ BoxCollider
    - пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ `patrolZone` - пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ BoxCollider пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ Patrol пїЅ Combined
    - пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ patrolZone, пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ patrolPoints пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅ `SetPatrolZone(BoxCollider)` пїЅ `ClearPatrolZone()` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `UsesPatrolZone` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Gizmos (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ)

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Physics пїЅпїЅпїЅпїЅпїЅпїЅ**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Odin Inspector
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `using Sirenix.OdinInspector` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ ExplosiveForce, ImpulseZone, MagneticField

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Physics пїЅпїЅпїЅпїЅпїЅпїЅ**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

---

## [5.3.5] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **InteractiveObject**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅ) пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅ `checkObstacles` (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ) - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ raycast
    - пїЅпїЅпїЅпїЅпїЅ `obstacleLayers` - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ 2D пїЅ 3D пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- **InteractiveObject**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ Down/Up
    - пїЅпїЅпїЅпїЅпїЅпїЅ `onInteractDown` пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ (isHovered), пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- **пїЅпїЅпїЅпїЅпїЅпїЅ Physics**: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - **ExplosiveForce**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        - пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Rigidbody
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ)
        - пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ: AddForce, AddExplosionForce
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅ OnExplode пїЅ OnObjectAffected
    - **ImpulseZone**: пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ)
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅ
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ cooldown
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅ OnObjectEntered пїЅ OnImpulseApplied
    - **MagneticField**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        - пїЅпїЅпїЅпїЅпїЅпїЅ: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ, пїЅ Transform пїЅпїЅпїЅпїЅ, пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ:
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (LayerMask)
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Rigidbody пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
        - UnityEvent пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Gizmos
        - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ API пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        - пїЅпїЅпїЅпїЅпїЅпїЅ XML пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

---

## [5.3.4] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (SaveProvider)**: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `SaveProvider` пїЅ API пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ PlayerPrefs
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `ISaveProvider` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `PlayerPrefsSaveProvider` (пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ) пїЅ `FileSaveProvider` (JSON пїЅпїЅпїЅпїЅпїЅ)
    - ScriptableObject `SaveProviderSettings` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Inspector
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ PlayerPrefs пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ (OnDataSaved, OnDataLoaded, OnKeyChanged)
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- **VisualToggle**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ UnityEvent (On, Off, OnValueChanged)
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Inspector пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Toggle пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Toggle
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ `setOnAwake` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ API пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Toggle(), SetActive(), SetInactive()
    - пїЅпїЅпїЅпїЅпїЅпїЅ XML пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ IsActive пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ/пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **SaveManager**: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ PlayerPrefs
- **GlobalSave**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
- **пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ SaveProvider
    - Money, ScoreManager, TimeReward, Collection, Box, Map, Leaderboard, Shop
- **VisualToggle**: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ ToggleView пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ VisualToggle
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Image, пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ GameObject'пїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **ToggleView**: пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ VisualToggle
    - пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ VisualToggle
    - StarView пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ VisualToggle

---

## [5.3.3] - Unreleased

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **KeyboardMover**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ 3D - пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ (XY/XZ/YZ)
- **MouseMover2D**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ AxisMask пїЅпїЅпїЅ 3D (XZ, YZ, Z)
- **Follow**: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: MoveTowards, Lerp, SmoothDamp, Exponential
    - Distance Control, Deadzone, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ API
- **CameraConstraint**: пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (SpriteRenderer/Collider2D/Collider/Manual), 3D пїЅпїЅпїЅпїЅпїЅпїЅ
- **DistanceChecker**: FixedInterval пїЅпїЅпїЅпїЅпїЅ, ContinuousTracking, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- **InteractiveObject**: useMouseInteraction, useKeyboardInteraction, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- **AiNavigation**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Combined пїЅпїЅпїЅпїЅпїЅ
    - walkSpeed/runSpeed, SetRunning()
    - пїЅпїЅпїЅпїЅпїЅпїЅ: FollowTarget, Patrol, Combined
    - Combined: aggroDistance, maxFollowDistance
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ API

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Follow**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ Lerp, Time.smoothDeltaTime, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
- **AiNavigation**: пїЅпїЅпїЅпїЅ "GetRemainingDistance", пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ isOnNavMesh
- **FindAndRemoveMissingScriptsWindow**: пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **MoveController**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- sqrMagnitude пїЅ DistanceChecker, AiNavigation (~20-30%)
- пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Animator пїЅ AiNavigation (~10-15%)

---

## [5.3.2]

---

## [5.3.1]

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **DrunkardGame**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ "пїЅпїЅпїЅпїЅпїЅпїЅпїЅ" пїЅ no-code пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ HandComponent пїЅ BoardComponent
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Player Goes First` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ
- **DeckConfig**: `GameDeckType` пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ (36/52/54)
- **HandComponent**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅ `OnCardCountChanged(int)`, пїЅпїЅпїЅпїЅпїЅпїЅ `DrawFirst()/DrawRandom()`, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ `Add To Bottom`
- **CardComponent**: пїЅпїЅпїЅпїЅпїЅпїЅ `UpdateOriginalTransform()`, `ResetHover()`
- **TypewriterEffectComponent**: пїЅпїЅпїЅпїЅпїЅ `Play()` пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ TMP_Text пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
- **CustomEditor**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ (пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ Tools > Neoxider > Visual Settings)

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **CardComponent**: Hover Scale пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ (0.1 = +10%)
- **HandComponent**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **CardComponent**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ FlipAsync, hover пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
- **HandComponent**: NullReferenceException пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ Awake
- **DrunkardGame**: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
- **CustomEditorBase**: OnValidate пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ Edit Mode

---

## [5.3.0]

### пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ

- **Cards**: пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅ MVP пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - `CardData` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - `DeckModel`, `HandModel` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅ
    - `CardView`, `DeckView`, `HandView` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ DOTween
    - `CardPresenter`, `DeckPresenter`, `HandPresenter` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ MVP
    - `DeckConfig` пїЅ ScriptableObject пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
    - No-code пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `CardComponent`, `DeckComponent`, `HandComponent`, `BoardComponent`
    - пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ: 36, 52, 54 пїЅпїЅпїЅпїЅпїЅ
    - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ: Fan, Line, Stack, Grid
    - пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ: `Beats()`, `CanCover()` пїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
    - **Poker**: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        - `PokerCombination` пїЅ пїЅпїЅ HighCard пїЅпїЅ RoyalFlush
        - `PokerHandEvaluator` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ 5-7 пїЅпїЅпїЅпїЅ
        - `PokerRules` пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ, пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ, Texas Hold'em

---

(Previous versions...)
