using UnityEditor;
using UnityEngine;

namespace Flexalon.Editor
{
    [CustomEditor(typeof(FlexalonComponent)), CanEditMultipleObjects]
    public class FlexalonComponentEditor : UnityEditor.Editor
    {
        public static void Create<T>(string name, Object context) where T : MonoBehaviour
        {
            FlexalonEditor.Create();
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);

            if (context is GameObject)
            {
                go.transform.SetParent((context as GameObject).transform, false);
            }

            Flexalon.AddComponent<T>(go);
            Selection.activeGameObject = go;
        }

        protected void ForceUpdateButton()
        {
            if (GUILayout.Button("Force Update"))
            {
                foreach (var target in targets)
                {
                    ForceUpdate(target as FlexalonComponent);
                }
            }
        }

        protected void ApplyModifiedProperties()
        {
            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var target in targets)
                {
                    Record(target as FlexalonComponent);
                    (target as FlexalonComponent).MarkDirty();
                }

                Flexalon.GetOrCreate().UpdateDirtyNodes();
            }
        }

        protected void Record(FlexalonComponent script)
        {
            Undo.RecordObject(script, "Record Component Edit");
            PrefabUtility.RecordPrefabInstancePropertyModifications(script);

            if (script.Node != null && script.Node.Result)
            {
                Undo.RecordObject(script.Node.Result, "Record Component Edit");
                PrefabUtility.RecordPrefabInstancePropertyModifications(script.Node.Result);
            }

            Flexalon.RecordFrameChanges = true;
        }

        protected void MarkDirty(FlexalonComponent script)
        {
            script.MarkDirty();
            Flexalon.GetOrCreate().UpdateDirtyNodes();
        }

        protected void ForceUpdate(FlexalonComponent script)
        {
            Record(script);
            script.ForceUpdate();
        }
    }
}