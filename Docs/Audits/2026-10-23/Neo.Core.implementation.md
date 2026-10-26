# Neo.Core — 11.0.0 implementation receipt

Status: **SOURCE-FINAL; acceptance UNEXECUTED** on `demo`, target `11.0.0`.
Same original owner/session `neo-audit-1023-b02-08`; `gpt-6.1-sol`, reasoning `max`.
Read the complete implementation contract, exact request, original audit/plan and relevant current
source. Preserved the partial edits across the Windows interruption. Global review approval was
recorded; execution remained disabled. No shared tracker was edited.

## Published dependency API

Available now in `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolModel.cs`:

```csharp
public bool CanSpend(string resourceId, float amount, out string failReason)
```

`CanSpend` and `TrySpend` share validation: reject non-finite amounts, accept finite nonpositive
amounts as successful no-ops, then check id, balance, `MaxDecreaseAmount` and an effective stored
decrease, in that order. The NI2 correction rejects a positive cost whose candidate float balance
cannot decrease, with `Amount does not decrease resource.`; ordinary fractional costs do not require
an exact delta/amount match.
The query writes no balance, timer or event and makes no reservation. `IResourcePoolProvider`
is unchanged. Neo.Abilities may use it for captured-cost preflight under `ABIL-CAPPED-PAYMENT`;
consumer payment/captured-cost edits remain owned by Neo.Abilities. This API is source-ready,
with authored regression cases still UNEXECUTED.

## Approved actions and completed source

| Action | Final disposition |
| --- | --- |
| `CORE-DEPLETION-REENTRY` | Preserved the earlier correction: `Decrease` and `TrySpend` capture their own positive-to-zero transition before Changed callbacks, then publish Depleted. Nested changes cannot replace that captured transition. |
| `CORE-CANSPEND` | Preserved the shared, read-only validation and concrete additive API above, including full-cost decrease-cap admission and existing failure precedence. |
| `ABIL-CAPPED-PAYMENT` | Core dependency is source-final: exact preflight and cap-aware `TrySpend` are available, including the NI2 stored-decrease check. Captured ability costs/payment orchestration remain Neo.Abilities-owned; this receipt does not claim its consumer action complete. |
| `CORE-XP-REENTRANT-PAYLOAD` | `LevelModel.AddXp` captures the recomputed resulting level before `OnXpGained`. Its later level comparison/event uses that captured value, retaining XP-before-level order under nested additions or resets. |
| `CORE-POOL-REGISTRATION-GENERATION` | Each valid `AddPool` creates a fresh opaque registration token, including same-entry replacement. `RemovePool` retires it. Tick snapshots id/entry/token and checks the token before regen and before heal; a new registration receives none of the old snapshot's delta. |
| `LEVEL-DATA-DOC` | Canonical `LevelCurveDefinition.md` links `Level/Data/CurveEvaluation.md` and explains the 11.0 fractional endpoint migration. Nested Data source/docs and centrally owned folder metadata were not edited. |
| Root-confirmed NI2 precision lead | `Decrease` returns previous minus stored final current, captured after clamping and before callbacks. Shared spending validation rejects a zero-effective candidate after the established failure checks, without state/timer/event writes. The float backend and ordinary fractional spending remain. |

Exact files changed in this implementation phase:

- `Assets/Neoxider/Scripts/Core/Level/Domain/LevelModel.cs`
- `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolModel.cs`
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreLevelTests.cs`
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourceTests.cs`
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreResourceLifecycleTests.cs`
- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoCoreCanSpendTests.cs`
- `Assets/Neoxider/Docs/Core/README.md`
- `Assets/Neoxider/Docs/Core/LevelCurveDefinition.md`
- `Docs/Audits/2026-10-23/Neo.Core.implementation.md`

Existing regression files/metadata were reused; no new package files or assembly references were
needed. Core README documents captured XP event payloads and fresh pool registration/timer semantics,
alongside the existing C# examples, preflight contract and optional component/NoCode backlinks.
The NI2 follow-up changes only ResourcePoolModel, ResourceLifecycleTests, CanSpendTests, Core README
and this receipt; the earlier XP/registration/curve-documentation patches remain intact.

## Authored acceptance — UNEXECUTED

First implementation pass: seven new cases in five methods:

- `AddXp_NestedXpListener_PreservesEachOperationsLevelPayloadAndEventOrder`: two 100-XP additions
  produce XP totals 100, 200, then nested level `(2,3)` and outer `(1,2)`; live state is level 3 / 200 XP.
- `AddXp_XpListenerResetsState_DoesNotSuppressItsCommittedLevelTransition`: resetting to zero XP in
  the callback still leaves the outer `(1,2)` payload after its XP event; live state remains the reset.
- `AddXp_XpListenerChangesState_DoesNotInventALevelTransitionForTheOuterOperation`: adding 50 XP
  then resetting to 200 in the callback produces only the 50-XP event, with no invented level event.
- `Tick_ReRegisterSameEntryDuringRegen_StartsNewRegistrationNextTick`, two cases: direct same-instance
  replacement and remove/readd, with case-insensitive ids. Current remains 7 after the old tick and
  another half second, then becomes 10 after the next half second; Changed counts are 1, 1, 3.
- `Tick_ReRegisterAnotherSnapshotEntry_SkipsItsOldRegistration`, two cases: the first callback replaces
  whichever pool has not run yet, independently of dictionary order. That registration stays at 7;
  the first pool completes at 13. Half-second ticks then reach 10 and 16, with six total notifications.

NI2 follow-up: nine new cases in six methods:

- `Decrease_BelowStoredPrecision_ReturnsZeroAndRetainsChangedNotification`: current 100,000,000,
  decrease 1 returns zero, keeps current unchanged, emits one Changed and no Depleted.
- `Decrease_ReturnsActualStoredDeltaAfterRoundingAndLimit`, three cases: requests 5 and 9 without
  a limit, and request 20 limited to 5, all store 99,999,992 and return the actual delta 8.
- `Decrease_ChangedListenerReplacesBalance_ReturnsItsCapturedStoredDelta`: that rounded subtraction
  returns 8 even when its Changed listener sets current to 40; both Changed payloads remain observable.
- `CanSpend_ZeroEffectiveCost_MatchesTrySpendWithoutChangingStateEventsOrTimers`: cost 1 at
  100,000,000 fails preflight and payment with the same precision reason, preserving balance, max,
  both pending timer phases and zero events. Later scheduled regen/heal reach 100,000,024.
- `CanSpend_ZeroEffectiveCostAboveDecreaseLimit_PreservesLimitFailurePrecedence`: a zero decrease
  limit still reports `Amount exceeds decrease limit.` before the new precision rejection.
- `CanSpend_EffectiveRoundedCosts_PreserveImmediateTrySpendSuccess`, two cases: 1 minus 0.1 and
  100,000,000 minus 5 both pass preflight and payment, despite their stored deltas differing from cost.

Prior cases remain in the five individually owned fixtures
`Audit20261023NeoCore{Level,Resource,Profile,ResourceLifecycle,CanSpend}Tests.cs`.
There are **78 authored NUnit cases total**: 34 original audit, 28 bounded correction, 7 initial
implementation and 9 NI2 follow-up cases.
The retained lifecycle cases cover outer Decrease/TrySpend nesting, revived depletion, zero-state
behavior and cleanup after a throwing Tick listener. The retained 23 CanSpend cases cover cap-4/cost-10
failure without events, affordable read-only preflight preserving both timer remainders, immediate
TrySpend parity, failure precedence, missing ids, non-finite amounts and successful nonpositive no-ops.
No unsupported `NonParallelizable` attribute was added. No authored case was executed.

## Compatibility, reuse and dependencies

No serialized names, DTO layouts, public signatures or provider requirements were changed in this
phase. The published `CanSpend` addition came from the earlier bounded correction. Caller-owned mutable
entries, ordinary synchronous calls, event order, per-cycle recovery caps and timer cleanup remain.
The token dictionary and retained tuple snapshot reuse the existing BCL/model design; tokens allocate
on registration, with no token allocation for each Tick. Models remain single-threaded.

Intentional 11.0 behavior corrections: XP level events describe their originating operation even
when callbacks have already changed live state; same-instance re-registration starts fresh timers
on the next Tick. The canonical curve guide also documents the Data owner's fractional endpoint fix:
final X = 2.6 ends at integer level 2 instead of exposing rounded level 3. No asset rewrite is performed.
Optional owned-state, immutable change-record and transaction frameworks remain proposals in the
historical plan; none was introduced.

NI2 intentionally changes rounded damage receipts: a nominal decrease can now return zero or a
different representable delta, excluding callback mutations. A positive spend that rounds away now
fails without observers or timer changes. Existing finite nonpositive no-ops and failure precedence
remain; no exact float equality requirement or double/integer resource backend was introduced.
HealthComponent and Abilities consumers receive these actual deltas through the unchanged
`public float Decrease(string resourceId, float amount)` signature; their integration acceptance
must include zero-effective and rounded receipts. No consumer assembly was edited here.

**Remaining Core source/API dependency: none.** Neo.Abilities owns captured-cost preflight/payment
integration using the published concrete API, which makes no reservation or arbitrary-callback
transaction guarantee. Core Level.Data owns curve implementation/acceptance; root owns missing folder
metadata centrally. Neither blocks these completed Core source edits.

## Actual review and evidence debt

Performed source reads, scoped diffs and manual event/timer tracing. Read back the new regression
bodies and documentation changes. Python was used only for source reads, case-attribute enumeration
and SHA-256 artifact bookkeeping. No compiler, Unity/MCP, test runner, product checker, service,
installer, commit/push or child agent was launched. No new validation-pass claim is made.

The original audit, plan and correction hashes remain respectively
`6d197e36308cc46878ac2de1e6a27d167ad5c93c164703ba4b4290e7239a27d7`,
`cfe234b552b01ebc197e83f76cf1e2db348327f90e105170a30d6815789b8e5e`, and
`e673aac6ed8673b7b6ed263f007a5115d4a6a9fc76c5a8c26398c314aabcf8c5`.
Provider interface and Core asmdef hashes also match the phase-start snapshot.

Source-final SHA-256:

- `LevelModel.cs`: `b421d876e131a0f89fd3c90e5148724bbd765089818040b309f7aa161ec71d70`
- `ResourcePoolModel.cs`: `7fe3ac2cebdada0a6b212370cab520f88456a14b9997688c3ec1c5520667213f`

After EVERY writer finishes, root must compile the frozen integrated source on Unity 6.3 and 6.6
and execute the 78 Core cases plus existing level/resource and dependent wrapper/Abilities
coverage. Current compilation, Edit/Play discovery, native behavior, packaging/text/meta checks and
cross-owner integration remain unverified. Historical audit checks do not validate this final snapshot.
