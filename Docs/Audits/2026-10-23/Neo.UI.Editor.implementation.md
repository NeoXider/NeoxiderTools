# Neo.UI.Editor — 11.0.0 implementation receipt

Status: **SOURCE-FINAL; no pending source/API dependency for this owner.** Same original actor/session
`neo-audit-1023-b08-08`, `gpt-6.1-sol`, reasoning `max`; target branch `demo`, package `11.0.0`.
Completed on 2026-10-24 after the Windows resource interruption. Prior coherent source and regression
edits are preserved. Latest source-final includes the root-approved Snapshot3
`UIEDITOR-NATIVE-NESTED-POSE` correction below; that corrected source has not been executed. Historical
audit and individual plan remain unchanged.

## Approved actions and dependency closure

### UI-EDITOR-NAVIGATION

Completed the owned documentation reconciliation in `Assets/Neoxider/Docs/UI/Editor.md`, final C# path
section. It now describes the shared runtime/Inspector replacement rejection policy across uGUI, world
mesh and SpriteRenderer: root points and branches containing another rig, including inactive nested rigs,
are rejected before removal or creation. Runtime reports `System.InvalidOperationException`; Inspector
commands report a Console warning. The guide distinguishes runtime's absence of Undo and automatic mode
changes, its explicit `previewInEditMode` opt-in, and the Inspector command's Undo/Pose/preview behavior.
Allowed replacement still deletes complete point objects and ordinary non-rig descendants; unrelated
content must stay outside those disposable child branches.

The parent-owned canonical backlink is already present in `Assets/Neoxider/Docs/UI/UIMeshRig.md:31`,
with another editor-companion link near the runtime authoring description. No parent documentation edit
was made here.

The C-19 dependency is source-present in `Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigLayoutPreset.cs`,
`RemoveExistingPoints`: the complete replacement set is validated before either immediate or deferred
Destroy. This read confirms the source contract used by the updated guide, not executed acceptance.
Runtime source and its tests/docs remain owned by `Neo.UI` and were not changed by this worker.

### EDITOR-TEST-REMAINING-ISOLATION / B-20

The required addressed path already exists on `UIMeshRigMotionPreviewDriver`:

```csharp
internal static void StopPreview(UIMeshRigPointMotion motion)
```

In `Assets/Neoxider/Scripts/UI/Editor/UIMeshRigGraphicEditor.cs:160`, it unregisters/stops the supplied
motion and refreshes the shared update subscription. Existing
`Assets/Neoxider/Scripts/UI/Editor/AssemblyInfo.cs` grants `Neo.Editor.Tests` friend access. No duplicate
seam, public API or assembly-reference change was needed.

The primary owner's current `Assets/Neoxider/Tests/Edit/UI/UIMeshRigEditorTests.cs` already consumes
`StopPreview(motion)` for owned cleanup, preserves baseline counts/subscription, and authors a live foreign
preview sentinel whose flag/time survive stopping the fixture's other motion. Cleanup stops its two
owned motions individually. No test-isolation call to `StopAllPreviews` was added here.

The complete `Docs/Audits/2026-10-23/Neo.Editor.Tests.implementation.md` was read. It reports SOURCE-FINAL
with the three Shop provider-scope corrections, unique preference/timer keys and owned repaint/preview
isolation completed by the primary test actor. This owner does not edit those legacy Shop, repaint or UI
fixtures. The addressed preview dependency has been consumed; no API handoff remains pending.

## Preserved source and regression work

The reviewed original audit fixes remain in:

- `Assets/Neoxider/Scripts/UI/Editor/UIMeshRigGraphicEditor.cs`: preflight before layout mutation;
  protected deletion/root duplication rejection; source-parent duplication preserving pose/rest space;
  owner-scoped new keys including inactive copied descendants; owner Undo recording before the final
  authoring-mode change; safe handling of a rejected duplicate in Focus.
- `Assets/Neoxider/Scripts/UI/Editor/UIMeshRigMenu.cs`: `MarkChanged(selectable)` after in-place Image
  conversion retargeting, recording the prefab instance modification.
- `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIEditorAuthoringTests.cs` and its `.meta`: the original
  twenty-one cases remain; the three nested-pose cases are strengthened and three reference/Undo cases
  are added in the Snapshot3 follow-up. Current source has ten test methods / twenty-four expanded cases.
- `Assets/Neoxider/Docs/UI/Editor.md.meta`: existing unique metadata retained.

The initial implementation resume wrote the owned `Editor.md` correction and this completion receipt.
The Snapshot3 follow-up additionally updates the owned duplicate implementation and prefixed fixture,
as detailed below. No serialized field, prefab/scene, shared configuration, tracker or another actor's
receipt was changed. `UIMeshRigAdaptersEditor.cs` and `AssemblyInfo.cs` require no implementation change.
The optional immutable authoring-plan/executor framework remains a proposal in the historical plan;
it was not implemented or made a prerequisite.

## Authored acceptance — UNEXECUTED on the corrected source

The owned fixture's current twenty-four cases observe:

- Three strengthened adapter cases: a positive native source-pose precondition; unchanged source and
  parent; fixed local/world position, rotation and scale; copied rest data; local/world Reset Pose and
  Undo preserving the source. The original relative-position and rest-center oracles remain as well.
- Three added adapter cases: internal `Button.targetGraphic` remaps to the cloned Image; explicit
  Navigation retains the external Selectable; Undo removes only the duplicate and preserves the
  original references, external object and original point count.
- Six creation cases: both point creation paths allocate distinct keys across the owner's hierarchy.
- Three custom-key duplication cases and one inactive-descendant case: preserved original keys and unique
  copied owner-scoped bindings.
- Two ordinary deletion/layout Undo cases: original point/key and Setup mode restored.
- Two nested-rig and two root-point rejection cases: both hierarchies, original points/counts and mode
  preserved before any destructive authoring operation.
- One root-duplication case: no cloned renderer hierarchy.
- One prefab conversion case: the live Button target and stored target property modification both refer
  to the replacement rig; uniquely named temporary prefab assets are owned by the fixture's cleanup.

Primary-owned foreign-preview isolation acceptance remains with the primary actor; no current integrated
pass is claimed here. No unavailable `[NonParallelizable]` annotation was introduced. Runner exclusivity
and exact cleanup remain the contract.

## Snapshot3 UIEDITOR-NATIVE-NESTED-POSE follow-up

### Native evidence read before editing

Read the updated complete implementation contract/request, central relocation reservation map, the
three exact failure nodes and stacks from
`Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`, and the frozen s3
duplicate implementation. The current and frozen duplicate paths agreed. The native failures were:

| `DuplicateNestedPoint_PreservesParentPoseAndRestPose` argument | Recorded distance at old fixture line 74 |
| --- | --- |
| `Neo.UI.UIMeshRigGraphic` | `4.77597952`, required `< 0.0001` |
| `Neo.UI.UIMeshRigWorldRenderer` | `16.6385078`, required `< 0.0001` |
| `Neo.UI.UIMeshRigSpriteRenderer` | `16.6385078`, required `< 0.0001` |

Each stack identifies the owned prefixed fixture in the frozen u63 s3 project. These are existing failed
native results supplied by root, not executions by this worker and not results for the corrected source.

### Cause, bounded correction and preserved contract

Inspected the actual `UIMeshRigPoint` serialized rest fields, `CaptureRestPose`/`ResetPose`, owner
world/normalized/local conversions, mode handling and point synchronization for all three adapters.
The nested branch's Reset Pose uses its serialized local rest TRS; it must not be recaptured from the
current deformation. The old editor helper selected the correct parent but relied entirely on the
implicit transform result of `Instantiate(source, parent)` and creation/Undo bookkeeping. It neither
supplied the authored world pose nor restored the captured local pose afterward. Thus its same-parent
fix still returned the multi-unit position drift observed in s3.

The numeric errors are consistent with lost authored offsets, not floating-point tolerance: under parent
scale `(1.5, 0.8)`, the length of scaled pose delta `(3, -2)` is approximately `4.77598`; the length of
scaled current local position `(10, 9)` is approximately `16.63851`. Parent rotation preserves these
lengths. XML records distances, not the individual positions at every native callback, so it does not
prove which internal creation/Undo callback reapplied a rest/origin position. No engine-wide defect or
bare-Unity baseline is claimed.

Changed only `Assets/Neoxider/Scripts/UI/Editor/UIMeshRigGraphicEditor.cs`, `DuplicatePoint`, for the
production correction: capture the source's local position/rotation/scale before creation; instantiate
with its explicit world position/rotation and the original parent; register the created object and a
complete Undo record for its Transform; apply the captured local TRS after creation bookkeeping. This
enforces the authored pose under rotated/nonuniform parents without modifying the parent or source,
recapturing rest data, or rebuilding objects/components by hand. Existing cloning still remaps internal
serialized references and preserves external ones. Existing key allocation and dirty/prefab recording
remain intact. The explicit-pose/parent overload and cloning semantics were checked against
[Unity 6.3's Object.Instantiate API](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/object/instantiate).

In the owned prefixed fixture, the original arrangement and every original oracle remain. New assertions
fix the expected world position before duplication, require Pose mode and the authored local TRS as
native preconditions, then independently compare both source and copy to that baseline. Parent identity,
position, rotation and scale, local/world rest restoration and serialized rest getters are checked.
The positional tolerance remains `0.0001`; no precision limit, comparison, count or negative oracle was
relaxed. Three helper-free cases additionally cover real built-in UI serialized reference remapping and
Undo across all three adapters. No new MonoBehaviour/test-helper class or metadata file was introduced.

The reservation map contains neither changed source path; the fixture embeds no Mono helpers. Legacy
`UIMeshRigEditorTests.cs`, its raycastPadding failure, shared asmdefs, `Neo.Tests.Fixtures`, runtime-owned
source/docs and all reserved relocation files were left to their owners. No canonical doc edit is needed
for this follow-up: Duplicate's documented parent/pose/rest guarantee is unchanged; this implements it.

Actual follow-up work: source/XML/stack/vendor-API reads, focused source editing and source-diff review,
regression-source authoring, and this receipt update. No Unity/compiler/checker/test process was started.
The existing frozen s3 failure evidence remains failed historical evidence. Root must open a new freeze
after every writer stops, then rerun the three strengthened cases and the complete twenty-four-case
owned fixture, including the added serialized-reference/Undo cases. Callback attribution and successful
native correction remain unverified until that run. This owner is source-final and stops source writes.

## Compatibility, actual work and remaining evidence debt

No public/serialized editor contract changed in this resume. The guide documents the runtime owner's
intentional failure for previously unsafe replacement hierarchies; ordinary disposable point replacement
remains supported. Runtime retains no Undo or implicit authoring-mode switch. No asset migration,
automatic reparenting, sample-layout change or new dependency was introduced.

Actually performed: complete implementation-contract/request and historical audit/plan reads; current
owned guide/source-diff and regression declaration reads; runtime guard/backlink reference reads; complete
primary test receipt read; addressed-stop/friend-access and fixture-consumption inspection; the scoped
documentation edit and receipt bookkeeping. Reads used small cmd.exe/Python calls.

No Unity/MCP, compiler, test runner, product checker/validator, install, service, commit/push or child
agent was launched. No current validation pass is claimed. Historical audit static checks predate this
source/doc snapshot and are not reused as integrated evidence.

After EVERY writer finishes, root's existing verification operator must compile/discover/execute the
frozen Unity 6.3/6.6 profiles, including the owned fixture and primary mesh-rig editor fixture. Remaining
evidence debt includes native Undo/Redo, actual saved-scene/prefab reconstruction, isolated Undo grouping,
preview lifecycle and both existing Unity-version branches. The owned prefab case observes property
modifications, not a completed disk round trip. Runtime C-19 Edit/Play/deferred-destruction evidence is
the runtime owner's slice. No execution was attempted while the source barrier is closed.
