using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Neo.HtmlToUitk.Json;

namespace Neo.HtmlToUitk.Tooling
{
    [ScriptedImporter(1, new[] { "htujob.json" })]
    public sealed class HtmlToUitkJobImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            HtmlToUitkJob job = ScriptableObject.CreateInstance<HtmlToUitkJob>();
            string fileName = Path.GetFileName(ctx.assetPath);
            job.name = fileName.EndsWith(HtmlToUitkJob.JsonExtension, System.StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring(0, fileName.Length - HtmlToUitkJob.JsonExtension.Length)
                : Path.GetFileNameWithoutExtension(fileName);
            try
            {
                job.FromJson(File.ReadAllText(ctx.assetPath));
            }
            catch (JsonParseException exception)
            {
                ctx.LogImportError("Invalid job file: " + exception.Message, job);
            }

            ctx.AddObjectToAsset("job", job);
            ctx.SetMainObject(job);
        }
    }

    [CustomEditor(typeof(HtmlToUitkJobImporter))]
    public sealed class HtmlToUitkJobImporterEditor : ScriptedImporterEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            HtmlToUitkJob job = AssetDatabase.LoadAssetAtPath<HtmlToUitkJob>(((AssetImporter)target).assetPath);
            Label summary = new Label(job == null
                ? "The job file could not be read."
                : "Inputs: " + job.html.Count + " html, " + job.css.Count + " css, " + job.uss.Count + " uss. Output: " + job.outputRoot);
            summary.style.whiteSpace = WhiteSpace.Normal;
            summary.style.marginBottom = 6;
            root.Add(summary);
            Button open = new Button(() => HtmlToUitkWindow.OpenWithJson(((AssetImporter)target).assetPath)) { text = "Open in HTML to UI Toolkit" };
            root.Add(open);
            return root;
        }

        public override bool showImportedObject => false;
    }
}
