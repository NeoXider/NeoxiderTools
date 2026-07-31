# PlayerController3DAnimatorDriver

> **Legacy since 10.3.0.** Superseded by CMF's `AnimationControl`, which drives an `Animator` from the [CharacterController module](./CharacterController/README.md). Tagged `[LegacyComponent]`; kept working and listed, but new setups should use `AnimationControl`.

**Purpose:** Drives an `Animator` from a [PlayerController3DPhysics](./PlayerController3DPhysics.md) (legacy): feeds speed, grounded state and jump into animator parameters.

## Setup

- Add the component via **Add Component → Neoxider → Tools → Legacy → PlayerController3DAnimatorDriver**, on the same object as the legacy controller.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_animator` | Animator. |
| `_blendDampTime` | Blend Damp Time. |
| `_blendMaxSpeed` | Blend Max Speed. |
| `_blendXParam` | Blend XParam. |
| `_blendYParam` | Blend YParam. |
| `_cameraTransform` | Camera Transform. |
| `_controller` | Controller. |
| `_isGroundedParam` | Is Grounded Param. |
| `_isMovingParam` | Is Moving Param. |
| `_isRunningParam` | Is Running Param. |
| `_jumpTriggerParam` | Jump Trigger Param. |
| `_locomotionStateParam` | Locomotion State Param. |
| `_movingThreshold` | Moving Threshold. |
| `_rigidbody` | Rigidbody. |
| `_speedParam` | Speed Param. |
| `_updateInLateUpdate` | Write animator parameters in `LateUpdate` instead of `Update`. |
| `_useDirectionalBlendTree` | Feed `_blendXParam`/`_blendYParam` for a 2D locomotion blend tree. |
| `_useJumpTrigger` | Fire `_jumpTriggerParam` on jump. |
| `_useLocomotionStateInt` | Drive `_locomotionStateParam` as an int state instead of separate bools. |
| `_velocitySpace` | Velocity Space. |

## Runtime API

| Member | Purpose |
|--------|---------|
| `bool IsReady { get; }` | True once the animator and controller references resolved. Read-only, not serialized. |

## See Also

- [Module Root](../README.md)