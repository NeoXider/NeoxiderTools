# BillboardUniversal

**What it is:** a MonoBehaviour that orients an object toward or away from a camera, or along a fixed
world-space direction, every `LateUpdate`. Script: `Scripts/Tools/View/BillboardUniversal.cs`, namespace
`Neo.Tools`, assembly `Neo.Tools.View`. Typical uses: health bars, name labels, icons and world-space
sprites that must stay readable from any camera angle (`ignoreY` keeps them upright). This is the
component's canonical page.

**How to use:**
1. Add the component and choose `billboardMode`.
2. Assign `targetCamera`, or keep `useMainCameraFallback` on; direction mode needs no camera.
3. Change it at runtime with `SetBillboardMode`, `SetCustomDirection`, `SetIgnoreY`, `SetTargetCamera`.

---

## Setup

- Add the component and choose `billboardMode`.
- Camera modes use `targetCamera`; `useMainCameraFallback` permits resolving `Camera.main` when empty.
  A missing camera leaves the rotation unchanged. Optional warnings are emitted at most once per binding.
- `TowardsDirection` uses `customDirection` without resolving or requiring a camera. `ignoreY` projects
  the direction onto the horizontal plane. A zero direction preserves the current rotation.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `billboardMode` | `TowardsCamera`, `AwayFromCamera` (default), or `TowardsDirection`. |
| `customDirection` | World-space facing vector in direction mode. |
| `targetCamera` | Explicit camera dependency for camera modes. |
| `ignoreY` | Ignore the vertical component (default true). |
| `useMainCameraFallback` | Resolve and cache `Camera.main` when no explicit camera is available. |
| `logMissingCamera` | Opt-in, gated missing-camera warning. |

## C# API

```csharp
billboard.SetBillboardMode(BillboardUniversal.BillboardMode.TowardsDirection);
billboard.SetCustomDirection(Vector3.right);
billboard.SetIgnoreY(true);
```

Use `SetTargetCamera(Camera)` to inject or replace the camera. Settings take effect on the next
`LateUpdate`; the Inspector context menu also supports direction mode with no camera. Read the current
configuration through `Mode`, `IgnoreY`, `CustomDirection` and `TargetCamera`. The component caches its
own and the camera's `Transform` (re-resolved when the camera changes), so a frame costs no extra
native `transform` lookups.

## Edit Mode

Rotation happens only in `LateUpdate` (Play Mode); there is no `OnValidate`, so loading or saving a prefab
never writes a camera-dependent rotation into it. To preview the orientation in the editor, use the
component's context menu **Face Camera Now** (recorded in Undo).

## See Also

- [Module Root](../README.md)
