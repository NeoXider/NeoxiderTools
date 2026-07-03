# ParallaxLayer

**Purpose:** seamless 2D parallax tiling. The component builds a small tile pool around a camera, offsets the layer by a configurable multiplier, recycles tiles outside the view, and supports auto-scroll, sprite variants, and editor preview.

File: `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs`

## Module Principle

`ParallaxLayer` is a reusable scene wrapper. Production scenes should inject the camera through `targetCamera` or `SetTargetCamera(Camera)`. `Camera.main` is only an optional fallback, so the component works with multiple cameras, split-screen setups, runtime-created cameras, and tests.

## Inspector Fields

| Field | Description |
|-------|-------------|
| `targetCamera` | Camera used for parallax calculations. Preferred production path. |
| `useMainCameraFallback` | Resolves `Camera.main` only when `targetCamera` is empty. |
| `logMissingCamera` | Allows a missing-camera warning through `NeoDiagnostics`; the global diagnostics gate still controls output. |
| `parallaxMultiplier` | Camera movement multiplier. `0` sticks close to the camera, `1` moves by the opposite camera delta. |
| `scrollSpeed` | Constant world-space scrolling speed. |
| `generateInEditor` | Builds preview tiles in Edit Mode. |
| `tileSpacing` | Extra spacing between tiles. |
| `tileHorizontally`, `tileVertically` | Tiling axes. |
| `paddingTiles` | Off-screen tile padding. |
| `templateRenderer` | Source `SpriteRenderer` for sprite/material/sorting settings. |
| `spriteVariants` | Optional sprites for initialization and recycling. |
| `randomiseOnInit`, `randomiseOnRecycle` | Variant randomization points. |
| `fitToMaxSpriteSize` | Scales tiles to the largest configured sprite to avoid gaps. |

## C# API

```csharp
parallaxLayer.SetTargetCamera(camera);
Camera activeCamera = parallaxLayer.TargetCamera;
```

`SetTargetCamera` clears the missing-camera state and rebuilds the layer when the component is active.

## Setup

1. Add `ParallaxLayer` to a GameObject with a `SpriteRenderer`.
2. Assign `targetCamera` explicitly. Keep `useMainCameraFallback` enabled only for simple demo scenes.
3. Configure `parallaxMultiplier` and `scrollSpeed`.
4. Enable vertical tiling, `paddingTiles`, `tileSpacing`, and `spriteVariants` as needed.
5. Keep `generateInEditor` enabled when immediate scene preview is useful.

## Behavior

- Reinitialization removes old tile objects and builds a new pool.
- Edit Mode cleanup uses `DestroyImmediate`; Play Mode cleanup uses `Destroy`.
- Missing camera does not spam the console. A warning is possible only when `logMissingCamera` and `NeoDiagnostics` warnings are enabled.
- The template renderer is restored after cleanup, so the source object does not stay hidden after stop/disable.
