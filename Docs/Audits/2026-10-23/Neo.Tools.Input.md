# Neo.Tools.Input: audit

Status: source audit and scoped fixes complete; Unity compilation and test execution pending coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Input/Neo.Tools.Input.asmdef`.
- Scope: 10 C# files; nested assemblies excluded.

## Scope

Read `AGENTS.md`, the complete worker contract, this report's initial state and the exact manifest entry before editing. The manifest and on-disk top-level C# inventory agree: **10/10 files reviewed in full**, with no nested assembly in this directory. Source/docs were initially clean; this report was already an untracked coordinator-created placeholder. No other worker files were changed.

| Owned C# file, under `Assets/Neoxider/Scripts/Tools/Input/` | Review and disposition |
| --- | --- |
| `KeyInputCompat.cs` | All three public key reads, legacy-disabled fallback, missing/unsupported keys. Runtime code retained; inaccurate public API documentation corrected. |
| `MouseEffect.cs` | All lifecycle callbacks, camera precedence/retry, follow/trail state, event subscriptions, spawn cadence/ownership, diagnostics and version branches. Fixed backend access, interrupted visual state and duplicate initial Hold spawn. |
| `MouseInputCompat.cs` | All four Try APIs and their out values, legacy/new fallback and supported button indices. Reused unchanged by manager/effect/swipe. |
| `MouseInputManager.cs` | Every event family, mode switch, press/click state, camera resolution, 2D/3D raycasts, polling caches, singleton initialization and editor gizmos. Fixed backend reads, dependent mode switches and stale presses. |
| `MouseInputManagerSubsystemRegistration.cs` | Subsystem cache reset and BeforeSceneLoad creation flag. Correct existing behavior retained: bootstrap enables lazy creation; it does not instantiate the singleton. |
| `MultiKeyEventTrigger.cs` | Every binding, null events, binding mutation/empty arrays, constructor and diagnostics. Added null-array guard. Keys remain individual bindings, not chords. |
| `Neo.Tools.Input.InternalsVisibleTo.cs` | Existing Edit Mode test visibility retained; added visibility for existing `Neo.Tests.Play`, allowing deterministic samples through the same processing methods used by Update. No shared asmdef edit. |
| `OptionalInputSystemAdapter.cs` | Entire reflection/type/cache path, all movement/look/jump/run reads, Try semantics, key mappings and subsystem reset. Fixed keypad Enter and invalid-state success; added internal primary-touch translation and reused cached ReadValue lookup. Replaced the existing inferred local type with explicit `Type`. |
| `SwipeController.cs` | All data/enum/interface contracts, static polling/instance lifecycle, both device paths, thresholds, direction suppression and event payload order. Fixed contact ownership/cancellation, phantom mouse gestures, qualifying-axis selection and backend access. |
| `SwipeControllerSubsystemRegistration.cs` | Existing instance reset hook reviewed and retained. Scene-reload-disabled behavior still needs central runtime verification. |

Dependency context was read only: input asmdef, `Singleton<T>`, existing Input Play Mode and subsystem reset tests, and existing Edit/Play test asmdefs. Local installed Input System source confirmed Keyboard controls, Touchscreen/TouchControl members and persistent raw touch phases. Reuse receipt: `neoxider-tools` tools/extensions references were searched; existing `MouseInputCompat`, `KeyInputCompat`, `OptionalInputSystemAdapter`, `Singleton<T>` and `NeoDiagnostics` were adapted/reused. No parallel input framework, pooling layer or new component was introduced.

## Findings and fixes

The following reproductions are source-level control-flow demonstrations, not claims of executed Unity scenarios. Regression files below are added but unexecuted.

### F1 — High: scene input consumers bypassed the compatibility layer

**Trigger:** Active Input Handling set to Input System Package (New), with legacy input disabled.

**Evidence/source:** `MouseInputManager.Update` and `BuildEventData`, `MouseEffect.TryGetCursorWorld`, and `SwipeController.Update`/mouse handling directly called `UnityEngine.Input` instead of the already available wrappers. Disabled legacy reads throw `InvalidOperationException`, aborting the frame's interaction/follow/swipe work.

**Fix:** manager/effect/swipe mouse reads now use `MouseInputCompat`; manager receives one sampled screen position throughout dispatch. Swipe's legacy touch-count read has a narrow exception boundary and falls back to the optional primary-touch adapter. Listener exceptions are outside that legacy-read try block. Legacy touches use indexed `GetTouch` instead of allocating `Input.touches`.

The installed Input System `Touchscreen.OnNextUpdate` clears delta/tap count but retains raw phase. The adapter translates enum names because the two TouchPhase enums have different values, normalizes a retained Began to Stationary without a current press edge, and ignores Ended/Canceled without a current release edge. This prevents restart after interruption and indefinite mouse blocking after a touch. No direct Input System assembly dependency was added.

**Coverage:** adapter phase/identity/position cases, persistent-phase cases and deterministic processing tests. Actual New-only mouse/touch Update integration and stripped player behavior remain unverified.

### F2 — Medium: disabling Press also disabled Hold and Click

**Trigger:** `enablePress = false` with Hold and/or Click enabled.

**Evidence/source:** `MouseInputManager.Update` assigned `_pressed`/`_pressedObj` only inside `if (enablePress && buttonDown)`. Hold required `_pressed`; Click required `_pressedObj`. Consequently independently enabled events could never fire.

**Fix:** track every observed button-down, then filter Press notifications separately. Release clears state before callbacks. Same-object non-null click semantics and ordinary/`in` event families are retained.

**Coverage:** `DisablingPressEvents_PreservesHoldClickAndPollingData` asserts no Press, one Hold, both Click channels, hit identity and static polling data after a real collider query.

### F3 — Medium: interrupted mouse presses survived disable/camera loss

**Trigger:** press an object, disable/re-enable the manager or remove its camera while the physical release occurs, then release over the same object after recovery.

**Evidence/source:** manager had no disable/focus/pause cleanup; the missing-camera early return left `_pressedObj` intact. A subsequent unrelated release could click the earlier target or resume Hold.

**Fix:** clear pending press state on disable, focus loss, application pause, unavailable mouse reads and missing camera. Cancellation emits no synthetic release. A new real press restores normal behavior.

**Coverage:** parameterized `InterruptedMouseGesture_CannotClickAfterRecovery` covers disable and missing camera, asserts no stale Click and then a successful new click. Focus/pause callbacks use the same cleanup; OS integration remains central QA.

### F4 — Medium: MouseEffect left active visuals/state after interruption

**Trigger:** disable an effect during a drag, or set `interactable = false` before release.

**Evidence/source:** `OnDisable` only unsubscribed; Update and OnRelease returned immediately when not interactable. `_isFollowing`, TrailRenderer emission and follower activation remained set. Re-enable could resume an obsolete drag, and every unmatched Release invoked `onStopFollow` again.

**Fix:** one idempotent StopFollowing path resets follow/hold state and hides visuals on disable/focus loss/pause/non-interactive update or release. Stop notification occurs once for an active follow. Ordinary release still honors `disableOnRelease = false`; spawned effects retain their documented lifetime/parent ownership.

**Coverage:** observable follower activation, trail emission and balanced start/stop event counts across disable/re-enable and non-interactive release. No screenshot/visual rendering claim is made.

### F5 — Medium: first Hold could spawn twice

**Trigger:** `spawnTrigger = Hold`, `spawnDuringHold = true`, `holdInterval = 0` (or the first Hold arrives after the repeat interval).

**Evidence/source:** `MouseEffect.OnHold` spawned the initial instance and fell through into the repeat branch without advancing its timestamp.

**Fix:** mark/timestamp the initial spawn and return; later Hold notifications perform repeats.

**Coverage:** `MouseEffect_FirstHoldSpawnsOnceWhenRepeatIntervalIsZero` asserts actual spawned child counts and `onSpawn` counts of one, then two after the next notification.

### F6 — Medium: touches shared and overwrote one gesture's origin

**Trigger:** finger A begins, finger B begins elsewhere, then A moves/releases; or an in-progress swipe is canceled before another swipe in the same direction.

**Evidence/source:** the original `HandleTouch` wrote the same origin for every Began and recognized every Ended; Canceled was ignored. Payload/direction could come from different fingers, and suppression state could leak into the next contact.

**Fix:** retain the first active finger ID, ignore other contacts until its end/cancellation, reset the origin/suppression for each begin, and cancel pending state on component/application interruption. The optional backend uses the documented Input System primary-touch policy.

**Coverage:** secondary-finger interference test checks exact count, direction and endpoints; cancellation test checks the next same-direction gesture is accepted.

### F7 — Medium: mouse swipes could occur without an observed press

**Trigger:** enable the component while the mouse is held, or release after disabling/re-enabling during a drag.

**Evidence/source:** original mouse handling called DetectSwipe for any held/released left button, even when this controller had never captured its down position.

**Fix:** require an active mouse gesture created by a sampled button-down; interruption clears it.

**Coverage:** `Swipe_RequiresObservedPressAndCancelsOnDisable` rejects both paths, then checks a new swipe and consume-once public direction polling.

### F8 — Medium: direction could violate its own configured threshold

**Trigger:** X threshold 100, Y threshold 20, displacement (30, 25).

**Evidence/source:** `SwipeDistanceCheckMet` accepted Y but `IsVerticalSwipe` compared only raw magnitudes, reporting Right even though X failed its threshold.

**Fix:** choose among qualifying axes, retaining dominant-axis selection and horizontal ties when both qualify. Threshold comparison remains strictly greater than, preserving default sensitivity.

**Coverage:** both asymmetric-axis cases assert no early release-only event and the correct final direction.

### F9 — Medium: KeypadEnter polled the main Enter key

**Trigger:** bind `KeyCode.KeypadEnter` with the Input System fallback.

**Evidence/source:** `GetInputSystemKeyPropertyName` grouped Return and KeypadEnter into `enterKey`; installed Keyboard source exposes a separate `numpadEnterKey`.

**Fix/coverage:** map KeypadEnter to `numpadEnterKey`; public mapping regression distinguishes both physical controls and unsupported KeyCode.None.

### F10 — Low: invalid state names reported successful Try reads

**Trigger:** call `TryReadKeyState` or `TryGetMouseButtonState` with a connected device and a missing/unsupported state property.

**Evidence/source:** the helpers returned true after GetControlBool had silently returned false for absent properties, conflating an unavailable state with a valid unpressed control.

**Fix:** shared TryGetControlBool distinguishes success from sampled value; only the three supported state names are accepted and failed outputs are false.

**Coverage:** fake-control reads distinguish valid false, valid true, missing control and invalid state. An additional public-API test adds/removes a temporary Input System keyboard using reflection, checks valid false versus unavailable state, and restores the prior current keyboard. It explicitly skips if the optional package is absent; it has not been executed here.

### F11 — Low: null key-binding array threw each Update

**Trigger:** a C# caller assigns `MultiKeyEventTrigger.keyEventPairs = null`.

**Evidence/source:** Update immediately enumerated the public array, throwing NullReferenceException rather than behaving like no bindings.

**Fix:** treat null as an empty configuration. No standalone no-throw regression was added: it would not verify an event/state contract beyond the guard, and backend-driven key dispatch is centrally pending. Existing non-null bindings and null UnityEvent handling remain unchanged.

### Compatibility and retained contracts

No serialized field or public type/member was removed or renamed; no defaults, scenes, prefabs, package settings, existing tests, asmdefs or assembly dependencies were edited. New processing/adapter methods are internal; the two named test assemblies can access them. Static reset hooks were kept. No Unity 6.5-obsolete instance-ID APIs were introduced. Existing gated/throttled `NeoDiagnostics` logging was retained. No runtime log was added.

Unchanged design choices were documented rather than silently changed: 3D hits take precedence over 2D, Click requires the same non-null GameObject, static mouse data persists until reset, swipe polling consumes only the latest direction, and instantiated effects use caller/parent lifetime rather than pooling.

## Documentation

Updated all six existing owned public pages; no new competing entry point:

- `Assets/Neoxider/Docs/Tools/Input/README.md`: canonical API/assembly overview, scene usage, backend/device semantics and pointer-delta versus stick-rate units.
- `KeyInputCompat.md`: actual GetKeyDown/GetKey/GetKeyUp signatures (replacing nonexistent IsPressed), supported fallback key subset and Try success/value semantics.
- `MouseInputManager.md`: lazy singleton creation, independent mode switches, click/picking rules, typed subscriptions, polling lifetime, cancellation and honest reflection-allocation limits.
- `MouseEffect.md`: backend/camera/depth behavior, event setup, spawn cadence, cleanup and instance ownership.
- `SwipeController.md`: useful authoring fields, C# events/static polling, threshold/segment rules, contact ownership and primary-touch limitations; corrected module link.
- `MultiKeyEventTrigger.md`: independent bindings, C# configuration, null/duplicate behavior, gated diagnostics and supported keyboard backend; corrected module link.

Кратко: модуль остаётся набором независимых API и сценовых адаптеров. Исправлены реальные ошибки ввода и жизненного цикла; инспекторные сценарии и ограничения описаны рядом с C# API.

## Substantial improvement proposal

### Buffered pointer sources and a reusable swipe recognizer — proposed, not implemented

**Problem:** gesture state is still inside MonoBehaviours and Update polling. Deterministic tests currently need internal processing seams plus Play Mode for lifecycle/physics. The optional backend uses reflection, allocates/cache-keys per read and relies on legacy exceptions to select fallback. Raw primary touch differs from legacy per-finger lifetime, and quick begin/move/end transitions between render updates can be lost. Custom AI, replay, remote input and multi-touch games cannot feed the existing public recognizer without a Unity component.

**Architecture/public API:** introduce a dependency-free C# `SwipeRecognizer` with a small immutable configuration and typed pointer samples. A proposed `PointerSample` carries pointer ID, screen position, phase and timestamp; `SwipeRecognizer.Process(in PointerSample sample)` emits typed `SwipeData` through a C# event, while `Cancel(int pointerId)` and `Reset()` explicitly terminate ownership. Keep suppression, thresholds and release-only decisions in that core. Add an `IPointerSampleSource` polling/draining contract that yields all samples since the last consumer update into a caller-owned buffer, rather than collapsing edges into one bool snapshot.

Keep `SwipeController` as the serialized scene adapter and UnityEvent facade. Provide legacy and recorded/injected sources without new dependencies. An optional typed Input System adapter assembly can subscribe to actual input events and preserve buffered transitions; use version defines/assembly boundaries managed by the coordinator, while keeping the current reflection fallback for installations without that package. Select/cache a backend explicitly once, expose its availability, and reset device-specific state on focus/device changes. Do not change mixed 2D/3D picking or introduce UI filtering into this recognizer; those remain composable consumers.

**Compatibility/migration:** retain all existing serialized fields, enum values, public events and static consuming poll. The wrapper translates existing settings into the core; no scene or prefab migration is required for existing behavior. Default single-contact ownership and threshold strictness stay unchanged. Buffered input and alternate primary-touch/per-finger policies should be opt-in initially, with documented transition semantics; selecting them must not silently change old games. Keep `ISwipeSubscriber` usable through the existing event wrapper.

**Acceptance scenarios:** pure C# tests prove all four directions, unequal thresholds/ties, suppression/reversal, release-only mode, cancellation and pointer ownership; legacy/new adapters produce matching sample transcripts for one-finger drags; down/move/up in one render frame produces one swipe; two fingers never mix endpoints; focus/device disconnect cancels a pending gesture; replayed transcripts produce identical output; old serialized scenes produce the same normal gesture events; no-package and Input System player builds both compile, and IL2CPP stripping retains the optional adapter. Profile the warmed-up paths and publish measured allocation limits rather than promising zero-GC universally.

**Effort/risk:** approximately 3–5 developer days plus target-device/IL2CPP QA, assuming existing assembly/test infrastructure can host the optional adapter. Main risks are ordering/latency changes when buffering, Input System fixed/dynamic update modes, compatibility of raw primary-touch semantics and preserving serialized wrapper behavior. Stage core extraction first, adapter second, then opt-in buffering; coordinator must own any new asmdef/package changes. This audit deliberately implements only narrow fixes and internal seams.

## Verification and remaining risks

### Added regressions

- `Assets/Neoxider/Tests/Edit/Core/Audit20261023NeoToolsInputAdapterTests.cs`: 14 declared NUnit cases across 7 methods, including public keypad/state reads, optional real-keyboard availability and reflected touch translation/persistent phases.
- `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputBehaviorTests.cs`: 11 declared cases across 9 methods, exercising actual event payloads, collider hits, polling, cancellation, follower/trail state, spawned object counts and swipe direction/endpoints.
- Both new files have unique generated `.cs.meta` GUIDs. Existing Edit/Play test asmdefs already reference `Neo.Tools.Input`; only this assembly's own friend list was extended.

### Checks actually run

- Read-only manifest-versus-filesystem comparison: **10/10** owned C# files match exactly.
- Scoped source diff reviewed after edits. Public member and serialized-name retention reviewed; a Python comparison against `git show HEAD:<owned file>` additionally checked serialized-name presence.
- Final static comparison retained all existing public declaration names and instance field names across all ten sources; UTF-8/whitespace checks passed for **21** scoped source/test/doc/report/meta files, including untracked additions.
- One-off Python static inspection: strict UTF-8 read of **18** source/test/doc files; no replacement characters, no C# `var`, balanced brace counts, no obsolete instance-ID calls, all relative links in the six owned docs resolve.
- Source-boundary checks: no direct `Input.*` calls in manager/effect; no direct legacy mouse calls or allocating `Input.touches` reads in SwipeController; touch legacy read is explicitly guarded.
- Cross-checked every literal returned keyboard control property against installed `Keyboard.cs`; primaryTouch/touchId/position/phase and public AddDevice/RemoveDevice signatures checked in installed Input System source. This is API source inspection, not compilation.
- Searched Assets metadata by each generated GUID: exactly one match each (`7d5bb59645684644bba491ca8eed0e34`, `435afde6287245979e3126dca8d4d13c`).
- `git diff --check --` owned source/docs/report and the four new test/meta paths: passed (exit 0). New-file whitespace is additionally checked directly because untracked files are absent from ordinary git diff output.

These checks establish static consistency only. No Unity, MCP, compiler, build/test runner, install, child agent, commit or shared service was started.

### Coordinator actions and unverified risks

1. Compile/import and run the two new fixtures (`Audit20261023NeoToolsInput*`) plus existing input/subsystem tests. Both Unity 6.0–6.4 and 6.5+ compilation remain unverified here. Do not interpret 25 declared cases as 25 passing cases.
2. Verify actual Update input with legacy-only, Both and New-only configurations; test mouse missing/reconnected, focus/pause, camera recreation and disabling during drag. A real touch device is needed for primary-touch versus per-finger lifetime. Fast transitions between updates remain a polling limitation addressed by the proposal.
3. Validate optional reflection in IL2CPP/managed stripping, allocation/performance cost and absence of the Input System package. Existing zero-GC claims were narrowed, not benchmarked.
4. Verify domain-reload-disabled and scene-reload-disabled play entry. The retained static reset hooks were reviewed, but instance rebinding when Awake does not rerun is not established by static inspection.
5. MouseEffect binds its manager on enable; replacing it still requires re-enabling the effect. Missing camera/input does not synthesize a Release event. Rendering/screenshots and retained-visual behavior for `disableOnRelease = false` need runtime QA.

No cross-owned code/config/doc edits or additional assembly references are required for the completed changes. The proposed buffered/typed adapter work would require coordinator-owned assembly changes. Shared test configuration, Unity execution and release validation remain reserved for the coordinator.
