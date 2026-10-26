# ParallaxLayer

**Purpose:** seamless 2D parallax tiling. The component builds a small tile pool around a camera, offsets the layer by a configurable multiplier, recycles tiles outside the view, and supports auto-scroll, sprite variants, and editor preview.

File: `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs`; namespace: `Neo.Parallax`.

## Module Principle

`ParallaxLayer` is a reusable scene wrapper. Production scenes should inject the camera through `targetCamera` or `SetTargetCamera(Camera)`. `Camera.main` is only an optional fallback, so the component works with multiple cameras, split-screen setups, runtime-created cameras, and tests. The component owns its generated tiles; other children of the layer remain available for scene composition.

## Inspector Fields

| Field | Description |
|-------|-------------|
| `targetCamera` | Camera used for parallax calculations. Preferred production path. |
| `useMainCameraFallback` | Resolves `Camera.main` only when `targetCamera` is empty. |
| `logMissingCamera` | Allows a missing-camera warning through `NeoDiagnostics`; the global diagnostics gate still controls output. |
| `parallaxMultiplier` | Per-axis inverse camera delta: `0` keeps the initial world position, `1` moves by the opposite camera delta. Auto-scroll is added separately. |
| `scrollSpeed` | Constant world-space scrolling speed, integrated with scaled `Time.deltaTime`. |
| `generateInEditor` | Builds preview tiles in Edit Mode. Camera injection also respects this setting. |
| `tileSpacing` | Extra spacing in world units. Negative spacing permits overlap while the resulting stride stays positive. Unused spacing on disabled tiling axes is ignored. |
| `tileHorizontally`, `tileVertically` | Tiling axes. |
| `paddingTiles` | Non-negative off-screen tile padding. Enabled-axis counts and their product must fit `Int32`; disabled axes use one tile regardless of padding. |
| `templateRenderer` | Source `SpriteRenderer` for sprite/material/sorting settings and the tile anchor. Uses the renderer on the layer itself when unassigned. |
| `spriteVariants` | Optional sprites for initialization and recycling. |
| `randomiseOnInit`, `randomiseOnRecycle` | Variant randomization points. |
| `fitToMaxSpriteSize` | Scales tiles to the largest configured sprite to avoid gaps. |

## C# API

```csharp
parallaxLayer.SetTargetCamera(camera);
Camera activeCamera = parallaxLayer.TargetCamera;
```

`SetTargetCamera` clears the missing-camera state and rebuilds the layer when the component is active. In Edit Mode, rebuilding also requires `generateInEditor`. On an inactive component, the method stores the camera for the next enable.

Rebinding starts a new camera/layer origin at the current positions and resets accumulated auto-scroll. Call it after changing the source sprite or other authoring settings when a rebuild is needed. To release the camera and pool, disable `useMainCameraFallback` in the Inspector and call `SetTargetCamera(null)`; the template's original renderer state is restored. With fallback enabled, passing `null` attempts to resolve `Camera.main` again.

## Setup

1. Add `ParallaxLayer` to a GameObject with a `SpriteRenderer`, or assign a source renderer on a child. The source anchors one tile; layer and source scales are applied once. Axis-aligned nested source transforms are also supported.
2. Assign `targetCamera` explicitly. Keep `useMainCameraFallback` enabled only for simple demo scenes.
3. Configure `parallaxMultiplier` and `scrollSpeed`.
4. Enable vertical tiling, `paddingTiles`, `tileSpacing`, and `spriteVariants` as needed.
5. Keep `generateInEditor` enabled when immediate scene preview is useful.

For an Inspector/UnityEvent workflow, wire a camera reference into `SetTargetCamera(Camera)` and use the component's enabled state to start or stop tiling. No separate NoCode action or ScriptableObject is required. Authored child objects and the source hierarchy survive pool creation, camera rebinding, and cleanup.

## Behavior

- Reinitialization removes only tiles created by this instance and builds a new pool. Replacing the template restores the previous source renderer before hiding the new one.
- Edit Mode cleanup uses `DestroyImmediate`; Play Mode cleanup deactivates old tiles immediately and uses deferred `Destroy`.
- Editor validation queues the rebuild outside `OnValidate`. Disable/destroy cancels queued preview work and releases owned tiles.
- Recycling centres the existing pool on the camera and wraps each tile in one step per axis, including camera teleports. A stationary camera does not keep rerolling off-screen padding tiles.
- A pool is not created if every configured sprite is null, or an enabled tiling axis has a non-finite stride or a stride of at most `0.0001` world units. Correct the configuration and rebind the camera to retry.
- Counts are calculated before tile allocation with widened padding/viewport arithmetic. Non-finite or unrepresentable enabled-axis counts, a total tile count above `Int32.MaxValue`, or an enabled-axis world period above the finite `float` range reject the rebuild. The previous pool is released and the source renderer's original enabled state is preserved. Reduce padding, viewport coverage, or spacing as appropriate, then call `SetTargetCamera` to retry; the same checks apply to Inspector-driven rebuilds.
- Before any tile is created, enabled-axis local pitch, the centered origin and both endpoint coordinates are calculated in `double` and checked against finite `float` position storage. Accepted coordinates stay in `double` through centering and are converted only at the final position assignment, allowing finite endpoints even when an uncentered intermediate would exceed `float` range. Disabled axes retain their source coordinate without evaluating unused spacing. A small layer scale can make local geometry unrepresentable even for a five-tile request with a finite world period; rejection uses the same source-restoration and retry path.
- Missing camera does not spam the console. A warning is possible only when `logMissingCamera` and `NeoDiagnostics` warnings are enabled.
- Disable/destroy cleanup restores the template's captured transform and original enabled state, including an intentionally disabled source. Rebuilding preserves the current authoring transform. Source sprites are not modified by tile generation.

## Scope and Constraints

Use an axis-aligned layer and camera for seamless world-X/Y recycling, with consistent sprite pivots and alignment. Nonuniform scale is supported for axis-aligned hierarchies; rotated or sheared hierarchies do not have a general seamless-tiling guarantee. Different sprite dimensions fit the largest configured dimensions when `fitToMaxSpriteSize` is enabled; turning it off can leave intentional gaps.

An orthographic camera determines the initial pool size from its current size and aspect. Runtime zoom, aspect, or scale changes require a rebuild through `SetTargetCamera`. Perspective cameras currently use fixed fallback tile counts rather than frustum coverage at the layer depth. Count, period and local-coordinate checks guard numerical representability; large representable requests can still exhaust available resources. There is no new default capacity limit or configurable tile budget.
