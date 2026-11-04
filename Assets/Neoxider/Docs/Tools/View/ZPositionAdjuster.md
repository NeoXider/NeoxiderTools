# ZPositionAdjuster

**What it is:** a MonoBehaviour that derives world Z from the normalized world Y every `LateUpdate`, for
2D depth ordering. Script: `Scripts/Tools/View/ZPositionAdjuster.cs`, namespace `Neo.Tools`, assembly
`Neo.Tools.View`.

**How to use:**
1. Attach it to the object whose world depth should follow its vertical position.
2. Set `_ratio` (and the normalization mode) in the Inspector, or `Ratio` / `UseNormalizeToUnit` from code.
3. Move the object normally; Z follows on the next `LateUpdate`, or call `Adjust()` to apply at once.

---

## Setup

- It updates in `LateUpdate` and previews deterministically in `OnValidate`.
- X and Y are preserved; only world Z is replaced. A parent transform therefore affects the derived
  local position. No UnityEvent wiring is required.
- The transform is written only when Z actually changes, so a resting object triggers no per-frame
  transform-changed work (child propagation, physics sync) and `OnValidate` does not dirty scenes needlessly.

## Fields

| Field | Description |
|-------|-------------|
| `_useNormalizeToUnit` | True: normalize Y to 0..1; false: normalize to -1..1. |
| `_ratio` | Multiply the normalized value to get Z; authoring minimum is zero. |

Normalization reuses `Neo.Extensions` defaults (-1,000,000 to 1,000,000), with clamping at those limits.
This is a normalized depth helper rather than a direct `Z = Y * ratio` mapping.

## C# API

| Member | Description |
|--------|-------------|
| `Ratio` | Multiplier; negative values are clamped to zero. |
| `UseNormalizeToUnit` | Normalization mode. |
| `Adjust()` | Applies the derived Z now. |
| `static ComputeDepth(float y, bool useNormalizeToUnit, float ratio)` | The Z `Adjust` would assign, without a scene object. |

```csharp
float z = ZPositionAdjuster.ComputeDepth(unit.position.y, true, 2f);
```

## See also

- [Module root](README.md)
