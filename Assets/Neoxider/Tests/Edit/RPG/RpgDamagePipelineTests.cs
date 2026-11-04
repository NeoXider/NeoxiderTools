using System.Collections.Generic;
using System.Reflection;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Components.Weapons;
using Neo.Rpg.Runtime;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit.RPG
{
    /// <summary>
    ///     Incoming damage pipeline seams (<see cref="IRpgDamageModifier" />, <see cref="RpgCharacter.DamageApplied" />),
    ///     network adapter lifetime, attack-speed cooldown scaling and the legacy damageable bridge receiver override.
    /// </summary>
    public sealed class RpgDamagePipelineTests
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

        [Test]
        public void DamageModifiers_RunAfterResistInOrderAndSeeTheSource()
        {
            RpgCharacter target = CreateCharacter("Target");
            RpgCharacter attacker = CreateCharacter("Attacker");
            target.ApplyBuff(CreateBuff("fire-ward", BuffStatType.DamageTypeResistPercent, 50f, "Fire"));
            List<string> calls = new();
            RecordingModifier halve = new(calls, "halve", amount => amount * 0.5f);
            RecordingModifier flat = new(calls, "flat", amount => amount - 3f);
            target.AddDamageModifier(halve);
            target.AddDamageModifier(flat);
            target.AddDamageModifier(halve);

            float applied = ((IRpgCombatReceiver)target).TakeDamage(new RpgDamageInfo(40f, "Fire", attacker));

            Assert.That(calls, Is.EqualTo(new[] { "halve:20", "flat:10" }));
            Assert.That(halve.LastSource, Is.SameAs(attacker), "TakeDamage used to drop RpgDamageInfo.Source.");
            Assert.That(applied, Is.EqualTo(7f));
            Assert.That(target.HpValue, Is.EqualTo(93f));
        }

        [Test]
        public void DamageModifiers_ClampNegativeIgnoreNonFiniteAndCanBeRemoved()
        {
            RpgCharacter target = CreateCharacter("Target");
            List<string> calls = new();
            RecordingModifier nan = new(calls, "nan", _ => float.NaN);
            RecordingModifier absorb = new(calls, "absorb", amount => amount - 1000f);
            target.AddDamageModifier(nan);

            Assert.That(target.Damage(10f), Is.EqualTo(10f), "Non-finite modifier output keeps the previous amount.");
            target.AddDamageModifier(absorb);
            Assert.That(target.Damage(10f), Is.Zero, "Negative output clamps to a fully absorbed hit.");
            Assert.That(target.HpValue, Is.EqualTo(90f));

            Assert.That(target.RemoveDamageModifier(absorb), Is.True);
            Assert.That(target.RemoveDamageModifier(absorb), Is.False);
            Assert.That(target.Damage(5f), Is.EqualTo(5f));
            Assert.That(target.HpValue, Is.EqualTo(85f));
        }

        [Test]
        public void DamageApplied_ReportsOriginalHitAndRemovedHp()
        {
            RpgCharacter target = CreateCharacter("Target");
            RpgCharacter attacker = CreateCharacter("Attacker");
            List<float> unityEventAmounts = new();
            RpgDamageInfo received = default;
            float receivedAmount = -1f;
            int raised = 0;
            target.OnDamagedEvent.AddListener(unityEventAmounts.Add);
            target.DamageApplied += (RpgDamageInfo info, float amount) =>
            {
                raised++;
                received = info;
                receivedAmount = amount;
            };

            target.ApplyDamage(new RpgDamageInfo(250f, "Cold", attacker));
            target.ApplyDamage(new RpgDamageInfo(5f, "Cold", attacker));

            Assert.That(raised, Is.EqualTo(1), "A hit that removes no HP raises nothing.");
            Assert.That(received.Amount, Is.EqualTo(250f));
            Assert.That(received.DamageType, Is.EqualTo("Cold"));
            Assert.That(received.Source, Is.SameAs(attacker));
            Assert.That(receivedAmount, Is.EqualTo(100f));
            Assert.That(unityEventAmounts, Is.EqualTo(new[] { 100f }));
            Assert.That(target.IsDead, Is.True);
        }

        [TestCase("", 90f)]
        [TestCase("Fire", 95f)]
        [TestCase("Cold", 90f)]
        public void MeleeWeapon_DamageTypeReachesTheTargetsResistance(string damageType, float expectedHp)
        {
            RpgCharacter wielder = CreateCharacter("Wielder");
            wielder.transform.position = new Vector3(52000f, 52000f, 0f);
            GameObject auraObject = Track(new GameObject("RpgPipelineAura"));
            auraObject.transform.SetParent(wielder.transform, false);
            AuraWeapon aura = auraObject.AddComponent<AuraWeapon>();
            auraObject.GetComponent<SphereCollider>().radius = 3f;
            aura.hitLayers = ~0;
            aura.targetTag = string.Empty;
            aura.damage = 10;
            aura.DamageType = damageType;
            RpgCharacter target = CreateCharacter("AuraTarget");
            target.transform.position = wielder.transform.position + Vector3.right;
            target.gameObject.AddComponent<SphereCollider>().radius = 0.5f;
            target.ApplyBuff(CreateBuff("fire-ward", BuffStatType.DamageTypeResistPercent, 50f, "Fire"));
            RpgDamageInfo received = default;
            target.DamageApplied += (RpgDamageInfo info, float amount) => received = info;
            Physics.SyncTransforms();

            aura.ApplyAuraDamage();

            Assert.That(target.HpValue, Is.EqualTo(expectedHp));
            Assert.That(received.DamageType, Is.EqualTo(damageType), "the weapon used to send untyped damage only");
            Assert.That(received.Source, Is.SameAs(wielder));
            Assert.That(wielder.HpValue, Is.EqualTo(100f));
        }

        [Test]
        public void MeleeWeapon_DamageTypeDefaultsToUntypedAndStoresNullAsEmpty()
        {
            GameObject weaponObject = Track(new GameObject("RpgPipelineWeapon"));
            AuraWeapon aura = weaponObject.AddComponent<AuraWeapon>();

            Assert.That(aura.DamageType, Is.Empty);
            aura.DamageType = "Fire";
            Assert.That(aura.DamageType, Is.EqualTo("Fire"));
            aura.DamageType = null;
            Assert.That(aura.DamageType, Is.Empty);
        }

        [Test]
        public void DestroyedNetworkAdapter_IsDroppedSoCommandsRunLocallyAgain()
        {
            RpgCharacter character = CreateCharacter("Networked");
            RpgRoutingAdapterProbe adapter = character.gameObject.AddComponent<RpgRoutingAdapterProbe>();
            Assert.That(adapter, Is.Not.Null, "The adapter probe must come from the runtime fixture assembly.");
            character.isNetworked = true;

            Assert.That(character.Damage(10f), Is.Zero);
            Assert.That(adapter.Routed, Is.EqualTo(1));
            Assert.That(character.HpValue, Is.EqualTo(100f));

            Object.DestroyImmediate(adapter);

            Assert.That(character.Damage(10f), Is.EqualTo(10f),
                "A destroyed adapter used to keep swallowing every command.");
            Assert.That(character.HpValue, Is.EqualTo(90f));
        }

        [Test]
        public void DestroyedAttackNetworkAdapter_IsDroppedSoProjectileAttacksAreAllowedAgain()
        {
            RpgCharacter character = CreateCharacter("NetworkedCaster");
            RpgAttackController controller = character.gameObject.AddComponent<RpgAttackController>();
            controller.EnableBuiltInInput = false;
            RpgProjectile projectile = Track(new GameObject("RpgPipelineProjectile")).AddComponent<RpgProjectile>();
            RpgAttackDefinition attack = CreateAttack("net-projectile", (int)RpgAttackDeliveryType.Projectile, 1f);
            Set(attack, "_projectilePrefab", projectile);
            RpgAttackNetworkAdapterProbe adapter = character.gameObject.AddComponent<RpgAttackNetworkAdapterProbe>();
            Assert.That(adapter, Is.Not.Null, "The adapter probe must come from the runtime fixture assembly.");

            Assert.That(controller.CanUseAttack(attack, out string blockedReason), Is.False);
            Assert.That(blockedReason, Does.Contain("server"));
            Assert.That(adapter.Queries, Is.EqualTo(1));

            Object.DestroyImmediate(adapter);

            Assert.That(controller.CanUseAttack(attack, out string failReason), Is.True,
                "A destroyed attack adapter used to keep blocking projectile attacks: " + failReason);
        }

        [Test]
        public void AttackSpeedScaling_DividesOnlyNewCooldownsWhenEnabled()
        {
            RpgCharacter character = CreateCharacter("Caster");
            RpgAttackController controller = character.gameObject.AddComponent<RpgAttackController>();
            controller.EnableBuiltInInput = false;
            RpgAttackDefinition attack = CreateAttack("haste-test", 0, 4f);
            character.ApplyBuff(CreateBuff("haste", BuffStatType.AttackSpeedPercent, 100f, null));

            Assert.That(character.GetAttackSpeedMultiplier(), Is.EqualTo(2f));
            Assert.That(controller.GetEffectiveCooldown(attack), Is.EqualTo(4f), "Scaling is opt-in.");
            controller.ScaleCooldownByAttackSpeed = true;
            Assert.That(controller.GetEffectiveCooldown(attack), Is.EqualTo(2f));
            Assert.That(controller.GetEffectiveCooldown(null), Is.Zero);
        }

        [Test]
        public void DamageableBridge_RuntimeReceiverOverridesAndClearsBackToCharacter()
        {
            RpgCharacter character = CreateCharacter("BridgeOwner");
            GameObject hitbox = Track(new GameObject("BridgeHitbox"));
            hitbox.transform.SetParent(character.transform, false);
            RpgStatsDamageableBridge bridge = hitbox.AddComponent<RpgStatsDamageableBridge>();
            bridge.DamageMultiplier = 2f;
            PlainReceiver plain = new();

            bridge.Initialize(plain);
            Assert.That(bridge.HasReceiverOverride, Is.True);
            Assert.That(bridge.TryTakeDamage(5, out float applied), Is.True);
            Assert.That(applied, Is.EqualTo(10f));
            Assert.That(plain.DamageCalls, Is.EqualTo(1));
            Assert.That(plain.CurrentHp, Is.EqualTo(90f));
            Assert.That(character.HpValue, Is.EqualTo(100f), "The override owns the hit exclusively.");
            Assert.That(bridge.TryHeal(3, out float healed), Is.True);
            Assert.That(healed, Is.EqualTo(3f));
            Assert.That(bridge.TryTakeDamage(0, out _), Is.False);

            bridge.ClearReceiver();
            bridge.TakeDamage(5);
            Assert.That(character.HpValue, Is.EqualTo(90f));
            Assert.That(plain.DamageCalls, Is.EqualTo(1));
            Assert.Throws<System.ArgumentNullException>(() => bridge.Initialize(null));
        }

        [Test]
        public void DamageableBridge_DestroyedOverrideIsNotRedirectedToAnotherActor()
        {
            RpgCharacter owner = CreateCharacter("BridgeOwner");
            RpgCharacter pooledActor = CreateCharacter("PooledActor");
            RpgStatsDamageableBridge bridge = owner.gameObject.AddComponent<RpgStatsDamageableBridge>();
            bridge.Initialize(pooledActor);
            Assert.That(bridge.TryTakeDamage(10, out _), Is.True);
            Assert.That(pooledActor.HpValue, Is.EqualTo(90f));

            Object.DestroyImmediate(pooledActor);

            Assert.That(bridge.TryTakeDamage(10, out float applied), Is.False);
            Assert.That(applied, Is.Zero);
            Assert.That(owner.HpValue, Is.EqualTo(100f));
        }

        private RpgCharacter CreateCharacter(string name)
        {
            GameObject actor = Track(new GameObject("RpgPipeline" + name));
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

        private BuffDefinition CreateBuff(string id, BuffStatType type, float value, string damageType)
        {
            BuffDefinition buff = Track(ScriptableObject.CreateInstance<BuffDefinition>());
            BuffStatModifier modifier = new();
            Set(modifier, "_statType", type);
            Set(modifier, "_value", value);
            Set(modifier, "_specificDamageType", damageType);
            Set(buff, "_id", id);
            Set(buff, "_duration", 1000000f);
            Set(buff, "_modifiers", new[] { modifier });
            return buff;
        }

        private RpgAttackDefinition CreateAttack(string id, int deliveryType, float cooldown)
        {
            RpgAttackDefinition attack = Track(ScriptableObject.CreateInstance<RpgAttackDefinition>());
            JsonUtility.FromJsonOverwrite("{\"_id\":\"" + id + "\",\"_deliveryType\":" + deliveryType +
                                          ",\"_cooldown\":" + cooldown.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                                          ",\"_costAmount\":0,\"_use2D\":false,\"_use3D\":false}", attack);
            return attack;
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

        private sealed class RecordingModifier : IRpgDamageModifier
        {
            private readonly List<string> _calls;
            private readonly string _name;
            private readonly System.Func<float, float> _modify;

            public RecordingModifier(List<string> calls, string name, System.Func<float, float> modify)
            {
                _calls = calls;
                _name = name;
                _modify = modify;
            }

            public IRpgCombatReceiver LastSource { get; private set; }

            public float ModifyIncomingDamage(RpgCharacter target, RpgDamageInfo info, float amount)
            {
                LastSource = info.Source;
                _calls.Add(_name + ":" + amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return _modify(amount);
            }
        }

        private sealed class PlainReceiver : IRpgCombatReceiver
        {
            public int DamageCalls { get; private set; }
            public float CurrentHp { get; private set; } = 100f;
            public float MaxHp => 100f;
            public int Level => 1;
            public bool IsDead => CurrentHp <= 0f;
            public bool IsInvulnerable => false;
            public bool CanPerformActions => true;

            public float TakeDamage(RpgDamageInfo info)
            {
                DamageCalls++;
                CurrentHp -= info.Amount;
                return info.Amount;
            }

            public float Heal(float amount)
            {
                CurrentHp += amount;
                return amount;
            }

            public bool TrySpendResource(string resourceId, float amount, out string failReason)
            {
                failReason = null;
                return true;
            }

            public bool TryApplyBuff(string buffId, out string failReason)
            {
                failReason = null;
                return true;
            }

            public bool TryApplyStatus(string statusId, out string failReason)
            {
                failReason = null;
                return true;
            }

            public void AddInvulnerabilityLock()
            {
            }

            public void RemoveInvulnerabilityLock()
            {
            }

            public float GetOutgoingDamageMultiplier()
            {
                return 1f;
            }

            public float GetMovementSpeedMultiplier()
            {
                return 1f;
            }
        }
    }
}
