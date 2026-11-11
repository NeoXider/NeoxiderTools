using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    public class PrefabToSpriteWindow : EditorWindow
    {
        private string _lastSavedPath;
        private GameObject _prefab;
        private Texture2D _previewTexture;
        private bool _hasFullPreview;
        private Vector2 _scroll;

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Create Sprite from Prefab", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Assign a prefab and click \"Create Sprite...\" to save its preview as a Sprite asset in the project.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            _prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _prefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                _previewTexture = null;
                _hasFullPreview = false;
            }

            if (_prefab != null)
            {
                // WHY: AssetPreview renders asynchronously. The first call usually returns null, and the mini
                // thumbnail used as a stand-in was then cached for good, so the real preview never appeared.
                // The full preview is polled until it arrives, repainting only while previews are loading.
                if (!_hasFullPreview)
                {
                    Texture2D full = AssetPreview.GetAssetPreview(_prefab);
                    if (full != null)
                    {
                        _previewTexture = full;
                        _hasFullPreview = true;
                    }
                    else
                    {
                        if (_previewTexture == null)
                        {
                            _previewTexture = AssetPreview.GetMiniThumbnail(_prefab);
                        }

                        if (AssetPreview.IsLoadingAssetPreviews())
                        {
                            Repaint();
                        }
                    }
                }

                if (_previewTexture != null)
                {
                    EditorGUILayout.Space(4);
                    float size = Mathf.Min(128, Mathf.Min(_previewTexture.width, _previewTexture.height));
                    Rect rect = GUILayoutUtility.GetRect(size, size);
                    EditorGUI.DrawPreviewTexture(rect, _previewTexture, null, ScaleMode.ScaleToFit);
                }
            }

            EditorGUILayout.Space(8);
            using (new EditorGUI.DisabledScope(_prefab == null))
            {
                if (GUILayout.Button("Create Sprite...", GUILayout.Height(28)))
                {
                    CreateAndSaveSprite(_prefab);
                    // WHY: the save panel is modal; the layout opened by this pass no longer matches after it
                    // returns, so the rest of the pass is abandoned instead of logging layout errors.
                    GUIUtility.ExitGUI();
                }
            }

            if (!string.IsNullOrEmpty(_lastSavedPath))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox($"Saved: {_lastSavedPath}", MessageType.None);
            }
        }

        [MenuItem("Neoxider/Windows/Prefab To Sprite", false, 2)]
        public static void OpenWindow()
        {
            PrefabToSpriteWindow w = GetWindow<PrefabToSpriteWindow>("Prefab to Sprite");
            w.minSize = new Vector2(320, 200);
        }

        private void CreateAndSaveSprite(GameObject prefab)
        {
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Prefab to Sprite", "Assign a prefab first.", "OK");
                return;
            }

            Texture2D preview = AssetPreview.GetAssetPreview(prefab);
            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(prefab);
            }

            if (preview == null)
            {
                EditorUtility.DisplayDialog("Prefab to Sprite",
                    "Could not get preview for this prefab. Try selecting it in Project and wait a moment.", "OK");
                return;
            }

            string defaultName = prefab.name + "_Icon";
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Sprite",
                defaultName,
                "png",
                "Choose where to save the sprite");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Sprite sprite = PrefabToSpriteUtility.CreateSpriteFromPreview(preview, path);
            if (sprite != null)
            {
                _lastSavedPath = path;
                EditorGUIUtility.PingObject(sprite);
                Repaint();
            }
        }

        public static Sprite CreateSpriteFromPreviewAndAssign(GameObject prefab, string savePath)
        {
            if (prefab == null || string.IsNullOrEmpty(savePath))
            {
                return null;
            }

            Texture2D preview = AssetPreview.GetAssetPreview(prefab);
            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(prefab);
            }

            return preview != null ? PrefabToSpriteUtility.CreateSpriteFromPreview(preview, savePath) : null;
        }
    }
}
