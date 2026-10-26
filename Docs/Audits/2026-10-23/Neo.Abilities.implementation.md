# Neo.Abilities — approved 11.0.0 implementation receipt

Date: 2026-10-23. Original owner: `/root/audit_abilities`, same native session, `gpt-6.1-sol` / `max`. Repository: `D:/Git/NeoxiderTools`, target `demo` / `11.0.0`, original baseline `ca198556`.

Status: **SOURCE-FINAL; snapshot2 actual Unity 6.3/6.6 compilation FAILED at two owned fixture constructor calls; the bounded World-property correction is now UNCOMPILED / UNEXECUTED.** Discovery, tests, native lifecycle and performance acceptance remain UNEXECUTED. This receipt describes the approved source work, not a runtime pass. This owner used source reads, reference enumeration and human diff review; no Unity/MCP, compiler, test runner, validator/checker, installer, service, commit, push or child agent was launched by this owner during the source phases. Root closed execution again before the snapshot2 correction; a new freeze must follow every writer's completion.

## Authority and phase boundaries

Read the complete [implementation contract](./IMPLEMENTATION-CONTRACT.md), [owned request](./Neo.Abilities.implementation-request.md), original [audit](./Neo.Abilities.md), [plan](./Neo.Abilities.plan.md), [correction](./Neo.Abilities.correction.md) and the relevant current source/docs. The earlier audit, plan, correction and independent review remain phase snapshots. This receipt does not replace their historical evidence or turn earlier checks into verification of this patch.

Only the six runtime files, four owned public guides, two new uniquely named fixtures with their metadata, and this receipt were written during this implementation phase. Nested Editor source and `Editor.md`, Core/RPG source, shared test internals, manifests, asmdefs, package/settings/sample layout and other actors' receipts were not edited. Root explicitly assigned the parent-owned `AbilityDesignerWindow.md` guide to this action.

## Approved actions and resulting behavior

| Action | Source disposition |
| --- | --- |
| `ABIL-OPERATION-RECEIPT-DELTA` / native A-17 | Implemented at all four accepted-delta sites; meaningful capped and reentrant resource regressions authored. |
| `ABIL-UNIT-DISPATCH-COST` | Implemented additive Target subscriptions in the existing bus, common observer order, exact capture phase, scoped unit wrappers and cumulative dispatch counters. Order/reentry/exception and 1/100/1000-wrapper regression sources authored. |
| `ABIL-FINITE-TICK-ADMISSION` | Implemented nonfinite admission guards in both public Tick paths, plus whole-step rejection of system-clock overflow. Ordinary finite timing and the existing eight-tick modifier catch-up cap retained. |
| `ABIL-EDITOR-DOC` | Corrected overriding radius-array wording and added canonical `Editor.md` backlinks from the parent README and legacy designer guide. |
| `ABIL-CATALOG-SLOT` | Prior correction retained: a grant keeps its mutable Blueprint reference; replacing a catalog id affects future grants, explicit revoke/regrant refreshes an existing grant. |
| `ABIL-CLEAR-REACTIONS` | Prior internal route retained and integrated with Target capture; user Clear still preserves modifier reactions. |
| `ABIL-CAPPED-PAYMENT` | Prior captured effective-cost, Core `CanSpend` preflight and exact `TrySpend` payment correction retained. No new payment or transaction redesign. |

### Accepted operation receipts

`DamageService.ApplyDamage` takes `HealthDamage` directly from `ResourcePoolModel.Decrease`. Its lifesteal path takes the healed amount from `Increase`. `HealEffectOperation.Execute` and `AbilityUnitBehaviour.ApplyHeal` likewise publish the returned `Increase` delta.

These values describe what that individual pool operation accepted, after its cap and available balance/headroom. They are no longer reconstructed from a balance that synchronous resource callbacks may already have changed again. For example, 50 HP minus an accepted 10, followed by a callback's 20 heal, finishes at 60 HP while the damage receipt remains 10. Lifesteal uses that accepted damage and reports its own accepted increase. Partial caps remain visible in receipts.

Death and later healing eligibility still use the existing live unit/health decisions. CastId, Source/Target, event ids, mitigation, shield ordering and existing exception propagation are retained. This is not callback isolation or rollback; a throwing resource notification can still abort the calling operation after mutation under the existing Core contract.

### Target routing and the exact observer boundary

New public API on the existing `AbilityEventBus`:

```csharp
void SubscribeTarget(UnitId target, Action<AbilityEventArgs> handler);
void UnsubscribeTarget(UnitId target, Action<AbilityEventArgs> handler);
long ObserverSnapshotCount { get; }
long ObserverSnapshotEntryCount { get; }
long ObserverInvocationCount { get; }
```

`SubscribeTarget` matches `AbilityEventArgs.Target` exactly; Source does not select a Target registration. `UnitId.None` is an explicitly subscribable id. Null handlers are ignored. Duplicate subscriptions remain distinct, and each unsubscribe removes the first matching delegate in that scope. Existing public constructors and Subscribe/Unsubscribe/SubscribeAny/UnsubscribeAny/Publish/Clear signatures remain available.

Publication proceeds in these phases:

1. Capture and invoke the specific event-id handlers, using the existing case-insensitive id lookup.
2. Capture the matching Target registrations and global registrations in one merged batch, using their shared registration sequence.
3. Invoke the system-owned internal modifier-reaction route.
4. Invoke the captured observer batch in that common order.

Specific callbacks' subscription changes therefore affect the current observer capture. Internal or observer callbacks can change future and nested publications, but cannot erase an already captured outer batch. Clear removes all user scopes and retains the internal route. A standalone bus still has no system reaction route. Exceptions propagate and can stop subsequent callbacks/phases; no observer exception aggregation was introduced.

`AbilityUnitBehaviour` now attaches/detaches its exact Target subscription. Its existing current-identity check remains necessary: an already captured callback can run after disable/re-enable has replaced the domain unit, and must reject the old Target.

The implementation indexes Target registrations by `UnitId` and merges only the matching list with the global list. Observer-copy/invocation work is proportional to global plus matching registrations, rather than all enabled unit wrappers. It creates one observer array for a nonempty batch and one observer record per subscription; it is not allocation-free. Specific-event snapshots and internal reaction/effect work remain separate costs.

Counters are read-only, cumulative `long` values for this bus's lifetime. `ObserverSnapshotCount` counts nonempty combined observer arrays; `ObserverSnapshotEntryCount` counts entries copied into them; `ObserverInvocationCount` counts attempted global/Target calls, including a throwing call. Nested publications contribute; specific handler snapshots/calls do not. Clear does not reset counters. These are dispatch-work counters, not measured elapsed time or GC bytes.

### Finite time admission

Both `AbilitySystem.Tick(float)` and direct `ModifierEngine.Tick(float)` return before timer/scratch mutation for NaN, positive/negative infinity and nonpositive steps. `AbilitySystem.Tick` additionally computes its prospective float domain time and rejects the whole step if that sum is nonfinite, before pending-projectile pruning or subsystem advancement.

No finite scheduling redesign was introduced. Lifetime-bounded modifier ticks, TickOnApply on the next accepted positive step, finite cooldown/resource progression and the existing eight-effect-ticks-per-modifier-per-call cap remain. Float-clock precision, finite accumulator/authoring limits, arbitrary nested Tick isolation and cross-callback transactions are not newly guaranteed by these input guards.

### Public documentation

The parent README now documents accepted receipt amounts, Target/global ordering and capture semantics, counter scope/costs, finite time admission and the canonical nested Editor entry. `AbilityUnitBehaviour.md` explains scoped subscription lifetime, final balance versus receipt amount and observer choices. `ModifierDefinition.md` states the finite-step rule and preserves the previously documented shield/removal lifecycle.

`AbilityDesignerWindow.md` now states the actual Editor validation rule: a nonempty `RadiusByLevel` overrides the flat radius, and every authored entry must be positive. A flat six with `[0, 3]` warns; a flat zero with `[2, 3]` passes this radius rule. It links `Editor.md` for exact authoring/validation limits. No Editor behavior or serialized assets were changed by this parent action.

## Exact files written in this phase

Runtime:

- `Assets/Neoxider/Scripts/Abilities/Domain/Combat/DamageService.cs` — accepted health-decrease/lifesteal-increase deltas; prior shield corrections retained.
- `Assets/Neoxider/Scripts/Abilities/Domain/Effects/Ops/HealEffectOperation.cs` — accepted heal-increase delta.
- `Assets/Neoxider/Scripts/Abilities/Components/AbilityUnitBehaviour.cs` — accepted direct-heal delta and Target registration/unregistration; existing fields and identity guard retained.
- `Assets/Neoxider/Scripts/Abilities/Domain/Events/AbilityEventBus.cs` — additive Target APIs/counters, indexed registrations and common-order observer capture; internal route retained.
- `Assets/Neoxider/Scripts/Abilities/Domain/AbilitySystem.cs` — finite/clock-overflow Tick admission and XML contract; earlier slot/cost/Clear changes retained.
- `Assets/Neoxider/Scripts/Abilities/Domain/Modifiers/ModifierEngine.cs` — direct finite Tick admission and XML contract; earlier removal/timing/shield changes retained.

Canonical public guides:

- `Assets/Neoxider/Docs/Abilities/README.md`.
- `Assets/Neoxider/Docs/Abilities/AbilityUnitBehaviour.md`.
- `Assets/Neoxider/Docs/Abilities/ModifierDefinition.md`.
- `Assets/Neoxider/Docs/Abilities/AbilityDesignerWindow.md`.

New regression sources and Unity metadata:

- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesReceiptsAndTimeTests.cs`.
- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesReceiptsAndTimeTests.cs.meta`, GUID `3b7f2e94ab804d7bac823c9e8fe56d10`.
- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesTargetEventTests.cs`.
- `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesTargetEventTests.cs.meta`, GUID `e2c8a30fa94d4a79a73a68ce19f6b2d5`.

This receipt: `Docs/Audits/2026-10-23/Neo.Abilities.implementation.md`.

No new folders or package assembly references were needed. Older changed Abilities files and prefixed fixtures visible in the aggregate working-tree diff belong to earlier completed phases or the nested Editor actor; this list isolates the present phase's writes.

## Authored acceptance sources — all UNEXECUTED

The two new fixtures contain **17 test methods / 28 authored parameter cases**, counted from source, not runner discovery. The receipts/time fixture has 8 methods / 16 cases; the Target fixture has 9 methods / 12 cases. No `[NonParallelizable]` attribute was added; the shared exclusive verification-runner and fixture cleanup contract still applies.

| Fixture / behavior | Expected acceptance |
| --- | --- |
| Damage receipt with healing resource callback, uncapped and decrease-capped | Actual accepted 10/3 damage remains the receipt while final HP becomes 60/67. |
| Depletion followed by scripted callback health restoration | Accepted damage remains 5; final live 20 HP suppresses death. |
| Lifesteal with victim healing and attacker draining callbacks, uncapped/capped | Damage remains 10; lifesteal receipt is accepted 5/3 with original CastId/Source/Target even though final attacker health falls. |
| Heal effect and scene ApplyHeal with damaging callbacks, uncapped/capped | Healing multiplier is honored; accepted 20/4 is published, independent of final 40/24 HP; wrapper UnityEvent receives that delta once. |
| System NaN/+infinity/-infinity Tick | No cooldown/duration/resource/event/projectile-clock advancement; the subsequent valid step preserves expected damage/regen/cooldown/expiry cadence. |
| Direct modifier NaN/+infinity/-infinity Tick | Duration and interval phase remain unchanged; the next valid step delivers the expected tick. |
| Finite system-clock overflow | Rejected step preserves HP50, an active two-second modifier and its pending projectile; the following ordinary one-second step gives HP54 and one second remaining, proving the clock remains usable. |
| Mixed global and Target registrations | Specific callback precedes common-order merged observers; Source-only matching is excluded. |
| Specific subscription mutation | Its removals/additions affect this publication's observer batch. |
| Observer unsubscribe plus nested Publish | Outer captured callbacks survive; nested/later batches see replacement registrations and expected common order. |
| Internal thorns reaction with nested specific Clear | Captured outer Target/global callbacks survive in order, internal reactions remain after Clear, subsequent publication uses new user subscriptions. |
| Duplicate unsubscribe, wrong Target and explicit None | Remove only one matching registration; Clear removes all user scopes and retains lifetime counters. |
| Global/scoped throwing observer | Same exception propagates, later captured callbacks stop, attempted-invocation counter includes the throwing call. |
| Actual wrappers at 1/100/1000 units, 16 publications, one matching wrapper and one global observer | Only that wrapper and global callback receive each publication: expected deltas are 16 observer arrays, 32 copied entries and 32 attempted calls in each workload. These are unexecuted expected counts, not benchmark measurements. |
| Re-enable from a specific callback before observer capture | Old Target wrapper is absent from that captured batch; the newly registered Target receives its next event. |
| Re-enable from an earlier global observer after capture | Captured old wrapper rejects its old Target through the retained live identity check; the new Target receives its next event. |

The existing `AbilitySceneRig` helper is reused without modification; it owns/destroys its GameObjects and restores the previous hub reference. These fixtures manually invoke EditMode lifecycle methods and cannot prove native Unity enable/disable ordering, domain-reload behavior or frame/audio/physics delivery. Pure domain cases use actual pool callbacks and returned gameplay receipts rather than source-text assertions.

## NI3-01 — bounded regression strengthening

Read the complete [native implementation review 3](./NativeImplementationReview-3.md). Its NI3-01 identifies a missing positive recovery control in `SystemTick_FiniteClockOverflowRejectsTheWholeStep`, not a current runtime defect. This follow-up writes only that existing test body and this receipt; the six approved runtime sources, public guides, other fixtures and metadata remain unchanged.

The test still advances an empty system to `float.MaxValue` first. It then creates a living HP50/Max100 unit with HealthRegen4 and a fresh Duration2 modifier with TickInterval0/no tick effects, alongside the successful empty-impact pending projectile. Preconditions assert the fresh health/duration/activity. The overflowing step must preserve HP50, active duration2 and successful `NotifyProjectileHit`. Only after that hit assertion does the test call `Tick(1f)` and require HP54 with active duration1. Those relative resource/modifier values use the accepted delta even where the absolute float clock cannot represent adding one.

This expected transcript distinguishes a prematurely assigned Infinity clock from the correct pre-assignment rejection: the poisoned-clock mutation would reject the later normal step and leave HP50/duration2. That distinction was source-traced, **not executed or mutation-tested**. The later ordinary Tick may normally prune the pending projectile because its absolute expiry rounds at `float.MaxValue`; continued projectile retention is not the recovery assertion, and no `readyAt = time + 1` precondition was introduced.

NI3-01 is closed at source level only. Counts remain **17 methods / 28 authored cases** overall (8/16 receipts-time, 9/12 Target). No new API, test method, attribute, metadata or dependency was added. All compilation/discovery/execution remains UNEXECUTED and the global execution gate stays closed.

## Snapshot2 actual compiler follow-up — two fixture arrangements

Date: 2026-10-24. Same original Abilities owner/session. Read the appended owned request and both actual `compiler-errors.json` files under `Local/Audits/assembly-2026-10-23/unity/final/f84c9007d0494cd8a421d84d70d931a0/{u6000_6_0f1,u6000_3_14f1}/mirror-on-dev/Receipts/`. Both report CS1729 at `Audit20261023AbilitiesReceiptsAndTimeTests.cs:199` and `:260`: `AbilitySystem` has no constructor taking one argument. These are real snapshot2 failures, not hypothetical findings or current passes. The 6.6 diagnostic record SHA-256 is `b1c0d49009a08f9208b499b2a3770fa9681eedd5b66acb994c37b701dc869939`; the 6.3 record SHA-256 is `caaedb21d9b130a8e185e6f23c9d219cef91ba3f6d932e1aa653b900c3eae160`. Other owners' diagnostics were not changed.

Read the real `Domain/AbilitySystem.cs:53` parameterless constructor and `:71` public `World` setter, plus the existing `FakeWorldAdapter` and complete owned fixture. Corrected exactly two arrangement lines in `Assets/Neoxider/Tests/Edit/Abilities/Audit20261023AbilitiesReceiptsAndTimeTests.cs`:

- Line 199 now uses `new AbilitySystem { World = world, PendingProjectileTimeout = 1f }`, retaining the named fake adapter instance.
- Line 260 now uses `new AbilitySystem { World = new FakeWorldAdapter(), PendingProjectileTimeout = 1f }`, retaining a fresh fake world for the overflow case.

Object initialization uses the existing parameterless constructor and assigns the real world seam before any units, casts or ticks. No production constructor, signature, behavior, helper, assertion or timeout was changed. The nonfinite case retains all health/resource, cooldown, duration, event and pending-hit assertions and its valid-step recovery. The overflow case retains the empty-system `float.MaxValue` setup, HP50/duration2/pending-hit rejection checks, and subsequent HP54/duration1 positive recovery control in the original order. Counts remain **17 methods / 28 authored cases** overall (8/16 receipts-time, 9/12 Target).

Actually performed in this follow-up: UTF-8 source/contract/request/receipt reads, existing API/reference inspection, the two-line edit and human unified-diff review against frozen snapshot2. The fixture changed from SHA-256 `1dcc936a3465ec685cffbbf7136748f97e0b0181401c05687e40a15f4ecbd5a6` to `8a3e89fed06cc391c96f91c683837b24c83900ad2cb8d6b4edf186ad62c69903` (297 lines). The actual production `AbilitySystem.cs` remains byte-identical to snapshot2, SHA-256 `04646aa2f2f2f3d3c51b54d130e2a2d6d2b62331cafc54c21167c8fa37f131dc`; fixture metadata likewise remains byte-identical, SHA-256 `ce5ba09dc6c59eb696435e96760f84cc4993d6d8e9c768b502eb169e0518c3e6`. Only this fixture and this receipt were edited in the follow-up.

No Unity, compiler, tests, checkers/validators or independent native-review report was run/written for this correction. Source completion does not close actual compiler acceptance: both versions require fresh compilation, then nonempty discovery and the retained regression execution after ALL writers stop and root authorizes a new freeze. The actual previous error records remain unchanged.

## Compatibility, dependencies and remaining evidence

- Existing serialized fields and public signatures were preserved. Added Target APIs/counters require no scene wiring or asset migration. Global observers retain their original relative order; scoped wrappers retain their original order relative to matching global callbacks.
- Consumers that inferred an operation delta from the final callback-mutated balance must use the corrected receipt for the operation and read the pool separately for the final balance. Damage/healing/kill event phases were not moved by this correction.
- NaN/infinite advancement now does nothing; previously corrupted/exhausted timers from such input are not preserved as intended behavior. Finite domain-clock overflow is rejected as a whole step.
- Existing grants still retain the granted **mutable reference**, not a deep immutable Blueprint snapshot. Earlier explicit revoke/regrant migration, exact per-cost cap semantics, callback-invalidated partial-payment limit and same-instance shield/replacement event-order decisions remain as documented in the correction and canonical guides.
- Reuses current `ResourcePoolModel.Increase`/`Decrease` return contracts and the already landed `CanSpend`/`TrySpend` API at `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolModel.cs`. These were read as integration context; Core was not edited. No pending additive API dependency remains for this action. Native review's Core NI2-01 coarse-float stored-delta/zero-effective-spend correction is a separate OPEN semantic integration dependency assigned to the SAME Core owner; this receipt does not claim it closed. Abilities must continue using the accepted Core return contract rather than callback-sensitive before/after reconstruction.
- Reuses existing `AbilityTestSupport`, `AbilitySceneRig`, `FakeWorldAdapter` and `EventLog` in the primary EditMode test assembly. Shared helper cleanup, exclusive runner behavior and compilation must be confirmed centrally. No unavailable NUnit attribute is relied upon.
- Final installed Unity 6.3/6.6 compilation, nonempty test discovery/execution, native lifecycle coverage, aggregate workload/profile evidence, UTF-8/meta/GUID/docs validation and whitespace/product checks remain root-owned and **UNEXECUTED for this patch**. No historical compiler diagnostic or earlier static pass is current acceptance.
- Further spatial-index/world-adapter, global SlotsVersion invalidation, typed delivery, catalog compiler and arbitrary resource transaction proposals remain proposals; none is required to integrate this bounded implementation.

**По-русски:** исправления в исходниках завершены в прежней сессии. Урон, лечение и lifesteal используют принятую дельту операции; Target-подписки сохраняют прежнюю границу снимка и порядок global callbacks; нечисловой шаг времени отклоняется до изменений. Новые регрессии и документация подготовлены. Сборка, запуск тестов и финальные проверки ожидают общей заморозки исходников и не объявлены успешными.
