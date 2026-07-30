# Tools / Move / CharacterController

**What it is:** The current 3D character controller of the package. Motor, cameras and animation come from the bundled [Character Movement Fundamentals](../../../../ThirdParty/CharacterMovementFundamentals/) (CMF, MIT); the Neoxider components in `Scripts/Tools/Move/CharacterController/` supply input, sensitivity, cursor-aware gating, sprint, external-camera binding and Mirror support. The previous [PlayerController3DPhysics](../PlayerController3DPhysics.md) stays available as legacy.

**Contents:**

| Page | Purpose |
|------|---------|
| [NeoCharacterInput](./NeoCharacterInput.md) | Movement + jump input for CMF controllers (New Input System / legacy, injection hooks) |
| [NeoCameraInput](./NeoCameraInput.md) | Look input for CMF cameras (sensitivity, cursor gate, pause) |
| [NeoCharacterSprint](./NeoCharacterSprint.md) | Sprint on top of CMF's single-speed walker |
| [NeoCharacterCameraBridge](./NeoCharacterCameraBridge.md) | Keep movement camera-relative when Cinemachine (or any external rig) drives the camera |
| [NeoCharacterNetworkBinding](./NeoCharacterNetworkBinding.md) | Local/remote split and `NetworkTransform` wiring for Mirror |

---

## Why this controller

CMF is a Rigidbody-based motor. The behaviour it brings that the legacy controller does not have:

- **Slopes.** `Slope Limit` on `AdvancedWalkerController` defines what is walkable; anything steeper is not climbed — the character slides back down at `Slide Gravity`. Walking up a wall is impossible by construction.
- **Stairs and ledges.** The `Mover` extends its ground sensor while grounded and applies a ground-adjustment velocity, so steps and slopes are traversed without losing ground contact or bouncing.
- **Moving platforms.** `MovingPlatform` moves a kinematic Rigidbody along waypoints and carries whatever stands in its `TriggerArea` along with it.
- **Momentum.** Movement is momentum-based. `AddMomentum(Vector3)` / `SetMomentum(Vector3)` inject external forces (explosions, launch pads, knockback) and the controller keeps the added speed instead of erasing it on the next frame.
- **Arbitrary gravity direction.** The motor works relative to its own `transform.up`, so wall and ceiling walking, gravity tunnels and planet-style gravity all work.
- **First and third person.** `CameraController` (first person) and `ThirdPersonCameraController` + `CameraDistanceRaycaster` (third person with camera collision) ship with the asset.
- **Animation.** `AnimationControl` feeds velocity, grounded state, jump and land into an `Animator`, with an optional strafe blend tree.

---

## How to set it up

1. Add a `Rigidbody`, a `CapsuleCollider` and CMF's `Mover` to the character root.
2. Add `AdvancedWalkerController`.
3. Add **[NeoCharacterInput](./NeoCharacterInput.md)** to the same GameObject — CMF resolves its `CharacterInput` from its own GameObject in `Awake`.
4. Optional: add **[NeoCharacterSprint](./NeoCharacterSprint.md)** for a run speed.
5. Camera:
   - *First person* — child a camera under the character, add CMF's `CameraController` plus **[NeoCameraInput](./NeoCameraInput.md)**, and assign that transform to `AdvancedWalkerController.cameraTransform`.
   - *Third person* — same, but use `ThirdPersonCameraController` and add `CameraDistanceRaycaster` so the camera does not clip through geometry.
   - *Cinemachine* — keep the pivot with `CameraController` + `NeoCameraInput` as the aim source, make it the Cinemachine camera's Follow/Look At target, and add **[NeoCharacterCameraBridge](./NeoCharacterCameraBridge.md)** to the character so movement follows the camera you actually render through.
6. Animation: add CMF's `AnimationControl` and point it at your `Animator`.
7. Multiplayer: add **[NeoCharacterNetworkBinding](./NeoCharacterNetworkBinding.md)**.

Ready-made prefabs and demo scenes ship in the sample — see [Sample](#sample).

---

## Cursor ownership

None of the Neoxider components here write `Cursor` state. [CursorLockController](../CursorLockController.md) stays the single cursor owner; `NeoCameraInput` only *reads* `Cursor.visible` to decide whether look should be processed (`Pause Look When Cursor Visible`, on by default).

Do **not** use CMF's own `MouseCursorLock` from the sample together with `CursorLockController` — two owners fight over the same state. It lives in the sample's `ShowcaseScripts/` folder for exactly that reason.

---

## Sample

`Samples~/CharacterMovementFundamentals/` holds the upstream demo content: example scenes (showcase, top-down, special/gravity), controller prefabs (blank, simplified, animated), the Capguy character with its animator, environment art, sounds and CMF's showcase-only scripts.

`Samples~` is not compiled by Unity. Copy the folder into `Assets/` to try the scenes; the prefabs there use CMF's own legacy-Input components, so swap in `NeoCharacterInput` / `NeoCameraInput` when moving them into a real project.

---

## Multiplayer

Handled by [NeoCharacterNetworkBinding](./NeoCharacterNetworkBinding.md): CMF simulates on every instance by default, so remote copies must be reduced to `NetworkTransform`-driven proxies. See that page for the authority model and its limits.

---

## Migrating from PlayerController3DPhysics

The legacy controller keeps working — nothing is removed and its serialized fields and public API are unchanged. When you move a character over:

| Legacy | Replacement |
|--------|-------------|
| `PlayerController3DPhysics` (movement, jump, gravity) | `Mover` + `AdvancedWalkerController` |
| `PlayerController3DPhysics` (input) | `NeoCharacterInput` (`SetMoveInput`/`SetJumpInput`/`SetRunInput` carry over) |
| `PlayerController3DPhysics` (look, sensitivity, cursor gate, pause) | `CameraController` + `NeoCameraInput` |
| `_walkSpeed` / `_runSpeed` | `AdvancedWalkerController.movementSpeed` + `NeoCharacterSprint.Sprint Speed Multiplier` |
| `PlayerController3DAnimatorDriver` | CMF `AnimationControl` |
| Mirror wiring inside the controller | `NeoCharacterNetworkBinding` |
| `_onJumped` / `_onLanded` UnityEvents | `Controller.OnJump` / `Controller.OnLand` C# events |

`CursorLockController`'s *Player Control* section drives `PlayerController3DPhysics` specifically. With the new controller the equivalent gate is built into `NeoCameraInput` (`Pause Look When Cursor Visible`) and `NeoCharacterInput.SetMovementEnabled`.

---

## Third-party code

CMF is bundled under the MIT license in `ThirdParty/CharacterMovementFundamentals/` (see the `LICENSE.md` there and the repository [THIRD-PARTY-NOTICES](../../../../THIRD-PARTY-NOTICES.md)). It is vendored as shipped, with two Unity 6 compile patches, both marked with a `NEOXIDER PATCH` comment:

- `Core/Sensor.cs` — removed a stray `[SerializeField]` from the `CastType` enum declaration. `SerializeField` is `AttributeTargets.Field`, so since Unity 6000.0.3 applying it elsewhere is a hard error (CS0592) instead of being ignored; upstream CMF does not build on Unity 6 without this.
- `Core/Mover.cs` — `SetVelocity` writes `Rigidbody.linearVelocity`, the Unity 6 name for the old `velocity`. Two folders were renamed for path hygiene (`Animation & Audio` -> `AnimationAudio`, `Core scripts` -> `Core`); no script GUIDs changed.

CMF's own 7 MB `Manual.pdf` is not bundled — get it from the [upstream repository](https://github.com/Jan-Ott/CharacterMovementFundamentals) when you need the full reference for `Mover`, `Sensor` and the controller internals. The upstream changelog ships as `ThirdParty/CharacterMovementFundamentals/CHANGELOG.txt`.

## See also

- [Tools / Move](../README.md)
- [PlayerController3DPhysics](../PlayerController3DPhysics.md) (legacy)
- [CursorLockController](../CursorLockController.md)
