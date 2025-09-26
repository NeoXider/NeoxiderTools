using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace Neo.Editor
{
    /// <summary>
    /// Custom inspector drawer that handles automatic component and resource assignment based on attributes.
    /// Supports finding components in scene, on GameObject, and loading from Resources.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class NeoCustomEditor : CustomEditorBase
    {
        protected override void ProcessAttributeAssignments()
        {
            var targetObject = target as MonoBehaviour;
            if (targetObject == null)
                return;

            // Process component attributes
            ComponentDrawer.ProcessComponentAttributes(targetObject);

            // Process resource attributes
            ResourceDrawer.ProcessResourceAttributes(targetObject);
        }
    }
}