using System.Collections;
using System.Reflection;
using Neo.Extensions;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class ExtensionsLifecycleTests
    {
        [TearDown]
        public void TearDown()
        {
            InvokeCoroutineReset();
        }

        [Test]
        public void CoroutineExtensions_StartNull_DoesNotCreateGlobalHelper()
        {
            InvokeCoroutineReset();

            CoroutineExtensions.CoroutineHandle handle = CoroutineExtensions.Start(null);

            Assert.IsNotNull(handle);
            Assert.IsFalse(handle.IsRunning);
            Assert.IsNull(GetCoroutineHelperInstance());
        }

        [Test]
        public void CoroutineExtensions_SubsystemReset_ClearsStaticHelperReference()
        {
            var helperObject = new GameObject("CoroutineHelperTest");
            var helper = helperObject.AddComponent<CoroutineHelper>();
            SetCoroutineHelperInstance(helper);

            InvokeCoroutineReset();

            Assert.IsNull(GetCoroutineHelperInstance());
            Object.DestroyImmediate(helperObject);
        }

        [Test]
        public void CoroutineLifecycleTracker_CompleteAll_InvalidatesHandles()
        {
            var ownerObject = new GameObject("CoroutineOwner");
            CoroutineExtensions.CoroutineHandle handle = null;

            try
            {
                var owner = ownerObject.AddComponent<CoroutineRunner>();
                var tracker = ownerObject.AddComponent<CoroutineLifecycleTracker>();
                handle = CreateHandle(owner);
                BindTracker(handle, tracker);
                SetCoroutine(handle, null);

                Assert.IsTrue(handle.IsRunning);

                InvokeCompleteAll(tracker);

                Assert.IsFalse(handle.IsRunning);
            }
            finally
            {
                Object.DestroyImmediate(ownerObject);
            }
        }

        [Test]
        public void AudioExtensions_FadeInNull_ReturnsNull()
        {
            AudioSource source = null;

            Assert.IsNull(source.FadeIn(0.1f));
        }

        private static CoroutineExtensions.CoroutineHandle CreateHandle(MonoBehaviour owner)
        {
            ConstructorInfo constructor = typeof(CoroutineExtensions.CoroutineHandle).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(MonoBehaviour) },
                null);

            Assert.IsNotNull(constructor);
            return (CoroutineExtensions.CoroutineHandle)constructor.Invoke(new object[] { owner });
        }

        private static void BindTracker(CoroutineExtensions.CoroutineHandle handle, CoroutineLifecycleTracker tracker)
        {
            MethodInfo method = typeof(CoroutineExtensions.CoroutineHandle).GetMethod(
                "BindTracker",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(method);
            method.Invoke(handle, new object[] { tracker });
        }

        private static void SetCoroutine(CoroutineExtensions.CoroutineHandle handle, Coroutine coroutine)
        {
            PropertyInfo property = typeof(CoroutineExtensions.CoroutineHandle).GetProperty(
                "Coroutine",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.IsNotNull(property);
            property.SetValue(handle, coroutine);
        }

        private static void InvokeCompleteAll(CoroutineLifecycleTracker tracker)
        {
            MethodInfo method = typeof(CoroutineLifecycleTracker).GetMethod(
                "CompleteAll",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(method);
            method.Invoke(tracker, null);
        }

        private static void InvokeCoroutineReset()
        {
            MethodInfo method = typeof(CoroutineExtensions).GetMethod(
                "ResetStaticState",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(method);
            method.Invoke(null, null);
        }

        private static CoroutineHelper GetCoroutineHelperInstance()
        {
            FieldInfo field = typeof(CoroutineExtensions).GetField(
                "instance",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(field);
            return (CoroutineHelper)field.GetValue(null);
        }

        private static void SetCoroutineHelperInstance(CoroutineHelper helper)
        {
            FieldInfo field = typeof(CoroutineExtensions).GetField(
                "instance",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(field);
            field.SetValue(null, helper);
        }
    }
}
