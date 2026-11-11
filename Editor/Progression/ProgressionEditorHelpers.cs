using System.Collections.Generic;
using UnityEditor;

namespace Neo.Editor.Progression
{
    internal static class ProgressionEditorHelpers
    {
        public static void DrawValidationBlock(IReadOnlyList<string> issues, string okMessage)
        {
            if (issues == null || issues.Count == 0)
            {
                EditorGUILayout.HelpBox(okMessage, MessageType.Info);
                return;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                EditorGUILayout.HelpBox(issues[i], MessageType.Warning);
            }
        }
    }
}
