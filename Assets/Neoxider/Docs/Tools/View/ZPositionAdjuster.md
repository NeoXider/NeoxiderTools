# ZPositionAdjuster

**Purpose:** Derive world Z from normalized world Y for 2D depth ordering. Namespace: `Neo.Tools`;
assembly: `Neo.Tools.View`.

## Setup

- Attach to the object whose world depth should follow its vertical position. Configure the ratio in
  the Inspector. It updates in `LateUpdate` and previews deterministically in `OnValidate`.
- X and Y are preserved; only world Z is replaced. A parent transform therefore affects the derived
  local position. Use ordinary transform movement from C#; no UnityEvent wiring is required.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_useNormalizeToUnit` | True: normalize Y to 0..1; false: normalize to -1..1. |
| `_ratio` | Multiply the normalized value to get Z; authoring minimum is zero. |

Normalization reuses `Neo.Extensions` defaults (-1,000,000 to 1,000,000), with clamping at those limits.
This is a normalized depth helper rather than a direct `Z = Y * ratio` mapping.

## See Also

- [Module Root](../README.md)
