# PlayAudio

**Purpose:** A utility for simply playing a sound via the `AM.I` manager. It supports playing a sound entry by **id**, a specific `AudioClip`, a random clip from a list, or a sound by its index (Legacy mode).

The sources are tried in that order of precedence: explicit clips win, then `_soundId`, then the legacy index - so a component that only has an index set behaves exactly as it always did.

## Setup

1. Add `Add Component > Neoxider > Audio > PlayAudio` to an object.
2. Assign an `AudioClip` (or several) to the `_clips` array.
3. Call the `AudioPlay()` method (e.g., from a UnityEvent or another script) to play the sound.
4. Alternatively, check `_playOnAwake` to play the sound automatically when the object is enabled.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_soundId` | **Preferred.** Id of a sound entry on `AM`, picked from a dropdown of the ids actually configured. Survives reordering the list, unlike the index. |
| `_clipType` | (Legacy) The sound index from the `_sounds` array in the main `AM`. |
| `_clips` | Array of `AudioClip`s. If only 1 clip is provided, it will always play. |
| `_useRandomClip` | If `true` and `_clips` has more than 1 item, a random sound from the array will be played on each call. |
| `_playOnAwake` | If `true`, plays the sound on start (in `Start()`). |
| `_volume` | Volume for this play, **replacing** the entry's own volume multiplier (`0..2`). **Negative = use the entry's volume**, which is the default for a new component. Still multiplied by the effects channel. |

## See Also
- [PlayAudioBtn](PlayAudioBtn.md) - Version designed for UI buttons and pointer events.
- [AM](AM.md) - Main audio manager.
- [Module Root](../README.md)
