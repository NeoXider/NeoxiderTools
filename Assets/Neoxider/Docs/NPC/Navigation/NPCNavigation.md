# NpcNavigation

**Purpose:** NavMeshAgent-driven NPC movement with three switchable behaviours: FollowTarget, Patrol, and Combined (patrol until a target enters aggro range, then chase). Composed from ordinary C# adapters over Unity APIs (`NpcNavAgentCore`, `NpcFollowTargetCore`, `NpcPatrolCore`, `NpcDestinationResolver`).

## Setup

- Add via `Add Component -> Neoxider/NPC/NpcNavigation` (auto-adds a `NavMeshAgent`), or create the ready prefab from `Neoxider/NPC/NpcNavigation`.
- Bake a NavMesh for the scene.
- Assign a `Follow Target` (FollowTarget mode), `Patrol Points`/`Patrol Zone` (Patrol mode), or `Combined Target` + `Patrol Points`/`Patrol Zone` (Combined mode).

## Modes

| Mode | Behaviour |
|------|-----------|
| `FollowTarget` | Continuously paths toward `followTarget`, respecting `triggerDistance`. |
| `Patrol` | Walks `patrolPoints` in order (or random points inside `patrolZone`), waiting `patrolWaitTime` at each. |
| `Combined` | Patrols until `combinedTarget` is within `aggroDistance`, chases it, and returns to patrol past `maxFollowDistance`, when the target disappears, or when sampling fails. |

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

- `IsActive` (get/set) — stop/resume movement. `Mode`, `FollowTarget`, `CombinedTarget`, `IsChasing` are read-only state.
- `SetMode(NavigationMode)` — switch behaviour at runtime.
- `SetFollowTarget(Transform)` / `SetCombinedTarget(Transform)` — retarget.
- `SetRunning(bool)` / `SetSpeed(float)` — speed control.
- `SetDestination(Vector3)` — path to an explicit point (FollowTarget mode); returns false before initialization, off the NavMesh, or when the request is rejected.
- `Stop()` / `Resume()` — pause/continue; both are Play Mode-only Inspector buttons, disabled while
  editing the scene.

## Lifecycle and path results

`SetMode`, `SetFollowTarget`, and `SetRunning` may be called before `Awake`, including from another
component's `Awake`. Mode entry is deferred while the navigation component is disabled. `Stop` and
`Resume` update `IsActive` safely even if the agent is disabled or not on a NavMesh; native stop state
is changed only for a bound agent. Disabling navigation stops the agent. On enable, it enters the
selected behaviour and resumes only when `IsActive` is true.

`Awake` emits one initial `onModeChanged` notification before any behaviour enters. A listener may
call `SetMode` during that notification: the last requested mode is selected for the first `OnEnable`,
without entering any superseded behaviour or emitting another startup mode notification. The event
argument describes the initially selected mode; read `Mode` after startup for the final selection.
This applies to Inspector event handlers and C# listeners attached before activating an inactive NPC.

With automatic updates enabled, a new follow destination (including world origin) is requested on
the first tick; a cleared path to an unchanged target is retried. Combined mode submits its initial
chase path even when `autoUpdatePath` is false. Replacing `CombinedTarget` during a chase retargets
the follow core; losing it returns to patrol. A failed chase can be attempted again on later ticks.
Set `maxFollowDistance` at least as large as `aggroDistance` to avoid conflicting thresholds.

`onPathUpdated` means the agent accepted a sampled destination, not that a complete route exists.
Partial/invalid paths report `onPathBlocked`; they do not report arrival at their endpoint. A patrol
with no valid waypoint or no sampled zone destination stops, and does not emit `onPatrolStarted` for
that failed start. After changing its configuration, restart the patrol through a mode change or the
direct patrol core API. A partial patrol path remains blocked and needs caller-directed recovery.

`followMovementBounds` clamps the desired destination in collider-local coordinates. Sampling can
snap it outside those bounds, and the calculated route can leave the box. Likewise, the resolver's
ring fallback can extend beyond `maxSampleDistance`; this is a sampling radius, not a strict travel
or containment guarantee. Configure the NavMesh and areas for physical movement restrictions.

## Events

`onMovementStarted`, `onMovementStopped`, `onModeChanged(NavigationMode)`, `onTargetChanged(Transform)`, `onDestinationReached(Vector3)`, `onPathBlocked(Vector3)`, `onPathUpdated(Vector3)`, `onPathStatusChanged(NavMeshPathStatus)`, `onPatrolStarted`, `onPatrolCompleted`, `onPatrolPointReached(int)`, `onStartFollowing`, `onStopFollowing`, `onSpeedChanged(float)`.

## See Also

- [Module Root](../README.md)
- [NpcAnimatorDriver](../NpcAnimatorDriver.md)
- [NpcTargetFinder](../NpcTargetFinder.md)
