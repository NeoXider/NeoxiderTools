#if MIRROR
using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using Neo.Network.Realtime;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoNetworkHandlerOwnershipTests
    {
        public struct OwnershipMessage : NetworkMessage
        {
            public int Value;
        }

        [TearDown]
        public void TearDown()
        {
            NetworkClient.UnregisterHandler<OwnershipMessage>();
            NetworkServer.UnregisterHandler<OwnershipMessage>();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OldSetDisposal_DoesNotRemoveCurrentReceiver(bool server)
        {
            int first = 0;
            int second = 0;
            int deliveries = 0;
            using NetClientHandlers clientA = new NetClientHandlers(false);
            using NetClientHandlers clientB = new NetClientHandlers(false);
            using NetServerHandlers serverA = new NetServerHandlers(false);
            using NetServerHandlers serverB = new NetServerHandlers(false);
            if (server)
            {
                serverA.Add<OwnershipMessage>((_, message) => first = message.Value, false);
                serverB.Add<OwnershipMessage>((_, message) => { second = message.Value; deliveries++; }, false);
                serverA.RegisterNow();
            }
            else
            {
                clientA.Add<OwnershipMessage>(message => first = message.Value);
                clientB.Add<OwnershipMessage>(message => { second = message.Value; deliveries++; });
                clientA.RegisterNow();
            }

            Deliver(server, 11);
            if (server) { serverB.RegisterNow(); serverA.Dispose(); }
            else { clientB.RegisterNow(); clientA.Dispose(); }
            Deliver(server, 22);
            Assert.That(first, Is.EqualTo(11));
            Assert.That(second, Is.EqualTo(22));
            Assert.That(deliveries, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NewSetDisposal_DoesNotRestoreOldReceiver_ExplicitReregistrationWorks(bool server)
        {
            int first = 0;
            int second = 0;
            using NetClientHandlers clientA = new NetClientHandlers(false);
            using NetClientHandlers clientB = new NetClientHandlers(false);
            using NetServerHandlers serverA = new NetServerHandlers(false);
            using NetServerHandlers serverB = new NetServerHandlers(false);
            if (server)
            {
                serverA.Add<OwnershipMessage>((_, message) => first = message.Value, false);
                serverB.Add<OwnershipMessage>((_, message) => second = message.Value, false);
                serverA.RegisterNow(); serverB.RegisterNow(); serverB.Dispose();
            }
            else
            {
                clientA.Add<OwnershipMessage>(message => first = message.Value);
                clientB.Add<OwnershipMessage>(message => second = message.Value);
                clientA.RegisterNow(); clientB.RegisterNow(); clientB.Dispose();
            }

            Assert.That(Table(server).ContainsKey(NetworkMessageId<OwnershipMessage>.Id), Is.False,
                "last registration wins; disposing it never implicitly restores its predecessor");
            if (server) { serverA.UnregisterAll(); serverA.RegisterNow(); }
            else { clientA.UnregisterAll(); clientA.RegisterNow(); }
            Deliver(server, 33);
            Assert.That(first, Is.EqualTo(33));
            Assert.That(second, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RestartedRegistration_HasANewGenerationThatOldSetsCannotRelease(bool server)
        {
            int value = 0;
            int count = 0;
            using NetClientHandlers clientA = new NetClientHandlers(false);
            using NetClientHandlers clientB = new NetClientHandlers(false);
            using NetServerHandlers serverA = new NetServerHandlers(false);
            using NetServerHandlers serverB = new NetServerHandlers(false);
            if (server)
            {
                serverA.Add<OwnershipMessage>((_, message) => { }, false);
                serverB.Add<OwnershipMessage>((_, message) => { value = message.Value; count++; }, false);
                serverA.RegisterNow(); serverB.RegisterNow();
                serverA.UnregisterAll(); serverB.UnregisterAll();
                NetworkServer.Shutdown();
                serverA.RegisterNow(); serverB.RegisterNow(); serverA.Dispose();
            }
            else
            {
                clientA.Add<OwnershipMessage>(_ => { });
                clientB.Add<OwnershipMessage>(message => { value = message.Value; count++; });
                clientA.RegisterNow(); clientB.RegisterNow();
                clientA.UnregisterAll(); clientB.UnregisterAll();
                NetworkClient.Shutdown();
                clientA.RegisterNow(); clientB.RegisterNow(); clientA.Dispose();
            }

            Deliver(server, 44);
            Assert.That(value, Is.EqualTo(44));
            Assert.That(count, Is.EqualTo(1));
        }

        private static Dictionary<ushort, NetworkMessageDelegate> Table(bool server)
        {
            Type type = server ? typeof(NetworkServer) : typeof(NetworkClient);
            FieldInfo table = type.GetField("handlers", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(table, Is.Not.Null);
            return (Dictionary<ushort, NetworkMessageDelegate>)table.GetValue(null);
        }

        private static void Deliver(bool server, int value)
        {
            // Read the actual installed Mirror delegate. This is handler lifecycle evidence, not remote admission.
            Assert.That(Table(server).TryGetValue(NetworkMessageId<OwnershipMessage>.Id,
                out NetworkMessageDelegate receive), Is.True);
            NetworkWriter writer = new NetworkWriter();
            writer.Write<int>(value);
            NetworkReader reader = new NetworkReader(writer.ToArraySegment());
            NetworkConnection peer = server
                ? (NetworkConnection)new NetworkConnectionToClient(801)
                : new NetworkConnectionToServer();
            receive(peer, reader, Channels.Reliable);
            Assert.That(reader.Remaining, Is.Zero);
        }
    }
}
#endif
