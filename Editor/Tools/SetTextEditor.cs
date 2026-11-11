using Neo.Editor;
using Neo.NoCode;
using Neo.Tools;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Tools
{
    /// <summary>
    ///     <see cref="SetText"/> is display-only; float→text binding lives on <see cref="NoCodeBindText"/> (same
    ///     <see cref="ComponentFloatBinding"/> UI as SetProgress). We surface that here so the inspector matches user
    ///     expectations.
    /// </summary>
    [CustomEditor(typeof(SetText))]
    [CanEditMultipleObjects]
    public sealed class SetTextEditor : CustomEditorBase
    {
        protected override void ProcessAttributeAssignments()
        {
            if (target is MonoBehaviour mb)
            {
                ComponentDrawer.ProcessComponentAttributes(mb);
                ResourceDrawer.ProcessResourceAttributes(mb);
            }
        }

        protected override void OnAfterDrawNeoProperties()
        {
            if (targets == null || targets.Length != 1)
            {
                return;
            }

            Component host = target as Component;
            if (host == null)
            {
                return;
            }

            if (host.GetComponent<NoCodeBindText>() != null)
            {
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "To drive this text from a float on another component (same Source → Component → Member flow as NeoCondition / SetProgress), add NoCode Bind Text to this GameObject.",
                MessageType.Info);

            if (GUILayout.Button("Add NoCode Bind Text"))
            {
                Undo.AddComponent<NoCodeBindText>(host.gameObject);
            }
        }
    }
}
