# PlayerController3DPhysics

**Purpose:** A robust, Rigidbody-based 3D first-person (or third-person) character controller. Features walking, sprinting, jumping (with coyote time and input buffering), mouse-look with sensitivity settings, and built-in integration with `CursorLockController` and game pause states. Supports both Legacy Input Manager and the New Input System out of the box.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Rigidbody** | Reference to the character's `Rigidbody`. Assigned automatically if left empty. |
| **Camera Pivot** | Transform used for vertical camera rotation (Pitch). Defaults to `Camera.main`. |
| **Walk / Run Speed** | Movement speeds for walking and sprinting. |
| **Jump Impulse** | Upward force applied when jumping. Can be disabled via `Can Jump`. |
| **Ground Check Radius** | The radius of the spherecast used to detect ground. |
| **Look Yaw Mode** | How to handle horizontal rotation: `RotateCharacter`, `RotateCameraPivot`, or `RotateBoth`. |
| **Use Game Settings Mouse Sensitivity** | Pulls mouse sensitivity dynamically from `GameSettings`. |
| **Lock Cursor On Start** | Automatically lock and hide the mouse cursor when the scene starts. |
| **Disable Look On Pause** | Automatically disable mouse-look when `EventManager.OnPause` is invoked. |
| **Toggle Cursor On Escape** | Allows the player to unlock the cursor by pressing the Escape key. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SetMovementEnabled(bool enabled)` | Enables/disables movement input processing (walk/sprint). |
| `void SetJumpEnabled(bool enabled)` | Enables/disables jumping. |
| `void SetLookEnabled(bool enabled)` | Enables/disables mouse-look input processing. |
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
