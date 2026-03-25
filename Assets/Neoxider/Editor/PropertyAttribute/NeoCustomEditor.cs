using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Custom inspector drawer that handles automatic component and resource assignment based on attributes.
    ///     Supports finding components in scene, on GameObject, and loading from Resources.
    ///     Works with Odin Inspector by using DrawDefaultInspector when Odin is active.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class NeoCustomEditor : CustomEditorBase
    {
        // Debug: verify the editor instance is constructed
        static NeoCustomEditor()
        {
            //Debug.Log("[NeoCustomEditor] Class loaded and registered as CustomEditor for MonoBehaviour");
        }

        // Debug: verify the editor instance is constructed

        protected override void ProcessAttributeAssignments()
        {
            var targetObject = target as MonoBehaviour;
            if (targetObject == null)
            {
                return;
            }

            // Process component attributes
            ComponentDrawer.ProcessComponentAttributes(targetObject);

            // Process resource attributes
            ResourceDrawer.ProcessResourceAttributes(targetObject);
        }
    }
}
