# PlayerController2DAnimatorDriver

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/PlayerController2DAnimatorDriver.cs`, namespace `Neo.Tools`) that feeds an `Animator` from a [PlayerController2DPhysics](./PlayerController2DPhysics.md) and its `Rigidbody2D`: grounded/moving/running flags, speed, a locomotion state, a jump trigger and blend-tree axes.

**How to use:**
1. Add the component (menu **Neoxider > Tools > PlayerController2DAnimatorDriver**) on the character; the `Animator` (in children), controller and body are found automatically.
2. Make the parameter names match your Animator Controller, or clear a name to skip that parameter.
3. Choose `_blendMode` (`HorizontalOnly` or `TwoAxis`) when using a blend tree.

---

## Parameter handling

Parameter names are resolved to hashes once per Animator Controller and written by hash every frame, so a warm
update makes no managed allocations (guarded by the EditMode test `Driver2D_WarmApplyAnimation_DoesNotAllocate`). A
parameter the controller does not declare, or declares with a different type, is skipped instead of producing
Unity's "parameter does not exist" warning every frame. The cache rebinds automatically when the Animator or its
runtime controller changes; nothing is written while the Animator is not initialized (for example on an inactive
object), but the grounded state is still tracked so a late-initialized Animator does not get a stale `Jump`.

To find a typo in a parameter name, enable warnings with `NeoDiagnostics.Configure(warnings: true)`: every
non-blank name that is in use and does not resolve is reported once per controller binding (for example
`Animator controller 'Hero' has no Bool parameter 'IsRuning'`). Names of disabled features (`_locomotionStateParam`
without `_useLocomotionStateInt`, `_jumpTriggerParam` without `_useJumpTrigger`, the blend axes without
`_useBlendTree`, and `MoveY` in `HorizontalOnly` mode) are not reported.

| Parameter field | Expected type | Value |
|-----------------|---------------|-------|
| `_isGroundedParam` (`IsGrounded`) | Bool | Controller `IsGrounded`. |
| `_isMovingParam` (`IsMoving`) | Bool | Speed above `_movingThreshold`. |
| `_isRunningParam` (`IsRunning`) | Bool | Controller `IsRunning` while moving. |
| `_speedParam` (`Speed`) | Float | Velocity magnitude. |
| `_locomotionStateParam` (`LocomotionState`) | Int | 0 idle, 1 move, 2 run, 3 airborne (when `_useLocomotionStateInt`). |
| `_jumpTriggerParam` (`Jump`) | Trigger | Leaving the ground upwards (when `_useJumpTrigger`). |
| `_blendXParam` / `_blendYParam` (`MoveX` / `MoveY`) | Float | Velocity divided by `_blendMaxSpeed`, damped by `_blendDampTime` (when `_useBlendTree`). |

`_updateInLateUpdate` writes the parameters in `LateUpdate` instead of `Update`.

## Runtime API

| Member | Purpose |
|--------|---------|
| `bool IsReady { get; }` | True once the animator, controller and body references are assigned. |
| `SetAnimator(Animator)` / `SetController(PlayerController2DPhysics)` / `SetRigidbody(Rigidbody2D)` | Replace references at runtime. |

## See Also

- [PlayerController2DPhysics](./PlayerController2DPhysics.md)
- [Tools/Move](./README.md)
