using System;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tools.Tests
{
    public sealed class Audit20261023NeoToolsManagersBootstrapTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<string> _trace = new();
        private Audit20261023NeoToolsManagersBootstrapProbe _bootstrap;

        [SetUp]
        public void SetUp()
        {
            GameObject owner = CreateObject("Bootstrap audit");
            _bootstrap = owner.AddComponent<Audit20261023NeoToolsManagersBootstrapProbe>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject owner in _objects)
            {
                if (owner != null)
                {
                    Object.DestroyImmediate(owner);
                }
            }

            _objects.Clear();
            _trace.Clear();
        }

        [Test]
        public void Startup_DrainsRegistrationsMadeDuringInit_AfterTheCurrentPriorityBatch()
        {
            Initializable late = new("late", 100, _trace);
            Initializable high = new("high", 10, _trace) { OnInit = () => _bootstrap.Register(late) };
            Initializable low = new("low", 1, _trace);
            _bootstrap.Register(low);
            _bootstrap.Register(high);

            _bootstrap.InitializeForTest();

            CollectionAssert.AreEqual(new[] { "high", "low", "late" }, _trace);
            Assert.That(late.InitCount, Is.EqualTo(1));
        }

        [Test]
        public void RuntimeRegistration_FromInit_DoesNotReenterTheParent()
        {
            _bootstrap.InitializeForTest();
            Initializable child = new("child", 1, _trace);
            Initializable parent = new("parent", 10, _trace) { OnInit = () => _bootstrap.Register(child) };

            _bootstrap.Register(parent);

            CollectionAssert.AreEqual(new[] { "parent", "child" }, _trace);
            Assert.That(parent.InitCount, Is.EqualTo(1));
            Assert.That(child.InitCount, Is.EqualTo(1));
        }

        [Test]
        public void Unregister_DuringAnEarlierInit_CancelsThePendingTarget()
        {
            Initializable low = new("low", 1, _trace);
            Initializable high = new("high", 10, _trace) { OnInit = () => _bootstrap.Unregister(low) };
            _bootstrap.Register(low);
            _bootstrap.Register(high);

            _bootstrap.InitializeForTest();

            CollectionAssert.AreEqual(new[] { "high" }, _trace);
            Assert.That(low.InitCount, Is.Zero);
            _bootstrap.Register(low);
            Assert.That(low.InitCount, Is.EqualTo(1), "Explicit re-registration starts a new registration.");
        }

        [Test]
        public void DestroyedUnityTarget_IsSkippedWhenAnEarlierInitDestroysIt()
        {
            GameObject targetObject = CreateObject("Destroyed pending target");
            Audit20261023NeoToolsManagersInitProbe target =
                targetObject.AddComponent<Audit20261023NeoToolsManagersInitProbe>();
            target.OnInit = () => _trace.Add("destroyed");
            Initializable high = new("high", 10, _trace)
            {
                OnInit = () => Object.DestroyImmediate(targetObject)
            };
            _bootstrap.Register(target);
            _bootstrap.Register(high);

            _bootstrap.InitializeForTest();

            CollectionAssert.AreEqual(new[] { "high" }, _trace);
        }

        [Test]
        public void Register_RejectsADestroyedUnityTargetHeldThroughIInit()
        {
            GameObject targetObject = CreateObject("Already destroyed target");
            Audit20261023NeoToolsManagersInitProbe target =
                targetObject.AddComponent<Audit20261023NeoToolsManagersInitProbe>();
            target.OnInit = () => _trace.Add("destroyed");
            IInit destroyed = target;
            Object.DestroyImmediate(targetObject);

            _bootstrap.Register(destroyed);
            _bootstrap.Register(new Initializable("live", 0, _trace));
            _bootstrap.InitializeForTest();

            CollectionAssert.AreEqual(new[] { "live" }, _trace);
        }

        [Test]
        public void FailedInit_ReleasesTheDrainGuard_ForExplicitRetry()
        {
            _bootstrap.InitializeForTest();
            Initializable target = new("target", 1, _trace)
            {
                OnInit = () => throw new InvalidOperationException("Expected initialization failure")
            };
            Assert.Throws<InvalidOperationException>(() => _bootstrap.Register(target));

            _bootstrap.Unregister(target);
            target.OnInit = null;
            _bootstrap.Register(target);

            Assert.That(target.InitCount, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { "target", "target" }, _trace);
        }

        private GameObject CreateObject(string name)
        {
            GameObject owner = new(name);
            owner.SetActive(false);
            _objects.Add(owner);
            return owner;
        }

        private sealed class Initializable : IInit
        {
            private readonly string _name;
            private readonly List<string> _trace;

            public Initializable(string name, int priority, List<string> trace)
            {
                _name = name;
                InitPriority = priority;
                _trace = trace;
            }

            public int InitPriority { get; }
            public int InitCount { get; private set; }
            public Action OnInit { get; set; }

            public void Init()
            {
                InitCount++;
                _trace.Add(_name);
                OnInit?.Invoke();
            }
        }
    }




}
