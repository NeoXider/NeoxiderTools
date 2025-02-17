using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Neo
{
    [CustomPropertyDrawer(typeof(ColorAttribute))]
    public class ColorAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ColorAttribute colorAttribute = (ColorAttribute)attribute;

            Color originalColor = GUI.color;

            GUI.color = colorAttribute.color;

            EditorGUI.PropertyField(position, property, label);

            GUI.color = originalColor;
        }
    }
}
#endif