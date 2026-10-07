#if MIRROR
using System;
using Mirror;

namespace Neo.Network.Realtime
{
    /// <summary>
    ///     A typed reliable event channel: the authority <see cref="Publish"/>es an event, it travels as one reliable
    ///     Mirror message to every ready, spawned <b>remote</b> client, and <see cref="Received"/> fires on those pure
    ///     clients only.
    ///     <para>
    ///         The host never receives its own published event back (the authority already acted on it locally), so a
    ///         kill-feed line or a sting is not doubled on the host. Handler lifetime across reconnects is owned by the
    ///         <see cref="NetClientHandlers"/> the channel registers with, and a publish only reaches connections that
    ///         can receive it (<see cref="NeoNetworkState.IsConnectionSpawned"/>), so an "unknown message id"
    ///         disconnect cannot happen.
    ///     </para>
    /// </summary>
    /// <typeparam name="T">The Mirror message type carrying the event (a plain struct of primitives).</typeparam>
    /// <example>
    ///     <code>
    ///     public struct KillEvent : NetworkMessage { public int Killer; public int Victim; }
    ///
    ///     NetClientHandlers handlers = new NetClientHandlers();
    ///     NetEventChannel&lt;KillEvent&gt; kills = new NetEventChannel&lt;KillEvent&gt;(handlers);
    ///     kills.Received += e =&gt; KillFeed.Show(e.Killer, e.Victim);   // pure clients
    ///     handlers.RegisterNow();
    ///
    ///     // authority
    ///     OnKill(killer, victim);                                       // act locally first
    ///     kills.Publish(new KillEvent { Killer = killer, Victim = victim });
    ///     </code>
    /// </example>
    public sealed class NetEventChannel<T> where T : struct, NetworkMessage
    {
        private readonly int _channelId;

        /// <summary>
        ///     Creates the channel and adds its client handler to <paramref name="handlers"/> (call
        ///     <see cref="NetClientHandlers.RegisterNow"/> from <c>Awake</c>).
        /// </summary>
        /// <param name="handlers">The registry that keeps the handler alive across reconnects.</param>
        /// <param name="channelId">Mirror channel; <c>Channels.Reliable</c> by default.</param>
        public NetEventChannel(NetClientHandlers handlers, int channelId = Channels.Reliable)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            _channelId = channelId;
            handlers.Add<T>(OnMessage);
        }

        /// <summary>Raised on a remote client for every event the authority published. Never raised on the host.</summary>
        public event Action<T> Received;

        /// <summary>How many events were received since creation (a cheap health counter).</summary>
        public int ReceivedCount { get; private set; }

        /// <summary>How many events this peer published since creation.</summary>
        public int PublishedCount { get; private set; }

        /// <summary>
        ///     Authority: sends <paramref name="message"/> to every ready, spawned remote client. Does nothing when no
        ///     server is active. The host's own loopback connection is skipped.
        /// </summary>
        /// <returns>How many clients it was sent to.</returns>
        public int Publish(T message)
        {
            if (!NetworkServer.active)
            {
                return 0;
            }

            PublishedCount++;
            return NetReadyBroadcast.ToReadyClients(message, _channelId, includeLocalHost: false);
        }

        /// <summary>
        ///     Authority: sends <paramref name="message"/> to one connection, if it can receive it.
        /// </summary>
        /// <returns><see langword="true"/> when the message was sent.</returns>
        public bool PublishTo(NetworkConnectionToClient connection, T message)
        {
            if (!NetworkServer.active || connection is LocalConnectionToClient)
            {
                return false;
            }

            if (!NetReadyBroadcast.SendTo(connection, message, _channelId))
            {
                return false;
            }

            PublishedCount++;
            return true;
        }

        private void OnMessage(T message)
        {
            // Defensive: a host's loopback never carries these (Publish skips it), but a custom relay might.
            if (NetworkServer.active)
            {
                return;
            }

            ReceivedCount++;
            try
            {
                Received?.Invoke(message);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }
    }
}
#endif
