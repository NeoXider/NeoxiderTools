# UI Mesh Rig

**What it is:** a sprite deformation rig in `Scripts/UI/MeshRig/` (namespace `Neo.UI`): child
`UIMeshRigPoint` transforms act as bones, and a renderer-agnostic core (`UIMeshRigGeometryBuilder`) bends a
grid mesh for uGUI, UI Toolkit, a world mesh or a `SpriteRenderer`.

**How to use:**
1. Create a rig from the menu (see Quick start) or add an adapter component and assign a Sprite.
2. Apply a layout (`UIMeshRigLayoutBuilder.Apply`, or the Inspector button) or add `UIMeshRigPoint` children.
3. Animate the points with an Animator, with `UIMeshRigPointMotion` presets, or from code with
   `SetProceduralPose`. The UI Toolkit element has no point objects: pose its layout points from code with
   `UIMeshRigElement.SetPointPose`.
4. To see an Animator clip without entering Play Mode, pick it under **Animator Clip Preview** in the rig
   inspector and press **Preview Clip**.

---

UI Mesh Rig deforms a single Sprite in four environments from one renderer-agnostic core:

- `UIMeshRigGraphic` — a standard `MaskableGraphic` inside a uGUI Canvas;
- `UIMeshRigElement` — a custom `VisualElement` for UI Toolkit/UXML/UI Builder;
- `UIMeshRigWorldRenderer` — `MeshFilter` + `MeshRenderer` for a scene without a Canvas;
- `UIMeshRigSpriteRenderer` — a plain `SpriteRenderer` (sorting layers, 2D lights, sprite masks, SRP batching).

The `UIMeshRigGeometryBuilder` core builds vertices, indices and UVs the same way everywhere, and computes
elliptical influence, falloff, pose and procedural motion. The adapters only translate the finished geometry
into the API of their environment. That is why `Columns`, `Rows`, `Preserve Aspect`, `Deformation Enabled`,
Sprite/Color and motion presets mean the same thing in every variant. One difference is structural: the uGUI,
world and SpriteRenderer rigs are posed through child point Transforms (Animator, Timeline, code), while the UI
Toolkit element has no Transforms at all and is posed by its layout preset, its motion settings and
`SetPointPose` from code (see [UI Toolkit](#ui-toolkit)).

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
not only in Setup (they hide only while an Animation preview runs, see below); radii are dragged from four
sides (±X and ±Y), and the anchor is noticeably larger and has a dark contrasting ring.

The pose gizmo writes only the channel of the selected tool and only the axes that moved: Move changes the
point's `anchoredPosition` (or `localPosition` for world rigs) in the rig plane, Rotate is a disc about the
rig's forward axis, and Scale keeps Z. Recording in the Animation window therefore keys exactly what was
dragged, without float noise on untouched channels or out-of-plane rotation the mesh ignores.

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

A running preview steps at most sixty times a second, repaints the Scene view and rebuilds, once per step,
every world and SpriteRenderer rig whose points advanced (those rigs otherwise rebuild only in `LateUpdate`,
which the editor does not run for a preview step; a Canvas rebuilds on the repaint). It never calls
`EditorApplication.QueuePlayerLoopUpdate()`: queuing a player loop update from an editor tick schedules
another tick, which re-enters the driver, which queues another — a loop with no exit while the preview is on.

Previews never run on prefab assets (persistent objects) and never run in Play Mode — in Play Mode the
component's own `Play On Enable` / `Play()` drives the motion instead.

Choosing a value in the motion component's **Preset** dropdown applies that preset at once: its curves are
copied into the profile and the live preview starts. `Custom` keeps the current curves. The check runs only
in the GUI event of the dropdown change, so loading a scene, Undo or editing a curve never re-applies a
preset over hand-tuned curves. From code call `ApplyPreset(preset)`.

Configuring a stopped motion (`ApplyPreset`, `Phase`, `Seed`, or adding the component, which applies
Breathe) no longer pushes a pose onto the point. Before 12.3 that left a frozen procedural pose behind: the
rig counted the point as previewed, stopped rebinding it in Setup and kept the mesh deformed. `EvaluateAt`
remains the explicit way to apply a pose without playing.

### Animator clips and Timeline in Edit Mode

Unity does not play Animator clips outside Play Mode. The rig inspectors (uGUI, world, SpriteRenderer) show an
**Animator Clip Preview** block when the rig or one of its parents has an `Animator`: choose a clip of its
controller and press **Preview Clip**. The clip is sampled through `AnimationMode` on `EditorApplication.update`
(at most sixty steps a second, looping), and **Stop Clip Preview** restores every property the clip touched.
Like the motion preview it stops on selection change, when a rig inspector is disabled, on entering Play
Mode, on assembly reload and on editor quit. It refuses to start while another Animation window or Timeline
preview owns `AnimationMode`.

While any `AnimationMode` preview runs — this block, the Animation window preview or recording, or a Timeline
preview — every rig treats itself as posed:

- `Setup` does not rebind a point the clip moved. Before 12.3 each sampled frame was captured as the new
  bind centre and written into the anchors, which `AnimationMode` does not restore, so scrubbing a clip
  in `Setup` corrupted the bind for good.
- The mesh deforms in `Setup` too, so the previewed clip is visible.
- The bind handles (anchor and radii) are hidden, `Capture Rest Pose` is disabled, and a rig in
  `Pose / Animate` cannot switch to `Setup`, because that resets every point and recording would key the
  reset. The inspector shows a warning while this applies.
- World and SpriteRenderer rigs rebuild their mesh from an editor update while the preview runs; they
  otherwise rebuild only in `LateUpdate`, which the editor does not run for a scrub.

## uGUI

`UIMeshRigGraphic` keeps its previous workflow and public API. Child `UIMeshRigPoint` objects are
`RectTransform`s, so Position/Rotation/Scale can be recorded by an ordinary Animator or Timeline. The context
menu of a Simple `Image` supports in-place and non-destructive conversion. Raycast modes:

- `Rect` — the original RectTransform;
- `Deformed Mesh` — the actual deformed mesh;
- `Sprite Alpha` — Sprite transparency (requires Read/Write Enabled, otherwise it safely falls back to the mesh).
  Readability is checked on the current sprite each time, so assigning a readable sprite in the Inspector
  after a non-readable one turns the alpha test back on.

**Anchors of direct child points.** A point that is a direct child of the rig rides a RectTransform anchor, so
it follows the rig when the rect changes. The bind centre is normalized in the aspect-fitted drawing rect
(with `Preserve Aspect` the sprite may cover only part of the RectTransform), while anchors are normalized in
the full RectTransform. Rebinding a point (`Setup` drag, `Capture Rest Pose`, a layout) therefore puts its
anchor on the bind centre in full-rect terms, and its rest `anchoredPosition` is `(0, 0)`: Animator keys on
`anchoredPosition` read as offsets from the bind at any rig size. When the rect, the sprite or
`Preserve Aspect` changes, in Edit and Play Mode and in both authoring modes, the rig moves those anchors so
each point keeps its offset from its bind; `anchoredPosition` itself is not touched, so Animator and code
poses stay relative to the bind. Before 12.3 a resize of a letterboxed rig shifted every point by
`(newWidth - oldWidth) * (bind - pivot)`, which showed as deformation at rest, and in `Setup` the shifted point
was rebound further off the sprite on every resize.

Points bound before 12.3 on a letterboxed rig keep their old rest offset (`anchoredPosition` equal to the
letterbox offset) and stay correct through resizes; rebinding such a point moves it to the new convention.
A clip that keyed those points' `anchoredPosition` against the old offset needs its rest keys re-recorded after
the rebind.

## UI Toolkit

`UIMeshRigElement` is declared as a Unity 6 custom control through `[UxmlElement]` on a `partial` class and
`[UxmlAttribute]` on its properties. It draws in `generateVisualContent`, allocates data through
`MeshGenerationContext.Allocate(...)` and fills `Vertex.position`, `Vertex.tint` and `Vertex.uv`. The adapter
honors `MeshWriteData.uvRegion`, so the texture is correct when UI Toolkit places it into an atlas. In
Unity 6.3 the remap is performed automatically, but reading the region is kept for compatibility across the
Unity 6.x branch.

For UXML/UI Builder, use the element directly. `UIMeshRigUIToolkitHost` is an optional scene wrapper: it
creates the element and sets Sprite, Size, Position, grid, preset and motion.

**Posing the element.** UI Toolkit has no point Transforms, so an `AnimationClip` cannot pose single points of
the element, and the Animation window cannot record them. The code path is the element's point pose API:

```csharp
UIMeshRigElement element = host.Element;          // or the element from UXML
int count = element.PointCount;                    // points of the current LayoutPreset
element.SetPointPose(0, new Vector2(12f, 4f), 8f, new Vector2(1.1f, 0.95f));
element.ClearPointPose(0);
element.ClearPointPoses();
```

The offset is in element pixels with x to the right and y up; it is added to the preset motion, the rotation
(degrees, counter-clockwise) is added and the scale multiplies, as for `SetProceduralPose` on a point.
Changing `LayoutPreset` clears every point pose.

Set motion through the host, not on its element: `SetMotionEnabled`, `SetMotionSpeed` and `SetMotionPhase`.
Every host setter re-applies all host fields to the element, so a value written only on `host.Element` is
overwritten by the next `host.SetSize(...)`. An Animator can key the host's own fields (motion speed and
phase, color, position, size); the host pushes them to the element when Unity reports animated properties.
The element's motion clock is `Time.realtimeSinceStartup`, so it ignores `Time.timeScale`.

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

**Sprite Atlas.** A sprite packed tightly or with rotation (the Sprite Atlas defaults are Tight Packing and
Allow Rotation) has no single axis-aligned texture rectangle; `Sprite.textureRect` even throws for it. The
adapter checks `UIMeshRigSpriteRenderer.CanDeform(sprite)` and keeps drawing the source sprite undeformed
instead of failing on every rebuild; with `NeoDiagnostics` warnings on it logs a throttled warning, and the
inspector shows one. Pack rig sprites as Full Rect without rotation.

**Flip X / Flip Y** mirror the drawn sprite but not the points, so the deformation and the Scene handles
appear on the mirrored side. The inspector warns while a flip is on. To mirror a rigged sprite with its points,
use a negative Transform scale on the rig.

## Influence and motion

A point has two independent ellipses: inside INNER the full weight applies, outside OUTER the weight is zero,
and between them the Falloff Curve is applied. `UIMeshRigPointMotion` adds a procedural pose on top of the
Transform and does not overwrite Animator keys. Presets: Float, Breathe, BodySway, HeadSway, SoftJiggle, Pulse,
SquashStretch, Wave and Noise. Shared layouts: SimpleBounce, Character and FlagCloth.

**Strength** is a relative weight, not an amplitude. The weights of all points over a vertex are averaged and
the vertex moves by at most the full pose, so a vertex never moves further than the point does. `0` turns a
point off and `1` is normal; above `1` the weight reaches full sooner, so the area that follows the point fully
grows towards OUTER, and the point wins over overlapping points. To exaggerate a motion, move the point
further or raise the motion amplitude.

The clip and the procedural pose compose per channel: positions add, rotations add and scales multiply. The
Transform rotation is read as the shortest angle from the rest rotation (`Mathf.DeltaAngle`), so a rotation
of more than 180 degrees from rest wraps, which shows as a jump when `Rotation Influence` is below 1.
`UIMeshRigPointMotion` runs on unscaled time by default while an Animator runs on scaled time; turn off
`Use Unscaled Time` when both must pause together.

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
element.SetPointPose(3, new Vector2(0f, 6f), -5f, Vector2.one); // nod the Character head from code

toolkitHost.SetMotionSpeed(2f); // motion settings go through the host, not host.Element
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
- `UIMeshRigPoint.SetProceduralPose` with the pose the point already holds, and `ClearProceduralPose` on a
  point without one, do nothing. A motion at speed 0, with flat curves or with the default empty `Custom`
  profile therefore no longer rebuilds and re-uploads the whole grid every frame.
- The influence of a point is rejected by the bounding box of its outer ellipse before any square root, and
  each vertex x point pair copies the point state once instead of three times.

`UIMeshRigGeometryBuilder.Build` has an overload with `normalizedBounds`: the grid covers only that part of
the coordinate space while the UVs still span the whole UV rect. The adapters pass
`UIMeshRigGeometryBuilder.GetSpriteContentBounds(sprite)`, which is the unit rect for ordinary sprites and the
untrimmed inner part for a sprite whose transparent border an atlas trimmed, as uGUI `Image` does with its
padding. Point binds stay normalized in the full sprite rect.

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

Import `NeoxiderTools.Demo.v<ver>.unitypackage` (see [Demo Scenes](../Samples.md)) and open
`Assets/NeoxiderDemos/Demo/UI Mesh Rig/UIMeshRigDemo.unity`. The top row keeps the three
previous uGUI workflows (static pose, procedural motion, Animator). The bottom row shows the four output
adapters side by side: uGUI Simple Bounce, UI Toolkit Character, world Flag Cloth and
`[Demo] World/Sprite Renderer - Character` (`UIMeshRigSpriteRenderer` with the Character layout).
Unity does not play Animator clips outside Play Mode, so the Animator card stands still in the editor until
you select it and press **Preview Clip** in its inspector (or press Play); the neighboring cards keep moving
in the editor thanks to the edit-mode preview of `UIMeshRigPointMotion`.
The collapsed info card in the top-left corner (`[Demo] Info Card`, expand it with `+` in Play Mode) names the
objects to select and the equivalent C# calls. The scene is a plain saved scene: edit it in the Editor, and
re-import the demo download to get the original back.

## Limitations

- uGUI conversion targets `Image.Type.Simple`; Sliced, Tiled and Filled have different geometry.
- UI Toolkit uses `ushort` indices; the current 40x40 limit is well below that cap.
- An `AnimationClip` cannot pose single points of the UI Toolkit element; use `SetPointPose` or key the host's
  motion fields. The element's motion ignores `Time.timeScale`.
- `UIMeshRigSpriteRenderer` requires `Draw Mode = Simple`; `Sliced` and `Tiled` rebuild the geometry themselves.
- Tightly packed or rotated Sprite Atlas sprites: the uGUI, world and UI Toolkit rigs draw the sprite's
  bounding rectangle of the atlas (neighbours can show through), and the SpriteRenderer rig draws the sprite
  undeformed. Use Full Rect packing without rotation for rig sprites.
- `SpriteRenderer` Flip X / Flip Y mirror the art but not the points; flip with a negative scale instead.
- Point rotation is read as the shortest angle from rest, so rotations beyond 180 degrees wrap.
- The bounds of the clone grow only by `Bounds Headroom`; increase the value for extreme deformation.
- For a multi-layer character, IK and a large set of skeletal clips, a dedicated 2D rig is a better fit.
- A dense grid and many moving points raise the rebuild CPU cost (no GC); measure with the Profiler.
