using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Extensions
{
    public static partial class PrefabPreviewExtensions
    {
        private static readonly Dictionary<Texture2D, Sprite> CachedPreviewSprites = new();

        // WHY: [InitializeOnLoadMethod] only fires on a domain reload, so with Enter Play Mode Options
        // disabling it the cache carries Sprites created during the previous session — those are destroyed
        // on exit and every lookup then falls back through a dangling entry. The extra hooks clear the
        // cache when play stops (Unity 6.5+) and when a session starts.
        // The lifecycle attribute is source-generated, so the containing class must stay partial.
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            CachedPreviewSprites.Clear();
        }

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
