using UnityEditor;
using UnityEditorInternal;

namespace Neo.Editor
{
    /// <summary>
    ///     Forces CustomEditor registration for package compatibility.
    ///     This ensures NeoCustomEditor works correctly when NeoxiderTools is loaded as a package.
    /// </summary>
    [InitializeOnLoad]
    public static class NeoCustomEditorRegistrar
    {
        static NeoCustomEditorRegistrar()
        {
            EditorApplication.delayCall += OnEditorReady;

            EditorApplication.projectChanged += OnProjectChanged;
        }

        private static void OnEditorReady()
        {
            // WHY: force refresh of all inspectors to ensure CustomEditor is properly registered
            InternalEditorUtility.RepaintAllViews();
        }

        private static void OnProjectChanged()
        {
            InternalEditorUtility.RepaintAllViews();
        }
    }
}
