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
| **Enable Cursor Control** | **On by default.** When enabled, this component may lock/unlock the cursor (Start, Escape, `SetCursorLocked`, auto-lock when enabling look with *Pause Look When Cursor Visible*). When **disabled**, it never touches `Cursor` on any path — the full opt-out for games with their own cursor system. Movement and mouse look still work. |
| **Lock Cursor On Start** | In **`Start()`** (not `Awake`), lock and hide the mouse cursor when entering play. Ignored if **Enable Cursor Control** is off or an active `CursorLockController` owns the cursor. |
| **Pause Look When Cursor Visible** | While the cursor is visible (unlocked), do not apply mouse look. Kept as a backstop even when a `CursorLockController` drives this player. |
| **Disable Look On Pause** | Automatically disable mouse-look when `EventManager.OnPause` is invoked. |
| **Toggle Cursor On Escape** | Toggle cursor lock and look with Escape. Skipped if **Enable Cursor Control** is off or an active `CursorLockController` owns the cursor. |
| **External Cursor Lock Controller** | Optional explicit reference to the authoritative `CursorLockController`. Auto-filled in `Awake` from a same-object controller; also bound automatically by a `CursorLockController` that references this player in its *Player Control* list. |

### Cursor ownership

**Esc owner = `CursorLockController`; this controller defers automatically.** When an active `CursorLockController` owns the cursor (`HasExternalCursorControl()` is true), the player skips **all** of its own cursor paths: no lock-on-start, no Escape handling, and `SetCursorLocked` forwards the request to the owner instead of writing `Cursor` directly. The owner suspends look (and optionally movement) while the cursor is visible and restores them on lock. Scenes **without** a `CursorLockController` are unaffected — the standalone controller keeps its full current behaviour.

### Cursor and startup order

Cursor locking when **Lock Cursor On Start** is enabled happens in **`Start()`**, not `Awake`. To prevent this controller from changing the cursor at all, turn off **Enable Cursor Control** in the Inspector (or set `CursorControlEnabled = false` before the first frame).

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SetMovementEnabled(bool enabled)` | Enables/disables movement input processing (walk/sprint). |
| `void SetJumpEnabled(bool enabled)` | Enables/disables jumping. |
| `void SetLookEnabled(bool enabled)` | Enables/disables mouse-look input processing. |
| `void SetCursorLocked(bool locked)` | Locks or unlocks the cursor. **No-op** when **Enable Cursor Control** is off; **forwards to the owning `CursorLockController`** when one is active. |
| `bool CursorControlEnabled { get; set; }` | Enable/disable all cursor changes from this component (default `true`). |
| `bool HasExternalCursorControl()` | True when an active `CursorLockController` owns the cursor and this component defers to it. |
| `CursorLockController ExternalCursorLockController { get; }` | The cursor controller this player defers to, if any. |
| `void SetExternalCursorLockController(CursorLockController controller)` | Assigns (or clears with `null`) the authoritative cursor controller. |
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
Create a Capsule with a `Rigidbody`. Attach `PlayerController3DPhysics`. Make a Camera a child of the capsule and drag it into the `Camera Pivot` field. Set your `Ground Mask` to the layer of your floor. Press Play - you can immediately walk (WASD), jump (Space), and look around (Mouse).

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
- [KeyboardMover](MovementToolkit/KeyboardMover.md)
- <- [Tools/Move](../README.md)
