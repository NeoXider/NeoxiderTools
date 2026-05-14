namespace Neo.Network
{
    /// <summary>
    ///     Manual authority policy for NoCode network actions.
    ///     Commands still use Mirror's requiresAuthority = false so scene objects work without ownership.
    /// </summary>
    public enum NetworkAuthorityMode
    {
        /// <summary>Any client/server may trigger the action. Default for simple NoCode multiplayer.</summary>
        None = 0,

        /// <summary>Only the owning client, host local connection, or direct server code may trigger the action.</summary>
        OwnerOnly = 1,

        /// <summary>Only direct server/host code may trigger the action; remote client commands are rejected.</summary>
        ServerOnly = 2
    }
}
