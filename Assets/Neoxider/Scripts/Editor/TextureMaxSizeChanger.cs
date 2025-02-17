using UnityEngine;
using UnityEditor;

namespace Neo
{
    public class TextureMaxSizeChanger : EditorWindow
    {
        private int maxSizeTrxture = 1024;
        private TextureImporterType textureType = TextureImporterType.Default;

        [MenuItem("Tools/Neoxider/" + "Change Texture Max Size")]
        public static void ShowWindow()
        {
            GetWindow<TextureMaxSizeChanger>("Change Texture Max Size");
        }

        private void OnGUI()
        {
            GUILayout.Label("Change Max Size for Textures", EditorStyles.boldLabel);
            maxSizeTrxture = EditorGUILayout.IntField("Max Size", maxSizeTrxture);
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
                    importer.maxTextureSize = maxSizeTrxture;
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("Max Size changed for all selected textures.");
        }
    }
}