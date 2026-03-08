using System;
using System.Reflection;
using NUnit.Framework;
using Neo.Rpg;
using UnityEngine;

namespace Neo.Rpg.Tests
{
    public class RpgCombatTests
    {
        [Test]
        public void Combatant_TakeDamage_IgnoresInvulnerability()
        {
            GameObject go = new("Combatant");
            RpgCombatant combatant = go.AddComponent<RpgCombatant>();

            try
            {
                combatant.AddInvulnerabilityLock();
                float dealt = combatant.TakeDamage(50f);

                Assert.That(dealt, Is.EqualTo(0f));
                Assert.That(combatant.CurrentHp, Is.EqualTo(100f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AttackController_DirectAttack_DamagesTargetCombatant()
        {
            GameObject source = new("Source");
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "Target";

            try
            {
                source.transform.position = Vector3.zero;
                target.transform.position = new Vector3(0f, 0f, 2f);
                source.transform.forward = Vector3.forward;
                Physics.SyncTransforms();

                RpgCombatant targetCombatant = target.AddComponent<RpgCombatant>();
                RpgAttackController controller = source.AddComponent<RpgAttackController>();
                RpgAttackDefinition attack = ScriptableObject.CreateInstance<RpgAttackDefinition>();
                SetPrivateField(attack, "_id", "slash");
                SetPrivateField(attack, "_deliveryType", RpgAttackDeliveryType.Direct);
                SetPrivateField(attack, "_hitMode", RpgHitMode.Damage);
                SetPrivateField(attack, "_power", 25f);
                SetPrivateField(attack, "_range", 5f);
                SetPrivateField(attack, "_radius", 0.25f);
                SetPrivateField(attack, "_castDelay", 0f);
                SetPrivateField(attack, "_cooldown", 0f);
                SetPrivateField(attack, "_use3D", true);
                SetPrivateField(attack, "_use2D", false);
                SetPrivateField(attack, "_maxTargets", 1);
                SetPrivateField(attack, "_targetLayers", (LayerMask)~0);

                SetPrivateField(controller, "_attacks", new[] { attack });

                bool success = controller.UsePrimaryAttack();

                Assert.That(success, Is.True);
                Assert.That(targetCombatant.CurrentHp, Is.EqualTo(75f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void EvadeController_TryStartEvade_GrantsInvulnerability()
        {
            GameObject go = new("EvadeOwner");
            RpgCombatant combatant = go.AddComponent<RpgCombatant>();
            RpgEvadeController evade = go.AddComponent<RpgEvadeController>();

            try
            {
                SetPrivateField(evade, "_combatant", combatant);

                bool started = evade.TryStartEvade();

                Assert.That(started, Is.True);
                Assert.That(combatant.IsInvulnerable, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Field '{fieldName}' was not found on type '{target.GetType().Name}'.");
            fieldInfo.SetValue(target, value);
        }
    }
}
