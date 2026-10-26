# AiNavigation (Legacy)

**Purpose:** A NavMeshAgent-based AI navigation component. Supports three modes: target following (`FollowTarget`), waypoint or zone patrol (`Patrol`), and a combined mode (`Combined` — patrols until a target enters aggro range, then chases).

> ⚠️ **Deprecated.** Use `Neo.NPC.NpcNavigation + modules` instead.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Movement Mode** | `FollowTarget`, `Patrol`, or `Combined`. |
| **Target** | The Transform to follow. |
| **Trigger Distance** | Follow only while the target is within this distance; `0` disables this gate. |
| **Stopping Distance** | How close to get before stopping. |
| **Patrol Points** | Array of patrol waypoint Transforms. |
| **Patrol Zone** | A BoxCollider for random patrol (if set, waypoints are ignored). |
| **Aggro Distance** | Distance at which the agent switches from patrol to chase (Combined). |
| **Walk / Run Speed** | Walking and running speeds. |
| **Auto Update Path / Path Update Interval** | Enable one automatic follow-path refresh per interval of scaled gameplay time. Manual requests remain available when disabled. |
| **Animator** | Optional Animator for `Speed` and `IsMoving` parameters. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SetTarget(Transform newTarget)` | Set a new follow target. |
| `bool SetDestination(Vector3 destination)` | Return whether a sampled world destination was accepted by the native agent; resolution is reported later through events. |
| `bool AutoUpdatePath { get; set; }` | Control periodic follow refresh. A change from `false` to `true` refreshes an active, enabled target once immediately; assigning the existing value does nothing. |
| `void Stop()` / `void Resume()` | Stop / resume movement. |
| `void SetRunning(bool enable)` | Enable/disable running speed. |
| `void StartPatrol()` / `void StopPatrol()` | Start / stop patrol behavior. |
| `void SetMovementMode(MovementMode mode)` | Change the movement policy; switching to `FollowTarget` preserves the current/initial target. |
| `void SetPatrolPoints(Transform[] points)` | Replace waypoints; an active waypoint patrol restarts at the first valid point. Null/empty/all-null routes stop it. |
| `void SetPatrolZone(BoxCollider zone)` / `void ClearPatrolZone()` | Select zone patrol, or return to waypoints when the zone is cleared. |
| `bool WarpToPosition(Vector3 position)` | Sample the NavMesh and return the agent's actual warp result; returns `false` while disabled. |
| `bool IsMoving { get; }` | Whether the adapter has requested movement that has not stopped or arrived. Use `CurrentSpeed` to inspect actual velocity, and `IsPathBlocked` to inspect blockage. |
| `float RemainingDistance { get; }` | Distance remaining to the current destination. |
| `bool HasReachedDestination { get; }` | A complete path reached its destination. Explicit stop, resume, and new movement requests clear this state. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `onDestinationReached` | `Vector3` | Agent has reached its destination. |
| `onPathBlocked` | `Vector3` | Path to destination is blocked. |
| `onPathUpdated` | `Vector3` | A target path was submitted or a positional destination was accepted. |
| `onPatrolPointReached` | `int` | A patrol waypoint was reached (index). |
| `onStartFollowing` / `onStopFollowing` | *(none)* | Switched between patrol and chase (Combined mode). |

Positional destinations emit arrival/blocked events too; a Transform target is not required. A partial path endpoint is reported as blocked rather than as successful arrival. When a followed target moves far enough to update its path, reached/blocked state is reset so another arrival can be reported.

Arrival/blocked arguments identify the captured accepted destination. They do not read a later position of a moving Transform while dispatching events. After an accepted request resolves with no remaining native path, the component reports one blockage episode and an effective `PathInvalid` status unless the agent is already within stopping distance of that destination. An already reached accepted destination counts as arrival. Idle agents, explicit stops, pending calculation, and the submission frame do not generate a missing-path notification. Another explicit accepted request clears the previous reached/blocked state.

## Lifecycle and runtime changes

Call movement APIs after `Awake`, while the component and agent are enabled and the agent is on a NavMesh. A successful `SetTarget`/`SetDestination` resumes a previously stopped agent. `Stop()` pauses movement and patrol scheduling until `Resume()` or a new movement request; `StopPatrol()` also clears `IsPatrolling`. In `FollowTarget` mode, `Stop()` clears `Target`; use `SetTarget` again to restore continuous tracking. `Resume()` can still continue the retained NavMesh path.

`SetTarget` submits one initial native path and emits one initial `onPathUpdated`. Subsequent follow refreshes use only the Update interval scheduler; there is no parallel path coroutine. Starting with an already accepted request, including one made before the first `Start`, does not resubmit it. Stop/disable prevent automatic refresh; re-enabling an unpaused adapter resumes the existing cadence. Stationary targets still receive periodic refreshes so native paths can recover after navigation changes.

Each Update observes the previous accepted request's settled status before an optional automatic refresh. Arrival and blockage can therefore be reported even when the interval is due every frame. Pending calculations and requests submitted in the current frame remain excluded from status observation; stationary refreshes preserve an already reported arrival.

Request state and the next interval are committed before `onPathUpdated`. Listeners can synchronously call `Stop`, replace the target/destination, or disable/destroy the component; the older caller does not resume or overwrite that command. Arrival/blocked dispatch captures its request identity, so a replacement from either event suppresses the old request's trailing status notification. A command change or adapter disable/destruction during dispatch also ends that Update's remaining automatic refresh/animation work; a change from `onPathUpdated` ends its animation tail. This preserves accepted-request event ordering without making UnityEvent observers transactional.

A `true` result from `SetDestination` acknowledges submission; it does not guarantee successful arrival or continued movement after a listener cancels/replaces that request.

Waypoint waits and zone retries are canceled when stopped, disabled, replaced, or interrupted by a combined-mode chase. Delayed work from the previous route cannot advance a replacement route. Null/destroyed waypoints are skipped with a bounded search; an unusable route stops instead of recursing. Disabling this adapter cancels its scheduling but does not disable the separate `NavMeshAgent`; call `Stop()` first if the physical agent must also halt.

## Examples

### No-Code Example (Inspector)
Add `NavMeshAgent` + `AiNavigation` to an enemy. Drag the player into `Target`. Set `Movement Mode = FollowTarget`. Bake your NavMesh. On play, the enemy will chase the player.

### Code Example
```csharp
[SerializeField] private AiNavigation _guard;

public void AlertGuard(Transform intruder)
{
    _guard.SetTarget(intruder);
    _guard.SetRunning(true);
}
```

## See Also
- ← [Tools/Other](README.md)
