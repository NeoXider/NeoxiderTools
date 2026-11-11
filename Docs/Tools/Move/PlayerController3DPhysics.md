# PlayerController3DPhysics

> **Legacy since 10.3.0.** Superseded by the [CharacterController module](./CharacterController/README.md) (CMF motor + `NeoCharacterInput`), which adds slope limits with slide-off, stairs, moving platforms, momentum and camera collision. This component is tagged `[LegacyComponent]` and its menu entries moved to `Neoxider/Tools/Legacy/` — nothing is removed, existing scenes keep working, and the [migration table](./CharacterController/README.md#migrating-from-playercontroller3dphysics) covers the switch.

**What it is:** a `MonoBehaviour` (a Mirror `NetworkBehaviour` when Mirror is installed) in `Scripts/Tools/Move/PlayerController3DPhysics.cs`, namespace `Neo.Tools`: a Rigidbody-based 3D first-person (or third-person) character controller with walking, sprinting, jumping (coyote time and input buffering), mouse look, `CursorLockController` integration and pause handling. Supports both the Legacy Input Manager and the New Input System.

**How to use:**
1. Add the component (menu **Neoxider > Tools > Legacy > PlayerController3DPhysics**) to a capsule; a `Rigidbody` is required and set to interpolate.
2. Assign **Camera Pivot** (defaults to `Camera.main`), **Ground Check** and **Ground Mask**.
3. Drive it from code with `SetMoveInput`, `SetLookInput`, `SetJumpInput`, `SetMovementEnabled` and `SetLookEnabled`, or let it read keyboard/mouse input.

---

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

### Ground query completeness

Grounding keeps the configured `Ground Mask`, ignores trigger colliders and rejects colliders in the
character's own hierarchy. Its reusable overlap buffer grows and retries when saturated, so many
overlapping self/child colliders cannot hide a valid floor behind a truncated prefix. The initial
capacity of 16 is not a result limit. Growth allocates only when the previous capacity is insufficient;
different players still probe their own ground positions.

### Update timing

Input and look deltas are read in `Update`. The camera pivot (pitch, plus yaw in `RotateCameraPivot` mode) is
written every rendered frame, so looking stays smooth at frame rates above the physics rate. Body yaw goes
through `Rigidbody.MoveRotation`, and walking, jumping and extra gravity run in `FixedUpdate`; the body is
interpolated. The legacy-input availability probe is cached, so an Input System-only project does not pay an
exception per input read.

`OnEnable` remembers exactly which events it subscribed to (`GameSettings.OnSettingsChanged`, and `EM.OnPause` /
`EM.OnResume` of the `EM` instance present at that moment), and `OnDisable` removes exactly those, even if the
Inspector flags changed in between.

### Cursor ownership

**Esc owner = `CursorLockController`; this controller defers automatically.** When an active `CursorLockController` owns the cursor (`HasExternalCursorControl()` is true), the player skips **all** of its own cursor paths: no lock-on-start, no Escape handling, and `SetCursorLocked` forwards the request to the owner instead of writing `Cursor` directly. The owner suspends look (and optionally movement) while the cursor is visible and restores them on lock. Scenes **without** a `CursorLockController` are unaffected — the standalone controller keeps its full current behaviour.

`HasExternalCursorControl()` is also false while the owner is being disabled, deactivated or destroyed and this player stays in play (its scene stays loaded and its GameObject stays active). Look that the owner hands back during that cleanup therefore auto-locks the cursor like a standalone player, unless another `CursorLockController` still holds the cursor. A player that leaves play together with its owner keeps deferring, so nothing is re-locked: during a scene change (the player's own scene unloading) and when the player's own GameObject is deactivated or destroyed, for example with a `CursorLockController` on the player root.

An owner whose GameObject is inactive counts as absent, the same as one with `enabled = false`: the player keeps the binding (`ExternalCursorLockController`), but it handles Escape and lock-on-start itself and `SetCursorLocked` writes `Cursor` directly instead of forwarding to a controller that cannot run. Once the owner is active again the player defers to it again. Before 12.0 the player kept deferring to an inactive owner, so nothing handled Escape and a forwarded `SetCursorLocked` pushed the inactive owner onto the cursor stack.

> `HasExternalCursorControl()` checks that the controller exists, is `enabled`, has `ControllerEnabled` and sits on an active GameObject (during the controller's own disable/destroy cleanup it is true only for a player that leaves play with it) — it does **not** look at **Manage Cursor** or the global `GlobalCursorManagement` kill-switch. So if you keep a `CursorLockController` in the scene but turn **Manage Cursor** off, neither component locks the cursor on start or handles Escape: the player defers to an owner that has opted out. That is the intended setup only when you run your own cursor system (scenario (c) in the [CursorLockController matrix](./CursorLockController.md#choosing-a-cursor-setup)) — otherwise remove the controller instead of disabling its management.

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
| `bool HasExternalCursorControl()` | True when an enabled `CursorLockController` on an active GameObject owns the cursor and this component defers to it. |
| `CursorLockController ExternalCursorLockController { get; }` | The cursor controller this player defers to, if any. |
| `void SetExternalCursorLockController(CursorLockController controller)` | Assigns (or clears with `null`) the authoritative cursor controller. |
| `void Teleport(Vector3 worldPosition)` | Instantly moves the transform and the Rigidbody (so interpolation does not blend from the old pose) and kills any current velocity/momentum. |
| `void SetMoveInput(Vector2? input)` | Override input for on-screen joysticks. Magnitude is clamped to 1 and preserved: quarter input targets quarter walk/run speed. Pass `null` to revert to hardware input. |
| `void SetJumpInput()` | Queues a one-shot external jump command; also exposed as **Jump** in the Play Mode Inspector. |
| `bool IsGrounded { get; }` | Returns whether the character is currently on the ground. |

The Inspector **Jump** button calls `SetJumpInput()` and is disabled outside Play Mode. It follows the
same jump-enabled, authority, grounding, coyote-time, and input-buffer processing as external UI input.

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
    _player.SetMovementEnabled(false);
    _player.SetLookEnabled(false);
}
```

## See Also
- [CursorLockController](CursorLockController.md)
- [KeyboardMover](MovementToolkit/KeyboardMover.md)
- <- [Tools/Move](./README.md)
