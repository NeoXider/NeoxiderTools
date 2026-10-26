# Tests-Play-Network: supplemental deep audit

Status: source review and supplemental changes complete; Unity compilation and execution reserved for coordinator.

Baseline: `ca1985564dfe7726834e2832d51c33e0e63e060c`, package `10.19.0`. Project version file identifies Unity `6000.3.14f1`. Assignment model: `gpt-6.1-sol`, reasoning `max`.

## Scope and ownership

Read `AGENTS.md`, `WORKER-CONTRACT.md`, the exact supplemental manifest entry and the original report. The supplemental restriction takes precedence: all 15 listed existing C# files were read-only. No existing test, runtime source, other owner's document, assembly definition, package setting, scene or prefab was edited by this worker. No child agents, commits, installs, Unity/MCP calls, builds, test runners or network/runtime processes were started.

Reviewed every listed file: initially 3,139 lines and 70 test methods. Fourteen files subsequently changed under the primary owner's concurrent work; their diff hunks were reviewed. The refreshed inventory is 3,174 lines and the same 70 methods. Changes credited below to the primary owner are not supplemental fixes or executed test results.

Context inspection was limited to the relevant runtime APIs, existing EditMode network coverage, the Play test asmdef, canonical Network docs, local Mirror core and the shared in-memory save provider. Applied `neoxider-tools` and `unity-game-agent` as file-only audit guidance; user restrictions override their normal Editor/delegation workflow. There was no independent QA agent.

## Complete file review

All paths in this table are under `Assets/Neoxider/Tests/Play/Network/`.

| File | Tests | Reviewed behavior and limits |
|---|---:|---|
| `InteractiveObjectNetworkTests.cs` | 4 | Host interaction/click event counts, non-networked fallback, default authority and synthetic sender authorization. Host counts detect duplicate events; the synthetic connection does not send a Command. Primary owner added bounded startup and tracked player-prefab cleanup. Remote owner acceptance/rejection remains outside this fixture. |
| `MoneyNetworkTests.cs` | 3 | Shared Add/Spend and personal wallet state. Assertions inspect one host wallet, not another peer. Primary owner now scopes an in-memory `ISaveProvider`, restores it in `finally`, tracks the prefab and bounds startup. Internal behavior-array reflection remains version-sensitive. |
| `MultSceneNetworkTests.cs` | 2 | Development scene template cloning/activation and two context-relay target changes. Calls relay APIs directly, rather than causing a collision. Legitimate missing-development-scene skip; `Assets/Scenes/Mult.unity` exists in this checkout. Scene/host waits are now bounded by the primary owner. Single-scene loading and broad manager teardown require an exclusive test environment. |
| `NeoNetworkBootstrapTests.cs` | 11 | Solo/refused dedicated requests, host settings, client address, dedicated presentation suppression/frame cap, status providers, duplicate bootstrap and static flag release. Port assertion checks a property, not a bound socket. Frame/VSync/background snapshots are good. Wait helper now fails on timeout. Duplicate object's test-body cleanup is not protected against an earlier assertion failure. |
| `NeoNetworkManagerScenePlayerTests.cs` | 4 | Template/copy separation, automatic migration, independent Counter state and per-copy weapon changes. The second player is an unspawned copy created through a private method; the pickup is test-owned behavior. This checks clone isolation, not a second client. Local pickup/copy cleanup occurs only at the successful end of the test. Startup waits were fixed by the primary owner. |
| `NeoNetworkManagerSessionTests.cs` | 14 | Handshake modes, event order/count, listener failure, manual player attachment, restart, scene activation, handler restart, host exclusion and 4,500-byte fragmented delivery. These include real serialized Mirror loopback exchange. Fragment assertions verify complete bytes and delivery count. The primary owner's shared failing timeout now prevents silent wait expiry. |
| `NetworkActionRelayTests.cs` | 7 | Void/float/string, server-only/others-only scope, named channel and invalid index. Host counts and unrelated-channel checks are useful; others-only has no positive remote recipient. Invalid-index test asserts only no exception. Primary owner bounded startup and tracks the player prefab. |
| `NetworkContextActionRelayTests.cs` | 2 | Matching clone child changes, unchanged sibling/template and local-player context. Explicitly disables local-context filtering for the synthetic second-player scenario. This leaves the default dedup filter untested here. Spawn waits are now bounded; second-player spawn also has a bounded 120-frame loop and explicit netId assertion. |
| `NetworkEventDispatcherTests.cs` | 3 | Both purported networked host tests leave `isNetworked=false`; they exercise local fallback. Offline test is a valid local-path check. Supplemental RPC-observed coverage added separately. Primary owner fixed startup/prefab cleanup, not this coverage mistake. |
| `NetworkOwnerFilterTests.cs` | 3 | ServerOnly/Everyone allow on host and boolean query. No default LocalPlayerOnly or actual denied pure-client case. `onDenied` is only asserted false. Startup/prefab cleanup fixed by primary owner. |
| `NetworkSingletonTests.cs` | 3 | Initialization, host authority and Command value mutation on the same singleton instance. Does not establish SyncVar replication to a remote object or late join. Contains the inert `DummyTransport` and shared manager subclass. Primary owner fixed wait/prefab lifetime. |
| `NetworkTestHelper.cs` | 0 | Inactive manager assembly before activation and explicit transport assignment are sound. Private `_assetId` reflection lacks a missing-field diagnostic. New primary-owner `WaitUntil` validates inputs and fails against a realtime deadline; `WaitForHost` composes it. Helper still leaves transport/session lifetime to callers. |
| `NetworkToolsTests.cs` | 9 | Counter, Selector state/events, fill/exclusion, condition, bounded random results and reactive notification count. Observable outputs are meaningful host checks. “Globally” still means the same host instance. The pure reactive bridge case also lives in a host-setup fixture. Primary owner fixed wait/prefab cleanup. |
| `PhysicsEventsNetworkTests.cs` | 2 | Reflectively invokes private collision callbacks; actor stays five units away. Verifies host/local callback gating only, not physics contact or replication. Boolean event flag cannot detect duplicates. Primary owner fixed startup and prefab cleanup. |
| `RpgCharacterNetworkAdapterTests.cs` | 3 | Explicitly bounded host connection, exactly-once host damage, immediate limiter rejection and delimiter-rich custom resource snapshot round trip. `try/finally` cleanup is good. Snapshot apply is a private seam on two host-side objects; no late-joining client receives it over Mirror. Existing summary correctly calls this host smoke coverage. |

## Findings and disposition

### PNET-01 — Medium: dispatcher host tests can pass without a network message

Sources: `NetworkEventDispatcherTests.cs:65` and `:88`; `Scripts/Network/Core/NeoNetworkComponent.cs:31`; `NetworkEventDispatcher.DispatchGlobalEvent` in `Scripts/Network/Core/NetworkEventDispatcher.cs:54`.

Trigger: run either host dispatcher test with the default `isNetworked=false`. Observable consequence: the event counter reaches one through local fallback even if RPC dispatch is broken or absent. The existing tests cannot distinguish these implementations.

Supplemental correction: added `NetworkedDispatcher_SendsAndReceivesEachRpcWithoutHostEventDuplicates`. It sets networking explicitly, constructs the behavior before identity Awake, spawns it, verifies its ready observer, observes four outgoing and four incoming `RpcMessage` envelopes, and asserts exact void/int/float/string host events. A same-connection reliable barrier establishes that incoming RPCs were processed before final assertions. Diagnostic listeners are released in `finally`. Existing read-only tests remain unchanged; compilation/execution is pending.

### PNET-02 — Medium: “global” checks largely do not test remote exchange

Sources: `NetworkSingletonTests.cs`, `MoneyNetworkTests.cs`, `NetworkActionRelayTests.cs`, `NetworkToolsTests.cs`; `DummyTransport.ServerSend`, `ClientSend` and connect methods are empty. Local Mirror `LocalConnectionToClient`/`LocalConnectionToServer` implement their own queued serialization path.

Trigger: regress only a pure-client branch, observer filtering, transport connection or late-join application. Consequence: host-local state can remain correct and these assertions still pass. `NeoNetworkState.IsClientOnly` cannot become true while server and client globals are both active. A second host-side GameObject does not create a second peer.

Existing session message/fragment tests are genuine loopback exchange and should be retained. Supplemental ready-player gating and live-handler request/ack tests add received sequences, sender identity and complete Unicode payload assertions. They deliberately make no remote-client claim. A separate-peer matrix is proposed below.

### PNET-03 — Medium: scaled delays remain sensitive to pause and machine timing

Evidence: 26 `new WaitForSeconds(...)` occurrences remain across the 15 reviewed files, including Money, ActionRelay, Tools, Singleton and Physics fixtures. Trigger: `Time.timeScale=0` from the surrounding test environment, or unusually slow message processing. Consequence: a delay stalls despite healthy networking, or a guessed 0.1–0.15 second delay observes state too early.

Primary owner fixed connection/spawn waits, but these scaled sleeps remain. Recommend replacing network delays with the shared realtime deadline plus an observable postcondition; negative cases need a positive same-channel control. Supplemental tests already use this approach and do not mutate `timeScale`.

### PNET-04 — Medium: cleanup still misses test-body objects and global restoration

Sources: `NeoNetworkManagerScenePlayerTests.cs:152` creates the second copy and `:153` the pickup; destruction at `:181` happens only after all assertions. `NeoNetworkBootstrapTests.ASecondBootstrap_DisablesItselfAndTheFirstKeepsOwnership` similarly destroys its duplicate only after assertions. Mirror's `NetworkManager` enables `Application.runInBackground` during startup, and most fixtures do not restore it or the prior `Transport.active`.

Trigger: an assertion fails before these local cleanup statements, or a fixture runs in an Editor with nondefault globals. Consequence: unspawned copies/pickups can survive to affect later object searches; global state differs after the run. The eight legacy dummy-player leaks identified initially have been fixed by their primary owner, so they are not an outstanding finding.

Supplemental fixture tracks every created object, refuses a foreign active session/manager or preexisting scene identities before claiming ownership, restores background/frame/VSync/transport values, clears its registrations, and asserts an inactive session afterward. The shared-fixture proposal would extend this contract to legacy tests without editing them here.

### PNET-05 — Low/Medium: assertions cover positive host paths more strongly than rejection and real workflows

Sources: `NetworkActionRelayTests.cs:279` only checks `DoesNotThrow`; OwnerFilter tests never produce an allowed-to-denied transition; Physics tests call `m.Invoke` at `:113`/`:127`; context/scene-player tests invoke relay/pickup methods directly; RPG snapshot test directly invokes `ApplySnapshot`.

Trigger: invalid dispatch mutates an unrelated channel, a remote non-owner passes, a physics setup stops colliding, or initial state is not sent to a newly joined peer. Consequence: the corresponding existing test can remain green. These callback/codec/clone checks still have value and should be described at that boundary.

Recommended regressions for their owners: assert unchanged valid-channel state after invalid input and a subsequent successful trigger; add pure-client denied/owner cases; drive one real physics step/contact; observe a fresh remote peer's Money/RPG state and context target. Do not substitute private callback invocation for those scenarios.

### PNET-06 — Medium: optional Mirror-free test configuration is not established

Sources: `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` directly references `Mirror`, has no version defines and is enabled by `UNITY_INCLUDE_TESTS`; 11 of the 15 listed files are not guarded by `MIRROR` (Mult is only Editor-guarded). This checkout supplies `MIRROR` globally for Standalone.

Trigger: import/run package tests with `UNITY_INCLUDE_TESTS` in a consumer without Mirror. Consequence: this assembly cannot provide Mirror-free test coverage as configured. This is a test assembly dependency issue, not evidence that normal runtime builds without tests fail.

Coordinator/assembly-owner action: separate optional Mirror fixtures/references from Mirror-free tests, then compile/import both configurations. Supplemental file is fully `#if MIRROR` guarded and needs no new reference, but cannot repair the enclosing asmdef's dependency by itself.

### PNET-07 — Medium, cross-boundary runtime: disposing an older handler registry removes its replacement

Sources: `Scripts/Network/Realtime/NetClientHandlers.cs:76`/`:77`, `UnregisterAll` and `Dispose`; equivalent server actions at `NetServerHandlers.cs:72`/`:73`. Each entry calls `ReplaceHandler<T>` when registered and unconditional `UnregisterHandler<T>` when released.

Concrete source-level reproduction: create registry A and register a handler for T; create B and register a replacement for T; dispose A while B remains live. Mirror's type-keyed handler is removed by A, although B still reports registered. A subsequent public `NetworkClient.UnregisterHandler<T>()` returns false; the receive path has lost B's handler. The server registry has the same ownership hazard. No live session or reproduction process was run here.

Consequence: overlapping scene/service lifetimes can remove another live owner's message handler, potentially causing an unknown-message disconnect. Existing tests exercise one registry per type and do not cover replacement ownership.

Runtime-owner action: define replacement/exclusivity policy and use registration leases/generations so an old owner cannot unregister a newer registration. Preserve the current `Add` API; add a two-owner disposal/restart regression. This source is outside supplemental ownership and was not changed. The new guide recommends unique fixture message types to avoid this collision in tests.

### Initial findings resolved concurrently by other owners

These changes were read and credited, not made by this worker. Their runtime results remain centrally pending.

| Initial finding | Evidence of current correction |
|---|---|
| High: Money tests loaded/wrote default persistent keys and unconditionally deleted `Money`/`MoneyAllMoney` through PlayerPrefs. Preexisting developer values could be overwritten or removed; custom-provider writes were not matched by cleanup. | `MoneyNetworkTests` now snapshots `SaveProvider.CurrentProvider`, uses the existing internal `ShopPurchasePlayModeTests.MemorySaveProvider`, restores the provider in `finally` and no longer deletes PlayerPrefs keys. Provider type accessibility was checked. |
| High: 20 unbounded scene/host/spawn loops across 11 files; Bootstrap/Session wait helpers also silently returned after expiry, allowing playerless/failed-handshake assertions to pass. | Primary owner's new `NetworkTestHelper.WaitUntil` validates null/nonfinite/negative inputs and fails at a realtime deadline. The corresponding legacy waits now call it or `WaitForHost`; Bootstrap and Session wrappers delegate to it. Remaining RPG loop was already bounded with explicit postconditions. |
| Medium: eight fixture-local dummy player prefabs were never destroyed, because server shutdown destroys spawned copies, not the original template. | Interactive, Money, ActionRelay, Dispatcher, OwnerFilter, Singleton, Tools and Physics fixtures now retain `_playerPrefab` and destroy it in teardown. |
| Medium: ActionRelay Commands used the instance-wide limiter, allowing peer A to consume peer B's first action budget. | Current `NetworkActionRelay` passes `sender` to all three limiter calls. The runtime owner added `Audit20261023NeoNetworkCommandTests.ActionRelay_OnePeersRateLimitDoesNotConsumeAnotherPeersFirstAction` for void/float/string woven receive handlers. This is command-ingress coverage, not a transport test. |

## Supplemental changes and evidence

Only these five files are supplemental writes:

- [Audit20261023TestPlayNetworkHostContractsTests.cs](../../../Assets/Neoxider/Tests/Play/Network/Audit20261023TestPlayNetworkHostContractsTests.cs) and its new `.meta`.
- [NetworkPlayMode.md](../../../Assets/Neoxider/Docs/Testing/NetworkPlayMode.md) and its new `.meta`.
- This report.

Three new tests observe public behavior:

| Test | Failure signal / contract |
|---|---|
| `ReadyWithoutPlayer_BroadcastSkipsUntilPlayerIsAttached` | A ready playerless host connection must receive neither rejected message; a direct reliable control proves the queue is being read. After public player attachment, broadcast and targeted messages must arrive in exact order with intact text. |
| `LiveHandlers_RoundTripPayloadAndExcludeTheHostFromBroadcast` | Adding handlers after registration must immediately handle a serialized request and acknowledgement. Checks originating connection, Unicode request/ack text, exact received sequence and absence of the excluded host broadcast. |
| `NetworkedDispatcher_SendsAndReceivesEachRpcWithoutHostEventDuplicates` | Exactly four outgoing and incoming object-specific RPC envelopes plus one event for each typed public dispatch. Fails if the fixture accidentally uses local fallback or RPCs echo host events twice. |

Reuse receipt: inspected `neoxider-tools/references/network.md` and coroutine reference in `references/extensions.md`. Reused `NetworkTestHelper.CreateTestNetworkManager`, the primary owner's new failing `WaitUntil`, `NetReadyBroadcast`, `NetClientHandlers`, `NetServerHandlers` and Mirror's public prefab-registration/diagnostic APIs. The original shared `SetAssetId` remains read-only; new tests use public `RegisterPrefab(gameObject, assetId)` instead. No runtime helper, test framework or transport was invented. `AttachPlayer` is a small fixture wrapper around public `NetworkServer.AddPlayerForConnection` with owned-object tracking.

## Documentation

The new narrow English testing guide links existing canonical Network docs and covers public C# entry points, required connection stages, NoCode authority/scope and event counts, inactive construction before identity Awake, snapshot/physics seam limitations, realtime waits and message barriers, ownership/teardown and save isolation. It explains the valid development-scene skip, all three sample layouts, optional Mirror test gating and the distinction between host smoke, loopback and remote evidence.

Coordinator should link this guide from the canonical Network/Testing entry and provide the currently missing `Assets/Neoxider/Docs/Testing.meta`. No other owner's README or folder metadata was edited. Both new package files have unique GUID metadata.

## Substantial improvement proposal — hermetic session fixture and real peer matrix

**Problem.** Legacy fixtures repeat startup and cleanup, often infer readiness from partial flags, and cannot observe pure-client behavior. DummyTransport's success-looking state can conceal a transport failure. Direct test-body cleanup and shared global state make failures order-dependent. The primary owner's bounded wait fixes are a useful foundation but do not solve peer separation or resource ownership.

**Proposed architecture/API, not implemented.** Introduce one test-only `NetworkTestSession : IDisposable` using the existing manager/transport and wait helpers. Suggested surface: `StartHost(options, onReady) : IEnumerator`, `WaitFor(NetworkPeerStage) : IEnumerator`, and `Own(GameObject) : GameObject`, with stages Connected, Authenticated, Ready, PlayerSpawned and Stopped. The fixture leases the session, owns handler subscriptions and objects, snapshots application/transport/save state, and cleans partial startup in `finally`. It rejects a foreign session instead of globally shutting it down. Reuse the existing memory save provider for non-persistence cases.

Add an opt-in Mirror-only integration layer with one dedicated server and two separate clients, driven only by the coordinator/CI operator. Use the production transport and existing scene/runtime APIs, unique per-job leased ports, bounded phase deadlines, structured peer evidence and termination cleanup. Keep ordinary host smoke fast and retain its honest scope. Do not attempt to emulate pure-client role checks by adding another GameObject in the host process.

**Compatibility/migration.** No production public API, serialized fields, sample layout or package dependency changes are required. Migrate legacy fixtures to the session helper incrementally. Assembly separation and reference changes belong to the assembly owner; keep Mirror-free checks independently importable. The helper's test-only ownership API is not shipped as a gameplay dependency. Handler replacement leases from PNET-07 require a separate runtime-owner decision and regression.

**Acceptance scenarios.**

1. A failed startup/spawn or paused game ends at a realtime deadline, reports its stage, and leaves no manager, owned object, handler, modified save value or application setting behind.
2. Ready-without-player broadcasts are rejected, then exact payloads reach the newly spawned peer once; the host's already-applied event is not echoed.
3. Client A's owner-only action succeeds; client B's is rejected. A's rate limit cannot consume B's budget for an allowed shared action. ServerOnly/AllClients/OthersOnly are checked on each actual peer.
4. A fresh client receives current Money/RPG resources and NoCode state; context actions affect the selected player's child and leave the sibling/template unchanged. One real collider contact drives the trigger path.
5. Host/server restart and reconnect retain the intended handlers; disposing an older registration cannot remove a newer owner's handler.
6. Two integration jobs use independent ports and bounded process teardown; host smoke and Mirror-free compile checks remain runnable separately.

**Effort/risk.** Approximately 1–2 engineer-days for the scoped fixture/migration foundation and 2–3 days for the first real-peer matrix on the existing development platform; medium integration risk. Main uncertainties are CI process control, real transport startup variance, Mirror version changes and optional test-assembly boundaries. Add platform/transport cases incrementally after the first reproducible matrix. This is a proposal, not an executed implementation.

## Verification and remaining risks

Actual checks performed:

- Full source read of all 15 assigned files and targeted runtime/Mirror/API context; reviewed concurrent primary-owner changes rather than reverting or claiming them.
- Static inventory and searches: 70 legacy test methods, 26 scaled waits, explicit classification of true loopback messages versus local calls, guard/reference checks and valid development-scene skip. Release sample root is `Assets/Neoxider/Samples~`; no layout changes made.
- Checked new test API signatures against local code, including diagnostic `MessageInfo`, RPC messages, public runtime prefab registration, player attachment, handler registration/disposal and the primary owner's realtime wait helper.
- Strict UTF-8/replacement-character/final-newline/trailing-whitespace checks on all five supplemental files; lexical C# delimiter check, MIRROR guard, explicit types/no `var`, three new UnityTests and nine calls to the shared bounded wait helper. These are static checks, not compilation.
- Both new metadata GUIDs are unique across `Assets`; all four local Markdown targets exist. `git diff --check -- <five supplemental paths>` returned exit 0. Untracked-file whitespace was also checked explicitly because ordinary git diff omits it.

Compile/import, console, Play Mode, all existing/new tests, real ports and remote peers were **not run**, as explicitly prohibited for this worker. The new fixture is designed for the existing `Neo.Tests.Play` references; no asmdef edits are needed for it. Unity 6.5 and other version branches remain uncompiled here. Mirror-generated serialization and diagnostic behavior require central Unity verification.

Coordinator actions: run the supplemental fixture first, then the affected Network suite; resolve PNET-07 with the runtime owner; retain PNET-03/04/05/06 as follow-ups; link the guide and complete folder metadata. The concurrent primary-owner fixes also require central compilation/testing before being treated as verified.

Русское резюме: все 15 назначенных файлов проверены. Добавлены три проверки с наблюдением сообщений и событий, руководство и этот отчёт. Чужие исправления ожиданий, сохранений и prefab-очистки учтены отдельно. Реальные удалённые клиенты и запуск Unity здесь не проверялись.
