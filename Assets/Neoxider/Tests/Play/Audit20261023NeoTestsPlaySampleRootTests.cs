#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoTestsPlaySampleRootTests
    {
        private string _temporaryRoot;

        [SetUp]
        public void SetUp()
        {
            _temporaryRoot = Path.Combine(Path.GetTempPath(), nameof(Audit20261023NeoTestsPlaySampleRootTests),
                Guid.NewGuid().ToString("N")).Replace('\\', '/');
            Directory.CreateDirectory(_temporaryRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_temporaryRoot))
            {
                Directory.Delete(_temporaryRoot, true);
            }
        }

        [Test]
        public void ImportedDemo_TakesPrecedenceOverHiddenReleaseSamples()
        {
            string hiddenRoot = _temporaryRoot + "/Neoxider/Samples~";
            string importedRoot = _temporaryRoot + "/Samples/NeoxiderTools";
            Directory.CreateDirectory(hiddenRoot + "/Demo");
            string expected = importedRoot + "/10.19.0/Demo Scenes";
            Directory.CreateDirectory(expected);

            string actual = SampleRuntimeSceneSmokeTests.FindSampleDemoRoot(new[] { hiddenRoot, importedRoot });

            Assert.That(actual, Is.EqualTo(expected), "Imported scripts can run; hidden package scripts cannot.");
        }

        [Test]
        public void DevelopmentDemo_RemainsThePreferredVisibleRoot()
        {
            string developmentRoot = _temporaryRoot + "/Neoxider/Samples";
            string importedRoot = _temporaryRoot + "/Samples/NeoxiderTools";
            Directory.CreateDirectory(developmentRoot + "/Demo");
            Directory.CreateDirectory(importedRoot + "/10.19.0/Demo Scenes");

            Assert.That(SampleRuntimeSceneSmokeTests.FindSampleDemoRoot(new[] { developmentRoot, importedRoot }),
                Is.EqualTo(developmentRoot + "/Demo"));
        }

        [Test]
        public void HiddenDemo_IsReturnedOnlyAsAFallbackForTheExplicitSkip()
        {
            string hiddenRoot = _temporaryRoot + "/Neoxider/Samples~";
            Directory.CreateDirectory(hiddenRoot + "/Demo");

            Assert.That(SampleRuntimeSceneSmokeTests.FindSampleDemoRoot(new[] { hiddenRoot }),
                Is.EqualTo(hiddenRoot + "/Demo"));
        }

        [Test]
        public void LegacyImportedPackageNameAndDemoFolder_AreSupported()
        {
            string importedRoot = _temporaryRoot + "/Samples/Neoxider Tools";
            string expected = importedRoot + "/10.19.0/Demo";
            Directory.CreateDirectory(expected);

            Assert.That(SampleRuntimeSceneSmokeTests.FindSampleDemoRoot(new[] { importedRoot }),
                Is.EqualTo(expected));
        }

        [Test]
        public void MissingRoots_ReturnNoCandidate()
        {
            Assert.That(SampleRuntimeSceneSmokeTests.FindSampleDemoRoot(new[] { _temporaryRoot + "/missing" }),
                Is.Null);
        }
    }
}
#endif
