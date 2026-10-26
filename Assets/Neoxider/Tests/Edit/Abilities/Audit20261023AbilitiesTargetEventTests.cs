using System;
using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    public sealed class Audit20261023AbilitiesTargetEventTests
    {
        [Test]
        public void TargetAndGlobalObservers_MergeByRegistrationOrderAndMatchTargetOnly()
        {
            AbilityEventBus bus = new AbilityEventBus();
            UnitId target = new UnitId(1);
            UnitId source = new UnitId(2);
            List<string> delivered = new List<string>();
            bus.SubscribeAny(args => delivered.Add("global-first"));
            bus.SubscribeTarget(target, args => delivered.Add("target-first"));
            bus.SubscribeAny(args => delivered.Add("global-second"));
            bus.SubscribeTarget(source, args => delivered.Add("source-target"));
            bus.SubscribeTarget(target, args => delivered.Add("target-second"));
            bus.Subscribe("ordered", args => delivered.Add("specific"));

            bus.Publish(new AbilityEventArgs("ordered", target, source));

            Assert.That(delivered, Is.EqualTo(new string[]
            {
                "specific", "global-first", "target-first", "global-second", "target-second"
            }));
            Assert.That(bus.ObserverSnapshotCount, Is.EqualTo(1L));
            Assert.That(bus.ObserverSnapshotEntryCount, Is.EqualTo(4L));
            Assert.That(bus.ObserverInvocationCount, Is.EqualTo(4L));
            delivered.Clear();
            bus.Publish(new AbilityEventArgs("another", source, target));
            Assert.That(delivered, Is.EqualTo(new string[] { "global-first", "global-second", "source-target" }));
        }

        [Test]
        public void SpecificCallbacks_ChangeTheObserverBatchOfTheCurrentPublication()
        {
            AbilityEventBus bus = new AbilityEventBus();
            UnitId target = new UnitId(1);
            List<string> delivered = new List<string>();
            Action<AbilityEventArgs> oldGlobal = args => delivered.Add("old-global");
            Action<AbilityEventArgs> oldTarget = args => delivered.Add("old-target");
            bus.SubscribeAny(oldGlobal);
            bus.SubscribeTarget(target, oldTarget);
            bus.Subscribe("capture", args =>
            {
                delivered.Add("specific");
                bus.UnsubscribeAny(oldGlobal);
                bus.UnsubscribeTarget(target, oldTarget);
                bus.SubscribeTarget(target, receipt => delivered.Add("new-target"));
                bus.SubscribeAny(receipt => delivered.Add("new-global"));
            });

            bus.Publish(new AbilityEventArgs("capture", target, UnitId.None));

            Assert.That(delivered, Is.EqualTo(new string[] { "specific", "new-target", "new-global" }));
            Assert.That(bus.ObserverSnapshotEntryCount, Is.EqualTo(2L));
        }

        [Test]
        public void ObserverUnsubscribeAndNestedPublish_KeepEachCapturedBatchIntact()
        {
            AbilityEventBus bus = new AbilityEventBus();
            UnitId target = new UnitId(1);
            List<string> delivered = new List<string>();
            Action<AbilityEventArgs> oldTarget = args => delivered.Add("target:" + args.EventId);
            Action<AbilityEventArgs> oldGlobal = args => delivered.Add("last:" + args.EventId);
            bus.SubscribeAny(args =>
            {
                delivered.Add("first:" + args.EventId);
                if (args.EventId == "outer")
                {
                    bus.UnsubscribeTarget(target, oldTarget);
                    bus.UnsubscribeAny(oldGlobal);
                    bus.SubscribeTarget(target, receipt => delivered.Add("replacement:" + receipt.EventId));
                    bus.Publish(new AbilityEventArgs("nested", target, UnitId.None));
                }
            });
            bus.SubscribeTarget(target, oldTarget);
            bus.SubscribeAny(oldGlobal);

            bus.Publish(new AbilityEventArgs("outer", target, UnitId.None));
            bus.Publish(new AbilityEventArgs("later", target, UnitId.None));

            Assert.That(delivered, Is.EqualTo(new string[]
            {
                "first:outer", "first:nested", "replacement:nested", "target:outer", "last:outer",
                "first:later", "replacement:later"
            }));
            Assert.That(bus.ObserverSnapshotCount, Is.EqualTo(3L));
            Assert.That(bus.ObserverSnapshotEntryCount, Is.EqualTo(7L));
            Assert.That(bus.ObserverInvocationCount, Is.EqualTo(7L));
        }

        [Test]
        public void InternalNestedClear_CannotEraseCapturedTargetOrGlobalObservers()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit attacker = AbilityTestSupport.CreateUnit(system, team: 1, health: 100f);
            AbilityUnit defender = AbilityTestSupport.CreateUnit(system, team: 2, health: 100f);
            ModifierBlueprint thorns = AbilityTestSupport.Modifier("target_snapshot_thorns");
            thorns.EventReactions.Add(new ModifierEventReaction
            {
                EventId = AbilityEvents.TakeDamage, TargetEventSource = true,
                Effects = AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(5f))
            });
            system.Modifiers.Apply(thorns, defender.Id, defender.Id);
            List<string> delivered = new List<string>();
            Action<AbilityEventArgs> capturedTarget = args =>
            {
                if (args.EventId == AbilityEvents.TakeDamage)
                {
                    delivered.Add("target");
                }
            };
            system.Events.Subscribe(AbilityEvents.TakeDamage, args =>
            {
                if (args.Target == attacker.Id)
                {
                    delivered.Add("internal-nested-specific");
                    system.Events.UnsubscribeTarget(defender.Id, capturedTarget);
                    system.Events.Clear();
                    system.Events.SubscribeTarget(defender.Id, receipt =>
                    {
                        if (receipt.EventId == AbilityEvents.TakeDamage)
                        {
                            delivered.Add("post-internal");
                        }
                    });
                }
            });
            system.Events.SubscribeAny(args =>
            {
                if (args.EventId == AbilityEvents.TakeDamage && args.Target == defender.Id)
                {
                    delivered.Add("global-first");
                }
            });
            system.Events.SubscribeTarget(defender.Id, capturedTarget);
            system.Events.SubscribeAny(args =>
            {
                if (args.EventId == AbilityEvents.TakeDamage && args.Target == defender.Id)
                {
                    delivered.Add("global-last");
                }
            });

            DamageService.ApplyDamage(system, attacker.Id, defender.Id, 10f, AbilityDamageTypes.Pure);

            Assert.That(delivered, Is.EqualTo(new string[]
            {
                "internal-nested-specific", "global-first", "target", "global-last"
            }));
            DamageService.ApplyDamage(system, attacker.Id, defender.Id, 10f, AbilityDamageTypes.Pure);
            Assert.That(delivered[delivered.Count - 1], Is.EqualTo("post-internal"));
            Assert.That(delivered.Count, Is.EqualTo(5));
            Assert.That(defender.Health, Is.EqualTo(80f));
            Assert.That(attacker.Health, Is.EqualTo(90f), "Internal reactions still run after user Clear.");
        }

        [Test]
        public void TargetUnsubscribe_RemovesOneDuplicateAndClearKeepsLifetimeCounters()
        {
            AbilityEventBus bus = new AbilityEventBus();
            UnitId target = new UnitId(1);
            int calls = 0;
            Action<AbilityEventArgs> handler = args => calls++;
            bus.SubscribeTarget(target, handler);
            bus.SubscribeTarget(target, handler);
            bus.SubscribeAny(handler);
            bus.UnsubscribeTarget(new UnitId(2), handler);
            bus.UnsubscribeTarget(target, handler);

            bus.Publish(new AbilityEventArgs("duplicates", target, UnitId.None));
            Assert.That(calls, Is.EqualTo(2));
            bus.Clear();
            bus.Publish(new AbilityEventArgs("duplicates", target, UnitId.None));
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(bus.ObserverSnapshotCount, Is.EqualTo(1L));
            Assert.That(bus.ObserverSnapshotEntryCount, Is.EqualTo(2L));
            Assert.That(bus.ObserverInvocationCount, Is.EqualTo(2L));

            bus.SubscribeTarget(UnitId.None, handler);
            bus.Publish(new AbilityEventArgs("unowned", UnitId.None, target));
            Assert.That(calls, Is.EqualTo(3));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ObserverException_PropagatesAndStopsTheCapturedBatch(bool scopedThrows)
        {
            AbilityEventBus bus = new AbilityEventBus();
            UnitId target = new UnitId(1);
            Action<AbilityEventArgs> throwing = args => throw new InvalidOperationException("observer fault");
            if (scopedThrows)
            {
                bus.SubscribeTarget(target, throwing);
            }
            else
            {
                bus.SubscribeAny(throwing);
            }

            int laterCalls = 0;
            bus.SubscribeAny(args => laterCalls++);

            Assert.Throws<InvalidOperationException>(() =>
                bus.Publish(new AbilityEventArgs("fault", target, UnitId.None)));

            Assert.That(laterCalls, Is.Zero);
            Assert.That(bus.ObserverSnapshotCount, Is.EqualTo(1L));
            Assert.That(bus.ObserverSnapshotEntryCount, Is.EqualTo(2L));
            Assert.That(bus.ObserverInvocationCount, Is.EqualTo(1L));
        }

        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void UnitWrappers_OnlyMatchingTargetAndGlobalsEnterObserverSnapshots(int unitCount)
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour[] units = new AbilityUnitBehaviour[unitCount];
                int[] unitCalls = new int[unitCount];
                for (int i = 0; i < unitCount; i++)
                {
                    int index = i;
                    units[i] = rig.AddUnit("Scoped unit " + i, 1, Vector3.zero);
                    units[i].OnDamaged.AddListener(amount => unitCalls[index]++);
                }

                int selected = unitCount / 2;
                int globalCalls = 0;
                rig.System.Events.SubscribeAny(args =>
                {
                    Assert.That(args.Target, Is.EqualTo(units[selected].UnitId));
                    globalCalls++;
                });
                long snapshotsBefore = rig.System.Events.ObserverSnapshotCount;
                long entriesBefore = rig.System.Events.ObserverSnapshotEntryCount;
                long invocationsBefore = rig.System.Events.ObserverInvocationCount;
                const int publications = 16;

                for (int i = 0; i < publications; i++)
                {
                    rig.System.Events.Publish(new AbilityEventArgs(AbilityEvents.TakeDamage,
                        units[selected].UnitId, UnitId.None, 3f));
                }

                Assert.That(globalCalls, Is.EqualTo(publications));
                for (int i = 0; i < unitCalls.Length; i++)
                {
                    Assert.That(unitCalls[i], Is.EqualTo(i == selected ? publications : 0), "Unit " + i);
                }

                Assert.That(rig.System.Events.ObserverSnapshotCount - snapshotsBefore, Is.EqualTo(16L));
                Assert.That(rig.System.Events.ObserverSnapshotEntryCount - entriesBefore, Is.EqualTo(32L));
                Assert.That(rig.System.Events.ObserverInvocationCount - invocationsBefore, Is.EqualTo(32L));
            }
        }

        [Test]
        public void ReenabledUnit_RejectsOldTargetAfterSpecificCallbackReplacesItsRegistration()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour unit = rig.AddUnit("Rebound unit", 1, Vector3.zero);
                UnitId original = unit.UnitId;
                bool replaced = false;
                int calls = 0;
                int globals = 0;
                unit.OnDamaged.AddListener(amount => calls++);
                rig.System.Events.Subscribe(AbilityEvents.TakeDamage, args =>
                {
                    if (!replaced)
                    {
                        replaced = true;
                        AbilitySceneRig.CallPrivate(unit, "OnDisable");
                        AbilitySceneRig.CallPrivate(unit, "OnEnable");
                    }
                });
                rig.System.Events.SubscribeAny(args => globals++);
                long entriesBefore = rig.System.Events.ObserverSnapshotEntryCount;

                rig.System.Events.Publish(new AbilityEventArgs(AbilityEvents.TakeDamage, original, UnitId.None, 2f));

                Assert.That(unit.UnitId, Is.Not.EqualTo(original));
                Assert.That(calls, Is.Zero);
                Assert.That(globals, Is.EqualTo(1));
                Assert.That(rig.System.Events.ObserverSnapshotEntryCount - entriesBefore, Is.EqualTo(1L));
                rig.System.Events.Publish(new AbilityEventArgs(AbilityEvents.TakeDamage, unit.UnitId, UnitId.None, 2f));
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(globals, Is.EqualTo(2));
            }
        }

        [Test]
        public void ReenabledUnit_RejectsCapturedOldTargetAfterEarlierObserverReplacesItsRegistration()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour unit = null;
                bool replaced = false;
                int globals = 0;
                rig.System.Events.SubscribeAny(args =>
                {
                    if (args.EventId == AbilityEvents.TakeDamage)
                    {
                        globals++;
                        if (!replaced)
                        {
                            replaced = true;
                            AbilitySceneRig.CallPrivate(unit, "OnDisable");
                            AbilitySceneRig.CallPrivate(unit, "OnEnable");
                        }
                    }
                });
                unit = rig.AddUnit("Captured rebound unit", 1, Vector3.zero);
                UnitId original = unit.UnitId;
                int calls = 0;
                unit.OnDamaged.AddListener(amount => calls++);
                long entriesBefore = rig.System.Events.ObserverSnapshotEntryCount;

                rig.System.Events.Publish(new AbilityEventArgs(AbilityEvents.TakeDamage, original, UnitId.None, 2f));

                Assert.That(unit.UnitId, Is.Not.EqualTo(original));
                Assert.That(calls, Is.Zero, "The captured wrapper still checks its current domain identity.");
                Assert.That(globals, Is.EqualTo(1));
                Assert.That(rig.System.Events.ObserverSnapshotEntryCount - entriesBefore, Is.EqualTo(2L));
                rig.System.Events.Publish(new AbilityEventArgs(AbilityEvents.TakeDamage, unit.UnitId, UnitId.None, 2f));
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(globals, Is.EqualTo(2));
            }
        }
    }
}
