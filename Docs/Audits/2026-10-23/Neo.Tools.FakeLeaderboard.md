# Neo.Tools.FakeLeaderboard: audit

Status: source audit and scoped fixes complete; compilation and engine verification pending coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Neo.Tools.FakeLeaderboard.asmdef`.
- Scope: all 3 manifest C# files reviewed completely; no nested assembly exists inside this assembly root.
- Constraints observed: no child agents, commits, shared configuration, package/asmdef edits, Unity/MCP,
  build/test runners, installs, scene/prefab edits or shared-service changes.

Кратко: исправлены реальные ошибки идентификации игрока, сортировки, сохранения имени, обновления
строк и отмены анимации. Проверки выполнены статически; новые Unity-тесты ещё не запускались.

## Scope

- `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs`
- `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardItem.cs`
- `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardMove.cs`

| File | Review coverage |
|------|-----------------|
| `Leaderboard.cs` | `LeaderboardUser` construction/serialization; singleton dependency; every public field and method; `Start` and `OnValidate`; generation/count/name boundaries; identity and zero-score policy; stable sorting, integer extremes and rank assignment; snapshot freshness; optional/missing/excess rows; save keys/load order/rename writes; formatting and culture. |
| `LeaderboardItem.cs` | Complete binding path, null user contract, optional TMP references, one-based display vs zero-based stored rank, placeholder/formatting precedence, event order and original serialized event names. No per-row resources or event subscriptions are owned. |
| `LeaderboardMove.cs` | Explicit dependency vs singleton fallback; `Start`/enable/disable/destroy; delayed invocation ownership; tween completion, cancellation and recycling references; interrupted scaling, missing/destroyed targets, active-state checks, world-Y coordinate behavior, diagnostics gates, retained legacy field. |

Context read without modification: existing `FakeLeaderboardBehaviorTests`, diagnostics coverage,
`Singleton<T>`, `SaveProvider`/`ISaveProvider`, relevant random/formatting extensions, both test assembly
definitions, relevant prefab field occurrences and the earlier FakeLeaderboard audit. Older evidence
was treated as leads, not verification of the new changes.

Reuse receipt: read `neoxider-tools` and its matching `tools.md` / `extensions.md` references before
adding private identity logic. Reused `RandomRange`, `GetRandomElement`, `FormatTime` and the existing
`FormatWithSeparator(" ")`; the latter already handles invariant grouping and integer extremes.
The identity predicate is local glue for this component's canonical player, not a parallel service.
`unity-game-agent` file-only workflow applies; local review is not independent QA. Delegation and
Unity execution were explicitly prohibited by the user.

## Findings and fixes

All findings below are demonstrated by the inspected original control/data flow. Regression cases
are authored and mapped below, not reported as executed reproductions.

### F01 — P2: no-result sentinel contradicts best-score comparison

- Trigger: an ascending board's initial score is `0`; submit `12`. Original `12 < 0` rejects the first
  real result. Descending boards similarly reject an initial negative result. Ascending submission of
  `0` can erase a positive best result even though zero is always ranked as unscored.
- Observable consequence: accepted gameplay results never appear, or a recorded result disappears.
- Fix: zero is handled before direction comparison. Accept the first nonzero result, reject ordinary
  zero submissions, preserve explicit `overrideBestScore` resets.
- Source: [Leaderboard.cs:139](../../../Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs#L139).
- Regression: `UpdatePlayerScore_AcceptsFirstNonzeroResult` (3 cases) and
  `UpdatePlayerScore_ZeroRequiresExplicitOverride` (2 cases).

### F02 — P2: integer sentinel collisions can rank zero ahead of a real result

- Trigger: input order `[zero, int.MinValue]` descending, or `[zero, int.MaxValue]` ascending. Original
  substituted keys are equal, so stable ordering leaves zero before the real result.
- Observable consequence: the advertised zero-last contract fails at valid integer boundaries.
- Fix: order by the separate unscored flag first, then the actual score; skip null source entries.
- Source: [Leaderboard.cs:263](../../../Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs#L263).
- Regression: `Sort_ExtremeResultsBeatZeroAndAllRanksExistWithoutViews` (both directions).

### F03 — P2: display names and missing IDs identify the wrong player

- Trigger: a bot shares the player's name. With the player absent from `users`, the name fallback
  overwrites the bot; with both present, `GetIdPlayer` and row highlighting can select the bot first.
  Unity-deserialized readonly IDs can also both be null, making the old ID comparison match any user.
- Observable consequence: opponent score corruption, multiple highlighted players, scrolling/rank
  selection for the wrong user.
- Fix: use one shared identity predicate: the canonical player reference, or a nonempty equal ID.
  Never identify by name or equal missing IDs. Synchronization installs the canonical player object.
  Queries return `-1` when no sorted snapshot/player exists.
- Source: [Leaderboard.cs:188](../../../Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs#L188),
  `SyncPlayer`, `GetIdPlayer` and row binding.
- Regressions: `UpdatePlayerScore_SameNamedBotIsNotOverwrittenOrSelectedAsPlayer`,
  `UpdatePlayerScore_DeserializedMissingIdsDoNotIdentifyAnotherUser`, and the dedicated-player test.
- Compatibility limit: detached legacy serialized player copies cannot be identified safely; see the
  remaining serialization risk and substantial proposal below. No speculative name-based migration.

### F04 — P2: rank assignment is incorrectly limited to instantiated rows

- Trigger: fewer UI rows than sorted users, or no rows at all. Original `num = i` runs only in the UI loop.
- Observable consequence: headless users retain stale/default ranks; a separate player summary can
  display rank 1 even when the player is last.
- Fix: assign every sorted user's zero-based rank before touching views.
- Source: [Leaderboard.cs:276](../../../Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs#L276).
- Regressions: extreme-result/headless ranking and
  `Sort_DedicatedPlayerRankAndHighlightDoNotDependOnVisibleRowCount`.

### F05 — P2: stale/empty startup snapshots and surplus rows leave incorrect UI

- Triggers: create a board at runtime with an empty `users` list; generate rows from a nonempty stale
  `sortUsers`; disable `onAwakeSort` while automatic generation is enabled; shrink a user list; place
  the separate player summary inside `container`.
- Observable consequences: missing bots/rows, newly created rows left unbound, old users still visible,
  or the separate player summary disabled as an untracked child. Loaded player data could also remain
  detached from the source list until another explicit score/name update.
- Fix: initialize empty runtime data when automatic generation is enabled and synchronize the loaded
  canonical player in `Start`. Size row
  creation from current non-null users, prune missing row references, preserve the summary, and sort
  after row creation. Refresh every row slot, hiding/reusing surplus rows and skipping null slots.
  Hide an absent player's separate summary.
- Source: [Leaderboard.cs:84](../../../Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs#L84),
  `GenerateLeaderboardItems` at 211 and `SetItems` at 287.
- Regressions: both `Start_*` lifecycle cases and
  `Sort_ShrinkingAndGrowingListUpdatesRowVisibilityAndSkipsMissingRows`.

### F06 — P2: renaming never writes saved player data

- Trigger: call `UpdatePlayerName`, then reload before another accepted score update.
- Observable consequence: the old name returns; only score updates originally staged name/score keys.
- Fix: stage both existing keys during name updates as well. SaveProvider remains responsible for
  provider flushing; no save format/key changes.
- Source: [Leaderboard.cs:179](../../../Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs#L179).
- Regression: `UpdatePlayerName_StagesNameWithoutRequiringAnotherScoreUpdate`, using an isolated
  in-memory provider and restoring the original provider.

### F07 — P2: partial row views abort binding and events

- Trigger: omit `textName`, `textScore` or `textId` in a score-only/custom row. Original unconditional
  dereferences abort `Set` before its events, potentially aborting the whole board refresh.
- Observable consequence: remaining assigned labels and highlight events are not updated.
- Fix: write only assigned labels while always binding user/rank/player state and emitting events.
  An actually null user has an explicit `ArgumentNullException` contract before mutation.
- Source: [LeaderboardItem.cs:26](../../../Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardItem.cs#L26).
- Regressions: `ItemSet_ScoreOnlyViewStillBindsStateAndEmitsPlayerEvents`; other new tests also exercise
  label-free rows and event-only highlighting.

### F08 — P2: disabling leaves a queued move alive

- Trigger: `OnEnable` queues `Invoke(nameof(Move), delayTime)` and the component is disabled before the
  delay expires. Original cleanup kills only already-created tweens.
- Observable consequence: delayed movement can start after the owner is disabled.
- Fix: cancel the named invocation in the shared cleanup used by disable/destroy/manual cancellation
  and move restarts. `Move` refuses inactive owners. If an `Enable` listener disables the owner,
  `OnEnable` stops before scheduling new work.
- Source: [LeaderboardMove.cs:60](../../../Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardMove.cs#L60),
  `OnDisable`, `OnDestroy`, `KillActiveTweens` at 166.
- Regression: `Disable_CancelsScheduledMoveBeforeItsDelayElapses`, entering real Play Mode in the
  centrally executed EditMode suite and checking both pending-invocation state and position/tweens.

### F09 — P2: interrupted player pulses accumulate scale

- Trigger: interrupt a pulse and call `Move` again for the same row. Original cleanup does not restore
  scale, and the next call captures the partially enlarged scale as its new baseline.
- Observable consequence: repeated updates grow the player card; disabling leaves it enlarged.
- Fix: cleanup restores the recorded original local scale before a new baseline is captured. Clear
  tween references on kill, guard missing rows, and skip completion pulses for destroyed targets.
- Source: [LeaderboardMove.cs:99](../../../Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardMove.cs#L99),
  `KillActiveTweens` at 166.
- Regression: `Move_RestartAndDisableRestoreOriginalScaleWithoutAccumulation`, exercising real tweens,
  non-unit scale, interrupted seek/restart and real `OnDisable` dispatch.

### F10 — P3: generation/authoring boundaries fail or lose the player

- Triggers: empty/null names, nonpositive `count`, or shrink `count` while the player is at the end.
- Observable consequences: random selection throws, configured count disagrees with output, or the
  player is truncated and the snapshot remains stale.
- Fix: generation/validation clamp count to one, initialize missing data, use `nickname` when no name
  is available, and retain the player while resizing in `OnValidate` with a fresh sort.
- Source: [Leaderboard.cs:108](../../../Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs#L108)
  and `GenerateUserList` at 245.
- Regression: `GenerateUserList_NormalizesCountAndSupportsEmptyNameSources` (4 cases). Editor resize
  behavior is statically reviewed; Inspector dispatch and authored prefab appearance need central QA.

### F11 — P3: grouped scores depend on OS locale

- Trigger: enable `formatScore` under `de-DE` or `fr-FR`. Original `N0` plus comma replacement keeps
  periods or locale-specific spaces instead of the component's intended ordinary space separator.
- Observable consequence: the same configured board renders different grouped score strings.
- Fix: use the existing invariant `FormatWithSeparator(" ")` extension, including `int.MinValue`.
- Source: [Leaderboard.cs:367](../../../Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs#L367).
- Regression: `FormatScore_GroupingUsesSpacesAcrossCultures` (2 cultures, positive and minimum int).

## Regression inventory

Only NEW uniquely prefixed tests were added under `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/`:

- [Audit20261023NeoToolsFakeLeaderboardBehaviorTests.cs](../../../Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardBehaviorTests.cs):
  19 authored cases covering public state, actual ordering/ranks, row text/events/visibility, serialization
  identity, generation, cultures and staged save values.
- [Audit20261023NeoToolsFakeLeaderboardLifecycleTests.cs](../../../Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardLifecycleTests.cs):
  4 authored engine cases using `EnterPlayMode`/`ExitPlayMode`, natural `Start`/`OnDisable`, actual DOTween
  targets/completion/seeking, and cleanup. No private callback reflection or source-text regression tests.
- Both new test files have unique `.meta` GUIDs. Existing tests were not edited. Required
  `Neo.Tools.FakeLeaderboard`, `Neo.Save`, `Unity.TextMeshPro`, TestRunner and `DOTween.dll` references
  already exist in `Neo.Editor.Tests`; no shared assembly-definition change is required.

## Documentation

Updated all four owned existing English pages:

- [README](../../../Assets/Neoxider/Docs/Tools/FakeLeaderboard/README.md): canonical entry, component
  responsibilities, typed C# example, Inspector workflow, score and identity contracts.
- [Leaderboard](../../../Assets/Neoxider/Docs/Tools/FakeLeaderboard/Leaderboard.md): public API, fields,
  initialization/order, null/zero boundaries, headless ranks, save keys/provider flushing, legacy IDs.
- [LeaderboardItem](../../../Assets/Neoxider/Docs/Tools/FakeLeaderboard/LeaderboardItem.md): optional
  labels, null-user precondition, displayed rank, placeholder and serialized event ordering.
- [LeaderboardMove](../../../Assets/Neoxider/Docs/Tools/FakeLeaderboard/LeaderboardMove.md): explicit
  references, world-coordinate assumptions, pending-call/tween ownership, restoration and unused legacy
  `useAnimStartScale` field.

No package/root entry point, another module's docs, DocsEn, existing metadata, samples or asset layouts
were changed. Existing document BOM policy was preserved.

## Substantial improvement proposal

### Persistent entry identity and a headless ranking model — proposed, NOT implemented

**Problem.** Ranking, random bot generation, persistence, singleton lookup and row creation currently
share one scene component. The small rank correction makes headless usage possible but callers still
need a GameObject. More seriously, readonly `LeaderboardUser.id` is omitted by Unity serialization;
`player`, `users` and `sortUsers` are inline serialized data and may deserialize as separate copies.
Identity cannot safely be reconstructed from a name, especially after renaming or with duplicate
names. This audit stops corrupting unrelated entries but cannot infer which detached legacy copy was
intended to be the player. It may remain visible as another opponent after canonical synchronization.

**Proposed architecture / API.** Add a plain C# `LeaderboardModel`, independent of MonoBehaviour,
TMP, DOTween, SaveProvider and scene searches. Use stable serialized entry IDs in a versioned
`LeaderboardSnapshot`, with a separate `PlayerId`, and an explicit `HasScore` flag. A narrow API:

```csharp
LeaderboardModel(string playerId, SortOrder sortOrder);
void ReplaceEntries(IEnumerable<LeaderboardEntry> entries);
bool TrySetPlayerScore(int score, bool overrideBestScore = false);
bool TryGetRank(string id, out int zeroBasedRank);
IReadOnlyList<LeaderboardEntry> GetRankedEntries();
LeaderboardSnapshot CaptureSnapshot();
void RestoreSnapshot(LeaderboardSnapshot snapshot);
```

`LeaderboardEntry` data would have a serialized private ID exposed through a read-only property,
display name, score and `HasScore`. Reject duplicate IDs or invalid snapshots before mutation. Keep
stable tie order. `Leaderboard` stays the scene/Inspector facade and owns row binding, event dispatch
and SaveProvider adaptation. Bot generation fills the model using the existing random primitives;
the model itself does not perform random generation or save I/O. `LeaderboardMove` continues to
consume visible row bindings and never owns ranking data.

**Compatibility / migration.** Keep all existing serialized field names, UnityEvents, methods and
the legacy readonly `id` member. Adapt the old `LeaderboardUser` facade to model entries instead of
changing that field's type/mutability. Existing methods retain zero-as-unscored semantics; the new
model may represent a legitimate zero result using `HasScore`. Continue reading the two old player
save keys when no versioned snapshot exists. Assign/persist new stable IDs on the first explicit
snapshot migration. For old inline lists with no recoverable IDs, provide an explicit authoring choice
to regenerate bots or map a chosen player entry. Do not silently map by display name. Scene/prefab
migrations and any dependency split belong to a separately reviewed coordinator task.

**Acceptance scenarios.**

1. Duplicate display names remain independent through rename, save/load and Unity serialization
   roundtrips; exactly one stable player ID is selected.
2. Headless ranking and UI binding produce identical ranks with zero, one or all rows instantiated.
3. Ascending/descending first-result and integer-extreme cases retain this audit's facade behavior;
   direct model callers can represent a scored zero explicitly.
4. Ties remain deterministic; invalid/duplicate IDs and invalid snapshot versions reject atomically.
5. Restoring a snapshot after scene reload keeps identity and rank; old player-only saves still load.
6. Legacy data with ambiguous player copies requires the explicit migration choice, preserving
   legitimate same-name opponents and all existing listener bindings.

**Effort / risk.** Approximately 2–3 engineering days plus one day for migration/engine verification,
assuming the facade remains source-compatible and no virtualization/network feature is added.
Primary risks are legacy inline serialization, public mutable lists being edited behind the adapter,
callback timing and accidental behavior changes around zero. Stage the work behind the existing facade,
test the pure model first, and validate saved prefabs/scenes on both supported Unity version families
before enabling migration by default. No new package dependency is needed.

## Verification and remaining risks

Checks actually run in this worker:

| Check | Result |
|-------|--------|
| Manifest inventory vs recursive C# inventory | Pass: exactly the assigned 3 files; all read completely, no nested assembly. |
| Public runtime declaration comparison against `ca198556` | Pass: all original public declarations unchanged in the 3 files. Existing serialized field names and save keys preserved. |
| UTF-8 decoding, replacement-character/line-ending/whitespace checks | Pass for changed source, tests, metadata and four owned docs. No console-based encoding repair. |
| Explicit-type and balanced C# delimiter scan | Pass for 3 runtime and 2 new test files. A lexical check, not a C# compilation. |
| Local documentation targets and heading anchors | Pass for all four owned pages. |
| New metadata GUID collision search under `Assets` | Pass: each of the 2 GUIDs occurs exactly once. |
| Authored test inventory | 23 cases: 19 ordinary parameterized cases and 4 engine lifecycle cases. Not executed. |
| Raw obsolete Unity ID API search | None in this assembly; no version-specific branch introduced. |
| `git diff --check -- <owned source/docs/new tests/report>` | Pass; new untracked text files additionally checked directly for trailing whitespace. |

No Unity import/compile, console check, Play Mode, screenshot, test runner, build, performance
measurement or independent-agent QA was run. These were explicitly excluded by the worker contract.
The new tests must not be interpreted as passing tests until centrally executed.

Remaining risks / coordinator actions:

- Compile on Unity 6.0–6.4 and 6.5+ and execute both new fixtures (including their Play Mode transitions)
  plus existing `FakeLeaderboardBehaviorTests` / relevant diagnostics coverage. Test assembly references
  are already sufficient by inspection; no reference edits requested.
- Review existing leaderboard prefabs/scenes for detached serialized player copies. The safe identity
  fix deliberately removes name fallback; rebuild generated user data or explicitly bind the canonical
  player where needed. No prefab/scene was edited by this worker. Persistent-ID migration is proposed
  above, not claimed solved.
- Verify the opt-in `OnValidate` generation/shrink workflow in the actual Inspector and inspect the
  resulting rows. This authoring path and any visual layout validation were only reviewed statically.
- Confirm canvas/world-Y assumptions for `offsetY`; no coordinate migration was attempted. Tween
  cancellation restores only the owned row scale and leaves scrolling at its current position.
- `useAnimStartScale` remains a serialized no-op; changing it needs an explicitly defined behavior.
  High-count row creation still scales with user count and was not profiled.
- No cross-boundary code/config edits are needed for the implemented fixes. Any stable-ID data
  migration, prefab update or future runtime/model assembly split requires separate coordination.
