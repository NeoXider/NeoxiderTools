# RandomMusicController

**Purpose:** A standalone C# helper that plays random tracks on a supplied `AudioSource`. It is not a
`MonoBehaviour` and cannot be added through the component menu. For scene/UnityEvent authoring, use
[AM music pools](AM.md#music-pools) and [MusicControl](MusicControl.md).

## Runtime ownership

The caller supplies the source and owns the controller's lifetime:

```csharp
using Neo.Audio;
using UnityEngine;

public sealed class LocalMusic : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] tracks;
    private readonly RandomMusicController controller = new RandomMusicController();

    private void OnEnable()
    {
        controller.Initialize(source, tracks);
        controller.Start();
    }

    private void OnDisable() => controller.Stop();
}
```

| API | Behaviour |
|-----|-----------|
| `Initialize(AudioSource, AudioClip[])` | Stops an existing session before replacing its source and tracks. Null source rejects the request; null track array becomes empty. |
| `Start()` | Starts random playback; several track slots avoid an immediate index repeat. An empty list does not start. |
| `Stop()` | Cancels the loop, releases its cancellation source and stops active playback. Repeated calls do not repeat `OnStopped`. |
| `Pause()` / `Resume()` | Pause/unpause this controller's source without advancing its track timer. |
| `CurrentTrack` | The source's assigned clip, including the last clip after stopping. |
| `IsPlaying` / `IsPaused` | Session state and controller-local pause state; listener pause does not change `IsPaused`. |
| `OnTrackChanged` / `OnStopped` | C# notifications for track assignment and the end of an active session. |

The timer uses unscaled time and the source's absolute pitch. `AudioListener.pause` holds it unless the
source has `ignoreListenerPause` enabled. Zero pitch holds the track; changing `Time.timeScale` does not
change audio timing.

A selected null clip or a destroyed source ends the session, clears playing/paused state and raises
`OnStopped` once. Cancellation from an older session cannot stop its replacement. Call `Stop()` from
your owner's shutdown/disable path rather than leaving a detached asynchronous loop alive.

`Initialize` owns a bounded rebind transition: `Start()` or another `Initialize(...)` called from its
`OnStopped` notification is ignored. The accepted initialization finishes stopped on its requested
source; call `Start()` after it returns to play there. Even if an `OnStopped` observer throws, the
old source is stopped and the accepted new binding is installed before the exception propagates.
Outside rebinding, a newer start/stop request made by an observer supersedes the older request;
captured source/track generations prevent an old loop from adopting the replacement's fields.

## See Also

- [AM](AM.md)
- [MusicControl](MusicControl.md)
- [Audio module](README.md)
