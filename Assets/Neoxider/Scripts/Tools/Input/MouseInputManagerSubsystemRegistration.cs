using UnityEngine;

/// <seealso cref="MouseInputManager"/>
internal static class MouseInputManagerSubsystemRegistration
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticsOnReload()
    {
        MouseInputManager.ResetSubsystemPollingState();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        MouseInputManager.EnableAutoCreateForRuntime();
    }
}
