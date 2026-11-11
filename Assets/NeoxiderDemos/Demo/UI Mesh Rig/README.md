# UI Mesh Rig demo

Open `UIMeshRigDemo.unity` and enter Play Mode. The scene keeps the three existing uGUI authoring
workflows in the top row:

1. **Static pose** — a point is edited directly in Pose mode.
2. **Procedural motion** — `UIMeshRigPointMotion` drives an editable preset.
3. **Unity Animator** — a looping `AnimationClip` animates a point `RectTransform`.

> **The Animator card only moves in Play Mode.** Its wiring is complete — `UIMeshRigAnimatorDemo`
> controller with a default state, the looping `UIMeshRigAnimatorDemo` clip, curves bound to the child
> named `Rig Point` — but Unity does not evaluate Animator clips outside Play Mode. Its two neighbours
> keep moving in the Editor because `UIMeshRigPointMotion` has an Edit Mode preview driver, so in a
> stopped Editor the Animator card looks static next to them. Press Play before judging it.

The bottom row compares the output adapters using the same sprite and geometry core:

1. **uGUI / Simple Bounce** — `UIMeshRigGraphic` in the Canvas.
2. **UI Toolkit / Character** — `UIMeshRigElement` hosted by `UIMeshRigUIToolkitHost`.
3. **World / Flag Cloth** — `UIMeshRigWorldRenderer` on `MeshFilter` + `MeshRenderer`, without Canvas.
4. **SpriteRenderer / Character** — `[Demo] World/Sprite Renderer - Character`: `UIMeshRigSpriteRenderer` on a
   plain `SpriteRenderer` whose sprite is deformed through a runtime clone (the `UIMeshRigDemoLogo.png` asset
   is never modified). Create your own from `GameObject > 2D Object > Neoxider UI Mesh Rig (Sprite Renderer)`.

The collapsed info card in the top-left corner (`[Demo] Info Card`, press `+` in Play Mode) lists the objects
to select and the equivalent C# calls (`UIMeshRigLayoutBuilder.Apply`, `UIMeshRigPointMotion.ApplyPreset`,
`SetSource`).

The `[Demo] ...` objects are authored by the editor builder `UIMeshRigDemoBuilder`
(`Neoxider/Demo/Rebuild Active Demo Scene`); everything else in the scene is hand-authored and left untouched.

The UI Toolkit host binds to `PanelRenderer` on Unity 6.5+ and falls back to the `UIDocument` this scene
carries on older editors, so the example keeps working either way.

Select a rig or its point children to inspect the common settings, Module header, presets and Scene-view
authoring tools — including the Scene overlay with the Setup / Pose switch and the label / ring
readability toggles. UI Toolkit exposes the custom element in UI Builder under
`Library > Custom Controls > Neoxider > UI Mesh Rig`.

The sample intentionally keeps each workflow independent so it can be copied without demo-only builder
logic. `UIMeshRigDemoPanelSettings.asset` belongs to the UI Toolkit example.
