# AudioControl

**Purpose:** Bridge component that connects a `Toggle` or `Slider` to `AMSettings` (Master/Music/Efx volume, mute), with optional custom mode via events. Namespace: `Neo.Audio`. Script: `Scripts/Audio/View/AudioControl.cs`.

**How to use:** Add to a GameObject with a `Toggle` or `Slider`, select a channel (`Master`, `Music`, `Efx`, or `Custom`); synchronization with `AMSettings` is automatic. For `Slider`, values use a normalized range `0..1`.

**Persisting volume between sessions** is configured in **[AMSettings](AMSettings.md)** — not here. Enable **Persist Volume**, set the **Save Key**, and use the **`SaveProvider`** API.

---

## Class Description

### AudioControl
- **Namespace**: `Neo.Audio`
- **Script**: `Assets/Neoxider/Scripts/Audio/View/AudioControl.cs`

**Description**  
Connects a `Toggle` or `Slider` to `AMSettings` for volume or mute control.

**Key Features**
- **Auto UI detection**: Automatically detects whether it is on a `Toggle` or `Slider`.
- **Flexible configuration**: Choose which channel to control: `Master`, `Music`, `Efx`, or `Custom`.
- **Backend selection**: `MixerOnly` (controls `MasterVolume`/`MusicVolume`/`EfxVolume` directly in the `AudioMixer`) or `AudioSourceAndMixer`.
- **UI input and refresh**: User changes call `AMSettings`; initial setup and re-enabling refresh the view without firing the UI element's value-change callbacks. Toggles also observe the corresponding reactive mute property. External volume-only changes are reflected on the next re-enable, not continuously.
- **Start and scene change**: UI initialization runs once even when settings are missing. Re-enabling or calling `Initialize(AMSettings)` retries binding and silently refreshes. Toggle mute subscriptions use disposable Neo.Reactive handles; disable, destruction and owner replacement detach the actual previous subscription.
- **Normalized percentage**: Slider values are always in the range `0..1` (volume percentage).
- **Custom events**: In `Custom` mode, attach your own logic via `onSetActiveCustom(bool)` and `onSetPercentCustom(float)`.
- **Code-free**: No scripting required. All configuration is done in the Inspector.

## Public Methods

| Method | Description |
|---|---|
| `Set(bool active)` | Applies an on/off state for the selected type (`Master`/`Music`/`Efx`/`Custom`). |
| `Set(float percent)` | Applies a normalized volume value `0..1` for the selected type (`Master`/`Music`/`Efx`/`Custom`). |
| `Initialize(AMSettings audioSettings)` | Supplies late or replacement settings. Active started controls rebind immediately; before Start the reference is retained. Null restores singleton fallback. |

`Set(true)` restores the configured percentage and clears the selected channel's mute. This lets a toggle
turn music or effects back on after another script muted them. `Set(false)` sets volume to zero; it does
not erase the remembered restore percentage. Custom controls refresh their cached value on re-enable
without invoking the Toggle/Slider callbacks again.

Without settings, the component temporarily disables an interactable built-in control. It restores only
the interactability it changed; an authored disabled Toggle/Slider stays disabled. Disabling/removing
AudioControl also releases that temporary gate. Disabled controls do not apply UI input or receive
mute updates. There is no per-frame dependency search.

For runtime-created dependencies, call `settings.Initialize(manager)` and then
`audioControl.Initialize(settings)`. Scene/UnityEvent startup can supply those component references
through the same public methods. Existing scene setups keep their singleton fallback.

## See Also
- [AMSettings](AMSettings.md)
- [AM](AM.md)
- [Audio README](README.md)
