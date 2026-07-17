using System.IO;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    public class SaveSlotTests
    {
        private string _testFile1;
        private string _testFile2;

        [SetUp]
        public void Setup()
        {
            _testFile1 = Path.Combine(Application.persistentDataPath, "testsavelot1.json");
            _testFile2 = Path.Combine(Application.persistentDataPath, "testsavelot2.json");

            if (File.Exists(_testFile1))
            {
                File.Delete(_testFile1);
            }

            if (File.Exists(_testFile2))
            {
                File.Delete(_testFile2);
            }

            SaveProvider.DeleteAll();

            // WHY: the global SaveProvider's Initialize uses SaveProviderSettings.
            // We can test multi-slots directly via FileSaveProvider instance.
        }

        [TearDown]
        public void Teardown()
        {
            if (File.Exists(_testFile1))
            {
                File.Delete(_testFile1);
            }

            if (File.Exists(_testFile2))
            {
                File.Delete(_testFile2);
            }
        }

        [Test]
        public void FileSaveProvider_ChangeSlot_SwapsFilesAndLoadsCorrectData()
        {
            var provider = new FileSaveProvider("testsavelot1.json");
            provider.SetInt("SlotData", 1);
            provider.Save();

            // WHY: Setup second slot via new provider to simulate existing file
            var provider2 = new FileSaveProvider("testsavelot2.json");
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
