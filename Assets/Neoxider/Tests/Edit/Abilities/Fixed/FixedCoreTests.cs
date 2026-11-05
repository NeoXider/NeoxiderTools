using System.Collections.Generic;
using Neo.Abilities.Fixed;
using NUnit.Framework;
using static Neo.Editor.Tests.Abilities.Fixed.FixedTestSupport;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>
    ///     Public contracts of the fixed core on its own: damage formula and hooks, shields, stack policies, every
    ///     trigger kind, zones, projectiles, casts, regeneration, rules replacement and the state hash.
    /// </summary>
    public sealed class FixedCoreTests
    {
        [Test]
        public void Damage_UsesExactArmorAndResistRatios()
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1);
            FixedUnit golem = Unit(system, 2, 600d);
            golem.SetBaseProperty(FixedProperties.Armor, Q(5d));
            FixedDamageResult physical = system.Damage.Apply(hero.Id, golem.Id, Q(40d), FixedDamageTypes.Physical);
            Assert.That(physical.HealthDamageRaw, Is.EqualTo(7877), "40 x 100 / (100 + 6 x 5) = 30.769");

            golem.SetBaseProperty(FixedProperties.Armor, Q(-4d));
            Assert.That(system.Damage.Apply(hero.Id, golem.Id, Q(40d), FixedDamageTypes.Physical).HealthDamageRaw,
                Is.EqualTo(FixedMath.MulDivToInt(Q(40d), 100 * 256 + 12 * 4 * 256, 100 * 256 + 6 * 4 * 256)));

            golem.SetBaseProperty(FixedProperties.MagicResistPercent, Q(25d));
            Assert.That(system.Damage.Apply(hero.Id, golem.Id, Q(40d), FixedDamageTypes.Magical).HealthDamageRaw,
                Is.EqualTo(Q(30d)));
            Assert.That(system.Damage.Apply(hero.Id, golem.Id, Q(40d), FixedDamageTypes.Pure).HealthDamageRaw,
                Is.EqualTo(Q(40d)));
        }

        [Test]
        public void Damage_ImmunityDeathKillAndLifesteal()
        {
            FixedAbilitySystem system = NewSystem();
            FixedEventLog log = new FixedEventLog(system);
            FixedUnit hero = Unit(system, 1, 100d);
            hero.SetCurrent(FixedResources.Health, Q(50d));
            hero.SetBaseProperty(FixedProperties.LifestealPercent, Q(50d));
            FixedUnit golem = Unit(system, 2, 30d);
            golem.SetPermanentState(FixedStates.Invulnerable, true);
            Assert.That(system.Damage.Apply(hero.Id, golem.Id, Q(10d), FixedDamageTypes.Pure).Negated, Is.True);
            golem.SetPermanentState(FixedStates.Invulnerable, false);

            FixedDamageResult result = system.Damage.Apply(hero.Id, golem.Id, Q(50d), FixedDamageTypes.Pure);
            Assert.That(result.Killed, Is.True);
            Assert.That(result.HealthDamageRaw, Is.EqualTo(Q(30d)), "only the remaining health is removed");
            Assert.That(golem.IsAlive, Is.False);
            Assert.That(hero.HealthRaw, Is.EqualTo(Q(65d)), "half of 30 healed back");
            Assert.That(log.Count(FixedEvents.Death), Is.EqualTo(1));
            Assert.That(log.Count(FixedEvents.Kill), Is.EqualTo(1));
            Assert.That(system.Damage.Apply(hero.Id, golem.Id, Q(5d), FixedDamageTypes.Pure).Negated, Is.True);
        }

        [Test]
        public void Hooks_MultiplyAddAbsorbCancelAndReflect()
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1);
            FixedUnit golem = Unit(system, 2);
            FixedModifierBlueprint power = Modifier("power");
            power.EventReactions.Add(Hook(FixedEvents.BeforeDealDamage, FixedOps.ModeMultiply, 1.5d));
            power.EventReactions.Add(Hook(FixedEvents.BeforeDealDamage, FixedOps.ModeAdd, 4d));
            FixedModifierBlueprint guard = Modifier("guard");
            guard.EventReactions.Add(Hook(FixedEvents.BeforeTakeDamage, FixedOps.ModeAbsorb, 6d));
            FixedModifierReaction thorns = new FixedModifierReaction { EventId = FixedEvents.BeforeTakeDamage };
            thorns.Effects.Add(Node(FixedOps.Reflect, 0.25d));
            guard.EventReactions.Add(thorns);
            system.Modifiers.Apply(power, hero.Id, hero.Id);
            system.Modifiers.Apply(guard, golem.Id, golem.Id);

            FixedDamageResult result = system.Damage.Apply(hero.Id, golem.Id, Q(20d), FixedDamageTypes.Pure);
            Assert.That(result.MitigatedRaw, Is.EqualTo(Q(36d)), "(20 + 4) x 1.5");
            Assert.That(result.AbsorbedRaw, Is.EqualTo(Q(6d)));
            Assert.That(result.HealthDamageRaw, Is.EqualTo(Q(30d)));
            Assert.That(result.ReflectedRaw, Is.EqualTo(Q(9d)), "a quarter of the mitigated damage");
            Assert.That(hero.HealthRaw, Is.EqualTo(Q(991d)), "the reflection is not reflected again");

            FixedModifierBlueprint ward = Modifier("ward");
            ward.EventReactions.Add(Hook(FixedEvents.BeforeTakeDamage, FixedOps.ModeCancel, 0d));
            system.Modifiers.Apply(ward, golem.Id, golem.Id);
            Assert.That(system.Damage.Apply(hero.Id, golem.Id, Q(20d), FixedDamageTypes.Pure).Cancelled, Is.True);
        }

        [Test]
        public void Shields_AbsorbBreakAndBypassPureDamage()
        {
            FixedAbilitySystem system = NewSystem();
            FixedEventLog log = new FixedEventLog(system);
            FixedUnit hero = Unit(system, 1);
            FixedUnit golem = Unit(system, 2, 100d);
            FixedModifierBlueprint barrier = Modifier("barrier", 300).WithProperty(FixedProperties.ShieldHp, FixedPropertyOp.Add, Q(15d));
            system.Modifiers.Apply(barrier, golem.Id, golem.Id);
            system.Damage.Apply(hero.Id, golem.Id, Q(10d), FixedDamageTypes.Magical);
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(100d)));
            Assert.That(system.Modifiers.TryGetActive(golem.Id, "barrier", out FixedModifierInstance shield), Is.True);
            Assert.That(FixedDamagePipeline.RemainingShield(shield), Is.EqualTo(Q(5d)));
            system.Damage.Apply(hero.Id, golem.Id, Q(10d), FixedDamageTypes.Magical);
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(95d)));
            Assert.That(system.Modifiers.CountActive(golem.Id, "barrier"), Is.Zero);
            Assert.That(log.Count(FixedEvents.ShieldBroken), Is.EqualTo(1));

            system.Modifiers.Apply(barrier, golem.Id, golem.Id);
            system.Damage.Apply(hero.Id, golem.Id, Q(10d), FixedDamageTypes.Pure);
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(85d)), "pure damage ignores shields");
        }

        [TestCase(FixedStackPolicy.Refresh, 1, 1, 60)]
        [TestCase(FixedStackPolicy.Stack, 1, 3, 60)]
        [TestCase(FixedStackPolicy.Unique, 1, 1, 40)]
        [TestCase(FixedStackPolicy.Prolong, 1, 1, 160)]
        [TestCase(FixedStackPolicy.Independent, 3, 1, 40)]
        [TestCase(FixedStackPolicy.IndependentCapped, 2, 1, 50)]
        public void StackPolicies_DecideReapplications(FixedStackPolicy policy, int count, int stacks, int remaining)
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit golem = Unit(system, 2);
            FixedModifierBlueprint blueprint = Modifier("p", 60, policy);
            blueprint.MaxStacks = policy == FixedStackPolicy.IndependentCapped ? 2 : 5;
            for (int i = 0; i < 3; i++)
            {
                system.Modifiers.Apply(blueprint, golem.Id, golem.Id);
                if (i < 2)
                {
                    system.Step(10);
                }
            }

            Assert.That(system.Modifiers.CountActive(golem.Id, "p"), Is.EqualTo(count));
            Assert.That(system.Modifiers.TryGetActive(golem.Id, "p", out FixedModifierInstance oldest), Is.True);
            Assert.That(oldest.Stacks, Is.EqualTo(stacks));
            Assert.That(oldest.RemainingTicks, Is.EqualTo(remaining));
        }

        [Test]
        public void Modifiers_TickOnIntervalsAndExpireOnTheLastTick()
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1);
            FixedUnit golem = Unit(system, 2, 100d);
            FixedModifierBlueprint burn = Modifier("burn", 150);
            burn.TickIntervalTicks = 30;
            burn.TickEffects.Add(Node(FixedOps.Damage, 4d));
            burn.TickEffects[0].AmountPerStackRaw = Q(2d);
            burn.StackPolicy = FixedStackPolicy.Stack;
            burn.MaxStacks = 3;
            system.Modifiers.Apply(burn, hero.Id, golem.Id);
            system.Modifiers.Apply(burn, hero.Id, golem.Id);
            system.Step(149);
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(100d - 4d * 6d)), "four ticks of 4 + 2 per extra stack");
            system.Step(1);
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(100d - 5d * 6d)), "the fifth tick lands on the expiry step");
            Assert.That(system.Modifiers.CountActive(golem.Id, "burn"), Is.Zero);
        }

        [TestCase(FixedStackPolicy.Refresh, false)]
        [TestCase(FixedStackPolicy.Refresh, true)]
        [TestCase(FixedStackPolicy.Stack, true)]
        [TestCase(FixedStackPolicy.Prolong, true)]
        [TestCase(FixedStackPolicy.Unique, true)]
        public void Modifiers_RestartTheTickPhaseOnRefreshOnlyWhenAsked(FixedStackPolicy policy, bool restart)
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1);
            FixedUnit golem = Unit(system, 2, 100d);
            FixedModifierBlueprint burn = Modifier("burn", 90, policy);
            burn.TickIntervalTicks = 30;
            burn.RestartTicksOnRefresh = restart;
            burn.TickEffects.Add(Node(FixedOps.Damage, 1d));
            bool restarts = restart && policy != FixedStackPolicy.Unique;

            system.Modifiers.Apply(burn, hero.Id, golem.Id);
            system.Step(20);
            system.Modifiers.Apply(burn, hero.Id, golem.Id);
            system.Step(10);
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(restarts ? 100d : 99d)), "tick 30 belongs to the old phase");
            system.Step(20);
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(99d)), "a restarted phase ticks 30 steps after the refresh");
            system.Step(10);
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(restarts ? 99d : 98d)));
        }

        [Test]
        public void Triggers_EveryNthChargesCooldownAndRemoveWhenSpent()
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1, mana: 100d);
            FixedUnit golem = Unit(system, 2);
            FixedModifierBlueprint counter = Modifier("counter");
            FixedModifierReaction every = new FixedModifierReaction
            {
                EventId = FixedEvents.DealDamage, Trigger = FixedModifierTrigger.EveryNth, EveryN = 2, CooldownTicks = 3
            };
            every.Effects.Add(ManaChange(-1d));
            counter.EventReactions.Add(every);
            FixedModifierBlueprint mark = Modifier("mark");
            FixedModifierReaction charges = new FixedModifierReaction
                { EventId = FixedEvents.DealDamage, MaxTriggers = 2, RemoveWhenSpent = true };
            charges.Effects.Add(ManaChange(-10d));
            mark.EventReactions.Add(charges);
            system.Modifiers.Apply(counter, hero.Id, hero.Id);
            system.Modifiers.Apply(mark, hero.Id, hero.Id);

            for (int i = 0; i < 8; i++)
            {
                system.Damage.Apply(hero.Id, golem.Id, Q(1d), FixedDamageTypes.Pure);
                system.Step(i < 4 ? 1 : 3);
            }

            Assert.That(system.Modifiers.TryGetActive(hero.Id, "counter", out FixedModifierInstance instance), Is.True);
            Assert.That(instance.GetReactionCounter(0), Is.EqualTo(8));
            Assert.That(instance.GetReactionFireCount(0), Is.EqualTo(3), "hits 2 and 6 and 8 fire; hit 4 is on cooldown");
            Assert.That(system.Modifiers.CountActive(hero.Id, "mark"), Is.Zero, "spent after two charges");
            Assert.That(hero.ManaRaw, Is.EqualTo(Q(100d - 20d - 3d)));
        }

        [Test]
        public void Triggers_HealthBelowIsAnEdgeThatRearmsAfterHealing()
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1, 100d, mana: 100d);
            FixedModifierBlueprint guardian = Modifier("guardian");
            FixedModifierReaction below = new FixedModifierReaction
                { EventId = FixedEvents.TakeDamage, Trigger = FixedModifierTrigger.HealthBelow, ThresholdRaw = Q(0.5d) };
            below.Effects.Add(ManaChange(-1d));
            guardian.EventReactions.Add(below);
            system.Modifiers.Apply(guardian, hero.Id, hero.Id);

            system.Damage.Apply(0, hero.Id, Q(50d), FixedDamageTypes.Pure);
            Assert.That(hero.ManaRaw, Is.EqualTo(Q(100d)), "50 is not below half");
            system.Damage.Apply(0, hero.Id, Q(1d), FixedDamageTypes.Pure);
            system.Damage.Apply(0, hero.Id, Q(1d), FixedDamageTypes.Pure);
            Assert.That(hero.ManaRaw, Is.EqualTo(Q(99d)), "fires once on the crossing");
            hero.Increase(FixedResources.Health, Q(30d));
            system.Damage.Apply(0, hero.Id, Q(40d), FixedDamageTypes.Pure);
            Assert.That(hero.ManaRaw, Is.EqualTo(Q(98d)), "healing above re-arms it");
        }

        [Test]
        public void Triggers_PriorityStateLifecycleTagAndHitFilters()
        {
            FixedAbilitySystem system = NewSystem();
            FixedEventLog log = new FixedEventLog(system);
            FixedUnit hero = Unit(system, 1, mana: 100d);
            List<string> order = new List<string>();
            system.Events.Subscribe(FixedEvents.HealReceived, (in FixedEventArgs args) => order.Add("heal"));

            FixedModifierBlueprint low = Modifier("low");
            FixedModifierReaction lowReaction = new FixedModifierReaction { EventId = FixedEvents.AnimationEvent, TagFilter = "hit_frame", Priority = 1 };
            lowReaction.Effects.Add(ManaChange(-1d));
            low.EventReactions.Add(lowReaction);
            FixedModifierBlueprint high = Modifier("high");
            FixedModifierReaction highReaction = new FixedModifierReaction { EventId = FixedEvents.AnimationEvent, Priority = 5 };
            highReaction.Effects.Add(ManaChange(-10d));
            high.EventReactions.Add(highReaction);
            system.Modifiers.Apply(low, hero.Id, hero.Id);
            system.Modifiers.Apply(high, hero.Id, hero.Id);
            system.RaiseAnimationEvent(hero.Id, "wind_up");
            Assert.That(hero.ManaRaw, Is.EqualTo(Q(90d)), "the tag filter skips the low reaction");
            system.RaiseAnimationEvent(hero.Id, "hit_frame");
            Assert.That(hero.ManaRaw, Is.EqualTo(Q(79d)));

            FixedModifierBlueprint watcher = Modifier("watcher");
            FixedModifierReaction enter = new FixedModifierReaction { Trigger = FixedModifierTrigger.StateEnter, StateId = FixedStates.Stunned };
            enter.Effects.Add(ManaChange(-5d));
            FixedModifierReaction exit = new FixedModifierReaction { Trigger = FixedModifierTrigger.StateExit, StateId = FixedStates.Stunned };
            exit.Effects.Add(ManaChange(-2d));
            FixedModifierReaction applied = new FixedModifierReaction { Trigger = FixedModifierTrigger.Applied };
            applied.Effects.Add(ManaChange(-3d));
            watcher.EventReactions.Add(enter);
            watcher.EventReactions.Add(exit);
            watcher.EventReactions.Add(applied);
            system.Modifiers.Apply(watcher, hero.Id, hero.Id);
            Assert.That(hero.ManaRaw, Is.EqualTo(Q(76d)), "applied fires once");
            FixedModifierBlueprint stun = Modifier("stun", 10);
            stun.States.Add(FixedStates.Stunned);
            system.Modifiers.Apply(stun, hero.Id, hero.Id);
            Assert.That(hero.ManaRaw, Is.EqualTo(Q(71d)));
            system.Step(10);
            Assert.That(hero.ManaRaw, Is.EqualTo(Q(69d)));
            Assert.That(log.Count(FixedEvents.StateEntered), Is.EqualTo(1));
            Assert.That(log.Count(FixedEvents.StateExited), Is.EqualTo(1));

            FixedModifierBlueprint onHit = Modifier("on_hit");
            FixedModifierReaction hitsOnly = new FixedModifierReaction { EventId = FixedEvents.BeforeTakeDamage, HitsOnly = true };
            hitsOnly.Effects.Add(ManaChange(-1d));
            onHit.EventReactions.Add(hitsOnly);
            system.Modifiers.Apply(onHit, hero.Id, hero.Id);
            system.Damage.Apply(0, hero.Id, Q(1d), FixedDamageTypes.Pure);
            Assert.That(hero.ManaRaw, Is.EqualTo(Q(69d)), "damage without the Hit flag is not a hit");
            system.Damage.Apply(0, hero.Id, Q(1d), FixedDamageTypes.Pure, flags: FixedDamageFlags.Hit);
            Assert.That(hero.ManaRaw, Is.EqualTo(Q(68d)));
            Assert.That(order, Is.Empty);
        }

        [Test]
        public void Zones_PulseOnSpawnAndIntervalsFollowAndExpire()
        {
            FixedAbilitySystem system = NewSystem();
            FixedEventLog log = new FixedEventLog(system);
            FixedUnit hero = Unit(system, 1);
            FixedUnit golem = Unit(system, 2, 100d, x: 1d);
            FixedUnit ally = Unit(system, 1, 100d, x: 1d);
            FixedUnit far = Unit(system, 2, 100d, x: 9d);
            FixedAbilityBlueprint pulse = Ability("pulse", FixedTargeting.NoTarget, Node(FixedOps.Damage, 5d));
            system.RegisterAbility(pulse);
            int id = system.SpawnZone(new FixedZoneSpec
            {
                Caster = hero.Id, Anchor = hero.Id, FollowAnchor = true, RadiusRaw = Q(3d), DurationTicks = 90,
                IntervalTicks = 30, PayloadAbilityId = "pulse", TeamFilter = FixedTeamFilter.Enemies
            });
            Assert.That(id, Is.GreaterThan(0));
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(95d)), "pulses on spawn");
            system.Step(30);
            hero.Position = new FixedVec3(Q(8d), 0, 0);
            system.Step(30);
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(90d)));
            Assert.That(far.HealthRaw, Is.EqualTo(Q(95d)), "the zone followed its anchor");
            system.Step(30);
            Assert.That(system.ZoneCount, Is.Zero);
            Assert.That(far.HealthRaw, Is.EqualTo(Q(90d)), "the last pulse lands on the expiry step");
            Assert.That(ally.HealthRaw, Is.EqualTo(Q(100d)));
            Assert.That(log.Count(FixedEvents.ZoneExpired), Is.EqualTo(1));

            system.SpawnZone(new FixedZoneSpec { Caster = hero.Id, Anchor = golem.Id, RadiusRaw = Q(1d), DurationTicks = 0 });
            system.Damage.Apply(hero.Id, golem.Id, Q(500d), FixedDamageTypes.Pure);
            system.Step();
            Assert.That(system.ZoneCount, Is.Zero, "a zone ends with its anchor");
        }

        [Test]
        public void Casts_CheckCooldownChargesCostsRangeAndStates()
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1, mana: 30d);
            FixedUnit golem = Unit(system, 2, x: 4d);
            FixedAbilityBlueprint bolt = Ability("bolt", FixedTargeting.Unit, Node(FixedOps.Damage, 10d));
            bolt.CooldownTicks = 30;
            bolt.RangeRaw = Q(5d);
            bolt.Costs.Add(new FixedAbilityCost(FixedResources.Mana, Q(10d)));
            system.RegisterAbility(bolt);
            FixedCastRequest request = FixedCastRequest.AtUnit(hero.Id, "bolt", golem.Id);
            Assert.That(system.Cast(in request).Failure, Is.EqualTo(FixedCastFailure.NotGranted));
            system.GrantAbility(hero.Id, "bolt");
            Assert.That(system.Cast(in request).Success, Is.True);
            Assert.That(system.Cast(in request).Failure, Is.EqualTo(FixedCastFailure.OnCooldown));
            hero.SetBaseProperty(FixedProperties.CooldownReductionPercent, Q(25d));
            system.Step(30);
            Assert.That(system.Cast(in request).Success, Is.True);
            Assert.That(system.TryGetSlot(hero.Id, "bolt", out FixedAbilitySlot slot), Is.True);
            Assert.That(slot.CooldownRemainingTicks, Is.EqualTo(23), "22.5 ticks end on the 23rd");
            system.Step(23);
            golem.Position = new FixedVec3(Q(6d), 0, 0);
            Assert.That(system.Cast(in request).Failure, Is.EqualTo(FixedCastFailure.OutOfRange));
            golem.Position = new FixedVec3(Q(5d), 0, 0);
            Assert.That(system.Cast(in request).Success, Is.True, "the range is inclusive");
            system.Step(23);
            Assert.That(system.Cast(in request).Failure, Is.EqualTo(FixedCastFailure.NotEnoughResources));
            hero.SetPermanentState(FixedStates.Silenced, true);
            Assert.That(system.Cast(in request).Failure, Is.EqualTo(FixedCastFailure.Silenced));
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(970d)));

            FixedAbilityBlueprint dash = Ability("dash", FixedTargeting.NoTarget);
            dash.MaxCharges = 2;
            dash.ChargeRestoreTicks = 10;
            system.RegisterAbility(dash);
            system.GrantAbility(golem.Id, "dash");
            FixedCastRequest dashRequest = FixedCastRequest.NoTarget(golem.Id, "dash");
            Assert.That(system.Cast(in dashRequest).Success && system.Cast(in dashRequest).Success, Is.True);
            Assert.That(system.Cast(in dashRequest).Failure, Is.EqualTo(FixedCastFailure.NoCharges));
            system.Step(10);
            Assert.That(system.Cast(in dashRequest).Success, Is.True, "one charge restored");
        }

        [Test]
        public void Projectiles_RunTheirImpactOnEveryReportedHit()
        {
            FixedAbilitySystem system = NewSystem();
            FixedRecordingWorld world = new FixedRecordingWorld(system);
            system.World = world;
            FixedUnit hero = Unit(system, 1);
            FixedUnit golem = Unit(system, 2, 100d, x: 5d);
            FixedAbilityBlueprint arrow = Ability("arrow", FixedTargeting.Unit, Node(FixedOps.Damage, 7d));
            arrow.Delivery = FixedDelivery.Projectile;
            arrow.ProjectileSpeedRaw = Q(20d);
            system.RegisterAbility(arrow);
            system.GrantAbility(hero.Id, "arrow");
            FixedCastRequest request = FixedCastRequest.AtUnit(hero.Id, "arrow", golem.Id);
            FixedCastResult cast = system.Cast(in request);
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(100d)), "nothing lands before the hit");
            Assert.That(world.Spawns[0].CastId, Is.EqualTo(cast.CastId));
            Assert.That(world.Spawns[0].Direction, Is.EqualTo(new FixedVec3(Q(5d), 0, 0)));
            Assert.That(system.NotifyProjectileHit(cast.CastId, golem.Id, golem.Position), Is.True);
            Assert.That(system.NotifyProjectileHit(cast.CastId, golem.Id, golem.Position), Is.True);
            Assert.That(golem.HealthRaw, Is.EqualTo(Q(86d)));
            system.ReleaseProjectileCast(cast.CastId);
            Assert.That(system.NotifyProjectileHit(cast.CastId, golem.Id, golem.Position), Is.False);
            system.Cast(in request);
            system.Step(system.PendingProjectileTimeoutTicks);
            Assert.That(system.PendingProjectileCount, Is.Zero, "the leak guard drops unreported casts");
        }

        [Test]
        public void Regeneration_IsExactPerSecond()
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1, 100d);
            hero.SetCurrent(FixedResources.Health, Q(10d));
            hero.SetBaseProperty(FixedProperties.HealthRegen, Q(1d));
            system.Step(30);
            Assert.That(hero.HealthRaw, Is.EqualTo(Q(11d)), "256 raw spread over 30 ticks without loss");
            system.Step(15);
            Assert.That(hero.HealthRaw, Is.EqualTo(Q(11d) + 128));
        }

        [Test]
        public void Rules_AreReplaceableAndInvalidateCachedProperties()
        {
            FixedAbilitySystem system = NewSystem();
            FixedUnit hero = Unit(system, 1);
            FixedUnit golem = Unit(system, 2);
            FixedModifierBlueprint buff = Modifier("buff")
                .WithProperty(FixedProperties.Armor, FixedPropertyOp.Mul, Q(1.5d))
                .WithProperty(FixedProperties.Armor, FixedPropertyOp.Mul, Q(1.5d));
            golem.SetBaseProperty(FixedProperties.Armor, Q(4d));
            system.Modifiers.Apply(buff, golem.Id, golem.Id);
            Assert.That(golem.GetProperty(FixedProperties.Armor), Is.EqualTo(Q(9d)), "compounding: 4 x 2.25");
            int version = system.Rules.Version;
            system.Rules.Aggregator = FixedAdditivePercentPropertyAggregator.Instance;
            Assert.That(system.Rules.Version, Is.GreaterThan(version));
            Assert.That(golem.GetProperty(FixedProperties.Armor), Is.EqualTo(Q(8d)), "additive: 4 x 2");
            system.Rules.Aggregator = null;
            Assert.That(system.Rules.Aggregator, Is.SameAs(FixedLayeredPropertyAggregator.Instance));

            system.Rules.Mitigation = new FlatArmor();
            Assert.That(system.Damage.Apply(hero.Id, golem.Id, Q(20d), FixedDamageTypes.Physical).HealthDamageRaw,
                Is.EqualTo(Q(11d)), "20 - 9 armor");
        }

        [Test]
        public void StateHash_IsReproducibleAndSeedSensitive()
        {
            ulong first = PlayHashedMatch(7u, true);
            ulong second = PlayHashedMatch(7u, false);
            Assert.That(second, Is.EqualTo(first), "the same inputs give the same hash; subscriptions do not count");
            HashSet<ulong> hashes = new HashSet<ulong>();
            for (uint seed = 1000u; seed < 5000u; seed += 1000u)
            {
                hashes.Add(PlayHashedMatch(seed, true));
            }

            Assert.That(hashes.Count, Is.GreaterThan(1), "other seeds roll other crits");
        }

        private static ulong PlayHashedMatch(uint seed, bool subscribe)
        {
            FixedAbilitySystem system = NewSystem();
            if (subscribe)
            {
                system.Events.SubscribeAny((in FixedEventArgs args) => { });
            }

            FixedUnit hero = Unit(system, 1, mana: 50d);
            FixedUnit golem = Unit(system, 2, 5000d, x: 2d);
            hero.SetBaseProperty(FixedProperties.CritChance, Q(0.5d));
            hero.SetBaseProperty(FixedProperties.CritMultiplier, Q(2d));
            FixedAbilityBlueprint strike = Ability("strike", FixedTargeting.Unit, Node(FixedOps.Damage, 10d));
            FixedModifierBlueprint burn = Modifier("burn", 90, FixedStackPolicy.Stack);
            burn.MaxStacks = 3;
            burn.TickIntervalTicks = 10;
            burn.TickEffects.Add(Node(FixedOps.Damage, 1d));
            strike.ImpactEffects.Add(new FixedEffectNode { OpId = FixedOps.ApplyModifier, ModifierId = "burn" });
            system.RegisterAbility(strike);
            system.RegisterModifier(burn);
            system.GrantAbility(hero.Id, "strike");
            for (int i = 0; i < 20; i++)
            {
                FixedCastRequest request = FixedCastRequest.AtUnit(hero.Id, "strike", golem.Id, seed + (uint)i);
                system.Cast(in request);
                system.Step(5);
            }

            return FixedStateHasher.Compute(system);
        }

        private static FixedModifierReaction Hook(string eventId, string mode, double amount)
        {
            FixedModifierReaction reaction = new FixedModifierReaction { EventId = eventId };
            FixedEffectNode node = Node(FixedOps.ModifyDamage, amount);
            node.CustomParam = mode;
            reaction.Effects.Add(node);
            return reaction;
        }

        private static FixedEffectNode ManaChange(double amount)
        {
            FixedEffectNode node = Node(FixedOps.ResourceChange, amount);
            node.ResourceId = FixedResources.Mana;
            return node;
        }

        private sealed class FlatArmor : IFixedMitigationRule
        {
            public int Mitigate(FixedDamageContext damage, FixedUnit target, int amountRaw)
            {
                return damage.IsPhysical ? amountRaw - target.GetProperty(FixedProperties.Armor) : amountRaw;
            }
        }
    }
}
