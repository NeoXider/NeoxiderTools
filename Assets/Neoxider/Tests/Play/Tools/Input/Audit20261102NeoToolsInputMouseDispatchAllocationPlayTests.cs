using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Warmed <see cref="MouseInputManager" /> dispatch above polling: picking, cached subscriber snapshots and
    ///     struct event data allocate nothing for Press/Hold/Release/Click with several <c>Action</c> and <c>in</c>
    ///     subscribers, while every handler still observes each event on the picked collider.
    /// </summary>
    public sealed class Audit20261102NeoToolsInputMouseDispatchAllocationPlayTests
    {
        private const int Iterations = 1024;
        private const int Subscribers = 8;

        [Test]
        public void WarmPressHoldReleaseClick_EightSubscribersPerEventAllocateNothing()
        {
            using (InputMouseManagerRig rig = new(Subscribers, true))
            {
                Assert.That(RunCycles(rig, 4), Is.EqualTo(4), "Warm-up must dispatch every event to every handler.");
                using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                {
                    counter.Begin();
                    int correct = RunCycles(rig, Iterations);
                    long allocationCount = counter.End();
                    Assert.That(correct, Is.EqualTo(Iterations),
                        "Each cycle delivers one Press, two Holds, one Release and one Click per handler on the target.");
                    Assert.That(rig.Misses, Is.Zero);
                    Assert.That(allocationCount, Is.Zero,
                        "Exactly zero native GC.Alloc calls for warmed picking and dispatch to 8+8 handlers per event.");
                }
            }
        }

        [Test]
        public void SubscriptionChange_RebuildsSnapshotOnceThenStaysAllocationFree()
        {
            using (InputMouseManagerRig rig = new(1, true))
            {
                rig.Press();
                Assert.That(rig.Hold(), Is.True);
                int extraHolds = 0;
                MouseInputManager.MouseEventHandler extra = (in MouseInputManager.MouseEventData data) => extraHolds++;
                rig.Manager.OnHoldIn += extra;
                try
                {
                    Assert.That(rig.Hold(), Is.True, "The first dispatch after a change rebuilds the snapshot.");
                    Assert.That(extraHolds, Is.EqualTo(1), "A new subscriber is visible on the next dispatch.");
                    using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
                    {
                        counter.Begin();
                        int correct = 0;
                        for (int i = 0; i < Iterations; i++)
                        {
                            if (rig.Hold())
                            {
                                correct++;
                            }
                        }

                        long allocationCount = counter.End();
                        Assert.That(correct, Is.EqualTo(Iterations), "The original handlers stay live.");
                        Assert.That(extraHolds, Is.EqualTo(Iterations + 1), "The added handler stays live.");
                        Assert.That(allocationCount, Is.Zero,
                            "A stable subscription reuses its snapshot instead of rebuilding it per dispatch.");
                    }
                }
                finally
                {
                    rig.Manager.OnHoldIn -= extra;
                }
            }
        }

        private static int RunCycles(InputMouseManagerRig rig, int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                if (rig.Cycle())
                {
                    correct++;
                }
            }

            return correct;
        }
    }
}
