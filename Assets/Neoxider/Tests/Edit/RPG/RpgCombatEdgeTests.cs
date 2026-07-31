using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Resources;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Runtime;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tests.Edit.RPG
{
    [TestFixture]
    public sealed class RpgCombatEdgeTests
    {
        [Test]
        public void CombatMath_NullResolversAndNullEntries_ReturnSafeDefaults()
        {
            var buffs = new List<ActiveBuffEntry> { null, new() { BuffId = "missing" } };
            var statuses = new List<ActiveStatusEntry> { null, new() { StatusId = "missing" } };

            Assert.That(RpgCombatMath.GetOutgoingDamageMultiplier(buffs, null), Is.EqualTo(1f));
            Assert.That(RpgCombatMath.GetIncomingDamageMultiplier(buffs, null, "Fire"), Is.EqualTo(1f));
            Assert.That(RpgCombatMath.GetRegenPerSecond(2f, buffs, null), Is.EqualTo(2f));
            Assert.That(RpgCombatMath.GetMovementSpeedMultiplier(buffs, statuses, null, null), Is.EqualTo(1f));
            Assert.That(RpgCombatMath.HasBlockingStatus(statuses, null), Is.False);
        }

        [Test]
        public void CombatMath_OverDefenseClampsIncomingDamageToZero()
        {
            BuffDefinition buff = CreateBuff("defense", new[]
            {
                CreateModifier(BuffStatType.DefensePercent, 75f),
                CreateModifier(BuffStatType.SpecificDefensePercent, 50f, "fire")
            });
            ActiveBuffEntry[] active = new[] { new ActiveBuffEntry { BuffId = "defense" } };

            try
            {
                float fireMultiplier = RpgCombatMath.GetIncomingDamageMultiplier(active, Resolve(buff), "FIRE");
                float iceMultiplier = RpgCombatMath.GetIncomingDamageMultiplier(active, Resolve(buff), "ice");

                Assert.That(fireMultiplier, Is.EqualTo(0f));
                Assert.That(iceMultiplier, Is.EqualTo(0.25f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(buff);
            }
        }

        [Test]
        public void CombatMath_StatusMovementMultiplierStacksAndBlocksActions()
        {
            StatusEffectDefinition slow = CreateStatus("slow", 0.5f);
            StatusEffectDefinition root = CreateStatus("root", 0f, true);
            ActiveStatusEntry[] active = new[]
            {
                new ActiveStatusEntry { StatusId = "slow" },
                new ActiveStatusEntry { StatusId = "root" }
            };

            try
            {
                Assert.That(RpgCombatMath.GetMovementSpeedMultiplier(null, active, null, Resolve(slow, root)),
                    Is.EqualTo(0f));
                Assert.That(RpgCombatMath.HasBlockingStatus(active, Resolve(slow, root)), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(slow);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RpgStatsDamageableBridge_ForwardsLegacyDamageAndHealWithMultipliers()
        {
            GameObject parent = new("RpgBridgeParent");
            GameObject child = new("RpgBridgeChild");
            RpgCharacterTemplate template = ScriptableObject.CreateInstance<RpgCharacterTemplate>();

            try
            {
                child.transform.SetParent(parent.transform);
                RpgCharacter character = AddCharacter(parent);
                template.resources = new[]
                {
                    new RpgResourceDefinition
                    {
                        id = new RpgStatId(RpgStatPreset.Hp),
                        startCurrent = 100f,
                        startMax = 100f,
                        restoreOnAwake = true,
                        restoreToFull = true
                    }
                };
                character.ApplyTemplate(template);

                RpgStatsDamageableBridge bridge = child.AddComponent<RpgStatsDamageableBridge>();
                bridge.DamageMultiplier = 2f;
                bridge.HealMultiplier = 0.5f;

                bridge.TakeDamage(10);
                bridge.Heal(10);

                Assert.That(character.HpValue, Is.EqualTo(85f));
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void EffectShelf_StackableBuff_ClampsProjectsAndCopiesStacks()
        {
            BuffDefinition buff = CreateBuff("rage", new[]
            {
                CreateModifier(BuffStatType.DamagePercent, 10f)
            });

            try
            {
                SetPrivateField(buff, "_stackable", true);
                SetPrivateField(buff, "_maxStacks", 2);

                var shelf = new RpgEffectShelf();
                shelf.ApplyBuff(buff);
                shelf.ApplyBuff(buff);
                shelf.ApplyBuff(buff);
                shelf.ActiveBuffs[0].Stacks = 99;
                shelf.ApplyBuff(buff);

                Assert.That(shelf.ActiveBuffs, Has.Count.EqualTo(1));
                Assert.That(shelf.ActiveBuffs[0].Stacks, Is.EqualTo(2));

                var modifiers = new List<BuffStatModifierApplication>();
                shelf.BuildModifierApplications(modifiers);
                Assert.That(modifiers, Has.Count.EqualTo(1));
                Assert.That(modifiers[0].Stacks, Is.EqualTo(2));

                float multiplier = RpgCombatMath.GetOutgoingDamageMultiplier(shelf.ActiveBuffs, Resolve(buff));
                Assert.That(multiplier, Is.EqualTo(1.2f).Within(0.001f));

                var copiedBuffs = new List<ActiveBuffEntry>();
                shelf.CopyActiveEffectsTo(copiedBuffs, null);
                Assert.That(copiedBuffs[0].Stacks, Is.EqualTo(2));
                copiedBuffs[0].Stacks = 1;
                Assert.That(shelf.ActiveBuffs[0].Stacks, Is.EqualTo(2));

                var restored = new RpgEffectShelf();
                restored.RegisterBuffLibrary(new[] { buff });
                restored.RestoreActiveEffects(new[]
                {
                    new ActiveBuffEntry { BuffId = "rage", ExpiresAtUtc = 123d, Stacks = 99 }
                }, null);
                Assert.That(restored.ActiveBuffs[0].Stacks, Is.EqualTo(2));

                float restoredMultiplier = RpgCombatMath.GetOutgoingDamageMultiplier(restored.ActiveBuffs,
                    Resolve(buff));
                Assert.That(restoredMultiplier, Is.EqualTo(1.2f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(buff);
            }
        }

        [Test]
        public void AttackController_PresetWithoutRequiredTarget_DoesNotSpendCost()
        {
            GameObject source = new("RpgPresetSource");
            RpgAttackDefinition attack = CreateAttack("costly", 5f);
            RpgAttackPreset preset = CreatePreset("requires-target", attack, true);

            try
            {
                TestCombatReceiver receiver = source.AddComponent<TestCombatReceiver>();
                RpgAttackController controller = source.AddComponent<RpgAttackController>();

                bool used = controller.TryUsePreset(preset, out string failReason);

                Assert.That(used, Is.False);
                Assert.That(failReason, Is.Not.Null);
                Assert.That(receiver.SpendCalls, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(preset);
            }
        }

        [Test]
        public void AttackController_ApplyHit_UsesCustomCombatReceiver()
        {
            GameObject source = new("RpgHitSource");
            GameObject target = new("RpgHitTarget");
            RpgAttackDefinition attack = CreateAttack("direct", 0f);

            try
            {
                source.AddComponent<TestCombatReceiver>();
                RpgAttackController controller = source.AddComponent<RpgAttackController>();
                TestCombatReceiver receiver = target.AddComponent<TestCombatReceiver>();
                SetPrivateField(attack, "_power", 3f);

                bool applied = InvokeInternal<bool>(controller, "ApplyHitToGameObject", target, attack);

                Assert.That(applied, Is.True);
                Assert.That(receiver.DamageCalls, Is.EqualTo(1));
                Assert.That(receiver.CurrentHp, Is.EqualTo(7f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void Projectile_InitializeAgain_ResetsLifetimeAndHitDedupeState()
        {
            GameObject ownerObject = new("ProjectileOwner");
            GameObject projectileObject = new("ReusableProjectile");
            GameObject targetObject = new("PreviousTarget");
            RpgAttackDefinition attack = CreateAttack("projectile", 0f);

            try
            {
                RpgAttackController owner = ownerObject.AddComponent<RpgAttackController>();
                RpgProjectile projectile = projectileObject.AddComponent<RpgProjectile>();
                TestCombatReceiver receiver = targetObject.AddComponent<TestCombatReceiver>();
                SetPrivateField(attack, "_projectileLifetime", 3.5f);
                SetPrivateField(attack, "_projectileMaxHits", 2);

                SetPrivateField(projectile, "_elapsed", 99f);
                GetPrivateField<HashSet<GameObject>>(projectile, "_hitTargets").Add(targetObject);
                GetPrivateField<HashSet<IRpgCombatReceiver>>(projectile, "_hitReceivers").Add(receiver);

                projectile.Initialize(owner, attack, null, Vector3.right);

                Assert.That(GetPrivateField<float>(projectile, "_elapsed"), Is.EqualTo(0f));
                Assert.That(GetPrivateField<HashSet<GameObject>>(projectile, "_hitTargets"), Is.Empty);
                Assert.That(GetPrivateField<HashSet<IRpgCombatReceiver>>(projectile, "_hitReceivers"), Is.Empty);
                Assert.That(GetPrivateField<int>(projectile, "_remainingHits"), Is.EqualTo(2));
                Assert.That(GetPrivateField<float>(projectile, "_lifetime"), Is.EqualTo(3.5f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(projectileObject);
                Object.DestroyImmediate(ownerObject);
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void AttackController_ApplyHit_DeduplicatesByCombatReceiver()
        {
            GameObject source = new("RpgHitSource");
            GameObject root = new("RpgHitTargetRoot");
            GameObject child = new("RpgHitTargetChild");
            RpgAttackDefinition attack = CreateAttack("direct", 0f);
            var affectedReceivers = new HashSet<IRpgCombatReceiver>();

            try
            {
                source.AddComponent<TestCombatReceiver>();
                RpgAttackController controller = source.AddComponent<RpgAttackController>();
                TestCombatReceiver receiver = root.AddComponent<TestCombatReceiver>();
                child.transform.SetParent(root.transform);
                SetPrivateField(attack, "_power", 3f);

                bool firstApplied = InvokeInternal<bool>(controller, "ApplyHitToGameObject", root, attack,
                    affectedReceivers);
                bool secondApplied = InvokeInternal<bool>(controller, "ApplyHitToGameObject", child, attack,
                    affectedReceivers);

                Assert.That(firstApplied, Is.True);
                Assert.That(secondApplied, Is.False);
                Assert.That(receiver.DamageCalls, Is.EqualTo(1));
                Assert.That(receiver.CurrentHp, Is.EqualTo(7f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(attack);
            }
        }

        private static Func<string, BuffDefinition> Resolve(params BuffDefinition[] buffs)
        {
            return id => Array.Find(buffs, buff => buff != null && buff.Id == id);
        }

        private static Func<string, StatusEffectDefinition> Resolve(params StatusEffectDefinition[] statuses)
        {
            return id => Array.Find(statuses, status => status != null && status.Id == id);
        }

        private static BuffDefinition CreateBuff(string id, BuffStatModifier[] modifiers)
        {
            BuffDefinition buff = ScriptableObject.CreateInstance<BuffDefinition>();
            SetPrivateField(buff, "_id", id);
            SetPrivateField(buff, "_modifiers", modifiers);
            return buff;
        }

        private static BuffStatModifier CreateModifier(BuffStatType type, float value, string damageType = null)
        {
            BuffStatModifier modifier = new();
            SetPrivateField(modifier, "_statType", type);
            SetPrivateField(modifier, "_value", value);
            SetPrivateField(modifier, "_specificDamageType", damageType);
            return modifier;
        }

        private static StatusEffectDefinition CreateStatus(string id, float movementSpeedMultiplier = 1f,
            bool blocksActions = false)
        {
            StatusEffectDefinition status = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            SetPrivateField(status, "_id", id);
            SetPrivateField(status, "_movementSpeedMultiplier", movementSpeedMultiplier);
            SetPrivateField(status, "_blocksActions", blocksActions);
            return status;
        }

        private static RpgCharacter AddCharacter(GameObject go)
        {
#if MIRROR
            go.AddComponent<NetworkIdentity>();
#endif
            return go.AddComponent<RpgCharacter>();
        }

        private static RpgAttackDefinition CreateAttack(string id, float cost)
        {
            RpgAttackDefinition attack = ScriptableObject.CreateInstance<RpgAttackDefinition>();
            SetPrivateField(attack, "_id", id);
            SetPrivateField(attack, "_costAmount", cost);
            SetPrivateField(attack, "_costResourceId", "Mana");
            SetPrivateField(attack, "_cooldown", 0f);
            return attack;
        }

        private static RpgAttackPreset CreatePreset(string id, RpgAttackDefinition attack, bool requireTarget)
        {
            RpgAttackPreset preset = ScriptableObject.CreateInstance<RpgAttackPreset>();
            SetPrivateField(preset, "_id", id);
            SetPrivateField(preset, "_attackDefinition", attack);
            SetPrivateField(preset, "_requireTarget", requireTarget);
            SetPrivateField(preset, "_useSelectorComponentWhenAvailable", false);
            return preset;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                $"Field '{fieldName}' was not found on type '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                $"Field '{fieldName}' was not found on type '{target.GetType().Name}'.");
            return (T)field.GetValue(target);
        }

        private static T InvokeInternal<T>(object target, string methodName, params object[] args)
        {
            var argumentTypes = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                argumentTypes[i] = args[i].GetType();
            }

            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                argumentTypes,
                null);
            Assert.That(method, Is.Not.Null,
                $"{target.GetType().Name}.{methodName} method was not found.");
            return (T)method.Invoke(target, args);
        }

        private sealed class TestCombatReceiver : MonoBehaviour, IRpgCombatReceiver
        {
            private float _currentHp = 10f;

            public int SpendCalls { get; private set; }
            public int DamageCalls { get; private set; }
            public float CurrentHp => _currentHp;
            public float MaxHp => 10f;
            public int Level => 1;
            public bool IsDead => _currentHp <= 0f;
            public bool IsInvulnerable => false;
            public bool CanPerformActions => true;

            public float TakeDamage(RpgDamageInfo info)
            {
                DamageCalls++;
                float amount = Mathf.Max(0f, info.Amount);
                _currentHp = Mathf.Max(0f, _currentHp - amount);
                return amount;
            }

            public float Heal(float amount)
            {
                float healed = Mathf.Max(0f, amount);
                _currentHp = Mathf.Min(MaxHp, _currentHp + healed);
                return healed;
            }

            public bool TrySpendResource(string resourceId, float amount, out string failReason)
            {
                SpendCalls++;
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
