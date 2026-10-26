# CameraAspectRatioScaler

`Neo.Tools.CameraAspectRatioScaler` adapts an orthographic or perspective camera to its output aspect
ratio. It belongs to `Neo.Tools.Components`; source:
`Assets/Neoxider/Scripts/Tools/Components/CameraAspectRatioScaler.cs`.

## Scene setup

1. Configure the Camera's reference orthographic size or vertical field of view, then add
   **Neoxider / Tools / CameraAspectRatioScaler**. Initialization captures both reference values.
2. Set **Target Resolution** to the reference composition, such as 1920 × 1080. When **Use Target
   Resolution** is off, the reference aspect is 16:9.
3. Choose a scale mode and suitable **Min Size** / **Max Size** limits. The same limits clamp
   orthographic size or perspective FOV in degrees; adjust them for the selected projection. Limits
   and **Scale Multiplier** can restrict how much of the reference composition remains visible.
4. Enable **Update In Runtime** for changes during play. Use **Update In Editor** for automatic
   authoring previews. The Inspector's **Apply Camera Scale** button forces an intentional update.

| Mode | Projection behavior |
|---|---|
| `FitWidth` | Preserves reference width by changing orthographic height or vertical FOV. |
| `FitHeight` | Keeps reference orthographic height or vertical FOV. |
| `FitBoth` | Preserves both dimensions with viewport bars on screen when enabled, otherwise expands projection. |
| `Manual` | Leaves size/FOV alone; applying the mode normalizes the viewport to the full rectangle. |

Perspective width fitting converts vertical/horizontal FOV with tangent geometry. Scaling uses the
captured reference rather than repeatedly multiplying an already-scaled camera value.

## Screen output and render textures

Without a target texture, the current aspect uses `Screen.width / Screen.height`. With **Use Viewport
Rect In Fit Both**, `FitBoth` centers letterbox/pillarbox bars through `Camera.rect` and retains the
reference projection, subject to the multiplier and limits. Other paths restore a full viewport.

When `Camera.targetTexture` is assigned, its width and height determine the current aspect.
`FitBoth` expands orthographic size or perspective FOV to preserve the reference composition and uses
a full viewport, even when the viewport option is enabled. Camera viewport rectangles do not produce
bars in this path; compose bars separately in the destination UI or render pipeline if required.

The runtime cache includes screen dimensions, target texture dimensions, orthographic/perspective
mode and scaling settings. Resizing or replacing a texture with different dimensions, or switching
projection, recalculates from the reference on the next enabled runtime update. Replacing a texture
with the same dimensions does not require different projection math.

## Editor preview and C# use

Automatic Edit Mode preview is discrete, through lifecycle/Inspector changes. With **Update In
Editor** off, automatic enable/start/validation scaling and disable viewport cleanup leave the
authored camera projection/rect unchanged. Reference capture still occurs. The explicit **Apply Camera
Scale** button remains available. At runtime, **Reset Viewport Rect On Disable** restores a full
viewport independently of the editor preview flag.

C# consumers can get or add the component through normal Unity APIs and assign the camera output:

```csharp
Camera camera = GetComponent<Camera>();
CameraAspectRatioScaler scaler = GetComponent<CameraAspectRatioScaler>();
camera.targetTexture = outputTexture; // Assigned RenderTexture owned by the caller.
scaler.enabled = true;
```

Configure the serialized scaling settings in the Inspector; there is no public forced-refresh method.
The automatic runtime path observes output changes when **Update In Runtime** is enabled. The scaler
does not allocate, resize or release the caller's render texture.

See [Components](Components/README.md).
