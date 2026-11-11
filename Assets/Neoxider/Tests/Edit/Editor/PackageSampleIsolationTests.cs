using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Keeps the package installable without its samples: an asset outside the sample roots must not
    ///     reference an asset that ships only inside <c>Samples~</c> (or the development <c>Samples</c> folder),
    ///     because a UPM install that skips "Import sample" has no such asset and the reference turns missing.
    ///     The files are parsed from disk, so the check also works while <c>Samples~</c> is hidden from Unity.
    /// </summary>
    public sealed class PackageSampleIsolationTests
    {
        private const string ImportedSamplesRoot = "Assets/Samples/";

        private static readonly string[] SampleRootNames = { "Samples~", "Samples" };

        private static readonly HashSet<string> YamlAssetExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".unity", ".prefab", ".asset", ".mat", ".controller", ".overrideController", ".anim", ".mask",
            ".playable", ".physicMaterial", ".physicsMaterial2D", ".spriteatlas", ".spriteatlasv2", ".mixer",
            ".renderTexture", ".lighting", ".terrainlayer", ".preset", ".signal", ".guiskin", ".fontsettings",
            ".brush", ".flare", ".cubemap", ".giparams"
        };

        private static readonly HashSet<string> UiToolkitExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".uxml", ".uss", ".tss"
        };

        private static readonly HashSet<string> AssemblyDefinitionExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".asmdef", ".asmref"
        };

        private static readonly Regex MetaGuidRegex =
            new(@"^guid:\s*([0-9a-fA-F]{32})\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex YamlReferenceRegex = new(@"guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);

        private static readonly Regex UiToolkitReferenceRegex =
            new(@"[?&]guid=([0-9a-fA-F]{32})", RegexOptions.Compiled);

        private static readonly Regex AssemblyReferenceRegex =
            new(@"GUID:([0-9a-fA-F]{32})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly List<string> _temporaryRoots = new();
        private PackageScan _package;

        private PackageScan Package => _package ??= PackageScan.Read(Audit20261023NeoEditorTestsPaths.PackageDiskRoot,
            Audit20261023NeoEditorTestsPaths.DemosRoot);

        [TearDown]
        public void DeleteTemporaryPackages()
        {
            foreach (string root in _temporaryRoots)
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }

            _temporaryRoots.Clear();
        }

        [Test]
        public void PackageAssets_DoNotReferenceGuidsThatShipOnlyInsideSamples()
        {
            PackageScan scan = Package;
            if (scan.SampleGuidCount == 0)
            {
                Assert.Ignore("No demo tree (Assets/NeoxiderDemos) on disk; nothing to isolate from " + scan.Root + ".");
            }

            AssertScannedRealAssets(scan);
            AssertNoFailures(FindSampleOnlyReferences(scan),
                "Package assets reference assets that exist only inside a sample. Give the package its own copy of " +
                "the dependency with a new GUID (so it cannot collide with a copy users imported from an older " +
                "sample) and repoint the package asset, or move the package asset into the sample:");
        }

        [Test]
        public void PackageAndSampleAssets_DoNotShareGuids()
        {
            PackageScan scan = Package;
            Assert.That(scan.Owners.Count, Is.GreaterThan(0), "The scan must read package .meta files.");
            AssertNoFailures(FindSharedGuids(scan),
                "A GUID is owned by more than one package asset (a copy left behind by a move?). Unity reassigns one " +
                "of them on import and every reference to the other breaks:");
        }

        [Test]
        public void PackageAssets_DoNotResolveReferencesIntoImportedSamples()
        {
            PackageScan scan = Package;
            AssertScannedRealAssets(scan);
            List<string> failures = new List<string>();
            HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, string> resolvedByGuid = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (PackageReference reference in scan.References)
            {
                if (!resolvedByGuid.TryGetValue(reference.Guid, out string resolved))
                {
                    resolved = AssetDatabase.GUIDToAssetPath(reference.Guid);
                    resolvedByGuid.Add(reference.Guid, resolved);
                }

                if (string.IsNullOrEmpty(resolved) ||
                    !resolved.StartsWith(ImportedSamplesRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (reported.Add(reference.SourcePath + "|" + reference.Guid))
                {
                    failures.Add(reference.SourcePath + " -> " + resolved + " (guid " + reference.Guid + ")");
                }
            }

            AssertNoFailures(failures,
                "Package assets resolve references into an imported sample under Assets/Samples. Usually the project " +
                "still holds an imported sample copy that shares a GUID with a package asset (Unity then keeps one " +
                "GUID owner and reassigns or ignores the other). Delete the stale imported copy:");
        }

        [Test]
        public void Scan_FlagsSampleOnlyReferencesFromYamlUiToolkitAndAssemblyFiles()
        {
            const string sampleSprite = "a0000000000000000000000000000001";
            const string sampleAssembly = "a0000000000000000000000000000002";
            const string packageSprite = "b0000000000000000000000000000001";
            string root = CreateTemporaryPackage();
            WriteMeta(root, "Samples~/Demo/Sprites/Icon.png", sampleSprite);
            WriteMeta(root, "Samples~/Demo/Scripts/Neo.Demo.asmdef", sampleAssembly);
            WriteMeta(root, "Art/Icon.png", packageSprite);
            WriteYamlAsset(root, "Prefabs/UsesSample.prefab", sampleSprite);
            WriteYamlAsset(root, "Prefabs/UsesPackage.prefab", packageSprite);
            WriteYamlAsset(root, "Documentation~/Hidden.prefab", sampleSprite);
            WriteYamlAsset(root, "Samples~/Demo/Scenes/SampleUsesSample.prefab", sampleSprite);
            WriteFile(root, "UI/Panel.uss",
                ".icon { background-image: url(\"project://database/Assets/Icon.png?fileID=2800000&guid=" +
                sampleSprite + "&type=3#Icon\"); }");
            WriteFile(root, "Runtime/Neo.Runtime.asmdef",
                "{ \"name\": \"Neo.Runtime\", \"references\": [ \"GUID:" + sampleAssembly + "\" ] }");

            PackageScan scan = PackageScan.Read(root);
            List<string> sources = new List<string>();
            foreach (string failure in FindSampleOnlyReferences(scan))
            {
                sources.Add(failure.Substring(0, failure.IndexOf(" -> ", StringComparison.Ordinal)));
            }

            Assert.That(sources,
                Is.EquivalentTo(new[] { "Prefabs/UsesSample.prefab", "UI/Panel.uss", "Runtime/Neo.Runtime.asmdef" }),
                "Only package files outside the sample and hidden roots may be reported.");
            Assert.That(scan.InspectedAssetCount, Is.EqualTo(4), "Two prefabs, one USS and one asmdef are package assets.");
        }

        [Test]
        public void Scan_FlagsGuidsSharedByPackageAndSampleButNotSampleTwins()
        {
            const string moved = "c0000000000000000000000000000001";
            const string twin = "c0000000000000000000000000000002";
            string root = CreateTemporaryPackage();
            WriteMeta(root, "Art/Moved.png", moved);
            WriteMeta(root, "Samples~/Demo/Moved.png", moved);
            WriteMeta(root, "Samples~/Demo/Twin.png", twin);
            WriteMeta(root, "Samples/Demo/Twin.png", twin);

            List<string> failures = FindSharedGuids(PackageScan.Read(root));

            Assert.That(failures, Has.Count.EqualTo(1), string.Join(Environment.NewLine, failures));
            Assert.That(failures[0], Does.StartWith(moved + ": "));
        }

        private static List<string> FindSampleOnlyReferences(PackageScan scan)
        {
            List<string> failures = new List<string>();
            foreach (PackageReference reference in scan.References)
            {
                if (scan.Owners.TryGetValue(reference.Guid, out List<AssetOwner> owners) && AllInSamples(owners))
                {
                    failures.Add(reference.SourcePath + " -> " + owners[0].RelativePath + " (guid " + reference.Guid +
                                 ")");
                }
            }

            return failures;
        }

        private static List<string> FindSharedGuids(PackageScan scan)
        {
            List<string> failures = new List<string>();
            foreach (KeyValuePair<string, List<AssetOwner>> pair in scan.Owners)
            {
                HashSet<string> identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (AssetOwner owner in pair.Value)
                {
                    identities.Add(owner.Identity);
                }

                if (identities.Count < 2)
                {
                    continue;
                }

                StringBuilder paths = new StringBuilder();
                foreach (AssetOwner owner in pair.Value)
                {
                    paths.Append(paths.Length == 0 ? string.Empty : ", ").Append(owner.RelativePath);
                }

                failures.Add(pair.Key + ": " + paths);
            }

            return failures;
        }

        private static bool AllInSamples(List<AssetOwner> owners)
        {
            foreach (AssetOwner owner in owners)
            {
                if (!owner.InSample)
                {
                    return false;
                }
            }

            return owners.Count > 0;
        }

        private static void AssertScannedRealAssets(PackageScan scan)
        {
            Assert.That(scan.InspectedAssetCount, Is.GreaterThan(0), "The scan must inspect real package assets.");
            Assert.That(scan.References.Count, Is.GreaterThan(0),
                "The scan found no GUID reference in any package asset, so the parser is broken.");
        }

        private static void AssertNoFailures(List<string> failures, string header)
        {
            if (failures.Count == 0)
            {
                return;
            }

            failures.Sort(StringComparer.Ordinal);
            Assert.Fail(header + Environment.NewLine + string.Join(Environment.NewLine, failures));
        }

        private string CreateTemporaryPackage()
        {
            string root = Path.Combine(Path.GetTempPath(), "NeoPackageSampleIsolation_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            _temporaryRoots.Add(root);
            return root;
        }

        private static void WriteMeta(string root, string assetPath, string guid)
        {
            WriteFile(root, assetPath + ".meta", "fileFormatVersion: 2\nguid: " + guid + "\n");
        }

        private static void WriteYamlAsset(string root, string assetPath, string referencedGuid)
        {
            WriteFile(root, assetPath,
                "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &1\nMonoBehaviour:\n" +
                "  m_Sprite: {fileID: 21300000, guid: " + referencedGuid + ", type: 3}\n");
        }

        private static void WriteFile(string root, string relativePath, string text)
        {
            string path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }

        private readonly struct AssetOwner
        {
            public AssetOwner(string relativePath, bool inSample, string identity)
            {
                RelativePath = relativePath;
                InSample = inSample;
                Identity = identity;
            }

            public string RelativePath { get; }
            public bool InSample { get; }

            /// <summary>The same sample file in Samples~ and Samples counts once; everything else is distinct.</summary>
            public string Identity { get; }
        }

        private readonly struct PackageReference
        {
            public PackageReference(string sourcePath, string guid)
            {
                SourcePath = sourcePath;
                Guid = guid;
            }

            public string SourcePath { get; }
            public string Guid { get; }
        }

        /// <summary>GUID owners of the whole package and the references held by its non-sample files.</summary>
        private sealed class PackageScan
        {
            private PackageScan(string root)
            {
                Root = root;
            }

            public string Root { get; }
            public Dictionary<string, List<AssetOwner>> Owners { get; } = new(StringComparer.Ordinal);
            public List<PackageReference> References { get; } = new();
            public int InspectedAssetCount { get; private set; }
            public int SampleGuidCount { get; private set; }

            public static PackageScan Read(string packageRoot, string demosRoot = null)
            {
                string root = Path.GetFullPath(packageRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Assert.That(Directory.Exists(root), Is.True, "Package root not found on disk: " + root);
                PackageScan scan = new PackageScan(root);
                foreach (string file in Directory.GetFiles(root))
                {
                    scan.ReadPackageFile(file);
                }

                foreach (string directory in Directory.GetDirectories(root))
                {
                    string name = Path.GetFileName(directory);
                    if (Array.IndexOf(SampleRootNames, name) >= 0)
                    {
                        scan.ReadSampleTree(directory, name);
                    }
                    else if (!IsHiddenFromUnity(name))
                    {
                        scan.ReadPackageTree(directory);
                    }
                }

                // WHY: since 12.2.0 the demos live outside the package (Assets/NeoxiderDemos); their GUIDs are the
                // sample side of the isolation check.
                if (!string.IsNullOrEmpty(demosRoot) && Directory.Exists(demosRoot))
                {
                    scan.ReadExternalSampleTree(Path.GetFullPath(demosRoot));
                }

                return scan;
            }

            private void ReadExternalSampleTree(string demosRoot)
            {
                string name = Path.GetFileName(demosRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                foreach (string file in Directory.EnumerateFiles(demosRoot, "*.meta", SearchOption.AllDirectories))
                {
                    string ownGuid = ReadOwnGuid(File.ReadAllText(file));
                    if (ownGuid == null)
                    {
                        continue;
                    }

                    string inDemos = file.Substring(demosRoot.Length, file.Length - demosRoot.Length - ".meta".Length)
                        .Replace('\\', '/').TrimStart('/');
                    AddOwner(ownGuid, new AssetOwner(name + "/" + inDemos, true, "sample:" + inDemos));
                    SampleGuidCount++;
                }
            }

            private void ReadPackageTree(string directory)
            {
                foreach (string file in Directory.GetFiles(directory))
                {
                    ReadPackageFile(file);
                }

                foreach (string child in Directory.GetDirectories(directory))
                {
                    if (!IsHiddenFromUnity(Path.GetFileName(child)))
                    {
                        ReadPackageTree(child);
                    }
                }
            }

            private void ReadSampleTree(string sampleRoot, string rootName)
            {
                foreach (string file in Directory.EnumerateFiles(sampleRoot, "*.meta", SearchOption.AllDirectories))
                {
                    string ownGuid = ReadOwnGuid(File.ReadAllText(file));
                    if (ownGuid == null)
                    {
                        continue;
                    }

                    string relative = Relative(file.Substring(0, file.Length - ".meta".Length));
                    string inSampleRoot = relative.Substring(rootName.Length).TrimStart('/');
                    AddOwner(ownGuid, new AssetOwner(relative, true, "sample:" + inSampleRoot));
                    SampleGuidCount++;
                }
            }

            private void ReadPackageFile(string file)
            {
                string extension = Path.GetExtension(file);
                if (string.Equals(extension, ".meta", StringComparison.OrdinalIgnoreCase))
                {
                    string text = File.ReadAllText(file);
                    string ownGuid = ReadOwnGuid(text);
                    string assetPath = Relative(file.Substring(0, file.Length - ".meta".Length));
                    if (ownGuid != null)
                    {
                        AddOwner(ownGuid, new AssetOwner(assetPath, false, "package:" + assetPath));
                    }

                    AddReferences(assetPath + ".meta", YamlReferenceRegex, text, ownGuid);
                    return;
                }

                if (YamlAssetExtensions.Contains(extension) && StartsWithYamlHeader(file))
                {
                    InspectedAssetCount++;
                    AddReferences(Relative(file), YamlReferenceRegex, File.ReadAllText(file), null);
                }
                else if (UiToolkitExtensions.Contains(extension))
                {
                    InspectedAssetCount++;
                    AddReferences(Relative(file), UiToolkitReferenceRegex, File.ReadAllText(file), null);
                }
                else if (AssemblyDefinitionExtensions.Contains(extension))
                {
                    InspectedAssetCount++;
                    AddReferences(Relative(file), AssemblyReferenceRegex, File.ReadAllText(file), null);
                }
            }

            private void AddReferences(string sourcePath, Regex regex, string text, string ownGuid)
            {
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (Match match in regex.Matches(text))
                {
                    string guid = match.Groups[1].Value.ToLowerInvariant();
                    if (guid != ownGuid && seen.Add(guid))
                    {
                        References.Add(new PackageReference(sourcePath, guid));
                    }
                }
            }

            private void AddOwner(string guid, AssetOwner owner)
            {
                if (!Owners.TryGetValue(guid, out List<AssetOwner> owners))
                {
                    owners = new List<AssetOwner>();
                    Owners.Add(guid, owners);
                }

                owners.Add(owner);
            }

            private string Relative(string path)
            {
                return path.Substring(Root.Length).Replace('\\', '/').TrimStart('/');
            }

            private static string ReadOwnGuid(string metaText)
            {
                Match match = MetaGuidRegex.Match(metaText);
                return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
            }

            private static bool IsHiddenFromUnity(string name)
            {
                // WHY: Unity imports neither "name~" nor dot folders, so their files are not package assets.
                return name.EndsWith("~", StringComparison.Ordinal) || name.StartsWith(".", StringComparison.Ordinal);
            }

            private static bool StartsWithYamlHeader(string file)
            {
                byte[] header = new byte[5];
                using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int read = stream.Read(header, 0, header.Length);
                    return read == header.Length && Encoding.ASCII.GetString(header) == "%YAML";
                }
            }
        }
    }
}
