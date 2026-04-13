namespace Neo.Network
{
    /// <summary>
    /// Context in which a network-aware event (like physics triggers or interaction) should execute.
    /// </summary>
    public enum NetworkExecutionMode
    {
        /// <summary>Execute everywhere (default for standard Unity objects).</summary>
        Everywhere = 0,
        
        /// <summary>Execute only if the current instance is the Server (or Host). Use this for damage, spawning, etc.</summary>
        ServerOnly = 1,
        
        /// <summary>Execute only if the current instance is the Client that has authority over the triggering object.</summary>
        LocalPlayerOnly = 2
    }
}
