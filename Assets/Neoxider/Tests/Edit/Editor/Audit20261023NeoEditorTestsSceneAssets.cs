using System;
using System.IO;
using UnityEditor;

namespace Neo.Editor.Tests
{
    /// <summary>Copies persistent test scenes without touching the current scene setup.</summary>
    internal static class Audit20261023NeoEditorTestsSceneAssets
    {
        private const string FixtureDirectory = "/Tests/Edit/Editor/SceneFixtures/";

        internal static SceneAsset EmptySceneAsset => RequireSeed("Audit20261023NeoEditorTestsEmptyScene.unity");

        internal static void CreateEmptySceneAsset(string ownedAssetPath)
        {
            CopySeed("Audit20261023NeoEditorTestsEmptyScene.unity", ownedAssetPath);
        }

        internal static void CreateMissingScriptSceneAsset(string ownedAssetPath)
        {
            CopySeed("Audit20261023NeoEditorTestsMissingScriptScene.unity", ownedAssetPath);
        }

        private static SceneAsset RequireSeed(string fileName)
        {
            string path = Audit20261023NeoEditorTestsPaths.PackageAssetRoot + FixtureDirectory + fileName;
            SceneAsset seed = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (seed == null) throw new InvalidOperationException("Native test scene seed is unavailable: " + path);
            return seed;
        }

        private static void CopySeed(string fileName, string ownedAssetPath)
        {
            if (string.IsNullOrEmpty(ownedAssetPath) ||
                !ownedAssetPath.StartsWith("Assets/Audit20261023", StringComparison.Ordinal) ||
                !ownedAssetPath.EndsWith(".unity", StringComparison.Ordinal) ||
                ownedAssetPath.Contains("\\") || ownedAssetPath.Contains("/../") || ownedAssetPath.Contains("/./"))
                throw new ArgumentException("Use a fresh .unity path in an owned Assets/Audit20261023 folder.", nameof(ownedAssetPath));

            string directory = Path.GetDirectoryName(ownedAssetPath)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(directory))
                throw new ArgumentException("Create the owned destination folder first.", nameof(ownedAssetPath));
            if (File.Exists(Audit20261023NeoEditorTestsPaths.ToDiskPath(ownedAssetPath)) ||
                !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(ownedAssetPath)))
                throw new InvalidOperationException("Refusing to overwrite a scene asset: " + ownedAssetPath);

            string source = AssetDatabase.GetAssetPath(RequireSeed(fileName));
            if (!AssetDatabase.CopyAsset(source, ownedAssetPath))
                throw new InvalidOperationException("Could not copy the native test scene seed to " + ownedAssetPath);
            try
            {
                AssetDatabase.ImportAsset(ownedAssetPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ownedAssetPath) == null)
                    throw new InvalidOperationException("The copied fixture is not a native SceneAsset: " + ownedAssetPath);
            }
            catch
            {
                AssetDatabase.DeleteAsset(ownedAssetPath);
                throw;
            }
        }
    }
}
