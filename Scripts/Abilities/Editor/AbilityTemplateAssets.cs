using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Neo.Abilities.Editor
{
    /// <summary>
    ///     Turns an <see cref="AbilityTemplateBundle" /> into project assets: one <see cref="AbilityDefinition" /> per
    ///     ability, one <see cref="ModifierDefinition" /> per modifier and an <see cref="AbilityLibrary" /> holding them
    ///     all. Existing assets with the same file names are left untouched (unique names are generated). Undo-able.
    /// </summary>
    public static class AbilityTemplateAssets
    {
        /// <summary>Converts an absolute folder inside the project to an "Assets/..." path, or null.</summary>
        public static string ToProjectPath(string absoluteFolder)
        {
            if (string.IsNullOrEmpty(absoluteFolder))
            {
                return null;
            }

            string full = Path.GetFullPath(absoluteFolder).Replace('\\', '/');
            string assets = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            if (full == assets)
            {
                return "Assets";
            }

            return full.StartsWith(assets + "/") ? "Assets" + full.Substring(assets.Length) : null;
        }

        /// <summary>
        ///     Creates the assets of <paramref name="bundle" /> under <paramref name="folder" /> ("Assets/..."), returning
        ///     them in creation order: abilities, modifiers, then the library.
        /// </summary>
        public static List<ScriptableObject> CreateAssets(AbilityTemplateBundle bundle, string folder)
        {
            List<ScriptableObject> created = new List<ScriptableObject>();
            if (bundle == null || string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            {
                return created;
            }

            List<AbilityDefinition> abilities = new List<AbilityDefinition>();
            List<ModifierDefinition> modifiers = new List<ModifierDefinition>();
            for (int i = 0; i < bundle.Abilities.Count; i++)
            {
                AbilityBlueprint blueprint = bundle.Abilities[i];
                AbilityDefinition asset = ScriptableObject.CreateInstance<AbilityDefinition>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(blueprint), asset.Blueprint);
                Save(asset, folder, blueprint.Id);
                abilities.Add(asset);
                created.Add(asset);
            }

            for (int i = 0; i < bundle.Modifiers.Count; i++)
            {
                ModifierBlueprint blueprint = bundle.Modifiers[i];
                ModifierDefinition asset = ScriptableObject.CreateInstance<ModifierDefinition>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(blueprint), asset.Blueprint);
                Save(asset, folder, blueprint.Id);
                modifiers.Add(asset);
                created.Add(asset);
            }

            AbilityLibrary library = ScriptableObject.CreateInstance<AbilityLibrary>();
            SerializedObject serialized = new SerializedObject(library);
            Fill(serialized.FindProperty("_abilities"), abilities);
            Fill(serialized.FindProperty("_modifiers"), modifiers);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Save(library, folder, bundle.TemplateId + "_library");
            created.Add(library);
            AssetDatabase.SaveAssets();
            return created;
        }

        private static void Fill<T>(SerializedProperty list, List<T> items) where T : Object
        {
            list.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static void Save(ScriptableObject asset, string folder, string id)
        {
            string fileName = string.IsNullOrEmpty(id) ? asset.GetType().Name : id;
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + fileName + ".asset");
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, "Create " + fileName);
        }
    }
}
