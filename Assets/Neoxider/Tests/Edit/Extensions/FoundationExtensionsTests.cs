using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Neo.Extensions;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using SystemRandom = System.Random;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Regression and allocation contracts for the foundation extension helpers.
    /// </summary>
    [TestFixture]
    public class FoundationExtensionsTests
    {
        private readonly List<Object> _created = new();

        private enum FoundationTestEnum
        {
            First = 3,
            Second = 7,
            Third = 11
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
        }

        #region RandomExtensions

        [Test]
        public void Shuffle_SeededSystemRandom_IsDeterministicPermutation()
        {
            int[] source = Enumerable.Range(0, 32).ToArray();

            IList<int> first = source.Shuffle(new SystemRandom(1234), false);
            IList<int> second = source.Shuffle(new SystemRandom(1234), false);

            CollectionAssert.AreEqual(first, second, "The same seed must produce the same order.");
            CollectionAssert.AreEquivalent(source, first, "A shuffle must be a permutation.");
            CollectionAssert.AreEqual(Enumerable.Range(0, 32).ToArray(), source, "A copy shuffle leaves the input.");
            CollectionAssert.AreNotEqual(source, first, "32 elements staying in order would mean no shuffle.");
        }

        [Test]
        public void Shuffle_SeededSystemRandom_InPlaceShufflesTheInput()
        {
            List<int> list = Enumerable.Range(0, 16).ToList();

            IList<int> result = list.Shuffle(new SystemRandom(7));

            Assert.That(result, Is.SameAs(list));
            CollectionAssert.AreEqual(Enumerable.Range(0, 16).ToList().Shuffle(new SystemRandom(7)), list);
        }

        [Test]
        public void Shuffle_IndexSource_UsesExclusiveBoundsAndScriptedIndices()
        {
            List<string> list = new() { "A", "B", "C", "D" };
            List<int> requestedBounds = new();

            list.Shuffle(max =>
            {
                requestedBounds.Add(max);
                return 0;
            });

            CollectionAssert.AreEqual(new[] { 4, 3, 2 }, requestedBounds);
            CollectionAssert.AreEqual(new[] { "B", "C", "D", "A" }, list);
        }

        [TestCase(-1)]
        [TestCase(4)]
        public void Shuffle_IndexSourceOutOfRange_ThrowsWithoutHidingTheBug(int badIndex)
        {
            List<int> list = new() { 1, 2, 3, 4 };

            Assert.Throws<ArgumentOutOfRangeException>(() => list.Shuffle(max => badIndex));
        }

        [Test]
        public void Shuffle_NullSources_Throw()
        {
            List<int> list = new() { 1, 2 };

            Assert.Throws<ArgumentNullException>(() => list.Shuffle((SystemRandom)null));
            Assert.Throws<ArgumentNullException>(() => list.Shuffle((Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((List<int>)null).Shuffle(new SystemRandom(1)));
        }

        [Test]
        public void GetRandomElement_SeededSystemRandom_IsDeterministic()
        {
            string[] items = { "a", "b", "c", "d", "e" };
            SystemRandom first = new(99);
            SystemRandom second = new(99);

            for (int i = 0; i < 20; i++)
            {
                Assert.That(items.GetRandomElement(first), Is.EqualTo(items.GetRandomElement(second)));
            }

            Assert.Throws<ArgumentNullException>(() => items.GetRandomElement(null));
            Assert.Throws<ArgumentException>(() => new List<int>().GetRandomIndex(new SystemRandom(1)));
        }

        [Test]
        public void GetRandomWeightedIndex_SeededSystemRandom_IsDeterministicAndSkipsZeroWeights()
        {
            float[] weights = { 0f, 2f, 0f, 1f, 0f };
            SystemRandom first = new(5);
            SystemRandom second = new(5);

            for (int i = 0; i < 200; i++)
            {
                int index = weights.GetRandomWeightedIndex(first);
                Assert.That(index, Is.EqualTo(weights.GetRandomWeightedIndex(second)));
                Assert.That(index, Is.EqualTo(1).Or.EqualTo(3));
            }

            Assert.That(new float[0].GetRandomWeightedIndex(new SystemRandom(1)), Is.EqualTo(-1));
            Assert.Throws<ArgumentNullException>(() => weights.GetRandomWeightedIndex(null));
        }

        [Test]
        public void GetRandomWeightedIndex_WarmListCall_DoesNotAllocate()
        {
            List<float> weights = new() { 1f, 2f, 3f };
            weights.GetRandomWeightedIndex();

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 1000; i++)
            {
                weights.GetRandomWeightedIndex();
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
        }

        [Test]
        public void GetRandomEnumValue_ReturnsDeclaredValuesWithoutAllocating()
        {
            FoundationTestEnum[] declared = { FoundationTestEnum.First, FoundationTestEnum.Second, FoundationTestEnum.Third };
            RandomExtensions.GetRandomEnumValue<FoundationTestEnum>();

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            FoundationTestEnum last = FoundationTestEnum.First;
            for (int i = 0; i < 200; i++)
            {
                last = RandomExtensions.GetRandomEnumValue<FoundationTestEnum>();
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
            CollectionAssert.Contains(declared, last);
        }

        #endregion

        #region StringExtension

        [Test]
        public void ToFloat_InvariantDecimalPoint_ParsesUnderCommaDecimalCulture()
        {
            RunWithCulture("ru-RU", () =>
            {
                Assert.That("1.5".ToFloat(), Is.EqualTo(1.5f), "Data strings use '.' regardless of the OS locale.");
                Assert.That("1,5".ToFloat(), Is.EqualTo(1.5f), "Locale-formatted input still parses as a fallback.");
                Assert.That("-0.25".ToFloat(), Is.EqualTo(-0.25f));
                Assert.That("abc".ToFloat(7f), Is.EqualTo(7f));
                Assert.That("42".ToInt(), Is.EqualTo(42));
                Assert.That("x".ToInt(-1), Is.EqualTo(-1));
            });
        }

        /// <summary>
        ///     Pins the accepted words and their case-insensitivity under tr-TR. The words contain no 'I', so the
        ///     old culture-dependent ToLower() also passed this; it is a contract test, not a Turkish-I regression.
        /// </summary>
        [Test]
        public void ToBool_AcceptsTrueYesAndOneCaseInsensitivelyUnderTurkishCulture()
        {
            RunWithCulture("tr-TR", () =>
            {
                Assert.That("TRUE".ToBool(), Is.True);
                Assert.That("Yes".ToBool(), Is.True);
                Assert.That("1".ToBool(), Is.True);
                Assert.That("no".ToBool(), Is.False);
                Assert.That(" true".ToBool(), Is.False);
                Assert.That(((string)null).ToBool(), Is.False);
            });
        }

        [Test]
        public void IsNumericAndIsNullOrEmptyAfterTrim_KeepResultsWithoutAllocating()
        {
            Assert.That("0123".IsNumeric(), Is.True);
            Assert.That("12a".IsNumeric(), Is.False);
            Assert.That(string.Empty.IsNumeric(), Is.False);
            Assert.That("   ".IsNullOrEmptyAfterTrim(), Is.True);
            Assert.That(" x ".IsNullOrEmptyAfterTrim(), Is.False);
            Assert.That(((string)null).IsNullOrEmptyAfterTrim(), Is.True);

            const string digits = "1234567890";
            const string padded = "   value   ";
            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 1000; i++)
            {
                digits.IsNumeric();
                padded.IsNullOrEmptyAfterTrim();
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
        }

        #endregion

        #region EnumerableExtensions

        [Test]
        public void IsNullOrEmpty_SizedCollections_AnswerFromCountWithoutAllocating()
        {
            List<int> list = new() { 1 };
            int[] empty = new int[0];
            HashSet<int> set = new() { 2 };
            Assert.That(list.IsNullOrEmpty(), Is.False);
            Assert.That(empty.IsNullOrEmpty(), Is.True);
            Assert.That(((IEnumerable<int>)null).IsNullOrEmpty(), Is.True);
            Assert.That(Enumerable.Range(0, 0).Where(x => x > 0).IsNullOrEmpty(), Is.True);
            Assert.That(Enumerable.Range(0, 3).Where(x => x > 1).IsNullOrEmpty(), Is.False);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 1000; i++)
            {
                list.IsNullOrEmpty();
                empty.IsNullOrEmpty();
                set.IsNullOrEmpty();
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
        }

        #endregion

        #region UnityEventDelegateCache

        [Test]
        public void DelegateCache_UnsubscribeAll_DetachesTrackedSlotsAndKeepsManualOnes()
        {
            UnityEventDelegateCache cache = new();
            UnityEvent first = new();
            UnityEvent second = new();
            int firstCalls = 0;
            int secondCalls = 0;
            int manualCalls = 0;
            UnityAction manual = () => manualCalls++;
            cache.SubscribeAt(0, first, () => firstCalls++);
            cache.SubscribeAt(2, second, () => secondCalls++);
            first.AddListener(manual);
            cache.Add(manual);

            int removed = cache.UnsubscribeAll();
            first.Invoke();
            second.Invoke();

            Assert.That(removed, Is.EqualTo(2));
            Assert.That(firstCalls, Is.Zero);
            Assert.That(secondCalls, Is.Zero);
            Assert.That(manualCalls, Is.EqualTo(1), "Add() records no event, so its listener stays attached.");
            Assert.That(cache.Count, Is.Zero);
        }

        #endregion

        #region TransformExtensions

        [Test]
        public void LookAt2D_TargetOnSameXyPosition_KeepsRotation()
        {
            Transform transform = CreateObject("LookAt2D").transform;
            transform.position = new Vector3(1f, 2f, 0f);
            Quaternion initial = Quaternion.Euler(0f, 0f, 33f);
            transform.rotation = initial;

            transform.LookAt2D(new Vector3(1f, 2f, 5f));

            Assert.That(Quaternion.Angle(initial, transform.rotation), Is.LessThan(0.001f),
                "A target without an XY offset has no direction; the rotation must not snap to -90 degrees.");

            transform.LookAt2D(new Vector3(1f, 3f, 0f));
            Assert.That(transform.eulerAngles.z, Is.EqualTo(0f).Within(0.01f), "Up faces a target straight above.");
        }

        [Test]
        public void LookAt2D_NullTransform_DoesNothing()
        {
            Assert.DoesNotThrow(() => ((Transform)null).LookAt2D(Vector3.one));
        }

        [Test]
        public void GetClosest_SkipsSelfNullAndDestroyedForListsAndSequences()
        {
            Transform self = CreateObject("Self").transform;
            Transform near = CreateObject("Near").transform;
            Transform far = CreateObject("Far").transform;
            Transform destroyed = CreateObject("Destroyed").transform;
            near.position = new Vector3(2f, 0f, 0f);
            far.position = new Vector3(5f, 0f, 0f);
            destroyed.position = new Vector3(1f, 0f, 0f);
            Object.DestroyImmediate(destroyed.gameObject);
            Transform[] candidates = { self, null, destroyed, far, near };

            Assert.That(self.GetClosest(candidates), Is.SameAs(near));
            Assert.That(self.GetClosest(candidates.Where(t => true)), Is.SameAs(near));
            Assert.That(self.GetClosest(new[] { self }), Is.Null);
            Assert.That(((Transform)null).GetClosest(candidates), Is.Null);
        }

        [Test]
        public void GetClosest_WarmArrayCall_DoesNotAllocate()
        {
            Transform self = CreateObject("Self").transform;
            Transform[] candidates = { CreateObject("A").transform, CreateObject("B").transform };
            candidates[1].position = Vector3.one;
            self.GetClosest(candidates);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 1000; i++)
            {
                self.GetClosest(candidates);
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
        }

        #endregion

        #region GameObjectArrayExtensions

        [Test]
        public void FindClosest_ListAndSequence_AgreeAndSkipDestroyed()
        {
            GameObject near = CreateObject("Near");
            GameObject far = CreateObject("Far");
            GameObject destroyed = CreateObject("Destroyed");
            near.transform.position = new Vector3(0f, 3f, 0f);
            far.transform.position = new Vector3(0f, 9f, 0f);
            Object.DestroyImmediate(destroyed);
            GameObject[] objects = { far, destroyed, null, near };

            Assert.That(objects.FindClosest(Vector3.zero), Is.SameAs(near));
            Assert.That(objects.Where(go => true).FindClosest(Vector3.zero), Is.SameAs(near));
            Transform[] transforms = { far.transform, near.transform };
            Assert.That(transforms.FindClosest(Vector3.zero), Is.SameAs(near.transform));
            Assert.That(new GameObject[0].FindClosest(Vector3.zero), Is.Null);
        }

        [Test]
        public void FindClosest_FarBeyondSquaredFloatRange_StillReturnsACandidate()
        {
            GameObject remote = CreateObject("Remote");
            remote.transform.position = new Vector3(3e19f, 0f, 0f);

            Assert.That(new[] { remote }.FindClosest(Vector3.zero), Is.SameAs(remote),
                "An overflowing squared distance must not hide the only candidate.");
        }

        [Test]
        public void SetActiveAtIndex_Sequence_WalksToTheIndexWithoutCopying()
        {
            GameObject first = CreateObject("First");
            GameObject second = CreateObject("Second");
            IEnumerable<GameObject> sequence = new[] { first, second }.Where(go => true);

            GameObject result = sequence.SetActiveAtIndex(1, false);

            Assert.That(result, Is.SameAs(second));
            Assert.That(second.activeSelf, Is.False);
            Assert.That(first.activeSelf, Is.True);
            Assert.That(sequence.SetActiveAtIndex(5, false), Is.Null);
            Assert.That(((IEnumerable<GameObject>)null).SetActiveAtIndex(0, false), Is.Null);
        }

        [Test]
        public void GetAveragePosition_WarmListCall_DoesNotAllocate()
        {
            List<GameObject> objects = new() { CreateObject("A"), CreateObject("B") };
            objects[1].transform.position = new Vector3(2f, 4f, 6f);
            Assert.That(objects.GetAveragePosition(), Is.EqualTo(new Vector3(1f, 2f, 3f)));

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 1000; i++)
            {
                objects.GetAveragePosition();
                objects.FindClosest(Vector3.zero);
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
        }

        [Test]
        public void GetCombinedBoundsAndSetActiveAll_WarmListAndArrayCalls_MatchTheSequencePathWithoutAllocating()
        {
            GameObject near = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject far = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _created.Add(near);
            _created.Add(far);
            far.transform.position = new Vector3(4f, 1f, 0f);
            List<GameObject> objects = new() { near, null, far };
            GameObject[] array = { near, far };
            List<MeshRenderer> renderers = new() { near.GetComponent<MeshRenderer>(), far.GetComponent<MeshRenderer>() };

            Bounds sequence = objects.Where(gameObject => true).GetCombinedBounds();
            Assert.That(objects.GetCombinedBounds(), Is.EqualTo(sequence), "The indexed path skips null like foreach.");
            Assert.That(array.GetCombinedBounds(), Is.EqualTo(sequence));
            Assert.That(renderers.GetCombinedBounds(), Is.EqualTo(sequence));
            Assert.That(sequence.size, Is.Not.EqualTo(Vector3.zero), "Control: both cubes contribute.");
            objects.SetActiveAll(true);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 200; i++)
            {
                objects.GetCombinedBounds();
                array.GetCombinedBounds();
                renderers.GetCombinedBounds();
                objects.SetActiveAll(true);
            }

            long allocationCalls = allocationCounter.End();

            Assert.That(allocationCalls, Is.EqualTo(0L));
        }

        #endregion

        #region Shapes

        [Serializable]
        private sealed class ShapeHolder
        {
            public Circle Area;
            public Sphere Volume;
        }

        [Test]
        public void Shapes_AsSerializedFields_RoundTripThroughUnitySerialization()
        {
            ShapeHolder holder = new()
            {
                Area = new Circle(new Vector2(1f, 2f), 3f),
                Volume = new Sphere(new Vector3(4f, 5f, 6f), 7f)
            };

            ShapeHolder copy = JsonUtility.FromJson<ShapeHolder>(JsonUtility.ToJson(holder));

            Assert.That(copy.Area.center, Is.EqualTo(new Vector2(1f, 2f)));
            Assert.That(copy.Area.radius, Is.EqualTo(3f));
            Assert.That(copy.Volume.center, Is.EqualTo(new Vector3(4f, 5f, 6f)));
            Assert.That(copy.Volume.radius, Is.EqualTo(7f),
                "Circle and Sphere are [Serializable], so Inspector fields of these types keep their values.");
        }

        #endregion

        #region ScreenExtensions

        [Test]
        public void IsOnScreen_DestroyedCameraReference_FallsBackToMainCamera()
        {
            GameObject mainHost = CreateObject("Foundation Main Camera");
            mainHost.tag = "MainCamera";
            mainHost.AddComponent<Camera>();
            Camera main = Camera.main;
            Assert.That(main, Is.Not.Null, "The test needs an active main camera.");
            Vector3 visible = main.transform.position + main.transform.forward * (main.nearClipPlane + 5f);

            Camera destroyed = CreateObject("Destroyed Camera").AddComponent<Camera>();
            Object.DestroyImmediate(destroyed.gameObject);

            Assert.That(visible.IsOnScreen(destroyed), Is.True,
                "A destroyed camera reference must fall back to Camera.main like a null one.");
            Assert.That(visible.IsOnScreen(), Is.True);
            Assert.That(visible.GetClosestScreenEdgePoint(destroyed), Is.EqualTo(visible));
        }

        #endregion

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new(name);
            _created.Add(gameObject);
            return gameObject;
        }

        private static void RunWithCulture(string cultureName, Action body)
        {
            CultureInfo culture;
            try
            {
                culture = new CultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                Assert.Ignore($"Culture {cultureName} is not available on this runtime.");
                return;
            }

            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = culture;
                body();
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
