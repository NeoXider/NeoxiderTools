# Realtime toolkit

**What it is:** the game-agnostic building blocks of a server-authoritative realtime game, in `Scripts/Network/Realtime` (namespace `Neo.Network.Realtime`, assembly `Neo.Network`). No game types: positions are floats, frames are bytes, events are Mirror messages. The pure parts (timeline, buffer, quantisation, prediction, fragmentation) are plain C# with no Unity or Mirror dependency and are unit-tested; the Mirror parts compile only with Mirror installed.

**Contents:**

| Page | Types | Needs Mirror |
|------|-------|--------------|
| [SnapshotInterpolation](./SnapshotInterpolation.md) | `SnapshotTimeline`, `SnapshotTimelineSettings`, `SnapshotBuffer<T>`, `NetInterpolation` | no |
| [LocalPredictionModel](./LocalPredictionModel.md) | `LocalPredictionModel`, `LocalPredictionSettings`, `PredictionBounds`, `IPredictionMovementConstraint` | no |
| [NetQuantization](./NetQuantization.md) | `NetQuantization` | no |
| [NetFraming](./NetFraming.md) | `NetFrameFraming`, `NetFragmentation`, `NetFragmentAssembler`, `NetFrameSender`, `NetFrameReceiver` | framing and sender/receiver: yes |
| [NetMessaging](./NetMessaging.md) | `NetClientHandlers`, `NetServerHandlers`, `NetReadyBroadcast`, `NetEventChannel<T>` | yes |

Telemetry ([NeoNetworkTelemetry](../NeoNetworkTelemetry.md)) and the command-line start-up ([NeoNetworkBootstrap](../NeoNetworkBootstrap.md)) are separate pages.

**How to use:** start with the [Realtime IO Guide](../Realtime_IO_Guide.md): it shows how the pieces fit together (handshake, snapshots, interpolation, prediction, events, bootstrap) and which one to add first.

## Design rules

- **Allocation-free hot paths.** Everything called per frame or per message (`SnapshotTimeline.OnFrame/Advance`, `SnapshotBuffer<T>.TryAdd/TrySample`, the prediction step, the fragment assembler, `NetTrafficMeter.Record`) allocates nothing after construction.
- **Total readers.** A reader rejects a wrong version, a truncated body, a count above its ceiling or an over/under-read body with `FormatException`; Mirror turns that into a disconnect instead of a silent desync.
- **Saturate, never wrap.** Quantisers pin out-of-range values to the edge.
- **Reset on session change.** Every stateful type has `Reset()` (or resets itself on a new client session).

## See also
- [Network README](../README.md)
- [Realtime IO Guide](../Realtime_IO_Guide.md)
