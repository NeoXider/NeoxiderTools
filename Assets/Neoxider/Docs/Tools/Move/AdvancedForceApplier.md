# AdvancedForceApplier

Applies a configured force/impulse to a 3D or 2D Rigidbody. Namespace `Neo.Tools`, assembly `Neo.Tools.Move`.

## Setup

- Add the component alongside a Rigidbody or Rigidbody2D. `Auto` prefers 3D when both are present.
- Configure the force mode, direction and optional randomized magnitude.
- `playOnAwake` applies the configured default/randomized force in `Start`. The serialized field name
  is retained for existing scenes. **Apply Now** is a Play Mode-only Inspector button.
- Connect `OnApplyForce` or `OnApplyFailed` to scene effects. Failure means no body of the selected type.

## C# API

`ApplyForce(float force = 0f, Vector3? direction = null)` uses a positive explicit magnitude, otherwise
the configured random range/default. An explicit direction overrides direction settings.
`SetTarget`, `SetDirectionMode` and `SetCustomDirection` also support UnityEvent-driven scene workflows.

With `TransformForward`, `useLocalForward = true` follows local forward (local right in 2D); `false`
uses world forward (world right in 2D). `invertDirection` reverses computed directions.
Stationary/missing-target directions fall back to forward in 3D or right in 2D. A 2D force discards Z
and uses the same fallback when no XY component remains, so the default stationary 2D setup still moves.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `BodyType` | Body Type. |
| `DirectionMode` | Direction Mode. |
| `OnApplyFailed` | On Apply Failed. |
| `OnApplyForce` | On Apply Force. |
| `bodyType` | Body Type. |
| `clampMaxSpeed` | Clamp Max Speed. |
| `clampSpeedEveryFixedUpdate` | Clamp Speed Every Fixed Update. |
| `customDirection` | Custom Direction. |
| `directionMode` | Direction Mode. |
| `forceMode2D` | Force Mode2D. |
| `forceMode3D` | Force Mode3D. |
| `forceRange` | Force Range. |
| `invertDirection` | Invert Direction. |
| `maxSpeed` | Max Speed. |
| `playOnAwake` | Play On Awake. |
| `randomizeForce` | Randomize Force. |
| `rigidbody2D` | Rigidbody2D. |
| `rigidbody3D` | Rigidbody3D. |
| `target` | Target. |
| `useLocalForward` | Use Local Forward. |

## See Also

- [Module Root](../README.md)
