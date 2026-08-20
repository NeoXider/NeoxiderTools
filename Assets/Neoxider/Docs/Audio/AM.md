# AM (Audio Manager)

**Purpose:** Central manager for sound effects and music. Singleton (`AM.I`), with separate `AudioSource`
channels for music and effects.

Sounds and music share **one record contract**, so there is one thing to learn rather than two. Music
records are additionally **pools**, and every music change **crossfades** by default.

## The record contract

One entry in either list holds:

| Part | Meaning |
|------|---------|
| **id** | Optional. Play by index as always; give it an id and `AM.I.Play("hit")` works too - and survives reordering the list. |
| **clips** | A **set**, not one clip. A random clip plays each time, never the same one twice in a row. Put every variation of a cue in one entry. |
| **volume** | A **multiplier** of the channel, default `1`. What you hear is `channel volume x entry volume`. |
| **pitch** | Optional detune with a min/max range. Defaults to **on** for sounds, **off** for music. |

> **Volume multiplies.** A music channel at `0.3` playing an entry at `1` comes out at `0.3`. This is what
> keeps the player's own volume slider working: `SetMusicVolume` / `SetEfxVolume` set the *channel*, and
> every entry and every per-call override sits underneath it.

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

The collapsed row of an entry already shows its id, clip count and volume slider, so the common tweak
needs no expanding.

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
AM.I.SetEfxVolume(0.9f);
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
> round-robin pool parented to `_efx`. The voices re-copy `_efx`'s routing, mute and volume on **every**
> shot, not once at creation: `SetEfxVolume` writes `_efx.volume`, and a voice that mirrored it once would
> keep playing at whatever the volume was when it happened to be created. With the toggle off nothing is
> allocated and playback goes straight through `_efx`.

## No-code use

`AM` is fully usable without writing anything:

- Configure everything in the inspector, including the pools and their modes.
- [PlayAudio](PlayAudio.md) / [PlayAudioBtn](PlayAudioBtn.md) play a sound by **id**, picked from a
  dropdown of the ids configured on `AM` so a typo is impossible.
- [MusicControl](MusicControl.md) starts a pool, moves to the next track or stops the music.
- `Play(string)`, `PlayMusicPool(string)`, `NextMusicTrack()` and `StopMusic()` take zero or one argument
  on purpose, so a **UnityEvent** can call them directly.

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
