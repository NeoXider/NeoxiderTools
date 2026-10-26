using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    // Requires the exclusive, isolated validation host: this fixture temporarily changes the static facade
    // and creates one Resources asset. It restores the provider/settings/logging without clearing listeners.
    public sealed class Audit20261023NeoSaveConfigurationTests
    {
        private const string Key = "1234567890123456";
        private const string Iv = "abcdefghijklmnop";
        private string _root;
        private string _profilePath;
        private string _assetFolder;
        private SaveProviderSettings _settings;
        private SaveProviderSettings _previousSettings;
        private ISaveProvider _previousProvider;
        private bool _previousLogging;
        private GameObject _bootstrapObject;

        [SetUp]
        public void SetUp()
        {
            _assetFolder = null;
            _bootstrapObject = null;
            _previousProvider = SaveProvider.CurrentProvider;
            _previousSettings = (SaveProviderSettings)GetFacadeField("_settings").GetValue(null);
            _previousLogging = SaveProvider.DebugLoggingEnabled;
            SaveProvider.DebugLoggingEnabled = false;
            _root = Path.Combine(Path.GetTempPath(), "Audit20261023NeoSaveConfig_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            _profilePath = Path.Combine(_root, "profile.json");
            _settings = ScriptableObject.CreateInstance<SaveProviderSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_bootstrapObject != null)
                {
                    Object.DestroyImmediate(_bootstrapObject);
                }

                SaveProvider.DebugLoggingEnabled = false;
                SaveProvider.SetProvider(_previousProvider);
                GetFacadeField("_settings").SetValue(null, _previousSettings);
                SaveProvider.DebugLoggingEnabled = _previousLogging;
            }
            finally
            {
                try
                {
                    if (_assetFolder != null)
                    {
                        Assert.That(AssetDatabase.DeleteAsset(_assetFolder), Is.True,
                            "The fixture must remove its temporary Resources asset folder.");
                    }

                    if (_settings != null && !EditorUtility.IsPersistent(_settings))
                    {
                        Object.DestroyImmediate(_settings);
                    }
                }
                finally
                {
                    if (_root != null && Directory.Exists(_root))
                    {
                        Directory.Delete(_root, true);
                    }
                }
            }
        }

        [TestCase("invalid-key-secret", Iv)]
        [TestCase(Key, "invalid-iv-secret")]
        [TestCase(Key, "")]
        [TestCase("", Iv)]
        public void InvalidEncryption_RejectsBothFactoriesWithoutFileAccess_AndAllowsCorrectedRetry(
            string key, string iv)
        {
            SeedEncryptedProfile();
            byte[] primary = File.ReadAllBytes(_profilePath);
            byte[] backup = File.ReadAllBytes(_profilePath + ".bak");
            byte[] staging = File.ReadAllBytes(_profilePath + ".tmp");
            Configure(SaveProviderType.File, true, key, iv);

            Assert.That(_settings.TryCreateProvider(out ISaveProvider rejected, out string error), Is.False);
            Assert.That(rejected, Is.Null);
            AssertSafeError(error, key, iv);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => _settings.CreateProvider());
            Assert.That(exception.Message, Is.EqualTo(error));
            AssertFilesUnchanged(primary, backup, staging);

            Configure(SaveProviderType.File, true, Key, Iv);
            Assert.That(_settings.TryCreateProvider(out ISaveProvider corrected, out string correctedError), Is.True);
            Assert.That(correctedError, Is.Null);
            Assert.That(corrected.GetInt("coins", -1), Is.EqualTo(20));
            corrected.SetInt("coins", 30);
            corrected.Save();

            Assert.That(_settings.CreateProvider().GetInt("coins", -1), Is.EqualTo(30));
            Assert.That(File.ReadAllBytes(_profilePath + ".bak"), Is.EqualTo(primary));
            Assert.That(File.ReadAllText(_profilePath).TrimStart(), Does.Not.StartWith("{"));
        }

        [TestCase(false, "invalid-key-secret", "invalid-iv-secret")]
        [TestCase(true, "", "")]
        [TestCase(true, Key, Iv)]
        public void SupportedFileConfiguration_PreservesPlaintextDefaultsAndCustomEncryption(
            bool encrypt, string key, string iv)
        {
            Configure(SaveProviderType.File, encrypt, key, iv);

            Assert.That(_settings.TryCreateProvider(out ISaveProvider provider, out string error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(provider, Is.TypeOf<FileSaveProvider>());
            provider.SetInt("coins", 7);
            provider.Save();
            Assert.That(_settings.CreateProvider().GetInt("coins", -1), Is.EqualTo(7));

            string payload = File.ReadAllText(_profilePath).TrimStart();
            if (encrypt)
            {
                Assert.That(payload, Does.Not.StartWith("{"));
                Assert.That(FileSaveEncryptionConfig.TryCreate(true, key, iv,
                    out FileSaveEncryptionConfig encryption, out string encryptionError), Is.True, encryptionError);
                Assert.That(SaveFileEncryption.TryDecrypt(payload, encryption.Key, encryption.Iv,
                    out string plaintext), Is.True);
                Assert.That(plaintext.TrimStart(), Does.StartWith("{"));
            }
            else
            {
                Assert.That(payload, Does.StartWith("{"));
            }
        }

        [Test]
        public void PlayerPrefsConfiguration_IgnoresUnusedFileEncryption()
        {
            Configure(SaveProviderType.PlayerPrefs, true, "invalid-key-secret", "invalid-iv-secret");

            Assert.That(_settings.TryCreateProvider(out ISaveProvider provider, out string error), Is.True);

            Assert.That(provider, Is.TypeOf<PlayerPrefsSaveProvider>());
            Assert.That(error, Is.Null);
            Assert.That(File.Exists(_profilePath), Is.False);
        }

        [Test]
        public void SceneBootstrapFailure_RetainsProviderAndEvents_ThenAcceptsCorrectedSettings()
        {
            SeedEncryptedProfile();
            byte[] primary = File.ReadAllBytes(_profilePath);
            byte[] backup = File.ReadAllBytes(_profilePath + ".bak");
            byte[] staging = File.ReadAllBytes(_profilePath + ".tmp");
            FileSaveProvider active = new FileSaveProvider("active.json",
                new FileSaveProviderOptions { PersistenceRoot = _root });
            active.SetInt("resident", 70);
            SaveProvider.SetProvider(active);
            Configure(SaveProviderType.File, true, Key, "");
            _bootstrapObject = new GameObject("Audit20261023NeoSaveConfigurationBootstrap");
            _bootstrapObject.SetActive(false);
            SaveProviderSettingsComponent component = _bootstrapObject.AddComponent<SaveProviderSettingsComponent>();
            SerializedObject serializedComponent = new SerializedObject(component);
            serializedComponent.FindProperty("_settings").objectReferenceValue = _settings;
            serializedComponent.ApplyModifiedPropertiesWithoutUndo();
            MethodInfo awake = typeof(SaveProviderSettingsComponent).GetMethod("Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null);
            int changedCount = 0;
            Action<string> onChanged = changedKey => changedCount++;
            SaveProvider.OnKeyChanged += onChanged;

            try
            {
                SaveProvider.DebugLoggingEnabled = true;
                LogAssert.Expect(LogType.Error, new Regex("Invalid file encryption configuration:"));
                awake.Invoke(component, null);
                SaveProvider.DebugLoggingEnabled = false;
                Assert.That(SaveProvider.CurrentProvider, Is.SameAs(active));
                AssertFilesUnchanged(primary, backup, staging);
                SaveProvider.SetInt("resident", 71);
                Assert.That(active.GetInt("resident"), Is.EqualTo(71));
                Assert.That(changedCount, Is.EqualTo(1));

                Configure(SaveProviderType.File, true, Key, Iv);
                awake.Invoke(component, null);
                Assert.That(SaveProvider.CurrentProvider, Is.Not.SameAs(active));
                Assert.That(SaveProvider.GetInt("coins", -1), Is.EqualTo(20));
                active.SetInt("resident", 72);
                Assert.That(changedCount, Is.EqualTo(1), "the old provider must be detached after successful replacement");
                SaveProvider.SetInt("coins", 30);
                Assert.That(changedCount, Is.EqualTo(2));
                SaveProvider.Save();
                Assert.That(_settings.CreateProvider().GetInt("coins", -1), Is.EqualTo(30));
            }
            finally
            {
                SaveProvider.OnKeyChanged -= onChanged;
            }
        }

        [Test]
        public void LazyResourcesInitialization_FailsAgainUntilCorrected_ThenRecoversEncryptedProfile()
        {
            Assert.That(Resources.Load<SaveProviderSettings>("SaveProviderSettings"), Is.Null,
                "Run in the isolated host without another Resources/SaveProviderSettings asset.");
            SeedEncryptedProfile();
            byte[] primary = File.ReadAllBytes(_profilePath);
            byte[] backup = File.ReadAllBytes(_profilePath + ".bak");
            byte[] staging = File.ReadAllBytes(_profilePath + ".tmp");
            Configure(SaveProviderType.File, true, "invalid-key-secret", Iv);
            string folderName = "Audit20261023NeoSaveConfig_" + Guid.NewGuid().ToString("N");
            _assetFolder = "Assets/" + folderName;
            AssetDatabase.CreateFolder("Assets", folderName);
            AssetDatabase.CreateFolder(_assetFolder, "Resources");
            string assetPath = _assetFolder + "/Resources/SaveProviderSettings.asset";
            AssetDatabase.CreateAsset(_settings, assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            _settings = Resources.Load<SaveProviderSettings>("SaveProviderSettings");
            Assert.That(_settings, Is.Not.Null);

            // Reset only the lazy-provider state, preserving public listeners and lifecycle-hook ownership.
            MethodInfo detach = typeof(SaveProvider).GetMethod("DetachProviderEvents",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(detach, Is.Not.Null);
            detach.Invoke(null, new object[] { _previousProvider });
            GetFacadeField("_provider").SetValue(null, null);
            GetFacadeField("_settings").SetValue(null, null);
            GetFacadeField("_isInitialized").SetValue(null, false);

            InvalidOperationException first = Assert.Throws<InvalidOperationException>(() =>
            {
                _ = SaveProvider.CurrentProvider;
            });
            AssertSafeError(first.Message, "invalid-key-secret", Iv);
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = SaveProvider.CurrentProvider;
            });
            AssertFilesUnchanged(primary, backup, staging);

            Configure(SaveProviderType.File, true, Key, Iv);
            ISaveProvider corrected = SaveProvider.CurrentProvider;
            Assert.That(corrected, Is.TypeOf<FileSaveProvider>());
            Assert.That(corrected.GetInt("coins", -1), Is.EqualTo(20));
            Assert.That(SaveProvider.CurrentProvider, Is.SameAs(corrected));
            SaveProvider.SetInt("coins", 30);
            SaveProvider.Save();
            Assert.That(_settings.CreateProvider().GetInt("coins", -1), Is.EqualTo(30));
            Assert.That(File.ReadAllBytes(_profilePath + ".bak"), Is.EqualTo(primary));
        }

        private void Configure(SaveProviderType providerType, bool encrypt, string key, string iv)
        {
            SerializedObject serializedSettings = new SerializedObject(_settings);
            serializedSettings.FindProperty("_providerType").intValue = (int)providerType;
            serializedSettings.FindProperty("_fileName").stringValue = _profilePath;
            serializedSettings.FindProperty("_encryptFileSave").boolValue = encrypt;
            serializedSettings.FindProperty("_fileEncryptionKey").stringValue = key;
            serializedSettings.FindProperty("_fileEncryptionIv").stringValue = iv;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SeedEncryptedProfile()
        {
            Assert.That(FileSaveEncryptionConfig.TryCreate(true, Key, Iv, out FileSaveEncryptionConfig encryption,
                out string error), Is.True, error);
            FileSaveProvider provider = new FileSaveProvider("profile.json",
                new FileSaveProviderOptions { PersistenceRoot = _root, Encryption = encryption });
            provider.SetInt("coins", 10);
            provider.Save();
            provider.SetInt("coins", 20);
            provider.Save();
            // FileSaveProvider.Load deletes this marker; retaining it proves rejected settings never construct one.
            File.WriteAllText(_profilePath + ".tmp", "uncommitted encrypted staging marker");
        }

        private void AssertFilesUnchanged(byte[] primary, byte[] backup, byte[] staging)
        {
            Assert.That(File.ReadAllBytes(_profilePath), Is.EqualTo(primary));
            Assert.That(File.ReadAllBytes(_profilePath + ".bak"), Is.EqualTo(backup));
            Assert.That(File.ReadAllBytes(_profilePath + ".tmp"), Is.EqualTo(staging));
        }

        private static void AssertSafeError(string error, string key, string iv)
        {
            Assert.That(error, Does.Contain("Invalid file encryption configuration:"));
            if (!string.IsNullOrEmpty(key))
            {
                Assert.That(error, Does.Not.Contain(key));
            }

            if (!string.IsNullOrEmpty(iv))
            {
                Assert.That(error, Does.Not.Contain(iv));
            }
        }

        private static FieldInfo GetFacadeField(string fieldName)
        {
            FieldInfo field = typeof(SaveProvider).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field;
        }
    }
}
