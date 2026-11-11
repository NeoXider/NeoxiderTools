# NpcNavigation

**What it is:** NavMeshAgent-driven NPC movement (`MonoBehaviour`, `Scripts/NPC/NpcNavigation.cs`, namespace
`Neo.NPC`) with three switchable behaviours: FollowTarget, Patrol, and Combined (patrol until a target enters aggro
range, then chase). Composed from ordinary C# adapters over Unity APIs (`NpcNavAgentCore`, `NpcFollowTargetCore`,
`NpcPatrolCore`, `NpcDestinationResolver`).

**How to use:**
1. Add `Neoxider/NPC/NpcNavigation` (adds a `NavMeshAgent`) and bake a NavMesh.
2. Pick a `Mode` and assign the follow target, patrol points/zone, or combined target.
3. Drive it from C# (`SetMode`, `SetFollowTarget`, `SetRunning`, `Stop`/`Resume`) or wire its UnityEvents.

```csharp
NpcNavigation navigation = GetComponent<NpcNavigation>();
navigation.SetCombinedTarget(player);
navigation.SetMode(NpcNavigation.NavigationMode.Combined);
navigation.onStartFollowing.AddListener(PlayAlertSound);
```

---

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
| `autoUpdatePath` | `true` | Re-path toward a moving target every `pathUpdateInterval`. When off, a path is requested only on target/mode changes and by `RefreshPath()`. |
| `pathUpdateInterval` | `0.5` | Seconds between path recalculations while `autoUpdatePath` is on; also the retry delay after a failed Combined chase. |
| `maxSampleDistance` | `100` | Max distance used to snap a desired point to the NavMesh. |
| `staggerPathUpdates` | `true` | Spreads the periodic re-path of NPCs retargeted in the same frame over one interval (deterministic per-instance phase, no `UnityEngine.Random`). Off keeps the exact legacy timing. |
| `patrolWaitTime` | `1` | Seconds waited at each patrol point. |
| `loopPatrol` | `true` | Loop the patrol route; when false, patrol completes at the last point. |
| `aggroDistance` | `10` | Combined: distance that starts chasing. |
| `maxFollowDistance` | `20` | Combined: distance that ends chasing (0 = never disengage). |

## Public API

- `IsActive` (get/set) — stop/resume movement. `Mode`, `FollowTarget`, `CombinedTarget`, `IsChasing`, `IsRunning` are read-only state.
- `SetMode(NavigationMode)` — switch behaviour at runtime.
- `SetFollowTarget(Transform)` / `SetCombinedTarget(Transform)` — retarget. In FollowTarget mode `SetFollowTarget`
  always raises `onTargetChanged` and requests a new path, even for the same target, so callers that run every
  frame should skip it when `FollowTarget` already matches.
- `SetRunning(bool)` / `SetSpeed(float)` — speed control.
- `SetDestination(Vector3)` — path to an explicit point (FollowTarget mode); returns false before initialization, off the NavMesh, or when the request is rejected.
- `AutoUpdatePath` (get/set) — the `autoUpdatePath` switch; setting it reconfigures a running follow at once.
- `RefreshPath()` — re-requests the path to the current follow target (or explicit destination) without raising
  `onTargetChanged`; use it to track a moving target while `AutoUpdatePath` is off. Returns false while nothing is
  followed (Patrol, Combined before a chase), off the NavMesh, or when the destination cannot be resolved.

```csharp
navigation.AutoUpdatePath = false;      // drive re-paths yourself
navigation.SetFollowTarget(player);     // first path is requested immediately
// ...later, e.g. from your own AI tick when the player moved far enough:
navigation.RefreshPath();
```
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
the follow core; losing it returns to patrol. A failed chase (unreachable target) returns to patrol and
is attempted again after `pathUpdateInterval` seconds, not on every frame; assigning a new combined target or
re-entering Combined mode allows an immediate attempt. Set `maxFollowDistance` at least as large as `aggroDistance`
to avoid conflicting thresholds.

## Performance

- The per-frame tick does not allocate (the movement-bounds constraint is a cached delegate).
- `Stagger Path Updates` gives every instance a deterministic phase, so NPCs spawned or retargeted together do not
  re-path in the same frame forever. The first request for a new target or destination is still immediate.
- Decision text for the Inspector `lastDecision` field is built only in the Editor or with `debugMode` on, so
  players do not pay for diagnostic strings.
- `NpcDestinationResolver` runs up to 61 `NavMesh.SamplePosition` queries for one unreachable point; the Combined
  retry delay bounds that to one search per interval per NPC.

`onPathUpdated` means the agent accepted a sampled destination, not that a complete route exists.
Partial/invalid paths report `onPathBlocked` once per path: it fires again only after a new path request
(re-path, new target or destination, `RefreshPath()`) or after the path became complete in between. Blocked
paths do not report arrival at their endpoint. A patrol
with no valid waypoint or no sampled zone destination stops, and does not emit `onPatrolStarted` for
that failed start. After changing its configuration, restart the patrol through a mode change or the
direct patrol core API. A partial patrol path remains blocked and needs caller-directed recovery.

`followMovementBounds` clamps the desired destination in collider-local coordinates. Sampling can
snap it outside those bounds, and the calculated route can leave the box. Likewise, the resolver's
ring fallback can extend beyond `maxSampleDistance`; this is a sampling radius, not a strict travel
or containment guarantee. Configure the NavMesh and areas for physical movement restrictions.

## Events

`onMovementStarted`, `onMovementStopped`, `onModeChanged(NavigationMode)`, `onTargetChanged(Transform)`, `onDestinationReached(Vector3)`, `onPathBlocked(Vector3)`, `onPathUpdated(Vector3)`, `onPathStatusChanged(NavMeshPathStatus)`, `onPatrolStarted`, `onPatrolCompleted`, `onPatrolPointReached(int)`, `onStartFollowing`, `onStopFollowing`, `onSpeedChanged(float)`.

Every event exists on a component added from code (`AddComponent<NpcNavigation>()`), so code-built NPCs can call
`navigation.onDestinationReached.AddListener(...)` right away without a null check.

## See Also

- [Module Root](../README.md)
- [NpcAnimatorDriver](../NpcAnimatorDriver.md)
- [NpcTargetFinder](../NpcTargetFinder.md)
