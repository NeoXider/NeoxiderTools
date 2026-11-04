# AMSettings

**What it is:** a `Singleton` MonoBehaviour for player-facing audio settings on top of [AM](AM.md): Master/Music/Efx
volume and mute, optional `AudioMixer` exposed parameters, reactive mute state for UI, and persistence of
volumes and mutes through `SaveProvider`. Script: `Scripts/Audio/AMSettings.cs`, namespace `Neo.Audio`.

**How to use:**
1. Add `Add Component > Neoxider > Audio > AMSettings` to a global object (next to `AM`).
2. Optionally assign the project's `AudioMixer` and expose `MasterVolume`, `MusicVolume`, `EfxVolume` in it.
3. Bind UI through [AudioControl](View/AudioControl.md), or call `SetMusic(bool)`, `SetEfx(bool)`,
   `SetMusicVolume(float)`, `ToggleAllAudio()` from code or UnityEvents.
4. Leave **Persist Volume** and **Persist Mute** on to keep the player's volume and "sound off" between launches.

---

## Binding to AM

`Start` initializes AM's lazy sources before caching them, so settings also work when AM is created at
runtime without authored source references. Persisted startup levels are applied through the same path.

If AM arrives after settings have already started, call `settings.Initialize(manager)` explicitly or
disable/re-enable AMSettings to retry the singleton fallback. There is no per-frame scene search.
The saved startup values are loaded once, including explicit initialization before `Start`, and applied
once for each successful manager binding. Re-enabling or initializing the same live binding preserves
the current volume, mute and playing clip. Null or nested initialization leaves the binding unchanged.

### Mute at startup

Binding never forces the channels back on. The mute applied on the **first** binding is, in order:

1. a `SetMusic` / `SetEfx` / `ToggleMusic` / `ToggleEfx` call made **before** any manager was bound - for
   example from a consumer's `Awake` that applies its own saved flag. The request is visible at once through
   `MuteMusic` / `MuteEfx` and is applied by `Initialize`;
2. the persisted mute (see below);
3. the mute the manager's sources already have.

A later binding to a **replacement** manager carries the current Music, Efx and UI-channel mute over (the
Master mute lives on the mixer and stays as it is), so replacing AM does not bring the sound back, even when
the previous manager was already destroyed. On the first binding a persisted Master mute is re-applied on the
mixer, and a persisted UI-channel mute (set by `ToggleAllAudio`) is re-applied on `AM`.

> **Behaviour change in this release.** Binding no longer turns sound back on, and **Persist Mute** is on by
> default, also for components already in your scenes. `SetMusic`, `SetEfx`, the `Toggle*` methods and
> `ToggleAllAudio` are therefore saved as the **player's preference**: a mute that is still on when the game
> quits is still on at the next launch. For temporary silence (pause menu, ad break, cutscene) do not use these
> methods; use `AudioListener.pause` / `AudioListener.volume`, a mixer snapshot or
> [AudioFocusMute](AudioFocusMute.md), or turn **Persist Mute** off. `AM.SetChannelMute(SoundChannel.Sfx, ...)`
> is the same flag as `SetEfx` and is saved with the next mute change, so it is not a temporary alternative.

## Persistence

| Setting | Saved as | Written when |
|---------|----------|--------------|
| Music/Efx volume | float 0..1 under `saveKeyMusic` / `saveKeyEfx` | a volume setter runs |
| Master volume | float 0..1 under `saveKeyMaster` (the restore level while muted) | Master volume or mute changes, mixer only |
| Mute | int bit mask under `saveKeyMute`: 1 = Music, 2 = Efx, 4 = Master, 8 = UI channel | a mute setter or toggle runs after the first binding |

`persistVolume` off writes and reads no key at all; `persistMute` off keeps volumes but not mutes. Binding
and restoring never write. Mute changes before the first binding are not written either: the other
channels' real state is unknown at that point, and writing them would overwrite last session's choice.

## Fields

| Field | Description |
|-------|-------------|
| `audioMixer` | The project's main `AudioMixer`. If absent, volume is changed on `AM`'s channels directly. |
| `MasterVolume`, `MusicVolume`, `EfxVolume` | The names of the exposed parameters in the AudioMixer. |
| `MuteMaster`, `MuteMusic`, `MuteEfx` | `ReactivePropertyBool` mute state for UI. Subscribe with `Subscribe(...)` / `OnChanged`. |
| `persistVolume` | Persist volumes through `SaveProvider` (default on). Off also disables mute persistence. |
| `saveKeyMaster`, `saveKeyMusic`, `saveKeyEfx` | Keys of the saved volumes. Change the prefix to avoid collisions. |
| `persistMute` | Persist the mute state too (default on). Needs `persistVolume`. |
| `saveKeyMute` | Key of the saved mute bit mask (default `Neo.Audio.AMSettings.Mute`). |
| `startEfxVolume`, `startMusicVolume` | Default volume (0 to 1) when no save data exists yet. |

## Methods

| Member | Description |
|--------|-------------|
| `Initialize(AM manager)` | Binds a manager explicitly; idempotent for the same live binding. |
| `SetMusic(bool active)`, `SetEfx(bool active)`, `SetMusicAndEfx(bool)` | Channel on/off. Before binding they are kept as a request. |
| `ToggleMusic()`, `ToggleEfx()`, `ToggleMusicAndEfx()` | Flip the mute (the pending request before binding). |
| `ToggleMaster()` | Mute/unmute Master on the mixer, remembering the level. |
| `ToggleAllAudio()`, `ToggleAudio(int group)` | All channels at once (Master, Music, Sfx, UI), or one group: 0 = Master, 1 = Music, 2 = Sfx. |
| `SetMusicVolume(float)`, `SetEfxVolume(float)`, `SetMusicAndEfxVolume(float)` | Channel volume 0..1 through `AM` (and the mixer when assigned). |
| `SetMasterVolume(float)` | Master mixer volume. While Master is muted the value becomes the level `ToggleMaster` restores and the mixer stays silent. |
| `SetMusicMixerVolume(float)`, `SetEfxMixerVolume(float)` | Mixer parameter only. |
| `SetMixerParameter(string, float)`, `SetMixerParameterDB(string, float)` | Any exposed parameter, normalized or in dB. |
| `GetMasterVolumeNormalized()`, `GetMusicVolumeNormalized()`, `GetEfxVolumeNormalized()` | Current levels 0..1 (Master reads 0 while muted). |
| `PersistVolume`, `PersistMute` | Typed access to the two persistence switches. |
| `MuteMusicValue`, `MuteEfxValue`, `MuteMasterValue`, `IsActiveMusic`, `IsActiveEfx` | Read-only state for conditions and reflection. |

The inspector buttons of the Music/Efx mute methods (`SetMusic`, `SetEfx`, `SetMusicAndEfx`, `ToggleMusic`,
`ToggleEfx`, `ToggleMusicAndEfx`, `ToggleAudio`) work in Play Mode only: in Edit Mode there is no bound manager,
and the click would bake a mute value into the scene or prefab.

## Examples

```csharp
// A game that stores its own "sound off" flag can apply it in Awake; binding keeps it.
private void Awake()
{
    AMSettings.I.SetMusic(!_profile.MusicOff);
}

// Settings menu.
AMSettings.I.SetMusicVolume(0.6f);
AMSettings.I.ToggleAllAudio();
bool musicMuted = AMSettings.I.MuteMusicValue;

// A runtime-created manager and UI can be wired explicitly after they become available.
settings.Initialize(manager);
audioControl.Initialize(settings);
```

Scene workflow: bind a Button's `OnClick` to `ToggleAllAudio()`, a Toggle to `SetMusic(bool)` /
`SetEfx(bool)`, or drop an [AudioControl](View/AudioControl.md) on the Toggle/Slider.

## AM sound channels

- The effects controls (`SetEfxVolume`, `SetEfx`, `ToggleEfx`, `SetMusicAndEfx*`) drive `AM`'s **Sfx** channel
  through `AM` itself, so pitched and positional sounds that are still ringing follow the slider or the
  toggle at once.
- `ToggleAllAudio()` also mutes and restores `AM`'s **UI** channel. Without a mixer
  nothing else would silence interface sounds.
- If any controlled mute flag is set (master, music, Sfx or UI), `ToggleAllAudio()` turns all channels on.
  Otherwise it turns all of them off. An independently muted Sfx or UI channel therefore restores all
  channels on the next call, rather than muting the channels that were still on.
- The UI channel volume has no field here: set it on `AM` (`AM.I.SetChannelVolume(SoundChannel.Ui, v)` or
  `AM.I.SetUiVolume(v)` from a slider's UnityEvent). See [AM](AM.md#sound-channels-sfx--ui).

Music mute also reaches ambience and outgoing music crossfade voices on AM's next `LateUpdate`.

## See also

- [AM](AM.md) - playing sounds.
- [AudioControl](View/AudioControl.md) - ready-to-use UI component for toggles and sliders.
- [Module root](README.md)
