namespace Neo.Tools
{
    /// <summary>
    ///     Pure decision helper shared by the Neoxider character input components: given the configured backend and what
    ///     is actually available in the current Player Settings, decide whether to read from the New Input System.
    /// </summary>
    /// <remarks>
    ///     Kept free of Unity API calls so the rule is unit-testable without entering Play Mode.
    /// </remarks>
    public static class NeoInputBackendResolver
    {
        /// <summary>
        ///     Decides whether the New Input System should be used.
        /// </summary>
        /// <param name="backend">Backend selected in the Inspector.</param>
        /// <param name="newInputAvailable">True when the New Input System package is present.</param>
        /// <param name="legacyAvailable">True when the legacy Input Manager is enabled in Player Settings.</param>
        /// <returns>
        ///     True to read from the New Input System. <see cref="NeoInputBackend.LegacyInputManager" /> always returns
        ///     false so an explicit choice is never silently overridden; the other modes fall back to legacy only when
        ///     the New Input System is missing and legacy is actually usable.
        /// </returns>
        public static bool ShouldUseNewInput(NeoInputBackend backend, bool newInputAvailable, bool legacyAvailable)
        {
            if (backend == NeoInputBackend.LegacyInputManager)
            {
                return false;
            }

            if (newInputAvailable)
            {
                return true;
            }

            // WHY: with "Active Input Handling = Input System Package (New)" every legacy Input call throws, so
            // reading legacy would break the controller entirely — the New Input path is the only survivable one.
            return !legacyAvailable;
        }
    }
}
