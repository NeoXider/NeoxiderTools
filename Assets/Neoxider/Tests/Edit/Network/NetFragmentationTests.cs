using System;
using System.Collections.Generic;
using Neo.Network.Realtime;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class NetFragmentationTests
    {
        private const int Chunk = 100;

        private static byte[] MakeFrame(int length, byte seed = 1)
        {
            byte[] frame = new byte[length];
            for (int i = 0; i < length; i++)
            {
                frame[i] = (byte)(seed + i * 7);
            }

            return frame;
        }

        private static List<(byte index, byte count, ArraySegment<byte> payload)> Split(byte[] frame, int chunk)
        {
            List<(byte, byte, ArraySegment<byte>)> list = new List<(byte, byte, ArraySegment<byte>)>();
            int count = NetFragmentation.FragmentCount(frame.Length, chunk);
            for (int i = 0; i < count; i++)
            {
                Assert.That(NetFragmentation.TrySlice(new ArraySegment<byte>(frame), chunk, i, out ArraySegment<byte> part),
                    Is.True);
                list.Add(((byte)i, (byte)count, part));
            }

            return list;
        }

        private static byte[] Copy(ArraySegment<byte> segment)
        {
            byte[] copy = new byte[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, copy, 0, segment.Count);
            return copy;
        }

        [Test]
        public void FragmentCount_RoundsUp_AndHandlesDegenerateInput()
        {
            Assert.That(NetFragmentation.FragmentCount(0, 100), Is.EqualTo(0));
            Assert.That(NetFragmentation.FragmentCount(1, 100), Is.EqualTo(1));
            Assert.That(NetFragmentation.FragmentCount(100, 100), Is.EqualTo(1));
            Assert.That(NetFragmentation.FragmentCount(101, 100), Is.EqualTo(2));
            Assert.That(NetFragmentation.FragmentCount(4500, 1176), Is.EqualTo(4));
            Assert.That(NetFragmentation.FragmentCount(10, 0), Is.EqualTo(0));
            Assert.That(NetFragmentation.MaxFrameBytes(1000), Is.EqualTo(255000));
        }

        [Test]
        public void ChunkSizeForThreshold_LeavesRoomForTheHeaders_AndHasAFloor()
        {
            Assert.That(NetFragmentation.ChunkSizeForThreshold(1200), Is.EqualTo(1200 - NetFragmentation.OverheadBytes));
            Assert.That(NetFragmentation.ChunkSizeForThreshold(100), Is.EqualTo(NetFragmentation.MinChunkBytes));
        }

        [Test]
        public void TrySlice_CutsViewsOfTheRightSizes()
        {
            byte[] frame = MakeFrame(250);
            Assert.That(NetFragmentation.TrySlice(new ArraySegment<byte>(frame), Chunk, 0, out ArraySegment<byte> a), Is.True);
            Assert.That(NetFragmentation.TrySlice(new ArraySegment<byte>(frame), Chunk, 2, out ArraySegment<byte> c), Is.True);
            Assert.That(a.Count, Is.EqualTo(100));
            Assert.That(c.Count, Is.EqualTo(50));
            Assert.That(c.Offset, Is.EqualTo(200));
            Assert.That(NetFragmentation.TrySlice(new ArraySegment<byte>(frame), Chunk, 3, out _), Is.False);
            Assert.That(NetFragmentation.TrySlice(new ArraySegment<byte>(frame), Chunk, -1, out _), Is.False);
        }

        [Test]
        public void TrySlice_RespectsTheSegmentOffset()
        {
            byte[] backing = MakeFrame(300);
            ArraySegment<byte> window = new ArraySegment<byte>(backing, 50, 200);
            NetFragmentation.TrySlice(window, Chunk, 1, out ArraySegment<byte> part);
            Assert.That(part.Offset, Is.EqualTo(150));
            Assert.That(part.Count, Is.EqualTo(100));
        }

        [Test]
        public void IsNewer_UsesWrappingOrder()
        {
            Assert.That(NetFragmentation.IsNewer(5, 4), Is.True);
            Assert.That(NetFragmentation.IsNewer(4, 5), Is.False);
            Assert.That(NetFragmentation.IsNewer(4, 4), Is.False);
            Assert.That(NetFragmentation.IsNewer(2, 65535), Is.True, "wrapped past the top");
            Assert.That(NetFragmentation.IsNewer(65535, 2), Is.False);
        }

        [Test]
        public void InOrderFragments_ReassembleTheFrame()
        {
            byte[] frame = MakeFrame(450);
            NetFragmentAssembler assembler = new NetFragmentAssembler(1024);
            List<(byte index, byte count, ArraySegment<byte> payload)> parts = Split(frame, Chunk);
            Assert.That(parts.Count, Is.EqualTo(5));

            ArraySegment<byte> assembled = default;
            for (int i = 0; i < parts.Count; i++)
            {
                bool done = assembler.TryAdd(7, parts[i].index, parts[i].count, Chunk, parts[i].payload, out assembled);
                Assert.That(done, Is.EqualTo(i == parts.Count - 1));
            }

            Assert.That(Copy(assembled), Is.EqualTo(frame));
            Assert.That(assembler.CompletedFrames, Is.EqualTo(1));
        }

        [Test]
        public void OutOfOrderFragments_ReassembleTheFrame()
        {
            byte[] frame = MakeFrame(450, 9);
            NetFragmentAssembler assembler = new NetFragmentAssembler(1024);
            List<(byte index, byte count, ArraySegment<byte> payload)> parts = Split(frame, Chunk);
            int[] order = { 4, 2, 0, 3, 1 };

            ArraySegment<byte> assembled = default;
            bool done = false;
            for (int i = 0; i < order.Length; i++)
            {
                (byte index, byte count, ArraySegment<byte> payload) part = parts[order[i]];
                done = assembler.TryAdd(1, part.index, part.count, Chunk, part.payload, out assembled);
            }

            Assert.That(done, Is.True);
            Assert.That(Copy(assembled), Is.EqualTo(frame), "the short last fragment arrived before the others");
        }

        [Test]
        public void ASingleFragmentFrame_CompletesImmediately()
        {
            byte[] frame = MakeFrame(40);
            NetFragmentAssembler assembler = new NetFragmentAssembler(1024);
            Assert.That(assembler.TryAdd(3, 0, 1, Chunk, new ArraySegment<byte>(frame), out ArraySegment<byte> assembled), Is.True);
            Assert.That(Copy(assembled), Is.EqualTo(frame));
        }

        [Test]
        public void DuplicateFragments_AreCountedAndDoNotComplete()
        {
            byte[] frame = MakeFrame(250);
            NetFragmentAssembler assembler = new NetFragmentAssembler(1024);
            List<(byte index, byte count, ArraySegment<byte> payload)> parts = Split(frame, Chunk);
            assembler.TryAdd(1, parts[0].index, parts[0].count, Chunk, parts[0].payload, out _);
            Assert.That(assembler.TryAdd(1, parts[0].index, parts[0].count, Chunk, parts[0].payload, out _), Is.False);
            Assert.That(assembler.TryAdd(1, parts[1].index, parts[1].count, Chunk, parts[1].payload, out _), Is.False);
            Assert.That(assembler.DuplicateFragments, Is.EqualTo(1));
            Assert.That(assembler.TryAdd(1, parts[2].index, parts[2].count, Chunk, parts[2].payload, out ArraySegment<byte> assembled), Is.True);
            Assert.That(Copy(assembled), Is.EqualTo(frame));
        }

        [Test]
        public void ANewerFrameCompleting_DropsTheOlderIncompleteOne()
        {
            byte[] older = MakeFrame(250, 1);
            byte[] newer = MakeFrame(250, 50);
            NetFragmentAssembler assembler = new NetFragmentAssembler(1024);
            List<(byte index, byte count, ArraySegment<byte> payload)> olderParts = Split(older, Chunk);
            List<(byte index, byte count, ArraySegment<byte> payload)> newerParts = Split(newer, Chunk);

            assembler.TryAdd(10, olderParts[0].index, olderParts[0].count, Chunk, olderParts[0].payload, out _);
            ArraySegment<byte> assembled = default;
            for (int i = 0; i < newerParts.Count; i++)
            {
                assembler.TryAdd(11, newerParts[i].index, newerParts[i].count, Chunk, newerParts[i].payload, out assembled);
            }

            Assert.That(Copy(assembled), Is.EqualTo(newer));
            Assert.That(assembler.DroppedFrames, Is.EqualTo(1), "the half-received older frame is worthless now");

            // the missing tail of the older frame arrives late: ignored
            Assert.That(assembler.TryAdd(10, olderParts[1].index, olderParts[1].count, Chunk, olderParts[1].payload, out _), Is.False);
            Assert.That(assembler.StaleFragments, Is.EqualTo(1));
        }

        [Test]
        public void ACompletedFrame_IsNeverDeliveredTwice()
        {
            byte[] frame = MakeFrame(150);
            NetFragmentAssembler assembler = new NetFragmentAssembler(1024);
            List<(byte index, byte count, ArraySegment<byte> payload)> parts = Split(frame, Chunk);
            for (int i = 0; i < parts.Count; i++)
            {
                assembler.TryAdd(5, parts[i].index, parts[i].count, Chunk, parts[i].payload, out _);
            }

            Assert.That(assembler.TryAdd(5, parts[0].index, parts[0].count, Chunk, parts[0].payload, out _), Is.False);
            Assert.That(assembler.StaleFragments, Is.EqualTo(1));
            Assert.That(assembler.CompletedFrames, Is.EqualTo(1));
        }

        [Test]
        public void WhenSlotsRunOut_TheOldestIncompleteFrameIsEvicted()
        {
            NetFragmentAssembler assembler = new NetFragmentAssembler(1024, 2);
            byte[] f1 = MakeFrame(250, 1);
            byte[] f2 = MakeFrame(250, 2);
            byte[] f3 = MakeFrame(250, 3);
            List<(byte index, byte count, ArraySegment<byte> payload)> p1 = Split(f1, Chunk);
            List<(byte index, byte count, ArraySegment<byte> payload)> p2 = Split(f2, Chunk);
            List<(byte index, byte count, ArraySegment<byte> payload)> p3 = Split(f3, Chunk);

            assembler.TryAdd(1, p1[0].index, p1[0].count, Chunk, p1[0].payload, out _);
            assembler.TryAdd(2, p2[0].index, p2[0].count, Chunk, p2[0].payload, out _);
            assembler.TryAdd(3, p3[0].index, p3[0].count, Chunk, p3[0].payload, out _);
            Assert.That(assembler.DroppedFrames, Is.EqualTo(1), "frame 1 lost its slot to frame 3");

            ArraySegment<byte> assembled = default;
            assembler.TryAdd(3, p3[1].index, p3[1].count, Chunk, p3[1].payload, out assembled);
            bool done = assembler.TryAdd(3, p3[2].index, p3[2].count, Chunk, p3[2].payload, out assembled);
            Assert.That(done, Is.True);
            Assert.That(Copy(assembled), Is.EqualTo(f3));
        }

        [Test]
        public void ALateFragmentOfAFrameOlderThanEveryInFlightOne_IsIgnored()
        {
            NetFragmentAssembler assembler = new NetFragmentAssembler(1024, 1);
            byte[] f5 = MakeFrame(250, 5);
            byte[] f4 = MakeFrame(250, 4);
            List<(byte index, byte count, ArraySegment<byte> payload)> p5 = Split(f5, Chunk);
            List<(byte index, byte count, ArraySegment<byte> payload)> p4 = Split(f4, Chunk);

            assembler.TryAdd(5, p5[0].index, p5[0].count, Chunk, p5[0].payload, out _);
            Assert.That(assembler.TryAdd(4, p4[0].index, p4[0].count, Chunk, p4[0].payload, out _), Is.False);
            Assert.That(assembler.StaleFragments, Is.EqualTo(1));
            Assert.That(assembler.DroppedFrames, Is.EqualTo(0), "frame 5 keeps its slot");
        }

        [Test]
        public void FrameIdWraparound_IsHandled()
        {
            NetFragmentAssembler assembler = new NetFragmentAssembler(1024);
            byte[] a = MakeFrame(150, 1);
            byte[] b = MakeFrame(150, 2);
            List<(byte index, byte count, ArraySegment<byte> payload)> pa = Split(a, Chunk);
            List<(byte index, byte count, ArraySegment<byte> payload)> pb = Split(b, Chunk);
            for (int i = 0; i < pa.Count; i++)
            {
                assembler.TryAdd(65535, pa[i].index, pa[i].count, Chunk, pa[i].payload, out _);
            }

            ArraySegment<byte> assembled = default;
            bool done = false;
            for (int i = 0; i < pb.Count; i++)
            {
                done = assembler.TryAdd(0, pb[i].index, pb[i].count, Chunk, pb[i].payload, out assembled);
            }

            Assert.That(done, Is.True, "frame 0 after frame 65535 is the newer one");
            Assert.That(Copy(assembled), Is.EqualTo(b));
        }

        [Test]
        public void MalformedFragments_AreRejected()
        {
            NetFragmentAssembler assembler = new NetFragmentAssembler(300);
            byte[] bytes = MakeFrame(100);
            ArraySegment<byte> full = new ArraySegment<byte>(bytes);

            Assert.That(assembler.TryAdd(1, 0, 0, Chunk, full, out _), Is.False, "count 0");
            Assert.That(assembler.TryAdd(1, 3, 3, Chunk, full, out _), Is.False, "index == count");
            Assert.That(assembler.TryAdd(1, 0, 3, 0, full, out _), Is.False, "chunk size 0");
            Assert.That(assembler.TryAdd(1, 0, 3, 50, full, out _), Is.False, "payload larger than the chunk size");
            Assert.That(assembler.TryAdd(1, 0, 3, Chunk, new ArraySegment<byte>(bytes, 0, 60), out _), Is.False,
                "a non-last fragment shorter than the chunk size");
            Assert.That(assembler.TryAdd(1, 0, 200, Chunk, full, out _), Is.False, "declares more than maxFrameBytes");
            Assert.That(assembler.TryAdd(1, 0, 1, Chunk, new ArraySegment<byte>(bytes, 0, 0), out _), Is.False, "empty payload");
            Assert.That(assembler.RejectedFragments, Is.EqualTo(7));
        }

        [Test]
        public void AFragmentThatContradictsItsFrame_IsRejected()
        {
            NetFragmentAssembler assembler = new NetFragmentAssembler(1024);
            byte[] bytes = MakeFrame(100);
            ArraySegment<byte> full = new ArraySegment<byte>(bytes);
            assembler.TryAdd(1, 0, 3, Chunk, full, out _);
            Assert.That(assembler.TryAdd(1, 1, 4, Chunk, full, out _), Is.False, "same frame id, different count");
            Assert.That(assembler.RejectedFragments, Is.EqualTo(1));
        }

        [Test]
        public void Reset_ForgetsEverythingIncludingTheWatermark()
        {
            NetFragmentAssembler assembler = new NetFragmentAssembler(1024);
            byte[] frame = MakeFrame(120);
            List<(byte index, byte count, ArraySegment<byte> payload)> parts = Split(frame, Chunk);
            for (int i = 0; i < parts.Count; i++)
            {
                assembler.TryAdd(900, parts[i].index, parts[i].count, Chunk, parts[i].payload, out _);
            }

            // a restarted server counts frames from 0 again: without Reset these would be "older" than 900
            assembler.Reset();
            ArraySegment<byte> assembled = default;
            bool done = false;
            for (int i = 0; i < parts.Count; i++)
            {
                done = assembler.TryAdd(0, parts[i].index, parts[i].count, Chunk, parts[i].payload, out assembled);
            }

            Assert.That(done, Is.True);
            Assert.That(Copy(assembled), Is.EqualTo(frame));
        }

        [Test]
        public void AFourAndAHalfKilobyteFrame_TravelsInFourDatagrams()
        {
            // the sizing that motivated the feature: a 10-player arena frame over a ~1.2 KB datagram budget
            int chunk = NetFragmentation.ChunkSizeForThreshold(1200);
            byte[] frame = MakeFrame(4500);
            Assert.That(NetFragmentation.FragmentCount(frame.Length, chunk), Is.EqualTo(4));

            NetFragmentAssembler assembler = new NetFragmentAssembler(8192);
            List<(byte index, byte count, ArraySegment<byte> payload)> parts = Split(frame, chunk);
            ArraySegment<byte> assembled = default;
            bool done = false;
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                done = assembler.TryAdd(42, parts[i].index, parts[i].count, chunk, parts[i].payload, out assembled);
            }

            Assert.That(done, Is.True);
            Assert.That(Copy(assembled), Is.EqualTo(frame));
        }
    }
}
