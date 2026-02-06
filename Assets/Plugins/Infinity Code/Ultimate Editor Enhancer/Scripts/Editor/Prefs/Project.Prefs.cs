/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public static partial class Prefs
    {
        public static bool projectCreateCustomEditor = true;
        public static bool projectCreateFolder = true;
        public static bool projectCreateFolderByShortcut = true;
        public static bool projectCreateScript = true;
        public static bool projectCreateMaterial = true;
        public static bool projectFileExtension = true;
        public static bool projectPlayAudio = true;

        public class ProjectManager : StandalonePrefManager<ProjectManager>, IHasShortcutPref, IStateablePref
        {
            public override IEnumerable<string> keywords
            {
                get
                {
                    return new[]
                    {
                        "Create Folder By Shortcut",
                        "Create Script Button",
                        "Play Audio Button",
                    };
                }
            }

            public override void Draw()
            {
                projectCreateCustomEditor = EditorGUILayout.ToggleLeft("Create Custom Editor For MonoBehaviour", projectCreateCustomEditor);
                projectCreateFolder = EditorGUILayout.ToggleLeft("Create Folder Button", projectCreateFolder);
                projectCreateFolderByShortcut = EditorGUILayout.ToggleLeft("Create Folder By Shortcut (F7)", projectCreateFolderByShortcut);
                projectCreateMaterial = EditorGUILayout.ToggleLeft("Create Material Button", projectCreateMaterial);
                projectCreateScript = EditorGUILayout.ToggleLeft("Create Script Button", projectCreateScript);
                projectFileExtension = EditorGUILayout.ToggleLeft("File Extension", projectFileExtension);
                projectPlayAudio = EditorGUILayout.ToggleLeft("Play Audio Button", projectPlayAudio);
            }

            public string GetMenuName()
            {
                return "Project";
            }

            public IEnumerable<Shortcut> GetShortcuts()
            {
                if (projectCreateFolderByShortcut)
                {
                    return new[]
                    {
                        new Shortcut("Create Folder", "Project", KeyCode.F7),
                    };
                }

                return null;
            }

            public void SetState(bool state)
            {
                projectCreateFolder = state;
                projectCreateFolderByShortcut = state;
                projectCreateMaterial = state;
                projectCreateScript = state;
                
                ProjectFolderIconManager.SetState(state);
            }
        }
    }
}