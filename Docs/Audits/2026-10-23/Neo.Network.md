# Neo.Network: audit

Status: all 37 owned source files reviewed; focused fixes, regression sources and public docs completed. Static validation passed; compilation and NUnit execution remain with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Network/Neo.Network.asmdef`.
- Scope: 37 C# files; nested assemblies excluded.

## Scope

- `Assets/Neoxider/Scripts/Network/Bootstrap/NeoNetworkBootstrap.cs`
- `Assets/Neoxider/Scripts/Network/Bootstrap/NeoStartupIntent.cs`
- `Assets/Neoxider/Scripts/Network/Core/NeoHandshakeMode.cs`
- `Assets/Neoxider/Scripts/Network/Core/NeoMirrorSceneReactivator.cs`
- `Assets/Neoxider/Scripts/Network/Core/NeoNetworkComponent.cs`
- `Assets/Neoxider/Scripts/Network/Core/NeoNetworkManager.cs`
- `Assets/Neoxider/Scripts/Network/Core/NeoNetworkState.cs`
- `Assets/Neoxider/Scripts/Network/Core/NetworkActionRelay.cs`
- `Assets/Neoxider/Scripts/Network/Core/NetworkContextActionRelay.cs`
- `Assets/Neoxider/Scripts/Network/Core/NetworkEventDispatcher.cs`
- `Assets/Neoxider/Scripts/Network/Core/NetworkExecutionMode.cs`
- `Assets/Neoxider/Scripts/Network/Core/NetworkOwnerFilter.cs`
- `Assets/Neoxider/Scripts/Network/Core/NetworkPropertySync.cs`
- `Assets/Neoxider/Scripts/Network/Core/NetworkReactiveProperty.cs`
- `Assets/Neoxider/Scripts/Network/Core/NetworkReactiveSync.cs`
- `Assets/Neoxider/Scripts/Network/Core/NetworkSingleton.cs`
- `Assets/Neoxider/Scripts/Network/Core/NetworkSingletonRuntimeReset.cs`
- `Assets/Neoxider/Scripts/Network/Lobby/NeoLobbyManager.cs`
- `Assets/Neoxider/Scripts/Network/Lobby/NeoLobbyPlayer.cs`
- `Assets/Neoxider/Scripts/Network/Lobby/NeoNetworkDiscovery.cs`
- `Assets/Neoxider/Scripts/Network/Neo.Network.InternalsVisibleTo.cs`
- `Assets/Neoxider/Scripts/Network/Player/NeoNetworkPlayer.cs`
- `Assets/Neoxider/Scripts/Network/Player/NetworkPlayerName.cs`
- `Assets/Neoxider/Scripts/Network/Realtime/LocalPredictionModel.cs`
- `Assets/Neoxider/Scripts/Network/Realtime/NetClientHandlers.cs`
- `Assets/Neoxider/Scripts/Network/Realtime/NetEventChannel.cs`
- `Assets/Neoxider/Scripts/Network/Realtime/NetFragmentation.cs`
- `Assets/Neoxider/Scripts/Network/Realtime/NetFrameFraming.cs`
- `Assets/Neoxider/Scripts/Network/Realtime/NetFrameTransport.cs`
- `Assets/Neoxider/Scripts/Network/Realtime/NetQuantization.cs`
- `Assets/Neoxider/Scripts/Network/Realtime/NetReadyBroadcast.cs`
- `Assets/Neoxider/Scripts/Network/Realtime/NetServerHandlers.cs`
- `Assets/Neoxider/Scripts/Network/Realtime/SnapshotBuffer.cs`
- `Assets/Neoxider/Scripts/Network/Realtime/SnapshotTimeline.cs`
- `Assets/Neoxider/Scripts/Network/Spawner/NeoNetworkSpawner.cs`
- `Assets/Neoxider/Scripts/Network/Telemetry/NeoNetworkTelemetry.cs`
- `Assets/Neoxider/Scripts/Network/Telemetry/NetTrafficMeter.cs`

## Findings and fixes

Reviewed every file listed above, including the Mirror and no-Mirror paths. No nested assembly, `NetworkContracts` source, excluded `Contracts.md`, existing tests, shared configuration, scenes or prefabs were edited. Source changes are limited to 12 owned C# files. No commits, Unity/MCP calls, compiler/build/test runners, installs or child agents were used.

### Review coverage

| Area | Files | Review performed |
|------|------:|------------------|
| Bootstrap | 2 | Argument/default parsing, URL restrictions, role precedence, startup idempotence, manager fallback, headless presentation/frame-rate ownership, subscription cleanup and status logging. Fixed F10/F11 below. |
| Core | 15 | Manager ready/add-player lifecycle, custom scene-template cloning/restoration and registration, local/remote authority, command/RPC scopes, context ingress validation and budgets, reflection invocation, SyncVar hooks, reactive notifications, singleton lifetime/reset, diagnostics gates and enum compatibility. Fixed F1-F6. |
| Lobby | 3 | Connect/leave/readiness paths, lobby callbacks, LAN response/list lifetime, QuickPlay and transport endpoint handling. Fixed F9/F12; `NeoLobbyPlayer` reviewed without changes. |
| Player | 2 | Local/remote presentation and AudioListener setup, nullable object arrays, nickname sanitization, ownership, rate limit and initial/hook notifications. No focused changes. |
| Realtime | 11 | Prediction/reconciliation/history, snapshot ordering/bracketing/eviction, timeline estimators, quantization bounds, frame count/length limits, fragment slot ownership and sequence wrap, handler registration/restarts/disposal, event delivery and ready-peer filtering. Fixed F7/F8. |
| Spawner | 1 | Null prefab/instance handling, server spawn/destroy, client ghost prevention, parent and offline compile fallback. No change. |
| Telemetry | 2 | Diagnostics subscription lifetime, traffic accumulation/window/ranking/reset, RTT sampling, overlay allocation throttling and no-Mirror stub. No change. |
| Assembly visibility | 1 | Existing friend test assemblies match the new test locations; no assembly/configuration edits. |

### Confirmed defects and changes

Locations below are relative to `Assets/Neoxider/Scripts/Network/`. These are source-proven defects with regression sources; runtime reproduction was not executed in this worker.

| ID / severity | Trigger and observable consequence | Evidence and focused fix | Regression |
|---------------|------------------------------------|--------------------------|------------|
| F1 / P1 | A concrete class derives from an existing `NetworkSingleton<Base>` specialization. Play-start reset tries constructing `NetworkSingleton<Derived>`, violates its generic constraint and aborts the remaining reset scan. | `Core/NetworkSingletonRuntimeReset.cs`, `ResetStaticState` / former `MakeGenericType(candidate)`. Resolve and reset the actual closed generic ancestor instead of manufacturing one for the leaf. | `SceneTests.RuntimeReset_ClearsTheActualSingletonBaseForAnInheritedType`: registered instance disappears after the subsystem callback and can be discovered again. |
| F2 / P2 | A context action configured for `Method(string)` has a null string; `argument?.GetType()` selects `Method()` instead. An unset method name also reaches reflection with null. | `Core/NetworkContextActionRelay.cs`, `TryInvokeConfiguredComponentMethod`. Resolve the signature from `MethodArgumentMode`, retain null as its argument, ignore an empty method name. The touched resolver's `var` was replaced with `Type` per AGENTS. | `SceneTests.ContextMethod_StringModeSelectsStringOverloadEvenForNull`: null/non-null payload reaches the string overload; None reaches the parameterless overload; unset name leaves receiver state unchanged. |
| F3 / P2 | Two clients use the same action relay in one 50 ms window. The first consumes a component-wide timestamp, suppressing the second client's first action. | `Core/NetworkActionRelay.cs`, all three command handlers. Reuse the existing `RateLimitCheck(sender)` overload; no parallel limiter/helper introduced. | `CommandTests.ActionRelay_OnePeersRateLimitDoesNotConsumeAnotherPeersFirstAction`, void/float/string: repeated sender is dropped, different sender is delivered with the right payload. |
| F4 / P2 | A null/empty serialized channel slot is selected. Index validation passes, then dispatch dereferences the null channel. The default C# channel array can contain such a slot. | `Core/NetworkActionRelay.cs`, `ValidateIndex`. Reject null slots through the same gated warning path as invalid indexes. | `SceneTests.EmptyChannelSlotsDoNotPreventDeliveryThroughConfiguredSlots`: empty slot does not mutate listeners; a subsequent named/indexed valid slot delivers all payloads. |
| F5 / P1 | OwnerToServer values arrive on a dedicated server. Only SyncVar fields change; the configured target/reactive property and its server listeners retain stale values. | `Core/NetworkPropertySync.cs` / `NetworkReactiveSync.cs`, `CmdSync*`. Local Mirror evidence: `Assets/Mirror/Core/NetworkBehaviour.cs:536-555` calls setter hooks only under `NetworkServer.activeHost`. Apply a changed value through the existing hook on a non-host server; resolve the property member before first write. Hosts keep their normal single hook path. | `CommandTests.PropertySync_OwnerCommandUpdatesTheDedicatedServersTargetAndEvent` (five types) and `ReactiveSync_OwnerCommandUpdatesDedicatedServerBindings` (three types) assert real target state and one notification. |
| F6 / P1 | An owning client sends a command to a ServerToClients adapter, a locally disabled networking adapter, or another configured payload type. Ownership passes Mirror's outer check, while the old handler accepts any payload into the SyncVar. | Both sync adapters, `CmdSync*`: require `isNetworked`, `OwnerToServer` and the selected value type before rate limiting or mutation. Public APIs/serialized defaults and wire payloads are preserved. | `CommandTests.*CommandCannotOverrideItsConfiguredWritePolicy`: for disabled/wrong-direction/wrong-type requests, assert target/listeners unchanged and identical public `OnSerialize(initialState: true)` output. |
| F7 / P2 | NaN/infinite snapshot timestamps enter the buffer/timeline, or a non-finite frame delta is advanced. Ordering comparisons fail open; infinity can reject every later finite snapshot and NaN can poison the render clock/estimators. | `Realtime/SnapshotBuffer.cs`, `TryAdd`; `SnapshotTimeline.cs`, `OnFrame` / `Advance`. Reject non-finite observations before ownership/state changes. | `RealtimeTests.SnapshotBuffer_InvalidStampDoesNotTakeOwnershipOrEvict`, `Timeline_InvalidObservationLeavesClockAndEstimatesIntact`, `Timeline_InvalidDeltaDoesNotPoisonFollowingFrames`: payload ownership, eviction, sample output, subsequent clock progression and clean-control estimates. |
| F8 / P2 | Large positive lengths/chunks or a negative extreme threshold wrap signed arithmetic. For example `(int.MaxValue + 2 - 1) / 2` wraps to a negative fragment count; `int.MinValue - 24` becomes a huge positive threshold-derived chunk. | `Realtime/NetFragmentation.cs`: use division without overflow for round-up, test the threshold floor before subtraction, and compute frame capacity in `long` with `int.MaxValue` saturation. | `RealtimeTests.FragmentCount_LargeLengthsDoNotWrap`, `FragmentThreshold_DoesNotWrap`, `FrameCapacity_SaturatesAtRepresentableLength`: exact full-range arithmetic outputs. |
| F9 / P2 | A discovered server advertises a non-default port or transport scheme. Both first-server and QuickPlay joins use only `uri.Host`, connecting through the local transport defaults instead of the advertised endpoint. | `Lobby/NeoNetworkDiscovery.cs`, `ConnectToFirstServer` / `OnDiscoveredServer`. Forward the complete URI through the existing Mirror URI API; dispatch through `NeoNetworkManager.StartClient(Uri)` when present to preserve its preparation path. | `LobbyTests.Discovery_ManualAndQuickPlayConnectionsPreserveTheAdvertisedUri`: an in-memory capture transport receives the full URI including port 17891; QuickPlay emits the client result. No UDP discovery socket is needed by the test. |
| F10 / P2 | Mirror is absent but a manager component exists. Bootstrap calls solo no-op start methods, latches `HasStartedNetwork`, returns true and emits a started event for a session that does not exist. | `Bootstrap/NeoNetworkBootstrap.cs`, `StartNetwork`: guard the actual start section with MIRROR; no-Mirror requests return false without latching success or raising the started event. Pure intent parsing remains available. | No-Mirror `SceneTests.Bootstrap_WithoutMirrorDoesNotReportAStartedSession`, with a manager present, asserts false return, false state and zero events. |
| F11 / P2 | Code reads `NeoStartupDefaults.Default.Address` / `DisplayName`. The parameterless struct creation zero-initializes fields instead of invoking the optional-argument constructor, returning null despite the documented default. | `Bootstrap/NeoStartupIntent.cs`, `NeoStartupDefaults.Default`: explicitly supply the default address to invoke the constructor. | `RealtimeTests.StartupDefaults_PublicDefaultContainsTheDocumentedAddress` asserts localhost, empty name and parser consistency. |
| F12 / P2 | A player cancels joining while the transport is still connecting. `LeaveLobby` checks only `isConnected`, so the connection attempt survives Cancel. | `Lobby/NeoLobbyManager.cs`, `LeaveLobby`: use `NetworkClient.active`, which includes Connecting (local Mirror evidence: `Core/NetworkClient.cs:71-84`). | `LobbyTests.LeaveLobby_CancelsAnAttemptBeforeTransportConnectionCompletes` observes Connecting before the call and inactive networking afterward. |

Severity means impact under the listed trigger; no external security exploit or transport-level verification is claimed.

### Regression files

All four files are newly created with this worker's required prefix; existing tests remain untouched. Each has a new `.meta` with a unique GUID.

| File, relative to `Assets/Neoxider/Tests/` | Cases authored | Coverage |
|-----------------------------------------------------------|---------------:|----------|
| `Edit/Network/Audit20261023NeoNetworkRealtimeTests.cs` | 27 | Finite snapshot admission, ownership/eviction, clock recovery, integer overflow and startup defaults. |
| `Edit/Network/Audit20261023NeoNetworkSceneTests.cs` | 4 with Mirror / 5 without | Public relay outputs, inherited singleton reset; bootstrap fallback is no-Mirror only. |
| `Play/Network/Audit20261023NeoNetworkCommandTests.cs` | 17, Mirror only | Dedicated-server command application, write policy, serialization output and per-peer relay fairness. |
| `Play/Network/Audit20261023NeoNetworkLobbyTests.cs` | 3, Mirror only | Advertised URI preservation and cancellation during Connecting. |

The command regressions use Mirror's public `RemoteProcedureCalls.GetDelegate` and its actual woven receive delegates, with serialized payload readers. This exercises command receive bodies and observable state/output without a reflection call into a private command or a second process. It does not replace end-to-end command admission/authentication or real transport tests. Reflection is limited to Inspector-only settings and the private subsystem callback; existing `DummyTransport`, `TestNetworkManager`, `NetworkTestHelper`, reactive properties and Mirror serializers are reused. Source discovery/reference review used `neoxider-tools/references/network.md`; no new runtime service or parallel networking abstraction was introduced.

## Documentation

Updated 12 existing English pages under the owned `Assets/Neoxider/Docs/Network` root:

- `README.md` and `Multiplayer_Guide.md`: distinguish explicit C# APIs from scene adapters, dedicated-server apply/write policy, real bridge type name, discovered endpoints, Cancel behavior, durable state versus transient events; remove incorrect claims of automatic transform/whole-game synchronization.
- `NetworkSingleton.md`: derived concrete types share/reset the actual generic base; scene setup remains unchanged.
- `NetworkContextActionRelay.md`: configured signature and null-string semantics from C# and Inspector.
- `NetworkActionRelay.md`: per-connection command budget, empty-channel behavior and gated warnings.
- `NetworkPropertySync.md`, `NetworkReactiveSync.md`: accepted owner updates on dedicated servers, configuration enforcement, server validation responsibilities, owner-echo option and typed C# bridge alternative; correct the interval example to 0.1 s.
- `NeoNetworkDiscovery.md`, `NeoLobbyManager.md`: public methods, button/event wiring, full discovered URI and cancellation while connecting.
- `NeoNetworkBootstrap.md`: explicit defaults factory and no-Mirror success/event semantics.
- `Realtime/SnapshotInterpolation.md`, `Realtime/NetFraming.md`: finite observation admission, rejected-frame ownership, actual Clear/Reset APIs, packet-loss sampling and overflow-safe arithmetic.

Кратко: сохранены публичные API и сериализуемые поля; исправлены маршрутизация, серверное применение значений, границы входных данных и подключение/отмена lobby. Тесты написаны, но Unity/NUnit здесь не запускались.

## Substantial improvement proposal

**Proposed, not implemented: typed replication binding core with explicit authority and initialization.**

**Problem.** `NetworkPropertySync` and `NetworkReactiveSync` combine polling, reflection resolution, serialized configuration, transport commands, authority, dirty state, owner echo and callbacks. Their useful C# path is currently to write a separate NetworkBehaviour or configure private scene fields. Reflection failures/type mismatches may surface only on an incoming write. Initial owner state, server state and late-join hydration need a clear policy. The dedicated-server and wrong-direction defects above show that transport hooks alone are an inadequate replication contract.

**Architecture / proposed public API.** Keep a plain C# `ReplicationBinding<T>` with an `INetworkValueEndpoint<T>` (typed read/apply), immutable `ReplicationOptions`, and an explicit peer/time context. Suggested contracts:

- `TryCapture(double networkTime, out ReplicatedValue<T> update)` performs dirty checking/interval control without Unity time or Mirror globals.
- `TryApply(in ReplicatedValue<T> update, in ReplicationPeer peer, double networkTime, out ReplicationRejectReason reason)` checks direction, authority and payload validity, then updates the endpoint exactly once.
- An initialization option selects server seed, owner seed or snapshot-only hydration; owner echo is an explicit policy. Runtime `Bind(...)` replaces a target safely and resets dirty state/cache.
- A Mirror adapter owns command admission/SyncVar or message serialization and maps host, dedicated server and clients onto the same apply contract. Existing MonoBehaviours remain scene authoring wrappers around it.
- The Inspector reflection adapter resolves and validates type/accessor compatibility once. A programmer supplies typed delegates/endpoints directly; a reactive endpoint reuses `NetworkReactivePropertyBridge`. ScriptableObjects may hold options/data only, while scene targets remain on MonoBehaviours.

**Compatibility and migration.** Add the typed core inside Neo.Network first; retain component class names, all existing serialized field names, UnityEvents, enum values and wire formats. Existing prefabs/scenes get a compatibility reflection endpoint and existing default policies. Introduce explicit `Bind`/initialization APIs additively; document any intentional policy transition before changing serialized defaults. Shared contracts would require agreement with the Neo.Network.Contracts owner; this audit makes no cross-assembly edits.

**Acceptance scenarios.**

1. The same endpoint tests run in ordinary C# for server-to-clients and owner-to-server; rejected peer/type/direction/value updates mutate neither state nor outbound snapshot.
2. Dedicated server and host observe the same accepted gameplay value and exactly one callback; receiving clients and owner-echo suppression agree with the configured policy.
3. Late join, reconnect, zero/default initial values and a non-default authored owner value follow the selected initialization policy without a transient overwrite or lost first update.
4. A target assigned late or rebound at runtime resolves cleanly; invalid/indexed/read-only/mismatched reflection members fail configuration once with gated diagnostics rather than per-frame reflection exceptions.
5. Pause/time-scale changes do not freeze networking budgets; tests inject a monotonic network clock. Runtime and scene paths invoke the same gameplay endpoint, with no ScriptableObject scene references.
6. Existing serialized configurations and Mirror/no-Mirror compilation remain compatible; preserve a documented C# and Inspector recipe and run dedicated/host/client plus IL2CPP smoke coverage.

**Effort / risk.** Estimate 3-5 engineer days for the core, adapters, migration tests and documentation, plus central Unity/player verification. Medium risk: callback ordering, initial-state ownership and Mirror's host hook timing. Ship in stages with compatibility fixtures; do not turn this audit into a transport/protocol rewrite.

## Verification and remaining risks

### Checks actually executed

- Ownership comparison against the exact manifest entry: 37 unique source files; all 12 edited source files belong to the entry, and all 12 edited public docs are under the owned root with `Contracts.md` excluded.
- Strict UTF-8 decoding, no null bytes/replacement characters, and no implicit `var` declarations: passed for 57 source/docs/new-test/metadata files. New/changed text also passed trailing-whitespace checks.
- Preprocessor stack and lightweight lexical delimiter checks: 41 C# files (37 inventory + 4 new test files), MIRROR off and on, 82 variants passed. This is a lexical consistency check, **not compilation**. A C# parser was not installed; none was installed for this task.
- Public/protected declaration and serialized-field comparison against HEAD: unchanged across all 12 edited source files.
- Local Markdown target existence: passed for all 12 changed public docs.
- New test GUID lookup across `Assets/**/*.meta`: each of four new GUIDs occurs exactly once.
- Scoped `git diff --check -- <owned source/docs/report and four new tests/metas>`: exit 0. New untracked file whitespace was checked separately because git diff does not include their contents until staged.
- Final source diff reviewed for scope and compatibility. No source branch was compiled or exercised; no NUnit result is reported.

### Coordinator actions / remaining verification

1. **Required configuration action:** `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` currently references `Mirror` but not `Mirror.Components`. The new lobby tests directly use `Mirror.Discovery.NetworkDiscovery` and a NeoLobbyManager subclass whose base is `Mirror.NetworkRoomManager`; add the `Mirror.Components` test reference centrally before compiling this fixture. No shared asmdef was changed here.
2. Compile Neo.Network and tests on Unity 6.0-6.4 and 6.5+; check Mirror and no-Mirror configurations. Existing optional-dependency asmdef references/defines are coordinator-owned. The no-Mirror bootstrap test is conditional and needs an actual no-Mirror test setup, not merely inspection of the branch.
3. Run the four new fixtures and the existing Network Edit/Play suites. With Mirror the new files author 51 cases; without Mirror the two Edit files author 32. These counts describe source, not executed/passing tests. Verify woven command signature lookup and callback counts in the installed Mirror version.
4. Check host/remote-owner callbacks, late join and reconnect, scene-player templates, real LAN discovery on a non-default port and Cancel while connecting. The new fixtures use an in-memory transport/response and do not prove socket delivery or a WebGL/IL2CPP player build.
5. Reflection target accessor compatibility, late owner initialization, multiple handler-registry ownership and settings containing non-finite configuration values remain architectural/coverage follow-ups; the proposal above is not implemented. Existing scaled-time polling/rate limits and Mirror's private `hasSpawned` template dependency also need their own integration scenarios. The snapshot fix validates observations/deltas with valid settings; it does not normalize every settings struct.
6. Mirror transport parsing, raw-socket admission and retained-batch behavior remain outside ownership. No Mirror/vendor, other assembly, shared service, package configuration or sample layout was changed.
