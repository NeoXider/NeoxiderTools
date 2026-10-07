#if MIRROR
using System;
using System.Collections.Generic;
using Mirror;

namespace Neo.Network.Realtime
{
    /// <summary>
    ///     The server-side twin of <see cref="NetClientHandlers"/>: keeps a set of <c>NetworkServer</c> message handlers
    ///     registered across server restarts. <c>NetworkServer.Shutdown</c> clears every handler, so a game that
    ///     registers its command handlers once in <c>Awake</c> loses them after "host, stop, host again" and every
    ///     client command then disconnects the sender for an unknown message id.
    ///     <para>
    ///         Usage: <see cref="Add{T}"/> the handlers at construction, call <see cref="RegisterNow"/> from
    ///         <c>Awake</c>. With a <see cref="NeoNetworkManager"/> in the scene the set re-registers itself in
    ///         <c>OnStartServer</c>; without one call <see cref="Tick"/> from <c>Update</c>.
    ///     </para>
    /// </summary>
    public sealed class NetServerHandlers : IDisposable
    {
        private readonly struct Entry
        {
            public readonly Action Register;
            public readonly Action Unregister;

            public Entry(Action register, Action unregister)
            {
                Register = register;
                Unregister = unregister;
            }
        }

        private readonly List<Entry> _entries = new();
        private readonly bool _followManager;
        private bool _registered;
        private bool _wasServerActive;
        private bool _disposed;

        /// <summary>Creates an empty set.</summary>
        /// <param name="followNeoManager">Re-register automatically whenever <see cref="NeoNetworkManager"/> starts a server.</param>
        public NetServerHandlers(bool followNeoManager = true)
        {
            _followManager = followNeoManager;
            if (followNeoManager)
            {
                NeoNetworkManager.ServerSessionStarted += OnServerSessionStarted;
            }
        }

        /// <summary>True while the handlers are registered with Mirror.</summary>
        public bool IsRegistered => _registered;

        /// <summary>How many handlers are managed.</summary>
        public int Count => _entries.Count;

        /// <summary>Adds a handler for <typeparamref name="T"/>. Registers immediately when the set is already live.</summary>
        /// <param name="handler">Called with the sending connection and the message.</param>
        /// <param name="requireAuthentication">
        ///     Mirror's flag: dispatch only after the connection authenticated. Default on, because these are
        ///     commands from clients.
        /// </param>
        public void Add<T>(Action<NetworkConnectionToClient, T> handler, bool requireAuthentication = true)
            where T : struct, NetworkMessage
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Entry entry = new Entry(
                () => NetworkServer.ReplaceHandler<T>(handler, requireAuthentication),
                () => NetworkServer.UnregisterHandler<T>());
            _entries.Add(entry);
            if (_registered)
            {
                entry.Register();
            }
        }

        /// <summary>Registers every handler. Idempotent. Call from <c>Awake</c>.</summary>
        public void RegisterNow()
        {
            if (_registered || _disposed)
            {
                return;
            }

            _registered = true;
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Register();
            }
        }

        /// <summary>Drops every handler. Idempotent.</summary>
        public void UnregisterAll()
        {
            if (!_registered)
            {
                return;
            }

            _registered = false;
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Unregister();
            }
        }

        /// <summary>
        ///     Optional per-frame call for projects without a <see cref="NeoNetworkManager"/>: re-registers when a
        ///     server (re)starts, forgets the handlers after it stopped.
        /// </summary>
        public void Tick()
        {
            if (NetworkServer.active)
            {
                if (!_wasServerActive)
                {
                    _registered = false;
                }

                _wasServerActive = true;
                RegisterNow();
            }
            else if (_wasServerActive)
            {
                _wasServerActive = false;
                _registered = false;
            }
        }

        /// <summary>Unregisters the handlers and stops following the manager.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            UnregisterAll();
            _disposed = true;
            if (_followManager)
            {
                NeoNetworkManager.ServerSessionStarted -= OnServerSessionStarted;
            }
        }

        private void OnServerSessionStarted()
        {
            if (_disposed)
            {
                return;
            }

            _registered = false;
            _wasServerActive = true;
            RegisterNow();
        }
    }
}
#endif
