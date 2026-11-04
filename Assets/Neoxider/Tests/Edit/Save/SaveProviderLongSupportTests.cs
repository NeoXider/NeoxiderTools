using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Neo.Editor.Tests;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Save.Tests
{
    /// <summary>
    ///     64-bit integer support: native <see cref="ILongSaveProvider" /> storage in <see cref="FileSaveProvider" />,
    ///     the invariant-string fallback of <see cref="SaveProviderExtensions" /> and the <see cref="SaveProvider" />
    ///     facade, including reads of data written by <c>SetInt</c> or by culture-formatted legacy builds.
    /// </summary>
    public sealed class SaveProviderLongSupportTests
    {
        private const string FileName = "long-support.json";

        private string _root;
        private CultureInfo _previousCulture;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "neo-save-long-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
            _previousCulture = CultureInfo.CurrentCulture;
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentCulture = _previousCulture;
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [TestCase(5_000_000_000L)]
        [TestCase(long.MaxValue)]
        [TestCase(long.MinValue)]
        [TestCase(0L)]
        public void FileProvider_SetLong_RoundTripsBeyondIntRangeAcrossReload(long value)
        {
            FileSaveProvider provider = CreateFileProvider();
            provider.SetLong("cents", value);
            provider.Save();

            FileSaveProvider reloaded = CreateFileProvider();

            Assert.That(reloaded.HasKey("cents"), Is.True);
            Assert.That(reloaded.GetLong("cents", 17L), Is.EqualTo(value));
            StringAssert.Contains("\"long\"", File.ReadAllText(Path.Combine(_root, FileName)));
        }

        [Test]
        public void FileProvider_GetLong_WidensIntAndIntegerStringEntries_AndRejectsOtherTypes()
        {
            FileSaveProvider provider = CreateFileProvider();
            provider.SetInt("legacy-int", -42);
            provider.SetString("legacy-string", "9000000000");
            provider.SetFloat("float", 1.5f);
            provider.SetString("text", "not a number");
            provider.SetBool("flag", true);

            Assert.That(provider.GetLong("legacy-int", 7L), Is.EqualTo(-42L));
            Assert.That(provider.GetLong("legacy-string", 7L), Is.EqualTo(9_000_000_000L));
            Assert.That(provider.GetLong("float", 7L), Is.EqualTo(7L));
            Assert.That(provider.GetLong("text", 7L), Is.EqualTo(7L));
            Assert.That(provider.GetLong("flag", 7L), Is.EqualTo(7L));
            Assert.That(provider.GetLong("missing", 7L), Is.EqualTo(7L));
        }

        [Test]
        public void FileProvider_IntValues_AreWrittenInvariant_AndCultureFormattedLegacyValuesStillLoad()
        {
            CultureInfo.CurrentCulture = CreateTildeNegativeCulture();
            FileSaveProvider provider = CreateFileProvider();
            provider.SetInt("coins", -7);
            provider.SetLong("cents", -8_000_000_000L);
            provider.Save();

            string json = File.ReadAllText(Path.Combine(_root, FileName));
            StringAssert.Contains("\"-7\"", json);
            StringAssert.Contains("\"-8000000000\"", json);
            StringAssert.DoesNotContain("~", json);

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            FileSaveProvider reloaded = CreateFileProvider();
            Assert.That(reloaded.GetInt("coins"), Is.EqualTo(-7));
            Assert.That(reloaded.GetLong("cents"), Is.EqualTo(-8_000_000_000L));

            // WHY: older builds formatted ints with the device culture; such files must keep loading there.
            CultureInfo.CurrentCulture = CreateTildeNegativeCulture();
            File.WriteAllText(Path.Combine(_root, FileName),
                "{\"items\":[{\"key\":\"coins\",\"value\":{\"type\":\"int\",\"value\":\"~5\"}}]}");
            Assert.That(CreateFileProvider().GetInt("coins", 0), Is.EqualTo(-5));
        }

        [Test]
        public void Extension_SetLong_WithoutNativeSupport_StoresInvariantString()
        {
            CultureInfo.CurrentCulture = CreateTildeNegativeCulture();
            TypedMemoryProvider provider = new TypedMemoryProvider();

            ((ISaveProvider)provider).SetLong("cents", -5_000_000_000L);

            Assert.That(provider.Strings["cents"], Is.EqualTo("-5000000000"));
            Assert.That(((ISaveProvider)provider).GetLong("cents"), Is.EqualTo(-5_000_000_000L));
        }

        [Test]
        public void Extension_GetLong_WithoutNativeSupport_ReadsLegacyStringsAndWidensInts()
        {
            CultureInfo.CurrentCulture = CreateTildeNegativeCulture();
            TypedMemoryProvider provider = new TypedMemoryProvider();
            ISaveProvider save = provider;
            save.SetString("consumer-helper", "123456789012");
            save.SetString("culture-legacy", "~9");
            save.SetInt("int-zero", 0);
            save.SetInt("int-negative", -42);
            save.SetFloat("float", 2.5f);
            save.SetString("garbage", "12abc");

            Assert.That(save.GetLong("consumer-helper", 77L), Is.EqualTo(123_456_789_012L));
            Assert.That(save.GetLong("culture-legacy", 77L), Is.EqualTo(-9L));
            Assert.That(save.GetLong("int-zero", 77L), Is.EqualTo(0L), "a stored zero must not read as missing");
            Assert.That(save.GetLong("int-negative", 77L), Is.EqualTo(-42L));
            Assert.That(save.GetLong("float", 77L), Is.EqualTo(77L));
            Assert.That(save.GetLong("garbage", 77L), Is.EqualTo(77L));
            Assert.That(save.GetLong("missing", 77L), Is.EqualTo(77L));
        }

        [Test]
        public void Extension_IntArray_IsCultureInvariant()
        {
            CultureInfo.CurrentCulture = CreateTildeNegativeCulture();
            TypedMemoryProvider provider = new TypedMemoryProvider();
            ISaveProvider save = provider;
            int[] source = { -3, 0, int.MaxValue, int.MinValue };

            save.SetIntArray("scores", source);

            Assert.That(provider.Strings["scores"], Is.EqualTo("-3,0,2147483647,-2147483648"));
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.That(save.GetIntArray("scores"), Is.EqualTo(source));
            save.SetString("scores", "1,99999999999");
            Assert.That(save.GetIntArray("scores", new[] { 5 }), Is.EqualTo(new[] { 5 }),
                "an out-of-range element must fall back instead of wrapping");
        }

        [Test]
        public void Facade_GetSetLong_UsesTheActiveProvider()
        {
            using (new Audit20261023NeoEditorTestsSaveScope())
            {
                SaveProvider.SetLong("Neo.Save.Tests.Facade.Cents", 12_345_678_901L);

                Assert.That(SaveProvider.GetLong("Neo.Save.Tests.Facade.Cents"), Is.EqualTo(12_345_678_901L));
                Assert.That(SaveProvider.CurrentProvider.GetLong("Neo.Save.Tests.Facade.Cents"),
                    Is.EqualTo(12_345_678_901L));
                Assert.That(SaveProvider.GetLong("Neo.Save.Tests.Facade.Missing", -1L), Is.EqualTo(-1L));
            }
        }

        [Test]
        public void PlayerPrefsProvider_LongRoundTrip_AndIntWidening()
        {
            string prefix = "Neo.Save.Tests.Long." + Guid.NewGuid().ToString("N") + ".";
            string longKey = prefix + "long";
            string zeroKey = prefix + "zero";
            string intKey = prefix + "int";
            string floatKey = prefix + "float";
            PlayerPrefsSaveProvider provider = new PlayerPrefsSaveProvider();
            try
            {
                provider.SetLong(longKey, 5_000_000_000L);
                PlayerPrefs.SetInt(zeroKey, 0);
                PlayerPrefs.SetInt(intKey, -3);
                PlayerPrefs.SetFloat(floatKey, 1.25f);

                Assert.That(provider.GetLong(longKey, 9L), Is.EqualTo(5_000_000_000L));
                Assert.That(provider.GetLong(zeroKey, 9L), Is.EqualTo(0L));
                Assert.That(provider.GetLong(intKey, 9L), Is.EqualTo(-3L));
                Assert.That(provider.GetLong(floatKey, 9L), Is.EqualTo(9L));
                Assert.That(provider.GetLong(prefix + "missing", 9L), Is.EqualTo(9L));
            }
            finally
            {
                PlayerPrefs.DeleteKey(longKey);
                PlayerPrefs.DeleteKey(zeroKey);
                PlayerPrefs.DeleteKey(intKey);
                PlayerPrefs.DeleteKey(floatKey);
            }
        }

        private FileSaveProvider CreateFileProvider()
        {
            return new FileSaveProvider(FileName, new FileSaveProviderOptions { PersistenceRoot = _root });
        }

        private static CultureInfo CreateTildeNegativeCulture()
        {
            CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = "~";
            return culture;
        }

        /// <summary>Type-separated store with PlayerPrefs semantics: a read of another type returns the default.</summary>
        private sealed class TypedMemoryProvider : ISaveProvider
        {
            public readonly Dictionary<string, int> Ints = new();
            public readonly Dictionary<string, float> Floats = new();
            public readonly Dictionary<string, string> Strings = new();
            public readonly Dictionary<string, bool> Bools = new();

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0)
            {
                return Ints.TryGetValue(key, out int value) ? value : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                DeleteKey(key);
                Ints[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return Floats.TryGetValue(key, out float value) ? value : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                DeleteKey(key);
                Floats[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return Strings.TryGetValue(key, out string value) ? value : defaultValue;
            }

            public void SetString(string key, string value)
            {
                DeleteKey(key);
                Strings[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return Bools.TryGetValue(key, out bool value) ? value : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                DeleteKey(key);
                Bools[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key)
            {
                return Ints.ContainsKey(key) || Floats.ContainsKey(key) || Strings.ContainsKey(key) ||
                       Bools.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                Ints.Remove(key);
                Floats.Remove(key);
                Strings.Remove(key);
                Bools.Remove(key);
            }

            public void DeleteAll()
            {
                Ints.Clear();
                Floats.Clear();
                Strings.Clear();
                Bools.Clear();
            }

            public void Save()
            {
                OnDataSaved?.Invoke();
            }

            public void Load()
            {
                OnDataLoaded?.Invoke();
            }
        }
    }
}
