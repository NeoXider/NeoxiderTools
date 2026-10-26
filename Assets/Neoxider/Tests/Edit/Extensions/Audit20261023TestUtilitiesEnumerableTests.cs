using System;
using System.Collections.Generic;
using Neo.Extensions;
using NUnit.Framework;
using Random = UnityEngine.Random;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class Audit20261023TestUtilitiesEnumerableTests
    {
        [Test]
        public void ForEach_EnumerableReceiver_VisitsEveryItemInOrder()
        {
            IEnumerable<int> source = new[] { 2, 4, 8 };
            List<int> observed = new();

            source.ForEach(observed.Add);

            CollectionAssert.AreEqual(new[] { 2, 4, 8 }, observed);
        }

        [Test]
        public void ForEach_CallbackFailure_DisposesEnumerationAndStopsVisiting()
        {
            bool disposed = false;
            List<int> observed = new();
            InvalidOperationException expected = new("Callback failed.");

            IEnumerable<int> Enumerate()
            {
                try
                {
                    yield return 1;
                    yield return 2;
                    yield return 3;
                }
                finally
                {
                    disposed = true;
                }
            }

            IEnumerable<int> source = Enumerate();
            InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
                source.ForEach(value =>
                {
                    observed.Add(value);
                    if (value == 2)
                    {
                        throw expected;
                    }
                }));

            Assert.That(actual, Is.SameAs(expected));
            Assert.That(disposed, Is.True);
            CollectionAssert.AreEqual(new[] { 1, 2 }, observed);
        }

        [Test]
        public void ForEach_NullAction_DoesNotAcquireAnEnumerator()
        {
            int enumerations = 0;

            IEnumerable<int> Enumerate()
            {
                enumerations++;
                yield return 1;
            }

            IEnumerable<int> source = Enumerate();
            source.ForEach(null);

            Assert.That(enumerations, Is.Zero);
        }

        [Test]
        public void ForEach_NullSource_DoesNotInvokeTheAction()
        {
            IEnumerable<int> source = null;
            int calls = 0;

            source.ForEach(_ => calls++);

            Assert.That(calls, Is.Zero);
        }

        [TestCase(-1, 30)]
        [TestCase(-4, 30)]
        [TestCase(int.MinValue, 20)]
        [TestCase(int.MaxValue, 20)]
        public void GetWrapped_NegativeAndExtremeIndices_ReturnTheWrappedElement(int index, int expected)
        {
            IList<int> source = new[] { 10, 20, 30 };

            Assert.That(source.GetWrapped(index), Is.EqualTo(expected));
        }

        [Test]
        public void GetSafe_NullAndExtremeIndices_PreserveTheCallerDefault()
        {
            IList<int> source = new[] { 10, 20, 30 };
            IList<int> missing = null;

            Assert.That(source.GetSafe(int.MinValue, -7), Is.EqualTo(-7));
            Assert.That(source.GetSafe(int.MaxValue, -7), Is.EqualTo(-7));
            Assert.That(missing.GetSafe(0, -7), Is.EqualTo(-7));
        }

        [Test]
        public void Shuffle_Copy_PreservesSourceOrderAndDuplicateMultiplicity()
        {
            IList<int> source = new List<int> { 1, 1, 2, 3, 5 };
            int[] original = { 1, 1, 2, 3, 5 };
            Random.State previous = Random.state;
            try
            {
                Random.InitState(61023);

                IList<int> shuffled = source.Shuffle(false);

                Assert.That(shuffled, Is.Not.SameAs(source));
                CollectionAssert.AreEqual(original, source);
                CollectionAssert.AreEquivalent(original, shuffled);
                shuffled[0] = 99;
                CollectionAssert.AreEqual(original, source);
            }
            finally
            {
                Random.state = previous;
            }
        }

        [Test]
        public void Shuffle_InPlace_ReturnsTheSourceAndPreservesDuplicateMultiplicity()
        {
            IList<int> source = new List<int> { 1, 1, 2, 3, 5 };
            int[] original = { 1, 1, 2, 3, 5 };
            Random.State previous = Random.state;
            try
            {
                Random.InitState(61023);

                IList<int> shuffled = source.Shuffle();

                Assert.That(shuffled, Is.SameAs(source));
                CollectionAssert.AreEquivalent(original, shuffled);
            }
            finally
            {
                Random.state = previous;
            }
        }
    }
}
