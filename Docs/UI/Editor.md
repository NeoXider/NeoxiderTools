# UI Mesh Rig editor tools

**What it is:** the editor companion to [UI Mesh Rig](UIMeshRig.md). `Neo.UI.Editor` supplies the creation menus,
inspectors and Scene view tools for the uGUI, world mesh and SpriteRenderer adapters, plus the optional
UI Toolkit host inspector. Runtime geometry and motion remain in `Neo.UI`.

**How to use:**
1. Create a rig from one of the menus in [Create and configure](#create-and-configure).
2. Choose a **Ready-Made Rig** layout and press **Apply Layout & Preview**, or add points yourself.
3. Edit and animate the points in the Scene view ([Edit points and animate](#edit-points-and-animate)).

---

## Create and configure

- uGUI: `GameObject > UI > Neoxider UI Mesh Rig`.
- World mesh: `GameObject > 2D Object > Neoxider UI Mesh Rig (World)`.
- SpriteRenderer: `GameObject > 2D Object > Neoxider UI Mesh Rig (Sprite Renderer)`.
- UI Toolkit host: `GameObject > UI Toolkit > Neoxider UI Mesh Rig`.
- UXML: `Assets > Create > Neoxider > UI Mesh Rig (UI Toolkit UXML)`.

The uGUI menu uses the requested parent when it is under a Canvas, otherwise an existing Canvas or a newly
created one. A new Canvas also gets an EventSystem when needed. The UI Toolkit menu reuses a PanelSettings
asset or creates `Assets/Neoxider UI Mesh Rig Panel Settings.asset`; the host inspector shows its binding
status. The panel backend follows the package's Unity version compatibility layer.

Choose a layout in **Ready-Made Rig**, then press **Apply Layout & Preview**. Layouts replace this owner's
existing point GameObjects, select Pose mode and start a transient preview. Other point content is part of
the replaced GameObjects, so keep independent content outside those point branches.

A point must be on a child GameObject. Delete, Duplicate and layout replacement reject a point on the
owner's root rather than removing or cloning the renderer itself. Delete and layout replacement also reject
a point containing another rig, including an inactive nested rig. Move that nested rig outside the point
branch first. A rejected layout reports a Console warning before changing any points or authoring mode.
Nested rigs beneath ordinary grouping objects remain independent.

## Edit points and animate

**Setup** edits bind centers, influence radii and falloff. **Pose / Animate** edits the point Transforms;
ordinary Animator or Timeline tracks can animate them. Bind handles are available in both modes. The Scene
overlay selects the mode and pose tool, and **Labels** / **All rings** control visual clutter.

**Add Point** creates a point at the rig center. **Duplicate** keeps the original parent, authored pose and
rest pose, including points under transformed grouping objects. Added points and duplicated branches receive
binding keys unique among this owner's points, including inactive descendants. Existing keys are preserved;
manually edited duplicate keys still produce an Inspector diagnostic. Points inside a separately owned nested
rig have their own key scope.

**Capture Rest Pose** accepts the current Transform pose as neutral. **Reset Pose** restores it. Layout
replacement, point creation, duplication, deletion and pose edits use Unity Undo; undoing a layout restores
the previous points and authoring mode.

The inspector's **Apply Layout & Preview** button is its own Undo step, so it never absorbs an earlier unrelated
edit. Menu commands that create or convert a rig (**GameObject > UI > Neoxider UI Mesh Rig**, the 2D Sprite
Renderer and World variants, **Convert To Neoxider UI Mesh Rig** and **Create Non-Destructive Neoxider Mesh Rig
Child**) apply their starter layout inside the command's own Undo step: one Undo reverts the whole command,
including the restored Image, its Button target and the hierarchy. Redo restores the generated
point keys, layout and motion settings. Duplicated branches keep their unique keys, authored/rest pose and
internal/external references through Undo/Redo and ordinary scene save/reopen on Graphic, World and Sprite adapters.
Points made with **Add Point** likewise keep their unique key, layer, placement and captured rest pose through
Undo/Redo and scene save/reopen.

Кнопка **Apply Layout & Preview** в инспекторе — отдельный шаг Undo и не захватывает предыдущее несвязанное
изменение. Команды меню, которые создают или конвертируют риг, применяют стартовый layout внутри своего шага Undo:
одно Undo отменяет команду целиком, вместе с восстановлением Image, цели Button и иерархии. После Redo и
повторного открытия сцены у дубликатов и у точек, добавленных через **Add Point**, сохраняются уникальные ключи,
слой, поза, данные анимации и ссылки.

The point and motion inspectors expose **Start Preview**, **Pause / Resume**, **Restart Preview** and
**Stop Preview**. Preview composes a temporary procedural pose without changing authored Transforms or
serialized authoring mode. It stops on selection changes, inspector retargeting, assembly reload, entering
Play Mode and editor quit. Persistent prefab assets are excluded. In Play Mode, use the motion component's
runtime playback controls. Preview behavior and timing are described in [UI Mesh Rig](UIMeshRig.md#edit-mode-preview).

## Convert a Simple Image

The Image component's context menu offers two workflows:

- **Convert To Neoxider UI Mesh Rig** replaces the Image on the same GameObject. It preserves the active
  sprite, color, material, aspect setting, enabled state, maskability and raycast settings, and retargets
  Selectable components on that object to the visible rig. The target change is recorded as a prefab
  instance override. Typed Image references and AnimationClip bindings require review after conversion.
- **Create Non-Destructive Neoxider Mesh Rig Child** keeps the original Image component, disables its
  rendering and creates a stretching child rig. Selectables on the source object target the child.

Both commands support Simple Images and Undo. Unsupported Image types produce a Console warning without
conversion. References from components on other GameObjects are not retargeted automatically.

## C# path

Use the runtime API when creating or controlling a rig from game code. For example, on an empty
`UIMeshRigGraphic rig`:

```csharp
UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(
    rig, UIMeshRigLayoutPreset.SimpleBounce, replaceExisting: false);
rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
UIMeshRigPointMotion motion = points[0].GetComponent<UIMeshRigPointMotion>();
motion.Play();
```

The layout builder also accepts `UIMeshRigWorldRenderer` and `UIMeshRigSpriteRenderer`. Use
`IUIMeshRigOwner` for renderer independent point operations and `UIMeshRigOwnerResolver` to identify the
nearest owner. No UnityEvent chain is required. The editor utility types are internal. Runtime and Inspector
layout replacement share the same rejection policy across all three adapters: points on the owner's root
and branches containing another rig, including inactive nested rigs, are rejected before any point is
removed or created. Runtime replacement throws `System.InvalidOperationException`; the Inspector reports
a Console warning.

Runtime calls do not record Unity Undo or change the authored Setup / Pose mode. Edit Mode preview is
opt-in through `previewInEditMode` (false by default). The Inspector's **Apply Layout & Preview** records
Undo, selects Pose mode and starts transient preview. Allowed replacement still removes entire point
GameObjects and their ordinary non-rig descendants, so keep unrelated content outside those disposable
child branches.

Serialized component fields and optional Inspector event wiring continue to use the same runtime APIs.
See [UI Mesh Rig](UIMeshRig.md#runtime-api) for the adapter and motion contracts.
