using UnityEngine;

namespace Neo.Tools
{
    /// <seealso cref="SwipeController"/>
    internal static class SwipeControllerSubsystemRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnReload()
        {
            SwipeController.ResetStaticStateForRuntime();
        }
    }
}
