# NpcNavigation

**Purpose:** NavMeshAgent-driven NPC movement with three switchable behaviours: FollowTarget, Patrol, and Combined (patrol until a target enters aggro range, then chase). Composed from small pure-C# cores (`NpcNavAgentCore`, `NpcFollowTargetCore`, `NpcPatrolCore`, `NpcDestinationResolver`).

## Setup

- Add via `Add Component -> Neoxider/NPC/NpcNavigation` (auto-adds a `NavMeshAgent`), or create the ready prefab from `Neoxider/NPC/NpcNavigation`.
- Bake a NavMesh for the scene.
- Assign a `Follow Target` (FollowTarget mode), `Patrol Points`/`Patrol Zone` (Patrol mode), or `Combined Target` + `Patrol Points`/`Patrol Zone` (Combined mode).

## Modes

| Mode | Behaviour |
|------|-----------|
| `FollowTarget` | Continuously paths toward `followTarget`, respecting `triggerDistance`. |
| `Patrol` | Walks `patrolPoints` in order (or random points inside `patrolZone`), waiting `patrolWaitTime` at each. |
| `Combined` | Patrols until `combinedTarget` is within `aggroDistance`, chases it, and returns to patrol past `maxFollowDistance`. |

## Key Fields (Inspector)

| Field | Default | Description |
|-------|---------|-------------|
| `isActive` | `true` | When false, the agent is stopped and behaviours are not ticked. |
| `mode` | `FollowTarget` | Active navigation behaviour. |
| `rotationPolicy` | `Agent` | `Agent` uses NavMeshAgent rotation; `ManualVelocity` rotates toward velocity via `turnSpeed`. |
| `walkSpeed` / `runSpeed` | `3` / `6` | Speeds; `SetRunning(true)` switches to run. |
| `stoppingDistance` | `2` | Distance at which the agent stops. |
| `triggerDistance` | `0` | FollowTarget: if > 0, the agent only moves while the target is within this distance. |
| `pathUpdateInterval` | `0.5` | Seconds between path recalculations while `autoUpdatePath` is on. |
| `maxSampleDistance` | `100` | Max distance used to snap a desired point to the NavMesh. |
| `patrolWaitTime` | `1` | Seconds waited at each patrol point. |
| `loopPatrol` | `true` | Loop the patrol route; when false, patrol completes at the last point. |
| `aggroDistance` | `10` | Combined: distance that starts chasing. |
| `maxFollowDistance` | `20` | Combined: distance that ends chasing (0 = never disengage). |

## Public API

- `IsActive` (get/set), `Mode`, `FollowTarget`, `CombinedTarget`, `IsChasing` — read-only state.
- `SetMode(NavigationMode)` — switch behaviour at runtime.
- `SetFollowTarget(Transform)` / `SetCombinedTarget(Transform)` — retarget.
- `SetRunning(bool)` / `SetSpeed(float)` — speed control.
- `SetDestination(Vector3)` — path to an explicit point (FollowTarget mode).
- `Stop()` / `Resume()` — pause/continue (also `[Button]` in the Inspector).

## Events

`onMovementStarted`, `onMovementStopped`, `onModeChanged(NavigationMode)`, `onTargetChanged(Transform)`, `onDestinationReached(Vector3)`, `onPathBlocked(Vector3)`, `onPathUpdated(Vector3)`, `onPathStatusChanged(NavMeshPathStatus)`, `onPatrolStarted`, `onPatrolCompleted`, `onPatrolPointReached(int)`, `onStartFollowing`, `onStopFollowing`, `onSpeedChanged(float)`.

## See Also

- [Module Root](../README.md)
- [NpcAnimatorDriver](../NpcAnimatorDriver.md)
- [NpcTargetFinder](../NpcTargetFinder.md)
