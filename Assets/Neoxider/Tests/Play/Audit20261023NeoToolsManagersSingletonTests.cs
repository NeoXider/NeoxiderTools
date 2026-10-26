using System.Collections;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoToolsManagersSingletonTests
    {
        private readonly List<GameObject> _objects = new();
        private bool _originalCreateInstance;

        [SetUp]
        public void SetUp()
        {
            _originalCreateInstance = Audit20261023NeoToolsManagersSingletonProbe.CreateInstance;
            Audit20261023NeoToolsManagersSingletonProbe.CreateInstance = false;
            // A normal Awake resets the public resolver's cached miss between cases.
            GameObject seed = new("Singleton audit seed");
            seed.AddComponent<Audit20261023NeoToolsManagersSingletonProbe>();
            Object.DestroyImmediate(seed);
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
            Audit20261023NeoToolsManagersSingletonProbe.CreateInstance = _originalCreateInstance;
        }

        [UnityTest]
        public IEnumerator ResolvingBeforeAwake_AppliesPersistence_AndInitializesOnlyOnce()
        {
            GameObject owner = CreateObject(false);
            Audit20261023NeoToolsManagersSingletonProbe manager =
                owner.AddComponent<Audit20261023NeoToolsManagersSingletonProbe>();

            Assert.That(Audit20261023NeoToolsManagersSingletonProbe.I, Is.SameAs(manager));
            Assert.That(manager.InitializationCount, Is.EqualTo(1));
            Assert.That(owner.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            owner.SetActive(true);
            yield return null;

            Assert.That(Audit20261023NeoToolsManagersSingletonProbe.I, Is.SameAs(manager));
            Assert.That(manager.InitializationCount, Is.EqualTo(1));
            Assert.That(owner.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        }

        [UnityTest]
        public IEnumerator DestroyInstance_CannotResolveTheDoomedObjectBeforeTheEndOfTheFrame()
        {
            GameObject original = CreateObject(true);
            original.AddComponent<Audit20261023NeoToolsManagersSingletonProbe>();

            Audit20261023NeoToolsManagersSingletonProbe.DestroyInstance();

            Assert.That(Audit20261023NeoToolsManagersSingletonProbe.HasInstance, Is.False);
            Assert.That(Audit20261023NeoToolsManagersSingletonProbe.I == null, Is.True);
            GameObject replacementObject = CreateObject(true);
            Audit20261023NeoToolsManagersSingletonProbe replacement =
                replacementObject.AddComponent<Audit20261023NeoToolsManagersSingletonProbe>();
            yield return null;

            Assert.That(original == null, Is.True);
            Assert.That(Audit20261023NeoToolsManagersSingletonProbe.I, Is.SameAs(replacement));
        }

        [UnityTest]
        public IEnumerator DestroyInstance_WithCreationEnabled_CreatesANewInitializedInstanceImmediately()
        {
            GameObject original = CreateObject(true);
            Audit20261023NeoToolsManagersSingletonProbe first =
                original.AddComponent<Audit20261023NeoToolsManagersSingletonProbe>();
            Audit20261023NeoToolsManagersSingletonProbe.CreateInstance = true;

            Audit20261023NeoToolsManagersSingletonProbe.DestroyInstance();
            Audit20261023NeoToolsManagersSingletonProbe replacement =
                Audit20261023NeoToolsManagersSingletonProbe.I;
            _objects.Add(replacement.gameObject);

            Assert.That(replacement, Is.Not.SameAs(first));
            Assert.That(replacement.InitializationCount, Is.EqualTo(1));
            yield return null;

            Assert.That(original == null, Is.True);
            Assert.That(Audit20261023NeoToolsManagersSingletonProbe.I, Is.SameAs(replacement));
        }

        private GameObject CreateObject(bool active)
        {
            GameObject owner = new("Singleton audit target");
            owner.SetActive(active);
            _objects.Add(owner);
            return owner;
        }
    }

    public sealed class Audit20261023NeoToolsManagersSingletonProbe : Singleton<Audit20261023NeoToolsManagersSingletonProbe>
    {
        public int InitializationCount { get; private set; }
        protected override bool DontDestroyOnLoadEnabled => true;

        protected override void Init()
        {
            base.Init();
            InitializationCount++;
        }
    }
}
