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

    public sealed class SwipeControllerSubsystemCachesEditModeTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            SwipeController.ResetStaticStateForRuntime();
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void ResetStaticStateForRuntime_ClearsInstanceAndPollingIsSafe()
        {
            _go = new GameObject("SwipeControllerTest");
            SwipeController controller = _go.AddComponent<SwipeController>();
            SetSwipeControllerInstance(controller);
            Assume.That(SwipeController.Instance, Is.Not.Null);

            SwipeController.ResetStaticStateForRuntime();

            Assert.That(SwipeController.Instance, Is.Null);
            Assert.That(SwipeController.GetSwipeDirection(out _), Is.False);
        }

        private static void SetSwipeControllerInstance(SwipeController controller)
        {
            FieldInfo field = typeof(SwipeController).GetField(
                "instance",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            field!.SetValue(null, controller);
        }
    }
}
