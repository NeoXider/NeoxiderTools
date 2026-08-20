# MusicControl

**Purpose:** Drives [AM](AM.md)'s music from the inspector, with no code. Start a music pool by id when the
object is enabled or from a UnityEvent, move to another track of the current pool, or stop the music.

Every public method takes zero or one argument, so all of them appear in a UnityEvent dropdown - a button,
a trigger volume or a state machine can drive the soundtrack directly.

## Setup

1. Configure the music pools on `AM` first: one entry per soundtrack, each with an **id** (`menu`,
   `gameplay`, `boss`) and its clips.
2. Add `Add Component > Neoxider > Audio > MusicControl` to whatever owns that music - the screen root, the
   boss arena, the wave controller.
3. Pick the **Pool Id** from the dropdown. It lists the ids actually configured on `AM`, so a typo is not
   possible.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_poolId` | Music pool to control, chosen from the ids configured on `AM`. |
| `_playOnEnable` | Start this pool automatically when the object is enabled. |
| `_transition` | `Default` uses `AM`'s crossfade, `Fade` uses the length below, `Instant` cuts. |
| `_fadeSeconds` | Crossfade length used when `_transition` is `Fade`. |
| `_volumeOverride` | Play the pool at this volume instead of its own. Negative keeps the pool's setting. Still multiplied by the music channel, so the player's volume slider keeps working. |

## Methods (UnityEvent-ready)

| Method | Effect |
|--------|--------|
| `PlayPool()` | Starts the configured pool. Safe to call repeatedly - it will not restart the track. |
| `PlayPool(string id)` | Starts any pool by id, using this component's transition and volume settings. |
| `NextTrack()` | Moves to another random track of the pool that is currently playing. |
| `StopMusic()` | Stops the music, using this component's transition setting. |

## Typical Set-Up

Three pools on `AM` - `menu`, `gameplay`, `boss` - and three `MusicControl` components with
`_playOnEnable` on, one per screen. Nothing else is needed: enabling a screen crossfades its soundtrack in.

For a track change inside a pool - a new wave, a boss phase - wire `NextTrack()` to whatever raises that
event, and leave the pool in `Loop` mode so it does not also change on its own.

## See Also
- [AM](AM.md) - the manager, its record contract and the code API.
- [PlayAudioBtn](PlayAudioBtn.md) - sound effects on button clicks.
- [Module Root](../README.md)
