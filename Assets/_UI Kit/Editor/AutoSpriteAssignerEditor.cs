﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AutoSpriteAssignerEditor : EditorWindow
{
    private bool alwaysSetNativeSize;
    private int assignedCount;

    private readonly List<string> ignoreNames = new();
    private readonly List<string> ignoreNativeSizeNames = new() { "background" };
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

        if (GUILayout.Button("Assign Sprites To All Images")) AssignSpritesToAllImages();

        GUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Названия GameObject'ов и Sprite'ов сравниваются без учёта регистра. Обрабатываются все Image, даже в неактивных объектах.",
            MessageType.Info);
    }


    [MenuItem("Tools/UIKit/Auto Sprite Assigner")]
    public static void ShowWindow()
    {
        GetWindow<AutoSpriteAssignerEditor>("Auto Sprite Assigner");
    }

    private static bool IsStretchedFullScreen(RectTransform rt)
    {
        // anchors по краям и нулевые отступы → занимаем весь родительский rect
        var anchorsFull = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one;
        return anchorsFull;
    }

    private void DrawStringList(List<string> list, string addButtonLabel)
    {
        // Кнопка добавления нового элемента
        if (GUILayout.Button(addButtonLabel)) list.Add(string.Empty);

        // Для каждого элемента — текстовое поле + кнопка удаления
        for (var i = 0; i < list.Count; i++)
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

        var allImages = Resources.FindObjectsOfTypeAll<Image>();

        foreach (var image in allImages)
        {
            var go = image.gameObject;

            // Убедимся, что объект находится в сцене (не ассет)
            if (string.IsNullOrEmpty(go.scene.name))
                continue;

            var objectNameLower = go.name.ToLowerInvariant();

            if (ignoreNames.Any(key =>
                    !string.IsNullOrEmpty(key) &&
                    objectNameLower.Contains(key.ToLowerInvariant())))
                continue;

            if (spriteDictionary.TryGetValue(objectNameLower, out var sprite))
            {
                if (image.sprite != sprite)
                {
                    Undo.RecordObject(image, "Assign Sprite");
                    image.sprite = sprite;
                    EditorUtility.SetDirty(image);
                    assignedCount++;
                }

                var skipNative = ignoreNativeSizeNames.Any(key =>
                    !string.IsNullOrEmpty(key) &&
                    objectNameLower.Contains(key.ToLowerInvariant()));

                var stretchedFull = IsStretchedFullScreen(image.rectTransform);

                if ((!skipNative || alwaysSetNativeSize) && !stretchedFull) image.SetNativeSize();
            }
        }

        Debug.Log(
            $"<color=green>[AutoSpriteAssigner]</color> Спрайты назначены. Всего замен: <color=yellow>{assignedCount}</color>");
    }

    private void LoadAllSprites()
    {
        spriteDictionary = new Dictionary<string, Sprite>();

        var fullPath = "Assets/" + spriteFolderPath;
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { fullPath });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
            {
                var lowerName = sprite.name.ToLowerInvariant();
                if (!spriteDictionary.ContainsKey(lowerName)) spriteDictionary.Add(lowerName, sprite);
            }
        }

        Debug.Log($"[AutoSpriteAssigner] Загружено спрайтов: {spriteDictionary.Count}");
    }
}