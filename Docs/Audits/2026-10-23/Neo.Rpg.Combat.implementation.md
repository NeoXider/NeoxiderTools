# Neo.Rpg.Combat — implementation receipt

Status: **SOURCE-FINAL for the Oct 25 S3 correction**, not native acceptance. The previous correction attempt's root-reported **exit 3** was incomplete; the older source-final receipt below is not completion evidence for this recovery. This same-session resume has completed the assigned source corrections. All current execution remains closed until every writer finishes and root declares a new global freeze.

- Current date: 2026-10-25; branch read as `demo`; target `11.0.0`.
- Current action: **RPGCOMBAT-NATIVE-LIFETIME**, retaining the completed **RPG-COMBAT-DOC-STATUS** work.
- Preserved prior edits and historical audit/plan. No new actor, source rebuild, test job, product checker, service, commit or push was launched.

## Exact S3 evidence read

Read the assigned case elements, messages, stacks and output from:

- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`, SHA-256 `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`.
- `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/21-full-play-graphics.xml`, SHA-256 `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`.
- `Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json`, groups E25, E26 and E27.

The Edit receipt contains 27 owned cases: 22 passed and the following five failed **in that old snapshot**. Those results are not a pass receipt for the corrected source.

| S3 cases | Observed failure | Source correction |
|---|---|---|
| `IgnoredTrigger_DoesNotNotifyOrConsumeRepeatHitAllowance(False)` and `(True)` | Native `ShouldRunBehaviour()` assertion at Edit-mode `GameObject.SendMessage`. | Replaced the two parameter cases with named 3D/2D Unity tests using top-level Play Mode admission and actual physics contacts; no manual trigger invocation. |
| `DisabledAttack_DoesNotDamageFromTriggerCallback` | Same native admission assertion at Edit-mode `SendMessage`. | Retained the case and disabled-hit oracle, with positive native contact controls before disable and after re-enable. |
| `Evade_CompletedEvent_ObservesDeferredCooldownBeforeRestartAttempt` | Completion count expected 1, observed 0 at old line 199. | Established native active/scheduled preconditions and captured/restored scaled-time control before interpreting the unchanged completion/re-entry assertions. |
| `ManagedHitbox_RestartExtendsWindow_AndDisableCancelsPendingEnd` | Expected disabled collider, observed enabled at old line 165. | Established native scheduling and elapsed scaled intervals before asserting window completion, then retained the subsequent disable/re-enable stale-completion checks. |

The last failure was the **normal end assertion before the disable branch**, as confirmed by reading the archived S3 source around line 165. It does not independently demonstrate a failed `OnDisable`. Both timing failures logged `[PM] Setting PageId: PageMenu`; the old tests did not establish an unpaused scaled clock. The scene log alone does not prove the cause. No new runtime defect is inferred before the corrected preconditions are executed.

The Play receipt has **zero cases from these three owned Combat fixtures**. The separate existing RPG Play fixtures appearing there are outside this correction's ownership; their results do not validate these changed Edit fixtures.

## Completed correction and preserved oracles

Changed only the two unreserved prefixed test files and this receipt in this recovery:

1. `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgCombatHitFilterTests.cs` now has top-level `UnitySetUp`/`UnityTearDown` with direct `EnterPlayMode`/`ExitPlayMode`. Startup is allowed to settle without resetting a scene/provider. The fixture captures the actual time scale, temporarily sets an explicit interval control, proves fixed-step advancement, and restores the captured value during cleanup. It destroys only its two created actors and only exits the Play Mode transition it acquired.
2. Its 2D/3D source actors use real trigger colliders and rigidbodies. Native contact first proves target HP 10→7, exactly one hit and one trigger event, exact target GameObject/collider payloads, and unchanged owner HP 10. The ignored-contact phase retains HP 10 and zero notifications; clearing the ignore entry within the same swing permits one hit; a further leave/re-enter is suppressed. The disabled case retains HP 10/zero events and adds normal positive controls before disable and after re-enable. `SendMessage`, reflection invocation and manual physics/lifecycle simulation are absent.
3. `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgCombatLifecycleTests.cs` now uses one top-level native setup/teardown per case. It captures/restores the actual time scale, verifies Play Mode and scaled-clock advancement, and observes the existing intervals against `Time.timeAsDouble` with bounded real-time deadlines. Existing configured action/trigger/cooldown durations and numeric tolerances were not enlarged or changed. Native scheduling is observed through the public `IsInvoking()` API, without invoking a private handler.
4. The managed-window case retains the restarted-open window/count-zero assertion, normal disabled/count-one end, and disabled/re-enabled closed/count-one stale-completion suppression. The deferred-evade case retains exactly one completion, rejected restart, and active cooldown, additionally capturing cooldown state **inside the completion listener before re-entry**. All twelve lifecycle cases, including both cooldown-start cancellation cases and their ordinary-cycle controls, remain present.

Case mapping for later discovery: the old ignored `(False)` case maps to `IgnoredTrigger3D_DoesNotNotifyOrConsumeRepeatHitAllowance`; `(True)` maps to `IgnoredTrigger2D_DoesNotNotifyOrConsumeRepeatHitAllowance`. The disabled case keeps its name. Counts remain **27 cases: Health 12, HitFilter 3, Lifecycle 12**. No substantive case/oracle was dropped, no ignore/whitelist was introduced, and the unsupported NUnit parallelism attribute was not added. Enter/Exit instructions occur directly in the top-level setup/teardown methods, never in a nested scenario enumerator.

No production source was changed: the current evidence requires fixture/native-admission correction first. Public and serialized APIs, production defaults/settings, assets, sample layout, GUIDs and existing meta files remain unchanged. The canonical README's previous source-fix/runtime-pending wording remains accurate; no runtime contract or migration change required another public-doc edit.

## Reservation boundary and current artifacts

Read `Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json` before every edit; its status was `source-final`. Both prefixed files and this receipt were unreserved. No move/extraction was performed. `RpgCombatEdgeTests.cs`, relocated runtime helper files under `Assets/Neoxider/Tests/Fixtures/`, shared asmdefs/references, and the primary's boundary regression were untouched. No reserved work is needed to complete these five assigned cases; legacy/helper acceptance remains with the primary owner.

Current SHA-256 values (artifact reads only):

| Exact path | SHA-256 |
|---|---|
| `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgCombatHitFilterTests.cs` | `e4033430037e305a3d189b1b9607489724d3f4383d532aac474829c7c6b33d36` |
| `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgCombatLifecycleTests.cs` | `53c68d171b3bccda404a4b031de85ab59984aaa7c04c95c868ab10ed502ef94b` |
| `Assets/Neoxider/Docs/Rpg/Combat/README.md` (unchanged) | `cc9adb19844835654dd231861dfb848932f717fcfcd8596ea4e21b9fff2eed2e` |

Existing fixture GUIDs are retained: HitFilter `4d1ebea591f4704b31f061ad95a8b091`; Lifecycle `0b9b544a7ea344e53239246f60ffa618`. All five runtime hashes still match the approved values in the historical table below. Work performed in this recovery was limited to source/evidence reads, artifact bookkeeping and the declared edits; no compiler or validator was used to infer source correctness.

## Current unresolved native evidence

All corrected cases are **UNEXECUTED**. After all writers stop and a new global freeze is recorded, root must obtain current Unity 6.3/6.6 compilation, discovery/count mapping and native results for the three HitFilter cases and twelve lifecycle cases, plus preserved Health cases. Verify fixed-step and scaled-time admission, actual 2D/3D contact/payloads, restored prior time scale, exactly-owned actor cleanup and top-level exit. A failed admission/clock/positive-contact control remains a native capability/precondition failure, not evidence to weaken the negative oracle or patch runtime defaults. If callbacks still fail after the positive controls pass, retain that exact evidence and return only the bounded runtime issue to this owner. Force direction/effect lifetime and separate network integration still have no new evidence here.

The old exit-3 attempt and the five S3 failures remain recorded, without relabelling them as passes. No API/resource failure recurred during this recovery's completed source operations. Source writes stop with this receipt; native acceptance and final root push remain behind the global barrier.

---

## Historical 2026-10-24 source receipt — superseded for S3

The following preserved receipt describes the earlier source-only snapshot, including its former fixture implementation and hashes. Its statements about unexecuted cases, time-scale handling and `SendMessage` describe that historical phase, not the corrected source above. Its earlier source-final status is not the Oct 25 recovery completion or a current verification pass.

- Date: 2026-10-24.
- Target: `demo`, package `11.0.0`.
- Approved action: **RPG-COMBAT-DOC-STATUS** (native review C-33).
- Same original Combat audit/plan session; no child actors launched.
- Read the complete implementation contract and request, retained the historical audit/plan context, and re-read the relevant current Evade, Timer, fixture and documentation source.
- Read-only state inspection confirmed `globalReviewApproved: true` and the current branch `demo`. Shared trackers were not changed.

## Completed source and documentation work

| Exact file | Completed change |
|---|---|
| `Assets/Neoxider/Docs/Rpg/Combat/README.md` | Replaced the obsolete claim that looping Timer completion is still broken. The guide now states that looping completion and start-listener cancellation are repaired in source, while runtime integration remains unverified until central Unity execution. Documented cooldown-start reset/disable, their different action-lifetime effects, and absence of cancelled cooldown completion/progress output. Existing C# and scene/UnityEvent paths are retained. |
| `Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgCombatLifecycleTests.cs` | Added two focused Unity regression sources and a shared private scenario helper. Updated the existing auto-heal comment to describe positive integration coverage rather than a currently broken shared timer. No old assertion, case or cleanup was removed. |
| `Docs/Audits/2026-10-23/Neo.Rpg.Combat.implementation.md` | This source-final receipt; no historical audit or plan was rewritten. |

The canonical backlinks are already present in the current parent-owned entries:

- `Assets/Neoxider/Docs/Rpg/README.md`: `[Combat compatibility](./Combat/README.md)`.
- `Assets/Neoxider/Docs/Tools/Components/AttackSystem/README.md`: `[RPG / Combat compatibility](../../../Rpg/Combat/README.md)`.

They were inspected and preserved, without concurrent edits to either parent's documentation. Existing URLs remain available. Folder metadata and package metadata remain coordinator-owned and were not changed.

## Authored acceptance — UNEXECUTED

New cases in the existing EditMode fixture:

1. `Evade_ResetFromCooldownStarted_CancelsCooldownWithoutDelayedUpdates`.
2. `Evade_DisabledFromCooldownStarted_CancelsCooldownWithoutDelayedUpdates`.

Both enter Play Mode through the existing `EnterPlayMode` source pattern, create their own actor, and execute a real `Evade.TryStartEvade()` timer-start dispatch. The listener either calls `ResetCooldown()` or disables the actor. The assertions require one actual start dispatch, the appropriate action state, no active cooldown, zero remaining time, ready progress, and no cooldown/action completion caused by interruption.

An unscaled real-time observation window extends beyond the cancelled cooldown's original duration. Reactive listeners count changes after cancellation, and the scenario asserts public outputs on successive frames. This detects stale timer updates even if they would eventually return to zero/one. The disable case also re-enables the actor before observation, covering pooled reuse.

Each scenario then removes its cancellation listener, interrupts any preserved action through disable/re-enable, and starts an ordinary cycle. It must deliver exactly one action completion and one cooldown completion, report two total start dispatches, and return to ready. This control prevents a permanently inactive timer from satisfying the cancellation assertions.

The existing `UnityTearDown` still destroys only the fixture-owned actor and exits Play Mode, including after an assertion failure. The new scenarios use bounded real-time deadlines, do not change time scale, create no persistent/static state, add no skips, and introduce no unsupported NUnit attributes. Shared-state execution relies on the coordinator's isolated/exclusive runner contract.

The original 25 authored Combat cases are preserved; two were added, for **27 authored cases** across the same three regression files: Health 12, hit filters 3, lifecycle 12. None were executed in this phase. The existing auto-heal case remains a positive HP-change/disable/re-enable assertion, without an expected-failure or ignore label.

## Dependencies and compatibility

The current `Assets/Neoxider/Scripts/Tools/Time/Timer.cs` contains the required implementation: cancellation is established before `OnTimerStart`, cancellation is checked after that event, and completed looping cycles publish `OnTimerEnd`. Combat consumes this existing `Neo.Tools.Time` behavior; no Time-owned code, tests or public API was changed here. There is **no pending additive API dependency** for this assigned action.

No new Combat runtime API, serialized field, assembly reference, GUID, scene, prefab or sample layout change was introduced. The five coherent phase 1 runtime edits remain byte-identical to the approved native review fingerprints:

| File under `Assets/Neoxider/Scripts/Rpg/Combat/` | SHA-256 prefix |
|---|---|
| `AdvancedAttackCollider.cs` | `c8144fc7d3c1ae1e` |
| `AttackExecution.cs` | `dc84d073974d5c71` |
| `Evade.cs` | `06725944900d4f49` |
| `Health.cs` | `ecf91483c1a9c0ae` |
| `RpgStatsDamageableBridge.cs` | `c1ac4bc054004b5c` |

Those earlier fixes retain the accepted legacy namespaces, fields, event routes and per-collider hit suppression; they are not silently replaced with Abilities. This assigned correction adds no further compatibility migration. Typed receiver injection and other optional improvements remain proposals in the historical plan and are **not implemented** by this receipt.

## Actual inspection and artifact bookkeeping

- Inspected the implementation contract/request, approval flag, branch, exact manifest ownership, current public README, complete current lifecycle fixture and Evade source, relevant Timer dispatch/cancellation body, and existing parent backlinks.
- Computed SHA-256 fingerprints with `certutil`; the five runtime fingerprints above match the approved source. Final edited artifact hashes:
  - Combat README: `cc9adb19844835654dd231861dfb848932f717fcfcd8596ea4e21b9fff2eed2e`.
  - Lifecycle fixture: `b3732a99dcca5ca957cb7d087ef458c31fefab1cf832a4d2080b0c8419b8b1d8`.
- Source-only `git diff --no-index --check NUL <file>` inspection of both edited, currently untracked assets produced no whitespace-error diagnostics. Git returned 1 for the new-file differences and warned about its existing LF-to-CRLF working-copy policy; no configuration or line-ending migration was performed.

These are source reads/diffs and artifact bookkeeping, not product validation or runtime acceptance. No Unity, compiler, test runner, product checker/validator, installer, service, commit or push was launched. No claim about an installed Unity/NUnit pass is made.

## Remaining evidence debt

After **every** writer finishes, the coordinator's verification operator must compile and discover nonempty tests on the frozen source using the installed Unity 6.3 and 6.6 environments, including the required MIRROR-on/off dependency paths. Execute the two new cancellation scenarios, the existing auto-heal lifecycle scenario, and the preserved Combat regressions; keep exact version, source snapshot and result evidence. Other unsupported-version branches remain unverified unless actually compiled.

Physics engine contact/knockback, effect-prefab lifetime, wider network authority integration and arbitrary public-field timing inputs remain the integration risks already distinguished in the audit/plan. The callback filter tests still deliberately use `SendMessage` and do not claim to prove physical contact. The public guide's integration status must be updated with actual central outcomes afterward, without rewriting historical verification receipts into current passes.
