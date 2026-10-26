# Tests-Edit-Utilities: additional deep audit

Status: supplemental static audit complete; Unity compilation and execution remain reserved for the coordinator.

Model `gpt-6.1-sol`, reasoning `max`.

## Assignment

Test assembly helper: listed existing tests are READ-ONLY; primary assembly owner may modify them. Audit deterministic assertions, lifecycle/cleanup, bounds and skip quality; add only meaningful NEW tests Audit20261023TestUtilities*.cs. Runtime source is owned by module agents; report concrete repros for coordinator. Own public docs only Assets/Neoxider/Docs/Testing/Utilities.md.

## Files

- `Assets/Neoxider/Tests/Edit/Audio/AmChannelEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/Audio/AmEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/Audio/AmEntryEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/Audio/AudioEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/Audio/AudioFocusMuteEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/Audio/RandomMusicControllerEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/Dialogue/DialogueControllerEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/Editor/CreateMenuObjectLegacyTests.cs`
- `Assets/Neoxider/Tests/Edit/Editor/NeoxiderCreateMenuTests.cs`
- `Assets/Neoxider/Tests/Edit/Editor/SampleAssetValidationTests.cs`
- `Assets/Neoxider/Tests/Edit/Editor/SampleSceneCoverageTests.cs`
- `Assets/Neoxider/Tests/Edit/Editor/SceneSaverAutoSaveTests.cs`
- `Assets/Neoxider/Tests/Edit/Extensions/ExtensionsLifecycleTests.cs`
- `Assets/Neoxider/Tests/Edit/Extensions/ExtensionsTests.cs`
- `Assets/Neoxider/Tests/Edit/Extensions/UnityExtensionsTests.cs`
- `Assets/Neoxider/Tests/Edit/PropertyAttribute/InspectorButtonCoverageTests.cs`
- `Assets/Neoxider/Tests/Edit/PropertyAttribute/InspectorControlIdStabilityTests.cs`
- `Assets/Neoxider/Tests/Edit/PropertyAttribute/NeoComponentHealthTests.cs`
- `Assets/Neoxider/Tests/Edit/PropertyAttribute/NeoInspectorAnimationTests.cs`
- `Assets/Neoxider/Tests/Edit/PropertyAttribute/PropertyAttributeEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/AuditFixesAttackSystemTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/AuditFixesDrawerTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/AuditFixesInteractiveObjectTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/AuditFixesSelectorTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/AuditFixesSpawnerTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/AuditFixesTimeTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/AuditFixesTypewriterTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/BillboardUniversalEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/CharacterControllerInputTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/CursorLockPlayerControlTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/Debug/DebugToolsBehaviorTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/FakeLeaderboardBehaviorTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/FreeFlyCameraControllerTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/ImageFillAmountAnimatorTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/InteractionQueryMathTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/InteractiveObjectAimSymmetryTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/InteractiveObjectMouseTrigger2DTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/InteractiveObjectTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/Inventory/InventorySystemTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/Move/TransformMoveTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/MoverPrefabColliderOffsetTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/NeoConditionTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/NeoConditionThrottleTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/Other/OtherToolsBehaviorTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/PhysicsToolsTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/Random/ChanceManagerTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/SelectorTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/SpawnerDenyZoneTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/SpawnerTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/TimerTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/ToolsComponentsTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/ToolsDiagnosticsDefaultsTests.cs`
- `Assets/Neoxider/Tests/Edit/Tools/TrustedClockTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/AnimationFlyEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/AuditFixesAnimationFlyTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/ButtonChangePageTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/ButtonScaleTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/CategoryBarTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/SetTextTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/TimeToTextTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/UIMeshRigEditorTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/UIMeshRigGeometryTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/UIMeshRigMotionTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/UIMeshRigSpriteRendererTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/UIMeshRigTests.cs`
- `Assets/Neoxider/Tests/Edit/UI/VisualToggleEditModeTests.cs`
- `Assets/Neoxider/Tests/Edit/View/AnimationsTests.cs`


## Findings, fixes and regression checks

### Scope and method

Reviewed all **67 assigned C# files** end to end, including assertions, fixture construction, reflection helpers, cleanup, skips, public API use, global state, native resource ownership, sample/package paths, and compatibility assumptions. The recorded current snapshot contained 14,366 lines; other workers can subsequently change their files. Line references below identify the inspected snapshot, not a promise of stable line numbers.

**Ownership assumption:** the supplemental manifest instructions override generic worker ownership. All 67 existing test files and all runtime/editor implementation files remain read-only for this worker. Writes are limited to three new `Audit20261023TestUtilities*.cs` fixtures, their metadata, the assigned public guide and its metadata, and this report. No existing test or production implementation was changed. References to a required existing-file fix below are coordinator handoffs, not completed source fixes.

Reuse: read the `neoxider-tools` and `unity-game-agent` skills, searched the Extensions/Tools references, and checked actual package APIs. The new tests directly reuse `EnumerableExtensions`, `RandomExtensions`, `DateTimeExtensions`, `TimeParsingExtensions`, `PlayerPrefsUtils`, `SetText`, `TimeToText`, and `UniversalRotator`. No replacement runtime helper/framework was introduced. No child agents, Unity/MCP, builds, test runners, commits, package/settings changes, or shared services were used.

### Demonstrated findings

Severity: P1 = possible loss of editor/project work; P2 = isolation failure, false result, or significant coverage defect; P3 = narrower coverage/maintenance limitation. Source paths for existing tests are listed in the scope inventory above.

#### F01 — P1: camera teardown can destroy a pre-existing scene object — coordinator action

- **Source:** `Tools/InteractiveObjectTests.cs:19–25,61–78`.
- **Trigger/repro:** an open scene already contains an active object named `MainCamera`. Setup creates a second object with that name but retains no field for it. Teardown calls `GameObject.Find("MainCamera")` and destroys whichever match is returned.
- **Observable consequence:** the user's camera is a valid deletion target; the actual fixture camera can survive. Test ordering and subsequent `Camera.main` resolution become unreliable, and scene work may be lost.
- **Required fix:** retain the exact camera reference and destroy only it. Do not replace this with another global name/tag lookup. Own the EventSystem explicitly and restore any prior current selection/system state if changed.
- **Evidence:** direct source ownership trace; the test was not executed. Supplemental tests use a single retained `_root` reference and no cleanup search.

#### F02 — P1: sample scene checks discard the current scene setup — coordinator action

- **Source:** `Editor/SampleAssetValidationTests.cs:183–220`; `Editor/SampleSceneCoverageTests.cs:57–128,135–207,240,280`.
- **Trigger/repro:** run the scene-opening cases with an untitled scene, dirty saved scene, or more than one loaded scene. The tests record only the active scene path, open samples with `OpenSceneMode.Single`, then reopen that path or create a new default scene.
- **Observable consequence:** the original additive setup and unsaved state cannot be reconstructed from a path. Sample validation can disrupt editing even when assertions pass. In the hidden release layout, `SampleAssetValidationTests` still performs its final scene reopen/new-scene operation after skipping every hidden sample scene.
- **Required fix:** inspect samples in isolated preview scenes where supported, or controlled additive scenes without unloading existing content. Preserve/restore the complete scene setup and never discard dirty scene contents merely to obtain a clean fixture. The primary owner has a new preview-scope helper in this worktree; integration of the existing cases belongs to that owner.

#### F03 — P2: persistence fixtures leave a replacement global save provider installed — coordinator action

- **Source:** `Tools/Inventory/InventorySystemTests.cs:81–218` and teardown at `15–28`; `Tools/SelectorTests.cs:339–368,465–516`. Context: `Scripts/Save/SaveProvider.cs:187–205`.
- **Trigger/repro:** record `SaveProvider.CurrentProvider`, then run a save/load case in either fixture. Each calls `SaveProvider.SetProvider(...)`, which replaces the global provider and its event wiring; neither fixture restores the previous provider.
- **Observable consequence:** later tests/editor code can read/write the fixture dictionary instead of the configured provider. Passing results depend on fixture order. Clearing an inventory singleton does not restore provider identity or event subscriptions.
- **Required fix:** capture the provider before replacement, destroy components that might save during cleanup, then restore that provider in `finally`/teardown. The provider should be restored even after an assertion or construction failure.

#### F04 — P2: generated audio clips are not released — coordinator action

- **Source:** `Audio/AmChannelEditModeTests.cs:47–56,281,289,791–794`; `Audio/AmEditModeTests.cs:40–52,488–492`; `Audio/AmEntryEditModeTests.cs:40–46,657–660`; `Audio/RandomMusicControllerEditModeTests.cs:25–31,83–85`.
- **Trigger/repro:** any clip-creating test, especially repeated runs in one Editor domain. Factories return `AudioClip.Create(...)` results without tracking them. Teardown destroys the GameObject/AudioSources only. The independent playback probe also loses its generated clip reference.
- **Observable consequence:** native clips survive fixture cleanup; destroying the AudioSource does not destroy an assigned clip. Repetition increases native resource use and contaminates resource baselines.
- **Required fix:** retain every created clip and destroy it after its sources/controllers are stopped or destroyed, including skip/failure paths. Also stop/dispose `RandomMusicController` in `finally` if an assertion fails after `Start()`.
- **Related narrower cases:** `UI/UIMeshRigEditorTests.cs` destroys a generated sprite/texture only after the conversion assertions; failure skips that asset cleanup. `Extensions/UnityExtensionsTests.cs`, parts of `Tools/Move/TransformMoveTests.cs`, and `Tools/NeoConditionTests.cs` destroy objects after assertions without failure-safe ownership. Record these as failure-path leaks rather than claiming they leak on every passing run.

#### F05 — P2: fixed preference keys and reset-to-default cleanup alter unrelated state — coordinator action

- **Source:** `Extensions/ExtensionsTests.cs:768–780,793–798`; `Tools/TimerTests.cs:144,170–175`; `Tools/CursorLockPlayerControlTests.cs:24,31–36`; `UI/AuditFixesAnimationFlyTests.cs:19–24`; `Audio/AmChannelEditModeTests.cs:47–55`.
- **Trigger/repro:** pre-populate `test.playerprefs.intArray` before running an unrelated test in `ExtensionsTests`; fixture teardown unconditionally deletes all four fixed array keys and calls `PlayerPrefs.Save()`. The missing-key assertion uses another fixed key without ensuring it is absent. Other fixtures force global cursor management/diagnostics defaults rather than preserving the previous values; the AnimationFly diagnostic enable has no paired restoration.
- **Observable consequence:** previous preference values disappear, an unrelated test can fail because a supposedly missing key exists, and global flags survive into later fixtures. Timer cleanup similarly deletes fixed save keys without preserving pre-existing values.
- **Required fix:** unique per-test keys and selective cleanup, or exact value/existence snapshots for deliberately fixed keys. Snapshot each changed flag, not an assumed default. Keep restoring after assertion/skip paths. New preference tests use GUID-suffixed keys, delete only those keys, and do not force a disk-wide save.

#### F06 — P2: advancing the shared inspector ticker invalidates other clients — coordinator action

- **Source:** `PropertyAttribute/NeoInspectorAnimationTests.cs:74,179–191`; implementation context `Editor/PropertyAttribute/CustomEditorBase.cs:200–246`.
- **Trigger/repro:** another inspector is already a repaint client when the fixture records `_baselineClients`. The lease test calls `TickRepaintClients(EditorApplication.timeSinceStartup + 60.0)` on the shared list.
- **Observable consequence:** all ordinary existing leases expire, so the count becomes zero instead of the recorded nonzero baseline. Shared last-repaint timestamps are also advanced into the future. A fixture intended to release one client can disrupt other inspectors and fail based on editor UI state.
- **Required fix:** isolate the ticker state/clock or test a per-client lease predicate separately; restore exact shared state when testing the integration. Do not treat a global future tick as local fixture cleanup. Similar ownership concerns apply to unconditional `StopAllPreviews()` and health/cache resets; their scope should be deliberate and isolated.

#### F07 — P2: several tests do not observe the contract their names claim — supplemental coverage added

| Existing source and gap | Consequence | New public-behavior coverage |
| --- | --- | --- |
| `Extensions/ExtensionsTests.cs:415–421`: receiver is `List<int>`, so `list.ForEach` binds to the BCL instance method | Removing/breaking `EnumerableExtensions.ForEach` does not affect this test | An explicitly typed `IEnumerable<int>` receiver; ordered visits; iterator disposal and stop-on-callback-failure; null action/source |
| `Extensions/ExtensionsTests.cs:891–897`: equivalence compares source/result after the call, with no independent source-order snapshot | A copying implementation that mutates the input or returns the same list can still pass | Original-order snapshot, distinct result identity, duplicate multiplicity, independent mutation, and in-place identity |
| `Extensions/ExtensionsTests.cs:608–620`: UTC-now round-trip compares only year through second | Fractional ticks and UTC `Kind` can regress unnoticed | Fixed fractional instant, exact serialized value, full ticks and UTC kind, Russian/English cultures, unspecified-kind policy, explicit offset |
| `Extensions/ExtensionsTests.cs:811–821`: the test named JSON/legacy only writes comma-separated data | The JSON-reader branch can be removed and this test still passes | An actual `Value` JSON object containing escaped quotes, commas, and an empty element; rejected-write preservation; empty-write deletion |
| `Tools/Move/TransformMoveTests.cs:92–115`: identity rotation already faces the forward target | Replacing `RotateTo` with a no-op still satisfies the assertion | Right/backward targets from a translated, unaligned pose, plus a real 30-degree absolute yaw clamp |
| `UI/SetTextTests.cs:48–67`: checks substrings only | Incorrect rounding/extra digits or a wrong magnitude containing the same digits can pass | Exact percentage/currency output and exact event payload/count; percentage limits; explicit separators across cultures |
| `UI/TimeToTextTests.cs:114–124`: successful parsing only | Rejected input could reset display/time or emit a change event | Preserve text, time, and events for invalid/null/non-finite/overflow input; custom-separator success and repeated-value event suppression |

These additions close the named public-behavior coverage gaps without editing existing tests. They have **not** been compiled or executed in Unity. Existing misleading names/assertions can still be corrected by the primary test owner.

#### F08 — P2: a component failure can be reported as an unavailable capability — coordinator action

- **Source:** `Audio/AmEditModeTests.cs:282–292,302–312,323–337`.
- **Trigger/repro:** introduce a failure in AM's ambience-start path. Each stop/same-track case first calls AM to start the clip and then uses `Assume.That(ambience.isPlaying, ...)` to decide whether the machine supports playback.
- **Observable consequence:** the regression can produce an inconclusive result instead of a failure. The guard also skips clip/loop/volume state checks that do not need audible playback.
- **Required fix:** independent native audio capability probe, with owned clip cleanup, and a separate playback-only integration test. Keep assignment/stop-state tests unconditional. `AmChannelEditModeTests` already demonstrates an independent one-shot probe pattern, although its clip cleanup needs F04's fix.
- **Other skip review:** package-mixer absence and actual native impulse probing have concrete environmental reasons; source-layout and IMGUI skips leave explicit coverage gaps. In `InspectorControlIdStabilityTests.cs:159–161`, a missing reflected `RepaintImmediately` method is silently ignored and can later look like absent IMGUI samples. Assert method availability separately from graphics availability. AnimationFly's no-singleton assumptions are setup restrictions, not proof that those code paths were tested. No new fixture contains a skip.

#### F09 — P2: package/sample validation can inspect no assets or the wrong layout — coordinator action

- **Source:** `Editor/SampleAssetValidationTests.cs:25–28,338–372,466–478`; `Editor/SampleSceneCoverageTests.cs:402–441,466–497`; `Tools/InteractionQueryMathTests.cs:12–21`; `Tools/MoverPrefabColliderOffsetTests.cs:30,78–85`; `PropertyAttribute/InspectorControlIdStabilityTests.cs:65–66`.
- **Trigger/repro:** install the package under `Packages/com.neoxider.tools`, or use only hidden release samples. The package-prefab root and several file/source checks are fixed to `Assets/Neoxider`. Hidden roots are found on disk but filtered out from AssetDatabase roots; `FindAssetsInAssetDatabaseSampleRoots` then returns an empty array.
- **Observable consequence:** prefab/network/terrain checks can pass empty loops while checking zero assets; installed-package source checks can fail or skip despite valid content. Importing two sample versions can select an arbitrary directory because resolution accepts the first enumerated child. Hidden script-GUID searches still assume `Assets/Neoxider`.
- **Required fix:** resolve the real package root, active sample layout, explicit imported version/sample identity, and discovery count. Use file/YAML packaging checks for hidden samples and imported Unity integration checks for components/terrain. A hidden-only layout is valid and must not be mislabeled as broken.
- **Evidence:** static path/branch trace. Read-only file checks confirmed both referenced RPG sample scene files exist in `Assets/Neoxider/Samples~/Demo/Scenes`. No missing-scene finding is asserted. YAML marker presence alone is not evidence of runtime wiring, prefab inheritance resolution, or sample compilation.

#### F10 — P3: smoke scene missing-script checks stop at root objects — coordinator action

- **Source:** `Editor/SampleSceneCoverageTests.cs:98–106,343–349`.
- **Trigger/repro:** a sample child GameObject, rather than a root, contains a missing script. The smoke helpers call `GetMonoBehavioursWithMissingScriptCount` on each root only.
- **Observable consequence:** this smoke test does not report that descendant failure. The separate recursive asset-validation test provides complementary visible-scene coverage, so this is not a claim that every test in the suite misses descendants.
- **Required fix:** share/use a recursive hierarchy walker including inactive descendants; retain a positive inspected-object count. New smoke acceptance should cover a child missing-script fixture.

#### F11 — P3: some lifecycle/throttle assertions can pass without exercising their intended path — coordinator action

- **Source:** `Tools/NeoConditionThrottleTests.cs:35–59` uses `update?.Invoke`, then expects zero events. Several positive-path helpers similarly use optional reflection; `Tools/TimerTests.cs:109–114` disposes an already idle timer; `Tools/AuditFixesAttackSystemTests.cs:59–71` invokes `OnDisable` directly on an enabled component.
- **Trigger/repro:** remove/rename the reflected throttle `Update` method; a null-conditional invocation leaves the event count zero, satisfying the gate assertion. An idle-only disposal test does not measure cancellation of a running timer. Direct lifecycle invocation does not establish that Unity actually delivers the callback on disable.
- **Observable consequence:** these checks establish narrower facts than their names suggest and can miss broken wiring/cancellation. Async typewriter tests also invoke an async operation through reflection without awaiting its result and do not guarantee `Stop()` when an assertion fails (`AuditFixesTypewriterTests.cs:18–22,46–55`).
- **Required fix:** assert reflection member existence and include a positive before/after control; prefer typed public APIs and deterministic drivers. Reserve real cancellation/disable/destroy dispatch for awaited Play Mode cases with failure-safe disposal. Retain reflection only when it is specifically the compatibility contract being tested.

#### R01 — P2: finite weighted inputs overflow into a deterministic last-slot result — runtime owner action

- **Source:** `Assets/Neoxider/Scripts/Extensions/RandomExtensions.cs:221–268`; existing coverage `Extensions/ExtensionsTests.cs:873–897`.
- **Trigger/repro recipe:** call `new float[] { float.MaxValue, float.MaxValue }.GetRandomWeightedIndex()` repeatedly while preserving the caller's `Random.state`. Both weights are finite and equal, so both outcomes should be eligible. This recipe was not executed by this worker.
- **Source-derived observable consequence:** accumulation uses `float`, so the total becomes positive infinity. For any positive random sample the selection point is infinity; a zero sample produces NaN. Neither can satisfy the finite-weight comparison, and the fallback always returns the last positive index, `1`. NaN/infinite individual weights are also not rejected by the negative-value check. This conclusion follows from the implementation's arithmetic/branches, not from a claimed Unity run.
- **Required fix:** validate finite weights and accumulate/select with sufficient range, for example `double` for a finite float-weight list, retaining the existing return/logging contract for invalid inputs. Add finite-overflow and invalid-non-finite regression cases in the runtime owner's test scope. No runtime source was changed and no knowingly failing active test was added here.

### Completed additions

| New file | Public contracts | NUnit methods / expanded cases |
| --- | --- | --- |
| `Tests/Edit/Extensions/Audit20261023TestUtilitiesEnumerableTests.cs` | Actual extension dispatch, iterator disposal, null handling, extreme indices, shuffle copy/in-place ownership and multiplicity | 8 / 11 |
| `Tests/Edit/Extensions/Audit20261023TestUtilitiesSerializationTests.cs` | UTC precision/kind/offset, cross-culture float preferences, actual legacy JSON, rejected/empty writes, duration separators and bounds | 8 / 16 |
| `Tests/Edit/Tools/Audit20261023TestUtilitiesSceneContractsTests.cs` | Complete TMP output/events, percentage clamp, culture-stable currency, rejected parse state, repeated-time notifications, nontrivial rotations and limit | 6 / 16 |

Total: **22 test methods, 43 statically enumerated NUnit cases**. These counts are source inspection, not discovery/execution results. Each fixture has a unique prefix; `.meta` files were added with distinct GUIDs. Existing assembly references already cover these APIs; no asmdef changes are required. Root objects remain inactive during synchronous API checks; no new test enters Play Mode, invokes global lifecycle reset, searches for cleanup objects, or changes an existing scene/prefab asset.

### Full-scope review record

The 67-file inventory above is the exact assigned scope. This matrix records the review across every group; omitted runtime features are coverage limits, not invented defects.

| Assigned files/group | Review outcome and practical limits |
| --- | --- |
| Six Audio fixtures | Channel routing, serialization defaults/migration, pool selection, level/mute products, positional ownership, ambience, controller events, listener volume restoration reviewed. Channel/default assertions are useful; native clip ownership, RNG restoration, playback skip classification, and failure-safe controller shutdown need follow-up (F04/F05/F08). `AudioFocusMute` restores the captured listener volume and destroys installed instances. |
| `DialogueControllerEditModeTests` | Owned GameObject cleanup uses `finally`; initial indices and started flag are checked. Stop/restart/bounds/typewriter integrations remain outside this single smoke test. |
| `CreateMenuObjectLegacyTests`, `NeoxiderCreateMenuTests` | Legacy signatures/menu exclusions, preset linked instances, parenting, canvas choice, naming and hierarchy idempotence checked. Logger replacement is restored. Current-root sweeping, selection reset rather than restoration, existing scene objects/canvases, and Undo state remain isolation concerns. |
| Two sample validation fixtures | Hidden/development/imported path branches, YAML references/curve data, visible scene wiring and root counts checked. F02/F09/F10 delimit what the tests really establish; no hidden YAML inspection is claimed as runtime execution. |
| `SceneSaverAutoSaveTests` | Explicit simulated time/revision transitions and saved/unsaved path cases are deterministic; EditorPrefs preserve values and key-existence and reload the shared settings. Multiple-scene scheduling/integration and interval exact-boundary cases remain additional coverage opportunities. |
| Three Extensions fixtures | String/time/number/color/collection/date/cooldown/prefs/random contracts and coroutine helper reset/tracker setup checked. F05/F07/F11 identify isolation/coverage issues; several Unity-object tests clean only on success. One-shot/current-thread allocation claims still require execution. |
| Five PropertyAttribute fixtures | Reflection attributes, typed reference validation, repaint scanner/IMGUI probes, console health, button discovery, caches and repaint leases reviewed. F06/F08/F11 apply; source-text scanner only recognizes its specific comparison syntax, and compatibility checks must compile on each Unity branch. SerializedObject disposal should be scoped consistently. |
| `AuditFixesAttackSystemTests` | Interruption state/event assertions and independent native impulse probe reviewed. Direct lifecycle invocation covers method behavior only; target/applier teardown is owned. |
| `AuditFixesDrawerTests` | Pool release/reuse and partial-stroke deletion observed. Static singleton resets and dependence on global PoolManager require isolation; cleanup is fixture-owned. |
| `AuditFixesInteractiveObjectTests`, `InteractiveObjectTests` | Hover/click gating, pointer events, manual typed API and inspector button defaults reviewed. F01; global camera/EventSystem and origin-based physics fixtures remain environmental dependencies. |
| `AuditFixesSelectorTests`, `SelectorTests` | Pure model transitions, excluded/unique state, physical/logical activation, counters/events, offset bounds, persistence reviewed. Useful exact counts/identity assertions; F03 and global RNG state require follow-up. |
| `AuditFixesSpawnerTests`, `SpawnerTests`, `SpawnerDenyZoneTests` | Spawn-loop disable guard, pool identity/generation, idempotent return, 2D/3D deny zones/null entries reviewed. A partially advanced `IEnumerator` should be disposed. Failure paths can bypass local pool clears; singleton/live scene pool interference remains a risk. |
| `AuditFixesTimeTests`, `TimerTests`, `TrustedClockTests` | Countdown/up reset/progress, deterministic tick, save/paused behavior, mutable fake clock, high-water/rewind/forward cases reviewed. F05/F11; timer-core running disposal and true persistence across process restarts are not proven. The rewind/forward farming test changes wall time twice without an intervening read, so it does not exercise detection of an observed intermediate rewind. |
| `AuditFixesTypewriterTests` | Reentrant callback ownership and synchronous completion checked. Async exceptions and cleanup after assertion failure require typed awaited integration (F11). |
| `BillboardUniversalEditModeTests` | Explicit serialized camera path and no-transform-change validation assertions; SerializedObjects are scoped. One optional OnValidate invocation needs an availability assertion if it remains necessary. |
| `CharacterControllerInputTests`, `CursorLockPlayerControlTests`, `FreeFlyCameraControllerTests` | Backend/rate math, injected move/look, component enable gates, registration/suspension ownership, deterministic camera tick reviewed. Device-fallback assertions assume idle physical input; cursor/static state needs exact restoration (F05). |
| `DebugToolsBehaviorTests`, `FakeLeaderboardBehaviorTests`, `OtherToolsBehaviorTests` | TMP output/no-output handling, frame-rate/v-sync restoration, generated counts/sorting/best-score, shake cancellation, config clamps reviewed. Leaderboard and shake paths consume global RNG; generated values are constrained by invariants rather than distribution claims. |
| `ImageFillAmountAnimatorTests` | Clamp/inversion, sliced anchors/visibility/min width/mode switch, explicit manual tween update and real Play Mode lifecycle cases reviewed. Warning flags are restored and owned tween killed. Global `DOTween.ManualUpdate` can advance unrelated manual tweens; runner transitions remain centrally unexecuted. |
| `InteractionQueryMathTests`, `InteractiveObjectAimSymmetryTests`, `InteractiveObjectMouseTrigger2DTests` | Hit-order/blocker/distance and 2D/3D trigger symmetry checked; trigger setting is restored, render textures released. Camera arguments/remote coordinates reduce interference; globally shared physics/camera discovery and fixed source paths still need isolated runners. |
| `InventorySystemTests` | Plain inventory stack/instance contracts, instance payloads and transfer, slot legacy loading, generated data-object reverse cleanup checked. F03; production singletons are cleared without prior-state snapshots. |
| `TransformMoveTests`, `MoverPrefabColliderOffsetTests` | Distance transitions, rotation commands and invariant-culture asset-field parsing reviewed. F07 fixes the no-op rotation gap through additions; prefab scanner is package-path dependent and matches the field name rather than proving component identity (F09). |
| `NeoConditionTests`, `NeoConditionThrottleTests` | Cache member lookup, comparisons, empty logic, list add/remove, event/reset and gate reflection reviewed. F11; these existing cases do not comprehensively exercise nonempty AND/OR short-circuit behavior. The module owner has separate new regression coverage; no duplicate owner tests edited. |
| `PhysicsToolsTests`, `ChanceManagerTests`, `ToolsComponentsTests`, `ToolsDiagnosticsDefaultsTests` | Public scalar clamps/flags, reset explosion, deterministic chance provider/normalized state/cache invalidation, counter math, lifecycle event bodies, random-range bounds and quiet defaults reviewed. Real force effects, timer/lifecycle dispatch and warning emission classification remain integration concerns; global RNG is not restored by existing random-range cases. |
| `AnimationFlyEditModeTests`, `AuditFixesAnimationFlyTests` | UI coordinate/size/count requests, legacy fallback and missing-canvas paths reviewed. Created sprite/texture are explicitly owned; singleton/environment skips, diagnostics state (F05), and asynchronous edit-update timing remain limits. |
| `ButtonChangePageTests`, `ButtonScaleTests`, `CategoryBarTests` | Submit/interactable gate, preserved Z, category start/disabled/wrap/selection/event/list replacement checked. UI singleton/EventSystem startup is reflected; exact full real UI navigation/lifecycle is not established by synchronous callbacks. |
| `SetTextTests`, `TimeToTextTests`, `VisualToggleEditModeTests` | TMP formatting/time parsing, NoCode adapter call and reactive bool smoke reviewed. F07 adds stronger observable outcomes; NoCode reflection is one binding step, not end-to-end inspector serialization. |
| Five UIMeshRig fixtures | Geometry/UV/indices, cross-adapter pose equivalence, motion curves/noise/allocation, clone/source ownership, disabled renderer restoration, influence/migration/anchors, editor menu/conversion/Undo/preview behavior checked. Native sprite cleanup on failure and broad scene/preview cleanup need isolation. Vertex-write test checks length only after rebuild, so wrong same-length vertex data can still pass; mesh correctness needs value assertions. Version-dependent PanelRenderer branches are source-reviewed only. |
| `View/AnimationsTests` | Play/stop/pause/resume state and explicit Perlin parameters checked. Actual frame progression, event values and time-scale interaction remain separate runtime contracts. |

## Documentation

Added **`Assets/Neoxider/Docs/Testing/Utilities.md`** as the sole public documentation change, with unique file metadata. It covers public C# contracts and scene/NoCode entry paths, true extension dispatch, disposal/copy semantics, fractional UTC/culture/prefs boundaries, exact text/event checks, failure-safe cleanup, capability skips, sample/UPM layouts, and the limits of synchronous lifecycle/reflection checks.

`Assets/Neoxider/Docs/Testing.md` did not exist at the documentation link check. No broken link or redundant module entry was added. **Coordinator action:** link this narrow guide from the primary Testing entry when available and supply `Docs/Testing` folder metadata. No other owner's docs, root README, changelog, or shared config was edited.

Кратко: исправлены пробелы в проверках публичных контрактов отдельными новыми тестами. Опасные очистки и изменения общего состояния в существующих тестах подробно переданы владельцу; эти файлы здесь не изменялись. Unity-проверки не запускались.

## Substantial improvement proposal

**Proposal: an explicit, failure-safe utility test environment — not implemented.**

**Problem.** The existing suite repeats ad hoc setup for Unity objects, native assets, save providers, singletons, preference keys, scene setup, cursor/diagnostic flags, random state, and capabilities. A fixture can pass its own assertions while leaving native resources or state behind, deleting another object's camera, or skipping the very regression it should detect. Resetting everything globally is not equivalent to returning the editor to its prior state.

**Architecture / test-support API.** Add an internal test-only scope to `Neo.Editor.Tests`, owned centrally, composed of small optional resource leases rather than a runtime manager:

```csharp
internal sealed class UtilityTestScope : IDisposable
{
    public T Own<T>(T resource) where T : UnityEngine.Object;
    public string CreatePreferenceKey(string purpose);
    public IDisposable UseRandomSeed(int seed);
    public IDisposable UseCulture(CultureInfo culture);
    public IDisposable UseSaveProvider(ISaveProvider provider);
    public IDisposable InspectSceneInIsolation(string assetPath);
}
```

The API sketch is a design proposal, not compilable declarations added to the package. Each lease restores its captured state in reverse acquisition order; cleanup continues after one disposal error and reports aggregated failures. Track scene roots and native assets separately, detach external references before destruction, and restore the provider after components have completed their own cleanup. Scope support for shared ticker/cache/singleton integration must be explicit and local to that subsystem; avoid a universal reflection-based reset. Separate capability probes for audio, IMGUI and native impulses from the SUT, with structured results/reasons; deterministic state tests should not depend on those probes.

**Compatibility / migration.** Test-only, with no runtime/public package or serialized API change. Migrate fixtures incrementally, beginning with F01–F06, then remove duplicated helpers after their behavior is covered. Retain fixed preference keys only where the key itself is the production contract, restoring both its original existence and value. Resolve package/sample discovery through a typed test helper with explicit active version; do not edit package manifests merely for fixture isolation. Existing NUnit names can remain while stronger assertions are added.

**Acceptance scenarios.**

1. Seed an editor scene with a camera named `MainCamera`, dirty objects, an additive/untitled scene, an active inspector preview and selection; after success, failure, and capability skip, all pre-existing objects/state remain intact.
2. Install a sentinel save provider and non-default diagnostics/cursor/RNG/culture values; run two migrated fixtures in either order and repeat them in one domain. Provider identity/event routing and exact captured settings are restored.
3. Fail immediately after creating a clip/sprite/texture and after replacing a provider. Owned native resources are destroyed, provider restored, unique keys removed, and cleanup proceeds even if one lease raises an error.
4. Run a headless job and a graphics/audio-enabled job. Missing capabilities are explicit inconclusive results only for integration cases; breaking AM playback fails the supported job rather than satisfying the capability probe. Pure state/API checks remain active in both.
5. Validate development, hidden release, and versioned imported sample roots, including two imported versions. Discovery selects the intended version and reports nonzero inspected assets; descendant missing-script fixtures fail.
6. Compile/run centrally on Unity 6.0–6.4 and 6.5+; no deprecated identity calls or per-call scattered version directives are introduced.

**Effort/risk.** Approximately 2–4 engineer-days for the small resource/state scopes and representative migration, plus 2–4 days for scene isolation/capability coverage and remaining fixture rollout. Main risks are Unity scene/Undo behavior across versions, restoration order during component destruction, reentrant save callbacks, and graphics availability in CI. Keep the proposal test-only and introduce it behind incremental migrations; do not refactor production singletons to solve test ownership by default.

## Verification and risks

Actually performed:

- Read-only source review of all 67 assigned files; relevant package implementation and assembly-reference checks. All 43 added cases call public APIs already referenced by `Neo.Editor.Tests`.
- `git diff --check --` scoped to this report and all eight new package files: exit code 0. Git does not check untracked file contents, so a separate static byte/whitespace pass checked every new file for strict UTF-8, terminal newlines and trailing whitespace.
- Static C# delimiter/string checks for the three new fixtures, explicit-type scan, and scan for obsolete identity APIs, skips and Play Mode transitions: clean. This is not C# compilation.
- Statically counted 22 `[Test]`/parameterized methods and 43 expanded cases. Parsed the C# legacy-JSON string literal and its payload independently to confirm valid escaping and the intended comma/quote/empty values.
- Scanned `Assets/**/*.meta` for the four new GUIDs: exactly one occurrence per GUID, in the corresponding new file metadata.
- Enumerated skip sites in the assigned inventory and checked current referenced RPG scene file existence; no Unity objects/scenes were loaded or saved. Verified the primary Testing documentation entry is currently absent; the new guide has no link to an absent entry.

**Not performed:** Unity import, C# compilation, NUnit discovery/execution, Edit/Play Mode runs, builds, device/audio/graphics checks, sample import, and compilation of both Unity version branches. None is reported as passing. The coordinator must compile/run the new fixtures and perform the central integration checks.

**Remaining risks / owner actions:** F01–F06 and F08–F11 require changes in the existing test owner's scope; R01 requires the Extensions runtime owner. The new cases improve coverage but cannot repair those read-only fixture isolation defects. Concurrent workers may already be addressing portions of these findings; reconcile against their latest files before central execution. No speculative production-source change or shared configuration change was made. Folder metadata and linking the new narrow Testing guide remain coordinator work.
