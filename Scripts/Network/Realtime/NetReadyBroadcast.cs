#if MIRROR
using Mirror;

namespace Neo.Network.Realtime
{
    /// <summary>
    ///     Server-side "send to everyone who can actually receive it".
    ///     <para>
    ///         A client that has not finished the Mirror ready handshake has no spawned objects yet, so a broadcast frame
    ///         reaches it before its handlers can exist and Mirror disconnects it for an unknown message id.
    ///         <c>isReady</c> flips on <c>ReadyMessage</c>, but the spawn burst only goes out once the connection owns a
    ///         player object (<c>AddPlayer</c>), so a non-null <c>identity</c> is the real "spawned" signal. Every game
    ///         that broadcasts anything outside an RPC re-learns this the hard way; <c>NetworkServer.SendToReady</c>
    ///         only checks the first flag.
    ///     </para>
    ///     <para>
    ///         A host's own loopback client is a connection too. A message the host already acted on locally (an event
    ///         the authority published itself) must not be delivered to that connection a second time, hence
    ///         <c>includeLocalHost</c>.
    ///     </para>
    /// </summary>
    public static class NetReadyBroadcast
    {
        /// <summary>True when a message sent now would be dispatched on this connection (ready AND owns a player).</summary>
        public static bool CanReceive(NetworkConnectionToClient connection)
        {
            return NeoNetworkState.IsConnectionSpawned(connection);
        }

        /// <summary>Counts the connections a broadcast would reach.</summary>
        /// <param name="includeLocalHost">Count the host's own loopback connection.</param>
        public static int CountReady(bool includeLocalHost = true)
        {
            int count = 0;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (!CanReceive(connection) || (!includeLocalHost && connection is LocalConnectionToClient))
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        /// <summary>
        ///     Sends to one connection only if <see cref="CanReceive"/>. Returns <see langword="false"/> when the
        ///     message was not sent.
        /// </summary>
        public static bool SendTo<T>(NetworkConnectionToClient connection, T message, int channelId = Channels.Reliable)
            where T : struct, NetworkMessage
        {
            if (!CanReceive(connection))
            {
                return false;
            }

            connection.Send(message, channelId);
            return true;
        }

        /// <summary>Sends to every ready, spawned connection. Returns how many connections it was sent to.</summary>
        /// <param name="message">The message to send.</param>
        /// <param name="channelId">Mirror channel, <c>Channels.Reliable</c> by default.</param>
        /// <param name="includeLocalHost">
        ///     Deliver to the host's own loopback connection. Pass <see langword="false"/> for events the authority
        ///     already acted on locally.
        /// </param>
        public static int ToReadyClients<T>(T message, int channelId = Channels.Reliable, bool includeLocalHost = true)
            where T : struct, NetworkMessage
        {
            int sent = 0;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (!CanReceive(connection))
                {
                    continue;
                }

                if (!includeLocalHost && connection is LocalConnectionToClient)
                {
                    continue;
                }

                connection.Send(message, channelId);
                sent++;
            }

            return sent;
        }
    }
}
#endif
