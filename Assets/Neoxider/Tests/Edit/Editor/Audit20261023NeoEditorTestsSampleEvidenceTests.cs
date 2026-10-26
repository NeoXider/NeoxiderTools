using System;
using System.Collections.Generic;
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
        public void GenericNativeSceneDiscovery_UsesSelectedCurrentIdentityRootsAndActualSceneAssets()
        {
            string[] guids = SampleAssetValidationTests.FindAssetsInAssetDatabaseSampleRoots("t:Scene");
            Audit20261023NeoEditorTestsPaths.PackageManifest manifest = Audit20261023NeoEditorTestsPaths.Manifest;
            List<string> visibleRoots = new List<string>();
            foreach (string root in Audit20261023NeoEditorTestsPaths.SampleRoots(false))
                if (AssetDatabase.IsValidFolder(root)) visibleRoots.Add(root);
            Assert.That(Audit20261023NeoEditorTestsPaths.TrySelectNativeSampleRoots(manifest, visibleRoots,
                out string[] selected, out string[] missing), Is.True, string.Join(", ", missing));
            Assert.That(selected, Has.Length.EqualTo(manifest.samples.Length));
            Assert.That(guids, Is.Not.Empty);
            HashSet<string> inspected = new HashSet<string>(StringComparer.Ordinal);
            foreach (string guid in guids)
            {
                Assert.That(inspected.Add(guid), Is.True, "Native scene GUIDs must not repeat: " + guid);
                string path = AssetDatabase.GUIDToAssetPath(guid);
                bool belongsToSelectedRoot = false;
                foreach (string root in selected)
                    belongsToSelectedRoot |= path.StartsWith(root + "/", StringComparison.Ordinal);
                Assert.That(belongsToSelectedRoot, Is.True, "Native scene came from an unselected layout/version: " + path);
                Assert.That(path, Does.Not.Contain("/Samples~/"));
                SampleSceneCoverageTests.RequireNativeSceneAsset(path);
            }
            TestContext.WriteLine("Generic native scene discovery inspected " + inspected.Count + " actual scenes.");
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
