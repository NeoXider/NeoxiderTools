using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit
{
    public sealed class Audit20261023NeoToolsViewSelectorTests
    {
        [TestCase(1)]
        [TestCase(3)]
        public void UniqueRandom_WithOneIncludedIndex_StopsUntilReset(int count)
        {
            SelectorModel model = new();
            model.Configure(count, 0, 0, true, false, false, true, false,
                count == 3 ? new[] { 0, 2 } : null, null);

            SelectorModelResult first = model.SetRandom(false, (_, _) => 0);
            int selected = model.CurrentIndex;
            SelectorModelResult exhausted = model.SetRandom(false, (_, _) => 0);

            Assert.That(first.SelectionChanged, Is.True);
            Assert.That(first.DeactivateOthers, Is.False);
            Assert.That(model.UniqueRemainingCount, Is.Zero);
            Assert.That(exhausted.SelectionChanged, Is.False);
            Assert.That(exhausted.UniqueCycleComplete, Is.True);
            Assert.That(exhausted.UniqueReset, Is.False);
            Assert.That(model.CurrentIndex, Is.EqualTo(selected));

            model.ResetUnique();
            Assert.That(model.UniqueRemainingCount, Is.EqualTo(1));
            Assert.That(model.SetRandom(false, (_, _) => 0).SelectionChanged, Is.True);
        }

        [TestCase(1)]
        [TestCase(3)]
        public void UniqueRandom_WithOneIncludedIndex_ReportsAutomaticCycleReset(int count)
        {
            SelectorModel model = new();
            model.Configure(count, 0, 0, true, false, false, true, true,
                count == 3 ? new[] { 0, 2 } : null, null);
            model.SetRandom(true, (_, _) => 0);

            SelectorModelResult next = model.SetRandom(true, (_, _) => 0);

            Assert.That(next.SelectionChanged, Is.True);
            Assert.That(next.UniqueCycleComplete, Is.True);
            Assert.That(next.UniqueReset, Is.True);
            Assert.That(model.UsedIndicesForUnique, Is.EquivalentTo(new[] { count == 3 ? 1 : 0 }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void UniqueRemainingCount_WithoutItems_IsZero(int count)
        {
            SelectorModel model = new() { Count = count, UniqueSelectionMode = true };

            Assert.That(model.UniqueRemainingCount, Is.Zero);
            Assert.That(model.SetRandom(true, null).SelectionChanged, Is.False);
        }

        [Test]
        public void Model_RestoringItsOwnCollections_PreservesExclusionsAndUniqueHistory()
        {
            SelectorModel model = new();
            model.Configure(4, 0, 0, true, false, false, true, false, new[] { 1 }, new[] { 0, 2 });

            model.SetExcludedIndices(model.ExcludedIndices);
            model.SetUsedIndicesForUnique(model.UsedIndicesForUnique);
            model.Configure(4, 0, 0, true, false, false, true, false,
                model.ExcludedIndices, model.UsedIndicesForUnique);

            Assert.That(model.ExcludedIndices, Is.EquivalentTo(new[] { 1 }));
            Assert.That(model.UsedIndicesForUnique, Is.EquivalentTo(new[] { 0, 2 }));
            model.SetRandom(true, (_, _) => 0);
            Assert.That(model.CurrentIndex, Is.EqualTo(3));
        }

        [Test]
        public void RemoveItem_WithPositiveOffset_KeepsValueAndSelectedObjectConsistent()
        {
            GameObject root = CreateSelector(3, out Selector selector);
            try
            {
                selector.IndexOffset = 1;
                selector.SetLast();
                GameObject remaining = selector.Items[1];
                selector.RemoveItem(selector.Items[2]);

                Assert.That(selector.Value, Is.EqualTo(0));
                Assert.That(selector.Item, Is.SameAs(remaining));
                Assert.That(selector.GetSelectedItem(), Is.SameAs(remaining));
                Assert.That(remaining.activeSelf, Is.True);
                Assert.That(selector.CountActive, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RefreshItems_WithNegativeOffset_PreservesValidSelection()
        {
            GameObject root = CreateSelector(3, out Selector selector);
            try
            {
                selector.IndexOffset = -2;
                selector.Set(3);
                GameObject selected = selector.Item;

                selector.RefreshItems();

                Assert.That(selector.Value, Is.EqualTo(3));
                Assert.That(selector.Item, Is.SameAs(selected));
                Assert.That(selector.Items[1].activeSelf, Is.True);
                Assert.That(selector.Items[0].activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RemoveLastItem_NotifiesEmptyObjectAndActiveCount()
        {
            GameObject root = CreateSelector(1, out Selector selector);
            try
            {
                int lastCount = -1;
                List<GameObject> selectedObjects = new();
                selector.OnCountActiveChanged.AddListener(value => lastCount = value);
                selector.OnSelectionChangedGameObject.AddListener(selectedObjects.Add);
                selector.Set(0);

                selector.RemoveItem(selector.Item);

                Assert.That(selector.Count, Is.Zero);
                Assert.That(selector.CountActive, Is.Zero);
                Assert.That(lastCount, Is.Zero);
                Assert.That(selectedObjects.Count, Is.EqualTo(2));
                Assert.That(selectedObjects[1], Is.Null);
                Assert.That(selector.IncludedIndices, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateSelector(int count, out Selector selector)
        {
            GameObject root = new("Audit20261023NeoToolsViewSelector");
            for (int i = 0; i < count; i++)
            {
                GameObject child = new($"Item{i}");
                child.transform.SetParent(root.transform);
            }

            selector = root.AddComponent<Selector>();
            selector.startOnAwake = false;
            typeof(Selector).GetField("_autoUpdateFromChildren", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(selector, false);
            selector.RefreshItems();
            return root;
        }
    }
}
