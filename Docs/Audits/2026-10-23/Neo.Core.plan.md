# Neo.Core — individual implementation plan

Phase 2, same auditor/session; `gpt-6.1-sol`, max. Baseline `ca198556`, package 10.19.0. Planning only.

Path shorthand: `C/` = `Assets/Neoxider/Scripts/Core/`; `T/` = `Assets/Neoxider/Tests/Edit/Core/`;
`D/` = `Assets/Neoxider/Docs/Core/`. Each filename below resolves against that exact root.

## Current outcome

The completed audit reviewed all 16 inventory files. Implemented fixes cover XP saturation, full-range
level search, threshold precision/normalization, mode synchronization, complete resource payments,
timer catch-up, pool replacement/reentrant ticking, operation-local healing results, invalid inputs,
uncapped persistence, and independent cloning. Seven source files and two existing public guides changed.
`D/README.md` now explains C# models and optional scene/NoCode adapters; `D/LevelCurveDefinition.md`
documents evaluator limits. Public signatures, serialized fields, and enum values were preserved.

The three `T/Audit20261023NeoCore{Level,Resource,Profile}Tests.cs` files contain 27 methods / 34 cases.
Inventory, UTF-8, signatures/serialization, lexical structure, links, GUIDs, and scoped diff checks passed.
These are source/static results: Unity compilation and test execution remain unverified.

## Prioritized remaining work

**P1-D1 — confirmed source-level callback defect, not fixed in phase 1.** In
`C/Resources/Domain/ResourcePoolModel.cs`, `Decrease` and `TrySpend` inspect depletion after
`NotifyChanged`, so they can attribute a nested operation's transition to the outer operation too.
Reproduction: HP 10, unlimited decrease; an `OnResourceChanged` listener receiving 9 calls
`Decrease("HP", 9)` once. Calling outer `Decrease("HP", 1)` produces two depleted notifications for
one transition to zero. The same occurs with outer `TrySpend(..., 1, ...)`. This is traced from current
source, not executed. `HealthComponent.HandleResourceDepleted` forwards each notification to `OnDeath`.

Owner: Neo.Core. Smallest correction: capture the operation's positive-to-zero transition immediately
after subtraction/clamping, before callbacks; keep existing Changed-then-Depleted ordering. Add
`T/Audit20261023NeoCoreResourceLifecycleTests.cs` plus unique metadata. Acceptance: both outer-operation
variants leave current zero and emit exactly one depleted event; the outer decrease returns 1 and
the spend succeeds. Further decreases at zero emit none. Estimate: half a day, low implementation risk;
event semantics require focused regression review.

**P1-V1 — verification debt, coordinator-owned.** After source-changing audits finish, compile and run
the 34 existing new cases plus `T/ResourcePoolModelTests.cs` and
`Assets/Neoxider/Tests/Edit/Level/LevelCurveDefinitionTests.cs`. Required observed results include:
XP ceiling reports only 5 stored XP; billion-step level 3 requires 2,000,000,000 XP; cost 10 against
limit 4 fails without mutation/events; scheduled catch-up reaches 25 then 30; uncapped saved 25 stays 25.
Add a lifecycle regression where a listener throws during Tick, then a later Tick advances state,
proving the guard/snapshot cleanup. Compilation or behavior failures become concrete corrections in
their owning assembly; static success is insufficient.

**P2 risks, not additional confirmed defects:** external entry mutation bypasses model invariants;
custom curve implementations must remain monotonic; Int32 XP has a deliberate ceiling. Wider XP and
thread safety are outside this implementation. Event-listener exceptions currently propagate.

## P2 proposal: owned resource configuration and committed changes

Proposal, unimplemented. Developers share mutable
`ResourcePoolEntry` state unintentionally and cannot retain an immutable description of an operation
while callbacks mutate the live pool.

Reuse `ResourcePoolModel`, its arithmetic/timers, `ResourcePoolEntry`, `IResourcePoolProvider`, profile
DTOs, and BCL collections/Action. No parallel resource engine, package dependency, or new assembly.
The new registration path copies immutable settings into an internal entry with no exposed mutable
reference; legacy registration retains its existing shared-reference behavior.

Proposed additive concrete-model API:

```csharp
bool TryAddPool(string id, ResourcePoolSettings settings, float initialCurrent);
bool TryUpdateSettings(string id, ResourcePoolSettings settings);
bool TryGetSnapshot(string id, out ResourcePoolSnapshot snapshot);
ResourceSpendResult Spend(string id, float amount);
event Action<ResourceChange> OnResourceCommitted;
```

New registration rejects duplicate ids and invalid/non-finite settings/current without mutation.
Max zero remains uncapped; negative operation limits remain unlimited. Snapshots are immutable.
`ResourceSpendResult` includes success, actual amount, and a typed failure reason; existing string-based
TrySpend delegates without changing its signature or established reasons. Do not add required members
to `IResourcePoolProvider`, which third-party components may implement.

Capture change records before callbacks: id, previous/current/max, actual delta, cause. Queue the new
typed notifications in commit order; callback-created records follow their originating record.
Preserve legacy synchronous event order. Keep exception propagation explicit and clean dispatch guards
in finally; do not silently introduce swallowing or a global event service.

Exact Core implementation files:

- Modify `C/Resources/Domain/ResourcePoolModel.cs`.
- Add under `C/Resources/Domain/`: `ResourcePoolSettings.cs`, `ResourcePoolSnapshot.cs`, `ResourceSpendResult.cs`,
  `ResourceSpendFailureReason.cs`, `ResourceChange.cs`, and `ResourceChangeCause.cs`.
- Add `T/Audit20261023NeoCoreOwnedResourcesTests.cs` and
  `T/Audit20261023NeoCoreResourceChangeTests.cs`; every new package file needs unique metadata.
- Update `D/README.md`; existing `ResourcePoolEntry`, provider interfaces, and profile layouts remain compatible.

Minimum proposal tests: two models using identical settings remain independent; source settings cannot
change registered current; invalid updates preserve the previous snapshot; failed costs emit no committed
change; nested spending produces immutable ordered records; replacements inherit no old timers; existing
34 cases and the legacy alias behavior still hold. Estimate: 4–6 developer days including adapters;
medium risk around event ordering and compatibility.

## Dependencies, sequence, and release acceptance

1. Implement P1-D1 and its public-behavior regressions first; add the exception-cleanup test. Run the
   focused Core checks centrally after the concurrent source wave ends, before optional API work.
2. Implement settings/snapshots, then typed spending, then committed records. Validate each contract
   before changing wrappers. Keep all calculations Unity-free and explicitly typed.
3. Neo.Core.Resources.Components owns `C/Resources/Components/HealthComponent.cs` and
   `ResourceEntryInspector.cs`: adapt settings creation/snapshot reads only after Core stabilizes,
   preserving inspector fields and UnityEvents. Its owner supplies wrapper/OnDeath integration coverage
   and updates `D/Resources/Components/HealthComponent.md`. Scene behavior remains opt-in and typed.
4. Neo.Core.Level.Components restoration already applies the incoming cap first; HealthComponent
   already reports the accepted model maximum. Verify these existing other-worker fixes rather than
   reimplementing them. Neo.Core.Level.Data and Neo.Core.Level.Bridge retain their interfaces and
   asset/NoCode workflows; no adapter redesign is required. Do not migrate Neo.Rpg's separate resource
   implementation as part of this proposal.
5. Finish programmer examples and scene migration notes, then coordinator-owned packaging validation:
   preserve DTO versions, asset GUIDs, serialized names, and active `Samples~` layout. No sample rename
   or new authoring asset is needed; imported samples remain separate project copies.

Acceptance requires focused EditMode passes and component-owner PlayMode checks for real Update-driven
regen, death events, save/load, and disable/enable lifecycle. Compile the package under a supported
Unity 6.0–6.4 version and 6.5+; record versions and untested targets. Existing identity compatibility
stays centralized in `NeoDiagnostics.StableId` / `NeoComponentHealth.StableId`; add no local version gates.
Stop the affected rollout on compile errors, duplicate death events, changed saves/serialized fields,
or incompatible legacy aliases/order. Leave the optional API proposal deferred if its compatibility
contract is unresolved; do not label it implemented or any unexecuted verification passed.
