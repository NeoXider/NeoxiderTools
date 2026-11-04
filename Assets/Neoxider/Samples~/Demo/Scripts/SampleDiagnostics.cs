using UnityEngine;

namespace Neo.Demo
{
    /// <summary>
    ///     Editor-only diagnostics for sample scripts. Player builds compile the calls away, so samples never
    ///     spam a shipped game's log. Callers keep their own "warn once" flags.
    /// </summary>
    internal static class SampleDiagnostics
    {
        public static void Log(string message, Object context = null)
        {
#if UNITY_EDITOR
            Debug.Log(message, context);
#endif
        }

        public static void Warn(string message, Object context = null)
        {
#if UNITY_EDITOR
            Debug.LogWarning(message, context);
#endif
        }
    }
}
