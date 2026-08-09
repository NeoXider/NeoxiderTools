# RandomMusicController

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Runtime API and Inspector testing

| Method | Description |
|--------|-------------|
| `Start()` | Starts random playback from the configured track list. |
| `Stop()` | Stops active playback. |
| `Pause()` | Pauses active playback. |
| `Resume()` | Resumes paused playback. |

The Inspector exposes **Play Random Music**, **Stop**, **Pause**, and **Resume** buttons in Play Mode. They
call the same public runtime API and are disabled outside Play Mode.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `CurrentTrack` | Current Track. |
| `IsPaused` | Is Paused. |
| `IsPlaying` | Is Playing. |
| `RandomMusicController` | Random Music Controller. |

## See Also

- [Module Root](../README.md)
