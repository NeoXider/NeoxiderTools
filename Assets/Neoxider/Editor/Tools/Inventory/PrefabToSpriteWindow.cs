using System.IO;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    public class PrefabToSpriteWindow : EditorWindow
    {
        private GameObject _prefab;
        private Texture2D _previewTexture;
        private Vector2 _scroll;
        private string _lastSavedPath;

        [MenuItem("Tools/Neoxider/Create Sprite from Prefab...", false, 400)]
        public static void OpenWindow()
        {
            PrefabToSpriteWindow w = GetWindow<PrefabToSpriteWindow>("Prefab to Sprite");
            w.minSize = new Vector2(320, 200);
        }

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
            }

            if (_prefab != null)
            {
                if (_previewTexture == null)
                {
                    _previewTexture = AssetPreview.GetAssetPreview(_prefab);
                    if (_previewTexture == null)
                    {
                        _previewTexture = AssetPreview.GetMiniThumbnail(_prefab);
                    }
                }

                if (_previewTexture != null)
                {
                    EditorGUILayout.Space(4);
                    float size = Mathf.Min(128, _previewTexture.width, _previewTexture.height);
                    Rect rect = GUILayoutUtility.GetRect(size, size);
                    EditorGUI.DrawPreviewTexture(rect, _previewTexture, null, ScaleMode.ScaleToFit);
                }
            }

            EditorGUILayout.Space(8);
            GUI.enabled = _prefab != null;
            if (GUILayout.Button("Create Sprite...", GUILayout.Height(28)))
            {
                CreateAndSaveSprite(_prefab);
            }
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(_lastSavedPath))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox($"Saved: {_lastSavedPath}", MessageType.None);
            }
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
                EditorUtility.DisplayDialog("Prefab to Sprite", "Could not get preview for this prefab. Try selecting it in Project and wait a moment.", "OK");
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
