using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Neo.Abilities;
using NUnit.Framework;

namespace Neo.Editor.Tests.Abilities.Fixed
{
    /// <summary>
    ///     Conformance suite of the two cores: every scenario is written once against <see cref="IAbilityScenarioDriver" />
    ///     and played on the float core with <see cref="DeterministicAbilityProfile" /> (30 ticks per second, Q8.8
    ///     rounding) and on the fixed core with the same content converted by <see cref="FixedContentConverter" />.
    ///     Observations must agree within their tolerance (raw units of Q8.8, counts exactly); every difference is
    ///     printed. Known differences are listed in Docs/Abilities/FixedCore.md.
    /// </summary>
    public sealed class FixedConformanceTests
    {
        private const double Raw = 1d / 256d;

        private static readonly Dictionary<string, Action<IAbilityScenarioDriver, Observations>> All =
            new Dictionary<string, Action<IAbilityScenarioDriver, Observations>>
            {
                { "mitigation", Mitigation },
                { "crit_and_evasion", CritAndEvasion },
                { "stacking_poison_dot", StackingPoisonDot },
                { "stack_policies", StackPolicies },
                { "property_layering", PropertyLayering },
                { "attack_enhancer_and_mark", AttackEnhancerAndMark },
                { "thorns_on_grid", ThornsOnGrid },
                { "auto_defense_shield", AutoDefenseShield },
                { "every_nth_with_cooldown", EveryNthWithCooldown },
                { "state_and_lifecycle_triggers", StateAndLifecycleTriggers },
                { "formation_zone", FormationZone },
                { "aura_zone", AuraZone },
                { "auto_cast_cost_and_cooldown_reduction", AutoCastCostAndCdr },
                { "cone_and_nearest_area", ConeAndNearestArea },
                { "projectiles", Projectiles },
                { "chain_execute_heal_lifesteal_regen", ChainExecuteHealLifestealRegen },
                { "off_grid_fraction_known_difference", OffGridFraction },
                { "timer_boundary_known_difference", TimerBoundary },
                { "restart_ticks_on_refresh", RestartTicksOnRefresh }
            };

        public static IEnumerable<string> Scenarios => All.Keys;

        [TestCaseSource(nameof(Scenarios))]
        public void Scenario_PlaysTheSameOnBothCores(string scenario)
        {
            FloatScenarioDriver floatDriver = new FloatScenarioDriver();
            FixedScenarioDriver fixedDriver = new FixedScenarioDriver();
            Observations expected = new Observations();
            Observations actual = new Observations();
            All[scenario](floatDriver, expected);
            All[scenario](fixedDriver, actual);

            Assert.That(actual.Items.Count, Is.EqualTo(expected.Items.Count), "both cores made the same observations");
            List<string> failures = new List<string>();
            StringBuilder report = new StringBuilder();
            for (int i = 0; i < expected.Items.Count; i++)
            {
                Observation e = expected.Items[i];
                Observation a = actual.Items[i];
                Assert.That(a.Name, Is.EqualTo(e.Name));
                double difference = Math.Abs(a.Value - e.Value);
                if (difference > 0d)
                {
                    report.Append(e.Name).Append(": float ").Append(Format(e.Value)).Append(", fixed ")
                        .Append(Format(a.Value)).Append(" (").Append(Format(difference / Raw)).Append(" raw)\n");
                }

                if (difference > e.Tolerance + 1e-9)
                {
                    failures.Add(e.Name + ": float " + Format(e.Value) + " vs fixed " + Format(a.Value) + ", tolerance " +
                                 Format(e.Tolerance / Raw) + " raw");
                }
            }

            for (int i = 0; i < fixedDriver.Issues.Count; i++)
            {
                report.Append("conversion: ").Append(fixedDriver.Issues[i]).Append('\n');
            }

            TestContext.WriteLine(report.Length > 0 ? report.ToString() : "identical");
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static void Mitigation(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int armored = d.Unit(2, 600d, x: 1d);
            int broken = d.Unit(2, 600d, x: 2d);
            int resistant = d.Unit(2, 600d, x: 3d);
            d.Base(armored, AbilityProperties.Armor, 5d);
            d.Base(broken, AbilityProperties.Armor, -4d);
            d.Base(resistant, AbilityProperties.MagicResistPercent, 25d);
            d.Register(new[]
            {
                Strike("cf_phys", 40f, AbilityDamageTypes.Physical),
                Strike("cf_magic", 40f, AbilityDamageTypes.Magical),
                Strike("cf_pure", 40f, AbilityDamageTypes.Pure)
            }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "cf_phys");
            d.Grant(hero, "cf_magic");
            d.Grant(hero, "cf_pure");
            for (int i = 0; i < 3; i++)
            {
                o.Exact("phys armored " + i, d.Cast(hero, "cf_phys", armored) ? 1 : 0);
                d.Cast(hero, "cf_phys", broken);
                d.Cast(hero, "cf_magic", resistant);
                d.Cast(hero, "cf_pure", resistant);
                d.Step(1);
            }

            o.Value("armored", d.Health(armored), 3 * Raw);
            o.Value("negative armor", d.Health(broken), 3 * Raw);
            o.Value("resistant", d.Health(resistant), 3 * Raw);
            o.Exact("take_damage receipts", d.EventCount(AbilityEvents.TakeDamage));
        }

        private static void CritAndEvasion(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int golem = d.Unit(2, 10000d, x: 1d);
            d.Base(hero, AbilityProperties.CritChance, 0.25d);
            d.Base(hero, AbilityProperties.CritMultiplier, 2d);
            d.Base(golem, AbilityProperties.EvasionChance, 0.125d);
            d.Register(new[] { Strike("cf_strike", 40f, AbilityDamageTypes.Physical) }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "cf_strike");
            for (int i = 0; i < 64; i++)
            {
                d.Cast(hero, "cf_strike", golem);
                d.Step(1);
            }

            o.Exact("crits", d.EventCount(AbilityEvents.CriticalHit));
            o.Exact("evasions", d.EventCount(AbilityEvents.Evaded));
            o.Value("golem", d.Health(golem), 0d);
        }

        private static void StackingPoisonDot(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int golem = d.Unit(2, 600d, x: 2d);
            SetUp(d, AbilityTemplates.StackingPoison("cf_venom", 4f, 5, 5f, 5f, 8f), hero);
            for (int i = 0; i < 4; i++)
            {
                o.Exact("dart " + i, d.Cast(hero, "cf_venom_dart", golem) ? 1 : 0);
                o.Exact("stacks " + i, d.Stacks(golem, "cf_venom"));
                d.Step(30);
                o.Value("golem " + i, d.Health(golem), (i + 1) * 2 * Raw);
            }

            d.Step(100);
            o.Exact("remaining", d.RemainingTicks(golem, "cf_venom"));
            d.Step(60);
            o.Exact("expired", d.Count(golem, "cf_venom"));
            o.Value("golem end", d.Health(golem), 12 * Raw);
            o.Exact("ticks", d.EventCount(AbilityEvents.TakeDamage));
        }

        private static void StackPolicies(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int golem = d.Unit(2, 600d);
            ModifierBlueprint[] policies =
            {
                Policy("cf_refresh", ModifierStackPolicy.Refresh, 1),
                Policy("cf_stack", ModifierStackPolicy.Stack, 3),
                Policy("cf_unique", ModifierStackPolicy.Unique, 1),
                Policy("cf_prolong", ModifierStackPolicy.Prolong, 1),
                Policy("cf_capped", ModifierStackPolicy.IndependentCapped, 2),
                Policy("cf_independent", ModifierStackPolicy.Independent, 1)
            };
            d.Register(Array.Empty<AbilityBlueprint>(), policies);
            int[] delays = { 15, 15, 31, 90 };
            for (int round = 0; round < delays.Length; round++)
            {
                for (int p = 0; p < policies.Length; p++)
                {
                    if (round < 3)
                    {
                        d.Apply(policies[p].Id, hero, golem);
                    }

                    string id = policies[p].Id;
                    o.Exact(id + " count " + round, d.Count(golem, id));
                    o.Exact(id + " stacks " + round, d.Stacks(golem, id));
                    o.Exact(id + " remaining " + round, d.RemainingTicks(golem, id));
                }

                d.Step(delays[round]);
            }

            o.Exact("removed receipts", d.EventCount(AbilityEvents.ModifierRemoved));
        }

        private static void PropertyLayering(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int golem = d.Unit(2, 10000d, x: 1d);
            d.Base(golem, AbilityProperties.Armor, 2d);
            d.Register(new[] { Strike("cf_strike", 40f, AbilityDamageTypes.Physical) }, new[]
            {
                Property("cf_add", AbilityProperties.Armor, PropertyOp.Add, 3f),
                Property("cf_mul", AbilityProperties.Armor, PropertyOp.Mul, 1.5f),
                Property("cf_percent", AbilityProperties.Armor, PropertyOp.Mul, 1.1f),
                Property("cf_floor", AbilityProperties.Armor, PropertyOp.Max, 10f),
                Property("cf_out", AbilityProperties.OutgoingDamageMul, PropertyOp.Mul, 1.5f),
                Property("cf_in", AbilityProperties.IncomingDamageMul, PropertyOp.Mul, 0.75f),
                PerStack("cf_layers", AbilityProperties.Armor, 1f, 0.5f)
            });
            d.Grant(hero, "cf_strike");
            string[] golemModifiers = { "cf_add", "cf_mul", "cf_percent", "cf_floor", "cf_in", "cf_layers", "cf_layers" };
            for (int i = 0; i < golemModifiers.Length; i++)
            {
                d.Apply(golemModifiers[i], hero, golem);
                o.Value("armor " + i, d.Property(golem, AbilityProperties.Armor), Raw);
            }

            d.Apply("cf_out", hero, hero);
            d.Cast(hero, "cf_strike", golem);
            o.Value("golem", d.Health(golem), 2 * Raw);
        }

        private static void AttackEnhancerAndMark(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int golem = d.Unit(2, 2000d, x: 2d);
            d.Base(golem, AbilityProperties.Armor, 2d);
            d.Register(new[] { Strike("cf_strike", 20f, AbilityDamageTypes.Physical) }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "cf_strike");
            SetUp(d, AbilityTemplates.AttackEnhancer("cf_empower", 1.5f, 3, 8f, 10f), hero);
            SetUp(d, AbilityTemplates.Mark("cf_mark", 15f, 3, 10f, 10f, 6f), hero);
            d.Cast(hero, "cf_strike", golem);
            o.Value("plain", d.Health(golem), Raw);
            o.Exact("empower", d.Cast(hero, "cf_empower_cast") ? 1 : 0);
            for (int i = 0; i < 4; i++)
            {
                d.Step(1);
                d.Cast(hero, "cf_strike", golem);
                o.Value("empowered " + i, d.Health(golem), (i + 2) * Raw);
                o.Exact("empower charges " + i, d.Charges(hero, "cf_empower", 0));
            }

            o.Exact("mark", d.Cast(hero, "cf_mark", golem) ? 1 : 0);
            for (int i = 0; i < 4; i++)
            {
                d.Step(1);
                d.Cast(hero, "cf_strike", golem);
                o.Value("marked " + i, d.Health(golem), (i + 6) * Raw);
                o.Exact("mark charges " + i, d.Charges(golem, "cf_mark_mark", 0));
            }
        }

        private static void ThornsOnGrid(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int golem = d.Unit(2, 2000d, x: 2d);
            d.Register(new[] { Strike("cf_strike", 20f, AbilityDamageTypes.Physical) }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "cf_strike");
            SetUp(d, AbilityTemplates.Thorns("cf_thorns", 0.25f), golem);
            for (int i = 0; i < 5; i++)
            {
                d.Cast(hero, "cf_strike", golem);
                d.Step(1);
            }

            o.Value("hero", d.Health(hero), 0d);
            o.Exact("reflections", d.EventCount(AbilityEvents.DamageReflected));
        }

        private static void AutoDefenseShield(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 600d);
            int golem = d.Unit(2, 2000d, x: 2d);
            d.Register(new[] { Strike("cf_smash", 70f, AbilityDamageTypes.Magical) }, Array.Empty<ModifierBlueprint>());
            d.Grant(golem, "cf_smash");
            SetUp(d, AbilityTemplates.AutoDefenseShield("cf_barrier", 0.5f, 50f, 5f, 4f), hero);
            int[] gaps = { 10, 10, 10, 10, 60, 90, 10, 10 };
            for (int i = 0; i < gaps.Length; i++)
            {
                d.Cast(golem, "cf_smash", hero);
                o.Value("hero " + i, d.Health(hero), 0d);
                o.Exact("barrier " + i, d.Count(hero, "cf_barrier"));
                d.Step(gaps[i]);
            }

            o.Exact("absorbed receipts", d.EventCount(AbilityEvents.ShieldAbsorbed));
            o.Value("absorbed", d.EventSum(AbilityEvents.ShieldAbsorbed), 0d);
            o.Exact("broken", d.EventCount(AbilityEvents.ShieldBroken));
            o.Exact("alive", d.Alive(hero) ? 1 : 0);
        }

        private static void EveryNthWithCooldown(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d, mana: 100d);
            int golem = d.Unit(2, 5000d, x: 2d);
            ModifierBlueprint counter = new ModifierBlueprint { Id = "cf_counter", StackPolicy = ModifierStackPolicy.Unique };
            ModifierEventReaction every = new ModifierEventReaction
            {
                EventId = AbilityEvents.AttackLanded,
                Trigger = ModifierTrigger.EveryNth,
                EveryN = 3,
                Cooldown = 0.5f
            };
            every.Effects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.ResourceChange,
                Target = EffectTargetSelector.Caster,
                ResourceId = AbilityResourceIds.Mana,
                Amount = -1f
            });
            counter.EventReactions.Add(every);
            d.Register(new[] { Strike("cf_strike", 5f, AbilityDamageTypes.Pure) }, new[] { counter });
            d.Grant(hero, "cf_strike");
            d.Apply("cf_counter", hero, hero);
            for (int i = 0; i < 24; i++)
            {
                d.Cast(hero, "cf_strike", golem);
                d.Step(4);
                o.Value("mana " + i, d.Mana(hero), 0d);
            }
        }

        private static void StateAndLifecycleTriggers(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d, mana: 100d);
            ModifierBlueprint stun = new ModifierBlueprint { Id = "cf_stun", Duration = 1f, StackPolicy = ModifierStackPolicy.Refresh };
            stun.States.Add(AbilityStates.Stunned);
            ModifierBlueprint watcher = new ModifierBlueprint { Id = "cf_watch", StackPolicy = ModifierStackPolicy.Unique };
            watcher.EventReactions.Add(ManaReaction(ModifierTrigger.StateEnter, AbilityStates.Stunned, -5f));
            watcher.EventReactions.Add(ManaReaction(ModifierTrigger.StateExit, AbilityStates.Stunned, -1f));
            ModifierBlueprint lifecycle = new ModifierBlueprint
                { Id = "cf_life", Duration = 0.5f, StackPolicy = ModifierStackPolicy.Independent };
            lifecycle.EventReactions.Add(ManaReaction(ModifierTrigger.Applied, null, -2f));
            lifecycle.EventReactions.Add(ManaReaction(ModifierTrigger.Removed, null, -3f));
            d.Register(Array.Empty<AbilityBlueprint>(), new[] { stun, watcher, lifecycle });
            d.Apply("cf_watch", hero, hero);
            for (int i = 0; i < 3; i++)
            {
                d.Apply("cf_stun", hero, hero);
                d.Apply("cf_life", hero, hero);
                o.Value("mana applied " + i, d.Mana(hero), 0d);
                d.Step(20);
                o.Value("mana mid " + i, d.Mana(hero), 0d);
                d.Step(20);
                o.Value("mana after " + i, d.Mana(hero), 0d);
            }

            o.Exact("entered", d.EventCount(AbilityEvents.StateEntered));
            o.Exact("exited", d.EventCount(AbilityEvents.StateExited));
        }

        private static void FormationZone(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int golem = d.Unit(2, 600d, x: 5d);
            int imp = d.Unit(2, 180d, x: 6d, z: 1d);
            int ally = d.Unit(1, 300d, x: 5d, z: -1d);
            int far = d.Unit(2, 300d, x: 15d);
            d.Base(golem, AbilityProperties.MoveSpeed, 5d);
            SetUp(d, AbilityTemplates.FormationZone("cf_formation", 4f, 6f, 1f, 15f, 0.6f, 12f, 12f), hero);
            o.Exact("cast", d.CastAt(hero, "cf_formation", 5d, 0d) ? 1 : 0);
            o.Exact("zones", d.ZoneCount);
            o.Value("slowed speed", d.Property(golem, AbilityProperties.MoveSpeed), 0d);
            d.Step(15);
            for (int second = 0; second < 8; second++)
            {
                d.Step(30);
                o.Value("golem " + second, d.Health(golem), (second + 2) * Raw);
                o.Value("imp " + second, d.Health(imp), (second + 2) * Raw);
                o.Exact("slow " + second, d.Count(golem, "cf_formation_slow"));
                o.Exact("zones " + second, d.ZoneCount);
            }

            o.Value("ally", d.Health(ally), 0d);
            o.Value("far", d.Health(far), 0d);
            o.Exact("spawned", d.EventCount(AbilityEvents.ZoneSpawned));
            o.Exact("expired", d.EventCount(AbilityEvents.ZoneExpired));
        }

        private static void AuraZone(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int near = d.Unit(1, 300d, x: 3d);
            int far = d.Unit(1, 300d, x: 9d);
            int enemy = d.Unit(2, 300d, x: 2d);
            SetUp(d, AbilityTemplates.Aura("cf_banner", 6f, 5f, 1.15f, 2f, 20f), hero);
            o.Exact("cast", d.Cast(hero, "cf_banner") ? 1 : 0);
            for (int i = 0; i < 6; i++)
            {
                o.Value("near armor " + i, d.Property(near, AbilityProperties.Armor), 0d);
                o.Value("near outgoing " + i, d.Property(near, AbilityProperties.OutgoingDamageMul), Raw);
                o.Exact("far buff " + i, d.Count(far, "cf_banner_buff"));
                o.Exact("enemy buff " + i, d.Count(enemy, "cf_banner_buff"));
                o.Exact("hero buff " + i, d.Count(hero, "cf_banner_buff"));
                d.Step(20);
            }

            o.Exact("zones", d.ZoneCount);
        }

        private static void AutoCastCostAndCdr(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d, mana: 100d);
            int golem = d.Unit(2, 600d, x: 4d);
            d.Base(hero, AbilityProperties.CooldownReductionPercent, 20d);
            AbilityTemplateBundle bundle = AbilityTemplates.AutoCastTechnique("cf_bolt", 1f, 20f, 8f);
            bundle.Abilities[0].Cooldown = 1.5f;
            bundle.Abilities[0].Costs.Add(new AbilityCost(AbilityResourceIds.Mana, 10f));
            SetUp(d, bundle, hero);
            for (int second = 0; second < 12; second++)
            {
                d.Step(30);
                o.Value("golem " + second, d.Health(golem), 0d);
                o.Value("mana " + second, d.Mana(hero), 0d);
            }

            o.Exact("casts", d.EventCount(AbilityEvents.AbilityCast));
        }

        private static void ConeAndNearestArea(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int ahead = d.Unit(2, 100d, x: 3d);
            int inside = d.Unit(2, 100d, x: 3d, z: 2d);
            int outside = d.Unit(2, 100d, x: 3d, z: 3.5d);
            int behind = d.Unit(2, 100d, x: -3d);
            int friend = d.Unit(1, 100d, x: 2d, z: 0.5d);
            AbilityBlueprint cone = new AbilityBlueprint { Id = "cf_cone", Targeting = TargetingMode.Point };
            cone.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage, Target = EffectTargetSelector.ConeFromCaster, Angle = 90f, Radius = 6f,
                Amount = 10f, DamageType = AbilityDamageTypes.Pure, TeamFilter = AbilityTeamFilter.Enemies
            });
            AbilityBlueprint nova = new AbilityBlueprint { Id = "cf_nova", Targeting = TargetingMode.Point };
            nova.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage, Target = EffectTargetSelector.AreaAroundTarget, Radius = 3f, MaxTargets = 2,
                Amount = 7f, DamageType = AbilityDamageTypes.Pure, TeamFilter = AbilityTeamFilter.Enemies
            });
            d.Register(new[] { cone, nova }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "cf_cone");
            d.Grant(hero, "cf_nova");
            d.CastAt(hero, "cf_cone", 5d, 0d);
            d.CastAt(hero, "cf_nova", 3d, 2.4d);
            o.Value("ahead", d.Health(ahead), 0d);
            o.Value("inside", d.Health(inside), 0d);
            o.Value("outside", d.Health(outside), 0d);
            o.Value("behind", d.Health(behind), 0d);
            o.Value("friend", d.Health(friend), 0d);
        }

        private static void Projectiles(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int golem = d.Unit(2, 600d, x: 6d);
            AbilityBlueprint payload = new AbilityBlueprint { Id = "cf_payload" };
            payload.ImpactEffects.Add(new EffectNodeData
                { OpId = AbilityEffectOps.Damage, Amount = 25f, DamageType = AbilityDamageTypes.Magical });
            AbilityBlueprint launcher = new AbilityBlueprint { Id = "cf_launcher", Targeting = TargetingMode.Unit };
            launcher.ImpactEffects.Add(new EffectNodeData
                { OpId = AbilityEffectOps.Projectile, AbilityId = "cf_payload", ArchetypeId = "cf_orb" });
            AbilityBlueprint arrow = new AbilityBlueprint
            {
                Id = "cf_arrow", Targeting = TargetingMode.Unit, Delivery = AbilityDeliveryType.Projectile,
                ProjectileArchetypeId = "cf_arrow_view"
            };
            arrow.ImpactEffects.Add(new EffectNodeData
                { OpId = AbilityEffectOps.Damage, Amount = 12f, DamageType = AbilityDamageTypes.Physical });
            d.Register(new[] { payload, launcher, arrow }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "cf_launcher");
            d.Grant(hero, "cf_arrow");
            d.Cast(hero, "cf_launcher", golem);
            o.Exact("orb hit", d.HitLastProjectile(golem) ? 1 : 0);
            o.Value("after orb", d.Health(golem), 0d);
            d.Cast(hero, "cf_arrow", golem);
            o.Exact("arrow hit", d.HitLastProjectile(golem) ? 1 : 0);
            o.Exact("pierce", d.HitLastProjectile(golem) ? 1 : 0);
            o.Value("after arrows", d.Health(golem), Raw);
        }

        private static void ChainExecuteHealLifestealRegen(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int first = d.Unit(2, 200d, x: 2d);
            int second = d.Unit(2, 200d, x: 4d);
            int third = d.Unit(2, 200d, x: 6d);
            int lonely = d.Unit(2, 200d, x: 20d);
            int golem = d.Unit(2, 600d, x: 1d);
            d.Base(hero, AbilityProperties.LifestealPercent, 50d);
            d.Base(hero, AbilityProperties.HealingReceivedMul, 1.5d);
            d.Base(hero, AbilityProperties.HealthRegen, 2d);
            AbilityBlueprint chain = new AbilityBlueprint { Id = "cf_chain", Targeting = TargetingMode.Unit };
            chain.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Chain, Amount = 40f, Radius = 4f, MaxTargets = 3, CustomParam = "0.5",
                DamageType = AbilityDamageTypes.Magical, TeamFilter = AbilityTeamFilter.Enemies
            });
            AbilityBlueprint execute = new AbilityBlueprint { Id = "cf_execute", Targeting = TargetingMode.Unit };
            execute.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Execute, Amount = 0.5f, CustomParam = "missing", DamageType = AbilityDamageTypes.Pure
            });
            AbilityBlueprint heal = new AbilityBlueprint { Id = "cf_heal", Targeting = TargetingMode.Self };
            heal.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.Heal, Target = EffectTargetSelector.Caster, Amount = 10f });
            d.Register(new[] { chain, execute, heal, Strike("cf_smash", 120f, AbilityDamageTypes.Pure) },
                Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "cf_chain");
            d.Grant(hero, "cf_execute");
            d.Grant(hero, "cf_heal");
            d.Grant(golem, "cf_smash");
            d.Cast(hero, "cf_chain", first);
            o.Value("first", d.Health(first), 0d);
            o.Value("second", d.Health(second), 0d);
            o.Value("third", d.Health(third), 0d);
            o.Value("lonely", d.Health(lonely), 0d);
            d.Cast(hero, "cf_execute", first);
            o.Value("executed", d.Health(first), Raw);
            d.Cast(golem, "cf_smash", hero);
            o.Value("hero hit", d.Health(hero), 0d);
            d.Cast(hero, "cf_heal");
            o.Value("hero healed", d.Health(hero), 0d);
            d.Cast(hero, "cf_chain", second);
            o.Value("hero lifesteal", d.Health(hero), 2 * Raw);
            d.Step(90);
            o.Value("hero regen", d.Health(hero), 3 * Raw);
        }

        private static void OffGridFraction(IAbilityScenarioDriver d, Observations o)
        {
            // WHY: 0.3 is not on the Q8.8 grid (77/256 = 0.30078); a reflect fraction (an effect amount, not a property
            // multiplier) keeps the rounded value, so every reflected hit of 20 differs by 4 raw. Listed as a known
            // difference; author fractions on the grid or use Q16.
            int hero = d.Unit(1, 300d);
            int golem = d.Unit(2, 2000d, x: 2d);
            d.Register(new[] { Strike("cf_strike", 20f, AbilityDamageTypes.Physical) }, Array.Empty<ModifierBlueprint>());
            d.Grant(hero, "cf_strike");
            SetUp(d, AbilityTemplates.Thorns("cf_thorns", 0.3f), golem);
            for (int i = 0; i < 5; i++)
            {
                d.Cast(hero, "cf_strike", golem);
                d.Step(1);
            }

            o.Value("hero", d.Health(hero), 5 * 4 * Raw);
        }

        private static void RestartTicksOnRefresh(IAbilityScenarioDriver d, Observations o)
        {
            int hero = d.Unit(1, 300d);
            int continued = d.Unit(2, 100d, x: 1d);
            int restarted = d.Unit(2, 100d, x: 2d);
            int stacked = d.Unit(2, 100d, x: 3d);
            d.Register(Array.Empty<AbilityBlueprint>(), new[]
            {
                TickingBurn("cf_burn_continue", ModifierStackPolicy.Refresh, false),
                TickingBurn("cf_burn_restart", ModifierStackPolicy.Refresh, true),
                TickingBurn("cf_burn_stack", ModifierStackPolicy.Stack, true)
            });
            d.Apply("cf_burn_continue", hero, continued);
            d.Apply("cf_burn_restart", hero, restarted);
            d.Apply("cf_burn_stack", hero, stacked);
            d.Step(20);
            d.Apply("cf_burn_continue", hero, continued);
            d.Apply("cf_burn_restart", hero, restarted);
            d.Apply("cf_burn_stack", hero, stacked);
            int[] checkpoints = { 5, 10, 10, 10, 10, 10, 20 };
            for (int i = 0; i < checkpoints.Length; i++)
            {
                d.Step(checkpoints[i]);
                o.Value("continued " + i, d.Health(continued), 0d);
                o.Value("restarted " + i, d.Health(restarted), 0d);
                o.Value("stacked " + i, d.Health(stacked), 0d);
            }

            d.Step(40);
            o.Value("continued end", d.Health(continued), 0d);
            o.Value("restarted end", d.Health(restarted), 0d);
            o.Value("stacked end", d.Health(stacked), 0d);
        }

        private static ModifierBlueprint TickingBurn(string id, ModifierStackPolicy policy, bool restart)
        {
            ModifierBlueprint burn = new ModifierBlueprint
            {
                Id = id, Duration = 3f, TickInterval = 1f, StackPolicy = policy, MaxStacks = 3,
                RestartTicksOnRefresh = restart
            };
            burn.TickEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage, Target = EffectTargetSelector.Target, Amount = 1f, AmountPerStack = 1f,
                DamageType = AbilityDamageTypes.Pure
            });
            return burn;
        }

        private static void TimerBoundary(IAbilityScenarioDriver d, Observations o)
        {
            // WHY: the float core subtracts the float step (0.033333335 s at 30 ticks per second) from every timer; the
            // rounding accumulates, so a timer of a whole number of ticks can end one tick late there. The fixed core
            // counts ticks exactly. Listed as a known difference; observe one tick away from an expiry to compare.
            int hero = d.Unit(1, 300d);
            d.Register(Array.Empty<AbilityBlueprint>(), new[] { Policy("cf_timer", ModifierStackPolicy.Refresh, 1) });
            d.Apply("cf_timer", hero, hero);
            d.Step(60);
            o.Value("alive on the expiry tick", d.Count(hero, "cf_timer"), 1d);
            d.Step(1);
            o.Exact("gone a tick later", d.Count(hero, "cf_timer"));
        }

        private static AbilityBlueprint Strike(string id, float amount, string damageType)
        {
            AbilityBlueprint strike = new AbilityBlueprint
                { Id = id, Targeting = TargetingMode.Unit, TeamFilter = AbilityTeamFilter.Enemies };
            strike.ImpactEffects.Add(new EffectNodeData { OpId = AbilityEffectOps.Damage, Amount = amount, DamageType = damageType });
            return strike;
        }

        private static ModifierBlueprint Policy(string id, ModifierStackPolicy policy, int maxStacks)
        {
            return new ModifierBlueprint { Id = id, Duration = 2f, StackPolicy = policy, MaxStacks = maxStacks };
        }

        private static ModifierBlueprint Property(string id, string propertyId, PropertyOp op, float value)
        {
            ModifierBlueprint modifier = new ModifierBlueprint { Id = id, StackPolicy = ModifierStackPolicy.Unique };
            modifier.Properties.Add(new PropertyContribution(propertyId, op, value));
            return modifier;
        }

        private static ModifierBlueprint PerStack(string id, string propertyId, float value, float perStack)
        {
            ModifierBlueprint modifier = new ModifierBlueprint
                { Id = id, StackPolicy = ModifierStackPolicy.Stack, MaxStacks = 3 };
            modifier.Properties.Add(new PropertyContribution(propertyId, PropertyOp.Add, value, perStack));
            return modifier;
        }

        private static ModifierEventReaction ManaReaction(ModifierTrigger trigger, string stateId, float amount)
        {
            ModifierEventReaction reaction = new ModifierEventReaction { Trigger = trigger, StateId = stateId };
            reaction.Effects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.ResourceChange, Target = EffectTargetSelector.Target,
                ResourceId = AbilityResourceIds.Mana, Amount = amount
            });
            return reaction;
        }

        private static void SetUp(IAbilityScenarioDriver d, AbilityTemplateBundle bundle, int unit)
        {
            d.Register(bundle.Abilities, bundle.Modifiers);
            for (int i = 0; i < bundle.GrantAbilityIds.Count; i++)
            {
                d.Grant(unit, bundle.GrantAbilityIds[i]);
            }

            for (int i = 0; i < bundle.InnateModifierIds.Count; i++)
            {
                d.Apply(bundle.InnateModifierIds[i], unit, unit);
            }
        }

        private static string Format(double value)
        {
            return value.ToString("0.#####", CultureInfo.InvariantCulture);
        }

        internal readonly struct Observation
        {
            public readonly string Name;
            public readonly double Value;
            public readonly double Tolerance;

            public Observation(string name, double value, double tolerance)
            {
                Name = name;
                Value = value;
                Tolerance = tolerance;
            }
        }

        internal sealed class Observations
        {
            public readonly List<Observation> Items = new List<Observation>();

            public void Value(string name, double value, double tolerance)
            {
                Items.Add(new Observation(name, value, tolerance));
            }

            public void Exact(string name, double value)
            {
                Items.Add(new Observation(name, value, 0d));
            }
        }
    }
}
