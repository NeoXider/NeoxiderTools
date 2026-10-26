# Neo.Core — bounded correction receipt

Status: **source-final; central compilation and test execution pending**.
Same owner/session, `gpt-6.1-sol`, max. This correction implements the coordinator's selected P1-D1
fix and small concrete-model preflight API. The original audit and plan remain phase snapshots.

## Changed artifacts

- `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolModel.cs`.
- `Assets/Neoxider/Docs/Core/README.md`.
- New `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourceLifecycleTests.cs` and unique `.meta`.
- New `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreCanSpendTests.cs` and unique `.meta`.
- This receipt only; no other assembly source or documentation changed.

## Completed correction: operation-local depletion

Both `Decrease` and `TrySpend` capture their own positive-to-zero transition immediately after their
state change, before `NotifyChanged`. They retain Changed-then-Depleted dispatch order.

For HP 10, an outer decrease/spend of 1 invokes a changed listener that decreases the remaining 9.
Only the nested operation now reports depletion; the outer decrease returns 1, and the outer spend
returns true with a null failure reason. Further decreases at zero retain their existing Changed
notification behavior and emit no additional depletion. A positive spend at zero fails without events.
If a Changed listener revives an operation's depleted pool, that operation still reports the transition
it committed, after Changed callbacks finish.

Tick implementation is unchanged in this correction. Its new exception regression checks the existing
finally cleanup: a listener throws after regen reaches 20; the next tick reaches 30 with exactly two
total changed callbacks. This detects a stuck ticking guard or a retained/duplicated snapshot.

## Completed additive public API

```csharp
public bool CanSpend(string resourceId, float amount, out string failReason)
```

`CanSpend` and `TrySpend` share the private `ValidateSpend` method. Validation order and reasons remain:

| Condition | Result | failReason |
| --- | --- | --- |
| NaN or either infinity | false | `Amount must be finite.` |
| Finite amount <= 0, including missing/null id | true | null |
| Unknown positive-cost resource | false | `Unknown resource.` |
| Current below positive cost | false | `Not enough resource.` |
| MaxDecreaseAmount prevents full payment | false | `Amount exceeds decrease limit.` |
| Affordable complete positive cost | true | null |

The preflight only calls validation, pool lookup, and the existing read-only decrease clamp. It does
not alter entries, timer accumulators, or events. TrySpend consumes the validated full positive amount.
The shared check preserves balance-before-cap failure precedence and case-insensitive ids.

For the Abilities owner: call this method on the concrete `ResourcePoolModel` with the captured cost.
It describes current affordability and reserves nothing; TrySpend validates again when actually paying.
Immediate outcomes match when no intervening mutation occurs. `IResourcePoolProvider` is unchanged.
The broader owned-state/transaction/change-record proposals remain unimplemented.

## Exact new regression coverage

`Audit20261023NeoCoreResourceLifecycleTests.cs`: **4 methods / 5 cases**.

- `Decrease_NestedDecrease_EmitsOneDepletionAndReturnsOwnAmount`.
- `TrySpend_NestedDecrease_EmitsOneDepletionAndReturnsOwnSuccess`.
- `Depletion_ChangedListenerRevivesPool_StillReportsTheCommittedTransition` — decrease/spend variants.
- `Tick_ThrowingChangedListener_CleansGuardAndSnapshotForNextTick`.

`Audit20261023NeoCoreCanSpendTests.cs`: **6 methods / 23 cases**.

- `CanSpend_AboveDecreaseLimit_FailsWithoutChangingBalanceOrEvents` — cap 4 / cost 10.
- `CanSpend_AffordableCost_PreservesBalanceAndBothTimerRemainders` — repeated queries preserve current
  20, then retained regen/heal timers produce 22 and 27, with the expected event counts.
- `CanSpend_MatchesImmediateTrySpendWithUnchangedValidationSemantics` — nine cases covering affordable,
  limited, insufficient, zero/depleted, full depletion, and finite nonpositive costs.
- `CanSpend_MissingPositiveCost_RejectsWithoutChangingOtherPools` — null, empty, unknown ids.
- `CanSpend_MissingNonpositiveCost_PreservesSuccessfulNoop` — three missing-id no-op cases.
- `CanSpend_NonFiniteAmount_RejectsBeforeIdLookupWithoutEvents` — six known/missing-id combinations.

All new cases assert public outputs, pool values, events/order, or timer progress; no reflection or source
text tests. No unsupported parallelization attribute was added. Existing 34 Core audit cases were not
edited by this correction and remain part of the central regression scope.

## Actual validation and remaining debt

Executed static checks passed:

- Strict UTF-8 and all 21 existing package mojibake-marker checks for changed/new artifacts.
- Explicit types, balanced C# delimiters, supported attribute scan, README links, new-file whitespace.
- Both new script GUIDs are unique across package metadata.
- Public method signature comparison against baseline: exactly CanSpend added; zero signatures removed.
- Hash comparison confirms original audit, original plan, provider interface, and Core asmdef unchanged.
- Scoped `git diff --check` exited 0; new test/meta whitespace was checked separately because the files
  are untracked. Manual review confirms the preflight call graph has no state/timer/event writes.

Source-final `ResourcePoolModel.cs` SHA-256:
`c831ad29e6c30cd0eee29bcf67f9d43a631239bba16022701f3abd2149c66857`.

**Not executed:** C# compilation, Unity import/console, EditMode/PlayMode, builds. No Unity or test/build
process was launched. The coordinator should execute the 28 new cases plus the previous Core/resource
and level regressions after the source wave, including both supported Unity compatibility branches.
No runtime pass is claimed from static validation. No commits, shared configuration changes, or child
agents were used.
