# Neo.Tools.Components — implementation receipt

Status: **source-final for owned runtime, documentation and regression sources**, `demo` / target
`11.0.0`, including root-accepted I19-01 and `COUNTER-NATIVE-WIRE-AND-UNITY-LIVENESS`.
Shared Play test reference integration is resolved.
Global review approval was observed;
the new execution barrier remains in force. This actor has launched no compilation, Unity, tests or
product validators. Root's actual source3 failure evidence and the subsequent fixture correction are
recorded below; this receipt does not promote that earlier run to a pass on the new source.
Original audit and plan remain historical artifacts.

## Dependency/API publication

- Counter retains its existing public `Set/Add/Subtract/Multiply/Divide` int/float overloads,
  `CounterModifyOperation` enum, reactive output and serialized fields. No new public API is required.
  Remote arithmetic now uses a private operation Command evaluated against the current server value;
  absolute Set remains last-writer-wins. All peers must use the matching 11.0.0 network build.
- Exact wire handoff for the supplemental network owner: existing public Counter methods require no
  adapter change. New private `CmdApplyOperation(CounterModifyOperation operation, float operand,
  NetworkConnectionToClient sender = null)` accepts `Add=0` and `Multiply=2` only; `Subtract` becomes
  a signed Add operand and guarded `Divide` a reciprocal Multiply operand. Enum values are unchanged;
  `CmdSetValue(float, NetworkConnectionToClient)` remains the absolute Set ingress. Both reject
  `isNetworked=false` before the existing sender-aware limiter. Remote operations dispatch before
  inspecting the client's stale balance or suppressing equality.
- I19 admission is now available at those same private receive endpoints, with no signature or opcode
  change. Absolute input must be finite; its old balance need not be finite. Known Add/Multiply
  require a finite operand and finite raw server float candidate before Int rounding and sender
  budget. The separate-peer owner can retain its actual woven/wire paths and extend bad-input cases.
- **Neo.Tools.Text dependency:** `SetText` must expose exactly `protected virtual void OnEnable()` and
  `protected virtual void OnDisable()`. Components overrides both with base calls and keeps its
  exact disposable subscription/wait ownership. These hooks have now landed from the Text owner,
  and TextScore overrides both with base calls. Components did not edit the Text owner's source.
- **Shared test reference resolved by root:**
  `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` now contains `Neo.Tools.Text` (observed by source
  reference enumeration). The previously reported Binding fixture dependency is closed in source.
  `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` already references it. This actor edited no asmdef.
- **RPG-COMBAT-NAVIGATION split:** Components owns the explicitly assigned old
  `Assets/Neoxider/Docs/Tools/Components/AttackSystem/README.md` backlink. Canonical
  `Assets/Neoxider/Docs/Rpg/README.md` belongs to Neo.Rpg; missing
  `Assets/Neoxider/Docs/Rpg/Combat.meta` belongs to root under the implementation contract. Both links
  and that metadata are now present from concurrent owners and were preserved (GUID
  `f75b7680dc604d8eb12dbf2c6294dbe6`). Components also links the canonical Combat guide from its entry.

## Assigned actions

| Action | Completed source correction |
|---|---|
| `COUNTER-REMOTE-OPERATIONS` / A-27 | Arithmetic Commands carry an operation/operand and read the server balance. Absolute Set dispatches before client equality suppression. Existing overloads, division checks, float storage, Int rounding and per-sender limiter are reused. Unknown operation and local-only ingress reject before budget. |
| `COUNTER-STALE-PUBLICATION` / A-26 | A private mutation revision invalidates the superseded continuation after reactive, typed, repeat and persistence callback boundaries. Direct server Set and absolute Command share the guarded mutation/publication path. Save load advances the revision too. |
| `I19-01` / P2 | Only remote Command ingress rejects NaN, both infinities and relative raw float overflow before budget or any mutation/notification/save/replication. Absolute finite Set remains able to repair a corrupt old balance. Existing cooperative authority, public direct behavior and Int rounding/division are preserved. |
| `COUNTER-NATIVE-WIRE-AND-UNITY-LIVENESS` / source3 G07–G08 | Owned Play fixture uses Mirror's registered enum/float serializers, Unity-native singleton liveness, and complete inactive identity/Counter graphs before their one native Awake. Zero unread bytes and all existing body oracles remain required. No production policy changed. |
| `TEXT-SCORE-LIFECYCLE` / A-28 | Protected overrides call the now-available base enable/disable hooks, preserving disposable subscription and deferred-wait ownership. Both component and GameObject disable are covered by authored native-animation cases. |
| `COMPONENTS-CAMERA-DOC` | Sibling camera guide now explains target dimensions, orthographic/perspective FitBoth expansion, full target viewport, cached inputs, shared size/FOV limits and editor opt-out/manual authoring behavior. |
| `RPG-COMBAT-NAVIGATION` | Components canonical entry links Combat directly. The specifically requested old AttackSystem backlink, canonical RPG link and Combat.meta were already landed by concurrent owners on the final read and were preserved. No duplicate folder metadata created. |

## Exact changed files in this implementation phase

- `Assets/Neoxider/Scripts/Tools/Components/Counter.cs`
- `Assets/Neoxider/Scripts/Tools/Components/TextScore.cs`
- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsComponentsNumericTests.cs`
- `Assets/Neoxider/Tests/Play/Audit20261023NeoToolsComponentsBindingTests.cs`
- `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoToolsComponentsNetworkTests.cs`
- `Assets/Neoxider/Docs/Tools/Components/Counter.md`
- `Assets/Neoxider/Docs/Tools/Components/TextScore.md`
- `Assets/Neoxider/Docs/Tools/Components/README.md`
- `Assets/Neoxider/Docs/Tools/CameraAspectRatioScaler.md` — explicitly authorized sibling guide.
- This receipt: `Docs/Audits/2026-10-23/Neo.Tools.Components.implementation.md`.

Existing regression metadata is retained; no new package file/GUID is introduced. Earlier coherent
audit fixes remain in place. Original audit/plan, shared manifest/reviews/contracts, asmdefs, other
owners' receipts, runtime sources, sample layout, package metadata and user files were not edited.

## Compatibility and callback contract

Ordinary notification order remains reactive → typed UnityEvent → optional repeats → save →
replication. A nested mutation supersedes the outer continuation after the current callback batch.
Reactive re-entry to Set(2) suppresses the obsolete typed event for Set(1). Typed re-entry still
delivers the already-started typed batch's captured 1 and the nested 2, then suppresses outer repeats,
save and replication. Repeat re-entry stops the old loop after that invocation. Existing notifications
are not rolled back, and callback exceptions are not promoted into a general transaction system.

The wire addition requires matching peer builds. Remote arithmetic now applies in server receive
order and does not predict a balance locally; absolute Set remains last-writer-wins. Rejected Commands
still have no acknowledgement/retry. Shared-counter authority remains the existing cooperative
`requiresAuthority=false` policy; no Money-style permission migration was added. I19 adds finite
admission only at the received Commands, including raw arithmetic before Int rounding; it does not
change direct server/host/offline public calls or add a clamp. Numeric float precision, a broader
direct-API nonfinite policy, exact-integer storage/save migration and large typewriter/session
frameworks remain proposals/limits in the historical plan. Serialized fields and public Counter
signatures are unchanged; no scene or prefab migration is required for these source changes.

TextScore retains `Set(int)` binding, automatically using the Text owner's exact integer final
formatting. With the existing On Enable Anim option enabled, base hooks pause/resume the owned tween;
rebind then targets the latest score. Turning that option off retains the base policy.

## Authored regression acceptance — inventory at the I19 source checkpoint

The first implementation added fourteen cases to the original 32: 46 cases at that checkpoint,
34 Edit and 12 Play, including nine conditional Mirror cases. **All previous 46 cases and their
14 implementation additions remain present.** I19 adds 47 cases below, giving **93 authored cases
total**: 34 Edit and 59 Play, including 56 conditional Mirror cases. This is source enumeration,
not discovery or a pass count. Source3 subsequently discovered the 56 Mirror cases and failed them
as detailed below; corrected-source execution remains pending the next freeze.

| Fixture | Added cases and observable acceptance |
|---|---|
| Numeric | Six: reactive and typed re-entry in each value mode assert final 2, exact typed sequences, two repeats and saved 2; repeat re-entry asserts `[3,1]`, final/saved 1; ordinary change retains reactive/typed/repeat order. |
| Binding | Two: disable actual TextScore component or GameObject during a native tween; currentNum/text stay frozen beyond the original duration, disabled binding releases its listener, re-enable reaches latest score 200 with exactly one binding. |
| Network | Six: direct Set and woven Set Command, each with reactive/typed re-entry, assert server, separate RPC receiver and initial snapshot all equal 2, with exactly one emitted value payload `[2]`; operation handlers with distinct sender identities assert 10→11→12→24→25 on server/RPC/snapshot; local-only/unknown ingress does not consume the next valid sender operation's budget. |

I19 additions in the same owned Network fixture, all through actual registered woven receive delegates:

- Six absolute denials: NaN/+Infinity/-Infinity in both modes.
- Twelve relative operand denials: each nonfinite operand, Add/Multiply, both modes.
- Eight finite-operand raw overflows: positive/negative Add and Multiply overflow in both modes;
  the Int cases establish rejection before a conversion can disguise invalid float arithmetic.
- Twelve corrupt-current denials: NaN/+Infinity/-Infinity with finite Add or zero Multiply in both
  modes, followed by a finite absolute repair from the exact same peer in the same frame.
- Six ordinary finite controls: Add/Multiply and absolute Set, including literal expected fractional
  results, Int rounding and repeat counts in both modes.
- One budget control: a successful Add blocks an immediate absolute Set from that same peer without
  changing the window/state/bytes; two other peers are accepted in receive order.
- Two direct-public controls: offline and direct host/server calls still accept legacy NaN and finite
  multiplication overflow. These are compatibility controls, not a recommended numeric policy.

Every numeric denial asserts unchanged stored balance and RPC receiver, exact initial serialized
bytes (including SyncVar), empty reactive/typed/repeat notifications, saved sentinel 77, zero save
writes, zero emitted value RPC payloads/bytes and no sender window. It then immediately accepts
absolute Set(3) from the same peer, with exact value/event/save/repeat/RPC assertions, a decoded
late-join snapshot and an established sender window. No yield occurs between denial and control.
Save counts use the existing isolated memory provider, which emits OnKeyChanged on every SetFloat
including an identical write; the unique key and observer are scoped and the prior provider restored
in finally. An unexpected repeat count above 32 throws to fail a broken implementation without
allowing a billions-iteration loop. Previously authored test bodies/assertions were not weakened.

The network fixture consumes the actual woven Mirror Command handlers through its public delegate
lookup, captures every matching serialized outgoing RPC payload (including duplicates), runs the
actual RPC receiver on a separate object, and performs real initial-state serialization. Commands
and receivers are still **in process with a host**; sender identities do not make them independent
remote clients. The supplemental separate-peer harness must exercise the existing public Counter
methods over production transport. No private method is invoked through reflection as remote proof.

Owned fixture cleanup removes its diagnostic subscription, stops its host, destroys its receiver/
manager/counter objects, restores the previous transport and removes the unique save key. It rejects
a foreign active session/manager. Binding cleanup restores Time.timeScale/CreateInstance and destroys
owned views/managers. These are authored cleanup paths, not executed isolation evidence.

## Actual work and remaining dependency/evidence

Actual work: complete implementation-contract/request reads, current relevant source and historical
audit/plan reads, scoped edits, source/diff inspection, reference enumeration and artifact bookkeeping.
Scoped `git diff --check -- <the exact files above>` returned exit 0 for tracked diffs. The three
prefixed fixtures and this receipt are untracked, so that command is not claimed as their validation.
No product checker, compiler, Unity/MCP, test runner, service, install, child agent, commit or push ran.

I19 follow-up changed only Counter.cs, the existing prefixed Network fixture, Counter.md and this
receipt. The root request and native I19 section/current receive/limiter/save-provider source were
read; the guards and acceptance sources were inspected without execution. Earlier scoped diff-check
evidence above predates I19 and is not promoted to a check of these new changes.
Read-only source enumeration found 46 TestCase attributes, one standalone Test and the retained nine
UnityTest methods in the Network fixture: 56 conditional cases. This is bookkeeping, not discovery.

**Remaining source/API dependency: none.** Root's Play reference addition is present. Runtime Text
hooks are consumed. The original supplemental network actor owns extension of the genuine dedicated
server/two-client harness for I19 numeric admission; this actor did not edit its source or receipt.

After every writer finishes, root must compile/import and discover/execute the frozen snapshot on
the isolated Unity 6.3/6.6 hosts, including MIRROR on/off dependency evidence and these fixtures.
Separate real clients must both Add(1) from 10 and converge to 12, exercise multiply/add ordering,
Subtract/Divide overloads, authoritative Set against stale displayed equality, independent throttling,
late join, reentrant server publication and the same I19 bad absolute/relative/overflow/repair cases.
In-process handler/snapshot coverage cannot close that
transport evidence. Native tween dispatch/timing, rendered camera output, wider Dialogue/typewriter
integration and package/UTF-8/meta/GUID checks remain unexecuted. Historical static results are not
reused as current pass evidence.

## Source3 follow-up — COUNTER-NATIVE-WIRE-AND-UNITY-LIVENESS

Status: **source-final; no remaining source/API dependency. Stop source writes pending root's new
freeze.** Changed only these two owned files in this follow-up:

- `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoToolsComponentsNetworkTests.cs`
- `Docs/Audits/2026-10-23/Neo.Tools.Components.implementation.md`

Actual failure evidence was read directly from
`Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml`, SHA256 recorded
by the full Play triage as `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`.
All 56 owned fixture cases failed in that historical run:

- One reached `Counter_CommandReceive_BudgetIsSharedByCommandsAndIndependentBetweenPeers`: `Deliver`
  line 676 expected `Remaining == 0`, actual 3, via `DeliverOperation:665` and case line 327.
- The later 55 failed in `SetUp:39`: expected CLR null, actual `<null>` destroyed Unity manager
  wrapper. Their substantive bodies did not run. Exact names/messages/stacks for both groups were
  enumerated from the XML. These are G07/G08, distinct from the legacy fixture's genuine live-manager
  cascade, which remains with its owner.

Decisive vendor APIs read before editing: `NetworkWriterExtensions.WriteInt` is fixed-width while
`WriteVarInt` carries WeaverPriority; enum writer generation delegates to the selected underlying
integer writer, and reader/writer registration honors that priority. `NetworkWriter.Write<T>` /
`NetworkReader.Read<T>` use those registered delegates; generated `InitReadWriters` initializes them.
`NetworkIdentity.Awake` caches all attached NetworkBehaviours and their identity/index. Vendor
`NetworkManager.OnDestroy` does not clear the singleton wrapper. Existing shared helper creates its
manager graph inactive already; this actor did not change that helper.

Completed fixture corrections:

- Operation enum and every manually authored float argument now use registered generic serializers;
  the RPC payload spy uses the registered float reader. SetUp explicitly requires those registrations.
  The actual registered woven Command/RPC delegates remain the receive paths. `Deliver` still asserts
  exactly zero unread bytes; payload-spy and actual RPC-receiver reads now require zero unread bytes too.
- The foreign-manager guard asserts `NetworkManager.singleton == null` using Unity liveness. It still
  rejects any live foreign manager and still requires both server/client idle before claiming ownership.
  No global singleton reset, deletion or takeover was added; existing exact-owned teardown remains.
- Inline host Counter, source Counter factory, RPC receiver and initial-snapshot receiver build their
  complete identity/Counter graph while inactive, configure it, then activate once. Each asserts the
  Counter's cached netIdentity is the actual NetworkIdentity before spawn or receive. No private cache
  assignment, repeated/manual Awake or production workaround was introduced.

All 56 conditional Mirror cases remain, including the cross-command shared-per-peer budget control,
normal/per-peer positives, invalid/local-only/unknown/nonfinite/overflow denials, finite repair,
reactive/typed/repeat/save assertions, exact RPC payload/count/byte oracles, host multiplicity and
initial snapshots. The other 37 authored cases are untouched: 93 authored cases remain in total.
No substantive assertion was removed, relaxed or converted into a skip/error whitelist.

Reservation map was read and the owned Play path is not reserved. No Mono helper was moved/extracted,
no shared asmdef or tracker edited, and no reserved file, other owner's source, vendor asset, migration
target, release sample or DocsEn.meta changed. Counter runtime policy and public/serialized APIs are
unchanged, so canonical product documentation required no change in this fixture-only correction.

Actual work here was source/XML/triage/vendor reads and scoped edits/inspection, without Unity,
compiler, tests, product checkers, services, installs, children, commits or push. The read-only
comparison against the owned fixture in the preserved source3 validation project confirmed
only the described setup/serializer/graph changes and added precondition/read-consumption assertions;
all previous body oracles remain. A later immutable
freeze must rerun all 56 cases: zero unread bytes, real native Awake/identity, destroyed-wrapper
admission, live foreign-manager rejection and every positive/negative body oracle remain acceptance
requirements. New-source compile and native results are unverified. Host/registered-receive coverage
still does not establish independent-peer transport; that capability stays with the separate harness
owner and frozen-snapshot operator. No source3 failure is marked passed by these source corrections.
