# Neo.Network: approved implementation request

Original actor: `neo-audit-1023-b04-05`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### EDITOR-LOBBY-PERSISTENCE (P1; approved-source-correction)

Editor C2: Join/Ready unwired, AddListener callbacks not persistent, scene RoomPlayer assigned as prefab

### NETWORK-REFLECTION-VALIDATION (P2; approved-source-correction)

Indexer/missing getter/read-only/mismatched member cached without validation; configure once with gated failure and no hidden narrowing.

### NETWORK-LATE-REACTIVE (P2; approved-source-correction)

Null reactive instance incorrectly makes cache resolved forever; retain metadata but retry absent instance readiness.

### NETWORK-UNSCALED-CLOCK (P2; approved-source-correction)

Network budgets/polling and QuickPlay timeout use scaled Time.time and freeze during pause; use unscaled double clock preserving gameplay guards.

### NETWORK-UNKNOWN-POLICY (P1; approved-source-correction)

NC03 unknown enum fail-open; reject unknown policy before trusted-caller early allowances, preserve named None/OwnerOnly/ServerOnly behavior and values.

### NETWORK-CONTRACTS-DOC (P3; approved-source-correction)

Link canonical Network entry to new Contracts.md; retain all-implementations scene reactivation/configuration semantics.

### SHOP-MONEY-CLIENT-OPERATIONS (P1; approved-source-correction)

Money accepts authority-free reward/set command with numeric checks only; assess explicit server policy and valid named-authority behavior separately from optional purchase request/ack.

Root decision: Approve one serialized _clientMutationAuthority + typed ClientMutationAuthority existing enum, ServerOnly default. Command validates isNetworked/known0..6 opcode/payload/known policy/IsAuthorized(gameObject,sender,policy) BEFORE budget, denies Refund7; direct offline/server APIs unchanged. ExplicitNone cooperative/OwnerOnly wallet identity migration. Private rate clock unscaled double. No HasServerAuthority sender shortcut. Read NativeReview-B-contracts.md.

### COUNTER-REMOTE-OPERATIONS (P1; approved-source-correction)

Remote Add/multiply sends locally computed absolute value; concurrent clients lose increments. Assess minimal server operation dispatch preserving float-backed API; exact numeric backend optional.

### NETWORK-POLL-CADENCE (P2; approved-source-correction)

Both sync adapters advance lastSync only when value changed; expired static binding polled every frame. Timestamp each eligible poll independently of send/dirty using unscaled double clock.

### NETWORK-RELAY-OPTOUT (P1; approved-source-correction)

All three ActionRelay CmdTrigger handlers omit isNetworked ingress gate; queued command must not dispatch after opt-out, reject before budget/mutation.

### NETWORK-HANDLER-OWNERSHIP (P1; approved-source-correction)

A/B handler sets same message type; B replaces A then A.Dispose unconditionally unregisters live B. Remove only owned registration using actual handler identity/generation; real delegate coverage.

## Relevant full native sections

Source: NativeReview-A.md

### A-10 — P1: invalid authority enum values fail open

Owner: `Neo.Network`, original task `neo-audit-1023-b04-05`; Contracts owner `neo-audit-1023-b01-03` supplies declarations/docs. Current source: `Assets/Neoxider/Scripts/Network/Core/NeoNetworkState.cs:160–179`, `IsAuthorized`; SHA256 `072340a0e1a526336e46e94a62dae5ab05ecc11a4ebd80e95636a14dd3ea50e4`.

Trigger: a configured/cast/deserialized mode is -1, 3 or int.MaxValue. Expected: unknown policies reject every caller category. Actual: `default: return true` admits the call, including an unrelated remote sender. No remotely controllable policy/exploit is alleged; the reusable authority gate itself demonstrably accepts invalid configuration.

Smallest repair: fail closed for unknown modes before any server/host allowance; the current implementation switches before named-policy allowances, so a rejecting default is sufficient if that structure is retained. Preserve exact named-policy and `sender == NetworkServer.localConnection` behavior. Cover remote owner/nonowner, direct server, host, null target/missing identity and invalid values. The optional pure policy extraction is not required. Link `Contracts.md` from the canonical Network README and document the deliberate invalid-input change.
Source: NativeReview-A.md

### A-11 — P2: reflection sync caches unusable accessors/types

Owner: `neo-audit-1023-b04-05`. Current source: `Assets/Neoxider/Scripts/Network/Core/NetworkPropertySync.cs:404–470`, `ResolveMember` / `ReadValue` / `WriteValue`; SHA256 `3960207c8c072150d7e19e1fda675ded8de192798120c029dc665a81eb42b6f9`.

Trigger: bind `_fieldName = "Item"` to an indexed property, choose a missing getter/setter, readonly field, or a value type that cannot be written from the selected payload. Expected: reject invalid binding without gameplay/outbound mutation and report one gated configuration diagnostic. Actual: any matching FieldInfo/PropertyInfo is cached; parameterless GetValue/SetValue and unchecked payload assignment can throw repeatedly or silently leave the target unchanged.

Smallest repair: validate index parameters, required accessors and supported read/write compatibility before caching/using the binding. Preserve intentionally supported conversions only where both directions are defined; introduce no silent narrowing or new generic endpoint framework. Add behavior regressions for indexed/write-only/read-only/incompatible targets and controls for the five supported types. The same generic property-get path in `NetworkReactiveSync.ResolveReactive` should reject indexed/inaccessible candidates rather than invoking them blindly.
Source: NativeReview-A.md

### A-12 — P2: a temporarily null reactive instance is cached permanently absent

Owner: `neo-audit-1023-b04-05`. Current source: `Assets/Neoxider/Scripts/Network/Core/NetworkReactiveSync.cs:294–334`, especially `_cacheResolved = true` at line 331; SHA256 `1fe706eea2b335718a00c7623b60b5521cbd6ba39176c3922b0a6fa5043d66ff`.

Trigger: the target's valid reactive field is null when initial state/hook/poll first resolves it, then the target creates its ReactivePropertyInt/Float/Bool during normal initialization. Expected: the next accepted update can bind and change the new instance once. Actual: the terminal negative cache suppresses all later resolution and updates remain lost.

Smallest repair: distinguish resolved member metadata from instance readiness; do not permanently cache a missing instance. Retry at the bounded poll/apply boundary and keep diagnostics gated once. Test null→instance plus a subsequent accepted update. Replaying a previously received snapshot without a new update is a separate explicit hydration policy, not required to repair this case.
Source: NativeReview-A.md

### A-13 — P2: networking budgets/timeouts freeze with gameplay time

Owner: `neo-audit-1023-b04-05`. Current source: `NeoNetworkComponent.cs:38–79` for both rate limits; `NetworkPropertySync.cs:75,107,150` and the other payload branches; `NetworkReactiveSync.cs:64,84,126` and other branches; `Lobby/NeoNetworkDiscovery.cs:99–132`.

Trigger: Time.timeScale becomes zero after a command or while QuickPlay/polling is pending. Expected: real networking rate windows, discovery refresh and QuickPlay timeout continue while explicitly configured gameplay pause/authority guards retain their own meaning. Actual: Time.time stops, so an accepted command can permanently exhaust its window, sync checks stop and discovery never reaches its timeout. Float clocks also lose sub-interval precision in long sessions.

Smallest repair: use one consistent unscaled monotonic double clock for these network intervals and stored timestamps. Preserve immediate-repeat rejection and all gameplay ownership/direction/pause gates; do not globally convert game timers to realtime. Add paused-window/timeout regressions restoring timeScale in teardown. No runtime pause test ran in this review.
Source: NativeReview-A.md

### A-14 — P2: unchanged bindings bypass the configured polling interval

Owner: `neo-audit-1023-b04-05`. Current source: `NetworkPropertySync.cs:99–230` and `NetworkReactiveSync.cs:76–180`, `Update`.

Trigger: an eligible binding remains unchanged after `_syncInterval` elapses. Expected: the Inspector's documented check cadence bounds capture/read attempts. Actual: `_lastSyncTime` advances only inside dirty branches. Once the gate opens, every subsequent frame polls the static value; property sync repeatedly reflects/boxes at N×FPS for N static adapters.

Smallest repair: advance the eligible poll timestamp independently of dirty/send success, using the same unscaled double clock as A-13, while retaining direction/readiness gates. Keep the configured send minimum and retry behavior coherent. A getter-count regression can prove a second same-window Update does not reread an unchanged target. This is aggregate-cost control flow evidence, not a measured profiler result.
Source: NativeReview-A.md

### A-15 — P1: queued action commands ignore the server's local-only switch

Owner: `neo-audit-1023-b04-05`. Current source: `Assets/Neoxider/Scripts/Network/Core/NetworkActionRelay.cs:348–408`, all three `CmdTrigger*` handlers.

Trigger: the server sets `isNetworked = false` while a previously valid client command is queued, or a peer sends that message to the still-spawned identity. Expected: remote ingress stops; local public APIs retain local-only operation. Actual: handlers check budget/index/authority but never `isNetworked`, then dispatch/broadcast the action. NetworkContextActionRelay already rejects disabled/non-networked ingress, so this is an inconsistent safety boundary.

Smallest repair: reject non-networked ingress in all three handlers before budget consumption/mutation, retaining named authority/channel scope behavior. Add woven-body regressions that toggle the server flag and observe zero server/client outputs; real admission remains separate evidence. No external exploit was executed.
Source: NativeReview-A.md

### A-16 — P2: disposing an old handler set removes another live set's replacement

Owner: `neo-audit-1023-b04-05`. Current source: `Realtime/NetClientHandlers.cs:68–77,104–116,152` and `Realtime/NetServerHandlers.cs:63–73,97–109,142`.

Trigger: set A registers a message T; set B registers/replaces T; then A.Dispose/UnregisterAll runs while B remains alive. Expected: A releases only registration it still owns, or duplicate ownership is explicitly rejected. Actual: A unconditionally calls UnregisterHandler<T>, deleting B's current handler. Local Mirror source confirms Replace overwrites one global dictionary slot and Unregister removes it without owner matching (`Assets/Mirror/Core/NetworkClient.cs:584,606–611`; `NetworkServer.cs:994,1010–1014`). No wire delivery is needed to establish this dictionary ownership mismatch.

Smallest repair: make unregistration conditional on the registration token/delegate still installed by that set, or explicitly reject conflicting ownership with a documented compatible policy. Do not silently add message multiplexing, transport changes or a replication framework. Add two-set lifecycle/reconnect regressions and document whether an older registration is restored when a newer one ends; that restoration policy is a root decision separate from preventing one disposed owner from deleting another's registration.

No claim of absence is made for unreviewed modules.
Source: NativeReview-A.md

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
Source: NativeReview-B.md

### B-16 / P1 — lobby generator can replace unsaved work and produces nonpersistent wiring

Owners: Neo.Editor `neo-audit-1023-b03-03`, Neo.Network `neo-audit-1023-b04-05` for room/game prefab and runtime presenter, sample coordinator for optional sample assets. `Assets/Neoxider/Editor/Network/LobbyDemoSceneCreator.cs:27` creates NewScene(Single) before any save/continue guard. Trigger: invoke its menu with an unsaved/dirty scene; expected cancel preserves all loaded work, actual source unconditionally unloads current scenes. First bounded fix is a save/continue decision before scene mutation with a cancellation-preservation regression. Separately, lines 46/109–142 assign a scene RoomPlayer to playerPrefab and register runtime AddListener callbacks; Join/Ready have no handler. A saved/reopened demo does not satisfy the advertised usable lobby. Preserve the existing entry point but use a correctly typed prefab/sample presenter contract; this needs Network/sample coordination and two-peer persistence verification. It is not fixed by a guard alone.
Source: NativeReview-B.md

### B-25 / P1 — an old network handler registry unregisters its replacement

Original runtime owner Neo.Network / `neo-audit-1023-b04-05`; supplemental lead Tests-Play-Network / `neo-audit-1023-extra-05`. Current `Assets/Neoxider/Scripts/Network/Realtime/NetClientHandlers.cs:76–77` and NetServerHandlers.cs:72–73 capture unconditional type-wide UnregisterHandler actions. Trigger: A.Add<T>/RegisterNow, B.Add<T>/RegisterNow, Dispose(A), then send T while B remains live. Expected: B receives exactly once. Actual: A removes B's type-keyed Mirror registration while B.IsRegistered remains true, potentially disconnecting on later unknown message. Confirmed against current local Mirror NetworkClient.cs:572–611 and NetworkServer.cs:986–1014; no live reproduction run. Minimal correction: internal Neo registration owner/generation leases, remove only the current owner, preserve Add/RegisterNow/Dispose and last-registration semantics. Test both client/server, opposite disposal order and restart using received payload/count. Raw external Mirror replacements remain an explicitly bounded compatibility limit unless a reliable identity check is available. A broad NetworkTestSession framework is not prerequisite.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Network.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Final NI7/NI8 integration handoff

ROOT personally FULL-read and accepts NI7-01/P1 and NI7-03/P2 in NativeImplementationReview-7.md. Remove unsupported NonParallelizable only, keep all meaningful handler cases. Fence obsolete Lobby RefreshView output stages after reentry commits an actually CHANGED snapshot. Increment publication revision only for changed snapshots; nested UNCHANGED Refresh must not cancel yet-undelivered ordinary count/countText/ready/status. Add public JoinLobby reentrant transcript, ordinary order and unchanged nested positive controls. No editor/Contracts changes or general framework. Source/docs/receipt only; no execution.

## Root accepted NI26 dedicated context comment correction

ROOT FULL-read NI26all142lines andactualContextRelay1175–1223. NI26-03/P3 ONLYcommentline1199 inDispatchOnServer contradictsactualdedicatedbranch1210–1213. CorrectcommenttodistinguishhostclientApplyResolvedonce vsdedicatedrelaywithoutlocalclientapplication. Runtimepolicy/branches/authority/event/wireAPIstayBYTEFORBYTE asidecomment; existingcanonicalguide73host/113transientlatejoinalreadyaccurate, no guidechange/testframeworkneeded. AppendaccurateSOURCE-FINAL receipt forcomment-onlyfollowup, doNOTrepeatpriorpatchoraddtestsforcomment. SAMEoriginalactor no sharedconfig/foreignharnessfiles/execution beforefreeze.

## Snapshot3 actual NETWORK-WOVEN-FIXTURE-INGRESS

ROOT has closed source3 execution after every owned native job stopped. SAME original actor only, gpt-6.1-sol/max. No Unity/compiler/checkers/tests until ALL source writers finish and root opens a NEW freeze. Root personally read all98 graphical Edit failure-message groups and all43 Play groups; Local/full-play-failure-triage.md (70 lines) and critical-failure-triage.md (35 lines) are independently reviewed. Actual XML paths are Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (7c6d24e1...) and21-full-play-graphics.xml (2b152164...). Read your exact cases/messages/stacks and actual runtime/vendor APIs before editing. CENTRAL RESERVATION: primary Neo.Editor.Tests b03-04 alone owns ALL Editor-only native helper moves/extractions, new Neo.Tests.Fixtures, shared test asmdefs, and its owned legacy test adaptations. Do NOT move/extract Mono helpers, edit shared asmdefs, or race any file in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. If the map is not yet present, work only the explicitly disjoint runtime/Play/new helper-free fixtures below; coordinate any caller containing embedded helpers before changing it. Preserve every substantive positive/negative/count/payload/ownership oracle and all public/serialized APIs/settings. Do not make production changes to hide a fixture precondition, add ExecuteAlways, blanket Ignore/error whitelists, manual Update/mocks as native evidence, reflection into private UserCode as receive proof, or weaker comparisons without actual native-baseline/numeric evidence. No other owners/vendor assets/314 migration targets, no children/commits/push; release Samples~ and protected DocsEn.meta untouched. Update own honest implementation receipt and canonical docs only if behavior/contract actually changes; report unresolved native capabilities explicitly. Stop all source writes when done. Own Play NetworkCommandTests.Deliver491 has25 real failures,3/11 unread bytes. Actual vendor NetworkWriterExtensions31 WriteInt=fixed4, WeaverPriority WriteVarInt46/ReadVarInt51 preferred by Editor/Weaver/Writers.Register34–58. Match actual registered wire serializers for EVERY manually assembled int/enum field (and unsigned fields if implicated); retain Remaining==0/all policy/security/per-peer budget denial/ordinary accepted state/event controls. Production protocol stays unchanged. Own Play LobbyTests five cases start client without required room/game prefabs: supply valid owned typed prefabs/complete inactive Mirror graphs, preserve actual join/cancel/presenter reentry and base validation. Do not override base validation or whitelist errors. Editor HandlerOwnershipTests144 has same fixed-vs-varint message failure; coordinate with primary before changing helper-bearing Editor files. Only disjoint own Play fixture corrections immediately; any genuine runtime issue requires actual evidence.

## Source3 released caller NETWORK-EDITOR-REGISTERED-SERIALIZER

Continue SAME original actor/session gpt-6.1-sol/max. General execution/freeze remain FALSE. NO Unity/compiler/test/checker/install/service/children/commit/push before ALL writers finish/newfreeze. Primary b03-04 has SOURCE-FINAL completed its54 helper relocations; root FULL-read dedicated21-line audit/17-line plan/112-line mapping and source-final declaration. ROOT NOW RELEASES ONLY your original module fixture CALLER files for the bounded behavioral correction below. The existing moved helper files under Tests/Fixtures, shared asmdefs/refs, original GUID/type mapping and extraction stay PRIMARY-EXCLUSIVE; do not relocate/recreate helpers or undo aliases. Read current source after relocation and actual source3 XML before patching. No production API/friend/serialized settings changes just to hide a fixture precondition. Retain every exact substantive positive/negative/count oracle, no blanket Ignore/expected-error/no-op/private fake lifecycle/ExecuteAlways/manual Update as native acceptance. Actual native callback claims require supported top-level Play transitions or real PlayerLoop/physics; pure public C# contract tests must say what they cover. Preserve UTF8/no-var, Samples~ and protected DocsEn.meta; no314 migration targets. Update honest own source receipt including deferred closure and stop. Proposed correction is unexecuted until later native run. Your EditorHandlerOwnershipTests is not in the relocation helper map; root explicitly releases that own helper-free fixture. Correct its ONE manually written OwnershipMessage.Value int writer.WriteInt(value) to actual registered writer.Write<int>(value), matching weaver priority VarInt. Keep all6 lease/reentry/unregister/re-register/shutdown payload/count cases and reader.Remaining==0; no production handler/protocol changes. Earlier Play command/lobby corrections remain intact. Coordinate current NI39 reviewer for changed-file hash only; no new helper movements or primary legacy source edits.
