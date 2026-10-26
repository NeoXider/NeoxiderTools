using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tests.Edit
{
    public sealed class Audit20261023NeoToolsViewSelectorInputTests
    {
        [Test]
        public void Exclusions_DeferredSelfProjection_RetainsFilteredValues()
        {
            SelectorModel model = new();
            model.Configure(4, 0, 0, true, false, false, false, true, new[] { 1, 2 }, null);

            model.SetExcludedIndices(model.ExcludedIndices.Where(index => index != 2));

            Assert.That(model.ExcludedIndices, Is.EquivalentTo(new[] { 1 }));
            model.SetRandom(true, (_, _) => 1);
            Assert.That(model.CurrentIndex, Is.EqualTo(2));
        }

        [Test]
        public void UniqueHistory_DeferredSelfProjection_RetainsConsumedValues()
        {
            SelectorModel model = new();
            model.Configure(4, 0, 0, true, false, false, true, false, new[] { 3 }, new[] { 0, 1, 2 });

            model.SetUsedIndicesForUnique(model.UsedIndicesForUnique.Where(index => index != 2));

            Assert.That(model.UsedIndicesForUnique, Is.EquivalentTo(new[] { 0, 1 }));
            model.SetRandom(true, (_, _) => 0);
            Assert.That(model.CurrentIndex, Is.EqualTo(2));
            Assert.That(model.UniqueRemainingCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Replacement_ThrowingEnumerable_PreservesPreviousSet(bool uniqueHistory)
        {
            SelectorModel model = new();
            model.Configure(4, 0, 0, true, false, false, true, false, new[] { 1, 2 }, new[] { 0, 2 });

            TestDelegate replace = uniqueHistory
                ? () => model.SetUsedIndicesForUnique(ThrowingIndices())
                : () => model.SetExcludedIndices(ThrowingIndices());
            Assert.Throws<InvalidOperationException>(replace);

            Assert.That(model.ExcludedIndices, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(model.UsedIndicesForUnique, Is.EquivalentTo(new[] { 0, 2 }));
            model.SetRandom(true, (_, _) => 0);
            Assert.That(model.CurrentIndex, Is.EqualTo(3));
        }

        [Test]
        public void Replacement_Null_ClearsOnlyRequestedSets()
        {
            SelectorModel model = new();
            model.Configure(3, 0, 0, true, false, false, true, false, new[] { 1 }, new[] { 0, 2 });

            model.SetExcludedIndices(null);
            Assert.That(model.ExcludedIndices, Is.Empty);
            Assert.That(model.UsedIndicesForUnique, Is.EquivalentTo(new[] { 0, 2 }));
            model.SetUsedIndicesForUnique(null);
            Assert.That(model.UniqueRemainingCount, Is.EqualTo(3));
        }

        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void Set_ExtremeIndexWithOffset_WrapsMathematically(int index)
        {
            SelectorModel model = new();
            model.Configure(3, 0, 1, true, false, false, false, true, null, null);

            model.Set(index);

            Assert.That(model.CurrentIndex, Is.EqualTo(1));
            Assert.That(model.IndexWithOffset, Is.EqualTo(2));
            Assert.That(model.GetLogicalActiveCount(), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MaximumBoundSingleton_AllEnumerationAndStepPathsTerminate(bool unique)
        {
            SelectorModel model = new();
            model.Configure(1, int.MaxValue, -int.MaxValue, true, false, false, unique, true, null, null);

            Assert.That(model.GetBounds(), Is.EqualTo((int.MaxValue, int.MaxValue)));
            Assert.That(model.GetIncludedIndices(), Is.EqualTo(new[] { int.MaxValue }));
            Assert.That(model.UniqueRemainingCount, Is.EqualTo(unique ? 1 : 0));
            model.SetRandom(false, (_, _) => 0);
            Assert.That(model.CurrentIndex, Is.EqualTo(int.MaxValue));
            if (unique)
            {
                SelectorModelResult repeated = model.SetRandom(false, (_, _) => 0);
                Assert.That(repeated.UniqueCycleComplete, Is.True);
                Assert.That(repeated.UniqueReset, Is.True);
            }

            Assert.That(model.Next().ReachedEnd, Is.True);
            Assert.That(model.CurrentIndex, Is.EqualTo(int.MaxValue));
            model.Previous();
            Assert.That(model.CurrentIndex, Is.EqualTo(int.MaxValue));
            model.ExcludeIndex(int.MaxValue);
            Assert.That(model.GetIncludedIndices(), Is.Empty);
            Assert.That(model.UniqueRemainingCount, Is.Zero);
            Assert.That(model.SetRandom(true, null).SelectionChanged, Is.False);
        }

        [Test]
        public void Previous_AtIntMinEmptyBound_WrapsToVisibleItem()
        {
            SelectorModel model = new();
            model.Configure(1, int.MinValue, int.MaxValue, true, false, true, false, true, null, null);
            Assert.That(model.GetLogicalActiveCount(), Is.Zero);

            model.Previous();

            Assert.That(model.CurrentIndex, Is.EqualTo(-int.MaxValue));
            Assert.That(model.IndexWithOffset, Is.Zero);
            Assert.That(model.GetLogicalActiveCount(), Is.EqualTo(1));
            model.Next();
            Assert.That(model.CurrentIndex, Is.EqualTo(int.MinValue));
            Assert.That(model.GetLogicalActiveCount(), Is.Zero);
        }

        [Test]
        public void EffectiveIndex_WidensRawSum_AndRawIntReadbackRejectsOverflow()
        {
            SelectorModel model = new();
            model.Configure(3, int.MaxValue, 1, true, true, false, false, true, null, null);

            Assert.That(model.GetEffectiveIndex(), Is.EqualTo(2));
            Assert.That(model.GetLogicalActiveCount(), Is.EqualTo(3));
            Assert.Throws<OverflowException>(() => Convert.ToInt64(model.IndexWithOffset));
        }

        [TestCase(1, int.MinValue)]
        [TestCase(2, -int.MaxValue)]
        public void Configure_UnrepresentableBounds_RejectsBeforeChangingConfiguration(int count, int offset)
        {
            SelectorModel model = new();
            model.Configure(3, 1, 0, true, false, false, false, true, new[] { 2 }, null);

            Assert.Throws<ArgumentOutOfRangeException>(() => model.Configure(count, 0, offset, false,
                true, false, true, false, null, null));

            Assert.That(model.Count, Is.EqualTo(3));
            Assert.That(model.CurrentIndex, Is.EqualTo(1));
            Assert.That(model.IndexOffset, Is.Zero);
            Assert.That(model.Loop, Is.True);
            Assert.That(model.FillMode, Is.False);
            Assert.That(model.ExcludedIndices, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void Component_MaximumBoundSingleton_KeepsInspectorAndActualItemConsistent()
        {
            GameObject root = CreateSingleton(out Selector selector, out GameObject item);
            try
            {
                selector.IndexOffset = -int.MaxValue;
                selector.SetLast();
                selector.RefreshItems();

                Assert.That(selector.Value, Is.EqualTo(int.MaxValue));
                Assert.That(selector.IndexWithOffset, Is.Zero);
                Assert.That(selector.IncludedIndices, Is.EqualTo(new[] { int.MaxValue }));
                Assert.That(selector.Item, Is.SameAs(item));
                Assert.That(selector.CountActive, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Component_UnrepresentableOffset_DoesNotReplacePreviousValidSelection()
        {
            GameObject root = CreateSingleton(out Selector selector, out GameObject item);
            try
            {
                selector.Set(0);

                Assert.Throws<ArgumentOutOfRangeException>(() => selector.IndexOffset = int.MinValue);

                Assert.That(selector.IndexOffset, Is.Zero);
                Assert.That(selector.Value, Is.Zero);
                Assert.That(selector.Item, Is.SameAs(item));
                Assert.That(item.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Component_UnrepresentableCount_DoesNotReplacePreviousValidConfiguration()
        {
            GameObject root = CreateSingleton(out Selector selector, out GameObject item);
            try
            {
                selector.IndexOffset = -int.MaxValue;

                Assert.Throws<ArgumentOutOfRangeException>(() => selector.Count = 2);

                Assert.That(selector.Count, Is.EqualTo(1));
                Assert.That(selector.Value, Is.EqualTo(int.MaxValue));
                Assert.That(selector.IndexOffset, Is.EqualTo(-int.MaxValue));
                Assert.That(selector.Item, Is.SameAs(item));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static IEnumerable<int> ThrowingIndices()
        {
            yield return 3;
            throw new InvalidOperationException("Rejected replacement input.");
        }

        private static GameObject CreateSingleton(out Selector selector, out GameObject item)
        {
            GameObject root = new("Audit20261023NeoToolsView.Input");
            item = new GameObject("OnlyItem");
            item.transform.SetParent(root.transform);
            selector = root.AddComponent<Selector>();
            selector.startOnAwake = false;
            typeof(Selector).GetField("_autoUpdateFromChildren", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(selector, false);
            selector.RefreshItems();
            return root;
        }
    }
}
