using System;
using System.Reflection;
using Neo.Settings;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Tests for the GameSettings static reset fix (P1).
    ///     Verifies that ResetStaticState clears events and all static fields.
    /// </summary>
    [TestFixture]
    public class GameSettingsStaticResetTests
    {
        private static void ResetSettings()
        {
            typeof(GameSettings).GetMethod("ResetStaticStateForTesting",
                BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
        }

        [TearDown]
        public void TearDown()
        {
            ResetSettings();
        }

        [Test]
        public void ResetStaticState_ClearsMouseSensitivity()
        {
            // Set a non-default value
            GameSettings.SetMouseSensitivity(10f, SettingsPersistMode.SkipUntilFlush);

            ResetSettings();

            Assert.AreEqual(2f, GameSettings.MouseSensitivity, 0.001f,
                "MouseSensitivity should reset to default 2.0");
        }

        [Test]
        public void ResetStaticState_ClearsOnSettingsChangedEvent()
        {
            int callCount = 0;
            GameSettings.OnSettingsChanged += () => callCount++;

            // Verify event fires
            GameSettings.SetMouseSensitivity(5f, SettingsPersistMode.SkipUntilFlush);
            Assert.AreEqual(1, callCount, "Event should fire before reset");

            // Reset clears events
            ResetSettings();

            // Setting again should NOT fire the old listener
            callCount = 0;
            GameSettings.SetMouseSensitivity(3f, SettingsPersistMode.SkipUntilFlush);
            Assert.AreEqual(0, callCount, "Old listener should not fire after reset");
        }

        [Test]
        public void ResetStaticState_ClearsGraphicsPreset()
        {
            ResetSettings();

            Assert.AreEqual(GraphicsPreset.High, GameSettings.GraphicsPreset,
                "GraphicsPreset should reset to High");
        }

        [Test]
        public void ResetStaticState_ClearsFramerateCap()
        {
            ResetSettings();

            Assert.AreEqual(-1, GameSettings.FramerateCap,
                "FramerateCap should reset to -1 (unlimited)");
        }
    }
}
