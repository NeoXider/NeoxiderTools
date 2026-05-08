using System.Reflection;
using Neo.Condition;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     Tests for the NeoCondition EveryFrame throttle fix (P1-7).
    ///     Verifies that _nextEveryFrameCheck field exists and the throttle gate works.
    /// </summary>
    [TestFixture]
    public class NeoConditionThrottleTests
    {
        [Test]
        public void NeoCondition_HasThrottleField()
        {
            // Verify the throttle field added by P1-7 exists
            FieldInfo field = typeof(NeoCondition).GetField("_nextEveryFrameCheck",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field,
                "NeoCondition should have _nextEveryFrameCheck field for EveryFrame throttle.");
            Assert.AreEqual(typeof(float), field.FieldType);
        }

        [Test]
        public void NeoCondition_EveryFrame_ThrottleGateRejectsBeforeInterval()
        {
            var go = new GameObject("CondThrottle");
            NeoCondition nc = go.AddComponent<NeoCondition>();
            try
            {
                // Set _checkMode to EveryFrame
                FieldInfo modeField = typeof(NeoCondition).GetField("_checkMode",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(modeField, "_checkMode field should exist");
                modeField.SetValue(nc, CheckMode.EveryFrame);

                // Set _nextEveryFrameCheck to far future so Update() skips
                FieldInfo throttleField = typeof(NeoCondition).GetField("_nextEveryFrameCheck",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                throttleField.SetValue(nc, float.MaxValue);

                // Track how many times Check is called
                int checkCount = 0;
                nc.OnTrue.AddListener(() => checkCount++);

                // Disable onlyOnChange to count all fires
                FieldInfo onlyOnChange = typeof(NeoCondition).GetField("_onlyOnChange",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                onlyOnChange?.SetValue(nc, false);

                // Simulate Update — should be skipped due to throttle
                MethodInfo update = typeof(NeoCondition).GetMethod("Update",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                update?.Invoke(nc, null);

                Assert.AreEqual(0, checkCount,
                    "Update should NOT call Check when throttle gate blocks it.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void NeoCondition_OnDisable_DoesNotThrow()
        {
            var go = new GameObject("CondDisable");
            NeoCondition nc = go.AddComponent<NeoCondition>();
            try
            {
                // OnDisable should gracefully handle cleanup
                Assert.DoesNotThrow(() => go.SetActive(false));
                Assert.DoesNotThrow(() => go.SetActive(true));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
