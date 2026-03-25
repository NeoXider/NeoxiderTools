using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor
{
    /// <summary>
    ///     Utility to add references to all Neo assemblies in Neo.Editor.asmdef.
    ///     Fixes component display when the package is installed via Package Manager.
    /// </summary>
    public static class NeoEditorAsmdefFixer
    {
        [MenuItem("Tools/Neoxider/Fix Editor Assembly References")]
        public static void FixEditorAssemblyReferences()
        {
            string editorAsmdefPath = FindEditorAsmdefPath();

            if (string.IsNullOrEmpty(editorAsmdefPath))
            {
                Debug.LogError("[Neoxider] Could not find Neo.Editor.asmdef");
                return;
            }

            // Find all Neo asmdef files
            List<string> neoAsmdefGUIDs = FindAllNeoAsmdefGUIDs();

            if (neoAsmdefGUIDs.Count == 0)
            {
                Debug.LogWarning("[Neoxider] No additional Neo asmdef files found");
                return;
            }

            // Read current asmdef
            string json = File.ReadAllText(editorAsmdefPath);

            // See which GUIDs are already referenced
            int addedCount = 0;
            foreach (string guid in neoAsmdefGUIDs)
            {
                if (!json.Contains(guid))
                {
                    // Locate references array and insert new GUID
                    int referencesStart = json.IndexOf("\"references\": [");
                    if (referencesStart != -1)
                    {
                        int arrayStart = json.IndexOf('[', referencesStart);
                        int insertPosition = arrayStart + 1;

                        // Append new reference
                        string newReference = $"\n    \"GUID:{guid}\",";
                        json = json.Insert(insertPosition, newReference);
                        addedCount++;
                    }
                }
            }

            if (addedCount > 0)
            {
                File.WriteAllText(editorAsmdefPath, json);
                AssetDatabase.Refresh();
                Debug.Log(
                    $"[Neoxider] Added {addedCount} reference(s) to Neo.Editor.asmdef. Unity will recompile scripts.");
            }
            else
            {
                Debug.Log("[Neoxider] All required references are already present in Neo.Editor.asmdef");
            }
        }

        private static string FindEditorAsmdefPath()
        {
            // Search in Assets
            string[] assetsPaths = AssetDatabase.FindAssets("Neo.Editor t:asmdef");
            if (assetsPaths.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(assetsPaths[0]);
            }

            // Search in Packages
            string[] files = Directory.GetFiles("Packages", "Neo.Editor.asmdef", SearchOption.AllDirectories);
            return files.Length > 0 ? files[0] : null;
        }

        private static List<string> FindAllNeoAsmdefGUIDs()
        {
            List<string> guids = new();

            // Find asmdef files whose name starts with Neo.
            string[] foundAssets = AssetDatabase.FindAssets("Neo t:asmdef");

            foreach (string guid in foundAssets)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                // Exclude Neo.Editor.asmdef itself
                if (fileName != "Neo.Editor" && fileName.StartsWith("Neo"))
                {
                    guids.Add(guid);
                }
            }

            return guids;
        }
    }
}
