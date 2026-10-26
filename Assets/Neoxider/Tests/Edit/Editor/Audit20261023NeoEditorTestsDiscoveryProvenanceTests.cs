using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using PackageRecord = Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.PackageIdentityRecord;
using Provenance = Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.DiscoveryPackageProvenance;

namespace Neo.Editor.Tests
{
    /// <summary>Admission controls using owned manifest files and projected records, not native PackageInfo/discovery evidence.</summary>
    public sealed class Audit20261023NeoEditorTestsDiscoveryProvenanceTests
    {
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
                    Path.Combine(project, "."));

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

                Provenance selected = Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(observed, project);

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
                    new[] { registered }, project);
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
                    Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(new[] { registered }, project));
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
                    Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(Array.Empty<PackageRecord>(), project));
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
                    Audit20261023NeoEditorTestsValidationHost.SelectDiscoveryPackageProvenance(Array.Empty<PackageRecord>(), project));
            });
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
