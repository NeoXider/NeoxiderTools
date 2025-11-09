using UnityEditor;
using UnityEngine;

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
            // Register editor refresh on Unity load
            EditorApplication.delayCall += OnEditorReady; 
        }

        private static void OnEditorReady()
        {
            // Force refresh of all inspectors to ensure CustomEditor is properly registered
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}

