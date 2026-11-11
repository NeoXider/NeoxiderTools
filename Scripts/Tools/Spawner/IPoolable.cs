namespace Neo.Tools
{
    /// <summary>
    ///     Interface for objects managed by the pooling system.
    ///     Implement on your components to receive callbacks at pool lifecycle stages.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        ///     Called once when the pool creates a new instance (via Instantiate).
        ///     Use for caching components (GetComponent).
        /// </summary>
        void OnPoolCreate();

        /// <summary>
        ///     Called each time the object is taken from the pool.
        ///     Reset state here (health, timers, etc.).
        /// </summary>
        void OnPoolGet();

        /// <summary>
        ///     Called each time the object is returned to the pool.
        ///     Disable logic, stop effects, etc.
        /// </summary>
        void OnPoolRelease();
    }
}
