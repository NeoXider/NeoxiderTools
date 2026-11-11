using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Shared legacy-first backend selection for key, mouse and touch compatibility reads.
    /// </summary>
    public static class InputBackendCompat
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        private const bool LegacyInputConfigured = true;
#elif ENABLE_INPUT_SYSTEM
        private const bool LegacyInputConfigured = false;
#else
        // WHY: legacy-only installations may not define either optional input symbol.
        private const bool LegacyInputConfigured = true;
#endif

        private static bool _legacyUnavailable;
        private static int _legacyReadFailureCount;

        /// <summary>True while the configured legacy backend has not rejected a compatibility read.</summary>
        public static bool UsesLegacyInput => LegacyInputConfigured && !_legacyUnavailable;

        /// <summary>
        ///     True when Player Settings enable the legacy Input Manager (Input Manager or Both), regardless of later
        ///     read failures. False in Input System-only projects, where every <c>UnityEngine.Input</c> read throws.
        /// </summary>
        public static bool IsLegacyInputConfigured => LegacyInputConfigured;

        internal static int LegacyReadFailureCount => _legacyReadFailureCount;

        /// <summary>
        ///     Discards the unavailable-backend cache. Also called at subsystem registration.
        ///     Device removal alone does not invalidate backend configuration.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetBackendSelection()
        {
            _legacyUnavailable = false;
            _legacyReadFailureCount = 0;
        }

        internal static void MarkLegacyUnavailable()
        {
            _legacyUnavailable = true;
            _legacyReadFailureCount++;
        }
    }
}
