namespace Neo.Tools
{
    /// <summary>
    ///     Which input API the Neoxider character components read from.
    /// </summary>
    public enum NeoInputBackend
    {
        /// <summary>Use the New Input System when it is available, otherwise fall back to the legacy Input Manager.</summary>
        AutoPreferNew,

        /// <summary>Prefer the New Input System; falls back to the legacy Input Manager when the package is missing.</summary>
        NewInputSystem,

        /// <summary>Always read from the legacy Input Manager (<see cref="UnityEngine.Input" />).</summary>
        LegacyInputManager
    }
}
