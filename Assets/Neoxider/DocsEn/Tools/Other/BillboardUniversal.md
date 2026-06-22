# BillboardUniversal

**Purpose:** Component that adjusts its `Transform` rotation every `LateUpdate` (to avoid jitter) so that the object faces the camera or a fixed direction based on the selected mode.

**Namespace:** `Neo.Tools`  
**Script:** `Assets/Neoxider/Scripts/Tools/Other/BillboardUniversal.cs`

---

## Description

`BillboardUniversal` makes an object always "look at" the camera or a given direction. This is useful for elements that must always be visible to the player regardless of the camera angle: health bars above enemies, character names, icons, particles, or sprites in a 3D world.

---

## Key Fields

### Billboard Mode

- `billboardMode` — Defines how the object orients itself:
  - `TowardsCamera`: The object always faces the camera directly. Ideal for 3D sprites that should look like 2D images.
  - `AwayFromCamera`: The object always faces away from the camera. Useful for certain effects or "back side" scenarios.
  - `TowardsDirection`: The object always faces a fixed direction defined by `customDirection`. Useful for objects that should be oriented along a world axis (e.g. the X axis).

### Other Settings

- `ignoreY` (`bool`) — If `true`, the object only rotates around its vertical (Y) axis and does not tilt up or down. Ideal for health bars or name labels that should always remain upright.
- `targetCamera` (`Camera`) — The camera the object should face. If not assigned, `Camera.main` is used.
- `customDirection` (`Vector3`) — The direction used in `TowardsDirection` mode.

## Public Methods

| Method | Description |
|---|---|
| `SetCustomDirection(Vector3 direction)` | Programmatically changes `customDirection`. |
| `SetBillboardMode(BillboardMode mode)` | Programmatically changes the orientation mode. |
| `SetIgnoreY(bool ignore)` | Programmatically enables or disables Y-axis ignore. |
| `SetTargetCamera(Camera camera)` | Programmatically changes the target camera. |

## See Also

- [View module README](./README.md)
