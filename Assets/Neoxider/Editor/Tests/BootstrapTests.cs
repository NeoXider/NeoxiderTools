using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tools.Tests
{
    public class BootstrapTests
    {
        [Test]
        public void Init_ProcessesQueuedRegistrationsInPriorityOrder()
        {
            GameObject bootstrapObject = new("Bootstrap");
            GameObject lowObject = new("Low");
            GameObject highObject = new("High");
            Bootstrap bootstrap = bootstrapObject.AddComponent<Bootstrap>();
            List<int> initOrder = new();
            TestInitializable low = lowObject.AddComponent<TestInitializable>();
            TestInitializable high = highObject.AddComponent<TestInitializable>();
            low.Setup(1, initOrder);
            high.Setup(10, initOrder);

            try
            {
                bootstrap.Register(low);
                bootstrap.Register(high);
                InvokeInit(bootstrap);

                CollectionAssert.AreEqual(new[] { 10, 1 }, initOrder);
            }
            finally
            {
                Object.DestroyImmediate(bootstrapObject);
                Object.DestroyImmediate(lowObject);
                Object.DestroyImmediate(highObject);
            }
        }

        [Test]
        public void Register_AfterBootstrap_InitializesImmediately()
        {
            GameObject bootstrapObject = new("Bootstrap");
            GameObject registeredObject = new("RuntimeInit");
            Bootstrap bootstrap = bootstrapObject.AddComponent<Bootstrap>();
            List<int> initOrder = new();
            TestInitializable runtimeInitializable = registeredObject.AddComponent<TestInitializable>();
            runtimeInitializable.Setup(5, initOrder);

            try
            {
                InvokeInit(bootstrap);
                bootstrap.Register(runtimeInitializable);

                CollectionAssert.AreEqual(new[] { 5 }, initOrder);
            }
            finally
            {
                Object.DestroyImmediate(bootstrapObject);
                Object.DestroyImmediate(registeredObject);
            }
        }

        private static void InvokeInit(Bootstrap bootstrap)
        {
            MethodInfo initMethod =
                typeof(Bootstrap).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(initMethod, Is.Not.Null);
            initMethod.Invoke(bootstrap, null);
        }

        private sealed class TestInitializable : MonoBehaviour, IInit
        {
            private List<int> _initOrder;

            public int InitPriority { get; private set; }

            public void Setup(int priority, List<int> initOrder)
            {
                InitPriority = priority;
                _initOrder = initOrder;
            }

            public void Init()
            {
                _initOrder.Add(InitPriority);
            }
        }
    }
}
