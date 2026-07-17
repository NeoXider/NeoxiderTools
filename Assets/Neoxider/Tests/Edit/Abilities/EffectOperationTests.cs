using System.Collections.Generic;
using Neo.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     Built-in effect operations, the area target selector, deterministic chance rolls and the
    ///     nested-execution depth cap.
    /// </summary>
    public sealed class EffectOperationTests
    {
        private AbilitySystem _system;
        private AbilityUnit _caster;

        [SetUp]
        public void SetUp()
        {
            _system = new AbilitySystem();
            _caster = AbilityTestSupport.CreateUnit(_system, team: 1);
        }

        [Test]
        public void DamageOp_DamagesResolvedTargets()
        {
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(_system, team: 2, health: 100f);
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: enemy.Id);

            _system.ExecuteEffects(AbilityTestSupport.Nodes(AbilityTestSupport.DamageNode(30f)), ctx);

            Assert.That(enemy.Health, Is.EqualTo(70f).Within(0.001f));
        }

        [Test]
        public void HealOp_RestoresHealth_AndFiresHealReceived()
        {
            var log = new EventLog(_system);
            AbilityUnit ally = AbilityTestSupport.CreateUnit(_system, team: 1, health: 100f, healthCurrent: 50f);
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: ally.Id);

            _system.ExecuteEffects(AbilityTestSupport.Nodes(AbilityTestSupport.HealNode(30f)), ctx);

            Assert.That(ally.Health, Is.EqualTo(80f).Within(0.001f));
            Assert.That(log.Count(AbilityEvents.HealReceived), Is.EqualTo(1));
        }

        [Test]
        public void HealOp_HonorsHealingReceivedMul()
        {
            AbilityUnit ally = AbilityTestSupport.CreateUnit(_system, team: 1, health: 100f, healthCurrent: 50f);
            ally.SetBaseProperty(AbilityProperties.HealingReceivedMul, 0.5f);
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: ally.Id);

            _system.ExecuteEffects(AbilityTestSupport.Nodes(AbilityTestSupport.HealNode(30f)), ctx);

            Assert.That(ally.Health, Is.EqualTo(65f).Within(0.001f), "30 heal * 0.5");
        }

        [Test]
        public void ApplyModifierOp_AttachesCatalogModifier()
        {
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(_system, team: 2);
            _system.RegisterModifier(AbilityTestSupport.Modifier("poison", 5f));

            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.ApplyModifier,
                Target = EffectTargetSelector.Target,
                ModifierId = "poison"
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: enemy.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            List<ModifierInstance> active = AbilityTestSupport.ActiveModifiers(_system, enemy.Id);
            Assert.That(active.Count, Is.EqualTo(1));
            Assert.That(active[0].Blueprint.Id, Is.EqualTo("poison"));
        }

        [Test]
        public void RemoveModifierOp_RemovesById()
        {
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(_system, team: 2);
            _system.Modifiers.Apply(AbilityTestSupport.Modifier("poison", 5f), _caster.Id, enemy.Id);

            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.RemoveModifier,
                Target = EffectTargetSelector.Target,
                ModifierId = "poison"
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: enemy.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            Assert.That(AbilityTestSupport.ActiveModifiers(_system, enemy.Id).Count, Is.EqualTo(0));
        }

        [Test]
        public void ResourceChangeOp_DrainsAndRestores()
        {
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(_system, team: 2, mana: 100f);

            var burn = new EffectNodeData
            {
                OpId = AbilityEffectOps.ResourceChange,
                Target = EffectTargetSelector.Target,
                ResourceId = AbilityResourceIds.Mana,
                Amount = -40f
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id, targets: enemy.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(burn), ctx);
            Assert.That(enemy.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(60f).Within(0.001f));

            var gain = new EffectNodeData
            {
                OpId = AbilityEffectOps.ResourceChange,
                Target = EffectTargetSelector.Target,
                ResourceId = AbilityResourceIds.Mana,
                Amount = 25f
            };
            _system.ExecuteEffects(AbilityTestSupport.Nodes(gain), AbilityTestSupport.Context(_system, _caster.Id, targets: enemy.Id));
            Assert.That(enemy.Resources.GetCurrent(AbilityResourceIds.Mana), Is.EqualTo(85f).Within(0.001f));
        }

        [Test]
        public void AreaSelector_HitsOnlyLivingTeamFilteredUnitsInRadius()
        {
            var world = new FakeWorldAdapter();
            _system.World = world;

            AbilityUnit enemyIn = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
            AbilityUnit enemyFar = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
            AbilityUnit allyIn = AbilityTestSupport.CreateUnit(_system, team: 1, health: 1000f);
            AbilityUnit deadEnemy = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);

            world.SetPosition(_caster.Id, Vector3.zero);
            world.SetPosition(enemyIn.Id, new Vector3(3f, 0f, 0f));   // WHY: inside radius, enemy -> hit
            world.SetPosition(enemyFar.Id, new Vector3(10f, 0f, 0f)); // WHY: outside radius -> miss
            world.SetPosition(allyIn.Id, new Vector3(2f, 0f, 0f));    // WHY: inside, ally -> filtered out
            world.SetPosition(deadEnemy.Id, new Vector3(1f, 0f, 0f)); // WHY: inside, enemy but dead -> skipped
            _system.MarkDead(deadEnemy, UnitId.None);

            var node = new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage,
                Target = EffectTargetSelector.AreaAroundCaster,
                TeamFilter = AbilityTeamFilter.Enemies,
                Radius = 5f,
                Amount = 10f,
                DamageType = AbilityDamageTypes.Pure
            };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            Assert.That(enemyIn.Health, Is.EqualTo(990f).Within(0.001f), "living enemy in radius is hit");
            Assert.That(enemyFar.Health, Is.EqualTo(1000f), "out of radius spared");
            Assert.That(allyIn.Health, Is.EqualTo(1000f), "ally spared by team filter");
        }

        [Test]
        public void Chance_Zero_NeverExecutes_One_AlwaysExecutes()
        {
            AbilityUnit a = AbilityTestSupport.CreateUnit(_system, team: 2, health: 100f);
            AbilityUnit b = AbilityTestSupport.CreateUnit(_system, team: 2, health: 100f);

            EffectNodeData never = AbilityTestSupport.DamageNode(10f);
            never.Chance = 0f;
            _system.ExecuteEffects(AbilityTestSupport.Nodes(never), AbilityTestSupport.Context(_system, _caster.Id, targets: a.Id));
            Assert.That(a.Health, Is.EqualTo(100f), "chance 0 never runs");

            EffectNodeData always = AbilityTestSupport.DamageNode(10f);
            always.Chance = 1f;
            _system.ExecuteEffects(AbilityTestSupport.Nodes(always), AbilityTestSupport.Context(_system, _caster.Id, targets: b.Id));
            Assert.That(b.Health, Is.EqualTo(90f).Within(0.001f), "chance 1 always runs");
        }

        [Test]
        public void Chance_IsDeterministicUnderFixedSeed()
        {
            const uint seed = 424242u;

            float RunProbabilistic(uint runSeed)
            {
                AbilityUnit enemy = AbilityTestSupport.CreateUnit(_system, team: 2, health: 1000f);
                var nodes = new List<EffectNodeData>();
                for (int i = 0; i < 8; i++)
                {
                    EffectNodeData n = AbilityTestSupport.DamageNode(10f);
                    n.Chance = 0.5f;
                    nodes.Add(n);
                }

                _system.ExecuteEffects(nodes, AbilityTestSupport.Context(_system, _caster.Id, runSeed, enemy.Id));
                return 1000f - enemy.Health;
            }

            float first = RunProbabilistic(seed);
            float second = RunProbabilistic(seed);

            Assert.That(second, Is.EqualTo(first), "same seed -> same set of chance rolls");
            // WHY: For this seed exactly 4 of the 8 half-chance nodes roll through (10 damage each).
            Assert.That(first, Is.EqualTo(40f).Within(0.001f));
        }

        [Test]
        public void NestedExecution_IsCappedAtMaxDepth()
        {
            var recursive = new RecursiveOp();
            _system.Ops.Register(recursive);

            var node = new EffectNodeData { OpId = recursive.Id, Target = EffectTargetSelector.Caster };
            EffectContext ctx = AbilityTestSupport.Context(_system, _caster.Id);
            _system.ExecuteEffects(AbilityTestSupport.Nodes(node), ctx);

            Assert.That(recursive.Executions, Is.EqualTo(EffectContext.MaxDepth),
                "recursion stops once context depth reaches MaxDepth");
        }

        private sealed class RecursiveOp : IEffectOperation
        {
            public int Executions;
            public string Id => "recurse_test";

            public void Execute(EffectContext context, EffectNodeData node, List<UnitId> targets)
            {
                Executions++;
                EffectContext nested = context.CreateNested(context.Caster, context.AbilityId, targets);
                context.System.ExecuteEffects(new List<EffectNodeData> { node }, nested);
            }
        }
    }
}
