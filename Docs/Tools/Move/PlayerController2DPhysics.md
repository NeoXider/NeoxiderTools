# PlayerController2DPhysics

**What it is:** a `MonoBehaviour` (a Mirror `NetworkBehaviour` when Mirror is installed) in `Scripts/Tools/Move/PlayerController2DPhysics.cs`, namespace `Neo.Tools`: a `Rigidbody2D` side-scroller controller with walk/run acceleration, jumping with coyote time and input buffering, sprite flipping and an optional follow camera.

**How to use:**
1. Add the controller and a `Rigidbody2D` + collider to the character.
2. Assign a foot-level `_groundCheck` and the floor's `_groundMask`; optionally assign the follow camera (defaults to `Camera.main`).
3. Play: it reads horizontal axis, jump and run input (Legacy Input Manager or Input System). For touch UI, feed it with `SetMoveInput`, `SetJumpInput` and `SetRunInput`.

---

## Setup notes

- Grounding checks all overlapping colliders in that mask and excludes the character's own hierarchy.
  The character's own collider cannot keep it grounded in mid-air, even when it shares the floor layer.
- Input, grounding and jump timers run in `Update`; horizontal velocity is applied in `FixedUpdate`; the follow
  camera moves in `LateUpdate`. The legacy-input availability probe is cached, so an Input System-only project
  does not pay an exception per input read.
- With `_flipByVelocityX`, `localScale.x` is written only when the facing direction actually changes.

## C# API

Use `SetMoveInput(float?)` or `SetMoveInput(Vector2?)` for external horizontal input; `null` restores
hardware input. `SetRunInput(bool)` supplies sprint while external movement is active.
`SetJumpInput()` queues a one-shot jump. `SetMovementEnabled(bool)`, `SetJumpEnabled(bool)` and
`SetCameraFollowEnabled(bool)` control the scene adapter without replacing its physics logic.
`Teleport(Vector3, bool resetVelocity = true)` moves both the transform and the `Rigidbody2D`, so interpolation
does not blend from the old pose. Observe `IsGrounded`, `IsRunning`, `MovementEnabled` and `JumpEnabled`, or
connect the movement/jump/landing UnityEvents in the Inspector.

```csharp
[SerializeField] private PlayerController2DPhysics _player;

public void OnJoystick(Vector2 value) => _player.SetMoveInput(value);
public void OnJoystickReleased() => _player.SetMoveInput((Vector2?)null);
public void OnJumpButton() => _player.SetJumpInput();
```

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_rigidbody` | Character body; auto-assigned from this object. |
| `_groundCheck` / `_groundMask` / `_groundCheckRadius` | Ground probe position, layers and radius. Without a probe, a point 0.5 units below the transform is used. |
| `_followCamera` / `_followCameraEnabled` / `_cameraOffset` / `_cameraFollowSpeed` | Optional exponential camera follow. |
| `_walkSpeed` / `_runSpeed` / `_canRun` | Horizontal speeds and whether running is allowed. |
| `_acceleration` / `_deceleration` | Velocity change rates with and without input. |
| `_flipByVelocityX` | Mirror `localScale.x` to face the movement direction. |
| `_canJump` / `_jumpImpulse` / `_coyoteTime` / `_jumpBufferTime` | Jump settings. |
| `_inputBackend` / `_horizontalAxis` / `_jumpButton` / `_runKey` | Input source and legacy axis/button names. |
| `_movementEnabled` | Initial movement state (see `SetMovementEnabled`). |
| `_logSetupWarnings` / `_logInputFallbackWarnings` | Optional one-time diagnostics. |
| `_onJumped` / `_onLanded` / `_onMoveStart` / `_onMoveStop` | UnityEvents for jump, landing and movement start/stop. |

## Cursor

This controller does **not** change `Cursor.lockState` / `Cursor.visible`. There is no FPS-style mouse look here — unlike **PlayerController3DPhysics**, no **Enable Cursor Control** switch is needed. Use **CursorLockController** or your UI flow for menus and pointer visibility.

## Inspector testing

In Play Mode, press **Jump** in the Inspector to call `SetJumpInput()`. This queues the same one-shot
external jump command used by an on-screen button; the controller consumes it through its normal input
path. The button is disabled outside Play Mode.

## See Also

- [PlayerController2DAnimatorDriver](./PlayerController2DAnimatorDriver.md)
- [Tools/Move](./README.md)
