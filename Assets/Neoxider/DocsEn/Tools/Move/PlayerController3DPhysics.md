# PlayerController3DPhysics

**Purpose:** A robust, Rigidbody-based 3D first-person (or third-person) character controller. Features walking, sprinting, jumping (with coyote time and input buffering), mouse-look with sensitivity settings, and built-in integration with `CursorLockController` and game pause states. Supports both Legacy Input Manager and the New Input System out of the box.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Rigidbody** | Reference to the character's `Rigidbody`. Assigned automatically if left empty. |
| **Camera Pivot** | Transform used for vertical camera rotation (Pitch). Defaults to `Camera.main`. |
| **Walk / Run Speed** | Movement speeds for walking and sprinting. |
| **Jump Impulse** | Upward force applied when jumping. Can be disabled via `Can Jump`. |
| **Ground Check Radius** | The radius of the sphere used for ground detection (OverlapSphere). |
| **Look Yaw Mode** | How to handle horizontal rotation: `RotateCharacter`, `RotateCameraPivot`, or `RotateBoth`. |
| **Use Game Settings Mouse Sensitivity** | Pulls mouse sensitivity dynamically from `GameSettings`. |
| **Enable Cursor Control** | **On by default.** When enabled, this component may lock/unlock the cursor (Start, Escape, `SetCursorLocked`, auto-lock when enabling look with *Pause Look When Cursor Visible*). When **disabled**, it never touches `Cursor`—useful when `CursorLockController` or your UI owns the pointer. Movement and mouse look still work. |
| **Lock Cursor On Start** | In **`Start()`** (not `Awake`), lock and hide the mouse cursor when entering play. Ignored if **Enable Cursor Control** is off or an active external `CursorLockController` is assigned. |
| **Pause Look When Cursor Visible** | While the cursor is visible (unlocked), do not apply mouse look. |
| **Disable Look On Pause** | Automatically disable mouse-look when `EventManager.OnPause` is invoked. |
| **Toggle Cursor On Escape** | Toggle cursor lock and look with Escape. Does nothing if **Enable Cursor Control** is off. |

### Cursor and startup order

Cursor locking when **Lock Cursor On Start** is enabled happens in **`Start()`**, not `Awake`. To prevent this controller from changing the cursor at all, turn off **Enable Cursor Control** in the Inspector (or set `CursorControlEnabled = false` before the first frame).

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SetMovementEnabled(bool enabled)` | Enables/disables movement input processing (walk/sprint). |
| `void SetJumpEnabled(bool enabled)` | Enables/disables jumping. |
| `void SetLookEnabled(bool enabled)` | Enables/disables mouse-look input processing. |
| `void SetCursorLocked(bool locked)` | Locks or unlocks the cursor. **No-op** when **Enable Cursor Control** is off. |
| `bool CursorControlEnabled { get; set; }` | Enable/disable all cursor changes from this component (default `true`). |
| `void Teleport(Vector3 worldPosition)` | Instantly moves the character and kills any current velocity/momentum. |
| `void SetMoveInput(Vector2? input)` | Override input for on-screen joysticks. Pass `null` to revert to hardware input. |
| `bool IsGrounded { get; }` | Returns whether the character is currently on the ground. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnJumped` / `OnLanded` | *(none)* | Fired exactly when the character jumps or touches the ground. |
| `OnMoveStart` / `OnMoveStop` | *(none)* | Fired when the character begins moving (input > 0) or comes to a halt. |

## Examples

### No-Code Example (Inspector)
Create a Capsule with a `Rigidbody`. Attach `PlayerController3DPhysics`. Make a Camera a child of the capsule and drag it into the `Camera Pivot` field. Set your `Ground Mask` to the layer of your floor. Press Play — you can immediately walk (WASD), jump (Space), and look around (Mouse).

### Code Example
```csharp
[SerializeField] private PlayerController3DPhysics _player;

public void ImmobilizePlayerForCutscene()
{
    // Prevent the player from walking or looking around during a cutscene
    _player.SetMovementEnabled(false);
    _player.SetLookEnabled(false);
}
```

## See Also
- [CursorLockController](CursorLockController.md)
- [KeyboardMover](KeyboardMover.md)
- ← [Tools/Move](../README.md)
