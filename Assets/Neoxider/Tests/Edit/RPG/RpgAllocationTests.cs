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
    ///     Zero-GC guards for the RPG hot paths: per-frame character ticking, regeneration, damage, multiplier
    ///     queries and target selection. Each test warms the path up, then asserts no managed allocation.
    /// </summary>
    public sealed class RpgAllocationTests
    {
        private const int Iterations = 200;
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
        public void ResourceService_RegenTickAndMutations_DoNotAllocate()
        {
            RpgCharacterResourceService service = new();
            RpgResourceDefinition mana = new()
            {
                id = new RpgStatId("Mana"),
                startCurrent = 0f,
                startMax = 1000000f,
                restoreOnAwake = false,
                regen = new RpgRegenDefinition { enabled = true, mode = RpgRegenMode.FlatPerSecond, value = 1f }
            };
            RpgResourceDefinition rage = new()
            {
                id = new RpgStatId("Rage"),
                startCurrent = 0f,
                startMax = 1000000f,
                restoreOnAwake = false,
                regen = new RpgRegenDefinition
                    { enabled = true, mode = RpgRegenMode.FlatPerTick, value = 1f, tickInterval = 0.01f }
            };
            int notifications = 0;
            Action<string, RpgResourceRuntime> listener = (string id, RpgResourceRuntime resource) => notifications++;
            service.ResourceChanged += listener;
            service.Build(new[] { mana, rage });
            Func<RpgResourceRuntime, float> max = (RpgResourceRuntime resource) => resource.BaseMax;
            Func<RpgResourceRuntime, float> regen = (RpgResourceRuntime resource) => resource.Definition.regen.value * 100f;
            service.RefreshDerived(max, regen, false);
            WarmUp(() => Exercise(service));

            long allocated = Measure(() => Exercise(service));

            Assert.That(notifications, Is.GreaterThan(Iterations), "The regen path must actually run.");
            Assert.That(allocated, Is.Zero, "Guarded resource writes used to allocate a closure per write.");
        }

        [Test]
        public void CharacterUpdate_WithActiveEffects_DoesNotAllocate()
        {
            RpgCharacter character = CreateCharacter(1000f);
            character.ApplyBuff(CreateBuff());
            StatusEffectDefinition status = CreateStatus(0f);
            // WHY: 0.1 HP per tick stays visible in float at 1000 HP; the default tick amount rounds away.
            Set(status, "_tickDamagePerSecond", 10f);
            character.ApplyStatus(status);
            // WHY: Update reads Time.deltaTime, which an Edit Mode test does not advance; the seam takes a fixed
            // step so the regen, effect and status-damage work is really measured.
            MethodInfo simulate = typeof(RpgCharacter).GetMethod("TickSimulation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(simulate, Is.Not.Null);
            Action<float> step = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character, simulate);
            Action tick = () => step(0.016f);
            WarmUp(tick);
            float hpBefore = character.HpValue;

            long allocated = Measure(tick);

            Assert.That(character.HpValue, Is.LessThan(hpBefore), "status tick damage must run inside the window");
            Assert.That(allocated, Is.Zero, "Update used to allocate three method-group delegates per frame.");
        }

        [Test]
        public void CharacterDamageAndMultiplierQueries_DoNotAllocate()
        {
            RpgCharacter character = CreateCharacter(1000f);
            character.ApplyBuff(CreateBuff());
            character.ApplyStatus(CreateStatus(0.5f));
            float sink = 0f;
            Action work = () =>
            {
                sink += character.Damage(0.001f);
                sink += character.GetMovementSpeedMultiplier();
                sink += character.GetIncomingDamageMultiplier("Fire");
                sink += character.GetOutgoingDamageMultiplier();
                sink += character.CanPerformActions ? 1f : 0f;
            };
            WarmUp(work);

            long allocated = Measure(work);

            Assert.That(sink, Is.GreaterThan(0f));
            Assert.That(character.HpValue, Is.LessThan(1000f), "Damage must reach HP for the path to be measured.");
            Assert.That(character.GetMovementSpeedMultiplier(), Is.EqualTo(0.5f));
            Assert.That(allocated, Is.Zero,
                "Damage allocated a guard closure and movement queries boxed the status enumerator.");
        }

        [Test]
        public void TargetSelector_WarmSelection_DoesNotAllocate()
        {
            RpgCharacter source = CreateCharacter();
            source.transform.position = new Vector3(40000f, 0f, 0f);
            for (int i = 0; i < 20; i++)
            {
                RpgCharacter enemy = CreateCharacter();
                enemy.transform.position = source.transform.position + new Vector3(1f + i * 0.3f, 0f, 0f);
                enemy.gameObject.AddComponent<SphereCollider>().radius = 0.2f;
            }

            RpgTargetSelector selector = source.gameObject.AddComponent<RpgTargetSelector>();
            Physics.SyncTransforms();
            GameObject first = selector.SelectTarget();
            Assert.That(first, Is.Not.Null);
            Action select = () => selector.SelectTarget();
            WarmUp(select);

            long allocated = Measure(select);

            Assert.That(selector.CurrentTarget, Is.EqualTo(first));
            Assert.That(allocated, Is.Zero,
                "Selection used to allocate candidate lists, a set, collider arrays and a resolver delegate.");
        }

        private static void Exercise(RpgCharacterResourceService service)
        {
            service.TickRegen(0.016f, false);
            service.Increase("Mana", 1f);
            service.Decrease("Mana", 0.5f);
            service.Spend("Rage", 0.25f);
        }

        private static void WarmUp(Action action)
        {
            for (int i = 0; i < 5; i++)
            {
                action();
            }
        }

        private static long Measure(Action action)
        {
            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                action();
            }

            return allocationCounter.End();
        }

        private RpgCharacter CreateCharacter(float maxHp = 1000000f)
        {
            GameObject actor = Track(new GameObject("RpgAllocationActor"));
            RpgCharacter character = actor.AddComponent<RpgCharacter>();
            RpgCharacterTemplate template = Track(ScriptableObject.CreateInstance<RpgCharacterTemplate>());
            template.resources = new[]
            {
                new RpgResourceDefinition
                {
                    id = new RpgStatId(RpgStatPreset.Hp),
                    startMax = maxHp,
                    startCurrent = maxHp,
                    regen = new RpgRegenDefinition { enabled = true, mode = RpgRegenMode.FlatPerSecond, value = 1f }
                }
            };
            template.stats = new[] { new RpgStatDefinition { id = new RpgStatId("Wisdom"), baseValue = 10f } };
            character.ApplyTemplate(template);
            Assert.That(character.HpValue, Is.EqualTo(maxHp));
            Assert.That(character.Resources.ContainsKey(RpgResourceId.Hp), Is.True);
            return character;
        }

        private BuffDefinition CreateBuff()
        {
            BuffDefinition buff = Track(ScriptableObject.CreateInstance<BuffDefinition>());
            BuffStatModifier modifier = new();
            Set(modifier, "_statType", BuffStatType.DamageTypeResistPercent);
            Set(modifier, "_specificDamageType", "Fire");
            Set(modifier, "_value", 10f);
            Set(buff, "_id", "alloc-buff");
            Set(buff, "_duration", 1000000f);
            Set(buff, "_modifiers", new[] { modifier });
            return buff;
        }

        private StatusEffectDefinition CreateStatus(float movementMultiplier)
        {
            StatusEffectDefinition status = Track(ScriptableObject.CreateInstance<StatusEffectDefinition>());
            Set(status, "_id", "alloc-status");
            Set(status, "_duration", 1000000f);
            Set(status, "_movementSpeedMultiplier", movementMultiplier);
            Set(status, "_tickDamagePerSecond", 0.001f);
            Set(status, "_tickInterval", 0.01f);
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
    }
}
