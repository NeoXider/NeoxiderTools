# PlayAudioBtn

**Purpose:** A utility for playing sounds upon UI (User Interface) interactions. It automatically intercepts clicks, hovers, and selections on buttons or other UI elements.

## Setup

1. Add `Add Component > Neoxider > Audio > PlayAudioBtn` to a UI object (e.g., a `Button`).
2. In the `_triggerMode` field, select the desired event (e.g., `PointerClick` for mouse clicks or touch).
3. Add an `AudioClip` to the `_clips` array.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_idClip` | (Legacy) Sound index in the main `AM`. |
| `_clips` | Array of `AudioClip`s. |
| `_triggerMode` | Which UI event triggers the sound. Options: `PointerClick` (Click), `PointerEnter` (Cursor hover), `Select` (Gamepad selection), etc. `Manual` means it's only triggered via code. |
| `_useRandomClip` | If `_clips` contains multiple sounds, picks a random one each time. |
| `_volume` | Playback volume (from 0 to 1). |

## See Also
- [PlayAudio](PlayAudio.md) - A simpler player for code and `OnAwake` events.
- [Module Root](../README.md)
