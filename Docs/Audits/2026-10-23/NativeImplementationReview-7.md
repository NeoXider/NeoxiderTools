# Native implementation review 7 — Network and Shop

Status: **source review complete; three remaining bounded findings need SAME-owner correction; NI7-01 is mandated by the contract, NI7-02/03 are accepted by root**. Both assigned owners reported SOURCE-FINAL before this review. All compiler/discovery/test/native/performance acceptance remains **UNEXECUTED and globally blocked**. Only this MD is written. No product source, tests, public docs, original receipts/plans, configuration, metadata or validation isolate is changed; no Unity/MCP, compiler, test/validator/checker, service/install/process management or child agent is used.

Branch/target: `demo` / `11.0.0`, implementation contract base `ca198556`; review date 2026-10-24 local. This is a working-tree read, not final frozen-snapshot acceptance. Source reads, scoped diffs, reference enumeration and SHA bookkeeping are the actual evidence.

## Complete coverage and scope

Full current implementation receipts and requests for `Neo.Network` and `Neo.Shop` were read. Network phase runtime hunks and the complete new NetworkSyncMember/NeoLobbyPresenter/handler lease call paths were read. Complete current command (565 lines), lobby (198), handler ownership (149), Shop regression (838), Money authority (232), Shop Play (307) fixtures and both memory-provider implementations were read. All nine Network and five Shop owned documentation phase diffs were read, including canonical navigation and deliberate migration text. Current TextMoney was read in full; Shop commit/refund/reentry, typed/legacy purchase delegation, Money admission/direct paths and the MoneySpendResult contract were traced. All six new script metadata files were read. Local Mirror ReplaceHandler/UnregisterHandler implementations were inspected read-only. Earlier unchanged audit repairs are historical context, not new phase work or execution evidence.

Nearest-assembly ownership is retained: Network original owner `neo-audit-1023-b04-05`; Shop original owner `neo-audit-1023-b06-01`. Editor generator, Network.Contracts declarations/canonical guide, primary test owners, supplemental separate-peer harness, Shop.Bridges, Inventory and base Text are dependency context only. No approval of pending consumers/harness is implied. The two unchanged original Network Edit fixtures retain the hashes/read coverage recorded in native A; they were not rewritten or represented as newly executed tests.

## NI7-01 — P1: handler fixture reintroduces the known unavailable NUnit attribute

Owner: SAME `Neo.Network` / `neo-audit-1023-b04-05`. Current `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkHandlerOwnershipTests.cs:11` declares `[NonParallelizable]` inside its MIRROR-conditional compilation block.

Trigger: compile the authored EditMode fixture with actual MIRROR enabled on the installed Unity NUnit configuration. That attribute was unavailable in BOTH historical preliminary compiler runs and is explicitly forbidden by IMPLEMENTATION-CONTRACT. Source expectation: the fixture compiles using the isolated/exclusive runner and exact cleanup contract. Actual source reintroduces the same unsupported dependency; an MIRROR-off compile can hide it by excluding the fixture. This is a current source-confirmed known compatibility blocker, not a newly executed compiler result.

Smallest repair: SAME owner removes the attribute only, preserves all six meaningful client/server handler payload cases and cleanup, updates its receipt, and relies on later isolated/exclusive execution. No new NUnit package or serial-test framework is needed. Root was notified immediately; no source repair was performed by this reviewer.

## NI7-02 — P2: a positive Money spend can confirm without lowering its stored balance

Owner: SAME `Neo.Shop` / `neo-audit-1023-b06-01`. Current `Assets/Neoxider/Scripts/Shop/Money.cs:279–311` (`TrySpend`), `:319–324` (`SpendLocal`), `:569–571` (`CanSpend`); consumer `Assets/Neoxider/Scripts/Shop/Shop.cs:1207–1209` (`TrySpendCurrency`). Root personally confirmed and accepted the bounded repair; no probe/test was executed.

Public trigger: uncapped wallet AddOverflow(100,000,000), positive spend/price 1. CanSpend checks nonnegative/finite affordability only. The float subtraction stores the original 100,000,000, but TrySpend returns Confirmed and emits the spend path; Shop trusts IsConfirmed and grants the price-1 item without a stored debit. This also reaches cooperative accepted Command ingress through CanSpend. This is the same representational class as the already bounded Core NI2 fix, not a reason to reconstruct callback-sensitive receipts or require a new numeric backend.

Expected: an authoritative positive cost that cannot lower stored Current must reject without mutation/events/persistence/replication or budget consumption. Minimal repair: share an effective float-decrease admission rule in CanSpend and the authoritative TrySpend/Command path, preserving zero-cost behavior, finite/affordability precedence, normal effective fractional costs, policy/order/cap semantics and existing status API compatibility. **Keep the pure-client TrySpend path returning RequestedServerAuthority BEFORE authoritative affordability/effective-debit checks**: its local balance may be stale; the server evaluates its own current candidate before budget/mutation/notifications. Do not reject the remote request solely because the client replica cannot represent a debit. Keep nominal input limits separate from actual representable delta: e.g. 100,000,000−5 can store delta 8, so do not require exact nominal-delta equality. The same owner should add a meaningful price-1 Shop grant rejection, direct/ingress zero-effective controls, stale-client pending control, an effective fractional positive control and explicit rounding documentation. No double/int backend or complex new receipt framework is prerequisite.

## NI7-03 — P2: nested lobby refresh can leave permanently stale outputs

Owner: SAME `Neo.Network` / `neo-audit-1023-b04-05`. Current `Assets/Neoxider/Scripts/Network/Lobby/NeoLobbyPresenter.cs:105–130`, `RefreshView`. Root confirmed and accepted the bounded repair; source tracing only.

Trigger: on the first refresh, an earlier OnPlayerCountChanged listener calls presenter.JoinLobby once. JoinLobby changes connection state and calls a nested RefreshView, which publishes Connecting and commits `_lastStatus=Connecting`. The original refresh then continues to its captured Disconnected status and publishes it last. Future unchanged polling compares current Connecting to the already committed Connecting cache and does not correct the stale label. Committing all cached values before callbacks prevents duplicate same-snapshot output, but does not fence obsolete outer output stages after a nested different snapshot.

Expected: a newer committed view snapshot cannot be overwritten by remaining old callbacks. Smallest repair: a bounded view-publication generation/revision fence between output stages when reentry commits a changed snapshot, preserving ordinary count→countText→ready→status ordering and unchanged-refresh suppression. Same owner should author a public typed Join/reentrant output transcript plus normal/unchanged positive controls. No general presenter/event framework or editor-generator change is required.

## Network action closure

| Approved action / native lead | Current source disposition |
| --- | --- |
| A-10 / `NETWORK-UNKNOWN-POLICY` | `NeoNetworkState.IsAuthorized:160–180` switches on the mode before named trusted allowances; default returns false. Unknown -1/3/int.MaxValue cannot reach null/exact-local-host allowances. None/OwnerOnly/ServerOnly values and named truth table remain. |
| A-11 / `NETWORK-REFLECTION-VALIDATION` | Shared internal NetworkSyncMember validates declared payload/member compatibility before caching: writable scalar fields, non-indexed getter+setter scalar properties, selected mutable reactive type through a field or getter. Readonly/get-only reactive references remain valid. Scalar payload types are sealed/value types, so IsAssignableFrom gives the exact supported scalar types; no int/float/double/string coercion remains. Invalid metadata is cached with a gated warning; accessor faults clear the binding and do not produce successful property notification. |
| A-12 / `NETWORK-LATE-REACTIVE` | Metadata readiness and instance readiness are separate. Valid null metadata remains retryable at a subsequent eligible capture/apply; initialized instances are then cached. The test sends a NEW accepted changed value after constructing the instance and observes its actual listener. No automatic replay/seed or arbitrary later instance-replacement policy is claimed. |
| A-13 / `NETWORK-UNSCALED-CLOCK` | Base shared/per-remote limits, both sync capture timestamps and discovery timeout/refresh use unscaled double time. Direction/network/server-owner gates remain before capture; public duration fields remain float. Gameplay pause is not bypassed globally. |
| A-14 / `NETWORK-POLL-CADENCE` | Each eligible attempt stamps before resolve/read, independently of dirty sends, including missing readiness. Unchanged property and null reactive getter-count controls close the same-window poll; no unconditional per-frame reflect/box loop remains after the interval opens. |
| A-15 / `NETWORK-RELAY-OPTOUT` | All three CmdTrigger bodies short-circuit !isNetworked before peer budget. Existing channel/authority/scope behavior and local public dispatch are retained. The three woven-body tests deny while opted out, then immediately accept the enabled request, observing output count and unspent budget. This is body coverage source, not remote admission. |
| A-16 / `NETWORK-HANDLER-OWNERSHIP` | Client/server entries capture a NEW per-type registration token on each register. Unregister removes only the matching current token; older disposal cannot remove a newer Neo receiver; newer disposal restores no predecessor. Explicit unregister/register can reclaim the type. Authentication defaults, public APIs and reconnect entry points remain. Runtime lease logic is coherent; fixture compatibility remains NI7-01. |
| `NETWORK-CONTRACTS-DOC` | Canonical README links Contracts.md and documents that every optional implementation in a hierarchy must opt out; disconnected state does not imply configuration opt-out. Unknown-mode compatibility change is explicit. Separate Contracts ownership is preserved. |
| `EDITOR-LOBBY-PERSISTENCE`, Network portion | Typed NeoLobbyPresenter APIs and serialized output events exist without a UI package dependency, resolve the current local player at click time and poll changed outputs at 0.1 unscaled seconds. NI7-03 remains in its new runtime publication. Editor save-cancel/prefab/persistent listener integration is outside this source approval and still needs later import/native acceptance. |

The complete six-case handler fixture drives the actual Mirror installed receive delegate with serialized payloads and verifies concrete receiver/count for both client/server, both disposal orders and explicit shutdown/re-registration. It goes beyond IsRegistered/table presence. Direct raw external Mirror registration can replace the slot without updating Neo's token; that documented compatibility limit remains intentional, with no restoration stack/multiplexing claim. Tests following explicit RegisterNow after shutdown do not establish automatic reconnect timing or production sockets.

The changed command fixture has 38 authored cases, lobby 5 and handler fixture 6: **49 current authored cases, all MIRROR-conditional**, not discovered results. The command cases observe actual target/reactive state, notifications and serialized outbound bytes for five scalar/three reactive payloads and invalid metadata. They call the actual woven receive delegate directly and deliberately bypass connection/ownership admission. The host truth-table test proves only its intended local context when later run. The URI test uses a dummy URI transport and injected discovery response; paused timeout invokes Update with UDP disabled. These are useful routing/body/cadence controls, not real LAN/remote acceptance. Time scale and owned networking/objects are cleaned; the runner must own the isolated network session.

Aggregate source cost: eligible poll cadence now bounds unchanged/unready reads; per-payload reflection/boxing and per-registration token allocation remain. Lease checks add constant work per entry on registration/disposal, not per message. Per-peer limiter dictionaries retain historic ids until component destruction; no measured memory result or pruning policy is asserted. Presenter scans only the current local player component at its unscaled refresh cadence and emits changed outputs, subject to NI7-03. Earlier event-driven context/reflection and lifecycle scene-search paths remain separate costs. A typed ReplicationBinding extraction, hydration/seed framework, transport rewrite and arbitrary lookup registry remain proposals.

## Shop action closure

`Shop.ExecutePurchase:555–570` guards both string entry points before currency resolution/debit/callbacks and clears through finally. Asset and retained int/parameterless overloads delegate to those paths. Four item/bundle nested combinations and a wallet-balance callback case observe no second debit/grant, then a later independent purchase succeeds. A throwing failure observer after true rollback also exercises guard release. No deferred purchase queue is introduced.

`TryCommitPurchase:1097–1123` limits compensation to profile clone/change/SaveProfile. BuyAndEquip selection is assigned before the save; only after commit do VisualItems, ShopChanged, purchase, selection and preview observer stages run. Bundle ids and single-purchase ownership decisions are staged BEFORE currency callbacks, the whole profile commits before the first child notification, and later ids/final bundle notification are separately attempted after an earlier stage throws. InvokePurchaseObserver creates a typed immutable fault receipt and logs it; fault-report exceptions are contained without recursive publication. One UnityEvent retains ordinary listener exception behavior inside its own batch. This closes the old retained-inventory/refunded-wallet inconsistency under the explicitly chosen best-effort delivery contract, not a guaranteed inventory reservation/ack policy.

The memory provider fails an actual Shop-key SetString BEFORE storage mutation, once. Rollback tests cover paid item/bundle with ordinary/above-cap balances and free item, inspect persisted JSON ownership/selection, exact current/lifetime balance, no purchase/grant output and successful retry controls. Built-in RefundSpend bypasses reward MaxMoney and does not increment AllMoney. Custom IMoneyAdd refund or repeated storage failure can still fail and be logged; no durable journal or arbitrary callback rollback was promised. The receipt's generic free/paid item/bundle wording is broader than the literal rollback matrix: no distinct free-bundle save-fault case is authored. Shared zero-debit commit logic is source-traced; that specific combination is not reported as executed or separately tested.

Five observer-fault cases retain exact committed balance/profile/selection and no failure event, and observe the fault stage/message. The bundle case changes the original source array and throws after the first id; later staged ids and final event still appear in its expected transcript. The two Play grant cases use actual ShopInventoryGrantBridge and InventoryComponent sources, check persisted ownership and exact inventory counts alongside wallet150/AllMoney200 after price50, and register the throwing observer AFTER bridge subscription. They are meaningful future cross-module scenarios; pending bridge/Inventory consumers and native lifecycle are not approved by this read pass.

Money source adds exactly one serialized policy with ServerOnly initializer and typed property. CmdMoneyOp calls CanAcceptClientMutation before RateLimit; that predicate rejects local-only, unknown opcode/refund7, nonfinite payload, negative deposit/overflow, unaffordable/negative Spend and unknown policy before budget. Named modes delegate to existing IsAuthorized with this wallet GameObject and original sender. There is no HasServerAuthority shortcut. None explicitly trusts amounts, OwnerOnly means this identity's connectionToClient, and trusted null/exact local host allowances occur only after input/policy checks. Direct offline/server/host public paths retain their previous dispatch, soft-cap/overflow and level-adjustment semantics. NI7-02 remains in effective debit admission, rather than in the newly correct policy order.

The authority fixture's 24 authored cases include 18 MIRROR-only cases and six available without it. Its missing-token prefab test removes an ACTUALLY serialized field, imports the unique temporary prefab and checks loaded contents, with cleanup; this is a suitable real import driver once run, not proof from the initializer. Admission-unit tests invoke the private predicate and inspect unchanged state/unallocated budget; they do not exercise Cmd dispatch/RPC or remote authority. The paused Play limiter case invokes the private limiter directly with separate peer ids and real-time delay. Native TextMoney coverage configures a real TMP label, observes a partially running tween, disables without reflective lifecycle calls, then re-enables against the changed balance. TMP/tween capability and base Text dependency remain unexecuted integration conditions.

TextMoney's new protected overrides call base.OnEnable and guarantee base.OnDisable in finally around unsubscribe; current source switching stays detached while disabled and reads current balance on enable. Root-owned base Text hooks are consumed without editing them. Canonical Shop/Bridges navigation and the legacy Inventory-path compatibility pointer now match the parent-first/active-scene Shop fallback and existing-singleton Inventory rule. Deliberate 11.0 observer timing, ignored nested purchases, best-effort delivery, ServerOnly client migration and absent correlated acknowledgement are documented.

Current Shop inventory is **44 regression + 24 authority + 4 Play = 72 authored cases**, including MIRROR blocks; the literal no-MIRROR source inventory is 53. These are attribute counts only. UI refresh remains event-driven; failed wallet-key resolution can still scan per row, and purchase observer closures/bundle lists allocate per purchase. No pass-scoped lookup cache or workload benchmark is claimed. The optional transaction/journal/request-ack framework remains a proposal.

## Deferred integration and execution debt

No source profile, authored fixture, direct private/woven-body call or host loopback is production transport evidence. After ALL writers finish and root freezes an exact source snapshot, the original operator still needs both installed Unity compiler branches, actual MIRROR-on/off dependencies/defines and nonempty discovery/skip counts, Edit/Play suites, native import/lifecycle/TMP outcomes, reconnect/late join/weaving behavior and separate dedicated server + two independent clients using production transport. Money denial must conserve balance, lifetime/level state, persistence/events/RPC and valid-request budget; every operation/policy/owner case must use the server's current state. Direct server/host paths and trusted exact-local/null helper contexts remain separate controls.

Editor generator's save-cancel guard, real saved room/game prefabs and persistent address/Host/Join/Ready/Leave/labels need save/reopen and peer scene-transition evidence. URI tests do not certify actual scheme/port discovery sockets. Reflection stripping on IL2CPP/WebGL, raw external handler replacements, repeated persistence failures, domain/scene reload combinations, native callback order and aggregate timing/allocations remain disclosed limits. Root owns unique validation persistence namespaces and final package/text/meta/GUID/tool checks; none is run here. Pending primary tests, supplemental harness and bridge consumers receive no implicit source or execution approval.

## Artifact identity

SHA256 entries identify this source-read snapshot, not compiler/validator/test results. Requests/receipts and listed new fixture bodies were read fully; runtime/doc read coverage is the complete phase hunks plus call paths stated above. Earlier unchanged Network fixture hashes match native A and retain that historical source-read coverage.

| Read artifact | SHA256 |
| --- | --- |
| `Docs/Audits/2026-10-23/IMPLEMENTATION-CONTRACT.md` | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| `Docs/Audits/2026-10-23/Neo.Network.implementation-request.md` | `2346684a4704532c81b59821d82023a81571fb514a4a0bff385edaf9c488914e` |
| `Docs/Audits/2026-10-23/Neo.Network.implementation.md` | `a6f6c9a18c56fae4dad9916b5fb8de4bc56dabe25bd84bd154ccd95cb0ce6323` |
| `Docs/Audits/2026-10-23/Neo.Shop.implementation-request.md` | `346521652d7f0cbd3c04e3440c98d07da3481a480bce8bd4649548a716003d47` |
| `Docs/Audits/2026-10-23/Neo.Shop.implementation.md` | `4b45adf050270ca02e90975c64d494c5d09d4e5b6efc423222a2542f24fb684a` |
| `Assets/Neoxider/Scripts/Network/Core/NeoNetworkState.cs` | `1e86b88bb98d9b6a1cffe0fb026e2905f7fe412ed0d3638fb8cf5a17e20156c6` |
| `Assets/Neoxider/Scripts/Network/Core/NeoNetworkComponent.cs` | `45012ee0df1c54ffe20ae7b2786c33daf1c0ad57f81a0bd36f62ec6bf6913e60` |
| `Assets/Neoxider/Scripts/Network/Core/NetworkSyncMember.cs` | `1f0b26b7e4d314fdc2bf3e17cba27cf4b26eb635967818a885c7d10e523bb888` |
| `Assets/Neoxider/Scripts/Network/Core/NetworkSyncMember.cs.meta` | `20dc89511d105f14ced585711b342beb3f07f773875036fe0c8c7aed9cea6786` |
| `Assets/Neoxider/Scripts/Network/Core/NetworkPropertySync.cs` | `88f88fa118af456bac8ce6a97417cafa06973aea2944219297731a75a987f2bf` |
| `Assets/Neoxider/Scripts/Network/Core/NetworkReactiveSync.cs` | `f5bda48b0107ce2cfdb49a7cd39980956614b00eeb9b110a8518c277bf504d0c` |
| `Assets/Neoxider/Scripts/Network/Core/NetworkActionRelay.cs` | `0355c64a68628ec5b4bababaab0401f49781a9138dd46e0396e48db4f8e68814` |
| `Assets/Neoxider/Scripts/Network/Realtime/NetClientHandlers.cs` | `e410d7b569d9ff0b1b44b6c49aa2c901f6cf9ec5cc475515a2f06cfddd5a5ce1` |
| `Assets/Neoxider/Scripts/Network/Realtime/NetServerHandlers.cs` | `8a8315cfbac54333ab71178e346402d9cd98903be99424c95898a220753588d2` |
| `Assets/Neoxider/Scripts/Network/Lobby/NeoNetworkDiscovery.cs` | `9905aba2c6489dfe13ae85c52115161d2fc21c24e137d0017ae12caf036e8d2d` |
| `Assets/Neoxider/Scripts/Network/Lobby/NeoLobbyPresenter.cs` | `9222783d0cf3582ecda3633fa6c396f8e402338af826352ca0bbe8210a1abaef` |
| `Assets/Neoxider/Scripts/Network/Lobby/NeoLobbyPresenter.cs.meta` | `707077f84ccb521c0d249e183b00ef3404f0bc351745ae81dd486eece4cf56a8` |
| `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoNetworkCommandTests.cs` | `8887c1ec4fe840496c61dccf888adb7bd595c92847a25200cd047388f37722ce` |
| `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoNetworkLobbyTests.cs` | `6ae712538158b420adf9aeefa6fcdba30c4bc2b5a3c3a9163547fd753e93fe6d` |
| `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkHandlerOwnershipTests.cs` | `fc6caa30ee284b43bf740d55d01df95ae553801db23b4ff2a4f6ebdc527154f0` |
| `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkHandlerOwnershipTests.cs.meta` | `7915eaccb050728d743bafe7697ffe7a9c01057949cf648fa72714714c2fd2af` |
| `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkRealtimeTests.cs` | `25ffd573d0750d50b3aee05faaa88c4934842541168db0d1990883d8f97852f3` |
| `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkSceneTests.cs` | `a1e8a6bc7c758464917f5d1c8cd21020f531924812ae3c21b5c8413ff81cd82d` |
| `Assets/Neoxider/Docs/Network/README.md` | `b1bbf0f3b4216f8d8c73bc85afa0af1106639f76846de3be8c80df6d4653d743` |
| `Assets/Neoxider/Docs/Network/NetworkPropertySync.md` | `70d121afa0e40d56580603b3b58e8989dc232f123b2f23c7c74c9eea71ce566b` |
| `Assets/Neoxider/Docs/Network/NetworkReactiveSync.md` | `4fcede9057b50c26c17a517d6c01239f973ce85771b2a81ca945bf211932a984` |
| `Assets/Neoxider/Docs/Network/NetworkActionRelay.md` | `d13ce3c2c663fb162f7aa9d24838fe5637312c734805b18e84cfc7054ca0b06e` |
| `Assets/Neoxider/Docs/Network/NeoNetworkComponent.md` | `286ec056feb68386cc161958cc924e894152a4bcd72b6baca9805784ab4ffa86` |
| `Assets/Neoxider/Docs/Network/NeoNetworkDiscovery.md` | `ec0559a79ce99b3f1371ae4ff09aa0c9fd2acbdc43e77d57dac7a12e3c02b99f` |
| `Assets/Neoxider/Docs/Network/Realtime/NetMessaging.md` | `858ed2cbd617504545ccb8407e1b634632896106ea15a03e3ddcaf2a0ec6d049` |
| `Assets/Neoxider/Docs/Network/Lobby.md` | `5d458442f9df2a503ba02949fc15f943244bd5a6ab9dbb6df591f1f7dd397a95` |
| `Assets/Neoxider/Docs/Network/Multiplayer_Guide.md` | `15d53ec5bc8cbff4418499be046bf639745080937b157edba378da2f2e04923f` |
| `Assets/Neoxider/Scripts/Shop/Shop.cs` | `29c932ce37396bf49dfb95c229a4e20d152f0bf33c62e738cc51ec07e3ab8d31` |
| `Assets/Neoxider/Scripts/Shop/Money.cs` | `b1d31ae3adbad199d3a30a3044aad9069585c854b2e892ec501826ce977a1806` |
| `Assets/Neoxider/Scripts/Shop/TextMoney.cs` | `67472fe36e2d6f35bbfe2b0926713cb1621fab98e3986e27900fcd6cf4d4642a` |
| `Assets/Neoxider/Scripts/Shop/Events/ShopNotificationFault.cs` | `18e11ca162b417cd3dae857c884038077b1359b2d2f67ed478859f3f931a2ae7` |
| `Assets/Neoxider/Scripts/Shop/Events/ShopNotificationFault.cs.meta` | `1ecdd34906dec5e03825e4d7a5803ccfa344e438bbc8726f2fbba08489d0c513` |
| `Assets/Neoxider/Scripts/Shop/InterfaceMoney.cs` | `aaf69be71d6efb380a08103bc901ac95a8c50b4e24f742a7ebd63c650d158579` |
| `Assets/Neoxider/Tests/Edit/Shop/Audit20261023NeoShopMemoryProvider.cs` | `f459d58f77549455e2a8f85fcb8a30d9a13187c76df2589caf46c0b322822c09` |
| `Assets/Neoxider/Tests/Edit/Shop/Audit20261023NeoShopRegressionTests.cs` | `93998814375ebedaa0b64fccbdd7a2d5d58f5ae33585669bb517a4d9149b9d33` |
| `Assets/Neoxider/Tests/Edit/Shop/Audit20261023NeoShopMoneyAuthorityTests.cs` | `dd7a16d318c48532619b94ae82d52a1054262ab749d2c9ed023499abf73dbb32` |
| `Assets/Neoxider/Tests/Edit/Shop/Audit20261023NeoShopMoneyAuthorityTests.cs.meta` | `b3835857d2c916a8ebcdcc6ab7f6f0c89f365dae0cd06ce9ed27fdd9e6450617` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoShopCommitPlayModeTests.cs` | `58aa4a189fbd4a69a63928a7a6e65b59ab0010fd9322b5d0ee937f7159b31cf7` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoShopCommitPlayModeTests.cs.meta` | `33c8aaa0463a5cdb68a0086d921b6afa3d3a7a375b075ab41faa5b9c3c80cc47` |
| `Assets/Neoxider/Docs/Shop/README.md` | `4a4e6cf7d6d59e64e3ce63bb07d40c8ebbf9bd8b7368f880aedb9a74112df72e` |
| `Assets/Neoxider/Docs/Shop/Shop.md` | `19f6ea746ec88340de69b1d70e5c49f2b41aaf664957a7fd9a195e164d7e26fc` |
| `Assets/Neoxider/Docs/Shop/Money.md` | `8003cbd472d12945def23433d45fde331ea7766dab597ed7e3ccffe39d4b7d66` |
| `Assets/Neoxider/Docs/Shop/TextMoney.md` | `939e57d2ddf11415de954fa5ecd746853ffb7c7b96ba776b299099f8b698dd5b` |
| `Assets/Neoxider/Docs/Tools/Inventory/ShopInventoryGrantBridge.md` | `3ba756e39a33de10a32ef40bb2d22cff90ea1fdfb5b8f12b3b56b615a43fb46c` |
| `Assets/Mirror/Core/NetworkClient.cs` | `2405e6205609e3f564e7401f90bc6de9708be3d157fee393f41b913c3e7671dd` |
| `Assets/Mirror/Core/NetworkServer.cs` | `92f562ea5d05f35ae29bee1d8aac59870db79e3634d922b2d753bb9fa780bf0d` |

## Handoff

The contract requires NI7-01 removal; root accepted NI7-03 for the SAME Network actor and NI7-02 for the SAME Shop actor. Their new receipts/current source require a bounded recheck after correction. Other reviewed phase logic is coherent within the stated scope, but neither area is final acceptance while these findings remain. No test launch is requested now; the execution gate remains closed until every writer finishes and root explicitly resumes the original operator.
