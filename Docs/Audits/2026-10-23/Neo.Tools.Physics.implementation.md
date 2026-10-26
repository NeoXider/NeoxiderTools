# Neo.Tools.Physics — implementation receipt

Status: **source-final for C-01, C-02, RootPhysicsEmitterReview and PHYSICS-NATIVE-CALLBACK-ADMISSION (E46/L); corrected fixture UNCOMPILED / UNEXECUTED**.

Updated: 2026-10-25. Target: `demo` / package `11.0.0`. Original audit baseline: `ca198556`. Continued the original audit/plan session and preserved its coherent earlier patches. Historical snapshot results below do not prove the newly corrected fixture.

## Snapshot3 E46/L correction

Read the exact Physics cases in `Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml`: 38 cases, 18 passed, 20 failed with the exact native message `Assertion failed on expression: 'ShouldRunBehaviour()'`, at the manual `SendMessage` drivers. The action index assigns E46 to this owner as a confirmed fixture/precondition failure, action L. `21-full-play-graphics.xml` contains zero cases from this fixture; that is absence of Play discovery, not a Play pass.

Evidence SHA-256: Edit XML `7c6d24e11012ed5903fff8f24d603e8130ec4bcae262af3761f4cf772a6e57d2`; Play XML `2b15216472bdc287418567ed947899346c6a134b33658b07533fa9a1d157dc3a`.

This round changes only `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsPhysicsRegressionTests.cs` and this receipt. The primary relocation map was reread before each edit; neither file is reserved. No helper was moved/extracted, no shared caller/asmdef was edited, and no reserved work is deferred for this module. Production code, public docs, settings, GUIDs and historical audit/plan remain unchanged in this correction.

- Top-level `[UnitySetUp]` yields `EnterPlayMode` before creating owned objects. Top-level `[UnityTearDown]` destroys only the owned hierarchy and yields `ExitPlayMode` for the successfully acquired Play session. The installed `EnterPlayMode` instruction refuses an existing Play session; ownership is recorded after success. No nested Enter/Exit command, foreign scene/session reset or shared physics-setting mutation is used.
- Removed all manual `Start`, `Update`, `FixedUpdate` and `OnDisable` dispatch. Native frame/fixed-time progression is observed with an unscaled five-second failure bound, not inferred from yielding one Editor iteration. Automatic fixed-step physics, positive scaled time, trigger shape/body state and an allowed layer collision pair are explicit preconditions; the fixture asserts these instead of changing host settings.
- `DelayedActivation` now observes native Start and a real integrated 10-unit impulse, then calls the existing public `Explode()` again to retain the once-only oracle. Cooldown maintenance observes both initial public impulses, native frame/physics advancement and both independent cooldowns remaining effective.
- Magnetic cases use genuine scheduled passes, native disable/re-enable and valid 0.1-second toggle durations. The mode-destruction case uses the existing nonnegative query-interval setting to defer discovery until its timed mode callback; it retains zero entries/forces and all four destruction/update-path combinations without a zero-duration phase.
- Native physics clears accumulated force each step. Magnetic force oracles therefore use `mass * deltaVelocity / elapsedFixedTime`, retaining force 10, independent-body ownership, compound deduplication, both successive contributions, cumulative impulse, exact membership/entry/exit counts and the existing 0.001 vector tolerance. The self-exclusion negative now includes an independent positively driven body; own-body entry count and motion remain zero.
- The reentrant impulse case now creates a real trigger contact, asserts its target payload, one enter/one applied event and a native 12-unit integrated impulse. Two additional layer/tag cases first prove the allowed native hit, then retain an overlapping rejected target with zero force/events. One additional cooldown-expiry case uses a repeatable positive control, verifies one-shot remains consumed, and observes 24 total impulse only after explicit reset.
- The three immediate-destruction `ImpulseZone` cases retain their explicit `OnTriggerEnter` message driver **inside admitted Play** to exercise synchronous callback liveness outside a PhysX contact step. They assert actual component/GameObject/body ownership, zero impulse and no applied event. They are callback-logic controls, not claimed native contact evidence; real contact delivery/reentry/filtering is covered separately. No private reflection, expected-error whitelist, ExecuteAlways or ignored case body was introduced.

All previous 38 input combinations remain; the revised source expects **41 cases** after adding two filter cases and one expiry/reset case. Coroutine boolean combinations use the installed framework's supported `UnityTest`/`ValueSource` combinatorial path. Actual discovery and every revised result remain pending a new global freeze.

## Implemented actions and exact files

| Action | File | Completed source/documentation change |
|---|---|---|
| C-01 | `Assets/Neoxider/Scripts/Tools/Physics/ImpulseZone.cs` | After `OnObjectEntered`, `ApplyImpulse` checks `this == null` together with the target collider/body checks before `CalculateDirection` or force submission. The existing `finally` still releases the in-progress body reservation. No enabled-state guard was added to the manual API. |
| C-01, root followup and E46/L | `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsPhysicsRegressionTests.cs` | Preserved the original 20, three C-01 and 15 root-followup combinations; corrected native admission/drivers and added three native filtering/expiry cases. Expected total 41; discovery is unexecuted. |
| C-01 | `Assets/Neoxider/Docs/Tools/Physics/ImpulseZone.md` | Documents cancellation after immediate enter-listener destruction and continued support for explicit calls on live disabled components. Existing filtering/history/event-order and scene instructions remain. |
| C-02 | `Assets/Neoxider/Docs/Tools/Physics/README.md` | Qualifies center-of-mass direction: it applies to the `AddForce` explosion mode and magnetic evaluation; native `AddExplosionForce` delegates direction/application to Unity's closest-surface behavior. Custom explosion falloff uses center of mass in both modes. Existing component/backlink navigation remains intact. |
| RootPhysicsEmitterReview | `Assets/Neoxider/Scripts/Tools/Physics/ExplosiveForce.cs` | Adds Unity-null fences at explicit entry and after `OnObjectAffected`/`OnExplode`. Immediate emitter destruction stops the old body loop/completion/cleanup continuation, retaining already submitted impulses. Live disabled explicit calls still work. |
| RootPhysicsEmitterReview | `Assets/Neoxider/Scripts/Tools/Physics/MagneticField.cs` | Checks Unity-null before `isActiveAndEnabled` at field-pass entry and after enter listeners. Entry also protects the continuation after a mode listener destroys the field. Existing `OnDisable` implementation is unchanged. |
| RootPhysicsEmitterReview | `Assets/Neoxider/Docs/Tools/Physics/ExplosiveForce.md`, `Assets/Neoxider/Docs/Tools/Physics/MagneticField.md` | Documents immediate callback destruction, committed force preservation, completion cleanup cancellation, live-disabled explicit explosion and unchanged deferred-destruction timing. |

No additive public API, serialized field, assembly reference, asset GUID, scene/prefab or sample-layout change was introduced in this continuation. Existing test metadata remains associated with the same source file. The original audit and individual plan were not edited.

## Authored acceptance — not executed

The original 38 combinations remain within the expected 41-case corrected fixture. The previous C-01 additions retain these observations:

- `TriggerCallback_DestroyedEmitter_DoesNotApplyImpulse(false)`: an enter listener immediately destroys the zone component; target body/collider survive, entry count is one, applied count is zero, accumulated impulse is zero.
- `TriggerCallback_DestroyedEmitter_DoesNotApplyImpulse(true)`: the same observations after destroying the zone GameObject, independently of the target hierarchy.
- `ManualImpulse_DisabledLiveEmitter_RemainsSupported`: a live disabled zone accepts an explicit filtered manual call and submits exactly the configured upward impulse of 12, with one applied notification.

The root followup's 15 combinations retain these oracles, now with native magnetic scheduling:

| Test method | Cases | Authored observable acceptance |
|---|---|---|
| `Explosion_AffectedListenerDestroysEmitter_StopsAfterCommittedImpulse` | 2: component / GameObject | Two independent bodies and native `AddExplosionForce`; exactly one affected notification, no completion, the notified body's impulse remains 10, the other body stays at zero. Query order is not assumed. |
| `Explosion_CompletionListenerDestroysEmitter_SkipsAutomaticCleanup` | 4: component / GameObject × destroy delay 0 / 0.25 | Both bodies retain their opposite 10-unit impulses; affected/affected/complete order remains exact. Callback destruction cancels the old automatic cleanup; component-only destruction leaves the emitter GameObject alive. |
| `Explosion_LiveExplicitCall_AffectsBothBodiesBeforeCompletion` | 2: active / live disabled | Both bodies receive opposite 10-unit impulses before one completion, and `HasExploded` is set. No new enabled-state restriction. |
| `MagneticField_EntryListenerDestroysEmitter_StopsBeforeAnyForce` | 2: component / GameObject | Two independent bodies; exactly one native enter callback, both velocities stay zero after physics advancement, no old second-body entry/force tail. |
| `MagneticField_ModeListenerDestroysEmitter_DoesNotDiscoverOrPush` | 4: component / GameObject × Update / FixedUpdate | One timed `OnModeChanged` callback from the selected native update path, zero entries and zero body motion; valid duration with discovery intentionally deferred by the existing interval. |
| `MagneticField_OrdinaryCycle_PushesBothBodiesWithoutRepeatingEntries` | 1 | Two native progression windows each contribute force 10 on both bodies; cumulative impulse equals force × total fixed time. Entries/membership remain exactly two throughout. |

These authored observations use accumulated force for synchronous public API calls and integrated velocity for actual physics windows, together with exact event/payload checks. UnityEvent listeners perform real immediate component/GameObject destruction. The earlier snapshot's 18 passes are historical only; none of the corrected 41 expected cases has been compiled or run in this source round. Queued/deferred `Destroy` remains separate acceptance work.

The fixture contains no `[NonParallelizable]` attribute. It relies on the coordinator's isolated/exclusive runner and its existing exact hierarchy cleanup. No unavailable NUnit annotation or shared runner configuration was added.

## Compatibility and preserved implementation

The narrow C-01 change cancels a pending push once the emitter is actually Unity-null, instead of dereferencing its destroyed `transform`; it does not reject an explicit manual call solely because the live component is disabled. C-02 changes documentation only, not force math or numerical regressions.

The root followup preserves already submitted force instead of rolling it back. Immediate destruction during an affected callback cancels later targets and completion; destruction during completion cancels the old cleanup. Masks, force modes, radii, defaults, serialized fields, public signatures, live-disabled manual explosion behavior and normal event order are unchanged. The existing live-emitter `Destroy(gameObject, destroyDelay)`/`Destroy(gameObject)` branches remain untouched. No pending-destruction flag or generation framework was added, so merely queuing `Destroy` is not treated as immediate Unity-null.

Earlier F1–F9 remain in the current source: delayed activation, per-body force/history, single custom explosion attenuation, deadline maintenance, callback reservation, magnetic cache/disable cleanup and Inspector gating. Their intentional 11.0 behavior changes remain documented: corrected native-explosion attenuation can increase blast strength; compound forces/affected notifications no longer multiply; manual impulses obey repeat limits; magnetic disable emits exits and clears membership. No earlier test pass is claimed for this current source.

## Proposal and related source review

The substantial continuous-cadence/value-evaluator proposal remains in `Neo.Tools.Physics.plan.md`; it was not implemented or silently made the new default. Saturated overlap handling and same-context discovery sharing also remain optional proposals. Every active field still performs its own query and retains field-specific force/membership. Reused buffers are mutable working storage, not immutable snapshots; completeness/performance claims require explicit saturation handling and later query-count/velocity evidence.

Root's confirmed residual `ExplosiveForce` continuation accesses are now fenced, with the public-event controls above authored. The exact old destroyed-Behaviour `isActiveAndEnabled` getter runtime outcome remains **UNVERIFIED**; this receipt does not claim a reproduced exception or depend on that getter returning a particular value. Magnetic checks now short-circuit on Unity-null before accessing it. Existing disable/target-destruction observations remain separate from emitter-destruction controls; no general callback-generation or capacity redesign was introduced.

## Actual work, dependencies and evidence debt

Performed small UTF-8 Python/source/XML/JSON reads, source/reference review and SHA-256 artifact bookkeeping through `cmd.exe`. Read the installed framework's `UnityTestAttribute` and `EnterPlayMode` source for supported case/admission behavior. No Unity, compiler, tests, product checker/validator, installer, service, commit, push or child agent was launched in this correction. No shared tracker/configuration or another owner's source/test/report was changed.

No additive API or reserved-file dependency blocks this correction. After **all** writers finish and root declares a new freeze, the coordinator must compile on Unity 6.3 and 6.6, discover all 41 expected cases, rerun the complete fixture and retain every positive/negative/count/ownership/payload oracle. Native frame/physics admission, successful Play exit/cleanup, all revised callback controls and filter/expiry positives remain UNVERIFIED on this source. Then verify delayed cleanup with a live emitter, remaining body-creation/Inspector capabilities and global MIRROR/version requirements. Root owns final integration and any eventual push after thorough verification.

Current continuation artifacts (bookkeeping only):

| Path | SHA-256 |
|---|---|
| `Assets/Neoxider/Scripts/Tools/Physics/ImpulseZone.cs` | `1e5f6c128be08a17aead88d1e69a56c572b8eec5778af52c0bbdf310de4637e7` |
| `Assets/Neoxider/Scripts/Tools/Physics/ExplosiveForce.cs` | `6bdccab0266c69320bd7af150bd8340d7226459af30ee779bea11de8e27cab45` |
| `Assets/Neoxider/Scripts/Tools/Physics/MagneticField.cs` | `83b08621cd01af8e41f6d6e20a9a8780309bde8e674d6ffb2cf07b56ea011af2` |
| `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsPhysicsRegressionTests.cs` | `c945c2a7a22dbe67b3b4a6265c3b9b0a3f1db18b74da121465e0515e2d50b6c7` |
| `Assets/Neoxider/Docs/Tools/Physics/README.md` | `be83c82c693c6889773acbdddabe1db5389bd07b97808cfeff66aa2c3a02e9b1` |
| `Assets/Neoxider/Docs/Tools/Physics/ImpulseZone.md` | `1ce4e4ef74ba364c9da7cf8751f37312131b62eaae5309dd2ce10f45ff04f83c` |
| `Assets/Neoxider/Docs/Tools/Physics/ExplosiveForce.md` | `24bb198205a9699bbdc38bc43ce841ed0d5241b3708a314584b8a85ce63b8996` |
| `Assets/Neoxider/Docs/Tools/Physics/MagneticField.md` | `f7a835d2b4f781c1f86393e6a41f44eb1701a9b76878549c0f2aa2faef96d701` |
