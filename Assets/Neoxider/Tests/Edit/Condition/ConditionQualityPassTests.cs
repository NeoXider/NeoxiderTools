using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Condition;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     Covers the 2026-11 Condition quality pass: allocation-free component lookup and retries, the boxed-threshold
    ///     removal, cached method arguments, exception-free ambiguous lookups, <see cref="ConditionEntry.TryEvaluate"/>
    ///     and the public Other-object / method-argument API.
    /// </summary>
    [TestFixture]
    public sealed class ConditionQualityPassTests
    {
        private const int Iterations = 200;
        private readonly List<GameObject> _objects = new();
        private object[] _boxSink;

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
        public void MatchesComponentTypeName_AgreesWithLegacyFullOrShortNameRule()
        {
            Type[] types =
            {
                typeof(Transform), typeof(Audit20261023NeoConditionValueProbe), typeof(NestedProbeType),
                typeof(List<int>)
            };
            string[] names =
            {
                "Transform", "UnityEngine.Transform", "Engine.Transform", "ransform",
                "Audit20261023NeoConditionValueProbe", typeof(Audit20261023NeoConditionValueProbe).FullName,
                "Edit.Audit20261023NeoConditionValueProbe", "ValueProbe", "NestedProbeType",
                "ConditionQualityPassTests+NestedProbeType", typeof(NestedProbeType).FullName, "List`1",
                typeof(List<int>).FullName, "", "Tests"
            };

            for (int t = 0; t < types.Length; t++)
            {
                for (int n = 0; n < names.Length; n++)
                {
                    bool legacy = !string.IsNullOrEmpty(names[n]) &&
                                  (types[t].FullName == names[n] || types[t].Name == names[n]);
                    Assert.That(BindingSourceGameObjectResolver.MatchesComponentTypeName(types[t], names[n]),
                        Is.EqualTo(legacy), types[t].FullName + " vs '" + names[n] + "'");
                }
            }
        }

        [Test]
        public void FindComponentOccurrence_CountsMatchingTypesAndDoesNotAllocateOnceWarm()
        {
            GameObject root = NewObject("occurrence");
            root.AddComponent<BoxCollider>();
            Audit20261023NeoConditionValueProbe first = root.AddComponent<Audit20261023NeoConditionValueProbe>();
            root.AddComponent<SphereCollider>();
            Audit20261023NeoConditionValueProbe second = root.AddComponent<Audit20261023NeoConditionValueProbe>();
            string shortName = nameof(Audit20261023NeoConditionValueProbe);

            Assert.That(BindingSourceGameObjectResolver.FindComponentOccurrence(root, shortName, 0), Is.SameAs(first));
            Assert.That(BindingSourceGameObjectResolver.FindComponentOccurrence(root,
                typeof(Audit20261023NeoConditionValueProbe).FullName, 1), Is.SameAs(second));
            Assert.That(BindingSourceGameObjectResolver.FindComponentOccurrence(root, shortName, 2), Is.Null);
            Assert.That(BindingSourceGameObjectResolver.FindComponentOccurrence(root, shortName, -1), Is.Null);
            Assert.That(BindingSourceGameObjectResolver.FindComponentOccurrence(null, shortName, 0), Is.Null);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                BindingSourceGameObjectResolver.FindComponentOccurrence(root, shortName, 1);
                BindingSourceGameObjectResolver.FindComponentOccurrence(root, "MissingComponentType", 0);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.Zero, "Component lookups are retried on every check and must not allocate.");
        }

        [Test]
        public void MissingComponent_WarmRetries_DoNotAllocate()
        {
            GameObject root = NewObject("missing component");
            ConditionEntry entry = new()
            {
                ComponentTypeName = typeof(Audit20261023NeoConditionValueProbe).FullName,
                PropertyName = nameof(Audit20261023NeoConditionValueProbe.Value),
                CurrentValueType = Neo.Condition.ValueType.Int,
                ThresholdInt = 3
            };
            Assert.That(entry.Evaluate(root), Is.False);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                entry.Evaluate(root);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.Zero, "A missing component is retried every check without garbage.");

            Audit20261023NeoConditionValueProbe probe = root.AddComponent<Audit20261023NeoConditionValueProbe>();
            probe.Value = 3;
            Assert.That(entry.Evaluate(root), Is.True, "The late component is picked up by the next check.");
        }

        [Test]
        public void ConstantThreshold_WarmEvaluate_BoxesOnlyTheReadValue()
        {
            GameObject root = NewObject("constant");
            Audit20261023NeoConditionValueProbe probe = root.AddComponent<Audit20261023NeoConditionValueProbe>();
            probe.Value = 3;
            ConditionEntry entry = NewValueEntry(CompareOp.Equal, 3);
            Assert.That(entry.Evaluate(root), Is.True);

            long boxCalls = MeasureIntBoxCalls();
            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                entry.Evaluate(root);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.LessThan(2 * boxCalls),
                "A constant threshold must not be boxed next to the reflected value on every evaluation.");
        }

        [Test]
        public void MethodArgument_IsCachedButStillReadEveryEvaluation()
        {
            GameObject root = NewObject("method");
            Audit20261023NeoConditionValueProbe probe = root.AddComponent<Audit20261023NeoConditionValueProbe>();
            ConditionEntry entry = new()
            {
                ComponentTypeName = typeof(Audit20261023NeoConditionValueProbe).FullName,
                PropertyName = nameof(Audit20261023NeoConditionValueProbe.ReadShort),
                IsMethodWithArgument = true,
                PropertyArgumentKind = ArgumentKind.Int,
                PropertyArgumentInt = 12,
                CurrentValueType = Neo.Condition.ValueType.Int,
                Compare = CompareOp.Equal,
                ThresholdInt = 13
            };

            Assert.That(entry.Evaluate(root), Is.True);
            Assert.That(probe.LastArgument, Is.EqualTo(12));

            entry.PropertyArgumentInt = 20;
            Assert.That(entry.Evaluate(root), Is.False, "ReadShort(20) returns 21.");
            Assert.That(probe.LastArgument, Is.EqualTo(20), "A changed argument applies to the next evaluation.");

            entry.PropertyArgumentInt = 40000;
            int callsBefore = probe.Calls;
            Assert.That(entry.Evaluate(root), Is.False);
            Assert.That(probe.Calls, Is.EqualTo(callsBefore), "An out-of-range short argument never invokes the method.");

            entry.PropertyArgumentInt = 12;
            Assert.That(entry.Evaluate(root), Is.True, "Recovery after a failed conversion.");

            long boxCalls = MeasureIntBoxCalls();
            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                entry.Evaluate(root);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.LessThan(2 * boxCalls),
                "An unchanged argument must not be re-boxed and re-converted on every evaluation.");
        }

        [Test]
        public void TryEvaluate_SeparatesUnavailableSourcesFromFailedComparisons()
        {
            GameObject root = NewObject("try evaluate");
            ConditionEntry entry = NewValueEntry(CompareOp.Equal, 3);
            entry.Invert = true;

            Assert.That(entry.Evaluate(root), Is.True, "Legacy: unavailable is false before inversion.");
            Assert.That(entry.TryEvaluate(root, out bool unavailable), Is.False);
            Assert.That(unavailable, Is.False);

            Audit20261023NeoConditionValueProbe probe = root.AddComponent<Audit20261023NeoConditionValueProbe>();
            probe.Value = 3;
            Assert.That(entry.TryEvaluate(root, out bool matched), Is.True);
            Assert.That(matched, Is.False, "Equal passes, Invert flips it.");
            probe.Value = 4;
            Assert.That(entry.TryEvaluate(root, out bool mismatched), Is.True);
            Assert.That(mismatched, Is.True);
            Assert.That(entry.Evaluate(root), Is.EqualTo(mismatched), "Both APIs agree when a comparison happened.");
        }

        [Test]
        public void OtherObjectBranch_IsConfigurableThroughPublicApi()
        {
            GameObject left = NewObject("left");
            Audit20261023NeoConditionValueProbe leftProbe = left.AddComponent<Audit20261023NeoConditionValueProbe>();
            leftProbe.Value = 5;
            GameObject right = NewObject("right");
            Audit20261023NeoConditionValueProbe rightProbe = right.AddComponent<Audit20261023NeoConditionValueProbe>();
            rightProbe.Value = 3;
            rightProbe.Limit = 10;
            ConditionEntry entry = NewValueEntry(CompareOp.Greater, 0);
            entry.ThresholdSource = ThresholdSource.OtherObject;
            entry.OtherSourceMode = SourceMode.Component;
            entry.OtherSourceObject = right;
            entry.OtherComponentTypeName = typeof(Audit20261023NeoConditionValueProbe).FullName;
            entry.OtherPropertyName = nameof(Audit20261023NeoConditionValueProbe.Value);

            Assert.That(entry.Evaluate(left), Is.True, "5 > 3");
            entry.OtherPropertyName = nameof(Audit20261023NeoConditionValueProbe.Limit);
            Assert.That(entry.Evaluate(left), Is.False, "The setter rebinds the warm right-hand cache: 5 > 10 fails.");

            entry.OtherIsMethodWithArgument = true;
            entry.OtherPropertyArgumentKind = ArgumentKind.Int;
            entry.OtherPropertyArgumentInt = 1;
            entry.OtherPropertyName = nameof(Audit20261023NeoConditionValueProbe.ReadShort);
            Assert.That(entry.Evaluate(left), Is.True, "5 > ReadShort(1) == 2");
            Assert.That(rightProbe.LastArgument, Is.EqualTo(1));
        }

        [Test]
        public void AmbiguousIndexer_TryGetPropertyIsCachedAndExceptionFree()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type type = typeof(Audit20261023NeoConditionAmbiguousIndexerProbe);

            Assert.That(ReflectionCache.TryGetProperty(type, "Item", flags, out PropertyInfo ambiguous), Is.False);
            Assert.That(ambiguous, Is.Null);
            Assert.That(ReflectionCache.TryGetProperty(type, "Item", flags, out PropertyInfo again), Is.False);
            Assert.That(again, Is.Null);
            Assert.Throws<AmbiguousMatchException>(() => ReflectionCache.GetProperty(type, "Item", flags),
                "The legacy lookup keeps its documented exception.");
            Assert.That(ReflectionCache.TryGetProperty(type, "Missing", flags, out PropertyInfo missing), Is.True);
            Assert.That(missing, Is.Null);

            GameObject root = NewObject("ambiguous");
            Audit20261023NeoConditionAmbiguousIndexerProbe probe =
                root.AddComponent<Audit20261023NeoConditionAmbiguousIndexerProbe>();
            ConditionEntry entry = new()
            {
                ComponentTypeName = type.FullName,
                PropertyName = "Item",
                CurrentValueType = Neo.Condition.ValueType.Int
            };
            for (int i = 0; i < 3; i++)
            {
                Assert.That(entry.Evaluate(root), Is.False);
            }

            Assert.That(probe.IndexerReads, Is.Zero);
        }

        private ConditionEntry NewValueEntry(CompareOp compare, int threshold)
        {
            return new ConditionEntry
            {
                ComponentTypeName = typeof(Audit20261023NeoConditionValueProbe).FullName,
                PropertyName = nameof(Audit20261023NeoConditionValueProbe.Value),
                CurrentValueType = Neo.Condition.ValueType.Int,
                Compare = compare,
                ThresholdInt = threshold
            };
        }

        /// <summary>
        ///     Positive control: <see cref="Iterations"/> int boxes must read as exactly one GC.Alloc call each, so the
        ///     "fewer than two boxes per evaluation" bounds (only the read value is boxed) are meaningful.
        /// </summary>
        /// <remarks>
        ///     WHY: these bounds read 0 against 0 (and failed as "less than 0") while they were measured with the managed
        ///     byte counter, which is always 0 in the Unity 6 Editor.
        /// </remarks>
        private long MeasureIntBoxCalls()
        {
            _boxSink ??= new object[1];
            _boxSink[0] = -1;
            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                _boxSink[0] = i;
            }

            long boxCalls = allocationCounter.End();
            Assert.That(boxCalls, Is.EqualTo(Iterations), "Positive control: the counter must see one call per box.");
            return boxCalls;
        }

        private GameObject NewObject(string name)
        {
            GameObject target = new("ConditionQualityPass " + name);
            _objects.Add(target);
            return target;
        }

        private sealed class NestedProbeType
        {
        }
    }
}
