using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Neo.HtmlToUitk.Json;

namespace Neo.HtmlToUitk.Tooling
{
    [Serializable]
    public sealed class FolderEntry
    {
        public string key;
        public string value;
    }

    [CreateAssetMenu(menuName = "Neoxider/UI/HTML to UITK Job", fileName = "NewJob")]
    public sealed class HtmlToUitkJob : ScriptableObject
    {
        public const string JsonExtension = ".htujob.json";

        public string jobName = string.Empty;
        public string configFile = string.Empty;
        public HtmlToUitkConfig config;
        public List<string> html = new List<string>();
        public List<string> css = new List<string>();
        public string cssOrder = string.Empty;
        public List<string> uss = new List<string>();
        public List<string> partials = new List<string>();
        public string outputRoot = "Assets/UI/Generated";
        public string assemble = string.Empty;
        public string partialsMode = string.Empty;
        public List<FolderEntry> folders = new List<FolderEntry>();
        public bool watchEnabled;
        public int debounceMs = 400;
        [TextArea(2, 8)] public string configOverridesJson = string.Empty;
        [TextArea(2, 8)] public string perFileJson = string.Empty;
        [SerializeField, HideInInspector] private string sourceJson = string.Empty;
        [SerializeField] private string jsonPathOverride = string.Empty;

        public bool IsImportedFromJson => AssetDatabase.GetAssetPath(this).EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase);

        public string JsonAssetPath
        {
            get
            {
                string asset = AssetDatabase.GetAssetPath(this);
                if (string.IsNullOrEmpty(asset))
                    return string.IsNullOrEmpty(jsonPathOverride) ? null : jsonPathOverride;
                if (asset.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase))
                    return asset;
                if (!string.IsNullOrEmpty(jsonPathOverride))
                    return jsonPathOverride;
                return Path.ChangeExtension(asset, null) + JsonExtension;
            }
        }

        public string JsonAbsolutePath
        {
            get
            {
                string path = JsonAssetPath;
                return string.IsNullOrEmpty(path) ? null : HtmlToUitkPaths.ToAbsolute(path);
            }
        }

        public void SetJsonPath(string projectRelativeOrAbsolute)
        {
            jsonPathOverride = projectRelativeOrAbsolute ?? string.Empty;
        }

        public JobDocument ToDocument()
        {
            JobDocument document = string.IsNullOrEmpty(sourceJson) ? new JobDocument() : JobDocument.Parse(sourceJson);
            document.Name = jobName ?? string.Empty;
            document.Config = configFile ?? string.Empty;
            document.Html = new List<string>(html);
            document.Css = new List<string>(css);
            document.CssOrder = cssOrder ?? string.Empty;
            document.Uss = new List<string>(uss);
            document.Partials = new List<string>(partials);
            document.OutputRoot = outputRoot ?? string.Empty;
            document.Assemble = assemble ?? string.Empty;
            document.PartialsMode = partialsMode ?? string.Empty;
            document.WatchEnabled = watchEnabled;
            document.DebounceMs = debounceMs;
            document.Folders = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < folders.Count; i++)
                document.Folders.Add(new KeyValuePair<string, string>(folders[i].key, folders[i].value));
            document.ConfigOverrides = ParseOrNull(configOverridesJson);
            document.PerFile = ParseOrNull(perFileJson);
            if (string.IsNullOrEmpty(document.Config) && config != null)
                document.Config = config.configJsonPath;
            if (config != null)
                config.ApplyPanelSettings(document);
            return document;
        }

        public void FromDocument(JobDocument document)
        {
            sourceJson = document.ToJson();
            jobName = document.Name;
            configFile = document.Config;
            html = new List<string>(document.Html);
            css = new List<string>(document.Css);
            cssOrder = document.CssOrder;
            uss = new List<string>(document.Uss);
            partials = new List<string>(document.Partials);
            outputRoot = document.OutputRoot;
            assemble = document.Assemble;
            partialsMode = document.PartialsMode;
            watchEnabled = document.WatchEnabled;
            debounceMs = document.DebounceMs;
            folders = new List<FolderEntry>();
            for (int i = 0; i < document.Folders.Count; i++)
                folders.Add(new FolderEntry { key = document.Folders[i].Key, value = document.Folders[i].Value });
            configOverridesJson = document.ConfigOverrides != null ? MiniJson.Write(document.ConfigOverrides, true) : string.Empty;
            perFileJson = document.PerFile != null ? MiniJson.Write(document.PerFile, true) : string.Empty;
        }

        public string ToJson()
        {
            return ToDocument().ToJson();
        }

        public void FromJson(string json)
        {
            FromDocument(JobDocument.Parse(json));
        }

        public bool SaveJson(out string error)
        {
            string path = JsonAbsolutePath;
            if (string.IsNullOrEmpty(path))
            {
                error = "The job asset has no JSON path yet. Save the asset first or choose a JSON location.";
                return false;
            }

            try
            {
                File.WriteAllText(path, ToJson(), new UTF8Encoding(false));
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public bool LoadJson(out string error)
        {
            string path = JsonAbsolutePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                error = "JSON file not found: " + path;
                return false;
            }

            try
            {
                FromJson(File.ReadAllText(path));
                error = null;
                return true;
            }
            catch (JsonParseException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static JsonValue ParseOrNull(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            return MiniJson.TryParse(json, out JsonValue value, out string _) ? value : null;
        }
    }
}
