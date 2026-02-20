using System.IO;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    public static class PrefabToSpriteUtility
    {
        /// <summary>
        ///     Copies preview texture to a readable Texture2D (AssetPreview textures are often non-readable).
        /// </summary>
        public static Texture2D GetReadableCopy(Texture2D source)
        {
            if (source == null)
            {
                return null;
            }

            if (source.isReadable)
            {
                return source;
            }

            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        /// <summary>
        ///     Creates a Sprite asset from a preview texture: writes PNG to path, reimports as Sprite, returns the asset.
        /// </summary>
        public static Sprite CreateSpriteFromPreview(Texture2D previewTexture, string projectPath)
        {
            if (previewTexture == null || string.IsNullOrEmpty(projectPath))
            {
                return null;
            }

            Texture2D readable = GetReadableCopy(previewTexture);
            if (readable == null)
            {
                return null;
            }

            string dir = Path.GetDirectoryName(projectPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            byte[] png = readable.EncodeToPNG();
            if (readable != previewTexture)
            {
                Object.DestroyImmediate(readable);
            }

            File.WriteAllBytes(projectPath, png);
            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(projectPath) as TextureImporter;
            if (importer == null)
            {
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            Object asset = AssetDatabase.LoadAssetAtPath<Sprite>(projectPath);
            return asset as Sprite;
        }
    }
}
