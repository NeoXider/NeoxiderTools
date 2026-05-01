using System.Reflection;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tools.Tests
{
    public sealed class SaveManagerSubsystemCachesEditModeTests
    {
        [Test]
        public void ClearSubsystemCaches_ClearsIsLoadFlag()
        {
            PropertyInfo isLoadProp = typeof(SaveManager).GetProperty(
                nameof(SaveManager.IsLoad),
                BindingFlags.Public | BindingFlags.Static);
            MethodInfo setIsLoad = isLoadProp?.GetSetMethod(nonPublic: true);
            Assume.That(setIsLoad, Is.Not.Null);
            setIsLoad!.Invoke(null, new object[] { true });
            Assume.That(SaveManager.IsLoad, Is.True);

            SaveManager.ClearSubsystemCaches();

            Assert.That(SaveManager.IsLoad, Is.False);
        }
    }

    public sealed class MouseInputManagerSubsystemCachesEditModeTests
    {
        private bool _savedCreateInstance;

        [SetUp]
        public void SetUp()
        {
            _savedCreateInstance = MouseInputManager.CreateInstance;
        }

        [TearDown]
        public void TearDown()
        {
            MouseInputManager.CreateInstance = _savedCreateInstance;
            MouseInputManager.ResetSubsystemPollingState();
        }

        [Test]
        public void ResetSubsystemPollingState_ClearsStatics()
        {
            MouseInputManager.LastEventData = new MouseInputManager.MouseEventData(
                Vector2.one, Vector3.one, null, default, default);
            MouseInputManager.HasEventData = true;

            MouseInputManager.ResetSubsystemPollingState();

            Assert.That(MouseInputManager.HasEventData, Is.False);
            Assert.That(MouseInputManager.LastEventData.HitObject, Is.Null);
        }

        [Test]
        public void EnableAutoCreateForRuntime_SetsCreateInstanceFlag()
        {
            MouseInputManager.CreateInstance = false;

            MouseInputManager.EnableAutoCreateForRuntime();

            Assert.That(MouseInputManager.CreateInstance, Is.True);
        }
    }
}
