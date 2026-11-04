using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoEditorTestsSampleEvidenceTests
    {
        private const string ScriptGuid = "1234567890abcdef1234567890abcdef";
        private const string RequiredType = "Neo.Samples.ModuleDemoSceneInfo";
        private const string ValidYaml = "%YAML 1.1\nm_Script: {fileID: 11500000, guid: " + ScriptGuid +
            ", type: 3}\nm_EditorClassIdentifier: " + RequiredType + "\n";

        [Test]
        public void DuplicateRootsForOneSample_CannotSubstituteForMissingManifestIdentity()
        {
            Audit20261023NeoEditorTestsPaths.PackageManifest manifest = CreateManifest();
            string demoImport = Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, manifest.samples[0]);
            string pagesImport = Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, manifest.samples[1]);
            string[] candidates = { demoImport, Audit20261023NeoEditorTestsPaths.DevelopmentRoot(manifest.samples[0]), pagesImport, demoImport };

            bool complete = Audit20261023NeoEditorTestsPaths.TrySelectNativeSampleRoots(manifest, candidates,
                out string[] selected, out string[] missing);

            Assert.That(complete, Is.False, "Four roots still represent only two identities.");
            CollectionAssert.AreEqual(new[] { demoImport, pagesImport }, selected);
            Assert.That(missing, Has.Length.EqualTo(1));
            Assert.That(missing[0], Does.Contain("Character Movement Fundamentals").And.Contain("11.0.0"));
        }

        [Test]
        public void AllManifestIdentities_SelectOneCurrentRootEach_PreferringImportedOverDevelopment()
        {
            Audit20261023NeoEditorTestsPaths.PackageManifest manifest = CreateManifest();
            string demoImport = Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, manifest.samples[0]);
            string pagesDevelopment = Audit20261023NeoEditorTestsPaths.DevelopmentRoot(manifest.samples[1]);
            string cmfImport = Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, manifest.samples[2]);
            string[] candidates =
            {
                Audit20261023NeoEditorTestsPaths.DevelopmentRoot(manifest.samples[0]), demoImport,
                pagesDevelopment, cmfImport, "Assets/UnrelatedSample", cmfImport
            };

            Assert.That(Audit20261023NeoEditorTestsPaths.TrySelectNativeSampleRoots(manifest, candidates,
                out string[] selected, out string[] missing), Is.True);

            CollectionAssert.AreEqual(new[] { demoImport, pagesDevelopment, cmfImport }, selected);
            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void OlderImportedVersion_CannotSatisfyTheCurrentManifestIdentity()
        {
            Audit20261023NeoEditorTestsPaths.PackageManifest manifest = CreateManifest();
            string[] candidates =
            {
                Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, manifest.samples[0]),
                Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, manifest.samples[1]),
                "Assets/Samples/NeoxiderTools/10.9.0/Character Movement Fundamentals"
            };
            Assert.That(Audit20261023NeoEditorTestsPaths.TrySelectNativeSampleRoots(manifest, candidates,
                out string[] selected, out string[] missing), Is.False);
            Assert.That(selected, Has.Length.EqualTo(2));
            Assert.That(missing, Has.Length.EqualTo(1));
            Assert.That(missing[0], Does.Contain("Character Movement Fundamentals"));
        }

        [Test]
        public void PartiallyVisibleSamples_ValidateEachVisibleIdentityAndSkipOnlyTheHiddenOne()
        {
            Audit20261023NeoEditorTestsPaths.PackageManifest manifest = CreateManifest();
            string demoDevelopment = Audit20261023NeoEditorTestsPaths.DevelopmentRoot(manifest.samples[0]);
            string pagesImport = Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, manifest.samples[1]);
            string[] visible = { demoDevelopment, pagesImport, "Package/Samples~/CharacterMovementFundamentals" };

            string[] selected = SampleAssetValidationTests.SelectVisibleNativeSampleRoots(manifest, visible,
                out string[] skipped);

            CollectionAssert.AreEqual(new[] { demoDevelopment, pagesImport }, selected,
                "A hidden Character Movement Fundamentals must not hide the Demo and NeoxiderPages checks.");
            Assert.That(skipped, Has.Length.EqualTo(1));
            Assert.That(skipped[0], Does.Contain("Character Movement Fundamentals"));
        }

        [Test]
        public void NoVisibleSample_IsIgnoredExplicitly_NeverPassedSilently()
        {
            Audit20261023NeoEditorTestsPaths.PackageManifest manifest = CreateManifest();
            string[] hidden = { "Package/Samples~/Demo", "Package/Samples~/NeoxiderPages" };

            IgnoreException ignore = Assert.Throws<IgnoreException>(() =>
                SampleAssetValidationTests.SelectVisibleNativeSampleRoots(manifest, hidden, out _));

            Assert.That(ignore.Message, Does.Contain("No native sample is visible").And.Contain("Demo Scenes"));
        }

        [Test]
        public void ReferenceClassification_SeparatesHiddenSampleDependenciesFromTrulyMissingAssets()
        {
            const string resolvedScript = "11111111111111111111111111111111";
            const string hiddenScript = "22222222222222222222222222222222";
            const string unknownScript = "33333333333333333333333333333333";
            const string visiblePrefab = "44444444444444444444444444444444";
            const string hiddenPrefab = "55555555555555555555555555555555";
            string yaml = "%YAML 1.1\n" +
                "  m_Script: {fileID: 11500000, guid: " + resolvedScript + ", type: 3}\n" +
                "  m_Script: {fileID: 11500000, guid: " + hiddenScript + ", type: 3}\n" +
                "  m_Script: {fileID: 11500000, guid: " + hiddenScript + ", type: 3}\n" +
                "  m_SourcePrefab: {fileID: 100100000, guid: " + visiblePrefab + ", type: 3}\n" +
                "  m_SourcePrefab: {fileID: 100100000, guid: " + hiddenPrefab + ", type: 3}\n";
            Dictionary<string, string> assets = new Dictionary<string, string>
            {
                { resolvedScript, "Assets/Sample/Resolved.cs" },
                { visiblePrefab, "Assets/Sample/Visible.prefab" }
            };
            HashSet<string> hiddenGuids = new HashSet<string> { hiddenScript, hiddenPrefab };
            List<string> prefabs = new List<string>();
            List<string> inHidden = new List<string>();
            List<string> missing = new List<string>();

            SampleAssetValidationTests.ClassifyReferences(yaml,
                guid => assets.TryGetValue(guid, out string path) ? path : string.Empty, hiddenGuids, prefabs, inHidden,
                missing);
            CollectionAssert.AreEqual(new[] { "Assets/Sample/Visible.prefab" }, prefabs);
            CollectionAssert.AreEqual(new[] { hiddenScript, hiddenPrefab }, inHidden);
            Assert.That(missing, Is.Empty, "Every unresolved reference here is explained by a hidden sample.");

            SampleAssetValidationTests.ClassifyReferences(
                "  m_Script: {fileID: 11500000, guid: " + unknownScript + ", type: 3}\n  m_Script: {fileID: 0}\n",
                guid => string.Empty, hiddenGuids, prefabs, inHidden, missing);
            CollectionAssert.AreEqual(new[] { unknownScript, "m_Script {fileID: 0}" }, missing,
                "A GUID no sample owns and a broken script slot are real failures.");
        }

        [Test]
        public void HiddenOnlyRoots_HavePackagingEvidence_ButCannotReportNativeIntegration()
        {
            Audit20261023NeoEditorTestsPaths.PackageManifest manifest = CreateManifest();
            string[] hidden =
            {
                "Package/Samples~/Demo", "Package/Samples~/NeoxiderPages", "Package/Samples~/CharacterMovementFundamentals"
            };
            Assert.That(Audit20261023NeoEditorTestsPaths.TrySelectNativeSampleRoots(manifest, hidden,
                out string[] selected, out string[] missing), Is.False);
            Assert.That(selected, Is.Empty);
            Assert.That(missing, Has.Length.EqualTo(3));
            List<string> failures = new List<string>();
            SampleSceneCoverageTests.ValidateSceneYamlPackaging("hidden.unity", ValidYaml, RequiredType,
                ScriptGuid, guid => guid == ScriptGuid, failures);
            Assert.That(failures, Is.Empty, "Real nonempty YAML/reference checks are independent of native readiness.");
            Assert.Throws<IgnoreException>(() => SampleSceneCoverageTests.RequireNativeContext(
                "Package/Samples~/Demo", false, typeof(Transform)));
        }

        [TestCase("empty")]
        [TestCase("missing-marker")]
        [TestCase("missing-script")]
        [TestCase("unknown-reference")]
        public void PackagingEvidence_RejectsEmptyMissingOrUnresolvedContent(string invalidCase)
        {
            string text = ValidYaml;
            bool referenceExists = true;
            switch (invalidCase)
            {
                case "empty": text = string.Empty; break;
                case "missing-marker": text = text.Replace(RequiredType, "Other.Component"); break;
                case "missing-script": text = "%YAML 1.1\nm_Script: {fileID: 0}\n" + RequiredType; break;
                case "unknown-reference": referenceExists = false; break;
                default: throw new ArgumentOutOfRangeException(nameof(invalidCase));
            }
            List<string> failures = new List<string>();
            SampleSceneCoverageTests.ValidateSceneYamlPackaging("invalid.unity", text, RequiredType,
                string.Empty, guid => referenceExists, failures);
            Assert.That(failures, Is.Not.Empty);
        }

        [Test]
        public void NativeReadiness_SeparatesMissingPrerequisitesFromInvalidActiveTypesAndAssets()
        {
            const string activeRoot = "Assets/CurrentSample";
            Assert.Throws<IgnoreException>(() => SampleSceneCoverageTests.RequireNativeContext(activeRoot, false, typeof(Transform)));
            Assert.Throws<IgnoreException>(() => SampleSceneCoverageTests.RequireNativeContext(activeRoot, true, new Type[] { null }));
            Assert.DoesNotThrow(() => SampleSceneCoverageTests.RequireNativeContext(activeRoot, true, typeof(Transform)));
            Assert.Throws<AssertionException>(() => SampleSceneCoverageTests.RequireNativeContext(activeRoot, true, typeof(string)));
            Assert.Throws<AssertionException>(() => SampleSceneCoverageTests.RequireNativeSceneAsset(
                "Assets/NeoMissingSample_" + Guid.NewGuid().ToString("N") + ".unity"));
        }

        [Test]
        public void SkippedSampleVisibleElsewhere_IsReportedWithItsPath_NotAsHidden()
        {
            Audit20261023NeoEditorTestsPaths.PackageManifest manifest = CreateManifest();
            string demoDevelopment = Audit20261023NeoEditorTestsPaths.DevelopmentRoot(manifest.samples[0]);
            string pagesImport = Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, manifest.samples[1]);
            const string oldCopy = "Assets/Samples/CharacterMovementFundamentals";

            string[] selected = SampleAssetValidationTests.SelectVisibleNativeSampleRoots(manifest,
                new[] { demoDevelopment, pagesImport, oldCopy }, out string[] skipped,
                sample => sample.displayName == "Character Movement Fundamentals" ? oldCopy : null);

            CollectionAssert.AreEqual(new[] { demoDevelopment, pagesImport }, selected,
                "A copy outside the canonical roots is not the current-version sample.");
            Assert.That(skipped, Has.Length.EqualTo(1));
            Assert.That(skipped[0], Does.StartWith("Character Movement Fundamentals (11.0.0; ")
                .And.Contain("visible at non-canonical path " + oldCopy).And.Contain("not validated"));
        }

        [Test]
        public void HiddenScriptSlots_CountEachNestedInstance_AndSkipStrippedStubsOfVisiblePrefabs()
        {
            const string hiddenScript = "22222222222222222222222222222222";
            const string visibleScript = "11111111111111111111111111111111";
            const string visiblePrefab = "44444444444444444444444444444444";
            const string hiddenPrefab = "55555555555555555555555555555555";
            const string visiblePrefabPath = "Assets/Sample/Visible.prefab";
            string prefabYaml = "%YAML 1.1\n" +
                MonoBehaviourDocument("10", false, hiddenScript, null) +
                MonoBehaviourDocument("11", false, hiddenScript, null) +
                MonoBehaviourDocument("12", false, visibleScript, null);
            string sceneYaml = "%YAML 1.1\n" +
                MonoBehaviourDocument("1", false, hiddenScript, null) +
                MonoBehaviourDocument("2", false, visibleScript, null) +
                PrefabInstanceDocument("100", visiblePrefab) +
                MonoBehaviourDocument("101", true, hiddenScript, "100") +
                PrefabInstanceDocument("200", visiblePrefab) +
                PrefabInstanceDocument("300", hiddenPrefab) +
                MonoBehaviourDocument("301", true, hiddenScript, "300") +
                MonoBehaviourDocument("302", true, visibleScript, "300");
            Dictionary<string, string> assets = new Dictionary<string, string>
            {
                { visibleScript, "Assets/Sample/Visible.cs" },
                { visiblePrefab, visiblePrefabPath }
            };
            HashSet<string> hiddenGuids = new HashSet<string> { hiddenScript, hiddenPrefab };

            int slots = SampleAssetValidationTests.CountHiddenSampleScriptSlots(sceneYaml,
                guid => assets.TryGetValue(guid, out string path) ? path : string.Empty,
                path => path == visiblePrefabPath ? prefabYaml : string.Empty, hiddenGuids);

            Assert.That(slots, Is.EqualTo(1 + 2 + 2 + 1),
                "Own hidden component, two per instance of the visible prefab (its stripped stub not again), and the " +
                "hidden-script stub of the hidden-source instance.");
        }

        [Test]
        public void GenericNativeSceneDiscovery_UsesSelectedCurrentIdentityRootsAndActualSceneAssets()
        {
            string[] guids = SampleAssetValidationTests.FindAssetsInAssetDatabaseSampleRoots("t:Scene", false,
                out string[] skipped);
            Audit20261023NeoEditorTestsPaths.PackageManifest manifest = Audit20261023NeoEditorTestsPaths.Manifest;
            List<string> expectedRoots = new List<string>();
            List<string> expectedSkipped = new List<string>();
            foreach (Audit20261023NeoEditorTestsPaths.SampleManifest sample in manifest.samples)
            {
                string imported = Audit20261023NeoEditorTestsPaths.ImportedRoot(manifest, sample);
                string development = Audit20261023NeoEditorTestsPaths.DevelopmentRoot(sample);
                if (AssetDatabase.IsValidFolder(imported)) expectedRoots.Add(imported);
                else if (AssetDatabase.IsValidFolder(development)) expectedRoots.Add(development);
                else expectedSkipped.Add(sample.displayName);
            }

            Assert.That(skipped, Has.Length.EqualTo(expectedSkipped.Count),
                "Exactly the samples without a visible current-version root are skipped: " + string.Join(", ", skipped));
            for (int i = 0; i < expectedSkipped.Count; i++)
                Assert.That(skipped[i], Does.StartWith(expectedSkipped[i] + " ("));
            foreach (string sample in skipped) TestContext.WriteLine("Skipped sample: " + sample);
            Assert.That(guids, Is.Not.Empty);
            HashSet<string> inspected = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> rootsWithScenes = new HashSet<string>(StringComparer.Ordinal);
            foreach (string guid in guids)
            {
                Assert.That(inspected.Add(guid), Is.True, "Native scene GUIDs must not repeat: " + guid);
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string owner = expectedRoots.Find(root => path.StartsWith(root + "/", StringComparison.Ordinal));
                Assert.That(owner, Is.Not.Null, "Native scene came from an unselected layout/version: " + path);
                rootsWithScenes.Add(owner);
                Assert.That(path, Does.Not.Contain("/Samples~/"));
                SampleSceneCoverageTests.RequireNativeSceneAsset(path);
            }

            foreach (string root in expectedRoots)
            {
                if (HasSceneOnDisk(root))
                    Assert.That(rootsWithScenes, Does.Contain(root),
                        "Every visible sample with scenes on disk is validated, not only the first one: " + root);
            }

            TestContext.WriteLine("Generic native scene discovery inspected " + inspected.Count + " actual scenes in " +
                rootsWithScenes.Count + " visible sample root(s).");
        }

        private static bool HasSceneOnDisk(string root)
        {
            string diskRoot = Audit20261023NeoEditorTestsPaths.ToDiskPath(root);
            if (!Directory.Exists(diskRoot)) return false;
            using (IEnumerator<string> scenes = Directory.EnumerateFiles(diskRoot, "*.unity", SearchOption.AllDirectories)
                       .GetEnumerator())
            {
                return scenes.MoveNext();
            }
        }

        private static string MonoBehaviourDocument(string id, bool stripped, string scriptGuid, string prefabInstance)
        {
            return "--- !u!114 &" + id + (stripped ? " stripped" : string.Empty) + "\nMonoBehaviour:\n" +
                "  m_PrefabInstance: {fileID: " + (prefabInstance ?? "0") + "}\n" +
                "  m_Script: {fileID: 11500000, guid: " + scriptGuid + ", type: 3}\n";
        }

        private static string PrefabInstanceDocument(string id, string sourceGuid)
        {
            return "--- !u!1001 &" + id + "\nPrefabInstance:\n" +
                "  m_SourcePrefab: {fileID: 100100000, guid: " + sourceGuid + ", type: 3}\n";
        }

        private static Audit20261023NeoEditorTestsPaths.PackageManifest CreateManifest()
        {
            return new Audit20261023NeoEditorTestsPaths.PackageManifest
            {
                name = Audit20261023NeoEditorTestsPaths.PackageName, displayName = "NeoxiderTools", version = "11.0.0",
                samples = new[]
                {
                    new Audit20261023NeoEditorTestsPaths.SampleManifest { displayName = "Demo Scenes", path = "Samples~/Demo" },
                    new Audit20261023NeoEditorTestsPaths.SampleManifest { displayName = "NeoxiderPages", path = "Samples~/NeoxiderPages" },
                    new Audit20261023NeoEditorTestsPaths.SampleManifest { displayName = "Character Movement Fundamentals", path = "Samples~/CharacterMovementFundamentals" }
                }
            };
        }
    }
}
