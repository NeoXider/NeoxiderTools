using System;
using System.Collections.Generic;
using Neo.Core.Resources;
using Neo.Tests.Performance;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     <see cref="ResourcePoolModel.BeginNotificationBatch" /> / <see cref="ResourcePoolModel.EndNotificationBatch" />:
    ///     mutations apply at once, notifications arrive in order after the outermost end, and no listener can run
    ///     between payments of one batch.
    /// </summary>
    [TestFixture]
    public class ResourcePoolNotificationBatchTests
    {
        [Test]
        public void Batch_DefersChangedAndDepletedInOrderWithEachOperationsValues()
        {
            ResourcePoolModel model = CreateModel(20f, 10f);
            List<string> events = Record(model);

            model.BeginNotificationBatch();
            Assert.That(model.IsBatchingNotifications, Is.True);
            Assert.That(model.TrySpend("mana", 5f, out _), Is.True);
            Assert.That(model.TrySpend("hp", 10f, out _), Is.True);
            Assert.That(model.Decrease("mana", 3f), Is.EqualTo(3f));
            Assert.That(model.GetCurrent("mana"), Is.EqualTo(12f), "Mutations apply immediately inside a batch.");
            Assert.That(events, Is.Empty, "Nothing is published before the batch ends.");

            model.EndNotificationBatch();

            Assert.That(model.IsBatchingNotifications, Is.False);
            CollectionAssert.AreEqual(
                new[] { "changed mana 15/20", "changed hp 0/10", "depleted hp", "changed mana 12/20" }, events);
        }

        [Test]
        public void Batch_ListenerCannotInvalidateALaterPaymentOfTheSameBatch()
        {
            ResourcePoolModel withoutBatch = CreateModel(20f, 10f);
            DrainManaWhenHpChanges(withoutBatch);
            Assert.That(withoutBatch.TrySpend("hp", 5f, out _), Is.True);
            Assert.That(withoutBatch.TrySpend("mana", 10f, out string failReason), Is.False,
                "Control: without a batch the hp listener drains mana before the second payment.");
            Assert.That(failReason, Is.Not.Null);

            ResourcePoolModel batched = CreateModel(20f, 10f);
            DrainManaWhenHpChanges(batched);
            List<string> events = Record(batched);
            Assert.That(batched.CanSpend("hp", 5f, out _) && batched.CanSpend("mana", 10f, out _), Is.True);
            bool paidHp;
            bool paidMana;
            batched.BeginNotificationBatch();
            try
            {
                paidHp = batched.TrySpend("hp", 5f, out _);
                paidMana = batched.TrySpend("mana", 10f, out _);
            }
            finally
            {
                batched.EndNotificationBatch();
            }

            Assert.That(paidHp, Is.True);
            Assert.That(paidMana, Is.True, "Both validated costs are paid before any listener runs.");
            Assert.That(batched.GetCurrent("hp"), Is.EqualTo(5f));
            Assert.That(batched.GetCurrent("mana"), Is.Zero, "The listener still ran afterwards and drained the rest.");
            CollectionAssert.AreEqual(
                new[] { "changed hp 5/10", "changed mana 10/20", "changed mana 0/20", "depleted mana" }, events,
                "The listener's drain is queued behind the batch's own mana notification, so observers end on the " +
                "live value and see the depletion.");
        }

        [Test]
        public void ListenerMutationDuringDelivery_IsQueuedBehindOlderNotificationsOfTheSamePool()
        {
            ResourcePoolModel model = CreateModel(20f, 10f);
            List<string> events = Record(model);
            bool refilled = false;
            model.OnResourceDepleted += id =>
            {
                if (!refilled && id == "hp")
                {
                    refilled = true;
                    model.SetCurrent("hp", 4f);
                }
            };

            model.BeginNotificationBatch();
            model.Decrease("hp", 10f);
            model.Decrease("mana", 1f);
            model.EndNotificationBatch();

            CollectionAssert.AreEqual(
                new[] { "changed hp 0/10", "depleted hp", "changed mana 19/20", "changed hp 4/10" }, events);
            Assert.That(model.GetCurrent("hp"), Is.EqualTo(4f));
        }

        [Test]
        public void NestedBatches_DeliverOnlyAtTheOutermostEnd_AndUnmatchedEndIsIgnored()
        {
            ResourcePoolModel model = CreateModel(20f, 10f);
            List<string> events = Record(model);

            model.EndNotificationBatch();
            model.BeginNotificationBatch();
            model.BeginNotificationBatch();
            model.Decrease("hp", 1f);
            model.EndNotificationBatch();
            model.Decrease("hp", 3f);

            Assert.That(events, Is.Empty, "The inner end must not deliver while the outer batch is open.");
            Assert.That(model.IsBatchingNotifications, Is.True);

            model.EndNotificationBatch();
            model.EndNotificationBatch();

            CollectionAssert.AreEqual(new[] { "changed hp 9/10", "changed hp 6/10" }, events,
                "Each operation is published once, in order, with its own value.");
            Assert.That(model.IsBatchingNotifications, Is.False);
        }

        [Test]
        public void ListenerBatchDuringDelivery_AppendsInOrderWithoutADuplicateDelivery()
        {
            ResourcePoolModel model = CreateModel(20f, 10f);
            List<string> events = Record(model);
            bool nested = false;
            model.OnResourceChanged += (id, current, max) =>
            {
                if (nested || id != "hp")
                {
                    return;
                }

                nested = true;
                model.BeginNotificationBatch();
                model.Decrease("mana", 1f);
                model.EndNotificationBatch();
            };

            model.BeginNotificationBatch();
            model.Decrease("hp", 2f);
            model.Decrease("mana", 2f);
            model.EndNotificationBatch();

            CollectionAssert.AreEqual(new[] { "changed hp 8/10", "changed mana 18/20", "changed mana 17/20" }, events,
                "The listener's own batch is queued behind the outer notifications and delivered once.");
            Assert.That(model.GetCurrent("mana"), Is.EqualTo(17f));
        }

        [Test]
        public void ThrowingListener_StillDeliversTheRestOfTheQueue_ThenRethrowsTheFirstFailure()
        {
            ResourcePoolModel model = CreateModel(20f, 10f);
            int failures = 0;
            Action<string, float, float> throwOnMana = (id, current, max) =>
            {
                if (id == "mana")
                {
                    failures++;
                    throw new InvalidOperationException("batch listener failure " + failures);
                }
            };
            model.OnResourceChanged += throwOnMana;
            List<string> events = Record(model);

            model.BeginNotificationBatch();
            Assert.That(model.TrySpend("mana", 5f, out _), Is.True);
            Assert.That(model.TrySpend("hp", 10f, out _), Is.True);
            Assert.That(model.Decrease("mana", 1f), Is.EqualTo(1f));
            InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(model.EndNotificationBatch);

            Assert.That(thrown.Message, Is.EqualTo("batch listener failure 1"), "The first failure is rethrown.");
            Assert.That(failures, Is.EqualTo(2), "Every queued mana notification reached the throwing listener.");
            CollectionAssert.AreEqual(new[] { "changed hp 0/10", "depleted hp" }, events,
                "The later payment's change and depletion are still delivered: the operations were already applied.");
            Assert.That(model.IsBatchingNotifications, Is.False);

            model.OnResourceChanged -= throwOnMana;
            model.Decrease("mana", 1f);
            Assert.That(events[events.Count - 1], Is.EqualTo("changed mana 13/20"),
                "Later operations notify immediately again.");
        }

        [Test]
        public void WarmBatch_DoesNotAllocate()
        {
            ResourcePoolModel model = CreateModel(1e6f, 1e6f);
            int changes = 0;
            model.OnResourceChanged += (id, current, max) => changes++;
            Action payTwoCosts = () =>
            {
                model.BeginNotificationBatch();
                model.TrySpend("mana", 1f, out _);
                model.TrySpend("hp", 1f, out _);
                model.EndNotificationBatch();
            };
            payTwoCosts();
            Action batches = () =>
            {
                for (int i = 0; i < 500; i++)
                {
                    payTwoCosts();
                }
            };

            GcAlloc.AssertNone(batches, "The queue keeps its capacity and stores value-type notifications.");

            Assert.That(changes, Is.EqualTo(2 * 501));
        }

        private static ResourcePoolModel CreateModel(float mana, float hp)
        {
            ResourcePoolModel model = new();
            model.AddPool("mana", Pool(mana));
            model.AddPool("hp", Pool(hp));
            return model;
        }

        private static ResourcePoolEntry Pool(float value)
        {
            return new ResourcePoolEntry
            {
                Current = value,
                Max = value,
                MaxDecreaseAmount = -1f,
                MaxIncreaseAmount = -1f
            };
        }

        private static List<string> Record(ResourcePoolModel model)
        {
            List<string> events = new();
            model.OnResourceChanged += (id, current, max) => events.Add($"changed {id} {current}/{max}");
            model.OnResourceDepleted += id => events.Add($"depleted {id}");
            return events;
        }

        private static void DrainManaWhenHpChanges(ResourcePoolModel model)
        {
            model.OnResourceChanged += (id, current, max) =>
            {
                if (id == "hp")
                {
                    model.Decrease("mana", model.GetCurrent("mana"));
                }
            };
        }
    }
}
