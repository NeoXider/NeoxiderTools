using Neo.Rpg;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Rpg
{
    [CustomPropertyDrawer(typeof(RpgStatId))]
    public sealed class RpgStatIdDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty preset = property.FindPropertyRelative("preset");
            bool custom = preset != null && preset.enumValueIndex == (int)RpgStatPreset.Custom;
            return custom
                ? EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing
                : EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty preset = property.FindPropertyRelative("preset");
            SerializedProperty customId = property.FindPropertyRelative("customId");

            EditorGUI.BeginProperty(position, label, property);
            Rect presetRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(presetRect, preset, label);

            if (preset != null && preset.enumValueIndex == (int)RpgStatPreset.Custom)
            {
                Rect customRect = new(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(customRect, customId, new GUIContent("Custom Id"));
            }

            EditorGUI.EndProperty();
        }
    }
}
