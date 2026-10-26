# Leaderboard

`Neo.Tools.Leaderboard` combines a local ranking API with optional scene views. It inherits
`Singleton<Leaderboard>`; assign explicit references when possible. Add it through
**Neoxider / Tools / Leaderboard**.

## Runtime API

| Method/data | Contract |
|-------------|----------|
| `GenerateUserList()` | Replace `users` with `count - 1` generated opponents and the canonical `player` once. Clamp `count` to at least one. Empty/null name sources use `nickname`. Does not refresh rows until sorting. |
| `Sort()` | Canonicalize one unambiguous player source entry, rebuild `sortUsers`, assign ranks, refresh rows, then invoke `OnSort`. Null entries are skipped. Equal-score order is stable. Repeated canonical references rank once without removing source slots. |
| `UpdatePlayerScore(int score, bool overrideBestScore = false)` | Accept the first nonzero result, then retain the better result according to `sortOrder`. Synchronize the player, stage its save data and sort after an accepted update. |
| `UpdatePlayerName(string newName)` | Update and synchronize the player, stage its name and score, then sort. |
| `GetId(LeaderboardUser user)` / `GetIdPlayer()` | Return a zero-based index in the latest sorted snapshot, or `-1` if unavailable. |
| `IsPlayerIdentityResolved` | The source contains the canonical reference or exactly one distinct persistent-key match. A query does not generate keys or mutate data. |
| `HasDuplicatePlayerKey` | Distinct source users share the player's nonempty key. Can be true even when a canonical reference already resolves the player safely. |
| `TryBindPlayerEntry(int sourceIndex)` | Intentionally replace a selected non-null source slot with `player`, retain its current name/score, repair missing/colliding player identity and sort. Invalid index, null slot or missing data returns `false` without writes/events. |
| `FormatText(int num)` | Pad a number to the digit width of `count` when `useZero` is enabled. |
| `FormatScore(int score)` | Format as time, grouped integers with spaces, or raw integers according to the formatting fields. |

`Descending` ranks higher values first; `Ascending` ranks lower values first. **Zero means no result**
in both modes and always comes last, including at `int.MinValue` / `int.MaxValue` boundaries.
Use `UpdatePlayerScore(0, true)` to explicitly clear a result. A normal zero submission cannot replace
a recorded score. Time scores use integer seconds.

```csharp
using System.Collections.Generic;
using Neo.Tools;

board.playerSaveKey = string.Empty;
board.users = new List<LeaderboardUser>
{
    new LeaderboardUser("Bot", 120),
    board.player
};
board.UpdatePlayerScore(90, true);
// Ranks are valid even when leaderboardItems is empty.
int playerRank = board.GetIdPlayer();
```

## Scene configuration and lifecycle

| Fields | Use |
|--------|-----|
| `count`, `names`, `rangeScore` | Total generated users including the player, opponent name choices and score range. Integer random range has an exclusive upper bound. |
| `useNum`, `formatNum`, `sep`, `useZero` | Optional opponent name suffix, padded suffix formatting, separator and padding policy. |
| `player`, `users`, `sortUsers` | Canonical player, source users and last sorted snapshot. Call `Sort()` after direct list/score edits. |
| `prefab`, `container`, `leaderboardItems` | Row template, optional parent and row slots. Null slots are skipped; surplus rows are hidden and reused when users return. |
| `leaderboardItemPlayer` | Optional separate summary using the player's actual rank, independent of visible row count. Hidden when the player is absent. |
| `generateLeaderboardOnAwake` | Legacy name: generate and bind rows during `Start` when a prefab is assigned. Generation always refreshes ranking and bindings. |
| `onAwakeSort` | Sort during `Start`, including when automatic row generation is disabled. |
| `generateLeaderboardItemsOnValidate` | Opt into edit-time row generation when configuration changes. Leave off if maintaining rows manually. |
| `formatScore`, `useTimeFormat`, `timeFormat`, `timeSeparator` | Score formatting; time formatting takes precedence. Integer grouping uses spaces independently of OS culture. |
| `noScoreText` | Player placeholder when score is zero; defaults to `--`. |
| `playerSaveKey` | Save namespace; an empty value disables player loading and saving. |

`Start` loads player data first and generates users when automatic generation is enabled and the source
list is empty; otherwise it synchronizes the canonical player. It then performs the configured row
generation/sort. Automatic row generation uses
the current user list, replaces missing row references, and preserves the separate player summary.
Without a prefab, ranking and manually assigned rows still work through `Sort()`.

In Edit Mode, validation resizes generated data to `count` and preserves the player when shrinking.
For a nonempty unresolved list or a duplicate player key, validation refreshes the derived ranking
without truncating/regenerating source rows; resolve the identity explicitly before resizing that data.
`OnSort` runs after ranking and row refresh; `LeaderboardItem` supplies the per-row Inspector events.

## Identity and persistence

`LeaderboardUser` retains `name`, `score`, `num` and the public readonly runtime `id`, and adds the
read-only `PersistentId` property backed by a serialized string. The constructor initializes both
IDs. Pure managed serialization callbacks can copy a known runtime ID into a missing backing key;
property reads and sorting never generate keys. `id` may be null or differ from `PersistentId` after
deserialization or explicit identity repair. Use `PersistentId` for durable identity.

Player binding uses this order:

1. A source entry that is the canonical `board.player` object wins, including when earlier entries
   share its key. Only the canonical object is highlighted. Its known aliases rank once; all source
   slots remain present.
2. Otherwise, exactly one distinct source object with the player's nonempty `PersistentId` is replaced
   by the canonical player, keeping the player's current name/score. `sortUsers` is derived data and
   is rebuilt rather than used as a source of identity.
3. Missing keys or multiple distinct matching rows do not identify a player. Names, scores, ranks and
   apparent uniqueness of a name never select a slot. `Sort()` preserves unresolved rows. Startup or
   player updates may append the canonical player without overwriting those rows.

`HasDuplicatePlayerKey` remains useful after that append: reference resolution is safe, but colliding
legacy entries are still preserved. For intentional repair:

```csharp
// This index must be selected intentionally by the caller, not inferred from a display name.
bool repaired = board.TryBindPlayerEntry(chosenSourceIndex);
```

A valid binding replaces only the chosen slot, preserves every other slot and the player's current
name/score, and invokes the normal sort/refresh. It creates a key when missing, or renews the canonical
key when another distinct unselected row would collide. Other users keep their keys and data; the old
readonly `id` value is retained. If another source slot already holds the canonical player, repair
creates a new canonical copy with the current name/score and legacy ID value before binding the chosen
slot. That other row stays unchanged rather than receiving the renewed key through an alias; retain
`board.player` again after this explicit repair. Ordinary synchronization never changes the canonical
reference. No save-provider operation is performed by this repair. To deliberately
discard generated opponents instead, use `GenerateUserList()` / `Sort()`.

### Authored tokens and raw legacy JSON

The backing field carries `FormerlySerializedAs("id")` to recover old authored Unity tokens, including
the IDs still present in the supplied leaderboard prefab. No YAML text rewrite or automatic asset
resave is performed. Real prefab save/reload and whole-aggregate JsonUtility acceptance are authored
and remain subject to the integrated Unity-version checks; rename metadata alone does not prove every
external JSON import path.

For explicit old raw JSON, `LeaderboardLegacyJson.TryDeserialize` reads exactly this aggregate shape:

```json
{"player":{"id":"player-key","name":"Alex","score":20,"num":0},"users":[{"id":"player-key","name":"Old","score":5,"num":0}]}
```

```csharp
using System.Collections.Generic;
using Neo.Tools;

if (LeaderboardLegacyJson.TryDeserialize(json, out LeaderboardUser importedPlayer,
        out List<LeaderboardUser> importedUsers))
{
    board.player = importedPlayer;
    board.users = importedUsers;
    board.Sort();
}
```

This managed DTO conversion reads writable old `id` tokens independently of rename metadata, ignores
`sortUsers`, and reconnects one keyed player entry. It retains keyless rows without name-based guesses.
Invalid/missing aggregate data or duplicate source matches for the player key returns `false` with
null outputs. The root must contain exactly one explicit lowercase `users` member whose value is an
array; `users: []` is valid. An absent member, `null`, string/object/scalar value, duplicate root member,
nested `users` or quoted text does not satisfy that requirement. One structural JSON reader validates
the complete object and reads the DTO's decoded names and values; standard JSON escapes in `player`,
`users`, `id`, `name`, `score` and `num` work without text replacement. Duplicate decoded root `users`
names are still rejected. Unknown/custom members are ignored, never renamed or treated as aliases.
Names and string values are decoded once: literal backslash-u text remains literal, case is exact,
and custom names such as `users key`, `namespace:users` or `_x0075_sers` do not alias `users`.
The player must be an object; its fields remain optional (missing ID/name are null, missing score/num
are zero). Supplied ID/name values are strings or null, and supplied score/num values are 32-bit integer
numbers. Null source rows and exact string/numeric payloads are retained; JsonUtility's default empty
list is not evidence of a supplied array.
The helper does not change a board, deliver events, read/write saves or implement a new
snapshot format. It is for the old `id` shape; use ordinary Unity serialization for the new backing
field. Install imported data before `Start` when the existing SaveProvider name/score overlay should
take precedence.

The active `Neo.Save.SaveProvider` stores only `<playerSaveKey>_Name` and `<playerSaveKey>_Score`.
Both score updates and renames stage these values. `Start` loads them onto the canonical player before
binding its serialized source copy; the source copy's stale name/score never replaces loaded values.
Provider flushing follows the save system's
lifecycle, or call `SaveProvider.Save()` at an application-defined checkpoint. Bots, ranks and runtime
IDs are not saved by SaveProvider. Persistent IDs live in authored player/user serialization;
no `_Id` / `_Snapshot` save key is introduced. Give independent boards different keys. The inherited singleton's
**Set Instance On Awake** setting must be disabled on additional explicitly referenced boards.

See [module entry](./README.md), [row binding](./LeaderboardItem.md), and [scrolling](./LeaderboardMove.md).
