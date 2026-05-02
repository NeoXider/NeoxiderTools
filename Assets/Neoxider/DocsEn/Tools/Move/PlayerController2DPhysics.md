# PlayerController2DPhysics

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `10f` | 10f. |
| `55f` | 55f. |
| `5f` | 5f. |
| `70f` | 70f. |
| `8f` | 8f. |
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
| `true` | True. |

## Cursor

This controller does **not** change `Cursor.lockState` / `Cursor.visible`. There is no FPS-style mouse look here — unlike **PlayerController3DPhysics**, no **Enable Cursor Control** switch is needed. Use **CursorLockController** or your UI flow for menus and pointer visibility.

## See Also

- [Module Root](../README.md)