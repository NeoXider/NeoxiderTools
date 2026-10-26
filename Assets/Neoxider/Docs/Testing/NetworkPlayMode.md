# Network Play Mode testing

This page describes the package's network test boundaries and how to add useful regression coverage. Start with the [Network module](../Network/README.md) for public runtime APIs and the [Multiplayer Guide](../Network/Multiplayer_Guide.md) for scene setup.

## What each kind of test proves

| Test arrangement | Evidence | Additional verification needed |
|---|---|---|
| Offline component call | Local state and UnityEvent behavior with networking disabled | Network authority, serialization and delivery |
| Host with `DummyTransport` | Host lifecycle and the local server/client path | Real transport connection, ports, pure-client execution |
| Explicit host-loopback messages | Mirror serialization, queued delivery and registered handlers | Separate client objects, remote ownership, transport behavior |
| Server and remote client processes | Actual transport exchange and separate peer state | Other supported platforms/transports and adverse network conditions |

`DummyTransport` in `NetworkSingletonTests.cs` has empty send/connect implementations and opens no sockets. Mirror's `LocalConnectionToClient` and `LocalConnectionToServer` supply the host loopback independently. A passing host test alone does not prove that a remote client received a SyncVar, Command or RPC. There is no real port contention in this arrangement; `PortDummyTransport.Port` only tests configuration propagation.

The host session tests already exchange custom messages and fragmented frames. The supplemental `Audit20261023TestPlayNetworkHostContractsTests` fixture adds three contracts:

- Ready connections without players receive neither broadcast nor targeted `NetReadyBroadcast` traffic; attaching a player enables ordered delivery with intact payloads.
- Handlers added to a live registry receive a serialized request and acknowledgement; excluding the host from a broadcast does not suppress that positive control.
- A networked `NetworkEventDispatcher` sends and receives four real Mirror `RpcMessage` values while each host UnityEvent executes once. Observing both message directions prevents a local fallback from satisfying this test.

These are host-loopback checks. Remote-client verification remains separate.

## Public C# API contracts

For a host fixture, explicitly configure `autoCreatePlayer` and `HandshakeMode` before `StartAsHost()`. Wait for the role and connection state required by the action. `NetworkClient.active` means a connection attempt exists; also check `isConnected`, readiness and player state when the scenario requires them.

`NetReadyBroadcast.CanReceive(connection)` requires a ready connection with a player identity. Check the return value of `SendTo` or `ToReadyClients`, then observe the receiver's payload and count. A send count alone is insufficient evidence of delivery. To test rejection, enqueue a positive control on the same reliable connection and assert the complete received sequence after that control arrives.

Register message handlers before sending. `NetClientHandlers` and `NetServerHandlers` follow `NeoNetworkManager` restarts and implement `IDisposable`; release them when their owner ends. Give each test message a unique type so fixtures cannot replace each other's handlers.

Construct networked objects while inactive, add their `NetworkIdentity` and all `NetworkBehaviour` components, then activate them. Mirror collects behaviors in `NetworkIdentity.Awake`; adding behaviors afterward leaves the cached array incomplete. In test fixtures, the public `NetworkClient.RegisterPrefab(gameObject, nonzeroAssetId)` overload can assign a runtime object's asset ID without reflecting `_assetId`. This registration does not create a project prefab asset.

## Scene and NoCode workflows

For `NetworkActionRelay`, dispatcher, interaction and physics checks, set `isNetworked`/`IsNetworked` explicitly. Assert that the object is spawned and has the intended observer or owner before triggering its public API. Configure authority and scope explicitly for the case being tested.

Count events instead of storing only a boolean: duplicate host execution otherwise remains invisible. Check int/float/string payloads and unrelated channel or player state. For context relays, resolve the entering player's spawned identity, assert only its intended child changes, and keep the disabled template unchanged. A second host-side player copy tests target selection, but does not establish a second connected client.

Calling a private `OnTriggerEnter` method checks its callback logic. Actual collision coverage needs collider/Rigidbody configuration and a simulated physics step. Calling a snapshot apply method checks the codec/application seam; late-join synchronization additionally needs a new client to receive its initial state through Mirror.

`MultSceneNetworkTests` intentionally require the development-only `Assets/Scenes/Mult.unity`; their missing-scene skip is not a package multiplayer pass. General sample validators should resolve the active layout: `Assets/Neoxider/Samples`, release `Assets/Neoxider/Samples~`, or imported `Assets/Samples/NeoxiderTools/<version>/<sample name>`. Renaming sample folders is not part of running these network contracts.

## Determinism and cleanup

Use a realtime deadline followed by a failing assertion for every connection, scene-load or spawn wait. Include role, connected, ready and player state in timeout messages. An elapsed deadline with no postcondition can turn a stalled setup into a false pass. Avoid scaled `WaitForSeconds` for networking; paused gameplay must not suspend test timeouts. Prefer an observed message barrier over a guessed number of frames.

Run fixtures serially in an empty test scene. A fixture must refuse an existing session before claiming ownership. Track and destroy every created object, including player templates and objects created inside test bodies; cleanup must run when an assertion fails. Dispose handler registries and unsubscribe diagnostic callbacks in `finally` blocks. Stop the owned session, clear registrations, release its objects, and restore the previous transport, frame rate, VSync and run-in-background settings. Assert that server and client are inactive afterward.

Money tests should disable persistence for a session-only wallet before `Start` or use an isolated `SaveProvider` and unique keys. Do not delete the development project's `Money` and `MoneyAllMoney` keys. If persistence itself is under test, preserve and restore the provider and prior values through that provider's API.

## Running and interpreting results

Use the Unity Test Runner's Play Mode tab with the project operator's exclusive Editor session. Filter by `Neo.Tests.Play.Audit20261023TestPlayNetworkHostContractsTests` for the supplemental cases. They use the existing `Neo.Tests.Play` assembly, require Mirror and `MIRROR`, and do not require extra assembly references. Their execution and compilation are centrally pending until the operator runs them.

An optional-network release also needs a separate compile/import check with Mirror and its symbols absent. The current Play test assembly directly references `Mirror`, and many legacy fixtures import it unconditionally; excluding a single guarded test file does not establish that configuration. Any split into a Mirror-only test assembly belongs to the assembly owner.

For remote integration, record the exact Unity, Mirror, transport and package versions, peer roles, ports, received payloads/counts and teardown outcome. Use a leased port for real transports and bounded process termination. Keep host smoke results, missing-scene skips and remote integration results distinguishable in the evidence.

## Dedicated server and two independent clients

The source-authored matrix is in `Tests/Integration/Network` (`Neo.Tests.Network.Integration`). Its launcher is [`scripts/network_playmode_matrix.ps1`](../../../../scripts/network_playmode_matrix.ps1). **Compilation, builds and execution are pending the coordinator's frozen-source verification.** These authored cases are not a pass receipt.

The matrix uses the installed `Mirror.TelepathyTransport` over TCP, one dedicated server and two standalone Windows client processes. It refuses Editor/host substitution, a foreign session, nonempty bootstrap scenes, reused output roots and mismatched build/persistence identities. It does not install Mirror or a transport. The test assembly requires `MIRROR`, `UNITY_INCLUDE_TESTS`, Mirror/Mirror.Transports and the runtime assemblies listed in its asmdef; production builds without test assemblies do not get this entrypoint. It contains no ordinary NUnit-discovered cases: launch the three players to execute the matrix.

### Build entrypoint (verification operator, after source freeze)

Use an isolated validation project containing the frozen package, installed Mirror/Telepathy, Windows build support and the test assemblies. Enable `MIRROR` and `UNITY_INCLUDE_TESTS` in that validation project. Do not use the working/user project. The operator invokes:

```bat
"<Unity.exe>" -batchmode -nographics -projectPath "<isolated validation project>" ^
  -executeMethod Neo.Tests.Network.Integration.Audit20261023TestPlayNetworkPlayerBuild.Build -quit ^
  -neoNetworkMatrixBuildRunId <32 lowercase hex run ID> ^
  -neoNetworkMatrixBuildSnapshot <frozen 40 or 64 hex snapshot SHA> ^
  -neoNetworkMatrixBuildOutput "D:\NeoQA\matrix-build-<run ID>" ^
  -logFile "D:\NeoQA\matrix-build-<run ID>.log"
```

`Build()` creates one task-owned empty scene, builds three `StandaloneWindows64` players sequentially with `Development | IncludeTestAssemblies | StrictMode`, then releases that scene/folder and restores company/product/version and the previously active scene. Existing scenes are kept open. The build output must not exist. Check `build-receipt.json.passed`, the Editor exit/log/build reports, and actual compilation/Weaver discovery before launching. A failed build is not a matrix result.

Each player has company `NeoQA_Network_<run ID>`, version `11.0.0` and a separate product:

| Role | Product / output directory | Executable |
|---|---|---|
| server | `NetworkMatrix_server` | `NetworkMatrix.exe` |
| client-a | `NetworkMatrix_client-a` | `NetworkMatrix.exe` |
| client-b | `NetworkMatrix_client-b` | `NetworkMatrix.exe` |

The run ID must be decided **before** building: identity is baked into each player. Reuse the same run ID/snapshot when launching these three builds, with a fresh artifact root. A new attempt uses a new run ID and builds. The snapshot argument is a correlation token for the operator's frozen-source manifest, not a self-computed or independently attested source hash. Record the installed Mirror package/source revision separately; `mirrorAssemblyVersion` is only an assembly version.

### Launch and persistence probes

```powershell
powershell.exe -NoProfile -File scripts/network_playmode_matrix.ps1 `
  -ServerExe "D:\NeoQA\matrix-build-<run ID>\NetworkMatrix_server\NetworkMatrix.exe" `
  -ClientAExe "D:\NeoQA\matrix-build-<run ID>\NetworkMatrix_client-a\NetworkMatrix.exe" `
  -ClientBExe "D:\NeoQA\matrix-build-<run ID>\NetworkMatrix_client-b\NetworkMatrix.exe" `
  -RunId <32 lowercase hex run ID> -SnapshotSha <frozen snapshot SHA> `
  -Port 28771 -OutputRoot "D:\NeoQA\matrix-run-<run ID>" `
  -PhaseTimeout 20 -OverallTimeout 300
```

Reserve a distinct port for every concurrent matrix. The launcher holds a port-specific Windows mutex, probes the TCP port without taking over a listener, starts only its three supplied executables with hidden windows, and keeps their returned process handles. The release-to-bind race can fail the owned server; no foreign process is killed. Startup advances only on `server-listening` and `launch-client-b` receipts. Each connection additionally crosses Connected, Authenticated, Ready, PlayerSpawned and world-initialization barriers. All waits use realtime deadlines. Poll intervals and the explicit one-second cadence measurement are not delivery assumptions.

Before launch, the script requires the three expected Windows persistence directories and PlayerPrefs registry keys to be absent. The effective directories are `%USERPROFILE%\AppData\LocalLow\NeoQA_Network_<run ID>\<product>` (derived from the current account's local AppData location); the namespaces are `HKEY_CURRENT_USER\Software\NeoQA_Network_<run ID>\<product>`. Each player checks its actual company/product/`Application.persistentDataPath` before scenario setup. `-neoNetworkMatrixPersistentDataPath` is an **expected-value assertion**, not an override of Unity storage.

Before the first provider load/write, the player records the actual identities, PID, transport/port and store locations. It then installs `FileSaveProvider("matrix-save.json", new FileSaveProviderOptions { PersistenceRoot = <artifact root>/<role>/save })` through `SaveProvider.SetProvider`, round-trips and removes only its unique sentinel, and checks the same provider before each phase. The original Counter/Selector saves, original wallet's persistence and RPG load/autosave are disabled. NI7-02 uses one additional persisted wallet; the Counter malformed-input cases use separate persisted fixture identities. All use unique run-owned keys in this same isolated provider. No scenario calls PlayerPrefs. Thus implicit Unity state and explicit scenario stores both have owned, distinct namespaces.

The launcher records registry **key/value names only**, effective paths and whether they exist in `persistence-probe.json`; it does not read or erase user values. It retains all owned logs/save roots for inspection. `owned-processes.json` identifies every child it may terminate. Normal completion requires each player's successful cleanup/result receipt and zero exit code. A deadline/failure kills only still-running handles it created, and records forced termination as cleanup evidence debt. No recursive persistence deletion occurs.

Direct runtime CLI switches are `-neoNetworkMatrix`, then `-neoNetworkMatrixRunId`, `Snapshot`, `Peer`, `Root`, `Company`, `Product`, `PersistentDataPath`, `Port`, `PhaseTimeout`, `OverallTimeout` (each suffix carries the same `-neoNetworkMatrix` prefix). Use the launcher to create the owned marker and directories. Peers are exactly `server`, `client-a`, `client-b`; phase timeout is 2–120 seconds and the overall player timeout is at most 3600 seconds.

### Authored observable contracts

| Case | Required receiver evidence |
|---|---|
| RPG initialization and late join | A's local template starts at HP/max9 and custom resource1, yet receives server120/120/10. After real server mutations, A and newly admitted B receive73/120/7, including a delimiter-containing resource ID. |
| Concurrent Counter.Add(1) | Both independent callers record10; actual server Command ingress yields11,12. Only the test world's outgoing replication is temporarily held through Mirror's public observer collections. The server's12 is checked before two explicit trusted publications13,12 restore both receivers; each receives exactly those two payloads. |
| Counter malformed remote inputs (NI19) | Separate persisted FLOAT fixtures reject NaN/+Infinity/-Infinity for Set/Add/Multiply and finite-operand raw overflow, with zero state/event/save/RPC effects. The same peer's immediate finite operation succeeds in the measured server window. An INT2 multiplied by float.MaxValue case requires rejection before integer normalization can hide infinity. Finite fractional/independent-peer controls remain mandatory; original Counter12/Money213 are checked throughout. |
| Owner / unknown / local-only / rate admission | Owner A's11 is delivered once; B's22 is absent. Unknown policy delivers zero events, with32 on a separate known-policy component as positive control. Local-only queued ingress delivers zero events, then accepted42 still works on that same component within a test-owned1000-second budget. A's duplicate51 is throttled while B's52 is independently accepted. |
| Relay scopes | AllClients: server/A/B each71 once. ServerOnly: server72 once, both clients zero. OthersOnly from A: B73 once, server/A zero. |
| Selector | B cannot change authoritative state; its optimistic local Set is reported without asserting an unsupported rollback. A's accepted additive choice3 retains active0,2,3 and complete exclusions; reconnect receives the complete snapshot. Test-only long budget makes a denied B consuming A's budget observable. |
| Money | Fresh ServerOnly rejects both clients; unknown policy rejects. Actual malformed Command wire payloads (refund7, unknown255, NaN, negative add/spend, unaffordable spend) precede a valid same-channel add1 and must consume no budget. Explicit None permits3/4; OwnerOnly permits A5 and rejects B7. All three receive final213. |
| Money NI7-02 | Additional persisted wallet: server100000000, lifetime123, level7, last change17. Actual remote TrySpend(1) returns pending but is denied with zero state/key/event/RPC effects. Same-peer TrySpend(1), TrySpend(5) in one measured server frame admits only5: stored balance99999992, debit8, nominal last change-5; both receivers get one Spend RPC with payload5. Fractional10.5 minus0.25 reaches10.25. The original213 wallet and all reconnect oracles remain unchanged. |
| Handler generations | Older sets disposed after replacement cannot remove the current client/server handler. Exact Unicode request/response payloads and one reply per peer survive opposite disposal order, B reconnect, and a complete dedicated-server restart. Disposing the current client generation also leaves no previously registered handler. |
| Paused polling / late reactive field | With server timeScale0, unchanged property reads stay bounded by the0.2-second interval over an explicit realtime window. Property9 and a field changed from null to ReactivePropertyFloat15 reach both clients while still paused. |
| Physics/context | A's real server Collider/Rigidbody enters a trigger. Server/A/B each observe exactly one PhysicsEvents3D entry carrying A's actual netId. AllClients activates A's Bonus on both current clients; the dedicated server's BonusA stays inactive. BonusB stays inactive everywhere. Client colliders are disabled to prevent duplicate local contacts. |
| Reconnect / restart / cleanup | B is replaced by a new connection in the same independent OS process. A retains its active BonusA; B's fresh replicas start with BonusA inactive, and both server children stay inactive. Both peers later reconnect after server stop/start, using owned atomic rendezvous files and readiness barriers. Teardown receipts and process exit are checked for all three roles. |

Authoring reflection only configures serialized fields while objects are inactive; it never invokes a private Command, receive hook, snapshot applicator or physics callback. Malformed Money payloads intentionally exercise Mirror's actual `CommandMessage` decoder; the signature/hash is tied to the installed Cecil/Weaver convention and must be checked during compilation/execution. A signature mismatch or disconnect fails the matrix.

Context actions use `AllClients` and `TriggerOnlyForLocalContext=true`. The physics wait observes the real engine event carrying A's player netId; the subsequent receiver assertions require exactly one such entry on every role. Context target activity is a transient action, so it has separate role expectations from replicated Counter/Selector/Money/RPG/property state:

| Context observation | Dedicated server BonusA | Retained client A BonusA | Client B BonusA | BonusB on every role |
|---|---|---|---|---|
| After the physical entry, with both clients connected | false | true | true | false |
| After B disconnects and reconnects | false | true | false | false |

B's shutdown destroys all its observed player replicas; the registered spawn factory creates their Bonus children inactive. The context relay does not replay the earlier action to these new replicas. The launcher independently checks these role values and the exact physics identity/count, while the complete reconnect snapshot still requires Counter12, Selector3/active0,2,3/excluded1, Money213, RPG73/120/7 and property9/reactive15. No forced child activation or additional state synchronization is used.

Контекстное действие не сохраняет состояние для нового клиента: после переподключения B его BonusA неактивен, у оставшегося клиента A он активен, на выделенном сервере оба Bonus неактивны.

### Receiver schema and limits

Each `<artifact root>/<role>/events.jsonl` line has schema1: `kind`, `runId`, `snapshotSha`, `peerId`, `fromPeer`, `processId`, `connectionId`, `epoch`, `step`, `caseId`, `passed`, `realtime`, `detail`, `environment`, `state`. State carries net/player IDs, ownership/Start, Counter, complete Selector active/excluded arrays, Money, RPG HP/max/resource, property/reactive values, context child states, property-read count, exact handler payload and named event-value arrays. Event-array length is the count; values are checked individually. Handler receiver lines include exact text and server sender connection ID.

`server-receiver` records checked authoritative values, `receiver` records actual client observations, and `remote-receiver` contains the received client observation on the server. Reliable control acknowledgements fence ingress; separate observations prove RPC/SyncVar delivery. The launcher correlates both client and server snapshots by run/PID/epoch/step/case, independently checks core payload/count cases, and requires `cleanup` plus `result` from every player. The final process result is `matrix-result.json` with first failure and separate cleanup failures. A JSON send log, host pass, missing receipt or successful build cannot satisfy it.

### NI7-02: rounded debit and pending client requests

The same launcher also requires `money-ni7-zero-debit-denied`, `money-ni7-rounded-debit-budget`, `money-ni7-fractional-control` and `money-ni7-original-wallet-preserved`. These are source-authored and **UNEXECUTED**, like the other matrix cases. They create one additional network-spawned Money with singleton assignment disabled, an owned `matrix-ni7-<run ID>` save key and persistence enabled. The server initially retains ServerOnly until both clients' startup requests are fenced, then explicitly enables cooperative `None` for these requests. No private Money method, hand-built Command packet or local predicate substitutes for the real client `TrySpend` calls.

The first cost1 request is made with local/server current100000000. It must return `RequestedServerAuthority` on A even though a local float32 subtraction of1 would store no decrease. The actual server then rejects it: current/lifetime/level/last change remain100000000/123/7/17, provider values and persisted-file SHA256 stay unchanged, reactive and save-key notifications are zero, and neither client receives a Money RPC.

A second phase makes A's public reactive CurrentMoney temporarily stale0, invokes `TrySpend(1)` and `TrySpend(5)` consecutively without yielding, and records both returned pending results with before/after0. The fixture restores A's public reactive current to100000000 before yielding. This restoration isolates pending-request routing from the existing delta-based RPC behavior; it does **not** claim that Money repairs stale replicas. A's explicitly asserted current-event sequence is therefore0,100000000,99999992; B observes only99999992. Both real Commands must be observed in the same server frame and less than50ms apart. Only5 changes server current, last change, persisted keys and outgoing RPCs. It stores a decrease of8, while the request/RPC price remains5. No assertion equates nominal price with stored debit.

`money-request-status` carries actual client `MoneySpendResult` fields in `spendRequests`: amount, status, pending/confirmed flags and reported before/after balances. The server's `remote-request-status` contains that same telemetry and sender connection. These statuses are separate from `server-receiver` / `receiver` outcomes: the existing generic control fence is not a new Money request-acknowledgement API. Nested `state.precision` contains current/lifetime/level/last change, stored provider values, phase-before/stored-debit, file SHA256/unchanged flag and measured server ingress frames/times.

Passive Mirror diagnostics count the additional wallet's actual Command/RPC envelopes and decode their real op/amount payloads. Named `ni7-*` event arrays count current, last-change, lifetime, level, key changes, saves and wire op/amount values. FileSaveProvider's public key notification fires for every SetFloat call, including unchanged values, so a rejected spend cannot hide an unnecessary persistence write. Accepted5 requires one current/key update per peer, two server RPC recipients and one actual RPC per client. Each process then flushes and reloads its own real file; `money-persistence-reload` is required for the accepted rounded and fractional controls.

The additional wallet is publicly reseeded to10.5 for `TrySpend(0.25)` after a measured eligible budget boundary; it must store10.25 and deliver exactly one0.25 RPC to each client. It is then destroyed through NetworkServer and both client despawns are observed before continuing the original handler/reconnect/restart matrix. The standalone NI7 status records, exact effect counts, persistence reloads and original Money213 oracle are all required by the launcher.

### Counter malformed ingress (NI19)

The same three-process matrix now requires eight bounded input groups, each with `counter-ni19-<group>-denied` and `counter-ni19-<group>-budget` receiver records. This extension is **source-authored and UNEXECUTED**. The Components owner's remote-only finite-input/raw-candidate guards are present in current Counter source before budget and integer normalization; compilation and actual wire acceptance remain deferred. The extension introduces no general numeric policy or new runtime API, and does not change direct server/offline behavior.

| Group | Actual remote malformed input | Immediate finite operation / result |
|---|---|---|
| `set-nonfinite` | FLOAT10.5: public Set(NaN), Set(+Infinity), Set(-Infinity) | Set(12.5) →12.5 |
| `add-nonfinite` | FLOAT10.5: public Add(NaN), Add(+Infinity), Add(-Infinity) | Add(0.25) →10.75 |
| `multiply-nonfinite` | FLOAT10.5: public Multiply(NaN), Multiply(+Infinity), Multiply(-Infinity) | Multiply(2) →21 |
| `add-positive-overflow` | FLOAT2e38: Add(2e38), whose raw result overflows | Add(-2e38) →0 |
| `add-negative-overflow` | FLOAT-2e38: Add(-2e38), whose raw result overflows | Add(2e38) →0 |
| `multiply-positive-overflow` | FLOAT2e38: Multiply(2), whose raw result overflows | Multiply(0.5) →1e38 |
| `multiply-negative-overflow` | FLOAT-2e38: Multiply(2), whose raw result overflows | Multiply(0.5) →-1e38 |
| `int-multiply-overflow` | INT2: Multiply(float.MaxValue), whose raw float result is infinite | Multiply(3) →6 |

Every operation above is called through the actual pure-client Counter API. The test does not invoke a private body or construct a Counter Command packet. The FLOAT mode prevents client integer normalization from hiding NaN/Infinity before transmission. The INT multiplication case independently catches a server check that runs only after infinity has been normalized to an integer.

Each group gets a fresh additional spawned identity (asset IDs1023100600–1023100607), persistence enabled under `matrix-ni19-<run ID>-<group>`, and a test-only Counter subclass with a1000-second admission interval. Authoring fields are configured while inactive; save loading, optional sends and repeat events are disabled. The actual server public Set seeds the finite baseline, both clients must receive it, and all three flush/reload their isolated provider before the event baseline is reset. The large float baselines2e38 and finite results1e38 also keep the persistence control separate from unrelated decimal formatting precision.

The `-denied` phase requires unchanged exact server/client float32 bits and persisted value, unchanged file SHA256, zero reactive and typed Counter events, zero key/save notifications and zero outgoing/received Counter RPCs. Passive Mirror diagnostics capture each actual Command's operation, operand kind/bits, frame/time and pre-handler value/event/key-write/RPC counts. The installed Mirror emits this diagnostic before dispatch, so the next Command also observes whether the preceding malformed request had any effects.

The `-budget` phase repeats that group's malformed inputs and immediately calls its valid operation from A without yielding. Every recorded pre-handler value remains the baseline and all pre-handler effect counts remain zero, including just before the final valid Command. First/last arrivals must share one server frame and be less than50ms apart. Exactly one finite state/reactive/typed event and key write occurs per process; the server sends two RPC envelopes, and A/B each receive one exact value. This proves the malformed inputs did not consume the sender's budget. Each accepted result is then flushed and reloaded from every process's own real file, with required `counter-input-persistence-reload` receipts.

`counter-ni19-finite-peer-a-rate` adds0.25 then0.5 from A in one frame: only the first is accepted, final10.75. `counter-ni19-independent-peer-b` then multiplies by2 from B within the same test-owned1000-second window, final21.5, proving A's admission does not starve B. These use a ninth fresh identity1023100608. Every fixture is NetworkServer-destroyed and both client despawns are observed before the next fixture or original reconnect matrix. `counter-ni19-original-oracles-preserved` requires the original12/213 afterward. No extra player processes or ports are needed.

The additive `state.counterInput` schema contains case/mode/net/save identity, exact value/stored float32 hex bits, finite-value display fields, persistence SHA256/unchanged flag and actual ingress records. Nonfinite telemetry is encoded with explicit `NaN`/`+Infinity`/`-Infinity` kind and bit strings, never invalid JSON numeric tokens. The mandatory launcher checks all16 rejection/budget receivers, both finite peer controls, exact effects, received wire operands, timing, file reloads and unchanged original oracles. Existing NI7 status/payload/count assertions and all original scopes/reconnect checks remain mandatory.

This is a focused Windows/Telepathy loopback-address integration harness, not internet reliability, other transports/platforms, malicious-client security certification, or a general NetworkTestSession framework. It does not cover packet loss/reordering, raw third-party Mirror handler replacement, absent serialized Money-field asset migration, or aggregate interaction-query1/100/1000 measurements. Those retain separate owner/operator acceptance. Test-only subclass rate windows do not change production defaults. Selector sender admission and Counter finite guards are present in current source; their integrated behavior still requires frozen-source compilation/Weaver and the complete matrix.

Кратко: матрица запускает отдельный сервер и два самостоятельных Windows-процесса через Telepathy. У каждого процесса собственные product, persistentDataPath, пространство PlayerPrefs и файловое хранилище. Проверяются полученные значения и число событий, а не только отправки. Исходники подготовлены; сборка и запуск разрешены оператору только после завершения всех авторов. Отсутствующая проверка или принудительное завершение процесса не считаются успешным результатом.
