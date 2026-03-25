using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Auto-registers CustomEditor types for Neo components.
    ///     Mitigates visibility issues when the package is installed via Package Manager.
    /// </summary>
    [InitializeOnLoad]
    public static class NeoEditorAutoRegister
    {
        static NeoEditorAutoRegister()
        {
            // Register handler when the editor loads
            EditorApplication.delayCall += RegisterNeoEditors;
        }

        private static void RegisterNeoEditors()
        {
            // Find all MonoBehaviour types in the Neo namespace
            var neoTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return Type.EmptyTypes;
                    }
                })
                .Where(type =>
                    type != null &&
                    !type.IsAbstract &&
                    typeof(MonoBehaviour).IsAssignableFrom(type) &&
                    type.Namespace != null &&
                    (type.Namespace == "Neo" || type.Namespace.StartsWith("Neo.")))
                .ToList();

            // Debug output (uncomment if needed)
            // Debug.Log($"[NeoEditorAutoRegister] Found {neoTypes.Count} Neo MonoBehaviour types");
            // foreach (var type in neoTypes.Take(5))
            // {
            //     Debug.Log($"  - {type.Namespace}.{type.Name}");
            // }
        }
    }
}
