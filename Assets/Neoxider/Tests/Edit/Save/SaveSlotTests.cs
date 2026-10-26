using System;
using System.IO;
using Neo.Save;
using NUnit.Framework;

namespace Neo.Editor.Tests.Edit
{
    public class SaveSlotTests
    {
        private string _testRoot;

        [SetUp]
        public void Setup()
        {
            _testRoot = Path.Combine(Path.GetTempPath(),
                "Audit20261023NeoEditorTests-Slots-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);
        }

        [TearDown]
        public void Teardown()
        {
            if (_testRoot != null && Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }

        [Test]
        public void FileSaveProvider_ChangeSlot_SwapsFilesAndLoadsCorrectData()
        {
            FileSaveProviderOptions options = new FileSaveProviderOptions { PersistenceRoot = _testRoot };
            FileSaveProvider provider = new FileSaveProvider("testsavelot1.json", options);
            provider.SetInt("SlotData", 1);
            provider.Save();

            // WHY: Setup second slot via new provider to simulate existing file
            FileSaveProvider provider2 = new FileSaveProvider("testsavelot2.json", options);
            provider2.SetInt("SlotData", 2);
            provider2.Save();

            provider2.ChangeSlot("testsavelot1.json");

            Assert.AreEqual(1, provider2.GetInt("SlotData", 0),
                "After swapping to slot 1, we should read slot 1 data.");

            provider2.ChangeSlot("testsavelot2.json");
            Assert.AreEqual(2, provider2.GetInt("SlotData", 0),
                "After swapping to slot 2, we should read slot 2 data.");
        }
    }
}
