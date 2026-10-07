# Snapshot interpolation

**What it is:** `SnapshotTimeline` (an adaptive jitter-buffer render clock) and `SnapshotBuffer<T>` (a ring of timestamped frames with bracket search) in `Scripts/Network/Realtime/SnapshotTimeline.cs` and `SnapshotBuffer.cs`, namespace `Neo.Network.Realtime`. Pure C#.

**How to use:**
1. For every received snapshot call `buffer.TryAdd(serverTime, frame)`; when it returns `true` call `timeline.OnFrame(serverTime, localNow)`.
2. Once per rendered frame call `timeline.Advance(Time.unscaledDeltaTime)`.
3. Sample `buffer.TrySample(timeline.RenderTimeExact, out from, out to, out alpha)` and blend `from` to `to` by `alpha`.
4. Call `Reset()` on both on disconnect and on a restarted match.

---

## Why a render clock

The newest frame's stamp is not "now": it only changes when a frame arrives, so `renderTime = stamp - delay` never falls between two frames and every body moves in snapshot-rate steps. `SnapshotTimeline` runs on the client's own delta time and is **steered**, not stepped: it speeds up or slows down by at most `MaxSpeedAdjust` (10 %) so that its lag behind the newest frame converges on `BufferSeconds`, which itself follows the measured snapshot interval plus the arrival jitter.

- A rough link (late packets) widens the buffer: smoothness over latency. A clean link narrows it again.
- A long stall (reconnect, a frozen tab) snaps the clock instead of fast-forwarding visibly.
- The authority's own clock rate is measured (`ServerClockRate`), so a hit-stop that slows the authority's `Time.timeScale` does not push the render clock in front of the frames.

## SnapshotTimelineSettings

Plain readonly struct; use `SnapshotTimelineSettings.Default` (12 Hz assumption) or `SnapshotTimelineSettings.ForSendRate(hz)` and tune from there, or construct it with explicit numbers.

| Field | Default | Meaning |
|-------|---------|---------|
| `BufferIntervals` | 1.5 | Render lag in snapshot intervals. |
| `MinBufferSeconds` / `MaxBufferSeconds` | 0.06 / 0.4 | Clamp on the lag. |
| `JitterWeight` | 2 | Mean arrival-jitter deviations added to the lag. |
| `Smoothing` | 0.1 | Exponential-average weight of each new observation. |
| `CatchUpGain` | 0.5 | How hard the clock leans toward its target lag, per second of error. |
| `MaxSpeedAdjust` | 0.1 | The clock runs between 0.9x and 1.1x real time (capped at 0.5). |
| `SnapThresholdSeconds` | 0.6 | An error above this snaps. |
| `InitialIntervalSeconds` | 1/rate | Assumed interval until two frames were seen. |
| `RateWindowSeconds` | 0.5 | Window of the authority clock-rate measurement. |

## SnapshotTimeline members

| Member | Description |
|--------|-------------|
| `void OnFrame(double serverTime, double localTime)` (and a `float` overload) | A frame arrived. Out-of-order stamps are ignored. |
| `void Advance(float deltaSeconds)` | Advances the render clock; call once per rendered frame with unscaled delta time. |
| `float RenderTime`, `double RenderTimeExact` | Authority time to draw at; always at or behind the newest frame. Use the exact one in long sessions. |
| `float ServerNow` | Estimate of the authority's current time. |
| `float BufferSeconds`, `LagSeconds` | Target lag and current lag. |
| `float SendIntervalSeconds` | Measured seconds between frames. |
| `float JitterSeconds` | Mean deviation of arrival spacing: the link-quality number. |
| `float ServerClockRate` | Server seconds per local second. |
| `bool HasTimeline` | True after the first frame. |
| `void Reset()` | Forget everything. |

## SnapshotBuffer&lt;T&gt;

A ring of the last `capacity` (minimum 2) frames. `T` is typically a struct, or a pooled class (see `OnEvicted`). It rejects a stamp that is not newer than the newest stored one, which also drops reordered packets.

| Member | Description |
|--------|-------------|
| `bool TryAdd(double time, T frame)` | Stores a frame; `false` when stale or duplicate. |
| `SnapshotSampleKind TrySample(double renderTime, out T from, out T to, out float alpha)` | Brackets `renderTime`. |
| `bool TryGetNewest(out T frame)` | The newest frame. |
| `T GetFrame(int index)`, `double GetTime(int index)` | 0 = oldest. |
| `int Count`, `int Capacity`, `double OldestTime`, `double NewestTime` | State. |
| `Action<T> OnEvicted` | Called for a frame pushed out by a newer one or dropped by `Clear`, so a pooled payload can be recycled. |
| `void Clear()` | Drops every frame. |

`SnapshotSampleKind`:

| Value | Meaning | `from` / `to` / `alpha` |
|-------|---------|--------------------------|
| `None` | The buffer is empty. | default |
| `BeforeOldest` | The render time is older than every frame (just connected). | oldest, oldest, 0 |
| `Between` | Normal case. | two surrounding frames, 0..1 |
| `AfterNewest` | The clock outran the data (packet loss). Worth counting. | newest, newest, 1 |

There is deliberately no extrapolation: a held frame is always a valid world state, a guessed one is not.

`NetInterpolation` has `Lerp` (clamped), `LerpAngleDegrees` and `LerpAngleRadians` (shortest way around).

## Example

```csharp
private readonly SnapshotTimeline _timeline = new SnapshotTimeline(SnapshotTimelineSettings.ForSendRate(20f));
private readonly SnapshotBuffer<BodyFrame> _buffer = new SnapshotBuffer<BodyFrame>(6);

void OnSnapshot(double serverTime, BodyFrame frame)
{
    if (_buffer.TryAdd(serverTime, frame))
        _timeline.OnFrame(serverTime, Time.realtimeSinceStartupAsDouble);
}

void Update()
{
    _timeline.Advance(Time.unscaledDeltaTime);
    if (_buffer.TrySample(_timeline.RenderTimeExact, out BodyFrame from, out BodyFrame to, out float alpha)
        != SnapshotSampleKind.None)
    {
        transform.position = new Vector3(
            NetInterpolation.Lerp(from.X, to.X, alpha),
            NetInterpolation.Lerp(from.Y, to.Y, alpha), 0f);
    }
}
```

## See also
- [Realtime IO Guide](../Realtime_IO_Guide.md)
- [LocalPredictionModel](./LocalPredictionModel.md)
- [Realtime README](./README.md)
