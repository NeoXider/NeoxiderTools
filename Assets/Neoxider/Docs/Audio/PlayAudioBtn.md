# PlayAudioBtn

**Purpose:** A utility for playing sounds upon UI (User Interface) interactions. It automatically intercepts clicks, hovers, and selections on buttons or other UI elements.

## Setup

1. Add `Add Component > Neoxider > Audio > PlayAudioBtn` to a UI object (e.g., a `Button`).
2. In the `_triggerMode` field, select the desired event (e.g., `PointerClick` for mouse clicks or touch).
3. Pick a sound with `_soundId` (a dropdown of the ids configured on `AM`), or add an `AudioClip` to the `_clips` array.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_soundId` | **Preferred.** Id of a sound entry on `AM`, picked from a dropdown of the ids actually configured. Survives reordering the list, unlike the index. |
| `_idClip` | (Legacy) Sound index in the main `AM`. This path always uses the record's own volume and ignores `_volume`. |
| `_clips` | Array of `AudioClip`s. |
| `_triggerMode` | Which UI event triggers the sound. Options: `PointerClick` (Click), `PointerEnter` (Cursor hover), `Select` (Gamepad selection), etc. `Manual` means it's only triggered via code. |
| `_useRandomClip` | If `_clips` contains multiple sounds, picks a random one each time. |
| `_volume` | Volume for this play, **replacing** the entry's own volume multiplier (`0..2`). **Negative = use the entry's volume**, which is the default for a new component. Still multiplied by the effects channel. |

## See Also
- [PlayAudio](PlayAudio.md) - A simpler player for code and `OnAwake` events.
- [Module Root](../README.md)
