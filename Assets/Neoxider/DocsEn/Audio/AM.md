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

## Code Usage

```csharp
// Play sound by index 0 from the _sounds array
AM.I.Play(0);

// Play a specific AudioClip at 0.5 volume
AM.I.Play(myClip, 0.5f);

// Enable random music
AM.I.EnableRandomMusic();
```

## See Also
- [AMSettings](AMSettings.md) - Audio settings and saving.
- [PlayAudioBtn](PlayAudioBtn.md) - Component for buttons.
- [Module Root](../README.md)
