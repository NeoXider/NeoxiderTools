using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Contracts used by the triggers demo: UnitTemplate innate modifiers (passives applied on registration) and
    ///     DamageService.RemainingShield for shield readouts.
    /// </summary>
    public sealed class UnitTemplateInnateTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
        }

        [Test]
        public void ApplyTo_RegistersAndAppliesInnateModifiers_WithTheUnitAsCaster()
        {
            ModifierDefinition thorns = ScriptableObject.CreateInstance<ModifierDefinition>();
            _created.Add(thorns);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(AbilityTemplates.Thorns("innate_thorns").Modifiers[0]),
                thorns.Blueprint);
            UnitTemplate template = ScriptableObject.CreateInstance<UnitTemplate>();
            _created.Add(template);
            SerializedObject serialized = new SerializedObject(template);
            SerializedProperty innate = serialized.FindProperty("_innateModifiers");
            innate.arraySize = 1;
            innate.GetArrayElementAtIndex(0).objectReferenceValue = thorns;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = system.CreateUnit(new TeamId(2));
            template.ApplyTo(unit);

            Assert.That(system.TryGetModifier("innate_thorns", out _), Is.True, "registered into the catalog");
            Assert.That(system.Modifiers.TryGetActive(unit.Id, "innate_thorns", out ModifierInstance instance), Is.True);
            Assert.That(instance.Caster, Is.EqualTo(unit.Id));
            Assert.That(instance.IsPermanent, Is.True);
            Assert.That(template.InnateModifiers.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemainingShield_ReportsCapacityMinusAbsorbed()
        {
            AbilitySystem system = new AbilitySystem();
            AbilityUnit unit = AbilityTestSupport.CreateUnit(system, team: 1, health: 100f);
            ModifierBlueprint shield = AbilityTestSupport.Modifier("readout_shield")
                .WithProperty(AbilityProperties.ShieldHp, PropertyOp.Add, 50f);
            ModifierInstance instance = system.Modifiers.Apply(shield, unit.Id, unit.Id).Instance;

            DamageService.ApplyDamage(system, UnitId.None, unit.Id, 20f, AbilityDamageTypes.Magical);

            Assert.That(DamageService.RemainingShield(instance), Is.EqualTo(30f).Within(0.001f));
            DamageService.ApplyDamage(system, UnitId.None, unit.Id, 40f, AbilityDamageTypes.Magical);
            Assert.That(instance.IsActive, Is.False, "the broken shield is removed");
            Assert.That(DamageService.RemainingShield(instance), Is.Zero);
            Assert.That(DamageService.RemainingShield(null), Is.Zero);
        }
    }
}
