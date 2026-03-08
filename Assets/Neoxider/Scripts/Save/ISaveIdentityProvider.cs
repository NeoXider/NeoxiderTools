namespace Neo.Save
{
    /// <summary>
    /// Provides a custom stable identity for saveable scene components.
    /// </summary>
    public interface ISaveIdentityProvider
    {
        /// <summary>
        /// Gets the persistent identity used by the save system for this component.
        /// </summary>
        string SaveIdentity { get; }
    }
}
