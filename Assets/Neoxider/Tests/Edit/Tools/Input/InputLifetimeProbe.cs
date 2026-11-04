using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Runs a warm-up on a short-lived worker thread and collects afterwards, so a weak-reference check observes
    ///     only real (static or table) ownership.
    /// </summary>
    /// <remarks>
    ///     WHY: the Boehm GC of the Unity Editor scans thread stacks and registers conservatively; a dead local of the
    ///     test thread can keep an object alive at random and make a correct lifetime oracle flaky. A finished worker
    ///     thread leaves no stack behind. The warm-up must not call Unity APIs.
    /// </remarks>
    internal static class InputLifetimeProbe
    {
        internal static WeakReference[] WarmOnFinishedThread(Func<WeakReference[]> warmUp)
        {
            WeakReference[] released = null;
            Exception failure = null;
            Thread worker = new(() =>
            {
                try
                {
                    released = warmUp();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            });
            worker.Start();
            worker.Join();
            if (failure != null)
            {
                throw new InvalidOperationException("Lifetime warm-up failed: " + failure.Message, failure);
            }

            return released;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Collect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
