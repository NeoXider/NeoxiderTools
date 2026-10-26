# Neo.Tools.Text: audit

Status: source audit and scoped changes complete; compilation and runtime verification centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Tools/Text/Neo.Tools.Text.asmdef`.
- Scope: 2 C# files; nested assemblies excluded.

## Scope

Reviewed **2/2 owned C# files in full**, matching the exact manifest inventory. There are no nested assemblies in this source directory.

| File | Reviewed behavior and contracts |
|---|---|
| `Assets/Neoxider/Scripts/Tools/Text/SetText.cs` | Every overload/property, formatter options, integer offset/precision, string and `BigInteger` parsing, Inspector affixes, missing/destroyed TMP targets, change events, DOTween creation/cancellation/disable/enable/destruction and gated diagnostics. |
| `Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs` | Static formatting delegation, every property, Clock/fixed Compact/dynamic Compact, zero/negative/non-finite/large inputs, parsing result semantics, notification ordering/reentrancy, target fallback and serialized validation. |

Read-only context: the assembly definition; the existing `SetTextTests`, `TimeToTextTests`, diagnostics tests and Edit test references; `NumberFormatExtensions`, `TimeParsingExtensions`, relevant `PrimitiveExtensions`/`TimeSpanExtensions` code; NoCode display dispatch and DOTween usage. Reused the existing `ToPrettyString`, duration parser and time formatters rather than adding another formatting implementation. The `neoxider-tools` Text and number/time formatting references were consulted before adding local lifecycle/display helpers.

Ownership: only the two manifest sources, three existing owned documentation pages, this report and two new uniquely prefixed regression files with their metadata were edited. No shared configuration, existing tests, other assembly sources, scenes, prefabs or sample layouts were changed. No child agents, commits, installs, Unity sessions, builds or test runners were started. This was a local review; independent QA remains with the coordinator.

## Findings and fixes

### F1 — Medium: an earlier number animation overwrites a later immediate write

- **Trigger/source:** `SetText.Set(float)` created a tween whose callback called `Set(string)`, while public string writes, percentage/currency/large-integer writes, custom formatting and `Clear()` did not cancel it.
- **Observable consequence:** `Set(1000f); Clear()` initially empties the label, then the next tween update replaces it with a number and publishes another `OnTextUpdated`. An immediate status/price could be replaced in the same way.
- **Fix:** separate internal `ApplyText`/numeric refresh from public replacement operations. Immediate writes and clear cancel the owned tween; its own frame callback renders without cancelling itself. A new numeric animation still supersedes the previous one.
- **Regression:** seven `ImmediateWrite_ReplacesAnimationAndRemainsOnScreen` cases advance an unfinished tween, perform each replacement, advance again and assert exact visible text plus no later notifications.

### F2 — Medium: tween ownership does not follow component/hierarchy lifetime

- **Trigger/source:** `SetText` had only `OnEnable`; it tested `gameObject.activeSelf` when deferring an animation. A disabled component or a child under an inactive parent still has an active GameObject flag. There was no explicit destruction cleanup.
- **Observable consequence:** a hidden label can finish its animation before it is shown; a surviving external/TMP target can still receive writes and events after its `SetText` is removed.
- **Fix:** use `isActiveAndEnabled` for initial deferral, pause on disable when the existing **On Enable Anim** option is enabled, resume on enable, and kill on destruction. Use DOTween target/link ownership and disable recycling for this retained tween reference. Kill/pause/play guard inactive tweens. The existing option to animate while hidden when **On Enable Anim** is disabled is retained. The animated Inspector button is now Play Mode only.
- **Regression:** two `[UnityTest]` scenarios enter actual Play Mode: pause/resume through a hidden parent followed by destruction with a surviving label; and queuing a new number on a disabled component before enabling it. These are prepared, not executed by this worker.

### F3 — Medium: formatting properties reinterpret display text as numeric state

- **Trigger/source:** `Separator` and `DecimalPlaces` used `float.TryParse(text.text.Replace(separator, ""))`; the separator setter overwrote the previous separator first. The group and decimal separator both default to a period.
- **Observable consequence:** changing decimal places after displaying `1.25` reparses `125`. Grouped numbers can be misread after a separator change; decorated currency and large integers cannot be reliably recovered from their label. A literal numeric-looking string can unexpectedly start an animation.
- **Fix:** retain a numeric formatting delegate over the original typed value, or the current animated float. Separator/decimal/notation/rounding changes immediately redraw that value without parsing, restarting a tween, or dropping currency/percentage decoration. Large integers remain `BigInteger`; literal and explicit custom-format results remain as supplied. Clear forgets the numeric source. Equality guards avoid redundant redraws for identical settings.
- **Regression:** exact decimal text/current value; regrouping `9007199254740993` without lost digits; currency/notation/rounding updates; literal numeric text and clear remaining stable after formatting changes.

### F4 — Medium: integer offset wraps before float interpolation

- **Trigger/source:** `Set(int)` evaluated `value + indexOffset` as `Int32` before casting to float.
- **Observable consequence:** `int.MaxValue` plus offset `1` becomes negative; `int.MinValue` plus offset `-1` becomes positive.
- **Fix:** widen the addition to `long`, then retain the established float animation path. Exact large-number text is still available through `SetBigInteger`; float interpolation precision is explicitly documented.
- **Regression:** both signed boundaries assert the final public `currentNum` and the displayed sign after animation.

### F5 — High: time change listeners can recurse or corrupt the event baseline

- **Trigger/source:** `TimeToText.Set` invoked `OnTimeChanged` before committing `lastTime`. A listener that changed `Separator` or another formatting property called `Set(CurrentTime)` with the old baseline still active.
- **Observable consequence:** an unconditional formatting refresh can recurse indefinitely. A nested `Set(120)` from the event for `90` was subsequently overwritten in `lastTime` by the outer call, causing a duplicate notification on the next `Set(120)`.
- **Fix:** commit the notification baseline before invoking listeners; there is no state write after the callback.
- **Regression:** a bounded formatting listener receives exactly one notification and produces `01.30`; a nested update produces exactly `[90, 120]`, leaves `CurrentTime == 120` and displays `02:00`.

### F6 — High: accepted duration strings can throw during display, and invalid floats corrupt state

- **Trigger/source:** the duration parser legitimately accepts finite floats such as `1e30`, but Compact display passed them directly to `TimeSpan.FromSeconds`. NaN/infinity and large Clock values also had no component-level validation; `CurrentTime` was assigned before formatting.
- **Observable consequence:** a `TrySetFromString` call could throw for a parsable duration, or leave state inconsistent with the last valid label. NaN could clear the label while becoming `CurrentTime`.
- **Fix:** validate finiteness and the active display range before mutating state. Clock accepts displayed absolute seconds within `Int32`; Compact accepts values strictly below the positive `TimeSpan` bound. Unsupported updates leave the time, label and events unchanged. Parsing now checks the display domain before returning success. Finite negative values still clamp to zero when negative display is disabled.
- **Regressions:** six invalid Clock inputs, fixed/dynamic Compact overflow, parsable but undisplayable strings, plus successful dynamic/fixed Compact durations beyond the Clock range. The latter protect against accidentally restricting Compact totals to `Int32`.

### F7 — Low: parsing reports successful display without a target

- **Trigger/source:** `TrySetFromString` returned `true` after parsing even when `Set` returned immediately because `text` was missing.
- **Observable consequence:** callers could treat a rejected display update as successful, contrary to the documented return contract.
- **Fix/regression:** require a live target before reporting success; the new missing-target regression asserts `false` and unchanged `CurrentTime`.

Additional review: both components retain explicit target assignment and same-object `GetComponent<TMP_Text>` fallback. Serialized field names/types and public/protected declarations are preserved. Runtime warnings remain opt-in and routed through `NeoDiagnostics`. The time component owns no persistent resources or scene searches. No new version-specific Unity ID APIs, dependencies or serialization migrations were introduced.

## Documentation

- `Assets/Neoxider/Docs/Tools/Text/README.md`: repaired the component table and documented the canonical assembly/namespace, C# API path, compatible Inspector event path, references/dependencies, animation ownership and rejected duration updates.
- `Assets/Neoxider/Docs/Tools/Text/SetText.md`: documented all overloads and formatting properties, exact integer path versus float interpolation, immediate replacement/cancellation, retained numeric formatting, hidden/disabled behavior, diagnostics and concrete C#/Inspector examples. Corrected the notation name to `IdleShort`.
- `Assets/Neoxider/Docs/Tools/Text/TimeToText.md`: documented fixed versus dynamic Compact configuration, parsing/target/range semantics, event reentrancy and approximate change detection, legacy centiseconds, static formatter saturation and current Compact wide totals. Removed the incorrect `TimerObject.OnTimeChanged` wiring example in favor of a compatible float event.

Кратко: исправлены потеря текста после анимации, искажение чисел при смене формата, переполнение смещения, повторные события времени и принятие непоказываемой длительности. Публичные API и сериализованные поля сохранены.

## Substantial improvement proposal

### One shared, testable duration formatting contract

**Problem.** `TimeToText` currently coordinates negative clamping, validation, zero visibility, fixed/dynamic Compact selection, Clock formatting and prefix/suffix behavior. Its static Clock facade and the low-level extensions have different invalid-input policies. The parser's float domain is larger than the display domain. A UI caller cannot obtain a validated formatted result through a single plain C# contract, and the component must know the dependency's numeric limits.

**Proposed architecture/public API.** Add an additive pure C# API, designed jointly with the `Neo.Extensions` owner, that reuses the existing Clock/Compact formatters:

```csharp
public static bool TryFormatDuration(
    float seconds,
    DurationFormatOptions options,
    out float acceptedSeconds,
    out string formatted);
```

`DurationFormatOptions` would contain display mode, `TimeFormat`, separator, negative/zero policy, fixed/dynamic Compact choice, seconds inclusion and maximum parts. The function would define finite/range rejection and return the clamped accepted value together with text. It would not reference TMP, scene objects, UnityEvents or DOTween. `TimeToText` would map its existing serialized fields into options, apply the returned text with its affixes, and retain responsibility only for target assignment and notification ordering. Reuse existing formatting routines; do not add a parallel string formatter or a mandatory service/singleton.

**Compatibility/migration.** Keep every existing serialized field, default and public component method. Introduce the options/Try API first, then route `Set`/`TrySetFromString` through it. Preserve valid output, including the documented Clock centisecond modes. Keep legacy static `FormatTime` saturation as a documented adapter; offer the Try API when callers need rejection instead. This needs coordination across `Neo.Extensions` and `Neo.Tools.Text`, not a unilateral change to another worker's assembly.

**Acceptance scenarios.** Plain C# consumers and the component agree on accepted seconds and text for every `TimeFormat`; NaN/infinity/overflow reject without partial output; permitted negatives and zero hiding have explicit results; the last float below the Clock limit succeeds and the next float rejects; fixed and dynamic Compact accept large valid `TimeSpan` values with wide totals; parsing a valid float outside the display range returns false; format-only updates do not publish a time change; nested callbacks leave the newest accepted time intact. Core cases should run without a scene, with a small component integration suite covering events and target lifetime.

**Effort/risk.** Estimated 1–2 developer days plus central Unity validation. Medium compatibility risk around rounding, legacy fractional formats, negative signs and saturation versus rejection; mitigate with table-driven output fixtures against current valid behavior. No scene/prefab migration or new package dependency is needed. **Proposal only; not implemented in this audit.**

## Verification and remaining risks

Actual checks executed by this worker:

- Manifest-derived inventory comparison: **2/2** owned C# files; no unreviewed additional C# files under the assembly root.
- Scoped `git diff --check`: passed for the owned changes. New/untracked files were additionally checked directly for trailing whitespace and final newlines.
- Python static checks: strict UTF-8 decoding; replacement-character and package mojibake-marker checks; explicit local types; lexical delimiter balance; preservation of public/protected declarations and serialized declarations against `ca198556`; documentation code fences and local links; required existing test asmdef references; unique `.meta` GUIDs across `Assets`.
- Arithmetic/source witnesses: the old `1.25` separator removal yields `125`; signed `Int32.MaxValue + 1` wraps negative; converting `Int32.MaxValue` to float gives `2147483648`, while the preceding float is `2147483520`; `3000000000` seconds decomposes into 34722 days, 5 hours and 20 minutes. These are static/independent arithmetic evidence, not execution of the Unity C# methods.
- Added **31 regression cases** in `Audit20261023NeoToolsTextSetTextTests.cs` (15) and `Audit20261023NeoToolsTextTimeToTextTests.cs` (16). They assert actual text, public state and/or event history; the two lifecycle cases use real Play Mode transitions. No new asmdef references are required. Test GUIDs: `f550a970587d42de9193e74f08e6911b`, `03ee8454d3e8496c9d1d3d44c9c57b16`.

**Concurrent dependency integration.** During this audit the `Neo.Extensions` owner updated Clock formatting to sanitize/saturate excessive inputs and Compact formatting to use wide tick-based totals. Those files were inspected read-only. The final component guard and tests reflect those changes: valid Compact totals are not restricted to `Int32`. Their implementation and validation are credited to that owner, not this report. The old Compact overflow/format leads are not claimed as remaining defects. Coordinator action: compile and run the Text regressions together with that final Extensions change set; retain the distinct documented instance-rejection/static-saturation policies.

**Not executed:** Unity import/compilation, NUnit/Unity tests, Play Mode, console checks, screenshots or builds, as required by the worker contract. No test pass is claimed. Central verification must check TMP initialization, DOTween link/kill/manual-update behavior and the two disable/destroy scenarios on supported Unity versions. The wider float precision limit of animated integers remains intentional/documented; exact values use the existing `BigInteger` path. Static checks do not substitute for compilation or independent runtime QA.
