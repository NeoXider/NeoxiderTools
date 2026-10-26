# Neo.Bonus implementation receipt

Status: **SOURCE-FINAL — execution deferred**. Date: 2026-10-24. Same original Neo.Bonus owner/session; approved implementation on `demo`, target 11.0.0. The original audit and individual plan remain historical evidence and were not edited.

## Approved actions completed

| Action | Source result |
| --- | --- |
| `BONUS-CLOCK-BOUNDARY` / root trusted-clock integration | `CooldownReward.GetUtcNow()` overrides the new protected virtual TimerObject hook and returns `NeoTrustedTime.UtcNow`. Eligibility and inherited RealTime persistence now share the trusted clock, including initialization, restart, disable/save and restore. |
| `BONUS-ADVANCE-PRECISION` | Legacy `TimeReward.AdvanceLastClaimTime` delegates to `CooldownRewardExtensions.AdvanceLastClaimTime`. It inherits widened multiplication, tick conversion and UTC DateTime saturation rather than multiplying claims by cooldown in float. |
| `BONUS-BOX-COMMITTED-PRICE` / `C09` | Box settles the clamped remaining progress for an accepted collection unlock before collection UnityEvents. A throwing observer cannot grant a second item for the same paid threshold. No eligible prize consumes nothing. |
| `NI16-01` / P2 progress publication | A private monotonic write revision is captured before `SaveProvider.SetFloat`; superseded outer setters skip reactive/visual publication, including same-value and ABA nested writes. Independent nested credit and the accepted threshold cost are retained. |
| `RootBoxPrecisionFinding.md` / positive effective price | Readiness rejects a positive price whose clamped float candidate cannot decrease finite stored progress, before collection lookup/grant. Readiness and payment share the candidate calculation; rounded positive decreases and legacy zero thresholds remain accepted. |

All original corrections and both root-directed NI16/precision follow-ups are implemented. Previous audit fixes, serialization and regression sources were preserved. No optional service framework was implemented.

## Exact files changed in this implementation phase

Paths are relative to `D:/Git/NeoxiderTools`.

Runtime:

- `Assets/Neoxider/Scripts/Bonus/TimeReward/CooldownReward.cs`: protected `GetUtcNow()` override only in this phase.
- `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs`: replace private advancement arithmetic with the existing extension.
- `Assets/Neoxider/Scripts/Bonus/Collection/Collection.cs`: internal `GetPrize(Action onCommitted)` overload, private `TryAddItem(int, Action)` overload and captured selected prize data. Both ordinary public entry points retain their signatures and share the same selection/unlock implementation.
- `Assets/Neoxider/Scripts/Bonus/Collection/Box.cs`: use that internal settlement seam and private `CommitPrizeProgress()`; retain the existing `finally` claim-guard release. NI16 resume adds private `_progressWriteRevision`, superseded-publication checks in `progress`, and private `GetProgressAfterPrize(float)` shared by readiness and payment.

Owned regression sources:

- `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusCollectionTests.cs`.
- `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusRewardTests.cs`.
- `Assets/Neoxider/Tests/Play/Bonus/Audit20261023NeoBonusLifecycleTests.cs`.

Owned public documentation, with English contract details and Russian summaries:

- `Assets/Neoxider/Docs/Bonus/Collection/Box.md`.
- `Assets/Neoxider/Docs/Bonus/Collection/README.md`.
- `Assets/Neoxider/Docs/Bonus/TimeReward/CooldownReward.md`.
- `Assets/Neoxider/Docs/Bonus/TimeReward/TimeReward.md`.

Completion artifact: `Docs/Audits/2026-10-23/Neo.Bonus.implementation.md` (this file). No new Unity assets or regression files were added in this phase, so existing regression `.meta` files remain untouched. Other owners' runtime, tests, docs, assembly definitions and shared trackers were not changed.

## Commit boundary and compatibility

Collection first accepts the selected unlock in memory and writes its existing save key. Its internal settlement callback then consumes and saves Box progress, publishes reactive progress and threshold events, and returns before `OnItemAdded`, completion events and `OnGetItem`. The selected item data is captured before callbacks so the returned prize remains the actual selected object if an observer changes the catalog.

The settlement callback runs in `finally` around the unlock save. Thus a save-key observer throwing after accepting an unlock still settles its associated cost. Box releases its take guard in `finally` on success, no prize and exceptions. An exception propagates and may prevent subsequent notifications or visuals; accepted ownership and payment are not rolled back or replayed.

**Deliberate 11.0 event-order change:** Box progress/threshold observers now run before collection UnityEvents. `OnTakePrize` still follows the successful collection notification path and prize visuals. A listener must not interpret an exception as a failed/unpaid grant. This is a bounded existing-API correction; independent SaveProvider key writes are not an atomic transaction, and arbitrary observer side effects/provider failures have no general rollback.

Public methods and serialized fields are preserved. Collection keys remain `<prefix>Item_<index>`, Box remains `<Save Name>progress`, cooldown retains `LastRewardTime<Add Key>_rt` plus `_a`/`_t`, and legacy TimeReward retains `LastRewardTime<Add Key>` with round-trip UTC timestamps. There are no automatic scene, prefab, catalog-index or saved-timestamp rewrites. Legacy accumulated-cycle retention remains distinct from CooldownReward's full restart after a take. Plain TimerObject's default clock and seconds modes were not changed by this owner.

NI16's publication fix does not reconstruct a live before/after cost or undo nested commands. The setter increments a private write revision before exposing the save callback, skips stale reactive/visual work after a newer write, and rechecks after reactive publication before updating visuals. Numeric equality is not used to identify the active write. Manual AddProgress/ChangeProgress, claim guard, payment, and progress-before-collection event order are retained.

**Deliberate readiness correction:** a positive threshold now requires finite current progress/threshold and a strictly smaller clamped stored candidate. `CheckProgress` makes the same admission decision used by `TakePrize` before Collection can grant; payment uses the shared subtraction helper. At 100,000,000 progress, threshold 1 is rejected without grant, save, mirror or event mutation, whereas threshold 5 remains valid and stores a debit of 8. There is no exact nominal-price/delta equality requirement or numeric backend change. Ordinary fractional and zero-threshold behavior are retained; zero threshold at zero progress still admits an available item. The existing independent-key/postcommit limits remain unchanged.

## Authored acceptance sources — ALL UNEXECUTED

The initial implementation added the following seven cases, all retained:

| Fixture / case | Observable control |
| --- | --- |
| Collection: `Box_ThrowingCollectionObserver_KeepsTheGrantedItemPaid_AndReleasesTheGuard(false/true)` (2 cases) | Actual item ID 0 and unlock/progress save keys are committed before throwing `OnItemAdded` or `OnGetItem`. Progress and reactive progress are zero inside the observer. Immediate retry grants no item 1; adding a new threshold grants ID 1 and proves guard release. |
| Collection: `Box_ExhaustedCollection_ConsumesNoProgressAndPublishesNoPrize` | Two no-prize calls retain persisted/reactive progress and emit no prize. Extending and loading the catalog supplies a positive control: the next paid take succeeds, proving the no-prize branch released its guard. |
| Collection: `Box_ThrowingSaveObserver_StillConsumesTheAcceptedPrizePrice` | A real SaveProvider key-change listener throws after item 0 is stored; item 0 and zero progress remain committed. Retry cannot grant item 1 until another threshold is supplied. The static listener is removed in `finally`. |
| Reward: `CooldownClaim_UsesTrustedDeadline_AndRejectsSequentialClaimsUntilExpiry` | Frozen trusted 2030 clock, real public takes and Tick calls: first deadline is exactly trusted-now + 60 seconds; immediate and +59-second takes fail; exact expiry permits one take and writes the next future deadline; two total grants. |
| Reward: `LegacyTake_FractionalCooldown_PersistsPreciseConsumedCyclesBeforeGranting` | Bounded three-event public take with `1.4f` cooldown. Independently specified expected advancement is 41,999,999 ticks. Each grant observes that exact persisted last-claim timestamp; batch is three and immediate retry fails. No millions-of-events fixture. |
| Play lifecycle: `CooldownClaim_DisableSaveAndRecreate_RestoresTheTrustedDeadlineAndRemainingTime` | Real Awake/Start/OnDisable paths, claim, +20-second Tick, file Save, destroy, +10-second absence, reopen FileSaveProvider and recreate component. Deadline survives unchanged, countdown restores near 30 seconds, no early/repeated take is possible, exact expiry permits the second grant and future deadline. Frame-driven countdown assertions allow elapsed frame time; UTC deadlines and claim counts remain exact. |

Existing cases were strengthened without changing them to device UTC:

- Box reentry now starts with **two** thresholds, making a nested take eligible except for the guard. Only one item is granted during dispatch; a later explicit call spends the remaining threshold successfully.
- Owned reward Edit tests reuse the primary owner's deterministic `Audit20261023NeoEditorTestsClockScope`, preserving all three prior private NeoTrustedTime cache values and the previous provider without invoking the lazy Clock getter.
- Owned Play tests use an `INeoClock` fixed at 2030-01-02 UTC and exact restoration of `_clock`, `_restored`, `_persistedHighWater`, prior provider and time scale. Objects are destroyed before restoring borrowed state; the fixture's unique temporary store is cleaned up.
- Existing restore fixtures seed trusted-clock deadlines. First-session auto-claim now advances the trusted clock and public Tick through two exact one-second cycles after real Start, checks both grants and future deadlines, and rejects an intervening direct take. Initial availability / RestartTime retain exact trusted-deadline assertions.

The Extensions owner's large arithmetic helper acceptance remains separate; the bounded public regression above exercises the actual Bonus consumer and persisted result. No `[NonParallelizable]` attribute was added. Shared state requires the coordinated exclusive/isolated runner and cleanup contract.

### NI16/precision resume acceptance — UNEXECUTED

Read all of `NativeImplementationReview-16.md` and `RootBoxPrecisionFinding.md`, plus the updated approved request. This resume changed only Box.cs, the existing owned Collection regression source, its public Box guide, and this receipt. It adds nine cases while retaining all 40 previously reviewed cases across these three fixtures: **20 Collection + 13 Reward + 16 Play = 49 authored cases**, not a discovery/pass count. The separate owned Slot fixture is unchanged.

| Collection case | Observable control |
| --- | --- |
| `Box_ProgressSaveObserver_NestedCreditPreservesPaidCostAndLatestMirrors(false/true)` (2 cases) | Real public progress-key listener, 20 progress / threshold 10. Ordinary payment leaves exactly 10; a one-shot AddProgress(5) leaves raw/saved/reactive and Collection observer snapshots at 15. A nested Take is rejected while still affordable, only item 0 is granted, progress/collection/prize event order is retained, and a later independent ChangeProgress(2) updates all mirrors. Handler removed in `finally`. |
| `Box_ProgressSaveObserver_SameValueOrAbaWriteSuppressesObsoletePublication(false/true)` (2 cases) | Actual progress-key observer writes 10, or 15 then 10. A derived reactive property records public virtual Value assignments while delegating to the real base implementation, detecting obsolete outer publication even when equality suppresses duplicate notifications. Latest raw/saved/reactive state remains 10, one prize is granted, and a later write publishes normally. No private revision inspection; handler removed in `finally`. |
| `Box_PositiveThresholdWithNoStoredDecrease_RejectsBeforeAnyGrantOrPublication` | Two usable locked items and the actual Collection singleton, progress 100,000,000 / threshold 1. Readiness is false; repeated Take changes no item, progress/mirror, save key, reactive/progress/threshold/completion/prize event, or bar value. Switching the same box/collection to threshold 5 succeeds, stores 99,999,992, saves the actual item and progress keys, and preserves notification order. Key observer removed in `finally`. |
| `Box_EffectiveOrLegacyZeroThreshold_GrantsExactlyOnePrize` (4 cases) | Fractional 2.5 / 0.75 leaves 1.75; 100,000,000 / 5 leaves 99,999,992 (effective debit 8); zero thresholds at progress 20 and 0 retain legacy grants without debit. Each case requires the usable singleton, actual item 0/persistence, no item 1, exactly one grant/prize event and correct observer/raw/saved/reactive progress. |

No previously authored case was deleted or weakened. Trusted-clock and original throwing-observer controls remain unchanged in this resume. These cases have not been compiled, discovered or run.

## Cross-owner dependencies and evidence actually obtained

- Read `NativeImplementationReview-8.md` and the primary `Neo.Editor.Tests.implementation.md` receipt. Consumed the landed Time API `protected virtual DateTime TimerObject.GetUtcNow()`; source reference enumeration shows its three RealTime writes and two reads call that hook. The Bonus override is now present and returns `NeoTrustedTime.UtcNow`, closing the concrete no-override consumer debt identified by root/NI8.
- Consumed the existing `Neo.Extensions` precision helper; no Extensions source was edited.
- Reused the primary Editor.Tests clock scope; that helper and all primary legacy fixtures remain owned and unchanged by this worker. Their deterministic sequential claim/re-arm controls must run against this integrated override during final verification.
- No pending source API dependency blocks this owner. NI8's other-owner findings and any central test-source cleanup remain with their owners/root.
- NI16 approves the existing trusted-clock override at source level; this resume preserves that source and its deterministic fixtures. Both newly assigned Box corrections are local and require no foreign API/configuration change. NI16's Editor scope remains with that owner/root.

Actual work performed: scoped current-source reads, source reasoning, patches, review of owned runtime diffs, API/call-site reference enumeration and scoped git status bookkeeping. No Unity, compiler, test runner, product checker, installer, service, commit, push or child actor was launched. **No new compilation, test pass, discovery, runtime behavior or package-validation result is claimed.** Historical audit checks do not validate these changes.

## Remaining evidence debt and proposals

After every writer finishes, the coordinated verification operator must compile and obtain nonempty Edit/Play discovery on Unity 6.3 and 6.6, run the owned fixtures plus the primary Bonus reward fixtures in isolated stores, and report actual pass/failure counts. In particular, verify precise fractional timestamp behavior on both supported runtimes, real disable/save/reopen/restore and first-session auto-claim, paid item/progress conservation through each throwing observer path, progress-key nested credit/same-value/ABA publication, and positive effective-price admission including rounded/zero controls. Final UTF-8, metadata/GUID, docs/package and compatibility checks remain under the global verification gate.

The substantial RewardSchedule/runtime-core improvement in `Neo.Bonus.plan.md` remains **a proposal**, not delivered source. It would reuse NeoTrustedTime, the cooldown extensions and SaveProvider behind a reusable scheduling core with optional component adapters. No new mandatory clock/session/transaction framework, journal, reward API migration or arbitrary UnityEvent rollback was introduced to close the assigned bounded corrections. Remaining unverified integration/runtime outcomes are evidence debt, not newly confirmed fixes.
