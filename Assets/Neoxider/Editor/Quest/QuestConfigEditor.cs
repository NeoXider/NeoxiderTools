using Neo.Quest;
using UnityEditor;

namespace Neo.Editor.Quest
{
    [CustomEditor(typeof(QuestConfig))]
    [CanEditMultipleObjects]
    public sealed class QuestConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}