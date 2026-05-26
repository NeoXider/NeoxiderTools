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
            MethodInfo setIsLoad = isLoadProp?.GetSetMethod(true);
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
        private GameObject _cameraObject;
        private GameObject _managerObject;

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

            if (_managerObject != null)
            {
                Object.DestroyImmediate(_managerObject);
            }

            if (_cameraObject != null)
            {
                Object.DestroyImmediate(_cameraObject);
            }
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

        [Test]
        public void SetTargetCamera_StoresExplicitCameraReference()
        {
            _managerObject = new GameObject("MouseInputManagerTest");
            MouseInputManager manager = _managerObject.AddComponent<MouseInputManager>();
            _cameraObject = new GameObject("MouseInputCamera");
            Camera camera = _cameraObject.AddComponent<Camera>();

            manager.SetTargetCamera(camera);

            Assert.That(manager.TargetCamera, Is.SameAs(camera));
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
