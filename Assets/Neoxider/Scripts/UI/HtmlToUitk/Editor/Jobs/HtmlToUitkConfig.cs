using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Neo.HtmlToUitk.Json;

namespace Neo.HtmlToUitk.Tooling
{
    [CreateAssetMenu(menuName = "Neoxider/UI/HTML to UITK Config", fileName = "HtmlToUitkConfig")]
    public sealed class HtmlToUitkConfig : ScriptableObject
    {
        public string configJsonPath = "htmltouitk.config.json";
        public PanelSettings panelSettings;
        public List<string> generatedFolderNames = new List<string> { "Generated" };
        public bool manageTextureImport = true;
        public FilterMode textureFilterMode = FilterMode.Bilinear;
        public bool textureAsSprite = true;
        public bool textureAlphaIsTransparency = true;

        public bool MatchesGeneratedPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
            string normalized = GlobMatcher.Normalize(assetPath);
            for (int i = 0; i < generatedFolderNames.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(generatedFolderNames[i]) && normalized.IndexOf("/" + generatedFolderNames[i].Trim('/') + "/", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        public void ApplyPanelSettings(JobDocument document)
        {
            if (panelSettings == null)
                return;
            JsonValue overrides = document.ConfigOverrides != null && document.ConfigOverrides.Kind == JsonKind.Object ? document.ConfigOverrides : JsonValue.NewObject();
            JsonValue unity = overrides["unity"].Kind == JsonKind.Object ? overrides["unity"] : JsonValue.NewObject();
            string panelPath = AssetDatabase.GetAssetPath(panelSettings);
            if (!string.IsNullOrEmpty(panelPath))
                unity.Set("panelSettings", JsonValue.FromString(panelPath));
            JsonValue resolution = JsonValue.NewArray();
            resolution.Add(JsonValue.FromNumber(panelSettings.referenceResolution.x));
            resolution.Add(JsonValue.FromNumber(panelSettings.referenceResolution.y));
            unity.Set("referenceResolution", resolution);
            overrides.Set("unity", unity);
            document.ConfigOverrides = overrides;
        }

        public static HtmlToUitkConfig FindDefault()
        {
            string[] guids = AssetDatabase.FindAssets("t:HtmlToUitkConfig");
            if (guids.Length == 0)
                return null;
            Array.Sort(guids, StringComparer.Ordinal);
            return AssetDatabase.LoadAssetAtPath<HtmlToUitkConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
