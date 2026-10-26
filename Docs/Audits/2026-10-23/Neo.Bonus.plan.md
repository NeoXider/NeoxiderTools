# Neo.Bonus: implementation plan

Phase 2, same auditor/session; audit baseline `ca198556`, package `10.19.0`.
This phase writes only this plan. The coordinator reads and integrates it after concurrent audits finish.

## Current outcome

The completed [audit](Neo.Bonus.md) reviewed 26/26 C# files and corrected B01–B22 in 16 source files:
duplicate grants/charges, reward startup/restoration, roulette cancellation, missing-cell wins,
payline selection, overflow, authoring failures, and null references. Fourteen existing English guides
now cover C# and scene workflows. Public declarations and serialized field names were preserved.

Four `Audit20261023NeoBonus*` fixtures contain 48 authored cases: Collection 7, Reward 11, Slot 15,
Lifecycle 15. Diff, UTF-8, GUID, declaration, and documentation-link checks passed during the audit.
No Unity compilation, test execution, or independent runtime QA has occurred. Those fixes are completed
source work, not pending proposals or verified runtime results.

Current reward source, timer/clock boundaries, test configuration, documentation, and slot guards were
rechecked for planning. No additional owned correction is asserted beyond the remaining boundary below.

## Priorities and remaining work

### P1: unify the cooldown clock boundary — confirmed source defect

`Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs:397–545` saves/loads real-time timestamps with
`DateTime.UtcNow`; `Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs:229–277` evaluates claims
with `NeoTrustedTime.UtcNow`. Example: device time 12:00, trusted time 12:10, duration 60 seconds.
After a claim, saving an end of 12:01 still leaves it claimable against 12:10. Recursive guards do not
prevent another sequential claim. This follows from current arithmetic; the runtime reproduction is pending.

**Owners/order:** `Neo.Tools.Time` first adds a protected virtual `GetUtcNow()` hook in `TimerObject.cs`,
defaulting to device UTC, and routes every real-time save/load calculation through it. Then Neo.Bonus
overrides it in `CooldownReward.cs` to return trusted UTC. Preserve unrelated timer defaults and save keys.
Do not independently replace `SaveState` or copy `LoadState` into Bonus.

Add `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusClockTests.cs`; the Time owner adds
`Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsTimeClockTests.cs`. Inject an existing `INeoClock`
through `TrustedClock` with trusted time ten minutes ahead. One available claim must succeed; the next
must fail, its saved deadline must be one cooldown after trusted now, and restore must retain that countdown.
Also prove ordinary TimerObject instances retain their default clock behavior.

### P1: central verification debt — no claimed compiler/test failure

Coordinator executes the four existing audit fixtures under `Tests/Edit/Bonus/` and `Tests/Play/Bonus/`,
then existing Bonus and affected TimerObject regressions. Observe wallet balances, one completion/grant,
saved timestamps, matrix orientation, cancellation, and first-session regeneration. Reproduce failures
before making the smallest corresponding correction; do not replay completed fixes.

`Neo.Editor.Tests` owns the shared `Audit20261023NeoEditorTestsSaveScope`; `Neo.Tests.Play` owns Play
infrastructure. Confirm shared-scope inclusion and UI reference resolution during compilation. Missing
references remain an unconfirmed risk; only the coordinator/test owners change asmdefs after an actual error.

### P2: finish uncovered editor/lifecycle observations

For `Slot/SlotElement.cs`, inspect a standalone automatic Scene label with visible-window filtering disabled:
unknown coordinates must render without an exception. For `Collection/CollectionVisualManager.cs`, assign
buttons after initialization and destroy the manager before its callback cache exists: teardown must produce
no exception or manager callback, and unrelated button listeners must remain usable. These are verification
gaps in existing fixes, not newly demonstrated defects. Owner: Neo.Bonus with central Editor QA.

## P2 proposal: a shared reward schedule core — not implemented

**Problem/reuse.** Two adapters currently combine eligibility, persistence, presentation, and different
backlog policies. Reuse `CooldownRewardExtensions`, UTC conversion helpers, `NeoTrustedTime`/`INeoClock`,
`SaveProvider`, reactive properties, and `TimeToText`; keep `TimerObject` for presentation. Do not introduce
another clock, wallet, scheduler framework, or mandatory NoCode system.

Add these exact files inside `Assets/Neoxider/Scripts/Bonus/TimeReward/`:

- `RewardSchedule.cs`: ordinary C# state transitions; callers supply UTC explicitly.
- `RewardScheduleContracts.cs`: settings, state, claim receipt, save record, accumulation enum.
- `RewardSchedulePersistence.cs`: versioned SaveProvider import/export adapter.

Suggested public API:

```csharp
RewardScheduleState GetState(DateTime nowUtc);
bool TryClaim(DateTime nowUtc, out RewardClaim claim);
void Restart(DateTime nowUtc);
void Restore(RewardScheduleSaveData data);
RewardScheduleSaveData Capture();
```

Settings specify duration, batch cap, initial availability, manual re-arm, and `ResetFromClaimTime` versus
`PreserveElapsedRemainder`. A receipt exposes count and a persisted sequence ID. Successful transitions
consume state before adapters persist and publish effects. Receipt IDs permit idempotent application
consumers; ordinary UnityEvents do not promise exactly-once delivery across a crash.

Adapt existing `CooldownReward.cs` and `TimeReward.cs` behind unchanged public APIs/UnityEvents.
`ResourceRegen.cs` retains its current binding contract and needs integration coverage, not a planned rewrite.
No new editor component or settings asset is necessary; existing serialized settings populate the core.

**Migration.** Preserve modern reset-from-now and legacy elapsed-remainder defaults. Explicitly import
`LastRewardTime<key>` as a last-claim timestamp and `<key>_rt` as an end timestamp; never interchange formats.
Write a versioned companion record with manual-waiting state and receipt sequence, retaining old keys during
the migration window. Historical keys cannot reconstruct missing waiting/receipt information: preserve
their legacy interpretation rather than inventing it. Invalid records and date bounds need explicit validation.

Add `Tests/Edit/Bonus/Audit20261023NeoBonusRewardScheduleTests.cs` and
`Tests/Edit/Bonus/Audit20261023NeoBonusRewardMigrationTests.cs` under `Assets/Neoxider/`; extend the existing
Play lifecycle fixture only for adapter equivalence. Minimum acceptance: 10.5 elapsed cycles, cap 3, produce
3/3/3/1 grants then half a cooldown under preserve mode; reset mode grants 3 then waits a full cooldown.
Also cover zero cap, manual waiting across restore, future/corrupt timestamps, recursive calls, throwing
listeners, both old key formats, unique receipts, and absent UI/wallet. None of these proposed cases has run.

Update existing `Assets/Neoxider/Docs/Bonus/TimeReward/{README,CooldownReward,TimeReward,ResourceRegen}.md`:
show direct core usage and unchanged Inspector bindings, migration rules, policies, and delivery limits.
Add unique `.meta` files; no new assembly dependency or public documentation tree is planned.
Estimated effort: 3–6 engineering days; medium migration/event-order risk. Clock correction can ship independently.

## Implementation order and acceptance gates

1. Integrate current audit source, then reproduce/fix the P1 clock seam across its two owners.
2. Run focused clock and 48 existing audit cases; resolve actual compile/reference/lifecycle failures.
3. If selected, implement/test the pure core, migration adapter, then component adapters and documentation.
4. Coordinator compiles supported Unity 6.0/6.4 and 6.5+ targets and records actual versions/results.
   Keep ID compatibility in `NeoDiagnostics.StableId` / `NeoComponentHealth.StableId`; add no scattered version gates.
5. Validate package imports and save migration. Preserve `Samples~` release layout; validation must also
   accept developer `Samples` and imported `Assets/Samples/NeoxiderTools/<version>/<sample name>` roots.

Stop a failing integration path with its reproduction and owner; continue unrelated work. Do not mark
runtime acceptance complete from static checks. Product decisions remain limited to selecting the optional
core and any durable delivery policy beyond current at-most-once consumption. Until then, retain documented
backlog defaults, all-zero weight fallback, integer saturation, and the existing float wallet precision limit.
