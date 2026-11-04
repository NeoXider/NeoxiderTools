using System;
using System.Collections;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     PlayMode self-tests of the shared allocation counter: same-frame windows on the real PlayerLoop, rejection
    ///     of a window that crosses a frame, and the Update-phase window used by PlayerLoop benchmarks.
    /// </summary>
    public sealed class GcAllocCounterPlayTests
    {
        private static object _sink;

        [TearDown]
        public void TearDown()
        {
            _sink = null;
        }

        [UnityTest]
        public IEnumerator Counter_CountsInsideAFrameAndRejectsAWindowAcrossFrames()
        {
            yield return null;
            Action three = AllocateThree;
            GcAllocCounter counter = GcAllocCounter.CreateOrIgnore();
            try
            {
                Assert.That(counter.Count(three), Is.EqualTo(3));
                counter.Begin();
                yield return null;
                TestDelegate end = () => counter.End();
                Assert.Throws<InvalidOperationException>(end,
                    "A window across a frame boundary would lose counts and must be rejected.");
                Assert.That(counter.Count(three), Is.EqualTo(3), "The counter stays usable after a rejected window.");
            }
            finally
            {
                counter.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator UpdateWindow_RecordsEachUpdatePhaseBetweenTheBeginAndEndScripts()
        {
            GameObject host = new("NeoPerfUpdateWindow");
            host.AddComponent<NeoPerfUpdateWindowBegin>();
            host.AddComponent<NeoPerfUpdateWindowEnd>();
            NeoPerfUpdateWindow window = NeoPerfUpdateWindow.Start(3);
            try
            {
                for (int frame = 0; frame < 10 && window.Count < 3; frame++)
                {
                    yield return null;
                }

                Assert.That(window.Count, Is.EqualTo(3));
                Assert.That(NeoPerfUpdateWindow.Active, Is.SameAs(window));
                for (int i = 0; i < window.Count; i++)
                {
                    Assert.That(window.Milliseconds[i], Is.GreaterThanOrEqualTo(0d));
                }

                if (!window.AllocationsAvailable)
                {
                    Assert.Ignore(GcAllocCounter.NotAdmittedMessage("Update window counter"));
                }

                for (int i = 0; i < window.Count; i++)
                {
                    Assert.That(window.AllocationCounts[i], Is.GreaterThanOrEqualTo(0L));
                }
            }
            finally
            {
                window.Stop();
                Object.Destroy(host);
            }

            Assert.That(NeoPerfUpdateWindow.Active, Is.Null);
        }

        private static void AllocateThree()
        {
            _sink = new object();
            _sink = new byte[8];
            _sink = new int[2];
        }
    }
}
