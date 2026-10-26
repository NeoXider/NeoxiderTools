# Neo.Tools.InteractableObject: audit

Status: full owned-source review completed; focused fixes and regressions authored. Unity compilation,
test execution, and runtime verification remain centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/InteractableObject/Neo.Tools.InteractableObject.asmdef`.
- Scope: 6 C# files / 2,490 original lines, all reviewed completely; nested assemblies excluded.
- Ownership: exact `Neo.Tools.InteractableObject` manifest entry; test prefix
  `Audit20261023NeoToolsInteractableObject`. No commits, shared configuration changes, Unity/MCP,
  builds/test runners, installs, or child agents.

## Scope

Owned source root: `Assets/Neoxider/Scripts/Tools/InteractableObject/`.

| File | Original lines | Review coverage and disposition |
|------|----------------|---------------------------------|
| `InteractionRayProvider.cs` | 165 | All modes, pointer/device boundaries, viewport math, camera ownership/cache, disable/domain-reset behavior; fixed. |
| `InteractiveObject.cs` | 1,408 | All serialized fields, Awake/Update, mouse/keyboard/pointer paths, UI, hover/range/double-click state, collider cache invalidation, 2D/3D obstacle queries, hierarchy filtering, Mirror commands/RPCs, diagnostics and public APIs; fixed. |
| `Internal/InteractiveObjectSceneSetup.cs` | 211 | EventSystem creation/options, optional Input System reflection, delayed module destruction, camera/raycaster setup and static reset; fixed. |
| `PhysicsEvents2D.cs` | 321 | All six callbacks, both event surfaces, layer/tag filters, solo/server/client/host routes and payload limitations; fixed. |
| `PhysicsEvents3D.cs` | 326 | All six callbacks, both event surfaces, layer/tag filters, solo/server/client/host routes and payload limitations; fixed. |
| `ToggleObject.cs` | 59 | Reactive assignment/event ordering, repeated Set, direct Value writes, debug validation, Play Mode buttons and null assumptions; runtime retained, inaccurate docs corrected. |

The nested `Core` assembly is excluded. Its camera resolver and query math were read only as
dependencies, as were `NeoNetworkComponent`, `NeoNetworkState`, `MouseInputCompat`, relevant reactive
property behavior, existing interaction tests and test asmdefs. No dependency, existing test, nested
assembly source/doc, scene, prefab, sample layout or package settings were edited.

Reuse: checked the installed `neoxider-tools` interaction/physics and extension references. Reused
`InteractionQueryMath`, `InteractionRayHit`, `InteractionCameraResolver`, input compatibility APIs,
`NeoNetworkState`, the existing per-connection `RateLimitCheck(sender)`, and reactive state. The new
internal ray/hover helpers only consolidate this component's policies; no new framework was added.

## Findings and fixes

These are source-proven traces against the baseline, not claims of executed Unity reproductions.

### P1: physics events disappeared in solo mode with networking enabled

**Trigger:** `isNetworked = true`, with Mirror absent or no active network session.
**Source:** every `OnTrigger*` / `OnCollision*` callback in both physics relay files tested only
`!isNetworked` before local dispatch. Without a server, neither branch invoked either event surface.
**Consequence:** configured triggers/collisions silently stopped firing offline.
**Fix:** all 12 callbacks also dispatch locally when `!NeoNetworkState.IsNetworkActive`; active-session
server/RPC/host semantics remain intact. Four parameterized regression cases exercise all trigger
phases with networking on/off in both dimensions; eight more retain filter/gate behavior and check
both C# and UnityEvent outputs. Native collision payloads still need central physics execution.

### P1: remote commands bypassed disabled interaction and shared the wrong rate budget

**Trigger:** a remote command arrives after the server disables `interactable` / `isNetworked`, or
another client sends unauthorized/high-frequency requests to the same object.
**Source:** `InteractiveObject.CmdInteractDown`, `CmdInteractUp`, `CmdClick` used the instance-wide
rate limiter before authorization and did not check either enable flag.
**Consequence:** a stale/malicious client could invoke disabled server interaction; one connection
could consume another connection's command budget, including through unauthorized requests.
**Fix:** validate both flags and sender authority first, then call the existing per-connection rate
limiter. Click commands additionally reject invalid button values. The dependency overload exists
at `ca198556`; no shared API/config change was required. Mirror weaving and two-client behavioral
regressions require coordinator execution; no network pass is claimed here.

### P2: Both mode clicked the hover target and cursor events overrode crosshair hover

**Trigger:** camera provider uses `Both`, with cursor and crosshair targeting different objects.
**Source:** `TryGetCurrentMouseTargetHit` read only `UseScreenCenterForHover`; its cached hit fed
`UpdateMouseInput` and `OnPointerClick`. Pointer handlers independently overwrote hover.
**Consequence:** clicks could activate the center target while the cursor pointed elsewhere, and a
cursor enter/exit could flash or clear crosshair hover.
**Fix:** resolve separate hover/click hits when their ray modes differ, share the query when they
match, and make click eligibility independent of hover state. Valid world pointer notifications in
center mode reconcile against the center query. Three cases cover cursor-miss/center-hit clicks and
EventSystem enter/exit arbitration in both center-hover modes.

### P2: offset camera viewports generated the wrong center ray

**Trigger:** a split-screen/offset camera viewport; or unavailable/non-finite/outside-viewport input.
**Source:** both components computed screen center from `pixelWidth/2`, `pixelHeight/2`, omitting
`pixelRect` origin. The provider's public mouse-ray methods had no finite/viewport guard.
**Consequence:** center interactions missed the crosshair; invalid device coordinates could reach
camera projection/logging code. The old object guard also rejected valid absolute screen positions
inside offset viewports.
**Fix:** one internal ray helper uses `ViewportPointToRay(0.5, 0.5)` and validates finite pointer
coordinates against `pixelRect`, including zero-size viewport rejection. Provider camera lookup is
lazy so its public query API works before Awake. Two ray-output cases cover offset viewports; the
Both-mode target test exercises the same helper through `InteractiveObject`. Sentinel/device cases
still require central Input System QA.

### P2: main-camera provider cache returned stale or disabled components

**Trigger:** retag cameras while the previous provider stays enabled, remove the main tag, or disable
the provider attached to the main camera.
**Source:** `InteractionRayProvider.FindOnMainCamera` returned its live cache before checking the
current main camera; the slow path returned a disabled provider as well.
**Consequence:** old camera modes remained global, and disabling the provider did not stop its use.
**Fix:** resolve the current main camera before reusing the cache; return null for a disabled provider
or absent main camera. Two public lookup regressions cover camera replacement and disabled/untagged
cases. This does not change the separate Core cached-camera policy or reassign checkpoints.

### P2: hover transitions repeated or stayed latched after availability changes

**Trigger:** duplicate pointer notifications, hover detection off, `interactable` off, component
disable, or loss of the camera while a raycast hover is active.
**Source:** pointer and ray paths invoked hover events independently; exit was gated by the same
flags that had just been disabled, Update returned early, and no disable cleanup existed.
**Consequence:** repeated events or a permanently highlighted target with stale input history.
**Fix:** idempotent hover transitions, unconditional necessary cleanup, disable/reset handling,
immediate hover-feature cleanup and lost-camera exit. Five cases check actual state and event
sequences. Edit Mode lifecycle callbacks are explicitly driven with `SendMessage`; they do not
substitute for central lifecycle/Play Mode testing.

### P2: scene setup skipped subsequent cameras and confused 2D with 3D raycasters

**Trigger:** setup runs for another camera after an earlier target, or a camera needs both dimensions.
**Source:** process-wide `_physicsRaycasterEnsured*` flags skipped new cameras; a
`Physics2DRaycaster` also satisfies `GetComponent<PhysicsRaycaster>()` because it derives from it.
**Consequence:** the new camera or mixed scene lacked its actual 3D/2D EventSystem raycaster.
**Fix:** idempotent per-camera component checks that distinguish 2D from 3D raycasters. One regression
asserts exactly one of each on two successive cameras. Input System-only setup now also disables a
legacy module before deferred Destroy, preventing it from continuing to process input that frame;
that particular branch remains runtime-unverified.

### P2: UI clicks required an impossible cached physics hit

**Trigger:** `InteractiveObject` on a RectTransform under a Canvas with a valid graphic pointer hit
and no collider, as advertised in the existing guide.
**Source:** `OnPointerClick` returned whenever `hasCurrentMouseHit` was false; only physics queries
could set it.
**Consequence:** UI hover could work while its click callbacks never fired.
**Fix:** accept a valid UI pointer hit on this object's hierarchy and apply the existing distance
policy to that hit. Null event data and unrelated UI hits are rejected. One regression checks an
unrelated-hit rejection followed by a real target click without either collider type.

### P2: a common scene root hid targets and sibling walls

**Trigger:** camera/checkpoint, target and walls are organized beneath the same level container.
**Source:** `ShouldIgnoreHitCollider` ran before target classification and `IsSameHierarchy` treated
every object sharing the checkpoint root as the player rig.
**Consequence:** even the target was discarded; common-root walls could also escape obstacle checks.
**Fix:** never ignore a selected target, and preserve whole-rig exclusion only for a root separate
from the target's root. Shared-root exclusion uses actual ancestor/descendant relationships. Two
real-physics-query regressions assert reachable target versus blocking sibling wall.

### P2: mouse release could emit interact-up without a target press

**Trigger:** press elsewhere, then release the configured button over this target.
**Source:** `UpdateMouseInput` checked the release's current target but not whether this object
accepted the press.
**Fix:** require `mouseButtonsPressedOnObject` for the up callback as well. The existing rule still
requires an eligible current target; this is not a global pointer-capture redesign. Device-driven
press/release verification remains central.

### Other reviewed contracts

- Serialized names/defaults and public field/method declarations are retained. No ScriptableObjects
  or new scene-object references were introduced.
- Collider selection remains same-object then first child, with explicit target override and explicit
  runtime invalidation. Corrected tooltips that incorrectly claimed same-object-only fallback.
- Keyboard range/look and trigger policies continue to use the existing Core math/input adapters;
  first-click/double-click and same-frame click deduplication were inspected and retained.
- No direct obsolete-as-error identity APIs were introduced. Runtime diagnostics remain behind the
  existing `NeoDiagnostics` gate; debug-ray drawing remains opt-in.
- Filter changes do not synthesize overlap exits; undefined tags retain Unity's CompareTag contract.
  Native remote collision payloads remain null; compound collider identity is not reconstructed.
- `ToggleObject.Set` deliberately repeats ON/OFF/OnChangeFlip action events even for the same value;
  reactive notifications only report changes. Direct reactive writes do not run those action events.
  These existing semantics were documented, not silently changed.

## Documentation

Updated all six owned existing English entry/component pages under
`Assets/Neoxider/Docs/Tools/InteractableObject/`:

- `README.md`: runtime versus scene usage, ray-provider link, UI setup and solo/network behavior.
- `InteractiveObject.md`: split hover/click sources, pointer/hover lifecycle, hierarchy filtering,
  UI click path, public networking options, actual collider fallback and Awake-only setup limitation.
- `InteractionRayProvider.md`: replaced placeholder text with a mode table, complete public API,
  viewport/input validation, lazy camera lookup and current-main/disabled-provider behavior.
- `PhysicsEvents2D.md` / `PhysicsEvents3D.md`: exact filter switches, body requirements, C# event
  subscriptions, solo/server/client/host behavior and nullable remote payload contract.
- `ToggleObject.md`: corrected nonexistent `value` / component `OnChange` names to `Value`,
  `ValueBool`, `Value.OnChanged`; documented repeat-Set and direct reactive-write behavior.

No redundant guide, nested Core document, DocsEn, root/package README or changelog was changed.

## Substantial improvement proposal

### Explicit query and input-session API (proposal only, not implemented)

**Problem.** `InteractiveObject` still owns camera lookup, input sampling, physics, query policy,
state transitions, networking and authoring setup. Every object repeats broad physics queries into
fixed 16-hit buffers; a saturated unsorted 3D buffer can omit the nearest target/blocker. Camera and
checkpoint caches are not a declared session, late cameras do not rerun authoring setup, and raw
down/up callbacks do not provide capture/release outside range. Tests must drive Unity callbacks
instead of testing those state transitions as ordinary C#.

**Proposed design/public API.** Add small scene-independent request/result and session types in the
Core owner, with a Unity physics adapter in this assembly:

- `InteractionRequest` explicitly contains hover/click rays, range origin/distance and query policy.
- `InteractionEvaluation` returns target hit, blocker, eligibility and an explicit saturation status.
- `InteractionSession.Update(evaluation, input)` emits typed hover/press/release/cancel transitions;
  `Cancel()` ends a held session on disable, target loss or input-source removal. Capture behavior is
  an explicit policy, rather than requiring every consumer to infer it from raw callbacks.
- `IInteractionHitSource` supplies candidate hits; a reusable Unity adapter grows/retries buffers on
  saturation rather than treating a full buffer as complete. Share identical rays per input source
  and frame before considering broader optimization.
- Add explicit component configuration such as `Configure(Camera, Transform distancePoint,
  Transform viewPoint)` and `PrepareCamera(Camera)`, plus an explicit ignore-root/collider selection.
  Scene search/automatic setup remains an opt-in fallback; no hidden second global manager.

**Compatibility/migration.** Keep current component, serialized field names, UnityEvents,
`IInteractiveTarget`, manual commands and networking route. The MonoBehaviour adapts existing
settings into the new evaluator/session; existing serialized defaults preserve legacy raw callback
behavior. New capture/cancel rules and explicit camera/ignore-root configuration are opt-in.
Do not silently change `InteractionCameraResolver.Resolve(cachedCamera)` retention semantics.
Coordinate the Core API with that assembly's owner; no shared asmdef edits in this worker.

**Acceptance scenarios.** (1) More than 16 unsorted collider hits still select the true nearest
target/blocker, or return explicit incomplete status. (2) Center-hover A and cursor-click B produce
one coherent set of transitions independent of EventSystem update order. (3) Component disable,
lost input and target removal cancel captured input once. (4) Late/switching cameras can be explicitly
prepared and selected without recreating targets. (5) Shared level roots retain walls while an
explicit player ignore root excludes the intended rig. (6) Existing scene data, manual C# APIs,
2D/3D trigger policies, solo mode and Mirror authority behavior remain compatible.

**Effort/risk.** Approximately 3–5 engineering days plus central multi-version/Mirror QA. Medium
migration/event-order risk; buffer allocation policy and capture semantics need focused acceptance
tests. Stage the pure evaluator/session first, then adapt the component behind compatibility defaults.

## Verification and remaining risks

Actual read-only/static checks performed:

1. Nearest-asmdef inventory comparison: **6/6** files exactly match the manifest; Core excluded.
2. Strict UTF-8 decoding, replacement-character and trailing-whitespace checks on all six sources,
   six docs, two new tests and two metadata files: **passed**.
3. Baseline declaration comparison against `ca198556`: **28 explicit serialized fields**, public
   field declarations and public method signatures preserved across all six sources. This is a
   targeted declaration check, not a compiler or serialized-asset migration test.
4. Eight C# files checked for explicit local types, forbidden obsolete identity calls, balanced
   lexical delimiters and MIRROR on/off preprocessor branches: **passed**. This is not compilation.
5. Relative links in all six owned docs resolve: **passed**.
6. New GUID lookup over package metadata matched only the two new test metadata files:
   `ccb667d691cd4a2984308b2696f920f1`, `54d254428398464ebfbd543cf8c278a2`.
7. `git diff --check -- <owned sources/docs/new tests/metas/report>`: **exit 0**. Git reports only
   normal LF-to-CRLF working-tree notices; no whitespace errors. Untracked files were also checked
   directly because ordinary git diff does not include their content.

New tests in `Assets/Neoxider/Tests/Edit/Tools/`, each with a unique `.meta`:

- `Audit20261023NeoToolsInteractableObjectInteractionTests.cs`: **16 cases** for hover, lifecycle
  callback boundaries, camera loss, Both/crosshair arbitration, viewport rays, provider lookup,
  mixed raycasters, UI targeting and shared-root obstruction.
- `Audit20261023NeoToolsInteractableObjectPhysicsEventsTests.cs`: **12 cases** for offline trigger
  enter/stay/exit, exact collider payloads, both event surfaces and disabled/layer/tag filtering.

**28 authored cases; none executed by this worker.** Existing tests/asmdefs were not edited, and
the test assembly already references the required module, input, network and UnityEditor APIs.
Central compilation will determine whether any additional assembly reference is necessary.

Coordinator actions / remaining risks:

- Compile both supported Unity API generations (6.0–6.4 and 6.5+), and MIRROR/no-Mirror runtime
  configurations; lexical branch checks do not establish compiler compatibility.
- Run the two new fixtures and existing interaction/physics-network suites. Execute native 2D/3D
  collision, actual disable/re-enable, pointer press/release, Input System-only/sentinel and UI input
  scenarios. No Unity, console, Play Mode, physics simulation or build was started here.
- Exercise disabled/unauthorized remote commands and rate isolation with two remote connections;
  those paths have source checks but no newly executed network regression.
- The existing Core resolver intentionally keeps a live supplied cached camera; provider lookup now
  follows Camera.main independently. Dynamic camera/checkpoint reassignment requires coordination
  with `Neo.Tools.InteractableObject.Core` and explicit configuration as proposed above.
- Fixed 16-hit buffers, Awake-only automatic raycaster setup, remote compound-collider payload loss
  and release-outside-target behavior remain explicit limitations. They are not claimed fixed by
  the current patch. No cross-boundary source/doc change was attempted.

Кратко: просмотрены все 6 файлов сборки; исправлены подтверждённые по исходникам ошибки,
обновлены публичные руководства и добавлены 28 регрессионных сценариев. Реальные Unity/Mirror
проверки не запускались и остаются задачей координатора.
