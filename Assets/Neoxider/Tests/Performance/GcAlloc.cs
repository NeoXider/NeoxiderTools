using System;
using NUnit.Framework;

namespace Neo.Tests.Performance
{
    /// <summary>
    ///     One-call allocation assertions on top of a fresh admitted <see cref="GcAllocCounter" />, the thin
    ///     <c>Is.Not.AllocatingGCMemory()</c>-style helper for simple cases. Every call ignores the test with a
    ///     <c>NotSupported:</c> message when the counter cannot be admitted; it never passes silently.
    /// </summary>
    /// <remarks>
    ///     Warm the measured path (first call, pools, caches) before the assertion, and build the delegate outside it:
    ///     only <paramref name="body" /> invocations are inside the window. For several windows in one test, or to finish
    ///     functional asserts before ignoring, use <see cref="GcAllocCounter" /> directly.
    /// </remarks>
    public static class GcAlloc
    {
        /// <summary>Returns the GC.Alloc calls <paramref name="body" /> makes on the calling thread.</summary>
        public static long Count(Action body)
        {
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                return counter.Count(body);
            }
        }

        /// <summary>
        ///     Runs <paramref name="body" /> once. With an admitted counter returns its GC.Alloc calls and a null
        ///     <paramref name="notAdmitted" />; otherwise runs it unmeasured and returns -1 with the reason, so the test
        ///     can finish its functional asserts and then call <see cref="IgnoreIfNotAdmitted" />.
        /// </summary>
        public static long TryCount(Action body, out string notAdmitted)
        {
            if (!GcAllocCounter.TryCreate(out GcAllocCounter counter, out notAdmitted))
            {
                body();
                return -1;
            }

            using (counter)
            {
                return counter.Count(body);
            }
        }

        /// <summary>Ignores the calling test when <see cref="TryCount" /> reported a reason; does nothing otherwise.</summary>
        public static void IgnoreIfNotAdmitted(string notAdmitted)
        {
            if (notAdmitted != null)
            {
                Assert.Ignore(GcAllocCounter.NotAdmittedMessage(notAdmitted) + " Functional asserts passed.");
            }
        }

        /// <summary>Asserts that <paramref name="body" /> makes no GC allocation.</summary>
        public static void AssertNone(Action body, string message)
        {
            AssertAtMost(0, body, message);
        }

        /// <summary>
        ///     Asserts that <paramref name="body" /> makes at most <paramref name="maxCalls" /> GC.Alloc calls, for paths
        ///     that may allocate a bounded amount (for example once per call while not once per element).
        /// </summary>
        public static void AssertAtMost(long maxCalls, Action body, string message)
        {
            long calls = Count(body);
            Assert.That(calls, Is.LessThanOrEqualTo(maxCalls), message);
        }
    }
}
