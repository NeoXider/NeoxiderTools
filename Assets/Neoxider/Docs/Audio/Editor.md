# Audio editor authoring

The [Audio module](README.md) is the canonical module entry point. `Neo.Audio.Editor` supplies the AM
inspector and the drawers for audio entries and audio ids. It is an Editor-only assembly; game code
uses [AM](AM.md) and the runtime `Neo.Audio` assembly.

## Authoring entries

1. Add **Neoxider > Audio > AM** to a scene object.
2. Drop selected `AudioClip` assets onto the **Sound Entries** or **Music Entries** list to create one
   entry per clip. Each new entry uses the clip name as its id and starts with default volume, pitch,
   channel and music settings. A duplicate name produces an id warning; rename the entry if necessary.
3. Drop clips onto an existing entry's header to append variations to that entry. Use the expanded
   per-clip object fields to replace a particular clip.
4. Expand an entry to edit its clip list, per-clip trim, volume, pitch and placeholder flag. Sound entries
   also expose channel and positional spatial blend; music entries expose pool mode and fade override.

For an empty or single-clip entry, the collapsed header also provides a clip picker. **+** adds an empty
clip slot; **-** removes a clip and its matching trim. Clearing the single clip using the header picker
also clears its trims. Replacing a populated slot keeps that slot's trim.

Trims are multipliers in `0..2`. Missing trims mean `1`; entries saved before trims existed keep their
original level. Adding a new slot discards trims left beyond the previous clip count, so a clip added
after a script shortened `Clips` starts at `1` instead of inheriting a removed clip's level. Existing
clips keep their trims.

All these edits use serialized properties and the inspector's normal apply/Undo workflow. Duplicate ids
and unintentionally empty entries show warnings. Empty placeholders deliberately suppress the empty
entry warning. Entries without ids remain playable by index.

## Id fields and UnityEvents

String fields marked with `[AudioId]` show configured sound ids. `[AudioId(AudioIdKind.Music)]` offers
music pool ids. Ids are case-sensitive; empty ids are omitted and duplicate ids are listed once.

The first **`<none>`** choice stores an empty string. A configured id literally named `<none>` has its own
choice, labelled **`<none> (id)`**, and stores the original id. A missing id is displayed with **(missing)**
and remains unchanged until a configured id or the empty choice is selected. Display suffixes are never
written into the id.

The drawer looks for a loaded scene AM, including inactive objects, and caches both successful and empty
lookups for one second. If no manager or no ids of the requested kind are found, it falls back to a text
field with a hint. A newly added AM may therefore take one second to appear. This lookup is global;
when editing multiple loaded scenes or prefab assets, verify that the ids belong to the intended
runtime manager.

For a scene workflow, assign ids on AM, then select them on [PlayAudio](PlayAudio.md),
[PlayAudioBtn](PlayAudioBtn.md) or [MusicControl](MusicControl.md), and connect their playback methods
to UnityEvents. `PlayAudio.AudioPlay()`, `PlayAudioBtn.AudioPlay()` and `MusicControl.PlayPool()` resolve
playback through the global `AM.I`. Selecting a dropdown id authors the string; it does not select or
bind a manager for playback. These methods use the same runtime APIs available to C#; game code needs
only the runtime assembly reference.

```csharp
using Neo.Audio;
using UnityEngine;

public sealed class EncounterAudio : MonoBehaviour
{
    [SerializeField] private AM _manager;
    [SerializeField, AudioId] private string _hitId;
    [SerializeField, AudioId(AudioIdKind.Music)] private string _musicId;

    public void PlayHit()
    {
        _manager.Play(_hitId);
    }

    public void BeginEncounter()
    {
        _manager.PlayMusicPool(_musicId);
    }
}
```

Assign `_manager` explicitly in the scene for this example's playback calls. Its `[AudioId]` fields
still use the global editor lookup described above; the drawer does not follow `_manager`. With
multiple managers, ensure the selected ids exist on the manager used for playback.

Plain C# entry authoring is also supported through
`SoundEntry` / `MusicEntry`, `AM.SetSoundEntries` / `SetMusicEntries`, and `AudioEntry.SetClipVolume`.
These APIs do not depend on the inspector. When replacing a clip array in code, its trims remain keyed
by index; the inspector's new-slot cleanup applies when subsequently adding clips through the drawer.
Existing serialized fields, assigned-slot trims and stored id strings retain their formats in 11.0;
this editor patch requires no scene, prefab or asset migration.

## Runtime readout

In Play Mode, the AM inspector shows the current music pool, track and channel levels. **Next Track**
calls `AM.NextMusicTrack()` and is enabled when the current pool has multiple clip slots. This readout
repaints while the game runs; it does not start playback in Edit Mode.

See [AM](AM.md) for playback, channel volume, pool transitions and per-call overrides.
