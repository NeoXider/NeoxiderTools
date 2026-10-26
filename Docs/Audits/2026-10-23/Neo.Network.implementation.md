# Neo.Network — implementation receipt

Status: **source-final for Neo.Network-owned runtime, documentation and regression source**.
Branch/target: `demo` / package `11.0.0`. Execution verification remains deferred until the coordinator
freezes all writers. Neo.Editor integration and its execution evidence remain separate handoffs below;
that owner's current source was not re-reviewed in the NI7 continuation.

This continues the original [audit](Neo.Network.md), [plan](Neo.Network.plan.md) and
[implementation request](Neo.Network.implementation-request.md). Earlier audit patches are preserved;
the historical audit and plan were not rewritten. The proposed typed replication/binding framework
remains a proposal, not an implemented feature or a prerequisite for these bounded fixes.

## Completed assigned changes

Runtime paths below are relative to `Assets/Neoxider/Scripts/Network/`.

| Request | Source and resulting behavior |
|---|---|
| `NETWORK-UNKNOWN-POLICY` | `Core/NeoNetworkState.cs`: unknown policy integers return false. This occurs outside the named-policy trusted-server/host allowances. `None=0`, `OwnerOnly=1`, `ServerOnly=2` and their existing named-policy truth tables are preserved. |
| `NETWORK-REFLECTION-VALIDATION` | New `Core/NetworkSyncMember.cs`, used by `Core/NetworkPropertySync.cs` and `Core/NetworkReactiveSync.cs`, validates metadata before accepted command mutation. Scalars require the exact configured type, writable fields, and non-indexed properties with getter and setter. Reactive members require the selected reactive type and a non-indexed getter; a readonly/get-only reference to a mutable reactive instance remains valid. Invalid metadata is cached and produces one gated diagnostic. Scalar numeric conversion/rounding and string coercion paths were removed. Accessor exceptions disable that binding with a gated diagnostic. |
| `NETWORK-LATE-REACTIVE` | `Core/NetworkReactiveSync.cs` caches validated metadata separately from the instance. A temporarily null instance is retried on the next eligible poll or apply. A later accepted changed value updates the newly initialized instance and its listeners. No automatic replay/seed policy was introduced. |
| `NETWORK-UNSCALED-CLOCK` | `Core/NeoNetworkComponent.cs` uses `Time.unscaledTimeAsDouble` and double timestamps for shared and per-remote command windows. Both sync adapters use that clock for polling; `Lobby/NeoNetworkDiscovery.cs` uses it for QuickPlay timeout and discovery refresh. Pausing gameplay no longer freezes these windows. Public interval types remain float. |
| `NETWORK-POLL-CADENCE` | Both sync adapters timestamp each eligible capture attempt before resolution/read, including unchanged values and unavailable bindings/instances. Networking, direction and server/owner eligibility still precede the timestamp. Only dirty values are sent. |
| `NETWORK-RELAY-OPTOUT` | `Core/NetworkActionRelay.cs`: all three `CmdTrigger` bodies reject `!isNetworked` before spending the peer budget. Enabled commands retain independent remote-sender windows and local public action behavior. |
| `NETWORK-HANDLER-OWNERSHIP` | `Realtime/NetClientHandlers.cs`, `Realtime/NetServerHandlers.cs`: each Neo registration receives a per-type generation token. Last registration wins; an older set's disposal cannot unregister the newer Neo receiver. Disposing the newer set removes it without restoring predecessors. Explicit unregister/register can reclaim a type. Reconnect registration obtains new tokens. Public signatures and authentication defaults are preserved. |
| `NETWORK-CONTRACTS-DOC` | `Assets/Neoxider/Docs/Network/README.md` links the canonical `Contracts.md`. The explanation preserves configuration semantics and requires every optional-network implementation on a scene object to opt out before offline recovery. `Contracts.md` itself remains with its separate owner. |
| `EDITOR-LOBBY-PERSISTENCE`, owned runtime portion | New `Lobby/NeoLobbyPresenter.cs` exposes typed address/Host/Join/Ready/Leave methods and serializable output events for persistent UI wiring. It resolves the current local `NeoLobbyPlayer` at click time, polls changed view outputs every 0.1 unscaled seconds, and has no UI-package dependency. Editor scene replacement, saved prefab assets and persistent wiring remain Neo.Editor-owned. |

Existing audit fixes, including dedicated-server application of accepted sync commands, are retained.
Binding validation precedes the SyncVar assignment and command limiter for invalid metadata; failed
property writes do not emit a successful `onValueChanged` notification.

## NI7 continuation — 2026-10-24

Root accepted `NI7-01/P1` and `NI7-03/P2` in
[NativeImplementationReview-7.md](NativeImplementationReview-7.md). Both bounded source corrections are
complete; the earlier implementation remains intact.

- **NI7-01:** removed only `[NonParallelizable]` from
  `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkHandlerOwnershipTests.cs`. All six meaningful
  client/server payload cases and their cleanup are preserved. The installed Unity NUnit configuration
  lacks that attribute. Execution relies on the coordinator's isolated/exclusive runner contract and
  existing cleanup; no replacement annotation, package or serial-test framework was added.
- **NI7-03:** `Assets/Neoxider/Scripts/Network/Lobby/NeoLobbyPresenter.cs` now increments a private
  publication revision only after detecting an actually changed count/ready/status snapshot. It commits
  the snapshot before callbacks and checks the captured revision after count, count text and ready events.
  A nested changed snapshot cancels the obsolete publication's remaining stages; an unchanged nested
  `RefreshView()` returns without incrementing the revision and leaves ordinary pending stages intact.
  Ordinary order remains count → count text → ready → status. The fence does not alter listeners inside
  one UnityEvent, add deferred replay, or change public/serialized APIs or editor wiring.
- Added **eight unexecuted lobby regression cases** to
  `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoNetworkLobbyTests.cs`: three public `JoinLobby()`
  reentry transcripts from count/countText/ready, one ordinary four-stage order control, and unchanged
  nested `RefreshView()` controls at each of count/countText/ready/status. They use the existing isolated
  DummyTransport/manager fixture and public presenter methods/events, with no reflected lifecycle call.
  The changed-reentry cases assert the actual connecting state/address and exact transcript, then verify
  unchanged polling emits nothing further. They do not constitute remote admission or saved UI evidence.
- `Assets/Neoxider/Docs/Network/Lobby.md` documents the changed-snapshot fence and unchanged positive
  behavior. This receipt is the fifth changed file in the continuation. No Editor, Contracts, shared
  configuration, tracker or other owner's source was changed.

Actual NI7 work: full implementation contract/request reads, relevant review/source reads, scoped diffs
and manual tracing of the new output-stage guards and regression transcripts. No execution was performed;
the eight new cases and all six preserved handler cases still need later compilation/discovery/execution
after the global source freeze. The review's earlier artifact hashes and authored-case counts describe
its earlier snapshot, not these revised files or passing results.

## Regression source supplied — not executed

Owned files changed in this implementation phase:

- `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoNetworkCommandTests.cs`: invalid scalar indexer,
  readonly field/property, setter-only property, wrong string/numeric type; unchanged target, notification
  count and serialized outbound snapshot; invalid reactive metadata; null-then-initialized reactive instance;
  unchanged/unready polling cadence; all three relay opt-outs preserving the enabled request's budget;
  unknown policy values `-1`, `3`, `int.MaxValue` for remote/trusted callers; named-policy host behavior;
  limiter recovery while `timeScale == 0` with time scale restored in `finally`.
- `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoNetworkLobbyTests.cs`: presenter's typed Join/Leave,
  address and output paths, changed/unchanged reentry transcripts and ordinary output order; QuickPlay
  timeout during pause. The discovery fixture bypasses UDP discovery,
  so it does not claim socket/port evidence or saved-scene persistence.
- New `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkHandlerOwnershipTests.cs`: both client
  and server sets, old/new disposal ordering, no implicit restoration, explicit re-registration and shutdown
  generations. Assertions deliver a serialized payload through the actual installed Mirror handler delegate,
  rather than checking only `IsRegistered`/dictionary membership.

Original owned `Audit20261023NeoNetworkRealtimeTests.cs` and `Audit20261023NeoNetworkSceneTests.cs`
remain intact. The separate Contracts fixture and supplemental network fixtures are not claimed as my work.
Woven receive-delegate invocation exercises handler bodies but bypasses Mirror connection admission;
the host truth-table case is not evidence of a remote client's authorization behavior.

## Public documentation and compatibility

Updated this phase under `Assets/Neoxider/Docs/Network/`: `README.md`, `NetworkPropertySync.md`,
`NetworkReactiveSync.md`, `NetworkActionRelay.md`, `NeoNetworkComponent.md`, `NeoNetworkDiscovery.md`,
`Realtime/NetMessaging.md`, `Lobby.md`, `Multiplayer_Guide.md`. Previous audit doc corrections remain.

Existing serialized target/member names, public methods, authority enum values, Mirror wire payload types
and valid bindings remain. Exact scalar-type validation intentionally rejects previously coerced bindings
in 11.0.0; migration is to the matching payload/member type or an explicit typed gameplay adapter.
The helper is internal; `NeoLobbyPresenter` is an additive public scene adapter. Private timestamps became
double. No scene/prefab migrations or sample-layout changes were performed.

Handler ownership guarantees cover Neo handler sets. Direct external `NetworkClient/NetworkServer`
`RegisterHandler`, `ReplaceHandler` or `UnregisterHandler` calls for the same type bypass the tokens and
remain an explicitly documented compatibility limitation. `IsRegistered` is lifecycle bookkeeping,
not proof that every type is still owned by that set. No restoration stack was added.

New Unity script metadata is present beside all three new C# files:

| File | GUID |
|---|---|
| `Scripts/Network/Core/NetworkSyncMember.cs.meta` | `4ec618121cd940f8b1430009b8e3efdc` |
| `Scripts/Network/Lobby/NeoLobbyPresenter.cs.meta` | `745e612bfab145ccbdd8d1370f1ff44f` |
| `Tests/Edit/Network/Audit20261023NeoNetworkHandlerOwnershipTests.cs.meta` | `92d6410a2a124ac4b088ef2588a0c21d` |

Paths in this table are relative to `Assets/Neoxider/`; all parent directories already existed.

## Precise cross-owner handoff

- **Neo.Editor / `EDITOR-LOBBY-PERSISTENCE`:**
  `Assets/Neoxider/Editor/Network/LobbyDemoSceneCreator.cs` was not edited. During the initial source read
  it still used transient callbacks, lacked the save-cancel guard and used
  a scene room object as a prefab. This is historical dependency context, not a current-source assertion.
  The editor owner retains the contract to guard before replacing loaded scenes; create/save an actual
  `NeoLobbyPlayer` + `NetworkIdentity` room prefab and a separate gameplay prefab with `NetworkIdentity`
  (`NeoNetworkPlayer` optional); assign `roomPlayerPrefab`, `playerPrefab`, RoomScene and GameplayScene;
  and persist address/Host/Join/Ready/Leave and label wiring. Runtime contract is available now:
  `NeoLobbyPresenter.Manager`, `SetAddress(string)`, `HostLobby()`, `JoinLobby()`, `ToggleReady()`,
  `SetReady(bool)`, `LeaveLobby()`, `RefreshView()`; events `OnStatusChanged<string>`,
  `OnPlayerCountTextChanged<string>`, `OnPlayerCountChanged<int>`, `OnReadyChanged<bool>`.
  Serialized event fields are `_onStatusChanged`, `_onPlayerCountTextChanged`, `_onPlayerCountChanged`,
  `_onReadyChanged`; manager/address fields are `_manager`, `_address`. Use persistent listener targets,
  not captured lambdas. Required later acceptance: cancel preserves the loaded scene, save/reopen preserves
  every listener and prefab reference, and two peers can join/ready/transition with separate room/game players.
- **Neo.Shop / `SHOP-MONEY-CLIENT-OPERATIONS`:** no Money source was edited. Root's decided typed
  `ClientMutationAuthority` / serialized `_clientMutationAuthority` with ServerOnly default remains with
  that owner. Validate known operations 0–6, payload and policy, then `IsAuthorized`, before budget/mutation;
  reject refund opcode 7. The shared authorization helper now rejects unknown enum values without altering
  named-policy behavior. Verify nonowner/owner policies, rejection side effects and outbound state separately.
- **Neo.Tools.Components / `COUNTER-REMOTE-OPERATIONS`:**
  `Assets/Neoxider/Scripts/Tools/Components/Counter.cs` was not edited. That owner retains operation
  dispatch/authorization/payload validation; the shared sender-aware unscaled limiter is available.
  Rejected operations must preserve value, events, replication and subsequent valid-request budget.
- **Tests-Play-Network / coordinator:** separate-peer admission and accepted/rejected replicated outcomes
  remain with the supplemental harness owner. The shared Play asmdef `Mirror.Components` reference was
  already centrally completed per the request; no duplicate configuration edit or dependency request was made.

These handoffs identify integration work outside this assembly; they are not claims that another actor's
concurrent changes are absent or complete. No shared tracker or other owner's source was changed here.

## Actual validation and remaining verification debt

Performed in this implementation phase: manual review of the changed source/diffs and relevant installed
Mirror APIs, ownership-scoped path/reference enumeration, and readback of the three new script metadata
GUIDs. Source inspection confirms the guard ordering, exact-type resolver reuse, per-registration tokens
and unscaled timestamp locations described above. This is source evidence, not compiler/runtime evidence.

**No Unity, compiler, test runner, product checker/validator or install/service process was launched.**
No implementation-phase passing test result is claimed. Earlier audit static-check results are historical
and do not validate these later patches. No commits, push, children, shared asmdef/config edits or legacy
fixture edits were performed by this actor.

After every writer is frozen, the coordinator still owes nonempty discovery/execution of the owned
fixtures and supplemental real-peer suite, Mirror/no-Mirror compilation and both intended Unity version
branches (6.3/6.6), weaving/host/dedicated/late-join behavior, saved editor scene/prefab persistence and
real LAN URI/port behavior. Reflection preservation on IL2CPP/WebGL and production transport behavior
remain unverified platform risks; the bounded implementation does not claim to resolve stripping or add
a replication hydration/seed framework. Final repository push remains the coordinator's later action
after complete verification.

## NI26-03 / P3 — SOURCE-FINAL comment-only follow-up

Completed root's accepted wording correction from
[NativeImplementationReview-26.md](NativeImplementationReview-26.md). The only runtime-file edit is
the comment at `Assets/Neoxider/Scripts/Network/Core/NetworkContextActionRelay.cs:1199` in
`DispatchOnServer`: it now states that a dedicated server only relays to clients and has no local client
view to apply. The preceding comment still explains the host's single local client-view application.
This matches the existing host `ApplyResolved`/remote broadcast and dedicated relay-only branches.

Actual work: read the full current implementation contract/request, relevant NI26 finding and source
lines 1175–1223; performed a guarded raw-byte replacement of that one comment and reviewed its source
diff/readback. Every other byte of the runtime file was preserved, including policy, branches, authority,
events, wire/public APIs and line endings. The only other edited file is this appended receipt.

Earlier patches remain intact. Canonical guides, tests, shared configuration/trackers and foreign harness
files were untouched; no new regression was added for a comment. No execution, compiler, Unity, tests,
product checker, installation/service, child agent, commit or push was launched. Existing verification
debt and the global source-freeze barrier remain unchanged; this follow-up claims source completion only.

## Source3 NETWORK-WOVEN-FIXTURE-INGRESS — Play SOURCE-FINAL; Editor coordination pending

Root closed source3 execution and authorized this bounded fixture correction. Read the current contract,
request, central relocation reservation metadata/Network entries, actual owned XML cases/messages/stacks,
and the relevant installed Mirror implementation. The triage documents are located under
`Local/Audits/assembly-2026-10-23/` in this checkout.

Actual historical baseline, from `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/`:

| Receipt | SHA256 | Owned result read |
|---|---|---|
| `21-full-play-graphics.xml` | `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a` | Command: 25 failed / 13 passed; Lobby: 5 failed / 8 passed. |
| `22-full-edit-graphics.xml` | `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2` | Handler ownership: all six cases failed at the unread-byte oracle. |

These are the operator's earlier source3 results, not results for the following corrected source.

### Disjoint Play changes completed

- `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoNetworkCommandTests.cs`: changed all four manually
  assembled integer writes to `NetworkWriter.Write<int>`: both relay channel indices, the subsequent
  late-reactive value and the shared Int payload. The installed `NetworkWriterExtensions.WriteInt:31`
  writes four bytes, while `[WeaverPriority] WriteVarInt:46` / `ReadVarInt:51` are preferred by
  `Editor/Weaver/Writers.Register:34–58`. `NetworkWriter.Write<T>:221–233` dispatches through the
  Weaver-populated `Writer<T>.write`, matching that registration. Float/bool/string/Vector3 payloads
  retain their matching serializers. No other manually encoded int/enum/unsigned wire field exists in
  this fixture; enum test arguments configure components and are not command payload fields.
- The 25 baseline Command failures were relay per-peer budget (3), invalid scalar binding (7), invalid
  reactive binding (3), late reactive initialization (1), property policy denial (3), accepted property Int
  (1), reactive policy denial (3), accepted reactive Int (1), and relay opt-out/budget preservation (3).
  Their exact failure was `Deliver:491`, expected zero unread bytes, actual 3 or 11. The receive delegate,
  `reader.Remaining == 0`, every denial/security/budget assertion, accepted target/reactive value, payload,
  notification and outbound snapshot assertion are preserved. No production protocol was changed.
- `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoNetworkLobbyTests.cs`: removed the test manager
  subclass's empty `OnValidate` override and use the actual `NeoLobbyManager`. A private fixture factory
  prepares separate `NeoLobbyPlayer`/`NetworkIdentity` and `NeoNetworkPlayer`/`NetworkIdentity` templates
  before manager activation. Both complete graphs stay under an owned inactive holder; their prefab
  roots retain activeSelf=true so instantiated clones can use ordinary Awake. Nonzero task-owned IDs
  are assigned through public `NetworkClient.RegisterPrefab(template, id)`, followed by public
  `UnregisterPrefab`; normal manager startup performs the actual registrations without duplicate warnings.
  No private identity/cache assignment, reflected lifecycle replay or Editor asset API was introduced.
- The five Lobby failures were typed Join/Leave, connecting cancellation and the three public Join
  reentry stages. Each stopped on `NetworkRoomManager.OnStartClient:430`'s missing RoomPlayer error;
  the vendor also requires a game prefab at line 434. Both references now exist before public Join.
  Added assertions verify exact room/game types, distinct roots, valid identities/IDs, and both actual
  `NetworkClient.prefabs` entries after Join. All existing connecting/cancel, address, ordered reentry,
  unchanged refresh and status/count oracles remain. Base validation and unexpected-error handling
  remain active; no errors are whitelisted. Owned templates are children of the existing teardown root.

### Precise Editor handoff

`Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkHandlerOwnershipTests.cs` was **not edited**
in this continuation. Its six historical failures have the same `Expected: 0 / But was: 3` at
`Deliver:144`; the fixture writes `OwnershipMessage.Value` with fixed-width `writer.WriteInt(value)`.
The required coordinated correction is the corresponding registered `writer.Write<int>(value)`, keeping
all six actual installed-delegate payload/count/disposal/restart cases and `Remaining == 0` intact.
Root/primary Neo.Editor.Tests must coordinate that Editor edit under the central reservation before
assigning/resuming it; no helper move/extraction, reserved consumer, new Fixtures assembly or shared
asmdef was touched by this actor. This receipt does not claim the six Editor failures are repaired.

### Evidence and remaining gate

Actual work in this continuation: read historical XML failures and stacks, record their artifact hashes,
inspect native Mirror serializer/registration/identity/room-manager APIs, and manually review source
diffs against the source3 `u63/Assets` copies for both owned Play files. Those diffs retain all prior
substantive oracles and show only serializer/precondition corrections plus stronger registration checks.
Only the two owned Play fixtures and this receipt were edited. Production source/public APIs/serialized
settings and canonical docs are unchanged because no production behavior/contract changed. Earlier
audit, NI7 and NI26 patches remain preserved; no new asset or metadata file was needed.

No Unity, compiler, tests, product checker, install/service, child, commit or push was launched here.
Source/XML reads, source diffs and hash bookkeeping are not a new test run. Corrected Play acceptance
is **UNEXECUTED** until every writer finishes and root opens a new immutable freeze. Actual remote
admission/production transport, saved lobby scene/prefab persistence, later Unity-version coverage and
the coordinated Editor serializer repair remain distinct evidence/dependency items. The existing direct
woven-body, DummyTransport and manually scheduled polling controls are not promoted to remote/native
PlayerLoop evidence. Source writes by this actor stop with this receipt.

## NETWORK-EDITOR-REGISTERED-SERIALIZER — SOURCE-FINAL, 2026-10-25

Root explicitly released the original helper-free Editor handler fixture after the primary's relocation
completion. The previously deferred Editor source correction is now complete. In
`Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkHandlerOwnershipTests.cs:138`, changed only
`writer.WriteInt(value)` to `writer.Write<int>(value)` for `OwnershipMessage.Value`, using the registered
Weaver-priority VarInt serializer already traced above. All other fixture bytes, including all six
client/server lease/disposal/unregister/re-register/shutdown payload/count cases, cleanup and
`reader.Remaining == 0`, remain unchanged.

Actual work: read the current released fixture and source3 XML's six exact failures/messages/stacks;
reviewed the one-line source diff and byte-preserving replacement. Historical source3 Edit failures
remain the baseline; this correction is **UNEXECUTED** until root opens a new freeze after all writers
finish. This fixture exercises the installed Mirror receive delegate and lifecycle ownership contract,
not remote transport or native PlayerLoop acceptance.

NI39 changed-file hash handoff for the existing reviewer:

- Path: `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoNetworkHandlerOwnershipTests.cs`
- Before SHA256: `7fef587773092ed5d04c5d3148f0a7f370e639bb0164ffb4850bc9c639184291`
- After SHA256: `85577bf6de21ea2e587f06dd6a1d75cafd41fcb3f26b2e3c78204e56824fb90e`

Earlier reviewed Play corrections are untouched. Only this fixture and the appended own receipt changed;
production handlers/protocol/APIs, docs, moved helpers/mappings/GUIDs, aliases, shared asmdefs and primary
legacy files were not edited. No Unity, compiler, test, checker, install/service, child, commit or push was
launched. Source writes stop here; the Editor coordination dependency is source-closed and its six-case
execution closure remains deferred.
