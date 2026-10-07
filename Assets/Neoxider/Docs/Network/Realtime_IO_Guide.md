# Realtime IO Guide

**What it is:** the architecture of a server-authoritative realtime ".io" game (a shared arena, dozens of moving bodies, 10-100 players) built from the `Neo.Network` pieces: handshake, world snapshots, interpolation, client prediction, reliable events, telemetry, and a command-line bootstrap for the dedicated server. The pieces live in `Scripts/Network/Core`, `Bootstrap`, `Realtime` (namespace `Neo.Network.Realtime`, no game types) and `Telemetry`.

**How to use:** read the pipeline below, then add the pieces one at a time. Each step stands alone and has its own page:

| Step | Piece | Page |
|------|-------|------|
| 1. Start the process | `NeoNetworkBootstrap`, `NeoStartupIntent` | [NeoNetworkBootstrap](./NeoNetworkBootstrap.md) |
| 2. Handshake and roster | `NeoNetworkManager` events | [NeoNetworkManager](./NeoNetworkManager.md) |
| 3. Send the world | `NetFrameFraming`, `NetQuantization`, `NetFrameSender` / `NetFrameReceiver` | [NetFraming](./Realtime/NetFraming.md), [NetQuantization](./Realtime/NetQuantization.md) |
| 4. Draw it smoothly | `SnapshotTimeline`, `SnapshotBuffer<T>` | [SnapshotInterpolation](./Realtime/SnapshotInterpolation.md) |
| 5. Make your own body instant | `LocalPredictionModel` | [LocalPredictionModel](./Realtime/LocalPredictionModel.md) |
| 6. One-off events | `NetEventChannel<T>`, `NetClientHandlers`, `NetReadyBroadcast` | [NetMessaging](./Realtime/NetMessaging.md) |
| 7. Watch the link | `NeoNetworkTelemetry` | [NeoNetworkTelemetry](./NeoNetworkTelemetry.md) |

---

## The pipeline

```text
 command line ──> NeoNetworkBootstrap ──> NeoNetworkManager.StartAsServer / StartAsHost / StartAsClient
                                                   │
 client connects ─ Ready ─ AddPlayer ──────────────┤  (handshake guaranteed by the manager)
                                                   ▼
                            ServerPlayerReady(conn)  ── welcome, roster, first snapshot
                                                   │
   AUTHORITY (server or host)                      │                CLIENT
   ───────────────────────────                     │      ─────────────────────────────
   simulate at a fixed tick                        │
   collect state ──> NetFrameFraming + NetQuantization
   NetFrameSender (fragments if > MTU) ───── frames (unreliable) ───> NetFrameReceiver (reassembles)
                                                   │                       │ OnFrame(serverTime, arrival)
                                                   │                  SnapshotBuffer<T> + SnapshotTimeline
                                                   │                       │ RenderTime
                                                   │                  interpolated remote bodies
   apply input at tick N  <──── tick-stamped input ───────────────  LocalPredictionModel (own body)
   write AckTick = N into the snapshot ──────────────────────────>  Reconcile(server pos, AckTick)
                                                   │
   NetEventChannel.Publish(kill) ───── reliable ─> remote clients ──> Received (kill feed, stings)
```

Rules the pipeline is built on:

1. **The server is authoritative.** Clients send input and nothing else that matters. Prediction only chooses where to *draw* the local body; the authority never reads a position from a client.
2. **Nothing is sent to a connection that cannot receive it** (`NeoNetworkState.IsConnectionSpawned`), so Mirror never disconnects a client for an unknown message id.
3. **Every message handler is registered before the first message can arrive and survives a reconnect** (`NetClientHandlers` / `NetServerHandlers`).
4. **A frame is a flat byte payload** (versioned, length-checked, bounded counts, quantised). Mirror's weaver cannot serialise nested arrays of structs, and a flat payload is the only way to get allocation-free, small snapshots.
5. **The host is a peer too.** Its loopback connection is a normal connection: skip it for events the authority already acted on locally (`includeLocalHost: false`), include it for state the host's own client renders from snapshots.

---

## 1. Process start

Put `NeoNetworkBootstrap` next to the `NeoNetworkManager`. A dedicated server is then `Game.exe -batchmode -nographics -server -port 7777`, a client is `Game.exe -client -address host -port 7777`. The bootstrap suppresses rendering and audio on the server, caps its frame rate, keeps every networked peer running in the background and logs a status line. See [NeoNetworkBootstrap](./NeoNetworkBootstrap.md).

## 2. Handshake and roster

Do not poll `NetworkServer.connections`. Subscribe to the manager:

```csharp
manager.ServerPlayerReady += conn =>          // ready AND owns a player
{
    int id = _roster.Admit(conn);
    NetReadyBroadcast.SendTo(conn, new Welcome { YourId = id, Tick = _tick }, Channels.Reliable);
};
manager.ServerClientDisconnected += conn => _roster.Release(conn);   // conn.identity still readable
manager.OnLocalPlayerSpawnedEvent.AddListener(() => _camera.Follow(NetworkClient.localPlayer));
```

`ServerPlayerReady` is the moment to welcome a client and start sending it snapshots.

## 3. Send the world

The authority writes one frame per snapshot tick into a reusable `NetworkWriter` and hands it to a `NetFrameSender`; the client side is a `NetFrameReceiver`.

```csharp
using Mirror;
using Neo.Network;
using Neo.Network.Realtime;
using UnityEngine;

public sealed class WorldBroadcaster : MonoBehaviour
{
    private const byte FrameVersion = 1;
    private const byte WorldStream = 1;

    [SerializeField] private float _snapshotsPerSecond = 20f;

    private readonly NetworkWriter _writer = new NetworkWriter();
    private readonly NetFrameSender _sender = new NetFrameSender(WorldStream, Channels.Unreliable);
    private float _nextAt;
    private uint _tick;

    private void Update()
    {
        if (!NetworkServer.active || Time.unscaledTime < _nextAt) return;
        _nextAt = Time.unscaledTime + 1f / _snapshotsPerSecond;

        _writer.Reset();
        int lengthAt = NetFrameFraming.WriteHeader(_writer, FrameVersion);
        _writer.WriteDouble(NetworkTime.time);                       // the authority's stamp
        _writer.WriteUInt(++_tick);
        _writer.WriteVarUInt(NetFrameFraming.NonNegative(_bodies.Count));
        for (int i = 0; i < _bodies.Count; i++)
        {
            _writer.WriteUShort(_bodies[i].Id);
            _writer.WriteShort(NetQuantization.PackPosition(_bodies[i].X));   // 1/32 unit, saturating
            _writer.WriteShort(NetQuantization.PackPosition(_bodies[i].Y));
            _writer.WriteUShort(NetQuantization.PackAngle(_bodies[i].Heading));
        }
        NetFrameFraming.PatchLength(_writer, lengthAt);

        _sender.SendToReady(_writer.ToArraySegment());               // fragments when it exceeds a datagram
    }
}
```

- **Channel.** Snapshots are state, not events: a lost one is superseded by the next, so send them on `Channels.Unreliable`. With TCP transports (Telepathy) the unreliable channel is the same stream; with KCP / UDP a frame bigger than one packet (about 1.2 KB) would be dropped, which is what `NetFrameSender` solves by splitting the frame into fragments and the receiver by dropping incomplete frames.
- **Size.** Quantise: positions in 1/32 unit as `short`, angles as `ushort` or `byte`, timers as `ushort`. A frame of 10 players + 120 mobs + 300 pickups fits in 2-5 KB.
- **Safety.** `ReadHeader` / `ReadCount` / `VerifyBodyLength` throw `FormatException` on a wrong version, a truncated body or a count above its ceiling; Mirror turns that into a disconnect instead of a silent desync.

The client:

```csharp
public sealed class WorldReceiver : MonoBehaviour
{
    private readonly NetClientHandlers _handlers = new NetClientHandlers();
    private readonly SnapshotTimeline _timeline = new SnapshotTimeline(SnapshotTimelineSettings.ForSendRate(20f));
    private readonly SnapshotBuffer<WorldFrame> _buffer = new SnapshotBuffer<WorldFrame>(6);
    private NetFrameReceiver _frames;

    private void Awake()                       // before any transport can deliver
    {
        _frames = new NetFrameReceiver(_handlers, maxFrameBytes: 32 * 1024);
        _frames.Subscribe(1, OnFrame);
        _handlers.RegisterNow();
    }

    private void OnDestroy()
    {
        _frames.Detach();
        _handlers.Dispose();
    }

    private void OnFrame(ArraySegment<byte> payload)
    {
        using (NetworkReaderPooled reader = NetworkReaderPool.Get(payload))
        {
            NetFrameFraming.ReadHeader(reader, 1, "WorldFrame", out int bodyStart, out int bodyEnd);
            double serverTime = reader.ReadDouble();
            WorldFrame frame = WorldFrame.Read(reader);            // your own flat read
            NetFrameFraming.VerifyBodyLength(reader, bodyStart, bodyEnd);

            if (_buffer.TryAdd(serverTime, frame))                 // false: stale or reordered
            {
                _timeline.OnFrame(serverTime, Time.realtimeSinceStartupAsDouble);
            }
        }
    }
}
```

## 4. Draw it smoothly

Reading the stamp of the newest frame as "now" is a staircase: it only moves when a frame arrives, so bodies advance in snapshot-rate steps. `SnapshotTimeline` is a free-running render clock in the authority's time that holds a lag of about 1.5 snapshot intervals plus the measured jitter behind the newest frame; `SnapshotBuffer<T>` finds the two frames around it.

```csharp
private void Update()
{
    _timeline.Advance(Time.unscaledDeltaTime);
    SnapshotSampleKind kind = _buffer.TrySample(_timeline.RenderTimeExact,
        out WorldFrame from, out WorldFrame to, out float alpha);
    if (kind == SnapshotSampleKind.None) return;
    DrawBodies(from, to, alpha);            // NetInterpolation.Lerp / LerpAngleDegrees per body
}
```

`SnapshotSampleKind.AfterNewest` means the buffer ran dry (packet loss): worth counting. The clock widens its lag on a rough link and narrows it again on a clean one, snaps instead of fast-forwarding after a stall, and measures the authority's clock rate so a hit-stop on the authority (slowed `Time.timeScale`) does not push it ahead of the frames. `timeline.JitterSeconds` is the best single "how rough is this link" number.

## 5. Make your own body instant

Interpolation delays everything by about 100 ms, which is unplayable for your own body. `LocalPredictionModel` integrates your input immediately, remembers it under its tick, and when a snapshot brings the authoritative position and the **last input tick the authority applied** (`AckTick`) it replays the unacknowledged inputs on top and absorbs the difference into a decaying render offset.

```csharp
// both peers integrate with the same fixed tick
private void FixedUpdate()
{
    Vector2 move = ReadInput();
    _prediction.Step(++_inputTick, move.x, move.y, _speedFromServer);
    NetworkClient.Send(new MoveInput { Tick = _inputTick, X = move.x, Y = move.y });   // your own message
}

private void OnLocalSnapshot(float serverX, float serverY, uint ackTick, float speed)
{
    _speedFromServer = speed;
    PredictionReport report = _prediction.Reconcile(serverX, serverY, ackTick, speed);
    // report.Outcome: Initialised, Corrected (smoothed), Snapped (teleport, dash, respawn)
}

private void LateUpdate()
{
    _prediction.Relax(Time.deltaTime);
    float alpha = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
    transform.position = new Vector3(_prediction.RenderXAt(alpha), _prediction.RenderYAt(alpha), 0f);
}
```

Authority side: apply each client's input in tick order and write the last applied tick into that player's entry of the snapshot. Do not send input through `NeoNetworkComponent` commands: they are rate limited to one per 0.05 s per component, which drops every third sample of a 30 Hz stream; use `NetworkClient.Send` / `NetworkServer.RegisterHandler` (via `NetServerHandlers`) with your own message and your own flood guard.

## 6. Events

State travels in snapshots, one-off things (a kill, a level-up sting, a spawn warning) travel as events:

```csharp
public struct KillEvent : NetworkMessage { public int Killer; public int Victim; }

private readonly NetClientHandlers _handlers = new NetClientHandlers();
private NetEventChannel<KillEvent> _kills;

private void Awake()
{
    _kills = new NetEventChannel<KillEvent>(_handlers);
    _kills.Received += e => _killFeed.Add(e.Killer, e.Victim);     // remote clients only
    _handlers.RegisterNow();
}

// authority
private void OnKill(int killer, int victim)
{
    _killFeed.Add(killer, victim);                                 // act locally first (host included)
    _kills.Publish(new KillEvent { Killer = killer, Victim = victim });   // reliable, ready clients, never the host
}
```

## 7. Telemetry

Add `NeoNetworkTelemetry` to see ping, jitter, bytes per second in and out, and the busiest message kinds (`telemetry.ShowOverlay = true`); `telemetry.SetTimeline(_timeline)` adds the snapshot jitter. See [NeoNetworkTelemetry](./NeoNetworkTelemetry.md).

---

## Choosing numbers

| Quantity | Starting point | Why |
|----------|----------------|-----|
| Simulation tick | 50 Hz (0.02 s) | Same step on both peers, so replay matches. |
| Snapshot rate | 20 Hz | 50 ms of motion per frame; 12 Hz is visibly steppy for fast bodies, 30 Hz doubles bandwidth. |
| Input rate | 30 Hz or the tick rate | Tick-stamped, so the rate can differ from the tick. |
| Render lag | `SnapshotTimelineSettings.Default` (1.5 intervals + 2 x jitter, 60-400 ms) | Adapts; do not hard-code a delay. |
| Position quantisation | 1/32 unit, +-1023 | About 3 cm; `NetQuantization.FitsPosition` flags a world that outgrew it. |
| Frame budget | 2-5 KB for 10 players, 120 mobs, 300 pickups | 20 frames per second of that is 40-100 KB/s per client. |

## Transports

| Transport | Use it for | Notes |
|-----------|------------|-------|
| **Telepathy** (TCP) | Desktop, simplest | `Channels.Unreliable` is the same ordered stream; a lost packet stalls everything behind it. |
| **KCP** (UDP) | Desktop, best for realtime | True unreliable channel; frames above one datagram need `NetFrameSender` fragments. |
| **SimpleWeb** (WebSocket) | **WebGL** | Browsers cannot open raw sockets. Reliable only in practice; use `wss://` behind HTTPS. |
| **Multiplex** | A server that accepts desktop **and** browser clients | Wraps several transports behind one server. |

For WebGL assign a `SimpleWebTransport` to the bootstrap's **WebGL Transport** and let the page URL carry `?client&address=...&port=...` (see [NeoNetworkBootstrap](./NeoNetworkBootstrap.md#webgl)).

## Pitfalls checklist

- **Sending before the peer can receive.** Broadcast with `NetReadyBroadcast` / `NetFrameSender` / `NetEventChannel`, never `NetworkServer.SendToReady` (it only checks `isReady`).
- **Handlers vanish on shutdown.** Mirror clears every handler when a session ends. Keep them in `NetClientHandlers` / `NetServerHandlers`.
- **Scene objects disabled on clients.** Mirror disables scene `NetworkIdentity` objects; `NeoNetworkManager` wakes them when a session starts so their `Awake` handlers exist before the first message.
- **Host doubles.** A host's loopback connection receives what it also produced locally: pass `includeLocalHost: false` for events, `true` for snapshots its own client renders.
- **Hit-stop on the authority.** Slowing `Time.timeScale` on a host slows the simulation for every client; `SnapshotTimeline` copes with the clock rate, but keep cosmetic time manipulation off authority peers.
- **Stale reads after a restart.** Call `Reset()` on timelines, buffers and prediction on disconnect; `NetFrameReceiver` resets itself when a client session starts.
- **Float stamps.** Stamp frames with a `double` (`NetworkTime.time`), not a `float`: a float clock has 4 ms resolution after ten hours.

## See also
- [Network README](./README.md)
- [Multiplayer Guide](./Multiplayer_Guide.md)
- [Realtime toolkit index](./Realtime/README.md)
