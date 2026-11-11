namespace Neo.Network
{
    /// <summary>
    ///     Pure decision table for <see cref="NetworkAuthorityMode" />, free of Mirror and scene dependencies.
    ///     A transport adapter gathers the facts about the caller; this class decides whether the action may run.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><see cref="NetworkAuthorityMode.None" /> allows every caller.</item>
    ///         <item><see cref="NetworkAuthorityMode.OwnerOnly" /> allows direct server code, the host's local connection and the owning client.</item>
    ///         <item><see cref="NetworkAuthorityMode.ServerOnly" /> allows direct server code and the host's local connection.</item>
    ///         <item>An unnamed value (an enum cast or corrupted serialized data) is rejected for every caller, including the server.</item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code>
    /// bool allowed = NetworkAuthorityPolicy.IsAuthorized(
    ///     NetworkAuthorityMode.OwnerOnly,
    ///     isDirectServerCall: sender == null,
    ///     isLocalHostSender: sender == NetworkServer.localConnection,
    ///     senderOwnsObject: identity != null &amp;&amp; identity.connectionToClient != null &amp;&amp;
    ///                       identity.connectionToClient == sender);
    /// </code>
    /// </example>
    public static class NetworkAuthorityPolicy
    {
        /// <summary>True when <paramref name="mode" /> is one of the named policies.</summary>
        public static bool IsKnown(NetworkAuthorityMode mode)
        {
            return mode == NetworkAuthorityMode.None ||
                   mode == NetworkAuthorityMode.OwnerOnly ||
                   mode == NetworkAuthorityMode.ServerOnly;
        }

        /// <summary>Decides whether a caller described by the given facts may trigger an action under <paramref name="mode" />.</summary>
        /// <param name="mode">Configured policy.</param>
        /// <param name="isDirectServerCall">The action runs from server/host code without a remote sender.</param>
        /// <param name="isLocalHostSender">The sender is the host's own local client connection.</param>
        /// <param name="senderOwnsObject">The sender is the client connection that owns the target object.</param>
        /// <returns>True when the action is allowed; always false for an unnamed policy value.</returns>
        public static bool IsAuthorized(NetworkAuthorityMode mode, bool isDirectServerCall, bool isLocalHostSender,
            bool senderOwnsObject)
        {
            switch (mode)
            {
                case NetworkAuthorityMode.None:
                    return true;
                case NetworkAuthorityMode.ServerOnly:
                    return isDirectServerCall || isLocalHostSender;
                case NetworkAuthorityMode.OwnerOnly:
                    return isDirectServerCall || isLocalHostSender || senderOwnsObject;
                default:
                    return false;
            }
        }
    }
}
