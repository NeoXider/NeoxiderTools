# CameraAspectRatioScaler

**What it is:** an `[ExecuteAlways]` `MonoBehaviour` that adapts an orthographic or perspective camera to its
output aspect ratio (screen or render texture), keeping a reference composition visible. Script:
`Scripts/Tools/Components/CameraAspectRatioScaler.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Components`.

**How to use:**
1. Author the Camera's orthographic size / vertical FOV for the reference resolution, then add
   **Neoxider / Tools / CameraAspectRatioScaler**. The first initialization captures both values into
   **Reference Size** / **Reference Fov** on the scaler. Later edits of the Camera's **Size** / **Field of View**
   are picked up as the new reference on the next scaler refresh.
2. Set **Target Resolution** to the reference composition, such as 1920 × 1080.
3. Choose a scale mode and suitable **Min Size** / **Max Size** limits.
4. Leave **Update In Runtime** on to follow resolution changes during play.

---

## Scale modes

| Mode | Projection behavior |
|---|---|
| `FitWidth` | Preserves reference width by changing orthographic height or vertical FOV. |
| `FitHeight` | Keeps reference orthographic height or vertical FOV. |
| `FitBoth` | Preserves both dimensions with viewport bars on screen when enabled, otherwise expands projection. |
| `Manual` | Leaves size/FOV alone; applying the mode normalizes the viewport to the full rectangle. |

When **Use Target Resolution** is off, the reference aspect is 16:9. The same **Min Size** / **Max Size** limits
clamp orthographic size or perspective FOV in degrees; adjust them for the selected projection. Limits and
**Scale Multiplier** can restrict how much of the reference composition remains visible. Perspective width fitting
converts vertical/horizontal FOV with tangent geometry.

## Reference composition

Scaling always starts from the scaler's serialized reference (**Reference Size**, **Reference Fov**), never from the
camera's current, already scaled value. Because the reference is stored on the scaler, an Edit Mode preview that
rewrites the camera is not captured again as the reference after a domain reload or scene reopen (which used to
compound the scaling with `FitWidth`, e.g. 5 → 15.8 → 49.9 in a 9:16 Game view for a 16:9 reference).

The scaler also remembers the last camera size/FOV it wrote (or captured). A camera value that differs from it was
authored by someone else, so it becomes the new reference:

- **Edit Mode:** change the Camera's **Size** / **Field of View** as usual. The next scaler refresh (an Inspector
  change on the scaler, enable, **Apply Camera Scale**, `Refresh()`) adopts the value and scales from it. Only the
  active projection is adopted. This also works with **Update In Editor** off, where the camera is never rewritten.
- **Play Mode:** the composition is read once, when the scaler initializes (as the old `Awake` capture did). Gameplay
  code that zooms the camera later is not adopted; the next resolution change rescales from the reference again.
- To set the composition explicitly, edit **Reference Size** / **Reference Fov**, use the C# properties, or press
  **Capture Reference From Camera** (needed only to adopt a value equal to the scaler's last output).
- Components saved before the reference fields existed capture the camera once on their next initialization.

## Screen output and render textures

Without a target texture, the current aspect uses `Screen.width / Screen.height`. With **Use Viewport
Rect In Fit Both**, `FitBoth` centers letterbox/pillarbox bars through `Camera.rect` and retains the
reference projection, subject to the multiplier and limits. Other paths restore a full viewport.

When `Camera.targetTexture` is assigned, its width and height determine the current aspect.
`FitBoth` expands orthographic size or perspective FOV to preserve the reference composition and uses
a full viewport, even when the viewport option is enabled. Camera viewport rectangles do not produce
bars in this path; compose bars separately in the destination UI or render pipeline if required.

The runtime cache includes screen dimensions, target texture dimensions, orthographic/perspective
mode, scaling settings (mode, target resolution, multiplier, viewport option, **Min Size** / **Max Size**) and the
reference values. Resizing or replacing a texture with different dimensions, switching projection or changing a
setting or the reference recalculates on the next enabled runtime update. The per-frame check does not allocate.

## Editor preview

Automatic Edit Mode preview is discrete, through lifecycle/Inspector changes. With **Update In
Editor** off, automatic enable/start/validation scaling and disable viewport cleanup leave the
authored camera projection/rect unchanged. Reference capture still occurs. The explicit **Apply Camera
Scale** button remains available. At runtime, **Reset Viewport Rect On Disable** restores a full
viewport independently of the editor preview flag.

## C# API

| Member | Description |
|---|---|
| `float ReferenceOrthographicSize` | Reference orthographic size (min 0.01). Setting it wins over an earlier camera edit. |
| `float ReferenceFieldOfView` | Reference vertical FOV in degrees (1–179). Setting it wins over an earlier camera edit. |
| `ScaleMode Mode` | Scale mode used by the next refresh. |
| `Vector2 TargetResolution` | Reference resolution (each axis at least 1). |
| `bool UseTargetResolution` | Reference aspect from `TargetResolution`; `false` uses 16:9. |
| `float ScaleMultiplier` | Extra factor on the computed size/FOV (at least 0.01, like the Inspector clamp). |
| `bool UseViewportRectInFitBoth` | `FitBoth` letterbox/pillarbox through `Camera.rect` (never for render-texture targets). |
| `float MinSize` | Lower clamp of the size/FOV (at least 0.01); raising it above `MaxSize` raises `MaxSize`. |
| `float MaxSize` | Upper clamp of the size/FOV; never below `MinSize`. |
| `bool UpdateInRuntime` | Re-apply during Play when the output or a setting changes; when off, call `Refresh()`. |
| `bool UpdateInEditor` | Edit Mode preview on enable, validation and the Inspector buttons. |
| `bool ResetViewportRectOnDisable` | Restore the full `Camera.rect` when the scaler is disabled. |
| `void Refresh()` | Recalculates projection and viewport now, even when nothing changed. |
| `void CaptureReferenceFromCamera()` | Takes the camera's current size/FOV as the new reference. |

Settings changed through these properties apply on the next runtime update while **Update In Runtime** is on (the
change check covers every setting, including the size limits); in Edit Mode or with runtime updates off, call
`Refresh()`.

```csharp
CameraAspectRatioScaler scaler = GetComponent<CameraAspectRatioScaler>();
scaler.TargetResolution = new Vector2(1080f, 1920f); // portrait design
scaler.ReferenceOrthographicSize = 9.6f;
scaler.Mode = CameraAspectRatioScaler.ScaleMode.FitBoth;
scaler.UseViewportRectInFitBoth = false; // expand the projection instead of drawing bars
scaler.ScaleMultiplier = 1.1f;           // a little extra margin around the design
scaler.Refresh();
```

The scaler does not allocate, resize or release the caller's render texture.

## Demo scene

`Assets/NeoxiderDemos/Demo/Scenes/Tools/CameraAspectRatio.unity` (Demo download). Everything is
saved in the scene, so the wiring is visible without entering Play Mode:

- **Main Camera** carries the scaler: `FitBoth`, Target Resolution 1080 × 1920, **Reference Size** 9.6, multiplier 1.
- **[Demo] World / Design Frame** outlines the reference composition (height = 2 × Reference Size, width = height ×
  the target aspect) with colored corner markers, so you see which part of the design stays visible.
- **[Demo] Bars Camera** clears the letterbox/pillarbox area that `FitBoth` leaves outside `Camera.rect`.
- The left card switches **Scale mode** (`scaler.Mode`), the **Design resolution** (`TargetResolution` +
  `ReferenceOrthographicSize`) and the **Output**: *Screen* follows the Game view (try Free Aspect, 16:9, 9:16), while
  *Phone 9:19.5*, *Tablet 3:4* and *Ultrawide 21:9* render the same camera into `Camera.targetTexture` (render texture
  assets in `Data/Tools/CameraAspectRatio`) and show it in the center preview.
- The **Reference size** slider is the NoCode path: its `On Value Changed` calls
  `CameraAspectRatioScaler.ReferenceOrthographicSize` directly; `CameraAspectRatioDemoController.OnReferenceSizeChanged`
  only resizes the design frame.
- The **Live readout** shows the output size, aspect, `orthographicSize` against the reference, `Camera.rect`, the
  visible world and whether the design frame is cropped.

The scene is saved with its UI and wiring.

## See also

- [Components](Components/README.md)
