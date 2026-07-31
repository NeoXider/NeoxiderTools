using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using GUI = UnityEngine.GUI;

namespace Neo.Editor
{
    /// <summary>
    ///     Editor utility for creating Singleton class templates
    /// </summary>
    public static class SingletonCreator
    {
        private const string MenuPath = "Assets/Create/Neoxider/Singleton";
        private const string DefaultFileName = "NewSingleton";
        private const string FileExtension = ".cs";
        private const string DialogTitle = "Create New Singleton Script";
        private const string DialogMessage = "Enter singleton class name:";

        private static readonly string TemplateContent =
            @"//=== By Neoxider ===
using UnityEngine;
using Neo;
using Neo.Tools;

/// <summary>
/// Singleton implementation of {0} 
/// Use Singleton.I to access the instance
/// </summary>
public class {0} : Singleton<{0}>
{{
    #region Singleton Implementation
    
    // Custom setup when the Singleton is first created
    protected override void OnInstanceCreated()
    {{
    }}

    // Initialization logic here (Awake)
    protected override void Init()
    {{
        base.Init();
        
    }}
    
    #endregion

    private void Start()
    {{

    }}

    private void Update()
    {{

    }}
}}";

        [MenuItem(MenuPath, priority = 80)]
        public static void CreateSingletonTemplate()
        {
            try
            {
                string folderPath = GetSelectedPathOrFallback();

                string className = GetClassName();
                if (string.IsNullOrEmpty(className))
                {
                    return;
                }

                string filePath = Path.Combine(folderPath, className + FileExtension);

                if (File.Exists(filePath))
                {
                    if (!EditorUtility.DisplayDialog("File Already Exists",
                            $"The file '{className + FileExtension}' already exists. Do you want to overwrite it?",
                            "Yes", "No"))
                    {
                        return;
                    }
                }

                string scriptContent = string.Format(TemplateContent, className);

                File.WriteAllText(filePath, scriptContent);
                AssetDatabase.Refresh();

                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(GetRelativePath(filePath));
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);

                Debug.Log($"Singleton script created at {GetRelativePath(filePath)}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create singleton template: {e.Message}\nStack trace: {e.StackTrace}");
            }
        }

        private static string GetClassName()
        {
            string className = EditorInputDialog.Show(DialogTitle, DialogMessage, DefaultFileName);

            if (string.IsNullOrEmpty(className))
            {
                return null;
            }

            if (className.Length > 0)
            {
                className = char.ToUpper(className[0]) + className.Substring(1);
            }

            className = new string(className.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

            return string.IsNullOrEmpty(className) ? null : className;
        }

        private static string GetSelectedPathOrFallback()
        {
            string path = "Assets";

            foreach (Object obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (!AssetDatabase.IsValidFolder(path))
                {
                    path = Path.GetDirectoryName(path);
                }

                break;
            }

            return path;
        }

        private static string GetRelativePath(string absolutePath)
        {
            if (absolutePath.StartsWith(Application.dataPath))
            {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }

            return absolutePath;
        }
    }

    public class EditorInputDialog : EditorWindow
    {
        private Action<string> callback;
        private string defaultName = "";
        private bool initialized;
        private string input = "";
        private string message = "";
        private new string title = "";

        private void OnGUI()
        {
            if (!initialized)
            {
                input = defaultName;
                initialized = true;
            }

            EditorGUILayout.LabelField(message);
            GUI.SetNextControlName("Input");
            input = EditorGUILayout.TextField(input);

            if (!initialized)
            {
                EditorGUI.FocusTextInControl("Input");
                initialized = true;
            }

            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                Submit();
            }

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("OK"))
                {
                    Submit();
                }

                if (GUILayout.Button("Cancel"))
                {
                    input = "";
                    Close();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public static string Show(string title, string message, string defaultName = "")
        {
            string result = defaultName;

            EditorInputDialog window = CreateInstance<EditorInputDialog>();
            window.title = title;
            window.message = message;
            window.defaultName = defaultName;
            window.callback = value => result = value;
            window.position = new Rect(Screen.currentResolution.width / 2, Screen.currentResolution.height / 2, 300,
                100);
            window.ShowModal();

            return result;
        }

        private void Submit()
        {
            callback?.Invoke(input);
            Close();
        }
    }
}
