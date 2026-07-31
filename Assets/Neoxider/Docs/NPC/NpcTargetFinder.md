# NpcTargetFinder

**Purpose:** Finds a scene target by tag or name (or an explicit override) and binds it to `NpcNavigation` as the follow target. Handy for enemies that should chase "the Player" without wiring the reference by hand.

## Setup

- Add via `Add Component -> Neoxider/NPC/NpcTargetFinder` (requires `NpcNavigation`).
- Leave `Find By Tag` on with `Target Tag = Player` for the common case.

## Behaviour

- On `Awake` (when `Find On Awake` is on) it searches once.
- It then keeps retrying every `Retry Interval` seconds until a target is bound, and automatically resumes searching if the bound target is destroyed at runtime. This covers targets that spawn after the NPC.
- When a target is found it calls `NpcNavigation.SetFollowTarget(target)` and, if `Set Mode To Follow On Find` is on, switches the navigation mode to `FollowTarget`.

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

- `TargetOverride` (get/set) — assign an explicit target at runtime; it is applied on the next frame.
- `FindAndSetTarget()` — force an immediate (throttled) search.

## See Also

- [Module Root](./README.md)
- [NPCNavigation](./Navigation/NPCNavigation.md)
