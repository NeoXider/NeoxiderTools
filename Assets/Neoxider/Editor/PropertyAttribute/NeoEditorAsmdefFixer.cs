using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
        [MenuItem("Neoxider/Tools/Fix Editor Assembly References", false, 140)]
        public static void FixEditorAssemblyReferences()
        {
            string editorAsmdefPath = FindEditorAsmdefPath();

            if (string.IsNullOrEmpty(editorAsmdefPath))
            {
                Debug.LogError("[Neoxider] Could not find Neo.Editor.asmdef");
                return;
            }

            List<string> neoAsmdefGUIDs = FindAllNeoAsmdefGUIDs();

            if (neoAsmdefGUIDs.Count == 0)
            {
                Debug.LogWarning("[Neoxider] No additional Neo asmdef files found");
                return;
            }

            string json = File.ReadAllText(editorAsmdefPath);

            Dictionary<string, string> namesByGuid = new();
            foreach (string guid in neoAsmdefGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ReferenceData data = JsonUtility.FromJson<ReferenceData>(File.ReadAllText(path));
                if (data != null && (data.includePlatforms == null || data.includePlatforms.Length == 0) &&
                    !Contains(data.optionalUnityReferences, "TestAssemblies") &&
                    !Contains(data.defineConstraints, "UNITY_INCLUDE_TESTS"))
                {
                    namesByGuid[guid] = data.name;
                }
            }

            json = AddRuntimeReferences(json, namesByGuid, out int addedCount);
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

        // Editor/test assemblies can depend back on Neo.Editor. Adding them creates an assembly cycle.
        // Existing references may use either names or GUIDs; preserve both forms and all other JSON fields.
        internal static string AddRuntimeReferences(string json, IDictionary<string, string> namesByGuid,
            out int addedCount)
        {
            addedCount = 0;
            ReferenceData current = JsonUtility.FromJson<ReferenceData>(json);
            Match array = Regex.Match(json, "\"references\"\\s*:\\s*\\[");
            if (current == null || !array.Success)
            {
                return json;
            }

            HashSet<string> existing = new(current.references ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<string> additions = new();
            foreach (KeyValuePair<string, string> candidate in namesByGuid)
            {
                string name = candidate.Value;
                string reference = "GUID:" + candidate.Key;
                if (string.IsNullOrEmpty(name) || !name.StartsWith("Neo.", StringComparison.Ordinal) ||
                    name.Contains("Editor") || name.Contains("Tests") ||
                    existing.Contains(name) || existing.Contains(reference))
                {
                    continue;
                }

                existing.Add(name);
                existing.Add(reference);
                additions.Add("\"" + reference + "\"");
            }

            addedCount = additions.Count;
            if (addedCount == 0)
            {
                return json;
            }

            string insertion = "\n        " + string.Join(",\n        ", additions);
            if (current.references != null && current.references.Length > 0)
            {
                insertion += ",";
            }

            return json.Insert(array.Index + array.Length, insertion);
        }

        private static bool Contains(string[] values, string value)
        {
            return values != null && Array.IndexOf(values, value) >= 0;
        }

        [Serializable]
        private sealed class ReferenceData
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
            public string[] optionalUnityReferences;
            public string[] defineConstraints;
        }

        private static string FindEditorAsmdefPath()
        {
            string[] assetsPaths = AssetDatabase.FindAssets("Neo.Editor t:asmdef");
            foreach (string guid in assetsPaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == "Neo.Editor")
                {
                    return path;
                }
            }

            string[] files = Directory.GetFiles("Packages", "Neo.Editor.asmdef", SearchOption.AllDirectories);
            return files.Length > 0 ? files[0] : null;
        }

        private static List<string> FindAllNeoAsmdefGUIDs()
        {
            List<string> guids = new();

            string[] foundAssets = AssetDatabase.FindAssets("Neo t:asmdef");

            foreach (string guid in foundAssets)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                if (fileName != "Neo.Editor" && fileName.StartsWith("Neo"))
                {
                    guids.Add(guid);
                }
            }

            return guids;
        }
    }
}
