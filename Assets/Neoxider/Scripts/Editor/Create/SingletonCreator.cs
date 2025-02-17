using System.IO;
using UnityEditor;
using UnityEngine;

public static class SingletonCreator
{
    [MenuItem(itemName:"Assets/Create/Neoxider/Singleton", priority = 0)]
    public static void CreateSingletonTemplate()
    {
        string template =
@"//=== By Neoxider ===
using UnityEngine;
using Neo;
using Neo.Tools;

public class NewSingleton : Singleton<NewSingleton>
{
    // Custom setup when the Singleton is first created
    protected override void OnInstanceCreated()
    {

    }

    // Initialization logic here
    protected override void Init()
    {
        base.Init();

    }

    private void Start()
    {

    }

    private void Update()
    {

    }
}";
        string folderPath = GetSelectedPathOrFallback();
        string filePath = Path.Combine(folderPath, "NewSingleton.cs");

        if (System.IO.File.Exists(filePath))
        {
            Debug.LogError("NewSingleton.cs already exists at " + filePath);
            return;
        }

        System.IO.File.WriteAllText(filePath, template);
        AssetDatabase.Refresh();
        //Debug.Log("Singleton template created at " + path);
    }

    public static string GetSelectedPathOrFallback()
    {
        string path = "Assets";

        if (Selection.activeObject != null)
        {
            path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path))
            {
                path = Path.GetDirectoryName(path);
            }
        }

        return path;
    }
}
