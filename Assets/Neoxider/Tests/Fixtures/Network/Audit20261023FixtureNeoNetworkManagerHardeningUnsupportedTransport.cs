#if MIRROR
using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Network;
using Mirror;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023FixtureNeoNetworkManagerHardeningUnsupportedTransport : Transport
    {
        public int clientSendQueueLimit = 100;
        public override bool Available() => true;
        public override bool ClientConnected() => false;
        public override void ClientConnect(string address) { }
        public override void ClientSend(ArraySegment<byte> segment, int channelId) { }
        public override void ClientDisconnect() { }
        public override Uri ServerUri() => new Uri("unsupported://localhost");
        public override bool ServerActive() => false;
        public override void ServerStart() { }
        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId) { }
        public override void ServerDisconnect(int connectionId) { }
        public override string ServerGetClientAddress(int connectionId) => "test";
        public override void ServerStop() { }
        public override int GetMaxPacketSize(int channelId = 0) => 1024;
        public override void Shutdown() { }
    }
}
#endif
