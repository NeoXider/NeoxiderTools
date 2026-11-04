using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using PackageRecord = Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.PackageIdentityRecord;
using Provenance = Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.DiscoveryPackageProvenance;
using Manifest = Neo.Editor.Tests.Audit20261023NeoEditorTestsPaths.PackageManifest;

namespace Neo.Editor.Tests
{
    /// <summary>Admission controls using owned manifest files and projected records, not native PackageInfo/discovery evidence.</summary>
    public sealed class Audit20261023NeoEditorTestsDiscoveryProvenanceTests
    {
        private const string LegacyExpectedVersion = "11.0.0";
        private const string ValidManifest = "{\"name\":\"com.neoxider.tools\",\"version\":\"11.0.0\",\"samples\":[" +
            "{\"displayName\":\"Demo Scenes\",\"path\":\"Samples~/Demo\"}," +
            "{\"displayName\":\"NeoxiderPages\",\"path\":\"Samples~/NeoxiderPages\"}," +
            "{\"displayName\":\"Character Movement Fundamentals\",\"path\":\"Samples~/CharacterMovementFundamentals\"}]}";

        [Test]
        public void ProjectAssets_WithUnrelatedRegisteredPackages_PreservesTheObservedList()
        {
            WithOwnedProject((project, manifestPath) =>
            {
                WriteManifest(manifestPath, ValidManifest);
                PackageRecord first = new PackageRecord { name = "com.example.z", version = "2.1.0", source = "Registry", packageId = "com.example.z@2.1.0" };
                PackageRecord second = new PackageRecord { name = "com.example.a", version = "1.0.0", source = "Embedded", packageId = "com.example.a@1.0.0" };
                List<PackageRecord> registered = new List<PackageRecord> { first, second };
                PackageRecord[] observed = registered.ToArray();
                string firstBefore = JsonUtility.ToJson(first);
                string secondBefore = JsonUtility.ToJson(second);

                Provenance selected = Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(registered,
                    Path.Combine(project, "."), LegacyExpectedVersion);

                string root = Path.GetFullPath(Path.Combine(project, "Assets", "Neoxider"));
                Assert.That(selected.name, Is.EqualTo("com.neoxider.tools"));
                Assert.That(selected.version, Is.EqualTo("11.0.0"));
                Assert.That(selected.kind, Is.EqualTo("ProjectAssets"));
                Assert.That(selected.source, Is.EqualTo("ProjectAssets"));
                Assert.That(selected.resolvedPath, Is.EqualTo(root));
                Assert.That(selected.manifestOrigin, Is.EqualTo(Path.Combine(root, "package.json")));
                CollectionAssert.AreEqual(observed, registered, "Provenance cannot append, filter or sort the registered list.");
                Assert.That(JsonUtility.ToJson(first), Is.EqualTo(firstBefore));
                Assert.That(JsonUtility.ToJson(second), Is.EqualTo(secondBefore));
                foreach (PackageRecord package in registered)
                    Assert.That(package.name, Is.Not.EqualTo("com.neoxider.tools"), "Assets provenance is not a synthetic PackageInfo.");
            });
        }

        [TestCase("Local")]
        [TestCase("Embedded")]
        [TestCase("Registry")]
        public void RegisteredVersion11_WinsWithItsActualSourceAndRoot(string source)
        {
            WithOwnedProject((project, manifestPath) =>
            {
                WriteManifest(manifestPath, ValidManifest);
                string registeredRoot = Path.Combine(project, "Packages", ".", "ObservedNeo");
                PackageRecord registered = new PackageRecord
                {
                    name = "com.neoxider.tools", version = "11.0.0", source = source, resolvedPath = registeredRoot
                };
                PackageRecord[] observed = { registered };

                Provenance selected = Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(observed,
                    project, LegacyExpectedVersion);

                Assert.That(selected.kind, Is.EqualTo("RegisteredPackageInfo"));
                Assert.That(selected.source, Is.EqualTo(source));
                Assert.That(selected.name, Is.EqualTo(registered.name));
                Assert.That(selected.version, Is.EqualTo(registered.version));
                Assert.That(selected.resolvedPath, Is.EqualTo(Path.GetFullPath(registeredRoot)));
                Assert.That(selected.manifestOrigin, Is.EqualTo("PackageInfo.GetAllRegisteredPackages"));
                Assert.That(observed, Has.Length.EqualTo(1));
                Assert.That(observed[0], Is.SameAs(registered));
                Assert.That(registered.resolvedPath, Is.EqualTo(registeredRoot), "Canonical summary paths cannot rewrite observed metadata.");
            });
        }

        [Test]
        public void RegisteredVersion11_DoesNotReadMalformedAssetsFallback()
        {
            WithOwnedProject((project, manifestPath) =>
            {
                WriteManifest(manifestPath, "{");
                PackageRecord registered = new PackageRecord
                {
                    name = "com.neoxider.tools", version = "11.0.0", source = "Local",
                    resolvedPath = Path.Combine(project, "Packages", "ObservedNeo")
                };
                Provenance selected = Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(
                    new[] { registered }, project, LegacyExpectedVersion);
                Assert.That(selected.kind, Is.EqualTo("RegisteredPackageInfo"));
                Assert.That(selected.resolvedPath, Is.EqualTo(Path.GetFullPath(registered.resolvedPath)));
            });
        }

        [Test]
        public void RegisteredWrongVersion_CannotFallThroughToValidAssets()
        {
            WithOwnedProject((project, manifestPath) =>
            {
                WriteManifest(manifestPath, ValidManifest);
                PackageRecord registered = new PackageRecord
                {
                    name = "com.neoxider.tools", version = "10.9.0", source = "Embedded",
                    resolvedPath = Path.Combine(project, "Packages", "ObservedNeo")
                };
                InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
                    Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(new[] { registered },
                        project, LegacyExpectedVersion));
                Assert.That(failure.Message, Does.Contain("registered").And.Contain("10.9.0"));
                Assert.That(registered.version, Is.EqualTo("10.9.0"));
            });
        }

        [Test]
        public void UnregisteredNeo_MissingAssetsManifest_IsRejected()
        {
            WithOwnedProject((project, manifestPath) =>
            {
                Assert.That(File.Exists(manifestPath), Is.False);
                Assert.Throws<FileNotFoundException>(() =>
                    Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(Array.Empty<PackageRecord>(),
                        project, LegacyExpectedVersion));
            });
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("[]")]
        [TestCase("null")]
        [TestCase("{\"name\":\"com.neoxider.tools\",\"version\":\"11.0.0\"")]
        [TestCase("{\"name\":\"com.neoxider.tools\",\"version\":\"11.0.0\",\"broken\":}")]
        [TestCase("{\"version\":\"11.0.0\"}")]
        [TestCase("{\"name\":\"com.example.other\",\"version\":\"11.0.0\"}")]
        [TestCase("{\"name\":\"com.neoxider.tools\"}")]
        [TestCase("{\"name\":\"com.neoxider.tools\",\"version\":\"\"}")]
        [TestCase("{\"name\":\"com.neoxider.tools\",\"version\":\"10.9.0\"}")]
        public void UnregisteredNeo_InvalidAssetsManifest_IsRejected(string json)
        {
            WithOwnedProject((project, manifestPath) =>
            {
                WriteManifest(manifestPath, json);
                Assert.Throws<InvalidDataException>(() =>
                    Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(Array.Empty<PackageRecord>(),
                        project, LegacyExpectedVersion));
            });
        }

        [TestCase("11.0.1")]
        [TestCase("12.0.0-preview.1")]
        public void ExplicitExpectedVersion_ProjectAssets_AdmitsTheFrozenVersion(string expectedVersion)
        {
            WithOwnedProject((project, manifestPath) =>
            {
                string json = ManifestWithVersion(expectedVersion);
                WriteManifest(manifestPath, json);
                Provenance selected = Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(
                    Array.Empty<PackageRecord>(), project, expectedVersion);
                Assert.That(selected.kind, Is.EqualTo("ProjectAssets"));
                Assert.That(selected.source, Is.EqualTo("ProjectAssets"));
                Assert.That(selected.name, Is.EqualTo("com.neoxider.tools"));
                Assert.That(selected.version, Is.EqualTo(expectedVersion));
                Assert.That(selected.resolvedPath, Is.EqualTo(Path.GetDirectoryName(manifestPath)));
                Assert.That(selected.manifestOrigin, Is.EqualTo(manifestPath));
                Assert.That(File.ReadAllText(manifestPath), Is.EqualTo(json));
            });
        }

        [TestCase("11.0.1", "Local")]
        [TestCase("11.0.1", "Embedded")]
        [TestCase("11.0.1", "Registry")]
        [TestCase("12.0.0-preview.1", "Embedded")]
        public void ExplicitExpectedVersion_RegisteredPackage_PreservesPrecedenceAndProvenance(
            string expectedVersion, string source)
        {
            WithOwnedProject((project, manifestPath) =>
            {
                // WHY: an older Assets copy cannot select the expected version.
                WriteManifest(manifestPath, ValidManifest);
                PackageRecord registered = new PackageRecord
                {
                    name = "com.neoxider.tools", version = expectedVersion, source = source,
                    resolvedPath = Path.Combine(project, "Packages", ".", "ObservedNeo")
                };
                PackageRecord[] observed = { registered };
                string before = JsonUtility.ToJson(registered);
                Provenance selected = Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(
                    observed, project, expectedVersion);
                Assert.That(selected.kind, Is.EqualTo("RegisteredPackageInfo"));
                Assert.That(selected.source, Is.EqualTo(source));
                Assert.That(selected.version, Is.EqualTo(expectedVersion));
                Assert.That(selected.resolvedPath, Is.EqualTo(Path.GetFullPath(registered.resolvedPath)));
                Assert.That(selected.manifestOrigin, Is.EqualTo("PackageInfo.GetAllRegisteredPackages"));
                Assert.That(observed, Has.Length.EqualTo(1));
                Assert.That(observed[0], Is.SameAs(registered));
                Assert.That(JsonUtility.ToJson(registered), Is.EqualTo(before));
                Assert.That(File.ReadAllText(manifestPath), Is.EqualTo(ValidManifest));
            });
        }

        [TestCase("11.0.0")]
        [TestCase("12.0.0")]
        public void ExplicitExpectedVersion_ProjectAssets_RejectsARealVersionMismatch(string actualVersion)
        {
            WithOwnedProject((project, manifestPath) =>
            {
                string json = ManifestWithVersion(actualVersion);
                WriteManifest(manifestPath, json);
                Assert.Throws<InvalidDataException>(() =>
                    Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(
                        Array.Empty<PackageRecord>(), project, "11.0.1"));
                Assert.That(File.ReadAllText(manifestPath), Is.EqualTo(json));
            });
        }

        [TestCase("11.0.0")]
        [TestCase("12.0.0")]
        public void ExplicitExpectedVersion_WrongRegisteredVersion_CannotUseMatchingAssets(string actualVersion)
        {
            WithOwnedProject((project, manifestPath) =>
            {
                string json = ManifestWithVersion("11.0.1");
                WriteManifest(manifestPath, json);
                PackageRecord registered = new PackageRecord
                {
                    name = "com.neoxider.tools", version = actualVersion, source = "Embedded",
                    resolvedPath = Path.Combine(project, "Packages", "ObservedNeo")
                };
                InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
                    Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(
                        new[] { registered }, project, "11.0.1"));
                Assert.That(failure.Message, Does.Contain("registered").And.Contain(actualVersion).And.Contain("11.0.1"));
                Assert.That(registered.version, Is.EqualTo(actualVersion));
                Assert.That(File.ReadAllText(manifestPath), Is.EqualTo(json));
            });
        }

        [TestCase("11.0.0")]
        [TestCase("11.0.1")]
        [TestCase("12.0.0-preview.1")]
        public void SampleImportVersionAdmission_RequiresTheExplicitFrozenVersion(string expectedVersion)
        {
            Manifest manifest = JsonUtility.FromJson<Manifest>(ManifestWithVersion(expectedVersion));
            Assert.That(manifest.samples, Has.Length.EqualTo(3));
            Assert.DoesNotThrow(() => Audit20261023NeoEditorTestsValidationHost.RequireSampleImportVersions(
                manifest, expectedVersion, expectedVersion));
            Assert.That(manifest.version, Is.EqualTo(expectedVersion));
        }

        [TestCase("11.0.0", "11.0.1")]
        [TestCase("11.0.1", "11.0.0")]
        [TestCase("11.0.0", "11.0.0")]
        public void SampleImportVersionAdmission_RejectsEitherObservedVersionMismatch(
            string manifestVersion, string registeredVersion)
        {
            Manifest manifest = JsonUtility.FromJson<Manifest>(ManifestWithVersion(manifestVersion));
            Assert.Throws<InvalidDataException>(() => Audit20261023NeoEditorTestsValidationHost.RequireSampleImportVersions(
                manifest, registeredVersion, "11.0.1"));
            Assert.That(manifest.version, Is.EqualTo(manifestVersion));
            Assert.That(manifest.samples, Has.Length.EqualTo(3));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SampleImportVersionAdmission_KeepsExactNameAndThreeSampleRequirements(bool wrongName)
        {
            Manifest manifest = JsonUtility.FromJson<Manifest>(ManifestWithVersion("11.0.1"));
            if (wrongName) manifest.name = "com.example.other";
            else Array.Resize(ref manifest.samples, 2);
            Assert.Throws<InvalidDataException>(() => Audit20261023NeoEditorTestsValidationHost.RequireSampleImportVersions(
                manifest, "11.0.1", "11.0.1"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("11.0.1 ")]
        [TestCase("-anotherArgument")]
        public void ExplicitExpectedVersion_IsRequiredBeforeDiscoveryOrImport(string expectedVersion)
        {
            Assert.Throws<ArgumentException>(() =>
                Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(
                    Array.Empty<PackageRecord>(), "unused", expectedVersion));
            Manifest manifest = JsonUtility.FromJson<Manifest>(ManifestWithVersion("11.0.1"));
            Assert.Throws<ArgumentException>(() => Audit20261023NeoEditorTestsValidationHost.RequireSampleImportVersions(
                manifest, "11.0.1", expectedVersion));
        }

        private static string ManifestWithVersion(string version)
        {
            return ValidManifest.Replace("\"version\":\"11.0.0\"", "\"version\":\"" + version + "\"");
        }

        private static void WriteManifest(string path, string json)
        {
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        private static void WithOwnedProject(Action<string, string> inspect)
        {
            string temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            string project = Path.GetFullPath(Path.Combine(temporaryRoot, "NeoEditorTestsProvenance_" + Guid.NewGuid().ToString("N")));
            if (!project.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Temporary project must remain within its owned temporary root.");
            string manifest = Path.Combine(project, "Assets", "Neoxider", "package.json");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(manifest));
                inspect(project, manifest);
            }
            finally
            {
                if (Directory.Exists(project)) Directory.Delete(project, true);
            }
        }
    }
}
