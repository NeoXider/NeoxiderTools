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
            var colorAttribute = (GUIColorAttribute)attribute;

            Color originalColor = GUI.color;

            GUI.color = colorAttribute.color;

            EditorGUI.PropertyField(position, property, label);

            GUI.color = originalColor;
        }

        public static Color GetColor(GUIColorAttribute colorAttribute, Color fallback)
        {
            return colorAttribute != null ? colorAttribute.color : fallback;
        }
    }
}
#endif
