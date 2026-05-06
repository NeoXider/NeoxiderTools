# AiNavigation (Legacy)

**Purpose:** A NavMeshAgent-based AI navigation component. Supports three modes: target following (`FollowTarget`), waypoint or zone patrol (`Patrol`), and a combined mode (`Combined` — patrols until a target enters aggro range, then chases).

> ⚠️ **Deprecated.** Use `Neo.NPC.NpcNavigation + modules` instead.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Movement Mode** | `FollowTarget`, `Patrol`, or `Combined`. |
| **Target** | The Transform to follow. |
| **Trigger Distance** | Minimum distance before movement begins. |
| **Stopping Distance** | How close to get before stopping. |
| **Patrol Points** | Array of patrol waypoint Transforms. |
| **Patrol Zone** | A BoxCollider for random patrol (if set, waypoints are ignored). |
| **Aggro Distance** | Distance at which the agent switches from patrol to chase (Combined). |
| **Walk / Run Speed** | Walking and running speeds. |
| **Animator** | Optional Animator for `Speed` and `IsMoving` parameters. |

## API

| Method / Property | Description |
|-------------------|-------------|
| `void SetTarget(Transform newTarget)` | Set a new follow target. |
| `bool SetDestination(Vector3 destination)` | Navigate to a world position. |
| `void Stop()` / `void Resume()` | Stop / resume movement. |
| `void SetRunning(bool enable)` | Enable/disable running speed. |
| `void StartPatrol()` / `void StopPatrol()` | Start / stop patrol behavior. |
| `bool IsMoving { get; }` | Whether the agent is currently moving. |
| `float RemainingDistance { get; }` | Distance remaining to the current destination. |

## Unity Events

| Event | Arguments | Description |
|-------|-----------|-------------|
| `onDestinationReached` | `Vector3` | Agent has reached its destination. |
| `onPathBlocked` | `Vector3` | Path to destination is blocked. |
| `onPatrolPointReached` | `int` | A patrol waypoint was reached (index). |
| `onStartFollowing` / `onStopFollowing` | *(none)* | Switched between patrol and chase (Combined mode). |

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
