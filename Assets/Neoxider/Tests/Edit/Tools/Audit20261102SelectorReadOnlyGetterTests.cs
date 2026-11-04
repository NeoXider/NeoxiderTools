using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neo.Editor.Tests;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tests.Edit
{
    public sealed class Audit20261102SelectorReadOnlyGetterTests
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

        [Test]
        public void LiveSerializedFields_AreObservedBetweenReadsInTheSameFrame()
        {
            Selector selector = CreateSelector();
            SetState(selector, 5, 1, 0, true, false);
            SelectorModel isolated = selector.CreateModelSnapshot();
            int frame = Time.frameCount;

            SetState(selector, 3, -2, 2, true, false);
            AssertReads(selector, true, false, 1);
            SetField(selector, "_allowEmptyEffectiveIndex", true);
            AssertReads(selector, false, false, 1);
            SetField(selector, "_currentIndex", -3);
            AssertReads(selector, true, false, 0);
            SetField(selector, "_currentIndex", 0);
            AssertReads(selector, false, true, 3);
            SetField(selector, "_fillMode", false);
            AssertReads(selector, false, true, 1);
            SetField(selector, "_count", 0);
            AssertReads(selector, true, true, 0);

            Assert.That(Time.frameCount, Is.EqualTo(frame));
            Assert.That(isolated.Count, Is.EqualTo(5));
            Assert.That(isolated.CurrentIndex, Is.EqualTo(1));
            Assert.That(isolated.GetLogicalActiveCount(), Is.EqualTo(2));
        }

        [TestCase(0, -1, int.MinValue, false, false, true, false, 0)]
        [TestCase(0, 1, int.MaxValue, true, true, false, true, 0)]
        [TestCase(3, -2, 2, true, false, true, false, 1)]
        [TestCase(3, -2, 2, true, true, false, false, 1)]
        [TestCase(3, -3, 2, true, true, true, false, 0)]
        [TestCase(3, -3, 2, true, false, true, false, 1)]
        [TestCase(3, 0, 2, true, false, false, true, 3)]
        [TestCase(3, int.MinValue, -1, true, false, true, false, 1)]
        [TestCase(3, int.MaxValue, 1, true, false, false, true, 3)]
        [TestCase(3, int.MaxValue, 1, false, false, false, true, 1)]
        [TestCase(int.MaxValue, int.MaxValue - 1, 0, true, false, false, true, int.MaxValue)]
        [TestCase(1, int.MaxValue, -int.MaxValue, true, false, true, true, 1)]
        [TestCase(1, int.MinValue, int.MaxValue, true, false, true, false, 1)]
        [TestCase(1, int.MinValue, int.MaxValue, true, true, true, false, 0)]
        public void RawBoundsAndLogicalCount_MatchHandDerivedOracles(int count, int currentIndex, int offset,
            bool fillMode, bool allowEmpty, bool atStart, bool atEnd, int active)
        {
            Selector selector = CreateSelector();
            SetState(selector, count, currentIndex, offset, fillMode, allowEmpty);

            AssertReads(selector, atStart, atEnd, active);
            Assert.That(selector.Value, Is.EqualTo(currentIndex), "Reading must not clamp the stored index.");

            SelectorModel model = new();
            model.Configure(count, currentIndex, offset, true, fillMode, allowEmpty, true, false,
                new[] { 0 }, new[] { 0 });
            Assert.That(model.IsAtStart, Is.EqualTo(atStart));
            Assert.That(model.IsAtEnd, Is.EqualTo(atEnd));
            Assert.That(model.GetLogicalActiveCount(), Is.EqualTo(active));
            Assert.That(model.CurrentIndex, Is.EqualTo(currentIndex));
        }

        [TestCase(3, int.MaxValue, 1, false, 2)]
        [TestCase(3, int.MinValue, -1, false, 0)]
        [TestCase(3, -2, 2, false, 0)]
        [TestCase(3, -3, 2, true, -1)]
        [TestCase(1, int.MaxValue, -int.MaxValue, false, 0)]
        public void PublicModelEffectiveIndex_RetainsWideSumAndClamps(int count, int currentIndex, int offset,
            bool allowEmpty, int expected)
        {
            SelectorModel model = new();
            model.Configure(count, currentIndex, offset, false, false, allowEmpty, false, false, null, null);

            Assert.That(model.GetEffectiveIndex(), Is.EqualTo(expected));
            Assert.That(model.CurrentIndex, Is.EqualTo(currentIndex));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void PublicModelNonPositiveCount_ReturnsZeroBeforeBoundsValidation(int count)
        {
            SelectorModel model = new();
            model.Configure(3, int.MaxValue, 0, false, true, false, false, false, null, null);
            model.Count = count;
            model.IndexOffset = int.MinValue;

            Assert.That(model.GetEffectiveIndex(), Is.Zero);
            Assert.That(model.GetLogicalActiveCount(), Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PositiveCount_ValidatesBoundsBeforeClampOrNonFillReturn(bool fillMode)
        {
            Selector selector = CreateSelector();
            SetState(selector, 1, int.MaxValue, int.MinValue, fillMode, false);
            string before = JsonUtility.ToJson(selector);

            AssertBoundsError(() => selector.IsAtStart);
            AssertBoundsError(() => selector.IsAtEnd);
            AssertBoundsError(() => selector.CountActive);
            Assert.That(JsonUtility.ToJson(selector), Is.EqualTo(before));

            SelectorModel model = new();
            model.Configure(3, int.MaxValue, 0, false, fillMode, false, false, false, null, null);
            model.Count = 1;
            model.IndexOffset = int.MinValue;
            AssertBoundsError(model.GetEffectiveIndex);
            AssertBoundsError(model.GetLogicalActiveCount);
            Assert.That(model.CurrentIndex, Is.EqualTo(int.MaxValue));
            Assert.That(model.IndexOffset, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void RawIndexWithOffset_RemainsCheckedWhileLogicalIndexClamps()
        {
            Selector selector = CreateSelector();
            SetState(selector, 3, int.MaxValue, 1, true, false);
            AssertReads(selector, false, true, 3);
            Assert.Throws<OverflowException>(() => Convert.ToInt64(selector.IndexWithOffset));

            SelectorModel model = new();
            model.Configure(3, int.MaxValue, 1, false, true, false, false, false, null, null);
            Assert.That(model.GetEffectiveIndex(), Is.EqualTo(2));
            Assert.That(model.GetLogicalActiveCount(), Is.EqualTo(3));
            Assert.Throws<OverflowException>(() => Convert.ToInt64(model.IndexWithOffset));
        }

        [Test]
        public void RealItems_ReadActiveSelfUnderInactiveParentAndObserveArrayChangesWithoutBoundsValidation()
        {
            Selector selector = CreateSelector();
            GameObject active = CreateChild(selector, "Active");
            GameObject inactive = CreateChild(selector, "Inactive");
            GameObject fallback = CreateChild(selector, "Fallback");
            GameObject destroyed = CreateChild(selector, "Destroyed");
            inactive.SetActive(false);
            SetField(selector, "_items", new[] { active, inactive, null, destroyed, fallback });
            Object.DestroyImmediate(destroyed);
            SetState(selector, 0, 0, int.MinValue, true, true);

            Assert.That(active.activeInHierarchy, Is.False);
            Assert.That(active.activeSelf, Is.True);
            Assert.That(selector.CountActive, Is.EqualTo(2));
            AssertBoundsError(() => selector.IsAtStart);
            AssertBoundsError(() => selector.IsAtEnd);

            selector.Items[0] = inactive;
            Assert.That(selector.CountActive, Is.EqualTo(1));
            inactive.SetActive(true);
            Assert.That(selector.CountActive, Is.EqualTo(3), "The existing count is per array entry.");
            selector.Items[1] = null;
            Assert.That(selector.CountActive, Is.EqualTo(2));

            SetState(selector, 3, 1, 0, true, false);
            Assert.That(selector.Count, Is.EqualTo(3));
            Assert.That(selector.CountActive, Is.EqualTo(2));
            SetField(selector, "_currentIndex", 2);
            Assert.That(selector.CountActive, Is.EqualTo(3), "Positive virtual count must override populated Items.");
            SetField(selector, "_indexOffset", int.MinValue);
            AssertBoundsError(() => selector.CountActive);
            SetField(selector, "_count", -1);
            Assert.That(selector.CountActive, Is.EqualTo(2), "Negative virtual count falls back to real Items.");
        }

        [Test]
        public void RealItems_UseSelectorItemStateAndMissingComponentFallbackImmediately()
        {
            Selector selector = CreateSelector();
            GameObject first = CreateChild(selector, "LogicalOff");
            GameObject second = CreateChild(selector, "LogicalOn");
            GameObject fallback = CreateChild(selector, "WithoutSelectorItem");
            SelectorItem firstItem = first.AddComponent<SelectorItem>();
            SelectorItem secondItem = second.AddComponent<SelectorItem>();
            second.SetActive(false);
            secondItem.SetActive(true);
            SetField(selector, "_items", new[] { first, second, fallback });
            SetField(selector, "_notifySelectorItemsOnly", true);
            SetState(selector, 0, 0, int.MinValue, false, false);

            Assert.That(firstItem.ActiveValue, Is.False);
            Assert.That(secondItem.ActiveValue, Is.True);
            Assert.That(selector.CountActive, Is.EqualTo(2));
            firstItem.SetActive(true);
            fallback.SetActive(false);
            Assert.That(selector.CountActive, Is.EqualTo(2));
            selector.NotifySelectorItemsOnly = false;
            Assert.That(selector.CountActive, Is.EqualTo(1));
            selector.NotifySelectorItemsOnly = true;
            Assert.That(selector.CountActive, Is.EqualTo(2));
            Assert.That(first.activeSelf, Is.True);
            Assert.That(second.activeSelf, Is.False);
        }

        [Test]
        public void RepeatedReads_PreserveSerializedStateSnapshotsEventsAndSaveSilence()
        {
            using (Audit20261023NeoEditorTestsSaveScope scope = new())
            {
                Selector selector = CreateSelector();
                SetState(selector, 5, 2, 0, true, false);
                SetField(selector, "_uniqueSelectionMode", true);
                selector.SetExcludedIndices(new[] { 0, 4 });
                selector.Set(2);
                EnableSave(selector, "SelectorGetterPurity");
                selector.Set(2);

                int selectionEvents = 0;
                int objectEvents = 0;
                int activeEvents = 0;
                int writes = 0;
                selector.OnSelectionChanged.AddListener(index => selectionEvents++);
                selector.OnSelectionChangedGameObject.AddListener(item => objectEvents++);
                selector.OnCountActiveChanged.AddListener(count => activeEvents++);
                ISaveProvider provider = SaveProvider.CurrentProvider;
                Action<string> onWrite = key => writes++;
                provider.OnKeyChanged += onWrite;
                try
                {
                    string before = JsonUtility.ToJson(selector);
                    int[] excluded = selector.ExcludedIndices.ToArray();
                    int[] used = selector.UsedIndicesForUnique.ToArray();
                    int[] included = selector.IncludedIndices.ToArray();
                    GameObject[] items = selector.Items;
                    for (int group = 0; group < 100; group++)
                    {
                        AssertReads(selector, false, false, 3);
                    }

                    Assert.That(JsonUtility.ToJson(selector), Is.EqualTo(before));
                    Assert.That(selector.ExcludedIndices, Is.EqualTo(excluded));
                    Assert.That(selector.UsedIndicesForUnique, Is.EqualTo(used));
                    Assert.That(selector.IncludedIndices, Is.EqualTo(included));
                    Assert.That(selector.Items, Is.SameAs(items));
                    Assert.That(selectionEvents, Is.Zero);
                    Assert.That(objectEvents, Is.Zero);
                    Assert.That(activeEvents, Is.Zero);
                    Assert.That(writes, Is.Zero);
                    Assert.That(provider.GetInt("SelectorGetterPurity_Index", -1), Is.EqualTo(2));
                    Assert.That(provider.GetString("SelectorGetterPurity_Excluded", ""), Is.EqualTo("0,4"));

                    selector.Set(3);
                    Assert.That(selectionEvents, Is.EqualTo(1));
                    Assert.That(objectEvents, Is.EqualTo(1));
                    Assert.That(activeEvents, Is.EqualTo(1));
                    Assert.That(writes, Is.EqualTo(2), "The same subscribed provider must observe real writes.");
                    Assert.That(provider.GetInt("SelectorGetterPurity_Index", -1), Is.EqualTo(3));
                }
                finally
                {
                    provider.OnKeyChanged -= onWrite;
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Commands_ResynchronizeLiveFieldsWithOrWithoutPriorGetterReads(bool readFirst)
        {
            Selector selector = CreateSelector();
            SetState(selector, 5, 1, 0, true, false);
            selector.SetExcludedIndices(new[] { 4 });
            selector.Set(1);
            SetState(selector, 3, -2, 2, true, false);
            if (readFirst)
            {
                AssertReads(selector, true, false, 1);
            }

            selector.Next();
            Assert.That(selector.Value, Is.EqualTo(-1));
            AssertReads(selector, false, false, 2);
            selector.Previous();
            Assert.That(selector.Value, Is.EqualTo(-2));
            AssertReads(selector, true, false, 1);
            selector.Set(0);
            AssertReads(selector, false, true, 3);
            selector.SetFirst();
            Assert.That(selector.Value, Is.EqualTo(-2));
            AssertReads(selector, true, false, 1);
            Assert.That(selector.ExcludedIndices, Is.EqualTo(new[] { 4 }));
            Assert.That(selector.CreateModelSnapshot().IndexOffset, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SelectionCallbackReplacement_PreservesLatestStateWithOrWithoutGetterReads(bool readInCallback)
        {
            Selector selector = CreateSelector();
            SetState(selector, 4, 0, 0, true, false);
            selector.Set(0);
            List<int> selected = new();
            selector.OnSelectionChanged.AddListener(index =>
            {
                selected.Add(index);
                if (index == 1)
                {
                    if (readInCallback)
                    {
                        AssertReads(selector, false, false, 2);
                    }

                    selector.Set(3);
                }
            });

            selector.Set(1);

            Assert.That(selected, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(selector.Value, Is.EqualTo(3));
            AssertReads(selector, false, true, 4);
            Assert.That(selector.CreateModelSnapshot().CurrentIndex, Is.EqualTo(3));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SameIndexSet_RetainsPublicationSaveOrderAndCountNotificationGuard(bool readFirst)
        {
            using (Audit20261023NeoEditorTestsSaveScope scope = new())
            {
                Selector selector = CreateSelector();
                SetState(selector, 4, 1, 0, true, false);
                selector.SetExcludedIndices(new[] { 3 });
                selector.Set(1);
                EnableSave(selector, "SelectorGetterOrder");
                List<string> trace = new();
                selector.OnSelectionChanged.AddListener(index => trace.Add("selection:" + index));
                selector.OnSelectionChangedGameObject.AddListener(item => trace.Add(item == null ? "object:null" : "object"));
                selector.OnCountActiveChanged.AddListener(count => trace.Add("active:" + count));
                ISaveProvider provider = SaveProvider.CurrentProvider;
                Action<string> onWrite = key => trace.Add(key == "SelectorGetterOrder_Index" ? "save:index" : "save:excluded");
                provider.OnKeyChanged += onWrite;
                try
                {
                    if (readFirst)
                    {
                        AssertReads(selector, false, false, 2);
                        Assert.That(trace, Is.Empty);
                    }

                    selector.Set(1);

                    Assert.That(trace, Is.EqualTo(new[]
                    {
                        "selection:1", "object:null", "save:index", "save:excluded"
                    }));
                    Assert.That(provider.GetInt("SelectorGetterOrder_Index", -1), Is.EqualTo(1));
                    Assert.That(provider.GetString("SelectorGetterOrder_Excluded", ""), Is.EqualTo("3"));
                }
                finally
                {
                    provider.OnKeyChanged -= onWrite;
                }
            }
        }

        [Test]
        public void ReadsAndCommands_RetainDefensiveEnumerableAndSnapshotIsolation()
        {
            Selector selector = CreateSelector();
            SetState(selector, 4, 0, 0, false, false);
            SetField(selector, "_uniqueSelectionMode", true);
            SetField(selector, "_resetUniqueWhenCycleComplete", false);
            SetField(selector, "_useRandomSelection", true);
            List<int> callerInput = new() { 1, 2 };
            selector.SetExcludedIndices(callerInput);
            callerInput.Clear();
            selector.Set(0);
            AssertReads(selector, true, false, 1);
            SelectorModel isolated = selector.CreateModelSnapshot();

            isolated.SetExcludedIndices(isolated.ExcludedIndices.Where(index => index != 2));
            isolated.SetUsedIndicesForUnique(isolated.UsedIndicesForUnique.Where(index => index == 0));
            isolated.SetExcludedIndices(isolated.ExcludedIndices);
            isolated.SetUsedIndicesForUnique(isolated.UsedIndicesForUnique);
            Assert.Throws<InvalidOperationException>(() => isolated.SetExcludedIndices(ThrowingIndices()));
            Assert.Throws<InvalidOperationException>(() => isolated.SetUsedIndicesForUnique(ThrowingIndices()));

            Assert.That(isolated.ExcludedIndices, Is.EquivalentTo(new[] { 1 }));
            Assert.That(isolated.UsedIndicesForUnique, Is.EquivalentTo(new[] { 0 }));
            Assert.That(selector.ExcludedIndices, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(selector.UsedIndicesForUnique, Is.EqualTo(new[] { 0 }));
            Assert.That(selector.UniqueRemainingCount, Is.EqualTo(1));
            selector.SetRandom();
            Assert.That(selector.Value, Is.EqualTo(3), "There is exactly one eligible unused item.");
            AssertReads(selector, false, true, 1);
        }

        private Selector CreateSelector()
        {
            GameObject owner = new("Audit20261102SelectorReadOnlyGetter");
            _objects.Add(owner);
            owner.SetActive(false);
            Selector selector = owner.AddComponent<Selector>();
            selector.startOnAwake = false;
            selector.isNetworked = false;
            SetField(selector, "_autoUpdateFromChildren", false);
            SetField(selector, "_items", Array.Empty<GameObject>());
            SetField(selector, "_saveEnabled", false);
            return selector;
        }

        private GameObject CreateChild(Selector selector, string name)
        {
            GameObject child = new(name);
            _objects.Add(child);
            child.transform.SetParent(selector.transform, false);
            return child;
        }

        private static void SetState(Selector selector, int count, int currentIndex, int offset, bool fillMode,
            bool allowEmpty)
        {
            // WHY: direct writes to the serialized authoring fields deliberately leave the private model stale, so
            // reads must come from the live fields; no private lifecycle or runtime method is invoked.
            SetField(selector, "_count", count);
            SetField(selector, "_currentIndex", currentIndex);
            SetField(selector, "_indexOffset", offset);
            SetField(selector, "_fillMode", fillMode);
            SetField(selector, "_allowEmptyEffectiveIndex", allowEmpty);
        }

        private static void EnableSave(Selector selector, string key)
        {
            SetField(selector, "_saveEnabled", true);
            SetField(selector, "_saveKey", key);
            SetField(selector, "_saveMode", SelectorSaveMode.Index | SelectorSaveMode.ExcludedIndices);
        }

        private static void SetField(Selector selector, string name, object value)
        {
            FieldInfo field = typeof(Selector).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(selector, value);
        }

        private static void AssertReads(Selector selector, bool atStart, bool atEnd, int active)
        {
            Assert.That(selector.IsAtStart, Is.EqualTo(atStart), "IsAtStart");
            Assert.That(selector.IsAtEnd, Is.EqualTo(atEnd), "IsAtEnd");
            Assert.That(selector.CountActive, Is.EqualTo(active), "CountActive");
        }

        private static void AssertBoundsError<T>(Func<T> read)
        {
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                T value = read();
                GC.KeepAlive(value);
            });
            Assert.That(error.ParamName, Is.EqualTo("indexOffset"));
        }

        private static IEnumerable<int> ThrowingIndices()
        {
            yield return 3;
            throw new InvalidOperationException("Rejected replacement.");
        }
    }
}
