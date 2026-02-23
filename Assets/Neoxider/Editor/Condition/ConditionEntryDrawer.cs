using Neo.Condition;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Condition
{
    /// <summary>
    ///     Property drawer for <see cref="ConditionEntry" />. Ensures the same condition UI can be used
    ///     in NeoCondition inspector and in StateMachine transition editor (e.g. ConditionEntryPredicate).
    /// </summary>
    [CustomPropertyDrawer(typeof(ConditionEntry))]
    public sealed class ConditionEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
