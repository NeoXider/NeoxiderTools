using System;
using System.Globalization;
using Neo.Extensions;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class Audit20261023TestUtilitiesSerializationTests
    {
        [TestCase("en-US", DateTimeKind.Utc)]
        [TestCase("ru-RU", DateTimeKind.Utc)]
        [TestCase("ru-RU", DateTimeKind.Unspecified)]
        public void UtcRoundTrip_PreservesTicksAndUtcKindAcrossCultures(string culture, DateTimeKind kind)
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                DateTime original = new DateTime(2026, 10, 23, 7, 8, 9, kind).AddTicks(1234567);

                string serialized = original.ToRoundTripUtcString();
                bool parsed = serialized.TryParseUtcRoundTrip(out DateTime restored);

                Assert.That(serialized, Is.EqualTo("2026-10-23T07:08:09.1234567Z"));
                Assert.That(parsed, Is.True);
                Assert.That(restored.Ticks, Is.EqualTo(original.Ticks));
                Assert.That(restored.Kind, Is.EqualTo(DateTimeKind.Utc));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void UtcParsing_ExplicitOffset_NormalizesTheInstantWithoutLosingTicks()
        {
            bool parsed = "2026-10-23T12:08:09.1234567+05:00".TryParseUtcRoundTrip(out DateTime restored);
            DateTime expected = new DateTime(2026, 10, 23, 7, 8, 9, DateTimeKind.Utc).AddTicks(1234567);

            Assert.That(parsed, Is.True);
            Assert.That(restored.Ticks, Is.EqualTo(expected.Ticks));
            Assert.That(restored.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void FloatPreferences_WrittenUnderRussianCulture_LoadUnderEnglishCulture()
        {
            string key = "Neo.Audit20261023TestUtilities." + Guid.NewGuid().ToString("N");
            CultureInfo previous = CultureInfo.CurrentCulture;
            float[] source = { 1.25f, -12.5f, 0f };
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
                PlayerPrefsUtils.SetFloatArray(key, source);
                Assert.That(PlayerPrefs.GetString(key), Is.EqualTo("1.25,-12.5,0"));

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

                CollectionAssert.AreEqual(source, PlayerPrefsUtils.GetFloatArray(key));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                PlayerPrefs.DeleteKey(key);
            }
        }

        [Test]
        public void StringPreferences_ActualLegacyJson_RestoresCommasQuotesAndEmptyElements()
        {
            string key = "Neo.Audit20261023TestUtilities." + Guid.NewGuid().ToString("N");
            const string raw = "{\"Value\":[\"left,right\",\"quote\\\"name\",\"\"]}";
            try
            {
                PlayerPrefs.SetString(key, raw);

                string[] restored = PlayerPrefsUtils.GetStringArray(key);

                CollectionAssert.AreEqual(new[] { "left,right", "quote\"name", "" }, restored);
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        [Test]
        public void StringPreferences_RejectedComma_LeavesExistingValueUnchanged()
        {
            string key = "Neo.Audit20261023TestUtilities." + Guid.NewGuid().ToString("N");
            try
            {
                PlayerPrefs.SetString(key, "kept");

                Assert.Throws<ArgumentException>(() =>
                    PlayerPrefsUtils.SetStringArray(key, new[] { "valid", "two,parts" }));

                Assert.That(PlayerPrefs.GetString(key), Is.EqualTo("kept"));
                CollectionAssert.AreEqual(new[] { "kept" }, PlayerPrefsUtils.GetStringArray(key));
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StringPreferences_EmptyWrite_DeletesTheKeyAndPreservesTheReadDefault(bool useNull)
        {
            string key = "Neo.Audit20261023TestUtilities." + Guid.NewGuid().ToString("N");
            string[] fallback = { "fallback" };
            try
            {
                PlayerPrefs.SetString(key, "old");

                PlayerPrefsUtils.SetStringArray(key, useNull ? null : Array.Empty<string>());

                Assert.That(PlayerPrefs.HasKey(key), Is.False);
                Assert.That(PlayerPrefsUtils.GetStringArray(key, fallback), Is.SameAs(fallback));
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        [TestCase("2||30.5", 150.5f)]
        [TestCase("1||2||3||4", 93784f)]
        public void Duration_MultiCharacterSeparator_ProducesTheExpectedSeconds(string raw, float expected)
        {
            bool parsed = TimeParsingExtensions.TryParseDuration(raw, out float seconds, "||");

            Assert.That(parsed, Is.True);
            Assert.That(seconds, Is.EqualTo(expected));
        }

        [TestCase("1:60")]
        [TestCase("1:24:00:00")]
        [TestCase("1::2")]
        [TestCase("1:2:3:4:5")]
        [TestCase("-1:30")]
        public void Duration_InvalidSegments_ReturnFalseAndZero(string raw)
        {
            bool parsed = TimeParsingExtensions.TryParseDuration(raw, out float seconds);

            Assert.That(parsed, Is.False);
            Assert.That(seconds, Is.Zero);
        }
    }
}
