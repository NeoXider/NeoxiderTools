# Neo.Abilities — independent review, 2026-10-23

**Result: static review PASS after the correction pass. No open actionable finding in the reviewed diff. Compilation, executed tests and real Unity lifecycle checks remain pending with the central coordinator.**

## Scope and evidence

Reviewed `ca198556` against the primary worker's source-final working tree, then independently rechecked its same-session corrections. Read [the primary audit](./Neo.Abilities.md), repository `AGENTS.md`, all changed runtime source, both new regression files, affected documentation, and relevant resource, modifier, event and test-support contracts. This is a review of the delivered changes, rather than a second claim to have audited all 72 owned source files.

Final runtime scope is 13 files under `Assets/Neoxider/Scripts/Abilities/`:

- `Bridge/AbilityNoCodeAction.cs`
- `Components/AbilityAutoCaster.cs`, `AbilityCasterBehaviour.cs`, `AbilitySystemBehaviour.cs`, `AbilityUnitBehaviour.cs`
- `Domain/AbilitySystem.cs`, `Casting/AbilitySlot.cs`, `Combat/DamageService.cs`
- `Domain/Effects/EffectContext.cs`, `Effects/Ops/ChainEffectOperation.cs`
- `Domain/Modifiers/ModifierEngine.cs`, `ModifierInstance.cs`, `Units/AbilityUnit.cs`

Nested `Neo.Abilities.Editor`, its worker's tests, asmdefs, shared configuration, samples and package metadata were outside this review. QA wrote only this report. No Unity/MCP session, compilation, test process, build, commit or child agent was started.

The correction reuses the existing domain services, modifier instances, resource pools and test rigs. Public signatures, serialized fields, enum values and assembly ownership remain compatible. The additional shield transition flag is internal.

## Findings against the first handoff, now corrected

### ABR-01 / P2 — a NaN cost poisoned the new cumulative affordability check

**Location before correction:** `Domain/AbilitySystem.cs:368–395`, especially the preceding-entry sum at line 388. **Final correction:** `Domain/AbilitySystem.cs:372–400` and `666–680`.

**Reproduction:** give a caster 100 mana and an ability with costs `{ mana, float.NaN }`, then `{ MANA, 120 }`. The first cumulative patch added NaN into the second total, making `current < total` false. Payment skipped the NaN entry, clamped the finite 120 decrease to 100, then accepted the cast and started its cooldown. The original `ca198556` independent-entry check rejected the finite 120 entry, so this was a regression introduced by the first fix.

**Expected:** reject invalid effective costs before any resource payment, charge use, cooldown or cast receipt. A valid earlier cost in another resource must remain unpaid too.

**Reviewed correction:** nonfinite effective amounts are rejected before the nonpositive-cost skip; cumulative totals are checked; a nonfinite mana multiplier is rejected before flooring it to zero. Finite nonpositive costs retain their prior semantics. Tests `NaNResourceCost_CannotBypassLaterUnaffordableCost` and `NonFiniteEffectiveCost_RejectsBeforeAnyResourcePayment` begin at runtime-test lines 38 and 73. Their nine cases cover the reported bypass, NaN, both infinities, multiplication overflow and observable absence of partial payment.

**Status:** corrected; source and regression assertions independently reviewed, execution pending.

### ABR-02 / P2 — shield callbacks invalidated the current instance's exhaustion decision

**Location before correction:** `Domain/Combat/DamageService.cs:230–237`. These were pre-existing gaps in the shield callback path that remained after the first AB-08 fix, which only skipped a removed later shield in the scratch list.

Concrete reproductions and observable results:

- **Stack during absorption:** apply a Stack shield with capacity 10 and `PerStackValue = 10`, `MaxStacks = 2`. Its first `ShieldAbsorbed` callback adds stack two during a 10-damage hit. The old local capacity remained 10, so the now-capacity-20 instance was removed. Expected: that instance stays active with 10 capacity available, emits no break, and absorbs a subsequent 5-damage hit.
- **Nested exhaustion:** apply a 100-capacity shield. During an outer 10-damage absorption, a one-shot callback applies another 90 damage to the same unit. The nested hit broke and removed the shield; the outer hit then emitted a second `ShieldBroken`. Expected: two absorption receipts, one break and one removal, with health unchanged. Dispelling the current instance during its absorption similarly must not emit a later break for that removed instance.
- **Renew from a break:** reapply the same Refresh shield from `ShieldBroken`, either through a subscriber or the shield's own `apply_modifier` reaction. The old order refreshed the exhausted instance and then removed it. Expected: a different active instance survives, the old one is inactive, and the replacement absorbs a later hit. Removing before publishing the break would suppress the exhausted modifier's own declarative reaction, so that alternative would regress existing behavior.

**Reviewed correction:** `Domain/Combat/DamageService.cs:207–255` checks activity before consumption and after callbacks, then re-reads capacity. `Domain/Modifiers/ModifierInstance.cs:19` marks the old instance as breaking during its receipt; `Domain/Modifiers/ModifierEngine.cs:288` excludes it from refresh/stack reuse. The old instance remains available to its own reaction and is removed in `finally`; replacements survive and cannot be mistaken for it. The outer scratch-depth `finally` remains intact on exceptions.

Public regressions begin at runtime-test lines 422, 461, 482 and 513. They assert expanded capacity, self-dispel, single nested break/removal, replacement identity, later absorption, and both callback and own-reaction renewal.

**Status:** corrected; source and regression assertions independently reviewed, execution pending.

## Other reviewed contracts

- Charge restoration retains the effective interval, catches up elapsed intervals, preserves the remainder, refills zero-duration charges and normalizes against the duration that actually started.
- Bulk modifier removal uses a separate snapshot per nested call, excludes inactive instances and callback-created replacements, counts only its own removals and releases scratch state in `finally`. Foreign-engine removal cannot deactivate an instance.
- Unit ticking has a separate snapshot from area queries. Resource maximum deltas are recorded before synchronous notifications, allowing the tested nested bonus change to recover the same base maximum.
- Utility units without health are not killed by ineffective damage. Nested effect contexts and event reactions preserve the triggering cast id. Scene source teardown follows the actual subscribed level source; auto-cast targetability and effective range match the scoped contract.
- The payment fix validates affordability before payment. Payment still follows the documented `ResourcePoolModel.Decrease` policy: custom decrease caps and callbacks that mutate resources/slots/catalogs are outside an atomic transaction guarantee. That existing limitation and the separate projectile limitations remain recorded in the canonical README and primary audit.

The new tests exercise public results, resource values, instance identities, receipts and subsequent gameplay effects. Static inventory independently confirms **24 methods / 34 cases** in `Audit20261023AbilitiesRuntimeTests.cs` plus **3 methods / 3 cases** in `Audit20261023AbilitiesSceneTests.cs`: **27 methods / 37 cases total**, all unexecuted by QA.

## Required central verification

1. Import and compile the final integrated package and test assembly in supported Unity 6.0–6.4 and 6.5+ branches. `ProjectVersion.txt` currently declares `6000.3.14f1`; this file alone is not compiler evidence for either branch.
2. Execute the nonempty `Neo.Editor.Tests.Abilities` selection, including both new audit classes and the existing cast, cooldown, damage, modifier, reaction, auto-cast and projectile fixtures. Record actual selected/pass/fail counts and console diagnostics.
3. Run focused PlayMode checks for real registration/disable/re-enable, switching level source A to B across pooled activation, and auto-casting past an untargetable nearer unit and within a positive range bonus. The EditMode scene rig invokes lifecycle methods manually, so it does not replace those checks.
4. Run `Neo.Editor.Tests.ModulePrinciplesTests.PackageTextFiles_DoNotContainKnownMojibakeMarkers` with the integrated documentation changes.

QA independently ran scoped `git diff --check` successfully and strictly decoded 28 changed source/documentation/test/metadata/audit files as UTF-8 with no replacement characters. No compile, executed test, performance, cross-platform determinism or PlayMode pass is claimed.

## Reviewed correction fingerprints

SHA-256 values matched the primary worker's source-final handoff:

| File | SHA-256 |
|------|---------|
| `Domain/AbilitySystem.cs` | `CE418CBFAFB1F4630A12A928BFD11F2783590EFB49BDCA4488196D1DB60CA921` |
| `Domain/Combat/DamageService.cs` | `8B1D5B425B0AF643E7F8F023E51CA442F97FA1B8FF88BC69A3201D776397B43B` |
| `Domain/Modifiers/ModifierEngine.cs` | `6CD160DD68EE2A51043EB312C8F21E5F6475A545F60551792FA881A196FC3804` |
| `Domain/Modifiers/ModifierInstance.cs` | `7A3FEA6E38BBA744A65D4C48D331AEC67923C454B45CB9995978AE03FBC6ECCF` |
| `Audit20261023AbilitiesRuntimeTests.cs` | `B50165C261151A7D33CD36EDB1DE11E77905DC9CDE5FC364E4C87768AE8DC482` |
| `Audit20261023AbilitiesSceneTests.cs` | `971A50991EFC8569AB4D2481C5BB231EF1331E29876ED945159CD1142CE66AEE` |
