# NpcAnimatorDriver

**Purpose:** Drives an Animator from the `NavMeshAgent` velocity so walk/run clips follow movement. Sets a normalized `Speed` float (normally 0..1, agent velocity / agent speed, smoothed) and an `IsMoving` bool.

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
- A disabled or unbound agent contributes zero speed and `IsMoving = false`; its native stop state is not queried off the NavMesh.
- `NpcAnimationCore.Tick(float deltaTime)` uses the supplied time step for smoothing. A zero step preserves the speed value; callers can choose their own update clock. The component passes `Time.deltaTime`.
- The speed value is a ratio and can exceed 1 if external motion exceeds the configured agent speed.

## Direct C# usage

```csharp
NpcAnimationCore animation = new NpcAnimationCore(agent, animator, "Speed", "IsMoving", 0.1f);
animation.Tick(deltaTime);
```

Use `Neo.NPC.Navigation` for the core type. The caller owns the agent and Animator; the core does not
create or destroy them. The component workflow above provides the same calls from its `Update`.

## See Also

- [Module Root](./README.md)
- [NPCNavigation](./Navigation/NPCNavigation.md)
