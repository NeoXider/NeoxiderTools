# AM (Audio Manager)

**What it is:** the central manager for sound effects and music (`Scripts/Audio/AudioSimple/AM.cs`, namespace
`Neo.Audio`), a singleton reached through `AM.I`. Sounds and music share one entry format; music entries are
pools that crossfade by default. Sounds play on two channels, **Sfx** and **UI**, each with its own volume, mute
and mixer group.

**How to use:**
1. `Add Component > Neoxider > Audio > AM` on a global object.
2. Drop clips onto **Sound Entries** to get one entry per clip, named after it. Drop them onto an existing entry's
   row to make them variations of that cue.
3. Do the same on **Music Entries**. Give each pool an id (`menu`, `gameplay`, `boss`) and pick its Mode.
4. Optionally set **Startup Music Id**; empty starts the first pool.
5. Call `AM.I.Play("id")` and `AM.I.PlayMusicPool("id")` ([Code usage](#code-usage)), or use [PlayAudio](PlayAudio.md),
   [PlayAudioBtn](PlayAudioBtn.md) and [MusicControl](MusicControl.md) without code.

---

## The record contract

One entry in either list holds:

| Part | Meaning |
|------|---------|
| **id** | Optional. Entries play by index; with an id, `AM.I.Play("hit")` works too and survives reordering. |
| **clips** | A set. A random clip plays each time, never the same one twice in a row. Put every variation of a cue in one entry. |
| **volume** | Multiplier of the channel, `0..2`, default `1`. Above `1` lifts a clip mastered too quietly. |
| **clip trim** | Per-clip multiplier, `0..2`, next to each clip. Evens out takes without re-exporting. Untouched clips read as `1`. |
| **pitch** | Optional detune with a min/max range. On by default for sounds, off for music. |
| **channel** | Sounds only. `Sfx` (default) or `Ui`: which volume, mute and mixer group the entry uses. |
| **spatial blend** | Sounds only, used by `PlayAt`: `0` = 2D, `1` = fully 3D (default). `Play` ignores it. |
| **placeholder** | The entry is empty on purpose; playing it without a clip does nothing and logs nothing. |

The collapsed row shows the id and, for a one-clip entry, the clip field, so a new entry is filled with one drag.
Expanding it lists every clip with its trim slider, plus volume, pitch and, for music, mode and fade override.

**Volume multiplies.** The audible level is `channel x entry x clip trim`. A music channel at `0.3` playing an
entry at `1` is heard at `0.3`, so the player's slider keeps working: `SetMusicVolume` and `SetEfxVolume` set the
channel, and entries and per-call overrides sit under it. Entry, trim and override go up to `2` so they can lift a
clip; the result is still capped at `1` because `AudioSource.volume` is `0..1`.

## Music pools

A music entry with several clips is a pool. It starts on a random track; the pool's **Mode** decides what happens
when the track ends:

| Mode | Behaviour |
|------|-----------|
| `Loop` (default) | The track repeats. Only the game changes it, with `NextMusicTrack()` or another pool, usually at a game beat such as a wave boundary or a boss entrance. |
| `Shuffle` | Another random track of the pool crossfades in, never the one that just played. This replaces the legacy `EnableRandomMusic()`. |

A menu, gameplay and boss soundtrack is three entries with three ids. Game code only says "play pool X" or
"next track".

## Inspector fields

| Field | Description |
|-------|-------------|
| `_efx` / `_music` | The two channel `AudioSource`s. Created automatically if null. |
| `_ambience` | Optional looping source under the music. Created on first use when empty. |
| `_ambienceVolume` | Ambience level relative to the music channel, `0..1` (`0.3`). |
| `_ambienceFadeDuration` | Ambience fade in and out, seconds (`1.5`). |
| `_soundEntries` | Sound-effect entries. |
| `_musicEntries` | Music entries, the pools. |
| `_playMusicOnStart` | Start music when the manager starts (`true`). Skipped if something already asked for a pool. |
| `_startupMusicId` | Pool to start with. Empty = the first entry. |
| `_crossfadeMusic` | Crossfade music changes (`true`). Off makes every change a hard cut unless a call passes `MusicTransition.Fade`. |
| `_musicFadeDuration` | Default crossfade length, seconds (`0.8`). A pool can override it. |
| `_randomizePitch`, `_pitchMin`, `_pitchMax` | Pitch for one-shots from the legacy `_sounds` array or a direct `AudioClip` (`off`, `0.94`, `1.06`). Entries carry their own pitch. |
| `_pitchVoices` | `AudioSource` pool size for pitched one-shots, `1..32` (`8`). |
| `_uiVolume` | UI channel volume, `0..1` (`1`). The Sfx channel volume is the `_efx` source volume. |
| `_uiMixerGroup` | Optional mixer group for the UI channel. Empty = the `_efx` source's group. |
| `_positionalVoices` | `AudioSource` pool size for `PlayAt`, `1..32` (`8`). |

**Helper sources.** The UI source, the pitch and positional voices, the crossfade source and a created ambience
source appear on first use, also in Edit Mode from an inspector button. They are `DontSave`, so opening a scene or
prefab adds no children and does not mark it dirty. AM destroys them in `OnDestroy` in Play Mode and builds; an
authored ambience source is left alone. In Edit Mode, where `OnDestroy` is not sent, helpers made by an inspector
button stay until the scene reloads and are never saved.

## Code usage

```csharp
using Neo.Audio;

// --- sound effects
AM.I.Play("hit");                    // by id, entry defaults
AM.I.Play(0);                        // by index, entry defaults
AM.I.Play("hit", 0.4f);              // quieter, still x the effects channel
AM.I.Play(myClip);                   // a clip directly, no entry needed
AM.I.Play(myClip, 0.5f);

// --- music
AM.I.PlayMusicPool("gameplay");                          // crossfade in
AM.I.PlayMusicPool("boss", MusicTransition.Instant);     // hard cut
AM.I.PlayMusicPool("boss", MusicTransition.Fade(2f));    // one-off length
AM.I.NextMusicTrack();                                   // another track of the current pool
AM.I.StopMusic();                                        // fades out
AM.I.StopMusic(MusicTransition.Instant);

AM.I.PlayMusicByClip(myTrack, 0.8f);   // a clip directly

// --- the player's volume sliders
AM.I.SetMusicVolume(0.6f);
AM.I.SetEfxVolume(0.9f);                              // = SetChannelVolume(SoundChannel.Sfx, 0.9f)
```

Channels, `TryPlay` and `PlayAt` have their own sections below.

- `PlayMusicPool` for the pool already playing does nothing: no restart, no fade into itself. A screen can
  re-assert its music every frame.
- A request without a usable track keeps the current clip, pool and volume multiplier, and does not raise
  `OnMusicStarted`. This includes `MusicOptions.Track(index)` on a null slot.
- `TryNextMusicTrack` returns `false` in that case and keeps the previous multiplier, even if the rejected options
  carried a volume override.
- Missing entries follow the usual diagnostic policy; empty placeholder pools stay silent.

### Per-call overrides

The entry supplies the defaults. `SoundOptions` and `MusicOptions` replace them for one call only and never write
back into the entry.

```csharp
AM.I.Play("ui", SoundOptions.Volume(0.6f).WithoutPitch());
AM.I.Play("step", SoundOptions.Clip(stepIndex));          // a specific clip, not a random one
AM.I.Play("charge", SoundOptions.Pitch(1f + stage * 0.1f));

AM.I.PlayMusicPool("boss", MusicOptions.Volume(0.5f).WithFade(2f));
AM.I.PlayMusicPool("menu", MusicOptions.Track(0));
```

A volume override replaces the entry volume, not the channel: `0.5` on an effects channel at `0.8` is heard at
`0.4`.

### Wave-driven soundtrack

Use `Loop` pools and let the game decide when to move:

```csharp
void OnWaveStarted(bool isBoss)
{
    AM.I.PlayMusicPool(isBoss ? "boss" : "gameplay");   // crossfades; a no-op if already there
}

void OnWaveCleared() => AM.I.NextMusicTrack();          // new track, same pool
```

## Sound channels (Sfx / UI)

Every sound entry has a **Channel**: a volume, a mute flag and an optional `AudioMixerGroup`.

| Channel | Settings live on | Notes |
|---------|------------------|-------|
| `SoundChannel.Sfx` (default) | the `_efx` `AudioSource` | `EfxVolume`, `SetEfxVolume` and `AMSettings` (Efx slider and mute) drive it. |
| `SoundChannel.Ui` | AM: `_uiVolume` (`1`), `_uiMixerGroup` (empty = the Efx group), a runtime mute | A 2D helper source created on first use; projects that never use it never get it. |

```csharp
AM.I.SetChannelVolume(SoundChannel.Sfx, 0.5f);       // the settings menu's "Effects" slider
AM.I.SetChannelVolume(SoundChannel.Ui, 0.5f);        // ... and its "Interface" slider
float ui = AM.I.GetChannelVolume(SoundChannel.Ui);
AM.I.SetUiVolume(0.5f);                              // one float argument: a Slider's UnityEvent can call it
AM.I.SetChannelMute(SoundChannel.Ui, true);
AM.I.SetChannelMixerGroup(SoundChannel.Ui, uiGroup); // null = route like Sfx
AudioMixerGroup g = AM.I.GetChannelMixerGroup(SoundChannel.Ui); // what was configured (may be null)

AM.I.Play("coin", SoundOptions.Channel(SoundChannel.Ui));  // route one play elsewhere
```

- Pick the channel per entry in the inspector. Plain one-shots play on the channel's source. Pitched one-shots
  take a voice from that channel's own rotation, so a UI click never drags a ringing explosion tail to the UI level.
- A channel change re-levels voices already ringing on it, so a dragged slider moves the sound you hear.
  `AMSettings` uses the same calls (`SetEfxVolume`, `SetChannelMute`), so its effects slider and toggle do too.
- Volumes are plain `AudioSource.volume`, never the mixer, so channels work on every platform, WebGL included,
  where the Audio Mixer is not available. The mixer group is optional routing on top.
- `AMSettings` and `AudioControl` offer only Master, Music and Efx controls. Set the UI channel from code or from a
  slider's UnityEvent via `SetUiVolume`. `AMSettings.ToggleAllAudio()` mutes and restores the UI channel too.

## Optional cues: TryPlay and placeholders

Both stay silent even with `NeoDiagnostics` warnings on:

- **`TryPlay(id)`** (also `TryPlay(id, volume)`, `TryPlay(id, options)`) returns `true` when a clip started. An
  empty id, an unknown id or an entry without a usable clip return `false` without a warning. `Play(id)` keeps
  warning about those, since they are usually typos.
- **Placeholder** (`AudioEntry.IsPlaceholder`) marks an entry created before its sound exists. While it has no
  clip, every path is a silent no-op: `Play` by id or index, `PlayAt`, `TryPlay`, and `PlayMusicPool` (the current
  music keeps playing). The inspector does not flag it as empty. Once a clip is added it plays normally.

```csharp
if (!AM.I.TryPlay("combo_" + level)) AM.I.Play("combo_max");
```

## Positional one-shots (PlayAt)

```csharp
AM.I.PlayAt("explosion", hitPoint);                                 // entry's spatial blend (default 1 = 3D)
AM.I.PlayAt("explosion", hitPoint, 0.6f);                           // entry-volume override
AM.I.PlayAt("explosion", hitPoint, SoundOptions.Spatial(0.5f));     // half 2D, half 3D, this play only
AM.I.PlayAt(debrisClip, hitPoint, 1f, 1f, SoundChannel.Sfx);        // a clip directly
```

- Same clip pick, pitch, per-call overrides and channel as `Play`. The level is `channel x entry x clip trim`,
  capped at `1`, and follows later channel changes while it rings.
- Voices come from a pool of `_positionalVoices` (`8`) sources parented to AM. A free voice is used when there is
  one, otherwise the next one in rotation is stopped and reused. Nothing is allocated per call once the pool is warm.
- Rolloff, min and max distance and spread come from the `_efx` source on every channel; tune them there. Doppler
  is off on positional voices.
- The call returns the voice that took the sound, or `null`. The pool reuses it, so read it at once and do not keep
  the reference.
- Keep AM on a static manager object: its transform is the voices' parent.

## Crossfade

Every music change fades across `_musicFadeDuration`. The outgoing track moves to a hidden second `AudioSource`
at its exact position, and the primary `_music` source takes the incoming one. `Music`, `GetCurrentMusicClip()`
and volume tweaks on `_music` therefore always describe the track you hear.

- Fades run on `Time.unscaledDeltaTime`, so `Time.timeScale = 0` does not freeze a transition.
- Changing the music channel volume scales the incoming and outgoing tracks at once, during a stop fade too,
  without restarting their fades. Volume zero silences all music voices; raising it restores their fade levels.
- The music source's `mute` is copied to outgoing voices in `LateUpdate`, so `AMSettings.SetMusic(false)` also
  silences tracks fading away.
- Stopping during a crossfade fades each voice from its own level.
- Outside Play Mode (EditMode tests, inspector buttons) every transition is a clean cut.

## Ambience

A second loop under the music, such as a night lake, rain or a crowd. It has its own source (`_ambience`), so it
keeps running while music pools crossfade over it.

```csharp
AM.I.PlayAmbience(nightLakeClip);   // fades in over _ambienceFadeDuration (1.5 s)
AM.I.AmbienceVolume = 0.3f;         // relative to the music channel
AM.I.StopAmbience();                // fades out, then stops
```

- Audible level = music channel volume × `AmbienceVolume` × fade, so a music slider moves both.
  `AM.SetMusicVolume`, `AMSettings.SetMusicVolume` / `SetMusicAndEfxVolume` (called by an `AudioControl` Music or
  Master slider) and `ApplyStartVolumes` (run by `AMSettings.Start`) all set the music channel.
- It uses the music source's mixer group and copies its `mute` every frame, so `AMSettings.SetMusic(false)`
  silences it too.
- Asking for the clip already playing is a no-op; `PlayAmbience(null)` stops it. Fades run on unscaled time and
  are instant outside Play Mode.

## Releasing caller-owned runtime clips

Call `ReleaseClips(params AudioClip[] clips)` before destroying clips you generated or otherwise own:

```csharp
// PlayAt also supports 2D effects on a separately controllable pooled voice.
am.PlayAt(generatedEffect, Vector3.zero, 1f, 0f);
am.PlayMusicByClip(generatedMusic);

// On disposal, after removing your event listeners:
am.ReleaseClips(generatedEffect, generatedMusic);
// The caller then destroys its clips.
```

- It compares exact clip references. It stops and clears matching primary music, outgoing crossfade, ambience and
  positional voices, and detaches matching legacy random-track references without shifting the other indices.
- Other tracks keep playing. Sources, channel settings and the entry lists stay intact.
- If primary music was active, `OnMusicStopped` fires once after cleanup. Releasing only an outgoing track does not
  report the current track as stopped.
- Repeated release is harmless. Release neither creates sources nor destroys clips.
- Shared `Play(...)` effects are outside this API: Unity cannot stop one `PlayOneShot` among several on a shared
  source. Use `PlayAt(..., spatialBlend: 0f)` for releasable 2D effects.
- Remove your own runtime catalog registrations, if any, before destroying registered clips. Ownership is never
  inferred from a source name or hierarchy.

## Pausing in the background and during ads

`AM` does not pause itself when the app is minimised. On mobile, audio must stop in the background (home button,
screen lock, a call) and during a rewarded video, then resume where it stopped. `AudioListener.pause = true` does
that for every source, including AM's music, ambience and pooled effects, and keeps their playback positions.

Give `AudioListener.pause` one owner that combines every reason, so two scripts cannot unpause each other:

```csharp
public sealed class AppAudioPause : MonoBehaviour
{
    static bool _ad, _background;
    public static bool AdPlaying { set { _ad = value; Apply(); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        GameObject go = new GameObject(nameof(AppAudioPause)) { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<AppAudioPause>();
    }

    void OnApplicationPause(bool paused) { _background = paused; Apply(); }
    // The Editor loses focus whenever another window is clicked; only real devices count focus.
    void OnApplicationFocus(bool focused) { if (!Application.isEditor) { _background = !focused; Apply(); } }
    static void Apply() => AudioListener.pause = _ad || _background;
}
```

A source that must keep playing while the listener is paused, such as a UI click during an ad overlay, sets
`AudioSource.ignoreListenerPause = true`.

## Random pitch

```csharp
AM.I.RandomizePitch = true;        // legacy / direct-clip one-shots only
AM.I.SetPitchRange(0.9f, 1.1f);    // the order is normalised for you
```

`AudioSource.pitch` retunes everything ringing on a source, so each pitched shot takes a voice from a small
round-robin pool of its channel (parented to `_efx` for Sfx, to the UI source for Ui). A voice re-copies its
channel's routing, mute and volume on every shot. With the toggle off nothing is allocated and playback goes
straight through the channel's source.

## No-code use

- Configure everything in the inspector, pools and modes included.
- [PlayAudio](PlayAudio.md) and [PlayAudioBtn](PlayAudioBtn.md) play a sound by id, picked from a dropdown of the
  ids configured on `AM`.
- [MusicControl](MusicControl.md) starts a pool, moves to the next track or stops the music.
- `Play(string)`, `PlayMusicPool(string)`, `NextMusicTrack()`, `StopMusic()` and `SetUiVolume(float)` take zero or
  one argument, so a UnityEvent can call them directly.

## Older scenes

Old scenes need no changes:

- Entries without channel data load as `Channel = Sfx`, `Spatial Blend = 1`, `Placeholder = off`; `_uiVolume`,
  `_uiMixerGroup` and `_positionalVoices` take `1`, empty and `8`.
- The legacy `_sounds`, `_musicClips`, `_randomMusicTracks` and `_useRandomMusic` stay serialized and migrate into
  the entry lists once, on first load.
- Each legacy `Sound` becomes one entry in the same order, so `Play(int)` is unchanged. A legacy `volume == 0`
  meant full and becomes `1`. Migrated entries inherit the old global pitch switch.
- Each legacy music clip becomes one entry in order, so `PlayMusic(int)` is unchanged. The random list is appended
  after them as a `Shuffle` pool with the id `Random`.
- `EnableRandomMusic()` and `SetRandomMusicTracks(...)` still work and are `[Obsolete]` in favour of pools.
- `PlayMusic(int)` and `PlayMusicByClip` follow `channel x entry` instead of writing the `AudioSource` volume. With
  the channel at `1` the result is the same.
- The music `AudioSource` volume authored in the inspector is adopted once as the music channel, so a source you
  turned down stays down.

## See also

- [MusicControl](MusicControl.md) — no-code music pool control.
- [AMSettings](AMSettings.md) — audio settings and saving.
- [PlayAudioBtn](PlayAudioBtn.md) — component for buttons.
- [Module root](README.md)
