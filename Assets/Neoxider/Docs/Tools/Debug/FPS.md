# FPS

Displays a rolling average of per-frame FPS in an assigned `TMP_Text` label. Sampling and display scheduling use unscaled time, so the counter continues while `Time.timeScale` is zero. Namespace: `Neo.Tools`; assembly: `Neo.Tools.Debug`.

## Setup

1. Add `FPS` using **Add Component → Neoxider → Tools → FPS**.
2. Assign **Text** before entering Play Mode. A missing reference reports through `NeoDiagnostics` and disables the component in `Awake`.
3. Set the sample count, refresh interval, thresholds and display format in the Inspector.

The first samples are averaged over the filled part of the buffer. The component does not change the project's frame-rate or VSync settings by default.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `text` | Assigned TMP output; no automatic component search. |
| `sampleSize` | Number of instantaneous FPS samples in the rolling average; minimum 1. |
| `updateInterval` | Seconds between label updates; minimum 0.05. Sampling still runs every frame. |
| `goodFpsThreshold` | Green at or above this FPS. |
| `warningFpsThreshold` | Yellow at or above this FPS; lower values are red. |
| `showDecimals` | Display one decimal place instead of a rounded integer. |
| `showSuffix` | Append ` FPS` to the number. |
| `unlockFramerateOnAwake` | Opt in to setting `Application.targetFrameRate = -1` and `QualitySettings.vSyncCount = 0` in `Awake`. |

## Public C# API and UnityEvents

Given an assigned `FPS counter`:

```csharp
float recentFps = counter.CurrentFps;
counter.SetTargetFramerate(60);
counter.SetVSync(false);
```

`CurrentFps` returns zero before a valid sample and then the average over filled samples. It averages reciprocal frame times; it is not total frame count divided by elapsed time. Frames with unscaled delta below `0.00001` seconds are skipped.

`SetTargetFramerate(int)` writes `Application.targetFrameRate`; `-1` requests Unity's platform-default behavior and does not itself disable VSync. `SetVSync(bool)` writes a VSync count of 1 or 0. Both methods are UnityEvent-compatible and affect the entire application. Disabling or destroying the counter does not restore previous global settings; their effect depends on the target platform.

For scene/NoCode control, bind an integer UnityEvent to `SetTargetFramerate` or a bool UnityEvent to `SetVSync`. Display-only use needs neither binding nor a frame-rate override. `CurrentFps` updates with sampling; the label refreshes at `updateInterval` and can show an earlier sampled value between refreshes.

## See Also

- [Tools / Debug](./README.md)
