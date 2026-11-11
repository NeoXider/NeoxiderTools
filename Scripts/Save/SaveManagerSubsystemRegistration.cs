using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     Hosts <see cref="RuntimeInitializeOnLoadMethodAttribute"/> outside <see cref="SaveManager"/>
    ///     so Unity does not report “method is in a generic class” (<see cref="SaveManager"/> inherits
    ///     <c>Neo.Tools.Singleton&lt;SaveManager&gt;</c>). Behaviour matches the former <c>ResetStaticState</c> hook.
    /// </summary>
    internal static class SaveManagerSubsystemRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            SaveManager.ClearSubsystemCaches();
        }
    }
}
