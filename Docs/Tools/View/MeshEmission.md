# MeshEmission

**What it is:** a MonoBehaviour that copies a `Light`'s intensity and color into a mesh's emission, with
optional scaling, brightening and a cutoff. Script: `Scripts/Tools/View/MeshEmission.cs`, namespace
`Neo.Tools.View`, assembly `Neo.Tools.View`.

**How to use:**
1. Add a `MeshRenderer` with an assigned material, then add `MeshEmission`.
2. Assign `targetLight` (or leave it empty to search this object and its children at `Start`).
3. Tune `intensityMultiplier`, `whiteThreshold` / `whiteRange` and `emissionCutoff`.

---

## Setup

- The component controls the first material slot and disables itself if the renderer/material is missing.
- When `syncWithLight` is enabled and `targetLight` is empty at `Start`, the fallback searches this object
  and its children. It accepts `Light`, not `Light2D`.
- Built-in/URP use `_EmissionColor`; HDRP uses `_EmissiveColor`. Initial color capture and subsequent
  writes support either property. A material with neither property has no visible emission output.

## C# API and ownership

```csharp
emission.targetLight = light;
emission.intensityMultiplier = 0.5f;
emission.syncWithLight = true;
```

`FindAndAttachLight()` explicitly reruns the local fallback. `ResetToOriginal()` restores the captured
baseline exactly - the source emission color with emission on - whatever `emissionCutoff` is, and is
also what disabling does; it updates `CurrentIntensity` (1) and `CurrentColor`. While syncing, those
properties describe the logical emission before the cutoff.

The component creates and owns a separate material clone, leaving the source asset/instance unchanged.
Removing the component restores the original material on a surviving renderer before releasing the
clone. If another owner replaced the renderer's material, that replacement is preserved. In Play Mode
Unity destroys the released clone at the end of the frame. Re-enabling keeps the owned clone and resumes
syncing. Inspector fields and public methods provide the same workflow without a separate NoCode bridge.

Per frame the component writes the emission color only when it changed, and toggles the `_EMISSION`
keyword and GI flags only when emission crosses the cutoff. Edit the owned clone only through this
component while it syncs; `ResetToOriginal()` rewrites everything.

To animate emission without a light and without a material clone per object, use
`AnimationUtils.ApplyToRenderer` with a `MaterialPropertyBlock`
(see [AnimationUtils](../../Animations/AnimationUtils.md)).

## Fields

| Field | Description |
|-------|-------------|
| `syncWithLight` | Copy the light every frame. |
| `targetLight` | Light to follow; may be on another object. |
| `syncIntensity`, `syncColor` | Which light values to copy; unsynced color keeps the captured emission color. |
| `intensityMultiplier` | Light intensity x this = emission intensity. |
| `whiteThreshold`, `whiteRange` | Above the threshold the intensity is boosted up to 2x over the range. |
| `emissionCutoff` | At or below this intensity emission is switched off (black, keyword off). |

`CurrentIntensity` and `CurrentColor` are read-only readbacks.

## See also

- [LightAnimator](LightAnimator.md)
- [Module root](README.md)
