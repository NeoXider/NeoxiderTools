using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Extensions
{
    public static class PrefabPreviewExtensions
    {
        private static readonly Dictionary<Texture2D, Sprite> CachedPreviewSprites = new();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void ResetStaticState()
        {
            CachedPreviewSprites.Clear();
        }
#endif

        public static Texture2D GetPreviewTexture(this GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

#if UNITY_EDITOR
            Texture2D preview = AssetPreview.GetAssetPreview(prefab);
            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(prefab);
            }

            return preview;
#else
            return null;
#endif
        }

        public static Sprite GetPreviewSprite(this GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            SpriteRenderer spriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                return spriteRenderer.sprite;
            }

            Image image = prefab.GetComponentInChildren<Image>(true);
            if (image != null && image.sprite != null)
            {
                return image.sprite;
            }

#if UNITY_EDITOR
            Texture2D preview = prefab.GetPreviewTexture();
            if (preview == null)
            {
                return null;
            }

            if (CachedPreviewSprites.TryGetValue(preview, out Sprite cached) && cached != null)
            {
                return cached;
            }

            var generated = Sprite.Create(preview, new Rect(0f, 0f, preview.width, preview.height),
                new Vector2(0.5f, 0.5f));
            CachedPreviewSprites[preview] = generated;
            return generated;
#else
            return null;
#endif
        }
    }
}
