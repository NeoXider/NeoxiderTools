# AM (Audio Manager)

**Purpose:** Central manager for sound effects and music. Singleton (`AM.I`), with separate `AudioSource`
channels for music and effects, and since 10.13.11 two sound channels — **Sfx** and **UI** — with their own
volume, mute and mixer group (see [Sound channels](#sound-channels-sfx--ui)).

Sounds and music share **one record contract**, so there is one thing to learn rather than two. Music
records are additionally **pools**, and every music change **crossfades** by default.

## The record contract

One entry in either list holds:

| Part | Meaning |
|------|---------|
| **id** | Optional. Play by index as always; give it an id and `AM.I.Play("hit")` works too - and survives reordering the list. |
| **clips** | A **set**, not one clip. A random clip plays each time, never the same one twice in a row. Put every variation of a cue in one entry. |
| **volume** | A **multiplier** of the channel, `0..2`, default `1`. What you hear is `channel volume x entry volume`. Above `1` lifts a clip that was mastered too quietly. |
| **clip trim** | A per-clip multiplier, `0..2`, drawn next to each clip. Takes are rarely matched in level; this evens them out without re-exporting the file. Untouched clips read as `1`. |
| **pitch** | Optional detune with a min/max range. Defaults to **on** for sounds, **off** for music. |
| **channel** | Sounds only. `Sfx` (default) or `Ui` — which volume / mute / mixer group the entry plays on. |
| **spatial blend** | Sounds only. Used by `PlayAt`: `0` = 2D, `1` = fully 3D (default). Ordinary `Play` ignores it. |
| **placeholder** | The entry is empty on purpose (its sound is not delivered yet): playing it while it has no clip does nothing and logs nothing. |

> **Volume multiplies.** A music channel at `0.3` playing an entry at `1` comes out at `0.3`. This is what
> keeps the player's own volume slider working: `SetMusicVolume` / `SetEfxVolume` set the *channel*, and
> every entry and every per-call override sits underneath it.
>
> The full product is `channel x entry x clip trim`. Entry, trim and per-call override each go up to `2`,
> because they are multipliers and a ceiling of `1` could only ever make a clip *quieter* than the channel.
> The audible music level is still capped at `1` - `AudioSource.volume` genuinely is `0..1` - so the
> headroom pays off against a channel the player has turned down.

Two independent ways to stop a repeated cue sounding like one sample on a loop - several clips, or a pitch
spread. They compose; use either or both.

## Music pools

A music entry with several clips **is a pool**. It starts on a random track; what happens at the end of
that track is the pool's **Mode**:

| Mode | Behaviour |
|------|-----------|
| `Loop` (default) | The track repeats. Only the game changes it - `NextMusicTrack()` or another pool. A track change usually belongs to a game beat (a wave boundary, a boss entrance), not to wherever the audio file happened to end. |
| `Shuffle` | When the track ends, another random track of the same pool crossfades in, never repeating the one that just played. This is what the legacy `EnableRandomMusic()` did. |

A menu / gameplay / boss soundtrack is therefore **three entries with three ids**, configured in the
inspector. Game code only ever says "play pool X" or "next track".

## Setup

1. `Add Component > Neoxider > Audio > AM` on a global object.
2. Drop clips onto the **Sound Entries** list to get one entry per clip, named after it. Drop them onto an
   existing entry's row instead to make them variations of that one cue.
3. Same for music, on the **Music Entries** list. Give each pool an id (`menu`, `gameplay`, `boss`) and
   pick its Mode.
4. Optionally set **Startup Music Id**; leave it empty to start with the first pool.

The collapsed row of an entry shows its id and, for a one-clip entry, the clip field itself - so a fresh
entry is filled by one drag with nothing to expand. Expanding it lists every clip with its own trim
slider, plus volume, pitch and (for music) mode and fade override.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_efx` / `_music` | The two channel `AudioSource`s. Created automatically if null. |
| `_soundEntries` | Sound-effect entries (the record contract above). |
| `_musicEntries` | Music entries - the pools. |
| `_playMusicOnStart` | Start music when the manager starts. Skipped if something already asked for a pool. |
| `_startupMusicId` | Which pool to start with. Empty = the first entry. |
| `_crossfadeMusic` | Crossfade music changes. Off makes every change a hard cut unless a call says otherwise. |
| `_musicFadeDuration` | Default crossfade length in seconds (`0.8`). A pool can override it. |
| `_randomizePitch`, `_pitchMin`, `_pitchMax` | Pitch for one-shots played from the **legacy** `_sounds` array or straight from an `AudioClip`. Entries carry their own pitch settings. |
| `_pitchVoices` | Size of the `AudioSource` pool used for pitched one-shots (`8`). |
| `_uiVolume` | Volume of the UI channel, `0..1` (`1`). The Sfx channel volume is the `_efx` source volume. |
| `_uiMixerGroup` | Optional mixer group for the UI channel. Empty = the `_efx` source's group. |
| `_positionalVoices` | Size of the `AudioSource` pool used by `PlayAt` (`8`). |

## Code Usage

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

// --- channels (the player's volume sliders)
AM.I.SetMusicVolume(0.6f);
AM.I.SetEfxVolume(0.9f);                              // = SetChannelVolume(SoundChannel.Sfx, 0.9f)
AM.I.SetChannelVolume(SoundChannel.Ui, 0.5f);

// --- optional cues, positional one-shots
bool played = AM.I.TryPlay("door_creak");             // false, silently, when there is nothing to play
AM.I.PlayAt("explosion", hitPoint);                   // 3D one-shot at a world position
```

Calling `PlayMusicPool` for the pool that is already playing does **nothing** - no restart, no fade into
itself - so a screen is free to re-assert its music every frame.

### Per-call overrides

The entry supplies the defaults; `SoundOptions` / `MusicOptions` replace them **for one call only** and
never write back into the entry. No `SetVolume` / play / `SetVolume` dance, which leaks state the moment
anything throws in between.

```csharp
AM.I.Play("ui", SoundOptions.Volume(0.6f).WithoutPitch());
AM.I.Play("step", SoundOptions.Clip(stepIndex));          // a specific clip, not a random one
AM.I.Play("charge", SoundOptions.Pitch(1f + stage * 0.1f));

AM.I.PlayMusicPool("boss", MusicOptions.Volume(0.5f).WithFade(2f));
AM.I.PlayMusicPool("menu", MusicOptions.Track(0));
```

A volume override replaces the **entry** volume, not the channel - `0.5` against an effects channel at
`0.8` is heard at `0.4`, so the player's slider keeps working.

### Wave-driven soundtrack

The pattern the pools were built for: `Loop` pools, and the game decides when to move.

```csharp
void OnWaveStarted(bool isBoss)
{
    AM.I.PlayMusicPool(isBoss ? "boss" : "gameplay");   // crossfades; a no-op if already there
}

void OnWaveCleared() => AM.I.NextMusicTrack();          // new track, same pool
```

## Sound channels (Sfx / UI)

Every sound entry has a **Channel**. A channel is a volume, a mute flag and an optional `AudioMixerGroup`;
the audible level of a cue is `channel volume x entry volume x clip trim`.

| Channel | Volume / mute / mixer group live on | Notes |
|---------|-------------------------------------|-------|
| `SoundChannel.Sfx` (default) | the `_efx` `AudioSource` | The channel every entry used before channels existed. `EfxVolume`, `SetEfxVolume` and `AMSettings` (Efx slider and mute) keep driving it. |
| `SoundChannel.Ui` | AM: `_uiVolume` (`1`), `_uiMixerGroup` (empty = the Efx group), a runtime mute | A 2D source AM creates on first use (also in edit mode, from an inspector button) and never saves into the scene. Projects that never use it never get it. |

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

- Pick the channel per entry in the inspector (expand the entry: **Channel**). Plain one-shots play on the
  channel's source; pitched one-shots take a voice from **that channel's own** pitch-voice rotation, which
  copies the channel's volume, mute and group. The rotations are separate on purpose: a voice may still be
  ringing an explosion tail when its turn comes round again, and a UI click must not drag that tail to the UI
  level.
- A channel change re-levels voices that are already ringing on that channel, so a slider being dragged moves
  the sound you are hearing. `AMSettings` goes through the same calls (`SetEfxVolume`, `SetChannelMute`), so its
  effects slider and toggle move ringing voices too.
- Volumes are plain `AudioSource.volume`, never the mixer, so channels work on every platform, including
  WebGL, where the Audio Mixer is not available. The mixer group is optional routing on top.
- `AMSettings` and `AudioControl` offer only Master / Music / Efx controls; the UI channel volume is set from
  code (or from a slider's UnityEvent via `SetUiVolume`). `AMSettings.ToggleAllAudio()` does mute and restore
  the UI channel as well - without a mixer (WebGL) nothing else would.

## Optional cues: TryPlay and placeholders

Two tools for "there may be nothing to play here", both silent even with `NeoDiagnostics` warnings on:

- **`TryPlay(id)`** (also `TryPlay(id, volume)`, `TryPlay(id, options)`) plays the entry and returns `true`
  when a clip started. An empty id, an unknown id or an entry without a usable clip return `false` with no
  warning. `Play(id)` keeps warning about those - they are usually typos.
- **Placeholder** (entry flag, `AudioEntry.IsPlaceholder`) marks an entry created ahead of its sound. While
  it has no clip, *every* path is a silent no-op: `Play` by id or index, `PlayAt`, `TryPlay`, and for music
  `PlayMusicPool` (the current music keeps playing). The inspector stops flagging it as empty. When the clip
  arrives it plays normally - there is nothing to untick.

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

- Same clip pick, pitch randomisation, per-call overrides and channel as `Play`. The level is
  `channel x entry x clip trim`, capped at `1` because it is set on the voice's `AudioSource.volume`
  (`PlayOneShot` has a per-shot scale, `Play` does not). It follows later channel changes while it rings.
- Voices come from a pool of `_positionalVoices` (`8`) `AudioSource`s parented to AM, created on first use
  (also in edit mode, from an inspector button) and never saved into the scene: a free voice when there is one, otherwise the next one in rotation is stopped and reused. A voice
  carries one sound, so reusing it never drags an older sound to the new point. Nothing is allocated per call
  once the pool is warm.
- Rolloff, min / max distance and spread come from the `_efx` source on every channel - tune them there; the
  channel decides only volume, mute and mixer group. Doppler is off on positional voices: a voice is moved to
  the point and started in the same frame, and that jump would read as a huge velocity.
- The call returns the voice that took the sound (or `null`). The pool reuses it, so read it right away and
  do not keep the reference.
- AM's own transform is the voices' parent: keep AM on a static manager object, not on something that moves.

## Crossfade

Every music change fades across `_musicFadeDuration`. A crossfade needs two sources, so the outgoing track
is handed to a hidden second `AudioSource` at its exact playback position while the primary `_music`
source takes the incoming one.

> **Why the primary source carries the *incoming* track:** `Music`, `GetCurrentMusicClip()` and every
> existing volume tweak point at `_music`. Keeping the new track there means all of them go on describing
> the track you can actually hear, instead of the one fading away.

Fades run on `Time.unscaledDeltaTime`, so `Time.timeScale = 0` does not freeze a transition. The fade
target is re-read every frame, so changing the music volume mid-fade lands where you expect. Outside Play
Mode - EditMode tests, inspector buttons - every transition degrades to a clean cut.

## Random Pitch

```csharp
AM.I.RandomizePitch = true;        // legacy / direct-clip one-shots only
AM.I.SetPitchRange(0.9f, 1.1f);    // the order is normalised for you
```

> **Why the voice pool.** `AudioSource.pitch` applies to the whole source, so setting it before
> `PlayOneShot` also retunes every one-shot still ringing on that source - and overlapping shots are
> exactly the case this feature exists for. Each pitched shot therefore takes a voice from a small
> round-robin pool of its own channel (parented to `_efx` for Sfx, to the UI source for Ui). The voices
> re-copy their channel's routing, mute and volume on **every** shot, not once at creation: `SetEfxVolume`
> writes `_efx.volume`, and a voice that mirrored it once would keep playing at whatever the volume was when
> it happened to be created. With the toggle off nothing is allocated and playback goes straight through the
> channel's source.

## No-code use

`AM` is fully usable without writing anything:

- Configure everything in the inspector, including the pools and their modes.
- [PlayAudio](PlayAudio.md) / [PlayAudioBtn](PlayAudioBtn.md) play a sound by **id**, picked from a
  dropdown of the ids configured on `AM` so a typo is impossible.
- [MusicControl](MusicControl.md) starts a pool, moves to the next track or stops the music.
- `Play(string)`, `PlayMusicPool(string)`, `NextMusicTrack()`, `StopMusic()` and `SetUiVolume(float)` take
  zero or one argument on purpose, so a **UnityEvent** can call them directly.

## Migration to 10.13.11 (channels, placeholders, PlayAt)

Nothing to do; everything sounds as it did.

- The new entry fields load with `Channel = Sfx`, `Spatial Blend = 1`, `Placeholder = off`. `Sfx` is the
  `_efx` source, so `EfxVolume`, `SetEfxVolume`, `AMSettings` and every existing entry behave exactly as before.
- The UI source and the voices are created on first use (also in edit mode, from an inspector button) with
  `DontSave` and are never saved, so opening an existing scene or prefab does not add children or mark it
  dirty. AM destroys them in `OnDestroy` in Play Mode and in builds. In edit mode Unity does not send `OnDestroy` (AM is not
  `[ExecuteAlways]`), so helpers created there by an inspector button stay as `DontSave` children until the
  scene reloads - harmless, and never saved. `_uiVolume`, `_uiMixerGroup` and `_positionalVoices` take their
  defaults (`1`, empty, `8`).
- Changed: a change of the Sfx volume or mute through AM (`SetEfxVolume`, `SetChannelVolume`,
  `SetChannelMute`, `ApplyStartVolumes`) - and through `AMSettings`, which now calls AM instead of writing the
  Efx source - re-levels pitched voices that are still ringing, instead of only the next shot.
  `AMSettings.ToggleAllAudio()` also mutes and restores the UI channel.
- `AudioFocusMute` (it uses `AudioListener.volume`) and the `0..2` entry / clip trims are untouched.

## Migration from before 10.13

Nothing to do. `_sounds`, `_musicClips`, `_randomMusicTracks` and `_useRandomMusic` are still serialized
and are migrated into the new lists on first load, once:

- each legacy `Sound` becomes one entry, in the same order, so `Play(int)` resolves to the same sound. A
  legacy `volume == 0` meant "full" and is migrated as `1`; in the new contract zero means zero.
- migrated sound entries inherit the manager's old global pitch switch, so a project sounds exactly as it
  did rather than picking up the new "pitch on by default".
- each legacy music clip becomes one entry, in order, so `PlayMusic(int)` is unchanged. The random list is
  appended **after** them as a `Shuffle` pool with the id `Random`, so it cannot shift any index.

`EnableRandomMusic()` and `SetRandomMusicTracks(...)` still work and are marked `[Obsolete]` pointing at
pools. Nothing is removed in this release.

Two deliberate behaviour changes:

- `PlayMusic(int)` and `PlayMusicByClip` used to write their volume straight onto the `AudioSource`,
  ignoring `SetMusicVolume`. They now follow the `channel x entry` contract like everything else. With
  the channel at its default `1` the result is identical.
- The music `AudioSource` volume authored in the inspector **is** the music channel now: it is adopted
  once at runtime init, so a source you turned down stays down instead of being overwritten by the first
  `PlayMusic` call.

## See Also
- [MusicControl](MusicControl.md) - no-code music pool control.
- [AMSettings](AMSettings.md) - audio settings and saving.
- [PlayAudioBtn](PlayAudioBtn.md) - component for buttons.
- [Module Root](../README.md)
