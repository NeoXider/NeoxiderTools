# NeoNetworkTelemetry

**What it is:** an optional MonoBehaviour (`Scripts/Network/Telemetry/NeoNetworkTelemetry.cs`, namespace `Neo.Network`) that measures a Mirror session: round-trip time and jitter, bytes and messages per second in and out, per message kind, with a small on-screen overlay. The counting is done by the pure `NetTrafficMeter` (`NetTrafficMeter.cs`), and `NetTelemetry` gives static read access to Mirror's RTT. Without Mirror the component is an empty stub.

**How to use:**
1. Add `NeoNetworkTelemetry` to any object (typically the `NeoNetworkManager` object).
2. Tick **Show Overlay** (or call `ToggleOverlay()` from a debug button) to see the readout.
3. Read the numbers from code: `telemetry.RttMs`, `BytesInPerSecond`, `BytesOutPerSecond`, `Incoming` / `Outgoing`.
4. Optional: `telemetry.SetTimeline(timeline)` adds the snapshot jitter measured by a [`SnapshotTimeline`](./Realtime/SnapshotInterpolation.md).

---

## What it shows

```text
ping 34 ms (+-5) | worst client 120 ms | snap jitter 8 ms | in 3.1 KB/s | out 12.4 KB/s
in NetFragmentMessage  3.0 KB/s  20 msg/s
out NetFragmentMessage  11.9 KB/s  60 msg/s
out SessionProbeMessage  0.4 KB/s  2 msg/s
```

- **ping / jitter:** Mirror's smoothed client RTT (`NetworkTime.rtt`) and its deviation (`NetworkTime.rttVariance`). Free: read every half second.
- **worst client:** on a server, the highest `NetworkConnectionToClient.rtt` among remote clients.
- **in / out:** from Mirror's `NetworkDiagnostics.InMessageEvent` / `OutMessageEvent`. An outgoing message sent to N connections counts N times; sizes exclude transport headers.

## Fields

| Field | Description |
|-------|-------------|
| **Capture Traffic** | Subscribe to Mirror's diagnostics events. Mirror boxes every message while anyone is subscribed, so this is a debugging cost: leave it off (or the component disabled) in a shipped build unless you want the numbers. RTT does not need it. |
| **Window Seconds** | Averaging window of the per-second rates (default 1 s). |
| **Show Overlay**, **Overlay Position** | The on-screen readout. |

## Members

| Member | Description |
|--------|-------------|
| `float RttMs`, `RttJitterMs`, `WorstClientRttMs` | Link numbers, refreshed twice a second. |
| `float BytesInPerSecond`, `BytesOutPerSecond` | Totals over the last window. |
| `NetTrafficMeter Incoming`, `Outgoing` | Per-kind meters. |
| `string BuildSummary()` | The first line of the overlay. |
| `bool CaptureTraffic`, `bool ShowOverlay`, `void ToggleOverlay()` | Runtime switches. |
| `void SetTimeline(SnapshotTimeline timeline)` | Show snapshot jitter. |
| `NetTelemetry.RttMilliseconds`, `RttJitterMilliseconds`, `WorstClientRttMilliseconds` | Static reads, no component needed. |

## NetTrafficMeter

Pure C# counter with an injectable clock; usable without the component, for example to measure the size of your own snapshot frames.

| Member | Description |
|--------|-------------|
| `Record(Type kind, int bytes, int messages = 1)` | Records traffic under a kind. One dictionary lookup, no allocation after the kind was seen. |
| `Tick(double nowSeconds)` | Rolls the window; call every frame. |
| `float BytesPerSecond`, `MessagesPerSecond`, `long TotalBytes`, `TotalMessages` | Totals. |
| `bool TryGetKind(Type kind, out KindStat stat)` | Rates and totals of one kind. |
| `int GetTopKinds(KindStat[] buffer)` | The busiest kinds, sorted, no allocation. |
| `Reset()` | Forget everything. |

## See also
- [Realtime IO Guide](./Realtime_IO_Guide.md)
- [Snapshot interpolation](./Realtime/SnapshotInterpolation.md)
- [Network README](./README.md)
