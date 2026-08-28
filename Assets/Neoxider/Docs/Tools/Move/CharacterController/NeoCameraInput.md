# NeoCameraInput

**What it is:** A `MonoBehaviour` deriving from CMF's `CameraInput` that feeds look input to CMF's `CameraController` / `ThirdPersonCameraController`. Replaces CMF's `CameraMouseInput` and `CameraJoystickInput` with the Neoxider input stack: New Input System or legacy Input Manager (auto-detected), `GameSettings.MouseSensitivity`, cursor-aware gating, pause handling and an injection hook for on-screen look pads. `Scripts/Tools/Move/CharacterController/NeoCameraInput.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add it to the same GameObject as CMF's `CameraController` — CMF resolves its `CameraInput` from its own GameObject in `Awake`.
2. Leave `Use Game Settings Mouse Sensitivity` on so the in-game settings slider works; turn it off and set `Mouse Sensitivity` for a fixed value.
3. Leave cursor handling to [CursorLockController](../CursorLockController.md). This component never writes `Cursor` state — it only reads it.
4. For mobile, drive it from your look pad with `SetLookInput(Vector2?)`.

---

## Fields

### Input

| Field | Type | Purpose |
|-------|------|---------|
| `Input Backend` | `NeoInputBackend` | Same rule as [NeoCharacterInput](./NeoCharacterInput.md#backend-decision-rule). |
| `Mouse X Axis` / `Mouse Y Axis` | `string` | Legacy Input Manager axes. Default `Mouse X` / `Mouse Y`. |
| `Mouse Input Multiplier` | `float` | Scales the raw pointer delta before sensitivity. Default `0.0025` — a quarter of CMF's `0.01`, tuned so look feels right at the default `GameSettings.MouseSensitivity` of 2. |
| `Max Pointer Delta Per Frame` | `float` | Rejects a raw pointer jump whose magnitude exceeds this many pixels in one frame. Default `400`; `0` disables the filter. This prevents WebGL cursor recapture from turning a window-sized position jump into a camera spin without clipping normal fast motion. |
| `Stick Input Multiplier` | `float` | Gamepad right-stick look speed, relative to the camera controller's `Camera Speed`. |
| `Invert Horizontal` / `Invert Vertical` | `bool` | Per-axis inversion. |

### Sensitivity

| Field | Type | Purpose |
|-------|------|---------|
| `Use Game Settings Mouse Sensitivity` | `bool` | Read `GameSettings.MouseSensitivity` live. On by default. |
| `Mouse Sensitivity` | `float` | Fixed sensitivity used when the toggle above is off. |

### Gating

| Field | Type | Purpose |
|-------|------|---------|
| `Look Enabled` | `bool` | Master switch. Change via `SetLookEnabled(bool)`. |
| `Pause Look When Cursor Visible` | `bool` | Return zero look while the cursor is visible. On by default, so menus and UI do not drag the view. |
| `Disable Look On Pause` | `bool` | Subscribe to `EM.OnPause` / `EM.OnResume` and follow them. On by default. |

### Diagnostics

| Field | Type | Purpose |
|-------|------|---------|
| `Log Input Fallback Warnings` | `bool` | Warn once on backend fallback. Off by default. |

---

## Properties

| Property | Type | Meaning |
|----------|------|---------|
| `LookEnabled` | `bool` | The master switch value. |
| `IsLookActive` | `bool` | Whether look is processed right now — `LookEnabled` combined with the cursor gate. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetHorizontalCameraInput()` | `float` | Yaw rate. Called by CMF. |
| `GetVerticalCameraInput()` | `float` | Pitch rate, sign-matched to CMF's own input scripts. Called by CMF. |
| `SetLookEnabled(bool)` | `void` | Enables/disables look. UnityEvent-friendly. |
| `SetLookInput(Vector2?)` | `void` | Injects a look rate (x = yaw, y = pitch) in gamepad-stick units. `null` reverts to the device. |

---

## Frame-rate independence

CMF's camera multiplies whatever it reads by `cameraSpeed * Time.deltaTime`, so the value handed to it must be a **rate**, not a per-frame delta. The two input sources are therefore treated differently:

- **Pointer delta** accumulates per frame, so it is converted to a rate by `NeoLookRate.FromFrameDelta` (divide by delta time, scale by time scale). The two multiplications then cancel and sensitivity stays identical at 30 and 240 FPS.
- **Gamepad stick** is already a continuous rate and is passed through untouched. Running it through the same conversion would make stick look speed scale with frame rate.

Before pointer scaling, a raw delta whose magnitude exceeds `Max Pointer Delta Per Frame` is discarded rather than clamped. Clamping would turn the browser's window-sized pointer-lock recapture artifact into a smaller but still visible camera kick; discarding leaves accepted motion unchanged. The stick never goes through this filter.

`NeoLookRate.FromFrameDelta` returns `0` when `Time.timeScale` or `Time.deltaTime` is zero — a paused game must not produce a `NaN` look delta.

## Cursor and pause

This component is a *reader*, never an owner:

- It does not lock, unlock, hide or show the cursor.
- `Pause Look When Cursor Visible` checks `Cursor.visible` each frame, so it cooperates with `CursorLockController`, with your own UI code, or with nothing at all.
- `Disable Look On Pause` binds to `EM` in `OnEnable` and unbinds in `OnDisable`, so it is safe on pooled or network-spawned characters.

Do not also add CMF's `MouseCursorLock` from the sample — it writes `Cursor` state directly and will fight `CursorLockController`.

## See also

- [CharacterController overview](./README.md)
- [NeoCharacterCameraBridge](./NeoCharacterCameraBridge.md) — when Cinemachine drives the camera
- [CursorLockController](../CursorLockController.md)
