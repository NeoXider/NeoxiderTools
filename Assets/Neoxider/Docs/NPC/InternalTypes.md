# NPC Module - Core Types

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
`PathUpdated` reports agent acceptance, `PathBlocked` reports partial/invalid paths, and
`DestinationReached` requires a complete path. A missing agent/NavMesh prevents submission.

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
