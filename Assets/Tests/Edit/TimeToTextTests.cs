using System;
using Neo.Extensions;
using Neo.Tools;
using Neo.NoCode;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Neo.Tests.Edit
{
    public class TimeToTextTests
    {
        private GameObject _go;
        private TimeToText _timeToText;
        private TextMeshProUGUI _textMesh;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TimeToTextTest");
            _textMesh = _go.AddComponent<TextMeshProUGUI>();
            _timeToText = _go.AddComponent<TimeToText>();
            _timeToText.text = _textMesh;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void Set_ClockMode_FormatsCorrectly()
        {
            _timeToText.DisplayMode = TimeDisplayMode.Clock;
            _timeToText.TimeFormat = TimeFormat.MinutesSeconds;
            _timeToText.Set(90);

            Assert.AreEqual("01:30", _textMesh.text);
        }
        
        [Test]
        public void Set_CompactMode_FormatsCorrectly()
        {
            _timeToText.DisplayMode = TimeDisplayMode.Compact;
            _timeToText.CompactIncludeSeconds = true;
            _timeToText.CompactMaxParts = 3;
            
            // 90 seconds = 1 minute, 30 seconds
            _timeToText.Set(90);
            
            Assert.AreEqual("1m 30s", _textMesh.text);
        }

        [Test]
        public void Set_CompactMode_DoesNotSkipZeros_IfLargestPartExisted()
        {
            _timeToText.DisplayMode = TimeDisplayMode.Compact;
            _timeToText.CompactUseTimeFormat = false; // Test dynamic
            _timeToText.CompactIncludeSeconds = true;
            _timeToText.CompactMaxParts = 2; // e.g., Days and Hours

            // 1 day (86400) + 1 second (1). Hours and minutes are 0.
            _timeToText.Set(86401);
            
            // Should be "1d 00h", not "1d 1s"
            Assert.AreEqual("1d 00h", _textMesh.text);
        }

        [Test]
        public void Set_CompactMode_WithTimeFormat_FormatsCorrectly()
        {
            _timeToText.DisplayMode = TimeDisplayMode.Compact;
            _timeToText.CompactUseTimeFormat = true;
            _timeToText.TimeFormat = TimeFormat.HoursMinutes;
            
            // 90 seconds = 0 hours, 1 minute
            _timeToText.Set(90);
            
            Assert.AreEqual("0h 01m", _textMesh.text);
        }

        [Test]
        public void ToCompactString_WithTimeFormat_ZeroTime_FormatsCorrectly()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(0);
            string result = timeSpan.ToCompactString(TimeFormat.HoursMinutes);
            
            Assert.AreEqual("0h 00m", result);
        }
        
        [Test]
        public void ToCompactString_DaysAndMinutes_ZerosAreKept()
        {
            // Just verifying the extension method itself
            TimeSpan timeSpan = TimeSpan.FromSeconds(86401); 
            string result = timeSpan.ToCompactString(true, 3);
            
            // 1 day, 0 hours, 0 minutes (since max parts is 3, it stops at minutes)
            Assert.AreEqual("1d 00h 00m", result);
        }

        [Test]
        public void Set_ZeroText_DisplaysZeroAccordingToFlag()
        {
            _timeToText.ZeroText = true;
            _timeToText.Set(0);
            Assert.IsNotEmpty(_textMesh.text);

            _timeToText.ZeroText = false;
            _timeToText.Set(0);
            Assert.IsEmpty(_textMesh.text);
        }

        [Test]
        public void TrySetFromString_ParsesAndSetsCorrectly()
        {
            _timeToText.DisplayMode = TimeDisplayMode.Clock;
            _timeToText.TimeFormat = TimeFormat.MinutesSeconds;

            bool result = _timeToText.TrySetFromString("02:30", ":");

            Assert.IsTrue(result);
            Assert.AreEqual("02:30", _textMesh.text);
            Assert.AreEqual(150f, _timeToText.CurrentTime);
        }

        [Test]
        public void NoCodeBindText_PushesToTimeToText()
        {
            var noCodeBind = _go.AddComponent<NoCodeBindText>();
            // Use reflection or standard Unity method to set private field if needed, but we can test via GetComponent since NoCodeBindText uses GetComponent as fallback!
            
            _timeToText.DisplayMode = TimeDisplayMode.Compact;
            _timeToText.CompactUseTimeFormat = true;
            _timeToText.TimeFormat = TimeFormat.HoursMinutes;

            // Assuming NoCodeBindText.ApplyFloat(float) is protected, 
            // but INoCodeBindFloat interface might have an UpdateValue method or similar.
            // Since it inherits from NoCodeFloatBindingBehaviour, we can simulate setting the value.
            // But NoCodeFloatBindingBehaviour handles value changes. 
            // We can call ApplyFloat via reflection since it's protected.
            
            var applyFloatMethod = typeof(NoCodeFloatBindingBehaviour).GetMethod("ApplyFloat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (applyFloatMethod != null)
            {
                applyFloatMethod.Invoke(noCodeBind, new object[] { 3600f }); // 1 hour
            }
            else
            {
                // Fallback if ApplyFloat doesn't exist, we just trust the test.
                Debug.LogWarning("ApplyFloat method not found");
            }

            Assert.AreEqual("1h 00m", _textMesh.text);
        }
    }
}
