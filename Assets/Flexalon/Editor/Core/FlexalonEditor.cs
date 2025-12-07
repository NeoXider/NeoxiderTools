using UnityEditor;
using UnityEngine;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(Flexalon))]
    public class FlexalonEditor : UnityEditor.Editor
    {
        private SerializedProperty _updateInEditMode;
        private SerializedProperty _updateInPlayMode;
        private SerializedProperty _skipInactiveObjects;
        private SerializedProperty _inputProvider;

        public static void Create()
        {
            if (Flexalon.TryGetOrCreate(out var flexalon))
            {
                Undo.RegisterCreatedObjectUndo(flexalon.gameObject, "Create Flexalon");
            }
        }

        void OnEnable()
        {
            _updateInEditMode = serializedObject.FindProperty("_updateInEditMode");
            _updateInPlayMode = serializedObject.FindProperty("_updateInPlayMode");
            _skipInactiveObjects = serializedObject.FindProperty("_skipInactiveObjects");
            _inputProvider = serializedObject.FindProperty("_inputProvider");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if ((Application.isPlaying && !(target as Flexalon).UpdateInPlayMode) ||
                (!Application.isPlaying && !(target as Flexalon).UpdateInEditMode))
            {
                if (GUILayout.Button("Update"))
                {
                    Undo.RecordObject(target, "Update");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    var flexalon = (target as Flexalon);
                    Flexalon.RecordFrameChanges = true;
                    flexalon.UpdateDirtyNodes();
                }
            }

            if (GUILayout.Button("Force Update"))
            {
                Undo.RecordObject(target, "Force Update");
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                var flexalon = (target as Flexalon);
                Flexalon.RecordFrameChanges = true;
                flexalon.ForceUpdate();
            }

            EditorGUILayout.PropertyField(_updateInEditMode);
            EditorGUILayout.PropertyField(_updateInPlayMode);
            EditorGUILayout.PropertyField(_skipInactiveObjects);
            EditorGUILayout.PropertyField(_inputProvider);

            if (serializedObject.ApplyModifiedProperties())
            {
                EditorApplication.QueuePlayerLoopUpdate();
            }

            EditorGUILayout.HelpBox("You should only have one Flexalon component in the scene. If you create a new one, disable and re-enable all flexalon components or restart Unity.", MessageType.Info);
        }
    }
}