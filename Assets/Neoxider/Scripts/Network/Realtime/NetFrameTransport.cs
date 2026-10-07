#if MIRROR
using System;
using System.Collections.Generic;
using Mirror;

namespace Neo.Network.Realtime
{
    /// <summary>
    ///     One fragment of a larger frame (see <see cref="NetFragmentation"/>). Registered once per project; the
    ///     <see cref="Stream"/> byte lets several independent frame streams (world snapshot, leaderboard) share it.
    /// </summary>
    public struct NetFragmentMessage : NetworkMessage
    {
        /// <summary>Which logical stream the frame belongs to (see <see cref="NetFrameSender"/>).</summary>
        public byte Stream;

        /// <summary>Sender's frame counter; wraps at 65535.</summary>
        public ushort FrameId;

        /// <summary>Fragment number, 0-based.</summary>
        public byte Index;

        /// <summary>Total fragments of the frame.</summary>
        public byte Count;

        /// <summary>Payload size of every non-last fragment.</summary>
        public ushort ChunkSize;

        /// <summary>The fragment bytes.</summary>
        public ArraySegment<byte> Payload;
    }

    /// <summary>
    ///     Server side of a fragmenting frame channel: sends one byte frame to ready, spawned clients as one or more
    ///     <see cref="NetFragmentMessage"/>s so a frame bigger than a datagram still fits the transport on an
    ///     unreliable channel (KCP / UDP). On a reliable channel the chunk size is large enough that frames normally
    ///     travel as one fragment.
    ///     <para>
    ///         Pair with <see cref="NetFrameReceiver"/> on the clients. Write the frame into a reusable
    ///         <c>NetworkWriter</c> (see <see cref="NetFrameFraming"/>) and pass <c>writer.ToArraySegment()</c>; nothing
    ///         is copied or allocated per send.
    ///     </para>
    /// </summary>
    public sealed class NetFrameSender
    {
        private readonly byte _stream;
        private readonly int _channelId;
        private readonly int _fixedChunkBytes;
        private ushort _nextFrameId;

        /// <summary>Creates a sender for one stream.</summary>
        /// <param name="stream">Stream id, matched by <see cref="NetFrameReceiver.Subscribe"/>.</param>
        /// <param name="channelId"><c>Channels.Unreliable</c> (default) for snapshots, <c>Channels.Reliable</c> for must-arrive frames.</param>
        /// <param name="chunkBytes">Payload per fragment; 0 derives it from the transport's batch threshold.</param>
        public NetFrameSender(byte stream = 0, int channelId = Channels.Unreliable, int chunkBytes = 0)
        {
            _stream = stream;
            _channelId = channelId;
            _fixedChunkBytes = chunkBytes;
        }

        /// <summary>The payload size frames are cut into right now.</summary>
        public int ChunkBytes
        {
            get
            {
                if (_fixedChunkBytes > 0)
                {
                    return _fixedChunkBytes > ushort.MaxValue ? ushort.MaxValue : _fixedChunkBytes;
                }

                int threshold = Transport.active != null ? Transport.active.GetBatchThreshold(_channelId) : 1200;
                int chunk = NetFragmentation.ChunkSizeForThreshold(threshold);
                return chunk > ushort.MaxValue ? ushort.MaxValue : chunk;
            }
        }

        /// <summary>Largest frame this sender can carry at the current chunk size.</summary>
        public int MaxFrameBytes => NetFragmentation.MaxFrameBytes(ChunkBytes);

        /// <summary>Frames handed to <see cref="SendToReady"/> / <see cref="SendTo"/> that were sent to at least one client.</summary>
        public int FramesSent { get; private set; }

        /// <summary>Fragments written to connections (one frame to N clients in K fragments counts N x K).</summary>
        public int FragmentsSent { get; private set; }

        /// <summary>Frames refused because they need more than 255 fragments.</summary>
        public int OversizeFramesRejected { get; private set; }

        /// <summary>Sends <paramref name="frame"/> to every ready, spawned client.</summary>
        /// <param name="frame">The frame bytes (not retained after the call).</param>
        /// <param name="includeLocalHost">Deliver to the host's own loopback connection too.</param>
        /// <returns>How many connections received the frame.</returns>
        public int SendToReady(ArraySegment<byte> frame, bool includeLocalHost = true)
        {
            if (!TryPlan(frame, out int chunk, out int count))
            {
                return 0;
            }

            ushort frameId = _nextFrameId++;
            int sent = 0;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (!NetReadyBroadcast.CanReceive(connection)
                    || (!includeLocalHost && connection is LocalConnectionToClient))
                {
                    continue;
                }

                SendFragments(connection, frame, frameId, chunk, count);
                sent++;
            }

            if (sent > 0)
            {
                FramesSent++;
            }

            return sent;
        }

        /// <summary>Sends <paramref name="frame"/> to one connection if it can receive it.</summary>
        /// <returns><see langword="true"/> when the frame was sent.</returns>
        public bool SendTo(NetworkConnectionToClient connection, ArraySegment<byte> frame)
        {
            if (!NetReadyBroadcast.CanReceive(connection) || !TryPlan(frame, out int chunk, out int count))
            {
                return false;
            }

            SendFragments(connection, frame, _nextFrameId++, chunk, count);
            FramesSent++;
            return true;
        }

        private bool TryPlan(ArraySegment<byte> frame, out int chunk, out int count)
        {
            chunk = ChunkBytes;
            count = NetFragmentation.FragmentCount(frame.Count, chunk);
            if (count == 0)
            {
                return false;
            }

            if (count > NetFragmentation.MaxFragments)
            {
                OversizeFramesRejected++;
                return false;
            }

            return true;
        }

        private void SendFragments(NetworkConnectionToClient connection, ArraySegment<byte> frame, ushort frameId,
            int chunk, int count)
        {
            for (int i = 0; i < count; i++)
            {
                NetFragmentation.TrySlice(frame, chunk, i, out ArraySegment<byte> payload);
                connection.Send(new NetFragmentMessage
                {
                    Stream = _stream,
                    FrameId = frameId,
                    Index = (byte)i,
                    Count = (byte)count,
                    ChunkSize = (ushort)chunk,
                    Payload = payload
                }, _channelId);
                FragmentsSent++;
            }
        }
    }

    /// <summary>
    ///     Client side of the fragmenting frame channel: registers the <see cref="NetFragmentMessage"/> handler with a
    ///     <see cref="NetClientHandlers"/> (so it survives reconnects) and hands whole frames to the subscriber of each
    ///     stream. A frame is delivered only when every fragment arrived; an incomplete or superseded frame is dropped,
    ///     which is exactly what a snapshot stream wants.
    /// </summary>
    public sealed class NetFrameReceiver
    {
        /// <summary>Called with a complete frame; the segment is valid only for the duration of the call.</summary>
        public delegate void FrameHandler(ArraySegment<byte> frame);

        private sealed class Stream
        {
            public readonly NetFragmentAssembler Assembler;
            public readonly FrameHandler Handler;

            public Stream(NetFragmentAssembler assembler, FrameHandler handler)
            {
                Assembler = assembler;
                Handler = handler;
            }
        }

        private readonly Dictionary<byte, Stream> _streams = new();
        private readonly int _maxFrameBytes;

        /// <summary>Creates the receiver and adds its handler to <paramref name="handlers"/>.</summary>
        /// <param name="handlers">Registry that keeps the handler alive across reconnects.</param>
        /// <param name="maxFrameBytes">Largest frame it will reassemble, per stream.</param>
        public NetFrameReceiver(NetClientHandlers handlers, int maxFrameBytes = 64 * 1024)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            _maxFrameBytes = maxFrameBytes;
            handlers.Add<NetFragmentMessage>(OnFragment);
            NeoNetworkManager.ClientSessionStarted += Reset;
        }

        /// <summary>Frames handed to subscribers.</summary>
        public int FramesDelivered { get; private set; }

        /// <summary>Fragments of a stream nobody subscribed to.</summary>
        public int UnknownStreamFragments { get; private set; }

        /// <summary>Subscribes to the complete frames of <paramref name="stream"/>.</summary>
        public void Subscribe(byte stream, FrameHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _streams[stream] = new Stream(new NetFragmentAssembler(_maxFrameBytes), handler);
        }

        /// <summary>Reassembly counters of one stream, or <see langword="null"/> when nobody subscribed.</summary>
        public NetFragmentAssembler GetAssembler(byte stream)
        {
            return _streams.TryGetValue(stream, out Stream entry) ? entry.Assembler : null;
        }

        /// <summary>Forgets every in-flight frame. Called automatically when a client session starts.</summary>
        public void Reset()
        {
            foreach (KeyValuePair<byte, Stream> pair in _streams)
            {
                pair.Value.Assembler.Reset();
            }
        }

        /// <summary>Stops listening for session starts. The handler itself is owned by the <see cref="NetClientHandlers"/>.</summary>
        public void Detach()
        {
            NeoNetworkManager.ClientSessionStarted -= Reset;
        }

        private void OnFragment(NetFragmentMessage message)
        {
            if (!_streams.TryGetValue(message.Stream, out Stream stream))
            {
                UnknownStreamFragments++;
                return;
            }

            if (!stream.Assembler.TryAdd(message.FrameId, message.Index, message.Count, message.ChunkSize,
                    message.Payload, out ArraySegment<byte> frame))
            {
                return;
            }

            FramesDelivered++;
            try
            {
                stream.Handler(frame);
            }
            catch (FormatException)
            {
                // a malformed frame must disconnect the sender's peer like any malformed Mirror message
                throw;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }
    }
}
#endif
