# Native source review A

Status: source review complete for all 25 assigned original owners. Corrections and execution acceptance remain open. No Unity, compiler, test, package validator, dependency installation or service has been run in this phase. No product source, public documentation, settings or assets are modified by this reviewer.

## Review basis and limits

Current branch: `demo`; base `ca198556`; target version `11.0.0` with current package metadata still `10.19.0`. The source barrier is recorded at 2026-10-23 08:16:03 UTC in `Local/Audits/assembly-2026-10-23/review-snapshot.json` (SHA256 `2ed6071d03ff7e5ba29aef081a8cb50efc17cf9fbdd7a07d9911cff398098217`; tracked patch SHA256 `32922b26a009df3ca00a93f1494df79e420c5468c048d5b38e3ed8a4c940662d`). Historical preliminary compiler diagnostics are leads only. Final compilation, runtime and visual behavior remain unverified.

Control artifacts read in full:

- `AGENTS.md` — `19e4e16eb1be1463ad4937584e19db783864a2009317ac5fd448eed631ac240f`.
- `Docs/Audits/2026-10-23/NATIVE-REVIEW-CONTRACT.md` — `c1bb559c242ee21a44542abacc013cba2f7955d64c154350a4f336cbd11f1edd`.
- `Docs/Audits/2026-10-23/NativeReview-A.assignment.json` — `12ce745fc42039bc20cae20894b55cef003bf7ee5692800e4e8cf4e1b9a59827`.
- `Docs/Audits/2026-10-23/INTERACTION-QUERY-DESIGN.md` — `c2d83ac281407ff36f897c50a1dbfef5d289447d8d64bde06d5dc2df9e5b90d1`.
- `Docs/Audits/2026-10-23/V11-RELEASE-PLAN.md` — `c95eefb8722c37e197ed00a47165818a811762b06a02821f4a47dede0a90fd34`.

Read coverage below distinguishes fully read artifacts and inspected source from enumeration alone. A completed source review is not an execution pass. Findings require root confirmation before the original owners implement corrections.

## Confirmed remaining defects

### A-01 — P1: identical camera queries still scale with target count

Owner: `Neo.Tools.InteractableObject`, original task `neo-audit-1023-b07-03`; Core collaboration only for a genuinely needed leaf contract. Current source: `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs:223` (`Update`), `:439` (`UpdateMouseTargetRaycast`), `:1069` (3D camera query), `:1084` (2D camera query).

Trigger: 100 or 1,000 enabled targets use the same camera/ray/dimension/trigger context. Every target calls its own physical query; `Both` can execute a second query per target at line 471. Expected: one warmed physical query per distinct raw context, independent of target count. Actual: O(N) identical camera queries, plus repeated main-camera/provider resolution and input sampling. This is the user's confirmed mandatory correction, and the amended plan correctly acknowledges that it is still unimplemented.

Smallest repair: share raw hit storage/query execution through the existing provider and a small backend keyed by the **actual physics query** (world/scene, kind, exact geometry/distance, physical mask, trigger/global-2D policy, epoch). Consumers retain per-target classification, eligibility and transitions. Do not include target identity, ignored target/rig hierarchy, tag or target-specific `IsTarget` in the raw-query key when those values affect only post-processing. Do not select one global winner: current `checkObstacles = false` can accept multiple targets along a ray. Preserve separately directed LOS queries at lines 575/595/605 and keyboard aim at 664/698; they may share only genuinely identical contexts. Later counter/profiler evidence is required; none ran during this review.

### A-02 — P2: saturated interaction/LOS buffers are accepted as complete

Owner: `neo-audit-1023-b07-03`. Current source: `InteractiveObject.cs:162–164`, query sites `:575`, `:595`, `:605`, `:664`, `:698`, `:1069`, `:1084`, and `BuildInteractionHits3D/2D` at `:1096` / `:1119`.

Trigger: more than 16 hits in one applicable physics query. Expected: evaluate the complete target/blocker set or explicitly reject an incomplete result. Actual: every returned populated prefix is reduced without detecting saturation, so an omitted target can disappear or an omitted nearer wall can permit interaction. This is independent of sorting the returned prefix; the missing hit cannot be reconstructed in Core.

Smallest repair: complete-buffer handling in the shared backend for every query kind, with reusable growth/retry or an equivalent complete fallback and explicit lifetime/completeness. Preserve each original mask, trigger policy and 2D global flags; expose/count all native retry/fallback calls separately. Never adopt truncated data as a complete snapshot. The plan's absolute cold budget of two calls is an implementation proposal, not a correctness requirement that can waive completeness.

### A-03 — P2: reentrant hover callback emits an obsolete state after reset

Owner: `neo-audit-1023-b07-03`. Current source: `InteractiveObject.cs:478–495`, `SetHoverState`; reset routes at `:277–295` and `UseHoverDetection` at `:1343–1353`.

Trigger: an `onHoverEnter` listener sets `UseHoverDetection = false` (or disables the component so `OnDisable` resets hover). The nested transition sets `IsHovered = false`, emits exit and `onHoverChanged(false)`. The outer call then unconditionally emits `onHoverChanged(true)` at line 495. Expected: final notifications reflect the final non-hovered state and no stale enter-state notification follows cleanup. Actual: listeners can relight a disabled target while `IsHovered` is false. The reverse exit/reenter path has the same obsolete-notification pattern.

Smallest repair: version/serialize hover transitions and suppress the remainder of a superseded transition after callbacks; define the reentry order while preserving ordinary enter/exit ordering. Add focused public event-sequence regressions for enter→disable/hover-off and exit→reenter. Existing tests cover sequential repeated calls but never mutate availability from a callback. This is source-proven control flow, not an executed Unity reproduction.

### A-04 — P2: nested XP callbacks replace the outer level-transition payload

Owner: `Neo.Core`, original task `neo-audit-1023-b02-08`. Current source: `Assets/Neoxider/Scripts/Core/Level/Domain/LevelModel.cs:124–130`, `AddXp`; read SHA256 `1e21c447a0686157bb78a9fe8050fbb985117a5b5d8448b924ca802c35ee5f0a`.

Trigger: on the default 100-XP curve, an `OnXpGained` listener performs one nested `AddXp(100)`, and the outer caller also adds 100 from zero. Expected operation transitions are nested `(2,3)` and outer `(1,2)` if the existing XP-before-level callback order is retained. Actual source flow emits nested `(2,3)` then outer `(1,3)`, because the outer code rereads the live `CurrentLevel` after listeners. A listener that resets the level can instead suppress or invent the outer level event. Consumers can grant incorrect level rewards or receive a payload for a different operation.

Smallest repair: capture the operation's resulting level immediately after recomputation, before `OnXpGained`, and compare/report that captured value; retain ordinary event ordering. Add public event-sequence regressions for nested AddXp and a listener resetting state. The existing level fixture observes arithmetic/events without mutation from its XP callback, so it does not cover this case. Source proof only; no test ran.

### A-05 — P2: re-registering the same resource entry bypasses tick identity protection

Owner: `Neo.Core`, original task `neo-audit-1023-b02-08`. Current source: `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolModel.cs:272` and `:288`, `Tick`; registration resets timers in `AddPool` but both tick guards compare only `ReferenceEquals`. Read source SHA256 `c831ad29e6c30cd0eee29bcf67f9d43a631239bba16022701f3abd2149c66857`.

Trigger: a pool has regen and discrete healing. Its first regen Changed listener removes the id, sets the retained caller-owned `ResourcePoolEntry.Current` to 7, and registers **the same entry instance** under that id. Expected: the new registration starts on the next Tick, as the public guide promises; current remains 7 for this tick. Actual: the reference check at line 288 passes, the reset heal timer receives the old tick's entire delta, and a 2-point/1-second heal yields 9 immediately. Re-registering another snapshotted pool with its same instance has the equivalent pre-loop issue.

Smallest repair: include a registration generation/token in the tick snapshot and validate it before each stage, incrementing/replacing the token on AddPool/remove; retain the legacy mutable-entry API and timer resets. Add same-instance remove/readd and same-instance replacement regressions. Existing coverage replaces an entry with a **different** object, so it misses this supported caller-owned alias case. This is a source-proven gap in the new tick protection, not a request to implement the broader owned-state proposal.

### A-06 — P2: first health-pool callback sees uninitialized state in later pools

Owner: `Neo.Core.Resources.Components`, original task `neo-audit-1023-b01-05`. Current source: `Assets/Neoxider/Scripts/Core/Resources/Components/HealthComponent.cs:210–211` and `:382–396`, `EnsureInitialized` / `SyncAllReactive`; later-pool constructor defaults are in `ResourceEntryInspector.cs:30–36`. Health source SHA256 `cc94c14738a2608a2ed0d4a4a8a5768fc9d4b4f89db7e9e867db45c345ab68fd`.

Trigger: initialize HP 40/80 followed by Mana 12/48, both without restore, and read `ManaCurrentValue`/max/percent from HP's first CurrentState listener. Expected: 12 / 48 / 0.25, consistent with the already-built model. Actual: initialization is marked complete but SyncAllReactive stages/notifies one entry at a time, so the later pool still exposes 100 / 100 / 1 through reactive convenience getters. A null later reactive object can also remain unrecreated at that point. The same owner's plan already identifies this remaining defect; independent source inspection confirms it.

Smallest repair: stage all valid pools, including missing reactive objects, before publishing any initial pool notifications, then preserve one initial notification per state and the readiness/reentry guard. Add cross-pool callback assertions for authored and loaded values. Do not expand this into atomic explicit Load, which remains a documented separate behavior.

### A-07 — P2: reward consumer retains the old float timestamp advancement

Owner: `Neo.Bonus`, original task `neo-audit-1023-b02-04` (cross-group consumer); Extensions supplies the already-correct helper. Current source: `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs:277–284`, private `AdvanceLastClaimTime`, called by TakeReward at `:323`; read SHA256 `8c74e4920b6a77f930588b4288d8a423f69ec335b0f2b651b8cfeeeef6186c5e`.

Trigger: 16,777,217 claims with a one-second cooldown. Expected timestamp advancement is 16,777,217 seconds. Actual source first multiplies int count by float cooldown, losing one second before AddSeconds. Large date advances can still escape the extension's new saturation path. The accumulated-count helper has **already** been delegated; do not repeat that completed correction.

Smallest repair: delegate only timestamp advancement to the existing `lastClaimUtc.AdvanceLastClaimTime(claimsGiven, cooldownSeconds)` and add a bounded public TakeReward/persisted-timestamp regression. Do not invoke millions of events merely to test large-count precision; the helper's large-count fixture already supplies that arithmetic case. Preserve save keys, event semantics and reward accounting. This is an independently confirmed source residual already identified by the Extensions plans; the Bonus audit itself is outside this review assignment.

### A-08 — P2: Inventory serializes a Sprite owned by the transient preview cache

Owner: `Neo.Tools.Inventory`, original task `neo-audit-1023-b07-05`; coordinate with supplemental Extensions task `neo-audit-1023-extra-02`. Current source: `Assets/Neoxider/Scripts/Tools/Inventory/Data/InventoryItemData.cs:62–72`, `OnValidate`; read SHA256 `18765663fff95ff1ec802b9952a785b19fc9848d4d8afdd9063f2d2d0caf3024`. Preview ownership/release is in `Assets/Neoxider/Scripts/Extensions/PrefabPreviewExtensions.cs`, read SHA256 `ae984eed918606ad7bbd33d36392269abf842756e2aef28731ee23dc2cdc25e0`.

Trigger: an item has no authored icon and a prefab without an authored Sprite; OnValidate stores `GetPreviewSprite()` in serialized `_icon`. The fallback Sprite is now HideAndDontSave and is explicitly destroyed by cache clear/lifecycle cleanup. Expected: persistent item data retains authored assets only, while UI obtains a fresh temporary fallback. Actual: persistent configuration holds a cache-owned invalidatable object. Save/reload/visual consequences have not been executed, so no persisted-corruption or visual-pass claim is made.

Smallest repair: stop assigning generated thumbnails to `_icon`; preserve authored/borrowed assets and let views request temporary fallbacks while binding. InventoryItemView already contains a fallback path, to be reviewed with the rest of Inventory. Confirm cache clear/rebind and authored-asset lifetime later; do not undo cache disposal to retain an invalid ownership relationship.

### A-09 — P3: primary Extensions layout regression leaks Unity RNG state

Owner: `Neo.Extensions`, original native task `/root/audit_extensions`. Current source: `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023ExtensionsTests.cs`, `Layout_OneShotSource_IsEnumeratedOnce`, `sphere` case. It calls `LayoutUtils.GetSphereSurface`, whose implementation consumes `UnityEngine.Random.onUnitSphere` at `Assets/Neoxider/Scripts/Extensions/LayoutUtils.cs:140`. The fixture creates/destroys its objects and deletes its PlayerPrefs key, but never snapshots/restores `UnityEngine.Random.state`.

Expected: a regression fixture leaves the prior shared RNG state intact. Actual: running the sphere case changes subsequent global random draws, making unrelated tests depend on execution order. Smallest repair: snapshot RNG state in setup and restore it in teardown, including failed cases, as the Numeric fixture already does. This is a source-level isolation defect; no order-dependent runtime failure was executed or claimed.

### A-10 — P1: invalid authority enum values fail open

Owner: `Neo.Network`, original task `neo-audit-1023-b04-05`; Contracts owner `neo-audit-1023-b01-03` supplies declarations/docs. Current source: `Assets/Neoxider/Scripts/Network/Core/NeoNetworkState.cs:160–179`, `IsAuthorized`; SHA256 `072340a0e1a526336e46e94a62dae5ab05ecc11a4ebd80e95636a14dd3ea50e4`.

Trigger: a configured/cast/deserialized mode is -1, 3 or int.MaxValue. Expected: unknown policies reject every caller category. Actual: `default: return true` admits the call, including an unrelated remote sender. No remotely controllable policy/exploit is alleged; the reusable authority gate itself demonstrably accepts invalid configuration.

Smallest repair: fail closed for unknown modes before any server/host allowance; the current implementation switches before named-policy allowances, so a rejecting default is sufficient if that structure is retained. Preserve exact named-policy and `sender == NetworkServer.localConnection` behavior. Cover remote owner/nonowner, direct server, host, null target/missing identity and invalid values. The optional pure policy extraction is not required. Link `Contracts.md` from the canonical Network README and document the deliberate invalid-input change.

### A-11 — P2: reflection sync caches unusable accessors/types

Owner: `neo-audit-1023-b04-05`. Current source: `Assets/Neoxider/Scripts/Network/Core/NetworkPropertySync.cs:404–470`, `ResolveMember` / `ReadValue` / `WriteValue`; SHA256 `3960207c8c072150d7e19e1fda675ded8de192798120c029dc665a81eb42b6f9`.

Trigger: bind `_fieldName = "Item"` to an indexed property, choose a missing getter/setter, readonly field, or a value type that cannot be written from the selected payload. Expected: reject invalid binding without gameplay/outbound mutation and report one gated configuration diagnostic. Actual: any matching FieldInfo/PropertyInfo is cached; parameterless GetValue/SetValue and unchecked payload assignment can throw repeatedly or silently leave the target unchanged.

Smallest repair: validate index parameters, required accessors and supported read/write compatibility before caching/using the binding. Preserve intentionally supported conversions only where both directions are defined; introduce no silent narrowing or new generic endpoint framework. Add behavior regressions for indexed/write-only/read-only/incompatible targets and controls for the five supported types. The same generic property-get path in `NetworkReactiveSync.ResolveReactive` should reject indexed/inaccessible candidates rather than invoking them blindly.

### A-12 — P2: a temporarily null reactive instance is cached permanently absent

Owner: `neo-audit-1023-b04-05`. Current source: `Assets/Neoxider/Scripts/Network/Core/NetworkReactiveSync.cs:294–334`, especially `_cacheResolved = true` at line 331; SHA256 `1fe706eea2b335718a00c7623b60b5521cbd6ba39176c3922b0a6fa5043d66ff`.

Trigger: the target's valid reactive field is null when initial state/hook/poll first resolves it, then the target creates its ReactivePropertyInt/Float/Bool during normal initialization. Expected: the next accepted update can bind and change the new instance once. Actual: the terminal negative cache suppresses all later resolution and updates remain lost.

Smallest repair: distinguish resolved member metadata from instance readiness; do not permanently cache a missing instance. Retry at the bounded poll/apply boundary and keep diagnostics gated once. Test null→instance plus a subsequent accepted update. Replaying a previously received snapshot without a new update is a separate explicit hydration policy, not required to repair this case.

### A-13 — P2: networking budgets/timeouts freeze with gameplay time

Owner: `neo-audit-1023-b04-05`. Current source: `NeoNetworkComponent.cs:38–79` for both rate limits; `NetworkPropertySync.cs:75,107,150` and the other payload branches; `NetworkReactiveSync.cs:64,84,126` and other branches; `Lobby/NeoNetworkDiscovery.cs:99–132`.

Trigger: Time.timeScale becomes zero after a command or while QuickPlay/polling is pending. Expected: real networking rate windows, discovery refresh and QuickPlay timeout continue while explicitly configured gameplay pause/authority guards retain their own meaning. Actual: Time.time stops, so an accepted command can permanently exhaust its window, sync checks stop and discovery never reaches its timeout. Float clocks also lose sub-interval precision in long sessions.

Smallest repair: use one consistent unscaled monotonic double clock for these network intervals and stored timestamps. Preserve immediate-repeat rejection and all gameplay ownership/direction/pause gates; do not globally convert game timers to realtime. Add paused-window/timeout regressions restoring timeScale in teardown. No runtime pause test ran in this review.

### A-14 — P2: unchanged bindings bypass the configured polling interval

Owner: `neo-audit-1023-b04-05`. Current source: `NetworkPropertySync.cs:99–230` and `NetworkReactiveSync.cs:76–180`, `Update`.

Trigger: an eligible binding remains unchanged after `_syncInterval` elapses. Expected: the Inspector's documented check cadence bounds capture/read attempts. Actual: `_lastSyncTime` advances only inside dirty branches. Once the gate opens, every subsequent frame polls the static value; property sync repeatedly reflects/boxes at N×FPS for N static adapters.

Smallest repair: advance the eligible poll timestamp independently of dirty/send success, using the same unscaled double clock as A-13, while retaining direction/readiness gates. Keep the configured send minimum and retry behavior coherent. A getter-count regression can prove a second same-window Update does not reread an unchanged target. This is aggregate-cost control flow evidence, not a measured profiler result.

### A-15 — P1: queued action commands ignore the server's local-only switch

Owner: `neo-audit-1023-b04-05`. Current source: `Assets/Neoxider/Scripts/Network/Core/NetworkActionRelay.cs:348–408`, all three `CmdTrigger*` handlers.

Trigger: the server sets `isNetworked = false` while a previously valid client command is queued, or a peer sends that message to the still-spawned identity. Expected: remote ingress stops; local public APIs retain local-only operation. Actual: handlers check budget/index/authority but never `isNetworked`, then dispatch/broadcast the action. NetworkContextActionRelay already rejects disabled/non-networked ingress, so this is an inconsistent safety boundary.

Smallest repair: reject non-networked ingress in all three handlers before budget consumption/mutation, retaining named authority/channel scope behavior. Add woven-body regressions that toggle the server flag and observe zero server/client outputs; real admission remains separate evidence. No external exploit was executed.

### A-16 — P2: disposing an old handler set removes another live set's replacement

Owner: `neo-audit-1023-b04-05`. Current source: `Realtime/NetClientHandlers.cs:68–77,104–116,152` and `Realtime/NetServerHandlers.cs:63–73,97–109,142`.

Trigger: set A registers a message T; set B registers/replaces T; then A.Dispose/UnregisterAll runs while B remains alive. Expected: A releases only registration it still owns, or duplicate ownership is explicitly rejected. Actual: A unconditionally calls UnregisterHandler<T>, deleting B's current handler. Local Mirror source confirms Replace overwrites one global dictionary slot and Unregister removes it without owner matching (`Assets/Mirror/Core/NetworkClient.cs:584,606–611`; `NetworkServer.cs:994,1010–1014`). No wire delivery is needed to establish this dictionary ownership mismatch.

Smallest repair: make unregistration conditional on the registration token/delegate still installed by that set, or explicitly reject conflicting ownership with a documented compatible policy. Do not silently add message multiplexing, transport changes or a replication framework. Add two-set lifecycle/reconnect regressions and document whether an older registration is restored when a newer one ends; that restoration policy is a root decision separate from preventing one disposed owner from deleting another's registration.

No claim of absence is made for unreviewed modules.

## Module decisions and incremental coverage

All 25 assignment entries are fully reviewed below, with current artifact hashes, changed-source/new-regression read coverage and explicit dispositions.

### Neo.Tools.InteractableObject.Core — source review complete

Original owner: `neo-audit-1023-b07-04`.

Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Tools.InteractableObject.Core.md` — `0b7bc196ec693455d22247aade9d0d57cb0b2136deb7f145ddfe803223a91d1f`.
- `Docs/Audits/2026-10-23/Neo.Tools.InteractableObject.Core.plan.md` — `eb6eeb1273bd70965cc4deb64280e678f7c2609556beef5b5df79902dbfb4751`.

Source coverage: every changed `InteractionQueryMath.cs` hunk, complete current camera resolver/interface/hit DTO and leaf asmdef; complete new `Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectCoreQueryMathTests.cs` (`3fb2be3a6bf15bd21b2c390e2d06fae628a2389b6ff63b460f1c3ae39aeaa35b`); complete `Assets/Neoxider/Docs/Tools/InteractableObject/Core/README.md`. Parent adapter call paths were inspected as read-only integration context.

Disposition: **completed fixes appear correct by source inspection**. Promoting coordinate subtraction before squaring to double handles the finite-float scale cases; positive-infinity sentinels admit maximum finite distances while invalid hit distances remain filtered. Tests assert observable Boolean/hit outputs with known geometry and stale-tail markers; no runtime objects or shared state require cleanup. No Update/search/allocation loop was added. Existing blocked-target output, first-entry ties and inclusive blocker rule remain intact.

The indexed richer selection result is a **future proposal**, not a blocker for shared raw-query reuse. Keep `IInteractiveTarget` unchanged. Camera disabled/sticky-cache behavior is an existing documented policy, not a confirmed bug. **Needs evidence:** supported-version compile and ordinary-scale boundary/runtime integration, after the barrier. **Docs follow-up:** the parent module entry still lacks a direct link to `Core/README.md`; root/parent owner should integrate the canonical leaf entry. The Core guide's final scene step should distinguish a custom adapter's controllable camera cache from the current concrete `InteractiveObject`, which exposes no public camera-cache setter/reset API yet.

### Neo.Tools.InteractableObject — source review complete; corrections required

Original owner: `neo-audit-1023-b07-03`.

Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Tools.InteractableObject.md` — `70d31f93d9655aba9d94c5b2027d94c489521a447e0df6251c3654a7f643b6bf`.
- `Docs/Audits/2026-10-23/Neo.Tools.InteractableObject.plan.md` — `786e5418b11739f640b6c4a387532a9b69f9c3d8d0901e9a1a68eb7e71c5641f` (amended shared-query plan).

Source coverage: all changed hunks across `InteractionRayProvider.cs`, `InteractiveObject.cs`, `Internal/InteractiveObjectSceneSetup.cs`, `PhysicsEvents2D.cs`, `PhysicsEvents3D.cs`; complete `InteractiveObject` call paths/fields/public API and current leaf dependencies; unchanged `ToggleObject` behavior; both asmdefs; tag/layer filtering and RPC payload/host gating context. `InteractiveObject.cs` read SHA256: `39e48e6d4f8788c8bf9c3349077b804d7ce6a2682944aa1cf68e639860299c7e`.

Complete new regression reads:

- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectInteractionTests.cs` — `4ce463984d17c26a831cc2a4619bdb9e675d4ef853fb6ff856e5ffdf8dd412eb`.
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsInteractableObjectPhysicsEventsTests.cs` — `a1fb3dcea2af450154ac3e666fa653003d4c53157112a2b6a29076f45035925b`.

All six existing public component/module guides and the new Core guide were read. Tests observe hover sequences, click counts, selected-camera identity and payload/filter effects; their created objects/render texture are destroyed and prior main-camera tags restored. `SendMessage` fixtures check callback boundaries, not native delivery; device-dependent Both behavior and real Mirror/collision routing remain **needs evidence**. No query-count/saturation or callback-reentry regression currently exists.

Disposition: offline collision dispatch, sender-before-budget checks, split hover/click ray selection, viewport center, provider refresh, UI click eligibility and shared-root filtering are **completed fixes that appear locally correct**. Full buffers/shared camera query are **confirmed remaining defects** A-01/A-02; callback reentry is A-03. Native collision/RPC/pointer/lifecycle execution remains unverified.

Aggregate cost: normal automatic interaction performs per-target camera physics and multiple target-specific eligibility LOS checks; `Both` adds a distinct camera ray. `Update` also polls range every frame for all targets to preserve range events. Public `IsInInteractionRange` performs a fresh LOS query, so additional callers can multiply that cost. Main-camera/provider resolution repeats around hover/click and pointer handlers. Fixed hit arrays are retained per target; no new per-frame result-array allocation was introduced, but their bounded capacity is incorrect. Scene raycaster setup calls `GetComponents` once per authoring/awake target, not every frame. Raw collision relay filters run on the actual callback GameObject and remain a separate channel.

Plan decision: **approve the required shared raw-hit capability and completeness/invalidation acceptance; reject the mandatory controller/registry/session migration and new scene wiring**. Root independently confirmed this decision after reading the same amended plan. The authoritative first implementation uses the existing provider plus a small shared backend; a lazy first consumer query followed by cache hits is valid. Targets retain their current eligibility/hover/input/network policies. `source-rig exclusion` is a post-filter here and must not split otherwise identical physics queries; `obstacleLayers` retains its LOS meaning. Preserve exact initial query masks/policies; do not silently introduce all-layer superset changes, approximate geometry keys or a single-selected-target snapshot. No mandatory new Core API or registration is required. New controller/capture/session behavior remains a **future proposal**. Later complete uncached parity and 1/100/1000 query counters are both required.

### Neo.Core.Level.Bridge — source review complete

Original owner: `neo-audit-1023-b03-01`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Core.Level.Bridge.md` — `dc5f666eab31b9c9924c4b9edf6a913c027a797c49425fafc8f7f9d203c6a81d`.
- `Docs/Audits/2026-10-23/Neo.Core.Level.Bridge.plan.md` — `16423b1143abe94aa0904fe919530f9d69dfa766db1362e386401b64b245cdfe`.
- `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelBridgeTests.cs` — `41dd291ed58ddbd371e7c30fb286ff9383ae033bf5397f613f85d42a78a47219`.

All changed source hunks and complete current `LevelConditionAdapter`/`LevelNoCodeAction` were inspected, together with the new fixture and both public-guide diffs. **Completed fixes appear correct:** missing-provider inversion and `LastResult` now agree; action result is sampled before success listeners so nested calls cannot replace the outer bridge payload. Tests protect real return/state/event sequence contracts, arrange existing Inspector configuration only, disable persistence and destroy owned hosts/assets. Downward SetLevel and success for a supported unchanged invocation remain documented behavior. No Update, global lookup, allocation loop or subscription ownership was added; resolution is local and performed per explicit call.

**Future proposal:** typed provider injection and a plain rule are reasonable additive composition work but are not needed to close the demonstrated defects. Retain concrete serialized references, enum identities and current missing-provider inversion. **Needs evidence:** actual Unity callbacks, destruction fallback and cross-version execution after all source work ends. Core mutation semantics are reviewed separately below; no additional Bridge defect is confirmed at this checkpoint.

### Neo.Core.Level.Components — source review complete

Original owner: `neo-audit-1023-b03-02`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Core.Level.Components.md` — `cec60cc0f21b208e52fbc1afc81a3bb36c9c897f4332b26cd98b3bdf2deed2dc`.
- `Docs/Audits/2026-10-23/Neo.Core.Level.Components.plan.md` — `6ca3630d46bc324e0b93cff1fcd847b7704d986764163fc3da8b408017fb063a`.
- `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelComponentsTests.cs` — `ef62319b7de02209d5fb01dc4632bc407c9e4c84c1498683838d663def8f1346` (current centrally integrated source, rather than the old fixture hash in the audit).

All changed hunks and the complete current `LevelComponent.cs` were inspected; the fixture and canonical-guide diff were read. **Completed fixes appear correct:** initialization state is set before consumer callbacks and reset on escaping initialization failure; restore applies incoming cap before state with this component's forwarding detached in `finally`; early reset/snapshot initialize intentionally; all three mirrors are staged before notifications. No per-frame Update or scene search exists. The model events are attached once and removed on destruction. Snapshot/JSON allocations occur on explicit save/snapshot operations, not per frame. The singleton `SaveProvider` remains an existing shared dependency; the fixture restores its prior provider in `finally` and disables object autosave before teardown.

Tests inspect saved profiles, counts, mirror state inside callbacks and bounded retry/reentry, with assertions outside swallowed reactive callbacks. They do not claim arbitrary mutation-from-callback correctness; the plan explicitly retains that evidence gap. **Future proposal:** options plus an injected profile-store adapter can improve runtime factories but are not a prerequisite for current fixes; preserve caller ownership and default SaveProvider routing. Existing manual pre-model Save/Load no-op and historical downward `OnLevelUp` semantics are documented, not new defects. **Needs evidence:** real disable/re-enable autosave and final integrated Core/Reactive behavior.

### Neo.Core.Level.Data — source review complete

Original owner: `neo-audit-1023-b01-04`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Core.Level.Data.md` — `c9206079d0f8bf8ad3cdbe5415996be1b9257b84554a66b86b50484be9e021ee`.
- `Docs/Audits/2026-10-23/Neo.Core.Level.Data.plan.md` — `fae1c24904a4aae0825de038801ca61927b8bf840e3bc3f06ab2aeff87ca8268`.
- `Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoCoreLevelDataTests.cs` — `6dfde6b543707b27ffe605370d0abbd9e8cfbff6e8ef893de0204824d6fe9877`.

Every changed `LevelCurveDefinition.cs` hunk, full new regression source and full `Assets/Neoxider/Docs/Core/Level/Data/CurveEvaluation.md` were inspected. **Completed fixes appear correct:** upward integer thresholds, subtract-before-saturating arithmetic, exact integer XP comparison using double, consistent floored authored endpoint/10,000 scan cap and absent-Custom-list lookup. The source reads the final key by index instead of copying `.keys`. Tests assert independent threshold/terminal/model outputs and destroy their temporary ScriptableObject. Serialized asset fields and scene independence are retained.

Aggregate cost remains up to 10,000 curve evaluations for each curve-level query, and next-XP may rescan; many component mirror refreshes can multiply it. That existing cost justifies the opt-in immutable threshold snapshot as a **future proposal**, not an implicit cache over publicly mutable `AnimationCurve`/entries. Formula integration belongs to Core and is reviewed separately. Invalid authoring schedules remain a documented precondition unless root approves diagnostics. **Needs evidence:** current Formula/Data parity and deliberate fractional-end behavior under actual Unity; parent-guide backlink integration remains coordinator work.

### Neo.Core — source review complete; two further corrections required

Original owner: `neo-audit-1023-b02-08`. Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Core.md` — `6d197e36308cc46878ac2de1e6a27d167ad5c93c164703ba4b4290e7239a27d7`.
- `Docs/Audits/2026-10-23/Neo.Core.plan.md` — `cfe234b552b01ebc197e83f76cf1e2db348327f90e105170a30d6815789b8e5e`.
- `Docs/Audits/2026-10-23/Neo.Core.correction.md` — `e673aac6ed8673b7b6ed263f007a5115d4a6a9fc76c5a8c26398c314aabcf8c5`.

Every changed source hunk in the seven assigned files was inspected, including all evaluator families, LevelModel saturation/inversion and ResourcePoolModel validation/timer/notification branches. Current source hashes for the two reentry findings are recorded above. The current canonical Core README and changed LevelCurveDefinition guide were read with the wrapper call sites as context.

Complete new regression reads:

- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreLevelTests.cs` — `bde596362aa0bf73e4b15a0f0e632b255a79128206da1ae677464deb344a6fe4`.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourceTests.cs` — `cfb4528253a726e09b1d646fc6bc1274d395631627d7036945a9d0779f1a4c2f`.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreProfileTests.cs` — `9d7b14ff0ffe6e257e547dd115c3377f00ad2ed80b8d84e9509f285cb9c53227`.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourceLifecycleTests.cs` — `3789cfb7fa64fa25280b8ce270db42a4734beaece54ccc1d26b0d764687e3ce3`.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreCanSpendTests.cs` — `23cddb1a0503b1359018ae086bdbcb933026f6efac724480353e10019dc1d53c`.

Disposition: XP saturation/full-range inverse search, promoted numeric thresholds, exact capped-spend validation, actual-delta capture, invalid operation inputs, uncapped restoration and deep cloning are **completed fixes that appear correct by source inspection**. The later operation-local depletion correction is present and its captured transition survives nested mutations. `CanSpend` is a small additive concrete-model query sharing validation with TrySpend; it changes no state, events or timers and does not reserve resources. It leaves third-party provider interfaces intact.

The new pure tests protect meaningful balances, operation deltas, timer remainder, event sequences and immutable cloned data; no Unity/global state needs cleanup. Exception cleanup in Tick is explicitly covered. **Confirmed remaining defects:** A-04 and A-05. Their missing cases are distinct from the already-fixed duplicate depletion and different-instance replacement cases. Runtime numerical/native compilation evidence remains pending.

Aggregate cost: Tick snapshots and scans all model pools on every explicit update using one retained list, with no cycle-count loop; large elapsed intervals are aggregated. Level SetLevel uses bounded doubling/binary search, repeatedly calling the curve evaluator, so expensive custom/AnimationCurve definitions can multiply work. Mutable caller entries remain an existing alias contract; the broad settings/snapshot/change-record API is a **future proposal**, not required for the two minimal reentry repairs. New scheduling or thread-safety claims are not approved by source inspection.

### Neo.Core.Resources.Components — source review complete; planned correction confirmed

Original owner: `neo-audit-1023-b01-05`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Core.Resources.Components.md` — `98281c1c70dbb7b29f43e6868454ca5a9ae67f901c855696bdf9f7e20ef475a3`.
- `Docs/Audits/2026-10-23/Neo.Core.Resources.Components.plan.md` — `f3fa0c8d967660d1649391cf7b0ced83bcd33995701c8cd159f08979f69c9d53`.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourcesComponentsHealthComponentTests.cs` — `d9e55e06da66324286eba20e74c9cfb08e7450e43bd9f27929b1f3d455f0aaa8`.

All HealthComponent changed hunks, current lifecycle/accessor/synchronization paths, complete ResourceEntryInspector and complete new fixture were read, together with the canonical guide diff and Core/resource dependencies. Recursive initial load is removed, model construction is silent, readiness precedes callbacks, same-pool mirrors are staged before their listeners, repeated no-op reactive publications are suppressed and max-change reports the accepted value. These **completed fixes appear correct**, but the cross-pool initial snapshot defect in its plan remains: A-06.

Fixture state/event/JSON assertions are substantive; inactive objects disable autosave, owned objects are destroyed and unique persistence keys deleted. Tests only read the same pool during the first reactive callback, so they do not close A-06. Real Update regeneration, serialization, save/disable/re-enable and callback mutation remain **needs evidence** after source work.

Aggregate cost: every HealthComponent ticks every frame, scanning all its pools even when their timers do not fire. Each changed pool calls `TryGetPoolEntry` at least twice through synchronization/event forwarding; that lookup scans the Inspector list. Initial SyncAllReactive also repeats the lookup for each entry. For P pools with active regen, this produces O(P²) wrapper comparisons on emission frames even though Core Tick is O(P). A small stable case-insensitive first-entry index is a **future performance proposal / profiling lead**, preserving the documented unsupported-duplicate policy and authoring-only list lifetime. It should be considered before adding a broad catalog API purely for performance. No measured timing claim is made.

The readonly catalog/explicit initialization/validation proposal is additive composition work for root to prioritize; it is not necessary to stage initial values safely. Reject silent changes to duplicate precedence or explicit-load atomicity while implementing A-06.

### Neo.Extensions — source review complete with supplemental scopes

Original owner: `/root/audit_extensions`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Extensions.md` — `58a02121d629f5aba4d22625f36b773f5a2ab7db9d513e4cb7edd93726f0655f`.
- `Docs/Audits/2026-10-23/Neo.Extensions.plan.md` — `4f4ffeb4c337b9bf9fe3867cbb695cd6b3d71faf171c51d133aa4795a1d28bda`.
- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023ExtensionsTests.cs` — `824a02760362c1cd2c14d8b8a308eab15a7b4b9a12940bd67aba58011b3a7bd0`.

All changed source hunks across the combined 18 modified utility files were inspected, including the supplemental changes listed below. The full main fixture, changed primary public-guide hunks and both new supplemental guides were read. **Completed fixes appear correct:** finite duration parsing, int Wrap/PingPong, bool fallback, singleton gradient, tracked event replacement, single-pass bounds, one-shot layouts, four-corner viewport bounds and canvas-local conversion. Main fixture assertions observe public outputs and enumeration count; objects/PlayerPrefs key are cleaned. Its RNG cleanup omission is A-09.

Aggregate cost: explicit formatting returns strings and may create BigInteger digit buffers; layouts now allocate an element snapshot, point list and closure per call, trading bounded managed work for correct one-shot semantics. Combined bounds still allocates Unity renderer arrays and LINQ iterators, although it no longer traverses the source multiple times. `UIUtils.IsPointerOverUI` constructs PointerEventData/list and invokes all EventSystem raycasters per call; it should not be described as allocation-free or as a shared interaction query. No autonomous per-frame manager was added. Diagnostics throttle-key growth and preview cache cardinality are profiling/retention leads when their opt-in paths are used, not newly demonstrated runtime failures.

The versioned string-array codec is a **future proposal**. It must keep explicit legacy format selection and bounded decode; it does not justify automatic save rewriting. **Needs evidence / bounded composition correction:** UIUtils still reads legacy `Input.mousePosition` at line 24. An additive screen-position/caller-buffer overload plus an upper-layer `Neo.Tools.Input` adapter can support New-only projects without creating an Extensions→Input cycle. Keep legacy signatures and specify absence-of-device/EventSystem behavior; this phase did not run an alternate input configuration. The two supplemental guide backlinks and affected existing coroutine/preview/depth guide updates remain primary-owner documentation integration work.

### Extensions-Numeric-Time-Random — source review complete

Original owner: `neo-audit-1023-extra-01`. Full artifact reads:

- `Docs/Audits/2026-10-23/Extensions-Numeric-Time-Random.md` — `a13c5ca5385d89b11b5553e444922d988126005980011f42ddf4880138d2fa1c`.
- `Docs/Audits/2026-10-23/Extensions-Numeric-Time-Random.plan.md` — `a06da07e4f60d4d11db150ff4432793f232b78dd06f7551e9a0187bb5aaaa989`.

All changed numeric/time/random hunks were inspected, including readonly value-option copying, signed rounding, decimal decomposition, sticky BigInteger digits, small-double/scientific fallback, finite weight totals, full-int snapping and tick-based timestamp arithmetic. Complete new tests:

- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023NumericFormatTests.cs` — `38dced82a1fce33de7848fb153f2a16e3fa0ce91974212d64ed8593491ff37cc`.
- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023NumericRandomAndPrimitiveTests.cs` — `e95356e79f3cce97397a58d6114f9265cb99d881a181f076dcc3e6f929b08df1`.
- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023NumericTimeTests.cs` — `0e09b06fe7ee20d0ab05731200b62c9cfdc17a49d8c564237c55fdfa5236a9dd`.

**Completed fixes appear correct by source inspection** for the named counterexamples. NumberFormatOptions is a struct, so local rounding/separator changes do not mutate caller options. Boundary tests use independent known numeric/text/date outputs; the RNG fixture restores its prior random state and diagnostics flag. No new scene searches or autonomous updates exist. Weighted draws remain O(n), and selector draws allocate a weights array; the immutable distribution is a **future proposal**, with existing ChanceManager reuse and dependency direction correctly acknowledged.

**Confirmed consumer residual:** A-07; count delegation is already present. **Needs evidence:** Mono E16/subnormal formatting and extreme native Random.Range, seeded-boundary compatibility and new rounding/saturation policies. Further double-rounding edge cases near the decimal routing threshold merit targeted numerical evidence before any additional behavior change; no unexecuted arithmetic probe is claimed as a C# result. The pure formatter/timing code does not prove consumer integration by itself.

### Extensions-Coroutines-Unity — source review complete; native semantics remain unverified

Original owner: `neo-audit-1023-extra-02`. Full artifact reads:

- `Docs/Audits/2026-10-23/Extensions-Coroutines-Unity.md` — `df2bc86a7999503d349fe9b073e670a912f617296dcef4440f64af4196e0aaf3`.
- `Docs/Audits/2026-10-23/Extensions-Coroutines-Unity.plan.md` — `6f35108c5e4cd8c4e6fc8634cf041628f93a940b2727c0dd580bd354087bff2c`.

Every changed supplemental source hunk was read, with the full startup/iterator tree/cleanup/reset/lifecycle call graph and relevant existing helper bodies. Current `CoroutineExtensions.cs` SHA256: `8397679b6a2aada37dd3309b07791c47f641623b283448f2804dcb89146e0fbe`. The complete `LifecycleAndOwnership.md` guide and both complete new fixtures were read:

- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityTests.cs` — `16cec2059e6322a134f5b15dc395997b21b55b70700e55cee5c11796e79cc57d`.
- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023CoroutineUnityLifecycleTests.cs` — `7ae920631e57100b9b3050834b793225854413c03ecaf848bc111f6189f955ab`.

Disposition: **completed fixes appear structurally sound**, with terminal state before cleanup, synchronous-completion protection, tracked nested disposal, owner destruction registration, active-hierarchy rejection, idempotent lifecycle cancellation, generated-only Sprite release and target lifetime checks. Global audio fades retain independent ownership/scaled time. Raw native StopCoroutine observability is a documented API limitation, not a defect that can be repaired by guessing whether a native Coroutine is alive.

Tests drive meaningful callback/disposal/order/lifetime state, track owned handles/objects and remove only new global helpers; Editor tests entering Play Mode remain an actual runtime driver when later executed. **Needs evidence:** native custom-wait/fixed/end-of-frame phases, cancellation during MoveNext/Current, reset/cleanup exceptions, raw stop→tracked Stop, and domain reload on/off. In particular ReleaseRoutine can be reached from native wrapper finalization while Advance is active; the claimed deferred-disposal contract must be checked through that native cancellation path rather than inferred from the `DisposeRoutines` guard alone. No executed defect is alleged without that evidence.

Per-operation list/proxy/cancellation-registration allocations and lifecycle snapshot allocation remain unmeasured; do not introduce a new scheduler or pooling solely from this review. CoroutineScope is a **future proposal**. Preview consumer ownership requires A-08, and ScreenPositioner's orthographic near-plane depth must be reconciled by its original Move owner using explicit migration notes, without rewriting serialized scene values automatically.

### Neo.Network.Contracts — source review complete; cross-owner fail-closed repair required

Original owner: `neo-audit-1023-b01-03`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Network.Contracts.md` — `51cd74dd0d92c28a57cbd6b0f1f659f43f736ebfd90c49f03ca03b8756ffa0b1`.
- `Docs/Audits/2026-10-23/Neo.Network.Contracts.plan.md` — `06ddd7d220f76c5bb1b587735f8c3af7aef9be6328fd90b69fd82ed5332b91d8`.
- `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkContractsTests.cs` — `f2d543f0478a6d053d0546ecd898b2d4108e7cf14f1ec86d09a47578f8b1e45c`.

Both source diffs and complete new regression/Contracts guide were read with the actual NeoNetworkState consumer. Changes are XML/documentation only; enum values and marker ABI are retained. Serialization tests use historical integer fixtures and an independent integer DTO, rather than merely restating enum declarations. They own no persistent/global resources. No Update/search/allocation work exists in these declarations.

**Completed docs fixes appear correct. Confirmed cross-owner defect:** A-10, still present in Network. Reject waiting for an optional pure-policy extraction before failing closed. Preserve named host behavior and keep Contracts free of a reverse implementation dependency. The Network README backlink is still missing. Runtime/editor scene reactivation and the optional-dependency matrix remain **needs evidence**; a Mirror-free test source does not make the shared test assembly Mirror-free.

### Neo.Network — source review complete; corrections required

Original owner: `neo-audit-1023-b04-05`. Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Network.md` — `8247ee882056c3fda0165a8f9acb97601216882b17422648d55c139961e62544`.
- `Docs/Audits/2026-10-23/Neo.Network.plan.md` — `26a16e527eb51256854cb5291798ddac779abd9925d5521acf96cff002b6e06c`.

Every changed hunk in the 12 owned sources was read, along with full relevant reflection/poll/rate-limit/discovery/authority/handler ownership paths. All Update/search/allocation sites were enumerated and the hot sync/polling paths inspected. All 12 changed public-guide diffs were read. Complete new regression sources:

- `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkRealtimeTests.cs` — `25ffd573d0750d50b3aee05faaa88c4934842541168db0d1990883d8f97852f3`.
- `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkSceneTests.cs` — `a1e8a6bc7c758464917f5d1c8cd21020f531924812ae3c21b5c8413ff81cd82d`.
- `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoNetworkCommandTests.cs` — `f969073eeadbcd9dfc57df201d05cf85fa09522fc10d694a412ad30e6c67a9a8`.
- `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoNetworkLobbyTests.cs` — `c8efcf592c942c7ff5928306db8915d3e0e2f79dd29bb86b1bcf191cfe5f71ff`.

**Completed fixes appear locally correct:** actual closed singleton-base reset; null-string configured overload resolution; null-channel rejection; per-peer relay budgets; dedicated-server hook application and direction/type gates; non-finite snapshot rejection before payload ownership; overflow-safe framing arithmetic; full discovered URI; connecting cancellation; actual startup defaults and no-Mirror false result. Local Mirror source supports the dedicated-host hook distinction and URI transport path; only later execution can prove weaving/host callbacks.

Tests inspect concrete target/reactive state, event counts, serialized outbound bytes, payload ownership and connection state; they stop/shut down owned test networking and destroy objects. Woven receive-delegate tests deliberately bypass outer Mirror admission/authentication, so they cannot establish real client ownership rejection. The old Mirror.Components reference request is **already centrally completed** in the current Play asmdef; do not repeat it. No current compilation success is claimed.

**Confirmed remaining defects:** A-10 through A-16. A-11/A-12/A-13 were already identified in the owner's plan; A-14/A-15/A-16 add concrete aggregate-cost/ingress/registration-ownership scenarios. Keep their minimal repairs separate from the typed ReplicationBinding proposal, which is **future work**.

Aggregate cost: valid unchanged bindings currently poll every frame after the first interval (A-14), with reflection/boxing in property sync. Context action name/component lookup is event-driven and may scan a target hierarchy/reflection metadata per invocation. Scene reactivation/bootstrap presentation scans are lifecycle/setup work, not continuous world searches. Singleton cache misses can repeat scene searches when a required instance is absent; negative caching would need an explicit late-spawn policy. Telemetry is opt-in and mostly refresh-throttled. Per-connection base limiter dictionaries retain historical integer peer ids without pruning until component destruction; this is a bounded-window retention/performance proposal to evaluate alongside the clock repair, without changing authority semantics. Handler sets' static/global lifecycle ownership is the concrete functional defect A-16.

**Needs evidence:** dedicated/host/remote admission, real LAN URI/cancel behavior, reconnect/late join, scene-template/private Mirror compatibility, player builds and no-Mirror compilation. No new transport, settings normalization, seed/hydration policy or forced framework extraction is approved by this review.

### Neo.Abilities — source review complete; resource receipts and approved dispatch correction remain

Original owner: `/root/audit_abilities`. Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Abilities.md` — `7da20a26c3966b182d9d93034d1b3b983b4919d70853a0b4164456a7174c2f8c`.
- `Docs/Audits/2026-10-23/Neo.Abilities.plan.md` — `ba37d46f0d5598475e074b4aa015ccfadbea4893000769cad9a87651f8f57f32`.
- `Docs/Audits/2026-10-23/Neo.Abilities.correction.md` — `1069d2e74dfbca14ac03540fd78808bad4d37207ca56e6609883bc5724ccea45`.
- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesRuntimeTests.cs` — `fc6fe4aba0e757f95a3e5780ec8037ccb35ff33babd7c0dcf6eb2affa4711636`.
- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesSceneTests.cs` — `ee98fb3aecedd7d3ad57737afd939198ad8b0ef5fd686d999115af92968f547f`.

Every changed runtime source hunk was inspected, with the full event bus/slot, damage/heal, cost capture/payment, modifier removal/timing, shield, maximum-bonus, level subscription, auto-cast and radius query paths. Both complete fixtures and all changed owned public-guide hunks were read. The fresh regressions exercise actual domain outputs, identities, public receipts, replacement lifetime and callback mutations; the scene fixture manually invokes lifecycle methods, so it cannot prove native Unity ordering. No tests ran.

**Completed fixes appear correct:** captured finite cumulative costs with exact per-entry payment; effective cooldown normalization and charge restoration; lifetime-bounded modifier ticks; snapshot-based bulk removal; retained granted blueprint references; internal reactions surviving user Clear with preserved snapshot timing; nested CastId; maximum-bonus staging; live shield checks/replacement survival; actual level-source unsubscription; targetability/effective-range auto-casting and no-health damage rejection. The correction explicitly permits earlier successful payments to remain spent if a later callback invalidates preflight; do not require a rollback framework. Mutable blueprint references are not immutable snapshots. Revoke/regrant is the explicit refresh path.

**Confirmed mandatory residual A-17:** `DamageService.cs:125–126` and `:170–172`, `HealEffectOperation.cs:37–39`, and `AbilityUnitBehaviour.cs:206–208` discard `ResourcePoolModel.Decrease/Increase`'s actual return and recompute an operation receipt from live balance after synchronous resource callbacks. A health listener that heals 20 while taking 10 from 50 makes the outer final balance 60; current `HealthDamage` becomes -10, suppressing damage/lifesteal receipts despite a real 10-point decrease. Healing has the converse defect. Severity **P2**; expected receipts describe that operation's accepted delta, independently of the final callback-mutated balance. Smallest repair: use the actual Decrease/Increase return at all four sites, retain the separate live death/availability decisions, and add callback-mutating damage/heal/lifesteal/wrapper regressions. Root explicitly accepted this repair for the same original author after review. No event transaction or final-balance rollback is implied.

**Approved 11.0 dispatch correction:** `AbilityUnitBehaviour.cs:137` subscribes every enabled unit through SubscribeAny, while `HandleEvent:216–220` immediately rejects every other Target. `AbilityEventBus.Publish:67–94` snapshots/invokes every any-listener for every event, producing O(events × registered units) wrapper calls and O(units) snapshot references per event. Root approves an additive Target subscription in the existing bus and moving the unit wrapper to it, while preserving public global observers. Match-target and global observers must merge by a common registration sequence. Preserve the exact current capture point: run specific snapshot handlers first; then capture matching/global snapshots; then internal reactions; then invoke the merged captured observers. A specific callback's subscription changes affect this publication, whereas internal Clear/unsubscribe does not cancel already captured observers. Required later acceptance: 1/100/1,000-unit invocation/snapshot counters and global/scoped ordering, nested Clear, unsubscribe and reentry parity. This is a concrete approved improvement, not evidence that routing already exists; original `/root/audit_abilities` owns implementation after the review barrier.

Aggregate work beyond dispatch: scene radius queries scan registered behaviours; auto-casters share their own widest-radius query per pass, but many casters/projectiles still scan O(casters × units) / O(projectiles × units) at distinct positions. IAbilityWorldAdapter is already the replacement seam for spatial indexing; a new framework is not required here. Global SlotsVersion invalidates every auto-caster's cache when any grant changes, a profiling lead. Modifier/tick/effect snapshots and nested removal pools allocate or retain capacity per operation/depth. Public Tick methods accept NaN/infinity (only <=0 guard), unlike Core resource Tick; finite-time admission is a bounded robustness lead requiring an explicit accepted input contract and focused evidence, not proof of full Tick/Cast transaction semantics.

**Needs evidence:** both Unity branches, native enable/disable/editor buttons, visual delivery/pause, projectile tunneling and real pooling. The corrected docs accurately distinguish domain time, scene projectile time, mutable catalogs, catch-up limits and adapter-order dependence. No lockstep/network-snapshot guarantee or mandatory catalog extraction is accepted.

### Neo.Abilities.Editor — source review complete

Original owner: `neo-audit-1023-b02-01`. Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Abilities.Editor.md` — `a369aa12a5f23c180e1945a3af53a27583eea01a5738888f3aa24e315956a56e`.
- `Docs/Audits/2026-10-23/Neo.Abilities.Editor.plan.md` — `a2fe8196a2cf944152c5872920b3e4baab1fa9525534a8855d5bdf6688a5ccf0`.
- `Docs/Audits/2026-10-23/Neo.Abilities.Editor.correction.md` — `f0726162e2dfefc2386159362ba725dec5425944626dc6ca7bc92d199b9194a7`.
- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023NeoAbilitiesEditorTests.cs` — `91eeaba257471240b4ccab0dfc545352a501bd2b308d384d4a37f3c961707193`.

All four changed editor-source diffs and the complete new 531-line fixture/Editor.md were read. **Completed fixes appear correct:** fresh node defaults and Undo path; inspector refresh; in-place inline card update; null modifier handling; full boxed node reads/chain hit count; all overriding radius levels validated; window-owned SerializedObject unbind/disposal; explicit types; case-insensitive catalogs/builtin classifier matching runtime without trimming or rewriting custom ids. Custom inspector-owned SerializedObjects are not disposed by the window. Regression sources assert serialized data, Undo/Redo, UI output and runtime-id parity; synthetic property events do not establish real typing/focus/event timing. The fixture closes its window, clears owned Undo and destroys its in-memory assets.

Aggregate authoring cost: every edit rescans cached discovered definitions/effects, rebuilds library lists and status UI, so large projects can incur O(all definitions/effects) work and allocations per edit. AssetDatabase discovery is a focus/refresh path, not demonstrated per-keystroke search. Contextual validation/indexing is a future proposal. Main Abilities README still needs the canonical Editor.md backlink, and AbilityDesignerWindow.md's earlier flat-or-any-level radius wording needs alignment with the new all-overriding-level rule. Native binding, Undo timing, active foldout typing and wrapper lifetime remain **needs evidence** after all source corrections. No mandatory new validation framework or scene wiring is approved.

### Neo.Tools.Move — source review complete; ownership/drag/completeness repairs required

Original owner: `neo-audit-1023-b07-07`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Tools.Move.md` — `b67a2a234157d159c435e34773e43254f932de2c47392029b4b8326ba275d894`.
- `Docs/Audits/2026-10-23/Neo.Tools.Move.plan.md` — `2c48bd427829b25d5ec456af2742350993718ce3c6e0c00f3ba520f3307a1a23`.
- `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMovePhysics2DTests.cs` — `b7bf04f5591982f9910ba7c65adfb1983e7e6a8591781f599d4f1aae14eb3737`.
- `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveRegressionTests.cs` — `508736bee380a4a10bdcaee4457cb2e3983daf7ff5a4288240a7a776a8dfae12`.

All 13 changed source diffs, both complete new fixtures, changed owned-guide diffs and relevant unchanged cursor/drag/grounding/ScreenPositioner paths were read. **Completed fixes appear correct** for the named low-speed, plane-coordinate, current-parent, sprint-disable, analog-magnitude, bounds-refresh, force-direction/start, zero-Z spin and optional-input scenarios. The local Physics2D fixture owns/closes its isolated scene; main fixture destroys its objects. Reflection-driven lifecycle calls do not establish native lifecycle behavior. The random force-range case should preserve Unity RNG state if the equal-endpoint native call consumes it; this is a test-isolation evidence item, not an asserted executed side effect.

**A-18, P1, confirmed:** `CursorLockController.SuspendPlayer:828–840`, `RestorePlayer:843–853`, `ReleaseControlInternal:670–695`. A shows cursor and disables player movement/look; B shows cursor over A and sees already-disabled gates, so records no ownership; releasing lower A unconditionally enables the player while B still owns a visible cursor. Expected each outstanding blocker continues blocking and final release restores prior intent. Minimal repair is internal per-player/per-gate owner accounting that registers requests even when already disabled, releases only the caller's request and restores a captured baseline once. Preserve existing serialized player lists and cursor-stack behavior. Later tests must cover top/non-top release, initial false, repeated release and destroy; do not substitute simply reapplying the top cursor state. General public scoped-gate API and externally changing base intent remain a separate design decision.

**A-19, P2, confirmed:** `CameraRotationController.Start:62–65` captures unsigned Euler, `Update:78–121` clamps enabled axes and rotates whenever held+modifier even without entering drag. Initial X=350 with -45..45 limits jumps to +45 on a zero-delta drag instead of retaining equivalent -10. Pressing modifier after mouse-down rotates from stale lastMousePosition without start; releasing modifier/losing pointer/disabling never balances the active drag. Expected stable initial equivalent angle and one start/end per active drag, with baseline reset on every entry. Minimal repair: equivalent-angle initialization respecting existing signed/unsigned authored ranges, plus a sample-driven active transition helper and disable/device-loss cleanup. Preserve historical mouse sensitivity/defaults; changing pixels×deltaTime requires an explicit compatibility mode. The author's D2/D3 are accepted source-confirmed fixes, not a requirement for a new math framework.

**A-20, P2, confirmed completeness defect:** `PlayerController3DPhysics.cs:145` and `UpdateGroundState:601–624` retain a 16-element OverlapSphereNonAlloc buffer and exclude self only afterwards. With more than 16 overlapping self/child colliders plus real floor, a returned prefix containing only self can report airborne. Expected grounding can find any valid floor in the complete configured query. Smallest repair: reusable growth/retry or complete list/fallback preserving mask/Ignore triggers and self policy; add a saturated self+foreign-floor regression. The 2D correction already uses a complete list. This is a source-level omitted-result counterexample; no ordering/native runtime reproduction is claimed.

Aggregate work: each player independently probes its own ground position every fixed step, including 3D remote players before the authority early-return; distinct geometry cannot be treated as one global cached ray. Pointer movers/rotators may perform one physics ray per owning object. CameraConstraint can recompute bounds every LateUpdate; Follow/DistanceChecker perform explicit-target vector work, with startup-only tag search and configurable interval. Cursor auto-discovery is a one-shot fallback, not per-frame discovery. Repeated pointer polling can share existing Input sampling without adding a scene registry. Shared cursor gates are the concrete ownership bug A-18. Force speed-cap timing, alternate input devices and native body scheduling remain **needs evidence**. MotionMath/public scoped-gate expansion are **future proposals**, not prerequisites to these narrow fixes.

ScreenPositioner still forwards its serialized `_depth` directly to the corrected near-plane-relative ScreenExtensions API, then optionally restores world Z. Original Move owner must explicitly document/reconcile orthographic depth compatibility with Extensions; do not auto-rewrite scene values or claim 3D pose parity from 2D XY tests. Existing `Follow.cs:103` also retains an old `var` declaration despite the report's no-introduced-var claim; no new var was introduced, but repository-wide style cleanup was not established by that scoped assertion.

### Neo.Progression — source review complete; same-profile replay correction required

Original owner: `neo-audit-1023-b04-07`. Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Progression.md` — `5e3c2163f2e9981d99c72308f00543cb9ca1d8f98cb57152a5da5e22e373acf4`.
- `Docs/Audits/2026-10-23/Neo.Progression.plan.md` — `07a9545bdafb7369078bfd64577a50d5198af851ef39a2e5fe5c7fdbf3d6f58a`.
- `Assets/Neoxider/Tests/Edit/Progression/Audit20261023NeoProgressionRegressionTests.cs` — `ef1abd1431a95bf969f901eda51772edd9e27c8ce70c49658b765f2b817d91f0`.

All six changed source diffs, full new fixture, changed nine public-guide diffs and relevant provider/profile/reward/notification paths were read. **Completed fixes appear correct:** saturating points, captured premium replay boundary, same-reference provider assignment, provider replacement snapshots, profile null-list repair/clone, accurate NoCode XP failure, first-use initialization and normalized negative legacy XP. Tests assert real balances, events, save counts and provider isolation. They restore the prior global save provider/singleton state and destroy owned inactive objects; the old audit's NonParallelizable claim predates central removal of unsupported attributes and is not a current execution guarantee.

**A-21, P1, confirmed:** `ProgressionManager.LoadProfile:609–612`, `ApplyProfile:685–697`, especially `_lastRewardedLevel = CurrentLevel` at 696. At level3 with 5 claimed points, lower to1, reload the same saved 5-point profile, then regain3: current code rewards levels2/3 again and reaches10. Expected ordinary reload of the same active profile preserves that session's reward watermark, consistent with the public no-replay contract. Minimal repair: distinguish same-active-key reload, actual profile switch and explicit reset; preserve max(previous watermark, incoming level) only for the same key, retaining V2 JSON/public signatures. Merely assigning SaveKey does not load a new profile. Add reload/repeated reload/reset/other-key regressions. Durable cross-restart claim identity is a separate future ledger decision. The original owner's plan already identifies this exact defect; source confirms it.

**A-22, P2, source-confirmed mirror limitation / recommended bounded correction:** `ProgressionManager.Init:644–648` subscribes only OnLevelUp, and `DispatchRewardsForLevel:767–770` returns for non-increasing levels. Direct Core AddXp(25) at level1 leaves manager getter25 but XpState0; direct load/reset/decrease similarly leave published mirrors stale. Expected interoperable manager mirrors follow their assigned provider while reward eligibility remains separate. Minimal repair: subscribe to the provider's existing reactive XP/level/remaining-XP channels, detach on replacement/destroy, stage/coalesce manager notifications and avoid additional saves/rewards for mirror-only changes. No polling, Core source edit or mandatory session extraction is needed. The guide now documents this limitation, so it is not concealed as a completed fix; root should approve the planned correction explicitly with the original author.

Aggregate work: no autonomous per-frame scan was added. Level progression/premium replay walks every integer up to the reached level and linearly resolves track definitions, so sparse large catalogs can cost O(level gap × entries), plus synchronous XP reward recursion. Profile persistence serializes/synchronizes collections and publishes all state on manager operations. These costs are source-visible but unmeasured. Indexed ranges and a queued standalone ProgressionSession/reward sink are **future proposals**. Definitions currently permit scene-target fields and missing reward services warn after state commitment; retain compatibility and documented no-atomic-delivery limits rather than silently introducing a ledger/schema migration. Later native lifecycle/provider/UI behavior and both compilers remain **needs evidence**.

### Neo.Tools.Input — source review complete; cancellation/reentry and shared backend repair required

Original owner: `neo-audit-1023-b07-02`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Tools.Input.md` — `fca739fd3f43e4c1819da39e8812e4fdcbb0e5fa8fdbee9a1fb29b376f3c88de`.
- `Docs/Audits/2026-10-23/Neo.Tools.Input.plan.md` — `ded1e829a63adae0ea63d764ecae13743176cb3efdc90cd58b3a0e0cef1c35a6`.
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsInputAdapterTests.cs` — `45011b1963b667b5fdc26a365066903d526503f1a2023a9dcd9d24bb6eb684e1`.
- `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputBehaviorTests.cs` — `6bdcfd490b1e08e370485de2abb2b7bab394a062d0f34448e72a2fa4fd1c375f`.

All six changed source diffs, full fixtures, all six changed public-guide diffs and compatibility/helper/manager/effect call paths were read. **Completed fixes appear correct** for mode independence, stale press cancellation, own-effect cleanup, first Hold spawn, contact identity, observed press, qualifying direction, keypad mapping, Try availability and null bindings. Tests assert actual ray-hit identities, public events/polling, visual activation and child spawn counts. The Play fixture requires an isolated scene/singleton and clears polling rather than restoring an arbitrary prior pending input; that prerequisite must remain explicit. The adapter fixture restores its temporary current keyboard but fake touch controls prove translation only, not native event timing/stripping.

**A-23, P1, confirmed:** `MouseInputManager.ClearPressedState:139–143` and cancellation callers clear without informing subscribers, while `MouseEffect.Update:125–158` continues `_isFollowing` independently. Press → disable only manager → release while disabled → reenable leaves effect following forever without a matching release. Expected interruption also terminates subscribed visual ownership once. Minimal repair: additive cancellation event, clear pending manager state before notifying once, effect subscribes/unsubscribes and reuses StopFollowing(true). Ordinary release is separate; no synthetic Release/Click/release-prefab. Camera/device loss, focus/pause and manager destruction need the same cancellation semantics. This is the author's remaining P1, source-confirmed.

**A-24, P2, confirmed:** `MouseEffect.OnPress:302–311` invokes SpawnAt/onSpawn before setting following. An onSpawn listener disables the effect; OnDisable cleans, then the outer call sets `_isFollowing=true` and emits start on the disabled component. Expected no resumed gesture or orphan start after interruption. Minimal repair: validate transition generation/current enabled/interactable state after user callbacks and suppress superseded continuation; retain normal event order. Include onSpawn→disable and onStart→disable event-count regressions. The same check should cover reentrant visual SetActive hooks where they invalidate the current press, rather than guarding only one named event. Owner's remaining P2 is confirmed.

**A-25, P2, confirmed aggregate cost:** `KeyInputCompat.cs:13–49`, `MouseInputCompat.cs:11–61`, `SwipeController.TryHandleTouch:157–171` retry disabled legacy Input on every read and catch InvalidOperationException to select New-only fallback. Manager alone performs four such reads each frame; every Move/Debug/effect/key consumer adds its own exceptions and reflected property/boxing work. Expected backend selection is shared/cached, not exception-driven per consumer sample. Minimal repair within Input: shared backend availability/selection with subsystem reset and explicit invalidation policy, preserving legacy-first/Both behavior, fallback key subset and unavailable out values. Reuse it from key/mouse/touch and existing upper-layer consumers; do not add a second key mapper to Debug or an Extensions→Input cycle. Later counters must prove warmed New-only reads stop throwing selection exceptions. No timing/allocation benchmark ran here.

Other aggregate work: manager rebuilds physics event data separately for same-frame Press/Hold/Release (up to two native queries per build with existing 3D-first policy); an event callback can mutate colliders/settings, so sharing across phases requires explicit invalidation/parity, not unconditional frame caching. Per-effect following resamples pointer/camera and uses scaled intervals. Reflected optional reads form cache-key strings/box values and retain Type metadata until reset. A buffered recognizer/typed optional adapter is a **future proposal**; preserve current single-contact/polling behavior and avoid new required assembly/package choices. UIUtils coordinate/caller-buffer overload belongs to Extensions with an additive Input adapter above it. Real legacy/Both/New-only devices, primary-touch phases, domain/scene reload and IL2CPP remain **needs evidence**.

### Neo.Settings — source review complete; native persistence/display evidence pending

Original owner: `neo-audit-1023-b05-08`. Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Settings.md` — `82c25b739e3e3e2604db8fdddd9a7e15b09dcafe3e126364ed6aed426c5b04c3`.
- `Docs/Audits/2026-10-23/Neo.Settings.plan.md` — `d622b76ef6c59797ca8a9b428f963edc561a0c923f0361796bc84230d24e104e`.
- `Assets/Neoxider/Tests/Edit/Settings/Audit20261023NeoSettingsTests.cs` — `dd7046661fe736c8b2f8e1a89128b8b03595eeb60a20aca599d6ce5bfee17416`.

All three changed source diffs, complete 454-line fixture, all five changed guide diffs and attach/deferred/UI handler paths were read. **Completed fixes appear sound:** finite sensitivity/default admission, enum/framerate normalization, requested fullscreen consistency, owned deferred callbacks/inactive flush, enabled-panel synchronization without rebuilding options, eleven-listener cleanup and null FPS list. Tests inspect public state, provider output, control values/listener counts and retained option identity; they preserve prior provider/quality/VSync/framerate/screen flags and suppress/restore diagnostics. They reset the static settings context rather than preserving a preexisting service, so later execution requires the documented isolated test environment. Coroutine start/real timed completion and actual OS display transitions are not established by synchronous source assertions.

No further source-confirmed mandatory defect was established beyond the original audit fixes. The quality-change VSync mismatch is a **needs evidence** native engine lead: setter paths call SetQualityLevel without restoring requested VSync, whereas ApplyAllEngine restores it; validate the configured profiles before assigning a bug result. UI/reference graph notes remain compilation debt, not a repeated proven compile error. Preserve reserved ApplyButton=1, SkipUntilFlush scheduling, all-group writes, provider durability separation and index-based resolution identity as currently documented.

Aggregate cost: settings changes fan out to every enabled view and serialize enabled groups; value refresh is O(assigned controls), while full option list creation occurs on enable/load/explicit refresh. Resolution discovery/dedup/sort is initialization/configuration work. Debounce uses realtime and only the current context may write; older context cancellation ownership is now explicit. No scene polling or new per-frame service exists. SettingsController/EditSession is a **future proposal**, with staged opt-in semantics and no silent legacy enum/default change. Main-scene bootstrap, domain reload, native coroutine disable/destroy and platform display remain **needs evidence** after source work.

### Neo.Tools.Components — source review complete; Counter and inherited lifecycle repairs required

Original owner: `neo-audit-1023-b06-05`. Full artifact reads:

- `Docs/Audits/2026-10-23/Neo.Tools.Components.md` — `4236da8ad3f0cdc4ab5afaa059e235fa732d5353959f6954320e0882ceaf79dd`.
- `Docs/Audits/2026-10-23/Neo.Tools.Components.plan.md` — `e451996bb23e982bdffa4fac7f1c431e86752e47143744695bbebf1fafa90df4`.

All eight changed source diffs, complete five new regression sources (Numeric, Scene, Typewriter, Binding, Network), changed ten guide diffs and relevant SetText/counter/async cleanup/camera paths were read. Test SHA256 values appear in the coverage receipt below. **Completed fixes appear locally correct** for arithmetic/registration, server snapshot startup/load and host RPC suppression, sender throttling, derived score/labels, callback generation checks/cleanup, wrapper startup/complete ordering, disposable score subscription, reversed float bounds, render-target projection/cache/editor gating and Play-only destruction loot. Tests inspect real object/state/events/serialized wire output. RNG is restored, unique save keys cleaned, target textures/objects destroyed and host transport restored. Host/snapshot round trips remain narrower than independent remote-client evidence; the late-binding Play test explicitly requires no preexisting score source. Synchronous typewriter tests cover zero-rounded delays, not awaited native update timing.

**A-26, P1, confirmed:** `Counter.SetValue:402–408` resumes after ApplyValueLocally callbacks; `CmdSetValue:442–444` also sends after callbacks. Set(1)'s Value listener Set(2) completes/broadcasts2, then the outer server path writes/sends obsolete1 (direct Set also rewinds `_syncValue`). Expected final local/remote/new-client snapshots agree on2. Minimal repair: revision/generation checks after callback boundaries before continuing stale replication/derived publication, in both direct server and Command paths. Define typed/repeat callback continuation while retaining ordinary event ordering; do not claim a global transaction. Add nested Value/typed handler cases and actual receiver/initial-snapshot state. Root independently accepted this P1.

**A-27, P1, confirmed:** `Counter.ApplyDelta:357–365`, `ApplyFactor:368–376`, `SetValue:394–397` calculate and send an absolute value from each remote client's stale local balance. Two clients both Add(1) to10 each submit11, so the server loses one increment. Expected sequential server operations produce12. Minimal repair: send existing CounterModifyOperation+operand, evaluate against current server state, retain per-peer limiter and public rounding/divide guards; absolute Set remains last-writer-wins. Shared-counter authorization is a separate explicit application policy, not an automatically established vulnerability. Root approved this bounded correction; exact-int models/save migration are optional future work.

**A-28, P1, confirmed integration defect:** `TextScore.OnEnable:36`, `OnDisable:44` hide the new private `SetText.OnEnable:29`, `OnDisable:37`; the inherited `_onEnableAnim` pause/resume contract is not composed. Disabling an animating TextScore can let its tween advance while disabled. Minimal cross-owner repair: original Text owner provides protected virtual lifecycle hooks, then Components overrides and calls base while preserving subscription cleanup. Retain existing base OnDestroy/SetLink cleanup. Add native disable-while-animating and reenable-to-current-score regression; source confirms hidden callbacks, native tween timing remains later evidence. This is already correctly called out by the author's plan and root.

Aggregate work: typewriter repeatedly materializes growing text strings (O(text length²) total copied characters), does per-character async/audio work and stores pause maps. Counter's optional repeat loop is synchronous O(rounded value), explicitly documented for bounded gameplay values; do not enable it for arbitrary balances by default. Camera's per-frame cache checks are O(1), projection recalculation only when applicable inputs differ. Late TextScore fallback can repeatedly search missing singleton while enabled; use explicit source for many labels. Public Counter.Registry is shared mutable lookup, now cleaned by captured-key lifecycle but not protected from external mutation. No universal pooling/math/typewriter framework is required for these fixes. Grapheme/TMP tokens, pure session and exact-numeric backend are **future proposals**. Rendered camera output, runtime loot teardown policy, native delays and real remote operations remain **needs evidence**.

Components regression read receipt:

- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsComponentsNumericTests.cs` — `d83b0aee6efffae618b3cccd30abc61a485429dc0eaa4849e4fe5e3cd4584860`.
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsComponentsSceneTests.cs` — `61d0c24abaf77fe1a9022161a5dd4da7c2d214f021affd54dd6874e2d70e205f`.
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsComponentsTypewriterTests.cs` — `01717ce350cdafc9e248c0d0eb661546a5190b55e27caa556634038df61a5751`.
- `Assets/Neoxider/Tests/Play/Audit20261023NeoToolsComponentsBindingTests.cs` — `a0228bb769c1a6d724ccd3cdb93f89773235e9a554af77f38c9ce00a92e47863`.
- `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoToolsComponentsNetworkTests.cs` — `dd7f0120c4ad736ebfe1c20b018abb01150696ea6ec409b6050d296b5d799336`.

### Neo.Tools.View — source review complete; four planned residuals confirmed

Original owner: `neo-audit-1023-b08-06`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Tools.View.md` — `2ae66ae0d067b78c30a7a2cde98b7a522e98fda530fa2e3f5794af6af51224e1`.
- `Docs/Audits/2026-10-23/Neo.Tools.View.plan.md` — `29d2ef0738057792640d975e447e1abb56a55cfeed8295b9d337d24502f61b83`.
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsViewSelectorTests.cs` — `fe8eadd9c11654ac7e1a3ed6401df18b29fdaee7850a07285667a5d64c4dfecd`.
- `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsViewLifecycleTests.cs` — `10d5fbde3917a8a62a0469d266071ad43fd0695c735572742891be1dcbf39d4a`.

All six changed source diffs, both full fixtures, nine changed guide diffs and relevant selection/model/network/lifecycle paths were inspected. **Completed fixes appear sound** for delayed startup persistence, singleton unique cycles, direct collection self-assignment, logical offset bounds, bulk exclusion routing, closed Light2D delegates, restored readbacks, owned material clone, StarView subscription and camera-free billboard. Tests observe actual activation/material/light outputs, save contents and listener counts, restore the save provider and destroy their objects/materials. Optional URP/HDRP skips establish no backend pass; synthetic/non-native lifecycle checks are not visual rendering evidence.

**A-29, P1, confirmed:** `SelectorModel.SetExcludedIndices:331–347`, `SetUsedIndicesForUnique:350–366` only protect direct ReferenceEquals. Passing `model.ExcludedIndices.Where(i => i != 2)` clears its backing set before deferred enumeration and loses all exclusions/history. Expected IEnumerable replacement retains the supplied sequence and an enumeration failure leaves old state intact. Smallest repair: materialize before Clear, keep direct-alias fast path/null-as-clear, then commit; regress filtered self-derived and throwing enumerable inputs. No new snapshot framework is needed.

**A-30, P1, confirmed:** `Selector.RpcSyncState:295–298`, `ApplyNetworkState:396–400` notify through ApplyUpdateSelection before restoring authoritative additive active indices. For snapshot {0,2} a callback sees one active object instead of final2. Expected Selector-level notifications observe the whole received state. Minimal repair: stage index/mode/exclusions/full active set, suppress intermediate Selector notifications/save, then publish final selection/count; preserve local operation ordering. This does not promise atomic Unity OnEnable/SelectorItem callbacks. Later remote/late-join tests must read actual active objects inside callbacks.

**A-31, P1, confirmed:** `Selector` Commands at 266,304,321,338,355,366 call parameterless RateLimitCheck despite receiving sender. Two authorized clients within one interval suppress each other. Expected independent peer budgets, matching Counter/relay policy. Use existing sender overload in every Command, retaining exact authorization/admission and rate semantics; regress two peers plus repeated same-peer/unauthorized cases. No Network framework change is needed.

**A-32, P2, confirmed:** `SelectorModel.GetBounds:108–110`, `Set:217–218`, Next/Previous170/197 and inclusive loops65/431/446/463; wrapper `Selector.SyncIncludedIndicesInspector:1112`. Count3, offset1, Loop=true, Set(int.MaxValue) overflows index-min and wraps to0 rather than1. Count1, offset=-int.MaxValue yields min=max=int.MaxValue; inclusive `int i++` wraps to int.MinValue and never terminates. Expected mathematically valid wrapping and finite iteration bounded by element count. Use long intermediates/steps and explicit policy for bounds outside Int32, preserving normal configs. Add extreme-offset singleton termination and exact wrap tests; no massive pool allocation is needed to expose this case.

Aggregate cost: Selector repeatedly Configures/copies model sets through read paths, allocates replacement sets on synchronization, rebuilds/sorts inspector collections, scans real item active state, and builds random candidate lists. This is O(pool/history) work even in some logical getter paths, not established zero-GC. Model-as-authority/revision optimization is a **future proposal**, separable from A-29–32. Billboards may retry Camera.main independently if no explicit camera exists; light/emission updates use distinct owner data and do not need world physics. Multiple writers to one renderer/light require explicit ownership policy; restoration when a prior material owner destroys its clone is an additional multi-owner evidence lead, not an executed new finding. Current StarView intentionally remains subscribed while disabled and documents it; do not silently change that behavior. Native remote admission, rendered light/material disposal and optional stripping remain **needs evidence**.

### Neo.Condition — source review complete; precise editor/doc integration follow-up

Original owner: `neo-audit-1023-b02-07`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Condition.md` — `75babe074bd3e2a3c81b8001647171f523559f8f6a2edb2cb8d8f52287f1277c`.
- `Docs/Audits/2026-10-23/Neo.Condition.plan.md` — `b4bbcd6011ae9416cf89ae341f51d21aa4908ab7e3007ccf99eb927dad5c04b3`.
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionRegressionTests.cs` — `1d6aacf5b1da198a573b7ff7d0ab2016d9b661b2e80ffe09cba5c623cbaacb36`.
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoConditionValueProbe.cs` — `ccc0c14d8a1c6c2892c8ade9ae150022610cebc9af16f0c51357f99f17057937`.

All four changed source diffs, complete fixtures/probe, four changed guide diffs and reflection resolution/evaluation, lifecycle, network and current Editor/StateMachine integration paths were read. **Completed runtime fixes appear sound:** current-context rebinding, flags-sensitive hit/miss caches, actual numeric parameter conversion, non-invokable generic rejection, destroyed explicit right-source identity, mode/network command ingress and repeated ServerRevalidate routing. Tests read returned members, actual method arguments/counts and public result/event state; woven-body rejection tests intentionally do not prove outer admission/transport, and absent weaving skips remain unverified.

**A-33, P2, confirmed narrow cross-owner parity residual:** `Assets/Neoxider/Editor/Condition/NeoConditionEditor.cs:GetCurrentValueFromComponent:1052–1073` selects the first same-name/argument-kind method without excluding open generic candidates, unlike runtime `ConditionEntry.FindMethodWithOneArgument:584`. A generic overload encountered before an invokable same-kind overload faults preview and returns null instead of displaying the runtime-selected value. Smallest repair by original Neo.Editor owner: use existing ReflectionCache/runtime method-selection rules (or the same complete filters) before conversion/invocation. **Do not repeat stale plan requests:** parameter conversion is already present at1068, and picker `ComponentBindingInspectorShared:335` already skips IsGenericMethod and indexers. These existing corrections were personally inspected. Current generic-overload preview parity still needs a focused regression, not a new descriptor schema.

Required bounded documentation correction by original Condition owner: README:43 says all four event types fire, while InvokeEvents publishes OnResult, OnInvertedResult and exactly one OnTrue/OnFalse branch. Condition_Reuse's StateMachine table should say only Owner falls back to the current state; missing Override never substitutes Owner (`ConditionEntryPredicate:74–84`). These are confirmed source/doc discrepancies, separate from runtime behavior. Preserve current unavailable-source inversion semantics; the typed status/session/descriptor core is **future work**.

Aggregate work: stable bindings reuse metadata but reflection reads/conversion still box; switching one shared entry among many contexts repeatedly allocates GetComponents arrays/rebinds its single mutable cache. Missing-member lookups can repeat component enumeration on every check despite warning suppression; name-search retries alone are realtime-throttled. EveryFrame checks throttle individually to ~60Hz scaled gameplay time, not a global evaluation cache; interval loops remain per component. Shared metadata caches retain configured keys/types, whereas entry bindings retain their last resolved target. A prepared per-owner session is a future optimization, not mandatory new scene wiring. Binding-time ambiguous property/indexer resolution outside read catches is a **needs evidence** lead already identified in the plan; the docs' broad failure wording is not a proof that all metadata resolution is contained. Native lifecycle, Mirror accepted ingress and AOT/stripping remain **needs evidence**.

### Neo.Tools.Other — source review complete; duplicate path scheduling/reentry remain

Original owner: `neo-audit-1023-b07-08`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Tools.Other.md` — `05c699013341da234c0f944567c7e47f691beed7955b02d9b412dc673d68c3be`.
- `Docs/Audits/2026-10-23/Neo.Tools.Other.plan.md` — `ae64f90ae0ddcc9cdeead8233c57f86e96e0dd5b9551f4cee410867de912eac9`.
- `Assets/Neoxider/Tests/Edit/Tools/Other/Audit20261023NeoToolsOtherCameraShakeTests.cs` — `c1b42dd45022f6a8d880520c230783b142d9c5abe0d1af121651b99c508aa950`.
- `Assets/Neoxider/Tests/Edit/Tools/Other/Audit20261023NeoToolsOtherNavigationTests.cs` — `225f235a64c04c3f32ada38687898a4a2b46f1ee9f9c31ac4febf03bd51b167e`.

All three changed source diffs (including both Spine branches), both complete fixtures, five changed guide diffs and relevant path/tween scheduling/ownership paths were read. **Completed fixes appear structurally sound** for root unscaled sequence/named shake arguments, latest-request tween ownership, captured channels, bounded unusable routes, tracked patrol waits, accepted path/warp readiness, target retention, arrival reset, gated diagnostics and conditional Spine initialization/state publication. Fixtures drive actual NavMesh islands/native movement or real DOTween transforms, with bounded waits and owned NavMesh cleanup/timeScale restoration; none ran. Navigation's temporary SerializedObject in VerifyPatrolCancellation should use a using scope to release its native wrapper. Optional Spine has no compiled evidence/vendor runtime here; do not install one as part of this review.

**A-34, P2, confirmed aggregate duplicate work:** `AiNavigation.SetTarget:492,497–499` calls UpdatePath then starts PathUpdateRoutine, whose first MoveNext immediately calls UpdatePath again at935; UpdatePathfinding:915–922 is a second interval scheduler. Expected one accepted initial submission/event and at most one automatic request per configured interval. Actual one SetTarget submits/notifies twice immediately and can duplicate periodic native path work across agents. Minimal repair: retain one Update interval scheduler and explicit initial request, preserve AutoUpdatePath/pause/disable/mode semantics. Later tests count accepted initial/path events and interval activity; no new navigation manager is needed. This is the original plan's confirmed duplicate path.

**A-35, P2, confirmed callback continuation:** `AiNavigation.SetTarget:492–499` calls UpdatePath, which invokes `onPathUpdated:960`, then resets hasStopped/reached/blocked state and may start its coroutine. An onPathUpdated listener Stop() sets hasStopped=true/isMovementPaused=true; the outer request resumes and sets hasStopped=false, disagreeing with the paused/stopped agent and enabling CheckPathStatus's active-state branch. Expected callback-issued Stop or replacement wins. Minimal repair: initialize operation state before callback and guard post-callback continuation with a request generation/current state, including SetTarget replacement; add public callback→Stop/replacement assertions alongside native agent state. Do not invent whole-engine atomic callbacks or change ordinary accepted-request event order.

**Needs evidence, retained P1 lead:** CheckPathStatus:995 requires hasPath before its PathInvalid branch. A ready agent with an accepted active request whose resolution loses all path is ignored, but actual engine trigger/transcript remains unexecuted. Track accepted request/destination and distinguish pending/idle/stopped before any narrow no-path repair; do not remove the hasPath guard indiscriminately or mock native flags. Rotated/scaled patrol zones, destroy during tween callbacks and optional Spine external-track ownership also remain evidence leads.

Aggregate cost: every agent polls native movement/path/animation state each Update and currently has duplicate path schedulers (A-34); patrol zone sampling and path calculations are request/interval work. Existing scaled gameplay time/route mechanics stay intact. Camera shake owns one sequence plus children, allocates per request and restores captured local channels; composing multiple transform writers requires the documented pivot. Spine lookup/list generation is initialization and event work. The amended procedural shake proposal correctly reuses Neo.Animations' existing sampler and remains **future optional bridge work**, not a required new noise generator or dependency in base Other. Native NavMesh/Spine/IL2CPP, real delays and rendering remain **needs evidence**.

### Neo.Quest — source review complete; late-content and predicate reentry gaps remain

Original owner: `neo-audit-1023-b05-01`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Quest.md` — `632a4a8da36bb077da4cf68c1ffc53100fed275c1240f761d0c7479ade5b6145`.
- `Docs/Audits/2026-10-23/Neo.Quest.plan.md` — `1bfe1e9bfd95c5cc40ee61144aef9a7b4d2b9204ccebef9ee241cbc310e8f1ae`.
- `Assets/Neoxider/Tests/Edit/Quest/Audit20261023NeoQuestRegressionTests.cs` — `e05c6297fc4d9b4056262858b916e704354f6d6c9b6b74479492d2889132b25b`.

All four changed source diffs, full 468-line regression source, ten changed guide diffs and current acceptance/notification/state/load paths were read. **Completed fixes appear sound** for binary progress before callbacks, objective commit before progress, active/reference guards around completion callbacks, temporary load normalization, list growth, accurate bridge/flow gates and runtime Inspector buttons. Tests assert callback-visible progress, exact completion counts/status/identity and round-trip save contents; unique key deletion is scoped. The fixture assumes its own Quest singleton and does not preserve an arbitrary existing manager, so execution needs an isolated scene. Native button enablement and JsonUtility recovery remain later evidence. No regression ran.

**A-36, P2, confirmed:** `QuestManager.Load:126–137` repairs completed binary progress for known content, while `RegisterQuest:276–280` only grows restored lists. Load unknown completed Talk with progress0/completed=true, then register its config: progress remains0; known-at-load returns1. Expected identical legacy normalization independent of registration timing. Smallest repair: shared private config reconciliation used by Load/RegisterQuest, preserve counter counts/status/unknown data and emit no gameplay event. Add all three binary kinds plus counter/no-event assertions. This is the original plan's confirmed residual.

**A-37, P1, confirmed old-notification credit through user predicates:** `QuestManager.EvaluateObjectiveConditions:692–701` checks original active/reference identity before calling ConditionEntry.Evaluate, but does not check it afterwards. A supported getter/method restarts its quest once and returns true; CompleteObjective(config,i) then resolves the new state and immediately credits it from the old notification. Expected replacements receive only the next notification, as Q3/docs promise. Minimal repair: recheck original identity/status after each user predicate, before credit, and stop the superseded state iteration. Add a real condition-probe method causing one restart and assert replacement progress0/statusActive. Current tests restart from objective-completed callbacks, so they do not cover predicate-time replacement.

**A-38, P2, confirmed duplicate admission through predicates:** `QuestManager.AcceptQuest:233–260` checks GetState before EvaluateStartConditions, then unconditionally adds a new state. A bounded start-condition method invokes one nested AcceptQuest of the same config and then returns true; both calls add the same id and emit acceptance. Expected one current state per id. Minimal repair: post-predicate existence/admission recheck before committing, and keep CanAcceptQuest coherent after user evaluation. Preserve ordinary callback order/legacy restart policy. Add nested acceptance and predicate-created-state availability assertions; no queue/FIFO engine is required.

Aggregate work: each notification snapshots all states and repeatedly performs linear state/config lookups inside per-objective loops, yielding O(quests² + objectives × quests) in large registries; custom-condition evaluation adds reflection/binding cost. ActiveQuests allocates a list on every read. There is no autonomous Quest Update or physics search; callers choose checkpoints/timers. AutoSave serializes the full state list on accepted operations. Dictionaries/immutable definitions/stable objective IDs are **future proposals**; adding IDs requires explicit legacy migration and should not block A-36–38. Keep synchronous interleaving, positional saves, metadata-only NextQuestIds, optional flow and documented destructive restart defaults; strict invalid-content/FIFO/atomic-restart policy is not silently approved.

### Neo.Reactive — source review complete; no additional mandatory runtime defect established

Original owner: `neo-audit-1023-b05-02`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Reactive.md` — `8904d7a1181065fe6441ba81e7c32b9d0efdfb6cb2d824264e63d8f5d175150b`.
- `Docs/Audits/2026-10-23/Neo.Reactive.plan.md` — `eedfb54a1446522414297d0361c52c095ffc959f8ead9b2c3a9657f5217d9b0e`.
- `Assets/Neoxider/Tests/Edit/Reactive/Audit20261023NeoReactiveRegressionTests.cs` — `c89f0424f7c535a869534f6e7e6b743926822086edcad4f44f64924725e3188c`.

Both source diffs, complete new fixture, both changed public-guide diffs and full notification/snapshot/subscription/liveness/disposal paths were read. **Completed fixes appear correct:** count-triggered liveness sweep, safe explicit-target initial dispatch, whole-multicast strong fallback and null removal. Code snapshots retain captured values/membership under nested notification; separate nested buffers are cleared in finally, outer shared buffer is cleared, and idempotent handles remove their own captured registration. Tests use public actual values/calls/counts, destroy owned Unity subscribers, and isolate GC targets in a no-inline scope; GC/AOT/native runtime outcomes remain unexecuted. Exception-log expectations require the diagnostics error gate enabled in the later isolated test configuration.

No new mandatory runtime defect was established. The author's remaining **P3 XML correction** is valid: the class summary's universal disappeared-listener “reported once” wording does not describe silent collected-target/count sweeps or throttled destroyed-target warnings. Narrow it to match the already-correct guide, without changing runtime behavior.

Aggregate cost: delivery is O(code listeners + UnityEvent calls), reuses its outer snapshot after growth, allocates nested arrays, and weak setup allocates state/WeakReference/delegate invocation lists. Count reads intentionally scan all registrations; delegate dedup registration is linear. The cached invoker metadata retains MethodInfo/types rather than listener instances. Strong multicast/AOT fallback retains the group, uses final delegate Target for group liveness and shares group exception behavior; independent lifetimes require separate registrations as documented. Held explicit weak handles do not retain the target when the handler is noncapturing. Legacy duplicate delegate handles share one registration by contract, so do not silently change ownership. The additive Observe/retention-metadata API is **future work**, and the amended plan correctly avoids an unnecessary new registry/framework or changes to external interfaces. Player/AOT, native persistent UnityEvent and GC behavior remain **needs evidence**.

### Neo.Tools.Inventory — source review complete; recovery/persistence/world-item corrections required

Original owner: `neo-audit-1023-b07-05`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Tools.Inventory.md` — `9df68ab0fc035c079991cb99aa05ffd34da9c701e3541949d6451a7c46985b41`.
- `Docs/Audits/2026-10-23/Neo.Tools.Inventory.plan.md` — `025c043701ad222a7f5302c15153ece3913115f997cb945b19674157570fd717`.
- `Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryCoreTests.cs` — `5ea8654961b3c18955e473122cc4579698092197dc0aa16dc12538a5ab9fdcb3`.
- `Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryFixture.cs` — `5cfa9b9ce5158e734df9f58810b6b87724d8bac36344e272cc657bdd3561183e`.
- `Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryRuntimeTests.cs` — `0166e17a1cf6570e8bcef3b82bb3d85c9547377acb2d8c88c21c4c03b7a1f4a7`.
- `Assets/Neoxider/Tests/Edit/Tools/Inventory/Audit20261023NeoToolsInventoryViewTests.cs` — `a0caeb9dadf5a832ce836e5134bcbc555489740763cd231ca764b6b3c1b3ed48`.

All 18 changed source diffs, complete four new test/support files, nine changed guide diffs and current mutation/count capture/transfer/load/drop/pickup/preview/hand paths were read. **Completed fixes appear locally correct** for constrained count conservation, representable totals, per-id unique capacity, layout/payload notifications, paired in-memory slot commit, backend restoration, initial-only provider avoidance, partial/reentrant pickup, drop gates/preflight/success exception boundary, disabled view rebind and generated-object ownership. Tests assert count/identity/payload/physical state, real event counts, save JSON and view/clone lifetime. Fixture restores its private save provider and destroys only owned objects. Actual native lifecycle, partial clone disposal, physics and current test-runner isolation remain unexecuted. A support fixture being called “nonparallel” in the old audit is historical wording; its current source contains no unsupported attribute.

**A-39, P1, confirmed recovery defect:** `InventoryDropper.DropSlot:288`, `DropRecord:412–415`, `ReAddRecord:423–437`. An instance removed from physical slot3 while slot0 is empty returns via generic Add after a state-restore exception, so it moves to slot0 although Drop reports0. Expected a failed drop restores physical provenance, identity/payload/count. Minimal repair: retain original physical location through physical/packed/by-id extraction; constrained restoration first targets that slot, with explicit collision/remainder handling if a callback occupied it. Never overwrite another item or ignore partial restoration return. Regress slot3/empty0 with injected restore failure, unchanged serialized layout and zero world clones; add conflict conservation. Existing restore-failure test checks only total/identity in aggregated mode. Original plan R1 is confirmed.

**A-40, P1, confirmed persistence gap:** `InventoryComponent.Operations.FinalizeMutation:238–242` publishes before Save; paired `InventoryComponent.Grid.TryApplySlotTransfer:125–128` finalizes source then target. A throwing source notification aborts autosave of committed source and prevents target notification/save, although both in-memory records already changed. Expected both eligible persistence attempts happen for committed endpoints and exceptions remain visible. Minimal repair: separate change determination/notification/persistence and finish each endpoint in finally, preserving first exception after both finalizers and ordinary event order. OnSaved throwing must not prevent the other endpoint's attempt. Provider exceptions remain explicit; no atomic cross-provider durability or continuing every listener within one UnityEvent is promised. Plan R2 is confirmed.

**A-41, P1, confirmed world-item conservation gap beyond A-40:** `PickableItem.TryCollectCore:216–228` waits for AddItemInstance/AddItemByIdAmount to return before changing `_wasCollected`/`_amount:237–244`; those add methods commit storage then invoke FinalizeMutation before returning (`Operations:52–63`, `:105–113`). A throw-once OnInventoryChanged listener leaves inventory +1 while pickup remains fully collectable; after the guard resets in finally, another Collect grants a second unit. Conversely `TryTakeSlot:90–93` / `Queries.TryTakeRecordAtPackedIndex:109–112` remove storage before notifications; a throw escapes DropSlot/DropPackedIndex before spawn/recovery, leaving no world item. Expected a notification failure cannot duplicate or lose committed pickup/drop units. Minimal repair: narrow internal commit/receipt boundary that exposes the actual accepted/extracted record before user notification; update source pickup ownership/remainder or guarantee drop spawn/restore conservation before propagating notification failure. Do not infer a delta from live before/after balances after reentrant callbacks, or suppress arbitrary errors. Keep public signatures and normal event ordering where possible; no generic transaction framework is required. Add throw-once item-added/inventory-changed/OnSaved cases for simple and instance pickup plus physical/packed drop; assert combined inventory/world quantity and identity. Autosave-finally alone does not repair this gap.

**Cross-owner mandatory residual:** A-08 remains current at InventoryItemData.OnValidate:69–71; do not serialize cache-owned generated preview sprites. Stop generated assignment, preserve authored icons and supply presentation fallback through existing preview lookup. Selected-item count text currently documents explicit Refresh and an additive selection event is **optional improvement**. Equipped visual payload edits are not committed back to storage; the identity/revision capability and opt-in hand capture proposal is **future work** requiring explicit capture/migration policy, not a mandatory new framework for these repairs.

Aggregate cost: every facade mutation captures deep record snapshots/count dictionaries before and after, unions ids and may serialize the full container. Stateful bulk adds/pickups can repeat that O(container/payload) work per unit; views rescan counts per displayed id and rebuild lists/sets. Slot replacement/import validation clones the full slot set, so loading many slots can be quadratic with deep payload copies. InventoryHand recreates/restores its visual on inventory changes, even unrelated counts, and can repeat hierarchy capture/restore scans. These event-driven costs are source-visible and unmeasured; consider narrow no-copy count traversal/batched mutation or relevant-item revision filtering before adding a new core. Input hot loops are only hand/dropper key polling (A-25), not world physics queries; FindDefault is an optional resolution fallback. Generated UI rows/slots and hand visuals now have explicit component ownership, while static transfer selection is canceled on disable/rebind. No new required scene registry or sample rewrite is justified.

## Final coverage and barrier handoff

All 25 assignment entries have complete audit/plan reads and every assigned current changed source hunk inspected; Core and both Abilities correction artifacts were also read in full. Complete new regression sources are listed with SHA256 in each module section. Dependency sources were read only to verify exact boundary claims; they were not changed. Historical compile receipts and original workers' static checks were not adopted as current acceptance. Findings A-01–41 are source findings/triage dispositions; A-22 is a documented mirror limitation needing root approval, A-33 is a narrowed cross-owner parity residual, and explicit needs-evidence items remain distinct from confirmed defects. Root should assign approved repairs to the SAME original authors before any final test stage. Cross-owner original-task routing: A-28 base hooks belong to Neo.Tools.Text `neo-audit-1023-b08-04`, with derived integration by Components `neo-audit-1023-b06-05`; A-33 belongs to Neo.Editor `neo-audit-1023-b03-03`.

At 2026-10-23 09:49:05 UTC the following assigned-source manifests were hashed by read-only enumeration. Each digest is SHA256 of sorted assignment paths encoded as `UTF8(path) + NUL + ASCII(lowercase file SHA256) + LF`, concatenated in that order. Supplemental scopes deliberately overlap Extensions; a hash is source identity, not proof of execution.

| Assigned module | Files | Current assigned-source manifest SHA256 |
|---|---:|---|
| Neo.Core.Level.Bridge | 4 | `e18be2ad29166d4ed17630f52cab5e143a3641b680a4f3769fa5a21715deda4a` |
| Neo.Core.Level.Components | 1 | `16aa556b0a1fdcd2803aeb6da029687240553cb13394b81f54002f74a028ad29` |
| Neo.Core.Level.Data | 2 | `f051c2963a5a3bb5b66261b37005a861398f80ea48225722b73d13078ad43895` |
| Neo.Core | 16 | `aa292a9e5f70981f2f5f55d91318e0e6c78628c39afe662f4559f86b196130b5` |
| Neo.Core.Resources.Components | 2 | `1254f0e73ded754a4a9afcd0ebea8ef22cf25096e90aa2c5fb199cc266231d65` |
| Neo.Extensions | 31 | `0fed25f2050741b546354fddafdac1042b075b8403413fd49edb8dc72a31ac5b` |
| Extensions-Numeric-Time-Random | 8 | `924bdf985d79baefa2a7875a399c92778f68398be5925defc3fcd329efcc5856` |
| Extensions-Coroutines-Unity | 23 | `1ca94ef0acca9989ceb5f2d747efb41a6b5f24d2288f27bdbab2a14871e38880` |
| Neo.Abilities.Editor | 4 | `d40bd959b2b1c65b21feb5d6754e916e9ad53ad380884b289498d8d967237434` |
| Neo.Abilities | 72 | `813f0a410c3443680e8904593d3d508658a22c4c4e726e1619773c639f4aded4` |
| Neo.Network | 37 | `e4c771e1da98617afdf51c0cfb6271ee4d1257f5df9917fafddeb523d53704c6` |
| Neo.Network.Contracts | 2 | `12e96dd5a4c84f5d863255d0c2de90a0f0dbbd4206b50887a61d2f93f5466fe7` |
| Neo.Tools.InteractableObject.Core | 4 | `e39fbafc2c562d260c49b2b38ad722358b137b2ffcddea864722e6a386110fd9` |
| Neo.Tools.InteractableObject | 6 | `5ddbfc50761f5670c6a25e703d18cb51408ec5ddb242963f36db8c251f38f4f4` |
| Neo.Tools.Move | 31 | `7acbbe866c43a8b3767fe0e89e4990f563505046777fbfc345bd2da2e99e3b11` |
| Neo.Progression | 13 | `13190a68cb70a90f7948b1ce018b5680902923b4cf1258b6474ed52f2fe1f838` |
| Neo.Tools.Input | 10 | `986abd00a5fdd4fc06d7b5498561e35fc8e199746df9afd14e1410e40f4b9a76` |
| Neo.Settings | 10 | `03b65d29c77093dbbf2d7c4e0b2ce1af37667e7310faf206553d4815d5e043b9` |
| Neo.Tools.Components | 13 | `3014b7898bc752479a4532b19d51932473d622eba454eec501e6b846de2aa9ea` |
| Neo.Tools.View | 10 | `3b7f5e50a29eb7d8d3e5ee97c07ae8feda89dc1becf93a3fbcc960926c535826` |
| Neo.Condition | 6 | `57a1dc2dfbcb33b43a5b62daa71f6b020ebd8e16703f59fc6b31eac60463f735` |
| Neo.Tools.Other | 4 | `2fd13aec064c23dd3ac386c5ea400d802ce0702a888e78f08d26ac2c08889afc` |
| Neo.Quest | 8 | `a1b8f822d798ea68bfa55f2bbab4430b3ea95656f185e4f0fa96b15a052d0d8a` |
| Neo.Reactive | 2 | `619952dbadeb31f2174d896fb5db504d5b72ca0303fd457b36bf675a1f4bd366` |
| Neo.Tools.Inventory | 39 | `ca58bb4e012ff7eec09bc142aaff928cd59354f44ee5b61aa6b98cb5a6fdea27` |

Future execution plan requirement from root (recorded only; no isolated project touched in this source-review phase): projectPath isolation is insufficient when copied company/product reuse persistence namespaces. Before later final runs, the single Unity operator must give EACH validation host unique companyName/productName, confirm task-owned effective persistentDataPath/PlayerPrefs namespace, and record those effective values. Preserve main settings/user saves. Final receipts must identify the exact source snapshot SHA, actual Unity version, MIRROR symbol/test discovery and nonempty executed/skipped counts. Current source-review completion authorizes no Unity/tests/checkers now; root explicitly resumes the same operator after all approved source corrections finish.

## Cross-owner order and future proposals

The authoritative interaction query design requires exact query-context sharing, full reusable buffers, epoch/invalidation semantics and independent per-target LOS. These are approved design requirements to review, not a claim that runtime implementation exists. New input/gesture frameworks or broader architectural proposals remain future work unless root explicitly approves them.
