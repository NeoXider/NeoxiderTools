# Neo.Tools.Other: audit

Status: complete source review, focused fixes, regression authoring, and documentation. Static validation completed; Unity compilation/execution remains with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Other/Neo.Tools.Other.asmdef`.
- Scope: all 4 manifest C# files, 2,121 baseline lines; nested assemblies excluded.
- No commits, shared configuration/asmdef edits, Unity/MCP calls, builds, test runners, installs, or child agents.

## Scope

Every line of the inventory was read. The asmdef, relevant existing tests, dependency signatures, and owned docs were also inspected.

| File under `Assets/Neoxider/Scripts/Tools/Other` | Baseline lines | Review outcome |
|---|---:|---|
| `AiNavigation.cs` | 1–1275 | Reviewed initialization, three movement modes, positional/follow paths, all public setters/queries, patrol points/zones, every coroutine, animation, gizmos, events, logging, and disable. Fixed route safety, movement state, native readiness, and coroutine ownership. |
| `CameraShake.cs` | 1–266 | Reviewed DOTween signatures, root/child lifetime, time policy, pose capture/reset, both channels, events/reentrancy, disable/destroy, and Inspector methods. Fixed the defects below. |
| `RevertAmount.cs` | 1–18 | Reviewed the entire event adapter, absent listeners, disabled direct calls, and out-of-range inputs. No demonstrated defect. Unclamped `1 - amount` is intentional; the existing `OtherToolsBehaviorTests.RevertAmount_EmitsOneMinusInput` covers the main contract. |
| `SpineController.cs` | 1–562 | Reviewed both conditional branches, lazy initialization, animation/default/track callbacks, skin lookup/offsets, state/persistence/events, serialized aliases, fallback component, and logging. Source-level fixes made; optional runtime remains unverified. |

Preserved existing serialized field names, public method signatures, property names/types, events, namespaces, component identity, and Spine serialization aliases. `HasReachedDestination` retains its public getter but now records successful arrival rather than inferring it from a stopped flag and Transform reference. No scene, prefab, sample, or another worker's topic was edited.

## Findings and fixes

The following are demonstrated source/API control-flow defects, not claims of executed runtime reproductions. Method names identify affected locations as line numbers change.

| ID / severity | Trigger and pre-fix consequence | Completed fix |
|---|---|---|
| O-01 / High | `CameraShake.StartShake`, unscaled mode during `Time.timeScale = 0`: only nested child tweens received independent updates, leaving the owning sequence stalled. | Set the time policy on the root sequence. Pause during construction and play after start callbacks. A regression observes actual paused-time completion. |
| O-02 / High | Position shake with `fadeOut = true`: the fifth `DOShakePosition` argument is snapping, so sub-unit movement was rounded to integers; `relativeShake` incorrectly controlled fade-out. | Pass named `snapping: false, fadeOut: fadeOut`. Non-relative mode starts selected channels from the captured pose. Installed `Assets/Plugins/Demigiant/DOTween/DOTween.XML` documents this exact vector overload and argument meanings. |
| O-03 / High | Start callbacks stopped/replaced a request before its sequence existed; outer execution then created an orphan or overwrote the replacement. Stop callbacks could also start a request that the outer start overwrote. | Own the paused sequence before callbacks; version checks preserve the latest request. Disabled/inactive components reject starts, including starts from disable/stop listeners. Regressions observe transform writes, duration, events, and completion. |
| O-04 / Medium | Change `shakeType` while running, then stop: reset restored the new type's channels instead of the active shake's channels. | Capture the started type; restore exactly those channels. Both type-change directions have regressions. |
| O-05 / High | An all-null looping patrol route recursively alternated `MoveToPatrolPoint`/`MoveToNextPatrolPoint` until stack overflow. Clearing the route during a wait later dereferenced null `patrolPoints.Length`. | Reject unusable startup routes, bound runtime null/destroyed waypoint searches to one route, stop unusable active routes, and cancel obsolete work. Combined mode can still start without a patrol route. |
| O-06 / High | Untracked waypoint waits/zone retries continued after stop, disable, route replacement, or combined-mode chase and could overwrite a later request. `Stop()` also let patrol Update schedule movement again. | Track/cancel patrol coroutines and guard continuations by route generation. An explicit paused state persists until resume/new movement; resuming a stopped waypoint wait continues the retained route. Clearing a zone consistently selects waypoints. |
| O-07 / High | New destinations after `Stop()` left `NavMeshAgent.isStopped` true despite reporting movement. Changing to FollowTarget cleared its target. Off-mesh stop/resume accessed invalid native state, and warp reported success regardless of its native result. | Resume the agent on accepted requests, preserve the follow target during mode changes, gate native access by readiness, return the real warp result, reject disabled warps, and remove a positional request's obsolete target-update coroutine. |
| O-08 / Medium | Positional requests omitted arrival/blocked events and reached state. Partial endpoints could report arrival. Moving an already reached follow target left arrival state latched, suppressing the next arrival. | Handle positional events/state, require a complete path for arrival, and reset/re-arm reached/blocked state when a target moves. Report accepted positional path submissions; rejected target submissions no longer emit path-updated events. |
| O-09 / Medium | Combined mode with a missing initial target and debug enabled warned every Update; unusable route retries could repeatedly warn. | Reuse the one-second debug throttle and package `NeoDiagnostics.LogWarningThrottled`/`StableId`. Correct the trigger-distance tooltip/docs to match the actual maximum-distance gate. |
| O-10 / High, conditional | The `SPINE_UNITY` branch used unqualified `StringComparison` without importing `System`. | Add `using System`; replace existing `var` declarations with explicit Spine types. This is a source fix, not an optional-branch compilation receipt. |
| O-11 / Medium, conditional | After `Stop()`, `CurrentAnimationName` fell back to the default name, so `PlayDefault()` skipped idle playback. Index calls resolved lists before lazy initialization; unknown animation names reached the runtime after the current completion handler was removed. | Initialize before index/default lookup, skip default playback only with an existing owned entry, remove callbacks even when the runtime reference is unavailable, and validate animation names before replacing the entry/subscription. |
| O-12 / Medium, conditional | Skin application could return early, yet callers updated state, persisted preferences, and emitted success. A missing live skeleton could be dereferenced; failed initialization was marked initialized. Invalid/overflowing offsets produced invalid logical choices. | Return application success and publish/save/notify only on success. Require a live skeleton, retry unsuccessful initialization, and clamp offsets before adding the legacy increment. |

Reuse receipt: consulted `neoxider-tools` tools/extensions/module references before adding local lifecycle bookkeeping. Retained existing DOTween and NavMesh APIs and existing component coroutines; reused package diagnostics/throttling/stable identity. No replacement timer, generic coroutine framework, navigation manager, or mandatory NoCode dependency was introduced.

## New behavioral regressions

New files under `Assets/Neoxider/Tests/Edit/Tools/Other`, with unique `.meta` GUIDs:

| File | Authored cases | Observable contracts |
|---|---:|---|
| `Audit20261023NeoToolsOtherCameraShakeTests.cs` | 8 | Fractional motion, captured baseline, both changed-type resets, stop/replacement in start events, replacement in stop events, actual unscaled completion, and disable-time restart rejection. |
| `Audit20261023NeoToolsOtherNavigationTests.cs` | 10 | All-null route rejection; movement after stop/mode switch and disabled warp; positional events/state; partial-path blockage without arrival; repeat arrival at a moved target; stopped/paused/resumed/disabled/cleared routes during real waits; null waypoint skipping. |

Navigation tests create/remove only their own in-memory NavMesh data and temporary objects, including disconnected islands for partial paths. Engine lifecycle cases enter Play Mode through the existing Editor test assembly. Camera tests inspect one private sequence field for deterministic seeking; assertions observe transforms, events, duration, and public state. Plain Edit Mode camera setup initializes the existing Awake message explicitly. No source-text/no-exception regressions were added.

`Neo.Editor.Tests` already references `Neo.Tools.Other` and `DOTween.dll`; these fixtures need no shared asmdef changes. **All 18 cases are authored, not executed.** Spine regressions against an absent, unverifiable vendor runtime were not practical; coordinator integration scenarios are listed below.

## Documentation

Updated existing English entry/component docs in `Assets/Neoxider/Docs/Tools/Other`:

- `README.md`: repaired the table; clarified assembly/dependency boundaries, optional global-namespace Spine adapter, and C#/Inspector access.
- `CameraShake.md`: fade-out, fractional motion, relative/captured baseline, affected-channel reset, time policy, replacement callbacks, lifecycle, and a child pivot for moving cameras.
- `AiNavigation.md`: readiness, stop/resume, runtime route changes, positional/partial/follow-arrival events, cancellation, correct trigger distance, and independent NavMeshAgent lifetime.
- `SpineController.md`: conditional API/reference requirements, initialization, idle after stop, successful-only skin publication/persistence, offset rules, and the legacy animation-name fallback.
- `RevertAmount.md`: C# subscription example and intentional unclamped/disabled-call semantics alongside the existing Inspector workflow.

`BillboardUniversal.md`, root/package documentation, DocsEn, and shared navigation/index configuration were untouched.

## Substantial improvement proposal

**Proposed only: deterministic, composable camera shake.**

Problem: `CameraShake` couples waveform generation, time advancement, pose ownership, DOTween lifetime, Inspector data, and events. Its Awake reset baseline can move a later-repositioned camera backward unless callers author a dedicated pivot. Sampling/replay/testing requires a Transform and DOTween; non-finite public input behavior is undefined.

Architecture/public API: first inspect/reuse existing `Neo.Animations` procedural sampling facilities. Introduce a small `ShakeRequest` carrying duration, position/rotation strengths, decay, and seed, with a plain `ShakeState.Start(request)`, `Tick(float deltaTime)`, `Stop()`, `PositionOffset`, and `RotationOffset`. Sampling should not read Unity time or mutate scene objects. The MonoBehaviour applies offsets to an explicitly assigned pivot, chooses scaled/unscaled delta time, and owns events. An optional data-only `CameraShakeProfile` asset may share requests without scene references. No global manager is needed.

Compatibility/migration: keep the current component, enum, serialized field names, methods, Inspector buttons, and events as an adapter. Keep the captured-baseline default; opt into a moving-rig baseline policy or a dedicated pivot. Preserve the cancellation/version semantics fixed here. Existing prefabs must retain behavior until explicitly opting in; waveform changes need release notes. Assembly/module integration requires coordination with the separately owned animations assembly.

Acceptance: identical seed/delta sequences give identical offsets; fractional/zero amplitudes; defined negative/NaN/infinite policy; moving camera/parent without baseline jumps; relative/captured modes; paused-unscaled progression; channel selection; replacement from every event; disable/destroy cleanup; natural completion versus cancellation; plain C# use without a GameObject.

Effort/risk: approximately 2–4 implementation/review days after validating existing sampler reuse, plus Unity integration. Medium risk from baseline composition and old DOTween waveform expectations; introduce the sampler behind opt-in behavior first.

## Verification and remaining risks

Executed static checks:

1. Strict UTF-8/no-replacement-character and trailing-whitespace checks on four inventory sources, five docs, two fixtures, and two metas (13 files), then report validation during finalization.
2. Lightweight delimiter/directive balance and explicit-type/direct-identity scans for all six inventory/fixture C# files. These are lexical checks, not compilation.
3. Field name/type, existing public method signature, and property name/type comparison against `ca198556`: all four inventory files preserve them. This does not replace Unity serialization/binary compatibility checks.
4. All nine relative links in the five docs resolve. Both new metadata GUIDs are valid and occur only in their own metas in the Assets tree.
5. Scoped `git diff --check` for owned source/docs/new fixtures/metas/report; untracked additions also receive explicit UTF-8/trailing-whitespace validation.

Coordinator actions / unverified risks:

- Compile and execute `Audit20261023NeoToolsOtherCameraShakeTests`, `Audit20261023NeoToolsOtherNavigationTests`, and existing `OtherToolsBehaviorTests`. New tests need real lifecycle dispatch, a usable NavMesh agent build setting, and DOTween. Their runtime results/timing remain unverified.
- Compile the supported Unity 6.0–6.4 and 6.5+ identity branches centrally. This worker added no direct version-specific identity calls.
- Compile `SPINE_UNITY` with a supported Spine Unity Runtime. It is absent here. If the installed vendor runtime uses asmdefs, the coordinator must provide its references to `Neo.Tools.Other` and future Spine test assemblies; enabling only the define is insufficient.
- Spine integration scenarios: stop then play default; unknown animation preserves playback; index lookup before initialization; missing skeleton then successful retry; failed custom skin preserves state/prefs/event count; valid named/indexed skin with zero/negative/oversized/legacy offsets; destroy unsubscribes completion.
- Legacy navigation still has two target path schedulers (coroutine and Update), MonoBehaviour-bound policy/route state, and no processing of fully invalid paths without `hasPath`. Further redesign should involve the `Neo.NPC` owner. Rotated/scaled patrol zones and external Spine track/skeleton rebuild ownership were reviewed but not runtime-verified.
- Non-finite camera configuration and composition with other Transform writers were not broadened into new behavior in this bug-fix pass; use the documented pivot approach and the proposal's explicit input/baseline contract.

Кратко: проверены все четыре исходника; исправлены ошибки тряски, патруля, состояния навигации и условной Spine-ветки. Добавлены 18 поведенческих регрессий и обновлены существующие документы. Unity и тестовые раннеры не запускались; статические проверки не подтверждают компиляцию или прохождение тестов.
