# MeshEmission

**Purpose:** Copy a `Light`'s intensity/color into a mesh's emission, with optional scaling, brightening
and a cutoff. Namespace: `Neo.Tools.View`; assembly: `Neo.Tools.View`.

## Setup

- Add a `MeshRenderer` with an assigned material before adding `MeshEmission`.
  The component controls the first material slot and disables itself if the renderer/material is missing.
- Assign `targetLight` explicitly. When `syncWithLight` is enabled and the reference is empty at `Start`,
  the fallback searches this object and its children. It accepts `Light`, not `Light2D`.
- Built-in/URP use `_EmissionColor`; HDRP uses `_EmissiveColor`. Initial color capture and subsequent
  writes support either property. A material with neither property has no visible emission output.

## C# API and ownership

```csharp
emission.targetLight = light;
emission.intensityMultiplier = 0.5f;
emission.syncWithLight = true;
```

`FindAndAttachLight()` explicitly reruns the local fallback. `ResetToOriginal()` restores the captured
emission color at intensity 1, subject to `emissionCutoff`. Disabling performs the same reset and updates
`CurrentIntensity` / `CurrentColor`. Those properties describe the logical emission before cutoff.

The component creates and owns a separate material clone, leaving the source asset/instance unchanged.
Removing the component restores the original material on a surviving renderer before releasing the
clone. If another owner replaced the renderer's material, that replacement is preserved. In Play Mode
Unity destroys the released clone at the end of the frame. Re-enabling keeps the owned clone and resumes
syncing. Inspector fields and public methods provide the same workflow without a separate NoCode bridge.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `CurrentColor` | Current Color. |
| `CurrentIntensity` | Current Intensity. |
| `emissionCutoff` | Emission Cutoff. |
| `intensityMultiplier` | Intensity Multiplier. |
| `syncColor` | Sync Color. |
| `syncIntensity` | Sync Intensity. |
| `syncWithLight` | Sync With Light. |
| `targetLight` | Target Light. |
| `whiteRange` | White Range. |
| `whiteThreshold` | White Threshold. |

## See Also

- [Module Root](../README.md)
