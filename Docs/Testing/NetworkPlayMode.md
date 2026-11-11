# Network Play Mode testing

**What it is:** the network test boundaries of the package and how to add useful coverage, from host-loopback Play Mode tests to a dedicated server with two client processes. Runtime APIs are in the [Network module](../Network/README.md); scene setup is in the [Multiplayer Guide](../Network/Multiplayer_Guide.md).

**How to use:**
1. Pick the arrangement that proves what you need from [the table below](#what-each-kind-of-test-proves).
2. For host tests, follow [public C# API contracts](#public-c-api-contracts) and [determinism and cleanup](#determinism-and-cleanup).
3. Run and record them as in [running and interpreting results](#running-and-interpreting-results).
4. For real peers, build and launch the [three-process matrix](#dedicated-server-and-two-independent-clients).

**Navigation:** [← Testing](../Testing.md) · [Play Mode testing](../PlayModeTesting.md)

## What each kind of test proves

| Test arrangement | Evidence | Still needs |
|---|---|---|
| Offline component call | Local state and UnityEvent behaviour with networking off | Network authority, serialization and delivery |
| Host with `DummyTransport` | Host lifecycle and the local server/client path | A real transport, ports, pure-client execution |
| Explicit host-loopback messages | Mirror serialization, queued delivery, registered handlers | Separate client objects, remote ownership, transport behaviour |
| Server and remote client processes | Real transport exchange and separate peer state | Other platforms and transports, adverse network conditions |

`DummyTransport` in `NetworkSingletonTests.cs` has empty send and connect methods and opens no sockets; Mirror's `LocalConnectionToClient` and `LocalConnectionToServer` supply the loopback. A host pass does not prove a remote client received a SyncVar, Command or RPC, and there is no port contention: `PortDummyTransport.Port` only tests configuration.

Beyond the custom-message and fragmented-frame host tests, `Audit20261023TestPlayNetworkHostContractsTests` adds three loopback contracts:

- Ready connections without players get neither broadcast nor targeted `NetReadyBroadcast` traffic; attaching a player enables ordered delivery with intact payloads.
- Handlers added to a live registry receive a serialized request and acknowledgement, even when the host is excluded from a broadcast.
- A networked `NetworkEventDispatcher` sends and receives four real `RpcMessage` values while each host UnityEvent runs once, so a local fallback cannot pass.

## Public C# API contracts

- Set `autoCreatePlayer` and `HandshakeMode` before `StartAsHost()`, then wait for the role and connection state the action needs. `NetworkClient.active` only means a connection attempt; also check `isConnected`, readiness and player state.
- `NetReadyBroadcast.CanReceive(connection)` needs a ready connection with a player identity. Check the result of `SendTo` or `ToReadyClients`, then the receiver's payload and count. To test rejection, enqueue a positive control on the same reliable connection and assert the full sequence after it.
- Register handlers before sending. `NetClientHandlers` and `NetServerHandlers` follow `NeoNetworkManager` restarts and are `IDisposable`; release them with their owner. Give each test message a unique type.
- Build networked objects while inactive, add the `NetworkIdentity` and every `NetworkBehaviour`, then activate: Mirror caches behaviours in `NetworkIdentity.Awake`.
- `NetworkClient.RegisterPrefab(gameObject, nonzeroAssetId)` assigns a runtime object's asset ID without reflecting `_assetId`; it creates no prefab asset.

## Scene and NoCode workflows

- For `NetworkActionRelay`, dispatcher, interaction and physics checks, set `isNetworked` / `IsNetworked`, authority and scope explicitly, and assert the object is spawned with the intended observer or owner before calling it.
- Count events instead of storing a boolean, or duplicate host execution stays invisible. Check int, float and string payloads and unrelated channel or player state.
- For context relays, resolve the entering player's spawned identity, assert that only its intended child changes and keep the disabled template unchanged. A second host-side player copy tests target selection, not a second client.
- A private `OnTriggerEnter` call checks callback logic only; collisions need colliders, a Rigidbody and a physics step. A snapshot apply call checks the codec; late join needs a new client to receive its state through Mirror.
- `MultSceneNetworkTests` need the development-only `Assets/Scenes/Mult.unity`; their skip is not a multiplayer pass. Demo validators read `Assets/NeoxiderDemos/<name>`; no folder renaming is needed.

## Determinism and cleanup

- Follow every connection, scene-load or spawn wait with a real-time deadline and a failing assertion that reports role, connected, ready and player state. Do not use scaled `WaitForSeconds`; prefer an observed message barrier over a frame count.
- Run fixtures serially in an empty scene; refuse an existing session before claiming ownership.
- Destroy every created object, player templates included, even after a failed assertion. Dispose handler registries and unsubscribe diagnostics in `finally`.
- Stop the owned session, clear registrations, restore the previous transport, frame rate, VSync and run-in-background, and assert server and client are inactive.
- Money tests disable persistence before `Start` or use an isolated `SaveProvider` with unique keys. Never delete the development project's `Money` and `MoneyAllMoney` keys; when testing persistence, restore prior values through the provider.

## Running and interpreting results

- Use the Play Mode tab in an exclusive Editor session; filter by `Neo.Tests.Play.Audit20261023TestPlayNetworkHostContractsTests` for the supplemental cases. They live in `Neo.Tests.Play`, need Mirror and `MIRROR`, and add no references.
- In the 12.0.0 run all 260 network-related Play Mode cases passed (host contracts, relay, sync, lobby, RPG and Money).
- An optional-network release also needs a compile and import check without Mirror and its symbols. The Play assembly references `Mirror` and many legacy fixtures import it, so excluding one guarded file proves nothing; splitting out a Mirror-only assembly is the owner's call.
- For remote runs, record Unity, Mirror, transport and package versions, roles, ports, received payloads and counts, and teardown. Use a leased port and bounded process termination, and keep host results, skips and remote results apart.

## Dedicated server and two independent clients

The matrix lives in `Tests/Integration/Network` (`Neo.Tests.Network.Integration`) with the launcher [`scripts/network_playmode_matrix.ps1`](../../../../scripts/network_playmode_matrix.ps1): one dedicated server and two standalone Windows clients over the installed `Mirror.TelepathyTransport` (TCP).

**Last run: 2026-11-04 (12.0.0).** Unity 6000.3.14f1, three Development players: all 54 cases passed, NI7-02 and NI19 included, with 240 client acknowledgements and clean teardown on every role, in about 20 seconds. Each new run needs its own build and run ID.

The matrix refuses Editor or host substitution, a foreign session, nonempty bootstrap scenes, reused output roots and mismatched build or persistence identities, and installs no Mirror or transport. The assembly needs `MIRROR`, `UNITY_INCLUDE_TESTS`, Mirror, Mirror.Transports and the runtime assemblies in its asmdef. It has no NUnit-discovered cases: launch the three players.

### Build entrypoint (verification operator, after source freeze)

Use an isolated validation project (not the working project) with the frozen package, Mirror and Telepathy, Windows build support, the test assemblies, and `MIRROR` and `UNITY_INCLUDE_TESTS` enabled:

```bat
"<Unity.exe>" -batchmode -nographics -projectPath "<isolated validation project>" ^
  -executeMethod Neo.Tests.Network.Integration.Audit20261023TestPlayNetworkPlayerBuild.Build -quit ^
  -neoNetworkMatrixBuildRunId <32 lowercase hex run ID> ^
  -neoNetworkMatrixBuildSnapshot <frozen 40 or 64 hex snapshot SHA> ^
  -neoNetworkMatrixBuildOutput "D:\NeoQA\matrix-build-<run ID>" ^
  -logFile "D:\NeoQA\matrix-build-<run ID>.log"
```

- `Build()` creates one owned empty scene, builds three `StandaloneWindows64` players with `Development | IncludeTestAssemblies | StrictMode`, then restores company, product, version and the active scene. The output folder must not exist.
- Before launching, check `build-receipt.json.passed`, the exit code, log and build reports, and Weaver discovery. A failed build is not a matrix result.
- Choose the run ID **before** building: identity is baked into each player. Launch with the same run ID and snapshot and a fresh artifact root; a new attempt needs a new run ID and build.
- The snapshot is a correlation token for the operator's frozen-source manifest, not an attested hash. Record the Mirror package revision separately; `mirrorAssemblyVersion` is only an assembly version.

Players use company `NeoQA_Network_<run ID>`, version `12.0.0` (set by the build script) and one product each:

| Role | Product / output directory | Executable |
|---|---|---|
| server | `NetworkMatrix_server` | `NetworkMatrix.exe` |
| client-a | `NetworkMatrix_client-a` | `NetworkMatrix.exe` |
| client-b | `NetworkMatrix_client-b` | `NetworkMatrix.exe` |

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

**Processes.** Use a distinct port per concurrent matrix. The launcher holds a port-specific mutex, probes the port without taking over a listener, starts only its three executables (hidden) and lists every child it may kill in `owned-processes.json`; no foreign process is killed. Startup advances on `server-listening` and `launch-client-b` receipts, and each connection crosses Connected, Authenticated, Ready, PlayerSpawned and world-initialization barriers with real-time deadlines. Success needs cleanup and result receipts and exit code zero from each player; on failure the launcher kills only its own running handles and records that as cleanup debt.

**Persistence.**

- Before launch, `%USERPROFILE%\AppData\LocalLow\NeoQA_Network_<run ID>\<product>` and `HKEY_CURRENT_USER\Software\NeoQA_Network_<run ID>\<product>` must be absent.
- Each player checks its company, product and `Application.persistentDataPath`; `-neoNetworkMatrixPersistentDataPath` is an **expected-value assertion**, not an override.
- Before the first save it records identities, PID, transport, port and stores, installs `FileSaveProvider("matrix-save.json", new FileSaveProviderOptions { PersistenceRoot = <artifact root>/<role>/save })` through `SaveProvider.SetProvider`, round-trips a unique sentinel and rechecks the provider before each phase.
- Original Counter and Selector saves, the original wallet's persistence and RPG load and autosave are off; NI7-02 and NI19 use their own fixtures with run-owned keys in that provider. No scenario calls PlayerPrefs.
- `persistence-probe.json` records only registry key and value names, paths and existence. Logs and save roots are kept; nothing is deleted recursively.

Runtime switches: `-neoNetworkMatrix`, `-neoNetworkMatrixRunId`, `-neoNetworkMatrixSnapshot`, `-neoNetworkMatrixPeer`, `-neoNetworkMatrixRoot`, `-neoNetworkMatrixCompany`, `-neoNetworkMatrixProduct`, `-neoNetworkMatrixPersistentDataPath`, `-neoNetworkMatrixPort`, `-neoNetworkMatrixPhaseTimeout`, `-neoNetworkMatrixOverallTimeout`. Let the launcher create the marker and directories. Peers are `server`, `client-a` and `client-b`; the phase timeout is 2–120 s and the overall player timeout at most 3600 s.

### Authored observable contracts

Each case needs evidence on the receiving side:

- **RPG initialization and late join.** A's template starts at HP/max `9` and resource `1` and receives the server's `120/120/10`; after server mutations A and the late B receive `73/120/7`, with a resource ID that contains a delimiter.
- **Concurrent `Counter.Add(1)`.** Both callers record `10`; server ingress yields `11`, `12`. With outgoing replication held through Mirror's observer collections, the server's `12` is checked, then trusted publications `13`, `12` reach each receiver exactly.
- **Counter NI19.** See [Counter malformed ingress](#counter-malformed-ingress-ni19); the original Counter `12` and Money `213` hold throughout.
- **Admission.** Owner A's `11` arrives once, B's `22` never. Unknown policy delivers nothing (`32` on a known-policy component is the control). Local-only ingress delivers nothing, then `42` still works within a test-owned `1000`-second budget. A's duplicate `51` is throttled while B's `52` is accepted.
- **Relay scopes.** AllClients: `71` once on server, A and B. ServerOnly: `72` once on the server only. OthersOnly from A: `73` once on B only.
- **Selector.** B cannot change authoritative state (its optimistic local Set is reported, no rollback asserted). A's additive choice `3` keeps active `0,2,3` and the exclusions; a reconnect gets the full snapshot. A long test budget would expose a denied B consuming A's budget.
- **Money.** Fresh ServerOnly rejects both clients; unknown policy rejects. Malformed Command payloads (refund `7`, unknown `255`, NaN, negative add or spend, unaffordable spend) consume no budget before a valid add `1`. None permits `3` and `4`; OwnerOnly permits A's `5`, rejects B's `7`. All roles end at `213`.
- **Money NI7-02.** See [NI7-02](#ni7-02-rounded-debit-and-pending-client-requests).
- **Handler generations.** Disposed older sets cannot remove the current handler. Exact Unicode request and response payloads, one reply per peer, survive opposite disposal order, a B reconnect and a full server restart; disposing the current client generation leaves nothing registered.
- **Paused polling.** With server timeScale `0`, unchanged reads stay bounded by the `0.2`-second interval; property `9` and a late ReactivePropertyFloat `15` reach both clients while paused.
- **Physics and context.** A's server Collider and Rigidbody enter a trigger; every role sees exactly one PhysicsEvents3D entry with A's netId. AllClients activates A's Bonus on both clients; the server's BonusA and every BonusB stay inactive. Client colliders are off to avoid duplicate contacts.
- **Reconnect and restart.** B reconnects in the same OS process: A keeps BonusA active, B's fresh replicas start with it inactive, server children stay inactive. Both peers reconnect after a server stop and start through owned rendezvous files; teardown receipts and exits are checked on all roles.

Authoring reflection only sets serialized fields on inactive objects; no private Command, receive hook, snapshot applicator or physics callback is called. Malformed Money payloads go through Mirror's real `CommandMessage` decoder, whose signature hash follows the installed Cecil/Weaver; a mismatch or disconnect fails the matrix.

### Context relay roles

Context actions use `AllClients` and `TriggerOnlyForLocalContext=true`; they are transient, so their expectations differ from replicated state:

| Context observation | Dedicated server BonusA | Retained client A BonusA | Client B BonusA | BonusB on every role |
|---|---|---|---|---|
| After the physical entry, with both clients connected | false | true | true | false |
| After B disconnects and reconnects | false | true | false | false |

B's shutdown destroys its replicas and the spawn factory recreates Bonus children inactive; the relay does not replay earlier actions. The reconnect snapshot still requires Counter `12`, Selector `3` / active `0,2,3` / excluded `1`, Money `213`, RPG `73/120/7` and property `9` / reactive `15`, with no forced activation or extra sync.

### Receiver schema and limits

Each `<artifact root>/<role>/events.jsonl` line has schema `1`: `kind`, `runId`, `snapshotSha`, `peerId`, `fromPeer`, `processId`, `connectionId`, `epoch`, `step`, `caseId`, `passed`, `realtime`, `detail`, `environment`, `state`.

- `state` carries net and player IDs, ownership and Start, Counter, Selector active and excluded arrays, Money, RPG values, property and reactive values, context child states, the property-read count, the handler payload and named event-value arrays (length is the count; values are checked one by one). Handler lines include the exact text and the server sender's connection ID.
- `server-receiver` holds checked authoritative values, `receiver` client observations, `remote-receiver` a client observation as received on the server. Control acknowledgements fence ingress; separate observations prove RPC and SyncVar delivery.
- The launcher correlates snapshots by run, PID, epoch, step and case, checks core payloads and counts itself, and needs `cleanup` and `result` from every player. The verdict is `matrix-result.json` with the first failure and cleanup failures; a send log, host pass, missing receipt or successful build cannot satisfy it.

### NI7-02: rounded debit and pending client requests

Required receipts: `money-ni7-zero-debit-denied`, `money-ni7-rounded-debit-budget`, `money-ni7-fractional-control`, `money-ni7-original-wallet-preserved`. They use one extra spawned Money with singleton assignment off, save key `matrix-ni7-<run ID>` and persistence on. The server keeps ServerOnly until both clients' startup requests are fenced, then allows `None`. Only real client `TrySpend` calls are used.

1. **Zero debit denied.** At `100000000`, lifetime `123`, level `7`, last change `17`, A's `TrySpend(1)` returns `RequestedServerAuthority` although float32 subtraction of `1` stores no decrease. The server rejects it: values, provider data and the file SHA256 are unchanged, notifications are zero and no client gets a Money RPC.
2. **Rounded debit and budget.** A's public `CurrentMoney` is made stale at `0`; `TrySpend(1)` and `TrySpend(5)` run back to back and both return pending with before/after `0`. The fixture restores `100000000` before yielding; this isolates request routing and does **not** claim Money repairs stale replicas. A's current events are `0`, `100000000`, `99999992`; B sees only `99999992`. Both Commands arrive in one server frame, under `50` ms apart, and only the `5` changes current value, last change, keys and RPCs: it stores a decrease of `8` while the request and RPC price stays `5`.
3. **Fractional control.** Reseeded to `10.5`, `TrySpend(0.25)` stores `10.25` and sends exactly one `0.25` RPC to each client. The wallet is then destroyed through NetworkServer and both despawns are observed.

Evidence: `money-request-status` and the server's `remote-request-status` carry the client's `MoneySpendResult` (amount, status, pending and confirmed flags, before/after balances); the control fence is not a Money acknowledgement API. `state.precision` holds the wallet values, stored provider values, phase-before and stored debit, the file SHA256 and ingress frames and times. Passive Mirror diagnostics decode the wallet's Command and RPC op and amount, and `ni7-*` arrays count every notification. `FileSaveProvider` notifies on every `SetFloat`, so a rejected spend cannot hide a write. Each process reloads its file (`money-persistence-reload`) after the accepted rounded and fractional controls.

### Counter malformed ingress (NI19)

Eight input groups, each with `counter-ni19-<group>-denied` and `counter-ni19-<group>-budget` records. Counter's remote-only finite-input and raw-candidate guards run before budget and integer normalization; direct server and offline behaviour is unchanged.

| Group | Remote malformed input | Immediate finite operation and result |
|---|---|---|
| `set-nonfinite` | FLOAT `10.5`: public `Set(NaN)`, `Set(+Infinity)`, `Set(-Infinity)` | `Set(12.5)` → `12.5` |
| `add-nonfinite` | FLOAT `10.5`: public `Add(NaN)`, `Add(+Infinity)`, `Add(-Infinity)` | `Add(0.25)` → `10.75` |
| `multiply-nonfinite` | FLOAT `10.5`: public `Multiply(NaN)`, `Multiply(+Infinity)`, `Multiply(-Infinity)` | `Multiply(2)` → `21` |
| `add-positive-overflow` | FLOAT `2e38`: `Add(2e38)`, whose raw result overflows | `Add(-2e38)` → `0` |
| `add-negative-overflow` | FLOAT `-2e38`: `Add(-2e38)`, whose raw result overflows | `Add(2e38)` → `0` |
| `multiply-positive-overflow` | FLOAT `2e38`: `Multiply(2)`, whose raw result overflows | `Multiply(0.5)` → `1e38` |
| `multiply-negative-overflow` | FLOAT `-2e38`: `Multiply(2)`, whose raw result overflows | `Multiply(0.5)` → `-1e38` |
| `int-multiply-overflow` | INT `2`: `Multiply(float.MaxValue)`, whose raw float result is infinite | `Multiply(3)` → `6` |

- All calls go through the real pure-client Counter API. FLOAT mode stops client integer normalization from hiding NaN or Infinity; the INT case catches a server check that runs only after normalization.
- Each group gets a fresh identity (asset IDs `1023100600`–`1023100607`), persistence under `matrix-ni19-<run ID>-<group>` and a test-only Counter subclass with a `1000`-second admission interval. The server's public Set seeds the baseline, both clients receive it, and all processes flush and reload before events are reset.
- **Denied:** exact float32 bits, persisted value and file SHA256 are unchanged, and every event, key, save and RPC count is zero. Passive Mirror diagnostics record each Command's operation, operand bits, frame and time, and pre-handler counts, so the next Command also shows whether the previous one had effects.
- **Budget:** the malformed inputs repeat and A immediately calls the valid operation. Pre-handler values and counts stay at the baseline, first and last arrivals share one frame under `50` ms apart, and exactly one event and key write happen per process with one exact value on A and B. Every process reloads the result (`counter-input-persistence-reload`).
- `counter-ni19-finite-peer-a-rate` adds `0.25` then `0.5` from A in one frame: only the first is accepted, final `10.75`. `counter-ni19-independent-peer-b` multiplies by `2` from B in the same window, final `21.5`. Both use identity `1023100608`.
- Fixtures are destroyed through NetworkServer with both despawns observed; `counter-ni19-original-oracles-preserved` requires `12` / `213` afterwards. `state.counterInput` writes nonfinite values as explicit `NaN` / `+Infinity` / `-Infinity` kinds and bit strings, never invalid JSON numbers. The launcher checks all `16` receivers, both peer controls, wire operands, timing and reloads.

### Scope limits

A focused Windows/Telepathy loopback harness. It does not cover internet reliability, other transports or platforms, malicious-client certification, packet loss or reordering, raw third-party Mirror handler replacement, absent serialized Money-field migration or aggregate interaction-query `1/100/1000` measurements. Test-only rate windows do not change production defaults.

## По-русски

Host-тест с `DummyTransport` проверяет только локальный путь и не доказывает доставку на удалённый клиент. Матрица запускает выделенный сервер и два Windows-клиента через Telepathy; последний прогон 2026-11-04 (12.0.0) прошёл все 54 случая. У каждого процесса свои product, persistentDataPath, PlayerPrefs и файловое хранилище; проверяются полученные значения и число событий, а не отправки. Собирать матрицу можно после завершения правок, с новым run ID на сборку; отсутствующая квитанция или принудительное завершение — не успех. Контекстное действие не сохраняется для нового клиента: после переподключения B его BonusA неактивен.
