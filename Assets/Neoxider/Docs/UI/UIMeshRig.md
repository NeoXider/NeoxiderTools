# UI Mesh Rig

**What it is:** a sprite deformation rig in `Scripts/UI/MeshRig/` (namespace `Neo.UI`). Child `UIMeshRigPoint`
transforms act as bones, and one renderer-agnostic core (`UIMeshRigGeometryBuilder`) bends a grid mesh for uGUI,
UI Toolkit, a world mesh or a `SpriteRenderer`.

**How to use:**
1. Create a rig from a menu in [Quick start](#quick-start), or add an adapter component and assign a Sprite.
2. Apply a layout (`UIMeshRigLayoutBuilder.Apply` or the Inspector button) or add `UIMeshRigPoint` children.
3. Animate the points with an Animator, with `UIMeshRigPointMotion` presets, or from code with `SetProceduralPose`.
   The UI Toolkit element has no point objects: pose it with `UIMeshRigElement.SetPointPose`.
4. To see an Animator clip outside Play Mode, pick it under **Animator Clip Preview** in the rig inspector and
   press **Preview Clip**.

---

## Adapters

| Component | Environment |
|-----------|-------------|
| `UIMeshRigGraphic` | A `MaskableGraphic` inside a uGUI Canvas. |
| `UIMeshRigElement` | A custom `VisualElement` for UI Toolkit, UXML and UI Builder. |
| `UIMeshRigWorldRenderer` | `MeshFilter` + `MeshRenderer` in a scene without a Canvas. |
| `UIMeshRigSpriteRenderer` | A plain `SpriteRenderer`: sorting layers, 2D lights, sprite masks, SRP batching. |

The core builds vertices, indices and UVs and computes elliptical influence, falloff, pose and procedural motion.
The adapters only hand the finished geometry to their environment. `Columns`, `Rows`, `Preserve Aspect`,
`Deformation Enabled`, Sprite, Color and motion presets therefore mean the same in every adapter.

One difference is structural. The uGUI, world and SpriteRenderer rigs are posed through child point Transforms
(Animator, Timeline, code). The UI Toolkit element has no Transforms; see [UI Toolkit](#ui-toolkit).

## Quick start

- uGUI: `GameObject > UI > Neoxider UI Mesh Rig`.
- UI Toolkit host: `GameObject > UI Toolkit > Neoxider UI Mesh Rig`.
- UI Toolkit UXML: `Assets > Create > Neoxider > UI Mesh Rig (UI Toolkit UXML)`, or add `UIMeshRigElement` in
  UI Builder from `Library > Custom Controls > Neoxider > UI Mesh Rig`.
- World mesh: `GameObject > 2D Object > Neoxider UI Mesh Rig (World)`.
- SpriteRenderer: `GameObject > 2D Object > Neoxider UI Mesh Rig (Sprite Renderer)`.

Every menu item creates a visible NeoLogo, a grid and a preset that already moves. Edit points in the Scene view:
**Setup** changes the bind pose, **Pose / Animate** changes the current deformation. **Capture Rest Pose** takes
the current pose as neutral, **Reset Pose** returns to it.

## Inspectors and Scene gizmos

Creation and conversion commands, Undo, point authoring and duplication are described in
[UI Mesh Rig editor tools](./Editor.md). Notes specific to the rig components:

- The custom editors add the **Apply Layout & Preview** button, grid diagnostics, the point list and Scene handles.
  Inherited uGUI fields (`Raycast Target`, `Raycast Padding`, `Maskable`, `Material`, `Color`) and the `Script`
  field stay visible.
- With `Auto Expand Raycast To Deformed Mesh` on (default), the visible `Raycast Padding` is recomputed every frame
  to fit the deformed mesh. Your own value is kept in a hidden field as the baseline; editing the visible field by
  hand becomes the new baseline.
- The Scene overlay has the `Setup` / `Pose / Animate` switch, the Move / Rotate / Scale tool and two toggles,
  `Labels` and `All rings`. By default an unselected point draws one pale outer ring without a label.
- Bind handles (anchor and radii) work in both modes and hide only during an Animation preview. Radii are dragged
  from four sides (±X, ±Y).
- The pose gizmo writes only the selected tool's channel and only the axes that moved. Move changes
  `anchoredPosition` (`localPosition` on world rigs) in the rig plane, Rotate is a disc about the rig's forward
  axis, Scale keeps Z. The Animation window therefore records only what was dragged.

## Edit Mode preview

`UIMeshRigPointMotion` can animate a point without entering Play Mode. The controls are in the Mesh Rig Point
inspector and on the motion component: **Start Preview**, **Pause** / **Resume**, **Restart Preview**,
**Stop Preview**.

The preview is transient:

- It writes only a procedural pose composed on top of the point Transform at draw time.
- It never writes the `Transform`, never changes the rig's authoring mode and is never saved into a scene or prefab.
- **Stop Preview** restores the point exactly.
- A previewed point is not rebound: dragging it in `Setup` does not move its bind anchor. Stop the preview to
  edit the bind.

A preview stops when its object is deselected, when a rig inspector is disabled, on assembly reload, on entering
or leaving Play Mode and on editor quit, so it never keeps the editor busy or the scene dirty. It does not run on prefab assets or in Play Mode, where `Play On Enable`
or `Play()` drives the motion. It steps at most 60 times a second, repaints the Scene view and rebuilds the world
and SpriteRenderer rigs whose points moved.

**Preset dropdown.** Choosing a preset copies its curves into the profile and starts the preview. `Custom` keeps
the current curves. The preset is applied only on the dropdown change itself, so loading a scene, Undo or a curve
edit never overwrites hand-tuned curves. From code call `ApplyPreset(preset)`.

Configuring a stopped motion (`ApplyPreset`, `Phase`, `Seed`, or adding the component, which applies Breathe)
does not push a pose onto the point. Call `EvaluateAt` to apply a pose without playing.

### Animator clips and Timeline in Edit Mode

Unity does not play Animator clips outside Play Mode. The uGUI, world and SpriteRenderer rig inspectors show
**Animator Clip Preview** when the rig or a parent has an `Animator`. Choose a clip of its controller and press
**Preview Clip**: the clip is sampled through `AnimationMode`, loops at up to 60 steps a second, and
**Stop Clip Preview** restores every property it touched. It stops on the same events as the motion preview and
does not start while an Animation window or Timeline preview owns `AnimationMode`.

While any `AnimationMode` preview runs (this block, the Animation window preview or recording, a Timeline
preview), every rig treats itself as posed and the inspector shows a warning:

- `Setup` does not rebind a point the clip moved, so scrubbing never corrupts the bind.
- The mesh deforms in `Setup` too, so the clip is visible.
- Bind handles are hidden and **Capture Rest Pose** is disabled.
- A rig in `Pose / Animate` cannot switch to `Setup`: that resets every point, and recording would key the reset.
- World and SpriteRenderer rigs rebuild from an editor update during the preview.

## uGUI

`UIMeshRigGraphic` child points are `RectTransform`s, so an ordinary Animator or Timeline records their Position,
Rotation and Scale. A Simple `Image` can be converted in place or non-destructively from its context menu
([editor tools](./Editor.md#convert-a-simple-image)).

Raycast modes:

- `Rect` — the RectTransform.
- `Deformed Mesh` — the deformed mesh.
- `Sprite Alpha` — sprite transparency above `Alpha Hit Test Minimum Threshold` (0.1). Needs Read/Write Enabled;
  otherwise it falls back to the mesh. Readability is re-checked on the current sprite, so assigning a readable
  sprite turns the alpha test back on.

**Anchors of direct child points.** A direct child point rides a RectTransform anchor, so it follows the rig when
the rect changes. The bind centre is normalized in the aspect-fitted drawing rect (with `Preserve Aspect` the
sprite may cover only part of the RectTransform); anchors are normalized in the full RectTransform.

- Rebinding a point (`Setup` drag, `Capture Rest Pose`, a layout) puts its anchor on the bind centre, and its rest
  `anchoredPosition` is `(0, 0)`. Animator keys on `anchoredPosition` are offsets from the bind at any rig size.
- When the rect, the sprite or `Preserve Aspect` changes, the rig moves the anchors so each point keeps its offset
  from the bind. `anchoredPosition` is not touched. This holds in Edit and Play Mode and in both authoring modes.
- Points bound under the earlier convention keep their old rest offset (`anchoredPosition` equal to the letterbox
  offset) and stay correct through resizes. Rebinding moves them to `(0, 0)`; re-record the rest keys of a clip that
  keyed them.

## UI Toolkit

`UIMeshRigElement` is a custom control (`[UxmlElement]`) for UXML and UI Builder. It reads
`MeshWriteData.uvRegion`, so the texture maps correctly when UI Toolkit puts it into an atlas.

`UIMeshRigUIToolkitHost` is an optional scene wrapper: it creates the element and sets Sprite, Size, Position,
grid, preset and motion.

**Posing the element.** UI Toolkit has no point Transforms, so an `AnimationClip` cannot pose single points of the
element and the Animation window cannot record them. Use the point pose API:

```csharp
UIMeshRigElement element = host.Element;          // or the element from UXML
int count = element.PointCount;                    // points of the current LayoutPreset
element.SetPointPose(0, new Vector2(12f, 4f), 8f, new Vector2(1.1f, 0.95f));
element.ClearPointPose(0);
element.ClearPointPoses();
```

The offset is in element pixels, x to the right and y up, and adds to the preset motion. Rotation (degrees,
counter-clockwise) adds, scale multiplies, as with `SetProceduralPose` on a point. Changing `LayoutPreset` clears
every point pose.

**Motion through the host.** Use `SetMotionEnabled`, `SetMotionSpeed` and `SetMotionPhase` on the host, not on its
element. Every host setter re-applies all host fields, so a value written only on `host.Element` is overwritten by
the next `host.SetSize(...)`. An Animator can key the host's fields (motion speed and phase, color, position,
size); the host pushes them to the element. The element's motion clock is `Time.realtimeSinceStartup`, so it
ignores `Time.timeScale`.

**Panel binding.** The host looks for a `PanelRenderer` on its GameObject (Unity 6000.5+, world-space UI Toolkit)
and adds the element to the root it provides; the element moves with every reload of the tree. Where
`PanelRenderer` does not exist (before 6000.5), it falls back to `UIDocument`. The host does not require a
`UIDocument`. The inspector shows the binding as `Host Kind` and says so when `PanelRenderer` has not provided a
root yet.

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:neo="Neo.UI">
    <neo:UIMeshRigElement name="rig" layout-preset="Character"
        style="width: 300px; height: 300px;" />
</ui:UXML>
```

## World

`UIMeshRigWorldRenderer` works without a Canvas. It writes the geometry into the runtime `Mesh` of a `MeshFilter`
and Sprite and Color into the `MeshRenderer`. Child points and motions work as on uGUI. `Pixels Per Unit` converts
the pixel amplitudes of motion presets into world units. Use it when you need a custom material or shader, or a
size and pivot independent of the sprite import settings.

## SpriteRenderer

`UIMeshRigSpriteRenderer` deforms the art and keeps a plain `SpriteRenderer`, so sorting layers, 2D lights, sprite
masks and SRP batching keep working. The size comes from the Sprite (`rect / pixelsPerUnit`); `Pixels Per Unit`
converts the pixel amplitudes into world units.

**The imported asset is never modified.** The component makes a runtime clone (`Sprite.Create` from the original's
texture and `textureRect`), writes the geometry into it and gives it to the renderer. On `OnDisable` the clone is
destroyed and the original Sprite returns. Editing the asset would survive Play Mode and corrupt the sprite across
the project.

The geometry is written through `UnityEngine.U2D.SpriteDataAccessExtensions` (`SetVertexCount`,
`SetVertexAttribute`, `SetIndices`) from `UnityEngine.CoreModule`; no extra packages are needed.
`Sprite.OverrideGeometry` is not used because it does nothing on a runtime clone.

**Bounds Headroom** (default `0.25`, range 0–2). `Sprite.bounds` does not grow with the written geometry, so a
strongly deformed sprite can be culled at the screen edge. The clone is created with a proportionally smaller PPU:
only the bounds grow, the vertices stay in world units.

**Draw Mode** must be `Simple`. `Sliced` and `Tiled` build their own geometry and overwrite the deformation; the
inspector warns about it.

**Sprite Atlas.** A sprite packed tightly or with rotation (the Sprite Atlas defaults) has no axis-aligned texture
rectangle, and `Sprite.textureRect` throws for it. The adapter checks `UIMeshRigSpriteRenderer.CanDeform(sprite)`
and draws such a sprite undeformed. The inspector warns, and with `NeoDiagnostics` warnings on a throttled warning
is logged. Pack rig sprites as Full Rect without rotation.

**Flip X / Flip Y** mirror the sprite but not the points, so the deformation and handles appear on the mirrored
side. The inspector warns while a flip is on. To mirror a rigged sprite with its points, use a negative Transform
scale.

## Influence and motion

A point has two ellipses: inside INNER the full weight applies, outside OUTER the weight is zero, and the Falloff
Curve blends between them. `UIMeshRigPointMotion` adds a procedural pose on top of the Transform and does not
overwrite Animator keys.

- Motion presets: Float, Breathe, BodySway, HeadSway, SoftJiggle, Pulse, SquashStretch, Wave, Noise.
- Layouts: SimpleBounce, Character, FlagCloth.

**Strength** is a relative weight, not an amplitude. The weights of all points over a vertex are averaged and the
vertex moves by at most the full pose, so it never moves further than the point. `0` turns a point off, `1` is
normal. Above `1` the weight reaches full sooner: the fully-following area grows towards OUTER and the point wins
over overlapping points. To exaggerate a motion, move the point further or raise the motion amplitude.

The clip and the procedural pose compose per channel: positions add, rotations add, scales multiply. Rotation is
read as the shortest angle from rest (`Mathf.DeltaAngle`), so more than 180 degrees wraps; with
`Rotation Influence` below 1 this shows as a jump. `UIMeshRigPointMotion` uses unscaled time by default while an
Animator uses scaled time; turn off `Use Unscaled Time` when both must pause together.

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

Layout centres are normalized to the aspect-fitted sprite area, so with `Preserve Aspect` the C# builder and the
Inspector both place points inside the drawn sprite. Authored points keep their saved bind poses.

**Point ownership.** The nearest ancestor implementing `IUIMeshRigOwner` owns a point, including points below
intermediate containers. Reparenting a point between rigs refreshes both rigs' point lists. Adding an owner
component between a rig and its points moves those points to the new nested rig; removing it hands them back.
Each point sends its procedural pose to its new owner. Disabling a nested owner does not change ownership.

### Runtime layout replacement

`UIMeshRigLayoutBuilder.Apply(...)` and `ApplyToOwner(...)` replace the owner's existing point GameObjects by
default (`replaceExisting = true`). The whole removal set is checked first, before any point is created, any
binding data changes or any `Destroy` is scheduled.

- Rejected with `System.InvalidOperationException`: a point on the owner's root, and a point branch that contains
  another `IUIMeshRigOwner`, inactive nested rigs included. A rejected call changes nothing. Move the root point to
  a child, or the nested rig out of the branch, first.
- Ordinary child points are replaced. Separately owned rigs under grouping objects outside the removed branches
  stay intact.
- Other non-rig content inside a removed point GameObject is deleted with it, so keep independent content outside
  point branches.
- `replaceExisting: false` appends a layout without removing anything:

```csharp
UIMeshRigPoint[] added = UIMeshRigLayoutBuilder.Apply(
    uguiRig, UIMeshRigLayoutPreset.SimpleBounce, replaceExisting: false);
```

The same check applies to all three scene adapters. Runtime calls record no Undo, do not switch the authoring
mode and do not start a preview; the Inspector equivalents are in the [editor tools](./Editor.md).

## Performance

The steady-state rebuild of an animated rig does not allocate.

- Each adapter rebuilds into one `UIMeshRigGeometry` through `UIMeshRigGeometryBuilder.Build(..., reuse)`. The
  overload writes into `reuse` when its arrays already have the grid's sizes, otherwise it returns a new geometry.
- The public `BuildGeometry()` methods return new arrays the caller owns; a later rebuild never overwrites them.
- A pose change uploads positions only. The world renderer uploads indices on a topology change and UVs on a grid
  or sprite UV change; the SpriteRenderer clone uploads UVs and indices when it is created or the grid changes.
- `UIMeshRigElement` repaints on a 16 ms timer only while the pose depends on time. With `MotionSpeed = 0`, no
  sprite or deformation off, it repaints once per property change.
- `SetProceduralPose` with the pose the point already has, and `ClearProceduralPose` on a point without one, do
  nothing. A motion at speed 0, with flat curves or an empty `Custom` profile does not rebuild the grid.

`UIMeshRigGeometryBuilder.Build` has an overload with `normalizedBounds`: the grid covers only that part of the
space while the UVs span the whole UV rect. The adapters pass `UIMeshRigGeometryBuilder.GetSpriteContentBounds(sprite)`:
the unit rect for ordinary sprites, the untrimmed inner part for a sprite whose transparent border an atlas trimmed
(as uGUI `Image` does with its padding). Point binds stay normalized in the full sprite rect.

```csharp
UIMeshRigGeometry buffer = null;
// Each frame: the same instance comes back once the grid size is stable.
buffer = UIMeshRigGeometryBuilder.Build(columns, rows, space, uvRect, true, pointStates, buffer);
```

The remaining cost scales with `Columns x Rows x points`; keep the grid as coarse as the silhouette allows.

## Demo

Scene: `Assets/NeoxiderDemos/Demo/UI Mesh Rig/UIMeshRigDemo.unity` from `NeoxiderTools.Demo.v<ver>.unitypackage`
(see [Demo Scenes](../Samples.md)). The top row shows uGUI workflows: static pose, procedural motion, Animator. The
bottom row shows the four adapters: uGUI Simple Bounce, UI Toolkit Character, world Flag Cloth and
`[Demo] World/Sprite Renderer - Character`. The Animator card stands still in the editor until you select it and
press **Preview Clip**. `[Demo] Info Card` (expand it with `+` in Play Mode) names the objects to select and the
C# calls. Everything is saved in the scene.

## Limitations

- uGUI conversion supports `Image.Type.Simple` only; Sliced, Tiled and Filled have different geometry.
- The grid is at most 40x40 (`Columns`, `Rows` 2–40); UI Toolkit uses `ushort` indices, well above that.
- An `AnimationClip` cannot pose single points of the UI Toolkit element; use `SetPointPose` or key the host's
  motion fields. The element's motion ignores `Time.timeScale`.
- `UIMeshRigSpriteRenderer` requires `Draw Mode = Simple`.
- Tightly packed or rotated atlas sprites: the uGUI, world and UI Toolkit rigs draw the sprite's bounding rectangle
  in the atlas (neighbours can show through); the SpriteRenderer rig draws the sprite undeformed. Use Full Rect
  without rotation.
- `SpriteRenderer` Flip X / Flip Y mirror the art but not the points; use a negative scale.
- Point rotation beyond 180 degrees from rest wraps.
- The clone's bounds grow only by `Bounds Headroom`; raise it for extreme deformation.
- For a multi-layer character, IK and many skeletal clips, a dedicated 2D rig fits better.
- A dense grid with many moving points costs CPU on rebuild (no GC); measure with the Profiler.
