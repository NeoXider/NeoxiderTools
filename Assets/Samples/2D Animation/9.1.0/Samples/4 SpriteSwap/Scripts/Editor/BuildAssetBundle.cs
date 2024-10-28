using UnityEditor;

namespace Unity.U2D.Animation.Sample
{
    // Ensure class initializer is called whenever scripts recompile
    [InitializeOnLoadAttribute]
    internal static class BuildAssetBundle
    {
        // Register an event handler when the class is initialized
        static BuildAssetBundle()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChange;
        }

        private static void PlayModeStateChange(PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingEditMode)
            {
                LoadSwapDLC.BuildAssetBundles();
            }
        }
    }
}
