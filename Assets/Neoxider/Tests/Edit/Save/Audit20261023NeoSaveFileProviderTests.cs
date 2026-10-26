using System;
using System.IO;
using System.Text.RegularExpressions;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023NeoSaveFileProviderTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "Audit20261023NeoSave_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_root, true);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void FailedSlotFlush_KeepsPendingDataAndOriginalSlot_ForRetry(int entryPoint)
        {
            FileSaveProvider source = CreateProvider("source.json");
            source.SetInt("coins", 10);
            source.Save();
            FileSaveProvider target = CreateProvider("target.json");
            target.SetInt("coins", 900);
            target.Save();
            source.SetInt("coins", 33);

            // A directory at the staging path deterministically prevents File.WriteAllText.
            string stagingPath = Path.Combine(_root, "source.json.tmp");
            Directory.CreateDirectory(stagingPath);
            int loadedCount = 0;
            source.OnDataLoaded += () => loadedCount++;
            ISaveProvider previousProvider = SaveProvider.CurrentProvider;

            try
            {
                LogAssert.Expect(LogType.Error, new Regex("Could not stage save data"));
                if (entryPoint == 0)
                {
                    source.ChangeSlot("target.json");
                }
                else if (entryPoint == 1)
                {
                    Assert.That(source.TryChangeSlot("target.json"), Is.False);
                }
                else
                {
                    SaveProvider.SetProvider(source);
                    SaveProvider.SetSlot("target.json");
                }

                Assert.That(source.GetInt("coins"), Is.EqualTo(33), "failed flush must retain pending state");
                Assert.That(loadedCount, Is.Zero, "a rejected switch must not load the destination");

                Directory.Delete(stagingPath);
                source.Save();
                Assert.That(CreateProvider("source.json").GetInt("coins"), Is.EqualTo(33));
                Assert.That(CreateProvider("target.json").GetInt("coins"), Is.EqualTo(900));
            }
            finally
            {
                if (entryPoint == 2)
                {
                    SaveProvider.SetProvider(previousProvider);
                }
            }
        }

        [TestCase("{}")]
        [TestCase("{\"items\":null}")]
        [TestCase("{\"items\":{}}")]
        [TestCase("{\"items\":0}")]
        [TestCase("{\"items\":false}")]
        [TestCase("{\"items\":\"[]\"}")]
        [TestCase("{\"metadata\":{\"items\":[]}}")]
        [TestCase("{\"metadata\":[{\"items\":[]}]}")]
        [TestCase("{\"note\":\"\\\"items\\\":[]\"}")]
        [TestCase("{\"Items\":[]}")]
        [TestCase("{\"items\":[],\"items\":null}")]
        [TestCase("{\"items\":null,\"items\":[]}")]
        [TestCase("{\"items\":[],\"items\":[]}")]
        [TestCase("{\"items\":[]")]
        [TestCase("{\"items\":[]} trailing")]
        [TestCase("{\"items\":[null]}")]
        [TestCase("{\"items\":[{\"key\":\"coins\",\"value\":null}]}")]
        [TestCase("{\"items\":[{\"key\":\"coins\",\"value\":{\"value\":\"20\"}}]}")]
        [TestCase("{\"items\":[{\"key\":\"main-only\",\"value\":{\"type\":\"int\",\"value\":\"99\"}},null]}")]
        public void StructurallyDamagedMainFile_RestoresCompleteBackup(string damagedPayload)
        {
            FileSaveProvider provider = CreateProvider("profile.json");
            provider.SetInt("coins", 10);
            provider.Save();
            provider.SetInt("coins", 20);
            provider.Save();
            string mainPath = Path.Combine(_root, "profile.json");
            byte[] backup = File.ReadAllBytes(mainPath + ".bak");
            File.WriteAllText(mainPath, damagedPayload);
            byte[] damagedPrimary = File.ReadAllBytes(mainPath);

            LogAssert.Expect(LogType.Error, new Regex("was unreadable; restored from"));
            FileSaveProvider recovered = CreateProvider("profile.json");

            Assert.That(recovered.GetInt("coins", -1), Is.EqualTo(10));
            Assert.That(recovered.HasKey("coins"), Is.True);
            Assert.That(recovered.HasKey("main-only"), Is.False, "a rejected primary must not expose partial records");
            Assert.That(File.ReadAllBytes(mainPath), Is.EqualTo(damagedPrimary), "loading must not rewrite the primary");
            Assert.That(File.ReadAllBytes(mainPath + ".bak"), Is.EqualTo(backup), "loading must preserve the complete backup");
        }

        [TestCase("{}", false)]
        [TestCase("{}", true)]
        [TestCase("{\"items\":null}", false)]
        [TestCase("{\"items\":null}", true)]
        [TestCase("{\"items\":[]}", false)]
        [TestCase("{\"items\":[]}", true)]
        [TestCase("{\"nested\":{\"items\":null},\"note\":\"\\\"items\\\":null [ } ]\",\"items\":[]}", false)]
        [TestCase("{\"nested\":{\"items\":null},\"note\":\"\\\"items\\\":null [ } ]\",\"items\":[]}", true)]
        [TestCase("{\"items\":[],\"nested\":[{\"items\":null,\"note\":\"[ } ]\"}]}", false)]
        [TestCase("{\"items\":[],\"nested\":[{\"items\":null,\"note\":\"[ } ]\"}]}", true)]
        [TestCase("{ \"it\\u0065ms\" : [ ] }", false)]
        [TestCase("{ \"it\\u0065ms\" : [ ] }", true)]
        public void RootArrayAdmission_DistinguishesMissingNullAndExplicitEmpty_InPlainAndEncryptedFiles(
            string json, bool encrypt)
        {
            Assert.That(FileSaveEncryptionConfig.TryCreate(encrypt, "", "", out FileSaveEncryptionConfig encryption,
                out string error), Is.True, error);
            FileSaveProviderOptions options = new FileSaveProviderOptions
            {
                PersistenceRoot = _root,
                Encryption = encryption
            };
            FileSaveProvider provider = new FileSaveProvider("admission.json", options);
            provider.SetInt("coins", 10);
            provider.Save();
            provider.SetInt("coins", 20);
            provider.Save();
            string mainPath = Path.Combine(_root, "admission.json");
            byte[] backup = File.ReadAllBytes(mainPath + ".bak");
            string payload = json;
            if (encrypt)
            {
                Assert.That(SaveFileEncryption.TryEncrypt(json, encryption.Key, encryption.Iv, out payload), Is.True);
            }

            File.WriteAllText(mainPath, payload);
            byte[] primary = File.ReadAllBytes(mainPath);
            bool requiresBackup = json == "{}" || json == "{\"items\":null}";
            if (requiresBackup)
            {
                LogAssert.Expect(LogType.Error, new Regex("was unreadable; restored from"));
            }

            FileSaveProvider restored = new FileSaveProvider("admission.json", options);

            Assert.That(restored.HasKey("coins"), Is.EqualTo(requiresBackup));
            Assert.That(restored.GetInt("coins", -1), Is.EqualTo(requiresBackup ? 10 : -1));
            Assert.That(File.ReadAllBytes(mainPath), Is.EqualTo(primary));
            Assert.That(File.ReadAllBytes(mainPath + ".bak"), Is.EqualTo(backup));

            restored.SetInt("coins", 30);
            restored.Save();
            Assert.That(new FileSaveProvider("admission.json", options).GetInt("coins", -1), Is.EqualTo(30));
            if (requiresBackup)
            {
                Assert.That(File.ReadAllBytes(mainPath + ".bak"), Is.EqualTo(backup),
                    "the first commit after recovery must retain the complete backup");
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void NonemptyRootArray_PreservesEscapedAndCanonicalPrimaryOverBackup(bool escapedRoot, bool encrypt)
        {
            const string canonicalPrimary =
                "{\"items\":[{\"key\":\"coins\",\"value\":{\"type\":\"int\",\"value\":\"20\"}},{\"key\":\"primary-marker\",\"value\":{\"type\":\"string\",\"value\":\"alive\"}}]}";
            // Keep a literal backslash-u in the file; do not let the C# compiler decode the member name.
            const string escapedPrimary =
                "{\"it\\u0065ms\":[{\"key\":\"coins\",\"value\":{\"type\":\"int\",\"value\":\"20\"}},{\"key\":\"primary-marker\",\"value\":{\"type\":\"string\",\"value\":\"alive\"}}]}";
            const string backupJson =
                "{\"items\":[{\"key\":\"coins\",\"value\":{\"type\":\"int\",\"value\":\"10\"}},{\"key\":\"backup-marker\",\"value\":{\"type\":\"string\",\"value\":\"kept\"}}]}";
            Assert.That(FileSaveEncryptionConfig.TryCreate(encrypt, "", "", out FileSaveEncryptionConfig encryption,
                out string error), Is.True, error);
            FileSaveProviderOptions options = new FileSaveProviderOptions
            {
                PersistenceRoot = _root,
                Encryption = encryption
            };
            string primaryPayload = escapedRoot ? escapedPrimary : canonicalPrimary;
            string backupPayload = backupJson;
            if (encrypt)
            {
                Assert.That(SaveFileEncryption.TryEncrypt(primaryPayload, encryption.Key, encryption.Iv,
                    out primaryPayload), Is.True);
                Assert.That(SaveFileEncryption.TryEncrypt(backupPayload, encryption.Key, encryption.Iv,
                    out backupPayload), Is.True);
            }

            string mainPath = Path.Combine(_root, "nonempty-primary.json");
            File.WriteAllText(mainPath, primaryPayload);
            File.WriteAllText(mainPath + ".bak", backupPayload);
            byte[] primary = File.ReadAllBytes(mainPath);
            byte[] backup = File.ReadAllBytes(mainPath + ".bak");

            // The public constructor performs the real load, including primary admission and backup selection.
            FileSaveProvider restored = new FileSaveProvider("nonempty-primary.json", options);
            int coins = restored.GetInt("coins", -1);
            string primaryMarker = restored.GetString("primary-marker", "<absent>");
            string backupMarker = restored.GetString("backup-marker", "<absent>");
            string observed = $"escapedRoot={escapedRoot}, encrypt={encrypt}: coins={coins}, " +
                              $"primary-marker={primaryMarker}, backup-marker={backupMarker}. " +
                              "10/kept indicates fallback; -1 with both markers absent indicates accepted-empty.";

            Assert.That(File.ReadAllBytes(mainPath), Is.EqualTo(primary), observed);
            Assert.That(File.ReadAllBytes(mainPath + ".bak"), Is.EqualTo(backup), observed);
            Assert.That(coins, Is.EqualTo(20), observed);
            Assert.That(primaryMarker, Is.EqualTo("alive"), observed);
            Assert.That(restored.HasKey("backup-marker"), Is.False, observed);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FirstCharacterEscapedRootArray_PreservesPrimaryOverBackup(bool encrypt)
        {
            // The first character is a literal JSON escape; the passing middle-character cases stay separate.
            const string primaryJson =
                "{\"\\u0069tems\":[{\"key\":\"coins\",\"value\":{\"type\":\"int\",\"value\":\"20\"}},{\"key\":\"primary-marker\",\"value\":{\"type\":\"string\",\"value\":\"alive\"}}]}";
            const string backupJson =
                "{\"items\":[{\"key\":\"coins\",\"value\":{\"type\":\"int\",\"value\":\"10\"}},{\"key\":\"backup-marker\",\"value\":{\"type\":\"string\",\"value\":\"kept\"}}]}";
            Assert.That(FileSaveEncryptionConfig.TryCreate(encrypt, "", "", out FileSaveEncryptionConfig encryption,
                out string error), Is.True, error);
            FileSaveProviderOptions options = new FileSaveProviderOptions
            {
                PersistenceRoot = _root,
                Encryption = encryption
            };
            string primaryPayload = primaryJson;
            string backupPayload = backupJson;
            if (encrypt)
            {
                Assert.That(SaveFileEncryption.TryEncrypt(primaryPayload, encryption.Key, encryption.Iv,
                    out primaryPayload), Is.True);
                Assert.That(SaveFileEncryption.TryEncrypt(backupPayload, encryption.Key, encryption.Iv,
                    out backupPayload), Is.True);
            }

            string mainPath = Path.Combine(_root, "first-character-primary.json");
            File.WriteAllText(mainPath, primaryPayload);
            File.WriteAllText(mainPath + ".bak", backupPayload);
            byte[] primary = File.ReadAllBytes(mainPath);
            byte[] backup = File.ReadAllBytes(mainPath + ".bak");

            FileSaveProvider restored = new FileSaveProvider("first-character-primary.json", options);
            int coins = restored.GetInt("coins", -1);
            string primaryMarker = restored.GetString("primary-marker", "<absent>");
            string backupMarker = restored.GetString("backup-marker", "<absent>");
            string observed = $"First-character escaped root, encrypt={encrypt}: coins={coins}, " +
                              $"primary-marker={primaryMarker}, backup-marker={backupMarker}. " +
                              "10/kept indicates fallback; -1 with both markers absent indicates accepted-empty.";

            Assert.That(File.ReadAllBytes(mainPath), Is.EqualTo(primary), observed);
            Assert.That(File.ReadAllBytes(mainPath + ".bak"), Is.EqualTo(backup), observed);
            Assert.That(coins, Is.EqualTo(20), observed);
            Assert.That(primaryMarker, Is.EqualTo("alive"), observed);
            Assert.That(restored.HasKey("backup-marker"), Is.False, observed);
        }

        [Test]
        public void SaveAfterRecovery_PreservesKnownGoodBackup_InsteadOfRotatingDamagedPrimary()
        {
            string mainPath = Path.Combine(_root, "recovered.json");
            FileSaveProvider provider = CreateProvider("recovered.json");
            provider.SetInt("coins", 10);
            provider.Save();
            provider.SetInt("coins", 20);
            provider.Save();
            File.WriteAllText(mainPath, "{truncated");
            LogAssert.Expect(LogType.Error, new Regex("was unreadable; restored from"));
            FileSaveProvider recovered = CreateProvider("recovered.json");
            recovered.SetInt("coins", 30);

            recovered.Save();
            Assert.That(CreateProvider("recovered.json").GetInt("coins"), Is.EqualTo(30));
            File.WriteAllText(mainPath, "{truncated again");
            LogAssert.Expect(LogType.Error, new Regex("was unreadable; restored from"));

            Assert.That(CreateProvider("recovered.json").GetInt("coins", -1), Is.EqualTo(10));
        }

        [Test]
        public void EmptyKey_RoundTripsThroughDisk()
        {
            FileSaveProvider provider = CreateProvider("empty-key.json");
            provider.SetInt(string.Empty, 17);
            provider.Save();

            FileSaveProvider restored = CreateProvider("empty-key.json");

            Assert.That(restored.HasKey(string.Empty), Is.True);
            Assert.That(restored.GetInt(string.Empty, -1), Is.EqualTo(17));
        }

        [Test]
        public void ValidEmptyProfile_DoesNotRestoreOlderBackup()
        {
            FileSaveProvider provider = CreateProvider("empty-profile.json");
            provider.SetInt("coins", 10);
            provider.Save();
            provider.DeleteAll();
            provider.Save();

            FileSaveProvider restored = CreateProvider("empty-profile.json");

            Assert.That(restored.HasKey("coins"), Is.False);
            Assert.That(restored.GetInt("coins", -1), Is.EqualTo(-1));
        }

        [Test]
        public void ExplicitReload_DiscardsPendingChanges_WithoutFlushingThemDuringSlotSwitch()
        {
            FileSaveProvider provider = CreateProvider("reload.json");
            provider.SetInt("coins", 10);
            provider.Save();
            provider.SetInt("coins", 20);
            provider.Load();
            Assert.That(provider.GetInt("coins"), Is.EqualTo(10));
            int savedCount = 0;
            provider.OnDataSaved += () => savedCount++;

            Assert.That(provider.TryChangeSlot("other.json"), Is.True);

            Assert.That(savedCount, Is.Zero);
            Assert.That(CreateProvider("reload.json").GetInt("coins"), Is.EqualTo(10));
        }

        private FileSaveProvider CreateProvider(string fileName)
        {
            return new FileSaveProvider(fileName, new FileSaveProviderOptions { PersistenceRoot = _root });
        }
    }
}
