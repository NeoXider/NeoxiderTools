# ImpulseZone

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Physics/ImpulseZone.cs`, namespace `Neo.Tools`) on a trigger collider that applies an instant impulse to rigidbodies entering it: trampolines, jump pads, wind gusts or speed boosters.

**How to use:**
1. Add a 3D collider (for example `BoxCollider`) and **Neoxider > Tools > Physics > ImpulseZone** to the same GameObject; `Awake` marks the collider as a trigger.
2. Set **Impulse Force**, **Direction**, **Affected Layers** and optional **Required Tag**, **One Time Only** or **Cooldown**.
3. Give the targets a dynamic `Rigidbody` and collider. Use `ApplyImpulseToObject(target)` to push from code, and `OnImpulseApplied` for sound or animation.

---

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
| `void SetImpulseForce(float newForce)` / `float ImpulseForce` | Change (negative values clamp to zero) or read the push strength. |
| `void SetDirection(ImpulseDirection newDirection)` | Select how the direction is computed. |
| `void SetDirectionIndex(int directionIndex)` / `ImpulseDirection CurrentDirection` | Select the direction mode by its `ImpulseDirection` index (UnityEvents cannot pass an enum; an undefined index is ignored), or read it. |
| `void SetCustomDirection(Vector3 newDirection)` | Set a world-space direction and select `Custom`. |
| `void SetCooldown(float seconds)` | Change the per-Rigidbody cooldown; 0 disables the cooldown check, including running deadlines. |
| `LayerMask AffectedLayers { get; set; }` | Layers allowed to receive the impulse. |
| `void ClearProcessedObjects()` | Clear both one-shot history and cooldowns, allowing objects to be pushed again. |

Direction calculations use the attached body's world center of mass. Filter layers/tags are taken from the collider GameObject, including for child colliders. A manual call during an enter/applied event cannot recursively push the same body again before the current call completes.

If an `OnObjectEntered` listener immediately destroys the zone component or its GameObject, the pending impulse is cancelled and `OnImpulseApplied` does not fire. Explicit manual calls on a live disabled component remain supported and still obey filters, one-shot history and cooldown.

Cooldowns are checked against their deadline on every impulse. The per-frame bookkeeping that forgets expired cooldowns and destroyed bodies runs only when the earliest deadline has passed (and at most once per second for one-shot history), so idle zones cost almost nothing per frame: a destroyed body's cooldown entry is forgotten on the first scan after that deadline, not at once, and neither the skipped nor the due pass allocates (guarded by the EditMode test `ImpulseZone_WarmUpdateWithLiveCooldowns_SkipsTheScanUntilDueAndDoesNotAllocate`). A warm `ApplyImpulseToObject` call makes no managed allocations (guarded by the EditMode test `ImpulseZone_WarmManualImpulse_DoesNotAllocate`); trigger contacts run the same impulse path.

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
    _jumpPad.SetImpulseForce(0f);
}

public void TurnIntoUpwardBooster()
{
    _jumpPad.SetCustomDirection(Vector3.up);
    _jumpPad.SetCooldown(0.5f);
    _jumpPad.SetImpulseForce(25f);
}
```

## Repeated bounces

The zone pushes a body only in `OnTriggerEnter`. A body that comes to rest inside the trigger gets no further push.
For a repeating bounce, place the trigger above an elastic surface with a gap larger than the body, so the body
leaves the trigger and enters it again, as the Physics 3D demo (`Scenes/Tools/Physics/Physics3DDemo.unity`) does.

## See Also
- [MagneticField](MagneticField.md)
- [ExplosiveForce](ExplosiveForce.md)
- ← [Tools/Physics](README.md)
