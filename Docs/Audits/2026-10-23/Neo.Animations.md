# Neo.Animations: audit

Status: source audit and scoped fixes complete; central compilation and test execution pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Animations/Animations.asmdef`.
- Scope: 9 C# files; nested assemblies excluded.

## Scope and review receipt

Read `AGENTS.md`, the entire worker contract, this assembly's exact manifest entry and the original
report. Reviewed every owned C# file in full: 9 files / 1,496 baseline source lines. Static inventory
comparison found exactly those nine files and one root asmdef; there are no nested assemblies here.
Only owned source/docs, this report, and new uniquely prefixed regression files were changed.

All source paths below are under `Assets/Neoxider/Scripts/Animations/`.

| File | Review coverage and outcome |
|------|-----------------------------|
| `AnimationType.cs` | All nine enum values and dispatch correspondence; enum order/public compatibility preserved. No defect found. |
| `AnimationUtils.cs` | Full float/color/vector dispatch, speed <= 0, unknown enum and null custom-curve fallbacks, interpolation bounds, noise scale/2D/offset forwarding, light/material application and `ILightAccessor`. Apply methods require a supplied valid accessor/material and do not allocate or own materials. No demonstrated runtime defect found. |
| `ColorAnimator.cs` | Start initialization, randomized noise/clock, all public setters, RGBA distance threshold, event invocation and Play/Stop/Pause/Resume behavior. Current color is updated independently of event throttling; no rendering resources are owned. Existing legacy lifecycle retained. |
| `FloatAnimator.cs` | Reactive output, serialized configuration, runtime range setters, Start/Update/OnValidate, events and lifecycle controls. Found and fixed incorrect output suppression; see F1. |
| `TransformAnimationEvaluator.cs` | Null settings/base pose, rotation, float direction fallback, cycle duration boundaries, scale mapping, deterministic noise, impulse lifetime/strength and curve fallbacks. Found and fixed inverted null impulse envelope; see F2. |
| `TransformAnimationSettings.cs` | All channel data, defaults, Inspector limits and `FormerlySerializedAs` aliases. Plain serializable data has no scene references. Inspector ranges are authoring hints; runtime callers supply suitable numeric values. No serialization changes. |
| `TransformAnimationState.cs` | Entire returned value type; only local position/Euler/scale data, no owned resources or scene dependencies. No defect found. |
| `TransformAnimator.cs` | Exact captured-target ownership, null/self fallback, retarget/restore, base capture, Start/OnEnable/OnDisable pooling lifecycle, scaled/unscaled advancement, pause/resume, null settings, disabled/negative impulse inputs and events. Existing target restoration and idempotent Play behavior are already implemented and covered by existing tests. No additional demonstrated defect found. |
| `Vector3Animator.cs` | Start initialization, full noise forwarding, all setters, current vector versus notification threshold and lifecycle controls. Output is data; applying position/scale/rotation belongs to its receiver. Existing legacy lifecycle retained. |

Cross-cutting review: no runtime logging, unmanaged resources, material instantiation, scene searches,
editor-only dependencies or obsolete instance-ID calls occur in the inventory. Public APIs, serialized
field names, enum values and asmdefs were preserved. No sample, scene, prefab or shared configuration
was edited. Review and verification were local; no child agents or independent QA were used.

Read-only context: runtime/test asmdefs, relevant `ReactiveProperty` setter/listener APIs, existing
`TransformAnimatorTests`, `TransformAnimatorCombinedPlayModeTests`, and `View/AnimationsTests`.
Reuse check consulted the NeoxiderTools animation/module/extension references. Existing
`ReactivePropertyFloat.Value` already implements equality-based notification suppression, so F1 reuses
that primitive; no parallel helper or runtime service was introduced.

## Findings and completed fixes

### F1 — P2 / medium: float output can remain wrong indefinitely

- Source: [FloatAnimator.cs](../../../Assets/Neoxider/Scripts/Animations/FloatAnimator.cs), `Update`;
  baseline lines 125–129 compared the evaluated value with private `lastValue`, rather than the actual
  publicly writable reactive value.
- Source-level reproduction A: set `MinValue = MaxValue = 0.0005f`, `AnimationSpeed = 0`, then `Play`.
  The evaluator returns 0.0005, but `Abs(0.0005 - 0) <= 0.001`; output remains 0 and listeners never
  receive the valid value. Tiny animation ranges can therefore stop working entirely.
- Source-level reproduction B: animate the constant 4, then write 17 into `Value.Value` or replace
  `Value` with `new ReactivePropertyFloat(17f)`. The next sample is still 4 and cached `lastValue` is 4,
  so the old code never restores the current property. UI/gameplay bindings can stay on 17 indefinitely.
- Fix: remove the private cache/deadband and assign each evaluated sample to `Value.Value`. Its existing
  equality comparer suppresses repeated identical notifications against the current property.
- Compatibility: all public/serialized members remain unchanged. Unequal changes below 0.001 now
  notify; consumers that need throttling should apply it at their binding. External writes are corrected
  only while playing and unpaused; property replacement does not transfer listeners.
- Regressions: new
  [Audit20261023NeoAnimationsFloatPlayModeTests.cs](../../../Assets/Neoxider/Tests/Play/Animations/Audit20261023NeoAnimationsFloatPlayModeTests.cs)
  exercises real Start/Update frames, with no reflection or substitute evaluator: tiny output and
  notification delivery, suppression of unchanged frames, recovery after an external write, and delivery
  to a replacement reactive property. Three UnityTest cases; fixture destroys its object and restores
  global random state. These tests have been authored, not executed here.

### F2 — P2 / medium: absent impulse decay curve reverses the envelope

- Source:
  [TransformAnimationEvaluator.cs](../../../Assets/Neoxider/Scripts/Animations/TransformAnimationEvaluator.cs),
  `Evaluate`; baseline line 68 reused the generic curve fallback, which returns the phase itself.
- Trigger: a C# caller sets `ImpulseDecayCurve = null` and evaluates an active impulse, directly or
  through `TransformAnimator`. The channel is explicitly a decaying one-shot shake.
- Observable consequence: normalized time 0 gives zero strength, and normalized time 1 gives full
  strength. A missing curve produces a delayed, growing shake and an abrupt cutoff instead of decay.
  With duration 0.4 and elapsed 0.1, the old envelope is 0.25 rather than the intended linear 0.75.
- Fix: only the null impulse-curve branch uses `1 - normalizedImpulseTime`. Assigned curves and the
  float/scale phase fallbacks remain unchanged. No new allocations, API or serialized-data migration.
- Regressions: new
  [Audit20261023NeoAnimationsImpulseTests.cs](../../../Assets/Neoxider/Tests/Edit/Animations/Audit20261023NeoAnimationsImpulseTests.cs)
  compares evaluated position/rotation with an explicit linear decay at 0, 0.1, 0.3 and 0.4 seconds.
  It also requires an actual initial jolt, exact base position/rotation at expiry and unchanged scale.
  Four parameterized cases use the public evaluator and observed pose data, without sampling a copy
  of the noise formula. These tests have been authored, not executed here.

Русское пояснение: убрана потеря малых float-значений и зависание после внешней записи; null-кривая
импульса теперь даёт линейное затухание. Проверены все 9 файлов; запуск Unity-тестов остаётся координатору.

## Documentation completed

Updated existing English canonical docs under `Assets/Neoxider/Docs/Animations/`:

- `README.md`: scene-free evaluation, C# and Inspector bindings, current values versus event
  notification thresholds, scaled clocks, startup randomization and transform lifecycle differences.
- `FloatAnimator.md`: precise reactive update behavior, external writes/property replacement, tiny
  values, equal-value suppression, listener ownership, a typed C# binding and Inspector workflow.
- `TransformAnimator.md`: null impulse linear fallback, other null curve behavior, scene-free example
  and `Shake` scene/UnityEvent workflow.
- `AnimationUtils.md`: precise non-positive-speed and null custom-curve contracts; distinguish the
  lower-level noise/blend helpers from the minimum-value shortcut.
- `Vector3Animator.md`: existing C# binding example now uses explicit types as required by AGENTS.md.

No competing module guide, root/package README or another owner's documentation was introduced.

## Substantial improvement proposal — explicit, testable playback clock

**Not implemented in this audit.**

Problem: the three value animators duplicate lifecycle and clock logic, while `TransformAnimator` has a
different, more explicit pooling contract. All value animators unconditionally randomize their clocks
in Start, so a caller's earlier `ResetTime` is overwritten and one-shot curves can start late in their
timeline. There is no common public manual-tick contract for deterministic game logic, replay or a
non-scene caller. Output filtering in that duplicated wrapper logic caused F1.

Proposed architecture/public API:

- Add a plain C# `AnimationPlaybackState` with read-only `Time`, `IsPlaying`, `IsPaused` and explicit
  `Initialize(float initialTime)`, `Play()`, `Stop()`, `Pause()`, `Resume()`, `ResetTime(float time = 0f)`
  and `Advance(float deltaTime)` methods. Initialization is separate from playback; the state owns no
  Unity object, random generator, callbacks or rendering output.
- Keep `AnimationUtils` and `TransformAnimationEvaluator` as the existing evaluators. A plain C# caller
  advances the state, then evaluates with its own seed/base/configuration. Unity wrappers select scaled
  or unscaled delta time and apply results/dispatch existing events.
- Add an opt-in wrapper startup policy, e.g. `AnimationStartPolicy.LegacyRandomized` versus
  `StartAtZero`, rather than silently changing existing component defaults. Keep random generation in
  adapters, with the resulting initial time supplied to the core explicitly.
- Keep value publication with its actual owner: `ReactivePropertyFloat` handles equality; color/vector
  wrappers retain their documented distance threshold. Avoid adding another cached copy of reactive
  output to the clock or introducing a common serialized MonoBehaviour base class.

Compatibility/migration: add the runtime type without moving or renaming serialized fields. Preserve
current UnityEvent invocation policies in existing adapters, the default randomized startup of value
animators, `PlayOnEnable`/target restoration of TransformAnimator, and the corrected F1/F2 behavior.
Adopt the clock one wrapper at a time after parity checks. Existing scenes require no resave; callers
choose the explicit startup policy only when desired. Any later default change needs a versioned migration.

Acceptance scenarios:

1. Initialize at zero, sample a one-shot custom curve, and verify monotonic time advancement independent
   of component startup order; initialization/reset performed before first playback is retained.
2. Identical supplied initial time/delta sequence gives identical sampled output; pause advances neither
   main nor impulse time, and resume continues from that point.
3. Existing Float/Color/Vector lifecycle event counts and output contracts remain compatible, including
   tiny float changes and replacement reactive properties; unchanged frames do not emit float events.
4. Transform pooling restores/re-captures the correct target, resets the impulse and starts only once
   per enable period; target changes never apply one target's base to another.
5. Invalid elapsed time is handled by one documented core policy, with focused tests, rather than by
   different checks scattered across wrappers. Verify Unity 6.0–6.4 and 6.5+ compilation centrally.

Effort/risk: roughly 2–3 engineering days for the core, adapter parity tests, docs and version checks.
Medium risk lies in startup/event ordering and pool behavior; additive API and preserved serialized
storage avoid asset migrations. This should be a separate reviewed change, not part of the two fixes.

## Actual verification and remaining risks

Executed static validation only; no Unity, MCP, compiler, build/test runner, installs, shared services,
commits or child agents were started.

- Scoped `git diff --check -- <owned source/docs/new test paths>`: exit 0, no whitespace errors. Git
  printed only LF-to-CRLF working-copy normalization notices. New untracked C# files also passed explicit
  trailing-whitespace inspection; they are not included in ordinary tracked diffs.
- Python read-only consistency check: exact nine-file source inventory and one asmdef; strict UTF-8 /
  no replacement characters across 20 package text files (9 source, 7 docs, 2 regressions, 2 metadata).
- All 11 source/regression C# files passed explicit-type, obsolete instance-ID API and balanced-delimiter
  scans. This is structural validation, not a C# compilation claim.
- Both test filenames use the exact `Audit20261023NeoAnimations` prefix. New `.meta` GUIDs are valid
  32-digit hex, and an `rg` search across all `Assets/**/*.meta` found each only at its new file.
- Existing Edit and Play test asmdefs already reference `Neo.Animations`; Play also references
  `Neo.Reactive`. No coordinator reference/config change is needed for these tests.
- All 28 relative links in the seven public animation docs resolve. Reviewed the focused tracked diff
  and the authored test sources against both defect triggers.
- Final report check: strict UTF-8, resolving report links and no queued placeholders; all five new files
  (report, two tests and two metadata files) are whitespace-clean. Public declaration lines in both edited
  runtime files match HEAD, and read-only provenance verification confirmed HEAD `ca198556`.

Central actions still required: compile on supported Unity versions, run the four new Edit Mode and
three new Play Mode cases, and rerun the existing animation tests. Runtime/visual effect behavior and
Unity test discovery have not been verified here. Unequal float samples can now generate more
callbacks than before; confirm real bindings tolerate their full precision. Existing finite-number
preconditions, value-animator randomized startup, and non-null reactive-property ownership are unchanged.

No unresolved cross-assembly defect or shared-config edit was demonstrated. The only integration action
requested from the coordinator is the central compilation and focused test execution above.
