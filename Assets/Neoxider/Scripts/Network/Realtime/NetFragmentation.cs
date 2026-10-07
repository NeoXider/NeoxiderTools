using System;

namespace Neo.Network.Realtime
{
    /// <summary>
    ///     Splitting arithmetic for sending one big frame over a datagram transport (KCP / UDP), where an unreliable
    ///     message must fit one packet (about 1.2 KB) and a 4.5 KB world snapshot does not. Pure C#: the Mirror layer is
    ///     <see cref="NetFrameSender"/> / <see cref="NetFrameReceiver"/>, the reassembly is
    ///     <see cref="NetFragmentAssembler"/>.
    ///     <para>
    ///         Each fragment carries <c>frameId</c> (wraps at 65535), <c>index</c>, <c>count</c> and the <c>chunkSize</c>
    ///         every non-last fragment has, so fragments can arrive in any order and the receiver can place them
    ///         without knowing anything else.
    ///     </para>
    /// </summary>
    public static class NetFragmentation
    {
        /// <summary>Most fragments one frame may be split into (the count is a byte).</summary>
        public const int MaxFragments = 255;

        /// <summary>
        ///     Bytes a fragment message adds on top of its payload: Mirror's message id (2), frameId (2), stream (1),
        ///     index (1), count (1), chunkSize (2), the segment length prefix (up to 3) and the batch timestamp (8).
        ///     Used to derive a safe chunk size from the transport's batch threshold.
        /// </summary>
        public const int OverheadBytes = 24;

        /// <summary>Smallest chunk size <see cref="ChunkSizeForThreshold"/> will return.</summary>
        public const int MinChunkBytes = 256;

        /// <summary>The payload size to cut frames into for a transport whose batch threshold is <paramref name="threshold"/> bytes.</summary>
        /// <param name="threshold">For Mirror: <c>Transport.active.GetBatchThreshold(channelId)</c>.</param>
        public static int ChunkSizeForThreshold(int threshold)
        {
            int chunk = threshold - OverheadBytes;
            return chunk < MinChunkBytes ? MinChunkBytes : chunk;
        }

        /// <summary>How many fragments a frame of <paramref name="frameLength"/> bytes needs at <paramref name="chunkSize"/>.</summary>
        /// <returns>0 for an empty frame or a non-positive chunk size; otherwise at least 1. May exceed <see cref="MaxFragments"/>.</returns>
        public static int FragmentCount(int frameLength, int chunkSize)
        {
            if (frameLength <= 0 || chunkSize <= 0)
            {
                return 0;
            }

            return (frameLength + chunkSize - 1) / chunkSize;
        }

        /// <summary>Largest frame that can be split into <see cref="MaxFragments"/> fragments of <paramref name="chunkSize"/>.</summary>
        public static int MaxFrameBytes(int chunkSize)
        {
            return chunkSize <= 0 ? 0 : chunkSize * MaxFragments;
        }

        /// <summary>
        ///     Cuts fragment <paramref name="index"/> out of <paramref name="frame"/>. A view, not a copy.
        /// </summary>
        /// <returns><see langword="false"/> when the index is out of range for this frame and chunk size.</returns>
        public static bool TrySlice(ArraySegment<byte> frame, int chunkSize, int index, out ArraySegment<byte> payload)
        {
            payload = default;
            int count = FragmentCount(frame.Count, chunkSize);
            if (index < 0 || index >= count)
            {
                return false;
            }

            int offset = index * chunkSize;
            int length = Math.Min(chunkSize, frame.Count - offset);
            payload = new ArraySegment<byte>(frame.Array, frame.Offset + offset, length);
            return true;
        }

        /// <summary>True when <paramref name="a"/> is later than <paramref name="b"/> in wrapping 16-bit order.</summary>
        public static bool IsNewer(ushort a, ushort b)
        {
            ushort diff = (ushort)(a - b);
            return diff != 0 && diff < 0x8000;
        }
    }

    /// <summary>
    ///     Reassembles fragments (see <see cref="NetFragmentation"/>) into whole frames with a bounded, preallocated
    ///     footprint: a few in-flight frames, each in a buffer of <c>maxFrameBytes</c>. Frames are latest-wins: when a
    ///     newer frame completes, older incomplete ones are dropped (a snapshot that is late is worthless), and a
    ///     fragment of a frame older than the newest completed one is ignored. Allocation-free after construction.
    /// </summary>
    public sealed class NetFragmentAssembler
    {
        private sealed class Slot
        {
            public readonly byte[] Buffer;
            public readonly uint[] Seen = new uint[8];
            public bool Active;
            public ushort FrameId;
            public int Count;
            public int ChunkSize;
            public int Received;
            public int LastLength;

            public Slot(int size)
            {
                Buffer = new byte[size];
            }

            public void Begin(ushort frameId, int count, int chunkSize)
            {
                Active = true;
                FrameId = frameId;
                Count = count;
                ChunkSize = chunkSize;
                Received = 0;
                LastLength = 0;
                Array.Clear(Seen, 0, Seen.Length);
            }
        }

        private readonly Slot[] _slots;
        private readonly int _maxFrameBytes;
        private ushort _lastCompleted;
        private bool _hasCompleted;

        /// <summary>Creates an assembler.</summary>
        /// <param name="maxFrameBytes">Largest frame it will accept; bigger declarations are rejected.</param>
        /// <param name="inFlightFrames">How many frames may be reassembled at once (minimum 1).</param>
        public NetFragmentAssembler(int maxFrameBytes, int inFlightFrames = 2)
        {
            _maxFrameBytes = maxFrameBytes < 1 ? 1 : maxFrameBytes;
            int slotCount = inFlightFrames < 1 ? 1 : inFlightFrames;
            _slots = new Slot[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                _slots[i] = new Slot(_maxFrameBytes);
            }
        }

        /// <summary>Frames reassembled completely.</summary>
        public int CompletedFrames { get; private set; }

        /// <summary>Incomplete frames discarded because a newer frame needed their slot or completed first.</summary>
        public int DroppedFrames { get; private set; }

        /// <summary>Fragments ignored because their frame is older than the newest completed one.</summary>
        public int StaleFragments { get; private set; }

        /// <summary>Fragments ignored because that exact fragment was already received.</summary>
        public int DuplicateFragments { get; private set; }

        /// <summary>Fragments refused as malformed (index out of range, wrong size, frame too large).</summary>
        public int RejectedFragments { get; private set; }

        /// <summary>Forgets every in-flight frame and the completed-frame watermark (disconnect, reconnect).</summary>
        public void Reset()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Active = false;
            }

            _hasCompleted = false;
            _lastCompleted = 0;
        }

        /// <summary>
        ///     Feeds one fragment. Returns <see langword="true"/> when it completed a frame; <paramref name="frame"/>
        ///     then views the assembler's own buffer and stays valid until the next call of this method.
        /// </summary>
        /// <param name="frameId">Sender's frame counter (wraps).</param>
        /// <param name="index">Fragment number, 0-based.</param>
        /// <param name="count">Total fragments of the frame, 1..255.</param>
        /// <param name="chunkSize">Payload size of every non-last fragment.</param>
        /// <param name="payload">The fragment bytes (copied; the caller's buffer may be reused immediately).</param>
        /// <param name="frame">The completed frame, when the result is <see langword="true"/>.</param>
        public bool TryAdd(ushort frameId, byte index, byte count, int chunkSize, ArraySegment<byte> payload,
            out ArraySegment<byte> frame)
        {
            frame = default;
            if (!IsWellFormed(index, count, chunkSize, payload))
            {
                RejectedFragments++;
                return false;
            }

            if (_hasCompleted && !NetFragmentation.IsNewer(frameId, _lastCompleted))
            {
                StaleFragments++;
                return false;
            }

            Slot slot = FindSlot(frameId);
            if (slot == null)
            {
                slot = AcquireSlot(frameId);
                if (slot == null)
                {
                    StaleFragments++;
                    return false;
                }

                slot.Begin(frameId, count, chunkSize);
            }
            else if (slot.Count != count || slot.ChunkSize != chunkSize)
            {
                RejectedFragments++;
                return false;
            }

            int word = index >> 5;
            uint bit = 1u << (index & 31);
            if ((slot.Seen[word] & bit) != 0u)
            {
                DuplicateFragments++;
                return false;
            }

            slot.Seen[word] |= bit;
            slot.Received++;
            Buffer.BlockCopy(payload.Array, payload.Offset, slot.Buffer, index * chunkSize, payload.Count);
            if (index == count - 1)
            {
                slot.LastLength = payload.Count;
            }

            if (slot.Received < slot.Count)
            {
                return false;
            }

            int total = (slot.Count - 1) * slot.ChunkSize + slot.LastLength;
            slot.Active = false;
            _lastCompleted = frameId;
            _hasCompleted = true;
            CompletedFrames++;
            DropOlderThan(frameId);
            frame = new ArraySegment<byte>(slot.Buffer, 0, total);
            return true;
        }

        private bool IsWellFormed(byte index, byte count, int chunkSize, ArraySegment<byte> payload)
        {
            if (count < 1 || index >= count || chunkSize < 1 || payload.Array == null || payload.Count < 1
                || payload.Count > chunkSize)
            {
                return false;
            }

            bool isLast = index == count - 1;
            if (!isLast && payload.Count != chunkSize)
            {
                return false;
            }

            long declared = (long)(count - 1) * chunkSize + (isLast ? payload.Count : 1);
            return declared <= _maxFrameBytes;
        }

        private Slot FindSlot(ushort frameId)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Active && _slots[i].FrameId == frameId)
                {
                    return _slots[i];
                }
            }

            return null;
        }

        // A free slot, else the oldest incomplete frame (dropped), unless every in-flight frame is newer than this one.
        private Slot AcquireSlot(ushort frameId)
        {
            Slot oldest = null;
            for (int i = 0; i < _slots.Length; i++)
            {
                Slot candidate = _slots[i];
                if (!candidate.Active)
                {
                    return candidate;
                }

                if (oldest == null || NetFragmentation.IsNewer(oldest.FrameId, candidate.FrameId))
                {
                    oldest = candidate;
                }
            }

            if (oldest != null && NetFragmentation.IsNewer(frameId, oldest.FrameId))
            {
                oldest.Active = false;
                DroppedFrames++;
                return oldest;
            }

            return null;
        }

        private void DropOlderThan(ushort frameId)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Active && NetFragmentation.IsNewer(frameId, _slots[i].FrameId))
                {
                    _slots[i].Active = false;
                    DroppedFrames++;
                }
            }
        }
    }
}
