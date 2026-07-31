# NpcAnimatorDriver

**Purpose:** Drives an Animator from the `NavMeshAgent` velocity so walk/run clips follow movement. Sets a normalized `Speed` float (0..1, agent velocity / agent speed, smoothed) and an `IsMoving` bool.

## Setup

- Add via `Add Component -> Neoxider/NPC/NpcAnimatorDriver` (auto-adds a `NavMeshAgent`).
- Put it on the same object as `NpcNavigation` and the `NavMeshAgent`.
- Give the Animator a float parameter (default `Speed`) and a bool parameter (default `IsMoving`), then blend clips on them.

## Key Fields (Inspector)

| Field | Default | Description |
|-------|---------|-------------|
| `animator` | (this GameObject) | Animator to drive; falls back to `GetComponent<Animator>()` when unset. |
| `speedParameter` | `Speed` | Animator float parameter that receives normalized speed (0..1). |
| `isMovingParameter` | `IsMoving` | Animator bool parameter set true while the agent moves. |
| `dampTime` | `0.1` | Smoothing time (seconds) for the speed transition. |

## Notes

- If the Animator or NavMeshAgent is missing at `Awake`, the driver stays idle (no errors).
- `Speed` is normalized by the agent's current `speed`, so it reads ~1 at full walk/run speed regardless of the absolute value.

## See Also

- [Module Root](./README.md)
- [NPCNavigation](./Navigation/NPCNavigation.md)
