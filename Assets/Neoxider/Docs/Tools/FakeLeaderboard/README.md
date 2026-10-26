# Tools / FakeLeaderboard

Configurable local leaderboard for prototypes, demos and simulated opponents. Scripts live in
`Scripts/Tools/FakeLeaderboard/`, assembly `Neo.Tools.FakeLeaderboard`, namespace `Neo.Tools`.

| API | Responsibility |
|-----|----------------|
| [Leaderboard](./Leaderboard.md) | Generate users, rank results, update and save the player, bind optional rows. |
| [LeaderboardItem](./LeaderboardItem.md) | Render one user through optional TMP labels and player highlight events. |
| [LeaderboardMove](./LeaderboardMove.md) | Scroll to the player's visible row and animate its scale with DOTween. |

## C# usage

Use an explicitly assigned `Leaderboard` reference; `Leaderboard.I` is also available through the
existing singleton wrapper. A custom assembly needs a reference to `Neo.Tools.FakeLeaderboard`.

```csharp
using Neo.Tools;

// board is an assigned Leaderboard component.
board.playerSaveKey = "DailyLeaderboard"; // Use an empty key for a transient board.
board.sortOrder = SortOrder.Ascending;    // For example, lower completion times win.
board.UpdatePlayerScore(72);             // The first nonzero result is accepted.
board.UpdatePlayerName("Alex");          // Name changes are saved too.
int rank = board.GetIdPlayer();          // Zero-based; -1 before sorting or if absent.
```

Zero represents no result and always ranks after nonzero scores, including negative scores. Rankings
and `LeaderboardUser.num` work without UI rows. `GenerateUserList()` replaces the user list;
call `Sort()` afterward when generating explicitly at runtime.

## Scene and Inspector workflow

1. Add `Leaderboard` from **Neoxider / Tools / Leaderboard**, or use the existing leaderboard prefab.
2. Configure `count` (including the player), opponent `names`, `rangeScore`, and `sortOrder`.
3. Assign a row `prefab` and `container` for automatic row creation, or supply `leaderboardItems`.
   `leaderboardItemPlayer` is an optional separate player summary.
4. Assign the TMP labels that each row needs. Connect `OnUser` / `OnUserTrue` / `OnuserFalse` for
   highlighting and `OnSort` for post-refresh reactions.
5. Optionally add `LeaderboardMove`, assign its `leaderboard`, and configure the delay and offset.

These UnityEvents provide the Inspector workflow; game logic can call the same typed C# methods.
Automatic loading and row creation run in `Start`, despite the legacy `generateLeaderboardOnAwake`
field name. `generateLeaderboardItemsOnValidate` additionally enables edit-time row generation.

Names are display values, not identity. `LeaderboardUser.PersistentId` is the serialized key; the
public readonly `id` remains a legacy runtime value. Sorting binds the canonical player reference,
or one unambiguous persistent-key match, and rebuilds the derived snapshot. Keyless or colliding
rows are preserved. `IsPlayerIdentityResolved` and `HasDuplicatePlayerKey` expose the source state.

For intentional repair, `board.TryBindPlayerEntry(chosenSourceIndex)` replaces only that selected
slot and preserves the current player's name/score. It can renew the player's key when needed.
`LeaderboardLegacyJson.TryDeserialize` explicitly reads the old raw JSON `player`/`users` shape with
writable `id` tokens. See [identity and persistence](./Leaderboard.md#identity-and-persistence) for
exact migration limits and examples. No new save keys or automatic prefab rewrite are introduced.
