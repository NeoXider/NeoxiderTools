# Neo.NPC: audit

Status: source, regressions, and documentation completed; static validation passed. Unity compilation
and NUnit execution are centrally pending, not passed by this worker.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/NPC/Neo.NPC.asmdef`.
- Scope: 13 C# files; nested assemblies excluded.

## Scope and complete source review

Read the complete repository `AGENTS.md`, worker contract, exact `Neo.NPC` manifest entry, initial
report, and both relevant Unity/Neoxider skill instructions. Read the NPC reference section and
module/helper reference matches before implementing. No new runtime subsystem or generic helper
was introduced. Existing navigation cores, target selector, attack controller, and diagnostics were reused.

All **13/13 owned C# files were read in full**, including method bodies. The inventory exactly matches
the manifest and current NPC directory. Paths below are relative to `Assets/Neoxider/Scripts/NPC/`.

| File | Review and outcome |
|------|--------------------|
| `Combat/NpcCombatDecisionCore.cs` | Checked decision precedence, absent targets, inclusive attack range, disabled lose distance, and blocked actions. Existing eight decision tests remain untouched; no demonstrated defect for finite distance inputs. |
| `Combat/NpcCombatPreset.cs` | Reviewed all asset fields/getters, ID fallback and validation. Contains asset/configuration data only; no scene references or runtime resource ownership. Unchanged. |
| `Combat/NpcRpgCombatBrain.cs` | Reviewed reference resolution, scheduling, acquisition, attack failure, actor state, navigation takeover/restore, events and debug logs. Fixed missing-controller failure, destroyed-reference repair, incomplete restoration, duplicate empty acquisition, and edit-time action buttons. |
| `Navigation/NpcAggroFollowCore.cs` | Reviewed squared thresholds, hysteresis events, reset, callback and transform ownership. Unchanged; caller must supply a live transform and compatible finite thresholds. |
| `Navigation/NpcAnimationCore.cs` | Reviewed parameter hashes, speed normalization, damping and unbound/disabled agents. Fixed ignored time step and native stop access off the NavMesh. |
| `Navigation/NpcDestinationResolver.cs` | Reviewed direct sample, all six rings/ten candidates, area mask and bounded search. Unchanged; agent-type filtering, connectivity and strict containment are explicit limitations below. |
| `Navigation/NpcFollowTargetCore.cs` | Reviewed target/explicit destination transitions, disabling, stop/resume, replan timing, agent acceptance, path status and arrival events. Fixed persistent stop, first/unchanged destination planning, ignored acceptance result, and partial-path arrival. |
| `Navigation/NpcNavAgentCore.cs` | Reviewed settings clamps, agent application, running state and speed override. No allocations/resources needing disposal; unchanged. |
| `Navigation/NpcPatrolCore.cs` | Reviewed empty/null routes, start/stop/wait, looping and non-looping indices, bounded waypoint/zone attempts, transformed boxes and agent acceptance. Fixed failed-start state/events, stale movement, ignored acceptance result and partial arrival. |
| `NpcAnimatorDriver.cs` | Reviewed required agent, local Animator fallback, initialization and null-safe ticking. Serialized names/defaults retained; no independent resources or demonstrated wrapper defect. Unchanged. |
| `NpcNavigation.Behaviours.cs` | Reviewed every enter/exit/tick path. Fixed duplicated Combined state, missing/different targets and initial chase when automatic replanning is disabled. |
| `NpcNavigation.cs` | Reviewed all serialized/public fields, Awake/enable/update/disable, modes, movement commands, constraints, rotation, events, gizmos and validation. Fixed pre-Awake calls, disabled/off-mesh stop ownership, pause preservation and stop events; replaced the existing implicit local type. |
| `NpcTargetFinder.cs` | Reviewed override precedence, Awake order, retry clock, destroyed targets, tag/name fallbacks and gated warnings. Fixed bound override replacement and undefined-tag fallback; replaced two existing implicit local types. |

Read-only dependency context included the RPG selector/attack controller/preset, targeting query and
utility, relevant character lifecycle/state members, existing NPC tests and both test asmdefs. No
nested assembly, dependency source, existing tests, shared configuration, prefab or scene was edited.

## Findings, evidence and completed fixes

These are **source-demonstrated control-flow defects**. The reproduction scenarios below are encoded
in new NUnit regressions where practical; this worker did not execute Unity reproductions.

### NPC-01 — P1: target binding could fail solely because of Awake order

Source: `NpcNavigation.SetFollowTarget`, `SetMode`, `SetRunning`; `NpcTargetFinder.Awake`.
The finder can call `SetFollowTarget` before navigation initializes `followCore`, causing a null
dereference. Public mode/speed calls had the same assumption. Target and mode now defer initialization;
running configuration initializes/reuses the agent settings core. A destination request before
initialization returns false. Regression: `Navigation_PreAwakeConfiguration_PreservesTargetModeAndRunningSpeed`.

### NPC-02 — P2: navigation stop ownership and movement events were incomplete

Source: `NpcNavigation.OnDisable`, `Stop`, `Resume`, `Update`, `SetModeInternal`.
Disabling only the navigation script left the enabled agent moving. Calls against an unbound agent
accessed native `isStopped`; changing to Patrol while paused restarted movement. Explicit stopping
returned before movement-stop events could be emitted. Native calls now require an enabled/bound
agent, disable stops it, mode entry preserves pause, and stopping updates movement transitions once.
Mode entry is deferred while the component is disabled. Regressions cover off-mesh/disabled agents,
disable/enable, paused mode switch and one stop event.

### NPC-03 — P2: direct follow-core Stop was undone by the next Tick

Source: `NpcFollowTargetCore.Stop`, `Resume`, `Tick`, `HandleTriggerDistance`.
With a zero trigger distance, every tick cleared `agent.isStopped`, undoing public `Stop`. The core
now retains an explicit stop flag until `Resume`; native calls are guarded. Regression:
`Follow_StopSurvivesTicks_UntilResume` observes real agent state.

### NPC-04 — P2: stationary, origin and replacement destinations could never receive a path

Source: `NpcFollowTargetCore.SetTarget`, `SetDestination`, `Tick`.
Planning depended only on distance from the default/last desired position. World origin matched the
default cache, same-position replacements were skipped, and `ResetPath` never repaired a stationary
request. New requests mark the path dirty and allow their first tick immediately; automatic updates
also repair a missing non-pending path. Three regressions observe emitted accepted-path events and
the destination, rather than inspecting the cache.

### NPC-05 — P2: partial paths incorrectly reported successful arrival

Source: `NpcFollowTargetCore.CheckDestinationReached`, `NpcPatrolCore.Tick`.
Remaining distance measures the partial endpoint too; the old code emitted arrival/waypoint events
when that endpoint was within stopping distance. Arrival now requires `PathComplete`, and status
inspection waits until calculation finishes. Two regressions construct disconnected NavMesh islands,
confirm a partial path, and assert blocked/not-reached outputs. Patrol recovery from a partial path
is still caller-directed; this change prevents false success rather than implementing a new policy.

### NPC-06 — P2: rejected agent requests were reported as successful

Source: `NpcFollowTargetCore.UpdatePath`, `NpcPatrolCore.TrySetDestination`.
Both ignored `NavMeshAgent.SetDestination`'s boolean result. They now propagate rejection; the follow
core emits `DestinationUnreachable` instead of `PathUpdated` on failure. Native rejection after a
successful sample needs central runtime validation; injecting that engine result was not practical
without adding a planner seam. Successful acceptance is exercised by the new NavMesh regressions.

### NPC-07 — P2: failed patrol selection retained a fictitious active patrol or stale movement

Source: `NpcPatrolCore.StartPatrol`, `MoveNext`.
Looping null/unreachable routes and exhausted zone attempts ignored selection failure, retained
`IsPatrolling`, or left the old chase path running. Non-looping failure could emit Completed then
Started. Empty routes and exhausted selection now stop; a failed start does not emit Started.
Regressions cover both loop flags with missing waypoints, five failed zone attempts, and stopping
the old chase after target loss with no patrol route.

### NPC-08 — P2: Combined chase state could freeze or follow the wrong target

Source: `CombinedBehaviour.Tick`, `NpcNavigation.OnFollowDestinationUnreachable`, `SetCombinedTarget`.
Unreachable handling cleared only the host flag; a second private `isFollowing` stayed true and
prevented re-entry while the follow core was disabled. Destroyed targets never exited that state,
and replacement targets updated only the host field. One host state now drives both paths, missing
targets exit to patrol, and replacement targets update the follow core. Combined entry also submits
its first path explicitly when automatic updates are off. Four regressions cover retry, destruction,
replacement and manual update mode.

### NPC-09 — P2: target finder ignored runtime override replacement and aborted name fallback

Source: `NpcTargetFinder.TargetOverride`, `Update`, `FindAndSetTarget`.
`Update` returned forever while the old target lived, even after assigning a new override. The setter
now invalidates that binding and retry time. Undefined project tags threw before the configured name
fallback; the lookup now catches that Unity exception and continues. Existing opt-in/throttled
diagnostics are retained. Two regressions assert the actual navigation target after these operations.

### NPC-10 — P2: combat restoration lost the original follow target or left navigation stopped

Source: `NpcRpgCombatBrain.HoldPosition`, `TryAttackCurrentTarget`, `CaptureNavigationModeIfNeeded`,
`RestoreNavigationModeIfNeeded`.
Only chasing captured state, so an immediately in-range hold stopped navigation permanently after
clearing. Chasing overwrote the follow target, and restoration always resumed even an originally
paused NPC. Capture now precedes either chase or stop and includes mode, follow target and activity.
Restoration consumes the capture independently of a subsequently replaced preset. Three regression
cases cover immediate hold and chase restoration for both prior activity states. Run/speed and exact
patrol progress remain outside this snapshot and are documented.

### NPC-11 — P2: ForceAttack and reference repair failed for missing/destroyed dependencies

Source: `NpcRpgCombatBrain.TryAttackCurrentTarget`, `AutoResolveReferences`.
A configured target-free preset reached a null attack controller through public `ForceAttack`.
It now returns false through the existing failure event. Null-coalescing assignment also treated a
destroyed Unity selector as present, preventing repair after replacement; reference resolution now
uses Unity null checks. Regressions assert the failure result and real target acquisition through a
replacement selector.

### NPC-12 — P2/P3: animation ignored the caller's clock and queried unbound agents

Source: `NpcAnimationCore.Tick`.
The `deltaTime` parameter was unused: SmoothDamp consumed the global frame time instead. Smoothing
now receives the supplied nonnegative step; agent speed/stop reads require a bound agent. Two cases
check zero-step preservation and a positive-step speed decay through real Animator parameters.

### NPC-13 — P3: runtime combat actions were available as edit-time Inspector buttons

Source: the four action `[Button]` attributes on `NpcRpgCombatBrain`.
`ForceAttack`/evaluation can execute attacks or mutate scene navigation/targets while editing.
These buttons now require Play Mode; `AutoResolveReferences` remains an authoring button. Public
method signatures are unchanged. Attribute inspection is static; actual Inspector gating remains
part of central verification.

### NPC-14 — P3: empty automatic acquisition scanned twice per evaluation

Source: `NpcRpgCombatBrain.EvaluateBrain`.
The initial acquisition ran before Decide, then the AcquireTarget switch arm repeated the same
search if nothing was found. Removed the duplicate call. Verified the control flow statically;
no performance claim or engine-query-count test is made.

## Documentation

Updated seven existing English public pages under `Assets/Neoxider/Docs/NPC/`:

- `README.md`: canonical C# entry point, scene/event workflow, lifecycle and legacy-combat distinction.
- `InternalTypes.md`: direct core API, time/lifetime ownership, path acceptance/arrival/failure contracts.
- `Navigation/NPCNavigation.md`: pre-Awake calls, safe stop/disable, first-path/retry, Combined targets,
  patrol failure, partial paths and actual bounds/sampling limitations.
- `NpcTargetFinder.md`: bound override replacement, clearing, undefined tags, C# and Awake order.
- `NpcAnimatorDriver.md`: supplied clock, unbound agent, ratio semantics and direct C# usage.
- `Combat/NpcRpgCombatBrain.md`: dependency failures/repair, Play Mode actions and snapshot restoration.
- `Combat/NpcCombatScenarios.md`: immediate-hold recovery and restoration limits in scene recipes.

`NpcCombatPreset.md` was reviewed and left unchanged. No separate guide or DocsEn tree was created.

## Substantial improvement proposal

### Proposal A — typed, agent-aware destination planning and bounded recovery (not implemented)

**Problem.** Sampling currently considers only an area mask. It can select another agent type's
surface or an unreachable island, extend outside the nominal radius/bounds, and consume up to 61
sample queries per miss. Combined retries can repeat on each tick. Partial patrol paths have no
defined retry/skip policy. These limitations should be addressed together instead of adding hidden
special cases to each behaviour.

**Architecture/API.** Introduce a small `INpcPathPlanner.TryPlan(NpcNavigationRequest request,
out NpcNavigationPlan plan)` boundary. The request carries desired position, agent type, area mask,
sample radius, optional destination constraint and complete-path policy. The result exposes sampled
position, path status, acceptance/failure reason and retry eligibility. A Unity adapter performs
agent-filtered sampling and connectivity checks. Follow/patrol state machines consume that typed
result and caller-provided time, using an explicit retry interval/backoff and transition-only failure
events. Reuse the current bounded sampler internally. Keep destination clamping separate from any
future strict path/corridor containment contract.

**Compatibility/migration.** Retain existing static resolver and core methods as wrappers with legacy
defaults. Add opt-in complete-path/retry settings without renaming serialized fields. Existing scenes
retain their current sampling policy; new C# callers can inject a planner. Do not silently turn
`onPathUpdated` into a complete-route guarantee. No package or asmdef migration is needed for a
first implementation confined to NPC.

**Acceptance scenarios.** Overlapping surfaces for two agent types choose the requested type;
disconnected islands produce Partial/Blocked without arrival; missing NavMesh later becomes available
and recovers after the configured interval; hundreds of stationary failures obey the query budget;
destroyed/replaced targets reset requests without retaining stale state; invalid numeric inputs are
rejected before native calls; non-Unity planner fakes exercise timing/state deterministically.

**Effort/risk.** Approximately 3–5 engineering days, including focused core tests and a live NavMesh
matrix. Medium compatibility risk around event frequency and legacy partial-path behaviour; mitigate
with opt-in defaults and explicit before/after scenario tests. Estimates assume existing agent types
and Unity versions remain supported; no new dependency is proposed.

### Proposal B — explicit navigation takeover snapshot (not implemented)

**Problem.** The repaired brain snapshot still cannot restore running/custom speed, exact patrol
progress or an explicit destination, and competing controllers can overwrite one another. Built-in
attack input is disabled in Awake without a scoped restoration contract.

**Architecture/API.** Add `NpcNavigation.CaptureState()` returning a typed `NpcNavigationState` and
`RestoreState(state)` for mode, targets/destination, activity, speed/running and patrol cursor. If
multiple controllers need ownership, add an optional token identifying the current takeover and
restore only while that token still owns navigation. Keep that mechanism local to the component;
no global manager is needed. A brain can similarly remember/restore the attack controller input
value only while it owns that override.

**Compatibility/migration.** Keep existing brain preset flags and methods as adapters over the new
snapshot; default to current behaviour until the fuller restoration contract is explicitly enabled.
Do not store scene snapshots in ScriptableObjects. No serialized renames or existing scene edits.

**Acceptance scenarios.** Patrol/chase/hold/clear and disable/re-enable restore a paused or running NPC,
custom speed, prior target/destination and waypoint; an external mode change made during combat is
not overwritten by a stale token; nested takeovers and target destruction have deterministic outcomes.

**Effort/risk.** Approximately 2–3 engineering days after Proposal A, with medium reentrancy/lifecycle
risk. Test callback-driven clear/disable and competing controllers before changing defaults.

## Verification and remaining risks

### Actual checks

- Read and reviewed all 13 owned C# files, relevant public docs and dependency/test context.
- Python static comparison against `ca198556`: **71 serialized field declarations and 48 public method
  signatures preserved** across all 13 files; manifest inventory matches disk exactly.
- Static UTF-8 decoding, no replacement characters, trailing whitespace/final newline checks, C#
  delimiter balance and explicit-local-type scan passed for owned source/new tests and NPC docs.
- All eight NPC Markdown pages have existing local link targets.
- New test metadata GUIDs occur once each in an ignore-independent `.meta` search under Assets:
  `b3fc084ffd6d46208591457a74b9a070`, `3182baea9a684c9ca80f830288960fb0`.
- Scoped `git diff --check` passed; new untracked files are also covered by the whitespace scan.
- No Unity, builds, NUnit/test runners, services, installs, child agents or git mutations were run.

### Added regression inventory and coordinator handoff

New files, each with unique Unity metadata, in `Assets/Neoxider/Tests/Edit/NPC/`:

- `Audit20261023NeoNPCNavigationTests.cs`: **16 cases** covering first/origin and stationary/retarget
  paths, persistent stopping, partial arrival, invalid patrol routes/zones, disable/enable, stop
  events, paused mode switch, and Combined manual entry/retry/destruction/replacement.
- `Audit20261023NeoNPCComponentTests.cs`: **11 cases** covering pre-Awake configuration, unbound agent
  stop/resume, override/name fallback, missing controller, destroyed-reference repair, immediate hold,
  chase snapshot restoration and supplied animation time steps.

**27 cases added, zero executed here.** NavMesh tests build/destroy transient data only when the
coordinator runs them; they remove only their own data, are nonparallel, and restore Random state.
Component tests use SerializedObject for scene configuration and limited reflection solely to drive
Unity lifecycle callbacks in EditMode; assertions inspect public state, real paths/events and Animator
parameters. No source-text or no-exception-only regressions were added.

The existing `Neo.Editor.Tests` asmdef already references `Neo.NPC` and the required RPG assemblies;
**no shared reference change is required**. Coordinator: compile and run these two fixtures, then the
existing NPC decision tests. Native velocity/remaining-distance timing and transient Animator setup
are deliberately asserted as preconditions, not skipped if unavailable; adjust only with observed
Unity evidence. Real automatic callback ordering, Inspector gating and live agent rejection still
need central integration checks. The PlayMode asmdef currently lacks Neo.NPC; add that reference only
if the coordinator elects to add separate PlayMode integration tests.

### Remaining limitations and risks

- Runtime/Unity-version compilation is unverified here, including Unity 6.0–6.4 versus 6.5+. No new
  instance-ID APIs, version branches or optional dependency assumptions were introduced.
- Sampling still lacks agent-type filtering/connectivity proof and strict bounds/radius guarantees;
  partial patrol recovery and retry throttling remain Proposal A, not completed fixes.
- A patrol started while the agent is unbound needs explicit restart after binding; automatic late
  NavMesh attachment and zero-length arrival behaviour need live integration coverage.
- Numeric NaN/infinity rejection is not a new promised contract. Keep caller inputs finite and aggro
  exit distance at least the entry distance; malformed thresholds can still oscillate.
- Full running/speed/patrol-progress restoration, attack-input takeover lifetime, external ownership
  changes and arbitrary UnityEvent reentrancy are not solved by the current snapshot (Proposal B).
- Existing dependency source was read only. No cross-assembly code fix or configuration edit is
  requested; remaining integration execution belongs to the coordinator.

Кратко: просмотрены все 13 файлов, исправлены подтверждённые ошибки жизненного цикла, путей,
состояния преследования, выбора цели и восстановления навигации. Добавлены 27 регрессионных случаев
и обновлена публичная документация. Выполнены только статические проверки; Unity-тесты не запускались.
