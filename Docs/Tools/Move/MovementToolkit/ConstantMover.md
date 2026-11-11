# ConstantMover

**What it is:** a `MonoBehaviour` (`Scripts/Tools/Move/MovementToolkit/ConstantMover.cs`, namespace `Neo.Tools`) that moves its object at a constant speed along an authored direction, through the Transform, a `Rigidbody` or a `Rigidbody2D`.

**How to use:**
1. Add **Neoxider > Tools > ConstantMover** (create menu **Neoxider/Tools/Movement/ConstantMover**).
2. Choose `mode`, `directionSource` and `spaceLocal`, then set `speed`.
3. Change the speed at runtime with `SetSpeed(float)`, from code or a UnityEvent.

---

## Behaviour

- `Transform` mode moves in `Update`. `Rigidbody` / `Rigidbody2D` modes call `MovePosition` in `FixedUpdate`; when
  the selected body is missing on this object, the Transform is moved in `FixedUpdate` instead.
- `directionSource`: `LocalForward3D` (forward), `Up2D` (up), `Right2D` (right) or `Custom` (`customDirection`).
  With `spaceLocal` the direction follows the object's rotation; otherwise world axes are used. A zero direction
  falls back to world forward.
- `useDeltaTime = true`: `speed` is units per second; `false`: units per update. Negative speeds reverse.
- `lockX` / `lockY` / `lockZ` zero the world-space displacement on those axes.

```csharp
[SerializeField] private ConstantMover _conveyor;

public void Boost() => _conveyor.SetSpeed(_conveyor.speed * 2f);
```

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| `mode` | `Transform`, `Rigidbody` or `Rigidbody2D`. |
| `spaceLocal` | Direction in local space (true) or world space. |
| `useDeltaTime` | Speed per second (true) or per update. |
| `directionSource` / `customDirection` | Movement direction. |
| `speed` | Units per second (or per update). |
| `lockX` / `lockY` / `lockZ` | Suppress movement on world axes. |

## See Also

- [ConstantRotator](./ConstantRotator.md)
- [MovementToolkit](./README.md)
