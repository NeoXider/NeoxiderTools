# UI Mesh Rig

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

**The host works through `PanelRenderer`, not through `UIDocument`.** Starting with Unity 6.4, world-space
UI Toolkit is rendered by `PanelRenderer`, so the host first looks for it on its own GameObject and subscribes
to `RegisterUIReloadCallback` — the element is added to the root that the renderer provides and migrates on
every reload of the tree. `UIDocument` remains only a fallback for editors where `PanelRenderer` does not exist
yet (verified by reflection: in Unity 6000.3 the `PanelRenderer` type is not present in the assembly at all,
so the branch is closed behind `#if UNITY_6000_4_OR_NEWER`). `[RequireComponent(typeof(UIDocument))]` has been
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

## Demo

Import the `Demo Scenes` sample and open `Demo/UI Mesh Rig/UIMeshRigDemo.unity`. The top row keeps the three
previous uGUI workflows (static pose, procedural motion, Animator). The bottom row shows the three output
adapters side by side: uGUI Simple Bounce, UI Toolkit Character and world Flag Cloth.
The Animator card moves only in Play Mode: Unity does not play Animator clips outside Play Mode, while the
neighboring cards keep moving in the editor thanks to the edit-mode preview of `UIMeshRigPointMotion`.
There is no `UIMeshRigSpriteRenderer` example in the scene yet — create one with
`GameObject > 2D Object > Neoxider UI Mesh Rig (Sprite Renderer)`.

## Limitations

- uGUI conversion targets `Image.Type.Simple`; Sliced, Tiled and Filled have different geometry.
- UI Toolkit uses `ushort` indices; the current 40x40 limit is well below that cap.
- `UIMeshRigSpriteRenderer` requires `Draw Mode = Simple`; `Sliced` and `Tiled` rebuild the geometry themselves.
- The bounds of the clone grow only by `Bounds Headroom`; increase the value for extreme deformation.
- For a multi-layer character, IK and a large set of skeletal clips, a dedicated 2D rig is a better fit.
- A dense grid and many moving points raise the rebuild cost; measure with the Profiler.
