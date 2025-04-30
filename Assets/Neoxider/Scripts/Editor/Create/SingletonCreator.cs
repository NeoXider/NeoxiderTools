using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    /// Editor utility for creating Singleton class templates
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
/// </summary>
public class {0} : Singleton<{0}>
{{
    #region Singleton Implementation
    
    protected override void OnInstanceCreated()
    {{
        // Custom setup when the Singleton is first created
    }}

    protected override void Init()
    {{
        base.Init();
        // Initialization logic here
    }}
    
    #endregion
    #region Unity Lifecycle

    private void Start()
    {{

    }}

    private void Update()
    {{

    }}

    #endregion
    #region Public Methods

    #endregion
    #region Private Methods

    #endregion
}}";

        [MenuItem(itemName: MenuPath, priority = 80)]
        public static void CreateSingletonTemplate()
        {
            try
            {
                // Get the selected folder path
                string folderPath = GetSelectedPathOrFallback();
                
                // Show input dialog for class name
                string className = GetClassName();
                if (string.IsNullOrEmpty(className))
                    return;

                // Generate file path
                string filePath = Path.Combine(folderPath, className + FileExtension);

                // Check if file already exists
                if (File.Exists(filePath))
                {
                    if (!EditorUtility.DisplayDialog("File Already Exists", 
                        $"The file '{className + FileExtension}' already exists. Do you want to overwrite it?", 
                        "Yes", "No"))
                    {
                        return;
                    }
                }

                // Create the script content
                string scriptContent = string.Format(TemplateContent, className);
                
                // Write the file
                File.WriteAllText(filePath, scriptContent);
                AssetDatabase.Refresh();

                // Select the created file
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(GetRelativePath(filePath));
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);

                Debug.Log($"Singleton script created at {GetRelativePath(filePath)}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create singleton template: {e.Message}\nStack trace: {e.StackTrace}");
            }
        }

        private static string GetClassName()
        {
            string className = EditorInputDialog.Show(DialogTitle, DialogMessage, DefaultFileName);
            
            if (string.IsNullOrEmpty(className))
                return null;

            // Ensure first letter is uppercase
            if (className.Length > 0)
            {
                className = char.ToUpper(className[0]) + className.Substring(1);
            }

            // Remove invalid characters
            className = new string(className.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

            return string.IsNullOrEmpty(className) ? null : className;
        }

        private static string GetSelectedPathOrFallback()
        {
            string path = "Assets";

            foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

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
        private string input = "";
        private string title = "";
        private string message = "";
        private string defaultName = "";
        private bool initialized = false;
        private System.Action<string> callback;

        public static string Show(string title, string message, string defaultName = "")
        {
            string result = defaultName;
            
            var window = CreateInstance<EditorInputDialog>();
            window.title = title;
            window.message = message;
            window.defaultName = defaultName;
            window.callback = (value) => result = value;
            window.position = new Rect(Screen.currentResolution.width / 2, Screen.currentResolution.height / 2, 300, 100);
            window.ShowModal();

            return result;
        }

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

            // Focus the text field
            if (!initialized)
            {
                EditorGUI.FocusTextInControl("Input");
                initialized = true;
            }

            // Handle Enter key
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

        private void Submit()
        {
            callback?.Invoke(input);
            Close();
        }
    }
}
