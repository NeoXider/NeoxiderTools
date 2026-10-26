# LeaderboardMove

`Neo.Tools.LeaderboardMove` scrolls its transform vertically toward the player's row and optionally
pulses that row with DOTween. Add it through **Neoxider / Tools / FakeLeaderboard / LeaderboardMove**.

## Setup

Assign `leaderboard` explicitly, or leave it empty to resolve `Leaderboard.I`. The player's rank must
have a corresponding non-null entry in `leaderboard.leaderboardItems`. A dedicated player summary is
not used as the scrolling target. Place the component on the intended scrolling transform and configure
`offsetY`; the legacy movement calculation aligns the row's world Y toward that offset when the row
moves with this transform. UI layout and canvas coordinates must suit that arrangement.

| Field | Behavior |
|-------|----------|
| `useMove`, `delayTime` | Queue `Move()` after enabling. Delay is in scaled game time. |
| `useSortEnable` | Refresh the board when this component is enabled. |
| `Enable` | UnityEvent invoked on enable, before automatic sorting/movement. |
| `timeMove`, `offsetY` | Scroll duration and target Y offset. |
| `useAnimPlayer`, `scaleDelta`, `durationAnimPlayer` | After scrolling, add `scaleDelta` on each local scale axis, then yoyo back. |
| `useAnimStartScale` | Retained legacy serialized field; currently unused. |
| `_debugLogging`, `_logWarnings` | Opt-in diagnostics; both default to disabled. |

## C# and lifecycle

```csharp
board.Sort();
mover.Move();             // Explicit movement while the component is active and enabled.
mover.KillActiveTweens(); // Cancel pending movement and owned tweens; restore the row scale.
```

`Move()` first cancels the previous request/animations and restores the original player-row scale.
Repeated calls therefore cannot accumulate a partially completed scale pulse. It ignores inactive or
disabled components and unavailable rows. A missing/destroyed player row cannot start a pulse after
the scroll completes.

Disabling or destroying the component cancels the delayed invocation, kills only its owned tweens,
and restores the player row's base scale. Calling `KillActiveTweens()` also cancels a queued move.
Scrolling stops at its current position; cancellation does not rewind the scrolling transform.

See [module entry](./README.md), [Leaderboard](./Leaderboard.md), and [LeaderboardItem](./LeaderboardItem.md).
