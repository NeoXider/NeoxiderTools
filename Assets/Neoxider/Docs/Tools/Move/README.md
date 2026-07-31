# Tools / Move

Movement and positioning: follow, character controllers, camera helpers, cursor control, MovementToolkit (IMover, KeyboardMover, MouseMover2D/3D, etc.). Scripts in `Scripts/Tools/Move/`. Use this page as the English module entry.

## Character controller

Start at **[CharacterController](./CharacterController/README.md)** — the current 3D character controller (CMF motor + Neoxider input, sprint, camera bridge and Mirror support). It handles slope limits with slide-off, stairs, moving platforms, momentum from external forces, first- and third-person cameras and animation.

[PlayerController3DPhysics](./PlayerController3DPhysics.md) and [PlayerController3DAnimatorDriver](./PlayerController3DAnimatorDriver.md) are **legacy**: still supported and unchanged, but new projects should use the CharacterController module. Their component menu entries moved under `Neoxider/Tools/Legacy/`. The 2D controllers are not affected.

## English pages (this folder)

- [CharacterController](./CharacterController/README.md) — [NeoCharacterInput](./CharacterController/NeoCharacterInput.md), [NeoCameraInput](./CharacterController/NeoCameraInput.md), [NeoCharacterSprint](./CharacterController/NeoCharacterSprint.md), [NeoCharacterCameraBridge](./CharacterController/NeoCharacterCameraBridge.md), [NeoCharacterNetworkBinding](./CharacterController/NeoCharacterNetworkBinding.md)
- [CursorLockController](./CursorLockController.md)
- [DistanceChecker](./DistanceChecker.md)
- [FreeFlyCameraController](./FreeFlyCameraController.md)
- [UniversalRotator](./UniversalRotator.md)
- [MovementToolkit](./MovementToolkit/README.md), [IMover](./MovementToolkit/IMover.md)

## Free-Fly Camera

Use [FreeFlyCameraController](./FreeFlyCameraController.md) for a Unity Scene View style debug/spectator camera. By default RMB gates look and movement; `W/A/S/D`, `Q/E`, `Left Shift`, `Left Alt`, and mouse wheel cover movement and speed control. Disable `Require Look Button` and optionally `Move Only While Looking` for always-on control.

## Ready-made prefabs

Character prefabs (first/third person, animated top-down, side-scroller, click-to-move) and a moving platform are listed in the **Create Neoxider Object** window under *Presets → Player* / *Presets → Environment*, and under **GameObject → Neoxider → Presets**. See [CharacterController → Ready-made prefabs](./CharacterController/README.md#ready-made-prefabs).
