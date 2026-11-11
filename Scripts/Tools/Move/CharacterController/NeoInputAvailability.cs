using System;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Probes whether the legacy Input Manager can be used in the current Player Settings.
    /// </summary>
    /// <remarks>
    ///     With "Active Input Handling = Input System Package (New)" every <see cref="Input" /> call throws
    ///     <see cref="InvalidOperationException" />. Probing once and caching the answer keeps the per-frame input path
    ///     free of exception handling in the common case.
    /// </remarks>
    internal static class NeoInputAvailability
    {
        private static bool? _legacyAvailable;

        /// <summary>
        ///     True when the legacy Input Manager is usable. The result is cached for the lifetime of the domain.
        /// </summary>
        public static bool IsLegacyInputAvailable()
        {
            if (_legacyAvailable.HasValue)
            {
                return _legacyAvailable.Value;
            }

            try
            {
                Vector3 _ = Input.mousePosition;
                _legacyAvailable = true;
            }
            catch (InvalidOperationException)
            {
                _legacyAvailable = false;
            }

            return _legacyAvailable.Value;
        }
    }
}
