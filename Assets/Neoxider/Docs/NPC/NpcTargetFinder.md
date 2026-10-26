# NpcTargetFinder

**Purpose:** Finds a scene target by tag or name (or an explicit override) and binds it to `NpcNavigation` as the follow target. Handy for enemies that should chase "the Player" without wiring the reference by hand.

## Setup

- Add via `Add Component -> Neoxider/NPC/NpcTargetFinder` (requires `NpcNavigation`).
- Leave `Find By Tag` on with `Target Tag = Player` for the common case.

## Behaviour

- On `Awake` (when `Find On Awake` is on) it searches once.
- It then keeps retrying every `Retry Interval` seconds until a target is bound, and automatically resumes searching if the bound target is destroyed at runtime. This covers targets that spawn after the NPC.
- When a target is found it calls `NpcNavigation.SetFollowTarget(target)` and, if `Set Mode To Follow On Find` is on, switches the navigation mode to `FollowTarget`.
- An undefined tag does not abort the search: the configured name fallback still runs. Missing-target diagnostics remain opt-in and warn once until a target is found.

## Key Fields (Inspector)

| Field | Default | Description |
|-------|---------|-------------|
| `_targetOverride` | none | Explicit target; when set, scene search is skipped and it is applied immediately. |
| `_findByTag` | `true` | Search with `GameObject.FindGameObjectWithTag`. |
| `_targetTag` | `Player` | Tag used when `Find By Tag` is on. |
| `_findByName` | `false` | Fallback search with `GameObject.Find`. |
| `_targetName` | `Player` | Name used when `Find By Name` is on. |
| `_setModeToFollowOnFind` | `true` | Switch `NpcNavigation` to `FollowTarget` when a target is found. |
| `_findOnAwake` | `true` | Run the first search in `Awake`. |
| `_retryInterval` | `1` | Seconds between search attempts. |
| `_debugLogMissingTarget` | `false` | Log one warning when no target is found. |

## Public API

- `TargetOverride` (get/set) — assign an explicit target at runtime; it replaces an already bound target on the next frame. Clear it to resume tag/name lookup; the old navigation target is replaced when lookup succeeds.
- `FindAndSetTarget()` — force an immediate (throttled) search.

```csharp
NpcTargetFinder finder = GetComponent<NpcTargetFinder>();
finder.TargetOverride = player;
finder.FindAndSetTarget(); // Explicit overrides bypass the search retry timer.
```

The finder can bind during its own `Awake` regardless of whether navigation's `Awake` has run yet.
The same methods can be called from C# or connected through scene event workflows.

## See Also

- [Module Root](./README.md)
- [NPCNavigation](./Navigation/NPCNavigation.md)
