# NeoCharacterCameraBridge

**What it is:** A `MonoBehaviour` that keeps CMF movement camera-relative when an external camera system — Cinemachine in particular — drives the camera instead of CMF's own rig. It points `AdvancedWalkerController.cameraTransform` at the camera the player actually renders through. `Scripts/Tools/Move/CharacterController/NeoCharacterCameraBridge.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add it to the character root next to `AdvancedWalkerController`.
2. Leave `Live Camera` empty to bind to `Camera.main` — with Cinemachine that is the Brain's camera, which is what you want.
3. Assign `Live Camera` explicitly for split screen or any setup where `Camera.main` is ambiguous.
4. Turn on `Track Camera Changes` if cameras are swapped at runtime, or call `Bind()` / `SetLiveCamera(Camera)` at the swap.

---

## Why it is needed

`AdvancedWalkerController` builds its movement basis by projecting `cameraTransform.forward` and `.right` onto the ground plane. That transform must be the one the player looks through.

With Cinemachine there are three different transforms in play — the virtual camera, the Brain's camera, and CMF's own pivot — and only the Brain's camera reflects blends, damping, noise and shot changes. Pointing the controller at anything else makes "forward" drift away from what is on screen, most visibly during a blend.

## Recommended Cinemachine setup

1. Keep a pivot object on the character with CMF's `CameraController` + [NeoCameraInput](./NeoCameraInput.md). It stays the **aim source**: it turns with the player's look input, and it holds the pitch clamp.
2. Make that pivot the Cinemachine camera's **Follow** and **Look At** target.
3. Put this component on the character with `Live Camera` empty.
4. Do **not** parent a real `Camera` under the pivot — Cinemachine's Brain camera renders instead.

Works with Cinemachine 2 and 3, and with any other external camera driver: the component references no Cinemachine API at all, so nothing here depends on the package being installed.

---

## Fields

| Field | Type | Purpose |
|-------|------|---------|
| `Controller` | `AdvancedWalkerController` | Auto-found on this GameObject. |
| `Live Camera` | `Camera` | The camera the player renders through. Empty falls back to `Camera.main`. |
| `Track Camera Changes` | `bool` | Re-resolve every `LateUpdate`. Off by default; costs a `Camera.main` lookup per frame when no camera is assigned. |
| `Log Setup Warnings` | `bool` | Warn once when no camera can be resolved. On by default. |

## Properties

| Property | Type | Meaning |
|----------|------|---------|
| `LiveCamera` | `Camera` | The assigned camera, or `null` while it falls back to `Camera.main`. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Bind()` | `void` | Re-resolves the camera and re-points the controller's movement basis at it. Called in `OnEnable`, and every `LateUpdate` when `Track Camera Changes` is on. |
| `SetLiveCamera(Camera)` | `void` | Assigns the camera explicitly, clears the warning latch and re-binds immediately. |

---

## Execution order

Runs at `[DefaultExecutionOrder(-50)]` so the binding is in place before the controller's `FixedUpdate` reads it.

## See also

- [CharacterController overview](./README.md)
- [NeoCameraInput](./NeoCameraInput.md)
