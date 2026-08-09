# PlayerController2DPhysics

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `IsGrounded` | Is Grounded. |
| `IsRunning` | Is Running. |
| `JumpEnabled` | Jump Enabled. |
| `MovementEnabled` | Movement Enabled. |
| `_cameraOffset` | Camera Offset. |
| `_coyoteTime` | Coyote Time. |
| `_followCamera` | Follow Camera. |
| `_groundCheck` | Ground Check. |
| `_groundCheckRadius` | Ground Check Radius. |
| `_groundMask` | Ground Mask. |
| `_horizontalAxis` | Horizontal Axis. |
| `_inputBackend` | Input Backend. |
| `_jumpBufferTime` | Jump Buffer Time. |
| `_jumpButton` | Jump Button. |
| `_onJumped` | On Jumped. |
| `_onLanded` | On Landed. |
| `_onMoveStart` | On Move Start. |
| `_onMoveStop` | On Move Stop. |
| `_rigidbody` | Rigidbody. |
| `_runKey` | Run Key. |

## Cursor

This controller does **not** change `Cursor.lockState` / `Cursor.visible`. There is no FPS-style mouse look here — unlike **PlayerController3DPhysics**, no **Enable Cursor Control** switch is needed. Use **CursorLockController** or your UI flow for menus and pointer visibility.

## Inspector testing

In Play Mode, press **Jump** in the Inspector to call `SetJumpInput()`. This queues the same one-shot
external jump command used by an on-screen button; the controller consumes it through its normal input
path. The button is disabled outside Play Mode.

## See Also

- [Module Root](../README.md)
