# Audio System (Audio)

**What it is:** the sound module: AM (playing effects and music), AMSettings (volume, mute), AudioControl (sliders/toggles), PlayAudio, PlayAudioBtn, RandomMusicController, SettingMixer. Scripts are in `Scripts/Audio/`.

**Table of contents:** see [Audio/README.md](Audio/README.md) and the class links below.

---

## Core Classes

### `AM`
**Namespace:** `Neo.Audio`
**Path:** `Scripts/Audio/AudioSimple/AM.cs`

The central audio manager. It is a singleton responsible for playing sound effects and music tracks. It manages two `AudioSource` components: one for effects (Efx) and one for music.

**Public properties:**
- `Efx` (`AudioSource`): The `AudioSource` component for sound effects.
- `Music` (`AudioSource`): The `AudioSource` component for music.
- `startVolumeEfx` (`float`): The initial volume for sound effects.
- `startVolumeMusic` (`float`): The initial volume for music.

**Public methods:**
- `Play(int id)`: Plays a sound effect from the `_sounds` array by its index.
- `Play(int id, float volume)`: Plays a sound effect at the specified volume.
- `PlayMusic(int id)`: Plays a music track from the `_musicClips` array by its index.
- `PlayMusic(int id, float volume)`: Plays a music track at the specified volume.
- `SetVolume(float volume, bool efx)`: Sets the volume of the effects or music `AudioSource`.
- `ApplyStartVolumes()`: Applies the initial volumes to the `AudioSource` components.

### `AMSettings`
**Namespace:** `Neo.Audio`
**Path:** `Scripts/Audio/AMSettings.cs`

A singleton for managing global sound settings. It provides an interface for controlling volume and mute states for music and effects, and can be linked to an `AudioMixer`.

**Public fields:**
- `audioMixer` (`AudioMixer`): An optional `AudioMixer` for volume control.
- `MasterVolume` (`string`): The name of the master volume parameter in the mixer.
- `MusicVolume` (`string`): The name of the music volume parameter in the mixer.
- `EfxVolume` (`string`): The name of the effects volume parameter in the mixer.
- `OnMuteEfx` (`UnityEvent<bool>`): Event invoked when the effects mute state changes.
- `OnMuteMusic` (`UnityEvent<bool>`): Event invoked when the music mute state changes.
- `startEfxVolume` (`float`): The initial volume for effects.
- `startMusicVolume` (`float`): The initial volume for music.

**Public properties:**
- `efx` (`AudioSource`): The effects `AudioSource` from `AM`.
- `music` (`AudioSource`): The music `AudioSource` from `AM`.
- `IsActiveEfx` (`bool`): Returns `true` if effect sounds are not muted.
- `IsActiveMusic` (`bool`): Returns `true` if music is not muted.

**Public methods:**
- `SetEfx(bool active)`: Enables or mutes the effects `AudioSource`.
- `SetMusic(bool active)`: Enables or mutes the music `AudioSource`.
- `SetMusicAndEfx(bool active)`: Sets the mute state for both music and effects.
- `SetMusicVolume(float percent)`: Sets the music volume (from 0 to 1).
- `SetEfxVolume(float percent)`: Sets the effects volume (from 0 to 1).
- `SetMasterVolume(float percent)`: Sets the master volume in the `AudioMixer`.
- `SetMusicAndEfxVolume(float percent)`: Sets the volume for both music and effects.
- `ToggleMusic()`: Toggles the music mute state.
- `ToggleEfx()`: Toggles the effects mute state.
- `ToggleMusicAndEfx()`: Toggles the mute state for both music and effects.

## User Interface Components

### `AudioControl`
**Namespace:** `Neo.Audio`
**Path:** `Scripts/Audio/View/AudioControl.cs`

A universal UI component that binds a `Slider` or `Toggle` to `AMSettings` for sound control.

**Public enums:**
- `ControlType`: `Master`, `Music`, `Efx`
- `UIType`: `Auto`, `Toggle`, `Slider`

### `PlayAudioBtn`
**Namespace:** `Neo.Audio`
**Path:** `Scripts/Audio/AudioSimple/PlayAudioBtn.cs`

A simple component for playing a sound effect from `AM` when a UI `Button` is clicked.

**Public methods:**
- `AudioPlay()`: Plays the sound effect specified in `_idClip`.

## Other Components

### `PlayAudio`
**Namespace:** `Neo.Audio`
**Path:** `Scripts/Audio/AudioSimple/PlayAudio.cs`

A component for playing a sound from `AM` by its ID. Lets you configure the clip ID, volume, and playback on scene start.

**Public methods:**
- `AudioPlay()`: Plays the sound effect with the parameters set in the component.

### `SettingMixer`
**Namespace:** `Neo.Audio`
**Path:** `Scripts/Audio/SettingMixer.cs`

A component for controlling a single exposed parameter in an `AudioMixer`. Three input modes: dB (−80…20), normalized 0–1, bool (on/off).

**Public fields:**
- `parameterName` (`string`): The name of the exposed parameter in the mixer (e.g. MasterVolume, MusicVolume, EfxVolume).
- `audioMixer` (`AudioMixer`): Reference to the AudioMixer.
- `readonly float MaxDb = 20`, `MinDb = -80`: The volume range in dB.

**Public methods (three modes):**
- `SetVolumeDb(float volumeDb)`: Volume in dB (−80…20) for `parameterName`.
- `SetVolumeDb(string name, float volumeDb)`: Same for an arbitrary parameter.
- `SetVolume(float normalizedVolume)`: Normalized volume 0–1; zero is guaranteed to mute.
- `SetVolumeEnabled(bool enabled)`: On/off by flag (true = full volume, false = mute).
- `GetVolume()`: Returns the current normalized volume (0–1).
