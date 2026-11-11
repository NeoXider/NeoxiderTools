using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Modifier strength by level (PropertyContribution.ValueByLevel on the captured ability level, a 20 / 30 / 40%
    ///     slow; a stun of 1 / 1.5 / 2 s) and effect-node extras by level: chance, target cap, zone duration and
    ///     interval.
    /// </summary>
    public sealed class LeveledModifierAndNodeTests
    {
        private AbilitySystem _system;
        private FakeWorldAdapter _world;
        private AbilityUnit _caster;
        private AbilityUnit _enemy;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _world = new FakeWorldAdapter();
            _system.World = _world;
            _caster = AbilityTestSupport.CreateUnit(_system, team: 1);
            _enemy = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(_caster.Id, Vector3.zero);
            _world.SetPosition(_enemy.Id, new Vector3(2f, 0f, 0f));
        }

        private void Grant(AbilityBlueprint ability)
        {
            _system.RegisterAbility(ability);
            _system.GrantAbility(_caster.Id, ability.Id);
        }

        private static AbilityBlueprint Applier(string id, string modifierId)
        {
            AbilityBlueprint ability = new AbilityBlueprint { Id = id, Targeting = TargetingMode.Unit };
            ability.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.ApplyModifier, ModifierId = modifierId });
            return ability;
        }

        [Test]
        public void SlowByLevel_TwentyThirtyFortyPercent()
        {
            ModifierBlueprint slow = AbilityTestSupport.Modifier("frost_slow", duration: 2f);
            slow.Properties.Add(new PropertyContribution(AbilityProperties.MoveSpeed, PropertyOp.Mul, 0.8f)
                { ValueByLevel = new[] { 0.8f, 0.7f, 0.6f } });
            _system.RegisterModifier(slow);
            Grant(Applier("frost", "frost_slow"));
            _enemy.SetBaseProperty(AbilityProperties.MoveSpeed, 10f);

            float[] expected = { 8f, 7f, 6f };
            for (int level = 1; level <= 3; level++)
            {
                _system.SetAbilityLevel(_caster.Id, "frost", level);
                Assert.That(_system.Cast(CastRequest.AtUnit(_caster.Id, "frost", _enemy.Id)).Success, Is.True);
                Assert.That(_enemy.GetProperty(AbilityProperties.MoveSpeed), Is.EqualTo(expected[level - 1]).Within(1e-4f),
                    "level " + level + " re-applies with its own strength");
            }
        }

        [Test]
        public void ValueByLevel_StacksWithPerStackValue()
        {
            PropertyContribution contribution = new PropertyContribution("armor", PropertyOp.Add, 1f, 0.5f)
                { ValueByLevel = new[] { 2f, 4f } };

            Assert.That(contribution.ValueFor(1, 1), Is.EqualTo(2f));
            Assert.That(contribution.ValueFor(3, 2), Is.EqualTo(5f), "4 at level 2 + 0.5 x 2 extra stacks");
            Assert.That(contribution.ValueForStacks(3), Is.EqualTo(2f), "the legacy helper keeps the flat value");
        }

        [Test]
        public void ShieldCapacity_FollowsTheLevelOfTheShield()
        {
            ModifierBlueprint shield = AbilityTestSupport.Modifier("lv_shield", duration: 5f);
            shield.Properties.Add(new PropertyContribution(AbilityProperties.ShieldHp, PropertyOp.Add, 50f)
                { ValueByLevel = new[] { 50f, 120f } });
            _system.RegisterModifier(shield);

            ModifierApplyResult applied = _system.ApplyModifier("lv_shield", _caster.Id, _enemy.Id, null, abilityLevel: 2);
            Assert.That(DamageService.ShieldCapacity(applied.Instance), Is.EqualTo(120f));
            // WHY: magical, because pure damage bypasses shields.
            DamageService.ApplyDamage(_system, _caster.Id, _enemy.Id, 100f, AbilityDamageTypes.Magical);
            Assert.That(_enemy.Health, Is.EqualTo(1000f), "a 120 shield at level 2 absorbs all 100");
        }

        [Test]
        public void StunByLevel_OneOneAndAHalfTwoSeconds()
        {
            ModifierBlueprint stun = AbilityTestSupport.Modifier("bash_stun", duration: 1f).WithState(AbilityStates.Stunned);
            stun.DurationByLevel = new[] { 1f, 1.5f, 2f };
            _system.RegisterModifier(stun);
            Grant(Applier("bash", "bash_stun"));

            float[] expected = { 1f, 1.5f, 2f };
            for (int level = 1; level <= 3; level++)
            {
                _system.SetAbilityLevel(_caster.Id, "bash", level);
                Assert.That(_system.Cast(CastRequest.AtUnit(_caster.Id, "bash", _enemy.Id)).Success, Is.True);
                _system.Modifiers.TryGetActive(_enemy.Id, "bash_stun", out ModifierInstance instance);
                Assert.That(instance.InitialDuration, Is.EqualTo(expected[level - 1]).Within(1e-4f));
                _system.Tick(3f);
                Assert.That(_enemy.HasState(AbilityStates.Stunned), Is.False);
            }
        }

        [Test]
        public void ChanceByLevel_IsSampledAtTheAbilityLevel()
        {
            AbilityBlueprint ability = new AbilityBlueprint { Id = "lv_chance", Targeting = TargetingMode.Unit };
            EffectNodeData node = AbilityTestSupport.DamageNode(10f);
            node.Chance = 1f;
            node.ChanceByLevel = new[] { 0f, 1f };
            ability.ImpactEffects.Add(node);
            Grant(ability);

            Assert.That(_system.Cast(CastRequest.AtUnit(_caster.Id, "lv_chance", _enemy.Id)).Success, Is.True);
            Assert.That(_enemy.Health, Is.EqualTo(1000f), "0% at level 1");

            _system.SetAbilityLevel(_caster.Id, "lv_chance", 2);
            Assert.That(_system.Cast(CastRequest.AtUnit(_caster.Id, "lv_chance", _enemy.Id)).Success, Is.True);
            Assert.That(_enemy.Health, Is.EqualTo(990f).Within(1e-3f), "100% at level 2");
            Assert.That(node.ResolveChance(9), Is.EqualTo(1f));
        }

        [Test]
        public void MaxTargetsByLevel_CapsTheAreaAtTheAbilityLevel()
        {
            List<AbilityUnit> enemies = new List<AbilityUnit>();
            for (int i = 0; i < 4; i++)
            {
                AbilityUnit unit = AbilityTestSupport.CreateUnit(_system, team: 2);
                _world.SetPosition(unit.Id, new Vector3(0f, 0f, 1f + i));
                enemies.Add(unit);
            }

            AbilityBlueprint ability = new AbilityBlueprint { Id = "lv_cap" };
            EffectNodeData node = AbilityTestSupport.DamageNode(10f, target: EffectTargetSelector.AreaAroundCaster);
            node.Radius = 10f;
            node.MaxTargets = 1;
            node.MaxTargetsByLevel = new[] { 1, 3 };
            ability.ImpactEffects.Add(node);
            Grant(ability);
            _system.SetAbilityLevel(_caster.Id, "lv_cap", 2);

            Assert.That(_system.Cast(CastRequest.NoTarget(_caster.Id, "lv_cap")).Success, Is.True);
            int hit = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                hit += enemies[i].Health < 1000f ? 1 : 0;
            }

            Assert.That(hit + (_enemy.Health < 1000f ? 1 : 0), Is.EqualTo(3), "the three nearest at level 2");
        }

        [Test]
        public void ZoneDurationAndIntervalByLevel_ReachTheSpawnedZone()
        {
            AbilityBlueprint payload = new AbilityBlueprint { Id = "lv_zone_tick" };
            payload.ImpactEffects.Add(AbilityTestSupport.DamageNode(1f));
            AbilityBlueprint ability = new AbilityBlueprint { Id = "lv_zone" };
            ability.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Zone,
                Target = EffectTargetSelector.Caster,
                Radius = 3f,
                AbilityId = "lv_zone_tick",
                Duration = 2f,
                DurationByLevel = new[] { 2f, 4f },
                Interval = 1f,
                IntervalByLevel = new[] { 1f, 0.5f }
            });
            _system.RegisterAbility(payload);
            Grant(ability);
            _system.SetAbilityLevel(_caster.Id, "lv_zone", 2);

            Assert.That(_system.Cast(CastRequest.NoTarget(_caster.Id, "lv_zone")).Success, Is.True);
            List<AbilityZone> zones = new List<AbilityZone>();
            _system.GetZones(zones);
            Assert.That(zones.Count, Is.EqualTo(1));
            Assert.That(zones[0].Duration, Is.EqualTo(4f));
            Assert.That(zones[0].Interval, Is.EqualTo(0.5f));
        }
    }
}
