using Neo.Abilities;
using Neo.Tests.Performance;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     <see cref="ModifierBlueprint.RestartTicksOnRefresh" />: a re-application that refreshes, stacks or prolongs
    ///     restarts the tick phase (the next tick a full interval later); without it the phase continues.
    /// </summary>
    public sealed class ModifierTickRestartTests
    {
        [TestCase(ModifierStackPolicy.Refresh, false)]
        [TestCase(ModifierStackPolicy.Refresh, true)]
        [TestCase(ModifierStackPolicy.Stack, true)]
        [TestCase(ModifierStackPolicy.Prolong, true)]
        public void Refresh_RestartsTheTickPhaseOnlyWhenAsked(ModifierStackPolicy policy, bool restart)
        {
            AbilitySystem system = new AbilitySystem();
            DeterministicAbilityProfile.Apply(system.Rules, 30);
            AbilityUnit hero = AbilityTestSupport.CreateUnit(system, team: 1);
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(system, team: 2, health: 100f);
            ModifierBlueprint burn = Burn(policy, restart);
            system.RegisterModifier(burn);

            system.ApplyModifier("burn", hero.Id, enemy.Id);
            system.StepTicks(20);
            system.ApplyModifier("burn", hero.Id, enemy.Id);
            system.StepTicks(10);
            Assert.That(enemy.Health, Is.EqualTo(restart ? 100f : 99f), "tick 30 belongs to the old phase");
            system.StepTicks(19);
            Assert.That(enemy.Health, Is.EqualTo(restart ? 100f : 99f));
            system.StepTicks(1);
            Assert.That(enemy.Health, Is.EqualTo(restart ? 99f : 99f), "a restarted phase ticks 30 steps after the refresh");
            system.StepTicks(10);
            Assert.That(enemy.Health, Is.EqualTo(restart ? 99f : 98f));
        }

        [Test]
        public void IgnoredReapplication_DoesNotRestartThePhase()
        {
            AbilitySystem system = new AbilitySystem();
            DeterministicAbilityProfile.Apply(system.Rules, 30);
            AbilityUnit hero = AbilityTestSupport.CreateUnit(system, team: 1);
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(system, team: 2, health: 100f);
            system.RegisterModifier(Burn(ModifierStackPolicy.Unique, true));
            system.ApplyModifier("burn", hero.Id, enemy.Id);
            system.StepTicks(20);
            system.ApplyModifier("burn", hero.Id, enemy.Id);
            system.StepTicks(10);
            Assert.That(enemy.Health, Is.EqualTo(99f), "Unique ignores the re-application entirely");
        }

        [Test]
        public void RestartingRefreshes_AllocateNothingWhenWarm()
        {
            AbilitySystem system = new AbilitySystem();
            DeterministicAbilityProfile.Apply(system.Rules, 30);
            AbilityUnit hero = AbilityTestSupport.CreateUnit(system, team: 1);
            AbilityUnit enemy = AbilityTestSupport.CreateUnit(system, team: 2, health: 1e6f);
            ModifierBlueprint burn = Burn(ModifierStackPolicy.Stack, true);
            burn.MaxStacks = 3;
            burn.TickInterval = 2f / 30f;
            system.RegisterModifier(burn);
            for (int i = 0; i < 4; i++)
            {
                system.ApplyModifier("burn", hero.Id, enemy.Id);
                system.StepTicks(3);
            }

            using GcAllocCounter counter = GcAllocCounter.CreateOrIgnore();
            counter.Begin();
            for (int i = 0; i < 200; i++)
            {
                system.ApplyModifier("burn", hero.Id, enemy.Id);
                system.StepTicks(3);
            }

            Assert.That(counter.End(), Is.EqualTo(0L));
            Assert.That(enemy.Health, Is.LessThan(1e6f));
        }

        private static ModifierBlueprint Burn(ModifierStackPolicy policy, bool restart)
        {
            ModifierBlueprint burn = AbilityTestSupport.Modifier("burn", 3f, policy);
            burn.MaxStacks = policy == ModifierStackPolicy.Stack ? 1 : burn.MaxStacks;
            burn.TickInterval = 1f;
            burn.RestartTicksOnRefresh = restart;
            burn.TickEffects.Add(AbilityTestSupport.DamageNode(1f));
            return burn;
        }
    }
}
