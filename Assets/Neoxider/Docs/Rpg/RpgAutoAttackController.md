# RpgAutoAttackController

**Purpose:** Attempts the primary attack when a tagged target is within range.

## Setup

- Add the component alongside `RpgAttackController`; configure `targetTag`, range, attack interval,
  and target search interval. It triggers the controller's existing forward attack; it does not turn the actor.
- A missing or inactive cached target suspends attempts until a valid target is found. This prevents
  resources being spent on a pooled/deactivated actor while the next tag search is pending.
- For C# AI that chooses a specific target, use `RpgAttackController.TryUsePreset(preset, target, out reason)`.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `targetTag` | Target Tag. |

## See Also

- [Module Root](README.md)
