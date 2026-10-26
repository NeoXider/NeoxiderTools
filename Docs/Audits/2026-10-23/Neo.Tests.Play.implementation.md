# Neo.Tests.Play — implementation receipt

## P5-01 / P5-07 source correction — 2026-10-25 (CURRENT)

Status: **source-final; current compile/native acceptance UNEXECUTED**. SAME original
`neo-audit-1023-b06-04` session; requested gpt-6.1-sol/max, branch `demo`, target `11.0.0`.
Completed approved **P5-01** and **P5-07**. Earlier NI49-01, LEGACY-ABSENCE-SCENE-OWNERSHIP and
B26–B28 corrections remain. No additive public/runtime/test-helper API or owner dependency is required.

Read the full implementation contract, current exact implementation request and
`Local/Audits/assembly-2026-10-23/p5-source-correction-common.md`. Root's p5 frozen snapshot identity is
`956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`. Read the assigned raw p5 XML
case identities, complete messages/stacks/outputs on both Unity versions, routed triage rows, current
affected source/receipt and the consumed uGUI/Mirror registration/receive code. Prior receipt SHA-256
`294f282a991a4a47d67e10f0d79a08e1a9321f9129cfb4d25495e1e7edec379d` is preserved below as history.
Its earlier execution statuses and affected hashes describe earlier revisions, not this current patch.

### Actual p5 failures and bounded fixes

**P5-01:** the three existing Mult isolation cases have u63 IDs 1129/1130/1131 and u66 IDs
1990/1991/1992. Every assigned failure is the unhandled error
`Failed setting EventSystem.current to unknown EventSystem`, with cleanup at MultSceneNetworkTests130.
The consumed uGUI current getter returns the native registered-list head; its setter logs for an
unknown/null instance. OnEnable registers and OnDisable unregisters. This is a confirmed fixture cleanup
error, not evidence that the ownership/sentinel assertions failed or that gameplay EventSystem behavior
needs changing. No log expectation/whitelist was added.

Mult teardown now restores the exact borrowed original only if Unity still considers it live and
active/enabled, and only if it is not already current. Because that original was captured from current,
supported native OnEnable/OnDisable state establishes its registration; there is no private registry
access. Null, disabled and destroyed originals are left to native selection. No foreign EventSystem is
enabled, disabled, destroyed or registered by teardown. Exact scene/manager/DDOL/host/client cleanup,
pending-load behavior, provider lifetime, active-scene/cursor/global restoration and NI49 provenance
checks remain intact. No temporal root adoption or external-ancestor adoption was reintroduced.

**P5-07:** `EventChannel_NeverDeliversToTheHostItself` (u63 ID1494, u66 ID2355) timed out after five
seconds waiting for Received(99), while the failure diagnostics reported server active, client connected
and local player present. Public NetEventChannel documents and implements dropping every host-side
message, including a directly sent 99. This is an invalid positive fixture oracle, not a runtime
host-delivery defect. `NetEventChannel` and its public contract are unchanged.

The existing case still uses its genuine channel registration. It requires actual ready host/local
connection/player state; keeps Publish(Id1) returning zero, PublishTo(local host, default payload)
returning false and PublishedCount exactly one. The direct Id99 still traverses the serialized reliable
host path. Read-only Mirror diagnostics now require exact sent/decoded payload lists `[99]`, exactly
one sent and admitted envelope, positive decoded bytes and reliable channel IDs. Any leaked published
Id1/default payload also fails those exact lists. A distinct existing ReliableBarrier then proves the
ready receive queue progressed while the channel's actual handler remains installed. The case requires
zero channel callbacks and ReceivedCount zero, plus live spawned connection/player state after receipt.
Diagnostics are unsubscribed in finally. No channel handler is overwritten, bypassed or called through
reflection; no private UserCode/lifecycle delivery or new peer framework is involved. A pure-client
Received positive belongs to the existing separate real-peer path, not this host exclusion test.

### Regression source and owned documentation

Added three existing-file UnityTest cases under `UNITY_EDITOR && MIRROR`:

- `MultScene_CleanupRestoresRegisteredBorrowedEventSystem`: native AddComponent/OnEnable, supported
  current selection and getter identity first prove registration; after another live system is selected,
  yielded fixture cleanup restores the exact original without destroying either borrowed object.
- `MultScene_CleanupLeavesDisabledBorrowedEventSystemUnchanged`: disable only this regression's own
  captured system through native OnDisable, establish a registered replacement, yield fixture cleanup,
  and require the old system remains alive/disabled while the replacement stays current and enabled.
- `MultScene_CleanupDoesNotSelectDestroyedBorrowedEventSystem`: destroy only this regression's own
  captured component through the native API, establish a registered replacement, yield cleanup and
  require the destroyed original stays absent and the replacement remains current/untouched.

The three cases also require exact provider restoration. Their existing outer yielded teardown remains
available on body interruption; only their own test objects are released and a still-active ambient
selection is restored. No lifecycle method is invoked manually. The existing pending-load, post-Awake,
during-load borrowed-root, partial-setup and foreign-session cases/assertions are retained. The isolation
fixture now has 19 authored cases (18 UnityTest plus one Test in Editor+Mirror); discovery is unexecuted
for this new source. Existing method names and meta GUID `641b2be1f2a24d05b9c41960ca79ab46` are preserved.

`PlayModeTesting.md` documents supported EventSystem restoration and the host channel admission/
exclusion oracle in English/Russian, keeping separate pure-client/peer acceptance explicit. All six
Mirror-absent solo cases and prior payload/count/authority/RPC/native-physics/pointer oracles remain.
No runtime/public/serialized API, GUID, settings or default was changed. Deadlines remain the existing
five-second real-time readiness/admission/barrier contracts; no arbitrary extension or new Ignore.

### Current exact files and hashes

Only these four assets and this receipt were written in P5 correction. Each baseline was checked before
editing, with the live relocation reservation map read before every repository write.

| Path | Before P5 correction SHA-256 | Current SHA-256 |
| --- | --- | --- |
| `Assets/Neoxider/Tests/Play/Network/MultSceneNetworkTests.cs` | `01fbafc832649577aaba1c22b3f93d540bbc285c209363cb169d080a807a18b1` | `028a4c61e00ed40ac254fc9833383edaa7e29e6c227f7955f40883a2b1a54d2a` |
| `Assets/Neoxider/Tests/Play/Network/NeoNetworkManagerSessionTests.cs` | `7cee12dddcd3f3cf7f492a3cb9e6ccd2f7d8ddf7e6d84c9125626e2ea920730b` | `aa075fdd33709a2a8dcd876838307e849ad3697b94ed8f21f3d8f87de6eeafeb` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoTestsPlayNetworkIsolationTests.cs` | `8c6c1595188cfeaa0aa4d5296492fda6a9efeabb07130628bffb283fd857fd60` | `703290d698b2ecb9b9d1fc9248a4e220ccc9078f43cf95388429c8e71ee383b0` |
| `Assets/Neoxider/Docs/PlayModeTesting.md` | `85bef1d8fefbaee53cbc2bc3a7e6ab8aa6e8c295834d29e55ce7f345919db654` | `f00f5ce8be052a829516ab39cd2ff6c2145ddf5822970a9282c1e9edacf8760c` |

Read-only/unchanged: `NetworkTestHelper.cs` SHA-256
`36f2f45937e89d8b7edc4d866a1ffa2cbf07b90d9d43552627ea6e6480dd891c`; production
`Assets/Neoxider/Scripts/Network/Realtime/NetEventChannel.cs` SHA-256
`cf06e6cb239b1c6cfa89b5301dffe59720692810faf7943cb0dce30568129b13`.
No shared asmdef/reference/config, Fixtures extraction, original audit/plan, foreign source/docs/receipt,
shared tracker, historical native artifact or protected DocsEn.meta was edited.

### Historical native failure artifacts read — not rerun

| Assigned raw Play XML | SHA-256 |
| --- | --- |
| `Local/Audits/assembly-2026-10-23/unity/final/p5/u63/Receipts/21-full-play-graphics.xml` | `7ae7f99a5f648cf31a86a445bdd59761bfc3390d4bcf9d4d3e1371a6e86d681c` |
| `Local/Audits/assembly-2026-10-23/unity/final/p5/u66/Receipts/21-full-play-graphics.xml` | `73d18c2935644dd92c8fecd6cd1415c5d8f9443b9f0fd3a7afadf2ce9974cc69` |

Actual work comprised scoped source/vendor/request/triage reads, parsing the existing XML for all eight
assigned version-specific failures, manual review of the changed source/oracles, scoped Git diff output
and file-byte SHA bookkeeping. No compile, Unity/MCP, tests, product checker, process control,
install/service, child agent, commit or push was launched. Those old failures are not relabelled passed.

**Pending source/API dependency: none.** Stop writes here. Root must wait for ALL writers and establish
a new freeze before current compile/discovery/native acceptance. Required remaining evidence: rerun
the four actual failed cases on both Unity versions, all three new native-registration cases, and the
affected Mult partial/pending/during-load ownership and host-cleanup paths with exact provider/globals/
borrowed-root assertions. Confirm actual registered-original selection and null/deregistered-original
cleanup without errors; actual reliable Id99 decoding/admission with zero host channel delivery and
unchanged ready host/player positives; and central cross-owner/Mirror branch integration. Pure-client
receive/independent-peer evidence stays separate. All current authored acceptance is UNEXECUTED.

---

## Historical pre-P5 implementation record (preserved)

## NI49-01 source correction — 2026-10-25 (historical)

Status: **source-final for independent delta review; compile/native acceptance UNEXECUTED**.
SAME original `neo-audit-1023-b06-04` session, requested gpt-6.1-sol/max, branch `demo`, target `11.0.0`.
Implemented approved **NI49-01/P2**, continuing `LEGACY-ABSENCE-SCENE-OWNERSHIP`; earlier
`TEST-NETWORK-ISOLATION-WAITS` / `TEST-NETWORK-COVERAGE-BOUNDARIES` corrections remain.

Read the complete implementation contract, current exact request, all 79 lines of
`Docs/Audits/2026-10-23/NativeImplementationReview-49.md` (SHA-256 `b00964b8b1256ce73d83064bba476169508be4a4036035fd86001803fa59397d`),
current affected source/guide and prior receipt. The previous 322-line receipt was
`ceab74fae4a2d9f98cca89dd6775340b8e4d4aa98136dd173d465179c11817ca`; it is retained below as
historical context, including the earlier S3 exit=1 recovery. Its affected source hashes, 15-case count
and DDOL ownership description are superseded here. NI49's defect was confirmed by source trace;
neither its reproduction nor the correction is claimed executed.

### Correction and compatibility

`MultSceneNetworkTests` no longer snapshots global roots or adopts newly appearing DDOL roots.
Removed the entire temporal-difference scan from `OnOwnedSceneLoaded` and `_rootsBeforeLoad` state.
Ownership now comes only from exact loaded-scene hierarchies or the manager's existing scene/template/
known-owned reference anchor. Manager adoption descends from the exact manager GameObject, without
claiming an external ancestor. Once a manager reference has been acquired, a different singleton is
not adopted; the existing session cleanup still refuses a replacement/foreign session.

The actual scene/DDOL manager is still ledgered with `OwnManager` and destroyed through yielded owned
session cleanup. Exact additive-scene unload, pending-load teardown, provider lifetime and global
restoration are retained. No name or before/after root difference establishes ownership. There is no
new general ownership framework, helper API or runtime/serialized/public API change.

`PlayModeTesting.md` now explains exact ownership provenance in English/Russian, preserves during-load
borrowed roots explicitly and describes the new native-callback regression without asserting a pass.

### Added regression source — NOT RUN

Added `MultScene_DuringLoadBorrowedDdolRoot_IsNeverOwnedAndSurvivesCleanup` under the existing
`UNITY_EDITOR && MIRROR` guards in `Audit20261023NeoTestsPlayNetworkIsolationTests.cs`.

- Registers its exact additive Mult `sceneLoaded` observer before `LoadOwnedScene` registers the
  fixture callback. That real native event creates an unrelated DDOL root and child during loading;
  callback count/scene/root preconditions must be observed, not simulated by invoking a private callback.
- Requires the external root and child to be absent from `OwnedSceneObjects` before and after yielded
  cleanup, using CLR reference identity even for destroyed Unity wrappers. Their exact native scene,
  root parent, root world position, child parent and child local position must survive.
- Positively requires a live actual owned manager moved into the same native DDOL scene as an explicitly
  preserved pre-existing root, the manager's authored template in the exact loaded scene and the manager
  in the owned ledger. Starts the actual own host with DummyTransport and requires server/client/local
  player readiness before cleanup, then requires manager destruction, nonempty owned-ledger release,
  exact scene unload and no remaining manager/server/client.
- Preserves the pre-existing borrowed DDOL root with exact scene/parent/position, loaded prior active
  scene, exact provider/EventSystem/transport identity, cursor state, background flag, frame cap, VSync,
  paused time scale and random state. Those assertions run after fixture cleanup and before the outer
  regression cleanup restores its own original globals.
- `finally` unsubscribes that observer and destroys only its own callback-created root/child hierarchy.
  The existing outer yielded teardown remains registered on body failure and releases only this
  regression's pre-existing sentinel/transport. No foreign scene/provider/session is reset or deleted.

Both existing Mult interruption methods and all their oracles remain. The isolation file now has
16 authored cases (15 UnityTest plus one Test in Editor+Mirror), with the same meta GUID
`641b2be1f2a24d05b9c41960ca79ab46`. All six Mirror-absent solo cases, existing authority/payload/count/
RPC/native-physics/pointer assertions, NUnit names and settings are unchanged by NI49-01.

### Exact current delta and byte fingerprints

Only the following three assets and this receipt were written. The previous source pins are NI49's
reviewed baseline; current pins below supersede those three rows in the historical S3 table.

| Path | Before NI49-01 correction SHA-256 | Current SHA-256 |
| --- | --- | --- |
| `Assets/Neoxider/Tests/Play/Network/MultSceneNetworkTests.cs` | `451f41fd619bd75b3b927e1b19752930e2457309e902fa4cf7ab2cd9dba22156` | `01fbafc832649577aaba1c22b3f93d540bbc285c209363cb169d080a807a18b1` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoTestsPlayNetworkIsolationTests.cs` | `0b646e40254e6165fa9a673dbb39ebc25b4a6d8d6ed904a6f3cb97747a99b333` | `8c6c1595188cfeaa0aa4d5296492fda6a9efeabb07130628bffb283fd857fd60` |
| `Assets/Neoxider/Docs/PlayModeTesting.md` | `13c6100f6cde0caee295092d076e5c9b59160fe04b3aca64f774f3a29dd5a359` | `85bef1d8fefbaee53cbc2bc3a7e6ab8aa6e8c295834d29e55ce7f345919db654` |

`Assets/Neoxider/Tests/Play/Network/NetworkTestHelper.cs` was read but unchanged:
`36f2f45937e89d8b7edc4d866a1ffa2cbf07b90d9d43552627ea6e6480dd891c`. Its ownership/shutdown/global-restoration implementation is retained.
No helper extraction/Fixtures/shared reference/asmdef, metadata/GUID, runtime, settings, original
audit/plan, shared tracker/review contract or another owner's receipt was written. Root alone owns the
separate Play-to-Core reference correction. The live relocation map was checked before every write.

### Actual validation and remaining evidence

Performed bounded current source/request/review reads, manually reviewed ownership call paths and the
new regression preconditions/oracles, and recorded exact file-byte hashes. Source editing completed with
small cmd.exe/Python calls. These observations are not compilation, discovery, native execution or a
product-checker result. No Unity/MCP, compiler, tests/checker, install/service, process control, child
agent, commit/push or shared configuration action was run.

**Pending owner/API dependency: none.** Independent NI49 delta review is still required. Root must wait
for ALL writers and authorize a new global freeze before executing compile/discovery/native acceptance.
Required new native evidence is the genuine during-load observer ordering, foreign root/child survival,
actual manager DDOL ownership/destruction, real host cleanup and exact ledger/scene/provider/global
restoration, together with both retained Mult interruptions. Prior S3 debt remains: both Unity versions,
true Mirror absence/discovery of the six solo cases, existing network/authority/payload/count/physics
paths, pointer geometry/events and repeat execution past the original G31–G43 manager cascade. Root's
shared-reference integration and independent-peer harness require their own actual evidence. No authored
case is marked passed; runner automatic teardown after an uncaught failure remains unverified.

Writes stop with this receipt. The retained S3 record below describes the previous source revision.

---

## Historical S3 recovery record — retained from ceab74fa

## S3 recovery — 2026-10-25

Status: **source-final; current compile/native acceptance UNEXECUTED**. Original actor
`neo-audit-1023-b06-04`, same session, gpt-6.1-sol/max. Branch observed: `demo`; target `11.0.0`.
Completed source actions: `LEGACY-ABSENCE-SCENE-OWNERSHIP` and the preserved earlier
`TEST-NETWORK-ISOLATION-WAITS` / `TEST-NETWORK-COVERAGE-BOUNDARIES` (B26–B28).

Root reported that the preceding S3 attempt ended **exit=1**. The older source-final receipt below was
not treated as completion evidence. Retained the partial S3 edits, completed recovery source/docs and
regression-source edits, and reviewed their current contents. Small cmd.exe/Python read/edit and SHA
bookkeeping calls completed successfully in this recovery. No native job was resumed and no passing
result is inferred from this source-final status.

### Additive APIs available now

- Under `UNITY_EDITOR && MIRROR`, internal `MultSceneNetworkTests.BeginOwnedSceneLoad()` returns `void`;
  `LoadOwnedScene()` returns `IEnumerator`. Internal `OwnedScene : Scene`,
  `OwnedManager : NeoNetworkManager` and `OwnedSceneObjects : IEnumerable<GameObject>` expose the
  fixture's exact load/ownership ledger to its interruption regressions.
- Under `MIRROR`, internal `NetworkTestHelper.RpcObservation(NetworkIdentity identity,
  bool requireSpawned)` permits observing an unspawned local identity only when explicitly passed
  `false`. The existing one-argument constructor retains `requireSpawned: true`; network positives
  still require a nonzero netId. Sent/Received envelope counters and default semantics are unchanged.
- Existing `NetworkTestSession`, `Own`, `OwnManager`, yielded `Cleanup`, readiness and barrier APIs
  remain available as recorded in the historical section. No new public gameplay/serialized API.

### Actual S3 failures and source corrections

**True Mirror absence:** read the complete 242-line compiler-error artifact: 30 errors, 29 in the 11
legacy Play network files, plus one primary-owned Edit import. Guarded actual Mirror imports,
identities, integration setup/classes/attributes and Mirror-only tests with the existing `MIRROR`
convention. `MultSceneNetworkTests` additionally requires `UNITY_EDITOR`. No entire-assembly gate,
reference removal, settings change or test deletion.

Six existing solo/generic cases remain compiled without Mirror:

- `DispatchGlobalEvent_OfflineFallback_FiresLocally`: no host setup prerequisite; exact one void callback
  and exact int `-17`, float `42.5f`, string `Offline | payload` lists; destroys only its own dispatcher.
- `InteractiveObject_OfflineFallback_FiresLocally`: own inactive graph, initialized UnityEvent, public
  `InteractDown`, exactly one local callback, independent of host setup.
- `PhysicsEvents3D_NotNetworked_FiresLocally`: dedicated owned local PhysicsScene, real native simulation,
  exactly one enter and exit with the exact actor collider. With Mirror, retains zero RPC envelopes.
- `NetworkReactivePropertyBridge_SetFromNetwork_UpdatesAndNotifies`: unchanged `7.5f` state/payload and
  exactly one notification, independent of host setup.
- `Money_Personal_ChangesRemainLocal`: unchanged exact balance `100`; isolated provider and exact owned
  cleanup work in either compilation mode. Rejects a borrowed Money singleton before constructing one.
  Its Mirror-enabled path retains host/spawn readiness and the reliable barrier.
- `Everyone_AlwaysAllows`: preserved allow positive, additionally exactly one allowed and zero denied
  callbacks. Its Mirror-enabled path retains the original host/spawn precondition.

The first four bypass the integration setup when invoked directly. Paused dispatcher/interaction groups
run their offline positive after their own host teardown, preserving every previous scenario. Integration
dispatcher tests still require four sent/received RPC envelopes and exact payload/count callbacks.
The external Edit import is currently visibly `#if MIRROR`-guarded; read only, not edited by this actor.
Actual Mirror-absent compilation/discovery still requires the later global freeze.

**Full Play G31–G43:** the first Mult pickup case captured a null manager before actual scene load/Awake;
ownership was lost and 62 later cases encountered the live foreign-manager acquisition guard at helper
line 187. This failed fixture precondition is not proof of a Neo.Network runtime defect or the separate
destroyed-object CLR-null issue. The corrected fixture requests an additive load, waits for actual
sceneLoaded/Awake plus manager publication, and ledgers scene hierarchies and DDOL roots before readiness
assertions. A manager is anchored to the exact scene, its serialized scene-player template or an existing
owned ledger entry; a foreign replacement manager is not adopted by a root-difference scan.

Teardown can finish a pending owned load without repeating the body's manager-readiness assertion,
then yields owned-session cleanup and unloads only its exact scene. It restores the prior active scene,
EventSystem, cursor flags and exact save provider, keeping that provider installed through owned-object
destruction. Existing session/manager/identity foreign guards remain. Already loaded Mult is rejected;
borrowed scenes and pre-existing DDOL roots are never selected for deletion. Relay discovery is scoped
to the exact owned additive scene; the existing two-relay count and pickup/player-template assertions
remain. A load that never finishes reaches an explicit failed cleanup deadline, not an accepted result.

**Two pointer failures:** `HoverExit_FiresEvent` expected true but received false;
`HoverChanged_PassesCorrectBool` expected false but received true while the default center hover remained
valid. The fixture now owns the sole active main camera and an explicit Mouse-mode ray provider, restoring
only borrowed cameras it suspended. Those cursor-only targets sit behind the camera with the per-object
center override disabled; tests establish a real enter before exact exit/state/count assertions and
reject duplicate exits. No interaction default or runtime mechanics changed.

### Added regression source and retained oracles

Added in existing `Audit20261023NeoTestsPlayNetworkIsolationTests.cs`, under `UNITY_EDITOR && MIRROR`:

1. `MultScene_InterruptedPendingLoad_ReleasesItsSceneAndManager`: interrupt after requesting the load,
   yield teardown, require an actually created/ledgered manager before proving destruction, prove all
   ledgered objects and the owned scene released, preserve an inactive borrowed-scene sentinel and the
   exact prior save provider. Requires no remaining host/client/singleton.
2. `MultScene_InterruptedAfterAwake_ReleasesDdolAndPreservesBorrowedRoot`: first require loaded scene,
   live manager moved into DDOL and its scene-authored template; interrupt, yield teardown, require owned
   scene/manager/ledger release and survival of the borrowed DDOL sentinel at its exact original position.

The file now contains 15 cases (14 UnityTest plus one Test when Editor+Mirror are enabled); its prior
13 cases and meta GUID `641b2be1f2a24d05b9c41960ca79ab46` are retained. Injections are caught intentionally
and nested cleanup is yielded, not manually exhausted. They do not prove automatic runner cleanup after
an uncaught failing test until that native path is executed centrally.

Added in existing `Tools/InteractiveObjectPlayTests.cs`:

3. `CenterHover_PointerExitRetainsRayHoverUntilNativeAimLeaves`: real center-ray hit/hover positive in
   Both mode, pointer exit retains hover with zero exits, moving aim off the target through actual native
   frames produces exactly one enter/exit and states `[true, false]`.
4. `HoverDisabled_CenterHitStillClicksAndMouseGateRemainsEffective`: real center hit with hover disabled,
   one click, then disabling mouse interaction across a new frame suppresses the next click; hover
   enter/exit remain zero. Crossing the frame prevents click deduplication from masking a broken gate.

All previous substantive positive/negative/count/ownership/payload assertions remain. Public pointer
handlers test their explicit API contract; actual frames establish native hover behavior, not hardware
input. No ExecuteAlways, private Update/lifecycle invocation, reflection UserCode bypass, added blanket
Ignore, expected-error whitelist or tolerance/default relaxation was introduced. The pre-existing
missing-Mult-asset skip remains specific to that optional development scene.

### Current source paths and hashes

These 15 assets are the S3 correction set, including retained partial edits and the owned public guide.
SHA-256 values describe current bytes, not a compile/test/validation result. Original audit and plan
remain historical artifacts, unchanged. Earlier B26–B28 files not changed by S3 are listed below.

| Current path | SHA-256 |
| --- | --- |
| `Assets/Neoxider/Tests/Play/Network/MoneyNetworkTests.cs` | `89cc50928dde03be90eee4caa36f5178dd6a6852e4f7b6e52573c2a9225a34ad` |
| `Assets/Neoxider/Tests/Play/Network/NeoNetworkManagerScenePlayerTests.cs` | `c033b927d0531dc750b48681a9d2de0ef4d029c7c08f8ba8da6e977c8ee35e19` |
| `Assets/Neoxider/Tests/Play/Network/NetworkActionRelayTests.cs` | `5197a40c951827f4447068702ce80a0975f5e099f1a754d619285a48279799e8` |
| `Assets/Neoxider/Tests/Play/Network/NetworkContextActionRelayTests.cs` | `ae15bffafcc29b7ac80904528e7256ec3950570a40762c175ecddf6e3536f35d` |
| `Assets/Neoxider/Tests/Play/Network/NetworkOwnerFilterTests.cs` | `c141b629a5cc1c6314214ed7e49539200f05dfcb414541ee5547400a796fb209` |
| `Assets/Neoxider/Tests/Play/Network/NetworkSingletonTests.cs` | `9e287521afd3186959e26e19efdd83bbbf0ece879f3d9e7f7a29a0f3bc1a1f74` |
| `Assets/Neoxider/Tests/Play/Network/NetworkToolsTests.cs` | `71384735eb452afc85ac60f7f1536df38006f028a2c2a0c20fbd53b59f5de35c` |
| `Assets/Neoxider/Tests/Play/Network/NetworkEventDispatcherTests.cs` | `cf8694e1faa16dc8d3e7c26fa86b4fa00be14c807faa72f9a21cb125247520e3` |
| `Assets/Neoxider/Tests/Play/Network/InteractiveObjectNetworkTests.cs` | `1d977e94f8491715563a1531bd51f367cc3283f7f986081b6d73bf7a2bc5061a` |
| `Assets/Neoxider/Tests/Play/Network/PhysicsEventsNetworkTests.cs` | `ff59d84b60f989552bbaf1e287fa4ef41ec814415d53fddbf301e62ab848f5a9` |
| `Assets/Neoxider/Tests/Play/Network/MultSceneNetworkTests.cs` | `451f41fd619bd75b3b927e1b19752930e2457309e902fa4cf7ab2cd9dba22156` |
| `Assets/Neoxider/Tests/Play/Network/NetworkTestHelper.cs` | `36f2f45937e89d8b7edc4d866a1ffa2cbf07b90d9d43552627ea6e6480dd891c` |
| `Assets/Neoxider/Tests/Play/Audit20261023NeoTestsPlayNetworkIsolationTests.cs` | `0b646e40254e6165fa9a673dbb39ebc25b4a6d8d6ed904a6f3cb97747a99b333` |
| `Assets/Neoxider/Tests/Play/Tools/InteractiveObjectPlayTests.cs` | `d432eb2bfcc27871cbf662bb68cc3c4a617e95514e648cc8fd260eb142d464ac` |
| `Assets/Neoxider/Docs/PlayModeTesting.md` | `13c6100f6cde0caee295092d076e5c9b59160fe04b3aca64f774f3a29dd5a359` |

### Reviewed native artifacts (historical failed execution, not rerun)

Read the exact assigned XML cases and the entire compiler-error JSON; the Edit action-index lookup has
no `Neo.Tests.Play`/b06-04 correction entry. These inputs document S3 failures and the reservation release,
not current acceptance. Only SHA bookkeeping was performed on their bytes in this recovery.

| Artifact path | SHA-256 |
| --- | --- |
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml` | `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2` |
| `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml` | `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a` |
| `Local/Audits/assembly-2026-10-23/unity/final/s3m/u63/Receipts/compiler-errors.json` | `964b1f0e0f23e5d2dbcfc5c80bcd34a27ca42e8c78680b5bec1094c9cb98b5cd` |
| `Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json` | `df467fde346b0a1f7eca09a82b972f2e7532b7274758981969400628b1770e97` |
| `Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json` | `14b7a974c8a8a861a666b65d43971f42e4f97b21c01734daacd466f1e88fc3a7` |

### Validation, compatibility and handoff

Actual work: read current owned source/relevant runtime signatures; manually reviewed guards, owned
cleanup ordering, mixed-fixture branches, regression oracles and documentation; inspected scoped Git
diffs/status and recorded exact byte hashes. Branch read returned `demo`. New guide/regression/receipt
files remain untracked with the rest of the existing audit work; nothing was staged or committed.
These are source-review/bookkeeping observations, not compiler, NUnit or product-checker passes.

Existing NUnit method names, serialized/public gameplay APIs and asset GUIDs are preserved. No runtime,
shared asmdef/reference/configuration/settings, helper-extraction/Fixtures file or foreign receipt edit.
The live relocation map was read before every repository write; primary actor b03-04 is `source-final`,
original Play callers are released, and Fixtures/shared references remain exclusively primary-owned.
No deferred reserved Play caller correction remains for this actor. Optional large frameworks and
separate-peer harness integration remain proposals/root-owned work, respectively.

**Pending source/API/owner dependency: none.** Root must wait until every source writer finishes, then
establish a new freeze and execute current acceptance. Still UNVERIFIED:

- True Mirror-absent import/compilation and nonempty discovery of all six generic/solo cases; current
  shared asmdef still lists Mirror/adapter references and this actor did not change them.
- Mirror-enabled compilation, existing discovery and every preserved payload/count/negative oracle,
  paused waits, native physics callbacks and RPC diagnostics in the modified legacy fixtures.
- Both Mult cases plus both interruption regressions, actual Awake/DDOL timing, exact native object/
  scene release, provider/cursor/EventSystem/global restoration and repeat execution through the later
  G31–G43 groups without a leaked live manager. A never-completing native load and the runner's uncaught
  failure teardown need actual evidence; source reasoning is not that evidence.
- Both corrected pointer exits, center-hover persistence/exit and hover-off click/mouse gate in native
  frames. Hardware/backend input and actual separate-peer admission require their separate evidence.
- Root's integration acceptance on the requested Unity versions, including the external primary-owned
  Edit import and extracted fixture assemblies/shared references.

No Unity/MCP, compiler, tests, product checker, service/install, commit/push or child actor was launched.
Writes stop with this receipt. The historical section below records pre-S3 state; its Single-load,
13-case and hard-dependency descriptions are superseded by this current recovery section.

---

## Historical 2026-10-24 receipt — B26–B28 (retained)

Status: **source-final, UNEXECUTED acceptance**, 2026-10-24. Branch `demo`, target `11.0.0`.
Original actor `neo-audit-1023-b06-04`; approved actions `TEST-NETWORK-ISOLATION-WAITS` and
`TEST-NETWORK-COVERAGE-BOUNDARIES` (B26–B28). Historical audit/plan remain unchanged.

## Additive test APIs available to dependents

In existing `Assets/Neoxider/Tests/Play/Network/NetworkTestHelper.cs`, under `MIRROR`:

- Internal `NetworkTestSession()`: rejects active sessions, existing manager/scene identities before mutation;
  captures exact transport, background, frame cap, VSync, time scale and random state.
- `T Own<T>(T value) where T : UnityEngine.Object`; `T OwnManager<T>(T manager) where T : NeoNetworkManager`.
- `IEnumerator Cleanup()`: only stops the owned manager, yields real shutdown, destroys explicit owned
  objects, restores captured globals. Idempotent. Must be yielded by UnityTearDown; never exhaust manually.
- `NetworkTestHelper.WaitForSpawn(NetworkIdentity)` and `ReliableBarrier(string description = ...)`:
  observed host/client/observer readiness and reliable serialized control with real-time deadline.
- Internal disposable `NetworkTestHelper.RpcObservation(NetworkIdentity)` with `Sent`/`Received` envelope counts.

Existing helper signatures remain. No runtime public/serialized API or asmdef change. These small fixture
internals implement the approved corrections; the general support framework/assembly split stays a proposal.

No compiler, Unity, runner or product checker has been launched. Authored acceptance is UNEXECUTED.

## Completed actions

**TEST-NETWORK-ISOLATION-WAITS / B27–B28:** replaced all 26 scaled sleeps in the seven legacy
fixtures: Money (5), ActionRelay (6), Dispatcher (2), Singleton (1), Tools (9), Physics (1),
InteractiveObject (2). State/event predicates use the existing assertion-producing real-time helper;
reliable serialized controls precede final count/negative assertions. Barrier messages carry a fresh
sequence, so a previous control cannot satisfy a later wait. Original payload/balance/count assertions
remain. Playerless handshake cases now observe the server-ready state and a barrier, then demonstrate
explicit player addition. Excluded event-channel publication is followed by an exact same-channel
positive payload, with the complete received list checked.

All 14 legacy network fixtures now capture globals before acquisition and yield `NetworkTestSession`
cleanup. Startup rejects an existing session, active/inactive manager and scene identities. Cleanup
stops its exact manager, bounds graceful shutdown, releases its remaining session on failure, destroys
only explicitly owned objects, and restores exact transport/background/frame/VSync/time/random values.
Replacement-session detection prevents global shutdown/reset/restoration from claiming another session.
Money retains its isolated provider through destruction and restores the exact previous provider last.
Bootstrap retains its own StopNetwork path and presentation cleanup before shared cleanup; its prior
frame/background/VSync restoration is consolidated with the missing transport restoration.

The unspawned scene-player copy is owned immediately after creation, before assertions; its pickup and
the duplicate bootstrap are owned at allocation. Context-relay copies and body-created legacy roots
are tracked too. Mult scene cleanup no longer stops/destroys discovered managers: it captures the exact
manager from its own isolated load and owns only that loaded scene's roots. The scene still uses Single.
The earlier original-player-template cleanup is preserved. Removed the redundant global singleton
DestroyInstance call; the inspected NetworkSingleton.OnDestroy already clears only its own instance.

**TEST-NETWORK-COVERAGE-BOUNDARIES / B26:** both legacy named networked dispatcher tests now set
`isNetworked = true`, build identity/behaviours while inactive, await host/client spawn and observer,
and require exactly four outgoing and received RPC envelopes. Void count is exactly one; int, float
and string lists contain exactly their sent payload. Offline fallback remains a separate case.
This reuses the supplemental four-RPC/barrier approach without editing supplemental sources.

Invalid relay indices/names now target a configured valid channel and prove unchanged void/float/string
state after a barrier, followed by exact positive controls. OthersOnly and named-channel exclusions
also have positive controls. Physics legacy cases now move a Rigidbody through a real trigger in an
owned local PhysicsScene, simulate native steps, and require exactly one enter and one exit with the
correct collider. Networked mode observes RPC envelopes; local mode requires zero envelopes. Private
OnTriggerEnter invocation is removed. Other touched component graphs are completed while inactive,
including Money, which no longer needs private InitializeNetworkBehaviours reflection.

## Exact files

`N = Assets/Neoxider/Tests/Play/Network/`. Changed existing owned files:

- `N/NetworkTestHelper.cs` — additive test APIs above, sequenced control message and exact cleanup.
- `N/NetworkEventDispatcherTests.cs` — explicit networked four-envelope assertions and owned roots.
- `N/NetworkActionRelayTests.cs` — complete inactive graphs, barriers, exact payload/count/negative controls.
- `N/NetworkToolsTests.cs`, `N/MoneyNetworkTests.cs`, `N/NetworkSingletonTests.cs`,
  `N/InteractiveObjectNetworkTests.cs` — observed state, reliable barriers, owned setup/body and restoration.
- `N/PhysicsEventsNetworkTests.cs` — native enter/exit, isolated physics scene and bounded scene unload.
- `N/NeoNetworkBootstrapTests.cs`, `N/NeoNetworkManagerScenePlayerTests.cs`,
  `N/NetworkContextActionRelayTests.cs` — owned duplicate/copies/pickup and exact shared cleanup.
- `N/NeoNetworkManagerSessionTests.cs` — shared cleanup, playerless/host-exclusion controls.
- `N/MultSceneNetworkTests.cs` — foreign-session refusal and exact loaded scene/manager ownership.
- `N/RpgCharacterNetworkAdapterTests.cs` — same host/global acquisition and cleanup contract;
  existing host damage and snapshot replay assertions preserved.

Outside `N`, under `Assets/Neoxider/Tests/Play/`:

- Changed `Audit20261023NeoTestsPlayNetworkWaitTests.cs`: interrupted nested fixture cleanup is a real
  yielded UnityTearDown; removed the synchronous IEnumerator exhaustion from finally.
- Added `Audit20261023NeoTestsPlayNetworkIsolationTests.cs` and `.cs.meta`
  (GUID `641b2be1f2a24d05b9c41960ca79ab46`): 13 additional cases.
- Updated `Assets/Neoxider/Docs/PlayModeTesting.md`: C# helper use, acquisition/cleanup order, paused
  waits, isolated-scene requirements, and accurate host/codec/clone/native-physics/peer boundaries,
  with a Russian summary. No shared index edit.
- This receipt is the only audit-directory artifact changed during implementation. Original audit,
  plan, request, contracts, shared trackers and other actors' sources/receipts remain untouched.

## Authored observable acceptance — NOT RUN

The new isolation fixture has seven paused groups invoking every formerly sleeping legacy path with
fresh setup/teardown per scenario; this includes exact dispatcher envelopes, relay payload/count/control,
Money balances, Tools state/events, singleton value, interaction events and native physics enter/exit.
Six further cases cover partial setup failure before manager creation plus repeated cleanup; rejection
of an inactive borrowed manager; exact globals/transport/random and original-template release across
two hosts; rejection of an active foreign session with a working dispatcher positive control; and
interrupted pickup/copy and duplicate-bootstrap allocation checkpoints preserving an unrelated object.

Failure injection is caught so regressions can complete, and suspended bodies are disposed before
yielded cleanup. These sources check interruption/ownership; they are not an executed demonstration
of the Unity runner's automatic cleanup after an uncaught failing test. That native failure-path
evidence remains part of central acceptance. Existing 17 audit cases are preserved, not relabelled passed.

## Source review, compatibility and dependencies

Performed source reads and reviewed the changed legacy/helper diffs and authored sources; enumerated
helper callers before extending it. A scoped `rg` over the seven assigned files finds no remaining
WaitForSeconds. Source enumeration shows all 14 legacy fixtures adopt NetworkTestSession. New regression
source contains 12 UnityTest methods plus one Test; GUID reference enumeration finds one occurrence
under Assets. These are source/bookkeeping observations, not compilation, test or product-validation passes.

No runtime/serialized gameplay API, existing NUnit name or asset GUID was changed. The only public
addition is a test-only NetworkMessage payload; support classes remain internal to Neo.Tests.Play.
No asmdef/config/package/settings edit was made here; coherent concurrent configuration edits were preserved.
Optional common support framework, save-provider extraction and optional-network assembly split remain
proposals. Scene smoke Single replacement and backend-specific device coverage retain their documented limits.

**Pending owner API dependency: none.** This implementation consumes the existing Neo.Network manager,
NetClientHandlers and Mirror diagnostics APIs. Neo.Network's concurrent authority/handler/cadence changes
must be integrated before execution; no new member is awaited. Supplemental Tests-Play-Network owns the
separate-peer harness and its prefixed sources/docs; host loopback here does not establish remote admission.

Root must wait for every source writer, then compile/import and run these sources on Unity 6.3 and 6.6,
verify nonempty discovery and actual paused/failure/global-restoration/physics outcomes, and record MIRROR
on/off dependency/discovery results separately. The current test assembly's hard Mirror references remain;
this receipt makes no Mirror-free compile claim. Native deferred destruction, diagnostics envelope counts,
local PhysicsScene callbacks, scene import and automatic failure cleanup are UNVERIFIED until that run.
No compiler, Unity/MCP, runner, product checker, service, installer, commit/push or child agent was used.
