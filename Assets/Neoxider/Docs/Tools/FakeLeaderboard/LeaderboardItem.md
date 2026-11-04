# LeaderboardItem

**What it is:** `Neo.Tools.LeaderboardItem` renders one user. Add it through
**Neoxider / Tools / FakeLeaderboard / LeaderboardItem** or use the supplied row prefab. Script: `Scripts/Tools/FakeLeaderboard/LeaderboardItem.cs`.

**How to use:**
1. Put the component on a row prefab and assign the optional `textId`, `textName` and `textScore` TMP labels.
2. Assign the prefab (or row instances) to a [Leaderboard](Leaderboard.md); `Sort()` binds each visible row.
3. Wire `OnUser` / `OnUserTrue` / `OnuserFalse` to highlight the player's row.

---

## Binding from C#

```csharp
LeaderboardUser user = new LeaderboardUser("Alex", 1250, 2);
row.Set(user, false, board);
```

`Set(LeaderboardUser user, bool isPlayer, Leaderboard leaderboard = null)` records `user`, zero-based
`id = user.num`, and `isPlayer`, updates the assigned labels, then emits highlight events. A null user
is invalid and raises `ArgumentNullException`. The `leaderboard` argument is optional.

| Field | Display behavior |
|-------|------------------|
| `textId` | One-based rank (`user.num + 1`). |
| `textName` | User display name. |
| `textScore` | `leaderboard.FormatScore(score)`, or raw integer text without a board. |

Each TMP label is optional: score-only, name-only and event-only rows are supported. For a player with
zero score, `textScore` shows the board's `noScoreText`, or `--` without a board, bypassing formatting.

## Inspector events

`OnUser(bool)` fires first on every bind. Then `OnUserTrue` fires for the player or `OnuserFalse` for
an opponent. The existing casing of `OnuserFalse` is retained for serialized listeners. Wire these
events to color/background/highlight components, or subscribe with `AddListener` from C#.

`Leaderboard.Sort()` controls row visibility and calls `Set` for occupied slots. The optional separate
player summary receives its rank even when the matching list row is not instantiated.

See [module entry](./README.md) and [Leaderboard](./Leaderboard.md).
