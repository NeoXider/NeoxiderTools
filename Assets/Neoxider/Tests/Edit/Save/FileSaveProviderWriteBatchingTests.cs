using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Save.Tests
{
    /// <summary>
    ///     A flush with no pending change must not rewrite the save file or rotate its backup, while recovery,
    ///     encryption migration and an externally removed file still force a commit.
    /// </summary>
    public sealed class FileSaveProviderWriteBatchingTests
    {
        private const string FileName = "batching.json";

        private string _root;

        private string MainPath => Path.Combine(_root, FileName);
        private string BackupPath => MainPath + ".bak";

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "neo-save-batching-tests", Path.GetRandomFileName());
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

        [Test]
        public void Save_WithoutChanges_SkipsTheWriteAndBackupRotation_ButStillReportsSaved()
        {
            FileSaveProvider provider = CreateProvider();
            provider.SetInt("coins", 10);
            provider.Save();
            provider.SetInt("coins", 20);
            provider.Save();
            Assert.That(File.Exists(BackupPath), Is.True);
            File.Delete(BackupPath);
            byte[] primary = File.ReadAllBytes(MainPath);
            int savedCount = 0;
            provider.OnDataSaved += () => savedCount++;

            provider.Save();
            provider.Save();

            Assert.That(savedCount, Is.EqualTo(2), "callers still observe a completed save");
            Assert.That(File.Exists(BackupPath), Is.False, "an unchanged flush must not rotate the backup");
            Assert.That(File.ReadAllBytes(MainPath), Is.EqualTo(primary));
            Assert.That(provider.HasUnsavedChanges, Is.False);
        }

        [Test]
        public void WritingTheStoredValue_KeepsTheProviderClean_AndStillRaisesKeyChanged()
        {
            FileSaveProvider provider = CreateProvider();
            provider.SetInt("coins", 10);
            provider.SetString("name", "hero");
            provider.SetFloat("volume", 0.5f);
            provider.SetBool("tutorial", true);
            provider.SetLong("cents", 5_000_000_000L);
            provider.Save();
            int changedCount = 0;
            provider.OnKeyChanged += _ => changedCount++;

            provider.SetInt("coins", 10);
            provider.SetString("name", "hero");
            provider.SetFloat("volume", 0.5f);
            provider.SetBool("tutorial", true);
            provider.SetLong("cents", 5_000_000_000L);

            Assert.That(changedCount, Is.EqualTo(5));
            Assert.That(provider.HasUnsavedChanges, Is.False);

            provider.SetString("coins", "10");
            Assert.That(provider.HasUnsavedChanges, Is.True, "a type change is a change even with equal text");
        }

        [Test]
        public void Save_WithoutChanges_RecreatesAnExternallyDeletedFile()
        {
            FileSaveProvider provider = CreateProvider();
            provider.SetInt("coins", 10);
            provider.Save();
            File.Delete(MainPath);

            provider.Save();

            Assert.That(File.Exists(MainPath), Is.True);
            Assert.That(CreateProvider().GetInt("coins", -1), Is.EqualTo(10));
        }

        [Test]
        public void Save_AfterBackupRecovery_RewritesTheDamagedPrimaryWithoutChanges()
        {
            FileSaveProvider provider = CreateProvider();
            provider.SetInt("coins", 10);
            provider.Save();
            provider.SetInt("coins", 20);
            provider.Save();
            byte[] backup = File.ReadAllBytes(BackupPath);
            File.WriteAllText(MainPath, "{\"items\":[{\"key\":\"co");
            LogAssert.Expect(LogType.Error, new Regex("was unreadable; restored from"));
            FileSaveProvider recovered = CreateProvider();

            recovered.Save();

            Assert.That(CreateProvider().GetInt("coins", -1), Is.EqualTo(10), "the primary must be repaired");
            Assert.That(File.ReadAllBytes(BackupPath), Is.EqualTo(backup), "the known-good backup must be kept");
        }

        [Test]
        public void Save_AfterLoadingLegacyPlaintext_EncryptsWithoutAnyValueChange()
        {
            FileSaveProvider plain = CreateProvider();
            plain.SetInt("coins", 7);
            plain.Save();
            Assert.That(File.ReadAllText(MainPath).TrimStart(), Does.StartWith("{"));
            Assert.That(FileSaveEncryptionConfig.TryCreate(true, "", "", out FileSaveEncryptionConfig encryption,
                out string error), Is.True, error);
            FileSaveProviderOptions options = new FileSaveProviderOptions
            {
                PersistenceRoot = _root,
                Encryption = encryption
            };
            FileSaveProvider encrypted = new FileSaveProvider(FileName, options);

            encrypted.Save();

            Assert.That(File.ReadAllText(MainPath).TrimStart(), Does.Not.StartWith("{"));
            Assert.That(new FileSaveProvider(FileName, options).GetInt("coins", -1), Is.EqualTo(7));
        }

        [Test]
        public void TryChangeSlot_WithEmptyName_KeepsTheCurrentSlot()
        {
            FileSaveProvider provider = CreateProvider();
            provider.SetInt("coins", 3);
            string path = provider.FilePath;

            Assert.That(provider.TryChangeSlot(null), Is.False);
            Assert.That(provider.TryChangeSlot("  "), Is.False);

            Assert.That(provider.FilePath, Is.EqualTo(path));
            Assert.That(provider.GetInt("coins"), Is.EqualTo(3));
            Assert.That(provider.HasUnsavedChanges, Is.True, "a rejected switch must not flush or reload");
        }

        [Test]
        public void Save_ReusesSerializationBuffers_AcrossFlushes()
        {
            FileSaveProvider provider = CreateProvider();
            for (int i = 0; i < 64; i++)
            {
                provider.SetInt("key" + i, i);
            }

            provider.Save();
            provider.DeleteKey("key0");
            provider.SetInt("key1", 100);
            provider.Save();

            FileSaveProvider reloaded = CreateProvider();
            Assert.That(reloaded.HasKey("key0"), Is.False);
            Assert.That(reloaded.GetInt("key1"), Is.EqualTo(100));
            Assert.That(reloaded.GetInt("key63"), Is.EqualTo(63));
        }

        private FileSaveProvider CreateProvider()
        {
            return new FileSaveProvider(FileName, new FileSaveProviderOptions { PersistenceRoot = _root });
        }
    }
}
