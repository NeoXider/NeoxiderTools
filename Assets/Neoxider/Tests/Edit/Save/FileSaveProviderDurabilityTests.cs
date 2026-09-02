using System.IO;
using System.Text.RegularExpressions;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the durability contract of <see cref="FileSaveProvider" />: a save is committed atomically and a
    ///     damaged main file falls back to the rotating backup instead of silently resetting the player's data.
    /// </summary>
    [TestFixture]
    public class FileSaveProviderDurabilityTests
    {
        private const string FileName = "durability-test.json";

        private string _root;

        private string MainPath => Path.Combine(_root, FileName);
        private string BackupPath => MainPath + ".bak";
        private string TempPath => MainPath + ".tmp";

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "neo-save-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        private FileSaveProvider CreateProvider()
        {
            return new FileSaveProvider(FileName, new FileSaveProviderOptions { PersistenceRoot = _root });
        }

        /// <summary>
        ///     The provider reports a recovery unconditionally, so every test that damages the main file has to
        ///     declare the resulting error.
        /// </summary>
        private static void ExpectRecoveryFromBackup()
        {
            LogAssert.Expect(LogType.Error, new Regex("was unreadable; restored from"));
        }

        [Test]
        public void Save_LeavesNoTemporaryFileBehind()
        {
            FileSaveProvider provider = CreateProvider();
            provider.SetInt("coins", 10);
            provider.Save();

            Assert.IsTrue(File.Exists(MainPath), "the save file must exist after a flush");
            Assert.IsFalse(File.Exists(TempPath), "the staging file must not survive a successful commit");
        }

        [Test]
        public void SecondSave_RotatesThePreviousFileIntoTheBackup()
        {
            FileSaveProvider provider = CreateProvider();
            provider.SetInt("coins", 10);
            provider.Save();
            provider.SetInt("coins", 20);
            provider.Save();

            Assert.IsTrue(File.Exists(BackupPath), "committing over an existing save must keep the previous one");
            StringAssert.Contains("10", File.ReadAllText(BackupPath), "backup should hold the previous payload");
            StringAssert.Contains("20", File.ReadAllText(MainPath), "main file should hold the newest payload");
        }

        [Test]
        public void TruncatedMainFile_IsRecoveredFromBackup()
        {
            FileSaveProvider provider = CreateProvider();
            provider.SetInt("coins", 10);
            provider.Save();
            provider.SetInt("coins", 20);
            provider.Save();

            // WHY: exactly what a process kill mid-write leaves behind.
            File.WriteAllText(MainPath, "{\"items\":[{\"key\":\"co");

            ExpectRecoveryFromBackup();

            FileSaveProvider reloaded = CreateProvider();

            Assert.AreEqual(10, reloaded.GetInt("coins", -1),
                "a damaged save must fall back to the backup instead of resetting");
        }

        [Test]
        public void EmptyMainFile_IsRecoveredFromBackup()
        {
            FileSaveProvider provider = CreateProvider();
            provider.SetInt("coins", 42);
            provider.Save();
            provider.SetInt("coins", 43);
            provider.Save();

            File.WriteAllText(MainPath, string.Empty);

            ExpectRecoveryFromBackup();

            FileSaveProvider reloaded = CreateProvider();

            Assert.AreEqual(42, reloaded.GetInt("coins", -1),
                "a zero-byte save is a truncated write, not an empty profile");
        }

        [Test]
        public void MissingMainFile_IsRecoveredFromBackup()
        {
            FileSaveProvider provider = CreateProvider();
            provider.SetString("name", "first");
            provider.Save();
            provider.SetString("name", "second");
            provider.Save();

            File.Delete(MainPath);

            ExpectRecoveryFromBackup();

            FileSaveProvider reloaded = CreateProvider();

            Assert.AreEqual("first", reloaded.GetString("name", string.Empty));
        }

        [Test]
        public void NoFilesAtAll_StartsFromEmptyDataWithoutThrowing()
        {
            FileSaveProvider provider = CreateProvider();

            Assert.AreEqual(0, provider.GetInt("coins"));
            Assert.AreEqual(string.Empty, provider.GetString("name", string.Empty));
        }

        [Test]
        public void RoundTrip_PreservesValuesAcrossReload()
        {
            FileSaveProvider provider = CreateProvider();
            provider.SetInt("coins", 5);
            provider.SetString("name", "hero");
            provider.SetBool("tutorial", true);
            provider.SetFloat("volume", 0.5f);
            provider.Save();

            FileSaveProvider reloaded = CreateProvider();

            Assert.AreEqual(5, reloaded.GetInt("coins"));
            Assert.AreEqual("hero", reloaded.GetString("name", string.Empty));
            Assert.IsTrue(reloaded.GetBool("tutorial"));
            Assert.AreEqual(0.5f, reloaded.GetFloat("volume"), 0.0001f);
        }
    }
}
