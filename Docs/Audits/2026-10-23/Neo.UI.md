# Neo.UI: audit

Status: source audit complete; focused fixes and regressions added. Unity compilation and test execution remain with the coordinator.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/UI/Neo.UI.asmdef`.
- Scope: 28 C# files; nested assemblies excluded.

## Scope and review receipt

Read `AGENTS.md`, `WORKER-CONTRACT.md`, the exact `Neo.UI` manifest entry, and this report before changing source. All 28 inventoried C# files were read completely (8,371 current source lines after fixes). Paths in this table are relative to `Assets/Neoxider/Scripts/UI/`.

| File | Review performed / outcome |
|---|---|
| `AnchorMove.cs` | Editor validation, missing RectTransform, anchor and position writes. No demonstrated change required; authoring-only API. |
| `Animation/ButtonScale.cs` | Awake/enable/disable, coroutine replacement, zero duration, base Z scale and pointer/code paths. Existing Z regression retained. |
| `Animation/ButtonShake.cs` | Shake start/stop/re-enable, target lifetime, indefinite duration, local Z. Fixed depth loss and inactive-start guard. |
| `AnimationFly.cs` | Entire legacy/typed surface, every trajectory, coordinate conversion, request/result callbacks, count/timing limits, tween ownership, pool reuse. Fixed four defects below. |
| `AssemblyInfo.cs` | Existing friend assemblies match runtime/editor/test boundaries. Unchanged. |
| `CategoryBar/CategoryBar.cs` | Initialization, runtime list replacement, view-less selection, id/index events, disabled navigation, authored/spawned views and marker ownership. Fixed startup/list alias/listener/resource defects. |
| `CategoryBar/CategoryBarItem.cs` | Optional text/icon/Button references, interactability and selected visuals; preserved authored layout. No separate fix required. |
| `MeshRig/IUIMeshRigOwner.cs` | Owner interface, nearest-ancestor lookup, nested owner exclusion. Existing interface reused. |
| `MeshRig/UIMeshRigAuthoringMode.cs` | Serialized enum values and authoring transitions. Unchanged. |
| `MeshRig/UIMeshRigElement.cs` | UXML properties, aspect/y-axis/UV conversion, index limits, procedural point generation and attach/detach schedule ownership. Performance opportunity recorded below. |
| `MeshRig/UIMeshRigFalloffPreset.cs` | Preset coverage and fallback curve construction. Unchanged. |
| `MeshRig/UIMeshRigGeometry.cs` | Grid/index bounds, UV generation, negative-size coordinate spaces, influence ellipses, weight composition, rotation/scale and aspect fitting. Existing pure builder reused. |
| `MeshRig/UIMeshRigGraphic.cs` | Source/layout API, point cache, preview/bind behavior, mesh/raycast caches, alpha-read fallback, raycast padding and Canvas event subscriptions. No version branch modified. |
| `MeshRig/UIMeshRigLayoutPreset.cs` | All layouts, owner overloads, point creation, rest capture and replacement. Fixed uGUI placement for aspect-fitted sprites. |
| `MeshRig/UIMeshRigMotionPreset.cs` | Every preset, mutable profile cloning, copied curves/wrap modes and migration attributes. Unchanged. |
| `MeshRig/UIMeshRigPoint.cs` | Serialized version migration, radius/channel clamps, rest/reset/pose calculations, transient state and owner changes. Fixed previous-owner cache invalidation. |
| `MeshRig/UIMeshRigPointMotion.cs` | Runtime/editor lifecycle, preview gate, pause/resume/restart/time, profile selection and transient pose cleanup. Unchanged. |
| `MeshRig/UIMeshRigProceduralPose.cs` | Pure curve/noise evaluation, phase, seed/hash, duration/speed and scale floor. Unchanged. |
| `MeshRig/UIMeshRigRaycastMode.cs` | Enum values and adapter usage. Unchanged. |
| `MeshRig/UIMeshRigSpriteRenderer.cs` | Clone/source ownership, NativeArray disposal, mesh writes, bounds headroom, setup/pose and resource release. Source assets remain untouched. Packed-sprite/render behavior requires central visual verification. |
| `MeshRig/UIMeshRigUIToolkitHost.cs` | Full host and compatibility backends, subscriptions/root replacement/detach, UIDocument fallback and PanelRenderer version gate. Both version branches read; neither compiled here. |
| `MeshRig/UIMeshRigWorldRenderer.cs` | Mesh/material/property-block ownership, geometry dirtiness, source/size/PPU and point lifecycle. Unchanged. |
| `PausePage.cs` | Time scale, GM and cursor snapshots/restoration, overlap order and unscaled Animator authoring. Overlap limitation documented; substantial shared-ownership proposal below. |
| `Simple/ButtonChangePage.cs` | Click/submit gates, left-click filtering, navigation, tween lifecycle and Selectable state. Fixed CanvasGroup interaction bypass. |
| `Simple/FakeLoad.cs` | Static reset, one-shot/revisit paths, coroutine progress and terminal events. Existing final-progress/revisit fixes retained. |
| `Simple/UI.cs` | Startup ids, empty/destroyed pages, cache/static lifetime, selection/events, delayed/animated and legacy paths. Fixed missing-array and stale-object handling. |
| `View/VariantView.cs` | Every variant type, navigation, authoring expansion/clear/validation and duplicated object slots. Fixed empty-state index and duplicated-slot visibility. |
| `View/VisualToggle.cs` | Optional Toggle resolution, startup/event/reactive state, ToggleGroup constraints, visual targets and listener cleanup. Fixed initialization/event consistency and rejected-toggle synchronization. |

Nested `Neo.UI.Editor` and `Neo.UI.HtmlToUitk*` source/docs were excluded. No shared config, asmdefs, scenes, prefabs, samples or existing tests were edited. New tests use five exact filenames below; another worker's `Audit20261023NeoUIEditorAuthoringTests.cs` is excluded even though its prefix also starts with `Audit20261023NeoUI`.

Reuse receipt: consulted the `neoxider-tools` UI/AnimationFly catalog and extension references, current canonical UI docs, existing CategoryBar/VisualToggle/ButtonScale/MeshRig/AnimationFly tests, `ReactiveProperty.SetValueWithoutNotify`, and `Singleton.OnDestroy`. Fixes adapt existing components and reuse the existing owner resolver/geometry conversion/reactive API; no parallel gameplay system or dependency was added. Work stayed local under the user's prohibition on Unity, test runners and child agents; this is not independent runtime QA.

## Findings and fixes

### Completed fixes

Severity uses P2 for observable functional/lifecycle defects and P3 for the empty authoring-state invariant. Evidence below is source-path reproduction and newly authored behavioral assertions; Unity execution is still pending.

| ID | Severity, trigger and original observable consequence | Source and completed fix | Regression evidence |
|---|---|---|---|
| UI-01 | P2: add an unconfigured `UI`, or select a page whose cached GameObject has been destroyed. Awake dereferenced a null `_pages` array; cached selection called `SetActive` on a destroyed object and prevented page events. | `Simple/UI.cs`: initialize/normalize the empty array in `BuildPageCache`, and validate a cached Unity object before activating it. | Lifecycle tests assert empty-list selection still publishes the chosen id, and a formerly active destroyed cached page still emits `OnChangePage`. |
| UI-02 | P2: destroy the current `UI`, then call a `ButtonChangePage` forwarder. Static `UI.I` retained a destroyed managed reference, so null-conditional calls could reach a missing Unity component. | `Simple/UI.cs:OnDestroy` clears the field only when this object owns it. | Lifecycle test destroys an older manager while a newer manager is current, then verifies actual reference-null after the current owner is destroyed. |
| UI-03 | P2: a button is inside a non-interactable CanvasGroup. Its Selectable's raw `interactable` remained true and click/submit/code execution could still navigate and invoke `OnClick`. | `Simple/ButtonChangePage.cs:Interactable` uses `Selectable.IsInteractable()`, including group state. | Lifecycle test observes zero clicks while the group is disabled and one click after restoring interaction. |
| UI-04 | P2: create VisualToggle from code beside a Toggle, or load an initially true view with startup events disabled. The reference was only editor-injected; `ValueBool` could remain false while the visual/IsActive state was true. On/Off listeners also read the previous reactive value. | `View/VisualToggle.cs:Awake` resolves the optional Toggle and silently initializes the reactive property; `InvokeEvents` updates it before state events. | Two real-Awake tests cover linked and standalone startup with `_setOnAwake=false`; an EditMode test reads `ValueBool` inside On/Off callbacks. |
| UI-05 | P2: call `SetActive(false)` on the last selected Toggle in a group that forbids switching off. Unity rejects the change, but the view previously forced its own state false. | `View/VisualToggle.cs:SetActive` applies the actual linked Toggle state after the request. | Lifecycle test checks Toggle.isOn, IsActive and ValueBool all stay true. |
| UI-06 | P2: set categories with an initial index before Initialize/Start, or pass `bar.Categories` back to SetCategories. The initial index was discarded; aliasing erased the input before enumeration. | `CategoryBar/CategoryBar.cs:SetCategories` snapshots before replacing and retains an explicit startup index. | Tests assert disabled initial entries advance to the next enabled item and self-input preserves entries/selection. |
| UI-07 | P2: rebuild/shrink a category list or destroy only its bar component. Old owned listeners remained on authored buttons, unused views remained interactive, and spawned views under an external root outlived their owner. | CategoryBar now removes only its owned handlers, retires excess authored views, releases spawned views on destroy and detaches the shared marker before release. Edit-time instance cleanup uses DestroyImmediate only on spawned scene instances. | Tests assert retired/restored interaction, no selection after bar removal while consumer clicks still work, external-root child counts, template survival and marker survival across rebuild/destroy. |
| UI-08 | P2: enable/stop/re-enable ButtonShake at nonzero local Z. Its Vector2 snapshot restored Z as zero. Calling Shake while inactive could also attempt a coroutine on an inactive object. | `Animation/ButtonShake.cs` stores a Vector3 snapshot, guards inactive starts and tolerates a destroyed target during cleanup. | Lifecycle test observes the complete local position before shake, after stop and after re-enable. Inactive guard/target-null branch were statically reviewed. |
| UI-09 | P2: create a uGUI layout for a wide/tall sprite inside a square RectTransform with Preserve Aspect. Full-rect anchors were captured as drawing-area coordinates, moving rest centers away from the requested layout. | `MeshRig/UIMeshRigLayoutPreset.cs:ApplyPointTransform` uses the existing owner's NormalizedToWorld conversion before rest capture. | Two aspect-ratio cases use an off-center pivot and assert the requested normalized center in both rest data and actual world placement. |
| UI-10 | P2: reparent a point between intermediate containers under different rigs after both caches were populated. Only the new owner was invalidated; the old rig retained/deformed a foreign point. | `MeshRig/UIMeshRigPoint.cs:ResolveOwner` invalidates a live previous owner before assigning the nearest new owner. | Three adapter cases (uGUI, world mesh, SpriteRenderer) assert the old list empties and the new list contains precisely that point. |
| UI-11 | P2/P3: AddState copies one GameObject into another slot; selecting the earlier slot then enabled and immediately disabled that same object. Empty OnValidate also clamped an index against an upper bound of -1. | `View/VariantView.cs` activates by selected-object identity and explicitly keeps empty-state index zero. Replaced the touched generic helper's pre-existing `var` with `T[]`. | Inspector-configured tests observe both shared slots remain visible and an empty configuration retains index zero. |
| UI-12 | P2: pool a visual under a container, then request it with no resolved parent; or pool the same Sprite first on Canvas, then in World. Reuse retained the old parent, or returned an Image where a SpriteRenderer was required. | `AnimationFly.cs:TryTakeFromPool` always reparents, including null; pool keys now include resolved spawn space. | Play tests assert identity reuse with a null parent and correct Image/SpriteRenderer output for the same sprite in different spaces. |
| UI-13 | P2: destroy an AnimationFly whose active/pooled objects are scene roots or children of an external object. They survived their owner, along with independent tweens/callbacks. | `AnimationFly.cs` tracks owned visuals across legacy and typed flows, kills/releases them on OnDestroy and transfers completed KeepAlive/manual legacy visuals to the caller. The singleton base cleanup still runs. | Play tests assert external pooled/active objects disappear, completed KeepAlive survives, no reward is delivered for interrupted flights, and legacy in-flight visuals are also released. |
| UI-14 | P2: use a Screen Space Camera Canvas with an untagged explicit worldCamera and omit the helper's optional camera. Static conversion used Camera.main/null instead of that Canvas's camera. | `AnimationFly.cs:WorldToCanvasPosition/CanvasToWorldPosition` prefer the supplied Canvas camera before the existing fallback; explicit camera priority is retained. | EditMode assertions compare both conversions with RectTransformUtility and verify explicit-camera override. |

Public/serialized compatibility: public signatures, serialized field names, enum values and migration attributes remain intact. The shake snapshot and pool/ownership collections are private runtime state. New layout placement affects newly created/reapplied points; saved bind data is not automatically rewritten. Owner destruction now cleans up active/pooled effects; completed KeepAlive is explicitly preserved.

## Documentation

Updated existing English entry/guide pages only, all under `Assets/Neoxider/Docs/UI/`:

- `README.md`: canonical entry links and repaired the malformed component table.
- `UI.md`: empty-list behavior, startup versus explicit none, current-owner static cleanup and code/UnityEvent usage.
- `ButtonChangePage.md`: real click/navigation API and effective CanvasGroup interaction state.
- `VisualToggle.md`: corrected the nonexistent `OnValueChanged` event and writable-IsActive instructions; documented subscriptions, silent initialization, event state and ToggleGroup constraints.
- `VariantView.md`: replaced field-name placeholders with actual authoring, runtime navigation and shared-slot contracts.
- `CategoryBar.md`: code setup, pre-Start initial index, list snapshot, UnityEvents and view/listener ownership.
- `ButtonShake.md`: preserved depth, disable/stop behavior and public code path.
- `AnimationFly.md`: pool representation/parent rules, owner destruction, KeepAlive transfer and camera precedence.
- `UIMeshRig.md`: normalized layout placement and point reparenting contract.
- `PausePage.md`: real active-screen workflow/API and the overlapping-owner limitation.

Русское резюме: проверены все 28 файлов. Исправлены подтверждённые ошибки состояния и владения объектами; публичные API и сериализованные имена сохранены. Unity, сборки и тестовые раннеры не запускались по контракту.

## Substantial improvement proposal

### Proposed, not implemented: composable pause ownership

**Problem / remaining P2 limitation:** `PausePage.OnEnable/OnDisable` stores/restores independent snapshots of global time scale and cursor state and unconditionally pairs GM Pause/Resume. Open A at scale 1, open B while A has set scale 0, close A, then close B: A restores 1 while B is still open, then B restores its snapshot 0 after the last page closes. A pre-existing GM pause may similarly be resumed by a page that did not own it. A local snapshot cannot correctly represent multiple independent owners.

**Architecture/public API:** introduce a small ordinary C# `PauseLeaseRegistry` that accepts `PauseRequest` values and returns `IDisposable` leases via `Acquire(request)`. It computes the effective paused state while any relevant lease remains, with an explicit policy for conflicting requested time scales/cursor modes. A scene `PauseCoordinator` adapts that state to Time, cursor and an explicitly supplied GM adapter. PausePage gets an optional serialized coordinator reference and holds one lease from OnEnable until OnDisable. No scene references belong in ScriptableObjects. A game's ad, modal, gameplay pause and menu systems can call the same typed `Acquire` API without depending on a particular screen.

**Compatibility/migration:** retain all current PausePage fields and the snapshot fallback when no coordinator is assigned. Migrate one scene at a time to a common coordinator; document ownership of externally applied time-scale changes and GM's previously paused state. The existing default is not silently reinterpreted. A `ForceLockedHidden` request needs an explicit arbitration/release rule instead of whichever page closes last.

**Acceptance:** EditMode tests exercise A/B release in both orders, duplicate Dispose, acquisition during an existing pause and conflicting cursor/time requests. PlayMode checks verify two pause pages, an ad pause plus a menu, scaled and unscaled animation, scene unload and cursor restoration. Removing one lease must never resume while another pause lease remains; removing the final lease restores the agreed baseline exactly once.

**Effort/risk and owner action:** approximately 2–3 engineering days including contracts, scene adapter and focused lifecycle tests, plus integration QA. Medium migration risk because GM and other global pause writers must agree on ownership. Coordinate with `Neo.Tools.Managers` rather than editing its code from this worker. The current guide recommends one common pause root until this is implemented.

### Proposed, not implemented: tracked flight completion/cancellation

**Problem:** the resource cleanup fixed here prevents orphan visuals on destruction, but `AnimationFlyResult.IsCompleted` still means all requested arrivals happened. A stopped spawn coroutine, externally destroyed item or disabled owner can leave a result permanently incomplete, with no distinction between active/canceled/failed. The mutable request object is also read while delayed spawns and callbacks run.

**Design:** snapshot each request into an internal immutable flight plan when accepted. Add `PlayTracked(request)` returning a handle with `Result`, `Status` (`Running`, `Completed`, `Canceled`, `Failed`), `Cancel()` and a terminal event. Reuse the existing spawn/tween/geometry facilities and the ownership set; per-flight bookkeeping determines which visuals to release and ensures terminal notification/reward policy exactly once. Existing `Play` and legacy methods remain wrappers with unchanged return types; `IsCompleted` keeps its arrival meaning, and a new `IsFinished` includes cancellation.

**Acceptance/migration:** cancel before the second delayed item, destroy/reparent a visual, disable/re-enable the owner, cancel from a callback, throw from a consumer callback, and complete with KeepAlive. No canceled flight emits arrival rewards; each terminal event is once; another simultaneous request remains unaffected. Existing callers keep their behavior, while callers that await completion can migrate to the tracked handle.

**Effort/risk:** approximately 2–4 days including callback/reentrancy tests. Medium risk around existing reward order and manual pooling callbacks; introduce additively and validate legacy behavior before adoption.

**Further measured follow-up:** `UIMeshRigElement.CollectPointStates` creates profiles/curves on each geometry build; the common geometry builder allocates three output arrays per build. Cache immutable preset templates and add a caller-owned reusable geometry buffer only after profiling the actual affected grids. Preserve the current allocating API as a wrapper. This is a performance opportunity inferred from allocation sites, not a measured performance regression or a completed optimization.

## Verification and remaining risks

### New behavioral regressions

Five new test sources and five unique-GUID `.meta` files; **22 test methods / 25 parameter-expanded cases**:

| File | Cases | Observable contract |
|---|---:|---|
| `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUICategoryBarTests.cs` | 5 | Initial selection, self-input, retired views, owned listener removal, external spawned-view/marker lifetime. |
| `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIMeshRigTests.cs` | 5 | Two sprite aspect ratios and three renderer ownership transfers. |
| `Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoUIViewTests.cs` | 4 | Shared-object state, empty index, reactive event observations, camera conversions. |
| `Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUILifecycleTests.cs` | 7 | Real Awake/startup, ToggleGroup/CanvasGroup behavior, shake depth, empty/destroyed pages and static lifetime. |
| `Assets/Neoxider/Tests/Play/UI/Audit20261023NeoUIAnimationFlyTests.cs` | 4 | Pool parent/output type, active/pooled/KeepAlive resource ownership and legacy cleanup. |

Tests assert state, identities, counts, events, coordinates and renderer output, rather than merely no exception or matching source text. Inspector setup uses SerializedObject; runtime startup tests use real lifecycle callbacks, and cached-page setup round-trips same-process serialized scene references before Awake. Flight waits are bounded. No existing test source or asmdef was edited.

### Checks actually run

- Read-only full-inventory review and focused source diff review against the worker baseline/current worktree.
- Python static consistency inspection of the exact 28 manifest files, five owned new test sources, changed guides and report: strict UTF-8 decoding, replacement/known encoding-marker checks, C# lexical delimiter and preprocessor balance, explicit local types/no direct GetInstanceID, and nearest-asmdef ownership. This is not compilation or a C# parser/test runner.
- Relative Markdown link checks for the changed public guides; new test GUID uniqueness checked against package `.meta` files.
- Scoped `git diff --check -- <owned inventory, changed docs, five test sources/metas, report>`: exit **0**. Existing Git CRLF conversion notices are informational, not whitespace errors. New untracked test/report lines were also explicitly checked for trailing whitespace because Git's ordinary diff omits untracked content.

### Remaining verification and coordinator handoff

- Compile/import, NUnit/UnityTest execution, Play Mode, screenshot/render verification and builds: **not run**, explicitly reserved for the coordinator. No claim of a passed Unity test is made.
- Run the five exact regression fixtures above plus existing UI/CategoryBar/AnimationFly/MeshRig suites. Prefix-only matching also catches the separate UI Editor worker's tests; keep ownership receipts distinct.
- Compile centrally on Unity 6.0–6.4 and 6.5+; the existing centralized PanelRenderer and stable-id compatibility branches were read but not validated by either compiler here. No new platform conditional or direct obsolete identifier API was introduced.
- Verify SpriteRenderer masks/lights/atlas inputs, UI Toolkit panel reload, deformed raycasts and Animator/preview behavior with a live editor. Existing version-specific renderer claims were not re-proven in this audit.
- Overlapping PausePage snapshot ownership remains a documented P2 limitation pending the proposed shared design. Coordinate its GM boundary with the Managers worker. Flight cancellation/disable status and callback-exception/reentrancy policy remain proposed work; destruction cleanup alone is not a new cancellation API.
- Point reparenting coverage is for moving the point itself. Reparenting an entire ancestor container or dynamically adding/removing a nested rig owner needs a deliberate hierarchy-cache policy and broader tests; this audit does not claim those cases are solved.
- No required new shared assembly references were established from static review; existing UI/Edit and Play assemblies already contain the relevant APIs/test patterns. Coordinator compilation remains authoritative.

Final scoped static receipt: **pass**, 49 text files checked, 28 owned sources / 8,371 current lines, five new fixtures / 22 methods / 25 expanded cases, ten changed public guides, 21 known encoding markers checked, five unique new test GUIDs, zero static issues, scoped `git diff --check` exit 0. This receipt does not establish compilation, rendered behavior or passed Unity tests.
