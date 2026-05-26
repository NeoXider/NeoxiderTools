using UnityEngine;

namespace Neo.Demo
{
    internal static class SampleDiagnostics
    {
        public static void Log(string message, Object context = null)
        {
#if UNITY_EDITOR
            Debug.Log(message, context);
#endif
        }
    }
}
