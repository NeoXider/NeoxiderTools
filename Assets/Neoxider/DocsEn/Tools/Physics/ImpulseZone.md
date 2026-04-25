# ImpulseZone

**Purpose:** A trigger zone (Collider) that applies an instant physical impulse to objects entering it. Perfect for trampolines, jump pads, wind tunnels, or speed boosters.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Impulse Force** | The strength of the push (uses ForceMode.Impulse). |
| **Direction** | Push direction: `AwayFromCenter` (push outward), `TowardsCenter` (pull inward), `TransformForward` (local forward), `Custom` (custom vector). |
| **Affected Layers** | Layers of objects that the zone is allowed to push. |
| **Required Tag** | (Optional) Only push objects with this specific tag (e.g., `Player`). |
| **One Time Only** | If true, each object gets pushed exactly once. Re-entering the zone won't work. |
| **Cooldown** | Delay in seconds before the same object can be pushed again. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void ApplyImpulseToObject(GameObject target)` | Forcibly push the specified object, as if it had entered the trigger. |
| `void SetImpulseForce(float newForce)` | Dynamically change the push strength. |
| `void ClearProcessedObjects()` | Clears the history (for `One Time Only`), allowing objects to be pushed again. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnObjectEntered` | `GameObject` | Fired when an object enters the trigger (before filter/cooldown checks). |
| `OnImpulseApplied` | `GameObject` | Fired immediately after an impulse is successfully applied to the object. |

## Examples

### No-Code Example (Inspector)
Place a BoxCollider on the ground shaped like a trampoline. Check `Is Trigger`. Add `ImpulseZone`, set Direction to `TransformForward` (and rotate the trigger to face upwards), set Force to `20`. Any Rigidbody falling onto it will now bounce up.

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
- ← [Tools/Physics](../README.md)
