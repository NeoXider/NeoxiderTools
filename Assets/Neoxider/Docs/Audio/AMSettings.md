# AMSettings

**Purpose:** Global audio settings manager (Singleton). It controls the `AudioMixer`, manages channel volumes (Master, Music, Efx), and automatically saves/loads settings between sessions (via `SaveProvider`). It also provides reactive properties (Mute states) for UI binding.

## Setup

1. Add `Add Component > Neoxider > Audio > AMSettings` to a global object (next to `AM`).
2. Assign the project's `AudioMixer` (if used) to the `audioMixer` field.
3. Ensure that the parameters (MasterVolume, MusicVolume, EfxVolume) are set as Exposed Parameters in the mixer.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `audioMixer` | The project's main `AudioMixer`. If absent, volume is changed directly on the `AudioSource`s inside `AM`. |
| `MasterVolume`, `MusicVolume`, `EfxVolume` | The string names of the Exposed Parameters in the AudioMixer. |
| `persistVolume` | If `true`, audio settings are automatically saved. |
| `saveKeyMaster`, `saveKeyMusic`, `saveKeyEfx` | Keys used for saving in `SaveProvider`. |
| `startEfxVolume`, `startMusicVolume` | Default volume (0 to 1) if no save data exists yet. |

## API

```csharp
// Mute music
AMSettings.I.SetMusic(false);

// Set master volume to 80%
AMSettings.I.SetMasterVolume(0.8f);

// Get current state for UI
bool isEfxMuted = AMSettings.I.MuteEfx.Value;
```

## AM sound channels

- The effects controls (`SetEfxVolume`, `SetEfx`, `ToggleEfx`, `SetMusicAndEfx*`) drive `AM`'s **Sfx** channel
  through `AM` itself, so pitched and positional sounds that are still ringing follow the slider or the
  toggle at once.
- `ToggleAllAudio()` also mutes and restores `AM`'s **UI** channel. Without a mixer (always the case on WebGL)
  nothing else would silence interface sounds.
- The UI channel volume has no field here: set it on `AM` (`AM.I.SetChannelVolume(SoundChannel.Ui, v)` or
  `AM.I.SetUiVolume(v)` from a slider's UnityEvent). See [AM](AM.md#sound-channels-sfx--ui).

## See Also
- [AM](AM.md) - Playing sounds.
- [AudioControl](View\AudioControl.md) - Ready-to-use UI component for checkboxes and sliders.
- [Module Root](../README.md)
