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
- `StartVolumeEfx` (`float`): The initial volume for sound effects (`startVolumeEfx` is a deprecated alias).
- `StartVolumeMusic` (`float`): The initial volume for music (`startVolumeMusic` is a deprecated alias).

**Public events:**
- `OnMusicStarted` (`Action<AudioClip>`): Raised when music starts playing.
- `OnMusicStopped` (`Action`): Raised when music stops.
- `OnRandomMusicTrackChanged` (`Action<AudioClip>`): Raised when the random-music track changes.

**Public methods:**
- `Play(int id)` / `Play(int id, float volume)`: Plays a sound effect from the `_sounds` array by index.
- `Play(AudioClip clip)` / `Play(AudioClip clip, float volume)`: Plays a clip directly, without adding it to `_sounds`.
- `PlayMusic(int id)` / `PlayMusic(int id, float volume)`: Plays a music track from `_musicClips` by index.
- `PlayMusicByClip(AudioClip clip)` / `PlayMusicByClip(AudioClip clip, float volume)`: Plays a music clip directly.
- `StopMusic()`: Stops any music (single track or random mode) and raises `OnMusicStopped`.
- `EnableRandomMusic()` / `DisableRandomMusic()` / `IsRandomMusicEnabled()`: Controls random background music.
- `SetRandomMusicTracks(params AudioClip[] tracks)`: Replaces the random-music track list at runtime.
- `GetCurrentMusicClip()`: Returns the currently playing music clip, or null.
- `SetVolume(float volume, bool efx)`, `SetMusicVolume(float)`, `SetEfxVolume(float)`: Set AudioSource volumes.
- `ApplyStartVolumes()`: Applies `StartVolumeEfx` / `StartVolumeMusic` to the `AudioSource` components.

### `AMSettings`
**Namespace:** `Neo.Audio`
**Path:** `Scripts/Audio/AMSettings.cs`

A singleton for managing global sound settings. It provides an interface for controlling volume and mute states for music and effects, and can be linked to an `AudioMixer`.

**Public fields:**
- `audioMixer` (`AudioMixer`): An optional `AudioMixer` for volume control.
- `MasterVolume` (`string`): The name of the master volume parameter in the mixer.
- `MusicVolume` (`string`): The name of the music volume parameter in the mixer.
- `EfxVolume` (`string`): The name of the effects volume parameter in the mixer.
- `MuteEfx`, `MuteMusic`, `MuteMaster` (`ReactivePropertyBool`): Reactive mute states; subscribe via `MuteEfx.OnChanged` or read `MuteEfx.Value`.
- `startEfxVolume` (`float`): The initial volume for effects.
- `startMusicVolume` (`float`): The initial volume for music.

**Public properties:**
- `efx` (`AudioSource`): The effects `AudioSource` from `AM`.
- `music` (`AudioSource`): The music `AudioSource` from `AM`.
- `IsActiveEfx` (`bool`): Returns `true` if effect sounds are not muted.
- `IsActiveMusic` (`bool`): Returns `true` if music is not muted.
- `MuteEfxValue`, `MuteMusicValue`, `MuteMasterValue` (`bool`): Current mute states (for NeoCondition / reflection).

**Public methods:**
- `SetEfx(bool active)`: Enables or mutes the effects `AudioSource`.
- `SetMusic(bool active)`: Enables or mutes the music `AudioSource`.
- `SetMusicAndEfx(bool active)`: Sets the mute state for both music and effects.
- `SetMusicVolume(float percent)`: Sets the music volume (from 0 to 1).
- `SetEfxVolume(float percent)`: Sets the effects volume (from 0 to 1).
- `SetMasterVolume(float percent)`: Sets the master volume in the `AudioMixer`.
- `SetMusicAndEfxVolume(float percent)`: Sets the volume for both music and effects.
- `SetMasterVolume(float percent)`, `SetMusicMixerVolume(float)`, `SetEfxMixerVolume(float)`: Set mixer-only volumes.
- `GetMasterVolumeNormalized()`, `GetMusicVolumeNormalized()`, `GetEfxVolumeNormalized()`: Read normalized (0-1) volumes.
- `SetMixerParameter(string, float)` / `SetMixerParameterDB(string, float)`: Set any exposed mixer parameter.
- `ToggleMusic()`, `ToggleEfx()`, `ToggleMusicAndEfx()`, `ToggleMaster()`, `ToggleAllAudio()`: Toggle mute states.
- `ToggleAudio(int group)`: Toggle by group (0 = Master, 1 = Music, 2 = Sfx) — single-int form for NeoCondition.

## User Interface Components

### `AudioControl`
**Namespace:** `Neo.Audio`
**Path:** `Scripts/Audio/View/AudioControl.cs`

A universal UI component that binds a `Slider` or `Toggle` to `AMSettings` for sound control.

**Public enums:**
- `ControlType`: `Master`, `Music`, `Efx`, `Custom`
- `UIType`: `Auto`, `Toggle`, `Slider`
- `BackendType`: `AudioSourceAndMixer`, `MixerOnly`

**Public methods:**
- `Set(bool active)`: Mutes/unmutes the selected channel (or raises the Custom bool event).
- `Set(float percent)`: Sets the normalized (0-1) volume of the selected channel (or raises the Custom float event).

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
- `parameterType` (`MixerParameterType`): Preset channel (`Master`, `Music`, `Efx`) or `Custom`.
- `customParameterName` (`string`): Exposed parameter name, used only when `parameterType = Custom` (was `parameterName`; auto-migrated).
- `audioMixer` (`AudioMixer`): Reference to the AudioMixer.
- `const float MaxDb = 20f`, `MinDb = -80f`: The volume range in dB.

**Public methods (three modes):**
- `SetVolumeDb(float volumeDb)`: Volume in dB (−80…20) for the selected parameter.
- `SetVolumeDb(string name, float volumeDb)`: Same for an arbitrary parameter (empty name falls back to the selected one).
- `SetVolume(float normalizedVolume)` / `Set(float)`: Normalized volume 0–1; zero mutes.
- `SetVolumeEnabled(bool enabled)` / `Set(bool)`: On/off by flag (true = full volume, false = mute).
- `Set(MixerParameterType type, float|bool)` / `SetCustom(string name, float|bool)`: Target a specific parameter.
- `GetVolume()`: Returns the current normalized volume (0–1).
