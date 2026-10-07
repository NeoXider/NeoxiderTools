#if MIRROR
using System;
using Mirror;
using Neo.Network;
using Neo.Network.Realtime;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>EditMode tests for the Mirror-facing parts of the realtime toolkit that need no live session.</summary>
    [TestFixture]
    public sealed class NetRealtimeMirrorTests
    {
        public struct ProbeMessageA : NetworkMessage
        {
            public int Value;
        }

        public struct ProbeMessageB : NetworkMessage
        {
            public int Value;
        }

        [SetUp]
        public void SetUp()
        {
            NetworkClient.Shutdown();
            NetworkServer.Shutdown();
        }

        [TearDown]
        public void TearDown()
        {
            NetworkClient.Shutdown();
            NetworkServer.Shutdown();
        }

        // ---- NetFrameFraming -----------------------------------------------------------------------

        private static ArraySegment<byte> WriteFrame(byte version, int[] values, out NetworkWriter writer)
        {
            writer = new NetworkWriter();
            int lengthAt = NetFrameFraming.WriteHeader(writer, version);
            writer.WriteVarUInt(NetFrameFraming.NonNegative(values.Length));
            for (int i = 0; i < values.Length; i++)
            {
                writer.WriteVarUInt(NetFrameFraming.NonNegative(values[i]));
            }

            NetFrameFraming.PatchLength(writer, lengthAt);
            return writer.ToArraySegment();
        }

        [Test]
        public void Framing_RoundTripsAVersionedFrame()
        {
            ArraySegment<byte> frame = WriteFrame(3, new[] { 7, 300, 0, 123456 }, out _);
            NetworkReader reader = new NetworkReader(frame);

            NetFrameFraming.ReadHeader(reader, 3, "TestFrame", out int bodyStart, out int bodyEnd);
            int count = NetFrameFraming.ReadCount(reader, 16, "value");
            Assert.That(count, Is.EqualTo(4));
            Assert.That(NetFrameFraming.ReadCount32(reader), Is.EqualTo(7));
            Assert.That(NetFrameFraming.ReadCount32(reader), Is.EqualTo(300));
            Assert.That(NetFrameFraming.ReadCount32(reader), Is.EqualTo(0));
            Assert.That(NetFrameFraming.ReadCount32(reader), Is.EqualTo(123456));
            Assert.DoesNotThrow(() => NetFrameFraming.VerifyBodyLength(reader, bodyStart, bodyEnd));
        }

        [Test]
        public void Framing_RefusesAWrongVersion()
        {
            ArraySegment<byte> frame = WriteFrame(3, new[] { 1 }, out _);
            NetworkReader reader = new NetworkReader(frame);
            FormatException error = Assert.Throws<FormatException>(
                () => NetFrameFraming.ReadHeader(reader, 4, "TestFrame", out _, out _));
            StringAssert.Contains("version mismatch", error.Message);
            StringAssert.Contains("TestFrame", error.Message);
        }

        [Test]
        public void Framing_RefusesATruncatedBody()
        {
            ArraySegment<byte> frame = WriteFrame(1, new[] { 1, 2, 3, 4, 5 }, out _);
            ArraySegment<byte> cut = new ArraySegment<byte>(frame.Array, frame.Offset, frame.Count - 3);
            NetworkReader reader = new NetworkReader(cut);
            Assert.Throws<FormatException>(() => NetFrameFraming.ReadHeader(reader, 1, "TestFrame", out _, out _));
        }

        [Test]
        public void Framing_RefusesACountAboveItsCeiling()
        {
            NetworkWriter writer = new NetworkWriter();
            int lengthAt = NetFrameFraming.WriteHeader(writer, 1);
            writer.WriteVarUInt(1000u);
            NetFrameFraming.PatchLength(writer, lengthAt);
            NetworkReader reader = new NetworkReader(writer.ToArraySegment());
            NetFrameFraming.ReadHeader(reader, 1, "TestFrame", out _, out _);
            FormatException error = Assert.Throws<FormatException>(() => NetFrameFraming.ReadCount(reader, 16, "entity"));
            StringAssert.Contains("entity", error.Message);
        }

        [Test]
        public void Framing_DetectsAnUnderReadBody()
        {
            ArraySegment<byte> frame = WriteFrame(1, new[] { 5, 6 }, out _);
            NetworkReader reader = new NetworkReader(frame);
            NetFrameFraming.ReadHeader(reader, 1, "TestFrame", out int bodyStart, out int bodyEnd);
            NetFrameFraming.ReadCount(reader, 16, "value");
            Assert.Throws<FormatException>(() => NetFrameFraming.VerifyBodyLength(reader, bodyStart, bodyEnd));
        }

        [Test]
        public void Framing_NarrowingHelpersSaturate()
        {
            Assert.That(NetFrameFraming.NonNegative(-5), Is.EqualTo(0u));
            Assert.That(NetFrameFraming.NonNegative(9), Is.EqualTo(9u));
            Assert.That(NetFrameFraming.ClampByte(-1), Is.EqualTo((byte)0));
            Assert.That(NetFrameFraming.ClampByte(300), Is.EqualTo((byte)255));
            Assert.That(NetFrameFraming.ClampByte(77), Is.EqualTo((byte)77));
        }

        // ---- connection state ------------------------------------------------------------------------

        [Test]
        public void IsConnectionSpawned_IsFalseForNullAndForUnreadyConnections()
        {
            Assert.That(NeoNetworkState.IsConnectionSpawned(null), Is.False);
            Assert.That(NetReadyBroadcast.CanReceive(null), Is.False);

            NetworkConnectionToClient unready = new NetworkConnectionToClient(41);
            Assert.That(NeoNetworkState.IsConnectionSpawned(unready), Is.False);

            NetworkConnectionToClient readyButPlayerless = new NetworkConnectionToClient(42) { isReady = true };
            Assert.That(NeoNetworkState.IsConnectionSpawned(readyButPlayerless), Is.False,
                "ready alone is not enough: the spawn burst only goes out once the connection owns a player");
            Assert.That(NeoNetworkState.IsLocalHostConnection(readyButPlayerless), Is.False);
            Assert.That(NeoNetworkState.IsLocalHostConnection(null), Is.False);
        }

        [Test]
        public void ReadyBroadcast_WithNoServer_SendsNothing()
        {
            Assert.That(NetReadyBroadcast.ToReadyClients(new ProbeMessageA { Value = 1 }), Is.EqualTo(0));
            Assert.That(NetReadyBroadcast.CountReady(), Is.EqualTo(0));
            Assert.That(NetReadyBroadcast.SendTo(null, new ProbeMessageA()), Is.False);
        }

        // ---- handler registries ------------------------------------------------------------------------

        [Test]
        public void ClientHandlers_RegisterOnceAndDropOnRequest()
        {
            using NetClientHandlers handlers = new NetClientHandlers(false);
            int received = 0;
            handlers.Add<ProbeMessageA>(_ => received++);
            handlers.Add<ProbeMessageB>(_ => received++);
            Assert.That(handlers.Count, Is.EqualTo(2));
            Assert.That(handlers.IsRegistered, Is.False);

            handlers.RegisterNow();
            handlers.RegisterNow();
            Assert.That(handlers.IsRegistered, Is.True);
            Assert.That(NetworkClient.UnregisterHandler<ProbeMessageA>(), Is.True, "registered with Mirror");
            Assert.That(NetworkClient.UnregisterHandler<ProbeMessageB>(), Is.True);

            handlers.UnregisterAll();
            Assert.That(handlers.IsRegistered, Is.False);
        }

        [Test]
        public void ClientHandlers_AddedAfterRegistration_AreRegisteredImmediately()
        {
            using NetClientHandlers handlers = new NetClientHandlers(false);
            handlers.RegisterNow();
            handlers.Add<ProbeMessageA>(_ => { });
            Assert.That(NetworkClient.UnregisterHandler<ProbeMessageA>(), Is.True);
        }

        [Test]
        public void ClientHandlers_RegisterAgainAfterMirrorShutdown_WithoutADuplicateWarning()
        {
            using NetClientHandlers handlers = new NetClientHandlers(false);
            handlers.Add<ProbeMessageA>(_ => { });
            handlers.RegisterNow();

            NetworkClient.Shutdown(); // what StopClient does: every handler is cleared
            Assert.That(NetworkClient.UnregisterHandler<ProbeMessageA>(), Is.False);

            handlers.UnregisterAll(); // bookkeeping only: Mirror already forgot them
            handlers.RegisterNow();
            Assert.That(NetworkClient.UnregisterHandler<ProbeMessageA>(), Is.True);
        }

        [Test]
        public void ClientHandlers_NullHandler_Throws()
        {
            using NetClientHandlers handlers = new NetClientHandlers(false);
            Assert.Throws<ArgumentNullException>(() => handlers.Add<ProbeMessageA>(null));
        }

        [Test]
        public void ClientHandlers_Dispose_UnregistersAndStopsRegistering()
        {
            NetClientHandlers handlers = new NetClientHandlers(false);
            handlers.Add<ProbeMessageA>(_ => { });
            handlers.RegisterNow();
            handlers.Dispose();
            Assert.That(NetworkClient.UnregisterHandler<ProbeMessageA>(), Is.False);
            handlers.RegisterNow();
            Assert.That(NetworkClient.UnregisterHandler<ProbeMessageA>(), Is.False, "a disposed set registers nothing");
        }

        [Test]
        public void ServerHandlers_RegisterAndDrop()
        {
            using NetServerHandlers handlers = new NetServerHandlers(false);
            handlers.Add<ProbeMessageA>((connection, message) => { });
            handlers.RegisterNow();
            Assert.That(handlers.IsRegistered, Is.True);
            NetworkServer.UnregisterHandler<ProbeMessageA>();
            handlers.UnregisterAll();
            Assert.That(handlers.IsRegistered, Is.False);
            Assert.Throws<ArgumentNullException>(() => handlers.Add<ProbeMessageB>(null));
        }

        [Test]
        public void EventChannel_WithNoServer_PublishesNothing()
        {
            using NetClientHandlers handlers = new NetClientHandlers(false);
            NetEventChannel<ProbeMessageA> channel = new NetEventChannel<ProbeMessageA>(handlers);
            Assert.That(channel.Publish(new ProbeMessageA { Value = 5 }), Is.EqualTo(0));
            Assert.That(channel.PublishedCount, Is.EqualTo(0));
            Assert.That(channel.PublishTo(null, new ProbeMessageA()), Is.False);
            Assert.Throws<ArgumentNullException>(() => new NetEventChannel<ProbeMessageA>(null));
            Assert.That(handlers.Count, Is.EqualTo(1), "the channel added its handler to the registry");
        }

        // ---- fragment transport ---------------------------------------------------------------------------

        [Test]
        public void FrameSender_PlansChunksFromAnExplicitSizeAndRejectsOversizeFrames()
        {
            NetFrameSender sender = new NetFrameSender(0, Channels.Unreliable, 500);
            Assert.That(sender.ChunkBytes, Is.EqualTo(500));
            Assert.That(sender.MaxFrameBytes, Is.EqualTo(500 * 255));

            byte[] tooBig = new byte[500 * 255 + 1];
            Assert.That(sender.SendToReady(new ArraySegment<byte>(tooBig)), Is.EqualTo(0));
            Assert.That(sender.OversizeFramesRejected, Is.EqualTo(1));
            Assert.That(sender.SendToReady(new ArraySegment<byte>(new byte[0])), Is.EqualTo(0));
            Assert.That(sender.FramesSent, Is.EqualTo(0));
        }

        [Test]
        public void FrameReceiver_ExposesPerStreamAssemblers()
        {
            using NetClientHandlers handlers = new NetClientHandlers(false);
            NetFrameReceiver receiver = new NetFrameReceiver(handlers, 4096);
            int delivered = 0;
            receiver.Subscribe(2, _ => delivered++);
            Assert.That(receiver.GetAssembler(2), Is.Not.Null);
            Assert.That(receiver.GetAssembler(9), Is.Null);
            Assert.That(delivered, Is.EqualTo(0));
            receiver.Detach();
        }
    }
}
#endif
