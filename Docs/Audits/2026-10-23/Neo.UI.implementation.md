# Neo.UI — implementation receipt

Target: `demo` / `11.0.0`; original actor `neo-audit-1023-b08-07`, `gpt-6.1-sol`, reasoning `max`.

Status: **SOURCE-FINAL — Neo.UI-owned source, documentation and regression source complete**.
The published GM transition API has been inspected against all three existing consumer reads; no binding
change is required. No remaining Neo.UI source dependency is identified. Compilation, discovery and runtime
verification of the latest source correction remain unexecuted behind the renewed global writer barrier.
The root-provided snapshot-3 results below predate that correction; this is not a verification-pass claim.

## Approved actions implemented in owned files

| Action | Source disposition |
|---|---|
| `UI-RUNTIME-RIG-REPLACEMENT` | `UIMeshRigLayoutBuilder.ApplyToOwner` preflights the complete existing owned-point removal set before any creation, binding mutation or immediate/deferred Destroy. Root points and branches containing another owner, including inactive owners, throw `InvalidOperationException`. Ordinary replacement and append remain supported. |
| `UI-EDITOR-NAVIGATION` | Canonical UIMeshRig guide links `Editor.md` and documents runtime preflight, append, preserved modes/keys/hierarchies, disposable branches and the absence of runtime Undo. The excluded editor guide's outdated comparison is handed back to its owner below. |
| `UI-PAUSE-GM-BASELINE` | Shared time baseline is captured before a GM pause; a GM joining existing page time receives that original baseline. The exact owned GM resumes before final page time restoration, preserving resume observers' time. |
| `UI-PAUSE-GM-OWNERSHIP` | Shared page requests attempt one GM transition. Release checks the exact instance, current singleton, Pause state and acquired transition version. Prepaused, replacement and externally resumed/repaused managers do not receive the old group's Resume. Uses the published GM generation property below. |
| `UI-PAUSE-OVERLAP` | Default PausePage instances share first/last per-channel ownership without scene wiring. Time requests use their minimum; a time-controlling owned GM holds zero. Cursor owners keep it visible/unlocked, and any explicit final ForceLockedHidden request is retained until the last cursor release. |

`PausePage` registers before external GM callbacks, reconciles reentrant changes in bounded passes, and
settles shared frame state at most once per frame. Cleanup attempts GM/time/cursor release even when an
observer throws, then rethrows the first fault. Acquired flags are released even if authoring options change
while open. Runtime subsystem reset clears shared state; still-active components can re-register next update.

## Exact files changed in this implementation phase

Runtime (`Neo.UI`):

- `Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigLayoutPreset.cs`
- `Assets/Neoxider/Scripts/UI/PausePage.cs`

Owned public documentation:

- `Assets/Neoxider/Docs/UI/UIMeshRig.md`
- `Assets/Neoxider/Docs/UI/PausePage.md`
- `Assets/Neoxider/Docs/UI/README.md`

New regression sources, each with its own UUID `.meta`:

- `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIRuntimeReplacementTests.cs`
- `Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUIRuntimeReplacementPlayTests.cs`
- `Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUIPausePageTests.cs`

This receipt is the only new audit artifact. Historical `Neo.UI.md` and `Neo.UI.plan.md` were not edited.
Prior coherent audit patches, original five fixtures and other workers' changes were retained.
The later snapshot-3 correction also updates the original owned fixture
`Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUICategoryBarTests.cs`; exact changes are recorded below.

## Published cross-owner API — dependency resolved

Provider-ready resume, 2026-10-24: read the complete current implementation contract/request, Managers
completion receipt and actual `GM.cs`, including its getter, State setter, Pause/Resume and common event
fence. `Neo.Tools.GM` now publishes the exact additive read-only contract:

```csharp
public ulong TransitionVersion => _transitionVersion;
```

The common counter advances with `unchecked(previous + 1UL)` exactly once before each effective State
transition and its callbacks. Same-state assignments and guarded no-op Pause/Resume calls leave it unchanged;
reentrant effective transitions advance it further. The same generation suppresses obsolete outer GM
broadcasts after nested transitions. Leaving Pause still restores the captured time before state/resume
notifications.

Inspected the three existing reads in `PausePage.AcquireManager` and `PausePage.OwnsManagerPause` against
that implementation. Acquisition expects exactly `unchecked(manager.TransitionVersion + 1UL)`; release
requires the captured version and exact live/current paused manager. Name, type and transition semantics
match without any binding patch. The previous pending API dependency is resolved by published source.
At that provider-ready handoff, integrated compilation and behavioral acceptance were unexecuted;
the subsequent root-provided snapshot-3 outcome is recorded separately below.

That provider-ready resume changed only this receipt. Completed runtime/docs/regression patches are
preserved and were not replayed. No Managers source, shared tracker or asmdef is changed by this actor.
The existing dependency direction remains Neo.UI -> Neo.Tools.Managers; no reverse reference is needed.

Additional narrow handoff to **Neo.UI.Editor**: `Assets/Neoxider/Docs/UI/Editor.md`, final C#-path paragraph,
currently says runtime builder calls do not provide Inspector safety guards. Runtime now also rejects root
and nested-owner destruction before mutation. That paragraph should distinguish the now-shared rejection
policy from editor-only Undo/mode/preview operations. The canonical runtime guide is corrected and linked;
this excluded topic was read as context and left to its owner. Its wording handoff remains separate from
Neo.UI source completion and does not require another runtime binding change.

## Compatibility, reuse and deliberate 11.0 behavior

No existing public method signature, serialized field name, enum value or default was removed/renamed.
No new UI service or scene reference is required. The runtime layout API deliberately rejects destructive
root/foreign-rig replacement shapes with an explicit exception instead of deleting them. It still does not
protect arbitrary non-rig contents of otherwise disposable point branches or provide editor Undo.

Pause screens automatically use shared channel ownership rather than isolated snapshots. Early cursor
force-lock requests now wait for the last cursor owner, and page groups no longer resume a foreign GM
transition. Configuration is captured per enable. A foreign/replacement time-controlling paused GM remains
paused when page time is released. This is scoped page coordination; arbitrary raw time/cursor writes and
external GM policy changes are not a general shared-lease platform.

Reused `IUIMeshRigOwner`, `UIMeshRigOwnerResolver`, existing GM Pause/Resume/current-instance APIs, ordinary
C# collections, Unity lifecycle and existing NUnit/UnityTest assemblies. No package/install or shared asmdef
change. New code has explicit types and no new version conditional or obsolete instance-id call.
General pause-service and tracked flight/cancellation designs in the historical plan remain proposals.

## Original authored acceptance — unexecuted at initial handoff

Three fixtures declare **20 test methods / 26 parameter-expanded source cases**; those source counts are unchanged. Loops inside Play
methods additionally exercise each of the three scene render adapters and the listed closure/baseline cases.
The snapshot-3 receipt below records later execution of the relevant original cases; the latest corrections
have not been compiled, discovered or executed.

- Edit replacement: root-point rejection, inactive foreign owner under an owned point and independent
  nested rig retention, each across uGUI/world/SpriteRenderer; append preserves protected existing branches.
  Assertions inspect live object identities, exact keys, mode, counts and hierarchy after whole-set failure.
- Real Play replacement: all three adapters retain both hierarchies across the next-frame deferred-Destroy
  boundary after rejection; ordinary replacement retires old points while preserving the independent rig.
- Pause: baselines 1, 0.35 and 0; actual time observed inside Resume; both A/B closure orders; a GM joining
  an already paused time channel; already paused GM; destroyed/replacement GM; external resume/re-pause;
  synchronous observer closure, throwing observer cleanup and nested state-resume event trace; both cursor
  closure orders with/without final ForceLockedHidden; independent channels; minimum requested scale;
  captured-option release after authoring values change.

Pause tests use task-owned GM/EM probes and real MonoBehaviour lifecycle, assert state/time/event counts,
and restore time, frame rate, cursor and owned objects in teardown. They rely on the exclusive runtime
runner contract. **No `[NonParallelizable]` attribute was added.**

## Snapshot-3 source correction — UI-NATIVE-CATEGORY-OWNERSHIP / G28

Read the complete renewed request/contract, the action-index E50 group and actions L/U, the reservation
map, the exact assigned XML cases, current CategoryBar/PausePage fixture source, CategoryBar runtime and
the Play triage's common cursor action H. Recorded receipt identities, read without running hash/check tools:

- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`:
  index SHA-256 `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`.
  CategoryBar's two destruction cases failed with expected 0 / actual 1; the other three cases passed in
  that historical snapshot. These failures alone do not establish a runtime disposal defect.
- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml`:
  index SHA-256 `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`.
  PausePage's cursor case failed at line 308 with expected Locked / actual None; the other twelve cases
  passed in that historical snapshot. Native focus/lock capability was not established before that assertion.

The original S3 correction changed two owned source files and this receipt; public/serialized APIs, runtime,
canonical docs, settings, metadata/GUIDs, other owners and shared assembly references remain untouched.
No public contract change was demonstrated, so no public documentation change is required.

`Audit20261023NeoUICategoryBarTests.cs` retains all five test names and every previous count/listener/view
oracle. The three pure API cases remain Edit tests. The two destruction cases are now top-level UnityTests
that enter Play Mode before creating their objects; top-level UnityTearDown destroys only owned resources
and exits the acquired Play transition. Native Start must first initialize the active bar without a manual
Initialize/lifecycle call. The authored-button case proves live selection and the consumer listener,
reselects the first entry, then destroys only the component. After confirming bar-null it observes a later
native frame before asserting that the bar is dead, its GameObject and both
authored children alive, stale selection callbacks zero and consumer clicks exactly one.

The prefab case proves one actual spawned view, then one replacement after the old view's deferred
destruction. Removing only the owner component crosses both its own and its view's deferred-destruction
boundaries. The replacement must be dead and the owned-view count exactly zero. It retains the original
external-root child-count 1 -> 0 scenario and adds a sibling scenario with total count 2 -> 1, exact sibling
identity/parent/position retention and no CategoryBarItem remaining. Both scenarios preserve the external
root, owner GameObject, template and marker; the marker returns under the surviving owner and is hidden.
Whole-GameObject deletion is cleanup only, not substituted for component-removal acceptance.

`Audit20261023NeoUIPausePageTests.cs` retains all thirteen methods and the complete cursor force/no-force
and both-close-order matrix. Before any page exists, the cursor case requires actual host focus and directly
writes Locked/hidden, checking the exact readback immediately and after one native frame. A host rejecting
that control fails explicitly; None is never accepted as a lock result. The original None/hidden baseline,
first/second owners' unlocked/visible state, surviving owner's state, exact final lock/visibility and next
non-force group's restoration remain asserted, with native-frame persistence and focus checks added.

Reservations were checked before each edit: neither owned fixture nor this receipt is reserved. No helper
was moved/extracted, no reserved caller was edited, and no Neo.UI work needs deferred reserved-file access.
Primary b03-04 retains exclusive helper/Fixtures/shared-reference work. Cross-owner handoff through root:
MOVE's G23/G24 and UI's G28 share action H; compare their native direct-write/read persistence and focus
evidence on the same capable frozen host before attributing either failure to ownership. No shared cursor
helper or MOVE change is introduced here.

Source disposition: **SOURCE-FINAL for this correction**. Read/review only; no execution resumed. Native
CategoryBar destruction delivery and nested deferred cleanup, cursor focus/capability, all retained cursor
ownership assertions, compilation/discovery and cleanup after a failed Play transition require a new frozen
native run. No passed result from the old snapshot is claimed for these changed sources.

## NI51-02 follow-up — bounded native observation, 2026-10-25

Read the complete current request/contract and all 68 lines of immutable NI51, SHA-256
`b6ae5438baa8d198556e9fd300ac1167f55b37056956147c32ec354491ebcb6f`.
The reviewed source was `5f25d947933ab17b7b65ae4a0573521e7e55211741f99a461f05b2c9df1dcb1e`;
the preceding 193-line receipt was `44593d96c57390f4d8b86306378678c744b56c749e83f164c2d9e40ddf0cc5ec`.
NI51 correctly identified that the Editor test job's fixed null polls did not guarantee native frames,
despite genuine owner/clone-null positives. No runtime defect or native pass follows from that gap.

Only `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUICategoryBarTests.cs` and this receipt change in this
follow-up. Both native tests now wait for actual initial selection, owner destruction, and applicable old
and replacement view destruction. Each wait has a five-second deadline using
`EditorApplication.timeSinceStartup`, independent of native frame advancement. Existing positive checks
still fail if the required completion is absent; no timeout becomes a skip or pass.

After the bar-null positive, each test captures `Time.frameCount` and requires it to increase before any
borrowed-child/root/marker/sibling survivor or listener/consumer oracle. The prefab case additionally waits
for the exact replacement reference to become Unity-null. These predicates replace counted Editor polls;
adding polls alone could not supply the required observation window. No helper, lifecycle invocation,
framework, expected-error rule, tolerance or replacement resource was added.

All five case names, three pure API cases and two native cases remain. Exact selection0/consumer1,
component-only deletion, original owner/children/parent identities, old-view and replacement-null positives,
owned-view count zero, no-sibling 1 -> 0, sibling 2 -> 1, template/prefab retention, exact marker return/hidden
state and foreign sibling identity/parent/position remain asserted. Top-level entry/exit and owned-only
cleanup remain; a fixture flag records completed Play entry so teardown exits only its acquired transition.
Faults remain failures. No runtime/public/serialized/default/settings/GUID/shared-reference or other-owner
file was changed. Reservation reads before edits confirmed the two paths are not reserved.

Current source pins (artifact SHA bookkeeping, not validation):

| Path | Lines | SHA-256 |
| --- | ---: | --- |
| `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUICategoryBarTests.cs` | 290 | `c4dc67a69534e1df2052b327b7bfc5d34f146ac2b67be44d1600fcbeeda3ddd1` |
| Unchanged `Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUIPausePageTests.cs` | 466 | `77e1ea1bb6aabcb431b06ec27181723e0623614d1b928b51015555f38b90da0f` |

**SOURCE-FINAL for independent delta review; native UNEXECUTED.** PausePage's thirteen Play tests and
focus/direct-Locked control/matrix were not rewritten. Read back the changed CategoryBar source and
recorded hashes only; no Unity, compiler, discovery/tests, checker, process control, install/service,
child actor, commit or push ran. Source writes stop here. Native Start, actual destruction/frame advance,
all survival/listener/count oracles, timeout failure paths and cleanup/Play exit still require execution
after ALL writers finish and root establishes a new global freeze.

## Work actually performed and evidence debt

- Read implementation contract/request, own full audit/plan, relevant current runtime, GM/EM boundary,
  editor/public guides, test assembly references and existing own fixture source.
- Inspected the focused source/document diff and enumerated the generation references; on the provider-ready
  resume, read the published Managers receipt/source and confirmed the existing consumer binding by inspection.
- Authored runtime/docs/regression source and three new test metadata files; no asset/scene/sample changes.
- **Did not run Unity, compiler, tests, product validators/checkers, installers, services, commits, pushes or
  child actors.** No phase-1 static or historical runtime receipt is claimed as evidence for this source.

After every writer's completion, root must establish a new global freeze, compile/discover and run
nonempty Edit/Play suites on the installed Unity 6.3 and 6.6 hosts. Native cursor/focus behavior, deferred
destruction, callback exception logging, reload-disabled repopulation, serialized reference setup and existing
renderer/atlas/mask/panel/raycast paths need current integrated evidence. This worker performed no new
execution; the source correction has no compile/test-pass receipt. Other Unity 6.x compiler branches remain explicit
evidence debt. Metadata/GUID/UTF-8/package checks and final root push remain behind the global barrier.
