using System;
using System.Collections.Generic;
using Neo.Condition;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    [TestFixture]
    public sealed class Audit20261023NeoConditionBindingTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject target in _objects)
            {
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                }
            }

            _objects.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LegacyJson_DefaultsToFirstOccurrence_WithoutReinterpretingFlatIndices(bool otherBranch)
        {
            GameObject target = NewRepeatedSource(out Audit20261023NeoConditionValueProbe first,
                out Audit20261023NeoConditionValueProbe second);
            string typeName = typeof(Audit20261023NeoConditionValueProbe).FullName;
            ConditionEntry entry = JsonUtility.FromJson<ConditionEntry>(
                "{\"_componentTypeName\":\"" + typeName + "\",\"_componentIndex\":3," +
                "\"_propertyName\":\"Value\",\"_thresholdInt\":3,\"_thresholdSource\":" +
                (otherBranch ? "1" : "0") + ",\"_otherComponentIndex\":3,\"_otherComponentTypeName\":\"" +
                typeName + "\",\"_otherPropertyName\":\"Value\"}");
            if (otherBranch)
            {
                Audit20261023NeoConditionValueProbe left = NewObject("legacy left")
                    .AddComponent<Audit20261023NeoConditionValueProbe>();
                left.Value = 3;
                entry.SourceObject = left.gameObject;
            }

            Assert.That(entry.ComponentOccurrence, Is.Zero);
            Assert.That(entry.OtherComponentOccurrence, Is.Zero);
            Assert.That(entry.ComponentIndex, Is.EqualTo(3));
            Assert.That(first.Value, Is.Not.EqualTo(second.Value));
            Assert.That(entry.Evaluate(target), Is.True);
            Assert.That(JsonUtility.ToJson(entry), Does.Contain("\"_otherComponentIndex\":3"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Occurrence_CountsOnlyMatchingTypes_AndPublicSetterRebinds(bool otherBranch)
        {
            GameObject target = NewRepeatedSource(out Audit20261023NeoConditionValueProbe first,
                out Audit20261023NeoConditionValueProbe second);
            ConditionEntry entry = NewEntry(typeof(Audit20261023NeoConditionValueProbe), otherBranch, 3);

            Assert.That(entry.Evaluate(target), Is.True);
            SetOccurrence(entry, otherBranch, 1);
            Assert.That(entry.Evaluate(target), Is.False, "The second matching component has Value=8.");
            SetOccurrence(entry, otherBranch, 0);
            Assert.That(entry.Evaluate(target), Is.True);
            Assert.That(first.Value, Is.EqualTo(3));
            Assert.That(second.Value, Is.EqualTo(8));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SerializedOccurrenceChange_RebindsWarmCache_WithoutFirstMatchFallback(bool otherBranch)
        {
            GameObject target = NewRepeatedSource(out Audit20261023NeoConditionValueProbe first,
                out Audit20261023NeoConditionValueProbe second);
            ConditionEntry entry = NewEntry(typeof(Audit20261023NeoConditionValueProbe), otherBranch, 3);
            Assert.That(entry.Evaluate(target), Is.True);

            string fieldName = otherBranch ? "_otherComponentOccurrence" : "_componentOccurrence";
            JsonUtility.FromJsonOverwrite("{\"" + fieldName + "\":1}", entry);
            Assert.That(entry.Evaluate(target), Is.False);
            JsonUtility.FromJsonOverwrite("{\"" + fieldName + "\":-1}", entry);
            Assert.That(entry.Evaluate(target), Is.False);
            JsonUtility.FromJsonOverwrite("{\"" + fieldName + "\":0}", entry);
            Assert.That(entry.Evaluate(target), Is.True);
        }

        [TestCase(false, -1)]
        [TestCase(true, -1)]
        [TestCase(false, int.MinValue)]
        [TestCase(true, int.MinValue)]
        [TestCase(false, 2)]
        [TestCase(true, 2)]
        [TestCase(false, int.MaxValue)]
        [TestCase(true, int.MaxValue)]
        public void InvalidOccurrence_IsUnavailable_AndValidSelectionCanRecover(bool otherBranch, int occurrence)
        {
            GameObject target = NewRepeatedSource(out Audit20261023NeoConditionValueProbe first,
                out Audit20261023NeoConditionValueProbe second);
            ConditionEntry entry = NewEntry(typeof(Audit20261023NeoConditionValueProbe), otherBranch, 3);
            Assert.That(entry.Evaluate(target), Is.True);

            SetOccurrence(entry, otherBranch, occurrence);
            Assert.That(entry.Evaluate(target), Is.False, "Invalid selections must not read the first match.");
            SetOccurrence(entry, otherBranch, 0);
            Assert.That(entry.Evaluate(target), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DestroyedSelectedOccurrence_DoesNotFallBackToRemainingFirstMatch(bool otherBranch)
        {
            GameObject target = NewRepeatedSource(out Audit20261023NeoConditionValueProbe first,
                out Audit20261023NeoConditionValueProbe second);
            ConditionEntry entry = NewEntry(typeof(Audit20261023NeoConditionValueProbe), otherBranch, 8);
            SetOccurrence(entry, otherBranch, 1);
            Assert.That(entry.Evaluate(target), Is.True);

            first.Value = 8;
            Object.DestroyImmediate(second);

            Assert.That(entry.Evaluate(target), Is.False,
                "The remaining first component would pass, but occurrence 1 is now missing.");
            SetOccurrence(entry, otherBranch, 0);
            Assert.That(entry.Evaluate(target), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Invalidation_RecountsOccurrences_AfterEarlierMatchingComponentIsRemoved(bool otherBranch)
        {
            GameObject target = NewRepeatedSource(out Audit20261023NeoConditionValueProbe first,
                out Audit20261023NeoConditionValueProbe second);
            ConditionEntry entry = NewEntry(typeof(Audit20261023NeoConditionValueProbe), otherBranch, 8);
            SetOccurrence(entry, otherBranch, 1);
            Assert.That(entry.Evaluate(target), Is.True);

            Object.DestroyImmediate(first);
            entry.InvalidateCacheFull();

            Assert.That(entry.Evaluate(target), Is.False);
            SetOccurrence(entry, otherBranch, 0);
            Assert.That(entry.Evaluate(target), Is.True);
            Assert.That(second.Value, Is.EqualTo(8));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SingleIndexer_IsUnavailable_WithoutCallingGetter_AndOrdinaryFieldRecovers(bool otherBranch)
        {
            Audit20261023NeoConditionValueProbe probe = NewObject("single indexer")
                .AddComponent<Audit20261023NeoConditionValueProbe>();
            probe.Value = 3;
            ConditionEntry entry = NewEntry(typeof(Audit20261023NeoConditionValueProbe), otherBranch, 3);
            SetMember(entry, otherBranch, "Item");

            Assert.That(entry.Evaluate(probe.gameObject), Is.False);
            Assert.That(entry.Evaluate(probe.gameObject), Is.False);
            Assert.That(probe.IndexerReads, Is.Zero);
            SetMember(entry, otherBranch, "Value");
            Assert.That(entry.Evaluate(probe.gameObject), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AmbiguousIndexers_AreUnavailable_WithoutPoisoningSubsequentBinding(bool otherBranch)
        {
            Audit20261023NeoConditionAmbiguousIndexerProbe probe = NewObject("ambiguous indexers")
                .AddComponent<Audit20261023NeoConditionAmbiguousIndexerProbe>();
            probe.Value = 3;
            ConditionEntry entry = NewEntry(typeof(Audit20261023NeoConditionAmbiguousIndexerProbe), otherBranch, 3);
            SetMember(entry, otherBranch, "Item");

            Assert.That(entry.Evaluate(probe.gameObject), Is.False);
            Assert.That(entry.Evaluate(probe.gameObject), Is.False);
            Assert.That(probe.IndexerReads, Is.Zero);
            SetMember(entry, otherBranch, "Value");
            Assert.That(entry.Evaluate(probe.gameObject), Is.True);
        }

        private GameObject NewObject(string name)
        {
            GameObject target = new("Audit20261023NeoCondition " + name);
            target.SetActive(false);
            _objects.Add(target);
            return target;
        }

        private GameObject NewRepeatedSource(out Audit20261023NeoConditionValueProbe first,
            out Audit20261023NeoConditionValueProbe second)
        {
            GameObject target = NewObject("repeated components");
            first = target.AddComponent<Audit20261023NeoConditionValueProbe>();
            first.Value = 3;
            target.AddComponent<BoxCollider>();
            second = target.AddComponent<Audit20261023NeoConditionValueProbe>();
            second.Value = 8;
            return target;
        }

        private ConditionEntry NewEntry(Type componentType, bool otherBranch, int threshold)
        {
            ConditionEntry entry = new()
            {
                ComponentTypeName = componentType.FullName,
                PropertyName = "Value",
                ThresholdInt = threshold
            };
            if (otherBranch)
            {
                Audit20261023NeoConditionValueProbe left = NewObject("constant left")
                    .AddComponent<Audit20261023NeoConditionValueProbe>();
                left.Value = threshold;
                entry.SourceObject = left.gameObject;
                entry.ComponentTypeName = typeof(Audit20261023NeoConditionValueProbe).FullName;
                JsonUtility.FromJsonOverwrite("{\"_thresholdSource\":1,\"_otherComponentTypeName\":\"" +
                    componentType.FullName + "\",\"_otherPropertyName\":\"Value\"}", entry);
            }

            return entry;
        }

        private static void SetOccurrence(ConditionEntry entry, bool otherBranch, int occurrence)
        {
            if (otherBranch)
            {
                entry.OtherComponentOccurrence = occurrence;
            }
            else
            {
                entry.ComponentOccurrence = occurrence;
            }
        }

        private static void SetMember(ConditionEntry entry, bool otherBranch, string memberName)
        {
            if (otherBranch)
            {
                JsonUtility.FromJsonOverwrite("{\"_otherPropertyName\":\"" + memberName + "\"}", entry);
                entry.InvalidateCacheFull();
            }
            else
            {
                entry.PropertyName = memberName;
            }
        }
    }
}
