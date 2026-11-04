using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Components.Weapons;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play.RPG
{
    /// <summary>
    ///     Attack and aura physics queries (ordering, hurtbox dedupe, allocation-free warm casts), attack-speed
    ///     cooldown scaling, evade state publication and contact-damage target events in a running player loop.
    /// </summary>
    public sealed class RpgAttackQueryPlayModeTests
    {
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

        [UnityTest]
        public IEnumerator DirectSphereCast_WithMaxTargets_HitsNearestReceiversFirst()
        {
            RpgCharacter caster = CreateCharacter("Caster", new Vector3(30000f, 0f, 0f));
            RpgAttackController controller = AddController(caster);
            RpgAttackDefinition attack = CreateAttack("line", 0, 0f,
                "\"_range\":20,\"_radius\":0.5,\"_maxTargets\":2,\"_power\":10,\"_use3D\":true");
            Set(controller, "_attacks", new[] { attack });
            Vector3 origin = caster.transform.position;
            RpgCharacter far = CreateTarget("Far", origin + Vector3.forward * 12f);
            RpgCharacter near = CreateTarget("Near", origin + Vector3.forward * 4f);
            RpgCharacter middle = CreateTarget("Middle", origin + Vector3.forward * 8f);
            Physics.SyncTransforms();
            yield return null;

            Assert.That(controller.TryUseAttack(attack.Id, out string failReason), Is.True, failReason);

            Assert.That(near.HpValue, Is.EqualTo(90f));
            Assert.That(middle.HpValue, Is.EqualTo(90f));
            Assert.That(far.HpValue, Is.EqualTo(100f), "MaxTargets now cuts the farthest hit, not an arbitrary one.");
            Assert.That(caster.HpValue, Is.EqualTo(100f));
        }

        [UnityTest]
        public IEnumerator AreaAttack_ChildHurtboxesDamageTheirParentCharacterOnce()
        {
            RpgCharacter caster = CreateCharacter("Caster", new Vector3(-30000f, 0f, 0f));
            RpgAttackController controller = AddController(caster);
            RpgAttackDefinition attack = CreateAttack("nova", 1, 0f,
                "\"_range\":3,\"_radius\":2,\"_maxTargets\":5,\"_power\":7,\"_use3D\":true");
            Set(controller, "_attacks", new[] { attack });
            RpgCharacter target = CreateCharacter("Target", caster.transform.position + Vector3.forward * 3f);
            for (int i = 0; i < 3; i++)
            {
                GameObject hurtbox = Track(new GameObject("AuditRpgHurtbox" + i));
                hurtbox.transform.SetParent(target.transform, false);
                hurtbox.transform.localPosition = Vector3.right * (0.2f * i);
                hurtbox.AddComponent<SphereCollider>().radius = 0.3f;
            }

            Physics.SyncTransforms();
            yield return null;

            Assert.That(controller.TryUseAttack(attack.Id, out string failReason), Is.True, failReason);
            Assert.That(target.HpValue, Is.EqualTo(93f));
        }

        [UnityTest]
        public IEnumerator AreaAttack_WarmCast_DoesNotAllocate()
        {
            RpgCharacter caster = CreateCharacter("Caster", new Vector3(0f, 30000f, 0f));
            RpgAttackController controller = AddController(caster);
            RpgAttackDefinition attack = CreateAttack("pulse", 1, 0f,
                "\"_range\":3,\"_radius\":4,\"_maxTargets\":32,\"_power\":0.001,\"_use3D\":true,\"_use2D\":true");
            Set(controller, "_attacks", new[] { attack });
            List<RpgCharacter> crowd = new();
            for (int i = 0; i < 24; i++)
            {
                crowd.Add(CreateTarget("Crowd" + i, caster.transform.position + Vector3.forward * 3f +
                                                    new Vector3(i % 5 - 2f, 0f, i / 5 - 2f) * 0.5f));
            }

            Physics.SyncTransforms();
            yield return null;
            for (int i = 0; i < 4; i++)
            {
                Assert.That(controller.UsePrimaryAttack(), Is.True);
            }

            float hpAfterWarmUp = crowd[12].HpValue;
            int casts = 0;
            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 50; i++)
            {
                if (controller.UsePrimaryAttack())
                {
                    casts++;
                }
            }

            long allocated = allocationCounter.End();
            Assert.That(casts, Is.EqualTo(50), "every measured cast must really run");
            Assert.That(crowd[12].HpValue, Is.LessThan(hpAfterWarmUp), "the measured casts must reach the crowd");
            Assert.That(allocated, Is.Zero, "Area casts used to allocate collider arrays and a HashSet per cast.");
        }

        [UnityTest]
        public IEnumerator DirectSphereCast_WarmCast_DoesNotAllocate()
        {
            RpgCharacter caster = CreateCharacter("Caster", new Vector3(0f, 0f, -30000f));
            RpgAttackController controller = AddController(caster);
            RpgAttackDefinition attack = CreateAttack("beam", 0, 0f,
                "\"_range\":20,\"_radius\":1,\"_maxTargets\":32,\"_power\":0.001,\"_use3D\":true,\"_use2D\":true");
            Set(controller, "_attacks", new[] { attack });
            List<RpgCharacter> line = new();
            for (int i = 0; i < 24; i++)
            {
                line.Add(CreateTarget("Line" + i, caster.transform.position + Vector3.forward * (2f + i * 0.5f) +
                                                  Vector3.right * ((i % 3 - 1) * 0.4f)));
            }

            Physics.SyncTransforms();
            yield return null;
            for (int i = 0; i < 4; i++)
            {
                Assert.That(controller.UsePrimaryAttack(), Is.True);
            }

            float hpAfterWarmUp = line[0].HpValue;
            int casts = 0;
            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 50; i++)
            {
                if (controller.UsePrimaryAttack())
                {
                    casts++;
                }
            }

            long allocated = allocationCounter.End();
            Assert.That(casts, Is.EqualTo(50), "every measured cast must really run");
            Assert.That(line[0].HpValue, Is.LessThan(hpAfterWarmUp), "the measured casts must reach the nearest target");
            Assert.That(allocated, Is.Zero, "Direct casts used to allocate hit arrays and a HashSet per cast.");
        }

        [UnityTest]
        public IEnumerator AuraPulse_Warm_DoesNotAllocate()
        {
            RpgCharacter owner = CreateCharacter("AuraOwner", new Vector3(30000f, 30000f, 0f));
            GameObject auraObject = Track(new GameObject("AuditRpgQueryAura"));
            auraObject.transform.SetParent(owner.transform, false);
            AuraWeapon aura = auraObject.AddComponent<AuraWeapon>();
            auraObject.GetComponent<SphereCollider>().radius = 4f;
            aura.hitLayers = ~0;
            aura.targetTag = string.Empty;
            aura.damage = 1;
            List<RpgCharacter> targets = new();
            for (int i = 0; i < 24; i++)
            {
                targets.Add(CreateTarget("AuraCrowd" + i, owner.transform.position +
                                                          new Vector3(i % 5 - 2f, 0f, i / 5 - 2f) * 0.6f));
            }

            Physics.SyncTransforms();
            yield return null;
            for (int i = 0; i < 4; i++)
            {
                aura.ApplyAuraDamage();
            }

            float hpAfterWarmUp = targets[0].HpValue;
            Assert.That(hpAfterWarmUp, Is.LessThan(100f), "The warm-up pulses must reach the crowd.");
            Assert.That(owner.HpValue, Is.EqualTo(100f), "The aura never damages its own hierarchy.");

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 50; i++)
            {
                aura.ApplyAuraDamage();
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.Zero, "Aura pulses used to allocate a collider array per pulse.");
            Assert.That(targets[0].HpValue, Is.LessThan(hpAfterWarmUp));

            IRpgCombatReceiver hitSource = null;
            targets[0].DamageApplied += (RpgDamageInfo info, float applied) => hitSource = info.Source;
            aura.ApplyAuraDamage();
            Assert.That(hitSource, Is.SameAs(owner), "melee and aura hits pass their wielder as the damage source");
        }

        [UnityTest]
        public IEnumerator EvadeStates_PublishOnceAfterEnableThenOnlyOnChange()
        {
            GameObject actor = Track(new GameObject("AuditRpgQueryEvade"));
            RpgEvadeController evade = actor.AddComponent<RpgEvadeController>();
            evade.EnableBuiltInInput = false;
            List<bool> evading = new();
            List<float> cooldowns = new();
            evade.IsEvadingState.AddListener(evading.Add);
            evade.RemainingCooldownState.AddListener(cooldowns.Add);

            yield return null;
            yield return null;
            yield return null;

            Assert.That(evading, Is.EqualTo(new[] { false }), "Bound UI still gets the initial evading state.");
            Assert.That(cooldowns, Is.EqualTo(new[] { 0f }), "Bound UI still gets the initial cooldown.");

            evade.enabled = false;
            evading.Clear();
            cooldowns.Clear();
            evade.enabled = true;
            yield return null;
            yield return null;

            Assert.That(evading, Is.EqualTo(new[] { false }), "Re-enabling publishes the state again, once.");
            Assert.That(cooldowns, Is.EqualTo(new[] { 0f }));
        }

        [UnityTest]
        public IEnumerator AttackSpeedScaling_ShortensTheNextCooldownOnly()
        {
            RpgCharacter caster = CreateCharacter("Caster", new Vector3(0f, -30000f, 0f));
            RpgAttackController controller = AddController(caster);
            RpgAttackDefinition attack = CreateAttack("haste", 0, 4f, null);
            Set(controller, "_attacks", new[] { attack });
            yield return null;

            Assert.That(controller.TryUseAttack(attack.Id, out _), Is.True);
            Assert.That(controller.GetRemainingCooldown(attack.Id), Is.EqualTo(4f).Within(0.1f));

            controller.ScaleCooldownByAttackSpeed = true;
            caster.ApplyBuff(CreateHasteBuff());
            Assert.That(controller.GetRemainingCooldown(attack.Id), Is.EqualTo(4f).Within(0.1f),
                "A running cooldown is not rescaled.");
            ((Dictionary<string, float>)typeof(RpgAttackController)
                .GetField("_cooldowns", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(controller)).Clear();
            Assert.That(controller.TryUseAttack(attack.Id, out _), Is.True);
            Assert.That(controller.GetRemainingCooldown(attack.Id), Is.EqualTo(2f).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator ContactDamage_RaisesFoundOnceAndLostOnceForAnInactiveTarget()
        {
            RpgCharacter target = CreateCharacter("ContactTarget", new Vector3(0f, 0f, 30000f));
            GameObject source = Track(new GameObject("AuditRpgContactSource"));
            source.transform.position = target.transform.position;
            RpgContactDamage contact = source.AddComponent<RpgContactDamage>();
            Set(contact, "targetTag", string.Empty);
            Set(contact, "cooldown", 0.01f);
            Set(contact, "targetFindInterval", 0.01f);
            contact.SetTargetReceiver(target);
            int found = 0;
            int lost = 0;
            contact.OnTargetFound.AddListener(() => found++);
            contact.OnTargetLost.AddListener(() => lost++);

            yield return null;
            yield return null;
            Assert.That(found, Is.EqualTo(1), "OnTargetFound never fired because caching a target marked it found.");
            Assert.That(target.HpValue, Is.LessThan(100f));

            target.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.1f);
            Assert.That(lost, Is.EqualTo(1), "An inactive cached target re-raised OnTargetLost on every retry.");

            target.gameObject.SetActive(true);
            yield return null;
            yield return null;
            Assert.That(found, Is.EqualTo(2));
            Assert.That(lost, Is.EqualTo(1));
        }

        private RpgCharacter CreateCharacter(string name, Vector3 position)
        {
            GameObject actor = Track(new GameObject("AuditRpgQuery" + name));
            actor.transform.position = position;
            RpgCharacter character = actor.AddComponent<RpgCharacter>();
            RpgCharacterTemplate template = Track(ScriptableObject.CreateInstance<RpgCharacterTemplate>());
            template.resources = new[]
            {
                new RpgResourceDefinition { id = new RpgStatId(RpgStatPreset.Hp) },
                new RpgResourceDefinition { id = new RpgStatId(RpgStatPreset.Mana) }
            };
            character.ApplyTemplate(template);
            return character;
        }

        private RpgCharacter CreateTarget(string name, Vector3 position)
        {
            RpgCharacter target = CreateCharacter(name, position);
            target.gameObject.AddComponent<SphereCollider>().radius = 0.5f;
            return target;
        }

        private static RpgAttackController AddController(RpgCharacter caster)
        {
            RpgAttackController controller = caster.gameObject.AddComponent<RpgAttackController>();
            controller.EnableBuiltInInput = false;
            return controller;
        }

        private RpgAttackDefinition CreateAttack(string id, int deliveryType, float cooldown, string extraJson)
        {
            RpgAttackDefinition attack = Track(ScriptableObject.CreateInstance<RpgAttackDefinition>());
            JsonUtility.FromJsonOverwrite("{\"_id\":\"" + id + "\",\"_deliveryType\":" + deliveryType +
                                          ",\"_cooldown\":" + cooldown.ToString(CultureInfo.InvariantCulture) +
                                          ",\"_costAmount\":0,\"_use2D\":false,\"_use3D\":false}", attack);
            if (!string.IsNullOrEmpty(extraJson))
            {
                JsonUtility.FromJsonOverwrite("{" + extraJson + "}", attack);
            }

            return attack;
        }

        private BuffDefinition CreateHasteBuff()
        {
            BuffDefinition buff = Track(ScriptableObject.CreateInstance<BuffDefinition>());
            BuffStatModifier modifier = new();
            Set(modifier, "_statType", BuffStatType.AttackSpeedPercent);
            Set(modifier, "_value", 100f);
            Set(buff, "_id", "haste");
            Set(buff, "_duration", 1000000f);
            Set(buff, "_modifiers", new[] { modifier });
            return buff;
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
