using Neo.Core.Resources;
using Neo.Rpg;
using Neo.Rpg.Components;
using NUnit.Framework;
using UnityEngine;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tests.Edit.RPG
{
    [TestFixture]
    public class RpgCharacterTests
    {
        [Test]
        public void ApplyTemplate_UsesCanonicalHpAndCustomResourceIds()
        {
            GameObject go = new("RpgCharacter");
            RpgCharacter character = AddCharacter(go);
            RpgCharacterTemplate template = ScriptableObject.CreateInstance<RpgCharacterTemplate>();

            try
            {
                template.resources = new[]
                {
                    new RpgResourceDefinition
                    {
                        id = new RpgStatId(RpgStatPreset.Hp),
                        startCurrent = 75f,
                        startMax = 100f,
                        restoreOnAwake = true,
                        restoreToFull = false
                    },
                    new RpgResourceDefinition
                    {
                        id = new RpgStatId("DarkMana"),
                        startCurrent = 10f,
                        startMax = 30f,
                        restoreOnAwake = true,
                        restoreToFull = false
                    }
                };

                character.ApplyTemplate(template);

                Assert.That(character.HpValue, Is.EqualTo(75f));
                Assert.That(character.MaxHpValue, Is.EqualTo(100f));
                Assert.That(character.GetResource("DarkMana"), Is.EqualTo(10f));
                Assert.That(character.GetResourceMax("DarkMana"), Is.EqualTo(30f));
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DamageAndHeal_UpdateReactiveHp()
        {
            GameObject go = new("RpgCharacter");
            RpgCharacter character = AddCharacter(go);
            RpgCharacterTemplate template = CreateBasicTemplate();

            try
            {
                character.ApplyTemplate(template);

                float dealt = character.Damage(25f);
                float healed = character.Heal(10f);

                Assert.That(dealt, Is.EqualTo(25f));
                Assert.That(healed, Is.EqualTo(10f));
                Assert.That(character.HpValue, Is.EqualTo(85f));
                Assert.That(character.HpPercentState.CurrentValue, Is.EqualTo(0.85f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SpendAndRefill_CustomResource_WorkThroughUniversalApi()
        {
            GameObject go = new("RpgCharacter");
            RpgCharacter character = AddCharacter(go);
            RpgCharacterTemplate template = CreateBasicTemplate();

            try
            {
                template.resources = new[]
                {
                    template.resources[0],
                    new RpgResourceDefinition
                    {
                        id = new RpgStatId("BloodMana"),
                        startCurrent = 40f,
                        startMax = 80f,
                        restoreOnAwake = true,
                        restoreToFull = false
                    }
                };
                character.ApplyTemplate(template);

                bool spent = character.Spend("BloodMana", 15f);
                float refilled = character.Refill("BloodMana", 5f);

                Assert.That(spent, Is.True);
                Assert.That(refilled, Is.EqualTo(5f));
                Assert.That(character.GetResource("BloodMana"), Is.EqualTo(30f));
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ManualUpgrade_CanIncreaseStatAndDerivedResourceMax()
        {
            GameObject go = new("RpgCharacter");
            RpgCharacter character = AddCharacter(go);
            RpgCharacterTemplate template = CreateBasicTemplate();
            RpgProgressionDefinition progression = ScriptableObject.CreateInstance<RpgProgressionDefinition>();

            try
            {
                progression.growthMode = RpgLevelGrowthMode.ManualUpgradePoints;
                progression.upgradePointsPerLevel = 1;
                progression.upgradeRules = new[]
                {
                    new RpgStatUpgradeRule
                    {
                        statId = new RpgStatId(RpgStatPreset.Vitality),
                        increasePerPoint = 1f,
                        costPerUpgrade = 1,
                        maxUpgradeCount = -1,
                        derivedResourceModifiers = new[]
                        {
                            new RpgResourceModifier
                            {
                                resourceId = new RpgStatId(RpgStatPreset.Hp),
                                kind = RpgResourceModifierKind.AddMaxFlat,
                                value = 15f
                            }
                        }
                    }
                };
                template.progression = progression;
                template.stats = new[]
                {
                    new RpgStatDefinition
                    {
                        id = new RpgStatId(RpgStatPreset.Vitality),
                        baseValue = 10f
                    }
                };

                character.ApplyTemplate(template);
                character.AddUpgradePoints(1);
                bool upgraded = character.UpgradeStat(nameof(RpgStatPreset.Vitality));

                Assert.That(upgraded, Is.True);
                Assert.That(character.GetStat(nameof(RpgStatPreset.Vitality)), Is.EqualTo(11f));
                Assert.That(character.MaxHpValue, Is.EqualTo(115f));
            }
            finally
            {
                Object.DestroyImmediate(progression);
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(go);
            }
        }

        private static RpgCharacterTemplate CreateBasicTemplate()
        {
            RpgCharacterTemplate template = ScriptableObject.CreateInstance<RpgCharacterTemplate>();
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
            return template;
        }

        private static RpgCharacter AddCharacter(GameObject go)
        {
#if MIRROR
            go.AddComponent<NetworkIdentity>();
#endif
            return go.AddComponent<RpgCharacter>();
        }
    }
}
