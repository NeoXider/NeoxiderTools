using Flexalon.Editor;
using UnityEditor;

namespace Flexalon.Samples
{
    [CustomEditor(typeof(CustomLayout)), CanEditMultipleObjects]
    public class CustomLayoutEditor : FlexalonComponentEditor
    {
        public override void OnInspectorGUI()
        {
            ForceUpdateButton();
            SerializedObject so = serializedObject;
            EditorGUILayout.PropertyField(so.FindProperty("_gap"), true);
            ApplyModifiedProperties();
        }
    }
}