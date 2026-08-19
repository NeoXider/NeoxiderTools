# AM (Audio Manager)

**Purpose:** Central manager for sound effects and music. Implements the Singleton pattern (`AM.I`). Contains separate channels (`AudioSource`) for music and effects. Can play sounds by index, by passing an `AudioClip`, and supports a random background music mode (without consecutive repeats).

## Setup

1. Add the component `Add Component > Neoxider > Audio > AM` to a global scene object.
2. Fill the `_sounds` and `_musicClips` arrays with frequently used sounds.
3. If random background music is used, enable `_useRandomMusic` and fill `_randomMusicTracks`.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_efx` | Reference to `AudioSource` for short sound effects. Created automatically if null. |
| `_music` | Reference to `AudioSource` for music. |
| `_musicClips` | Array of music tracks (for playback by index). |
| `_sounds` | Array of sounds (`Sound` class contains `AudioClip` and base volume). |
| `_useRandomMusic` | If `true`, random music from `_randomMusicTracks` starts on awake. |
| `_randomMusicTracks` | Array of tracks for random background music mode. |
| `_randomizePitch` | If `true`, every one-shot effect plays at a randomised pitch. |
| `_pitchMin` / `_pitchMax` | Pitch range used when `_randomizePitch` is on. `1` = the clip's own pitch. Default `0.94`-`1.06`. |
| `_pitchVoices` | Size of the AudioSource pool used for pitched one-shots. Default `8`. |

## Code Usage

```csharp
// Play sound by index 0 from the _sounds array
AM.I.Play(0);

// Play a specific AudioClip (effect) at 0.5 volume
AM.I.Play(myClip, 0.5f);

// Play a specific AudioClip (effect) at default volume (1)
AM.I.Play(myClip);

// Play music from a specific AudioClip at 0.7 volume
AM.I.PlayMusicByClip(myMusicClip, 0.7f);

// Play music from a specific AudioClip at default volume (1)
AM.I.PlayMusicByClip(myMusicClip);

// Enable random music
AM.I.EnableRandomMusic();
```

> The `Play(AudioClip)` and `PlayMusicByClip(AudioClip)` overloads play the passed clip directly,
> without needing to add it to the `_sounds` / `_musicClips` arrays first.

## Random Pitch

A cue that fires constantly - a hit, a button, a coin - gives itself away as one sample on repeat.
Turn on `_randomizePitch` and each one-shot is detuned slightly.

```csharp
AM.I.RandomizePitch = true;
AM.I.SetPitchRange(0.9f, 1.1f);   // min, max; the order is normalised for you
```

Music is never pitched - only one-shot effects.

> **Why the pool.** `AudioSource.pitch` applies to the whole source, so setting it before
> `PlayOneShot` also retunes every one-shot still ringing on that source - and overlapping shots are
> exactly the case this feature exists for. Each pitched shot therefore takes a voice from a small
> round-robin pool parented to `_efx`; the voices copy `_efx`'s mixer group, spatial blend and
> volume. With `_randomizePitch` off nothing is allocated and playback goes straight through `_efx`.

## See Also
- [AMSettings](AMSettings.md) - Audio settings and saving.
- [PlayAudioBtn](PlayAudioBtn.md) - Component for buttons.
- [Module Root](../README.md)
