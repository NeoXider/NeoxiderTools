using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Neo.HtmlToUitk.Tooling
{
    public sealed class GeneratedAssetPostprocessor : AssetPostprocessor
    {
        public const string ManagedMarker = "htu:texture:1";

        private static HtmlToUitkConfig _cachedConfig;
        private static bool _cacheValid;

        public static event Action<IReadOnlyList<string>> InputsChanged;

        private static HtmlToUitkConfig Config()
        {
            if (!_cacheValid)
            {
                _cachedConfig = HtmlToUitkConfig.FindDefault();
                _cacheValid = true;
            }

            return _cachedConfig;
        }

        public static bool IsGeneratedTexturePath(string assetPath, HtmlToUitkConfig config)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return false;
            // Configuration may narrow ownership; it must never replace exact manifest proof.
            if (config != null && (!config.manageTextureImport || !config.MatchesGeneratedPath(assetPath)))
                return false;
            string absolute = Path.GetFullPath(HtmlToUitkPaths.ToAbsolute(assetPath));
            string directory = Path.GetDirectoryName(absolute);
            string project = Path.GetFullPath(HtmlToUitkPaths.ProjectRoot);
            while (!string.IsNullOrEmpty(directory) && directory.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                string manifest = Path.Combine(directory, GeneratedTextureManifest.FileName);
                if (File.Exists(manifest))
                {
                    try { return GeneratedTextureManifest.Owns(File.ReadAllText(manifest), directory, absolute); }
                    catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is Neo.HtmlToUitk.Json.JsonParseException)
                    { Debug.LogWarning("[HtmlToUitk] Invalid generated texture manifest: " + manifest + ": " + exception.Message); return false; }
                }
                directory = Path.GetDirectoryName(directory);
            }
            return false;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || !HtmlToUitkSettings.ManageTextureImport)
                return;
            HtmlToUitkConfig config = Config();
            if (config != null && !config.manageTextureImport)
                return;
            if (!IsGeneratedTexturePath(assetPath, config))
                return;
            TextureImporter importer = (TextureImporter)assetImporter;
            if (importer.userData == ManagedMarker)
                return;
            importer.userData = ManagedMarker;
            importer.textureType = config == null || config.textureAsSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (importer.textureType == TextureImporterType.Sprite)
                importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = config == null || config.textureAlphaIsTransparency;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = config != null ? config.textureFilterMode : FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            _cacheValid = false;
            if (InputsChanged == null)
                return;
            List<string> changed = null;
            Collect(imported, ref changed);
            Collect(deleted, ref changed);
            Collect(moved, ref changed);
            if (changed != null)
                InputsChanged(changed);
        }

        private static void Collect(string[] paths, ref List<string> changed)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                bool relevant = path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".htm", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(HtmlToUitkJob.JsonExtension, StringComparison.OrdinalIgnoreCase);
                if (!relevant)
                    continue;
                if (changed == null)
                    changed = new List<string>();
                changed.Add(HtmlToUitkPaths.ToAbsolute(path));
            }
        }
    }
}
