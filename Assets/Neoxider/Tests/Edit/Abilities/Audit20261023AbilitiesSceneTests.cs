using System.Reflection;
using Neo.Abilities;
using Neo.Core.Level;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    public sealed class Audit20261023AbilitiesSceneTests
    {
        [Test]
        public void SetLevelSourceWhileRegistered_UnsubscribesOriginalSourceBeforeReenable()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour behaviour = rig.AddUnit("level_unit", 1, Vector3.zero);
                LevelComponent original = CreateLevelSource(rig, "original", 2);
                LevelComponent replacement = CreateLevelSource(rig, "replacement", 5);
                AbilitySceneRig.CallPrivate(behaviour, "OnDisable");
                behaviour.SetLevelSource(original);
                AbilitySceneRig.CallPrivate(behaviour, "OnEnable");
                Assert.That(behaviour.Unit.Level, Is.EqualTo(2));

                behaviour.SetLevelSource(replacement);
                Assert.That(behaviour.Unit.Level, Is.EqualTo(2), "The setter retains its next-enable timing.");
                AbilitySceneRig.CallPrivate(behaviour, "OnDisable");
                AbilitySceneRig.CallPrivate(behaviour, "OnEnable");
                Assert.That(behaviour.Unit.Level, Is.EqualTo(5));

                original.OnLevelUp.Invoke(9);
                Assert.That(behaviour.Unit.Level, Is.EqualTo(5), "The old source must no longer drive this unit.");
                replacement.OnLevelUp.Invoke(6);
                Assert.That(behaviour.Unit.Level, Is.EqualTo(6));
            }
        }

        [Test]
        public void AutoCast_SkipsUntargetableNearestUnit()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour player = rig.AddUnit("player", 1, Vector3.zero);
                player.gameObject.AddComponent<AbilityCasterBehaviour>();
                AbilityAutoCaster autoCaster = player.gameObject.AddComponent<AbilityAutoCaster>();
                AbilityUnitBehaviour hidden = rig.AddUnit("untargetable", 2, Vector3.right);
                hidden.Unit.SetPermanentState(AbilityStates.Untargetable, true);
                AbilityUnitBehaviour valid = rig.AddUnit("valid", 2, Vector3.right * 2f);
                GrantZap(rig, player, 5f);

                autoCaster.CastReadyAbilities();

                Assert.That(hidden.CurrentHealth, Is.EqualTo(100f));
                Assert.That(valid.CurrentHealth, Is.EqualTo(90f));
            }
        }

        [Test]
        public void AutoCast_TargetSearchUsesCastRangeBonus()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour player = rig.AddUnit("player", 1, Vector3.zero);
                player.gameObject.AddComponent<AbilityCasterBehaviour>();
                AbilityAutoCaster autoCaster = player.gameObject.AddComponent<AbilityAutoCaster>();
                AbilityUnitBehaviour enemy = rig.AddUnit("enemy", 2, Vector3.right * 7f);
                player.Unit.SetBaseProperty(AbilityProperties.CastRangeBonus, 3f);
                GrantZap(rig, player, 5f);

                autoCaster.CastReadyAbilities();

                Assert.That(enemy.CurrentHealth, Is.EqualTo(90f));
            }
        }

        [Test]
        public void AutoCast_CatalogReplacementKeepsGrantedTargetingUntilRegrant()
        {
            using (AbilitySceneRig rig = new AbilitySceneRig())
            {
                AbilityUnitBehaviour player = rig.AddUnit("player", 1, Vector3.zero);
                player.gameObject.AddComponent<AbilityCasterBehaviour>();
                AbilityAutoCaster autoCaster = player.gameObject.AddComponent<AbilityAutoCaster>();
                AbilityUnitBehaviour enemy = rig.AddUnit("enemy", 2, Vector3.right * 3f);
                AbilityBlueprint original = new AbilityBlueprint
                {
                    Id = "held_targeting",
                    Targeting = TargetingMode.Unit,
                    TeamFilter = AbilityTeamFilter.Enemies,
                    Range = 5f,
                    Cooldown = 5f
                };
                original.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
                rig.System.RegisterAbility(original);
                rig.System.GrantAbility(player.UnitId, original.Id);
                AbilityBlueprint replacement = new AbilityBlueprint
                {
                    Id = original.Id,
                    Targeting = TargetingMode.NoTarget
                };
                replacement.ImpactEffects.Add(AbilityTestSupport.DamageNode(20f, target: EffectTargetSelector.Caster));
                rig.System.RegisterAbility(replacement);

                autoCaster.CastReadyAbilities();

                Assert.That(enemy.CurrentHealth, Is.EqualTo(90f));
                Assert.That(player.CurrentHealth, Is.EqualTo(100f));
                Assert.That(rig.System.RevokeAbility(player.UnitId, original.Id), Is.True);
                rig.System.GrantAbility(player.UnitId, original.Id);
                autoCaster.CastReadyAbilities();

                Assert.That(enemy.CurrentHealth, Is.EqualTo(90f));
                Assert.That(player.CurrentHealth, Is.EqualTo(80f));
            }
        }

        private static LevelComponent CreateLevelSource(AbilitySceneRig rig, string name, int level)
        {
            GameObject sourceObject = rig.Own(new GameObject(name));
            LevelComponent source = sourceObject.AddComponent<LevelComponent>();
            // Keep the regression independent of the user's saved level profile.
            typeof(LevelComponent).GetField("_loadOnAwake", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(source, false);
            typeof(LevelComponent).GetField("_autoSave", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(source, false);
            typeof(LevelComponent).GetField("_useXp", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(source, false);
            source.SetLevel(level);
            return source;
        }

        private static void GrantZap(AbilitySceneRig rig, AbilityUnitBehaviour player, float range)
        {
            AbilityBlueprint ability = new AbilityBlueprint
            {
                Id = "audit_zap",
                Targeting = TargetingMode.Unit,
                TeamFilter = AbilityTeamFilter.Enemies,
                Range = range,
                Cooldown = 5f
            };
            ability.ImpactEffects.Add(AbilityTestSupport.DamageNode(10f));
            rig.System.RegisterAbility(ability);
            rig.System.GrantAbility(player.UnitId, ability.Id);
        }
    }
}
