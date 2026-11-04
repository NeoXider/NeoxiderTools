using System;
using System.Collections.Generic;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tools.Tests
{
    /// <summary>
    ///     Allocation guards for Selector reads and commands on a warm virtual pool and on child items with the default
    ///     auto-update, plus behaviour pins for the item-activation rule that now lives in
    ///     <see cref="SelectorModel.IsItemActive(int)"/>. Allocations are counted by the calibrated GC.Alloc probe of
    ///     <see cref="GcAlloc"/>; when it is not admitted the allocation cases are ignored, not passed.
    /// </summary>
    [TestFixture]
    public sealed class SelectorHotPathTests
    {
        private readonly List<GameObject> _objects = new();
        private UnityEngine.Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = UnityEngine.Random.state;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                for (int index = _objects.Count - 1; index >= 0; index--)
                {
                    if (_objects[index] != null)
                    {
                        Object.DestroyImmediate(_objects[index]);
                    }
                }
            }
            finally
            {
                _objects.Clear();
                UnityEngine.Random.state = _randomState;
            }
        }

        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void ReadOnlyGetters_WarmVirtualPoolWithExclusionsAndHistory_DoNotAllocate(int count)
        {
            Selector selector = CreateVirtualSelector(false);
            int current = ConfigureMeasuredPool(selector, count);
            bool expectedStart = current == 0;
            bool expectedEnd = current == count - 1;
            int expectedActive = current + 1;
            ReadGroup(selector, out bool atStart, out bool atEnd, out int active);

            long allocationCalls = GcAlloc.TryCount(() =>
            {
                for (int group = 0; group < 100; group++)
                {
                    ReadGroup(selector, out atStart, out atEnd, out active);
                }
            }, out string notAdmitted);

            Assert.That(atStart, Is.EqualTo(expectedStart));
            Assert.That(atEnd, Is.EqualTo(expectedEnd));
            Assert.That(active, Is.EqualTo(expectedActive));
            Assert.That(selector.Value, Is.EqualTo(current));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocationCalls, Is.Zero,
                "IsAtStart/IsAtEnd/CountActive must not copy the E/U sets.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Commands_WarmVirtualPoolWithExclusionsAndHistory_DoNotAllocate(bool autoUpdateFromChildren)
        {
            Selector selector = CreateVirtualSelector(autoUpdateFromChildren);
            ConfigureMeasuredPool(selector, 100);
            RunCommandCycle(selector);
            RunCommandCycle(selector);
            int[] excluded = ToArray(selector.ExcludedIndices);
            int[] used = ToArray(selector.UsedIndicesForUnique);

            long allocationCalls = GcAlloc.TryCount(() => RunCommandCycle(selector), out string notAdmitted);

            Assert.That(selector.Value, Is.EqualTo(99), "The cycle ends with SetLast.");
            Assert.That(selector.Count, Is.EqualTo(100), "A virtual pool without children keeps its Count.");
            Assert.That(selector.Items == null || selector.Items.Length == 0, Is.True);
            Assert.That(selector.ExcludedIndices, Is.EqualTo(excluded));
            Assert.That(selector.UsedIndicesForUnique, Is.EqualTo(used));
            Assert.That(selector.CountActive, Is.EqualTo(100));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocationCalls, Is.Zero,
                "Set/Next/Previous/SetFirst/SetLast must reuse the selector's index sets, model and child scan, and sort " +
                "the Inspector snapshots without List.Sort (one GC.Alloc per call on the Editor Mono).");
        }

        [Test]
        public void Commands_WarmChildItemsWithDefaultAutoUpdate_DoNotAllocate()
        {
            Selector selector = CreateVirtualSelector(true);
            for (int i = 0; i < 100; i++)
            {
                GameObject item = new("Child" + i);
                _objects.Add(item);
                item.transform.SetParent(selector.transform, false);
            }

            List<int> excludedInput = new();
            for (int i = 0; i < 100; i += 4)
            {
                excludedInput.Add(i);
            }

            selector.SetExcludedIndices(excludedInput);
            RunCommandCycle(selector);
            RunCommandCycle(selector);
            GameObject[] items = selector.Items;
            int[] excluded = ToArray(selector.ExcludedIndices);
            int[] used = ToArray(selector.UsedIndicesForUnique);

            long allocationCalls = GcAlloc.TryCount(() => RunCommandCycle(selector), out string notAdmitted);

            Assert.That(items.Length, Is.EqualTo(100), "Items come from the children.");
            Assert.That(selector.Items, Is.SameAs(items), "Synced children must not rebuild Items.");
            Assert.That(selector.Value, Is.EqualTo(99));
            Assert.That(selector.ExcludedIndices, Is.EqualTo(excluded));
            Assert.That(selector.UsedIndicesForUnique, Is.EqualTo(used));
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocationCalls, Is.Zero,
                "Warm commands on child items must not allocate: indexed child sync check, in-place index-set copies and " +
                "allocation-free sorting of the Inspector snapshots.");
        }

        [Test]
        public void Commands_KeepIndexSetsIsolatedFromCallerInputsAndSnapshots()
        {
            Selector selector = CreateVirtualSelector();
            selector.Count = 6;
            List<int> callerInput = new() { 5, 1 };
            selector.SetExcludedIndices(callerInput);
            callerInput.Add(2);
            selector.Set(3);
            SelectorModel snapshot = selector.CreateModelSnapshot();
            snapshot.ExcludeIndex(4);
            selector.Next();
            selector.IncludeIndex(1);

            Assert.That(selector.ExcludedIndices, Is.EqualTo(new[] { 5 }));
            Assert.That(selector.IsExcluded(2), Is.False);
            Assert.That(selector.IsExcluded(4), Is.False);
            Assert.That(snapshot.ExcludedIndices, Is.EquivalentTo(new[] { 1, 4, 5 }));
            Assert.That(selector.UsedIndicesForUnique, Is.EqualTo(new[] { 3 }));
            Assert.That(selector.Value, Is.EqualTo(4));
        }

        [Test]
        public void IncludedIndices_StayAscendingAcrossNegativeBoundsAndExclusions()
        {
            Selector selector = CreateVirtualSelector();
            JsonUtility.FromJsonOverwrite("{\"_allowEmptyEffectiveIndex\":true}", selector);
            selector.Count = 5;
            selector.IndexOffset = 2;
            selector.SetExcludedIndices(new[] { 1, -1, 7 });
            selector.Set(0);

            Assert.That(selector.IncludedIndices, Is.EqualTo(new[] { -3, -2, 0, 2 }));
            Assert.That(selector.ExcludedIndices, Is.EqualTo(new[] { -1, 1, 7 }));

            selector.SetLast();
            Assert.That(selector.Value, Is.EqualTo(2));
            Assert.That(selector.IncludedIndices, Is.EqualTo(new[] { -3, -2, 0, 2 }));
        }

        [Test]
        public void ApplySelection_PlainItemsFollowExclusiveFillAdditiveEmptyAndUncontrolledModes()
        {
            Selector selector = CreateItemSelector(4, false);

            selector.Set(2);
            AssertActive(selector, false, false, true, false);
            AssertModelRule(selector);

            selector.FillMode = true;
            AssertActive(selector, true, true, true, false);
            AssertModelRule(selector);

            selector.FillMode = false;
            selector.Set(1);
            AssertActive(selector, false, true, false, false);
            JsonUtility.FromJsonOverwrite("{\"_useRandomSelection\":true}", selector);
            selector.SetExcludedIndices(new[] { 0, 1, 2 });
            selector.SetRandom(false);
            Assert.That(selector.Value, Is.EqualTo(3), "Only index 3 is eligible.");
            AssertActive(selector, false, true, false, true);

            selector.Set(0);
            AssertActive(selector, true, false, false, false);
            JsonUtility.FromJsonOverwrite("{\"_allowEmptyEffectiveIndex\":true}", selector);
            selector.SetFirst();
            Assert.That(selector.Value, Is.EqualTo(-1));
            AssertActive(selector, false, false, false, false);
            AssertModelRule(selector);

            JsonUtility.FromJsonOverwrite("{\"_controlGameObjectActive\":false}", selector);
            selector.Set(2);
            Assert.That(selector.Value, Is.EqualTo(2));
            AssertActive(selector, false, false, false, false);
        }

        [Test]
        public void ApplySelection_NotifyModeDrivesSelectorItemsAndTogglesOnlyPlainItems()
        {
            Selector selector = CreateItemSelector(4, true);
            SelectorItem first = selector.Items[0].GetComponent<SelectorItem>();
            SelectorItem third = selector.Items[2].GetComponent<SelectorItem>();
            bool firstObjectActive = selector.Items[0].activeSelf;
            bool thirdObjectActive = selector.Items[2].activeSelf;
            Assert.That(firstObjectActive, Is.True, "Plain mode selected item 0 while the items were added.");
            Assert.That(thirdObjectActive, Is.False);
            selector.NotifySelectorItemsOnly = true;
            selector.FillMode = true;

            selector.Set(1);

            Assert.That(first.ActiveValue, Is.True);
            Assert.That(third.ActiveValue, Is.False);
            Assert.That(first.Index, Is.EqualTo(0));
            Assert.That(third.Index, Is.EqualTo(2));
            Assert.That(selector.Items[0].activeSelf, Is.EqualTo(firstObjectActive), "SelectorItem objects are not toggled.");
            Assert.That(selector.Items[2].activeSelf, Is.EqualTo(thirdObjectActive), "SelectorItem objects are not toggled.");
            Assert.That(selector.Items[1].activeSelf, Is.True);
            Assert.That(selector.Items[3].activeSelf, Is.False);
            Assert.That(selector.CountActive, Is.EqualTo(2));

            selector.FillMode = false;
            selector.Set(2);

            Assert.That(first.ActiveValue, Is.False);
            Assert.That(third.ActiveValue, Is.True);
            Assert.That(selector.Items[1].activeSelf, Is.False);
            Assert.That(selector.Items[3].activeSelf, Is.False);
            Assert.That(selector.CountActive, Is.EqualTo(1));
        }

        [TestCase(5, 2, 0, false, false, new[] { 2 })]
        [TestCase(5, 2, 0, true, false, new[] { 0, 1, 2 })]
        [TestCase(5, 1, 1, false, false, new[] { 2 })]
        [TestCase(5, 10, 0, true, false, new[] { 0, 1, 2, 3, 4 })]
        [TestCase(5, -1, 0, true, true, new int[0])]
        [TestCase(5, -5, 0, false, false, new[] { 0 })]
        [TestCase(0, 0, 0, true, false, new int[0])]
        public void SelectorModel_IsItemActive_FollowsTheSelectionRule(int count, int currentIndex, int offset,
            bool fillMode, bool allowEmpty, int[] expectedActive)
        {
            SelectorModel model = new();
            model.Configure(count, currentIndex, offset, true, fillMode, allowEmpty, false, true, null, null);

            List<int> active = new();
            for (int item = -2; item < 8; item++)
            {
                if (model.IsItemActive(item))
                {
                    active.Add(item);
                }
            }

            Assert.That(active, Is.EqualTo(expectedActive));
            Assert.That(active.Count, Is.EqualTo(model.GetLogicalActiveCount()));
        }

        [Test]
        public void SelectorModel_IsItemActive_RejectsUnrepresentableBounds()
        {
            SelectorModel model = new();
            model.Configure(3, 0, 0, true, false, false, false, true, null, null);
            model.Count = 1;
            model.IndexOffset = int.MinValue;

            ArgumentOutOfRangeException error =
                Assert.Throws<ArgumentOutOfRangeException>(() => model.IsItemActive(0));
            Assert.That(error.ParamName, Is.EqualTo("indexOffset"));
        }

        private Selector CreateVirtualSelector(bool autoUpdateFromChildren = false)
        {
            GameObject owner = new("SelectorHotPath");
            _objects.Add(owner);
            owner.SetActive(false);
            Selector selector = owner.AddComponent<Selector>();
            JsonUtility.FromJsonOverwrite("{\"_uniqueSelectionMode\":true,\"_resetUniqueWhenCycleComplete\":false," +
                                          "\"_saveEnabled\":false,\"_autoUpdateFromChildren\":" +
                                          (autoUpdateFromChildren ? "true" : "false") + "," +
                                          "\"_controlGameObjectActive\":false,\"_loop\":false," +
                                          "\"_allowEmptyEffectiveIndex\":false,\"_changeDebug\":false}", selector);
            selector.startOnAwake = false;
            selector.isNetworked = false;
            return selector;
        }

        private Selector CreateItemSelector(int count, bool selectorItemsOnEvenSlots)
        {
            GameObject owner = new("SelectorHotPathItems");
            _objects.Add(owner);
            owner.SetActive(false);
            Selector selector = owner.AddComponent<Selector>();
            JsonUtility.FromJsonOverwrite("{\"_saveEnabled\":false,\"_autoUpdateFromChildren\":false," +
                                          "\"_controlGameObjectActive\":true,\"_loop\":true," +
                                          "\"_allowEmptyEffectiveIndex\":false,\"_changeDebug\":false}", selector);
            selector.startOnAwake = false;
            selector.isNetworked = false;
            for (int i = 0; i < count; i++)
            {
                GameObject item = new("Item" + i);
                _objects.Add(item);
                item.transform.SetParent(owner.transform, false);
                if (selectorItemsOnEvenSlots && i % 2 == 0)
                {
                    item.AddComponent<SelectorItem>();
                }

                selector.AddItem(item);
            }

            Assert.That(selector.Count, Is.EqualTo(count));
            return selector;
        }

        /// <summary>
        ///     The workload of the 2026-11-02 native baseline: fill mode, every fourth index excluded, a sparse unique
        ///     history and the middle index selected.
        /// </summary>
        private static int ConfigureMeasuredPool(Selector selector, int count)
        {
            selector.Count = count;
            selector.FillMode = true;
            selector.IndexOffset = 0;
            List<int> excluded = new();
            for (int i = 0; i < count; i += 4)
            {
                excluded.Add(i);
            }

            selector.SetExcludedIndices(excluded);
            for (int i = count == 1 ? 0 : 1; i < count; i += 4)
            {
                selector.Set(i);
            }

            int current = count / 2;
            selector.Set(current);
            Assert.That(selector.ExcludedIndices.Count, Is.GreaterThan(0));
            Assert.That(selector.UsedIndicesForUnique.Count, Is.GreaterThan(0));
            return current;
        }

        private static void ReadGroup(Selector selector, out bool atStart, out bool atEnd, out int active)
        {
            atStart = selector.IsAtStart;
            atEnd = selector.IsAtEnd;
            active = selector.CountActive;
        }

        private static void RunCommandCycle(Selector selector)
        {
            for (int index = 0; index < 100; index++)
            {
                selector.Set(index);
            }

            selector.Previous();
            selector.Next();
            selector.SetFirst();
            selector.Next();
            selector.SetLast();
        }

        private static int[] ToArray(IReadOnlyList<int> values)
        {
            int[] result = new int[values.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = values[i];
            }

            return result;
        }

        private static void AssertActive(Selector selector, params bool[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(selector.Items[i].activeSelf, Is.EqualTo(expected[i]), "Item " + i);
            }
        }

        private static void AssertModelRule(Selector selector)
        {
            SelectorModel snapshot = selector.CreateModelSnapshot();
            for (int i = 0; i < selector.Items.Length; i++)
            {
                Assert.That(selector.Items[i].activeSelf, Is.EqualTo(snapshot.IsItemActive(i)), "Rule for item " + i);
            }
        }
    }
}
