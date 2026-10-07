#if MIRROR
using System;
using System.Collections.Generic;
using Mirror;

namespace Neo.Network.Realtime
{
    /// <summary>
    ///     Keeps a set of client-side Mirror message handlers registered across disconnects and reconnects.
    ///     <para>
    ///         Two Mirror behaviours bite every game: (1) a message that arrives for a type with no handler disconnects
    ///         the client, and frames start flowing while the client is still connecting, so the handlers must exist
    ///         <b>before</b> the connection does; (2) <c>NetworkClient.Shutdown</c> clears every handler, so after a
    ///         reconnect they are gone. The usual fix is a hand-rolled "registered" flag plus an <c>Update</c> that
    ///         watches <c>NetworkClient.active</c>, written again in every component that owns a handler. This is that
    ///         code once.
    ///     </para>
    ///     <para>
    ///         Usage: <see cref="Add{T}"/> the handlers at construction, call <see cref="RegisterNow"/> from
    ///         <c>Awake</c>. With a <see cref="NeoNetworkManager"/> in the scene nothing else is needed: the set
    ///         re-registers itself in <c>OnStartClient</c>, before any message can arrive. Without one (a custom
    ///         <c>NetworkManager</c>) call <see cref="Tick"/> from <c>Update</c>. Dispose it when its owner is destroyed.
    ///     </para>
    /// </summary>
    public sealed class NetClientHandlers : IDisposable
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
        private bool _wasClientActive;
        private bool _disposed;

        /// <summary>Creates an empty set.</summary>
        /// <param name="followNeoManager">
        ///     Re-register automatically whenever <see cref="NeoNetworkManager"/> starts a client session. Leave on.
        /// </param>
        public NetClientHandlers(bool followNeoManager = true)
        {
            _followManager = followNeoManager;
            if (followNeoManager)
            {
                NeoNetworkManager.ClientSessionStarted += OnClientSessionStarted;
            }
        }

        /// <summary>True while the handlers are registered with Mirror.</summary>
        public bool IsRegistered => _registered;

        /// <summary>How many handlers are managed.</summary>
        public int Count => _entries.Count;

        /// <summary>Adds a handler for <typeparamref name="T"/>. Registers immediately when the set is already live.</summary>
        /// <param name="handler">Called on the main thread for every received message of that type.</param>
        /// <param name="requireAuthentication">Mirror's flag: dispatch only after the connection authenticated.</param>
        public void Add<T>(Action<T> handler, bool requireAuthentication = false) where T : struct, NetworkMessage
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Entry entry = new Entry(
                () => NetworkClient.ReplaceHandler(handler, requireAuthentication),
                () => NetworkClient.UnregisterHandler<T>());
            _entries.Add(entry);
            if (_registered)
            {
                entry.Register();
            }
        }

        /// <summary>
        ///     Registers every handler. Call from <c>Awake</c>, before any transport can deliver: an Update-gated
        ///     registration loses the race against the first frames. Idempotent.
        /// </summary>
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
        ///     Optional per-frame call for projects without a <see cref="NeoNetworkManager"/>. While a client is active
        ///     it makes sure the handlers are registered (re-registering after a reconnect); after a real disconnect it
        ///     forgets them, because Mirror has already cleared them. It deliberately does nothing while a client has
        ///     never been active, so the registration done in <c>Awake</c> survives until the first connection.
        /// </summary>
        public void Tick()
        {
            if (NetworkClient.active)
            {
                if (!_wasClientActive)
                {
                    // a new session: Mirror cleared the old handlers at shutdown, so apply them again
                    _registered = false;
                }

                _wasClientActive = true;
                RegisterNow();
            }
            else if (_wasClientActive)
            {
                _wasClientActive = false;
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
                NeoNetworkManager.ClientSessionStarted -= OnClientSessionStarted;
            }
        }

        private void OnClientSessionStarted()
        {
            if (_disposed)
            {
                return;
            }

            // Mirror wiped the handler table on the previous shutdown; replace-registering is idempotent.
            _registered = false;
            _wasClientActive = true;
            RegisterNow();
        }
    }
}
#endif
