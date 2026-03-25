using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Pages.Editor
{
    public class AutoSpriteAssignerEditor : EditorWindow
    {
        private readonly List<string> ignoreNames = new();
        private readonly List<string> ignoreNativeSizeNames = new() { "background" };
        private bool alwaysSetNativeSize;
        private int assignedCount;
        private Dictionary<string, Sprite> spriteDictionary;
        private string spriteFolderPath = "_source/Sprites";

        private void OnGUI()
        {
            GUILayout.Label("Auto Sprite Assigner", EditorStyles.boldLabel);
            GUILayout.Space(5);

            spriteFolderPath = EditorGUILayout.TextField("Sprite Folder (relative to Assets/)", spriteFolderPath);

            GUILayout.Space(10);

            GUILayout.Label("Ignore Names (skip sprite assignment):", EditorStyles.label);
            DrawStringList(ignoreNames, "+");

            GUILayout.Space(5);

            GUILayout.Label("Ignore SetNativeSize Names:", EditorStyles.label);
            DrawStringList(ignoreNativeSizeNames, "+");
            GUILayout.Space(10);
            alwaysSetNativeSize = GUILayout.Toggle(alwaysSetNativeSize, "Always set SetNativeSize()");

            if (GUILayout.Button("Assign Sprites To All Images"))
            {
                AssignSpritesToAllImages();
            }

            GUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "GameObject and Sprite names are compared case-insensitively. All Images are processed, including on inactive objects.",
                MessageType.Info);
        }


        [MenuItem("Tools/UIKit/Auto Sprite Assigner")]
        public static void ShowWindow()
        {
            GetWindow<AutoSpriteAssignerEditor>("Auto Sprite Assigner");
        }

        private static bool IsStretchedFullScreen(RectTransform rt)
        {
            // Edge anchors and zero offsets → fill parent rect
            bool anchorsFull = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one;
            return anchorsFull;
        }

        private void DrawStringList(List<string> list, string addButtonLabel)
        {
            // Add new list entry button
            if (GUILayout.Button(addButtonLabel))
            {
                list.Add(string.Empty);
            }

            // Each entry: text field + remove button
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i] = EditorGUILayout.TextField(list[i]);
                if (GUILayout.Button("—", GUILayout.Width(20)))
                {
                    list.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void AssignSpritesToAllImages()
        {
            assignedCount = 0;
            LoadAllSprites();

            Image[] allImages = Resources.FindObjectsOfTypeAll<Image>();

            foreach (Image image in allImages)
            {
                GameObject go = image.gameObject;

                // Only objects in a scene (not assets)
                if (string.IsNullOrEmpty(go.scene.name))
                {
                    continue;
                }

                string objectNameLower = go.name.ToLowerInvariant();

                if (ignoreNames.Any(key =>
                        !string.IsNullOrEmpty(key) &&
                        objectNameLower.Contains(key.ToLowerInvariant())))
                {
                    continue;
                }

                if (spriteDictionary.TryGetValue(objectNameLower, out Sprite sprite))
                {
                    if (image.sprite != sprite)
                    {
                        Undo.RecordObject(image, "Assign Sprite");
                        image.sprite = sprite;
                        EditorUtility.SetDirty(image);
                        assignedCount++;
                    }

                    bool skipNative = ignoreNativeSizeNames.Any(key =>
                        !string.IsNullOrEmpty(key) &&
                        objectNameLower.Contains(key.ToLowerInvariant()));

                    bool stretchedFull = IsStretchedFullScreen(image.rectTransform);

                    if ((!skipNative || alwaysSetNativeSize) && !stretchedFull)
                    {
                        image.SetNativeSize();
                    }
                }
            }

            Debug.Log(
                $"<color=green>[AutoSpriteAssigner]</color> Sprites assigned. Total replacements: <color=yellow>{assignedCount}</color>");
        }

        private void LoadAllSprites()
        {
            spriteDictionary = new Dictionary<string, Sprite>();

            string fullPath = "Assets/" + spriteFolderPath;
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { fullPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                if (sprite != null)
                {
                    string lowerName = sprite.name.ToLowerInvariant();
                    if (!spriteDictionary.ContainsKey(lowerName))
                    {
                        spriteDictionary.Add(lowerName, sprite);
                    }
                }
            }

            Debug.Log($"[AutoSpriteAssigner] Sprites loaded: {spriteDictionary.Count}");
        }
    }
}