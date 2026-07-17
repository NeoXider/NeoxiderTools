using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     The leveled-value micro-atom: per-level arrays keyed by ability/unit level, additive
    ///     property scaling, named specials, backward-compatible flat amounts, leveled radius and
    ///     modifier duration, the cast → slot-level plumbing, and capture-at-apply-time DoT scaling.
    /// </summary>
    public sealed class LeveledValueTests
    {
        private AbilitySystem _system;
        private AbilityUnit _caster;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _caster = AbilityTestSupport.CreateUnit(_system, team: 1);
        }

        private static EffectNodeData AmountNode(float[] byLevel, LevelSource source)
        {
            return new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage,
                Target = EffectTargetSelector.Target,
                DamageType = AbilityDamageTypes.Pure,
                AmountByLevel = byLevel,
                AmountLevelSource = source
            };
        }

        [Test]
        public void LeveledArray_ResolvesPerAbilityLevel_WithOutOfRangeClamp()
        {
            EffectNodeData node = AmountNode(new[] { 5f, 10f, 15f }, LevelSource.AbilityLevel);

            Assert.That(ResolveAt(node, level: 1), Is.EqualTo(5f).Within(0.001f));
            Assert.That(ResolveAt(node, level: 2), Is.EqualTo(10f).Within(0.001f));
            Assert.That(ResolveAt(node, level: 3), Is.EqualTo(15f).Within(0.001f));
            Assert.That(ResolveAt(node, level: 9), Is.EqualTo(15f).Within(0.001f), "clamps above the array");
            Assert.That(ResolveAt(node, level: 0), Is.EqualTo(5f).Within(0.001f), "clamps below the array");
        }

        [Test]
        public void AllDefaultNode_ResolvesToFlatAmount_BackwardCompatible()
        {
            var node = new EffectNodeData { Amount = 7f };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id);

            Assert.That(LeveledValueResolver.ResolveAmount(node, ctx, _caster.Id), Is.EqualTo(7f).Within(0.001f));
            Assert.That(node.ToLeveledValue().Base, Is.EqualTo(7f), "the mapped LeveledValue is a flat Base");
        }

        [Test]
        public void PropertyScaling_FromCaster_AddsCoefficientTimesProperty()
        {
            _caster.SetBaseProperty(AbilityProperties.SpellPower, 50f);
            var node = new EffectNodeData
            {
                Amount = 10f,
                AmountScaleProperty = AbilityProperties.SpellPower,
                AmountScalePerPoint = 0.5f,
                AmountScaleSource = ScaleAmountSource.Caster
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id);

            // WHY: 10 + 0.5 * 50
            Assert.That(LeveledValueResolver.ResolveAmount(node, ctx, _caster.Id), Is.EqualTo(35f).Within(0.001f));
        }

        [Test]
        public void PropertyScaling_FromTarget_ReadsTargetProperty()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            target.SetBaseProperty("bonus", 20f);
            var node = new EffectNodeData
            {
                Amount = 10f,
                AmountScaleProperty = "bonus",
                AmountScalePerPoint = 1f,
                AmountScaleSource = ScaleAmountSource.Target
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);

            // WHY: 10 + 1 * 20
            Assert.That(LeveledValueResolver.ResolveAmount(node, ctx, target.Id), Is.EqualTo(30f).Within(0.001f));
        }

        [Test]
        public void UnitLevel_DrivesResolution_ForCasterAndTarget()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2);
            _caster.SetLevel(3);
            target.SetLevel(2);
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);

            EffectNodeData casterScaled = AmountNode(new[] { 5f, 10f, 15f }, LevelSource.CasterUnitLevel);
            Assert.That(LeveledValueResolver.ResolveAmount(casterScaled, ctx, target.Id),
                Is.EqualTo(15f).Within(0.001f), "caster level 3 -> index 2");

            EffectNodeData targetScaled = AmountNode(new[] { 5f, 10f, 15f }, LevelSource.TargetUnitLevel);
            Assert.That(LeveledValueResolver.ResolveAmount(targetScaled, ctx, target.Id),
                Is.EqualTo(10f).Within(0.001f), "target level 2 -> index 1");
        }

        [Test]
        public void NamedSpecial_ResolvedByAmountKey()
        {
            var node = new EffectNodeData { AmountKey = "dmg" };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id);
            ctx.Specials = new List<AbilitySpecialValue>
            {
                new AbilitySpecialValue
                {
                    Name = "dmg",
                    Value = new LeveledValue { Levels = new[] { 7f, 14f }, LevelFrom = LevelSource.AbilityLevel }
                }
            };

            ctx.AbilityLevel = 1;
            Assert.That(LeveledValueResolver.ResolveAmount(node, ctx, _caster.Id), Is.EqualTo(7f).Within(0.001f));
            ctx.AbilityLevel = 2;
            Assert.That(LeveledValueResolver.ResolveAmount(node, ctx, _caster.Id), Is.EqualTo(14f).Within(0.001f));
        }

        [Test]
        public void Cast_UsesSlotLevel_ForLeveledDamage()
        {
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);

            var ability = new AbilityBlueprint
            {
                Id = "leveled_bolt",
                Targeting = TargetingMode.Unit,
                TeamFilter = AbilityTeamFilter.Enemies
            };
            ability.ImpactEffects.Add(AmountNode(new[] { 10f, 25f }, LevelSource.AbilityLevel));
            _system.RegisterAbility(ability);
            _system.GrantAbility(_caster.Id, ability.Id);

            _system.Cast(CastRequest.AtUnit(_caster.Id, ability.Id, enemy.Id));
            Assert.That(enemy.Health, Is.EqualTo(990f).Within(0.001f), "level 1 -> 10 damage");

            _system.SetAbilityLevel(_caster.Id, ability.Id, 2);
            _system.Cast(CastRequest.AtUnit(_caster.Id, ability.Id, enemy.Id));
            Assert.That(enemy.Health, Is.EqualTo(965f).Within(0.001f), "level 2 -> +25 damage");
        }

        [Test]
        public void ModifierDot_UsesCapturedApplyTimeLevel_NotLiveSlotLevel()
        {
            AbilityUnit victim = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);

            var burn = new ModifierBlueprint { Id = "burn_lvl", Duration = 0f, TickInterval = 1f };
            burn.TickEffects.Add(AmountNode(new[] { 4f, 9f }, LevelSource.AbilityLevel));
            _system.RegisterModifier(burn);

            var ability = new AbilityBlueprint
            {
                Id = "ignite",
                Targeting = TargetingMode.Unit,
                TeamFilter = AbilityTeamFilter.Enemies
            };
            ability.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.ApplyModifier,
                Target = EffectTargetSelector.Target,
                ModifierId = "burn_lvl"
            });
            _system.RegisterAbility(ability);
            _system.GrantAbility(_caster.Id, ability.Id);

            // WHY: Applied at ability level 1.
            _system.Cast(CastRequest.AtUnit(_caster.Id, ability.Id, victim.Id));
            _system.Tick(1f);
            Assert.That(victim.Health, Is.EqualTo(996f).Within(0.001f), "tick at captured level 1 -> 4");

            // WHY: Slot levels up, but the modifier already captured level 1 — ticks stay at level 1.
            _system.SetAbilityLevel(_caster.Id, ability.Id, 2);
            _system.Tick(1f);
            Assert.That(victim.Health, Is.EqualTo(992f).Within(0.001f), "captured, not live -> still 4");

            // WHY: Re-applying (recast) re-captures the current level.
            _system.Cast(CastRequest.AtUnit(_caster.Id, ability.Id, victim.Id));
            _system.Tick(1f);
            Assert.That(victim.Health, Is.EqualTo(983f).Within(0.001f), "recaptured level 2 -> 9");
        }

        [Test]
        public void LeveledRadius_GrowsWithAbilityLevel()
        {
            var world = new FakeWorldAdapter();
            _system.World = world;
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
            world.SetPosition(_caster.Id, Vector3.zero);
            world.SetPosition(enemy.Id, new Vector3(4f, 0f, 0f));

            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage,
                Target = EffectTargetSelector.AreaAroundCaster,
                TeamFilter = AbilityTeamFilter.Enemies,
                RadiusByLevel = new[] { 2f, 6f },
                Amount = 10f,
                DamageType = AbilityDamageTypes.Pure
            };

            EffectContext lvl1 = AbilityTestSupport.Context(_system, _caster.Id);
            lvl1.AbilityLevel = 1;
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), lvl1);
            Assert.That(enemy.Health, Is.EqualTo(1000f), "radius 2 at level 1 misses the enemy at distance 4");

            EffectContext lvl2 = AbilityTestSupport.Context(_system, _caster.Id);
            lvl2.AbilityLevel = 2;
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), lvl2);
            Assert.That(enemy.Health, Is.EqualTo(990f).Within(0.001f), "radius 6 at level 2 hits");
        }

        [Test]
        public void ModifierDuration_ResolvesByCapturedLevel()
        {
            AbilityUnit a = AbilityTestSupport.CreateUnit(_system, team: 2);
            AbilityUnit b = AbilityTestSupport.CreateUnit(_system, team: 2);
            var stun = new ModifierBlueprint { Id = "stun_lvl", DurationByLevel = new[] { 1f, 3f } };

            ModifierApplyResult r1 = _system.Modifiers.Apply(stun, _caster.Id, a.Id, "src", 1);
            Assert.That(r1.Instance.InitialDuration, Is.EqualTo(1f).Within(0.001f), "level 1 duration");

            ModifierApplyResult r2 = _system.Modifiers.Apply(stun, _caster.Id, b.Id, "src", 2);
            Assert.That(r2.Instance.InitialDuration, Is.EqualTo(3f).Within(0.001f), "level 2 duration");

            // WHY: Level-1 instance expires within 1.5s; level-3-duration instance survives.
            _system.Tick(1.5f);
            Assert.That(AbilityTestSupport.ActiveModifiers(_system, a.Id).Count, Is.EqualTo(0), "1s stun gone");
            Assert.That(AbilityTestSupport.ActiveModifiers(_system, b.Id).Count, Is.EqualTo(1), "3s stun stays");
        }

        [Test]
        public void SetAbilityLevel_ClampsToOne_AndFailsForUnknownSlot()
        {
            var ability = new AbilityBlueprint { Id = "ab" };
            _system.RegisterAbility(ability);
            _system.GrantAbility(_caster.Id, ability.Id);

            Assert.That(_system.SetAbilityLevel(_caster.Id, ability.Id, -5), Is.True);
            _system.TryGetSlot(_caster.Id, ability.Id, out AbilitySlot slot);
            Assert.That(slot.Level, Is.EqualTo(1), "negative clamps to 1");

            Assert.That(_system.SetAbilityLevel(_caster.Id, ability.Id, 4), Is.True);
            Assert.That(slot.Level, Is.EqualTo(4));

            slot.Level = 0;
            Assert.That(slot.Level, Is.EqualTo(1), "slot setter also clamps");

            Assert.That(_system.SetAbilityLevel(_caster.Id, "missing", 3), Is.False, "unknown slot -> false");
        }

        [Test]
        public void UnitSetLevel_ClampsToOne()
        {
            _caster.SetLevel(-3);
            Assert.That(_caster.Level, Is.EqualTo(1));
            _caster.SetLevel(5);
            Assert.That(_caster.Level, Is.EqualTo(5));
        }

        private float ResolveAt(EffectNodeData node, int level)
        {
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: _caster.Id);
            ctx.AbilityLevel = level;
            return LeveledValueResolver.ResolveAmount(node, ctx, _caster.Id);
        }
    }
}
