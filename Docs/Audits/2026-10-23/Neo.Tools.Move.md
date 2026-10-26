# Neo.Tools.Move: audit

Status: source audit and focused fixes complete; Unity compilation and test execution await the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Move/Neo.Tools.Move.asmdef`.
- Scope: 31 C# files; nested assemblies excluded.

## Scope

- `Assets/Neoxider/Scripts/Tools/Move/AdvancedForceApplier.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CameraConstraint.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CameraRotationController.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CharacterController/NeoCameraInput.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CharacterController/NeoCharacterCameraBridge.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CharacterController/NeoCharacterInput.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CharacterController/NeoCharacterNetworkBinding.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CharacterController/NeoCharacterSprint.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CharacterController/NeoInputAvailability.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CharacterController/NeoInputBackend.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CharacterController/NeoInputBackendResolver.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CharacterController/NeoLookRate.cs`
- `Assets/Neoxider/Scripts/Tools/Move/CursorLockController.cs`
- `Assets/Neoxider/Scripts/Tools/Move/DistanceChecker.cs`
- `Assets/Neoxider/Scripts/Tools/Move/Follow.cs`
- `Assets/Neoxider/Scripts/Tools/Move/FreeFlyCameraController.cs`
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/ConstantMover.cs`
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/ConstantRotator.cs`
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/DirectionUtils.cs`
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/IMover.cs`
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/KeyboardMover.cs`
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/MouseMover2D.cs`
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/MouseMover3D.cs`
- `Assets/Neoxider/Scripts/Tools/Move/Neo.Tools.Move.InternalsVisibleTo.cs`
- `Assets/Neoxider/Scripts/Tools/Move/OptionalInputSystemBridge.cs`
- `Assets/Neoxider/Scripts/Tools/Move/PlayerController2DAnimatorDriver.cs`
- `Assets/Neoxider/Scripts/Tools/Move/PlayerController2DPhysics.cs`
- `Assets/Neoxider/Scripts/Tools/Move/PlayerController3DAnimatorDriver.cs`
- `Assets/Neoxider/Scripts/Tools/Move/PlayerController3DPhysics.cs`
- `Assets/Neoxider/Scripts/Tools/Move/ScreenPositioner.cs`
- `Assets/Neoxider/Scripts/Tools/Move/UniversalRotator.cs`

## Findings and fixes

All 31 inventory files (8,453 baseline lines) were read in full. No nested asmdef exists under this
inventory root. Source, tests and documentation below are confined to this entry; no shared config,
prefab/scene/sample, package manifest or another worker's file was changed.

### Completed defect fixes

Evidence below is source tracing and concrete regression scenarios, not claimed Unity execution.
`Regression` means `Assets/Neoxider/Tests/Edit/Tools/Move/Audit20261023NeoToolsMoveRegressionTests.cs`;
`Physics2D` means the adjacent `Audit20261023NeoToolsMovePhysics2DTests.cs`.

| ID / severity | Trigger, source and observable consequence | Completed change / regression |
|---|---|---|
| M01 / high | `PlayerController2DPhysics.UpdateGroundState`: default mask includes the player's own collider. A single `OverlapCircle` treats self/children as ground even with no floor, enabling airborne grounding/coyote refresh. | Query into a reusable list and accept only hits outside the character hierarchy. Preserve the configured layer mask and global trigger policy. `Regression.Controller2D_RejectsOwnCollidersAndStillFindsGroundBehindThem` checks false for self/child, then true for a foreign collider in the same volume. |
| M02 / medium | `KeyboardMover.ApplyDelta`, `MouseMover2D.ApplyDelta`, `MouseMover3D.ApplyStep` discard steps with squared magnitude at most `0.0001`; `FreeFlyCameraController.Tick` discards at most `0.000001`. At 50 Hz, speed `0.1` gives a `0.002` step that never moves. Free-fly minimum speed `0.05` at 240 Hz is also discarded. | Remove those absolute movement cutoffs and keep movement events/state consistent. Mouse click-target completion tests exact zero rather than approximate vector equality, so a small step does not cancel the target. `Regression.KeyboardSmallStep_MovesAndEmitsOneStartAndStop`, `MouseDeltaMode_AppliesSmallPhysicsSteps` and `FreeFly_MinimumSpeedStillMovesAt240Hz`. |
| M03 / medium | `KeyboardMover.MoveDelta` ignores `movementPlane`; `MoveToPoint` writes implicit XY/Z=0. XZ target `(6,8)` from `(10,20,30)` incorrectly becomes `(6,8,0)`. | Map commands to the selected plane and retain its normal coordinate. Preserve the historical immediate Transform-command behavior and automatic-event semantics. Three delta cases and three target cases in `Regression`. |
| M04 / medium | `MouseMover2D.MoveToPoint` and `StepTo` subtract world XY even on XZ/YZ/Z. From `(10,20,30)`, XZ target `(6,8)` becomes `(6,20,18)` and drifts again on repeated commands. `ScreenToWorld` also uses zero camera depth and drops Z, collapsing perspective targets and other planes. | Add the inverse of the existing plane mapping; project the camera ray onto the selected plane through the mover. A parallel ray produces no new target displacement. Six idempotent command cases, an automatic XZ point-mode case and three perspective camera/plane cases in `Regression`. |
| M05 / medium | `UniversalRotator.ComputeDesiredWorldRotation` computes a world 2D angle but treats it as local. Under a parent rotated 90 degrees, aiming right points up. Cached parent conversion also uses the old parent after runtime reparenting, including local limits. | Convert the world 2D aim to local only for limit calculations; use the current parent for conversions. Instant/smooth 2D and reparented 3D local-limit cases in `Regression`. Initial-pose limit baseline is preserved. |
| M06 / medium | `NeoCharacterSprint` has no disable cleanup. Disabling at walk speed 4 / sprint speed 6.8 leaves the walker at 6.8 indefinitely and retains `IsSprinting`. `SetWalkSpeed` promises immediate application outside sprint but only changes the cached value. | Restore walk speed on disable, clear sprint state and emit stop once for an active sprint; apply `SetWalkSpeed` immediately when not sprinting. Two `Regression` cases check speed, state, event counts and a second sprint cycle. |
| M07 / medium | `PlayerController3DPhysics.HandleMovement` normalizes an already magnitude-clamped input. Quarter stick therefore requests full walk/run speed. | Preserve input magnitude while retaining acceleration and vertical velocity. `Regression.Controller3D_QuarterStickProducesQuarterSpeedAndPreservesVerticalVelocity` requests 0.25 at walk speed 8 and expects horizontal speed 2, with vertical speed 3 unchanged. |
| M08 / medium | `CameraConstraint.SetAxisConstraint` changes flags without refreshing bounds. If automatic updates are off, enabling Z retains the previous minZ=maxZ=currentZ cache and clamps to the old depth instead of source bounds. | Recalculate via existing `UpdateBounds`. `Regression.AxisConstraintChange_RecalculatesDepthWithoutAutomaticBoundsUpdates` checks depth bounds 5..15 and clamping from 20 to 15. |
| M09a / medium | `AdvancedForceApplier.Start` passes positive `defaultForce` to `ApplyForce`, bypassing `randomizeForce`. A configured 7..7 range with default 100 applies 100 at startup. | Use the same default/random path as a no-argument public call. `Regression.ForceApplier_StartUsesConfiguredRandomRange` observes queued force magnitude 7. |
| M09b / medium | `AdvancedForceApplier.ComputeDirection`: `transform.forward` and `TransformDirection(Vector3.forward)` are the same direction; the local/world option does nothing. The 2D right-vector branch has the same issue. | World mode uses `Vector3.forward` / `Vector3.right`. `Regression.ForceApplier_WorldForwardDoesNotRotateWithOwner` observes force along world Z on an owner rotated 90 degrees. |
| M09c / medium | `AdvancedForceApplier`: a stationary 2D body, missing target or default custom Z vector falls back to 3D forward; conversion to XY produces no force. | Project 2D directions into XY, fall back to right when necessary and preserve inversion for computed directions. Five `Physics2D` cases observe actual body velocity after an isolated simulation step. Mark **Apply Now** Play Mode-only so Inspector authoring cannot apply runtime physics forces. |
| M10 / medium | `ConstantRotator.RotateRigidbody2D`: an axis with zero Z projection still gets a positive rotation sign, including the explicit `sign == 0 ? 1 : sign` fallback. An XY axis spins a 2D body about Z. | Return without rotation for zero Z projection. Four `Physics2D` cases cover X, Y, +Z and -Z and inspect the resulting body angle. |
| M11 / high | `MouseMover2D` / `MouseMover3D` Awake/Update/FixedUpdate, `CameraRotationController.Update`, and mouse aiming in `UniversalRotator` directly call legacy `Input`. With New-only Active Input Handling these paths throw, preventing movement/aim. `KeyboardMover` also falls into legacy axes when its optional new adapter is unavailable even though legacy is disabled. | Reuse existing `MouseInputCompat` / `KeyInputCompat`; return neutral/no target when pointer data is unavailable. Keyboard automatic fallback uses the existing `NeoInputAvailability` probe. Static inspection confirms no direct legacy Input calls remain in the four pointer components. Device/backend integration remains a coordinator smoke check; no hardware/no-exception-only regression is counted as behavioral coverage. |

### File-by-file coverage

Paths below are relative to `Assets/Neoxider/Scripts/Tools/Move/`. “Reviewed” does not mean compiled or
executed. Unchanged files received full source review as well.

| Inventory file | Reviewed contracts and result |
|---|---|
| `AdvancedForceApplier.cs` | Body selection, startup/random selection, direction projection, force modes, clamping and events; M09a-c. Native speed-cap timing still needs Play Mode verification. |
| `CameraConstraint.cs` | Initialization failure/one-time warning, source types, viewport caches, axis toggles, padding and public queries; M08. Current viewport math assumes XY viewing along Z, now documented. |
| `CameraRotationController.cs` | Drag/modifier gates, axis settings, start/end callbacks and input availability; M11. Broader angle/rate model belongs to proposal P1 below. |
| `CharacterController/NeoCameraInput.cs` | CMF rate contract, external input, inversion, settings lookup, cursor gate, pointer-outlier filter, backend resolution and pause subscriptions. Existing public gates retained. |
| `CharacterController/NeoCharacterCameraBridge.cs` | Explicit/main camera fallback, controller binding, runtime rebinding, warning latch and absent Cinemachine dependency. `LiveCamera` intentionally exposes the assigned reference; the existing public guide explicitly documents null during automatic fallback. |
| `CharacterController/NeoCharacterInput.cs` | External move clamp, held-jump contract, run gate, nullable input reset, optional backend paths and warning latches. Held jump semantics preserved. |
| `CharacterController/NeoCharacterNetworkBinding.cs` | MIRROR/no-Mirror branches, authority lifecycle, local-only objects, Rigidbody kinematic policy and NetworkTransform target/sync direction. No network branch was executed. |
| `CharacterController/NeoCharacterSprint.cs` | Walk-speed ownership, grounded/run gates, smoothing, immediate setter and disable/reset events; M06. |
| `CharacterController/NeoInputAvailability.cs` | Cached legacy probe and unavailable-input exception handling; reused directly for M11. |
| `CharacterController/NeoInputBackend.cs` | Serialized enum order and explicit legacy/prefer-new semantics preserved. |
| `CharacterController/NeoInputBackendResolver.cs` | Availability truth table, explicit legacy selection and safe neutral new-path selection when neither backend works. Existing tests reviewed as context. |
| `CharacterController/NeoLookRate.cs` | Frame delta versus continuous rate, stopped/zero-time guards, outlier threshold and stick preservation. Existing rate tests retained. |
| `CursorLockController.cs` | Static scene-hook reset, cursor stack, lifecycle snapshots, key access, explicit/global opt-outs, driven-player ownership and cleanup. Multi-owner input restoration is a follow-up design concern in P2 below. |
| `DistanceChecker.cs` | Squared thresholds, hysteresis, fixed intervals, source fallback, reactive event threshold and forced checks; existing distance regressions retained. |
| `Follow.cs` | Target-loss events, tag fallback, smoothing, deadzone/stopping/activation distances, position/rotation limits and teleport APIs. No cosmetic refactor or API migration. |
| `FreeFlyCameraController.cs` | Manual Tick, injected input, local/world direction, speed modifiers, look/flight state, cursor snapshot restoration and disable cleanup; M02. |
| `MovementToolkit/ConstantMover.cs` | Transform/3D/2D update loops, missing-body fallback, local/world direction and axis locks; retained. |
| `MovementToolkit/ConstantRotator.cs` | Axis None/custom handling, local/world axis conversion, missing-body fallback and physics angle stepping; M10. |
| `MovementToolkit/DirectionUtils.cs` | Public direction/axis enum coverage, normalization and world-space paths. No new duplicate general utility. |
| `MovementToolkit/IMover.cs` | Public signatures preserved; XML contract now names configured-plane coordinates and per-call units. |
| `MovementToolkit/KeyboardMover.cs` | Input resolution, render velocity/fixed displacement separation, plane mapping, command compatibility, body selection and event thresholds; M02/M03/M11. |
| `MovementToolkit/MouseMover2D.cs` | Every movement mode, pointer sampling, paused delta-time guard, target arrival, plane mapping, body path and events; M02/M04/M11. Rigidbody2D is documented as XY-only. |
| `MovementToolkit/MouseMover3D.cs` | Every movement mode, delta-time guard, optional ground raycast/plane fallback, direct commands, Rigidbody stepping and events; M02/M11. |
| `Neo.Tools.Move.InternalsVisibleTo.cs` | Existing friend assembly `Neo.Editor.Tests` matches the test asmdef; retained. |
| `OptionalInputSystemBridge.cs` | All forwarding methods and key-state names match the existing optional input adapter; retained. |
| `PlayerController2DAnimatorDriver.cs` | Reference resolution/readiness, jump edge, locomotion and blend parameters, optional parameter names and velocity scaling; retained. |
| `PlayerController2DPhysics.cs` | Authority, input capture, run/jump gates, coyote/buffer timing, grounding, movement, follow and teleport contracts; M01. |
| `PlayerController3DAnimatorDriver.cs` | Legacy compatibility, local/world/camera velocity bases, planar speed, blend scaling and animator parameter dispatch; retained. |
| `PlayerController3DPhysics.cs` | Legacy API/serialization, authority, cursor delegation, settings/pause subscriptions, grounding, jump buffers, kinematic guard and movement bases; M07. |
| `ScreenPositioner.cs` | Camera fallback, authoring/runtime updates, retained Z/depth behavior, additive offset and Configure path; retained. |
| `UniversalRotator.cs` | All aim sources, zero direction, instant/smooth updates, local limits, wrap ranges, parent changes and pointer projection; M05/M11. |

### Reuse and compatibility

- Read the `neoxider-tools` tools and transform-extension reference sections before adding the narrow
  inverse plane mapping. Existing `DirectionUtils` handles directions, not world-position/plane-coordinate
  projection. `WorldToPlane` is a private inverse of this component's existing mapping, not a parallel framework.
- Directly reuse `MouseInputCompat`, `KeyInputCompat`, `NeoInputAvailability`, `NeoDiagnostics` and existing
  update/body paths. Ground self filtering follows the already established 3D-controller hierarchy policy,
  using Unity's list query to avoid a fixed-size 2D hit truncation.
- No public declaration, serialized field name, enum order, component menu, prefab reference or assembly
  reference was renamed. `IMover` command timing/event semantics remain historical; only coordinates are fixed.
- No new static runtime state, background work, coroutine, ScriptableObject or scene-only dependency was added.
  Runtime logs remain existing one-time/gated diagnostics. Owned sources have no `GetInstanceID` or
  `objectReferenceInstanceIDValue` calls to migrate. Existing MIRROR/ODIN/UNITY_EDITOR branches require
  central compilation; delimiter checks are not evidence that those branches compile.

Кратко: проверены все 31 C# файла, исправлены воспроизводимые по исходникам ошибки координат,
малого шага, заземления, sprint lifecycle, поворотов, сил и выбора ввода. Unity и тесты не запускались.

## Documentation

Updated the canonical `Assets/Neoxider/Docs/Tools/Move/README.md` with the typed API path, assembly name
and Inspector workflow. Updated the existing MovementToolkit entry and its IMover, KeyboardMover,
MouseMover2D, MouseMover3D and ConstantRotator guides with coordinate units, body support and event semantics.

Updated existing AdvancedForceApplier, CameraConstraint, CameraRotationController, FreeFlyCameraController,
UniversalRotator, PlayerController2DPhysics, PlayerController3DPhysics and NeoCharacterSprint guides.
Changes explain the actual C# API and scene setup rather than leaving generated field-only descriptions.
English style is retained; no duplicate documentation root, DocsEn folder or new package guide was created.

## Substantial improvement proposal

### P1: deterministic movement/rotation math with explicit input units (proposed, not implemented)

**Problem.** The coordinate and small-step defects came from each component defining its own plane
mapping, speed/deadzone rules and movement-state threshold. `IMover` carries Vector2 while 3D adapters
have different body/event semantics. Camera rotation similarly mixes accumulated mouse displacement
with `Time.deltaTime`, and starts from unsigned Euler angles while limits are commonly signed. These
contracts are hard to exercise without scene input and invite repeated fixes across components.

**Architecture/public API.** Add a small ordinary C# math core, keeping components as adapters:

```csharp
public readonly struct MotionPlane { /* ProjectPoint / UnprojectPoint / ProjectDelta */ }
public readonly struct MotionStep { /* WorldDelta, Arrived, HasDisplacement */ }
public static class MotionMath
{
    public static MotionStep StepToPoint(Vector3 position, Vector3 target,
        MotionPlane plane, float speed, float deltaTime, float arrivalDistance);
}
```

The proposed signatures are a design sketch, not existing APIs. Use explicit input samples distinguishing
`FrameDisplacement` from `UnitsPerSecond` / `DegreesPerSecond`. A rotation math counterpart accepts signed
angles, ranges and a supplied delta time. Keep device reading in existing `Neo.Tools.Input` adapters;
keep Transform/Rigidbody writes and UnityEvents in the current components. Avoid a new manager or service locator.

**Compatibility/migration.** Keep all serialized components, enum values and `IMover` signatures. Initially
delegate their internal calculations to the math core with legacy defaults and the documented existing
manual-command/event policies. Add an explicit Vector3 command path for new callers rather than changing
the old interface. Document motion units and zero/negative input policy before any sensitivity migration.
Expose opt-in rate-based drag behavior if correcting legacy camera sensitivity would change existing scenes.

**Acceptance scenarios.** Pure math contracts must cover XY/XZ/YZ and single axes, nonzero normal coordinates,
idempotent target commands, zero speed/delta time, bounded final arrival, no small-step deadlock, analog
magnitude preservation and frame-rate equivalence at 30/60/240 Hz. Wrapper smoke tests must compare
Transform and Rigidbody output, pause/resume, disable/re-enable and documented event transitions.
Camera tests must include an initial -10-degree pose represented as 350 degrees and signed angle limits.

**Effort/risk.** Approximately 3-5 developer days including adapter migration, EditMode math contracts and
physics/input smoke tests. Main risks are public event timing, legacy sensitivity, and physics scheduling;
staged adapter adoption and compatibility defaults bound those risks.

### P2: shared input-gate ownership for nested cursor/UI controllers (proposed, not implemented)

**Problem/source evidence.** `CursorLockController` tracks `_lookSuspendedPlayers` and
`_movementSuspendedPlayers` per controller, while `ReleaseControlInternal` restores its own set even
when it is below another active owner. If two owners reference the same player, the second owner may
see an already disabled gate and record no ownership; releasing the first can re-enable input under
the still-open second UI. Conversely, a locked top owner cannot necessarily undo a lower owner's
suspension. This is a source-traced multi-owner scenario, not an executed reproduction in this audit.

**Architecture/public API.** Introduce a per-player gate registry with scoped ownership, for example
`IDisposable AcquireInputBlock(InputBlockMask mask, object owner)`. Effective movement/look combines
the explicit gameplay gate with outstanding cursor/pause/UI blocks. The cursor stack selects cursor
state; disposable gate handles control input independently. Existing `SetLookEnabled` and
`SetMovementEnabled` remain explicit base gates. Start with legacy players, then provide a thin CMF
adapter through the existing typed input setters.

**Compatibility/migration.** Preserve serialized player lists and cursor presets; internally replace the
per-controller HashSets with owned handles. Do not make a cursor close forcibly enable an explicitly
disabled gameplay gate. Keep standalone/no-Mirror behavior and global opt-outs defined. This needs a
coordinated contract decision for the managers/input/network consumers, so it is separate from the
focused fixes above.

**Acceptance scenarios.** Two overlapping menus sharing one player; top and non-top release; owner
destroy/scene unload; disable gameplay while a menu is open; cursor lock override; global management
off; local/remote authority transition; balanced, idempotent handle disposal and no retained destroyed owners.

**Effort/risk.** Approximately 2-4 developer days plus central scene/network QA. Highest risk is changing
the historic meaning of explicit setters; separate base gates from ownership blocks and migrate with
focused integration tests before adopting it across NoCode workflows.

## Verification and remaining risks

### Actual local checks

- Read and reconciled the exact manifest entry, this report, AGENTS.md and WORKER-CONTRACT.md. Compared
  the current C# root with the manifest: all 31 files accounted for; no nested assembly included.
- Read-only Python validation decoded 63 scoped source/test/doc/report/meta files as UTF-8, rejected
  replacement characters/trailing whitespace, compared public declarations and serialized names for all
  31 sources against HEAD, checked delimiter/preprocessor balance and found no introduced `var` declarations.
  Exit 0. This is static validation, not compilation.
- Two new uniquely prefixed test files contain **39 parameterized/non-parameterized cases**. They inspect
  real Transform/body state, movement events, grounding and queued forces. Physics tests create/close a
  temporary isolated Physics2D scene when the coordinator runs them; no scene assets are generated.
- Both new `.meta` GUIDs matched the 32-hex format; repository metadata search found each exactly once.
  The existing `Neo.Editor.Tests` asmdef already references `Neo.Tools.Move`, CMF and editor test APIs.
  No shared asmdef change is required by these regressions.
- Scoped `git diff --check -- <inventory, owned docs, report, prefixed tests/metas>` returned exit 0.
  Git only reported existing LF-to-CRLF conversion notices for four modified player-controller text files;
  no text normalization was performed. A final UTF-8/whitespace pass covered all 63 scoped text files,
  relative-link validation covered all 27 owned Markdown guides, and both new test files passed brace,
  explicit-type and metadata checks. All returned exit 0; four migrated pointer components have zero
  direct `Input.` calls. These checks do not execute the regression cases.

### Coordinator verification / unresolved risks

- **Not run:** Unity import/compilation, test runners, builds, Play Mode, console inspection or visual QA.
  The user/worker contract reserves all of these for the coordinator. No compile/test success is claimed.
  Local QA is source/static review, not independent agent QA; child agents were prohibited.
- Run both new fixtures plus existing `TransformMoveTests`, `CharacterControllerInputTests`,
  `FreeFlyCameraControllerTests`, `CursorLockPlayerControlTests`, diagnostics defaults and Inspector button
  coverage. In particular, confirm local Physics2D scene creation/simulation/cleanup in the central Unity version.
- Compile supported Unity 6.0-6.4 and 6.5+ branches with the project's actual Mirror/optional integrations.
  Verify pointer movers and camera drag/aim with New-only and Both input handling, missing devices,
  pause/unpause and enabled/disabled components. Those backend scenarios need real integration validation.
- Check legacy analog motion and 2D self-filtering in actual prefabs with their ground masks, triggers,
  acceleration, animation and camera hierarchy. Also check native force clamping after physics integration.
- No code/docs/test edits outside ownership are needed for the completed fixes. P2 requires a later
  shared input-gating decision with relevant module owners; it is not implemented or presented as fixed.
