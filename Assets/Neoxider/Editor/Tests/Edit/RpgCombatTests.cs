using System.Reflection;
using Neo.Core.Resources;
using NUnit.Framework;
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
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Combatant_TrySpendResource_WhenNoProvider_ReturnsFalse()
        {
            GameObject go = new("Combatant");
            RpgCombatant combatant = go.AddComponent<RpgCombatant>();

            try
            {
                bool ok = combatant.TrySpendResource(RpgResourceId.Mana, 10f, out string reason);
                Assert.That(ok, Is.False);
                Assert.That(reason, Is.Not.Null.And.Not.Empty);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AttackController_DirectAttack_DamagesTargetCombatant()
        {
            GameObject source = new("Source");
            var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
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
                SetPrivateField(attack, "_costAmount", 0f);

                SetPrivateField(controller, "_attacks", new[] { attack });

                bool success = controller.UsePrimaryAttack();

                Assert.That(success, Is.True);
                Assert.That(targetCombatant.CurrentHp, Is.EqualTo(75f));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void AttackController_WhenCostAmountPositive_AndNoResourceProvider_Fails()
        {
            GameObject source = new("Source");
            var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "Target";

            try
            {
                source.transform.position = Vector3.zero;
                target.transform.position = new Vector3(0f, 0f, 2f);
                source.transform.forward = Vector3.forward;
                Physics.SyncTransforms();

                RpgCombatant sourceCombatant = source.AddComponent<RpgCombatant>();
                target.AddComponent<RpgCombatant>();
                RpgAttackController controller = source.AddComponent<RpgAttackController>();
                RpgAttackDefinition attack = ScriptableObject.CreateInstance<RpgAttackDefinition>();
                SetPrivateField(attack, "_id", "costly");
                SetPrivateField(attack, "_deliveryType", RpgAttackDeliveryType.Direct);
                SetPrivateField(attack, "_hitMode", RpgHitMode.Damage);
                SetPrivateField(attack, "_power", 10f);
                SetPrivateField(attack, "_range", 5f);
                SetPrivateField(attack, "_radius", 0.25f);
                SetPrivateField(attack, "_castDelay", 0f);
                SetPrivateField(attack, "_cooldown", 0f);
                SetPrivateField(attack, "_costResourceId", "Mana");
                SetPrivateField(attack, "_costAmount", 30f);

                SetPrivateField(controller, "_attacks", new[] { attack });
                SetPrivateField(controller, "_combatantSource", sourceCombatant);

                bool success = controller.TryUseAttack("costly", out string failReason);

                Assert.That(success, Is.False);
                Assert.That(failReason, Is.Not.Null.And.Not.Empty);
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void AttackController_WhenCostAmountPositive_AndEnoughMana_SucceedsAndSpendsMana()
        {
            GameObject source = new("Source");
            var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "Target";

            try
            {
                source.transform.position = Vector3.zero;
                target.transform.position = new Vector3(0f, 0f, 2f);
                source.transform.forward = Vector3.forward;
                Physics.SyncTransforms();

                HealthComponent health = source.AddComponent<HealthComponent>();
                RpgCombatant sourceCombatant = source.AddComponent<RpgCombatant>();
                SetPrivateField(sourceCombatant, "_healthProvider", health);

                RpgCombatant targetCombatant = target.AddComponent<RpgCombatant>();
                RpgAttackController controller = source.AddComponent<RpgAttackController>();
                RpgAttackDefinition attack = ScriptableObject.CreateInstance<RpgAttackDefinition>();
                SetPrivateField(attack, "_id", "mana_attack");
                SetPrivateField(attack, "_deliveryType", RpgAttackDeliveryType.Direct);
                SetPrivateField(attack, "_hitMode", RpgHitMode.Damage);
                SetPrivateField(attack, "_power", 15f);
                SetPrivateField(attack, "_range", 5f);
                SetPrivateField(attack, "_radius", 0.25f);
                SetPrivateField(attack, "_castDelay", 0f);
                SetPrivateField(attack, "_cooldown", 0f);
                SetPrivateField(attack, "_costResourceId", "Mana");
                SetPrivateField(attack, "_costAmount", 20f);

                SetPrivateField(controller, "_attacks", new[] { attack });
                SetPrivateField(controller, "_combatantSource", sourceCombatant);

                float manaBefore = health.GetCurrent(RpgResourceId.Mana);
                bool success = controller.TryUseAttack("mana_attack", out string failReason);

                Assert.That(success, Is.True, failReason);
                Assert.That(targetCombatant.CurrentHp, Is.EqualTo(85f));
                Assert.That(health.GetCurrent(RpgResourceId.Mana), Is.EqualTo(manaBefore - 20f));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(target);
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
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TargetSelector_SelectTarget_PicksNearestCombatant()
        {
            GameObject source = new("Source");
            var nearTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var farTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            try
            {
                nearTarget.name = "NearTarget";
                farTarget.name = "FarTarget";
                source.transform.position = Vector3.zero;
                nearTarget.transform.position = new Vector3(0f, 0f, 2f);
                farTarget.transform.position = new Vector3(0f, 0f, 5f);
                nearTarget.AddComponent<RpgCombatant>();
                farTarget.AddComponent<RpgCombatant>();

                RpgTargetSelector selector = source.AddComponent<RpgTargetSelector>();
                GameObject selected = selector.SelectTarget();

                Assert.That(selected, Is.EqualTo(nearTarget));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(nearTarget);
                Object.DestroyImmediate(farTarget);
            }
        }

        [Test]
        public void AttackController_UsePreset_SelectsTargetAndDamagesNearestCombatant()
        {
            GameObject source = new("Source");
            var nearTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var farTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            try
            {
                source.transform.position = Vector3.zero;
                source.transform.forward = Vector3.forward;
                nearTarget.name = "NearTarget";
                farTarget.name = "FarTarget";
                nearTarget.transform.position = new Vector3(0f, 0f, 2f);
                farTarget.transform.position = new Vector3(0f, 0f, 5f);
                RpgCombatant nearCombatant = nearTarget.AddComponent<RpgCombatant>();
                RpgCombatant farCombatant = farTarget.AddComponent<RpgCombatant>();
                Physics.SyncTransforms();

                RpgAttackDefinition attack = ScriptableObject.CreateInstance<RpgAttackDefinition>();
                SetPrivateField(attack, "_id", "preset_attack");
                SetPrivateField(attack, "_deliveryType", RpgAttackDeliveryType.Area);
                SetPrivateField(attack, "_hitMode", RpgHitMode.Damage);
                SetPrivateField(attack, "_power", 20f);
                SetPrivateField(attack, "_range", 5f);
                SetPrivateField(attack, "_radius", 1f);
                SetPrivateField(attack, "_castDelay", 0f);
                SetPrivateField(attack, "_cooldown", 0f);
                SetPrivateField(attack, "_use3D", true);
                SetPrivateField(attack, "_use2D", false);
                SetPrivateField(attack, "_maxTargets", 1);
                SetPrivateField(attack, "_targetLayers", (LayerMask)~0);

                RpgTargetQuery query = new();
                SetPrivateField(query, "_range", 10f);
                SetPrivateField(query, "_use3D", true);
                SetPrivateField(query, "_use2D", false);
                SetPrivateField(query, "_selectionMode", RpgTargetSelectionMode.Nearest);

                RpgAttackPreset preset = ScriptableObject.CreateInstance<RpgAttackPreset>();
                SetPrivateField(preset, "_id", "ai_slash");
                SetPrivateField(preset, "_attackDefinition", attack);
                SetPrivateField(preset, "_requireTarget", true);
                SetPrivateField(preset, "_useSelectorComponentWhenAvailable", false);
                SetPrivateField(preset, "_aimAtTarget", true);
                SetPrivateField(preset, "_targetQuery", query);

                RpgAttackController controller = source.AddComponent<RpgAttackController>();
                SetPrivateField(controller, "_presets", new[] { preset });

                bool success = controller.UsePrimaryPreset();

                Assert.That(success, Is.True);
                Assert.That(nearCombatant.CurrentHp, Is.EqualTo(80f));
                Assert.That(farCombatant.CurrentHp, Is.EqualTo(100f));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(nearTarget);
                Object.DestroyImmediate(farTarget);
            }
        }

        [Test]
        public void AttackController_TryUsePreset_WithForcedTarget_DamagesSpecifiedCombatant()
        {
            GameObject source = new("Source");
            var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            try
            {
                source.transform.position = Vector3.zero;
                source.transform.forward = Vector3.forward;
                target.transform.position = new Vector3(0f, 0f, 2f);
                Physics.SyncTransforms();

                RpgCombatant targetCombatant = target.AddComponent<RpgCombatant>();
                RpgAttackDefinition attack = ScriptableObject.CreateInstance<RpgAttackDefinition>();
                SetPrivateField(attack, "_id", "forced_target_attack");
                SetPrivateField(attack, "_deliveryType", RpgAttackDeliveryType.Area);
                SetPrivateField(attack, "_hitMode", RpgHitMode.Damage);
                SetPrivateField(attack, "_power", 15f);
                SetPrivateField(attack, "_range", 5f);
                SetPrivateField(attack, "_radius", 1f);
                SetPrivateField(attack, "_castDelay", 0f);
                SetPrivateField(attack, "_cooldown", 0f);
                SetPrivateField(attack, "_use3D", true);
                SetPrivateField(attack, "_use2D", false);
                SetPrivateField(attack, "_maxTargets", 1);
                SetPrivateField(attack, "_targetLayers", (LayerMask)~0);

                RpgAttackPreset preset = ScriptableObject.CreateInstance<RpgAttackPreset>();
                SetPrivateField(preset, "_id", "forced_target_preset");
                SetPrivateField(preset, "_attackDefinition", attack);
                SetPrivateField(preset, "_requireTarget", true);
                SetPrivateField(preset, "_useSelectorComponentWhenAvailable", false);
                SetPrivateField(preset, "_aimAtTarget", true);

                RpgAttackController controller = source.AddComponent<RpgAttackController>();

                bool success = controller.TryUsePreset(preset, target, out string failReason);

                Assert.That(success, Is.True, failReason);
                Assert.That(targetCombatant.CurrentHp, Is.EqualTo(85f));
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(target);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null,
                $"Field '{fieldName}' was not found on type '{target.GetType().Name}'.");
            fieldInfo.SetValue(target, value);
        }
    }
}
