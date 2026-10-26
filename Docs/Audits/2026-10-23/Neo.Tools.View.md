# Neo.Tools.View: audit

Status: full source review and focused fixes complete; static validation passed. Unity compilation and
Edit/Play Mode execution remain centrally pending. No runtime test pass is claimed.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/View/Neo.Tools.View.asmdef`.
- Scope: 10 C# files; nested assemblies excluded.

## Scope and completed source review

Read the complete repository `AGENTS.md`, worker contract, exact manifest entry and original report.
The manifest matches all ten direct C# files under `Assets/Neoxider/Scripts/Tools/View/`; no nested
assembly source was edited. Every file below was read in full, including conditional compilation branches.

| File | Reviewed contracts and outcome |
|---|---|
| `BillboardUniversal.cs` | Start/LateUpdate, injected/fallback cameras, warning latch, zero direction, Y projection, Undo preview, prefab-safe absence of OnValidate. Fixed camera-independent direction mode. |
| `DOTweenUIImageFallback.cs` | Entire DOTWEEN/no-UI-module branch, null rejection, fill getter/setter and tween target ownership. No demonstrated defect; unchanged. |
| `ImageFillAmountAnimator.cs` | Immediate/animated paths, finite-value normalization/inversion, inactive behavior, tween kill/disable target completion, destroyed Image guards, mode restoration, captured anchors, minimum width and warning gating. Existing focused tests cover these supported workflows; unchanged. |
| `LightAnimator.cs` | Light/optional Light2D initialization, reflection receiver types, original-value capture, Play/Stop/Pause/Resume, phase control, notification caches, disable restoration, debug gate, intensity authoring bounds. Fixed delegates and reset/readback consistency; replaced the existing local `var` with `Type`. |
| `MeshEmission.cs` | Missing renderer/material, original emission capture for both property names, cutoff/brightening, light lookup, material isolation, disable/destroy, external renderer replacement, readbacks and diagnostic gates. Fixed initialization, restoration and ownership. |
| `Selector.cs` | Every field, property and method: virtual/real items, offset/empty/fill/additive selection, child synchronization, SelectorItem bootstrap, active counts/events, persistence initialization, exclusions/unique model bridge, Mirror commands/RPC/late-join snapshots, authority/rate checks. Fixed startup writes, item bounds/empty notifications and bulk client exclusions. |
| `SelectorItem.cs` | Initial reactive state, repeated/forced notifications, inverse callback, parent-cache invalidation, index assignment, exclusion forwarding and absent-parent behavior. No demonstrated defect in intended parent-driven usage; unchanged. |
| `SelectorModel.cs` | All configuration/mutation/result paths, empty counts, looping/clamping, offset bounds, unique exhaustion/reset, exclusions, injectable random, serialization and collection inputs. Fixed singleton unique cycles, empty remaining count and direct collection aliasing. |
| `StarView.cs` | Explicit manager dependency, current-count display, null arrays/entries, reactive subscription lifetime, public Awake re-entry and externally owned visuals. Fixed null entries/source and subscription cleanup. |
| `ZPositionAdjuster.cs` | Complete world-space update/validation path and normalization dependency. Confirmed existing extension defaults are +/-1,000,000; deterministic authoring preview. No demonstrated defect; unchanged. |

Dependencies/tests inspected as context: component/test asmdefs, relevant selector/fill/billboard tests,
ScoreManager, Singleton, ReactiveProperty, VisualToggle, normalization extensions, AnimationUtils and
ISaveProvider/SaveProvider contracts. Reused package APIs after reading the matching neoxider-tools
reference: ReactiveProperty listener APIs, VisualToggle.SetActive, AnimationUtils and existing network
authorization/rate limiting. No replacement framework, package or assembly reference was added.

## Findings and completed fixes

Sources below use current line anchors within `Assets/Neoxider/Scripts/Tools/View/`.
Evidence is source/control-flow analysis plus authored behavior regressions, not an executed Unity repro.

1. **High — saved selector state overwritten before loading.** With saving and automatic startup enabled,
   OnEnable calls Set/child synchronization, then ApplyUpdateSelection calls SaveState. Previously these
   writes replaced `_Index` and could delete `_Excluded` before Start.LoadState read them. Saved selection
   therefore reverted on scene startup. `Selector.cs:605,989`: runtime writes are gated until Start finishes
   loading. Explicit runtime calls before that first Start also defer persistence; Edit Mode's existing
   explicit persistence behavior is retained. The startup regression asserts saved keys immediately after
   enable, loaded index/exclusions and actual active children after Start, then subsequent writes.

2. **Medium — singleton unique pools never exhausted.** The one-available-index shortcut ran before unique
   mode, repeatedly selecting an already consumed entry and omitting cycle/reset events. This also occurred
   with multiple items and all but one excluded. `SelectorModel.cs:266`: all unique requests now use the
   same unique-cycle path. No-item `UniqueRemainingCount` previously reported one because GetBounds returned
   `(0,0)`; `SelectorModel.cs:49` now returns zero. Regressions cover singleton and exclusion-reduced pools,
   automatic/manual reset behavior, result flags and zero/negative counts.

3. **Medium — restoring a model's own collections erased them.** SetExcludedIndices and
   SetUsedIndicesForUnique cleared their backing HashSets before enumerating an argument which could be
   that same exposed read-only collection. Configure accepted those inputs too. `SelectorModel.cs:331,350`
   preserves direct self-assignment; the regression asserts both histories survive and the next random
   selection respects them. Arbitrary deferred projections over the backing collection are outside this
   focused fix; callers can materialize those enumerables before replacing state.

4. **Medium — offset selection disagreed with visible objects after item changes.** Real-item updates
   clamped only the effective display index, while Value/Item retained an invalid logical index. Removal
   and refresh also clamped against raw Count rather than offset-aware bounds. A positive offset followed
   by removal could leave a visible selection but Item == null; a negative offset followed by an unchanged
   refresh could select a different object. Removing the final entry omitted the zero count/null-object
   notification. `Selector.cs:88,1591,1883` now normalizes logical bounds consistently and notifies the empty
   transition. Three regressions observe actual selected objects, activation and events.

5. **High — Light2D accessor initialization rejected valid URP lights.** Open delegates were declared with
   a Component receiver for methods declared on the derived Light2D type. That receiver signature is
   incompatible, so Delegate.CreateDelegate throws during Awake before animation can start.
   `LightAnimator.cs:405` now uses closed Func<float>/Action<float>/color delegates bound to the actual
   component. The optional URP regression animates and restores a real Light2D through the public component;
   it explicitly skips when URP is absent. URP/IL2CPP execution is still required centrally.

6. **Medium — reset/disable left stale light readbacks.** ResetToOriginal updated reactive intensity but
   left lastIntensity at the preceding animated value. If the next frame produced that same value, no
   update was published and CurrentIntensity stayed at the reset value while the light animated. OnDisable
   restored only hardware state. `LightAnimator.cs:226,290` resets caches and readbacks together. The Play
   regression uses a constant animated intensity to expose the stale-cache case, then checks disable.

7. **Medium — StarView initialization/updates failed on missing references and retained subscriptions.**
   A missing manager threw in Awake; a null array entry aborted subsequent slot updates. Repeated public
   Awake calls added callbacks, and destroying a view never explicitly unsubscribed. `StarView.cs:18,32`
   caches the subscribed ReactiveProperty, rebinds idempotently, skips absent slots and unsubscribes from
   the original property on destroy even if the public manager field changed. Regressions observe real
   GameObject/VisualToggle states and listener counts, including an external star surviving view removal.

8. **Medium — emission setup/restoration violated material and shader contracts.** Missing material caused
   null dereference; HDRP-only materials were read through `_EmissionColor` unconditionally. Destroying only
   the component destroyed its material without restoring the surviving renderer's source material.
   OnDisable restored emission but left CurrentIntensity/CurrentColor stale. `MeshEmission.cs:74,86,128,134`
   validates material presence, selects a supported emission property, explicitly owns a clone, restores
   the source only if the renderer still uses that clone, and resets readbacks. Regressions cover missing
   material, preserved source/replacement materials, deferred clone disposal, disable output and optional HDRP.

9. **Medium — custom-direction billboards required an unrelated camera.** ApplyRotation returned on null
   camera before reading TowardsDirection's custom vector, so a camera-free scene never rotated.
   `BillboardUniversal.cs:33,61` resolves cameras only in camera modes and permits direction-only rotation.
   The Play regression checks facing, Y projection and preservation of orientation for zero direction.

10. **Medium — client bulk exclusions bypassed the authoritative path.** SetExcludedIndices mutated only
    client-local state, unlike ExcludeIndex/IncludeIndex, causing the random pool to disagree with the server
    and late-joining clients. `Selector.cs:353,1497` routes bulk client changes through a command using the
    existing authorization/rate checks, then the existing server apply/broadcast path. The MIRROR branch was
    inspected statically; dedicated multiplayer execution is an explicit coordinator action below.

По-русски: исправлены потеря сохранения при старте, рассогласование индекса и отображения, уникальный
выбор из одного элемента, подписки StarView, доступ к Light2D и владение материалом. Unity не запускалась.

## Added regressions

- `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsViewSelectorTests.cs`: seven test methods / ten
  cases covering unique cycles, empty counts, self-assignment, offset removal/refresh and empty events.
- `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsViewLifecycleTests.cs`: eleven cases covering
  public facing, persistence, star display/subscription, light reset, material lifecycle and missing inputs.
  URP and HDRP cases explicitly skip only when the relevant optional backend is absent.
- Both new test files have unique GUID `.meta` files. They use existing test assembly references.
  The lifecycle fixture restores the previous SaveProvider and cleans up its own objects/materials;
  persistence uses an in-memory provider. No existing worker tests were edited.

## Documentation

Updated existing English public docs under `Assets/Neoxider/Docs/Tools/View/`:
`README.md`, `BillboardUniversal.md`, `LightAnimator.md`, `MeshEmission.md`, `Selector.md`,
`SelectorModel.md`, `SelectorItem.md`, `StarView.md`, `ZPositionAdjuster.md`.
They now explain typed C# usage, Inspector wiring, offset/unique contracts, initialization, subscription
and material ownership. Removed the `var` example and spurious LightAnimator nested-accessor fields.
The existing detailed ImageFillAmountAnimator guide required no changes. `MeshEmissionAnimator.md`
describes a different assembly's implementation and was deliberately left for that owner.

## Substantial improvement proposal — make SelectorModel authoritative between adapters

**Problem.** Selector's rules are already extracted, but the wrapper repeatedly calls Configure from
read-only getters and operations. Each operation copies exclusion/unique sets back into fields and
rebuilds/sorts Inspector lists, while activation, save initialization and network snapshots remain in
the same large component. This makes state ordering fragile and adds work proportional to the pool on
otherwise read-only queries. Client snapshot application can notify before all active-state data has
been applied. Legacy save keys also cannot distinguish an intentionally empty excluded set from absent
data because empty sets delete the key.

**Proposed design/public API (not implemented).** Keep and extend SelectorModel rather than introducing
a parallel selection service. Add `Apply(in SelectionCommand) -> SelectionChange`, `Revision`,
`CaptureSnapshot()` and `RestoreSnapshot(in SelectionSnapshot)`. Commands cover selecting/stepping,
random selection, configuration changes and replacing exclusions. A change contains final logical and
effective indices, active count and existing cycle/end flags. Snapshots contain only primitive state and
copied index collections, never GameObjects. The model remains usable from ordinary C# with an injected
random function. Recompute bounds/pool caches only when the relevant revision changes.

Keep Selector's serialized authoring fields, but initialize the model once and send subsequent changes
through its typed operations. Update Inspector snapshots lazily in editor/debug paths. Isolate small
persistence and network adapters around the existing ISaveProvider and Neo network contracts; do not add
new singleton managers or mandatory assembly dependencies. Apply complete authoritative snapshots before
emitting view/events. GameObject and SelectorItem rendering remain replaceable optional adapters.

**Compatibility/migration.** Preserve existing Selector/SelectorModel methods, fields, UnityEvents and
event order through forwarding APIs. Keep current exclusion semantics (random-only) and offset/empty/fill
behavior. Read legacy `_Index` / `_Excluded` keys; introduce an opt-in versioned snapshot key only when
explicitly migrating persistence, including a representation for saved-empty exclusions. Retain existing
Mirror entry points or document a coordinated wire-format migration. Scene-only users need no rewiring.

**Acceptance scenarios.**

1. Pure C# selection supports empty/virtual pools, positive/negative offsets, fill, excluded entries and
   singleton unique cycles with the same outputs as scene adapters.
2. Loading saved index and explicitly empty exclusions causes no intermediate writes or wrong-state
   callbacks; scene reload/re-enable follow documented behavior and legacy keys still load.
3. Host, remote client and late joiner agree on additive active sets and bulk exclusions; unauthorized
   commands leave all peers unchanged, and callbacks observe the final authoritative state.
4. Warmed-up CountActive/IsAtStart/IsAtEnd/UniqueRemainingCount reads do not rebuild exclusion sets or
   Inspector lists; measure allocations before/after at representative UI and large virtual-pool sizes.
5. Existing scenes/serialized fields and the existing plus new behavior regressions survive the migration;
   centrally compile both supported Unity version families and optional Mirror configurations.

**Effort/risk.** Approximately 3–5 engineering days plus central integration validation, assuming working
Mirror test projects. Medium/high behavior-compatibility risk around event order, loading and wire state;
introduce internal adapters first, keep compatibility tests, then expose additive snapshot/command APIs.
This proposal is intentionally separate from the focused completed fixes.

## Actual verification and coordinator actions

- Verified HEAD is `ca1985564dfe7726834e2832d51c33e0e63e060c` and manifest inventory equals all ten owned
  direct C# files. Read-only comparison against that revision preserved public declaration lines and
  serialized private field names across all ten sources; manually reviewed the complete source diff.
- Inline Python static checks passed for 24 source/test/doc/meta files: strict UTF-8 decoding, no U+FFFD
  replacement characters, no trailing whitespace; no `var` locals and balanced preprocessor directives
  in all twelve C# files. These are consistency checks, not C# compilation or test execution.
- Relative links in all ten in-scope entry/component docs resolve. Both test filenames use the manifest
  prefix; their GUIDs are unique across `Assets/**/*.meta`. Counted 21 authored regression cases.
- `git diff --check -- <owned source/docs>` passed, exit 0. Git emitted only normal LF-to-CRLF conversion
  notices for the touched LightAnimator source/doc. The new untracked tests/meta were additionally checked
  for whitespace by the inline scan. The report receives a final scoped whitespace check as well.
- No Unity/MCP, Play Mode, compiler, build/test runner, install, shared config edit, commit or child agent
  was used. Validation is local/static and self-reviewed; independent QA is centrally pending.

Coordinator: run the ten Edit cases and eleven Play cases, existing selector/fill/billboard regressions,
and compile with/without MIRROR in the supported Unity version matrix. Run the optional real Light2D test
in URP (including IL2CPP preservation) and the HDRP property test in HDRP. The current project manifest
does not list URP, so its presence must not be assumed. No new test assembly reference is requested.

Dedicated network integration still needed: authorized client SetExcludedIndices (including null/empty)
updates the server and another/late-joining client; unauthorized or rate-limited requests do not change
server state. This scenario requires central network lifecycle infrastructure, so no misleading local-only
test is substituted for it. Pipeline rendering/material disposal must also be checked in actual Unity.
