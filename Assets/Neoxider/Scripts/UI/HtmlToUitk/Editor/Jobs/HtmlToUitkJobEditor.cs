using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Tooling
{
    [CustomEditor(typeof(HtmlToUitkJob))]
    public sealed class HtmlToUitkJobEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            HtmlToUitkJob job = (HtmlToUitkJob)target;
            VisualElement root = new VisualElement();
            HelpBox status = new HelpBox(string.Empty, HelpBoxMessageType.None);
            VisualElement fields = new VisualElement();
            InspectorElement.FillDefaultInspector(fields, serializedObject, this);
            bool imported = job.IsImportedFromJson;
            fields.SetEnabled(!imported);

            string jsonPath = job.JsonAssetPath;
            status.text = imported
                ? "Imported from " + jsonPath + ". The JSON file is the source of truth: edit it in the window or a text editor."
                : "JSON file: " + (string.IsNullOrEmpty(jsonPath) ? "(save the asset first)" : jsonPath);
            root.Add(status);
            root.Add(fields);

            VisualElement buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginTop = 6 } };
            buttons.Add(new Button(() => HtmlToUitkWindow.OpenWithJob(job)) { text = "Open in window" });
            if (!imported)
            {
                buttons.Add(new Button(() => Save(job, status)) { text = "Save JSON" });
                buttons.Add(new Button(() => Load(job, status)) { text = "Load JSON" });
            }

            root.Add(buttons);
            return root;
        }

        private static void Save(HtmlToUitkJob job, HelpBox status)
        {
            if (job.SaveJson(out string error))
            {
                AssetDatabase.Refresh();
                status.text = "Saved " + job.JsonAssetPath;
            }
            else
            {
                status.text = error;
            }
        }

        private static void Load(HtmlToUitkJob job, HelpBox status)
        {
            Undo.RecordObject(job, "Load job JSON");
            if (job.LoadJson(out string error))
            {
                EditorUtility.SetDirty(job);
                status.text = "Loaded " + job.JsonAssetPath;
            }
            else
            {
                status.text = error;
            }
        }
    }
}
