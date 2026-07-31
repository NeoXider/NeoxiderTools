using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     The "chain" (bounce with falloff, team-filtered, deterministic hop order) and "execute"
    ///     (health-derived damage: missing / max / current / flat) effect ops.
    /// </summary>
    public sealed class ChainAndExecuteOpTests
    {
        private AbilitySystem _system;
        private FakeWorldAdapter _world;
        private AbilityUnit _caster;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _world = new FakeWorldAdapter();
            _system.World = _world;
            _caster = AbilityTestSupport.CreateUnit(_system, team: 1);
            _world.SetPosition(_caster.Id, Vector3.zero);
        }

        // ---------------------------------------------------------------- chain

        [Test]
        public void Chain_BouncesNearestFirstWithFalloff()
        {
            AbilityUnit a = AbilityTestSupport.CreateUnit(_system, team: 2);
            AbilityUnit b = AbilityTestSupport.CreateUnit(_system, team: 2);
            AbilityUnit c = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(a.Id, new Vector3(1f, 0f, 0f));
            _world.SetPosition(b.Id, new Vector3(2f, 0f, 0f));
            _world.SetPosition(c.Id, new Vector3(3f, 0f, 0f));

            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.Chain,
                Target = EffectTargetSelector.Target,
                TeamFilter = AbilityTeamFilter.Enemies,
                DamageType = AbilityDamageTypes.Pure,
                Amount = 100f,
                Radius = 5f,
                MaxTargets = 3,
                CustomParam = "0.5"
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: a.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            // Hop order A -> B -> C, damage 100 / 50 / 25 (each bounce multiplies by falloff 0.5).
            Assert.That(a.Health, Is.EqualTo(AbilityTestSupport.DefaultHealth - 100f).Within(0.001f));
            Assert.That(b.Health, Is.EqualTo(AbilityTestSupport.DefaultHealth - 50f).Within(0.001f));
            Assert.That(c.Health, Is.EqualTo(AbilityTestSupport.DefaultHealth - 25f).Within(0.001f));
        }

        [Test]
        public void Chain_StopsAtMaxTargets()
        {
            AbilityUnit a = AbilityTestSupport.CreateUnit(_system, team: 2);
            AbilityUnit b = AbilityTestSupport.CreateUnit(_system, team: 2);
            AbilityUnit c = AbilityTestSupport.CreateUnit(_system, team: 2);
            _world.SetPosition(a.Id, new Vector3(1f, 0f, 0f));
            _world.SetPosition(b.Id, new Vector3(2f, 0f, 0f));
            _world.SetPosition(c.Id, new Vector3(3f, 0f, 0f));

            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.Chain,
                Target = EffectTargetSelector.Target,
                TeamFilter = AbilityTeamFilter.Enemies,
                DamageType = AbilityDamageTypes.Pure,
                Amount = 100f,
                Radius = 5f,
                MaxTargets = 2 // only the first two links take damage
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: a.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            Assert.That(a.Health, Is.LessThan(AbilityTestSupport.DefaultHealth));
            Assert.That(b.Health, Is.LessThan(AbilityTestSupport.DefaultHealth));
            Assert.That(c.Health, Is.EqualTo(AbilityTestSupport.DefaultHealth).Within(0.001f)); // never reached
        }

        [Test]
        public void Chain_SkipsAllies()
        {
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(_system, team: 2);
            AbilityUnit ally = AbilityTestSupport.CreateUnit(_system, team: 1); // same team as caster
            _world.SetPosition(enemy.Id, new Vector3(1f, 0f, 0f));
            _world.SetPosition(ally.Id, new Vector3(2f, 0f, 0f));

            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.Chain,
                Target = EffectTargetSelector.Target,
                TeamFilter = AbilityTeamFilter.Enemies,
                DamageType = AbilityDamageTypes.Pure,
                Amount = 100f,
                Radius = 5f,
                MaxTargets = 3
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: enemy.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            Assert.That(enemy.Health, Is.EqualTo(AbilityTestSupport.DefaultHealth - 100f).Within(0.001f));
            Assert.That(ally.Health, Is.EqualTo(AbilityTestSupport.DefaultHealth).Within(0.001f)); // filtered out
        }

        // ---------------------------------------------------------------- execute

        [Test]
        public void Execute_MissingHealthFraction()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2, health: 200f, healthCurrent: 120f);
            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.Execute,
                Target = EffectTargetSelector.Target,
                DamageType = AbilityDamageTypes.Pure,
                Amount = 0.5f,
                CustomParam = "missing" // 0.5 * (200 - 120) = 40
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            Assert.That(target.Health, Is.EqualTo(80f).Within(0.001f));
        }

        [Test]
        public void Execute_MaxHealthFraction()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2, health: 200f);
            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.Execute,
                Target = EffectTargetSelector.Target,
                DamageType = AbilityDamageTypes.Pure,
                Amount = 0.25f,
                CustomParam = "max" // 0.25 * 200 = 50
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            Assert.That(target.Health, Is.EqualTo(150f).Within(0.001f));
        }

        [Test]
        public void Execute_NoModeIsFlatDamage()
        {
            AbilityUnit target = AbilityTestSupport.CreateUnit(_system, team: 2, health: 200f);
            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.Execute,
                Target = EffectTargetSelector.Target,
                DamageType = AbilityDamageTypes.Pure,
                Amount = 30f // no CustomParam => flat 30
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: target.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            Assert.That(target.Health, Is.EqualTo(170f).Within(0.001f));
        }
    }
}
