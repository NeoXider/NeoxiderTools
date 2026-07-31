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
- **Two-way sync**: Changing the slider value changes volume in `AMSettings`. Conversely, if volume changes elsewhere, the slider or toggle state updates to match.
- **Start and scene change**: On `Start`, `AMSettings` is resolved and initial UI sync is performed; on `OnEnable` (after the first `Start`), the UI re-syncs to current values — useful with `DontDestroyOnLoad` settings and new menus on another scene. For **Toggle**, mute subscriptions: `MuteMaster` (Master), `MuteMusic` (Music), `MuteEfx` (Efx).
- **Normalized percentage**: Slider values are always in the range `0..1` (volume percentage).
- **Custom events**: In `Custom` mode, attach your own logic via `onSetActiveCustom(bool)` and `onSetPercentCustom(float)`.
- **Code-free**: No scripting required. All configuration is done in the Inspector.

## Public Methods

| Method | Description |
|---|---|
| `Set(bool active)` | Applies an on/off state for the selected type (`Master`/`Music`/`Efx`/`Custom`). |
| `Set(float percent)` | Applies a normalized volume value `0..1` for the selected type (`Master`/`Music`/`Efx`/`Custom`). |

## See Also
- [AMSettings](AMSettings.md)
- [AM](AM.md)
- [Audio README](README.md)
