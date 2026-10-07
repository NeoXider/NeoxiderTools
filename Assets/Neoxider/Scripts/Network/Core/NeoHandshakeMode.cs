namespace Neo.Network
{
    /// <summary>
    ///     How <see cref="NeoNetworkManager"/> completes the client side of the Mirror handshake
    ///     (<c>Ready</c>, then <c>AddPlayer</c>) after the connection is established.
    /// </summary>
    /// <remarks>
    ///     A server only sends spawned objects to <i>ready</i> connections, and only bursts the spawn list once the
    ///     connection owns a player object. A client that skips either step connects "successfully" and then silently
    ///     receives nothing.
    /// </remarks>
    public enum NeoHandshakeMode
    {
        /// <summary>
        ///     Ready is always sent. AddPlayer is sent when Mirror's <c>Auto Create Player</c> is on or a scene player
        ///     template is used, exactly once per connection, including after a server scene change.
        ///     This is Mirror's own behavior made reliable; it is the default.
        /// </summary>
        Auto = 0,

        /// <summary>
        ///     Ready and AddPlayer are always sent, even with <c>Auto Create Player</c> off. Use it when the game builds
        ///     its player object itself in an overridden <c>OnServerAddPlayer</c> (no Player Prefab) but still needs the
        ///     connection to own an object so the spawn burst and broadcasts reach it.
        /// </summary>
        Always = 1,

        /// <summary>
        ///     The manager adds nothing to Mirror's stock <c>OnClientConnect</c> / <c>OnClientSceneChanged</c>.
        ///     Pick it when the game drives Ready/AddPlayer entirely by hand.
        /// </summary>
        Manual = 2
    }
}
