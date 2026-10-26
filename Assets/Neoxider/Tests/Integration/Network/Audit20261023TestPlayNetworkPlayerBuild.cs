#if UNITY_EDITOR && MIRROR && UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Neo.Tests.Network.Integration
{
    /// <summary>Operator-only entrypoint in an isolated validation project, after every source writer finishes.</summary>
    public static class Audit20261023TestPlayNetworkPlayerBuild
    {
        [Serializable]
        private sealed class BuildReceipt
        {
            public string runId;
            public string snapshotSha;
            public string unityVersion;
            public string company;
            public string target = "StandaloneWindows64";
            public string options = "Development,IncludeTestAssemblies,StrictMode";
            public string[] products;
            public string[] executables;
            public bool passed;
        }

        public static void Build()
        {
            string runId = Argument("RunId");
            string snapshot = Argument("Snapshot");
            string output = Path.GetFullPath(Argument("Output")).TrimEnd(Path.DirectorySeparatorChar);
            if (!Regex.IsMatch(runId, "^[a-f0-9]{32}$")
                || !Regex.IsMatch(snapshot, "^(?:[a-fA-F0-9]{40}|[a-fA-F0-9]{64})$"))
                throw new ArgumentException("Use a unique 32-hex run ID and frozen snapshot SHA.");
            if (Directory.Exists(output) || File.Exists(output))
                throw new IOException("Build output already exists; no reuse or takeover.");
            string sceneFolder = "Assets/NeoQA_Network_" + runId;
            if (AssetDatabase.IsValidFolder(sceneFolder) || Directory.Exists(sceneFolder))
                throw new IOException("Task-owned bootstrap scene folder already exists.");
            Directory.CreateDirectory(output);
            BuildReceipt receipt = new BuildReceipt
            {
                runId = runId, snapshotSha = snapshot, unityVersion = Application.unityVersion,
                company = "NeoQA_Network_" + runId,
                products = new[] { "NetworkMatrix_server", "NetworkMatrix_client-a", "NetworkMatrix_client-b" },
                executables = new string[3]
            };
            string company = PlayerSettings.companyName;
            string product = PlayerSettings.productName;
            string version = PlayerSettings.bundleVersion;
            bool ownsFolder = false;
            try
            {
                if (string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets", "NeoQA_Network_" + runId)))
                    throw new IOException("Cannot create the owned bootstrap scene folder.");
                ownsFolder = true;
                string scenePath = sceneFolder + "/Empty.unity";
                // Building from a persistent copy also works beside Unity's untitled startup
                // scene and preserves any borrowed scene setup and unsaved work.
                string seedPath = AssetDatabase.GUIDToAssetPath("0ac7559d257e48f1b4a921a9753ec320");
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(seedPath) == null ||
                    !AssetDatabase.CopyAsset(seedPath, scenePath))
                    throw new IOException("Cannot copy the native empty-scene fixture for the player build.");
                PlayerSettings.companyName = receipt.company;
                PlayerSettings.bundleVersion = "11.0.0";
                for (int i = 0; i < receipt.products.Length; i++)
                {
                    PlayerSettings.productName = receipt.products[i];
                    string directory = Path.Combine(output, receipt.products[i]);
                    Directory.CreateDirectory(directory);
                    string executable = Path.Combine(directory, "NetworkMatrix.exe");
                    receipt.executables[i] = executable;
                    BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = new[] { scenePath }, locationPathName = executable,
                        target = BuildTarget.StandaloneWindows64,
                        options = BuildOptions.Development | BuildOptions.IncludeTestAssemblies | BuildOptions.StrictMode
                    });
                    if (report.summary.result != BuildResult.Succeeded)
                        throw new InvalidOperationException("Matrix build failed: " + receipt.products[i]);
                }
                receipt.passed = true;
            }
            finally
            {
                PlayerSettings.companyName = company;
                PlayerSettings.productName = product;
                PlayerSettings.bundleVersion = version;
                if (ownsFolder && !AssetDatabase.DeleteAsset(sceneFolder))
                {
                    receipt.passed = false;
                    Debug.LogError("Could not release the owned matrix bootstrap scene folder: " + sceneFolder);
                }
                File.WriteAllText(Path.Combine(output, "build-receipt.json"), JsonUtility.ToJson(receipt, true),
                    new UTF8Encoding(false));
            }
        }

        private static string Argument(string suffix)
        {
            string key = "-neoNetworkMatrixBuild" + suffix;
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, key);
            if (index < 0 || index + 1 >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index + 1]))
                throw new ArgumentException("Required argument: " + key);
            return arguments[index + 1];
        }
    }
}
#endif
