# Neo.Animations — individual implementation plan

Phase 2, same auditor/session; assigned configuration: `gpt-6.1-sol`, reasoning `max`.
Read PLAN-CONTRACT, the completed [audit](Neo.Animations.md), manifest entry and relevant current
source/docs/tests. Planning changes only this file.

## Current outcome

The audit reviewed all nine owned C# files. Two demonstrated defects are already fixed:

- `FloatAnimator.Update` publishes every evaluated sample through `ReactivePropertyFloat.Value`;
  values below 0.001 and recovery after external writes/property replacement now reach the actual output.
- `TransformAnimationEvaluator.Evaluate` uses linear decay when `ImpulseDecayCurve` is null, instead
  of increasing impulse strength. Assigned curves retain their behavior.

Seven regression cases and five public-doc updates are complete. Static inventory, encoding, GUID,
link, type, public-declaration and diff checks passed. Compilation and Unity verification remain
unexecuted. Do not schedule these completed fixes again.

## Prioritized remaining work

No additional confirmed, unfixed runtime defect remains from this audit.

| Priority / status | Evidence, owner and smallest next action | Observable acceptance |
|-------------------|-----------------------------------------|-----------------------|
| P1 — verification debt | Coordinator after source audits finish: compile/run new and existing animation fixtures. Correct reproduced failures only; dependency failures belong to their assembly owner. | Seven new cases discovered/passing; existing assertions pass. |
| P2 — unverified compatibility risk | F1 increases small-change callbacks; scene binding cost is unmeasured. `ReactiveProperty.cs:68–80` suppresses equal values. Animation owner checks notifications; consumer owner corrects demonstrated expensive bindings. | Constant value notifies once; unchanged frames never notify; small changes reach receivers. No speculative throttle. |
| P3 — proposal | Value animators randomize time in `Start`, overwriting earlier `ResetTime`. This documented behavior motivates the opt-in feature below; it is not a newly declared defect. | Explicit startup/manual advancement preserves legacy defaults. |

Existing verification scope: `TransformAnimatorTests`, `TransformAnimatorCombinedPlayModeTests`,
and `Neo.Editor.Tests.AnimationsTests` from `Tests/Edit/View/AnimationsTests.cs`.
New fixture paths, already present:

- `Assets/Neoxider/Tests/Edit/Animations/Audit20261023NeoAnimationsImpulseTests.cs`
- `Assets/Neoxider/Tests/Play/Animations/Audit20261023NeoAnimationsFloatPlayModeTests.cs`

## Substantial proposal: explicit playback state and manual advancement

Developers duplicate clocks for deterministic sampling while wrappers differ in event/pooling semantics.
Add scene-free state, retaining evaluators/adapters; no scheduler, common serialized base or manager.

Reuse `AnimationUtils`, `TransformAnimationEvaluator`, their settings/state and `ReactivePropertyFloat`.
The checked time catalog/API shows `Neo.Tools.Time.Timer` is duration/UniTask-driven; `TimerObject`
is a broader scene wrapper. Adapt existing animation clocks rather than introduce that dependency.

Proposed public API in `Neo.Animations`:

| Member | Contract |
|--------|----------|
| `AnimationPlaybackState.Time`, `IsPlaying`, `IsPaused` | Read-only time and playback flags. |
| `void Initialize(float initialTime = 0f)` | Explicit finite, nonnegative starting time; clears playback flags. No randomness/events. |
| `void Play()`, `Stop()`, `Pause()`, `Resume()` | Change flags; Stop retains time, Pause/Stop prevent advancement. |
| `void ResetTime(float time = 0f)` | Sets finite, nonnegative time without changing playback flags. |
| `float Advance(float deltaTime)` | Returns actual elapsed advancement; zero when stopped, paused or delta is negative. Reject nonfinite input/result before mutation. |

Components gain `bool ManualUpdate { get; set; }` (default false), `void Tick(float deltaTime)`.
Update calls Tick outside manual mode. Tick observes playback flags and samples even at zero elapsed
time. Transform impulse uses accepted delta; target restoration/impulse ownership stay in its adapter.

Float/Color/Vector gain `AnimationStartPolicy StartPolicy`: `LegacyRandomized = 0`, `StartAtZero = 1`.
Legacy preserves Start randomization; StartAtZero initializes once, respecting earlier ResetTime/RandomizeTime.
Noise offsets remain separate. Transform keeps `RandomizeStartTime`, `UseUnscaledTime`, `PlayOnEnable`.
Value adapters preserve repeated Play/Pause notifications; Transform keeps idempotent guards.
Core state dispatches no callbacks.

Determinism applies to supplied clock inputs and analytic/seeded evaluation. `RandomFlicker` retains
its existing Unity global RNG contract; this proposal does not promise deterministic flicker.

## Exact implementation scope and compatibility

All proposed runtime changes remain in `Neo.Animations`, under
`Assets/Neoxider/Scripts/Animations/`:

- New `AnimationPlaybackState.cs` and `AnimationStartPolicy.cs`, with unique `.meta` files.
- Adapt `FloatAnimator.cs`, `ColorAnimator.cs`, `Vector3Animator.cs`, then `TransformAnimator.cs`.
- Preserve `AnimationType.cs`, `AnimationUtils.cs`, `TransformAnimationEvaluator.cs`,
  `TransformAnimationSettings.cs` and `TransformAnimationState.cs` APIs and corrected behavior.

New tests: `Assets/Neoxider/Tests/Edit/Animations/Audit20261023NeoAnimationsPlaybackStateTests.cs`
and `Assets/Neoxider/Tests/Play/Animations/Audit20261023NeoAnimationsPlaybackAdaptersPlayModeTests.cs`,
with unique metadata. Existing test asmdefs already reference required assemblies; no reference changes.

Update `Assets/Neoxider/Docs/Animations/README.md`, `FloatAnimator.md`, `ColorAnimator.md`,
`Vector3Animator.md`, `TransformAnimator.md`: typed examples, listener ownership, startup and Inspector/UnityEvent
setup. Preserve public fields/signatures, serialized names/events/migration attributes and canonical entry.
Manual-mode fields default false; startup enum defaults zero. No asset resave or sample move.

Cross-assembly coordination: `Neo.Reactive` owns setter/listener semantics; wait for its final source
before integration checks. `Neo.PropertyAttribute` owns Inspector buttons and `Neo.Extensions` remains
an existing reference; neither needs edits. `Neo.Tools.View` owns downstream `LightAnimator` consumers
of `AnimationUtils`/`ILightAccessor`; preserve these contracts and let that owner verify integration.
`Neo.Rpg.Demo` owns `Assets/Neoxider/Samples~/Demo/Scripts/Shell/AnimatorsDemoController.cs`; sample
enhancement is optional and belongs to that owner after runtime acceptance. If checked, support active
`Samples`, release `Samples~`, and imported `Assets/Samples/NeoxiderTools/<version>/<sample name>` roots.

## Implementation order, acceptance and stop conditions

1. Coordinator closes P1 debt first. Keep audit corrections independently deliverable; diagnose failures
   before adding the proposed API.
2. Add state/enum and Edit Mode tests: initialization/reset, pause/resume/stop, negative/nonfinite delta,
   unchanged state after rejected input, and repeatable caller-driven evaluator results.
3. Adapt value components individually, then Transform. Add Play Mode assertions for legacy event counts,
   zero-start before Start, manual mode without automatic double advancement, and unchanged F1/F2 results.
4. Verify Transform pause freezes main/impulse clocks; disable restores the exact captured target;
   re-enable starts once and retargeting restores the old pose without contaminating the new target.
5. Update public docs, then coordinator validates package/test compilation in Unity 6.0–6.4 and 6.5+.
   No new version-specific calls are needed; shared compatibility helpers stay centralized with owners.
   Check metadata, serialized defaults, sample-root expectations and affected demo behavior centrally.

Minimum observable outputs remain 0.0005 delivered once, external 17 corrected to 4 on the next playing
frame, replacement property notified, and null impulse matching explicit linear decay at 0/0.1/0.3/0.4s
with base pose at expiry. Proposed manual Tick must produce equivalent poses for identical accepted
time/seed inputs while paused ticks leave output unchanged.

Estimated proposal effort: 3–4 engineering days including adapter parity tests/docs; medium risk in
startup ordering, callbacks and pooling. Stop feature promotion on compile failures, serialization drift
or changed legacy event/pose behavior; repair the smallest reproduced issue and rerun affected checks.
Changing default randomization or event policy is a separate product/migration decision, not required
for the additive design. Until coordinator execution, Unity acceptance remains explicitly unverified.
