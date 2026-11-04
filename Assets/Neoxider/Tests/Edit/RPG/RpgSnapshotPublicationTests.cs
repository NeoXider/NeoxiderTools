using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Resources;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Runtime;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tests.Edit.RPG
{
    /// <summary>
    ///     Network snapshot publication count of <see cref="RpgCharacter" />: one frame of regeneration and effect
    ///     ticks, one multi-step mutation and one <see cref="RpgCharacter.DeferSnapshotPublication" /> scope each
    ///     publish one complete snapshot, not one per changed resource or stat.
    /// </summary>
    public sealed class RpgSnapshotPublicationTests
    {
        private static readonly MethodInfo TickSimulationMethod =
            typeof(RpgCharacter).GetMethod("TickSimulation", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void SimulationFrame_SeveralRegeneratingResourcesAndStatusTicks_PublishOneCompleteSnapshot()
        {
            RpgCharacter character = CreateCharacter();
            character.ApplyStatus(CreateStatus(1f, 0.02f));
            Audit20261023NeoRpgSnapshotProbe probe = AttachProbe(character);
            int hpChanges = 0;
            int manaChanges = 0;
            character.OnResourceChangedEvent.AddListener((string id, float value) =>
            {
                if (id == RpgResourceId.Hp)
                {
                    hpChanges++;
                }
                else if (id == RpgResourceId.Mana)
                {
                    manaChanges++;
                }
            });

            Tick(character, 0.1f);

            Assert.That(manaChanges, Is.GreaterThan(1), "the per-tick mana regen must deliver several ticks this frame");
            Assert.That(hpChanges, Is.GreaterThan(1), "HP regen and the status damage ticks must both change HP");
            Assert.That(probe.Snapshots.Count, Is.EqualTo(1),
                "one frame used to capture and serialize the profile once per changed resource");
            AssertSnapshotMatches(probe.Snapshots[0], character);

            Tick(character, 0.1f);

            Assert.That(probe.Snapshots.Count, Is.EqualTo(2), "every simulated frame that changes state publishes once");
            AssertSnapshotMatches(probe.Snapshots[1], character);
        }

        [Test]
        public void SimulationFrame_WithoutChanges_PublishesNothing()
        {
            RpgCharacter character = CreateCharacter(100f, 100f);
            Audit20261023NeoRpgSnapshotProbe probe = AttachProbe(character);

            Tick(character, 0.1f);

            Assert.That(character.HpValue, Is.EqualTo(100f));
            Assert.That(character.ManaValue, Is.EqualTo(100f));
            Assert.That(probe.Snapshots, Is.Empty, "a full character with no effects has nothing to publish");
        }

        [Test]
        public void SimulationFrame_ListenerExceptionStillClosesTheFrameScope()
        {
            RpgCharacter character = CreateCharacter();
            Audit20261023NeoRpgSnapshotProbe probe = AttachProbe(character);
            bool thrown = false;
            Action<RpgDamageInfo, float> throwOnce = (RpgDamageInfo info, float applied) =>
            {
                if (thrown)
                {
                    return;
                }

                thrown = true;
                throw new InvalidOperationException("listener failure");
            };
            character.DamageApplied += throwOnce;
            character.ApplyStatus(CreateStatus(1f, 0.02f));

            Assert.Throws<TargetInvocationException>(() => Tick(character, 0.1f));
            Assert.That(thrown, Is.True);
            int afterFailure = probe.Snapshots.Count;

            character.Damage(1f);

            Assert.That(probe.Snapshots.Count, Is.EqualTo(afterFailure + 1),
                "a failed frame must not leave publication deferred forever");
        }

        [Test]
        public void LethalHit_PublishesHpAndDeathInOneSnapshot()
        {
            RpgCharacter character = CreateCharacter(100f, 100f);
            Audit20261023NeoRpgSnapshotProbe probe = AttachProbe(character);

            character.Damage(1000f);

            Assert.That(character.IsDead, Is.True);
            Assert.That(probe.Snapshots.Count, Is.EqualTo(1), "HP and death used to publish one snapshot each");
            Assert.That(probe.Snapshots[0].IsDead, Is.True);
            AssertSnapshotMatches(probe.Snapshots[0], character);
        }

        [Test]
        public void RestoreAll_PublishesOneSnapshotForEveryRefilledResource()
        {
            RpgCharacter character = CreateCharacter(10f, 10f);
            Audit20261023NeoRpgSnapshotProbe probe = AttachProbe(character);

            character.Restore();

            Assert.That(character.HpValue, Is.EqualTo(100f));
            Assert.That(character.ManaValue, Is.EqualTo(100f));
            Assert.That(probe.Snapshots.Count, Is.EqualTo(1));
            AssertSnapshotMatches(probe.Snapshots[0], character);
        }

        [Test]
        public void DeferredScope_NestedScopesPublishTheFinalStateOnceWhenTheOutermostEnds()
        {
            RpgCharacter character = CreateCharacter(100f, 100f);
            Audit20261023NeoRpgSnapshotProbe probe = AttachProbe(character);

            using (character.DeferSnapshotPublication())
            {
                character.Damage(30f);
                using (character.DeferSnapshotPublication())
                {
                    character.AddXp(12f);
                    character.Spend(RpgResourceId.Mana, 5f);
                }

                Assert.That(probe.Snapshots, Is.Empty, "an inner scope must not publish while an outer one is open");
                character.Heal(10f);
                Assert.That(probe.Snapshots, Is.Empty);
            }

            Assert.That(probe.Snapshots.Count, Is.EqualTo(1));
            Assert.That(probe.Snapshots[0].Xp, Is.EqualTo(12f));
            AssertSnapshotMatches(probe.Snapshots[0], character);
            Assert.That(character.HpValue, Is.EqualTo(80f));
        }

        [Test]
        public void DeferredScope_WithoutChanges_PublishesNothing()
        {
            RpgCharacter character = CreateCharacter(100f, 100f);
            Audit20261023NeoRpgSnapshotProbe probe = AttachProbe(character);

            using (character.DeferSnapshotPublication())
            {
                character.Damage(0f);
            }

            Assert.That(probe.Snapshots, Is.Empty);
        }

        [Test]
        public void DeferredScope_DisposedTwiceOrDefault_DoesNotDisablePublication()
        {
            RpgCharacter character = CreateCharacter(100f, 100f);
            Audit20261023NeoRpgSnapshotProbe probe = AttachProbe(character);
            RpgCharacter.SnapshotPublicationScope scope = character.DeferSnapshotPublication();
            scope.Dispose();
            scope.Dispose();
            default(RpgCharacter.SnapshotPublicationScope).Dispose();

            character.Damage(5f);

            Assert.That(probe.Snapshots.Count, Is.EqualTo(1), "publication must run immediately again");
        }

        [Test]
        public void SimulationFrame_WarmTickWithAdapter_PublishesOncePerFrameWithoutAllocating()
        {
            RpgCharacter character = CreateCharacter(1000f, 1f, 1000000f);
            character.ApplyStatus(CreateStatus(1f, 0.02f));
            CountingAdapter adapter = new();
            Set(character, "_networkAdapter", adapter);
            Action<float> tick = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character,
                TickSimulationMethod);
            tick(0.05f);
            tick(0.05f);
            int before = adapter.Notifications;
            float hpBefore = character.HpValue;
            float manaBefore = character.ManaValue;
            Action frames = () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    tick(0.05f);
                }
            };

            long allocations = GcAlloc.TryCount(frames, out string notAdmitted);

            Assert.That(adapter.Notifications - before, Is.EqualTo(50), "exactly one publication per changed frame");
            Assert.That(character.ManaValue, Is.GreaterThan(manaBefore), "the per-tick regen must run");
            Assert.That(character.HpValue, Is.Not.EqualTo(hpBefore), "regen and status damage must change HP");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocations, Is.Zero, "a warm simulation frame with a publication scope must not allocate");
        }

        private static void Tick(RpgCharacter character, float deltaTime)
        {
            Assert.That(TickSimulationMethod, Is.Not.Null, "RpgCharacter.TickSimulation(float) seam is missing");
            TickSimulationMethod.Invoke(character, new object[] { deltaTime });
        }

        private static void AssertSnapshotMatches(RpgCharacterProfileData snapshot, RpgCharacter character)
        {
            for (int i = 0; i < snapshot.Resources.Count; i++)
            {
                RpgResourceSaveEntry row = snapshot.Resources[i];
                Assert.That(row.Current, Is.EqualTo(character.GetResource(row.Id)),
                    $"snapshot row {row.Id} must hold the final value, not an intermediate one");
            }

            Assert.That(snapshot.IsDead, Is.EqualTo(character.IsDead));
        }

        private Audit20261023NeoRpgSnapshotProbe AttachProbe(RpgCharacter character)
        {
            Audit20261023NeoRpgSnapshotProbe probe =
                character.gameObject.AddComponent<Audit20261023NeoRpgSnapshotProbe>();
            Set(character, "_networkAdapter", probe);
            probe.Snapshots.Clear();
            return probe;
        }

        private RpgCharacter CreateCharacter(float hp = 50f, float mana = 10f, float max = 100f)
        {
            GameObject actor = Track(new GameObject("RpgSnapshotPublicationActor"));
            RpgCharacter character = actor.AddComponent<RpgCharacter>();
            RpgCharacterTemplate template = Track(ScriptableObject.CreateInstance<RpgCharacterTemplate>());
            template.resources = new[]
            {
                new RpgResourceDefinition
                {
                    id = new RpgStatId(RpgStatPreset.Hp),
                    startMax = max,
                    startCurrent = hp,
                    restoreOnAwake = false,
                    regen = new RpgRegenDefinition { enabled = true, mode = RpgRegenMode.FlatPerSecond, value = 5f }
                },
                new RpgResourceDefinition
                {
                    id = new RpgStatId(RpgStatPreset.Mana),
                    startMax = max,
                    startCurrent = mana,
                    restoreOnAwake = false,
                    regen = new RpgRegenDefinition
                        { enabled = true, mode = RpgRegenMode.FlatPerTick, value = 0.5f, tickInterval = 0.01f }
                }
            };
            template.stats = new[] { new RpgStatDefinition { id = new RpgStatId("Wisdom"), baseValue = 10f } };
            character.ApplyTemplate(template);
            Assert.That(character.HpValue, Is.EqualTo(hp));
            Assert.That(character.ManaValue, Is.EqualTo(mana));
            return character;
        }

        private StatusEffectDefinition CreateStatus(float damagePerSecond, float interval)
        {
            StatusEffectDefinition status = Track(ScriptableObject.CreateInstance<StatusEffectDefinition>());
            Set(status, "_id", "publication-status");
            Set(status, "_duration", 1000000f);
            Set(status, "_movementSpeedMultiplier", 1f);
            Set(status, "_tickDamagePerSecond", damagePerSecond);
            Set(status, "_tickInterval", interval);
            return status;
        }

        private T Track<T>(T value) where T : Object
        {
            _objects.Add(value);
            return value;
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        /// <summary>Plain C# adapter that only counts publications, so the measured frame excludes capture cost.</summary>
        private sealed class CountingAdapter : IRpgCharacterNetworkAdapter
        {
            public int Notifications { get; private set; }

            public bool SuppressLocalSimulation => false;

            public bool TryRoute(RpgCharacterNetworkCommand command)
            {
                return false;
            }

            public void NotifyStateChanged()
            {
                Notifications++;
            }
        }
    }
}
