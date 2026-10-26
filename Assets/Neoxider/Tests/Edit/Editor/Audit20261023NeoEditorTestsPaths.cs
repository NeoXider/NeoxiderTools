using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>Resolves the installed package and only its current-version sample identities.</summary>
    internal static class Audit20261023NeoEditorTestsPaths
    {
        internal const string PackageName = "com.neoxider.tools";

        [Serializable]
        internal sealed class PackageManifest
        {
            public string name;
            public string displayName;
            public string version;
            public SampleManifest[] samples;
        }

        [Serializable]
        internal sealed class SampleManifest
        {
            public string displayName;
            public string path;
        }

        internal static PackageInfo RegisteredPackage
        {
            get
            {
                foreach (PackageInfo package in PackageInfo.GetAllRegisteredPackages())
                    if (package.name == PackageName) return package;
                return null;
            }
        }

        internal static string PackageAssetRoot => RegisteredPackage?.assetPath ?? "Assets/Neoxider";
        internal static string PackageDiskRoot => Path.GetFullPath(RegisteredPackage?.resolvedPath ?? "Assets/Neoxider");

        internal static PackageManifest Manifest
        {
            get
            {
                string path = Path.Combine(PackageDiskRoot, "package.json");
                PackageManifest manifest = JsonUtility.FromJson<PackageManifest>(File.ReadAllText(path));
                if (manifest == null || manifest.name != PackageName || string.IsNullOrEmpty(manifest.version) ||
                    manifest.samples == null || manifest.samples.Length == 0)
                    throw new InvalidDataException("Expected a nonempty NeoxiderTools package/sample manifest: " + path);
                return manifest;
            }
        }

        internal static string ImportedRoot(PackageManifest manifest, SampleManifest sample)
        {
            return "Assets/Samples/" + manifest.displayName + "/" + manifest.version + "/" + sample.displayName;
        }

        internal static string DevelopmentRoot(SampleManifest sample) =>
            PackageAssetRoot + "/" + sample.path.Replace("Samples~/", "Samples/");

        internal static string DemoRoot
        {
            get
            {
                if (TryGetDemoRoot(out string root)) return root;
                throw new DirectoryNotFoundException("No Demo Scenes for package version " + Manifest.version);
            }
        }

        internal static bool TryGetDemoRoot(out string root)
        {
            PackageManifest manifest = Manifest;
            foreach (SampleManifest sample in manifest.samples)
            {
                if (sample.displayName != "Demo Scenes") continue;
                string imported = ImportedRoot(manifest, sample);
                string development = DevelopmentRoot(sample);
                string packaged = Path.Combine(PackageDiskRoot, sample.path).Replace('\\', '/');
                if (Directory.Exists(imported)) { root = imported; return true; }
                if (Directory.Exists(ToDiskPath(development))) { root = development; return true; }
                if (Directory.Exists(packaged)) { root = packaged; return true; }
            }
            root = null;
            return false;
        }

        /// <summary>Selects one visible current-version root for each exact manifest identity.</summary>
        internal static bool TrySelectNativeSampleRoots(PackageManifest manifest, IEnumerable<string> availableRoots,
            out string[] selectedRoots, out string[] missingIdentities)
        {
            HashSet<string> available = new HashSet<string>(StringComparer.Ordinal);
            foreach (string root in availableRoots) available.Add(root.Replace('\\', '/').TrimEnd('/'));
            HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
            List<string> selected = new List<string>();
            List<string> missing = new List<string>();
            if (manifest.samples == null || manifest.samples.Length == 0)
                throw new InvalidDataException("Native sample coverage requires nonempty manifest identities.");
            foreach (SampleManifest sample in manifest.samples)
            {
                if (string.IsNullOrEmpty(sample.displayName) || string.IsNullOrEmpty(sample.path) ||
                    !identities.Add(sample.displayName))
                    throw new InvalidDataException("Empty or duplicate manifest sample identity: " + sample.displayName);
                string imported = ImportedRoot(manifest, sample);
                string development = DevelopmentRoot(sample);
                if (available.Contains(imported)) selected.Add(imported);
                else if (available.Contains(development)) selected.Add(development);
                else missing.Add(sample.displayName + " (" + manifest.version + "; " + sample.path + ")");
            }
            selectedRoots = selected.ToArray();
            missingIdentities = missing.ToArray();
            return missing.Count == 0;
        }

        internal static string[] SampleRoots(bool requireAny = true)
        {
            PackageManifest manifest = Manifest;
            List<string> roots = new List<string>();
            foreach (SampleManifest sample in manifest.samples)
            {
                string imported = ImportedRoot(manifest, sample);
                string development = DevelopmentRoot(sample);
                string packaged = sample.path.Contains("Samples~/")
                    ? Path.Combine(PackageDiskRoot, sample.path).Replace('\\', '/')
                    : PackageAssetRoot + "/" + sample.path;
                if (Directory.Exists(imported)) roots.Add(imported);
                if (Directory.Exists(ToDiskPath(development))) roots.Add(development);
                if (Directory.Exists(ToDiskPath(packaged)) && !roots.Contains(packaged)) roots.Add(packaged);
            }
            if (requireAny && roots.Count == 0) throw new DirectoryNotFoundException("No current-version NeoxiderTools samples found.");
            return roots.ToArray();
        }

        internal static string ToDiskPath(string path)
        {
            string assetRoot = PackageAssetRoot;
            if (path == assetRoot) return PackageDiskRoot;
            if (path.StartsWith(assetRoot + "/", StringComparison.Ordinal))
                return Path.Combine(PackageDiskRoot, path.Substring(assetRoot.Length + 1));
            return Path.GetFullPath(path);
        }

        internal static IEnumerable<string> EnumerateSampleFiles(string pattern)
        {
            foreach (string root in SampleRoots())
            {
                string diskRoot = ToDiskPath(root);
                foreach (string file in Directory.EnumerateFiles(diskRoot, pattern, SearchOption.AllDirectories))
                {
                    string relative = file.Substring(diskRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    yield return (root + "/" + relative).Replace('\\', '/');
                }
            }
        }
    }
}
