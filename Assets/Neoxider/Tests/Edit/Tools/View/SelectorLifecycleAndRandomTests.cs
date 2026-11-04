using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tools.Tests
{
    /// <summary>
    ///     Player-order lifecycle of Inspector-authored exclusions, the list-free random pick of
    ///     <see cref="SelectorModel.SetRandom" /> and the sorted Inspector snapshots of <see cref="Selector" />.
    ///     Unity callbacks that EditMode does not run (Awake, OnEnable, Start) are invoked explicitly.
    /// </summary>
    [TestFixture]
    public sealed class SelectorLifecycleAndRandomTests
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
        public void PlayerLifecycle_WithoutOnValidate_KeepsInspectorAuthoredExclusions()
        {
            GameObject owner = new("SelectorPlayerLifecycle");
            _objects.Add(owner);
            Selector selector = owner.AddComponent<Selector>();
            selector.isNetworked = false;
            JsonUtility.FromJsonOverwrite("{\"_count\":6,\"_saveEnabled\":false,\"_changeDebug\":false," +
                                          "\"_currentIndex\":2,\"_excludedIndicesInspector\":[4,1]}", selector);
            Assert.That(selector.startOnAwake, Is.True, "positive control: OnEnable runs the start command");
            // WHY: a player build never runs OnValidate, so the runtime set is still unset when Awake runs.
            SetPrivateField(selector, "_excludedIndices", null);

            InvokeLifecycle(selector, "Awake");
            InvokeLifecycle(selector, "OnEnable");
            InvokeLifecycle(selector, "Start");

            Assert.That(selector.Value, Is.EqualTo(2));
            Assert.That(selector.ExcludedIndices, Is.EqualTo(new[] { 1, 4 }),
                "the start command in OnEnable must not wipe the authored list before Start applies it");
            Assert.That(selector.IsExcluded(1) && selector.IsExcluded(4), Is.True);
            Assert.That(selector.ExcludedCount, Is.EqualTo(2));
            Assert.That(selector.IncludedIndices, Is.EqualTo(new[] { 0, 2, 3, 5 }));
        }

        [Test]
        public void UsedIndicesSnapshot_IsAscendingWhateverTheSelectionOrder()
        {
            Selector selector = CreateSelector(12, true);
            int[] order = { 9, 2, 11, 0, 7, 5, 3 };
            for (int i = 0; i < order.Length; i++)
            {
                selector.Set(order[i]);
            }

            Assert.That(selector.UsedIndicesForUnique, Is.EqualTo(new[] { 0, 2, 3, 5, 7, 9, 11 }));

            selector.SetExcludedIndices(new[] { 10, -3, 6, 1 });
            Assert.That(selector.ExcludedIndices, Is.EqualTo(new[] { -3, 1, 6, 10 }));
        }

        [Test]
        public void ModelSetRandom_PicksTheSameIndexAsTheCandidateListOrder()
        {
            SelectorModel model = new();
            model.Configure(10, 0, 0, true, false, false, false, true, new[] { 1, 3 }, null);
            List<int> requestedMaxima = new();

            SelectorModelResult result = model.SetRandom(true, (min, max) =>
            {
                requestedMaxima.Add(max);
                return 4;
            });

            Assert.That(result.SelectionChanged, Is.True);
            Assert.That(model.CurrentIndex, Is.EqualTo(6), "candidates 0,2,4,5,6,7,8,9: ticket 4 is index 6");
            Assert.That(requestedMaxima, Is.EqualTo(new[] { 8 }));

            model.SetRandom(true, (min, max) => 4);
            Assert.That(model.CurrentIndex, Is.EqualTo(7), "a repeat of the current index moves to the next candidate");

            model.SetRandom(true, (min, max) => 7);
            Assert.That(model.CurrentIndex, Is.EqualTo(9));
            model.SetRandom(true, (min, max) => 7);
            Assert.That(model.CurrentIndex, Is.EqualTo(0), "the next candidate wraps to the first one");
        }

        [Test]
        public void ModelSetRandomUnique_WalksUnusedCandidatesAndResetsAtCycleEnd()
        {
            SelectorModel model = new();
            model.Configure(5, 0, 0, true, false, false, true, true, new[] { 1 }, new[] { 0, 2 });
            List<int> requestedMaxima = new();
            Func<int, int, int> first = (min, max) =>
            {
                requestedMaxima.Add(max);
                return 0;
            };

            model.SetRandom(true, first);
            Assert.That(model.CurrentIndex, Is.EqualTo(3), "unused candidates 3,4");
            model.SetRandom(true, first);
            Assert.That(model.CurrentIndex, Is.EqualTo(4));

            SelectorModelResult reset = model.SetRandom(true, first);

            Assert.That(reset.UniqueCycleComplete && reset.UniqueReset && reset.SelectionChanged, Is.True);
            Assert.That(model.CurrentIndex, Is.EqualTo(0));
            Assert.That(model.UsedIndicesForUnique, Is.EquivalentTo(new[] { 0 }));
            Assert.That(requestedMaxima, Is.EqualTo(new[] { 2, 1, 4 }));

            model.ResetUniqueWhenCycleComplete = false;
            model.SetUsedIndicesForUnique(new[] { 0, 2, 3, 4 });
            Assert.That(model.SetRandom(true, first).UniqueCycleComplete, Is.True);
            Assert.That(model.CurrentIndex, Is.EqualTo(0), "a finished cycle without auto-reset keeps the index");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SetRandom_WarmPoolWithExclusions_DoesNotAllocate(bool uniqueMode)
        {
            Selector selector = CreateSelector(100, uniqueMode);
            JsonUtility.FromJsonOverwrite("{\"_useRandomSelection\":true,\"_resetUniqueWhenCycleComplete\":true}",
                selector);
            List<int> excluded = new();
            for (int i = 0; i < 100; i += 7)
            {
                excluded.Add(i);
            }

            selector.SetExcludedIndices(excluded);
            UnityEngine.Random.InitState(1234);
            for (int i = 0; i < 300; i++)
            {
                selector.SetRandom();
            }

            long allocationCalls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < 200; i++)
                {
                    selector.SetRandom();
                }
            }, out string notAdmitted);

            Assert.That(selector.IsExcluded(selector.Value), Is.False, "picks never land on an excluded index");
            Assert.That(IsAscending(selector.UsedIndicesForUnique), Is.True);
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocationCalls, Is.Zero,
                "SetRandom must count candidates instead of building lists and reuse its random delegate.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ModelSetRandom_DefaultRandomRange_DoesNotAllocate(bool uniqueMode)
        {
            SelectorModel model = new();
            List<int> excluded = new();
            for (int i = 0; i < 100; i += 7)
            {
                excluded.Add(i);
            }

            model.Configure(100, 1, 0, true, false, false, uniqueMode, true, excluded, null);
            for (int i = 0; i < 300; i++)
            {
                model.SetRandom(true, null);
            }

            long allocationCalls = GcAlloc.TryCount(() =>
            {
                for (int i = 0; i < 200; i++)
                {
                    model.SetRandom(true, null);
                }
            }, out string notAdmitted);

            Assert.That(model.IsExcluded(model.CurrentIndex), Is.False, "picks never land on an excluded index");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocationCalls, Is.Zero,
                "randomRange: null must use the cached shared delegate and count candidates instead of listing them.");
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void ModelSetRandom_CallbackThatChangesTheExclusions_FallsBackWithoutThrowing(bool uniqueMode,
            bool excludeEverything)
        {
            SelectorModel model = new();
            model.Configure(10, 0, 0, true, false, false, uniqueMode, true, null, null);
            int excludeUpTo = excludeEverything ? 10 : 7;
            int requestedMax = -1;
            SelectorModelResult result = default;

            Assert.DoesNotThrow(() => result = model.SetRandom(true, (min, max) =>
            {
                requestedMax = max;
                for (int i = 0; i < excludeUpTo; i++)
                {
                    model.ExcludeIndex(i);
                }

                return max - 1;
            }));

            Assert.That(requestedMax, Is.EqualTo(10), "positive control: all ten candidates were counted first");
            Assert.That(model.CurrentIndex, Is.InRange(0, 9));
            if (excludeEverything)
            {
                Assert.That(model.CurrentIndex, Is.Zero, "with no candidate left the current index is kept");
                return;
            }

            Assert.That(result.SelectionChanged, Is.True);
            Assert.That(model.CurrentIndex, Is.EqualTo(9),
                "ticket 9 walks past the three remaining candidates 7, 8, 9 and falls back to the last one");
            Assert.That(model.IsExcluded(model.CurrentIndex), Is.False);
        }

        private Selector CreateSelector(int count, bool uniqueMode)
        {
            GameObject owner = new("SelectorLifecycleAndRandom");
            _objects.Add(owner);
            owner.SetActive(false);
            Selector selector = owner.AddComponent<Selector>();
            JsonUtility.FromJsonOverwrite("{\"_uniqueSelectionMode\":" + (uniqueMode ? "true" : "false") + "," +
                                          "\"_resetUniqueWhenCycleComplete\":false,\"_saveEnabled\":false," +
                                          "\"_autoUpdateFromChildren\":false,\"_controlGameObjectActive\":false," +
                                          "\"_loop\":true,\"_allowEmptyEffectiveIndex\":false,\"_changeDebug\":false}",
                selector);
            selector.startOnAwake = false;
            selector.isNetworked = false;
            selector.Count = count;
            return selector;
        }

        private static bool IsAscending(IReadOnlyList<int> values)
        {
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i - 1] > values[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void InvokeLifecycle(Selector selector, string methodName)
        {
            MethodInfo method = typeof(Selector).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(selector, null);
        }

        private static void SetPrivateField(Selector selector, string fieldName, object value)
        {
            FieldInfo field = typeof(Selector).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(selector, value);
        }
    }
}
