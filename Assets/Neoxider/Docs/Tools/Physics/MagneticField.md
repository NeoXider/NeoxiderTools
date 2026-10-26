# MagneticField

**Purpose:** Creates a 3D magnetic field that attracts or repels objects, or pulls them along a direction / toward a point. Uses `Physics.OverlapSphereNonAlloc` and submits `ForceMode.Force` to dynamic rigidbodies.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Mode** | Field behavior: `Attract` (pull to center), `Repel` (push away), `ToTarget` (pull to a Transform), `ToPoint` (pull to coordinates), `Direction` (constant pull along a vector). |
| **Field Strength** / **Radius** | The power and effective radius of the magnet. |
| **Falloff Type** | How the force diminishes over distance (`Linear`, `Quadratic`, `Constant`). |
| **Affected Layers** | The layer mask for objects that should be affected. |
| **Toggle** | Alternate forward/reverse phases in any mode; reverse negates the configured direction. **Attract Duration**, **Repel Duration**, and **Start With Attract** configure the phases. |
| **Add Rigidbody If Needed** | Automatically attaches a `Rigidbody` to objects caught in the field if they lack one. |
| **Use Fixed Update** | Apply from `FixedUpdate` (default, recommended for physics) or `Update`. |
| **Update Interval** | Minimum scaled-time interval between overlap/force updates; zero updates every selected callback. Increasing it also reduces how often force is submitted. |
| **Max Overlap Colliders** | Reusable query buffer capacity, default 64. Excess hits are omitted; raise this above the maximum expected collider count. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SetMode(FieldMode newMode)` / `ToggleMode()` | Select a mode, or switch Attract → Repel and any other mode → Attract. These do not emit `OnModeChanged`. |
| `void SetTarget(Transform target)` | Set target and select `ToTarget` if non-null. Clearing the target preserves the current mode; `ToTarget` then falls back to the emitter position. |
| `void SetTargetPoint(Vector3 point)` | Set a world-space point and select `ToPoint`. |
| `void SetDirection(Vector3 newDirection, bool local = true)` | Set a new pull vector for `Direction` mode. |
| `void SetStrength(float newStrength)` / `SetRadius(float newRadius)` | Change strength/radius; negative values clamp to zero. |
| `float CurrentStrength` / `CurrentRadius` | Read configured values. |
| `void SetAttractDuration(float duration)` / `SetRepelDuration(float duration)` | Change phase durations; values clamp to at least 0.1 seconds. |
| `void ResetToggleTimer()` | Restart from **Start With Attract**, without emitting `OnModeChanged`. |
| `bool CurrentToggleState` | Forward (`true`) or reverse (`false`) phase when **Toggle** is enabled. |
| `int ObjectsInFieldCount { get; }` | Number of distinct detected collider GameObjects, including objects without a Rigidbody. |

The query stays centered on this component even in `ToTarget` and `ToPoint` modes. Direction and falloff use each attached body's world center of mass. A zero direction falls back to the emitter's forward direction. Each query submits one force per Rigidbody; membership events still track individual collider GameObjects. Emitters exclude their own attached body and refresh cached body references after destruction, replacement, or collider reparenting.

Disabling the component or GameObject clears membership/caches and emits `OnObjectExited` for tracked objects. Re-enabling allows immediate discovery and fresh enter events. Listeners should handle destroyed objects, which compare equal to Unity null on exit. If an enter listener disables the field, that update stops before submitting further forces.

If an enter listener immediately destroys the field component or GameObject, that pass also stops before further force submission. Immediate destruction from an `OnModeChanged` listener stops the following query/force pass. Already submitted forces remain applied, and the existing `OnDisable` cleanup still owns membership exits. A queued `Destroy` retains Unity's normal deferred timing.

Use **Use Fixed Update = true**, **Update Interval = 0** for consistent continuous force. A full query buffer can produce incomplete membership and enter/exit churn. This component queries Unity's default physics scene; objects in separately simulated local physics scenes are outside its scope.

Toggle timing uses scaled time and advances at most one phase per selected callback. Long gaps do not replay missed transitions; the next phase starts at the next callback. For `ToTarget`/`ToPoint`/`Direction`, phase `true` follows that mode's direction and phase `false` reverses it.

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnObjectEntered` | `GameObject` | Fired when an object enters the magnetic field radius. |
| `OnObjectExited` | `GameObject` | Fired when an object leaves the field. |
| `OnModeChanged` | `bool` | Automatic phase switches only (if `Toggle` is enabled). `true` = forward, `false` = reverse relative to the selected mode. |

## Examples

### No-Code Example (Inspector)
Place an empty object in front of a fan, add `MagneticField`, and set Mode to `Direction`. Align the vector to point away from the fan. Set `Falloff Type = Linear`. The fan will realistically "blow away" physics boxes in its path.

### Code Example
```csharp
[SerializeField] private MagneticField _blackHole;

public void SuperchargeBlackHole()
{
    _blackHole.SetStrength(1000f);
    _blackHole.SetRadius(50f);
    Debug.Log("Warning! Black hole power increased.");
}
```

## See Also
- [ImpulseZone](ImpulseZone.md)
- [ExplosiveForce](ExplosiveForce.md)
- ← [Tools/Physics](README.md)
