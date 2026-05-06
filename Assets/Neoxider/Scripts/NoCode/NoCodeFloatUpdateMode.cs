namespace Neo.NoCode
{
    /// <summary>
    ///     How often the binding refreshes the target UI.
    /// </summary>
    public enum NoCodeFloatUpdateMode
    {
        /// <summary>Resolve and apply once when enabled.</summary>
        Once = 0,

        /// <summary>
        ///     Subscribe to <see cref="Neo.Reactive.ReactivePropertyFloat.OnChanged"/> when the member holds
        ///     <see cref="Neo.Reactive.ReactivePropertyFloat"/>.
        /// </summary>
        Reactive = 1,

        /// <summary>Call refresh every <see cref="LateUpdate"/> when poll is enabled.</summary>
        Poll = 2
    }
}
