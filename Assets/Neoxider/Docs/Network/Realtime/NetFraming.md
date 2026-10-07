# Frame framing and fragmentation

**What it is:** how a hand-written byte payload (a world snapshot) is made safe and sendable: `NetFrameFraming` (version byte, body length, bounded counts) and, for datagram transports, `NetFragmentation` / `NetFragmentAssembler` (pure C# splitting and reassembly) plus the Mirror glue `NetFrameSender` / `NetFrameReceiver` / `NetFragmentMessage` (`Scripts/Network/Realtime/NetFrameFraming.cs`, `NetFragmentation.cs`, `NetFrameTransport.cs`, namespace `Neo.Network.Realtime`).

**How to use:**
1. Write the frame with `NetFrameFraming.WriteHeader` ... `PatchLength` into a reusable `NetworkWriter` and read it with `ReadHeader` ... `VerifyBodyLength`.
2. Send it with `new NetFrameSender(stream, Channels.Unreliable).SendToReady(writer.ToArraySegment())`.
3. On the client create `new NetFrameReceiver(handlers, maxFrameBytes)`, `Subscribe(stream, onFrame)` and `handlers.RegisterNow()` from `Awake`.

---

## NetFrameFraming (needs Mirror)

Layout: `byte version, uint bodyLength, body...`. The reader is total: it throws `FormatException` for a wrong version, a declared body longer than what remains, a count above its ceiling and a body that was not consumed exactly; Mirror turns that into a disconnect instead of a silent desync.

| Member | Description |
|--------|-------------|
| `int WriteHeader(NetworkWriter, byte version)` | Writes version and a length placeholder; returns the position to patch. |
| `void PatchLength(NetworkWriter, int lengthFieldAt)` | Back-patches the body length once the body is written. |
| `void ReadHeader(NetworkReader, byte expectedVersion, string what, out int bodyStart, out int bodyEnd)` | Validates the header; `what` names the frame in the error. |
| `void VerifyBodyLength(NetworkReader, int bodyStart, int bodyEnd)` | Throws unless the body was consumed exactly. |
| `int ReadCount(NetworkReader, int max, string what)` | A varuint count, refused above `max`. |
| `int ReadCount32(NetworkReader)` | A varuint standing in for an int field (level, xp); saturates at `int.MaxValue`. |
| `uint NonNegative(int)`, `byte ClampByte(int)` | Narrowing helpers: negatives write as 0, bytes saturate. |

## Fragmentation: why

A datagram transport (KCP / UDP) carries an unreliable message in one packet of about 1.2 KB. A 10-player snapshot is 2-5 KB, so it cannot ride the unreliable channel as one message. With TCP (Telepathy) `Channels.Unreliable` is the same stream and nothing is needed.

## NetFrameSender (server, needs Mirror)

Sends one byte frame as one or more `NetFragmentMessage`s to ready, spawned clients only.

| Member | Description |
|--------|-------------|
| `NetFrameSender(byte stream = 0, int channelId = Channels.Unreliable, int chunkBytes = 0)` | `stream` separates independent frame streams (world, leaderboard). `chunkBytes` 0 derives the fragment payload from the transport's batch threshold minus the message overhead. |
| `int SendToReady(ArraySegment<byte> frame, bool includeLocalHost = true)` | Returns how many connections received it. Nothing is copied or allocated. |
| `bool SendTo(NetworkConnectionToClient conn, ArraySegment<byte> frame)` | One connection, if it can receive. |
| `int ChunkBytes`, `int MaxFrameBytes` | Current fragment payload and the largest frame (255 fragments). |
| `int FramesSent`, `int FragmentsSent`, `int OversizeFramesRejected` | Counters. |

A frame that needs more than 255 fragments is rejected and counted.

## NetFrameReceiver (client, needs Mirror)

| Member | Description |
|--------|-------------|
| `NetFrameReceiver(NetClientHandlers handlers, int maxFrameBytes = 65536)` | Registers its handler with the registry, so it survives reconnects. Resets itself when a client session starts. |
| `void Subscribe(byte stream, FrameHandler handler)` | `handler(ArraySegment<byte> frame)` is called with a complete frame, valid only during the call. |
| `NetFragmentAssembler GetAssembler(byte stream)` | Counters: completed, dropped, stale, duplicate, rejected. |
| `int FramesDelivered`, `int UnknownStreamFragments` | Counters. |
| `void Reset()`, `void Detach()` | Forget in-flight frames / stop following session starts. |

A frame is delivered only when every fragment arrived. An incomplete or superseded frame is dropped, which is exactly what a snapshot stream wants (latest wins).

## NetFragmentation and NetFragmentAssembler (pure C#)

| Member | Description |
|--------|-------------|
| `NetFragmentation.ChunkSizeForThreshold(int)` | Fragment payload for a batch threshold (threshold minus 24 bytes of overhead, at least 256). |
| `NetFragmentation.FragmentCount(int length, int chunk)`, `MaxFrameBytes(int chunk)` | Arithmetic. |
| `NetFragmentation.TrySlice(ArraySegment<byte> frame, int chunk, int index, out ArraySegment<byte> payload)` | A view of fragment `index`. |
| `NetFragmentation.IsNewer(ushort a, ushort b)` | Wrapping 16-bit ordering of frame ids. |
| `NetFragmentAssembler(int maxFrameBytes, int inFlightFrames = 2)` | Preallocated reassembly with bounded memory. |
| `bool TryAdd(ushort frameId, byte index, byte count, int chunkSize, ArraySegment<byte> payload, out ArraySegment<byte> frame)` | `true` when the fragment completed a frame; `frame` views the assembler's buffer until the next call. Fragments may arrive in any order. |
| `Reset()` | Needed after a server restart: a restarted server counts frame ids from 0 again. |

Behavior: duplicates and stale fragments are counted and ignored; a newer frame that completes drops older incomplete ones; malformed fragments (index out of range, wrong size, a frame larger than `maxFrameBytes`, a contradicting count) are rejected.

## See also
- [NetQuantization](./NetQuantization.md)
- [NetMessaging](./NetMessaging.md)
- [Realtime IO Guide](../Realtime_IO_Guide.md)
- [Realtime README](./README.md)
