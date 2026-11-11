# NPC Module - Core Types

**What it is:** the plain C# cores behind the NPC components (`Scripts/NPC/Navigation/*.cs`,
`Scripts/NPC/Combat/NpcCombatDecisionCore.cs`), usable without the MonoBehaviour wrappers.

**How to use:** construct a core with the caller-owned transform/agent/Animator, configure it, and call `Tick` from
your own update loop (see the example below).

---

| Type | Description |
|------|-------------|
| `NpcNavigation.Behaviours` | Partial class for NPC navigation behaviours. |
| `NpcCombatDecisionCore` | NPC combat decision-making core. |
| `NpcAggroFollowCore` | Aggro + follow core module. |
| `NpcAnimationCore` | NPC animation management core. |
| `NpcDestinationResolver` | NavMesh destination resolver. |
| `NpcFollowTargetCore` | Target follow core. |
| `NpcNavAgentCore` | NavMeshAgent core (speed, stopping, radius). |
| `NpcPatrolCore` | Patrol core (waypoints, zones, cycles). |

## Runtime contracts

`NpcCombatDecisionCore.Decide` is stateless. It acquires when no target exists, clears beyond a positive
lose distance, holds when actions are blocked, and otherwise chooses chase/hold/attack by range and
attack readiness. A zero lose distance disables distance-based clearing in this core.

The navigation cores are ordinary C# adapters with Unity dependencies, not standalone simulation
models. Call them on the Unity main thread with caller-owned transforms, agents, and Animators.
Their events expose typed C# outputs; `NpcNavigation` forwards them to scene UnityEvents.

```csharp
NpcFollowTargetCore follow = new NpcFollowTargetCore(npcTransform, agent);
follow.Configure(0f, true, 0.25f, 2f);
follow.SetTarget(playerTransform);
follow.Tick(time, null);
follow.Stop();   // Remains stopped across ticks.
follow.Resume();
```

Use `Neo.NPC.Navigation` for navigation cores. `SetDestination` on `NpcFollowTargetCore` stores a
request; `TryForceUpdatePath` submits it immediately, or automatic updates submit it on a tick.
`PathUpdated` reports agent acceptance, `PathBlocked` reports a partial/invalid path once per path request
(re-armed by a periodic re-path, `TryForceUpdatePath`, `SetTarget`, `SetDestination` or a complete status), and
`DestinationReached` requires a complete path. A missing agent/NavMesh prevents submission.

`NpcFollowTargetCore.PathUpdatePhase` (0..1, default 0) advances only the first periodic re-path after a new target
or destination by that fraction of the interval; later periods keep the full interval. Give each agent of a crowd a
different phase to keep their periodic `SetDestination` calls out of the same frame. Pass a cached
`Func<Vector3, Vector3>` (not a method group) as the constrain callback when ticking every frame, so the call does
not allocate a delegate.

```csharp
follow.PathUpdatePhase = agentIndex * 0.618f % 1f;
```

`NpcAnimationCore` validates its two parameters once per bound controller; missing/mistyped parameters are skipped
(`HasSpeedParameter`, `HasIsMovingParameter`).

`NpcPatrolCore.StartPatrol` reports a start only after accepting a destination. Empty/unusable routes
stop rather than retaining a previous agent path. Zone sampling is bounded to five attempts per
selection. Partial paths do not count as waypoint arrivals; route recovery remains the caller's job.
`NpcAggroFollowCore` provides separate distance hysteresis events for callers that supply target
positions; `NpcNavigation` implements its Combined state in its behaviour adapter.

`NpcDestinationResolver` samples positions without proving connectivity or matching a particular
agent type. Movement bounds and sample radii do not guarantee path containment; see the navigation
guide for these limitations.

## See Also
- [NpcNavigation](Navigation/NPCNavigation.md)
- <- [NPC](README.md)
