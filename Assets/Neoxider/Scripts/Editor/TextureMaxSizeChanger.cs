using UnityEngine;
using UnityEditor;

namespace Neoxider
{
    public class TextureMaxSizeChanger : EditorWindow
    {
        private int maxSize = 1024; // Новое значение Max Size
        private TextureImporterType textureType = TextureImporterType.Default; // Тип текстуры

        [MenuItem("Tools/Neoxider/" + "Change Texture Max Size")]
        public static void ShowWindow()
        {
            GetWindow<TextureMaxSizeChanger>("Change Texture Max Size");
        }

        private void OnGUI()
        {
            GUILayout.Label("Change Max Size for Textures", EditorStyles.boldLabel);
            maxSize = EditorGUILayout.IntField("Max Size", maxSize);
            textureType = (TextureImporterType)EditorGUILayout.EnumPopup("Texture Type", textureType);

            if (GUILayout.Button("Apply"))
            {
                ChangeMaxSize();
            }
        }

        private void ChangeMaxSize()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null && importer.textureType == textureType)
                {
                    importer.maxTextureSize = maxSize;
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("Max Size changed for all selected textures.");
        }
    }
}