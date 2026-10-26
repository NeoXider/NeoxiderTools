# AudioControl

**Purpose:** A UI component that finds a `Slider` or `Toggle` on the object and binds it to `AMSettings`.
It refreshes on startup and re-enable; toggles also follow reactive mute changes. External volume-only
changes are read on the next re-enable. See the [full component guide](../AudioControl.md).

## Setup

1. Add a standard UI `Toggle` (for mute/unmute) or `Slider` (for volume) to the scene.
2. Attach the `AudioControl` script to the exact same object.
3. Select what it should control (Master, Music, Efx).
4. Select `AudioSourceAndMixer` for source volume control without a mixer; `MixerOnly` requires an assigned mixer.

View refresh uses `SetIsOnWithoutNotify` / `SetValueWithoutNotify`: it does not invoke your UI callbacks.
User input still invokes those callbacks and updates settings. A music/effects toggle shows off when its
channel is muted even if the stored volume is positive, and turning it on clears that mute.

Code and UnityEvents can use `Set(bool)` for on/off or `Set(float)` for normalized volume. In `Custom`
mode these calls update the cached state and invoke the configured typed events. Re-enabling the
component refreshes the custom view from that cache.

When dependencies arrive later, call `audioControl.Initialize(settings)` or reopen the control to retry
binding. UI input is initialized once even if the initial lookup fails. Only input temporarily disabled
by AudioControl is restored; authored `interactable = false` is preserved. Mute subscriptions are
disposed on disable/destroy and moved to replacement settings; repeated initialization does not add
another input callback. Disabled AudioControl does not apply UI input or receive mute notifications.

If settings itself started before AM existed, first call `settings.Initialize(manager)` or re-enable
AMSettings. These explicit methods also support scene/UnityEvent setup without a polling loop.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `controlType` | What to control: `Master`, `Music`, `Efx` or `Custom`. |
| `uiType` | The UI element type. `Auto` will automatically find the `Toggle` or `Slider` on this GameObject. |
| `backendType` | If `AudioSourceAndMixer`, changes volume in both the `AudioSource` and the mixer. If `MixerOnly`, changes only the Exposed parameter in the mixer. |
| `onSetActiveCustom` | Events fired when toggled (used only for `Custom` type). |
| `onSetPercentCustom` | Events fired when the Slider changes (only for `Custom`). |
| `forceSliderNormalizedRange`| If `true`, the script forces the Slider's min/max range to 0 and 1. |
| `unmutePercent` | The volume level (0-1) to restore to when unmuting via a Toggle. |

## See Also
- [AMSettings](../AMSettings.md) - The manager this component synchronizes with.
- [Module Root](../../README.md)
