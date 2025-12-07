using UnityEditor;
using UnityEngine;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonCloner)), CanEditMultipleObjects]
    public class FlexalonClonerEditor : UnityEditor.Editor
    {
        private SerializedProperty _objects;
        private SerializedProperty _cloneType;
        private SerializedProperty _count;
        private SerializedProperty _randomSeed;
        private SerializedProperty _dataSource;

        [MenuItem("GameObject/Flexalon/Cloner")]
        public static void Create(MenuCommand command)
        {
            FlexalonComponentEditor.Create<FlexalonCloner>("Cloner", command.context);
        }

        void OnEnable()
        {
            _objects = serializedObject.FindProperty("_objects");
            _cloneType = serializedObject.FindProperty("_cloneType");
            _count = serializedObject.FindProperty("_count");
            _randomSeed = serializedObject.FindProperty("_randomSeed");
            _dataSource = serializedObject.FindProperty("_dataSource");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_objects, true);
            EditorGUILayout.PropertyField(_cloneType);

            if ((target as FlexalonCloner).DataSource == null)
            {
                EditorGUILayout.PropertyField(_count);
            }

            if ((target as FlexalonCloner).CloneType == FlexalonCloner.CloneTypes.Random)
            {
                EditorGUILayout.PropertyField(_randomSeed);
            }

            EditorGUILayout.PropertyField(_dataSource);

            if (serializedObject.ApplyModifiedProperties())
            {
                if (Application.isPlaying)
                {
                    foreach (var target in targets)
                    {
                        (target as FlexalonCloner).MarkDirty();
                    }
                }
            }
        }
    }
}