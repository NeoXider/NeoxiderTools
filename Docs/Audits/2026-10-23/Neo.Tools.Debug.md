# Neo.Tools.Debug: audit

Status: full source review and focused fixes complete; Unity compilation and regression execution centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Debug/Neo.Tools.Debug.asmdef`.
- Scope: 3 C# files; nested assemblies excluded.

## Scope

All 3 manifest C# files were read completely. Inventory was checked against the filesystem and nearest asmdef; there are no nested assemblies in this inventory.

| Owned source | Review performed / conclusion |
|---|---|
| `Assets/Neoxider/Scripts/Tools/Debug/ErrorLogger.cs` | Reviewed every enum flag and severity mapping, enable/disable subscription, null/destroyed output, append/replace APIs, duplicate identity/history, stack parsing, TMP formatting and serialized fields. Corrected script paths containing parentheses. Retention is currently unlimited; see proposal. The public `errorText` field is unused legacy serialization and was deliberately preserved. |
| `Assets/Neoxider/Scripts/Tools/Debug/FPS.cs` | Reviewed Awake dependency validation and diagnostic gate, opt-in global frame-rate changes, unscaled sampling/display scheduling, startup averaging, ring-buffer replacement, sample-size sanitization, thresholds, formatting and both public engine-setting methods. No demonstrated runtime defect found. Corrected XML descriptions of `CurrentFps` and target `-1`; runtime behavior is unchanged. |
| `Assets/Neoxider/Scripts/Tools/Debug/NeoDebugOverlay.cs` | Reviewed generated canvas ownership, owner destruction, enable/disable visibility, hidden sampling, input backends, unscaled throttling/smoothing, style/layout/raycast behavior, numeric formatting, manager reflection, missing modules and singleton side effects. Fixed disabled-legacy-input failure and passive inspection mutating audio singleton state. |

Read-only context: owned asmdef; existing `DebugToolsBehaviorTests`; Edit/Play test asmdefs; `Singleton<T>`; relevant AM properties/methods/lifecycle; SaveManager's state properties; `KeyInputCompat` and `OptionalInputSystemAdapter`; `NeoDiagnostics`; relevant NeoxiderTools skill catalogs. Existing tests and dependencies were not modified. No Unity/MCP, runner, compiler/build, install, shared configuration, scene/prefab/layout, Git mutation or child agent was used.

Reuse decision: `references/tools.md` already catalogs `KeyInputCompat` and its Input System fallback. The overlay now adapts that existing API with a cached delegate, using its existing type-resolution utility. No parallel keyboard mapper, new module or asmdef dependency was introduced.

## Findings and fixes

### DBG-01 — Medium: Input System-only configuration prevents overlay updates

- Trigger: Active Input Handling disables the legacy Input Manager, then an enabled overlay runs `Update`.
- Evidence: the original first operation was `Input.GetKeyDown(_toggleKey)`. Its `InvalidOperationException` exits before FPS sampling and `RebuildText`; the same path repeats each frame. The package already handles this backend distinction in `KeyInputCompat.GetKeyDown`.
- Observable consequence: readout stops updating, the hotkey does not work, and uncaught exceptions can flood the console.
- Fix: `NeoDebugOverlay.cs:169`, `GetToggleKeyDown`, resolves the existing compatibility helper and caches a `Func<KeyCode, bool>`. When the optional Input module is unavailable, legacy input remains the fallback; a disabled backend returns false without aborting the readout. The overlay's serialized hotkey and visibility settings remain intact.
- Regression: `Overlay_Update_RendersWithTheConfiguredInputBackend` invokes the component's update callback and checks actual TMP FPS, scene and time-scale output. Run this under Input System-only as well as legacy/Both; the former is the failure configuration. Keypress injection/F3 behavior remains a central runtime check.

### DBG-02 — Medium: manager readout can create or resolve an audio singleton

- Trigger: show manager information with no already-resolved AM. `AM.CreateInstance = true` permits lazy creation; an inactive, unresolved AM can also be found when creation is false.
- Evidence: original `AppendManagerInfo` unconditionally read the reflected `AM.I`. `Tools/Managers/Singleton.cs`, getter `I`, searches inactive objects, initializes a discovered instance, or creates a GameObject when `CreateInstance` is true. Its `HasInstance` property is a passive cache check.
- Observable consequence: viewing diagnostics changes `AM.HasInstance` and can add a manager GameObject to the scene. An inactive manager may become the singleton despite no gameplay code requesting it.
- Fix: `NeoDebugOverlay.cs:341` reads reflected `HasInstance` before accessing `I`; `EnsureManagerReflection` caches that property at line 447. Missing or unresolved managers render `AM  —`; resolved manager state is still displayed.
- Regressions: separate cases assert no lazy manager creation, no inactive-manager resolution, and continued rendering of an explicitly resolved manager. Tests observe singleton state, scene-object inventory and TMP text. They require an isolated audio scene and restore their static settings/objects.

### DBG-03 — Low: highlighted source location disappears for paths with parentheses

- Trigger: `ErrorLogger.showScriptPath = true` and a Unity stack frame such as `(at Assets/Feature (Demo)/Logger.cs:27)`.
- Evidence: both original patterns used `[^)]+` for the path, so a valid closing parenthesis inside a directory prevented a match. Raw stack text still appeared, but the separate cyan source location was omitted.
- Fix: `ErrorLogger.cs:118` and `:128` match lazily through a `.cs:<line>` frame ending, preserving parentheses in the captured path. Both supported frame formats and the public/serialized API remain unchanged.
- Regressions: four callback-to-TMP cases cover an ordinary asset path, asset and absolute paths with parentheses, and the alternate package-path format. Assertions require the separate cyan location, so the raw stack's presence cannot mask the defect.
- Executed proof: a PowerShell/.NET Regex probe used the updated patterns extracted from the real source and the two original patterns. The ordinary case succeeded before/after; all three parenthesized paths failed before and succeeded after. This probe did not run Unity or a test runner.

### Documentation defects corrected

The overlay guide incorrectly described IMGUI despite a uGUI/TMP implementation. The module entry omitted the overlay and had a malformed table row. FPS XML called target `-1` unlimited and implied `CurrentFps` always matched the throttled label. These descriptions now match the actual API. No FPS runtime change was made.

### New regressions

`Assets/Neoxider/Tests/Edit/Tools/Debug/Audit20261023NeoToolsDebugRegressionTests.cs`: 8 authored cases (4 overlay tests and 4 path cases), with a unique `.meta` GUID `4a34ddd7d572425193ded77436678b9c`.

Tests invoke Unity callbacks on inactive fixtures to avoid generating persistent canvases during Edit Mode tests, and assert component output or singleton/scene state rather than private implementation values. Reflection is limited to supplying the existing callback/UI seams because these components do not expose public sampling/capture APIs. The existing test asmdef already references Debug, Audio, Extensions and TMP; no new assembly references are requested. These regressions have **not been executed**.

## Documentation

Updated all owned canonical pages under `Assets/Neoxider/Docs/Tools/Debug/`:

- `README.md`: component entry points, assembly/namespace, C# and scene workflows, output dependencies and passive diagnostics behavior.
- `ErrorLogger.md`: severity flags, label assignment, callback lifetime, highlighted paths, public text methods/UnityEvent use, legacy field behavior and existing unlimited/deduplicated history semantics.
- `FPS.md`: configuration, unscaled sampling, filled-buffer averaging, public `CurrentFps`/global settings APIs and target `-1` semantics.
- `NeoDebugOverlay.md`: uGUI/TMP implementation, optional compatible input, manager observation without singleton discovery, fonts, generated-canvas ownership and lifecycle visibility.

Public guides remain English. Кратко: проверены все 3 файла; исправлены обновление панели при отключённом legacy input, побочные эффекты чтения AM и выделение пути со скобками. Unity-проверки не запускались по контракту.

## Substantial improvement proposal

### Bounded log storage with a testable C# capture API — proposed, not implemented

**Problem.** `ErrorLogger` stores every accepted formatted entry in a lifetime `List<string>`, even with duplicate checking disabled or replacement display enabled. Duplicate lookup scans the growing list; append display repeatedly concatenates the entire label. There is no capacity or public history reset. A noisy long-running diagnostic session can retain unlimited strings and increase capture/formatting costs. Changing retention silently in this bug-fix pass would alter existing behavior.

**Architecture and API.** Add a small ordinary C# `DebugLogEntry` value and `DebugLogBuffer` in this module, with no TMP, MonoBehaviour or scene dependencies. Proposed surface: `DebugLogBuffer(int maxEntries, int maxCharacters)`, `bool TryAdd(DebugLogEntry entry)`, a read-only retained-entry view and `Clear()`. Store bounded entries in an eviction queue/ring plus a matching duplicate index. Eviction removes its duplicate index entry; oversized records follow an explicit reject/truncate policy before retention. Deduplication uses typed severity/message/stack identity rather than colorized display text.

Keep `ErrorLogger` as the Unity adapter: the Application callback forwards to a public `Capture(string message, string stackTrace, LogType type)` path, while `ClearHistory()` clears storage/deduplication and refreshes the label. The same capture API serves C#, Unity callbacks and deterministic regression tests. Formatting is a view concern rebuilt from retained entries. Existing text methods remain direct label operations and are documented separately.

**Compatibility/migration.** Preserve existing fields and methods, including the unused serialized `errorText`. New entry/character limits default to zero (unlimited) so existing serialized components retain their behavior. Bounded capture is an explicit Inspector/C# choice; newly authored samples may opt in later. Preserve current append/replace and enable/disable history semantics by default. Any change in duplicate identity requires an explicit compatibility setting or documented major-version migration, since toggling formatting currently changes identity.

**Acceptance scenarios.**

1. Capture at least 10,000 unique entries with limits of 256 records and 64 KiB retained text; retained entries, duplicate keys and text remain within both limits.
2. Reject/truncate a single oversized message according to the selected policy; no original oversized payload remains retained indirectly.
3. Verify eviction permits a previously evicted message again, and `ClearHistory()` resets both displayed text and duplicate suppression.
4. Verify zero limits preserve existing behavior, and append/replace/filter/re-enable scenarios remain compatible.
5. Test core capture/eviction without scene objects, then run focused TMP/Unity callback integration checks. Formatting changes must follow the selected duplicate compatibility policy.

**Effort/risk.** Approximately 1–2 implementation days plus central Unity integration/QA. Moderate risk around duplicate compatibility, character accounting, truncation of rich text and log callbacks during UI refresh. Keeping migration opt-in and testing the buffer independently confines the change; no singleton or broad diagnostics framework is needed.

## Verification and remaining risks

Executed checks:

- Complete source review: **3/3 manifest files**, with filesystem inventory and nearest-asmdef membership confirmed.
- Scoped `git diff --check -- <owned source/docs/new tests/report>`: **exit 0**. Git emitted only its LF-to-CRLF advisory for `ErrorLogger.cs`.
- Python static checks: owned source/docs/new test/meta decode as UTF-8, contain no replacement characters/trailing whitespace, introduce no `var`, all **6 local documentation links** resolve, and the new `.meta` GUID occurs once across package metadata.
- Final static receipt also covered the report (**10 scoped files** total), confirmed **8 authored cases**, compared all public and serialized declaration lines in all 3 sources with `ca198556` (unchanged), and found no direct obsolete object-ID or editor-only API calls in owned C#.
- Real .NET Regex probe: **4/4 expected source paths extracted** by the updated patterns; original patterns missed the 3 parenthesized cases.
- Reviewed the focused source/documentation diff and new regression fixture; serialized fields, public method signatures and asmdef contents are preserved.

Not executed, by worker contract: Unity import/compilation, Edit/Play Mode tests, player builds, console/visual QA, or Unity 6.0/6.5 branch compilation. Source/Regex consistency is not a claim that the Unity regressions passed.

Coordinator follow-up:

1. Run `Audit20261023NeoToolsDebugRegressionTests` and existing `DebugToolsBehaviorTests` centrally. Isolate the audio singleton; assumption-skipped cases are not passes.
2. Exercise the overlay under Input System-only and legacy/Both, including an actual F3 press, hidden/disabled/re-enabled states, owner destruction and scene change. The authored input regression checks readout progress under the configured backend, not synthetic key delivery.
3. Check optional type reflection in a stripped/IL2CPP player. Existing AM/Save reflection and the input delegate rely on retained metadata. Without the Input module the readout continues but Input System hotkeys are unavailable; no `link.xml`/shared asmdef was changed here.
4. Cross-boundary note for the `Neo.Tools.Input` owner: `KeyInputCompat.GetKeyDown` currently tries legacy input on every call and catches `InvalidOperationException` before its new-input fallback. Reusing it preserves package behavior but carries that per-call exception cost in Input System-only projects. Any backend-selection optimization belongs to that worker.

Remaining design limits: unlimited ErrorLogger retention (proposal above); no public overlay show/hide API; frame-rate/VSync effects are platform-dependent global settings. None was silently changed during this audit.
