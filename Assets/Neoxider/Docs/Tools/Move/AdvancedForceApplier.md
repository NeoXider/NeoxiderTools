# AdvancedForceApplier

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/AdvancedForceApplier.cs`, namespace `Neo.Tools`, assembly `Neo.Tools.Move`) that applies a configured force or impulse to a 3D `Rigidbody` or a `Rigidbody2D`, with direction modes, randomized magnitude and an optional speed cap.

**How to use:**
1. Add the component next to a `Rigidbody` or `Rigidbody2D`. `Auto` prefers 3D when both are present.
2. Configure the force mode, direction mode and the default or randomized magnitude.
3. Leave `playOnAwake` on to apply the force in `Start`, or call `ApplyForce()` from code or a UnityEvent. **Apply Now** is a Play Mode-only Inspector button.
4. Connect `OnApplyForce` or `OnApplyFailed` to scene effects. Failure means no body of the selected type.

---

## C# API

`ApplyForce(float force = 0f, Vector3? direction = null)` uses a positive explicit magnitude, otherwise
the configured random range/default. An explicit direction overrides direction settings.
`SetTarget`, `SetDirectionMode` and `SetCustomDirection` also support UnityEvent-driven scene workflows.

```csharp
[SerializeField] private AdvancedForceApplier _launcher;

public void LaunchAt(Transform enemy)
{
    _launcher.SetTarget(enemy);
    _launcher.SetDirectionMode(AdvancedForceApplier.DirectionMode.ToTarget);
    _launcher.ApplyForce(25f);
}
```

With `TransformForward`, `useLocalForward = true` follows local forward (local right in 2D); `false`
uses world forward (world right in 2D). `invertDirection` reverses computed directions.
Stationary/missing-target directions fall back to forward in 3D or right in 2D. A 2D force discards Z
and uses the same fallback when no XY component remains, so the default stationary 2D setup still moves.

## Speed cap

Forces are integrated by the next physics step, so the velocity read right after `AddForce` is still the
old one. With `clampMaxSpeed` on, `ApplyForce` clamps the current velocity immediately and then again in the
next two `FixedUpdate` calls, which follow the step that integrates the force whether the call came from
`Update`, `FixedUpdate` or a physics callback. Enable `clampSpeedEveryFixedUpdate` to keep the cap on
permanently (for example when other systems also push the body).

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| `bodyType` | `Auto` (3D first, then 2D), `Rigidbody3D` or `Rigidbody2D`. |
| `defaultForce` | Magnitude used when `ApplyForce` receives no positive value and randomization is off. |
| `randomizeForce` / `forceRange` | Pick the magnitude uniformly from `forceRange` instead of `defaultForce`. |
| `playOnAwake` | Apply the configured force in `Start` (name kept for existing scenes). |
| `forceMode3D` / `forceMode2D` | Unity force mode for the active body type. |
| `clampMaxSpeed` / `maxSpeed` | Cap the body speed after each `ApplyForce` (see Speed cap). |
| `clampSpeedEveryFixedUpdate` | Keep clamping in every `FixedUpdate` while `clampMaxSpeed` is on. |
| `directionMode` | `Velocity`, `TransformForward`, `CustomVector` or `ToTarget`. |
| `useLocalForward` | `TransformForward`: local forward/right instead of world axes. |
| `invertDirection` | Reverse the computed direction. |
| `customDirection` | Direction for `CustomVector`. |
| `target` | Target for `ToTarget`. |
| `OnApplyForce` / `OnApplyFailed` | Raised after a successful application, or when no suitable body exists. |

## See Also

- [Tools/Move](./README.md)
