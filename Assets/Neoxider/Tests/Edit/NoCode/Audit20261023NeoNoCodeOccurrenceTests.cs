using System.Collections.Generic;
using Neo.NoCode;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023NeoNoCodeOccurrenceTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [TestCase(0, 10f)]
        [TestCase(1, 20f)]
        [TestCase(2, 30f)]
        public void Occurrence_CountsOnlyMatchingTypes_InComponentOrder(int occurrence, float expected)
        {
            Audit20261023NeoNoCodeSource first = CreateRepeatedSources();
            ComponentFloatBinding binding = CreateBinding(first.gameObject);
            binding.ComponentOccurrence = occurrence;

            bool read = binding.TryReadFloat(first, out float value);

            Assert.IsTrue(read);
            Assert.AreEqual(expected, value);
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(int.MaxValue)]
        public void Occurrence_InvalidOrMissing_RejectsWithoutFirstComponentFallback(int occurrence)
        {
            Audit20261023NeoNoCodeSource first = CreateRepeatedSources();
            ComponentFloatBinding binding = CreateBinding(first.gameObject);
            Assert.IsTrue(binding.TryReadFloat(first, out float initial));
            Assert.AreEqual(10f, initial);
            binding.ComponentOccurrence = occurrence;

            Assert.AreEqual(occurrence, binding.ComponentOccurrence, "Invalid values must not be clamped to occurrence zero.");
            Assert.IsFalse(binding.EnsureCache(first, out string error));
            Assert.IsNotEmpty(error);
            Assert.IsFalse(binding.TryReadFloat(first, out float value));
            Assert.AreEqual(0f, value);
        }

        [Test]
        public void Occurrence_MissingInstance_ResolvesWhenRequestedInstanceIsAdded()
        {
            GameObject root = CreateObject(false);
            Audit20261023NeoNoCodeSource first = root.AddComponent<Audit20261023NeoNoCodeSource>();
            first.Score = 10f;
            ComponentFloatBinding binding = CreateBinding(root);
            binding.ComponentOccurrence = 1;
            Assert.IsFalse(binding.TryReadFloat(first, out float missing));
            Assert.AreEqual(0f, missing);

            root.AddComponent<BoxCollider>();
            Audit20261023NeoNoCodeSource second = root.AddComponent<Audit20261023NeoNoCodeSource>();
            second.Score = 42f;

            Assert.IsTrue(binding.TryReadFloat(first, out float value));
            Assert.AreEqual(42f, value);
        }

        [Test]
        public void Occurrence_DestroyedSelectedInstance_DoesNotFallBackToFirst()
        {
            Audit20261023NeoNoCodeSource first = CreateRepeatedSources();
            ComponentFloatBinding binding = CreateBinding(first.gameObject);
            binding.ComponentOccurrence = 2;
            Assert.IsTrue(binding.TryReadFloat(first, out float initial));
            Assert.AreEqual(30f, initial);
            Audit20261023NeoNoCodeSource[] sources = first.GetComponents<Audit20261023NeoNoCodeSource>();

            Object.DestroyImmediate(sources[2]);

            Assert.IsFalse(binding.TryReadFloat(first, out float value));
            Assert.AreEqual(0f, value);
        }

        [Test]
        public void Occurrence_LegacySerializedDescriptor_DefaultsToFirstMatchingComponent()
        {
            Audit20261023NeoNoCodeSource first = CreateRepeatedSources();
            string legacyJson = "{\"_componentTypeName\":\"" + typeof(Audit20261023NeoNoCodeSource).FullName +
                                "\",\"_memberName\":\"Score\"}";
            ComponentFloatBinding binding = JsonUtility.FromJson<ComponentFloatBinding>(legacyJson);
            binding.SourceRoot = first.gameObject;

            Assert.AreEqual(0, binding.ComponentOccurrence);
            Assert.IsTrue(binding.TryReadFloat(first, out float value));
            Assert.AreEqual(10f, value);
        }

        [Test]
        public void Occurrence_SerializedDescriptorRoundtrip_PreservesSelectedInstance()
        {
            Audit20261023NeoNoCodeSource first = CreateRepeatedSources();
            ComponentFloatBinding binding = CreateBinding(null);
            binding.ComponentOccurrence = 1;
            string json = JsonUtility.ToJson(binding);

            ComponentFloatBinding restored = JsonUtility.FromJson<ComponentFloatBinding>(json);
            restored.SourceRoot = first.gameObject;

            Assert.AreEqual(1, restored.ComponentOccurrence);
            Assert.IsTrue(restored.TryReadFloat(first, out float value));
            Assert.AreEqual(20f, value);
        }

        [Test]
        public void Occurrence_ChangedOnLiveBinding_MovesReactiveSubscriptionToSelectedInstance()
        {
            Audit20261023NeoNoCodeSource first = CreateRepeatedSources();
            Audit20261023NeoNoCodeSource[] sources = first.GetComponents<Audit20261023NeoNoCodeSource>();
            sources[1].ReactiveScore.Value = 0.5f;
            GameObject root = CreateObject(true);
            root.SetActive(false);
            TMP_Text text = root.AddComponent<TextMeshProUGUI>();
            NoCodeBindText view = root.AddComponent<NoCodeBindText>();
            view.Binding.SourceRoot = first.gameObject;
            view.Binding.ComponentTypeName = typeof(Audit20261023NeoNoCodeSource).FullName;
            view.Binding.MemberName = nameof(Audit20261023NeoNoCodeSource.ReactiveScore);
            root.SetActive(true);
            view.EditorInvokeRefreshFromSource();
            Assert.AreEqual("0.25", text.text);
            Assert.AreEqual(1, first.ReactiveScore.ListenerCount);

            view.Binding.ComponentOccurrence = 1;
            view.EditorInvokeRefreshFromSource();

            Assert.AreEqual("0.5", text.text);
            Assert.AreEqual(0, first.ReactiveScore.ListenerCount);
            Assert.AreEqual(1, sources[1].ReactiveScore.ListenerCount);
            Assert.AreEqual(0, sources[2].ReactiveScore.ListenerCount);
            sources[1].ReactiveScore.Value = 0.75f;
            Assert.AreEqual("0.75", text.text);
        }

        private Audit20261023NeoNoCodeSource CreateRepeatedSources()
        {
            GameObject root = CreateObject(false);
            Audit20261023NeoNoCodeSource first = root.AddComponent<Audit20261023NeoNoCodeSource>();
            first.Score = 10f;
            root.AddComponent<BoxCollider>();
            root.AddComponent<Audit20261023NeoNoCodeSource>().Score = 20f;
            root.AddComponent<Audit20261023NeoNoCodeSource>().Score = 30f;
            return first;
        }

        private GameObject CreateObject(bool ui)
        {
            GameObject root = ui
                ? new GameObject("Audit20261023NeoNoCode occurrence UI", typeof(RectTransform))
                : new GameObject("Audit20261023NeoNoCode repeated sources");
            _objects.Add(root);
            return root;
        }

        private static ComponentFloatBinding CreateBinding(GameObject root)
        {
            return new ComponentFloatBinding
            {
                SourceRoot = root,
                ComponentTypeName = typeof(Audit20261023NeoNoCodeSource).FullName,
                MemberName = nameof(Audit20261023NeoNoCodeSource.Score)
            };
        }
    }
}
