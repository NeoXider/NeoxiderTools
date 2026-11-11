# PlayerController3DAnimatorDriver

> **Legacy since 10.3.0.** Superseded by CMF's `AnimationControl`, which drives an `Animator` from the [CharacterController module](./CharacterController/README.md). Tagged `[LegacyComponent]`; kept working and listed, but new setups should use `AnimationControl`.

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/PlayerController3DAnimatorDriver.cs`, namespace `Neo.Tools`) that feeds an `Animator` from a [PlayerController3DPhysics](./PlayerController3DPhysics.md) (legacy) and its `Rigidbody`: grounded/moving/running flags, planar speed, a locomotion state, a jump trigger and directional blend-tree axes.

**How to use:**
1. Add the component via **Add Component → Neoxider → Tools → Legacy → PlayerController3DAnimatorDriver**, on the same object as the legacy controller. The `Animator` (in children), controller, body and `Camera.main` are found automatically.
2. Make the parameter names match your Animator Controller, or clear a name to skip that parameter.
3. Pick `_velocitySpace` (`World`, `Local` or `CameraRelative`) for the blend-tree axes.

---

## Parameter handling

Parameter names are resolved to hashes once per Animator Controller and written by hash every frame, so a warm
update makes no managed allocations (guarded by the EditMode test `Driver3D_WarmApplyAnimation_DoesNotAllocate`). A
parameter the controller does not declare, or declares with a different type, is skipped instead of producing
Unity's "parameter does not exist" warning every frame. The cache rebinds automatically when the Animator or its
runtime controller changes; nothing is written while the Animator is not initialized, but the grounded state is
still tracked so a late-initialized Animator does not get a stale `Jump`.

To find a typo in a parameter name, enable warnings with `NeoDiagnostics.Configure(warnings: true)`: every
non-blank name that is in use and does not resolve is reported once per controller binding. Names of disabled
features (`_locomotionStateParam` without `_useLocomotionStateInt`, `_jumpTriggerParam` without `_useJumpTrigger`,
the blend axes without `_useDirectionalBlendTree`) are not reported.

| Parameter field | Expected type | Value |
|-----------------|---------------|-------|
| `_isGroundedParam` (`IsGrounded`) | Bool | Controller `IsGrounded`. |
| `_isMovingParam` (`IsMoving`) | Bool | Planar speed above `_movingThreshold`. |
| `_isRunningParam` (`IsRunning`) | Bool | Controller `IsRunning` while moving. |
| `_speedParam` (`Speed`) | Float | Planar velocity magnitude. |
| `_locomotionStateParam` (`LocomotionState`) | Int | 0 idle, 1 move, 2 run, 3 airborne (when `_useLocomotionStateInt`). |
| `_jumpTriggerParam` (`Jump`) | Trigger | Leaving the ground upwards (when `_useJumpTrigger`). |
| `_blendXParam` / `_blendYParam` (`MoveX` / `MoveY`) | Float | Planar velocity in `_velocitySpace`, divided by `_blendMaxSpeed` and damped by `_blendDampTime` (when `_useDirectionalBlendTree`). |

`_updateInLateUpdate` writes the parameters in `LateUpdate` instead of `Update`. `_cameraTransform` is the basis
for `CameraRelative`.

## Runtime API

| Member | Purpose |
|--------|---------|
| `bool IsReady { get; }` | True once the animator, controller and body references are assigned. Read-only, not serialized. |
| `SetAnimator(Animator)` / `SetController(PlayerController3DPhysics)` / `SetRigidbody(Rigidbody)` | Replace references at runtime. |

## See Also

- [PlayerController3DPhysics](./PlayerController3DPhysics.md)
- [Tools/Move](./README.md)
