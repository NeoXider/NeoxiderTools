# Neo.UI.Editor: audit

Status: source audit and scoped fixes complete; Unity compilation and Edit Mode execution pending with coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/UI/Editor/Neo.UI.Editor.asmdef`.
- Scope: 4 C# files; nested assemblies excluded.

## Scope

Read `AGENTS.md`, the complete worker contract, this assembly's exact manifest entry and the initial report.
Reviewed every line of all four inventoried C# files (2,093 lines before edits). No nested assembly source
was edited. Runtime implementations and existing tests were read only as dependency context.

| Owned file under `Assets/Neoxider/Scripts/UI/Editor/` | Review coverage | Result |
| --- | --- | --- |
| `AssemblyInfo.cs` | Friend assembly visibility and existing test wiring. | Correct `Neo.Editor.Tests` access; unchanged. |
| `UIMeshRigAdaptersEditor.cs` | World, SpriteRenderer and UI Toolkit inspectors; native size Undo; selection and preview cleanup; panel diagnostics; delegation to shared authoring code. | No separate demonstrated defect; unchanged. |
| `UIMeshRigGraphicEditor.cs` | Preview events, throttling, pause/stop, persistent objects, reload/Play Mode/quit, inspector lifecycle, diagnostics, Scene handles, point ownership, rest/posed coordinates, creation/deletion/duplication, keys, dirty/prefab recording and Undo. | Fixed hierarchy safety, duplicate coordinates, key collisions and layout mode Undo. |
| `UIMeshRigMenu.cs` | All creation menus and UXML output; Canvas/EventSystem fallback; package sprite lookup; PanelSettings ownership; Image conversion, unsupported types, serialized graphic settings, Selectable targets, Undo and Unity version switch. | Fixed prefab recording of in-place conversion retargeting. |

The editor-only asmdef and its existing `Neo.UI` / `Neo.Editor` references were inspected without edits.
No public signatures or serialized fields were renamed. New warnings occur only for explicit invalid
authoring commands. Existing preview throttling, transient motion state and shared attribute-driven
inspectors were retained.

Reuse receipt: checked the `neoxider-tools` extension/tool/system references for hierarchy, editor and mesh
rig helpers. No matching binding-key allocator or guarded point deletion service was present. The small
owned helpers reuse `UIMeshRigOwnerResolver`, `IUIMeshRigOwner`, `ObjectNames.GetUniqueName`, existing
`MarkChanged` and Unity Undo. No package or assembly dependency was added. `unity-game-agent` was applied
as a file-only workflow; user restrictions exclude Unity preflight/execution and child agents. This is a
local source review, not independent runtime QA.

## Findings and fixes

Evidence below is a source-level demonstration against the reviewed baseline. Regression assertions have
been added but were not executed in Unity during this worker task.

### F1 — High: point operations can destroy a nested rig or the owner itself

- Trigger: a point's GameObject contains another rig, including an inactive rig; alternatively, a point
  component is attached to the owner GameObject itself.
- Baseline source: `UIMeshRigEditorUtility.ApplyLayout` enumerated all points and tested nearest ownership,
  then destroyed each owned point's entire GameObject. Skipping nested-owner points did not protect their
  ancestor GameObject. `DeletePoint` likewise destroyed the entire object. The resolver accepts an owner
  on the starting Transform, so a root point passed this ownership test and deleted the renderer and all
  its descendants. Duplicating a root point cloned the owner hierarchy rather than a child control point.
- Observable consequence: unrelated nested rig data disappears; root deletion invalidates the owner
  before subsequent notifications/layout construction; root duplication creates an unintended renderer.
- Fix: `CanDeletePoint` rejects root points and point branches containing another `IUIMeshRigOwner`,
  including inactive descendants. Layout replacement validates every owned point before any Undo record,
  deletion or mode change. Individual deletion uses the same guard. Root duplication is rejected, and
  `Focus(null)` safely handles that rejected result in the existing Inspector action path. Foreign-owner
  points are not accepted for deletion/duplication. An explicit Console warning explains how to move the
  relevant object before retrying; no modal dialog is introduced.
- Source: `UIMeshRigGraphicEditor.cs`, `ApplyLayout`, `CanDeletePoint`, `DeletePoint`, `DuplicatePoint`, `Focus`.
- Regressions: both Delete and Apply Layout preserve nested rig objects, all original controls and mode;
  both operations preserve a root point and owner; root duplication creates no child hierarchy. Ordinary
  deletion/layout still run and can be undone.

### F2 — Medium: duplicating a nested point changes its pose and rest coordinate space

- Trigger: Duplicate on a point beneath a translated, rotated or scaled grouping object.
- Baseline source: `DuplicatePoint` instantiated the point under `rig.RigTransform`, copying a local
  Transform authored relative to a different parent. Serialized local rest values were copied as well.
- Observable consequence: the duplicate jumps and Reset Pose no longer returns to the original rest
  position. In the simple translation-only case, a group at `(40, -30)` and a point at local `(7, 11)`
  place the original at `(47, -19)` while the old duplicate appears at `(7, 11)` relative to the rig.
  This is a coordinate-space trace, not a claimed Unity execution result.
- Fix: instantiate under the source's existing parent and choose a name among the actual siblings.
- Source: `UIMeshRigGraphicEditor.cs`, `DuplicatePoint`.
- Regressions: uGUI, world and SpriteRenderer cases compare parent, world position, rotation, scale and
  rest center; Reset Pose restores the copied rest position; Undo removes only the duplicate.

### F3 — Medium: creation and duplication introduce ambiguous binding keys

- Trigger: an existing nested point owns the default new-point key; a custom key matches a duplicate's
  generated GameObject name; or a duplicated point branch contains descendant points.
- Baseline source: default creation used only immediate sibling names; preset creation used `layout.Name`;
  duplication assigned the new root name but left descendant keys copied from their originals.
- Observable consequence: the editor itself creates duplicate-key diagnostics and ambiguous identifiers
  for code consuming `BindingKey`, even when the input hierarchy had unique keys.
- Fix: choose each new key against the complete owner-scoped point list, excluding that point itself.
  Both creation overloads use this allocator. Duplication refreshes the point cache and allocates keys for
  every copied point belonging to the original owner, including inactive descendants. Keys belonging to
  separately owned nested rigs remain in their independent scope. Existing points are not renamed.
- Source: `UIMeshRigGraphicEditor.cs`, both `CreatePoint` overloads, `DuplicatePoint`, `GetUniqueBindingKey`.
- Regressions: both creation paths across all three Transform adapters; custom-key collisions across those
  adapters; an inactive copied child retains its enabled state and receives a distinct key. The copied
  branch's entire owner-scoped key set must remain unique.

### F4 — Medium: Undo of layout replacement can leave the new authoring mode

- Trigger: apply a layout while the rig is in Setup, then Undo.
- Baseline source: `RecordRig` ran before point creation, while `SetAuthoringMode(Pose)` ran after it.
  Point creation uses `Undo.RegisterCreatedObjectUndo`, which ends earlier `Undo.RecordObject` tracking;
  therefore the initial record did not cover the later mode mutation. This source inference follows
  [Unity's documented Undo recording lifecycle](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Undo.RegisterCreatedObjectUndo.html).
- Observable consequence: previous point objects can be restored while the rig remains in Pose.
- Fix: record the owner again immediately before the final mode assignment, within the existing group.
- Source: `UIMeshRigGraphicEditor.cs`, final mode assignment in `ApplyLayout`.
- Regression: ordinary layout replacement must enter Pose, then Undo must restore the authored key,
  original point count and Setup mode. The deletion branch also checks normal Undo behavior.

### F5 — Medium: in-place Image conversion omits Selectable prefab override recording

- Trigger: convert a Simple Image on a prefab instance whose Button/Selectable targets that Image.
- Baseline source: the converter assigned the replacement rig to `Selectable.targetGraphic` after
  `Undo.RecordObject`, but its final `MarkChanged` call covered only the GameObject, rig and Transform.
  The Selectable never reached `PrefabUtility.RecordPrefabInstancePropertyModifications`. The sibling
  non-destructive conversion path already included the required `MarkChanged(selectable)` call.
- Observable consequence: the visible target exists in live memory without an explicit stored prefab
  property override, risking a lost target when the instance is reconstructed.
- Fix: use the same existing `MarkChanged(selectable)` primitive after in-place retargeting.
- Source: `UIMeshRigMenu.cs:199`.
- Regression: create an isolated temporary Button prefab, instantiate and convert it, then assert both
  the live target and its `m_TargetGraphic` property override reference the replacement rig. The test
  cleans up its own uniquely named prefab asset and created GameObjects.

## Documentation

- Added the assigned narrow topic `Assets/Neoxider/Docs/UI/Editor.md` and a unique GUID `.meta`; no existing
  editor topic was present. It links the canonical runtime guide rather than duplicating its full API.
- Covers menu paths, adapter/host configuration, point workflows, nested/root safeguards, key scope,
  preview lifecycle, Undo and Simple Image conversion/prefab targeting.
- Explains the public C# path using `UIMeshRigLayoutBuilder`, `IUIMeshRigOwner` and motion methods, without
  exposing internal editor utilities as a supported API. The example uses a fresh rig and additive layout
  construction. It distinguishes runtime replacement from the editor safeguards added here.
- Coordinator action: link this new topic from `Assets/Neoxider/Docs/UI/UIMeshRig.md` or the module entry;
  those files belong to another assembly worker and were not edited.

Кратко: проверены все четыре файла редакторской сборки. Исправлены удаление чужого вложенного рига,
ошибки копирования позы и ключей, восстановление режима через Undo и сохранение цели кнопки в prefab.
Проверки Unity не запускались; их выполняет координатор.

## Substantial improvement proposal

**Proposed, not implemented: validated authoring plans shared by runtime policy and editor transactions.**

**Problem.** The runtime builder and editor utility separately decide which points to replace and how to
assign keys. The editor currently offers only internal mutation helpers; callers cannot inspect a change
before destructive work. Fixing the editor leaves the runtime replacement hazard described below. More
complex point branches, prefab overrides and complete Undo/Redo require the same rules at several call sites.

**Architecture / public API.** Reuse `IUIMeshRigOwner` rather than adding a renderer interface. Introduce
plain C# point descriptors and an immutable `UIMeshRigAuthoringPlan` containing planned point additions,
removals, key remaps and typed diagnostics. A side-effect-free planner validates ownership, root points,
nested owners and duplicate keys before mutation. Proposed entry points:

```csharp
UIMeshRigAuthoringPlan PrepareLayout(IUIMeshRigOwner owner, UIMeshRigLayoutPreset preset,
    UIMeshRigReplacePolicy policy);
UIMeshRigAuthoringPlan PrepareDuplicate(IUIMeshRigOwner owner, UIMeshRigPoint source);
UIMeshRigAuthoringResult Apply(UIMeshRigAuthoringPlan plan);
```

Place common validation/key policy in the runtime assembly, with scene objects resolved by adapters;
keep Unity Undo and prefab override recording in an editor executor. Plans should expose issues and key
maps for both C# tools and optional Inspector UI. The executor validates that the captured hierarchy still
matches before applying a plan, opens one isolated Undo group and records newly created/configured objects
at the correct points in Unity's recording lifecycle. No ScriptableObject should store scene references.

**Compatibility / migration.** Keep current layout builder signatures and serialized fields. Initially
route only editor commands through the executor; add runtime validation overloads and a deliberate
replacement policy. Preserve existing authored keys. Document a migration for the currently unsafe root
and nested-owner replacement cases instead of changing arbitrary hierarchy relationships automatically.
No mandatory new scene component or NoCode chain is required.

**Acceptance scenarios.** Plan construction changes neither scene state nor preview state; all adapters
produce identical owner-scoped key decisions; protected hierarchies fail without partial mutation; nested
rig scopes survive; inactive branches duplicate correctly; stale plans reject execution; one Undo and
Redo restore complete hierarchy, poses, mode and keys; prefab save/reload retains retargeted graphics.
Compile and execute these cases on both sides of the Unity 6.5 API split.

**Effort / risk.** Approximately 2–4 developer days plus a Unity version verification pass, assuming the
existing owner interface remains sufficient. Medium risk: live hierarchy changes between planning and
execution, Unity object identity, Undo creation snapshots and prefab modification persistence. Coordinate
with the `Neo.UI` owner before moving any common policy; this proposal makes no cross-boundary code edit.

## Verification and remaining risks

Added `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIEditorAuthoringTests.cs` and its unique GUID `.meta`:
9 test methods, 21 expanded test cases. Assertions observe hierarchy, poses, binding keys, authoring mode,
Undo state and prefab property modifications. The only reflection invokes the existing private context-menu
entry point, as in the established editor test workflow. No existing test file was edited.

Actual checks run successfully:

- Scoped `git diff --check --` for all four owned source files, the new test/doc and metadata, and this report.
- Read-only Python consistency checks: actual source inventory equals the four manifest entries, using
  nearest asmdef ownership; strict UTF-8 decoding, no replacement characters and no trailing whitespace
  across the nine scoped text files; balanced C# delimiters/preprocessor guards and no `var` declarations
  or obsolete Unity object identity calls in the five audited/new C# files. These checks are not compilation.
- Test inventory confirms 9 methods / 21 cases and the required filename prefix. Existing test asmdef
  references include `Neo.UI`, `Neo.UI.Editor`, `Neo.Editor`; existing friend access is sufficient.
- Both new package metadata GUIDs occur exactly once under `Assets`; owned guide relative file links resolve.

No Unity/MCP, build, compiler, test runner, install, child agent, commit, shared config or scene/prefab edit
was performed. The temporary prefab exists only in the unexecuted regression's future test workflow.
Console, visual behavior, native Undo/Redo and prefab round trips are not claimed verified. Both branches
of the existing Unity 6.5 UXML API switch still require central compilation; no new conditional branch or
identity API was added. Coordinator should run the new fixture and existing `UIMeshRigEditorTests`.

### Cross-boundary action for coordinator / Neo.UI owner

**High, source-confirmed:** `Assets/Neoxider/Scripts/UI/MeshRig/UIMeshRigLayoutPreset.cs:333`,
`UIMeshRigLayoutBuilder.RemoveExistingPoints`, still destroys the whole GameObject of each owned point
without checking for a root point or nested rig descendants. Its default runtime replacement path can
therefore delete the owner or a nested rig for the same hierarchy shapes fixed in the editor. This runtime
file is excluded from ownership and remains untouched. Apply a compatible safety policy there and add
runtime regressions; this worker's editor guards must not be interpreted as a runtime fix.
