# Neo.Tools.Physics — implementation plan

Phase 2; baseline `ca198556`, package `10.19.0`. Based on the planning contract, completed audit, manifest and current code/docs/tests. Proposals below are unimplemented.

Path abbreviations below resolve exactly to:

- `R/`: `Assets/Neoxider/Scripts/Tools/Physics/`
- `T/`: `Assets/Neoxider/Tests/Edit/Tools/`
- `D/`: `Assets/Neoxider/Docs/Tools/Physics/`

## Current outcome

All three sources were reviewed. F1–F9 are implemented: delayed activation, compound/self-body handling, single explosion falloff, safe cooldown maintenance, trigger/manual repeat restrictions, impulse callback guards, magnetic cache refresh, disable cleanup and Play Mode button gating. Four guides document API, Inspector workflows and compatibility. These fixes are not pending work.

`T/Audit20261023NeoToolsPhysicsRegressionTests.cs` contains 20 cases. Static inventory/declaration/text/link/GUID/whitespace checks passed; 37 serialized fields and 40 public declarations were preserved. Unity compilation, tests, physics and Inspector checks remain unexecuted. No additional mandatory source defect was confirmed.

## Priorities and verification debt

| Priority / owner | Evidence and smallest next action | Observable acceptance |
|---|---|---|
| P0 / coordinator; Physics corrections | Regressions dispatch lifecycle messages, not engine scheduling. Compile frozen changes; run the 20 cases and existing `PhysicsToolsTests`. Correct reproduced failures only. | Cases pass; linear/quadratic impulses are 10/15 for force 20, radius 4, distance 2. Compound bodies receive one force and documented events. |
| P1 / Physics owner, coordinator executes | `R/ExplosiveForce.cs:Start/ExplodeDelayed` and `R/ImpulseZone.cs:Update/ApplyImpulse` use scaled engine time. Add `T/Audit20261023NeoToolsPhysicsLifecycleTests.cs`, using the existing EditMode `EnterPlayMode`/`UnityTearDown` pattern. | Positive delay fires once after its deadline; pause freezes it. Cooldown permits a second application only after expiry; one-shot still blocks until reset, including across disable/re-enable. |
| P1 / same owners | Contact delivery/native disable remain unverified. Extend the lifecycle fixture with real colliders, Rigidbody movement and optional body creation. | Correct filtering, one accepted impulse, body creation only when enabled, exits on disable, fresh entries on enable, no force after listener-driven disable. |
| P1 / coordinator + `Neo.PropertyAttribute` owner | Explosion uses `ButtonAttribute.PlayModeOnly`; actual Inspector behavior is unverified. Smoke-check the existing drawer before considering any editor correction. | **Explode Now** disabled in Edit Mode, enabled in Play Mode; no authored-object mutation from the disabled button. |
| P2 / Physics owner | `R/MagneticField.cs:UpdateField` gates queries and forces together. This confirmed, documented timing limitation motivates the opt-in proposal below; it is not an unfinished F1–F9 fix. | Legacy behavior remains stable; continuous behavior meets the independent integration checks below. |

Unconfirmed risks: saturated-query membership, emitter destruction from listeners, cross-component recursion and invalid inputs. Require observable reproduction before promoting these to fixes. Default-scene scope, coalesced toggle transitions and automatic explosion pooling limits are documented contracts.

## Substantial proposal: independent discovery and force cadence

**Problem/reuse.** With fixed step 0.02, sampling strength 10 every 0.1 seconds submits approximately one fifth of full-rate impulse. Reuse filtering, deduplication, events, caches, buffers and cleanup. Catalog searches found no standalone magnetic evaluator. `AdvancedForceApplier` (`Neo.Tools.Move` assembly) is scene-based; coupling Physics to it would not separate discovery cadence.

**Focused design and exact files.** Add `R/MagneticForceCadence.cs` with `LegacySampled = 0, Continuous = 1`; `R/MagneticForceSettings.cs` with constructor-set, read-only `Strength`, `Radius`, `Mode`, `Falloff`; and `R/MagneticForceEvaluator.cs` with a scene-free calculation. Settings reuse existing `MagneticField.FieldMode`/`FalloffType` enums. Proposed API:

```csharp
public static Vector3 Evaluate(MagneticForceSettings settings,
    Vector3 fieldCenter, Vector3 bodyCenter, Vector3 attractionPoint,
    Vector3 worldDirection, bool reversePhase);
```

The evaluator reads no scene objects, randomness or global clock. Nonfinite input, zero radius and coincident centers return zero. Legacy evaluation remains unchanged.

Adapt `R/MagneticField.cs`: add `SetCadence(MagneticForceCadence)`, `SetAutomaticUpdate(bool)`, read-only `Cadence`/`AutomaticUpdate`, query-only `RefreshTargets()`, and `bool Step(float deltaTime)`. Manual stepping requires continuous mode, automatic updates off, active state and finite positive time; otherwise return false without force. One caller owns the driver; allow substeps.

Continuous automatic operation runs in `FixedUpdate`. Legacy operation retains `useFixedUpdate` and current sampling. Continuous query/toggle clocks advance from supplied scaled delta time. Discovery follows `updateInterval`; each step validates cached colliders/body ownership and submits one impulse `evaluatedForce * deltaTime` per body. Explicit impulse integration avoids accidentally using Unity's fixed step again for a manually supplied duration. Preserve per-GameObject membership and existing long-gap phase-event coalescing; pending impulses are not retroactively cancelled on disable.

**Tests/docs.** Add `T/Audit20261023NeoToolsPhysicsForceEvaluationTests.cs` for modes/falloffs/boundaries and `T/Audit20261023NeoToolsPhysicsCadenceTests.cs` for integration; lifecycle tests cover driver/disable behavior. Update `D/MagneticField.md` and `D/README.md` with C# driving, Inspector cadence, refresh, units and migration. Reuse UnityEvents for NoCode; add no editor/action chain. New package files require unique `.meta` files.

**Compatibility/risk.** Old assets default to zero/legacy cadence with automatic updates enabled. Preserve serialized names, enums, methods and payloads; add no assembly/dependency. Opt-in users may need retuning and accept discovery latency. Estimate 2–4 days including tests/docs. Risks: duplicate drivers, clock drift, deserialization defaults. Saturation policy/local-scene support remain separate proposals.

## Implementation order and completion gates

1. Coordinator freezes concurrent source changes, compiles and runs the audit regressions. Resolve demonstrated failures first; add lifecycle coverage before API work. Do not weaken force/event assertions to obtain a pass.
2. Implement/test the value evaluator, then cadence clocks and component adapter, then integration scenarios and docs. No changes to explosion/impulse behavior unless verification demonstrates a remaining defect.
3. Continuous acceptance: mass 1, strength 10, constant direction, no gravity/damping gives one-second velocity change 10 within 5%, with query intervals 0/0.1 and simulation steps 0.01/0.02. A render schedule of 30/60/120 FPS must not change the result materially. Manual/automatic driving agree; rejected steps add no impulse; destroyed/reparented bodies do not receive stale force; colliders do not multiply it. Legacy serialized fixtures retain their current results/defaults.
4. Run centralized compilation on supported Unity 6.0–6.4 and 6.5+, focused EditMode and real Play Mode checks. This module has no version branches; dependency branches still need their corresponding compilers. Any future ID use must go through existing centralized compatibility APIs, never scattered conditionals.
5. Validate public signatures, serialized assets, unique GUIDs, UTF-8, links and scoped diff whitespace. Preserve `Samples~` release layout; no sample/config rewrite is proposed. Coordinator owns package/changelog integration.

`Neo.PropertyAttribute` owns Inspector-gate corrections; `Neo.Extensions` remains an existing reference with no requested changes. `Neo.Editor.Tests` already references Physics and supports the chosen lifecycle-test pattern. `Neo.Tests.Play` currently lacks a Physics reference: a later move there requires coordinator-owned asmdef work first, not an implicit worker edit. `Neo.Editor` needs no new adapter.

Do not close implementation with failed compilation, incorrect physics, changed defaults or unexplained events. Report the failure/owner. Coordinator scheduling of the proposal does not block authorized audit corrections. This phase changes only this plan and runs no Unity/build/test processes.
