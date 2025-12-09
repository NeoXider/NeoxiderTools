using System;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Windows
{
    /// <summary>
    ///     GUI отрисовка для окна изменения максимального размера текстур
    /// </summary>
    public class TextureMaxSizeChangerGUI : EditorWindowGUI
    {
        private int _maxSizeTexture = 1024;
        private TextureImporterType _textureType = TextureImporterType.Default;

        /// <summary>
        ///     Отрисовка GUI
        /// </summary>
        public override void OnGUI(EditorWindow window)
        {
            GUILayout.Label("Change Max Size for Textures", EditorStyles.boldLabel);
            _maxSizeTexture = EditorGUILayout.IntField("Max Size", _maxSizeTexture);
            _textureType = (TextureImporterType)EditorGUILayout.EnumPopup("Texture Type", _textureType);

            if (GUILayout.Button("Apply"))
            {
                ChangeMaxSize();
            }
        }

        private void ChangeMaxSize()
        {
            if (!EditorUtility.DisplayDialog("Change Texture Max Size",
                    $"This will change max size to {_maxSizeTexture} for ALL textures of type {_textureType}.\n\nAre you sure?",
                    "Yes", "Cancel"))
            {
                return;
            }

            try
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D");
                int changed = 0;

                EditorUtility.DisplayProgressBar("Changing Texture Max Size", "Processing...", 0f);

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                    if (importer != null && importer.textureType == _textureType)
                    {
                        importer.maxTextureSize = _maxSizeTexture;
                        importer.SaveAndReimport();
                        changed++;
                    }

                    EditorUtility.DisplayProgressBar("Changing Texture Max Size",
                        $"Processing {i + 1}/{guids.Length}", (float)i / guids.Length);
                }

                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();

                Debug.Log($"[TextureMaxSizeChanger] Changed max size for {changed} textures of type {_textureType}.");
                EditorUtility.DisplayDialog("Complete",
                    $"Changed max size for {changed} textures.", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[TextureMaxSizeChanger] Error: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to change textures: {e.Message}", "OK");
            }
        }
    }
}