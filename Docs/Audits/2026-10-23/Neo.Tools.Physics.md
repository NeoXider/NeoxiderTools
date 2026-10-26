# Neo.Tools.Physics: audit

Status: source audit, focused fixes, regressions, documentation, and static validation complete. Unity compilation and test execution remain centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Physics/Neo.Tools.Physics.asmdef`.
- Scope: 3 C# files; nested assemblies excluded.

## Scope

All three manifest-owned C# files were read completely. Inventory was checked against the nearest asmdef; no nested assembly source was included.

| Source under `Assets/Neoxider/Scripts/Tools/Physics/` | Review coverage |
|---|---|
| `ExplosiveForce.cs` | All activation modes, delayed coroutine, repeated/manual calls and reset, force/randomness/falloff, zero radius and center direction, compound/self colliders, optional Rigidbody creation, UnityEvents, destruction, gizmos, Inspector buttons. |
| `ImpulseZone.cs` | Required collider and trigger authoring, trigger/manual API parity, layer/tag filtering, per-body history and cooldown lifecycle, destroyed bodies, callback reentry/destruction, all direction modes, optional Rigidbody creation, reset and gizmos. |
| `MagneticField.cs` | Update/FixedUpdate and interval timing, all modes and setters, local/world directions, toggle phases, overlap capacity/allocation, body cache ownership, compound/self colliders, membership and disable/re-enable events, callback changes, gizmos. |

Also inspected the owned asmdef, four canonical Physics docs, existing `PhysicsToolsTests.cs`, test assembly references, and the `ButtonAttribute`/drawer Play Mode gate as dependency context. There are no ScriptableObjects, NoCode action types, subscriptions to external services, scene-search dependencies, direct runtime logs, editor imports, obsolete Unity object-ID calls, or per-version conditional branches in this assembly. The components themselves provide the Inspector/UnityEvent layer over public C# methods.

Reuse receipt: consulted `neoxider-tools/references/tools.md` (existing Physics components) and `references/extensions.md` (collections/coroutine helpers). Existing component APIs and Unity force/query APIs were adapted. No parallel physics framework or new runtime helper module was introduced; deadline bookkeeping and body identity remain private component state. The existing delayed coroutine was reused.

Проверены все три исходника; исправления сохраняют публичные сигнатуры и сериализованные поля. Проверки Unity не запускались согласно контракту работника.

## Findings and fixes

### F1 — High: Delayed explosions never activate

**Trigger/consequence:** configure the documented grenade workflow with `ActivationMode.Delayed`. Neither `Awake` nor `Start` handled this enum value, so no coroutine or explosion was started and the grenade remained inert.

**Evidence/fix:** baseline `Start` checked only `OnStart`. `ExplosiveForce.cs:126` now routes `Delayed` through the existing delay/immediate path. Non-manual repeat protection is preserved. Regression: `DelayedActivation_WithZeroDelay_DetonatesOnStartOnce` checks one event, `HasExploded`, and the target's accumulated impulse. Positive-delay scheduling still needs central Play Mode verification.

### F2 — Medium: collider multiplicity changes force and notification counts

**Trigger/consequence:** one Rigidbody has multiple colliders, on its own GameObject or children. Both overlap loops submitted a force for every collider. A three-collider body received three impulses/forces; explosion events also repeated. Magnetic membership deduplicated GameObjects but its force application did not. Results depended on prefab collider count and child positions.

**Evidence/fix:** `ExplosiveForce.cs:178` and `MagneticField.cs:366` now deduplicate by attached Rigidbody and use its world center of mass for direction/custom falloff. Explosion emits one affected event with a representative collider GameObject. Magnetic membership retains its existing per-GameObject identity while force is once per body. Both exclude the emitter's own attached Rigidbody, including child colliders that previously pushed the emitter itself.

**Regressions:** compound explosion impulse/event count; magnetic compound force plus two distinct collider-object membership entries; separate self-body tests for explosion and magnet. No public fields, serialized names, or event payload types changed. Applications relying on repeated compound-collider force/event multiplication must adjust; docs describe the corrected behavior.

### F3 — Medium: explosion falloff is applied twice

**Trigger/consequence:** `ForceMode.AddExplosionForce` first called `CalculateForceAtDistance`, then passed that reduced value and the nonzero radius to Unity, which attenuated it again. `Linear` therefore did not produce the documented linear custom falloff.

**Evidence/fix:** `ExplosiveForce.cs:220` passes zero for Unity's explosion radius after applying custom falloff; the overlap query/custom calculation still enforce the configured radius. Unity documents that a zero native radius disables its additional attenuation while retaining its force application behavior. See [Unity AddExplosionForce](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody.AddExplosionForce.html).

For a 20-unit blast, radius 4, body center distance 2 and a centered unit box, custom linear/quadratic impulses should be 10/15. The old native surface-distance factor was `1 - 1.5/4`, yielding 6.25/9.375. These are source calculations, not executed Unity results. Four regression cases check actual accumulated impulse for both application modes and both falloffs. Correcting attenuation can increase existing blast strength; this migration effect is documented.

### F4 — High on Unity Mono collection profiles: cooldown enumeration mutates its dictionary

**Trigger/consequence:** any active cooldown enters `ImpulseZone.Update`, which replaced dictionary values inside `foreach` over the same dictionary. Unity's Mono/.NET Framework collection profile invalidates that enumeration on value replacement, so cooldown maintenance can throw instead of completing; other runtime profiles should not be relied on to make this mutation safe.

**Evidence/fix:** `ImpulseZone.cs:82` now stores absolute scaled-time deadlines, collects expired/destroyed keys into a reusable list, and removes them after enumeration. No dictionary value is changed during its traversal. This also removes the previous per-frame temporary-list allocation and lets cooldown time elapse while the zone is disabled. A regression creates two independent cooldowns, dispatches maintenance, then checks that neither body receives a second impulse/event. Runtime exception behavior remains to be confirmed by the central Unity run.

### F5 — Medium: manual impulses and cooldown expiry bypass repeat limits

**Trigger/consequence:** `ApplyImpulseToObject` bypassed the one-shot/cooldown checks found only in `OnTriggerEnter`. Separate child colliders bypassed collider-keyed history. Additionally, cooldown cleanup removed one-shot history, contradicting the documented once-until-reset contract.

**Evidence/fix:** `ImpulseZone.cs:178` centralizes eligibility and application for both entry paths. History/deadlines use Rigidbody identity. Expired cooldowns never remove live one-shot history; destroyed history entries are pruned. `ClearProcessedObjects` clears both histories. Regressions exercise manual repeats across child colliders with one-shot, cooldown, and both options, observe impulse/event counts, then verify reset permits another impulse. Scaled-time expiry and one-shot-after-expiry still need central Play Mode timing coverage.

### F6 — Medium: enter callbacks can double-apply or destroy impulse targets

**Trigger/consequence:** an `OnObjectEntered` listener calls the manual impulse API, then the outer trigger call applies again; or it destroys the target Rigidbody/collider before the outer call dereferences it.

**Evidence/fix:** `ImpulseZone.cs:203` reserves a body's in-progress call with `try/finally` cleanup and rechecks the target after callbacks. Existing trigger-only enter notification semantics, including entry for a filtered-in object lacking a Rigidbody when auto-add is off, are retained. Regressions observe exactly one impulse/event on reentry and no applied event after target-body destruction.

### F7 — Medium: magnetic cache can remain bound to a dead or previous Rigidbody

**Trigger/consequence:** replace a Rigidbody, or reparent its collider to another Rigidbody, without leaving the field. The old cache returned its previous value unconditionally: a destroyed reference suppressed force indefinitely; a live previous body kept receiving another object's force.

**Evidence/fix:** `MagneticField.cs:449` validates the cached body against the collider's current attached body and refreshes/removes stale entries. Two regressions check force on a replacement body and transfer of force after collider reparenting, without an exit/entry reset.

### F8 — Medium: disabling a field leaves stale membership and permits callback-time force

**Trigger/consequence:** disable or pool an active magnet. With no disable cleanup, `ObjectsInFieldCount` retained old objects/caches, exits were absent, and re-enable did not report fresh entries. An enter listener disabling the component also failed to stop the currently running force pass.

**Evidence/fix:** `MagneticField.cs:163` snapshots exits, clears membership/caches/buffer references, and resets the sampling deadline. `UpdateField` checks enabled state before querying and after entry callbacks, and validates targets after callbacks. Exit iteration tolerates cleanup performed from a listener. Regressions observe membership/exits and fresh re-entry, zero force after entry-time disable, and exactly-once remaining exits after exit-time disable. A nonpositive serialized buffer size now compares against its clamped allocation size, avoiding repeated buffer reallocation.

### F9 — Medium: explosion Inspector button was enabled outside Play Mode

**Trigger/consequence:** clicking **Explode Now** in Edit Mode could add rigidbodies, enqueue forces, run arbitrary UnityEvent listeners, and request destruction of authored objects.

**Evidence/fix:** `ExplosiveForce.cs:297` uses the package's existing `[Button(..., PlayModeOnly = true)]` gate. No public runtime method was removed or gated, and no custom editor was added. Verified statically against `ButtonAttribute.PlayModeOnly` and the existing drawer gate; Inspector interaction remains centrally pending.

## Documentation

Updated only these owned existing English entry/guide files:

- `Assets/Neoxider/Docs/Tools/Physics/README.md`: canonical assembly/namespace entry, C# calls, Inspector/UnityEvent setup, dynamic 3D target requirements, trigger setup, per-body force versus per-object membership semantics.
- `ExplosiveForce.md`: automatic/manual activation, scaled delay, force override/reset behavior, exact falloff/application semantics, self/compound handling, corrected-force migration, Play Mode button, pooled scheduling limits.
- `ImpulseZone.md`: manual/trigger eligibility, per-body one-shot and deadlines, reset, event order/payload, callback reentry, child filtering and explicit migration guidance.
- `MagneticField.md`: complete public setters/state, toggle direction semantics, query capacity/timing/default-scene limits, cache refresh, disable/exits and re-enable behavior.

Fixed each component's local backlink to the Physics entry and the entry's broken table row. No root/package docs, shared configs, asmdefs, samples, scenes, or prefabs were edited.

## New regression coverage

`Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsPhysicsRegressionTests.cs` declares **20 NUnit cases**. All observe accumulated force/impulse, events, or public membership state; none rely solely on absence of an exception. Inspector configuration uses `SerializedObject`; lifecycle/trigger messages are dispatched explicitly for Edit Mode. There is no reflection into private runtime collections and no source-text assertion in these regressions.

The fixture is nonparallel, owns/destroys its temporary hierarchy, uses layer 31 at a remote position, and does not simulate physics or change global physics settings. Force observations use Unity's public [GetAccumulatedForce](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody.GetAccumulatedForce.html); impulse assertions convert by `Time.fixedDeltaTime`. Its new `.meta` has a unique GUID. The existing `Neo.Editor.Tests` asmdef already references `Neo.Tools.Physics`; no coordinator reference change is needed. **Tests were authored, not run.**

## Substantial improvement proposal

### Separate magnetic discovery cadence from force integration (not implemented)

**Problem:** `updateInterval` currently throttles both the overlap query and `AddForce`. At a 0.02-second fixed step, a strength-10 field sampled only every 0.1 seconds submits about one fifth of the full-rate total impulse. Selecting `Update` can instead make the total depend on render callback count. The field's advertised strength therefore depends on a performance setting. Current behavior is now documented; changing every existing asset's physical tuning silently would be incompatible.

**Proposed design/public API:** retain the component as a scene adapter but split `RefreshTargets()` from `ApplyCachedForces()`. Introduce an opt-in `MagneticForceCadence { LegacySampled = 0, Continuous = 1 }`, an `AutomaticUpdate` option, and a public `Step(float deltaTime)` for explicit deterministic driving. In continuous mode, overlap discovery uses the configured interval, while each physics step evaluates forces for valid cached bodies. Manual stepping must have one driver and must not double-apply with automatic updates. The registry must prune destroyed/reparented bodies and preserve the current membership/event identity contract.

Extract the reusable calculation into a small C# `MagneticForceSettings` value type and `MagneticForceEvaluator.Evaluate(settings, emitterPosition, bodyPosition, targetPosition, direction, reversePhase)` returning a force vector. It should depend only on value data, normalize/clamp finite input once, and be testable without a scene. Rigidbody/query ownership stays in the adapter. Do not add a singleton, service locator, or scene references in ScriptableObjects.

**Compatibility/migration:** keep existing serialized names, enums, methods, UnityEvents and the default `LegacySampled = 0`, so existing scenes retain their current force tuning. Document opting into continuous mode and retuning legacy interval-compensated strengths. `Step` is additive. Reuse the current layer/body filtering and disable cleanup; no asset rewrite or asmdef split is required for the first version.

**Acceptance scenarios:** for a mass-1 body and constant strength 10, continuous mode produces approximately the same one-second velocity change with query intervals 0/0.1 and physics steps 0.01/0.02, independently of 30/60/120 render FPS; repeated colliders never multiply force; stale cached targets are removed by the next refresh; pause advances neither scaled phase time nor force integration; disable emits exits and clears membership; a manual driver with automatic updates off produces the same result; old serialized assets retain legacy results. Pure evaluator tests cover all modes/falloffs, zero radius/direction, and nonfinite input policy. Engine integration/performance checks validate the actual velocity and query count.

**Effort/risk:** roughly 2–4 development days including value-core tests, timing integration tests, docs, and profiling. Main risks are force retuning on migration, stale-membership latency between discoveries, and double integration from multiple drivers. Opt-in cadence plus explicit driver ownership contains those risks. A separate future saturation policy can expose incomplete queries or grow a bounded buffer; it should not be hidden inside the force calculation.

## Verification and remaining risks

Actual local validation performed:

1. Inline read-only Python static audit matched **3/3** manifest C# files to the nearest asmdef. Compared against `ca198556`: all **37 serialized private field declarations** and **40 public declarations** were preserved (11/13 explosion, 8/7 impulse, 18/20 magnetic).
2. Checked UTF-8 decoding, replacement/NUL markers, C# delimiter balance, absence of `var` and obsolete object-ID APIs, all relative links in the four owned guides, new-file trailing whitespace, and test-case count. Nine source/docs/test/metadata files passed before this report was finalized.
3. Searched all `Assets/**/*.meta` for the generated GUID: exactly one occurrence. Checked existing test assembly JSON: `Neo.Tools.Physics` reference present.
4. Reviewed the scoped source diff and performed the source-only numerical falloff calculation recorded above. This is not a C# compilation or physics simulation result.
5. `git diff --check -- <owned source/docs, new tests/meta, this report>` passed with exit code 0 and no output. Final UTF-8/newline/trailing-whitespace/relative-link checks passed for all **10 owned changed/new files**, including the report and untracked tests/meta. Scoped Git status showed only the three runtime files, four guides, this report, and the uniquely prefixed test/meta pair for this worker.

Central checks still required: compile/import on supported Unity versions (including 6.5+), run `Audit20261023NeoToolsPhysicsRegressionTests` and existing `PhysicsToolsTests`, confirm positive delayed scheduling and cooldown expiry/one-shot persistence in Play Mode, real trigger contact delivery and optional Rigidbody creation, Inspector Play Mode button gating, and a quick scene disable/re-enable smoke check. The test fixture dispatches lifecycle messages directly and does not prove engine scheduling/contact delivery. Open-scene colliders on layer 31 at the fixture's remote location could interfere; central execution should use a clean test scene.

Remaining documented limits: default physics scene only; fixed-capacity overlap results can omit targets and churn membership at saturation; legacy magnetic timing is sample-dependent (proposal above); toggle timing coalesces long gaps; pooled automatic explosions do not rearm themselves. Tag configuration must name a defined Unity tag, and physics setter/vector inputs are expected to be finite. Centered radial directions retain the existing random fallback. Native kinematic bodies can be reported but do not move. Arbitrarily recursive event chains across different components are not globally coordinated.

No cross-boundary implementation change or shared-assembly reference edit is requested. Coordinator action is the central verification above. No commits, Unity/MCP operations, builds, test runners, installations, shared services, or child agents were used.
