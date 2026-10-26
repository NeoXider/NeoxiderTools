# Native implementation review 26 — final three-process network harness

Status: **COMPLETE; SOURCE ONLY. Two mandatory P2 harness corrections and one P3 comment correction remain.** Other assigned source is scoped approved for the later compile/build/run stage. Only this report was written. No Unity/MCP, compiler/build, syntax checker, tests/launcher, install/service/process management, product/config/other-report edit, child agent or commit occurred. All three-process, wire, physics, persistence and cleanup outcomes remain UNEXECUTED. The eventual sole Unity operator is still `/root/verify_unity`; the global execution gate is closed.

Reviewer: SAME native `gpt-6.1-sol` / `max`, `D:/Git/NeoxiderTools`, `demo` / `11.0.0`. Original harness owner/session: `neo-audit-1023-extra-05`.

Admission: root explicitly confirmed original PID 44700 exited / wrapper done0. Current complete 138-line receipt is SOURCE-FINAL, SHA256 `91a17dfdbff7efc2f9e2a1486ecddffd3c22efe4bdf2d8844ea4b2165d03faf3`. Full request SHA `b4119d5149f42c37b2037b7db7513c35f39aed9e3b6d19e90bfda690f5b3447d` read. Initially saved liveness artifact at `Local/Audits/assembly-2026-10-23/cli-liveness.json`, SHA `6bd000b814286103dd8c5d02657a36fdcc64a04d360843dd024a1b16e59871c6`, mtime `2026-10-23T21:35:19.7943425Z`, lists only GridSystem; no harness writer. No fresh process query was made. Receipt's old pending Selector/Counter descriptions are historical; current dependency methods below confirm the integrated guards.

Final saved liveness read: `Local/Audits/assembly-2026-10-23/cli-liveness.json`, SHA `c955a2a09fe66810a83ce86cfa784fa65a66700ae2b33e9a2956a58be00b1918`, mtime `2026-10-23T21:45:33.8422472Z`, lists only Other/Dialogue/Parallax independent reviewers and no harness writer. This is this scope's admission evidence, not a global freeze claim. Manifest/request identify the same original actor; their older progress statuses do not supersede root's explicit current handoff.

## Complete changed-source and metadata coverage

Receipt (138 lines), request (46), all eight C# sources, asmdef (39), launcher (582), guide (181) and all 13 relevant metadata files were read completely. Truncated display intervals were reread. The eight C# sources total 2853 lines; full non-meta coverage including asmdef/launcher/guide/request/receipt is 3839 lines. Scoped Git status shows the implementation sources/launcher/guide as new/untracked against HEAD, so complete new-file reads cover the current changes. Dependency excerpts are explicitly bounded below; this is not a broad re-audit of unchanged modules. Final raw hashes match the admitted source identities.

Source prefix for the next table: `Assets/Neoxider/Tests/Integration/Network/`.

| Complete artifact | Lines | SHA256 |
|---|---:|---|
| Audit20261023TestPlayNetworkArtifacts.cs |174|`eed0a2230ab9c5ffa15fa0d565d92067ce4f0e334d9315ae115af79ac792ca9a`|
| Audit20261023TestPlayNetworkCounterInput.cs |460|`3e2994cd8cae5731f63c9e407e47491494b299d9c6979bf1be4cdb6eeb1214a7`|
| Audit20261023TestPlayNetworkMoneyPrecision.cs |382|`6d4438f6b0bbd04f2a074fc76253b2578d231f7fd8203f460c21461ceb5bdc7e`|
| Audit20261023TestPlayNetworkPeerDriver.cs |530|`f520b2a9f1b99b554f0e353b8442bb38629b052c196e3bf836b23006b163ca22`|
| Audit20261023TestPlayNetworkPlayerBuild.cs |114|`c6264174fc42c52140192a8a479f20d08d21dcc9f9057b68c8f119d84fc3443c`|
| Audit20261023TestPlayNetworkProtocol.cs |312|`50939ca42857faafc34ca8bbaaa77b3a50f88b6025b1dd5b9b75f4e97eb2273c`|
| Audit20261023TestPlayNetworkScenarios.cs |569|`40776c3eb113f1cda0394b13305aa07a48a1c025fcb8510dbfd90c710730405f`|
| Audit20261023TestPlayNetworkWorld.cs |312|`c01e8c332c6c31fbd5f506630327ed5b6ebdaf84a3703cf479946a971c6f59d7`|
| Neo.Tests.Network.Integration.asmdef |39|`b034cf494ab48cfe72575ccec4d39e60bb0aef86dcc56e65791a9acac5fd07d2`|
| scripts/network_playmode_matrix.ps1 |582|`191c430b12611b2e171c5b5e17bae6f351be054de1cf846341ef3bc3a3681ad9`|
| Assets/Neoxider/Docs/Testing/NetworkPlayMode.md |181|`e7a05076b9153656a7246ee4fc3c36f75e4d0b182e119cbf4db49117fc11c386`|

The eight C# metadata labels below abbreviate only the common `Audit20261023TestPlayNetwork` filename prefix. All metadata contents were fully read; no global GUID/import checker was executed.

| Metadata | GUID | SHA256 |
|---|---|---|
| Artifacts.cs.meta |`c8d194d15b0c442cab273387673427ff`|`75f50a8b3c2e760f62cf79dd608494ab060ed1ff11b5023fb60664aa2220fda0`|
| CounterInput.cs.meta |`554f5c54d2184bbbac4cdef76b2a02b6`|`9d7fab57ad6d2ea17a83b820fdab85fee14a8e3fdae3048a87b34aa9d2409219`|
| MoneyPrecision.cs.meta |`8c7874133099483dbaf523d44b059c92`|`42e21e68d762869f4d8b7a06484383f868f3af6fd50a4114cd4208cf69866041`|
| PeerDriver.cs.meta |`2a09a95943724db4b9e09e9cf116ea1f`|`d1dac504cd8db3de4430b806c2e6f86af02fd773f8f9e1a237f5af6cfc1fa34f`|
| PlayerBuild.cs.meta |`18acfa5e0e244cb7b738ad289b2e8858`|`66055834dbad2c8f70044a1ce86b78e590f97739ebc18053b9e712dea16b48e8`|
| Protocol.cs.meta |`82a4ed3323f8439d8d2fe95aa77232b2`|`080a3c7976ce407c83b7fc1b28168673f9d291bbee33a50ab8351e451433f2a6`|
| Scenarios.cs.meta |`5b3dafea0f504c898c6f6fde77511ef4`|`e5f7ebc64b998c4df273df549171c8f0bfb2eb4db27796b76ce7df588d8ca8a9`|
| World.cs.meta |`79b72b3af4d44673bdc7652c19f457f0`|`d8dcdc28933df5feb1ffd96117fca6d1037606081e5c6d974f0cfe1ef36c9301`|
| Neo.Tests.Network.Integration.asmdef.meta |`2107b0b421d642148396ec95375f5a9e`|`03041aee206b59c6855691f7980b64d22278ad57411d906c2753b4ff255f8d4c`|
| Assets/Neoxider/Tests/Integration.meta (root authored) |`afcd7e449cbe41f9b4dd36cba8a92bac`|`a71a3a5379a6abd14fd9c32be23fbcd6e57c8d830f0cc312fd5a50b015c844a1`|
| Assets/Neoxider/Tests/Integration/Network.meta (root authored) |`c5895afeed5d418cb39da0fd99496a33`|`7de1157021f6d1b7658795c8acf58f88b0dd5241f83764c299b3ed7a90082ced`|
| Assets/Neoxider/Docs/Testing/NetworkPlayMode.md.meta |`fcc152c5bd924e9a9c11fd1710885b5c`|`35a4e93fcf6dccf8237cca6593df8da67065bb3575b0ef1b60858f27b7ccb55b`|
| Assets/Neoxider/Docs/Testing.meta |`1200ea879a12472ca58ada64761e47ea`|`bfefba86f0b010c13ada80a183a8c6cc4cada97ebc19f5dfc73676a071b90ce9`|

## Confirmed remaining corrections

### NI26-01 / P2 — dedicated AllClients fixture waits for a server target change that never occurs

Original owner: Tests-Play-Network / `neo-audit-1023-extra-05`. Exact current sites: `Audit20261023TestPlayNetworkScenarios.cs`, `PhysicsCase:283–293`, `ObservePeers:541` and `CheckServer:528–534`; factory policy `Audit20261023TestPlayNetworkWorld.cs:246`. Runtime evidence: `Assets/Neoxider/Scripts/Network/Core/NetworkContextActionRelay.cs`, `DispatchOnServer:1198–1219`, especially `if (isHost)`1200 and dedicated branch1210–1213.

Trigger: server physically moves A's Rigidbody into the trigger at Scenarios288. The real server onTriggerEnter listener records A's identity and calls the public context relay (World260–264). Existing AllClients scope applies locally on a host; dedicated server only broadcasts. Expected runtime outcome: server BonusA=false, clients A/B each BonusA=true, sibling BonusB=false everywhere. Actual harness sets one shared BonusA=true, waits for the server's Bonus at291 and would then apply the same Context expectation in CheckServer. No current path activates the dedicated server child. The correct-runtime run therefore times out before completing the matrix. This is a source-confirmed oracle defect, not an observed Unity failure or a required runtime-scope change.

Smallest root-approved correction: bound the server wait on the actual physics event carrying A's identity, then use role-specific target expectations. Keep exactly one physics event with A's actual netId on server/A/B, both clients' A-target activation and every B-sibling negative control. Preserve real Rigidbody/collider execution, current AllClients/TriggerOnlyForLocalContext settings and public paths. Do not force server SetActive, change to host/ServerOnly, invoke a private callback or substitute guessed frames. Root independently read and accepted this correction during NI26.

### NI26-02 / P2 — reconnect oracle invents persistence for a transient context event

Same original harness owner. Exact sites: `Audit20261023TestPlayNetworkScenarios.cs`, `RunServer:50–51` after `Reconnect(false)`; `scripts/network_playmode_matrix.ps1`, `Assert-MatrixReceipts:484–488`; supporting `Audit20261023TestPlayNetworkWorld.cs`, `CreatePlayer:270–282`, and `Audit20261023TestPlayNetworkPeerDriver.cs`, `RunClient:403–409`.

Trigger: after the physical target case, B stops and reconnects. Mirror Shutdown calls DestroyAllClientObjects (NetworkClient1803–1814), including observed player replicas1698–1751; the registered unspawn handler destroys them. Fresh CreatePlayer initializes Bonus inactive at World281 and has no child-state synchronizer. No reconnect action replays the earlier context message. The runtime canonical guide already states target state is not auto-synced for late joiners (`Assets/Neoxider/Docs/Network/NetworkContextActionRelay.md:113`). Expected existing mechanics: dedicated server BonusA=false, retained A replica BonusA=true, fresh B replica BonusA=false, BonusB=false for all. Actual C# imposes shared true and launcher488 independently requires true on B. Correcting only NI26-01 leaves this subsequent mismatch.

Smallest root-approved correction: honest per-role context expectations in C# and PowerShell, retaining complete Counter12 / Selector3 active0,2,3 excluded1 / Money213 / RPG73,120,7 / property9 / reactive15 snapshots and all correlation/count oracles. Keep the original physical positive control where both clients were present. Root selected no implicit replay and no production policy rewrite. Document fresh inactive B in the owned guide/receipt; do not remove Context checks wholesale or introduce durable-state/session infrastructure.

### NI26-03 / P3 — runtime comment contradicts dedicated-server branch

Original runtime owner: Neo.Network / `neo-audit-1023-b04-05`. `Assets/Neoxider/Scripts/Network/Core/NetworkContextActionRelay.cs`, `DispatchOnServer:1199`, says dedicated server still applies server-side;1212 explicitly says do not apply and1213 only sends. Smallest repair: comment-only wording matching the current host/dedicated branches. Canonical guide73's host qualifier and113's late-join limitation already match behaviour; no additional guide defect is confirmed. Root accepted the bounded wording correction, with no runtime change or new test framework.

## Scoped source dispositions and meaningful controls

**Build/bootstrap/provenance — scoped source approval; native debt remains.** PlayerBuild31–100 validates run/SHA/new output/scene-folder absence, creates one owned empty additive scene, builds three StandaloneWindows64 players using Development/IncludeTestAssemblies/StrictMode, captures/restores company/product/version/active scene in finally, closes/deletes only its owned scene folder and emits build-receipt. Company NeoQA_Network_<runId> and separate server/client-a/client-b products are baked before each build. Actual build, restoration and cleanup bool outcomes still require observation. The operator-only helper does not independently enforce projectPath/frozen-manifest ownership; the outer execution recipe supplies those guards. SHA is a supplied correlation token, not a baked/self-computed source attestation; guide89 discloses this. Frozen manifest/build DLL identity receipts remain necessary.

Asmdef explicitly requires MIRROR and UNITY_INCLUDE_TESTS, has test-assembly metadata and autoReferenced=false; Build code is editor-guarded. Bootstrap105–124 requires exact -neoNetworkMatrix and matching actual WindowsPlayer identity before creating its driver. There are no ordinary NUnit-discovered cases in this assembly. Actual compile graph/symbols/references, Weaver serializers and inclusion/bootstrap in all three players must be observed; source guards or empty Test Runner discovery do not prove capability. Existing host-loopback evidence remains separate.

**Storage and cleanup — scoped source approval.** Artifacts strictly parses roles/run/port/deadline/full paths, validates the owned marker, refuses reuse and creates new JSONL. PersistentDataPath CLI asserts Unity's actual path, without inventing an override. Distinct baked products isolate implicit Unity persistence/HKCU. Driver136–168 records identity/PID/store locations before constructing FileSaveProvider with owned PersistenceRoot, installs it, round-trips/removes only a unique sentinel and checks provider identity at phases. Original Counter/Selector/Money/RPG persistence is disabled; added wallet/counters use unique owned keys. Current provider constructor Load and Save/fresh-file reload paths were traced.

Launcher17–47 checks Windows, distinct resolved exes, fresh output and prior absence of all three persistence directories/HKCU keys. Start-OwnedPeer70–85 starts hidden windows and retains returned Process objects. Port mutex/TCP probe512–522 never takes over a listener; an external bind race fails its own server. Finally543–575 terminates only retained running handles with bounded WaitForExit, records forced kill as cleanup debt, probes registry key/value names only, retains stores and releases its own lease. No recursive persistence deletion or foreign-process kill exists. Driver Finish479–526 disposes registries/diagnostics, stops its claimed session, destroys tracked objects, observes inactivity, restores captured per-process settings and requires clean cleanup/result/exit. Refusal, missing receipts or cleanup errors cannot pass. Actual handles/storage/exit behaviour remains unexecuted.

**Three peers and actual wire — scoped source approval apart from NI26-01/02.** Dedicated server forbids NetworkClient/local connection; both pure clients forbid NetworkServer/LocalConnectionToServer. Hello checks authenticated/Ready/player state, distinct actual PIDs, connection/epoch identity and assigns A real ownership. Neo manager HandshakeMode.Always makes AddPlayer readiness valid despite autoCreatePlayer=false (EnsureClientHandshake716–742). Installed public AddPlayerForConnection/RegisterSpawnHandler APIs match usage. Reliable ACKs fence ingress; distinct receiver and server remote-receiver snapshots prove the authored delivery assertion. Launcher correlates run/PID/step/epoch/case, requires all cases plus clean result/zero exit from all three and has no required-case skip-to-pass path.

Installed metadata reads identify Mirror 96.0.1 / Telepathy V1.9. Telepathy Port/max-message/lifecycle APIs match; transport uses real TCP/reliable callbacks. Mirror NetworkIdentity297–339 caches inactive child NetworkBehaviours, matching inactive factories and child PhysicsEvents3D. NetworkMessages150–179 decodes and emits diagnostics before user dispatch. MessageInfo.count means recipients, matching dedicated two-recipient outgoing versus one incoming RPC count. Cecil FullName/16-bit registration and enum-underlying serializers match Money's byte opcode and Counter's int operation. These are source/API confirmations, not compilation or real delivery.

**Counter concurrency/finite ingress — scoped source approval.** Both original public Add callers capture10; controlled outgoing public observer collections keep stale replicas while authoritative server must produce11,12 before trusted publications13,12 restore both receivers. Nine added sequential spawned fixtures have long test-only1000-second peer budgets, Float/Int modes, owned keys, Start/receive/flush barriers and observed despawns. Eight malformed groups cover all Set/Add/Multiply NaN and both infinities, both signs of finite Add/Multiply raw overflow and INT2 multiplied by float.MaxValue before normalization, using actual client public APIs.

Rejected current/stored bits, events, keys, file SHA and RPCs must remain unchanged/zero. Immediate finite controls must succeed exactly once in the measured same server frame/window; pre-handler counters expose preceding malformed effects. Finite A duplicate rejection and independent B acceptance prove active sender-isolated limiting. Actual per-process flush/fresh-provider reloads, original12/213 preservation and nonfinite telemetry kind/bit strings remain mandatory in C#/PowerShell. Current Counter474–513 rejects nonfinite operand/raw candidate before budget/int normalization; stale pending receipt text is historical. No direct-server/offline policy change is introduced by this harness.

**Money authority/precision — scoped source approval.** Current Money279–311 retains pure-client RequestedServerAuthority before local CanSpend;570–586 rejects positive zero-effective debit while allowing ordinary fractional/rounded debit;711–748 validates opcode/payload/current policy before budget. Original cases cover ServerOnly/unknown denial, actual malformed Command decoder plus same-channel valid Add1, None/OwnerOnly and final213. Additional persisted wallet retains ServerOnly through startup fences, invokes actual public remote TrySpend, records pending separately, rejects1 at1e8 without state/key/file/event/RPC/budget effects, then accepts5 with stored debit8/nominal RPC5. Stale client0 is explicitly restored before yield, disclosing that stale-replica repair is not tested. Fractional10.5→10.25, exact effects/recipient counts, file reloads and original wallet preservation are substantive controls. No local predicate/private body/host substitutes for these calls.

**Handler/polling/RPG/physics — scoped source approval within this consumer.** Current registries use per-registration ownership and manager session hooks. Exact Unicode payload/count101/103/105/107 probes cover older disposal, opposite order, current-client-empty via public UnregisterHandler bool, reconnect and server stop/start; no raw third-party replacement guarantee is implied. Unscaled polling timestamps unchanged read attempts and retries a valid null reactive field; explicit paused cadence plus property9/reactive15 receiver controls are meaningful. Deliberately different RPG client templates9/9/1 must receive server120/120/10, then live/late73/120/7; current adapter OnStartServer/SyncVar hook/OnStartClient apply paths were traced. This is not a native lifecycle run or a replacement for RPG owner QA.

Physics uses the actual server kinematic Rigidbody/collider, trigger and public event/context path; client colliders are disabled, and received collider identity comes from RPC. NI26-01/02 repair expectations without changing mechanics. Reconnect replaces B's connection in the same OS PID. Dedicated server restart means StopNetwork/StartAsServer in the same server PID, not a replacement server OS process. Owned atomic rendezvous/epoch barriers are substantive controls. Other transports/platforms/adverse network conditions and interaction query1/100/1000 are explicitly outside this harness.

## Read-only dependency identity ledger

The following are excerpt/API traces, not full unrelated-module re-audits or execution approvals. Full additional reads include TelepathyTransport (251 lines), both registry files (193/182), NetworkDiagnostics (63) and canonical NetworkContextActionRelay guide (124). Remaining relevant declaration/method spans are identified above.

| Dependency | SHA256 |
|---|---|
| Assets/Mirror/version.txt |`05c7090109e235707678119ae3c393aacac90ad56881aee4f0d07a996576a6c2`|
| Assets/Mirror/Core/Mirror.asmdef |`aa1511ce2af73deee76ce98e19d5177b75af3e4b06a19687e60bcccc54bbe1ee`|
| Assets/Mirror/Transports/Mirror.Transports.asmdef |`0f7010c766ee6c6a5ee23cef1c43a9502f4c619eecb6ac3083a9afe41d91ee77`|
| Assets/Mirror/Transports/Telepathy/Telepathy/VERSION |`dcf3b9e4c6bff396f9c2f654e06f928c9dafc54e375a92299df9f595fe8ca9a2`|
| Assets/Mirror/Transports/Telepathy/TelepathyTransport.cs |`add26242bfa115d00de4e732a92867dec264ec42fde4ebd17ad922da5e891d98`|
| Assets/Mirror/Core/NetworkIdentity.cs |`58d90af8113e249a3171185136b1166fd45ecd8d48c4541d0af96779a9f948ff`|
| Assets/Mirror/Core/NetworkClient.cs |`2405e6205609e3f564e7401f90bc6de9708be3d157fee393f41b913c3e7671dd`|
| Assets/Mirror/Core/NetworkServer.cs |`92f562ea5d05f35ae29bee1d8aac59870db79e3634d922b2d753bb9fa780bf0d`|
| Assets/Mirror/Core/NetworkConnectionToClient.cs |`0006dccf50894d8eb49cb610d7db8d84d2ca3615c9ed54bfb1eed94ff24832f5`|
| Assets/Mirror/Core/NetworkBehaviour.cs |`8ffa1e6bffc022dc593c2c07e293776c5e6f77c99e80a5fbfbcb7abfec5072f5`|
| Assets/Mirror/Core/NetworkMessages.cs |`4f9fd133cc7fd50d1abb994760cc1395c4519b9358c4d99bae6a71fa5853b367`|
| Assets/Mirror/Core/NetworkDiagnostics.cs |`9bacb7561c65178810ddf7efa9ef2e548f55fe85f062e2c3ebaebdbd22a6fb38`|
| Assets/Mirror/Core/RemoteCalls.cs |`37d37d1cf7ca9cec47c7388b59e9399ad97624cf135039c90a7e3c37a36b4835`|
| Assets/Mirror/Editor/Weaver/Processors/CommandProcessor.cs |`98b9d391d8fbf3133926d2041a77605f646d5a166b9febb70a0e00be4c4b88fb`|
| Assets/Mirror/Editor/Weaver/Processors/NetworkBehaviourProcessor.cs |`d7df7b0417acbb43d46017b2337aeb252df0a54520484c3e542942f1c8764b19`|
| Assets/Mirror/Editor/Weaver/Readers.cs |`f25b7d5e690ea8f3a0c91e62098c0934308788c99f43adc4256dc75e0bea3e47`|
| Assets/Mirror/Editor/Weaver/Writers.cs |`fd28e024f86da1cd1803f500a9679d088709ec73d2d459d2b19fa048296bcd5b`|
| Assets/Mirror/Editor/Weaver/Extensions.cs |`6dc068c5cb46d1bf92bc36af6cceea5300c1d1ab65f024144e6c05e377ec5ac8`|
| Assets/Neoxider/Scripts/Network/Core/NeoNetworkManager.cs |`b52435256132cb3fdb5d6fb912cdf594218e4be56aef2a23f78428fa84b441f2`|
| Assets/Neoxider/Scripts/Network/Core/NeoNetworkComponent.cs |`45012ee0df1c54ffe20ae7b2786c33daf1c0ad57f81a0bd36f62ec6bf6913e60`|
| Assets/Neoxider/Scripts/Network/Core/NeoNetworkState.cs |`1e86b88bb98d9b6a1cffe0fb026e2905f7fe412ed0d3638fb8cf5a17e20156c6`|
| Assets/Neoxider/Scripts/Network/Core/NetworkActionRelay.cs |`0355c64a68628ec5b4bababaab0401f49781a9138dd46e0396e48db4f8e68814`|
| Assets/Neoxider/Scripts/Network/Core/NetworkContextActionRelay.cs |`b5d84db5d20f10f31b317a02d66fd4deaca317f834ebeb6dffc1c647e9f7a315`|
| Assets/Neoxider/Scripts/Network/Realtime/NetClientHandlers.cs |`e410d7b569d9ff0b1b44b6c49aa2c901f6cf9ec5cc475515a2f06cfddd5a5ce1`|
| Assets/Neoxider/Scripts/Network/Realtime/NetServerHandlers.cs |`8a8315cfbac54333ab71178e346402d9cd98903be99424c95898a220753588d2`|
| Assets/Neoxider/Scripts/Network/Core/NetworkPropertySync.cs |`88f88fa118af456bac8ce6a97417cafa06973aea2944219297731a75a987f2bf`|
| Assets/Neoxider/Scripts/Network/Core/NetworkReactiveSync.cs |`f5bda48b0107ce2cfdb49a7cd39980956614b00eeb9b110a8518c277bf504d0c`|
| Assets/Neoxider/Scripts/Tools/Components/Counter.cs |`280faa4b24fb9babd4d51deefa67893d45c2052e41324d44c25ee1e648a7857f`|
| Assets/Neoxider/Scripts/Tools/Components/ModifyCounterByKey.cs |`e96bd89e60d0c4deba080ab130a61123b00cd484e33bcb0f6d5e0a2b84ffc051`|
| Assets/Neoxider/Scripts/Tools/View/Selector.cs |`8117a7719e9f3808a7c48e0afc1f2bfeeed4336c106957d0d133b618a6fc3d15`|
| Assets/Neoxider/Scripts/Shop/Money.cs |`ca141e9f3a54082f66130a6b6ce9877e1f91af136c2886ae630627585bacff0d`|
| Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents3D.cs |`e3f3852b0a9512e4f393219be542b28c0e29004953e74c20f65b78c1da9c488f`|
| Assets/Neoxider/Scripts/Save/SaveProvider.cs |`6195d7abe3096d9126f235b27aaec08c564e5b6851d8fd6a31621fc1ad449cb1`|
| Assets/Neoxider/Scripts/Save/Providers/FileSaveProvider.cs |`a677c26b4ec381ff7f68be318801754d3d36b6bd4b0c40a6622d89193f8e05ba`|
| Assets/Neoxider/Scripts/RpgNetwork/RpgCharacterNetworkAdapter.cs |`41d917bed933ae330e35bec822df5e103b60786667284c75ea12753479c660cb`|
| Assets/Neoxider/Docs/Network/NetworkContextActionRelay.md |`c698608d8dbc0f50e169dba9b5e36c6ccf92af311f7ab723df4598231fe18877`|

## Closure and execution debt

Return NI26-01/02 to the same Tests-Play-Network actor and NI26-03 to the same Neo.Network actor after root's full report read. Recheck their bounded changed sources/final receipts before freeze. Preserve the entire original and NI7/NI19 matrix. No host substitution, removed required payload/count oracle, ignored required case, automatic replay or runtime policy change is an acceptable closure; no additional framework is requested.

Later acceptance requires actual installed Unity 6.3/6.6 import/compile/Weaver and compiled graph receipts, isolated IncludeTestAssemblies player builds, observed baked identities/settings/temp-scene restoration, real three-PID Telepathy execution, actual receiver/status/pre-handler/persistence JSONL, zero exits and clean owned teardown. Failed build, absent bootstrap, missing/empty case receipt, timeout, forced kill or capability skip cannot become a pass. This matrix supplements nonempty full Edit/Play suites and separate native GUI/render/end-frame/input/performance/UPM/query acceptance. No execution or snapshot preparation is authorized by this source-only report.
