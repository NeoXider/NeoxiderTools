# MagneticField

**Purpose:** Creates a magnetic field that attracts or repels objects, or pulls them in a specific direction / toward a point. Automatically finds objects within its radius using `Physics.OverlapSphere`.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Mode** | Field behavior: `Attract` (pull to center), `Repel` (push away), `ToTarget` (pull to a Transform), `ToPoint` (pull to coordinates), `Direction` (constant pull along a vector). |
| **Field Strength** / **Radius** | The power and effective radius of the magnet. |
| **Falloff Type** | How the force diminishes over distance (`Linear`, `Quadratic`, `Constant`). |
| **Affected Layers** | The layer mask for objects that should be affected. |
| **Toggle** | Automatically alternate between attract and repel over time (e.g., pull for 2s, push for 2s). |
| **Add Rigidbody If Needed** | Automatically attaches a `Rigidbody` to objects caught in the field if they lack one. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void ToggleMode()` | Manually flip between attract and repel modes. |
| `void SetTarget(Transform target)` | Change the target Transform for `ToTarget` mode. |
| `void SetDirection(Vector3 newDirection, bool local = true)` | Set a new pull vector for `Direction` mode. |
| `int ObjectsInFieldCount { get; }` | Returns how many objects are currently influenced by the field. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `OnObjectEntered` | `GameObject` | Fired when an object enters the magnetic field radius. |
| `OnObjectExited` | `GameObject` | Fired when an object leaves the field. |
| `OnModeChanged` | `bool` | Fired when the phase switches (if `Toggle` is enabled). `True` = Attract phase. |

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
- ← [Tools/Physics](../README.md)
