using UnityEditor;
using UnityEngine;

namespace Neo
{
    public class TextureMaxSizeChanger : EditorWindow
    {
        private int maxSizeTrxture = 1024;
        private TextureImporterType textureType = TextureImporterType.Default;

        private void OnGUI()
        {
            GUILayout.Label("Change Max Size for Textures", EditorStyles.boldLabel);
            maxSizeTrxture = EditorGUILayout.IntField("Max Size", maxSizeTrxture);
            textureType = (TextureImporterType)EditorGUILayout.EnumPopup("Texture Type", textureType);

            if (GUILayout.Button("Apply")) ChangeMaxSize();
        }

        [MenuItem("Tools/Neoxider/" + "Change Texture Max Size")]
        public static void ShowWindow()
        {
            GetWindow<TextureMaxSizeChanger>("Change Texture Max Size");
        }

        private void ChangeMaxSize()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

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