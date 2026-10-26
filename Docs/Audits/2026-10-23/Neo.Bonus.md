# Neo.Bonus: audit

Status: source audit, fixes, regression authoring, documentation, and scoped static validation complete.
Unity compilation, Edit/Play Mode execution, and independent QA remain centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Bonus/Neo.Bonus.asmdef`.
- Scope: 26 C# files; nested assemblies excluded.
- Changes: 16 owned source files, 14 existing public documents, four new regression files with unique metadata.
- Regression inventory: 48 authored NUnit/Unity cases; none executed by this worker.
- Constraints honored: no child agents, commits, configuration/asmdef edits, installs, Unity/MCP, builds, or test runners.
- Reused the existing audit/session after the watchdog continuation; no review restart or duplicate mutations.

Кратко: проверены все 26 файлов; исправлены повторная выдача наград, ошибки восстановления таймеров,
жизненного цикла рулеток, оплаты и выбора линий слота, переполнения и обработка пустых ссылок.
Тесты добавлены, но не запускались: проверка в Unity остаётся задачей координатора.

## Scope

- `Assets/Neoxider/Scripts/Bonus/Collection/Box.cs`
- `Assets/Neoxider/Scripts/Bonus/Collection/Collection.cs`
- `Assets/Neoxider/Scripts/Bonus/Collection/CollectionVisualManager.cs`
- `Assets/Neoxider/Scripts/Bonus/Collection/ItemCollection.cs`
- `Assets/Neoxider/Scripts/Bonus/Collection/ItemCollectionData.cs`
- `Assets/Neoxider/Scripts/Bonus/Collection/ItemCollectionInfo.cs`
- `Assets/Neoxider/Scripts/Bonus/LineRoulett.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/CheckSpin.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/BetsData.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/LinesData.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/SpriteMultiplayerData.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/SpritesData.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/PaylineLineGeometry.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/Row.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/SlotEconomyDefinition.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/SlotElement.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/SlotSymbolWeightOverrides.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/SpeedControll.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/SpinController.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/VisualSlotLines.cs`
- `Assets/Neoxider/Scripts/Bonus/Slot/WinLineRendererPlayback.cs`
- `Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs`
- `Assets/Neoxider/Scripts/Bonus/TimeReward/ResourceRegen.cs`
- `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs`
- `Assets/Neoxider/Scripts/Bonus/WheelFortune/WheelFortune.cs`
- `Assets/Neoxider/Scripts/Bonus/WheelFortune/WheelMoneyWin.cs`

## Findings and fixes

The complete manifest inventory above was reviewed, including runtime C# APIs, Inspector/UnityEvent entry
points, lifecycle/resource ownership, null and boundary inputs, persistence, ScriptableObject data,
dependencies, diagnostics, and Unity version-sensitive calls. No nested assembly exists under this source root.
Findings below are demonstrated by source control flow and concrete regression arrangements; they are not
claims of an executed Unity reproduction. Source paths in the table are relative to `Scripts/Bonus/`.

| ID / severity | Source and trigger | Observable defect before the fix | Completed change / regression evidence |
|---|---|---|---|
| B01 / P1 | `TimeReward/TimeReward.cs:TakeReward`, `TimeReward/CooldownReward.cs:TakeReward`; a grant callback calls `TakeReward` again | Grant events ran before consumption, so the same availability could issue multiple grants | Consume/save before grant events; a `try/finally` guard rejects recursion and releases after listener exceptions. Reward tests cover both components, both save modes, event counts, remaining availability, and exception recovery. |
| B02 / P1 | Both reward components; save-on-take disabled, then two direct `TakeReward` calls | `CanTakeReward` respected manual waiting, but direct takes and claim counts bypassed it | Claim counts now respect the same waiting gate; regressions require one grant until explicit re-arm. An initially available reward also respects a zero per-take cap. |
| B03 / P2 | `CooldownReward.cs:Init/Start/StartTime`; restore a partially elapsed real-time timer | Base initialization ran before real-time save settings were configured, and startup `Play()` reset the loaded remaining time | Configure before base initialization, subscribe before startup actions, and preserve remaining time while starting updates. Play regressions arrange a saved 20-second remainder with startup enabled and disabled. |
| B04 / P2 | `CooldownReward.cs:StartTime/OnBaseTimerCompleted/TakeReward/RestartTime`; first session or initially available reward | Fresh timers had no persisted deadline until disable/claim; completion could report zero claimable rewards and stop forever. `initialProgress = 1` also made `Play()` reset a re-armed reward back to zero | Seed missing deadlines, process startup availability, resume a small positive UTC remainder after early frame completion, explicitly restore full duration after re-arm, and save the running flag. Play cases require two first-session auto-claim cycles and full-duration re-arm for initially available rewards. |
| B05 / P2 | `TimeReward.cs:GetAccumulatedClaimCount`; tiny cooldown plus long absence | Direct double-to-int conversion overflowed, making earned rewards unavailable | Delegate to existing saturating `CooldownRewardExtensions.GetAccumulatedClaimCount`; regression queries a one-day absence with a microsecond cooldown and requires `int.MaxValue` without iterating grants. |
| B06 / P2 | `Collection/Collection.cs:GetPrize/TryAddItem`; catalog contains null entries | Empty entries could be persisted as unlocked and emit prize/unlock events while returning no item | Select and unlock assigned entries only; regressions verify selected ID, save keys, completion state, and no phantom second grant. Catalog indices and array-based totals remain compatible. |
| B07 / P2 | `Collection/Box.cs:progress/TakePrize`; progress changes or collection callbacks recursively take a prize | Bar/text remained stale; an unlock callback could obtain another item before the first threshold was consumed | Progress assignments refresh optional views; guard the complete prize transaction. Tests inspect bar fill, reactive/saved progress, unlocked count, and exactly one prize event. |
| B08 / P2 | `Collection/CollectionVisualManager.cs:SetItem/Subscriber`; missing collection, invalid view/index, or teardown before button cache exists | Selection could dereference a missing singleton or emit invalid IDs; unsubscribe could index a null/short cache | Validate assigned view membership and collection availability; guard cache access on both subscription paths. Selection regression checks rejected IDs and a valid locked preview. Teardown cache path was checked statically. |
| B09 / P2 | `Collection/ItemCollection.cs:SetEnabled`; optional public change event is null | State transition threw before activation/deactivation events completed | Null-safe event dispatch; regression observes one activation and one deactivation, with repeated identical state suppressed. |
| B10 / P2 | `Collection/ItemCollectionInfo.cs:SetData`; switch from an icon-bearing item to one without an icon | Previous item's sprite remained visible | Always assign the new sprite, including null; regression observes the actual Image output. |
| B11 / P1 | `LineRoulett.cs:OnDisable/Roll/SlowDown`; disable during either phase, or start a new roll in `OnWin` | Component disable left the coroutine running, allowing hidden motion/grants; separately started deceleration escaped parent cancellation; terminal cleanup could erase a callback-started roll's handle | Stop the owned routine, nest deceleration in that routine, and clear the completed handle before invoking the win callback. Three Play cases verify cancelled motion/grants, restart, and ownership of a callback-started roll. Invalid setups are rejected before rolling. |
| B12 / P2 | `WheelFortune/WheelFortune.cs:Stop/Spin`; manually stop before a scheduled auto-stop, then spin again | Old scheduled stop could interrupt the next spin; missing wheel transform could consume single use and throw on update | Cancel the pending invoke in `Stop`, validate setup/activity before accepting a spin. Tests inspect state, grant count, and retained single use. |
| B13 / P2 | `WheelFortune.cs:RotateWheel/AlignWheel/ArrangePrizes`; zero/negative phase durations or unassigned prize entries | Zero deceleration never finished; zero alignment duration at zero scaled time produced an invalid interpolation; partial prize arrays threw during authoring | Non-positive values finish the relevant phase immediately; skip null prize references without changing sector indices. Tests observe one completed grant and assigned prize transforms/indices. |
| B14 / P1 | `Slot/CheckSpin.cs:CommitRun`; a visible matrix contains missing-cell IDs `-1` | Three missing cells counted as a winning symbol run, with the default multiplier | Missing IDs cannot commit winning runs; null evaluation matrices return empty results. Tests distinguish missing runs from a valid run after a missing cell and inspect multiplier outputs. |
| B15 / P2 | `Slot/SpinController.cs:AddLine/GetMaxPaylineCountForUi`; optional line-view objects absent or empty | Line count could grow beyond definitions and charge for nonexistent paylines | Cycle within actual definitions when no visual objects are assigned. Both null/empty view cases assert line sequence and bet prices. |
| B16 / P1 | `SpinController.cs:StartSpin`; price/wallet callbacks call `StartSpin`, disable the controller, or a visual table has holes | Start was unguarded until after payment, permitting duplicate charges; incomplete tables could fail after payment | Guard the whole start phase, check activity around payment, establish lifecycle before display callbacks, reject incomplete tables before payment. Play regressions inspect wallet balance/call counts, queued outcome retention, and one completion. |
| B17 / P1 | `SpinController.cs` layout/bet setters and configure methods; reels temporarily idle between staggered starts | `IsStop()` became true before the accepted spin finished, allowing mutations that invalidated the active outcome's dimensions/bet | Block configuration throughout starting/active lifecycle and avoid refreshing partial matrices. Regression stops the first reel, verifies the idle gap, attempts mutations, then settles the original matrix. |
| B18 / P1 | `SpinController.cs:SetPrice/Win`; large bets, multiple paylines, or large winnings | Int multiplication could wrap a price negative and activate free mode; float/int rounding could turn a maximum win into zero | Long/double accumulation with documented saturation at `int.MaxValue`; invalid multipliers do not contribute. Regressions require a chargeable maximum price and one credited/announced maximum payout. |
| B19 / P2 | `Slot/Row.cs` update/visible recovery/visual assignment; destroyed cached element; `SpinController.cs` partial rows/visual queries | Destroyed or unassigned references interrupted settlement, query, or layout paths | Skip destroyed elements in update/fallback/assignment, null-safe visual lookup and sparse-column layout/start paths. Tests observe recovered symbol ID, stopped state, matrix dimensions, and configured surviving reel. |
| B20 / P2 | `Slot/Data/SpriteMultiplayerData.cs:AutoGenerateSpriteMultiplayer`; generate on a fresh asset or with empty source entries | Generation dereferenced an uninitialized output table or a null input visual | Initialize output table and skip null visuals; authoring regression observes generated symbol ID, run counts, and multiplier values. |
| B21 / P2 | `Slot/SlotEconomyDefinition.cs`, `Slot/SlotSymbolWeightOverrides.cs`; finite weights whose float sum overflows, or non-finite weights | Weighted picks/normalization lost the intended distribution and could select a disabled tail entry | Double sums/cursors, finite-positive eligibility, sanitized override writes. Deterministic tests inspect picks, normalized 0.5/0.5 weights, and unchanged shared asset data. |
| B22 / P2 | `Slot/SlotElement.cs:AutoDetectColRow`; standalone element with visible-window-only filtering disabled | Scene label calculation dereferenced a missing parent `Row` | Guard the parent lookup and retain unknown `[-1,-1]` coordinates. This editor-only path is source-checked; Scene-view visual execution remains centrally unverified. |

### Reviewed files requiring no source changes

| File | Review result |
|---|---|
| `Collection/ItemCollectionData.cs` | Data-only ScriptableObject, rarity/category/type fields; no scene object dependency or runtime lifecycle. |
| `Slot/Data/BetsData.cs` | Existing serialized bet array/API retained; consumer arithmetic and invalid-value handling fixed in the controller. |
| `Slot/Data/LinesData.cs` | Serialized coordinate definitions retained; dimensions/ranges validated by `CheckSpin`. |
| `Slot/Data/SpritesData.cs` | Automatic index IDs and null-aware authoring retained; sparse consumer paths audited/fixed separately. |
| `Slot/PaylineLineGeometry.cs` | Null reel fallback, local/world conversion, RectTransform center, and window-row lookup reviewed. |
| `Slot/SpeedControll.cs` | Serializable value configuration and default factory reviewed; no owned resources. |
| `Slot/VisualSlotLines.cs` | Both overloads already handle null arrays, null objects, and invalid line indices. |
| `Slot/WinLineRendererPlayback.cs` | Renderer ownership, configuration/clear, color modes, gradients, sequential/parallel consumers, and timing reviewed. Frame allocations remain an optimization opportunity, not a claimed correctness fix. |
| `TimeReward/ResourceRegen.cs` | Explicit wallet/reward references with same-object fallback, claim deposit, capped-label output, subscribe/unsubscribe reviewed. Uses existing `Money`, cooldown and `TimeToText` APIs. |
| `WheelFortune/WheelMoneyWin.cs` | Prize bounds, missing wallet/label behavior, and gated diagnostics already present. |

### Compatibility and reuse

No public/protected declarations or existing serialized field names were changed. New fields are private
runtime guards. Existing `.meta` files, scenes, prefabs, samples, package manifests, asmdefs, shared tests,
and other workers' documents were untouched. Behavior corrections are documented: negative IDs are missing
cells, invalid visual tables reject starts before payment, arithmetic saturates, and non-finite weights
cannot participate in a distribution. The all-zero economy compatibility fallback is retained.

Consulted the `neoxider-tools` skill and its Bonus/game-system/module/extension references before changes.
Reused `CooldownRewardExtensions`, `NeoTrustedTime`, `SaveProvider`, existing reactive properties,
`TimerObject` operations, `NeoDiagnostics.StableId`, and existing reel/query APIs. No replacement timer,
wallet, picker component, NoCode layer, or framework was introduced. File-only execution and local review
follow the explicit worker contract; this worker's review is not independent QA.

### New regression files

- `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusCollectionTests.cs`: 7 cases.
- `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusRewardTests.cs`: 11 cases.
- `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusSlotTests.cs`: 15 cases.
- `Assets/Neoxider/Tests/Play/Bonus/Audit20261023NeoBonusLifecycleTests.cs`: 15 cases.

All four have unique GUID `.meta` files. Save-writing Edit tests reuse the existing
`Audit20261023NeoEditorTestsSaveScope`; Play tests use a unique temporary save provider, restoring the
previous provider, clock, and time scale. Assertions observe grants, balances, persisted keys, selected
IDs, matrices, transforms, countdown values, and state. Reflection is limited to serialized configuration
and the existing private authoring callbacks; no source-text regression tests were added.

## Documentation

Updated 14 existing English documents under `Assets/Neoxider/Docs/Bonus`:

- Canonical `README.md`, `Collection/README.md`, `Slot/README.md`, and `TimeReward/README.md` now explain
  runtime APIs, optional views, Inspector/UnityEvent wiring, persistence, and module entry points.
- `Collection/Box.md`, `Collection/CollectionVisualManager.md`, and `Collection/ItemCollectionInfo.md`
  describe progress refresh, recursive take protection, selection gates, and clearing absent icons.
- `TimeReward/CooldownReward.md` and `TimeReward/TimeReward.md` explain claim ordering, manual re-arm,
  startup restoration, caps, event exceptions, different timestamp layouts, and differing accumulation policies.
- `Slot/SpinController.md` documents lifecycle configuration locks, refund ownership, complete visual tables,
  monetary limits, and the distinction between automatic CheckSpin payout and caller-owned economy rewards.
  Its code example registers listeners before spinning and uses explicit C# types.
- `Slot/SlotEconomyDefinition.md`, `Slot/Data/SpriteMultiplayerData.md`, `LineRoulett.md`, and
  `WheelFortune/WheelFortune.md` describe numeric/authoring boundaries and cancellation/setup behavior.

No new public guide or documentation tree was created. The Russian summary is in this report.

## Substantial improvement proposal

### Extract a shared reward schedule core with explicit accumulation and persistence policies — NOT IMPLEMENTED

**Problem.** `TimeReward` duplicates date/count logic, while `CooldownReward` derives eligibility from
persisted UTC timestamps but presentation/activation from `TimerObject`. The lifecycle defects above
come from these independent sources of truth. The two components also implement materially different
accumulation policies: legacy advances the consumed timestamp; the modern component resets from now,
discarding excess backlog and fractional elapsed time. Migrating without an explicit policy can change
the player's earned rewards. UnityEvent callbacks currently provide effects without a durable claim receipt.

**Architecture/public API.** Add a plain C# `RewardSchedule` inside `Neo.Bonus`, with configuration and
state values that contain no scene references. Suggested public contracts:

```csharp
RewardScheduleState GetState(DateTime nowUtc);
bool TryClaim(DateTime nowUtc, out RewardClaim claim);
void Restart(DateTime nowUtc);
void Restore(RewardScheduleSaveData data);
RewardScheduleSaveData Capture();
```

`RewardScheduleSettings` specifies cooldown, maximum batch size, initial availability, manual re-arm,
and an accumulation enum: `ResetFromClaimTime` or `PreserveElapsedRemainder`. `RewardClaim` contains a
claim sequence/receipt ID, count, and next availability. The core updates state before returning a
successful claim; the component persists that state, then dispatches effects. An application can use
the receipt ID with an idempotent grant sink when durable rewards are needed. Tests supply explicit
UTC values; scene adapters obtain them from the existing `NeoTrustedTime` facade.

Keep `CooldownReward` and `TimeReward` as serialized-compatible adapters. TimerObject/UI hooks become
presentation of the schedule's remaining time, rather than a second authority for earned claims.
`ResourceRegen` continues to bind the same public claim/countdown events. Optional ScriptableObjects
can author settings, but must contain no scene wallet or view references.

**Compatibility/migration.** Preserve all current fields and UnityEvent names. Default the modern adapter
to `ResetFromClaimTime` and the legacy adapter to `PreserveElapsedRemainder`. Introduce a versioned saved
record alongside the existing keys, with explicit one-time import of either the legacy last-claim key or
modern `_rt` end key. Never silently reinterpret one format as the other. Keep the old timestamp keys
readable through a migration window and document callback ordering/at-most-once consumption semantics.

**Acceptance scenarios.** Pure C# tests should cover 10.5 elapsed cycles with a cap of 3 (7.5 cycles remain
under preserve mode; reset mode starts a full cooldown), missing/corrupt records, future timestamps,
zero caps, manual re-arm, repeated/recursive claims, listener failure, and save/restore equivalence.
Adapter tests should cover first-session auto-claim, partial/expired offline restore, disabled objects,
initial availability, absent UI/wallet, and a trusted clock ahead of device UTC. Migrating either existing
key format must preserve its previous policy and grant no duplicate receipt.

**Effort/risk.** Approximately 3–6 engineering days including migration fixtures and focused integration
QA. Risk is medium: saved-state migration and event timing affect shipped games. Stage adoption behind
the adapters and preserve their defaults; avoid changing other assemblies until their clock/persistence
contracts are agreed with the coordinator.

## Verification and remaining risks

### Actual checks run

- Full manual source review: **26/26 manifest C# files**; verified the physical C# inventory exactly
  matches the manifest and contains no extra/nested source assembly.
- Reviewed scoped source diffs against `ca198556`; mechanically compared public/protected declaration
  lines with expression bodies excluded: **pass for all 16 modified source files**.
- `git diff --check -- <owned source/docs/new test paths>`: **pass**. Untracked tests are additionally
  checked for trailing whitespace, since normal Git diff does not include their contents.
- Strict Python UTF-8 decoding and absence of U+FFFD: **pass for the 38 changed package files**
  (16 source, 14 documents, four tests, four metadata files); report checked in the closing pass.
- New regression files contain no `var` declarations; all four metadata GUIDs appear exactly once in
  an `rg --no-ignore` scan of **all Assets metadata**: **pass**.
- Relative links in changed public documents: **42 links resolve**.
- Owned source scan for direct `GetInstanceID()` / `objectReferenceInstanceIDValue`: **no occurrences**.
- Regression attribute inventory: **48 authored cases**. This is an inventory count, not test execution.

### Centrally pending / coordinator actions

1. Compile and execute the three new Edit fixtures and the Play fixture, plus existing Bonus regression
   coverage, on supported Unity versions. **No compiler, Unity process, build, test runner, or independent
   QA agent was run here.** Static declaration/encoding checks do not prove compilation or runtime behavior.
2. Check inherited `TimerObject` integration after concurrent worker changes. This audit read that
   dependency as context and did not edit it. Its saved real-time arithmetic still uses `DateTime.UtcNow`,
   while Bonus claim checks use `NeoTrustedTime.UtcNow`; trusted/device clock offsets need a coordinated
   `Neo.Tools.Time` decision and integration regression. No cross-owned source was changed for this issue.
3. Include the existing shared Edit-test save scope in central compilation. Required production references
   (`Neo.Bonus`, `Neo.Save`, `Neo.Tools.Time`, `Neo.Shop`) already exist in the test asmdefs inspected here;
   this worker made no shared assembly/configuration changes. Central compilation must confirm UI/test
   reference resolution as well.
4. Scene-view standalone SlotElement label behavior and teardown-before-button-cache initialization
   have focused static fixes but no claim of executed visual/lifecycle QA. Existing samples/prefabs were
   neither rewritten nor opened in Unity.
5. The current modern cooldown's reset-on-claim accumulation policy and the all-zero economy fallback
   remain intentionally compatible and are now documented. Numeric results remain subject to the existing
   float wallet interface's precision. The improvement proposal addresses the reward policy boundary;
   it is separate from the completed defect fixes.
