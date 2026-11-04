using System.Collections.Generic;
using System.Runtime.Serialization;
using Neo.Condition;
using Neo.NoCode;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     Covers the unassigned-source fallback shared by NeoCondition entries and NoCode bindings: an empty source
    ///     falls back to the context also when the Unity Editor deserialized the field as an empty placeholder (it
    ///     compares equal to null but is not a C# null), while a destroyed explicit source stays unavailable.
    /// </summary>
    /// <remarks>
    ///     WHY: the destroyed-source rule used to treat that placeholder as destroyed, so an empty Source Object in
    ///     a saved asset (StateMachineData context predicates) never read the context in Editor Play Mode.
    /// </remarks>
    [TestFixture]
    public sealed class BindingSourceFallbackTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void IsDestroyedReference_SeparatesUnassignedPlaceholdersFromDestroyedObjects()
        {
            GameObject placeholder = CreateUnassignedPlaceholder();
            Assert.That(placeholder == null, Is.True, "Positive control: the placeholder compares equal to null.");
            Assert.That(ReferenceEquals(placeholder, null), Is.False, "Positive control: it is not a C# null.");
            Assert.That(BindingSourceGameObjectResolver.IsDestroyedReference(placeholder), Is.False);
            Assert.That(BindingSourceGameObjectResolver.IsDestroyedReference(null), Is.False);
            Assert.That(BindingSourceGameObjectResolver.IsDestroyedReference(NewObject("alive")), Is.False);

            GameObject destroyed = new("BindingSourceFallback destroyed");
            Object.DestroyImmediate(destroyed);
            Assert.That(ReferenceEquals(destroyed, null), Is.False);
            Assert.That(BindingSourceGameObjectResolver.IsDestroyedReference(destroyed), Is.True);
        }

        [Test]
        public void Resolve_UnassignedPlaceholderFallsBackAndDestroyedSourceDoesNot()
        {
            GameObject host = NewObject("host");
            BindingSourceGameObjectResolver.ResolveCache cache = default;
            Assert.That(BindingSourceGameObjectResolver.Resolve(false, null, false, CreateUnassignedPlaceholder(), host,
                ref cache, "[Test]"), Is.SameAs(host));
            Assert.That(BindingSourceGameObjectResolver.Resolve(false, null, false, null, host, ref cache, "[Test]"),
                Is.SameAs(host));

            GameObject destroyed = new("BindingSourceFallback destroyed source");
            Object.DestroyImmediate(destroyed);
            Assert.That(BindingSourceGameObjectResolver.Resolve(false, null, false, destroyed, host, ref cache,
                "[Test]"), Is.Null, "A destroyed explicit source must not silently read the host.");
        }

        [Test]
        public void ConditionEntry_UnassignedPlaceholderSource_ReadsTheContext()
        {
            GameObject host = NewObject("entry host");
            Audit20261023NeoConditionValueProbe probe = host.AddComponent<Audit20261023NeoConditionValueProbe>();
            probe.Value = 4;
            ConditionEntry entry = NewEntry(4);
            entry.SourceObject = CreateUnassignedPlaceholder();

            Assert.That(entry.TryEvaluate(host, out bool result), Is.True, "The empty source falls back to the context.");
            Assert.That(result, Is.True);
            probe.Value = 5;
            Assert.That(entry.Evaluate(host), Is.False);
        }

        [Test]
        public void ConditionEntry_DestroyedExplicitSource_StaysUnavailable()
        {
            GameObject host = NewObject("entry host");
            host.AddComponent<Audit20261023NeoConditionValueProbe>().Value = 4;
            GameObject source = NewObject("destroyed source");
            source.AddComponent<Audit20261023NeoConditionValueProbe>().Value = 4;
            ConditionEntry entry = NewEntry(4);
            entry.SourceObject = source;
            Assert.That(entry.Evaluate(host), Is.True, "Positive control: the live explicit source matches.");

            Object.DestroyImmediate(source);

            Assert.That(entry.TryEvaluate(host, out bool result), Is.False,
                "A destroyed explicit source must not fall back to the host's equal value.");
            Assert.That(result, Is.False);
        }

        [Test]
        public void ConditionEntry_UnassignedPlaceholderOtherSource_BindsToTheLeftSource()
        {
            GameObject left = NewObject("left");
            Audit20261023NeoConditionValueProbe probe = left.AddComponent<Audit20261023NeoConditionValueProbe>();
            probe.Value = 7;
            probe.Limit = 3;
            GameObject owner = NewObject("owner");
            ConditionEntry entry = NewEntry(0);
            entry.Compare = CompareOp.Greater;
            entry.SourceObject = left;
            entry.ThresholdSource = ThresholdSource.OtherObject;
            entry.OtherComponentTypeName = typeof(Audit20261023NeoConditionValueProbe).FullName;
            entry.OtherPropertyName = nameof(Audit20261023NeoConditionValueProbe.Limit);
            entry.OtherSourceObject = CreateUnassignedPlaceholder();

            entry.BindOtherToSourceIfNull(owner);

            Assert.That(entry.OtherSourceObject, Is.SameAs(left),
                "An unassigned Other Source Object binds to the left-hand source like a C# null.");
            Assert.That(entry.Evaluate(owner), Is.True, "7 > 3 on the same left-hand object.");
        }

        [Test]
        public void ConditionEntry_DestroyedOtherSource_IsNotRebound()
        {
            GameObject left = NewObject("left");
            left.AddComponent<Audit20261023NeoConditionValueProbe>();
            GameObject other = new("BindingSourceFallback destroyed other");
            Object.DestroyImmediate(other);
            ConditionEntry entry = NewEntry(0);
            entry.SourceObject = left;
            entry.ThresholdSource = ThresholdSource.OtherObject;
            entry.OtherSourceObject = other;

            entry.BindOtherToSourceIfNull(left);

            Assert.That(entry.OtherSourceObject, Is.SameAs(other), "A destroyed explicit choice is kept.");
        }

        [Test]
        public void ComponentFloatBinding_UnassignedPlaceholderRoot_ReadsTheHost()
        {
            NoCodeBindEditModeFloatSource host = NewObject("binding host").AddComponent<NoCodeBindEditModeFloatSource>();
            host.Score = 12f;
            ComponentFloatBinding binding = new()
            {
                ComponentTypeName = typeof(NoCodeBindEditModeFloatSource).FullName,
                MemberName = nameof(NoCodeBindEditModeFloatSource.Score)
            };
            binding.SourceRoot = CreateUnassignedPlaceholder();

            Assert.That(binding.TryReadFloat(host, out float value), Is.True,
                "An empty Source Root uses the binding's own GameObject.");
            Assert.That(value, Is.EqualTo(12f));
        }

        private static ConditionEntry NewEntry(int threshold)
        {
            return new ConditionEntry
            {
                ComponentTypeName = typeof(Audit20261023NeoConditionValueProbe).FullName,
                PropertyName = nameof(Audit20261023NeoConditionValueProbe.Value),
                CurrentValueType = Neo.Condition.ValueType.Int,
                Compare = CompareOp.Equal,
                ThresholdInt = threshold
            };
        }

        /// <summary>
        ///     A managed GameObject wrapper without a native object: what the Editor deserializes into an unassigned
        ///     serialized field. It compares equal to null, is not a C# null and has instance id 0.
        /// </summary>
        private static GameObject CreateUnassignedPlaceholder()
        {
            return (GameObject)FormatterServices.GetUninitializedObject(typeof(GameObject));
        }

        private GameObject NewObject(string name)
        {
            GameObject target = new("BindingSourceFallback " + name);
            _objects.Add(target);
            return target;
        }
    }
}
