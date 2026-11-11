# Follow Component

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/Follow.cs`, namespace `Neo.Tools`) that makes its object follow a target's position and rotation in `LateUpdate`. Used for cameras following the player, pets, homing objects, and other follow mechanics.

**How to use:**
1. Add **Neoxider > Tools > Follow** to the follower and assign `target` (or enable **Find Target By Tag**).
2. Pick `followMode` (`ThreeD` / `TwoD`), the position and rotation smoothing modes and their speeds, and an `offset`.
3. Optionally enable the deadzone, distance control, position limits and rotation limits.
4. Change the behaviour from code with `SetTarget`, `SetOffset`, `SetPositionSpeed`, `TeleportToTarget` and the other setters below; react to `onStartFollowing`, `onStopFollowing` and `onTargetLost`.

## Demo scene

`Scenes/Tools/Move/Follow.unity` in the Demo download (`Assets/NeoxiderDemos/Demo`). Everything is saved in the scene, so the wiring is visible without Play Mode:

| Object | What to look at |
|--------|-----------------|
| `Target` | The object every follower chases. Its `MouseMover2D` (**Move To Point Hold**) drags it to the cursor while the left button is held; otherwise the demo controller moves it along a figure-eight. |
| `Follow`, `Follow (1)`, `Follow (2)` | The original capsules: MoveTowards with aiming, Lerp position only, aiming only. Their `onStartFollowing` / `onTargetLost` events write to the log. |
| `[Demo] World/Rail Follower` | `limitX` + `limitY` keep it on the rail; **Teleport** shows that `TeleportToTarget()` respects position limits. |
| `[Demo] World/Turret` | Rotation only, `rotationLimitZ` = -70..70: when the Target goes below, the aim stays at the nearer edge of the sector. |
| `[Demo] Frame/Content` | **Position speed** and **Deadzone** call `Follow.SetPositionSpeed` / `SetDeadzoneEnabled` directly (persistent UnityEvents); **Teleport** calls `TeleportToTarget` on every position follower. The smoothing dropdown, **Drop target** / **Restore target** and **Reset layout** go through `FollowDemoController`. |
| `[Demo] Frame/Live Followers` | `IsFollowing()` and `GetDistanceToTarget()` of every follower, left to right. |

The scene is saved with its UI and wiring.

---


## 1. Introduction

`Follow` is a professional component for making one object follow another, with support for multiple smoothing modes, a deadzone, and position/rotation limits. Fully reworked with critical bug fixes and an improved architecture.

Used for cameras following the player, pets, homing objects, and other follow mechanics.

---

## 2. Class Description

### Follow
- **Namespace**: `Neo.Tools`
- **File path**: `Assets/Neoxider/Scripts/Tools/Move/Follow.cs`

**Description**
A universal follow component with a professional smoothing implementation. Placed on the object that should follow the target. Runs in `LateUpdate` to prevent jitter.

---

## 3. Modes and Settings

### Follow Modes (FollowMode)
- `ThreeD`: Full 3D following across all axes
- `TwoD`: 2D following, ignores the Z axis for position

### Smoothing Modes (SmoothMode)
- `None`: Instant movement with no smoothing
- `MoveTowards`: Constant movement speed (default)
- `Lerp`: Linear interpolation (correct implementation with Clamp01)
- `SmoothDamp`: Smooth damping via `Vector3.SmoothDamp`
- `Exponential`: Exponential decay for natural movement

---

## 4. Position Settings

### Main Parameters
- `target`: The target to follow
- `followPosition`: Enable/disable position following
- `positionSmoothMode`: Smoothing mode for position
- `positionSpeed`: Smoothing speed (meaning depends on the mode)
- `offset`: Offset relative to the target

### Deadzone
- `deadzone.enabled`: Enable the deadzone
- `deadzone.radius`: Radius of the zone within which the camera does not move

**How it works**: The camera only starts moving when the target leaves the radius. This creates a stable camera without "jitter" during small player movements.

### Distance Control
- `distanceControl.activationDistance`: Minimum distance before following starts (0 = no restriction)
- `distanceControl.stoppingDistance`: Distance at which the camera stops before reaching the target (0 = goes all the way to the offset position)

**How it works**:
- **Activation Distance**: The camera only starts following when the distance to the target exceeds this value
- **Stopping Distance**: The camera stops the specified distance short of the target position (useful for AI, patrolling)

### Position Limits
- `limitX`, `limitY`, `limitZ`: `AxisLimit` structs for limiting each axis
  - `enabled`: Enable the limit
  - `min`, `max`: Minimum and maximum values

### Events
- `onStartFollowing`: Invoked when following starts (target moves beyond activationDistance)
- `onStopFollowing`: Invoked when following stops (target enters the activationDistance zone)
- `onTargetLost`: Invoked once when the target becomes null (destroyed or cleared)

A follow-state listener may move the follower (for example call `TeleportToTarget()`), replace the target, clear it or destroy the follower:
- after `onStartFollowing`, the frame continues from the follower's current position toward the current target (a replaced target is used right away, and a listener's teleport is not overwritten);
- if the target was cleared or the follower destroyed, the remaining position and rotation work for that frame is skipped.

---

## 5. Rotation Settings

### Main Parameters
- `followRotation`: Enable/disable rotation following
- `rotationSmoothMode`: Smoothing mode for rotation
- `rotationSpeed`: Rotation smoothing speed (default 180 degrees/sec for MoveTowards)
- `rotationOffset3D`: Additional rotation for 3D (Euler angles)
- `rotationOffset2D`: Additional angle for 2D (degrees)

### Rotation Limits
- **3D mode**: `rotationLimitX` (pitch) and `rotationLimitY` (yaw) limit the Euler angles of the look rotation. Yaw `0` faces +Z and `90` faces +X.
- **2D mode**: `rotationLimitZ` for limiting the rotation angle around the Z axis. The aim angle is `0` when the target is straight up and grows counter-clockwise (`-90` = right, `90` = left, `-180` / `180` = down), plus `rotationOffset2D`.

All three limits use the same sector rules, shared with [UniversalRotator](UniversalRotator.md) `limitRange`, so one authored range means the same thing on both components:
- A sector runs from `min` to `max` in increasing angle. An angle outside the sector snaps to the **nearer** edge (by angular distance), so a target crossing behind the sector (the -X axis in 2D, the back in 3D) no longer jumps to the opposite limit. The aim angle may be in any representation (the raw 2D aim angle is not normalized); only its direction counts.
- Limits beyond [-180, 180] stay valid: `[-225, -135]` is a downward-facing 2D sector, and `[135, 225]` is the same sector; for 3D yaw, `[135, 225]` keeps a turret looking backwards. A sector of 360 degrees or more does not limit anything.
- A range whose `max` is below its `min` wraps through 180: `[90, -90]` is the rear half (from 90 through 180 to 270) and `[-10, -20]` is the 350-degree sector that leaves out only the gap between -20 and -10. Write `[-90, 90]` for the front half. Before 12.0 an inverted range was passed to `Mathf.Clamp` and pinned the aim to one of its ends.

When the follower sits exactly on the target (for example a 2D follower with zero offset that reached it), there is no direction to aim along: the follower keeps its current rotation for that frame instead of snapping to a fixed angle.

---

## 6. Usage Examples

### Camera behind the player (3D)
```csharp
followMode = ThreeD
followPosition = true
positionSmoothMode = MoveTowards
positionSpeed = 10
offset = (0, 5, -10)

followRotation = true
rotationSmoothMode = MoveTowards
rotationSpeed = 180  // default
```

### Camera with a deadzone (2D platformer)
```csharp
followMode = TwoD
followPosition = true
positionSmoothMode = Exponential
positionSpeed = 5
offset = (0, 2, -10)

deadzone.enabled = true
deadzone.radius = 2  // camera doesn't move while the player is within 2 units
```

### Homing missile
```csharp
followMode = ThreeD
followPosition = true
positionSmoothMode = Lerp
positionSpeed = 10

followRotation = true
rotationSmoothMode = Exponential
rotationSpeed = 8
```

### Camera constrained to the level
```csharp
followPosition = true
limitX.enabled = true
limitX.min = -50
limitX.max = 50

limitY.enabled = true
limitY.min = 0
limitY.max = 20
```

### AI enemy chasing the player
```csharp
followMode = ThreeD
followPosition = true
positionSmoothMode = MoveTowards
positionSpeed = 5

distanceControl.activationDistance = 10  // starts chasing at 10 units
distanceControl.stoppingDistance = 2     // stops 2 units from the player

onStartFollowing → StartAttackAnimation()
onStopFollowing → StopAttackAnimation()
```

### Pet following the player
```csharp
followMode = ThreeD
followPosition = true
positionSmoothMode = SmoothDamp
positionSpeed = 3
offset = (-2, 0, -2)  // behind and to the side

distanceControl.activationDistance = 3  // starts catching up at 3 units
distanceControl.stoppingDistance = 1    // stops at 1 unit

deadzone.enabled = true
deadzone.radius = 1.5  // doesn't twitch on small movements
```

---

## 7. Smoothing Modes in Detail

### MoveTowards (default, constant speed)
```csharp
positionSpeed = 10  // units per second
```
- Constant movement speed regardless of distance
- `Vector3.MoveTowards` moves the object at a fixed speed
- Ideal for cameras, simple following, "catch up to the player" mechanics
- Predictable behavior: speed is always the same

### Lerp (smooth deceleration)
```csharp
positionSpeed = 5  // speed from 1 to 10
```
- Simple and predictable
- Critical bug fixed: now uses `Clamp01` to prevent extrapolation
- Suitable for cameras and UI elements

### SmoothDamp (the smoothest)
```csharp
positionSpeed = 3  // damping time: 1/speed seconds
```
- The smoothest and most natural movement
- Automatically slows down when approaching the target
- Ideal for cameras following the player

### Exponential (the professional choice)
```csharp
positionSpeed = 5  // speed from 3 to 8
```
- Exponential decay for natural physics
- Framerate independent
- Used in AAA games

### None (for special cases)
- Instant teleportation with no smoothing
- Use only if you need a rigid attachment

---

## 8. Public Methods (code API)

### Target Control
- `SetTarget(Transform newTarget)`: Sets a new target
- `GetTarget()`: Returns the current target
- `TeleportToTarget()`: Instant snap to the target (no smoothing, deadzone or stopping distance) on the channels the follower follows. With `followPosition` it moves to the target position (position limits still apply, the SmoothDamp velocity is reset); with rotation following it turns straight to the aim from the new position. A rotation-only follower (such as a turret) keeps its position, and a position-only follower keeps its rotation.

### Follow Control
- `SetFollowPosition(bool enabled)`: Enable/disable position following
- `SetFollowRotation(bool enabled)`: Enable/disable rotation following
- `IsFollowing()`: Returns true if currently following

### Speed and Mode Control
- `SetPositionSpeed(float speed)`: Set the movement speed
- `SetRotationSpeed(float speed)`: Set the rotation speed
- `SetPositionSmoothMode(SmoothMode mode)`: Change the position smoothing mode
- `SetRotationSmoothMode(SmoothMode mode)`: Change the rotation smoothing mode

### Distance Control
- `SetActivationDistance(float distance)`: Set the activation distance
- `SetStoppingDistance(float distance)`: Set the stopping distance
- `GetDistanceToTarget()`: Get the current distance to the target

### Deadzone
- `SetDeadzoneEnabled(bool enabled)`: Enable/disable the deadzone
- `SetDeadzoneRadius(float radius)`: Set the deadzone radius

### Offset
- `SetOffset(Vector3 newOffset)`: Change the offset relative to the target

---

### Code Usage Examples

```csharp
// Get the component
Follow follow = GetComponent<Follow>();

// Change the follow target
follow.SetTarget(newPlayer);

// Double the speed while running
follow.SetPositionSpeed(normalSpeed * 2f);

// Switch to smooth following
follow.SetPositionSmoothMode(Follow.SmoothMode.SmoothDamp);

// AI: start chasing
follow.SetActivationDistance(0f);  // always follows
follow.SetStoppingDistance(2f);     // stops at 2 units

// Instantly move the camera to the player
follow.TeleportToTarget();

// State check
if (follow.IsFollowing())
{
    Debug.Log("Camera is following the target");
}

// Dynamic deadzone tuning
if (playerRunning)
{
    follow.SetDeadzoneRadius(0.5f);  // smaller deadzone while running
}
else
{
    follow.SetDeadzoneRadius(2f);    // larger while walking
}
```

---

## 9. Debugging

### Visualization (showDebugGizmos)
When enabled, displays:
- **Green sphere**: deadzone radius
- **Yellow sphere**: activationDistance radius (around the current position)
- **Red sphere**: stoppingDistance radius (around the target position)
- **Cyan line**: link between the object and the target (offset included)

---

## 10. Technical Details

### Fixed Bugs
- ❌ **Before**: `Lerp` with `smoothSpeed * Time.smoothDeltaTime` could produce values > 1 (extrapolation)
- ✅ **After**: `Lerp` with `Clamp01(smoothSpeed * Time.deltaTime)` (correct interpolation)

- ❌ **Before**: Used the unstable `Time.smoothDeltaTime`
- ✅ **After**: Uses the stable `Time.deltaTime`

- ❌ **Before**: Limit check via `Vector2.zero` did not work for genuinely zero bounds
- ✅ **After**: Explicit `limit.enabled` check

### Performance
- Early exit when there is no target
- The follower's position is read once per frame and written only when it actually changes
- The rotation is written only when it changes: a frame that computes the same rotation as the last write, while the transform still holds what that write stored, is skipped. With `None` rotation smoothing (or `MoveTowards` once the aim is reached) an aligned follower, including one with a non-zero offset, stops dirtying its transform hierarchy; `Lerp`, `SmoothDamp` and `Exponential` keep writing while their step still changes the result. A rotation changed from outside is always corrected on the next frame.
- No per-frame allocations (guarded by the EditMode tests `Follow_WarmLateUpdate_DoesNotAllocateAndAnIdleFollowerDoesNotRewriteItsTransform` and `Follow_AlignedFollowerWithAnOffset_StopsRewritingItsRotationButFixesExternalTurns`)
