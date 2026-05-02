using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     AES file encryption helpers and FileSaveProvider encrypted roundtrips.
    /// </summary>
    public sealed class SaveEncryptionEditModeTests
    {
        private const string K16 = "1234567890123456";
        private const string Iv16 = "abcdefghijklmnop";

        [Test]
        public void DefaultEncryptionConstants_AreValidAesSizes()
        {
            byte[] k = Encoding.UTF8.GetBytes(SaveFileEncryption.DefaultEncryptionKey);
            byte[] v = Encoding.UTF8.GetBytes(SaveFileEncryption.DefaultEncryptionIv);
            Assert.That(k.Length, Is.EqualTo(16));
            Assert.That(v.Length, Is.EqualTo(16));
        }

        [Test]
        public void FileSaveEncryptionConfig_EmptyKeyAndIv_UsesBuiltInDefaults()
        {
            Assert.That(
                FileSaveEncryptionConfig.TryCreate(true, string.Empty, string.Empty, out FileSaveEncryptionConfig cfg,
                    out string err), Is.True, err);
            Assert.That(Encoding.UTF8.GetString(cfg.Key), Is.EqualTo(SaveFileEncryption.DefaultEncryptionKey));
            Assert.That(Encoding.UTF8.GetString(cfg.Iv), Is.EqualTo(SaveFileEncryption.DefaultEncryptionIv));
        }

        [Test]
        public void FileSaveEncryptionConfig_WhitespaceKeyAndIv_UsesBuiltInDefaults()
        {
            Assert.That(
                FileSaveEncryptionConfig.TryCreate(true, "  \t  ", " \n ", out FileSaveEncryptionConfig cfg,
                    out string err), Is.True, err);
            Assert.That(cfg.Key, Is.EqualTo(Encoding.UTF8.GetBytes(SaveFileEncryption.DefaultEncryptionKey)));
            Assert.That(cfg.Iv, Is.EqualTo(Encoding.UTF8.GetBytes(SaveFileEncryption.DefaultEncryptionIv)));
        }

        [Test]
        public void FileSaveEncryptionConfig_OnlyCustomKeyFilled_ReturnsFalse()
        {
            Assert.That(FileSaveEncryptionConfig.TryCreate(true, K16, string.Empty, out _, out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void FileSaveEncryptionConfig_OnlyCustomIvFilled_ReturnsFalse()
        {
            Assert.That(FileSaveEncryptionConfig.TryCreate(true, string.Empty, Iv16, out _, out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void SaveFileEncryption_Roundtrip_WithBuiltInDefaults()
        {
            Assume.That(FileSaveEncryptionConfig.TryCreate(true, "", "", out FileSaveEncryptionConfig cfg,
                out string err), Is.True, err);
            const string plain = "{\"items\":[],\"x\":1}";
            Assert.That(SaveFileEncryption.TryEncrypt(plain, cfg.Key, cfg.Iv, out string b64), Is.True);
            Assert.That(SaveFileEncryption.TryDecrypt(b64, cfg.Key, cfg.Iv, out string back), Is.True);
            Assert.That(back, Is.EqualTo(plain));
        }

        [Test]
        public void FileSaveProvider_EncryptedDisk_WithBuiltInDefaults_Roundtrips()
        {
            Assume.That(FileSaveEncryptionConfig.TryCreate(true, "", "", out FileSaveEncryptionConfig cfg,
                out string err), Is.True, err);

            string tempDir = Path.Combine(Path.GetTempPath(), "NeoSaveDef_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            const string fileName = "def.json";

            try
            {
                var options = new FileSaveProviderOptions { PersistenceRoot = tempDir, Encryption = cfg };
                var p1 = new FileSaveProvider(fileName, options);
                p1.SetInt("slot", 99);
                p1.Save();

                string raw = File.ReadAllText(Path.Combine(tempDir, fileName));
                Assert.That(raw.TrimStart(), Does.Not.StartWith("{"));

                var p2 = new FileSaveProvider(fileName, options);
                Assert.That(p2.GetInt("slot"), Is.EqualTo(99));
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
        }

        private static void TryDeleteDir(string tempDir)
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
            }
        }

        [Test]
        public void FileSaveEncryptionConfig_Disabled_ReturnsWithoutKeyMaterial()
        {
            Assert.That(FileSaveEncryptionConfig.TryCreate(false, "any", "any", out FileSaveEncryptionConfig cfg,
                out string err), Is.True, err);
            Assert.That(cfg.Enabled, Is.False);
            Assert.That(cfg.Key, Is.Null);
            Assert.That(cfg.Iv, Is.Null);
        }

        [Test]
        public void SaveFileEncryption_Roundtrip_PreservesText()
        {
            Assume.That(FileSaveEncryptionConfig.TryCreate(true, K16, Iv16, out FileSaveEncryptionConfig cfg,
                out string err), Is.True, err);
            const string plain = "{\"items\":[],\"note\":\"тест\"}";
            Assert.That(SaveFileEncryption.TryEncrypt(plain, cfg.Key, cfg.Iv, out string b64), Is.True);
            Assert.That(b64, Is.Not.Null.And.Not.Empty);
            Assert.That(SaveFileEncryption.TryDecrypt(b64, cfg.Key, cfg.Iv, out string back), Is.True);
            Assert.That(back, Is.EqualTo(plain));
        }

        [Test]
        public void FileSaveEncryptionConfig_InvalidKey_ReturnsFalse()
        {
            bool ok = FileSaveEncryptionConfig.TryCreate(true, "short", Iv16, out _, out string error);
            Assert.That(ok, Is.False);
            Assert.That(error, Is.Not.Null);
        }

        [Test]
        public void FileSaveProvider_EncryptedFile_RoundtripsThroughDisk()
        {
            Assume.That(FileSaveEncryptionConfig.TryCreate(true, K16, Iv16, out FileSaveEncryptionConfig cfg,
                out string err), Is.True, err);

            string tempDir = Path.Combine(Path.GetTempPath(), "NeoSaveEncTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string fileName = "slot.json";

            try
            {
                var options = new FileSaveProviderOptions
                {
                    PersistenceRoot = tempDir,
                    Encryption = cfg
                };

                var provider = new FileSaveProvider(fileName, options);
                provider.SetInt("gold", 42);
                provider.SetString("hero", "Neo");
                provider.Save();

                string raw = File.ReadAllText(Path.Combine(tempDir, fileName));
                Assert.That(raw.TrimStart(), Does.Not.StartWith("{"));

                var provider2 = new FileSaveProvider(fileName, options);
                Assert.That(provider2.GetInt("gold"), Is.EqualTo(42));
                Assert.That(provider2.GetString("hero"), Is.EqualTo("Neo"));
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
        }

        [Test]
        public void FileSaveProvider_PlainFile_Loads_WhenEncryptionConfigured()
        {
            Assume.That(FileSaveEncryptionConfig.TryCreate(true, K16, Iv16, out FileSaveEncryptionConfig cfg,
                out string err), Is.True, err);

            string tempDir = Path.Combine(Path.GetTempPath(), "NeoSavePlain_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string fileName = "legacy.json";

            try
            {
                var plain = new FileSaveProvider(fileName, new FileSaveProviderOptions { PersistenceRoot = tempDir });
                plain.SetInt("migrated", 7);
                plain.Save();

                var withEnc = new FileSaveProvider(fileName, new FileSaveProviderOptions
                {
                    PersistenceRoot = tempDir,
                    Encryption = cfg
                });
                Assert.That(withEnc.GetInt("migrated"), Is.EqualTo(7));
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
        }

        [Test]
        public void FileSaveProvider_PlainJson_Loads_WhenUsingBuiltInCipherKeys()
        {
            Assume.That(FileSaveEncryptionConfig.TryCreate(true, "", "", out FileSaveEncryptionConfig cfg,
                out string err), Is.True, err);

            string tempDir = Path.Combine(Path.GetTempPath(), "NeoSavePlDef_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            const string fileName = "legacy_def.json";

            try
            {
                var plain = new FileSaveProvider(fileName, new FileSaveProviderOptions { PersistenceRoot = tempDir });
                plain.SetInt("migrated", 8);
                plain.Save();

                var withEnc = new FileSaveProvider(fileName, new FileSaveProviderOptions
                {
                    PersistenceRoot = tempDir,
                    Encryption = cfg
                });
                Assert.That(withEnc.GetInt("migrated"), Is.EqualTo(8));
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
        }

        [Test]
        public void FileSaveProvider_EmptyFile_LoadsEmptyData()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "NeoSaveEmpty_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            const string fileName = "empty.json";

            try
            {
                File.WriteAllText(Path.Combine(tempDir, fileName), string.Empty);
                var provider = new FileSaveProvider(fileName, new FileSaveProviderOptions { PersistenceRoot = tempDir });
                Assert.That(provider.HasKey("any"), Is.False);
                Assert.That(provider.GetInt("k", 7), Is.EqualTo(7));
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
        }

        [Test]
        public void FileSaveProvider_InvalidJson_StartsFreshDictionary()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[FileSaveProvider\] Unrecognized save file format"));

            string tempDir = Path.Combine(Path.GetTempPath(), "NeoSaveBad_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            const string fileName = "bad.json";

            try
            {
                File.WriteAllText(Path.Combine(tempDir, fileName), "{not valid json");
                var provider = new FileSaveProvider(fileName, new FileSaveProviderOptions { PersistenceRoot = tempDir });
                Assert.That(provider.HasKey("gold"), Is.False);
                Assert.That(provider.GetFloat("gold", 3.5f), Is.EqualTo(3.5f));
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
        }
    }
}
