using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Resources;
using Neo.Rpg;
using Neo.Rpg.Components;
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

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                $"Field '{fieldName}' was not found on type '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }
    }
}
