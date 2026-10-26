# ImpulseZone

**Purpose:** A trigger zone (Collider) that applies an instant physical impulse to objects entering it. Perfect for trampolines, jump pads, wind tunnels, or speed boosters.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Impulse Force** | The strength of the push (uses ForceMode.Impulse). |
| **Direction** | Push direction: `AwayFromCenter` (push outward), `TowardsCenter` (pull inward), `TransformForward` (local forward), `Custom` (custom vector). |
| **Affected Layers** | Layers of objects that the zone is allowed to push. |
| **Required Tag** | (Optional) Only push objects with this specific tag (e.g., `Player`). |
| **One Time Only** | Each Rigidbody can receive one impulse until `ClearProcessedObjects()`. All its child colliders share the history. Cooldown expiry does not clear this history. |
| **Cooldown** | Scaled seconds before the same Rigidbody can receive another impulse. Uses a time deadline, including time spent with the zone disabled. |
| **Add Rigidbody If Needed** | Add a Rigidbody to a collider GameObject when it has no attached body. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void ApplyImpulseToObject(GameObject target)` | Apply an impulse using a collider on that GameObject, respecting layer/tag filters, one-shot history, and cooldown. Null targets or targets without a local collider are ignored. Does not emit `OnObjectEntered`. |
| `void SetImpulseForce(float newForce)` | Dynamically change the push strength. |
| `void ClearProcessedObjects()` | Clear both one-shot history and cooldowns, allowing objects to be pushed again. |

Direction calculations use the attached body's world center of mass. Filter layers/tags are taken from the collider GameObject, including for child colliders. A manual call during an enter/applied event cannot recursively push the same body again before the current call completes.

If an `OnObjectEntered` listener immediately destroys the zone component or its GameObject, the pending impulse is cancelled and `OnImpulseApplied` does not fire. Explicit manual calls on a live disabled component remain supported and still obey filters, one-shot history and cooldown.

Compatibility: manual calls now obey the same repeat limits as trigger calls. To intentionally repeat a one-shot impulse, call `ClearProcessedObjects()` first. A replacement Rigidbody has its own history; disabling/re-enabling the zone preserves history for existing bodies.

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnObjectEntered` | `GameObject` | Trigger calls only, after filters/repeat limits and before applying force. Also fires for an accepted collider with no Rigidbody when automatic creation is off. |
| `OnImpulseApplied` | `GameObject` | After submitting an impulse to the attached Rigidbody; payload is the collider GameObject. Kinematic bodies do not move. |

## Examples

### No-Code Example (Inspector)
Place a BoxCollider on the ground shaped like a trampoline, then add `ImpulseZone` (`Awake` sets `Is Trigger`). Set Direction to `TransformForward` and rotate the zone to face upwards, then set Force to `20`. Use a dynamic Rigidbody and collider on the player. Wire `OnImpulseApplied` to a sound or animation in the Inspector.

### Code Example
```csharp
[SerializeField] private ImpulseZone _jumpPad;

public void DisableJumpPad()
{
    // Disable the jump pad by removing its force
    _jumpPad.SetImpulseForce(0f);
}
```

## See Also
- [MagneticField](MagneticField.md)
- [ExplosiveForce](ExplosiveForce.md)
- ← [Tools/Physics](README.md)
