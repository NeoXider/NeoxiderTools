# Neo.Haptics: audit

Status: source audit and scoped fixes complete; central compilation and Unity tests pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Haptics/Neo.Haptics.asmdef`.
- Scope: 1/1 owned C# files reviewed; no nested assembly in this module.

## Scope

- `Assets/Neoxider/Scripts/Haptics/Haptics.cs`: reviewed completely, including every platform branch.
- Read-only context: `Neo.Haptics.asmdef`, both package test asmdefs, canonical Haptics documentation,
  installed-package inventory and representative EditMode test conventions. No existing Haptics tests
  or installed `com.tsyk5.mobilehapticfeedback` backend source were found.
- Ownership respected: changes are limited to this source, owned Haptics documentation, this report,
  and a new uniquely prefixed regression file with its own metadata. No shared configuration changed.

## Complete source review

| Area | Review and outcome |
|------|--------------------|
| Public contracts / serialization | All nine enum members and all public declarations match `ca198556`. This is a static API with no serialized scene fields, ScriptableObjects or scene searches. Invalid enum handling is now explicit. |
| Lifecycle | `SubsystemRegistration` restores the master switch, provider, interval, subscriptions and all cooldown history. `ResetThrottle()` clears the same history. Scene-owned delegates still need consumer cleanup, now documented. |
| Enable gates | Master switch short-circuits the live provider. Rejected/disabled requests do not create cooldown history. Provider and observer exceptions remain caller responsibilities; the API uses Unity main-thread state. |
| Throttle / timing | Reviewed unscaled time, independent type histories, custom-to-`Medium` and pattern-to-`Heavy` aliases, zero interval, reset and domain-reload behavior. Fixed interleaved-repeat bypass. Interval assignment retains its existing comparison semantics; no new coercion was introduced. |
| Inputs | Reviewed null/empty/mismatched arrays, invalid enum values and non-finite scalars/paired segments. Finite scalar clamping and shorter-array pairing remain compatible. Finite pattern durations/amplitudes retain their existing caller contract: nonnegative seconds and amplitudes in `0..1`. |
| Backend / platform | Reviewed all nine native mappings, strong-type-only iOS/Android fallback, editor/desktop silence and observation without hardware playback. Native `Prepare()` requires support and enablement; `Stop()` deliberately remains available when disabled. No backend mapping or platform directive changed. |
| Resources / performance | Native waveform arrays are allocated only after valid input, gates and support checks. Repeat history is bounded to nine validated enum values; reset clears entries. Native engine ownership stays with the optional package. No new update loop, logs or scene objects. |
| Version compatibility | No obsolete object-ID APIs, new Unity-version branches or assembly dependencies were introduced. Used existing BCL collections and `float.IsNaN`/`IsInfinity`, rather than a newer framework-only finite check. Optional-backend and Unity-version branches still need compilation by the coordinator. |
| C# / scene / NoCode | Module has no built-in scene or NoCode wrapper. Updated the canonical guide with thin scene methods that directly call the typed API and can be connected to UnityEvents. |

Reuse check: the NeoxiderTools `modules.md`, `tools.md` and `extensions.md` references had no matching
haptics validation/throttle helper. Kept the existing static facade, Unity time source and native
adapter calls; the only added helper is a private finite-number guard. No parallel framework added.

## Findings and fixes

The reproductions below are established from the original control flow, not claimed Unity executions.
Source locations refer to the owned `Assets/Neoxider/Scripts/Haptics/Haptics.cs`.

### HAP-01 — P2: interleaving types bypassed the repeat interval

- Trigger: request `Selection`, `Light`, `Selection` inside one `MinRepeatInterval`.
- Before: baseline `PassesGates` (lines 240–256) remembered only `_lastType` and `_lastPlayTime`.
  All three requests emitted `Played`; a different type erased the earlier type's effective cooldown.
  Repeated alternating input could queue excessive hardware pulses despite the documented throttle.
- Fix: `PassesGates` now maintains a timestamp per valid type in `LastPlayTimes`. Other types can play
  independently without clearing history. Both reset paths clear the dictionary. Custom pulses and
  waveforms continue sharing the named `Medium` and `Heavy` histories respectively.
- Regressions: all nine types interleaved across two passes; custom/named and pattern/named alias
  sequences; resetting multiple histories and allowing immediate repeats at interval zero.

### HAP-02 — P2: empty patterns announced a pulse and suppressed real feedback

- Trigger: `PlayPattern` receives either or both arrays empty, followed by `Play(Heavy)`.
- Before: baseline lines 194–213 updated the `Heavy` timestamp and raised `Played` before checking
  paired length. The check existed only inside the native branch. An empty request therefore emitted
  a false observation and throttled the real `Heavy` request even in the editor or without a backend.
- Fix: calculate the shorter length and reject zero pairs before gates/events on every platform.
- Regressions: empty/empty, empty/nonempty, nonempty/empty; each asserts zero events immediately
  after the empty call and one event from the subsequent valid request. Null cases and unequal-length
  pairing protect the neighboring contracts.

### HAP-03 — P2: non-finite numeric requests reached observation and backend dispatch

- Trigger: any shaped-pulse parameter or any paired pattern element is `NaN` or infinity.
- Before: baseline shaped/pattern paths (lines 175–218) had no finite validation before gates/events.
  Such input was announced as `Medium`/`Heavy`, consumed the real pulse's interval and could enter
  native argument construction. Actual device consequences were not measured.
- Fix: reject the whole malformed request before gate mutation or notification. Check every paired
  segment, while continuing to ignore the unmatched tail of unequal-length arrays. Existing finite
  scalar clamping and valid pattern data are unchanged.
- Regressions: all three non-finite values in every scalar position and both paired arrays, followed
  by valid feedback; non-finite values exclusively in unpaired tails remain ignored.

### HAP-04 — P2: undefined enum values generated phantom feedback observations

- Trigger: a caller casts an out-of-range integer to `HapticType`.
- Before: baseline `PassesGates` accepted it and `Play` raised `Played`, although no native switch case
  or fallback could dispatch that type. It also displaced the previous valid type's cooldown.
- Fix: reject values outside the existing enum range before evaluating enablement or cooldowns.
- Regressions: `-1`, the next unused value `9`, and `int.MaxValue`, interleaved with a valid type;
  assert the event sequence contains only the first valid pulse.

Compatibility: public signatures, enum values, default interval, supported valid inputs, native
dispatch and silent-platform observation are preserved. Repeated same-type requests separated by
another type are now intentionally throttled; malformed requests are intentionally unobservable.

По результатам полного просмотра исправлены обход ограничения повторов и ложные события от пустых
или некорректных запросов. Воспроизведение на устройствах и выполнение Unity-тестов не проверялись.

## Documentation

Updated the existing canonical `Assets/Neoxider/Docs/Haptics/Haptics.md`, without a competing guide:

- independent type cooldowns and the shared custom/pattern aliases;
- rejected-input behavior, array pairing and `Played` as accepted-request observation;
- honest fallback `IsSupported` semantics: platform availability does not probe the device motor;
- main-thread use, delegate cleanup, disabling versus stopping an already playing pattern;
- scene component example and explicit `Button.onClick` / dynamic `Toggle.onValueChanged` wiring.

Public XML comments now agree about iOS/Android fallback, capability reporting, observer behavior,
input rejection and clearing all cooldown timestamps.

## Substantial improvement proposal

### Proposal: an instance service with an injectable backend and clock — not implemented

**Problem.** The static facade combines enablement, Unity time, observation and platform dispatch.
Current EditMode tests can prove request events but cannot inspect normalized native payloads,
physical dispatch, support fallbacks or cancellation. Multiple players/tests also share one global
policy. `IsSupported` cannot distinguish named feedback from waveform capability in the fallback.

**Architecture / API.** Extract a plain C# `HapticService` that receives an `IHapticBackend` and an
explicit monotonic clock. Keep request validation and per-type throttling in that service. A compact
request value contains either a named type, a shaped pulse or a copied sequence of paired segments.
Backend capabilities distinguish named, shaped and waveform support. Suggested public surface:

```csharp
public HapticService(IHapticBackend backend, Func<double> unscaledTime);
public HapticPlayResult TryPlay(HapticType type);
public HapticPlayResult TryPlay(float intensity, float sharpness, float durationSeconds);
public HapticPlayResult TryPlayPattern(float[] durationsSeconds, float[] amplitudes);
public void Stop();
public void Prepare();
public void ResetThrottle();
```

`HapticPlayResult` should distinguish invalid/disabled/throttled requests from accepted requests and
backend dispatch availability. Preserve an accepted-request event separately from backend dispatch
evidence. Existing mobile and `Handheld` paths become small adapters; a recording backend supports
tests without Unity hardware. Keep any optional future scene emitter as a thin wrapper over the
service. Optional ScriptableObject profiles would contain values only, with no scene references.

**Compatibility / migration.** Existing `Haptics` properties, events and void methods forward to a
default service using Unity's unscaled clock. Keep enum numeric values and the current fallback
mapping. Existing clients require no changes; isolated consumers can opt into instances. Preserve
`SubsystemRegistration` reset and existing `Played` semantics. Avoid adding backend interfaces to
shared assemblies until their ownership and dependency references are agreed.

**Acceptance scenarios.**

1. A recording backend sees exact finite/clamped custom payloads and only paired waveform segments.
2. A fake clock proves rejection just below the interval and acceptance exactly at it, including
   interleaved types, zero time scale and reset.
3. Independent instances do not share enablement, subscriptions or cooldown history.
4. Capability results distinguish editor silence, platform fallback and native waveform support.
5. `Stop()` reaches the backend after enablement is switched off; `Prepare()` obeys enablement.
6. All existing facade regression cases still pass; iOS/Android device smoke checks verify styles,
   shaped pulses, patterns and cancellation with and without the optional backend.

**Effort / risk.** Approximately 2–3 developer days for extraction, recording tests and documentation,
plus device verification. Main risks are changed event ordering, facade reset/subscription migration,
native payload ownership and optional assembly wiring. Keep this as a separate reviewed change with
the existing facade tests as compatibility gates; it is outside the completed focused fixes.

## Verification and remaining risks

Completed checks:

- `git diff --check --` scoped to the source, docs, new test/metadata and this report: no whitespace
  errors. An additional direct whitespace scan covers the new untracked files too.
- Python static consistency inspection: manifest source inventory matches **1/1** actual C# files;
  one asmdef and no nested assemblies; unique regression prefix matches the assignment.
- Strict UTF-8 decoding, no replacement/C1 control characters, final newline and trailing-whitespace
  checks on changed text; source/test delimiter and preprocessor balance checked at text level.
- Public declaration and enum-body comparison against `ca198556`: unchanged. No `var`, obsolete
  instance-ID API or runtime log calls found in the edited source or new regression file.
- New metadata GUID `a117a463833c4036a382c12540757ab0` appears exactly once in `Assets`/`Packages`.
- Added `Assets/Neoxider/Tests/Edit/Audit20261023NeoHapticsTests.cs`: **12 test methods / 23 cases**,
  observing actual public `Played` sequences. Fixture is nonparallel and restores enablement/provider/
  interval and removes its own subscription. No reflection-based or source-text NUnit assertions.

**Coordinator action required before test compilation:** add `Neo.Haptics` to the references of
`Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef`. It was absent during inspection and was not edited
because shared configuration is reserved. Run the fixture `Neo.Editor.Tests.Audit20261023NeoHapticsTests`.

Compilation, tests, Unity import/console, Play Mode, native dispatch and device checks were **not run**,
as explicitly prohibited for this worker. Static delimiter/API checks are not compilation. Optional
backend source is not installed locally; both backend-presence branches and Unity 6.0–6.4 / 6.5+
compatibility remain centrally pending. No independent QA agent was used because child agents were
prohibited. No commits, installs, shared services, scenes, prefabs or shared settings were changed.
