# Neo.HtmlToUitk.Runtime: audit

Status: all 29 source files reviewed; focused fixes, regression sources and public documentation complete. Unity compilation/test execution remains centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Neo.HtmlToUitk.Runtime.asmdef`.
- Scope: 29 C# files; nested assemblies excluded.

## Scope

- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Actions/UitkActionBindings.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Animation/StyleApplier.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Animation/UitkAnimator.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Effects/BackdropBlur.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Effects/BackdropBlurPass.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Hosting/UitkRootConnection.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Localization/ILocalizer.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Animation/AnimSpec.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Animation/AnimTimeline.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Animation/AnimValue.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Animation/Easing.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Effects/BlurPlan.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Json/JsonValue.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Json/MiniJson.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Navigation/NavSpec.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Navigation/RouterCore.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Navigation/TransitionMath.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Navigation/UiKitMapping.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Responsive/BreakpointSet.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Logic/Selectors/SimpleSelector.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Navigation/ElementFocus.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Navigation/ElementTransition.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Navigation/IPageBackend.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Navigation/UiDocumentPageBackend.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Navigation/UitkRouter.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Navigation/UitkScreen.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Navigation/UitkTabs.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Responsive/BreakpointController.cs`
- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Responsive/BreakpointWatcher.cs`

## Findings and fixes

Проверены все 29 файлов сборки. Исправлены ошибки сериализации JSON, селекторов, повторного входа из callback, восстановления фокуса, анимаций и владения blur-ресурсами. Unity и тестовые раннеры не запускались.

Evidence below is a source-level value/control-flow witness against the pre-edit source, with public-behavior regression fixtures added for central execution. It is not a claim of executed C# tests. P1 denotes a crash or incorrect projected UI under an ordinary callback; P2 denotes incorrect values, focus, asset acceptance or resource/style ownership.

### Complete review ledger

Paths below are relative to `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/`. Each inventory file was read completely, including conditional compilation branches.

| File | Review outcome |
|---|---|
| `Actions/UitkActionBindings.cs` | Checked replacement-token ownership, disabled actions, typed payloads, root/child attach/detach, refresh, rebind and disposal. No focused change required; prior callback identity guards retained. |
| `Animation/StyleApplier.cs` | Checked all channels, unit conversion, inline snapshots/restoration and resolved underlying values. Reused existing channel contracts for F6/F7; supported-unit limits remain explicit converter/runtime constraints. |
| `Animation/UitkAnimator.cs` | Checked scheduling, reduced motion, terminal samples, router subscriptions, rebind, cancellation and callback reentry. Fixed F6, F7 and F8. |
| `Effects/BackdropBlur.cs` | Checked native availability/filter guards and explicit clear APIs. Added internal native-style restoration snapshot for F10 without exposing a new public dependency. |
| `Effects/BackdropBlurPass.cs` | Checked target replacement, mode changes, author styles, camera state, owned and temporary textures, disable/release and rendering exceptions. Fixed F10. |
| `Hosting/UitkRootConnection.cs` | Checked UIDocument polling, same-root reload invalidation, subscription lifetime and both PanelRenderer callback signatures. Version switches remain centralized in `PanelCompatibility`; native compilation still pending. |
| `Localization/ILocalizer.cs` | Checked null inputs, source key retention, unresolved translations and repeated localization. Existing `LocalizationKey` behavior retained; `userData` is reserved by this workflow for the key. |
| `Logic/Animation/AnimSpec.cs` | Checked parsing/defaults, stable keyframe sorting, property enumeration, reduced-motion metadata and lookup. Broader semantic validation/immutable data is proposed below, not silently added to legacy constructors. |
| `Logic/Animation/AnimTimeline.cs` | Checked phase/fill boundaries, direction, fractional iterations, stagger, instance completion, missing keyframes and reduced-motion policy. Reused its terminal evaluation in F6 instead of duplicating timeline math. |
| `Logic/Animation/AnimValue.cs` | Checked color parsing, premultiplied interpolation, scalar broadcasting, translate padding and incompatible-unit fallback. No new parsing feature or unsupported CSS-unit promise added. |
| `Logic/Animation/Easing.cs` | Checked named families, endpoint clamps, bezier parsing/solver and unknown-name fallback. Fixed non-finite coordinate admission in F9. |
| `Logic/Effects/BlurPlan.cs` | Checked radius/downsample calculation, iteration cap and minimum dimensions. Existing plan reused by the pass; no alternate blur planner introduced. |
| `Logic/Json/JsonValue.cs` | Checked missing/null semantics, array bounds, ordered members, replacement/removal, scalar conversions and mutation. Existing public model retained; writing assumes an acyclic JSON tree. |
| `Logic/Json/MiniJson.cs` | Checked every reader/writer branch, escapes, BOM, depth cap, positions, delimiters, numeric grammar and round trips. Fixed F1/F2. |
| `Logic/Navigation/NavSpec.cs` | Checked parsing/defaults, reference validation, lookups and optional transitions/tabs/modals. Strict identity/schema validation is a proposal; existing permissive API retained. |
| `Logic/Navigation/RouterCore.cs` | Checked stack/show/push/back policies, modal ownership, close ordering, tab state, focus keys, durations, visit cap and trigger resolution. Core planning retained; wrapper projection/focus corrected in F4/F5. |
| `Logic/Navigation/TransitionMath.cs` | Checked all transition kinds, progress bounds and forward/backward signs. No focused change required. |
| `Logic/Navigation/UiKitMapping.cs` | Checked page presets, popup ownership paths, duplicate-trigger policy and diagnostics for unsupported duration/tab mappings. Optional integration remains data-only; no Neo.UIKit reference introduced. |
| `Logic/Responsive/BreakpointSet.cs` | Checked inclusive bounds, viewport validity, orientation/aspect, optional fields/range validation and active class ordering. Existing public behavior retained. |
| `Logic/Selectors/SimpleSelector.cs` | Checked compound/complex/list parsing and matching, type/name/class rules, child/descendant traversal and unsupported syntax. Fixed F3. |
| `Navigation/ElementFocus.cs` | Checked root/panel/null paths, focusable/enabled/visible traversal and scheduled focus. Actual pointer/focus scheduling remains native QA work. |
| `Navigation/ElementTransition.cs` | Checked immediate/deferred paths, completion idempotence, scheduler pause and restoration of authored inline transforms. Existing contract retained. |
| `Navigation/IPageBackend.cs` | Checked completion handle, synchronous completion, plan-aware extension and hook/event contracts. No interface or public signature changes. |
| `Navigation/UiDocumentPageBackend.cs` | Checked missing elements, binding, page/modal visibility, animated exit layout snapshots and focus delegation. Existing backend reused by the screen; no nested integration edits. |
| `Navigation/UitkRouter.cs` | Checked all public operations, trigger wiring, completion/generation guards, callbacks, focus history and disposal. Fixed F4/F5. |
| `Navigation/UitkScreen.cs` | Checked serialized fields, disabled configure, explicit roots, document reconnection generation, error teardown, actions/hooks, cancel navigation and component lifecycle. Existing owner teardown retained. |
| `Navigation/UitkTabs.cs` | Checked previous/current panels/classes, null element handling and Toggle updates without notifications. No focused change required. |
| `Responsive/BreakpointController.cs` | Checked per-root lease, selectors, viewport/class diffing, authored-class retention, subtree detach and disposal. F3 now rejects malformed selectors before this controller takes ownership. |
| `Responsive/BreakpointWatcher.cs` | Checked disabled configuration, document/manual roots, rebuilding, LastError and cleanup. No additional per-frame logging introduced. |

### Completed fixes

1. **F1 — P2: JSON writer silently changes finite values.** `MiniJson.FormatNumber` previously used an absolute epsilon when choosing integer formatting. For `1e-13`, the difference from rounded zero is below `1e-12`, so output was `0`; `1.0000000000001` similarly became `1`. Integer formatting now requires exact equality; other finite doubles use the existing round-trip format. Regression cases compare restored double bits, including tiny, near-integer, subnormal and maximum values.

2. **F2 — P2: malformed JSON reaches runtime configuration.** `MiniJson.Reader.ReadNumber` scanned a broad token and relied only on `double.TryParse`, admitting `01`, `1.`, `1.e2` and, on runtimes accepting overflow as Infinity, `1e309`. `ReadString` expressly allowed an unescaped tab. The reader now enforces the JSON integer/fraction/exponent grammar, finite range and escaped control characters. `TryParse` retains null result plus positional diagnostic on rejection. Valid signed exponents and escaped controls have positive controls. Valid generated assets remain compatible; malformed formerly accepted input intentionally gets a diagnostic.

3. **F3 — P2: invalid selectors select unintended elements.** `SelectorList.ParseComplex` discarded a trailing `>` and collapsed repeated `>`; `ParseCompound` ignored misplaced `*` and replaced a prior conflicting ID. Thus `.card >` acted as `.card`, and `#other#card` acted as `#card`. These forms are now invalid and never match. Positive tests preserve supported child/descendant/list/universal matching. A controller regression checks rejection occurs before acquiring responsive ownership and that a valid replacement controller can still toggle/release its class.

4. **F4 — P1: callback redirects continue a superseded router plan.** `UitkRouter.Execute` guarded will/completion paths but did not guard all dispatch boundaries. During `Start`, `OnTabSelected` could navigate to `next`, after which the old plan still showed `home`. Redirecting in `Navigated` also allowed an old `WillShow`; disposing in `OnEvent` still delivered its corresponding typed event through `Raise`. Generation/disposal guards now cover those boundaries, including custom backend returns; stale returned handles are completed for cleanup. Regressions assert final visible page sets, ordered shown events and absence of stale focus/events. The interrupted outer call returns false while the callback's accepted navigation remains current.

5. **F5 — P2: stacked modal focus escapes its owner and overwrites page history.** `ApplyFocus` always focused the page after a close, even with another modal still open. `RememberFocus` stored the outer modal button as the page's focus when opening a second modal, and stored modal focus as page focus when navigation closed overlays. Focus now stays in the remaining modal; parent modal and page focus memories remain separate. Regressions cover remaining-modal fallback, restoring exact remembered controls, and navigation away/back from an overlay.

6. **F6 — P2: jump/Skip ignore fractional animation endpoints.** `UitkAnimator.JumpToEnd` and `Finish(jumpToEnd)` sampled local progress `1` for every finite iteration count. A normal quarter iteration therefore wrote opacity `1` instead of `0.25`. Both now reuse `AnimTimeline.Evaluate` for the actual terminal sample, respecting reverse/alternate direction. Reduced-motion Skip also resolves an omitted endpoint against the underlying value. Tests observe inline opacity through public Skip/Stop APIs. Infinite explicit jumps use the first cycle's directed endpoint, without casting Infinity to an integer.

7. **F7 — P2: omitted keyframes feed prior animation output into the base.** `UitkAnimator.ApplySample` resolved underlying style afresh on each sample, after previous samples had replaced that style. A missing end keyframe that should return to authored opacity `0.8` instead converged toward its own animated value. Each run now captures the underlying channel value alongside its existing inline restoration snapshot at first use. A native fixture observes an intermediate low opacity, then verifies Stop(true) and Skip return to the authored value.

8. **F8 — P1: cancellation callbacks invalidate run iteration or resurrect disposed work.** `Stop`, `StopAll` and replacement in `Start` indexed the live `_running` list while `Finish` invoked public cancellation callbacks. A callback calling `StopAll` could empty the list and make the outer loop index out of range; a callback disposing the animator during replacement still let `Start` append new work. Iteration now uses snapshots and replacement checks disposal after callbacks. A fixture checks two distinct cancellations, zero remaining runs, original style restoration, and rejection of a replacement after callback disposal.

9. **F9 — P2: non-finite cubic-bezier coordinates enter style animation.** `Easing.TryParseCubicBezier` accepted NaN/Infinity where `TryParse` succeeded; comparisons against x bounds do not reject NaN, and non-finite y coordinates can produce non-finite samples. All four coordinates must now be finite. Tests check unknown-name linear fallback and retain finite overshoot support.

10. **F10 — P2: blur cleanup destroys authored styles or leaves rendering state/resources behind.** `BackdropBlurPass.Release` unconditionally cleared a resolved element's background even when no source had been available or only native filtering had been applied; background size was not restored. Changing configuration/root lost the prior target, and render exceptions bypassed camera restoration/temporary texture release. The pass now snapshots only styles it actually replaces, restores image/size/native value, releases the prior target on Configure or target replacement, and frees unused fallback textures when switching to native. Camera target, active render target and temporary texture cleanup use `finally`. Tests cover no-source/idempotent release and GPU-backed fallback retarget/output ownership/active-target restoration. Camera-error injection and native filter-value restoration require central native QA; no artificial renderer abstraction was added just for those tests.

### Regression sources

All are new, uniquely prefixed files with unique GUID `.meta`; existing fixtures and asmdefs were not edited.

| File under `Assets/Neoxider/Tests/Edit/HtmlToUitk/` | Declared cases | Observable contracts |
|---|---:|---|
| `PureLogic/Audit20261023NeoHtmlToUitkRuntimeJsonSelectorTests.cs` | 43 | JSON bit-preserving values/diagnostics, selector matching/rejection, finite easing fallback. |
| `Editor/Audit20261023NeoHtmlToUitkRuntimeRouterTests.cs` | 6 | Visible page state, stale event suppression, modal/page focus targets. |
| `Editor/Audit20261023NeoHtmlToUitkRuntimeAnimationTests.cs` | 7 | 4 Skip cases and 3 scheduler/native fixtures: terminal opacity, captured base and cancellation ownership. |
| `Editor/Audit20261023NeoHtmlToUitkRuntimeBlurTests.cs` | 3 | Author style/output ownership, active render target, responsive lease recovery. |

Total: **59 declared cases, zero executed by this worker**. One blur case explicitly skips on `GraphicsDeviceType.Null`; that skip is not visual/GPU proof. Animation fixtures create and clean up their own EditorWindow without entering Play Mode.

### Reuse, compatibility and boundaries

Read the NeoxiderTools skill and searched `references/extensions.md`, `modules.md`, `game-systems.md` and `tools.md` for JSON/selector/UI Toolkit/router/blur/formatting reuse. Those catalogs do not provide a replacement for these module-specific restoration/sampling contracts. Existing `AnimTimeline`, `PreparedAnim`, `StyleChannel`, `BlurPlan`, `BackdropBlur` and transition handles are reused; only narrow private/internal glue was added. No package dependency was introduced.

All existing public declarations and serialized field declarations in the seven changed source files are retained against `ca198556`. Native filter version handling remains inside `BackdropBlur`; PanelRenderer version handling remains inside `PanelCompatibility`. Both branches still need central compilation. No commits, installs, child agents, Unity/MCP, builds/test runners, settings/asmdef edits, scenes/prefabs or sample layout changes were performed. No nested assembly source/docs or another worker's tests were edited. This was a local audit/self-review, not independent runtime QA.

## Documentation

Updated only the owned canonical entry `Assets/Neoxider/Docs/UI/HtmlToUitk.md`:

- public JSON Parse/TryParse/Write behavior and hand-authored metadata validation;
- supported selector grammar and invalid selector ownership behavior;
- callback redirects/disposal, return semantics and nested modal focus;
- fractional animation completion, captured underlying values and reentrant cancellation;
- blur Inspector setup plus Configure/Refresh/Release C# workflow, style/resource ownership and explicit source requirement;
- a dated statement separating this static audit from historical test receipts.

Existing English style and editor-owned setup/import sections were preserved. No redundant module guide, DocsEn directory, root/package README or shared documentation index was created or edited.

## Substantial improvement proposal

### Compile validated, immutable runtime specifications before touching a tree

**Problem.** JSON syntax now rejects malformed primitives, but the three metadata domains still have different semantic validation policies. `NavSpec` and `AnimSpec` accept missing arrays/identities and silently default unknown enum strings; duplicate identities overwrite dictionary entries while ordered lists still retain both entries. Public constructors retain caller-supplied mutable lists/arrays. Responsive selectors are parsed when a controller takes ownership; animation selectors are reparsed on each play. Invalid or subsequently mutated authoring data can therefore fail after part of a UI projection has been changed. `NavSpec.Validate()` is useful but is not a unified, immutable binding contract.

**Proposed runtime architecture/API (not implemented).** Add an engine-independent compilation service alongside existing Logic classes:

```csharp
bool RuntimeSpecCompiler.TryCompile(
    RuntimeSpecInput input,
    out CompiledScreenSpec specification,
    out IReadOnlyList<RuntimeSpecDiagnostic> diagnostics);
```

`RuntimeSpecInput` carries optional navigation/animation/media JSON plus diagnostic source names. Diagnostics carry severity, code and logical property path such as `nav.pages[2].id`, independent of Unity logs. Compilation validates supported schema versions, required types/nonempty unique IDs, page/modal/tab references, tab defaults, finite bounded times/offsets/counts, supported properties/units and selectors. It freezes copies of the accepted data and caches parsed selectors/prepared tracks. A compiled object may be shared by several roots; callback leases, run state, focus and owned styles remain per-root instances.

Add an overload such as `UitkScreen.ConfigureRoot(VisualElement root, CompiledScreenSpec specification, bool ownResponsiveLayout = true)` while preserving existing TextAsset/string APIs. The scene wrapper may opt into strict compilation and expose diagnostics through its existing error surface. Compile all inputs successfully before releasing the current projection or acquiring a replacement root; failed data leaves the previous usable screen intact. C# consumers can compile/cache once and bind through the same typed contract without Inspector or UnityEvent chains.

**Compatibility/migration.** Keep existing constructors, Parse methods, serialized names and forgiving defaults available. Introduce strict compilation as an additive explicit path, then document migration examples for custom hosts and Inspector users. Freeze data only in the new compiled representation, avoiding an immediate behavioral break for callers that currently mutate legacy lists. Decide unknown schema/enum diagnostics with the converter owner before making strict mode the default in a future major release. Keep the compiler free of Neo.UIKit and Unity object references.

**Acceptance scenarios.** (1) Equivalent valid legacy/generated metadata yields the same initial visibility, routes, tabs and animation samples. (2) Duplicate page/animation IDs, an unknown default tab, invalid selector or non-finite timing produce stable path-specific diagnostics before any root/style/callback changes. (3) Mutating caller lists after compilation changes neither lookups nor playback. (4) Two roots share one compiled spec and dispose independently. (5) A failed reload preserves a working screen and its actions. (6) Both supported Unity compiler branches consume the same pure compiled contract; no per-call version checks or scene references appear in shared data.

**Effort/risk.** Estimate 3–5 development days for validation/frozen representations/compatibility adapters plus 1–2 days of central converter and native integration QA, assuming no schema redesign. Main risks are newly exposed invalid legacy assets, diagnostics ordering, memory duplication and treating all CSS units as supported; mitigate through opt-in migration, positive generated-fixture tests, deterministic diagnostics and explicit capability checks. This proposal is separate from the completed fixes above and requires coordination with the converter/editor owners.

## Verification and remaining risks

Actual checks run:

1. **PASS — inventory ownership:** manifest's 29 paths equal the actual runtime C# files whose nearest asmdef is `Neo.HtmlToUitk.Runtime`. No nested assembly was included.
2. **PASS — source structure:** delimiter and preprocessor-balance scans over 33 C# files (29 runtime + 4 regressions), with strings/comments excluded; no `var` tokens. This is a structural scan, not a C# compiler.
3. **PASS — compatibility declarations:** compared baseline/current serialized-field declarations and retained public declaration lines for all 7 modified source files against `ca198556`. No removed public declaration or serialized field was detected.
4. **PASS — text/assets:** strict UTF-8 decoding, replacement/C1-control marker checks, regression EOF/trailing whitespace checks, and 4 new `.meta` GUIDs unique across package metadata. No mojibake-based rewrite was made.
5. **PASS — regression discovery:** 4 new files declare 59 cases; the existing pure/native source-check project globs include them. `Neo.HtmlToUitk.Tests.Editor.asmdef` already references the runtime, editor and Unity test assemblies; no reference change is required.
6. **PASS — scoped diff whitespace:** `git diff --check --` runtime, owned public doc/report and uniquely prefixed regression paths returned exit 0. New untracked test/meta whitespace was checked separately above.
7. **PASS — focused source diff review:** inspected the complete seven-file runtime diff; changes are limited to F1–F10 and retain existing serialization/public entry points.

**Not run:** Unity import/compilation, pure C# or Unity tests, Play Mode, console checks, native rendering, screenshots, player builds, performance or error-injected camera rendering. All remain centrally pending under the worker contract. No passing runtime assertion is claimed. Historical 2026-10-14 results and `source-final.json` are not validation of this source revision.

Coordinator actions:

- Run the 43 new pure-logic cases, the 16 editor/native cases and existing HtmlToUitk regressions; verify the GPU case on a graphics-capable machine and inspect actual focus/scheduler behavior.
- Compile both supported native source-check branches, especially native backdrop snapshot restoration; run applicable Unity import/editor checks. The historical `D:/unity/Editor/6000.6.0f1/Editor/Data/Managed` reference directory was absent during a read-only check here, so configure managed-DLL paths for the central machine.
- Exercise camera-render/Blit failures and native/fallback mode switching, root replacement and disable/re-enable to validate resource cleanup in Unity. Rendering quality was not assessed by style/ownership tests.
- Refresh central source-final receipts after validation. No shared configuration, assembly reference or cross-boundary source fix is requested. Coordinate the proposed strict specification API with converter/editor owners before implementation.

Residual contract limits: legacy programmatic specs remain mutable/permissive; unsupported CSS/browser behavior is not added; separate animation/transition/blur owners must not compete for the same style channels; native focus scheduling and render output still need device/editor evidence. Explicit Stop/Dispose remains required for service owners, including detached trees whose UI Toolkit scheduler can pause.
