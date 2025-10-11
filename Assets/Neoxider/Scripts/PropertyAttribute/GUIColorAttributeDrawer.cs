#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Neo
{
    /// <summary>
    ///     Custom property drawer for GUIColorAttribute.
    ///     Colors the background of fields in the Unity Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(GUIColorAttribute))]
    public class GUIColorAttributeDrawer : PropertyDrawer
    {
        /// <summary>
        ///     Draws the property with the specified color in the Unity Inspector
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for</param>
        /// <param name="label">The label to show on the property</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get the color attribute
            var colorAttribute = (GUIColorAttribute)attribute;

            // Store the original GUI color to restore it later
            var originalColor = GUI.color;

            // Set the color from the attribute
            GUI.color = colorAttribute.color;

            // Draw the property field with the custom color
            EditorGUI.PropertyField(position, property, label);

            // Restore the original GUI color
            GUI.color = originalColor;
        }
    }
}
#endif