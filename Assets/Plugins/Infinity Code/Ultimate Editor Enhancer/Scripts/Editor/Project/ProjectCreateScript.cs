/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer.ProjectTools
{
    [InitializeOnLoad]
    public static class ProjectCreateScript
    {
        static ProjectCreateScript()
        {
            ProjectItemDrawer.Register("CREATE_SCRIPT", DrawButton, 10);
        }

        private static void CreateScript(object userdata)
        {
            object[] data = (object[])userdata;
            Object asset = data[0] as Object;
            string name = (string)data[1];
            string path = null;

            string[] files = Directory.GetFiles(Resources.scriptTemplatesFolder, "*.txt", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (Path.GetFileName(file).StartsWith(name + "-"))
                {
                    path = file;
                    break;
                }
            }
            
            if (path == null) return;

            Selection.activeObject = asset;
            string defaultName = Path.GetFileNameWithoutExtension(path).Substring(name.Length + 1);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(path, defaultName);
        }

        private static void DrawButton(ProjectItem item)
        {
            if (!Prefs.projectCreateScript) return;
            if (!item.isFolder) return;
            if (!item.hovered) return;
            if (!item.path.StartsWith("Assets")) return;
            if (!item.path.Contains("Scripts")) return;

            Rect r = item.rect;
            r.xMin = r.xMax - 18;
            r.height = 16;

            item.rect.xMax -= 18;

            ButtonEvent be = GUILayoutUtils.Button(r, TempContent.Get(EditorIconContents.csScript.image, "Create Script\n(Right click to select a template)"), GUIStyle.none);
            if (be == ButtonEvent.click) ProcessClick(item);
        }

        private static void ProcessClick(ProjectItem item)
        {
            Event e = Event.current;
            if (e.button == 0) CreateScript(new object[] { item.asset, "C# Script" });
            else if (e.button == 1) ShowTemplatesMenu(item);
        }

        private static void ShowTemplatesMenu(ProjectItem item)
        {
            GenericMenuEx menu = GenericMenuEx.Start();

            menu.Add("C# Script", CreateScript, new object[] { item.asset, "C# Script" });
            menu.Add("C# ScriptableObject", CreateScript, new object[] { item.asset, "C# ScriptableObject" });
            menu.AddSeparator();
            menu.Add("C# Class", CreateScript, new object[] { item.asset, "C# Class" });
            menu.Add("C# Interface", CreateScript, new object[] { item.asset, "C# Interface" });
            menu.Add("C# Abstract Class", CreateScript, new object[] { item.asset, "C# Abstract Class" });
            menu.Add("C# Struct", CreateScript, new object[] { item.asset, "C# Struct" });
            menu.Add("C# Enum", CreateScript, new object[] { item.asset, "C# Enum" });
            menu.AddSeparator();
            menu.Add("C# Custom Editor Script", CreateScript, new object[] { item.asset, "C# Custom Editor" });
            menu.Add("C# Custom Property Drawer", CreateScript, new object[] { item.asset, "C# Custom Property Drawer" });
            menu.Add("C# Editor Window Script", CreateScript, new object[] { item.asset, "C# Editor Window" });
            menu.AddSeparator();
            menu.Add("C# Test Script", CreateScript, new object[] { item.asset, "C# Test Script" });
            menu.AddSeparator();
            menu.Add("Assembly Definition", CreateScript, new object[] { item.asset, "Assembly Definition" });
            menu.Add("Assembly Definition Reference", CreateScript, new object[] { item.asset, "Assembly Definition Reference" });

            menu.Show();
        }
    }
}