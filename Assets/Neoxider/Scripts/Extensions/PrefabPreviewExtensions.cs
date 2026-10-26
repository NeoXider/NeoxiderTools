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

        // Release owned sprites on editor reload/quit and across play sessions, including disabled domain reload.
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
            ClearPreviewSpriteCache();
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= ClearPreviewSpriteCache;
            AssemblyReloadEvents.beforeAssemblyReload += ClearPreviewSpriteCache;
            EditorApplication.quitting -= ClearPreviewSpriteCache;
            EditorApplication.quitting += ClearPreviewSpriteCache;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        /// <summary>
        ///     Releases sprites generated for editor thumbnails. Sprites from renderers/images and preview textures
        ///     are borrowed and are never destroyed. Discard generated sprite references after clearing the cache.
        /// </summary>
        public static void ClearPreviewSpriteCache()
        {
            foreach (Sprite sprite in CachedPreviewSprites.Values)
            {
                sprite.SafeDestroy();
            }

            CachedPreviewSprites.Clear();
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ClearPreviewSpriteCache();
            }
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

            Sprite generated = Sprite.Create(preview, new Rect(0f, 0f, preview.width, preview.height),
                new Vector2(0.5f, 0.5f));
            generated.hideFlags = HideFlags.HideAndDontSave;
            CachedPreviewSprites[preview] = generated;
            return generated;
#else
            return null;
#endif
        }
    }
}
