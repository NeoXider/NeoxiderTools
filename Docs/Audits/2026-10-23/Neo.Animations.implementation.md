# Neo.Animations — implementation receipt

Status: **SOURCE-FINAL — central execution and acceptance pending.**

Original actor: `neo-audit-1023-b02-02`. Branch `demo`, target `11.0.0`;
`globalReviewApproved: true` observed before implementation. Read the complete implementation contract,
exact request, historical audit/plan, manifest entry and all nine current owned C# files.

## Additive API and dependency notice

**No additive API is introduced or required.** Public signatures, serialized fields, enum values and
assembly references remain unchanged. Dependent owners do not need to wait for a new animation API.
The playback state/manual Tick design in the [historical plan](Neo.Animations.plan.md) remains an
unimplemented proposal, outside this release correction.

## Assigned actions

The approved request assigns no additional numbered runtime action. Preserve completed audit fixes
**F1** (`FloatAnimator` publication) and **F2** (null impulse decay), their regression sources and
metadata. Complete canonical owned documentation/navigation and this receipt. No duplicate tests or
runtime framework are assigned.

## Completed source and regression work

The original [audit](Neo.Animations.md) and [plan](Neo.Animations.plan.md) remain historical artifacts,
unedited in this phase. The approved runtime patch remains the two bounded corrections below; no new
runtime or test-source edits were needed during this implementation closeout.

| Action | Exact runtime file | Result and compatibility |
|--------|--------------------|--------------------------|
| F1 — preserved | `Assets/Neoxider/Scripts/Animations/FloatAnimator.cs` | Each active sample is assigned to the actual `Value.Value`; its existing equality comparison suppresses identical notifications. Tiny unequal samples now notify, and a playing animator corrects external writes/replaced properties on its next frame. |
| F2 — preserved | `Assets/Neoxider/Scripts/Animations/TransformAnimationEvaluator.cs` | Only an absent impulse decay curve uses `1 - normalizedImpulseTime`; assigned curves and other channel fallbacks retain their behavior. |

Preserved meaningful regression sources and metadata:

- `Assets/Neoxider/Tests/Edit/Animations/Audit20261023NeoAnimationsImpulseTests.cs`
- `Assets/Neoxider/Tests/Edit/Animations/Audit20261023NeoAnimationsImpulseTests.cs.meta`
- `Assets/Neoxider/Tests/Play/Animations/Audit20261023NeoAnimationsFloatPlayModeTests.cs`
- `Assets/Neoxider/Tests/Play/Animations/Audit20261023NeoAnimationsFloatPlayModeTests.cs.meta`

F1 deliberately removes the old 0.001 float deadband. Bindings can receive more small-change callbacks;
callback cost remains unmeasured. Equal frames remain suppressed by `Neo.Reactive`. A replacement
reactive property must remain non-null and does not inherit listeners from the old instance. F2 changes
only the previously reversed null-curve envelope. Neither fix requires serialized-data migration,
asset resaving, a new assembly reference or a version-specific API.

## Completed public documentation

Updated exactly these owned files under `Assets/Neoxider/Docs/Animations/`:

| File | Concrete closeout |
|------|-------------------|
| `README.md` | Canonical entry and component links; caller-owned sampling versus independent component clocks; output/event distinction; lifecycle/pooling comparison; optional sample import and release/development/imported sample roots; concise Russian API/scene guidance. |
| `AnimationType.md` | Time/speed, null/custom-curve wrapping, sine frequency and global-RNG contracts; backlinks to the canonical entry and component guides. |
| `AnimationUtils.md` | Public low-level helpers, shared-time typed example, explicit noise overloads and caller-owned light/material/shader lifetime. |
| `FloatAnimator.md` | Actual reactive publication and property replacement; retained property/delegate cleanup; typed CanvasGroup binding; Inspector receiver, speed/reset/disable semantics. |
| `ColorAnimator.md` | RGBA threshold, initial value versus event, typed SpriteRenderer binding with nullable-event initialization/cleanup, and dynamic Color Inspector wiring. |
| `Vector3Animator.md` | Distance threshold, initial value, component-wise interpolation and receiver-owned coordinate space; typed nullable-event binding/cleanup and scene workflow. |
| `TransformAnimator.md` | Scene-free evaluator description without an execution claim; Play before Shake instead of immediate cancellation; impulse/reset/apply/disable/target ownership and Inspector workflow; migration attribute stated without claiming asset-import verification. |

All component/utility guides link back to this canonical entry. No root/package documentation, samples,
shared configuration, historical reports/plans or another owner's files were edited. The only new file
in this phase is this receipt, `Docs/Audits/2026-10-23/Neo.Animations.implementation.md`.

## Actual source-phase evidence

- Read the complete current implementation contract/request, manifest entry and own audit/plan;
  reviewed all nine owned C# files as context before closeout. Re-read the contract/request after the
  Windows resource interruption; its prohibition on `[NonParallelizable]` is respected.
- Reviewed the scoped current runtime diff: only the previously reviewed F1/F2 hunks above. Read the
  preserved regression sources and the relevant utility/listener APIs used by the documentation.
- Reviewed the owned documentation diff. Scoped `git diff --check` returned exit 0 with no whitespace
  errors; output contained only Git LF/CRLF normalization notices. Ordinary tracked diffs do not cover
  untracked files. This is diff inspection, not compilation or product validation.
- No Unity/MCP, compiler, build/test runner, product checker, installer, service, commit/push or child
  agent was started. No current test result, visual result, performance result or import result is claimed.

## Authored acceptance — UNEXECUTED

The existing prefixed fixtures provide seven cases; they are authored source, not passing evidence:

| Fixture/case | Observable acceptance after the global execution barrier |
|--------------|---------------------------------------------------------|
| `Audit20261023NeoAnimationsFloatPlayModeTests.SmallValue_IsPublished_AndUnchangedFramesDoNotNotify` | Constant 0.0005 reaches both public output aliases and the listener once; the following unchanged frame does not notify again. |
| `ExternalValueWrite_IsCorrectedOnTheNextPlayingFrame` | Constant 4 is restored on the next playing frame after an external write of 17. |
| `ReplacementReactiveProperty_ReceivesTheCurrentAnimationValue` | A replacement initialized to 17 remains the assigned instance and receives/notifies the current value 4. |
| `Audit20261023NeoAnimationsImpulseTests.NullDecayCurve_MatchesExplicitLinearDecay` — 0 / 0.1 / 0.3 / 0.4 seconds | Public evaluated position/rotation match an explicit linear decay; scale stays unchanged, time zero has a jolt and expiry equals the base pose. |

The Play fixture destroys its object and restores `UnityEngine.Random.state`; it does not depend on an
unavailable NUnit parallelism attribute. Discovery, compilation, event timing and all assertions remain
pending. Central integration should also rerun the existing `TransformAnimatorTests`,
`TransformAnimatorCombinedPlayModeTests` and `Neo.Editor.Tests.AnimationsTests` fixtures. Manual
acceptance still needs real Inspector bindings, target/pool lifecycle, imported Demo Scenes, visible
effects and small-change callback cost. Documentation snippets have not been compiled or run.

## Cross-owner dependencies and remaining debt

**No source dependency is outstanding.** No additive API is awaited and no shared edit is requested.
Integration uses existing contracts:

- `Neo.Reactive`: equality/notification and listener ownership behind `ReactivePropertyFloat`.
- `Neo.Tools.View`: `AnimationUtils` / `ILightAccessor` consumers; signatures are unchanged.
- `Neo.PropertyAttribute` and `Neo.Extensions`: existing attributes/reference only.
- `Neo.Rpg.Demo`: owns `Assets/Neoxider/Samples~/Demo/Scripts/Shell/AnimatorsDemoController.cs`;
  documentation points to sample import without changing that source or its layout.
- Primary Edit/Play test owners retain shared fixtures and asmdefs; no reference change is needed here.

The remaining dependency is the coordinator's **all-writers-finished execution barrier**, then frozen
snapshot compilation/nonempty test discovery and execution on installed Unity 6.3 and 6.6, plus the
central package/docs/UTF-8/meta/GUID and sample-import checks. Other supported version branches remain
unverified until compiled on their actual versions. Historical static checks are not new integrated
acceptance. Confirm demonstrated failures with the relevant owner before changing cross-assembly code.

## Substantial improvement retained as a proposal

The [individual plan](Neo.Animations.plan.md#substantial-proposal-explicit-playback-state-and-manual-advancement)
describes a scene-free `AnimationPlaybackState`, optional manual advancement and opt-in startup policy.
It would reuse the existing evaluators and reactive publication, address duplicated clocks and explicit
zero-start/replay needs, and preserve serialized fields and legacy default startup/event behavior.
Deterministic supplied-time parity, pause/resume, pooling and invalid-delta acceptance are required
before adopting adapters. **None of these proposed types, properties or methods is implemented or
advertised as an available API.** This remains a separate reviewed improvement, not a release prerequisite.
