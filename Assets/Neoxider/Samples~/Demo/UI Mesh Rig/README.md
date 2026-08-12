# UI Mesh Rig demo

Open `UIMeshRigDemo.unity` and enter Play Mode. The scene presents the same
`NeoLogo` sprite in three authoring workflows:

1. **Static pose** - a rig point is moved, rotated and scaled directly in Pose mode.
2. **Procedural motion** - `UIMeshRigPointMotion` drives a point with an editable preset.
3. **Unity Animator** - a standard looping `AnimationClip` animates a point `RectTransform`.

The Animator clip has **Loop Time** enabled, so the third example keeps moving after its first cycle.

All three graphics use the same dense uGUI mesh, preserve the original sprite aspect,
and use deformed-mesh + sprite-alpha raycasting. Click a visible part of any logo to
see the interaction pulse; transparent corners do not accept clicks.

## Authoring

- Select a `UIMeshRigGraphic` to add points, change grid resolution, switch between
  Setup and Pose modes, or convert a regular `Image` from the component context menu.
- Select a child `UIMeshRigPoint` to edit its full-influence and fade-to-zero ellipses
  directly in Scene view.
- Animate point position, rotation and scale in a normal Animator, or add
  `UIMeshRigPointMotion` and choose a built-in motion preset.

The sample intentionally keeps each workflow independent so it can be copied without
bringing along demo-only scene logic.
