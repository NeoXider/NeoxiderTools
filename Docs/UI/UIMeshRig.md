# UI Mesh Rig

**What it is:** a sprite deformation rig in `Scripts/UI/MeshRig/` (namespace `Neo.UI`): child
`UIMeshRigPoint` transforms act as bones, and a renderer-agnostic core (`UIMeshRigGeometryBuilder`) bends a
grid mesh for uGUI, UI Toolkit, a world mesh or a `SpriteRenderer`.

**How to use:**
1. Create a rig from the menu (see Quick start) or add an adapter component and assign a Sprite.
2. Apply a layout (`UIMeshRigLayoutBuilder.Apply`, or the Inspector button) or add `UIMeshRigPoint` children.
3. Animate the points with an Animator, with `UIMeshRigPointMotion` presets, or from code with
   `SetProceduralPose`.

---

UI Mesh Rig deforms a single Sprite in four environments from one renderer-agnostic core:

- `UIMeshRigGraphic` — a standard `MaskableGraphic` inside a uGUI Canvas;
- `UIMeshRigElement` — a custom `VisualElement` for UI Toolkit/UXML/UI Builder;
- `UIMeshRigWorldRenderer` — `MeshFilter` + `MeshRenderer` for a scene without a Canvas;
- `UIMeshRigSpriteRenderer` — a plain `SpriteRenderer` (sorting layers, 2D lights, sprite masks, SRP batching).

The `UIMeshRigGeometryBuilder` core builds vertices, indices and UVs the same way everywhere, and computes
elliptical influence, falloff, pose and procedural motion. The adapters only translate the finished geometry
into the API of their environment. That is why `Columns`, `Rows`, `Preserve Aspect`, `Deformation Enabled`,
Sprite/Color and motion presets mean the same thing in every variant.

## Quick start

- uGUI: `GameObject > UI > Neoxider UI Mesh Rig`.
- UI Toolkit host: `GameObject > UI Toolkit > Neoxider UI Mesh Rig`.
- UI Toolkit UXML: `Assets > Create > Neoxider > UI Mesh Rig (UI Toolkit UXML)`, or add
  `UIMeshRigElement` in UI Builder from `Library > Custom Controls > Neoxider > UI Mesh Rig`.
- World mesh: `GameObject > 2D Object > Neoxider UI Mesh Rig (World)`.
- SpriteRenderer: `GameObject > 2D Object > Neoxider UI Mesh Rig (Sprite Renderer)`.

Every menu item creates a visible NeoLogo, a sensible grid, and a preset that is already moving. All
inspectors use the shared Module header. Points are edited in the Scene View: Setup changes the bind pose,
Pose / Animate changes the current deformation. `Capture Rest Pose` takes the current pose as the neutral
one, `Reset Pose` returns to the bind pose.

## Inspectors and Scene gizmos

See [UI Mesh Rig editor tools](./Editor.md) for creation/conversion commands, Undo, point-key authoring,
duplication and the Inspector replacement policy. Runtime replacement is described below.

Component fields are declared with ordinary `[Header]` / `[Tooltip]` and drawn by `CustomEditorBase` — the
same mechanism that gives the rest of the package its collapsible sections with counters, ON/OFF toggles and
colored bars. The custom editors add only what attributes cannot express: the `Apply Layout & Preview`
button, grid diagnostics, the point list and Scene handles. Inherited uGUI fields (`Raycast Target`,
`Raycast Padding`, `Maskable`, `Material`, `Color`) are visible in the common pass and are no longer hidden
in a collapsed `Advanced Rig Controls` foldout; the `Script` field is neither hidden nor moved — it is the
only way to repair a component whose script reference was lost.

The authored `Raycast Padding` value is now kept in a hidden field: the visible field of the rig is
recomputed every frame to fit the deformed mesh, so there is no longer a pair of "two Raycast Padding fields
in a row, one of which is meaningless". Editing the visible field by hand is picked up as the new authored
value.

The Scene View has a rig overlay panel: a `Setup` / `Pose / Animate` switch, a tool selector
(Move / Rotate / Scale) and two readability toggles — `Labels` and `All rings`. By default, unselected points
draw a single pale outer ring without a label, so seven points no longer turn into a mess of fourteen
ellipses and seven overlapping labels. The bind-pose handles (anchor and radii) are available in both modes,
not only in Setup; radii are dragged from four sides (±X and ±Y), and the anchor is noticeably larger and has
a dark contrasting ring.

> `Handles.Label` does not obey `Handles.color`: labels are drawn through a GUI style, so their transparency
> is set by their own `GUIStyle.normal.textColor`, not by the alpha of `Handles.color`.

## Edit Mode preview

`UIMeshRigPointMotion` can animate a point in Edit Mode, without entering Play Mode. The transport is
available both in the Mesh Rig Point inspector and on the motion component itself: `Start Preview`,
`Pause` / `Resume`, `Restart Preview`, `Stop Preview`.

The preview is strictly transient. It writes only a procedural pose that is composed on top of the point
Transform at draw time; it never writes the point's `Transform`, never changes the serialized authoring mode
of the rig (`Setup` / `Pose / Animate`), and is never serialized into the scene or a prefab. `Stop Preview`
restores the point exactly as it was.

Because the preview holds a procedural pose, a previewed point is not re-bound by the rig while it is
previewing: dragging such a point in `Setup` mode moves it without moving its bind anchor. Stop the preview
to edit the bind pose.

The editor subscribes to `EditorApplication.update` only while at least one preview is actually running, and
it stops every preview whose object is no longer selected. That check runs on selection change and again
whenever a rig inspector is disabled, so closing or re-targeting an Inspector cannot orphan a preview.
Previews are also stopped by assembly reload / recompile, by entering or leaving Play Mode, and on editor
quit. A preview therefore cannot keep the editor busy in the background or keep the scene permanently dirty.

A running preview steps at most sixty times a second and only repaints the Scene view. It never calls
`EditorApplication.QueuePlayerLoopUpdate()`: queuing a player loop update from an editor tick schedules
another tick, which re-enters the driver, which queues another — a loop with no exit while the preview is on.

Previews never run on prefab assets (persistent objects) and never run in Play Mode — in Play Mode the
component's own `Play On Enable` / `Play()` drives the motion instead.

## uGUI

`UIMeshRigGraphic` keeps its previous workflow and public API. Child `UIMeshRigPoint` objects are
`RectTransform`s, so Position/Rotation/Scale can be recorded by an ordinary Animator or Timeline. The context
menu of a Simple `Image` supports in-place and non-destructive conversion. Raycast modes:

- `Rect` — the original RectTransform;
- `Deformed Mesh` — the actual deformed mesh;
- `Sprite Alpha` — Sprite transparency (requires Read/Write Enabled, otherwise it safely falls back to the mesh).

## UI Toolkit

`UIMeshRigElement` is declared as a Unity 6 custom control through `[UxmlElement]` on a `partial` class and
`[UxmlAttribute]` on its properties. It draws in `generateVisualContent`, allocates data through
`MeshGenerationContext.Allocate(...)` and fills `Vertex.position`, `Vertex.tint` and `Vertex.uv`. The adapter
honors `MeshWriteData.uvRegion`, so the texture is correct when UI Toolkit places it into an atlas. In
Unity 6.3 the remap is performed automatically, but reading the region is kept for compatibility across the
Unity 6.x branch.

For UXML/UI Builder, use the element directly. `UIMeshRigUIToolkitHost` is an optional scene wrapper: it
creates the element and sets Sprite, Size, Position, grid, preset and motion.

**The host works through `PanelRenderer`, not through `UIDocument`.** Starting with Unity 6.5, world-space
UI Toolkit is rendered by `PanelRenderer`, so the host first looks for it on its own GameObject and subscribes
to `RegisterUIReloadCallback` — the element is added to the root that the renderer provides and migrates on
every reload of the tree. `UIDocument` remains only a fallback for editors where `PanelRenderer` does not exist
yet (verified: the `PanelRenderer` type is absent before Unity 6000.5 — it is missing in 6000.3 and 6000.4,
so the version switch is closed behind `#if UNITY_6000_5_OR_NEWER`, written once in `UIMeshRigPanelCompat`). `[RequireComponent(typeof(UIDocument))]` has been
removed: it forced a legacy component onto projects that had already moved away from `UIDocument`. The current
binding is shown by `Host Kind` in the inspector; if `PanelRenderer` has not yet provided a root, the inspector
says so explicitly instead of staying silent.

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:neo="Neo.UI">
    <neo:UIMeshRigElement name="rig" layout-preset="Character"
        style="width: 300px; height: 300px;" />
</ui:UXML>
```

## World

`UIMeshRigWorldRenderer` works without a Canvas. It writes the same geometry into the runtime `Mesh` of a
`MeshFilter` component, and Sprite/Color into the `MeshRenderer`. Child `UIMeshRigPoint` and
`UIMeshRigPointMotion` components are reused without a copy of the deformation code. `Pixels Per Unit`
converts the pixel-authored amplitudes of motion presets into world units.

`UIMeshRigWorldRenderer` remains for cases that need a custom material or shader, and an arbitrary size and
pivot independent of the sprite import settings.

## SpriteRenderer

`UIMeshRigSpriteRenderer` deforms the artwork while keeping a plain `SpriteRenderer`: sorting layers, 2D
lights, sprite masks and SRP batching keep working. The size is taken from the Sprite itself
(`rect / pixelsPerUnit`), and its `Pixels Per Unit` converts the pixel-authored amplitudes into world units.

**The imported asset is never modified.** The component creates a runtime clone (`Sprite.Create` from the
texture and the `textureRect` of the original), writes the geometry into the clone and hands the clone to the
renderer; on `OnDisable` the clone is destroyed and the original Sprite is returned to the renderer. The asset
is shared project state: an edit would survive exiting Play Mode and would silently corrupt the sprite across
the whole project.

**Why not `Sprite.OverrideGeometry`.** The method is public and does not require 2D Animation, but on a sprite
that is not backed by an import it silently does nothing: a call on a runtime clone leaves both the vertex
count and the vertex positions unchanged (measured in a live 6000.3.14f1 editor — 173 vertices before and
after). The only way to make it work is on an imported asset, that is, exactly the mutation of shared state
that the adapter must avoid. Therefore the public `UnityEngine.U2D.SpriteDataAccessExtensions`
(`SetVertexCount` / `SetVertexAttribute` / `SetIndices`) is used: it writes positions, UVs and indices into any
Sprite instance, lives in `UnityEngine.CoreModule` and requires no extra packages. The renderer picks up the
new geometry without reassigning `SpriteRenderer.sprite` (verified by rendering before and after the rewrite).
`SpriteRendererDataAccessExtensions.SetDeformableBuffer`, for comparison, is `internal` — a package cannot rely
on it.

**Bounds Headroom.** `Sprite.bounds` is computed from `rect / pixelsPerUnit` and does not grow together with
the written geometry, so a heavily deformed sprite can be culled at the edge of the screen. The
`Bounds Headroom` field (0.25 by default) creates the clone with a proportionally smaller PPU: only the bounds
grow, while the vertices stay in honest world units.

The Draw Mode of the `SpriteRenderer` must be `Simple`: `Sliced` and `Tiled` build their own geometry and
overwrite the deformation. The inspector warns about this explicitly.

## Influence and motion

A point has two independent ellipses: inside INNER the full weight applies, outside OUTER the weight is zero,
and between them the Falloff Curve is applied. `UIMeshRigPointMotion` adds a procedural pose on top of the
Transform and does not overwrite Animator keys. Presets: Float, Breathe, BodySway, HeadSway, SoftJiggle, Pulse,
SquashStretch, Wave and Noise. Shared layouts: SimpleBounce, Character and FlagCloth.

## Runtime API

```csharp
uguiRig.SetSource(sprite, Color.white);
uguiRig.SetGridResolution(16, 20);
UIMeshRigLayoutBuilder.Apply(uguiRig, UIMeshRigLayoutPreset.Character);

worldRig.SetSource(sprite, Color.white);
worldRig.SetSize(new Vector2(3f, 3f));
UIMeshRigLayoutBuilder.Apply(worldRig, UIMeshRigLayoutPreset.FlagCloth);

spriteRig.SetSource(sprite, Color.white);
UIMeshRigLayoutBuilder.Apply(spriteRig, UIMeshRigLayoutPreset.SimpleBounce);
spriteRig.Rebuild(); // immediate clone rebuild, without waiting for LateUpdate

UIMeshRigElement element = new UIMeshRigElement
{
    Sprite = sprite,
    Columns = 16,
    Rows = 20,
    LayoutPreset = UIMeshRigLayoutPreset.Character
};
document.rootVisualElement.Add(element);
```

Layout centers are normalized to the aspect-fitted sprite area. With `Preserve Aspect` enabled, a wide
or tall sprite may occupy only part of the RectTransform; both the C# layout builder and Inspector layout
actions place points inside that drawing area. Existing authored points keep their saved bind poses;
reapplying a layout replaces points only when `replaceExisting` is true.

When a point itself is reparented between rigs, both owners' point lists are invalidated, including points
below intermediate containers. The nearest ancestor implementing `IUIMeshRigOwner` becomes the owner.
Adding an owner component to an object between a rig and its points (for example `AddComponent` on a
grouping object at runtime) moves those points to the new nested rig, and removing that component hands them
back to the enclosing rig: both rigs re-collect their points, and each point sends its procedural pose to its
new owner. Disabling a nested owner does not change ownership; its points stay with it.

### Runtime layout replacement

`UIMeshRigLayoutBuilder.Apply(...)` and `ApplyToOwner(...)` replace existing owned point GameObjects by
default. The whole removal set is checked before creating a point, changing binding data or scheduling any
`Destroy`, including Play Mode's deferred destruction. A point on the owner's root or an owned point branch
containing another `IUIMeshRigOwner` is rejected with `System.InvalidOperationException`. Inactive nested rigs
are protected too. A rejected call leaves both hierarchies, binding keys, point counts and authoring modes
unchanged. Move the root point to a child or move a nested rig outside the replaced point branch first.

Ordinary disposable child points are still replaced. Separately owned rigs under grouping objects outside
the removed branches remain intact. Other non-rig content inside a removed point GameObject is still part
of that deletion, so keep independent content outside disposable point branches. Use `replaceExisting: false`
to append a layout without removing any existing branch:

```csharp
UIMeshRigPoint[] added = UIMeshRigLayoutBuilder.Apply(
    uguiRig, UIMeshRigLayoutPreset.SimpleBounce, replaceExisting: false);
```

This preflight is shared by the runtime overloads for all three scene renderer adapters. Runtime calls do
not provide editor Undo and do not switch the authoring mode or start the Inspector's preview workflow.
The [editor companion](./Editor.md) describes those additional authoring operations.

## Performance

An animated rig rebuilds its mesh every frame, so the steady-state rebuild path does not allocate:

- Every adapter keeps one `UIMeshRigGeometry` and rebuilds into it through
  `UIMeshRigGeometryBuilder.Build(..., reuse)`. The overload writes into `reuse` when its arrays already have
  the grid's sizes and returns it; otherwise it returns a new geometry.
- The public `BuildGeometry()` methods still return new arrays the caller owns, so a returned geometry is
  never overwritten by a later rebuild.
- `UIMeshRigWorldRenderer` uploads the index buffer only when the grid topology changes; a pose change writes
  positions only. UVs are uploaded again when the grid or the sprite's outer UV changes (a sprite swap on the
  same grid included), and the `MaterialPropertyBlock` is refreshed only after a source change.
- `UIMeshRigSpriteRenderer` rewrites only the clone's positions on a pose change. UVs and indices are
  uploaded when the clone is created or the grid changes (including a transposed grid with the same vertex
  count), and the temporary native buffers it writes through come from `Allocator.Temp`, not the managed heap.
- `UIMeshRigElement` reuses its geometry, vertex and index buffers and evaluates layout motion from shared,
  read-only preset profiles instead of creating new `AnimationCurve`s for every point on every repaint. Its
  16 ms repaint timer runs only while the pose depends on time: with `MotionSpeed = 0`, no sprite or
  deformation off it repaints once per property change instead.
- `UIMeshRigOwnerResolver.Find` walks the ancestors with `TryGetComponent`, and the adapters refill their
  point cache into a reused list, so owner lookups do not allocate either.

```csharp
UIMeshRigGeometry buffer = null;
// Each frame: the same instance comes back once the grid size is stable.
buffer = UIMeshRigGeometryBuilder.Build(columns, rows, space, uvRect, true, pointStates, buffer);
```

Remaining cost scales with `Columns x Rows x points`; keep the grid as coarse as the silhouette allows.
`Tests/Edit/UI/UIMeshRigAllocationTests.cs` guards the zero-allocation paths of every adapter (uGUI
populate, world renderer, SpriteRenderer, UI Toolkit element) and the owner lookup with Unity's `GC.Alloc`
counter (`Neo.Tests.Performance.GcAllocCounter`).

## Demo

Import the `Demo Scenes` sample and open `Demo/UI Mesh Rig/UIMeshRigDemo.unity`. The top row keeps the three
previous uGUI workflows (static pose, procedural motion, Animator). The bottom row shows the four output
adapters side by side: uGUI Simple Bounce, UI Toolkit Character, world Flag Cloth and
`[Demo] World/Sprite Renderer - Character` (`UIMeshRigSpriteRenderer` with the Character layout).
The Animator card moves only in Play Mode: Unity does not play Animator clips outside Play Mode, while the
neighboring cards keep moving in the editor thanks to the edit-mode preview of `UIMeshRigPointMotion`.
The collapsed info card in the top-left corner (`[Demo] Info Card`, expand it with `+` in Play Mode) names the
objects to select and the equivalent C# calls. The sprite-renderer adapter and the info card are added by the
editor builder `UIMeshRigDemoBuilder`; the hand-authored cards and adapters are left untouched.

## Limitations

- uGUI conversion targets `Image.Type.Simple`; Sliced, Tiled and Filled have different geometry.
- UI Toolkit uses `ushort` indices; the current 40x40 limit is well below that cap.
- `UIMeshRigSpriteRenderer` requires `Draw Mode = Simple`; `Sliced` and `Tiled` rebuild the geometry themselves.
- The bounds of the clone grow only by `Bounds Headroom`; increase the value for extreme deformation.
- For a multi-layer character, IK and a large set of skeletal clips, a dedicated 2D rig is a better fit.
- A dense grid and many moving points raise the rebuild CPU cost (no GC); measure with the Profiler.
