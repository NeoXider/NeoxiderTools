# Haptics

**Purpose:** one static entry point for device vibration with named feels (`Selection`, `Light`,
`Medium`, `Heavy`, `Soft`, `Rigid`, `Success`, `Warning`, `Error`), a master switch you bind to your
settings, and a repeat throttle so fast input does not turn into one long buzz.

Namespace: `Neo.Haptics`. Assembly: `Neo.Haptics`.

## Backends

| Setup | iOS | Android | Editor / desktop |
|-------|-----|---------|------------------|
| With `com.tsyk5.mobilehapticfeedback` (recommended) | Core Haptics / UIKit feedback | VibrationEffect, amplitude-aware | silent |
| Without it: `Play(HapticType)` | `Handheld.Vibrate` for `Heavy`/`Success`/`Warning`/`Error` only | same | silent |
| Without it: `Play(intensity, sharpness, seconds)`, `PlayPattern(...)` | silent (`Stop()` / `Prepare()` are no-ops) | same | silent |

Without the backend, `Play(intensity, sharpness, seconds)` and `PlayPattern(...)` never vibrate, yet they
still pass the gates and raise `Played` (as `Medium` and `Heavy` respectively), so they also share the
repeat throttle with `Play(HapticType.Medium)` / `Play(HapticType.Heavy)`. Install the backend if the
game relies on custom shapes or waveforms.

The optional package is detected by a version define (`NEO_MOBILE_HAPTICS`), the same way the
package handles Mirror: no package, no compile error, a coarser fallback. `Neo.Haptics.asmdef` lists
`tsyk5.MobileHapticFeedback` in `references` on purpose: Unity ignores a name reference to an assembly
that is not in the project, and once the package is installed that reference is what lets the
`NEO_MOBILE_HAPTICS` code see the backend types. Do not remove it.

Install the recommended backend:

```json
"com.tsyk5.mobilehapticfeedback": "https://github.com/tsyk5/MobileHapticFeedback.git?path=package/com.tsyk5.mobilehapticfeedback"
```

## Code usage

```csharp
using Neo.Haptics;

// once, where your settings live
Haptics.EnabledProvider = () => settings.VibrationOn;

Haptics.Play(HapticType.Selection); // finger picked something up
Haptics.Play(HapticType.Light);     // small confirmed action
Haptics.Play(HapticType.Heavy);     // level won
Haptics.Play(HapticType.Success);   // reward granted

Haptics.Play(0.6f, 0.9f, 0.03f);    // custom shape (intensity, sharpness, seconds)
Haptics.PlayPattern(new[] { 0.05f, 0.08f, 0.12f }, new[] { 0.4f, 0.7f, 1f });
Haptics.Prepare();                  // warm the engine before a burst (iOS)
```

## API

| Member | Description |
|--------|-------------|
| `Enabled` | Master switch, on by default. Combined with `EnabledProvider`. |
| `EnabledProvider` | Optional live source of truth, e.g. the Vibration setting. |
| `MinRepeatInterval` | The same type again within this many unscaled seconds is dropped (default `0.035`). |
| `IsSupported` | True when this device/build can vibrate. Always false in the editor. |
| `Played` | Raised for every pulse that passed the gates, on every platform - tests and debug overlays watch it. |
| `ResetThrottle()` | Forgets the last pulse so the repeat throttle starts clean (between test cases, after a scene reset). |
| `Play(HapticType)` / `Play(intensity, sharpness, seconds)` / `PlayPattern(...)` / `Stop()` / `Prepare()` | See above. |

Static state (`Enabled`, `EnabledProvider`, `MinRepeatInterval`, `Played` subscribers, the throttle) is
reset at `SubsystemRegistration`, so Enter Play Mode with domain reload disabled starts clean: bind
`EnabledProvider` and subscribe to `Played` from scene code (`Awake` / `Start`), not from edit-time code.

## Choosing a feel

- **Selection** for continuous or repeated contact (grab, drag step, picker tick). It is the only
  type that stays pleasant at a high rate.
- **Light / Medium** for confirmations, **Heavy** for the one big moment of a screen.
- **Success / Warning / Error** carry meaning; use them for outcomes, not for decoration.
